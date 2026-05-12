using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 생산형 NPC 에이전트 — 자원을 채집·생산하여 플레이어에게 납품한다.
//
// 역할 분리:
//   NpcController(소비형) — 상점 쇼핑, 구매 의사결정
//   ProducerNpcController(생산형) — 채집, 생산, 납품 (이 파일)
//   두 컴포넌트를 한 GameObject 에 동시에 붙일 수도 있다 (생산+소비 복합 NPC).
//
// FSM:
//   Idle
//   ──(확률)──► MovingToWorkspot ──(도착)──► Working
//   Working   ──(생산)──► Idle                      (임계 미달)
//             ──(생산)──► MovingToDropOff            (임계 도달)
//   MovingToDropOff ──(도착)──► OfferingItems
//   OfferingItems   ──(납품 완료/실패)──► Idle
//
// MBTI 반영:
//   traitEI: I형(음수)일수록 집중력↑ → 생산 주기 단축 (최대 30% 단축)
//   traitJP: J형(음수)일수록 계획적 작업 → 생산량 증가 (최대 20% 증가)
//   workEfficiency: 1.0 기준 배수. 직업 숙련도를 표현.
//
// 납품 메커니즘:
//   NPC 가 dropOffPoint 에 도착하면 각 아이템을 플레이어에게 자동으로 "판매 시도"한다.
//   EconomyService.TrySpend(단가) 성공 → Inventory.AddItem()
//   인벤토리 풀 또는 잔액 부족 시 → 해당 아이템은 NPC 인벤토리에 유지하고 다음에 재시도.
public class ProducerNpcController : MonoBehaviour
{
    public enum State { Idle, MovingToWorkspot, Working, MovingToDropOff, OfferingItems }

    [Header("정체성")]
    [Tooltip("MBTI/가중치. null이면 평균형 기본값 사용")]
    public NpcProfile profile;

    [Tooltip("생산할 품목과 속도를 정의하는 ScriptableObject")]
    public ProductionData productionData;

    [Tooltip("생산 직종. 계절 보정(SeasonModifier)에 사용")]
    public NpcSpecialty specialty = NpcSpecialty.Farmer;

    [Tooltip("결정론 RNG 시드. 0이면 이름 해시 기반 자동 설정")]
    public int randomSeed = 0;

    [Header("위치")]
    [Tooltip("채집/작업 지점 Transform. 없으면 현재 위치에서 제자리 작업")]
    public Transform workSpot;

    [Tooltip("납품 목적지 Transform. null이면 'Shop' 태그 오브젝트를 자동 탐색")]
    public Transform dropOffPoint;

    [Header("행동 파라미터")]
    [Tooltip("Idle 상태에서 다음 판단까지의 간격(초)")]
    public float idleTickInterval = 5f;

    [Tooltip("Idle 상태에서 일하러 갈 기본 확률 (MBTI 보정 후 사용)")]
    [Range(0f, 1f)]
    public float baseWorkProbability = 0.4f;

    [Tooltip("위치 도달 판정 거리(미터)")]
    public float arriveDistance = 1.5f;

    [Header("디버그 (읽기 전용)")]
    [SerializeField] private State _debugState;
    [SerializeField] private int _debugInventoryCount;
    [SerializeField] private float _debugProductionTimer;
    [SerializeField] private string _debugLastAction;
    [SerializeField] private bool _debugSchedulePaused;

    // -------- 내부 상태 --------

    private NavMeshAgent _agent;
    private System.Random _rng;

    // NpcScheduleController 에서 제어하는 일시 정지 플래그.
    // true 이면 Update 전체가 차단된다 (Work 페이즈 외 시간대).
    private bool _schedulePaused = false;

    // NPC 인벤토리 — 생산물을 담아 둔다.
    private readonly List<ItemInstance> _npcInventory = new List<ItemInstance>();

    private State _currentState;
    private float _idleTimer;
    private float _productionTimer;
    private bool _restoredFromSave;

    // 작업 중 남은 생산량 (사이클당)
    private int _pendingProductionAmount;

    private string DisplayName =>
        profile != null && !string.IsNullOrEmpty(profile.npcName) ? profile.npcName : gameObject.name;

    // -------- Unity 생명주기 --------

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        int seed = randomSeed != 0
            ? randomSeed
            : ((profile != null ? profile.npcName : gameObject.name) + "::Producer::" + gameObject.name).GetHashCode();
        _rng = new System.Random(seed);
    }

    void Start()
    {
        TryCacheDropOffPoint();
        if (!_restoredFromSave)
            ChangeState(State.Idle);
    }

    void Update()
    {
        // 디버그 표시 갱신
        _debugState = _currentState;
        _debugInventoryCount = TotalInventoryCount();
        _debugProductionTimer = _productionTimer;
        _debugSchedulePaused = _schedulePaused;

        // 스케줄에 의해 일시 정지 중이면 처리 차단
        if (_schedulePaused) return;

        switch (_currentState)
        {
            case State.Idle:              UpdateIdle();              break;
            case State.MovingToWorkspot:  UpdateMovingToWorkspot();  break;
            case State.Working:           UpdateWorking();           break;
            case State.MovingToDropOff:   UpdateMovingToDropOff();   break;
            case State.OfferingItems:     UpdateOfferingItems();     break;
        }
    }

    // -------- 상태: Idle --------

    void UpdateIdle()
    {
        _idleTimer += Time.deltaTime;
        if (_idleTimer < idleTickInterval) return;
        _idleTimer = 0f;

        // 인벤토리가 임계값 이상이면 바로 납품으로
        if (productionData != null && TotalInventoryCount() >= productionData.deliveryThreshold)
        {
            BeginDelivery();
            return;
        }

        // 인벤토리가 가득 차면 생산 중단 — 납품 대기
        if (productionData != null && TotalInventoryCount() >= productionData.maxInventoryCount)
        {
            _debugLastAction = "인벤토리 가득 차 대기 중";
            return;
        }

        // 일하러 갈 확률: I형일수록 집에서 일하는 성향이 강함 (더 자주 일터로)
        float workProb = baseWorkProbability + 0.15f * GetNegativeTrait(p => p.traitEI);
        workProb = Mathf.Clamp01(workProb);

        if (_rng.NextDouble() < workProb)
        {
            BeginWork();
        }
    }

    // -------- 상태: MovingToWorkspot --------

    void BeginWork()
    {
        if (productionData == null || productionData.producedItem == null)
        {
            _debugLastAction = "ProductionData 없음 — 작업 취소";
            return;
        }

        _productionTimer = 0f;
        _pendingProductionAmount = CalculateProductionAmount();

        // workSpot 이 없으면 현재 위치에서 제자리 작업
        if (workSpot != null && _agent != null && _agent.isOnNavMesh)
        {
            _agent.SetDestination(workSpot.position);
            ChangeState(State.MovingToWorkspot);
            Debug.Log($"🔨 {DisplayName}: 작업장으로 출발! (생산 예정: {_pendingProductionAmount}개)");
        }
        else
        {
            // 작업장 없음 — 제자리에서 바로 작업
            ChangeState(State.Working);
        }
    }

    void UpdateMovingToWorkspot()
    {
        if (_agent == null || !_agent.isOnNavMesh) { ChangeState(State.Idle); return; }

        if (!_agent.pathPending && _agent.remainingDistance < arriveDistance)
        {
            ChangeState(State.Working);
        }
    }

    // -------- 상태: Working --------

    void UpdateWorking()
    {
        if (productionData == null || productionData.producedItem == null)
        {
            ChangeState(State.Idle);
            return;
        }

        // 인벤토리 가득 참 확인 — 작업 중단
        if (TotalInventoryCount() >= productionData.maxInventoryCount)
        {
            _debugLastAction = "인벤토리 가득 차 작업 중단";
            ChangeState(State.Idle);
            return;
        }

        _productionTimer += Time.deltaTime;
        float effectiveInterval = CalculateEffectiveInterval();

        if (_productionTimer < effectiveInterval) return;
        _productionTimer = 0f;

        // 아이템 생산
        ProduceItems();

        // 납품 임계 도달 시 배달 시작
        if (TotalInventoryCount() >= productionData.deliveryThreshold)
        {
            BeginDelivery();
        }
    }

    void ProduceItems()
    {
        int amount = _pendingProductionAmount;
        // 인벤토리 한도를 초과하지 않도록 클램프
        int remaining = productionData.maxInventoryCount - TotalInventoryCount();
        amount = Mathf.Min(amount, remaining);
        if (amount <= 0) return;

        // 기존 스택에 합산하거나 새 ItemInstance 생성
        ItemInstance existing = _npcInventory.Find(i => i.data == productionData.producedItem);
        if (existing != null)
        {
            existing.count += amount;
        }
        else
        {
            _npcInventory.Add(new ItemInstance(productionData.producedItem, amount));
        }

        _debugLastAction = $"생산 완료: {productionData.producedItem.itemName} ×{amount}";
        Debug.Log($"🔨 {DisplayName}: {productionData.producedItem.itemName} ×{amount} 생산 (총 {TotalInventoryCount()}개)");
    }

    // -------- 상태: MovingToDropOff --------

    void BeginDelivery()
    {
        TryCacheDropOffPoint();
        if (dropOffPoint == null)
        {
            _debugLastAction = "납품 목적지 없음 — 대기";
            ChangeState(State.Idle);
            return;
        }

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.SetDestination(dropOffPoint.position);
        }

        ChangeState(State.MovingToDropOff);
        Debug.Log($"🚚 {DisplayName}: 납품하러 출발! ({TotalInventoryCount()}개)");
    }

    void UpdateMovingToDropOff()
    {
        if (_agent == null || !_agent.isOnNavMesh) { ChangeState(State.Idle); return; }

        if (!_agent.pathPending && _agent.remainingDistance < arriveDistance)
        {
            ChangeState(State.OfferingItems);
        }
    }

    // -------- 상태: OfferingItems --------

    void UpdateOfferingItems()
    {
        if (_npcInventory.Count == 0)
        {
            Debug.Log($"🚚 {DisplayName}: 납품 완료! Idle로 복귀.");
            ChangeState(State.Idle);
            return;
        }

        // 인벤토리 순회하며 자동 판매 시도
        bool anySold = false;
        for (int i = _npcInventory.Count - 1; i >= 0; i--)
        {
            ItemInstance inst = _npcInventory[i];
            if (inst == null || inst.data == null) { _npcInventory.RemoveAt(i); continue; }

            int unitPrice = productionData != null ? productionData.EffectiveDeliveryPrice : inst.data.basePrice;
            int totalCost = unitPrice * inst.count;

            // 플레이어 잔액 차감 시도
            if (EconomyService.Instance == null ||
                !EconomyService.Instance.TrySpend(totalCost, $"NPC납품[{DisplayName}]: {inst.data.itemName}"))
            {
                _debugLastAction = $"잔액 부족 — {inst.data.itemName} 납품 보류";
                Debug.Log($"🚚 {DisplayName}: 잔액 부족 — {inst.data.itemName}×{inst.count} 납품 보류 ({totalCost}G 필요)");
                continue;
            }

            // 플레이어 인벤토리에 추가 시도
            if (Inventory.instance == null || !Inventory.instance.AddItem(inst.data, inst.count))
            {
                // 인벤토리 풀 — 지출 취소는 불가능하므로 재고는 제거 (돈은 이미 빠짐)
                // 설계: 인벤토리 풀 상태에서는 "버려진" 것으로 처리. 실제 서비스에서는 UI 경고 추가 권장.
                Debug.LogWarning($"⚠️ {DisplayName}: 플레이어 인벤토리 풀 — {inst.data.itemName}×{inst.count} 유실. UI 경고 필요.");
                _npcInventory.RemoveAt(i);
                anySold = true;
                continue;
            }

            Debug.Log($"🚚 {DisplayName}: {inst.data.itemName}×{inst.count} 납품! 플레이어 -{totalCost}G");
            _npcInventory.RemoveAt(i);
            anySold = true;
        }

        if (!anySold)
        {
            // 돈이 부족해서 전혀 못 팔았을 경우 — 인벤토리 들고 Idle 복귀, 다음 사이클에 재시도
            Debug.Log($"🚚 {DisplayName}: 납품 불가 (잔액 부족). 다음에 재시도.");
            ChangeState(State.Idle);
            return;
        }

        // 모두 처리했으면 종료
        if (_npcInventory.Count == 0)
        {
            Debug.Log($"🚚 {DisplayName}: 납품 전량 완료. Idle로 복귀.");
        }
        ChangeState(State.Idle);
    }

    // -------- NpcScheduleController 공개 API --------

    /// <summary>
    /// 스케줄에 의한 일시 정지.
    /// 현재 FSM 을 Idle 로 되돌리고 NavMesh 이동을 정지한다.
    /// 보관 중인 NPC 인벤토리는 유지한다 — 다음 Work 페이즈 재개 시 납품 시도.
    /// </summary>
    public void Pause()
    {
        if (_schedulePaused) return;
        _schedulePaused = true;

        if (_agent != null && _agent.isOnNavMesh && !_agent.isStopped)
            _agent.ResetPath();

        ChangeState(State.Idle);
        _debugLastAction = "스케줄에 의해 정지됨";
    }

    /// <summary>스케줄에 의한 재개. FSM 이 Idle 에서 자연스럽게 다음 행동을 선택한다.</summary>
    public void Resume()
    {
        if (!_schedulePaused) return;
        _schedulePaused = false;
        _idleTimer = 0f; // 즉시 틱 평가되지 않도록 타이머 리셋
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
        TryCacheDropOffPoint();

        if (restoredState == State.Working && _pendingProductionAmount <= 0)
            _pendingProductionAmount = CalculateProductionAmount();

        ChangeState(restoredState);

        if (_agent == null || !_agent.isOnNavMesh) return;

        if (restoredState == State.MovingToWorkspot && workSpot != null)
        {
            _agent.SetDestination(workSpot.position);
        }
        else if (restoredState == State.MovingToDropOff && dropOffPoint != null)
        {
            _agent.SetDestination(dropOffPoint.position);
        }
        else
        {
            _agent.ResetPath();
        }
    }

    // -------- 상태 전환 --------

    void ChangeState(State newState)
    {
        _currentState = newState;
        _idleTimer = 0f;

        // 이동 정지가 필요한 상태 전환
        if (newState == State.Idle || newState == State.Working || newState == State.OfferingItems)
        {
            if (_agent != null && _agent.isOnNavMesh && !_agent.isStopped)
                _agent.ResetPath();
        }
    }

    // -------- 참조 캐싱 --------

    void TryCacheDropOffPoint()
    {
        if (dropOffPoint != null) return;

        GameObject shopGo = GameObject.FindGameObjectWithTag("Shop");
        if (shopGo != null)
        {
            dropOffPoint = shopGo.transform;
        }
    }

    // -------- MBTI 헬퍼 --------

    // traitEI, traitJP 등에서 "음수일수록 높은 값" 특성을 추출 (I형/J형 강조 가산용)
    private float GetNegativeTrait(System.Func<NpcProfile, float> selector)
    {
        if (profile == null) return 0f;
        return Mathf.Clamp01(-selector(profile));  // 음수를 양수 보정치로 변환
    }

    private float GetTrait(System.Func<NpcProfile, float> selector)
    {
        return profile != null ? selector(profile) : 0f;
    }

    // -------- 생산 계산 --------

    // 실제 생산 주기(초):  baseInterval / (workEfficiency × (1 + 0.3 × (-traitEI)))
    //   I형(-1) → ÷1.3 (빠름), E형(+1) → ÷0.7 (느림)
    private float CalculateEffectiveInterval()
    {
        if (productionData == null) return 30f;

        float workEff = profile != null ? Mathf.Max(0.1f, profile.workEfficiency) : 1f;
        float eiFactor = 1f + 0.3f * (-GetTrait(p => p.traitEI));
        eiFactor = Mathf.Max(0.1f, eiFactor);

        float seasonMod = SeasonModifier.GetProductionModifier(specialty);
        return productionData.baseProductionInterval / (workEff * eiFactor * Mathf.Max(0.1f, seasonMod));
    }

    // 실제 생산량:  max(1, round(baseAmount × workEfficiency × (1 + 0.2 × (-traitJP))))
    //   J형(-1) → ×1.2 (더 많이), P형(+1) → ×0.8 (덜 생산)
    private int CalculateProductionAmount()
    {
        if (productionData == null) return 1;

        float workEff = profile != null ? Mathf.Max(0.1f, profile.workEfficiency) : 1f;
        float jpFactor = 1f + 0.2f * (-GetTrait(p => p.traitJP));

        return Mathf.Max(1, Mathf.RoundToInt(productionData.baseProductionAmount * workEff * jpFactor));
    }

    private int TotalInventoryCount()
    {
        int total = 0;
        foreach (var inst in _npcInventory)
            if (inst != null) total += inst.count;
        return total;
    }

    // -------- 에디터 디버그 --------

    void OnDrawGizmosSelected()
    {
        if (workSpot != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(workSpot.position, 0.5f);
            Gizmos.DrawLine(transform.position, workSpot.position);
        }

        if (dropOffPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(dropOffPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, dropOffPoint.position);
        }
    }
}
