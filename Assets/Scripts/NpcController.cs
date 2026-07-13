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
    [SerializeField] private bool debugSchedulePaused;
    [SerializeField] private bool debugShoppingPriority;

    private NavMeshAgent agent;
    private Animator anim;
    private float idleTimer = 0f;

    // 결정론 RNG
    private System.Random _rng;

    // 선택적 대사 컴포넌트 (같은 GameObject) — 쇼핑 결과 대사 분기에 사용.
    private NpcDialogue _dialogue;

    // -------- NpcScheduleController 에서 제어하는 플래그 --------

    // true 이면 Update 전체가 차단된다 (수면/휴식 페이즈).
    private bool _schedulePaused = false;

    // true 이면 쇼핑 확률이 대폭 상승 (쇼핑 페이즈).
    private bool _shoppingPriorityMode = false;

    // 쇼핑 세션 상태
    private Shop _activeShop;
    private ShopSlot _currentSlotTarget;
    private readonly HashSet<ShopSlot> _visitedSlots = new HashSet<ShopSlot>();
    private bool _arrivedAtSlot;
    private float _browseTimer;
    private bool _restoredFromSave;

    private string DisplayName => profile != null && !string.IsNullOrEmpty(profile.npcName) ? profile.npcName : gameObject.name;

    void Awake()
    {
        agent    = GetComponent<NavMeshAgent>();
        var normalizer = GetComponent<NpcPresentationNormalizer>();
        if (normalizer != null)
        {
            normalizer.animationMode = NpcPresentationNormalizer.NpcAnimationMode.HumanoidProcedural;
            normalizer.animatorController = null;
            NpcPresentationNormalizer.Normalize(gameObject);
        }
        else
            NpcPresentationNormalizer.Normalize(gameObject);
        anim     = GetComponentInChildren<Animator>();
        _dialogue = GetComponent<NpcDialogue>();  // 있으면 대사 풀 연동, 없으면 null — 무해.

        int seed = randomSeed != 0
            ? randomSeed
            : ((profile != null ? profile.npcName : gameObject.name) + "::" + gameObject.name).GetHashCode();
        _rng = new System.Random(seed);
    }

    void Start()
    {
        if (_dialogue == null) _dialogue = GetComponent<NpcDialogue>();
        TryCacheShopReference();
        if (!_restoredFromSave)
            ChangeState(State.Idle);
    }

    void Update()
    {
        // 스케줄에 의해 일시 정지 중이면 처리 차단
        debugSchedulePaused  = _schedulePaused;
        debugShoppingPriority = _shoppingPriorityMode;
        if (_schedulePaused) return;

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

        // NavMeshAgent 속력으로 Walk/Idle 구동
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            float speed = agent != null ? agent.velocity.magnitude : 0f;
            anim.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
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
        // _shoppingPriorityMode(쇼핑 페이즈): 기본값 0.85 로 대폭 상승
        float shoppingProb;
        if (_shoppingPriorityMode)
        {
            shoppingProb = 0.85f + 0.08f * GetTrait(p => p.traitEI);
            shoppingProb = Mathf.Clamp(shoppingProb, 0.7f, 0.98f);
        }
        else
        {
            shoppingProb = 0.10f
                + 0.08f * GetTrait(p => p.traitEI)
                + 0.04f * GetTrait(p => p.traitSN)
                + 0.05f * GetSocialBias();
            shoppingProb = Mathf.Clamp(shoppingProb, 0.01f, 0.6f);
        }
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

        ReleaseShoppingClaims();
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
            _currentSlotTarget.ReleaseClaim(DisplayName);
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
            if (_currentSlotTarget != null) _currentSlotTarget.ReleaseClaim(DisplayName);
            _currentSlotTarget = null;
            PickNextSlotToBrowse();
            return;
        }

        // CDN-002 — 밤 영업 게이트: 가게가 손님에게 열려 있을 때만 구매를 진행한다.
        // Day 1 튜토리얼은 DayNightShopLoopController 가 항상 열림으로 처리해 첫 판매 루트를 보존한다.
        // 영업 전이면 오류가 아니라 자연스럽게 발길을 돌리고, 가끔 안내 말풍선을 띄운다.
        var shopLoop = DayNightShopLoopController.Instance;
        if (shopLoop != null && !shopLoop.IsShopOpenForCustomers)
        {
            MaybeShowShopClosedBubble();
            if (_currentSlotTarget != null) _currentSlotTarget.ReleaseClaim(DisplayName);
            _currentSlotTarget = null;
            EndShoppingVisit("아직 영업 전");
            return;
        }

        // §Week12 — 동시 구매 방지: 다른 NPC 가 이미 평가 중인 슬롯은 스킵.
        // 같은 NPC 의 재진입은 허용 (TryClaim 이 idempotent).
        if (!_currentSlotTarget.TryClaim(DisplayName))
        {
            Debug.Log($"🤖 {DisplayName}: 슬롯 {_currentSlotTarget.name} 은 다른 NPC 가 평가 중 — 스킵");
            _currentSlotTarget = null;
            PickNextSlotToBrowse();
            return;
        }

        PurchaseEvaluator.Result result = PurchaseEvaluator.Evaluate(profile, _currentSlotTarget, _rng);
        debugLastDecision = result.reason;
        Debug.Log($"🤖 {DisplayName} 평가 [{_currentSlotTarget.currentItem?.data?.itemName}]: {result.reason}");
        string feedback = BuildPurchaseFeedback(result, _currentSlotTarget);
        CustomerDemandInsightController.Instance?.RecordEvaluation(
            result,
            _currentSlotTarget.currentItem != null ? _currentSlotTarget.currentItem.data : null,
            DisplayName,
            _currentSlotTarget.EffectiveDisplayPrice);

        // SPY-002 — 구매/거절 이유 + 마을 변화 연결을 플레이어용 패널에 기록(읽기 전용).
        // willBuy/판매/돈/FSM 에 영향을 주지 않는다. demand insight 훅과 동일한 패턴.
        PurchaseFeedbackPresentationController.Instance?.RecordDecision(
            profile,
            result,
            _currentSlotTarget.currentItem != null ? _currentSlotTarget.currentItem.data : null,
            _currentSlotTarget.EffectiveDisplayPrice,
            DisplayName);

        if (result.willBuy)
        {
            if (_currentSlotTarget.TryPurchaseByNpc(DisplayName, out int paid))
            {
                Debug.Log($"🤖 {DisplayName}: 구매 성공! +{paid}G");
                ShowBubbleMessage(feedback);
                RecordScenarioFeedback(feedback);
                // 구매 직후 소감 대사 (DialogueData 가 연결된 NPC 만)
                if (_dialogue == null) _dialogue = GetComponent<NpcDialogue>();
                if (_dialogue != null) _dialogue.SpeakTopic(DialogueTopic.ShopBought);
                // 친밀도 가산 — NpcDialogue.friendshipId 가 있는 경우에만 집계.
                if (_dialogue != null
                    && !string.IsNullOrEmpty(_dialogue.friendshipId)
                    && FriendshipService.Instance != null)
                {
                    FriendshipService.Instance.AddPurchasePoints(_dialogue.friendshipId);
                }
                EndShoppingVisit("구매 완료");
                return;
            }
        }
        else
        {
            ShowBubbleMessage(feedback);
            RecordScenarioFeedback(feedback);

            // 패스 대사 — "가격이 너무 비싸" 또는 일반 잡담
            if (_dialogue == null) _dialogue = GetComponent<NpcDialogue>();
            if (_dialogue != null) _dialogue.SpeakTopic(DialogueTopic.ShopTooExpensive);
        }

        // 패스 — 다음 슬롯으로 (Claim 해제)
        if (_currentSlotTarget != null) _currentSlotTarget.ReleaseClaim(DisplayName);
        _currentSlotTarget = null;
        PickNextSlotToBrowse();
    }

    void EndShoppingVisit(string reason)
    {
        Debug.Log($"🤖 {DisplayName}: 쇼핑 종료 ({reason})");
        // 점령했던 모든 슬롯의 Claim 해제 — 누락 방지
        ReleaseShoppingClaims();
        _currentSlotTarget = null;
        _visitedSlots.Clear();
        _arrivedAtSlot = false;
        _browseTimer = 0f;
        ChangeState(State.Idle);
    }

    void ReleaseShoppingClaims()
    {
        if (_currentSlotTarget != null) _currentSlotTarget.ReleaseClaim(DisplayName);
        foreach (var s in _visitedSlots) if (s != null) s.ReleaseClaim(DisplayName);
    }

    string BuildPurchaseFeedback(PurchaseEvaluator.Result result, ShopSlot slot)
    {
        if (slot == null || slot.IsEmpty || slot.currentItem == null || slot.currentItem.data == null)
            return $"{DisplayName}: 진열 상품을 다시 확인해야겠어요.";

        var item = slot.currentItem.data;
        int displayPrice = Mathf.Max(1, slot.EffectiveDisplayPrice);
        int basePrice = Mathf.Max(1, item.basePrice);
        float ratio = displayPrice / (float)basePrice;
        int percent = Mathf.RoundToInt(result.probability * 100f);

        string itemName = item.itemName;
        string categoryHint = ResolveCategoryHint(item.category);

        if (result.willBuy)
        {
            if (ratio <= 0.85f)
                return $"{DisplayName}: 저렴해서 구매 ({percent}%)";
            if (ratio <= 1.15f)
                return $"{DisplayName}: 가격 적정, 구매 ({percent}%)";
            return $"{DisplayName}: {categoryHint} 선호로 구매 ({percent}%)";
        }

        if (ratio >= 1.35f)
            return $"{DisplayName}: 가격 높아 보류 ({percent}%)";

        if (result.probability < 0.35f)
            return $"{DisplayName}: 선호 낮아 보류 ({percent}%)";

        return $"{DisplayName}: 고민 후 보류 ({percent}%)";
    }

    string ResolveCategoryHint(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Raw => "원자재 계열",
            ItemCategory.Processed => "가공품 계열",
            ItemCategory.Utility => "실용품 계열",
            ItemCategory.Luxury => "선호 상품 계열",
            ItemCategory.Tool => "도구 계열",
            _ => "이 상품 계열"
        };
    }

    void ShowBubbleMessage(string message)
    {
        var bubble = GetComponentInChildren<NpcBubbleUI>(true);
        if (bubble != null)
            bubble.Show(message, 3f);
    }

    // CDN-002 — 영업 전 손님이 발길을 돌릴 때 가끔 보여주는 안내 말풍선(과도한 반복 방지).
    float _nextClosedBubbleAt;
    void MaybeShowShopClosedBubble()
    {
        if (Time.time < _nextClosedBubbleAt) return;
        _nextClosedBubbleAt = Time.time + 6f;
        ShowBubbleMessage($"{DisplayName}: 가게 열면 다시 올게요.");
    }

    void RecordScenarioFeedback(string message)
    {
        if (PlayableDayScenarioController.Instance != null)
            PlayableDayScenarioController.Instance.RecordManagementFeedback(message);
    }

    // ---------- NpcScheduleController 공개 API ----------

    /// <summary>
    /// 스케줄에 의한 일시 정지.
    /// 현재 FSM을 Idle 로 되돌리고 NavMesh 이동을 정지한다.
    /// </summary>
    public void Pause()
    {
        if (_schedulePaused) return;
        _schedulePaused = true;

        // 이동 중이면 경로 취소
        if (agent != null && agent.isOnNavMesh && !agent.isStopped)
            agent.ResetPath();

        // 쇼핑 세션 정리
        ReleaseShoppingClaims();
        _currentSlotTarget = null;
        _visitedSlots.Clear();
        _arrivedAtSlot = false;
        _browseTimer = 0f;

        ChangeState(State.Idle);
    }

    /// <summary>스케줄에 의한 재개. 다음 idleTickInterval 후 자연스럽게 행동을 시작한다.</summary>
    public void Resume()
    {
        if (!_schedulePaused) return;
        _schedulePaused = false;
        idleTimer = 0f; // 즉시 틱 평가되지 않도록 타이머 리셋
    }

    /// <summary>
    /// 쇼핑 우선 모드 설정.
    /// true: 쇼핑 확률이 대폭 상승 (쇼핑 페이즈).
    /// false: 일반 확률 복원 (배회 페이즈).
    /// </summary>
    public void SetShoppingPriority(bool priority)
    {
        _shoppingPriorityMode = priority;
    }

    /// <summary>
    /// 현재 Idle 상태이면 즉시 쇼핑을 시작한다.
    /// NpcScheduleController 가 쇼핑 페이즈 진입 시 호출해 즉각 반응성을 확보한다.
    /// </summary>
    public void TryForceShop()
    {
        if (_schedulePaused) return;
        if (currentState != State.Idle) return;

        // 상점 참조가 없으면 캐싱 재시도 후 시작
        TryCacheShopReference();
        if (shopLocation == null || _activeShop == null) return;

        BeginShoppingVisit();
    }

    // S3 — 실내 상점 방문: 외부 사이드카(InteriorCustomerController)가 대상 상점을
    // 지정해 쇼핑을 시작시킨다. 기존 BeginShoppingVisit/FSM 을 그대로 재사용한다.
    public bool TryBeginShoppingVisitAt(Transform newShopLocation)
    {
        if (_schedulePaused || currentState != State.Idle) return false;
        if (newShopLocation == null) return false;

        RetargetShop(newShopLocation);
        if (_activeShop == null) return false;

        BeginShoppingVisit();
        return currentState == State.MovingToShop;
    }

    // S3 — 상점 참조 교체. _activeShop 은 한 번 캐시되면 shopLocation 변경을 따라가지
    // 않으므로(실내→광장 복귀 시 스테일 위험) 반드시 이 경로로 함께 갱신한다.
    public void RetargetShop(Transform newShopLocation)
    {
        shopLocation = newShopLocation;
        _activeShop = null;
        TryCacheShopReference();
    }

    public string GetFsmState()
    {
        return currentState.ToString();
    }

    public void RestoreFsmState(string stateName)
    {
        if (!System.Enum.TryParse(stateName, out State restoredState))
            restoredState = State.Idle;

        _schedulePaused = false;
        _restoredFromSave = true;
        ReleaseShoppingClaims();
        _currentSlotTarget = null;
        _visitedSlots.Clear();
        _arrivedAtSlot = false;
        _browseTimer = 0f;
        idleTimer = 0f;

        TryCacheShopReference();
        ChangeState(restoredState);

        if (agent == null || !agent.isOnNavMesh) return;

        if (restoredState == State.MovingToShop && shopLocation != null)
        {
            agent.SetDestination(shopLocation.position);
        }
        else if (restoredState == State.BrowsingShop)
        {
            PickNextSlotToBrowse();
        }
        else
        {
            agent.ResetPath();
        }
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
