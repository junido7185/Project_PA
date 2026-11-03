using System.Linq;
using UnityEngine;
using UnityEngine.AI;

// P7 — 동행의 하루. 기존 DemoResident(집)·WorksiteBinding/ProducerNpcController(생산)·NavMesh 이동을 잇는 표현 어댑터다.
// 새 생산/재고 권위를 만들지 않는다. 생산자가 일하는 동안(StarterWorking) 이동은 생산자 FSM이 소유한다.
[RequireComponent(typeof(DemoResident))]
public sealed class DemoCompanionRoutine : MonoBehaviour
{
    public enum Activity { Waiting, GoingHome, Resting, GoingToWork, Working, BatchReady, GoingToShop, Shopping, Gathering }
    public Activity Current { get; private set; } = Activity.Waiting;
    public string Status { get; private set; } = "항구에서 기다리는 중";
    public string DisplayName { get; private set; }
    public NavMeshAgent Agent => _agent;
    public Transform WorkTarget => _workTarget;
    public int LinesSpoken { get; private set; }
    public bool ReachedHomeOnce { get; private set; }
    // P8: 영업 중 동행은 주민 손님으로 한 번 들른다(기존 NpcController·PurchaseEvaluator, 관광객 보정 없는 자기 프로필).
    public NpcController Shopper => _shopper;
    public int ShopVisits { get; private set; }
    NpcController _shopper;
    NpcProfile _shopperProfile;
    bool _shopperActive, _wasOpen, _visitedThisOpening;
    float _openedAt;

    const float NightStart = 19f, DayStart = 6f;
    DemoResident _resident;
    NavMeshAgent _agent;
    Vector3 _anchor;
    Transform _workTarget;
    float _phaseEndsAt, _nextWanderAt, _nextFeedbackAt;
    bool _saidHome, _saidWork, _saidBatch;
    readonly System.Collections.Generic.Queue<string> _pendingLines = new System.Collections.Generic.Queue<string>();
    GameObject _heldTool;
    Item _heldItem;
    NpcHeldToolPose _toolPose;
    float _pendingHitAt = -1f;
    Transform _pendingHitTarget;

    bool IsMiner => _resident != null && _resident.CompanionId != null && _resident.CompanionId.StartsWith("Miner", System.StringComparison.Ordinal);
    bool IsLumberjack => _resident != null && _resident.CompanionId != null && _resident.CompanionId.StartsWith("Lumberjack", System.StringComparison.Ordinal);

    void Awake()
    {
        _resident = GetComponent<DemoResident>();
        _anchor = transform.position;
    }

    void Start()
    {
        // 선택 카드 이름("미라 · TEMP" → 미라). 카드가 없으면 캐논 역할 이름으로, 내부 ID 는 보이지 않는다.
        DisplayName = ResidentNames.CompanionName(_resident.CompanionId, _resident.Profile)
                      ?? ResidentNames.CanonName(ResidentNames.RoleFromKey(_resident.CompanionId));
        TryEnsureAgent();
    }

    // 동행 객체는 이동 컴포넌트 없이 만들어진다. NavMesh 위에 올린 뒤 기존 NPC 이동(NavMeshAgent)을 붙인다.
    bool TryEnsureAgent()
    {
        if (_agent != null) return true;
        _agent = GetComponent<NavMeshAgent>();
        if (_agent != null) return true;
        if (!NavMesh.SamplePosition(transform.position, out var hit, 6f, NavMesh.AllAreas)) return false;
        transform.position = hit.position;
        _anchor = hit.position;
        _agent = gameObject.AddComponent<NavMeshAgent>();
        _agent.speed = 2.2f; _agent.acceleration = 12f; _agent.angularSpeed = 540f;
        _agent.radius = .32f; _agent.height = 1.7f; _agent.stoppingDistance = .6f;
        _agent.avoidancePriority = 60;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        return true;
    }

    void OnDestroy()
    {
        if (_shopperProfile != null) Destroy(_shopperProfile);
    }

    void Update()
    {
        if (_resident == null || !TryEnsureAgent() || !_agent.isOnNavMesh) return;
        var producer = _resident.Worksite != null ? _resident.Worksite.Producer : null;
        var settlement = DemoSettlementController.Instance;
        ReactToEvents(producer, settlement);
        if (!ReachedHomeOnce && _resident.Home != null && Vector3.Distance(transform.position, _resident.Home.position) < 2.5f) ReachedHomeOnce = true;
        if (_pendingLines.Count > 0 && !FirstDayWorldPresentation.ToastBusy)
        {
            LinesSpoken++;
            FirstDayWorldPresentation.Toast($"{DisplayName}: {_pendingLines.Dequeue()}", false);
        }
        UpdateHeldTool(producer);
        TurnBeforeWalking();
        if (HoldForTalk()) return;
        GlanceAtPasser();
        if (UpdateShopVisit(producer, settlement)) return;

        if (producer != null && producer.StarterWorking)
        {
            // 생산자 FSM이 이동을 소유한다. 여기서는 표현만 덧붙인다.
            bool atWork = producer.CurrentState == ProducerNpcController.State.Working;
            Current = atWork ? Activity.Working : Activity.GoingToWork;
            Status = atWork ? $"광석 캐는 중 · {ToolLabel(producer)}" : "광산 바위로 가는 중";
            if (atWork) WorkFeedback(producer.workSpot);
            return;
        }
        if (producer != null && producer.StarterBatchReady)
        {
            Current = Activity.BatchReady;
            Status = "광석 준비됨 · 와서 받아 가세요";
            StayAround(_resident.Home != null ? _resident.Home.position : _anchor, 0f);
            return;
        }

        float hour = GameClock.Instance != null ? GameClock.Instance.CurrentHour : 9f;
        bool night = hour >= NightStart || hour < DayStart;
        if (_resident.Home == null)
        {
            Current = Activity.Waiting;
            Status = "항구에서 기다리는 중";
            StayAround(_anchor, 2.5f);
            return;
        }
        if (!night && EveningGather(hour, settlement)) return;
        if (night || settlement == null || !settlement.Established)
        {
            Status = night ? "집에서 쉬는 중" : "새 집을 둘러보는 중";
            Current = Activity.Resting;
            StayAround(_resident.Home.position, night ? 0f : 2f);
            return;
        }
        DayCycle(_resident.Home);
    }

    // 레퍼런스 연구 K-02/03(ACNH 생활감): 실제로 대화가 시작되면 하던 걸음·휘두르기를 멈추고 플레이어를 본다.
    // 대화창이 닫히면 같은 목적지·작업으로 이어 간다(생산 주기·목적지는 그대로라 대화가 결과를 바꾸지 않는다).
    // 가게 방문 중(NpcController가 이동 소유)에는 끼어들지 않는다.
    public bool TalkPaused => _talkPaused;
    bool _talkPaused;
    int _talkPriority = 60;
    bool HoldForTalk()
    {
        bool talking = DialogueUI.TalkingWith(transform) && !_shopperActive;
        if (!talking)
        {
            if (_talkPaused)
            {
                _talkPaused = false;
                _agent.isStopped = false; _agent.avoidancePriority = _talkPriority;
                Glance.Release(); _nextGlanceAt = Time.time + 6f;
            }
            return false;
        }
        // 대화가 시작된 첫 프레임에 속도를 바로 0으로(isStopped만으로는 가속 잔여로 미끄러질 수 있음). 경로는 그대로 두고 대화 뒤 이어 간다.
        // 경로·목적지는 그대로 두어 대화가 끝나면 같은 일로 이어 간다. 회피 우선순위도 가장 높게(0) 두어 다른 동행이 비켜 가게 한다.
        // (2026-11-03 gift-r17~r44: 광부가 대화 시작 때 1.8m 미끄러진 원인은 구역 이음 링크 건너기 — FirstDayStepTraversal에서 isStopped를 지키게 고침.)
        if (!_talkPaused)
        {
            _talkPaused = true; _pendingHitAt = -1f; _nextFeedbackAt = Time.time + .8f;
            _agent.velocity = Vector3.zero; _talkPriority = _agent.avoidancePriority; _agent.avoidancePriority = 0;
        }
        _agent.isStopped = true;
        var player = Inventory.instance != null ? Inventory.instance.transform : null;
        if (player != null) { FaceTowards(player.position); Glance.Look(player, Vector3.up * 1.35f, .2f, hold: true); }
        return true;
    }

    // 2026-11-03 npc-r3 측정: 방향을 크게 바꿀 때 몸이 다 돌기 전에 속도가 붙어 0.3초쯤 뒷걸음(>120°)이 보였다.
    // 목표 방향과 몸 정면이 75°보다 벌어져 있으면 제자리에서 돌 만큼만 천천히 걷고, 정렬되면 원래 속도로 돌린다.
    // 가게 방문 중(NpcController가 속도 소유)에는 건드리지 않는다.
    // 한 번 감속은 최대 0.5초(다른 코드가 몸을 다른 쪽으로 붙잡아 두는 경우에도 멈춰 서지 않게, 2026-11-03 npc-r4 광부 정지).
    bool _turnSlowed;
    float _cruiseSpeed, _turnSince, _turnBlockedUntil;
    void TurnBeforeWalking()
    {
        bool turning = false;
        if (!_shopperActive && _agent.hasPath && !_agent.isStopped && Time.time >= _turnBlockedUntil)
        {
            Vector3 desired = _agent.desiredVelocity; desired.y = 0f;
            turning = desired.sqrMagnitude > .01f && Vector3.Angle(transform.forward, desired) > 75f;
        }
        if (turning && _turnSlowed && Time.time - _turnSince > .5f) { turning = false; _turnBlockedUntil = Time.time + 1.5f; }
        if (turning && !_turnSlowed) { _cruiseSpeed = _agent.speed; _agent.speed = .25f; _turnSlowed = true; _turnSince = Time.time; }
        else if (!turning && _turnSlowed) { _agent.speed = _cruiseSpeed; _turnSlowed = false; }
    }

    // 살피기 표현(생산 없음): 벌목꾼은 수관을 올려다보고, 농부는 발밑 풀밭을 내려다본다. 4초 중 2.4초.
    float _nextInspectAt;
    void InspectTarget()
    {
        if (_workTarget == null || Time.time < _nextInspectAt || IsMiner) return;
        _nextInspectAt = Time.time + 4f;
        Vector3 offset = IsLumberjack ? Vector3.up * 3.2f : Vector3.down * .2f + transform.forward * .4f;
        Glance.Look(_workTarget, offset, 2.4f);
    }

    // K-02: 일·걸음은 그대로 두고, 옆을 지나가는 플레이어를 고개만 돌려 1.6초 본다(14초에 한 번).
    // 멈춰 선 플레이어에겐 하지 않는다(가까이 서 있는 동안 계속 쳐다보면 감시처럼 보인다).
    NpcGlance _glance;
    NpcGlance Glance => _glance != null ? _glance : _glance = GetComponent<NpcGlance>() ?? gameObject.AddComponent<NpcGlance>();
    float _nextGlanceAt;
    public int Glances { get; private set; }
    void GlanceAtPasser()
    {
        if (Time.time < _nextGlanceAt || _shopperActive) return;
        var player = Inventory.instance != null ? Inventory.instance.GetComponent<PlayerController>() : null;
        if (player == null || player.ActualPlanarSpeed < .6f) return;
        Vector3 to = player.transform.position - transform.position; to.y = 0f;
        if (to.sqrMagnitude > 4.5f * 4.5f || Vector3.Angle(transform.forward, to) > 110f) return;
        Glance.Look(player.transform, Vector3.up * 1.35f, 1.6f);
        _nextGlanceAt = Time.time + 14f;
        Glances++;
    }

    // 영업이 열리면 동행마다 시간차를 두고 한 번 가게에 들른다. 방문 중 이동·구매·말풍선은 NpcController가 소유한다.
    bool UpdateShopVisit(ProducerNpcController producer, DemoSettlementController settlement)
    {
        var loop = DayNightShopLoopController.Instance;
        bool open = loop != null && loop.IsShopOpenForCustomers && settlement != null && settlement.OperatingShop != null;
        if (open && !_wasOpen) { _openedAt = Time.time; _visitedThisOpening = false; }
        _wasOpen = open;
        if (open) EnsureShopper(settlement);
        if (_shopper == null) return false;
        // 다른 초대(기존 손님 초대)로 쇼핑을 시작했어도 방문으로 받아들인다. 방문 중 이동은 NpcController 몫이다.
        if (_shopper.currentState != NpcController.State.Idle)
        {
            _shopperActive = true;
            Current = Activity.Shopping;
            Status = "가게에서 물건을 고르는 중";
            return true;
        }
        if (_shopperActive)
        {
            _shopperActive = false;
            _shopper.Pause();
            ShopVisits++;
            Current = Activity.GoingHome;
            if (_resident.Home != null) MoveTo(_resident.Home.position);
        }
        // 이웃인 동행은 문을 열자마자 들른다(관광객보다 먼저 와 진열을 살핀다).
        float delay = IsMiner ? 3f : 7f;
        if (open && !_visitedThisOpening && Time.time > _openedAt + delay && (producer == null || !producer.StarterWorking))
        {
            // 관광객처럼 먼저 상점 문 앞까지 걸어간 뒤 들어간다(집 앞에서 바로 진열대 경로를 찾지 않게).
            var interior = settlement.OperatingShop.GetComponentInParent<DemoShopInterior>(true);
            Vector3 door = interior != null && interior.outsideSpawn != null ? interior.outsideSpawn.position : settlement.OperatingShop.transform.position;
            if (Vector3.ProjectOnPlane(door - transform.position, Vector3.up).magnitude > 2.5f)
            {
                Current = Activity.GoingToShop;
                Status = "가게 구경 가는 중";
                if (!_agent.hasPath || Vector3.Distance(_agent.destination, door) > 1.5f) MoveTo(door);
                return true;
            }
            _shopper.Resume();
            _shopperActive = _shopper.TryBeginShoppingVisitAt(settlement.OperatingShop.transform);
            if (_shopperActive) { _visitedThisOpening = true; return true; }
            _shopper.Pause();
        }
        return false;
    }

    void EnsureShopper(DemoSettlementController settlement)
    {
        if (_shopper != null || _resident.Profile == null || settlement == null || settlement.OperatingShop == null) return;
        // 관광객과 같은 표현 정규화(절차 휴머노이드)로 붙인다.
        var normalizer = GetComponent<NpcPresentationNormalizer>() ?? gameObject.AddComponent<NpcPresentationNormalizer>();
        normalizer.animationMode = NpcPresentationNormalizer.NpcAnimationMode.HumanoidProcedural;
        normalizer.animatorController = null;
        _shopper = gameObject.AddComponent<NpcController>();
        // 구매 성향은 주민 프로필 그대로 두고, 말풍선·구매 반응에 보이는 이름만 동행 이름으로 한다(관광객과 같은 런타임 사본 방식).
        _shopperProfile = Instantiate(_resident.Profile);
        _shopperProfile.name = "Runtime_ResidentProfile_" + _resident.CompanionId;
        _shopperProfile.npcName = DisplayName;
        _shopper.profile = _shopperProfile;
        _shopper.randomSeed = IsMiner ? 7701 : 7702;
        _shopper.shopLocation = settlement.OperatingShop.transform;
        _shopper.Pause();
        if (GetComponentInChildren<NpcBubbleUI>(true) == null)
        {
            var bubbleObject = new GameObject("NpcBubbleUI", typeof(RectTransform), typeof(Canvas));
            bubbleObject.SetActive(false);
            bubbleObject.transform.SetParent(transform, false);
            var bubble = bubbleObject.AddComponent<NpcBubbleUI>();
            bubble.offset = new Vector3(0f, 4f, 0f);
            bubbleObject.SetActive(true);
        }
    }

    // 낮 일과: 일터 ↔ 집. 생산자가 없거나 첫 지원 생산을 마친 동행은 일터 주변을 살핀다.
    void DayCycle(Transform home)
    {
        if (_workTarget == null) _workTarget = FindWorkTarget();
        switch (Current)
        {
            case Activity.GoingToWork:
                if (Arrived()) { Current = Activity.Working; _phaseEndsAt = Time.time + 10f; }
                break;
            case Activity.Working:
                if (_workTarget != null) FaceTowards(_workTarget.position);
                WorkFeedback(_workTarget);
                InspectTarget();
                if (Time.time > _phaseEndsAt) { Current = Activity.GoingHome; MoveTo(home.position); }
                break;
            case Activity.GoingHome:
                if (Arrived()) { Current = Activity.Resting; _phaseEndsAt = Time.time + 6f; }
                break;
            case Activity.Resting:
                FacePlayerIfNear();
                if (Time.time > _phaseEndsAt) { Current = Activity.GoingToWork; MoveTo(WorkPoint(home)); }
                break;
            default:
                Current = Activity.GoingToWork; MoveTo(WorkPoint(home));
                break;
        }
        Status = Current switch
        {
            Activity.GoingToWork => IsMiner ? "광산 쪽으로 가는 중" : IsLumberjack ? "나무 쪽으로 가는 중" : "들판으로 가는 중",
            Activity.Working => IsMiner ? "광산 주변을 살피는 중" : IsLumberjack ? "나무 상태를 살피는 중" : "들판에서 쓸 만한 걸 찾는 중",
            Activity.GoingHome => "집으로 돌아가는 중",
            _ => "집 앞에서 쉬는 중"
        };
    }

    Transform FindWorkTarget()
    {
        var producer = _resident.Worksite != null ? _resident.Worksite.Producer : null;
        if (producer != null && producer.workSpot != null) return producer.workSpot;
        Vector3 from = _resident.Home != null ? _resident.Home.position : transform.position;
        if (IsMiner)
        {
            var rock = FindObjectsByType<MiningSpot>(FindObjectsSortMode.None).Where(m => m.IsDirectWorld)
                .OrderBy(m => Vector3.Distance(m.transform.position, from)).FirstOrDefault();
            if (rock != null) return rock.transform;
        }
        // 2026-11-03 K-01(지금 하는 일이 읽혀야 함): 벌목꾼은 "나무 상태를 살피는 중"이므로 집 가까운 실제 채집 나무로 간다(베지는 않는다).
        if (IsLumberjack)
        {
            var tree = FindObjectsByType<Gatherable>(FindObjectsSortMode.None).Where(g => g.IsDirectWorld && !g.DirectDepleted)
                .OrderBy(g => Vector3.Distance(g.transform.position, from)).FirstOrDefault();
            if (tree != null) return tree.transform;
        }
        var fruit = FindObjectsByType<DaytimeStockPrepPoint>(FindObjectsSortMode.None).Where(p => p.PhysicalFruit)
            .OrderBy(p => Vector3.Distance(p.transform.position, from)).FirstOrDefault();
        if (fruit != null) return fruit.transform;
        var meadow = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).OrderBy(b => Vector3.Distance(b.Home, from)).FirstOrDefault();
        return meadow != null ? meadow.transform : null;
    }

    Vector3 WorkPoint(Transform home)
    {
        if (_workTarget == null) return home.position + home.forward * 6f;
        Vector3 toward = Vector3.ProjectOnPlane(home.position - _workTarget.position, Vector3.up);
        return _workTarget.position + (toward.sqrMagnitude > .01f ? toward.normalized : Vector3.back) * 1.6f;
    }

    public int OffscreenHops { get; private set; }

    void MoveTo(Vector3 position)
    {
        if (NavMesh.SamplePosition(position, out var hit, 3f, NavMesh.AllAreas)) position = hit.position;
        _agent.isStopped = false;
        // Demo256의 NavMesh는 일부 단(테라스) 사이를 잇지 못한다(P10 지형 연결 과제). 경로가 끊기고
        // 플레이어 화면 밖일 때만 목적지가 있는 단의 가장 가까운 지점으로 옮겨 이어 걷는다. 화면 안에서는 옮기지 않는다.
        var path = new NavMeshPath();
        if (_agent.CalculatePath(position, path) && path.status != NavMeshPathStatus.PathComplete && !VisibleToPlayer() &&
            NavMesh.SamplePosition(position, out var near, 6f, NavMesh.AllAreas) && _agent.Warp(near.position))
            OffscreenHops++;
        _agent.SetDestination(position);
    }

    bool VisibleToPlayer()
    {
        var view = Camera.main;
        if (view == null) return false;
        Vector3 viewport = view.WorldToViewportPoint(transform.position + Vector3.up);
        return viewport.z > 0f && viewport.z < 60f && viewport.x > -.05f && viewport.x < 1.05f && viewport.y > -.05f && viewport.y < 1.05f;
    }

    // K-04(공동 장소·개인 수행) + K-03(다음 일과 이어짐): 문 열기(18시) 전 17:15부터 동행이 가게 문 옆에 모여 기다린다.
    // 문 옆 한 줄에 각자 자리를 잡고, 서로를 번갈아 보거나 가게 간판·진열창을 올려다본다(역할마다 자주 보는 곳이 다름).
    // 문이 열리면 기존 첫 방문(UpdateShopVisit)으로 그대로 들어가고, 열리지 않으면 18:30에 집으로 돌아간다. 표현·이동만, 생산·판매 권위 없음.
    const float GatherFrom = 17.25f, GatherUntil = 18.5f;
    public Vector3 GatherSpot { get; private set; }
    public bool AtGatherSpot { get; private set; }
    public int GatherLooks { get; private set; }
    float _nextGatherLookAt;
    int _gatherDay = -1;

    bool EveningGather(float hour, DemoSettlementController settlement)
    {
        var loop = DayNightShopLoopController.Instance;
        bool visited = loop != null && loop.IsShopOpenForCustomers && _visitedThisOpening;
        var shop = settlement != null && settlement.Established ? settlement.OperatingShop : null;
        if (hour < GatherFrom || hour >= GatherUntil || visited || shop == null || _resident.Home == null)
        {
            if (Current == Activity.Gathering)
            {
                AtGatherSpot = false; Glance.Release();
                Current = Activity.GoingHome; MoveTo(_resident.Home != null ? _resident.Home.position : _anchor);
            }
            return false;
        }
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        if (Current != Activity.Gathering || _gatherDay != day)
        {
            _gatherDay = day; AtGatherSpot = false;
            GatherSpot = ComputeGatherSpot(shop);
            Current = Activity.Gathering;
            MoveTo(GatherSpot);
        }
        if (!AtGatherSpot && Vector3.ProjectOnPlane(GatherSpot - transform.position, Vector3.up).magnitude < 1.1f && Arrived())
        {
            AtGatherSpot = true; _nextGatherLookAt = Time.time + .8f;
            if (_agent.hasPath) _agent.ResetPath();
        }
        else if (!AtGatherSpot && (!_agent.hasPath || Arrived())) MoveTo(GatherSpot);
        Status = AtGatherSpot ? "가게 문 열기를 기다리는 중" : "가게 앞으로 가는 중";
        if (!AtGatherSpot || FacePlayerIfNear()) return true;
        // 몸은 모인 줄 가운데(문 앞)를 보고, 고개로 이웃·간판·진열창을 번갈아 본다.
        var interior = shop.GetComponentInParent<DemoShopInterior>(true);
        Vector3 door = interior != null && interior.outsideSpawn != null ? interior.outsideSpawn.position : shop.transform.position;
        FaceTowards(door + Outward(shop, door) * 1.4f);
        if (Time.time >= _nextGatherLookAt)
        {
            _nextGatherLookAt = Time.time + Random.Range(3f, 4.5f);
            var neighbour = FindObjectsByType<DemoCompanionRoutine>(FindObjectsSortMode.None)
                .Where(r => r != this && r.Current == Activity.Gathering && r.AtGatherSpot).OrderBy(_ => Random.value).FirstOrDefault();
            // 광부는 값·간판을, 다른 동행은 이웃과의 이야기를 더 자주 본다.
            float neighbourChance = IsMiner ? .35f : .65f;
            if (neighbour != null && Random.value < neighbourChance) Glance.Look(neighbour.transform, Vector3.up * 1.4f, 2.2f);
            else Glance.Look(shop.transform, door - shop.transform.position + Vector3.up * (IsMiner ? 2.6f : 1.3f), 2f);
            GatherLooks++;
        }
        return true;
    }

    static Vector3 Outward(GameObject shop, Vector3 door)
    {
        Vector3 outward = Vector3.ProjectOnPlane(door - shop.transform.position, Vector3.up);
        return outward.sqrMagnitude > .04f ? outward.normalized : Vector3.ProjectOnPlane(shop.transform.forward, Vector3.up).normalized;
    }

    // 문 바로 앞(손님 동선)은 비우고 옆으로 한 줄: 0번 오른쪽 1.8m, 1번 왼쪽 1.8m, 2번 오른쪽 3m …
    Vector3 ComputeGatherSpot(GameObject shop)
    {
        var interior = shop.GetComponentInParent<DemoShopInterior>(true);
        Vector3 door = interior != null && interior.outsideSpawn != null ? interior.outsideSpawn.position : shop.transform.position;
        Vector3 outward = Outward(shop, door), lateral = Vector3.Cross(Vector3.up, outward);
        var order = FindObjectsByType<DemoCompanionRoutine>(FindObjectsSortMode.None)
            .Select(r => r._resident != null ? r._resident.CompanionId ?? r.name : r.name).OrderBy(id => id, System.StringComparer.Ordinal).ToList();
        int index = Mathf.Max(0, order.IndexOf(_resident.CompanionId ?? name));
        float side = index % 2 == 0 ? 1f : -1f;
        Vector3 spot = door + outward * 1.3f + lateral * side * (1.8f + 1.2f * (index / 2));
        return NavMesh.SamplePosition(spot, out var hit, 2.5f, NavMesh.AllAreas) ? hit.position : door + outward * 2f;
    }

    bool Arrived() => !_agent.pathPending && (_agent.remainingDistance <= _agent.stoppingDistance + .25f || !_agent.hasPath);

    // 중심 주변을 천천히 거닐며, 플레이어가 다가오면 멈춰서 바라본다(가만히 선 장식이 아니게).
    void StayAround(Vector3 center, float radius)
    {
        if (FacePlayerIfNear()) { if (_agent.hasPath) _agent.ResetPath(); return; }
        if (Vector3.Distance(transform.position, center) > radius + 1.5f && (!_agent.hasPath || Arrived()))
        { MoveTo(center); return; }
        if (radius <= 0f || Time.time < _nextWanderAt || _agent.hasPath && !Arrived()) return;
        _nextWanderAt = Time.time + Random.Range(5f, 8f);
        var offset = Random.insideUnitCircle * radius;
        MoveTo(center + new Vector3(offset.x, 0f, offset.y));
    }

    bool FacePlayerIfNear()
    {
        var player = Inventory.instance != null ? Inventory.instance.transform : null;
        if (player == null || Vector3.Distance(player.position, transform.position) > 3f) return false;
        FaceTowards(player.position);
        return true;
    }

    void FaceTowards(Vector3 point)
    {
        Vector3 flat = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
        if (flat.sqrMagnitude < .01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat.normalized), Time.deltaTime * 6f);
    }

    // 작업 소리/파편은 플레이어 근처에서만 낸다(멀리 있는 동행의 소리가 섞이지 않게).
    // 광부 동행은 바위를 보며 1.3초마다 플레이어와 같은 곡괭이 키로 휘두르고, 타격 효과는 날이 닿는 순간(ContactDelay)에 낸다.
    // 휘두르기 리듬은 거리와 무관하게 이어 가고(멀리서 봐도 손이 멈춰 있지 않게), 소리·파편은 플레이어 근처에서만 낸다.
    void WorkFeedback(Transform target)
    {
        if (target == null) return;
        var player = Inventory.instance != null ? Inventory.instance.transform : null;
        bool near = player != null && Vector3.Distance(player.position, transform.position) <= 14f;
        if (IsMiner && Vector3.Distance(transform.position, target.position) < 3.5f)
        {
            FaceTowards(target.position);
            if (_pendingHitAt > 0f && Time.time >= _pendingHitAt)
            {
                _pendingHitAt = -1f;
                // 플레이어 타격과 같은 규칙: 동행 몸에 가리지 않는 바위 윗면 옆쪽(표시 전용).
                if (near && _pendingHitTarget != null)
                    GatherFeedback.Hit(GatherFeedback.VisibleContact(_pendingHitTarget.position, _pendingHitTarget.position.y + .55f, .4f, transform.position), true, .45f);
            }
            if (Time.time < _nextFeedbackAt) return;
            _nextFeedbackAt = Time.time + 1.3f;
            if (_toolPose != null && _toolPose.Tool != null) _toolPose.Swing();
            _pendingHitAt = Time.time + GatherFeedback.ContactDelay;
            _pendingHitTarget = target;
        }
        else if (!IsMiner)
        {
            if (!near || Time.time < _nextFeedbackAt) return;
            _nextFeedbackAt = Time.time + 2.6f;
            GatherFeedback.Rustle(.4f);
        }
    }

    static string ToolLabel(ProducerNpcController producer)
    {
        var tool = producer.DemoEquippedTool != null ? producer.DemoEquippedTool.data : null;
        string name = tool != null ? ItemDisplayName.For(tool) : "맨손";
        return producer.DemoToolEfficiency > 1.01f ? $"{name} · 속도 ×{producer.DemoToolEfficiency:0.#}" : name;
    }

    // 가게 건물 바닥 위인지(방문 뒤 문까지 걸어 나가는 동안에도 도구를 넣어 둔다).
    bool InsideShop()
    {
        var shop = DemoSettlementController.Instance != null ? DemoSettlementController.Instance.OperatingShop : null;
        var interior = shop != null ? shop.GetComponentInParent<DemoShopInterior>(true) : null;
        return interior != null && interior.CoversGround(transform.position);
    }

    // 장착 도구를 손에 보인다. 받은 도구로 바뀌면 손의 모델도 바뀐다.
    // 들고 휘두르는 자세는 플레이어와 같은 키를 쓰는 NpcHeldToolPose가 매 프레임 맞춘다(2026-11-02 "플레이어처럼 자연스럽게").
    void UpdateHeldTool(ProducerNpcController producer)
    {
        Item tool = producer != null && producer.DemoEquippedTool != null ? producer.DemoEquippedTool.data : null;
        // 레퍼런스 연구 Q-NL06(잔류 도구로 쇼핑 = 실패): 가게에 손님으로 들른 동안은 도구를 넣어 두고, 나오면 다시 든다(장비 사실은 그대로).
        bool stow = _shopperActive || Current == Activity.GoingToShop || InsideShop();
        if (_heldTool != null && _heldTool.activeSelf == stow)
        {
            _heldTool.SetActive(!stow);
            _toolPose?.Hold(stow ? null : _heldTool, stow ? ToolType.None : _heldItem != null ? _heldItem.toolType : ToolType.None);
        }
        if (tool == _heldItem) return;
        _heldItem = tool;
        if (_heldTool != null) Destroy(_heldTool);
        _toolPose ??= GetComponent<NpcHeldToolPose>() ?? gameObject.AddComponent<NpcHeldToolPose>();
        _toolPose.Hold(null, ToolType.None);
        if (tool == null) return;
        var assets = FirstDayStudioAssets.Load();
        var model = assets != null ? assets.ModelFor(tool) : null;
        var animator = GetComponentInChildren<Animator>();
        var hand = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
        if (model == null || hand == null) return;
        // 플레이어 EquipmentSystem과 같은 원본 회전·손 길이 크기(.85m)로 만든다.
        _heldTool = Instantiate(model, hand.position, model.transform.localRotation);
        _heldTool.name = "CompanionTool_" + tool.id;
        foreach (var collider in _heldTool.GetComponentsInChildren<Collider>()) collider.enabled = false;
        FirstDayStudioAssets.ScaleVisual(_heldTool, .85f);
        _heldTool.transform.SetParent(hand, true);
        _toolPose.Hold(_heldTool, tool.toolType);
    }

    void ReactToEvents(ProducerNpcController producer, DemoSettlementController settlement)
    {
        // 혼잣말은 역할·말투별 표(ResidentDialogueLines bark:*)에서 고른다. 표가 비면 기존 문장을 쓴다.
        if (!_saidHome && !string.IsNullOrEmpty(_resident.TentId) && _resident.Home != null)
        {
            _saidHome = true;
            Say(ResidentContextDialogue.Bark(gameObject, _resident.Profile, "bark:home", "여기가 제 집이에요. 고마워요!"));
            // 같은 프레임에 정착이 끝나 생산자가 일터로 출발했다면 그 목적지를 덮지 않는다.
            if (producer == null || !producer.StarterWorking)
            {
                Current = Activity.GoingHome;
                MoveTo(_resident.Home.position);
            }
        }
        if (!_saidWork && settlement != null && settlement.Established)
        {
            _saidWork = true;
            Say(ResidentContextDialogue.Bark(gameObject, _resident.Profile, "bark:work",
                IsMiner ? "바위 쪽에서 광석을 캐 올게요." : "들판을 둘러보며 쓸 만한 걸 찾아볼게요."));
        }
        if (!_saidBatch && producer != null && producer.StarterBatchReady)
        {
            _saidBatch = true;
            Say(ResidentContextDialogue.Bark(gameObject, _resident.Profile, "bark:batch", "광석을 모아 왔어요. 집 앞에서 받아 가세요!"));
        }
    }

    // 사건 대사는 줄을 세워 두고, 진행 안내가 지나간 뒤 차례로 말한다.
    void Say(string line) => _pendingLines.Enqueue(line);
}
