using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 마을을 돌아다니며 생활·소비 활동을 하는 NPC 에이전트.
//
// 설계 의도:
// - NpcProfile(ScriptableObject)로 정체성(MBTI/가중치)을 주입받아 행동에 반영한다.
// - RNG는 System.Random 인스턴스 기반으로 결정론적이다.
// - 돈 관련 변경은 EconomyService 단일 경로를 거친다.
// - 쇼핑 의사결정은 PurchaseEvaluator(순수 함수)에 위임한다.
//
// FSM:
//   Idle ──(확률)──► MovingToShop ──(도착)──► BrowsingShop ──(만족/소진)──► Idle
//   BrowsingShop 단계에서 NPC 는 여러 ShopSlot 을 차례로 둘러보며 각 슬롯 앞에서 잠시 "구경" 후
//   PurchaseEvaluator 를 호출해 구매 여부를 결정한다.
public class NpcController : MonoBehaviour
{
    public enum State { Idle, MovingToShop, BrowsingShop }
    public State currentState = State.Idle;

    [Header("정체성")]
    [Tooltip("MBTI/가중치가 담긴 ScriptableObject. null이면 평균형 기본값 사용")]
    public NpcProfile profile;

    [Tooltip("결정론 RNG 시드. 0이면 이름 해시 기반으로 자동 설정")]
    public int randomSeed = 0;

    [Header("행동 파라미터")]
    public Transform shopLocation;
    [Tooltip("Idle 상태에서 다음 판단까지의 간격(초)")]
    public float idleTickInterval = 3f;
    [Tooltip("배회 반경(미터)")]
    public float wanderRadius = 5f;

    [Header("쇼핑 파라미터")]
    [Tooltip("한 번의 방문에서 둘러볼 최대 슬롯 수")]
    public int maxSlotsPerVisit = 3;
    [Tooltip("슬롯 앞에 도착한 뒤 평가까지 기다리는 '구경' 시간(초)")]
    public float browseDurationAtSlot = 1.5f;
    [Tooltip("슬롯에 도달한 것으로 간주하는 거리")]
    public float slotArriveDistance = 1.2f;
    [Tooltip("상점 입구 도착 판정 거리")]
    public float shopArriveDistance = 1.5f;

    [Header("디버그 (읽기 전용)")]
    [SerializeField] private float debugShoppingProbability;
    [SerializeField] private string debugLastDecision;

    private NavMeshAgent agent;
    private float idleTimer = 0f;

    // 결정론 RNG
    private System.Random _rng;

    // 쇼핑 세션 상태
    private Shop _activeShop;
    private ShopSlot _currentSlotTarget;
    private readonly HashSet<ShopSlot> _visitedSlots = new HashSet<ShopSlot>();
    private bool _arrivedAtSlot;
    private float _browseTimer;

    private string DisplayName => profile != null && !string.IsNullOrEmpty(profile.npcName) ? profile.npcName : gameObject.name;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        int seed = randomSeed != 0
            ? randomSeed
            : ((profile != null ? profile.npcName : gameObject.name) + "::" + gameObject.name).GetHashCode();
        _rng = new System.Random(seed);
    }

    void Start()
    {
        TryCacheShopReference();
        ChangeState(State.Idle);
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                UpdateIdle();
                break;
            case State.MovingToShop:
                UpdateMovingToShop();
                break;
            case State.BrowsingShop:
                UpdateBrowsingShop();
                break;
        }
    }

    // ---------- 상태: Idle ----------

    void UpdateIdle()
    {
        idleTimer += Time.deltaTime;
        if (idleTimer < idleTickInterval) return;
        idleTimer = 0f;

        // 배회 — 외향(E) 성향일수록 배회 반경이 넓다.
        float effectiveRadius = wanderRadius * (1f + 0.5f * GetTrait(p => p.traitEI));
        Vector3 randomDir = RandomInsideUnitSphere() * effectiveRadius + transform.position;

        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, effectiveRadius, NavMesh.AllAreas))
        {
            if (agent != null && agent.isOnNavMesh) agent.SetDestination(hit.position);
        }

        // 쇼핑 확률 계산 (E/N/사교형이 더 자주 외출)
        float shoppingProb = 0.10f
            + 0.08f * GetTrait(p => p.traitEI)
            + 0.04f * GetTrait(p => p.traitSN)
            + 0.05f * GetSocialBias();
        shoppingProb = Mathf.Clamp(shoppingProb, 0.01f, 0.6f);
        debugShoppingProbability = shoppingProb;

        if (_rng.NextDouble() < shoppingProb)
        {
            BeginShoppingVisit();
        }
    }

    // ---------- 상태: MovingToShop ----------

    void BeginShoppingVisit()
    {
        if (shopLocation == null) TryCacheShopReference();
        if (shopLocation == null || _activeShop == null)
        {
            Debug.LogWarning($"🤖 {DisplayName}: 상점을 찾을 수 없어 쇼핑을 취소합니다.");
            return;
        }

        _visitedSlots.Clear();
        _currentSlotTarget = null;
        _arrivedAtSlot = false;
        _browseTimer = 0f;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(shopLocation.position);
        }
        ChangeState(State.MovingToShop);
        Debug.Log($"🤖 {DisplayName}: 쇼핑하러 출발!");
    }

    void UpdateMovingToShop()
    {
        if (agent == null || !agent.isOnNavMesh) { ChangeState(State.Idle); return; }
        if (!agent.pathPending && agent.remainingDistance < shopArriveDistance)
        {
            // 상점 도착 — 첫 슬롯 선정으로 넘어간다.
            ChangeState(State.BrowsingShop);
            PickNextSlotToBrowse();
        }
    }

    // ---------- 상태: BrowsingShop ----------

    void UpdateBrowsingShop()
    {
        if (_activeShop == null || _currentSlotTarget == null)
        {
            PickNextSlotToBrowse();
            return;
        }

        // 슬롯이 다른 NPC 에게 이미 팔려 비었으면 다음 슬롯으로.
        if (_currentSlotTarget.IsEmpty)
        {
            _currentSlotTarget = null;
            PickNextSlotToBrowse();
            return;
        }

        if (!_arrivedAtSlot)
        {
            // 슬롯으로 이동 중
            if (agent != null && !agent.pathPending && agent.remainingDistance < slotArriveDistance)
            {
                _arrivedAtSlot = true;
                _browseTimer = 0f;
            }
        }
        else
        {
            // 슬롯 앞에서 구경 중
            _browseTimer += Time.deltaTime;
            if (_browseTimer >= browseDurationAtSlot)
            {
                EvaluateCurrentSlot();
            }
        }
    }

    void PickNextSlotToBrowse()
    {
        _arrivedAtSlot = false;
        _browseTimer = 0f;
        _currentSlotTarget = null;

        if (_activeShop == null)
        {
            EndShoppingVisit("상점 참조 없음");
            return;
        }

        if (_visitedSlots.Count >= maxSlotsPerVisit)
        {
            EndShoppingVisit($"최대 방문 슬롯 {maxSlotsPerVisit}개 도달");
            return;
        }

        var available = _activeShop.GetAvailableSlots();
        // 아직 방문하지 않은 슬롯만 후보에 넣는다.
        for (int i = available.Count - 1; i >= 0; i--)
        {
            if (_visitedSlots.Contains(available[i])) available.RemoveAt(i);
        }

        if (available.Count == 0)
        {
            EndShoppingVisit("둘러볼 슬롯 없음");
            return;
        }

        int idx = _rng.Next(available.Count);
        _currentSlotTarget = available[idx];
        _visitedSlots.Add(_currentSlotTarget);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(_currentSlotTarget.transform.position);
        }
    }

    void EvaluateCurrentSlot()
    {
        if (_currentSlotTarget == null || _currentSlotTarget.IsEmpty)
        {
            _currentSlotTarget = null;
            PickNextSlotToBrowse();
            return;
        }

        PurchaseEvaluator.Result result = PurchaseEvaluator.Evaluate(profile, _currentSlotTarget, _rng);
        debugLastDecision = result.reason;
        Debug.Log($"🤖 {DisplayName} 평가 [{_currentSlotTarget.currentItem?.data?.itemName}]: {result.reason}");

        if (result.willBuy)
        {
            if (_currentSlotTarget.TryPurchaseByNpc(DisplayName, out int paid))
            {
                Debug.Log($"🤖 {DisplayName}: 구매 성공! +{paid}G");
                EndShoppingVisit("구매 완료");
                return;
            }
        }

        // 패스 — 다음 슬롯으로
        _currentSlotTarget = null;
        PickNextSlotToBrowse();
    }

    void EndShoppingVisit(string reason)
    {
        Debug.Log($"🤖 {DisplayName}: 쇼핑 종료 ({reason})");
        _currentSlotTarget = null;
        _visitedSlots.Clear();
        _arrivedAtSlot = false;
        _browseTimer = 0f;
        ChangeState(State.Idle);
    }

    // ---------- 상태 전환 ----------

    void ChangeState(State newState)
    {
        currentState = newState;
    }

    // ---------- 상점 참조 캐싱 ----------

    void TryCacheShopReference()
    {
        if (shopLocation == null)
        {
            GameObject shopGo = GameObject.FindGameObjectWithTag("Shop");
            if (shopGo != null) shopLocation = shopGo.transform;
        }

        if (_activeShop == null && shopLocation != null)
        {
            _activeShop = shopLocation.GetComponentInChildren<Shop>();
            if (_activeShop == null) _activeShop = shopLocation.GetComponent<Shop>();
            if (_activeShop == null) _activeShop = shopLocation.GetComponentInParent<Shop>();
        }
    }

    // ---------- 헬퍼: profile null 안전 ----------

    private float GetTrait(System.Func<NpcProfile, float> selector)
    {
        return profile != null ? selector(profile) : 0f;
    }

    private float GetSocialBias()
    {
        return profile != null ? (profile.socialWeight - 0.5f) : 0f;
    }

    // ---------- 결정론 RNG 래퍼 ----------

    private Vector3 RandomInsideUnitSphere()
    {
        double u = _rng.NextDouble();
        double v = _rng.NextDouble();
        double theta = u * 2.0 * System.Math.PI;
        double phi = System.Math.Acos(2.0 * v - 1.0);
        double r = System.Math.Pow(_rng.NextDouble(), 1.0 / 3.0);
        float x = (float)(r * System.Math.Sin(phi) * System.Math.Cos(theta));
        float y = (float)(r * System.Math.Sin(phi) * System.Math.Sin(theta));
        float z = (float)(r * System.Math.Cos(phi));
        return new Vector3(x, y, z);
    }
}
