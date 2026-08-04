using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// IL/CDN follow-up — 손님 도착 페이싱.
//
// 설계 의도 (PROJECT_PA_CREATIVE_NORTH_STAR.md "Shop Operation Fantasy",
// Docs/IslandLife/GATHERING_AND_SHOP_GATE.md 다음 추천 작업):
// - 플레이어가 밤에 가게를 "열면"(CDN-002 게이트가 손님에게 열리면) 주민 손님이
//   한 명씩 자연스럽게 가게로 모여들게 한다. 영업 시작이 실제로 손님을 부른다는 체감을 준다.
// - 구매 확률/경제/PurchaseEvaluator/NPC FSM 내부는 바꾸지 않는다.
//   기존 NpcController.SetShoppingPriority / TryForceShop (NpcScheduleController 가 쓰는 멱등 공개 API)만 호출한다.
//
// 안전장치:
// - Day 1 튜토리얼(IsTutorialAlwaysOpen)에는 능동 초대를 하지 않는다 — 기존 PlayableDayScenarioController 가
//   Day 1 손님 흐름을 이미 관리하므로 그 페이싱을 보존한다.
// - 일시정지(수면/근무) 또는 이미 쇼핑 중인 NPC 는 TryForceShop 이 스스로 무시하므로 깨우지 않는다.
public class CustomerArrivalController : MonoBehaviour
{
    public static CustomerArrivalController Instance { get; private set; }

    [Header("Arrival Pacing")]
    [Tooltip("상태 폴링 간격(초)")]
    public float pollInterval = 0.5f;
    [Tooltip("손님을 한 명씩 초대하는 간격(초)")]
    public float inviteInterval = 1.6f;
    [Tooltip("동시에 가게로 향하는 손님 최대 수")]
    public int maxConcurrentCustomers = 6;
    [Tooltip("Rest 주민의 늦은 방문이 끝나지 않을 때 원래 일과로 돌려보내는 시간")]
    public float lateVisitTimeout = 45f;

    [Header("Tourist Visits")]
    [Tooltip("개점 뒤 첫 관광객이 들어오기까지의 짧은 여유")]
    public float firstTouristDelay = 2.5f;
    [Tooltip("한 번의 영업에서 생성할 세션 한정 관광객 수")]
    public int maxTouristsPerOpening = 2;
    [Tooltip("동시에 쇼핑 중일 수 있는 관광객 수")]
    public int maxConcurrentTourists = 1;
    [Tooltip("다음 관광객 입장 시도 간격")]
    public float touristInviteInterval = 12f;
    [Tooltip("관광객 쇼핑과 퇴장에 적용하는 안전 시간 제한")]
    public float touristVisitTimeout = 60f;
    public float touristExitTimeout = 20f;
    [Tooltip("가게에서 관광객 진입점을 찾는 기본 반경")]
    public float touristSpawnRadius = 14f;

    bool _wasOpen;
    float _nextPollAt;
    float _nextInviteAt;
    float _nextTouristInviteAt;
    int _invitedThisOpening;
    int _touristsSpawnedThisOpening;
    int _touristSequence;

    readonly List<NpcController> _npcBuffer = new List<NpcController>();

    class LateVisitorLease
    {
        public NpcController npc;
        public NpcScheduleController schedule;
        public Vector3 returnPosition;
        public Transform originalShopLocation;
        public float startedAt;
    }

    readonly List<LateVisitorLease> _lateVisitors = new List<LateVisitorLease>();

    class TouristVisit
    {
        public GameObject root;
        public NpcController npc;
        public NpcProfile runtimeProfile;
        public Vector3 entryPoint;
        public float startedAt;
        public bool waitingToLeave;
        public float leaveAt;
        public bool returning;
        public float returnStartedAt;
    }

    readonly List<TouristVisit> _tourists = new List<TouristVisit>();

    public bool IsOpenForCustomers
    {
        get
        {
            var loop = DayNightShopLoopController.Instance;
            return loop != null && loop.IsShopOpenForCustomers;
        }
    }

    // Day 1 튜토리얼은 능동 초대를 하지 않는다(시나리오 컨트롤러가 담당).
    public bool ShouldActivelyInvite
    {
        get
        {
            var loop = DayNightShopLoopController.Instance;
            return loop != null && loop.IsShopOpenForCustomers && !loop.IsTutorialAlwaysOpen;
        }
    }

    public int InvitedThisOpening => _invitedThisOpening;
    public int ActiveShopperCount => CountActiveShoppers();
    public int ActiveTouristCount => CountActiveTourists();
    public int TouristsSpawnedThisOpening => _touristsSpawnedThisOpening;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        RecallLateVisitors("컨트롤러 종료");
        DestroyAllTourists();
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (Time.unscaledTime < _nextPollAt) return;
        _nextPollAt = Time.unscaledTime + Mathf.Max(0.1f, pollInterval);

        ReturnFinishedLateVisitors();
        UpdateTourists();

        bool open = ShouldActivelyInvite;
        if (open && !_wasOpen)
            OnShopOpened();
        else if (!open && _wasOpen)
            OnShopClosed();
        _wasOpen = open;

        if (open && Time.time >= _nextInviteAt)
        {
            _nextInviteAt = Time.time + Mathf.Max(0.2f, inviteInterval);
            TryInviteWave(1);
        }

        if (open && Time.time >= _nextTouristInviteAt)
        {
            NpcController tourist = TryInviteTourist();
            _nextTouristInviteAt = Time.time + (tourist != null
                ? Mathf.Max(4f, touristInviteInterval)
                : 3f);
        }
    }

    void OnShopOpened()
    {
        _invitedThisOpening = 0;
        _touristsSpawnedThisOpening = 0;
        _nextInviteAt = 0f; // 즉시 첫 손님 초대
        _nextTouristInviteAt = Time.time + Mathf.Max(0.5f, firstTouristDelay);
    }

    void OnShopClosed()
    {
        RecallLateVisitors("영업 종료");
        RecallTourists("영업 종료");
        DisperseCustomers();
    }

    /// <summary>
    /// 활성·대기 중인 손님을 최대 max 명까지 즉시 가게로 초대한다.
    /// 실제로 쇼핑을 시작한(상태가 Idle 이 아닌) 손님 수를 반환한다.
    /// 게이트가 닫혀 있거나 Day 1 튜토리얼이면 0 을 반환한다(능동 초대 안 함).
    /// </summary>
    public int TryInviteWave(int max)
    {
        if (!ShouldActivelyInvite) return 0;

        int slots = maxConcurrentCustomers - CountActiveShoppers();
        if (slots <= 0) return 0;

        int target = Mathf.Min(Mathf.Max(1, max), slots);

        _npcBuffer.Clear();
        _npcBuffer.AddRange(FindObjectsByType<NpcController>(FindObjectsSortMode.None));

        int invited = 0;
        foreach (var npc in _npcBuffer)
        {
            if (invited >= target) break;
            if (npc == null) continue;
            if (IsTransientTourist(npc)) continue;
            if (npc.currentState != NpcController.State.Idle) continue;

            NpcScheduleController lateSchedule = null;
            Vector3 returnPosition = default;
            Transform originalShopLocation = null;
            if (npc.IsSchedulePaused)
            {
                lateSchedule = npc.GetComponent<NpcScheduleController>();
                if (lateSchedule == null || !lateSchedule.TryBeginShopOpenVisitOverride())
                    continue;

                returnPosition = npc.transform.position;
                originalShopLocation = npc.shopLocation;
            }

            npc.SetShoppingPriority(true);
            npc.TryForceShop(); // 일시정지/비-Idle 이면 내부에서 무시됨

            if (npc.currentState != NpcController.State.Idle)
            {
                invited++;
                _invitedThisOpening++;

                if (lateSchedule != null)
                {
                    _lateVisitors.Add(new LateVisitorLease
                    {
                        npc = npc,
                        schedule = lateSchedule,
                        returnPosition = returnPosition,
                        originalShopLocation = originalShopLocation,
                        startedAt = Time.timeSinceLevelLoad
                    });
                }
            }
            else if (lateSchedule != null)
            {
                // Resume 뒤 실제 쇼핑 시작에 실패했다면 같은 프레임에 Rest로 되돌린다.
                npc.SetShoppingPriority(false);
                lateSchedule.EndShopOpenVisitOverride();
            }
        }

        return invited;
    }

    /// <summary>
    /// Task 128 — 현재 상주 주민의 검증된 캐릭터 시각만 재사용해 일과표·직업·친밀도·저장 기록이 없는
    /// 세션 한정 관광객 한 명을 만든다. 쇼핑은 기존 NpcController/PurchaseEvaluator 권위를 그대로 쓴다.
    /// </summary>
    public NpcController TryInviteTourist()
    {
        if (!ShouldActivelyInvite) return null;
        if (_touristsSpawnedThisOpening >= Mathf.Max(0, maxTouristsPerOpening)) return null;
        if (CountActiveTourists() >= Mathf.Max(1, maxConcurrentTourists)) return null;
        if (CountActiveShoppers() >= Mathf.Max(1, maxConcurrentCustomers)) return null;
        if (!TryResolveTouristSource(out NpcController source, out Transform sourceVisual)) return null;

        Transform shop = source.shopLocation;
        if (shop == null)
        {
            GameObject shopObject = GameObject.FindGameObjectWithTag("Shop");
            shop = shopObject != null ? shopObject.transform : null;
        }
        if (shop == null) return null;

        int sequence = ++_touristSequence;
        if (!TryFindTouristEntry(shop, sequence, out Vector3 entryPoint)) return null;

        GameObject root = new GameObject($"NPC_Tourist_{sequence:00}");
        root.SetActive(false);
        root.transform.SetParent(transform, true);
        Vector3 facing = shop.position - entryPoint;
        facing.y = 0f;
        Quaternion rotation = facing.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(facing.normalized, Vector3.up)
            : Quaternion.identity;
        root.transform.SetPositionAndRotation(entryPoint, rotation);

        GameObject characterVisual = Instantiate(sourceVisual.gameObject, root.transform, false);
        characterVisual.name = "CharacterVisual";

        NavMeshAgent sourceAgent = source.GetComponent<NavMeshAgent>();
        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        ConfigureTouristAgent(agent, sourceAgent, sequence);

        var normalizer = root.AddComponent<NpcPresentationNormalizer>();
        normalizer.animationMode = NpcPresentationNormalizer.NpcAnimationMode.HumanoidProcedural;
        normalizer.animatorController = null;

        NpcProfile runtimeProfile = Instantiate(source.profile);
        runtimeProfile.name = $"Runtime_TouristProfile_{sequence:00}";
        runtimeProfile.npcName = $"여행 손님 {sequence:00}";

        NpcController npc = root.AddComponent<NpcController>();
        npc.profile = runtimeProfile;
        npc.randomSeed = 12800 + sequence;
        npc.shopLocation = shop;
        npc.idleTickInterval = source.idleTickInterval;
        npc.wanderRadius = source.wanderRadius;
        npc.maxSlotsPerVisit = source.maxSlotsPerVisit;
        npc.browseDurationAtSlot = source.browseDurationAtSlot;
        npc.slotArriveDistance = source.slotArriveDistance;
        npc.shopArriveDistance = source.shopArriveDistance;

        GameObject bubbleObject = new GameObject("NpcBubbleUI", typeof(RectTransform), typeof(Canvas));
        bubbleObject.SetActive(false);
        bubbleObject.transform.SetParent(root.transform, false);
        NpcBubbleUI bubble = bubbleObject.AddComponent<NpcBubbleUI>();
        bubble.offset = new Vector3(0f, 4f, 0f);
        bubbleObject.SetActive(true);

        root.SetActive(true);
        if ((!agent.isOnNavMesh && !agent.Warp(entryPoint)) || !npc.TryBeginShoppingVisitAt(shop))
        {
            Destroy(root);
            Destroy(runtimeProfile);
            return null;
        }

        _tourists.Add(new TouristVisit
        {
            root = root,
            npc = npc,
            runtimeProfile = runtimeProfile,
            entryPoint = entryPoint,
            startedAt = Time.timeSinceLevelLoad
        });
        _touristsSpawnedThisOpening++;
        _invitedThisOpening++;

        bubble.Show($"{runtimeProfile.npcName}: 밤 장터를 구경 왔어요!", 2.5f);
        Debug.Log($"🧳 [CustomerArrival] {runtimeProfile.npcName} 관광객 입장 ({entryPoint})");
        return npc;
    }

    public bool IsTransientTourist(NpcController npc)
    {
        if (npc == null) return false;
        foreach (TouristVisit visit in _tourists)
            if (visit != null && visit.npc == npc) return true;
        return false;
    }

    bool TryResolveTouristSource(out NpcController source, out Transform sourceVisual)
    {
        source = null;
        sourceVisual = null;
        _npcBuffer.Clear();

        foreach (NpcController npc in FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            if (npc == null || npc.profile == null || IsTransientTourist(npc)) continue;
            NpcScheduleController schedule = npc.GetComponent<NpcScheduleController>();
            if (schedule == null || schedule.scheduleData == null) continue;
            if (FindCharacterVisual(npc.transform) == null) continue;
            _npcBuffer.Add(npc);
        }

        _npcBuffer.Sort((left, right) => string.CompareOrdinal(
            left != null ? left.gameObject.name : string.Empty,
            right != null ? right.gameObject.name : string.Empty));
        if (_npcBuffer.Count == 0) return false;

        source = _npcBuffer[_touristsSpawnedThisOpening % _npcBuffer.Count];
        sourceVisual = FindCharacterVisual(source.transform);
        return sourceVisual != null;
    }

    static Transform FindCharacterVisual(Transform root)
    {
        if (root == null) return null;

        Transform named = FindDescendant(root, "CharacterVisual");
        if (named != null && named.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
            return named;

        Animator animator = root.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.transform != root
            && animator.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
            return animator.transform;

        SkinnedMeshRenderer renderer = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (renderer == null) return null;
        Transform visual = renderer.transform;
        while (visual.parent != null && visual.parent != root)
            visual = visual.parent;
        return visual != root ? visual : null;
    }

    static Transform FindDescendant(Transform root, string childName)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName) return child;
            Transform nested = FindDescendant(child, childName);
            if (nested != null) return nested;
        }
        return null;
    }

    bool TryFindTouristEntry(Transform shop, int sequence, out Vector3 entryPoint)
    {
        entryPoint = default;
        if (shop == null || !NavMesh.SamplePosition(shop.position, out NavMeshHit shopHit, 6f, NavMesh.AllAreas))
            return false;

        const int directionCount = 12;
        float radius = Mathf.Max(8f, touristSpawnRadius);
        for (int ring = 0; ring < 2; ring++)
        {
            float ringRadius = radius + ring * 4f;
            for (int i = 0; i < directionCount; i++)
            {
                float angle = (sequence * 137.5f + i * (360f / directionCount)) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 candidate = shopHit.position + direction * ringRadius;
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3.5f, NavMesh.AllAreas)) continue;
                if (Vector3.Distance(hit.position, shopHit.position) < 6f) continue;

                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(hit.position, shopHit.position, NavMesh.AllAreas, path)) continue;
                if (path.status != NavMeshPathStatus.PathComplete) continue;

                entryPoint = hit.position;
                return true;
            }
        }

        return false;
    }

    static void ConfigureTouristAgent(NavMeshAgent target, NavMeshAgent source, int sequence)
    {
        if (target == null) return;
        target.speed = source != null ? source.speed : 2f;
        target.acceleration = source != null ? source.acceleration : 8f;
        target.angularSpeed = source != null ? source.angularSpeed : 240f;
        target.stoppingDistance = source != null
            ? source.stoppingDistance
            : NpcPresentationNormalizer.AgentStoppingDistance;
        target.radius = NpcPresentationNormalizer.AgentRadius;
        target.height = NpcPresentationNormalizer.AgentHeight;
        target.baseOffset = 0f;
        target.areaMask = source != null ? source.areaMask : NavMesh.AllAreas;
        target.obstacleAvoidanceType = source != null
            ? source.obstacleAvoidanceType
            : ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        target.avoidancePriority = 35 + sequence % 20;
    }

    void UpdateTourists()
    {
        for (int i = _tourists.Count - 1; i >= 0; i--)
        {
            TouristVisit visit = _tourists[i];
            if (visit == null || visit.npc == null || visit.root == null)
            {
                DestroyTourist(visit);
                _tourists.RemoveAt(i);
                continue;
            }

            if (visit.returning)
            {
                NavMeshAgent agent = visit.npc.GetComponent<NavMeshAgent>();
                bool arrived = agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh
                    && !agent.pathPending && agent.remainingDistance <= Mathf.Max(0.8f, agent.stoppingDistance + 0.2f);
                bool timedOut = Time.timeSinceLevelLoad - visit.returnStartedAt > Mathf.Max(5f, touristExitTimeout);
                if (arrived || timedOut)
                {
                    Debug.Log($"🧳 [CustomerArrival] {visit.npc.profile?.npcName ?? visit.npc.name} 관광객 퇴장");
                    DestroyTourist(visit);
                    _tourists.RemoveAt(i);
                }
                continue;
            }

            if (visit.waitingToLeave)
            {
                if (Time.timeSinceLevelLoad >= visit.leaveAt && !BeginTouristReturn(visit, "쇼핑 종료"))
                {
                    DestroyTourist(visit);
                    _tourists.RemoveAt(i);
                }
                continue;
            }

            bool finished = visit.npc.currentState == NpcController.State.Idle;
            bool timedOutVisit = Time.timeSinceLevelLoad - visit.startedAt > Mathf.Max(15f, touristVisitTimeout);
            if (finished)
            {
                // 구매/거절 말풍선을 읽을 시간을 남기면서 다른 초대 시스템이 Idle 관광객을 빌리지 못하게 한다.
                visit.npc.Pause();
                visit.waitingToLeave = true;
                visit.leaveAt = Time.timeSinceLevelLoad + 2.6f;
            }
            else if (timedOutVisit && !BeginTouristReturn(visit, "방문 시간 초과"))
            {
                DestroyTourist(visit);
                _tourists.RemoveAt(i);
            }
        }
    }

    void RecallTourists(string reason)
    {
        for (int i = _tourists.Count - 1; i >= 0; i--)
        {
            TouristVisit visit = _tourists[i];
            if (visit == null || visit.npc == null || visit.root == null
                || (!visit.returning && !BeginTouristReturn(visit, reason)))
            {
                DestroyTourist(visit);
                _tourists.RemoveAt(i);
            }
        }
    }

    bool BeginTouristReturn(TouristVisit visit, string reason)
    {
        if (visit == null || visit.npc == null) return false;
        visit.npc.SetShoppingPriority(false);
        visit.npc.Pause();

        NavMeshAgent agent = visit.npc.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return false;
        var path = new NavMeshPath();
        if (!agent.CalculatePath(visit.entryPoint, path) || path.status != NavMeshPathStatus.PathComplete)
            return false;
        if (!agent.SetPath(path)) return false;

        visit.waitingToLeave = false;
        visit.returning = true;
        visit.returnStartedAt = Time.timeSinceLevelLoad;
        NpcBubbleUI bubble = visit.npc.GetComponentInChildren<NpcBubbleUI>(true);
        bubble?.Show($"{visit.npc.profile?.npcName ?? "여행 손님"}: 다음에 또 올게요!", 2.2f);
        Debug.Log($"🧳 [CustomerArrival] {visit.npc.profile?.npcName ?? visit.npc.name} 관광객 복귀 시작 ({reason})");
        return true;
    }

    void DestroyAllTourists()
    {
        for (int i = _tourists.Count - 1; i >= 0; i--)
            DestroyTourist(_tourists[i]);
        _tourists.Clear();
    }

    static void DestroyTourist(TouristVisit visit)
    {
        if (visit == null) return;
        if (visit.root != null) Destroy(visit.root);
        if (visit.runtimeProfile != null) Destroy(visit.runtimeProfile);
    }

    void ReturnFinishedLateVisitors()
    {
        for (int i = _lateVisitors.Count - 1; i >= 0; i--)
        {
            LateVisitorLease lease = _lateVisitors[i];
            if (lease.npc == null)
            {
                lease.schedule?.EndShopOpenVisitOverride();
                _lateVisitors.RemoveAt(i);
                continue;
            }

            bool finished = lease.npc.currentState == NpcController.State.Idle;
            bool timedOut = Time.timeSinceLevelLoad - lease.startedAt > Mathf.Max(10f, lateVisitTimeout);
            if (!finished && !timedOut) continue;

            ReturnLateVisitor(lease, timedOut ? "시간 초과" : "쇼핑 종료");
            _lateVisitors.RemoveAt(i);
        }
    }

    void RecallLateVisitors(string reason)
    {
        for (int i = _lateVisitors.Count - 1; i >= 0; i--)
        {
            if (_lateVisitors[i].npc != null)
                ReturnLateVisitor(_lateVisitors[i], reason);
            else
                _lateVisitors[i].schedule?.EndShopOpenVisitOverride();
            _lateVisitors.RemoveAt(i);
        }
    }

    static void ReturnLateVisitor(LateVisitorLease lease, string reason)
    {
        lease.npc.SetShoppingPriority(false);
        lease.schedule?.EndShopOpenVisitOverride(); // 기존 Pause/claim 정리 후 위치 복구

        NavMeshAgent agent = lease.npc.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            agent.Warp(lease.returnPosition);
        else
            lease.npc.transform.position = lease.returnPosition;

        lease.npc.RetargetShop(lease.originalShopLocation);
        if (lease.npc.currentState != NpcController.State.Idle)
            lease.npc.currentState = NpcController.State.Idle;

        Debug.Log($"🛍️ [CustomerArrival] {lease.npc.name} 늦은 손님 복귀 ({reason})");
    }

    void DisperseCustomers()
    {
        foreach (var npc in FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            if (npc == null) continue;
            npc.SetShoppingPriority(false);
        }
    }

    int CountActiveShoppers()
    {
        int count = 0;
        foreach (var npc in FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            if (npc == null) continue;
            if (npc.currentState != NpcController.State.Idle)
                count++;
        }
        return count;
    }

    int CountActiveTourists()
    {
        int count = 0;
        foreach (TouristVisit visit in _tourists)
            if (visit != null && visit.npc != null && !visit.returning) count++;
        return count;
    }
}
