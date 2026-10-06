using System.Linq;
using UnityEngine;
using UnityEngine.AI;

// P7 — 동행의 하루. 기존 DemoResident(집)·WorksiteBinding/ProducerNpcController(생산)·NavMesh 이동을 잇는 표현 어댑터다.
// 새 생산/재고 권위를 만들지 않는다. 생산자가 일하는 동안(StarterWorking) 이동은 생산자 FSM이 소유한다.
[RequireComponent(typeof(DemoResident))]
public sealed class DemoCompanionRoutine : MonoBehaviour
{
    public enum Activity { Waiting, GoingHome, Resting, GoingToWork, Working, BatchReady, GoingToShop, Shopping }
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

    bool IsMiner => _resident != null && _resident.CompanionId != null && _resident.CompanionId.StartsWith("Miner", System.StringComparison.Ordinal);

    void Awake()
    {
        _resident = GetComponent<DemoResident>();
        _anchor = transform.position;
    }

    void Start()
    {
        var candidate = DemoRouteController.SelectedCompanions.FirstOrDefault(c => c != null && c.id == _resident.CompanionId);
        DisplayName = candidate != null && !string.IsNullOrEmpty(candidate.displayName)
            ? candidate.displayName.Split('·')[0].Trim()
            : _resident.CompanionId;
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
        if (night || settlement == null || !settlement.Established)
        {
            Status = night ? "집에서 쉬는 중" : "새 집을 둘러보는 중";
            Current = Activity.Resting;
            StayAround(_resident.Home.position, night ? 0f : 2f);
            return;
        }
        DayCycle(_resident.Home);
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
            Activity.GoingToWork => IsMiner ? "광산 쪽으로 가는 중" : "들판으로 가는 중",
            Activity.Working => IsMiner ? "광산 주변을 살피는 중" : "들판에서 쓸 만한 걸 찾는 중",
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
    void WorkFeedback(Transform target)
    {
        if (target == null || Time.time < _nextFeedbackAt) return;
        var player = Inventory.instance != null ? Inventory.instance.transform : null;
        if (player == null || Vector3.Distance(player.position, transform.position) > 14f) return;
        if (IsMiner && Vector3.Distance(transform.position, target.position) < 3.5f)
        {
            _nextFeedbackAt = Time.time + 1.3f;
            GatherFeedback.Hit(Vector3.Lerp(target.position, transform.position, .35f) + Vector3.up * .55f, true, .45f);
        }
        else if (!IsMiner)
        {
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

    // 장착 도구를 손에 보인다. 받은 도구로 바뀌면 손의 모델도 바뀐다.
    void UpdateHeldTool(ProducerNpcController producer)
    {
        Item tool = producer != null && producer.DemoEquippedTool != null ? producer.DemoEquippedTool.data : null;
        if (tool == _heldItem) return;
        _heldItem = tool;
        if (_heldTool != null) Destroy(_heldTool);
        if (tool == null) return;
        var assets = FirstDayStudioAssets.Load();
        var model = assets != null ? assets.ModelFor(tool) : null;
        var animator = GetComponentInChildren<Animator>();
        var hand = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
        if (model == null || hand == null) return;
        _heldTool = Instantiate(model, hand.position, model.transform.rotation);
        _heldTool.name = "CompanionTool_" + tool.id;
        foreach (var collider in _heldTool.GetComponentsInChildren<Collider>()) collider.enabled = false;
        FirstDayStudioAssets.ScaleVisual(_heldTool, .7f);
        _heldTool.transform.SetParent(hand, true);
        _heldTool.transform.localRotation = Quaternion.Euler(0, 0, 90) * model.transform.localRotation;
    }

    void ReactToEvents(ProducerNpcController producer, DemoSettlementController settlement)
    {
        if (!_saidHome && !string.IsNullOrEmpty(_resident.TentId) && _resident.Home != null)
        {
            _saidHome = true;
            Say("여기가 제 집이에요. 고마워요!");
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
            Say(IsMiner ? "바위 쪽에서 광석을 캐 올게요." : "들판을 둘러보며 쓸 만한 걸 찾아볼게요.");
        }
        if (!_saidBatch && producer != null && producer.StarterBatchReady)
        {
            _saidBatch = true;
            Say("광석을 모아 왔어요. 집 앞에서 받아 가세요!");
        }
    }

    // 사건 대사는 줄을 세워 두고, 진행 안내가 지나간 뒤 차례로 말한다.
    void Say(string line) => _pendingLines.Enqueue(line);
}
