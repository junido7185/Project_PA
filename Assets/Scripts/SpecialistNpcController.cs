using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 전문가 NPC — Workbench 에서 레시피를 자동 가공하는 에이전트.
// Docs/05 §4 "전문가 시너지" 를 구현한다.
//
// 역할 분리:
//   NpcController(소비형) — 상점 쇼핑, 구매 의사결정
//   ProducerNpcController(생산형) — 채집, 생산, 납품
//   SpecialistNpcController(전문가) — Workbench 에서 레시피 가공 (이 파일)
//   세 컴포넌트를 한 GameObject 에 조합할 수 있다 (생산+소비+가공 복합 NPC).
//
// FSM:
//   Idle
//   ──(확률)──► MovingToWorkbench ──(도착)──► CraftingAtBench
//   CraftingAtBench ──(가공 완료)──► Idle
//
// MBTI 반영:
//   traitEI: I형(음수)일수록 집중력↑ → 가공 주기 단축 (최대 30% 단축)
//   traitJP: J형(음수)일수록 계획적 → 연속 가공 횟수 증가 (최대 2회 추가)
//   workEfficiency: 1.0 기준 배수.
//
// HiringService 연동:
//   SendMessage("ApplySpecialty", NpcSpecialty) 으로 specialty 를 외부에서 주입할 수 있다.
//   주입된 specialty 에 따라 NpcSpecialtyMapping.GetWorkbenchType() 로 대응 작업대를 결정.
public class SpecialistNpcController : MonoBehaviour
{
    const float ApproachSampleRadius = 0.9f;
    const float ApproachSourceTolerance = 0.25f;

    public enum State { Idle, MovingToWorkbench, CraftingAtBench }

    [Header("정체성")]
    [Tooltip("MBTI/가중치. null이면 평균형 기본값 사용")]
    public NpcProfile profile;

    [Tooltip("전문 분야. HiringService 에서 SendMessage 로 주입되거나 Inspector 에서 직접 설정.")]
    public NpcSpecialty specialty = NpcSpecialty.Carpenter;

    [Tooltip("결정론 RNG 시드. 0이면 이름 해시 기반 자동 설정")]
    public int randomSeed = 0;

    [Header("작업대")]
    [Tooltip("사용할 Workbench. null 이면 specialty 에 맞는 타입을 씬에서 자동 탐색.")]
    public Workbench targetWorkbench;

    [Header("레시피")]
    [Tooltip("이 전문가가 가공할 수 있는 레시피 목록. 순서대로 재료 보유 여부를 확인해 첫 번째 가능한 레시피를 선택.")]
    public List<RecipeData> assignedRecipes = new List<RecipeData>();

    [Header("행동 파라미터")]
    [Tooltip("Idle 상태에서 다음 판단까지의 간격(초)")]
    public float idleTickInterval = 5f;

    [Tooltip("Idle 상태에서 작업대로 이동할 기본 확률 (MBTI 보정 후 사용)")]
    [Range(0f, 1f)]
    public float baseWorkProbability = 0.5f;

    [Tooltip("한 사이클에 기본 가공 시간(초). MBTI 로 보정된다.")]
    public float baseCraftInterval = 20f;

    [Tooltip("위치 도달 판정 거리(미터)")]
    public float arriveDistance = 1.5f;

    [Header("디버그 (읽기 전용)")]
    [SerializeField] private State _debugState;
    [SerializeField] private string _debugLastAction;
    [SerializeField] private float _debugCraftTimer;
    [SerializeField] private bool _debugSchedulePaused;

    // -------- 내부 상태 --------

    private NavMeshAgent _agent;
    private System.Random _rng;

    private bool _schedulePaused = false;

    private State _currentState;
    private float _idleTimer;
    private float _craftTimer;
    private bool _restoredFromSave;

    // 현재 가공 중인 레시피 + 이번 방문에서 남은 연속 가공 횟수.
    private RecipeData _currentRecipe;
    private int _remainingCrafts;
    private readonly List<Vector3> _approachSourcePoints = new List<Vector3>(4);
    private readonly List<Vector2Int> _approachZoneCells = new List<Vector2Int>(4);
    private Vector3 _currentApproachSource;
    private Vector3 _currentApproachPoint;
    private string _approachReservationKey = string.Empty;

    // 같은 interaction 셀에 여러 전문 주민이 겹치지 않게 하는 세션 범위 예약이다.
    // 파괴된 Unity 오브젝트는 다음 예약 시 정리되므로 domain reload 비활성 환경도 안전하다.
    static readonly Dictionary<string, SpecialistNpcController> ApproachReservations =
        new Dictionary<string, SpecialistNpcController>();

    public Vector3 CurrentWorkbenchApproachPoint => _currentApproachPoint;
    public bool HasWorkbenchApproachReservation => !string.IsNullOrEmpty(_approachReservationKey);

    private string DisplayName =>
        profile != null && !string.IsNullOrEmpty(profile.npcName) ? profile.npcName : gameObject.name;

    // -------- Unity 생명주기 --------

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        int seed = randomSeed != 0
            ? randomSeed
            : ((profile != null ? profile.npcName : gameObject.name) + "::Specialist::" + gameObject.name).GetHashCode();
        _rng = new System.Random(seed);
    }

    void Start()
    {
        TryCacheWorkbench();
        if (!_restoredFromSave)
            ChangeState(State.Idle);
    }

    void Update()
    {
        _debugState = _currentState;
        _debugCraftTimer = _craftTimer;
        _debugSchedulePaused = _schedulePaused;

        if (_schedulePaused) return;

        switch (_currentState)
        {
            case State.Idle:              UpdateIdle();              break;
            case State.MovingToWorkbench: UpdateMovingToWorkbench(); break;
            case State.CraftingAtBench:   UpdateCraftingAtBench();   break;
        }
    }

    void OnDisable()
    {
        ReleaseApproachReservation();
    }

    void OnDestroy()
    {
        ReleaseApproachReservation();
    }

    // -------- 상태: Idle --------

    void UpdateIdle()
    {
        _idleTimer += Time.deltaTime;
        if (_idleTimer < idleTickInterval) return;
        _idleTimer = 0f;

        // 가공 가능한 레시피가 있는지 사전 검사 (재료 보유)
        RecipeData viable = FindViableRecipe();
        if (viable == null)
        {
            _debugLastAction = "가공 가능한 레시피 없음 (재료 부족)";
            return;
        }

        // I형일수록 작업 확률 증가
        float workProb = baseWorkProbability + 0.15f * Mathf.Clamp01(-GetTrait(p => p.traitEI));
        workProb = Mathf.Clamp01(workProb);

        if (_rng.NextDouble() < workProb)
        {
            BeginCraftSession(viable);
        }
    }

    // -------- 상태: MovingToWorkbench --------

    void BeginCraftSession(RecipeData recipe)
    {
        WorkbenchType requiredType = recipe != null && recipe.requiredWorkbench != WorkbenchType.None
            ? recipe.requiredWorkbench
            : NpcSpecialtyMapping.GetWorkbenchType(specialty);
        if (!TryReserveReachableWorkbench(requiredType, out string reason))
        {
            _debugLastAction = $"접근 가능한 작업대 없음: {reason}";
            Debug.LogWarning($"🔧 {DisplayName}: 전문 분야({specialty}) 작업대 접근 실패 — {reason}");
            return;
        }

        _currentRecipe = recipe;
        _remainingCrafts = CalculateCraftCount();
        _craftTimer = 0f;

        ChangeState(State.MovingToWorkbench);
        Debug.Log($"🔧 {DisplayName}: 작업대({targetWorkbench.displayName}) 전면 접근점으로 출발! [{recipe.recipeName}] ×{_remainingCrafts}");
    }

    void UpdateMovingToWorkbench()
    {
        if (_agent == null || !_agent.isOnNavMesh) { ChangeState(State.Idle); return; }
        if (!IsApproachReservationValid())
        {
            _debugLastAction = "작업대 이동/회수로 접근 예약 무효";
            targetWorkbench = null;
            ChangeState(State.Idle);
            return;
        }
        if (!_agent.pathPending && (_agent.pathStatus != NavMeshPathStatus.PathComplete
            || float.IsInfinity(_agent.remainingDistance)))
        {
            _debugLastAction = "작업대 전면까지 완전 경로 없음";
            ChangeState(State.Idle);
            return;
        }

        float arrivalRadius = Mathf.Max(_agent.stoppingDistance + 0.1f,
            Mathf.Clamp(arriveDistance, 0.2f, 0.65f));
        if (!_agent.pathPending && _agent.remainingDistance <= arrivalRadius)
        {
            FaceWorkbench();
            ChangeState(State.CraftingAtBench);
            _craftTimer = 0f;
        }
    }

    // -------- 상태: CraftingAtBench --------

    void UpdateCraftingAtBench()
    {
        if (!IsApproachReservationValid())
        {
            _debugLastAction = "작업대 이동/회수로 가공 중단";
            targetWorkbench = null;
            ChangeState(State.Idle);
            return;
        }
        if (_currentRecipe == null || _remainingCrafts <= 0)
        {
            _debugLastAction = "가공 세션 완료";
            Debug.Log($"🔧 {DisplayName}: 가공 세션 완료. Idle 복귀.");
            ChangeState(State.Idle);
            return;
        }

        _craftTimer += Time.deltaTime;
        float effectiveInterval = CalculateEffectiveInterval();

        if (_craftTimer < effectiveInterval) return;
        _craftTimer = 0f;

        // CraftingService 를 통한 가공 실행
        bool success = CraftingService.TryCraft(_currentRecipe, targetWorkbench);
        if (success)
        {
            _remainingCrafts--;
            _debugLastAction = $"가공 성공: {_currentRecipe.recipeName} (잔여 {_remainingCrafts}회)";
            Debug.Log($"🔧 {DisplayName}: {_currentRecipe.recipeName} 가공 성공! (잔여 {_remainingCrafts}회)");
        }
        else
        {
            // 재료 부족 또는 인벤토리 풀 — 세션 종료
            _debugLastAction = $"가공 실패 (재료/인벤토리) — 세션 중단";
            Debug.Log($"🔧 {DisplayName}: 가공 실패. 세션 중단. Idle 복귀.");
            _remainingCrafts = 0;
        }

        // 연속 가공 여부
        if (_remainingCrafts <= 0)
        {
            ChangeState(State.Idle);
        }
        else
        {
            // 다음 가공 전에 재료 재확인 — 레시피가 바뀔 수 있다
            RecipeData nextViable = FindViableRecipe();
            if (nextViable == null)
            {
                Debug.Log($"🔧 {DisplayName}: 추가 가공 불가 (재료 소진). Idle 복귀.");
                ChangeState(State.Idle);
            }
            else
            {
                _currentRecipe = nextViable;
            }
        }
    }

    // -------- NpcScheduleController / HiringService 공개 API --------

    public void Pause()
    {
        if (_schedulePaused) return;
        _schedulePaused = true;

        if (_agent != null && _agent.isOnNavMesh && !_agent.isStopped)
            _agent.ResetPath();

        _currentRecipe = null;
        _remainingCrafts = 0;
        ChangeState(State.Idle);
        _debugLastAction = "스케줄에 의해 정지됨";
    }

    public void Resume()
    {
        if (!_schedulePaused) return;
        _schedulePaused = false;
        _idleTimer = 0f;
        _debugLastAction = "스케줄에 의해 재개됨";
    }

    public string GetFsmState()
    {
        return _currentState.ToString();
    }

    public void RestoreFsmState(string stateName)
    {
        if (!System.Enum.TryParse(stateName, out State restoredState))
            restoredState = State.Idle;

        _schedulePaused = false;
        _restoredFromSave = true;
        _idleTimer = 0f;
        if (restoredState != State.Idle && _currentRecipe == null)
        {
            _currentRecipe = FindViableRecipe();
            _remainingCrafts = Mathf.Max(1, _remainingCrafts);
            if (_currentRecipe == null)
                restoredState = State.Idle;
        }

        // 저장된 작업 중 상태는 현재 배치/회전/NavMesh를 기준으로 전면 접근점을 다시 잡는다.
        if (restoredState != State.Idle)
        {
            WorkbenchType requiredType = _currentRecipe != null
                ? _currentRecipe.requiredWorkbench
                : NpcSpecialtyMapping.GetWorkbenchType(specialty);
            restoredState = TryReserveReachableWorkbench(requiredType, out _)
                ? State.MovingToWorkbench
                : State.Idle;
        }
        ChangeState(restoredState);
    }

    /// <summary>HiringService.InjectProfile() 에서 SendMessage 로 호출된다.</summary>
    public void ApplySpecialty(NpcSpecialty newSpecialty)
    {
        ReleaseApproachReservation();
        specialty = newSpecialty;
        targetWorkbench = null;  // 캐시 무효화 — 다음 사용 시 재탐색
        Debug.Log($"🔧 {DisplayName}: 전문 분야 → {specialty}");
    }

    // -------- 상태 전환 --------

    void ChangeState(State newState)
    {
        _currentState = newState;
        _idleTimer = 0f;

        if (newState == State.Idle)
        {
            ReleaseApproachReservation();
            if (_agent != null && _agent.isOnNavMesh && !_agent.isStopped)
                _agent.ResetPath();
        }
    }

    // -------- 작업대 탐색 --------

    void TryCacheWorkbench()
    {
        if (targetWorkbench != null) return;

        WorkbenchType wbType = NpcSpecialtyMapping.GetWorkbenchType(specialty);
        if (wbType == WorkbenchType.None) return;

        // 씬에서 매칭되는 작업대를 찾는다 (가장 가까운 것 우선)
        Workbench[] all = FindObjectsByType<Workbench>(FindObjectsSortMode.None);
        float bestDist = float.MaxValue;

        foreach (var wb in all)
        {
            if (wb.workbenchType != wbType) continue;
            float dist = Vector3.Distance(transform.position, wb.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                targetWorkbench = wb;
            }
        }

        if (targetWorkbench != null)
            Debug.Log($"🔧 {DisplayName}: 작업대 캐시 → {targetWorkbench.displayName} ({wbType})");
    }

    bool TryReserveReachableWorkbench(WorkbenchType requiredType, out string reason)
    {
        reason = string.Empty;
        ReleaseApproachReservation();
        PruneApproachReservations();

        if (requiredType == WorkbenchType.None)
        {
            reason = "전문 분야에 대응하는 작업대 타입이 없습니다.";
            return false;
        }
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
        {
            reason = "전문 주민이 NavMesh 위에 있지 않습니다.";
            return false;
        }

        Workbench bestWorkbench = null;
        Vector3 bestSource = Vector3.zero;
        Vector3 bestNavPoint = Vector3.zero;
        string bestKey = string.Empty;
        float bestLength = float.PositiveInfinity;
        int areaMask = _agent.areaMask != 0 ? _agent.areaMask : NavMesh.AllAreas;

        foreach (Workbench candidate in FindObjectsByType<Workbench>(FindObjectsSortMode.None))
        {
            if (candidate == null || candidate.workbenchType != requiredType
                || !candidate.gameObject.activeInHierarchy)
                continue;

            ResolveApproachSourcePoints(candidate, _approachSourcePoints, _approachZoneCells,
                out string placementId);
            for (int i = 0; i < _approachSourcePoints.Count; i++)
            {
                Vector3 source = _approachSourcePoints[i];
                string key = BuildApproachReservationKey(candidate, placementId, source,
                    i < _approachZoneCells.Count ? _approachZoneCells[i] : (Vector2Int?)null);
                if (ApproachReservations.TryGetValue(key, out SpecialistNpcController owner)
                    && owner != null && owner != this)
                    continue;
                if (!NavMesh.SamplePosition(source, out NavMeshHit hit, ApproachSampleRadius, areaMask))
                    continue;

                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(_agent.transform.position, hit.position, areaMask, path)
                    || path.status != NavMeshPathStatus.PathComplete)
                    continue;

                float length = CalculatePathLength(path);
                if (length >= bestLength) continue;
                bestLength = length;
                bestWorkbench = candidate;
                bestSource = source;
                bestNavPoint = hit.position;
                bestKey = key;
            }
        }

        if (bestWorkbench == null)
        {
            reason = "비어 있고 완전한 NavMesh 경로를 가진 전면 interaction 셀이 없습니다.";
            return false;
        }

        ApproachReservations[bestKey] = this;
        _approachReservationKey = bestKey;
        _currentApproachSource = bestSource;
        _currentApproachPoint = bestNavPoint;
        targetWorkbench = bestWorkbench;
        if (!_agent.SetDestination(bestNavPoint))
        {
            ReleaseApproachReservation();
            targetWorkbench = null;
            reason = "선택한 전면 접근점을 NavMesh 목적지로 설정하지 못했습니다.";
            return false;
        }
        return true;
    }

    void ResolveApproachSourcePoints(Workbench workbench, List<Vector3> points,
        List<Vector2Int> cells, out string placementId)
    {
        points.Clear();
        cells.Clear();
        placementId = string.Empty;

        ShopCustomizationController customization = ShopCustomizationController.Instance;
        if (customization != null
            && customization.TryGetNpcApproachPoints(workbench, points, cells, out placementId))
            return;

        // 배치 zone 밖의 구형 작업대도 collider 앞면을 사용해 장애물 원점 이동을 피한다.
        float frontDistance = 1.15f;
        BoxCollider box = workbench.GetComponent<BoxCollider>();
        if (box != null)
        {
            float scaledDepth = Mathf.Abs(box.size.z * workbench.transform.lossyScale.z);
            frontDistance = scaledDepth * 0.5f + Mathf.Max(0.15f, _agent.radius) + 0.15f;
        }
        Vector3 fallback = workbench.transform.position - workbench.transform.forward * frontDistance;
        fallback.y = workbench.transform.position.y + 0.04f;
        points.Add(fallback);
    }

    bool IsApproachReservationValid()
    {
        if (targetWorkbench == null || !targetWorkbench.gameObject.activeInHierarchy
            || string.IsNullOrEmpty(_approachReservationKey)
            || !ApproachReservations.TryGetValue(_approachReservationKey, out var owner)
            || owner != this)
            return false;

        ResolveApproachSourcePoints(targetWorkbench, _approachSourcePoints, _approachZoneCells, out _);
        foreach (Vector3 source in _approachSourcePoints)
        {
            Vector2 delta = new Vector2(source.x - _currentApproachSource.x,
                source.z - _currentApproachSource.z);
            if (delta.sqrMagnitude <= ApproachSourceTolerance * ApproachSourceTolerance)
                return true;
        }
        return false;
    }

    void ReleaseApproachReservation()
    {
        if (!string.IsNullOrEmpty(_approachReservationKey)
            && ApproachReservations.TryGetValue(_approachReservationKey, out var owner)
            && owner == this)
            ApproachReservations.Remove(_approachReservationKey);
        _approachReservationKey = string.Empty;
        _currentApproachSource = Vector3.zero;
        _currentApproachPoint = Vector3.zero;
    }

    static void PruneApproachReservations()
    {
        var stale = new List<string>();
        foreach (var pair in ApproachReservations)
            if (pair.Value == null || !pair.Value.gameObject.activeInHierarchy)
                stale.Add(pair.Key);
        foreach (string key in stale) ApproachReservations.Remove(key);
    }

    static string BuildApproachReservationKey(Workbench workbench, string placementId,
        Vector3 source, Vector2Int? zoneCell)
    {
        if (!string.IsNullOrEmpty(placementId) && zoneCell.HasValue)
            return $"{placementId}:{zoneCell.Value.x}:{zoneCell.Value.y}";
        return $"workbench:{workbench.GetInstanceID()}:{Mathf.RoundToInt(source.x * 10f)}:{Mathf.RoundToInt(source.z * 10f)}";
    }

    static float CalculatePathLength(NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length < 2) return 0f;
        float result = 0f;
        for (int i = 1; i < path.corners.Length; i++)
            result += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        return result;
    }

    void FaceWorkbench()
    {
        if (targetWorkbench == null) return;
        Vector3 direction = targetWorkbench.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    // -------- 레시피 선택 --------

    /// <summary>재료가 충분한 첫 번째 레시피를 반환한다. 없으면 null.</summary>
    RecipeData FindViableRecipe()
    {
        if (assignedRecipes == null || Inventory.instance == null) return null;
        WorkbenchType specialtyWorkbench = NpcSpecialtyMapping.GetWorkbenchType(specialty);

        foreach (var recipe in assignedRecipes)
        {
            if (recipe == null || recipe.outputItem == null) continue;
            if (recipe.ingredients == null || recipe.ingredients.Count == 0) continue;

            // 작업대 종류 매칭
            if (recipe.requiredWorkbench != WorkbenchType.None
                && recipe.requiredWorkbench != specialtyWorkbench)
                continue;

            // 티어 잠금
            if (TierService.Instance != null && !TierService.Instance.IsUnlocked(recipe.requiredTier))
                continue;

            // 히든 블루프린트 잠금
            if (FriendshipService.Instance != null
                && !FriendshipService.Instance.IsRecipeUnlocked(recipe))
                continue;

            // 재료 보유 확인
            bool hasAll = true;
            foreach (var ing in recipe.ingredients)
            {
                if (ing == null || ing.item == null || ing.count <= 0) continue;
                if (!Inventory.instance.HasItems(ing.item, ing.count))
                {
                    hasAll = false;
                    break;
                }
            }
            if (hasAll) return recipe;
        }
        return null;
    }

    // -------- MBTI 헬퍼 --------

    private float GetTrait(System.Func<NpcProfile, float> selector)
    {
        return profile != null ? selector(profile) : 0f;
    }

    // 가공 소요 시간: baseCraftInterval / (workEfficiency × (1 + 0.3 × (-traitEI)))
    private float CalculateEffectiveInterval()
    {
        float workEff = profile != null ? Mathf.Max(0.1f, profile.workEfficiency) : 1f;
        float eiFactor = 1f + 0.3f * Mathf.Clamp01(-GetTrait(p => p.traitEI));
        float seasonMod = SeasonModifier.GetProductionModifier(specialty);
        return baseCraftInterval / (workEff * Mathf.Max(0.1f, eiFactor) * Mathf.Max(0.1f, seasonMod));
    }

    // 연속 가공 횟수: 1 + round(0.5 × (-traitJP))
    //   J형(-1) → 최대 2회 추가 = 3회 연속
    //   P형(+1) → 0회 추가 = 1회만
    private int CalculateCraftCount()
    {
        float jp = GetTrait(p => p.traitJP);
        int bonus = Mathf.RoundToInt(0.5f * Mathf.Clamp01(-jp));
        return Mathf.Max(1, 1 + bonus);
    }
}
