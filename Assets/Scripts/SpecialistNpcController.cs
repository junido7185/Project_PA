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

    // 현재 가공 중인 레시피 + 이번 방문에서 남은 연속 가공 횟수.
    private RecipeData _currentRecipe;
    private int _remainingCrafts;

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
        if (targetWorkbench == null) TryCacheWorkbench();
        if (targetWorkbench == null)
        {
            _debugLastAction = "작업대를 찾을 수 없음";
            Debug.LogWarning($"🔧 {DisplayName}: 전문 분야({specialty})에 맞는 작업대를 찾지 못했습니다.");
            return;
        }

        _currentRecipe = recipe;
        _remainingCrafts = CalculateCraftCount();
        _craftTimer = 0f;

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.SetDestination(targetWorkbench.transform.position);
        }

        ChangeState(State.MovingToWorkbench);
        Debug.Log($"🔧 {DisplayName}: 작업대({targetWorkbench.displayName})로 출발! [{recipe.recipeName}] ×{_remainingCrafts}");
    }

    void UpdateMovingToWorkbench()
    {
        if (_agent == null || !_agent.isOnNavMesh) { ChangeState(State.Idle); return; }

        if (!_agent.pathPending && _agent.remainingDistance < arriveDistance)
        {
            ChangeState(State.CraftingAtBench);
            _craftTimer = 0f;
        }
    }

    // -------- 상태: CraftingAtBench --------

    void UpdateCraftingAtBench()
    {
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

    /// <summary>HiringService.InjectProfile() 에서 SendMessage 로 호출된다.</summary>
    public void ApplySpecialty(NpcSpecialty newSpecialty)
    {
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

    // -------- 레시피 선택 --------

    /// <summary>재료가 충분한 첫 번째 레시피를 반환한다. 없으면 null.</summary>
    RecipeData FindViableRecipe()
    {
        if (assignedRecipes == null || Inventory.instance == null) return null;

        foreach (var recipe in assignedRecipes)
        {
            if (recipe == null || recipe.outputItem == null) continue;
            if (recipe.ingredients == null || recipe.ingredients.Count == 0) continue;

            // 작업대 종류 매칭
            if (recipe.requiredWorkbench != WorkbenchType.None
                && targetWorkbench != null
                && recipe.requiredWorkbench != targetWorkbench.workbenchType)
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
