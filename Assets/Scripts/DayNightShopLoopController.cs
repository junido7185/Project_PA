using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public enum PADayNightPhase
{
    DayPreparation,
    ShopOpen,
    Settlement
}

// First safe Milestone 1 layer:
// day prep state -> visible shop open state -> runtime stock-prep points.
// This deliberately does not gate Shop/NPC purchase logic yet, so the existing
// Day 1 route remains intact while the new loop becomes visible and testable.
public class DayNightShopLoopController : MonoBehaviour
{
    public static DayNightShopLoopController Instance { get; private set; }

    [Header("Phase Hours")]
    [Range(0f, 23.99f)] public float dayStartHour = 6f;
    [Range(0f, 23.99f)] public float shopOpenHour = 18f;
    [Range(0f, 23.99f)] public float settlementHour = 23f;
    public bool keepDay1TutorialShopOpen = true;

    [Header("MVP Day Prep Activity")]
    public bool autoCreateDayPrepPoint = true;
    public string prepItemResourcePath = "Items/Item_Carrot";
    public int prepItemCount = 2;
    public string secondaryPrepItemResourcePath = "Items/Item_Wheat";
    public int secondaryPrepItemCount = 2;

    [Header("UI")]
    public bool autoCreateUI = true;
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI activityText;

    PADayNightPhase _phase;
    int _observedDay = -1;
    readonly Dictionary<string, int> _prepCollectionDays = new Dictionary<string, int>();
    string _lastActivityResult = "낮 준비: 채집 포인트를 찾아 오늘 밤 팔 물건을 준비하세요.";
    readonly List<DaytimeStockPrepPoint> _prepPoints = new List<DaytimeStockPrepPoint>();
    Canvas _canvas;

    // CDN-002 — 플레이어가 오늘 밤 영업을 시작했는지 추적한다.
    bool _playerOpenedShopToday;
    int _openedShopDay = -1;
    ShopOpenSign _shopSign;

    public PADayNightPhase CurrentPhase => _phase;
    public bool IsShopOpen => _phase == PADayNightPhase.ShopOpen
        || (keepDay1TutorialShopOpen && CurrentDay <= 1);
    public bool CanCollectDayPrepStock => _phase == PADayNightPhase.DayPreparation
        && HasAvailablePrepPoint();
    public string LastActivityResult => _lastActivityResult;

    // CDN-002 — 손님 구매 게이트.
    // Day 1 튜토리얼은 항상 열림으로 처리해 검증된 첫 판매 루트를 보존한다.
    public bool IsTutorialAlwaysOpen => keepDay1TutorialShopOpen
        && CurrentDay <= 1 && _phase != PADayNightPhase.Settlement;
    // 플레이어가 오늘 간판으로 영업을 시작했는가.
    public bool PlayerHasOpenedShopToday => _playerOpenedShopToday && _openedShopDay == CurrentDay;
    // 손님이 실제로 구매를 시도할 수 있는가(밤 ShopOpen + 플레이어가 영업 시작, 또는 Day 1 튜토리얼).
    public bool IsShopOpenForCustomers => IsTutorialAlwaysOpen
        || (_phase == PADayNightPhase.ShopOpen && PlayerHasOpenedShopToday);
    // 플레이어가 지금 영업을 시작할 수 있는가(밤 ShopOpen 단계, 아직 안 열었음).
    public bool CanPlayerOpenShop => !IsTutorialAlwaysOpen
        && _phase == PADayNightPhase.ShopOpen && !PlayerHasOpenedShopToday;
    // 정산 때 기존 가게 간판으로 하루를 마감하고 다음 날 아침을 시작한다.
    public bool CanPlayerStartNextDay => _phase == PADayNightPhase.Settlement;

    int CurrentDay => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
    float CurrentHour => GameClock.Instance != null ? GameClock.Instance.CurrentHour : 8f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (autoCreateUI && phaseText == null)
            BuildUI();
    }

    void Start()
    {
        if (GameClock.Instance != null)
        {
            GameClock.Instance.OnHourTick += OnHourTick;
            GameClock.Instance.OnMinuteTick += OnMinuteTick;
            GameClock.Instance.OnNewDay += OnNewDay;
        }

        RefreshState(force: true);

        if (autoCreateDayPrepPoint)
            EnsureDayPrepPoints();

        EnsureShopOpenSign();
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null)
        {
            GameClock.Instance.OnHourTick -= OnHourTick;
            GameClock.Instance.OnMinuteTick -= OnMinuteTick;
            GameClock.Instance.OnNewDay -= OnNewDay;
        }

        if (Instance == this)
            Instance = null;
    }

    void OnHourTick(int _) => RefreshState(force: false);
    void OnMinuteTick(int _) => RefreshState(force: false);

    void OnNewDay(int day)
    {
        _observedDay = day;
        _playerOpenedShopToday = false;
        _openedShopDay = -1;
        _lastActivityResult = day > 1 && SalesLogManager.Instance != null
            ? SalesLogManager.Instance.BuildNextDayAdvice(day - 1)
            : "새로운 아침: 밤 영업 전에 팔 물건을 준비하세요.";
        RefreshState(force: true);
        RefreshPrepPoints();
    }

    // CDN-002 — 플레이어가 간판/카운터로 밤 영업을 시작한다.
    // ShopOpen 단계에서만 실제로 열리며, 그 전에는 자연스러운 안내만 보여준다.
    public bool TryOpenShop()
    {
        RefreshState(force: false);

        if (IsTutorialAlwaysOpen)
        {
            _lastActivityResult = "튜토리얼: 가게가 이미 열려 있어요. 첫 손님을 맞이하세요.";
            RefreshUI();
            return true;
        }

        if (_phase != PADayNightPhase.ShopOpen)
        {
            _lastActivityResult = "아직 영업 시간이 아니에요. 해가 지면 간판에서 영업을 시작하세요.";
            RefreshUI();
            return false;
        }

        _playerOpenedShopToday = true;
        _openedShopDay = CurrentDay;
        _lastActivityResult = "가게 영업을 시작했어요! 손님이 곧 찾아옵니다.";
        RefreshUI();
        return true;
    }

    public bool TryStartNextDay()
    {
        return TryStartNextDayInternal(allowCompletedDay1Tutorial: false);
    }

    // Day 1은 시계보다 온보딩 단계가 먼저 끝날 수 있으므로 결산 버튼에서만 조기 마감을 허용한다.
    public bool TryStartNextDayAfterTutorial()
    {
        return TryStartNextDayInternal(allowCompletedDay1Tutorial: true);
    }

    bool TryStartNextDayInternal(bool allowCompletedDay1Tutorial)
    {
        RefreshState(force: false);

        if (LongPlayProgressionController.Instance != null
            && LongPlayProgressionController.Instance.IsMilestoneCompletionOpen)
        {
            _lastActivityResult = "운영 완주 기록에서 계속 플레이 또는 저장 후 종료를 선택하세요.";
            RefreshUI();
            return false;
        }

        bool tutorialCompletion = allowCompletedDay1Tutorial && CurrentDay == 1;
        if (!CanPlayerStartNextDay && !tutorialCompletion)
        {
            _lastActivityResult = "정산 시간이 되면 가게 간판에서 하루를 마무리할 수 있어요.";
            RefreshUI();
            return false;
        }

        if (GameClock.Instance == null)
        {
            Debug.LogWarning("[DayNightShopLoop] GameClock이 없어 다음 날을 시작할 수 없습니다.");
            return false;
        }

        int closingDay = CurrentDay;
        GameClock.Instance.AdvanceToNextDayMorning(dayStartHour, "상점 정산 완료");
        Debug.Log($"🌅 [DayNightShopLoop] Day {closingDay} 정산 완료 — Day {CurrentDay} 아침 시작");
        return CurrentDay == closingDay + 1;
    }

    // 검증용: 플레이어 영업 시작 상태를 직접 설정한다.
    public void SetShopOpenedForValidation(bool opened)
    {
        _playerOpenedShopToday = opened;
        _openedShopDay = opened ? CurrentDay : -1;
        RefreshUI();
    }

    // SaveManager 가 호출 — 당일 채집 완료 상태를 직렬화한다.
    public void WriteSaveFields(SaveData data)
    {
        if (data == null) return;

        data.dayPrepCollectedDay = CurrentDay;
        if (data.dayPrepCollectedActivities == null)
            data.dayPrepCollectedActivities = new List<string>();
        else
            data.dayPrepCollectedActivities.Clear();

        foreach (var kv in _prepCollectionDays)
        {
            if (kv.Value >= CurrentDay)
                data.dayPrepCollectedActivities.Add(kv.Key);
        }

        data.shopOpenedDay = PlayerHasOpenedShopToday ? CurrentDay : -1;
    }

    // SaveManager 가 호출 — 저장된 날과 현재 날이 같을 때만 당일 채집 완료 상태를 복원한다.
    public void RestoreSavedState(int collectedDay, List<string> collectedActivities,
        int openedShopDay = -1)
    {
        _prepCollectionDays.Clear();
        // 이전 플레이의 채집 성공 문구가 과거 저장의 빈 가방 안내와 충돌하지 않게 한다.
        _lastActivityResult = string.Empty;

        if (collectedActivities != null && collectedDay >= CurrentDay)
        {
            foreach (var id in collectedActivities)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    _prepCollectionDays[id.Trim()] = collectedDay;
            }
        }

        // GameClock.ForceSet intentionally does not emit gameplay events. Rebuild
        // the phase explicitly before restoring today's open-sign transaction.
        RefreshState(force: true);
        _playerOpenedShopToday = openedShopDay == CurrentDay &&
                                 _phase == PADayNightPhase.ShopOpen;
        _openedShopDay = _playerOpenedShopToday ? CurrentDay : -1;
        RefreshPrepPoints();
        RefreshUI();
    }

    // 주민 요청을 포함한 낮 활동이 오늘 이미 완료됐는지 조회한다.
    // SaveData 필드를 늘리지 않고 기존 dayPrepCollectedActivities 문자열 목록을 공유한다.
    public bool IsDailyActivityCompleted(string activityId)
    {
        if (string.IsNullOrWhiteSpace(activityId)) return false;

        return _prepCollectionDays.TryGetValue(activityId.Trim(), out int day)
            && day >= CurrentDay;
    }

    // 현재 낮 준비 단계에서만 일일 활동을 한 번 완료 처리한다.
    public bool TryCompleteDailyActivity(string activityId)
    {
        RefreshState(force: false);
        if (_phase != PADayNightPhase.DayPreparation || string.IsNullOrWhiteSpace(activityId))
            return false;

        string normalizedId = activityId.Trim();
        if (IsDailyActivityCompleted(normalizedId))
            return false;

        _prepCollectionDays[normalizedId] = CurrentDay;
        RefreshPrepPoints();
        RefreshUI();
        return true;
    }

    public bool TryCollectDayPrepStock(DaytimeStockPrepPoint source, GameObject interactor)
    {
        RefreshState(force: false);
        string activityId = ResolveActivityId(source);

        if (_phase != PADayNightPhase.DayPreparation)
        {
            _lastActivityResult = IsShopOpen
                ? "지금은 영업 시간: 채집은 내일 낮에 다시 할 수 있어요."
                : "재고 준비는 낮 시간에만 할 수 있어요.";
            RefreshUI();
            return false;
        }

        if (IsPrepActivityCollectedToday(activityId))
        {
            _lastActivityResult = "이 채집 활동은 오늘 이미 완료했어요.";
            RefreshUI();
            return false;
        }

        Item item = Resources.Load<Item>(string.IsNullOrWhiteSpace(prepItemResourcePath)
            ? "Items/Item_Carrot"
            : prepItemResourcePath);

        if (source != null && !string.IsNullOrWhiteSpace(source.itemResourcePath))
            item = Resources.Load<Item>(source.itemResourcePath) ?? item;

        if (item == null)
        {
            _lastActivityResult = "낮 준비 실패: 재고 아이템 데이터가 없어요.";
            RefreshUI();
            Debug.LogWarning("[DayNightShopLoop] Could not load prep stock item.");
            return false;
        }

        int count = source != null ? Mathf.Max(1, source.grantCount) : Mathf.Max(1, prepItemCount);
        var instance = new ItemInstance(item, count)
        {
            quality = 1.03f,
            currentPrice = item.basePrice
        };

        bool added = Inventory.instance != null && Inventory.instance.AddInstance(instance);
        if (!added)
        {
            _lastActivityResult = $"낮 준비 중단: 인벤토리가 가득 찼어요 ({item.itemName}).";
            RefreshUI();
            return false;
        }

        _prepCollectionDays[activityId] = CurrentDay;
        _lastActivityResult = $"낮 준비 완료: {item.itemName} x{count} — 오늘 밤 진열할 수 있어요!";
        Debug.Log($"[DayNightShopLoop] {_lastActivityResult}");
        RefreshPrepPoints();
        RefreshUI();
        return true;
    }

    public bool IsDayPrepPointAvailable(DaytimeStockPrepPoint point)
    {
        RefreshState(force: false);
        return _phase == PADayNightPhase.DayPreparation
            && !IsPrepActivityCollectedToday(ResolveActivityId(point));
    }

    public void SimulatePhaseForValidation(float hour, int day)
    {
        if (GameClock.Instance != null)
            GameClock.Instance.ForceSet(hour, day, "DayNightShopLoopValidator");

        RefreshState(force: true);
        RefreshPrepPoints();
    }

    public void ResetDayPrepForValidation()
    {
        _prepCollectionDays.Clear();
        _lastActivityResult = "Validation reset: day prep stock is available.";
        RefreshState(force: true);
        RefreshPrepPoints();
    }

    void RefreshState(bool force)
    {
        int day = CurrentDay;
        if (_observedDay != day)
        {
            _observedDay = day;
        }

        PADayNightPhase next = ResolvePhase(CurrentHour);
        if (!force && next == _phase)
        {
            RefreshUI();
            return;
        }

        _phase = next;
        RefreshUI();
    }

    PADayNightPhase ResolvePhase(float hour)
    {
        if (hour >= shopOpenHour && hour < settlementHour)
            return PADayNightPhase.ShopOpen;

        if (hour >= settlementHour || hour < dayStartHour)
            return PADayNightPhase.Settlement;

        return PADayNightPhase.DayPreparation;
    }

    void EnsureDayPrepPoints()
    {
        _prepPoints.Clear();
        _prepPoints.AddRange(FindObjectsByType<DaytimeStockPrepPoint>(FindObjectsSortMode.None));

        if (_prepPoints.Count < 1)
            _prepPoints.Add(CreatePrepPoint(
                "garden-basket",
                "PA_DaytimeStockPrepPoint_GardenBasket",
                prepItemResourcePath,
                prepItemCount,
                "텃밭 바구니",
                ResolvePrepPointPosition(0),
                new Vector3(0.95f, 0.55f, 0.95f),
                new Color(0.95f, 0.68f, 0.34f, 1f)));

        if (_prepPoints.Count < 2)
            _prepPoints.Add(CreatePrepPoint(
                "producer-dropbox",
                "PA_DaytimeStockPrepPoint_ProducerBox",
                secondaryPrepItemResourcePath,
                secondaryPrepItemCount,
                "생산자 납품함",
                ResolvePrepPointPosition(1),
                new Vector3(1.05f, 0.48f, 0.82f),
                new Color(0.40f, 0.66f, 0.92f, 1f)));

        // IL-001 — 마을을 걸어가 발견하는 실제 야생 채집 포인트(분산 배치, 하루 1회, sellable Raw 아이템).
        // 기존 Garden Prep Basket/Producer Drop Box 는 튜토리얼/NPC 지원용 보조 재고로 유지한다.
        EnsureForagePoint("forest-forage", "PA_ForagePoint_Forest", "Items/Item_Carrot", 2,
            "숲길 채집", 40f, 9f, new Color(0.45f, 0.78f, 0.40f, 1f));
        var shorePoint = EnsureForagePoint("shore-forage", "PA_ForagePoint_Shore", "Items/Item_Fish", 2,
            "해변 낚시터", 130f, 12f, new Color(0.36f, 0.74f, 0.92f, 1f));
        EnsureFishingSpot(shorePoint);
        EnsureForagePoint("meadow-forage", "PA_ForagePoint_Meadow", "Items/Item_Wheat", 2,
            "들판 채집", 225f, 10f, new Color(0.92f, 0.85f, 0.42f, 1f));
        var miningPoint = EnsureMiningPoint();
        EnsureMiningSpot(miningPoint);
        EnsureForagePoint("farm-seed-pouch", "PA_FarmSeedPouch", "Items/Item_15_Seed", 2,
            "농장 씨앗 주머니", 205f, 9f, new Color(0.76f, 0.67f, 0.38f, 1f));
        FarmPlotInteraction.EnsureRuntimePlots();

        RefreshPrepPoints();
    }

    DaytimeStockPrepPoint FindPrepPoint(string activityId)
    {
        foreach (var p in _prepPoints)
            if (p != null && ResolveActivityId(p) == activityId) return p;
        return null;
    }

    DaytimeStockPrepPoint EnsureForagePoint(string activityId, string objectName, string itemPath, int count,
        string label, float angleDeg, float distance, Color color)
    {
        var existing = FindPrepPoint(activityId);
        if (existing != null) return existing;

        Vector3 pos = ResolveForagePosition(angleDeg, distance);
        var point = CreatePrepPoint(activityId, objectName, itemPath, count, label, pos,
            new Vector3(0.7f, 0.7f, 0.7f), color);

        // NPC 이동을 막지 않도록 트리거 콜라이더로 둔다(PlayerInteraction 은 트리거도 감지).
        var col = point.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        _prepPoints.Add(point);
        return point;
    }

    void EnsureFishingSpot(DaytimeStockPrepPoint shorePoint)
    {
        if (shorePoint == null) return;

        // The child collider resolves FishingSpot before the parent IInteractable,
        // while the parent remains the owner of daily/save state.
        var parentCollider = shorePoint.GetComponent<Collider>();
        if (parentCollider != null)
            parentCollider.enabled = false;

        var interaction = shorePoint.transform.Find("FishingInteraction");
        if (interaction == null)
        {
            var interactionGo = new GameObject("FishingInteraction");
            interactionGo.layer = shorePoint.gameObject.layer;
            interaction = interactionGo.transform;
            interaction.SetParent(shorePoint.transform, false);
        }

        interaction.localPosition = Vector3.zero;
        interaction.localRotation = Quaternion.identity;
        interaction.localScale = Vector3.one;

        var trigger = interaction.GetComponent<BoxCollider>();
        if (trigger == null)
            trigger = interaction.gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.75f, 0f);
        trigger.size = new Vector3(2.4f, 2.2f, 2.4f);

        var fishing = interaction.GetComponent<FishingSpot>();
        if (fishing == null)
            fishing = interaction.gameObject.AddComponent<FishingSpot>();
        fishing.Configure(shorePoint);
    }

    DaytimeStockPrepPoint EnsureMiningPoint()
    {
        const string activityId = "quarry-mining";
        var existing = FindPrepPoint(activityId);
        if (existing != null) return existing;

        var point = CreatePrepPoint(
            activityId,
            "PA_MiningPoint_Quarry",
            "Items/Item_Ore",
            2,
            "광산 채굴지",
            ResolveMiningPosition(),
            new Vector3(0.9f, 0.75f, 0.9f),
            new Color(0.44f, 0.43f, 0.48f, 1f));

        var col = point.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        _prepPoints.Add(point);
        return point;
    }

    void EnsureMiningSpot(DaytimeStockPrepPoint miningPoint)
    {
        if (miningPoint == null) return;

        // 부모는 일일/저장 상태만 소유하고 자식이 전용 광질 입력을 받는다.
        var parentCollider = miningPoint.GetComponent<Collider>();
        if (parentCollider != null)
            parentCollider.enabled = false;

        var interaction = miningPoint.transform.Find("MiningInteraction");
        if (interaction == null)
        {
            var interactionGo = new GameObject("MiningInteraction");
            interactionGo.layer = miningPoint.gameObject.layer;
            interaction = interactionGo.transform;
            interaction.SetParent(miningPoint.transform, false);
        }

        interaction.localPosition = Vector3.zero;
        interaction.localRotation = Quaternion.identity;
        interaction.localScale = Vector3.one;

        var trigger = interaction.GetComponent<BoxCollider>();
        if (trigger == null)
            trigger = interaction.gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.8f, 0f);
        trigger.size = new Vector3(2.8f, 2.4f, 2.8f);

        var mining = interaction.GetComponent<MiningSpot>();
        if (mining == null)
            mining = interaction.gameObject.AddComponent<MiningSpot>();
        mining.Configure(miningPoint);
    }

    Vector3 ResolveMiningPosition()
    {
        GameObject anchor = GameObject.Find("WorkSpot_Miner") ?? GameObject.Find("MineZone");
        if (anchor == null)
            return ResolveForagePosition(310f, 13f);

        Vector3 target = anchor.transform.position;
        Vector3 rayStart = target + Vector3.up * 8f;
        if (Physics.Raycast(rayStart, Vector3.down, out var hit, 24f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            target = hit.point + Vector3.up * 0.35f;
        else
            target += Vector3.up * 0.35f;

        if (NavMesh.SamplePosition(target, out var navHit, 5f, NavMesh.AllAreas))
            target = new Vector3(navHit.position.x, target.y, navHit.position.z);

        return target;
    }

    Vector3 ResolveForagePosition(float angleDeg, float distance)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        Shop shop = PA_ShopLocator.FindPlazaShop(); // S2 — 실내 상점 제외 앵커
        Vector3 anchor = player != null ? player.transform.position
            : (shop != null ? shop.transform.position : Vector3.zero);

        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        Vector3 target = anchor + dir * distance;

        // 지면으로 떨어뜨려 Y 보정.
        Vector3 rayStart = target + Vector3.up * 6f;
        if (Physics.Raycast(rayStart, Vector3.down, out var hit, 20f))
            target = hit.point + Vector3.up * 0.35f;
        else
            target.y = anchor.y + 0.35f;

        // 가능하면 걷는 길 근처로 보정(NavMesh 가 있을 때만).
        if (NavMesh.SamplePosition(target, out var navHit, 5f, NavMesh.AllAreas))
            target = new Vector3(navHit.position.x, target.y, navHit.position.z);

        return target;
    }

    void EnsureShopOpenSign()
    {
        if (_shopSign != null) return;

        _shopSign = FindFirstObjectByType<ShopOpenSign>();
        if (_shopSign != null) return;

        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.name = "PA_ShopOpenSign";
        sign.transform.localScale = new Vector3(0.6f, 0.9f, 0.16f);
        sign.transform.position = ResolveSignPosition();

        var col = sign.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        var renderer = sign.GetComponent<Renderer>();
        if (renderer != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            renderer.material = new Material(shader) { color = new Color(0.85f, 0.45f, 0.30f, 1f) };
        }

        _shopSign = sign.AddComponent<ShopOpenSign>();
    }

    Vector3 ResolveSignPosition()
    {
        Shop shop = PA_ShopLocator.FindPlazaShop(); // S2 — 실내 상점 제외 앵커
        if (shop != null)
            return shop.transform.position + shop.transform.forward * 1.6f + shop.transform.right * 1.2f + Vector3.up * 0.45f;

        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        if (player != null)
            return player.transform.position + player.transform.forward * 2.0f + Vector3.up * 0.45f;

        return new Vector3(2f, 0.45f, 2f);
    }

    DaytimeStockPrepPoint CreatePrepPoint(string activityId, string objectName, string itemPath, int count,
        string label, Vector3 position, Vector3 scale, Color color)
    {
        GameObject point = GameObject.CreatePrimitive(PrimitiveType.Cube);
        point.name = objectName;
        point.transform.position = position;
        point.transform.localScale = scale;

        var renderer = point.GetComponent<Renderer>();
        if (renderer != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            renderer.material = new Material(shader) { color = color };
        }

        var prepPoint = point.AddComponent<DaytimeStockPrepPoint>();
        prepPoint.Configure(activityId, itemPath, count, label);
        return prepPoint;
    }

    Vector3 ResolvePrepPointPosition(int index)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        if (player != null)
        {
            float side = index == 0 ? 1.6f : -1.6f;
            float forward = index == 0 ? 1.2f : 1.45f;
            return player.transform.position + player.transform.right * side + player.transform.forward * forward + Vector3.up * 0.28f;
        }

        Shop shop = PA_ShopLocator.FindPlazaShop(); // S2 — 실내 상점 제외 앵커
        if (shop != null)
        {
            Vector3 offset = index == 0
                ? new Vector3(-2.0f, 0.28f, 1.5f)
                : new Vector3(2.0f, 0.28f, 1.5f);
            return shop.transform.position + offset;
        }

        return new Vector3(0f, 0.28f, 0f);
    }

    void RefreshPrepPoints()
    {
        if (_prepPoints.Count == 0)
            _prepPoints.AddRange(FindObjectsByType<DaytimeStockPrepPoint>(FindObjectsSortMode.None));

        foreach (var point in _prepPoints)
        {
            if (point == null) continue;
            point.SetCollected(IsPrepActivityCollectedToday(ResolveActivityId(point)));
        }
    }

    bool HasAvailablePrepPoint()
    {
        if (_prepPoints.Count == 0)
            _prepPoints.AddRange(FindObjectsByType<DaytimeStockPrepPoint>(FindObjectsSortMode.None));

        if (_prepPoints.Count == 0)
            return !IsPrepActivityCollectedToday("default");

        foreach (var point in _prepPoints)
        {
            if (point != null && !IsPrepActivityCollectedToday(ResolveActivityId(point)))
                return true;
        }

        return false;
    }

    bool IsPrepActivityCollectedToday(string activityId)
    {
        return IsDailyActivityCompleted(activityId);
    }

    static string ResolveActivityId(DaytimeStockPrepPoint point)
    {
        if (point == null || string.IsNullOrWhiteSpace(point.activityId))
            return "default";

        return point.activityId.Trim();
    }

    void RefreshUI()
    {
        if (phaseText != null)
        {
            string phaseName = _phase switch
            {
                PADayNightPhase.DayPreparation => "낮 준비",
                PADayNightPhase.ShopOpen => "밤 영업 시간",
                PADayNightPhase.Settlement => "정산",
                _ => "하루 루프"
            };

            // CDN-002 — 손님 구매 게이트 상태를 명확히 표시한다.
            string shopState;
            if (_phase == PADayNightPhase.Settlement)
                shopState = "상점: 정산 완료 · 간판 [Space]로 다음 날 시작";
            else if (IsShopOpenForCustomers)
                shopState = IsTutorialAlwaysOpen ? "상점: 영업 중 (튜토리얼)" : "상점: 영업 중 (손님 구매 가능)";
            else if (CanPlayerOpenShop)
                shopState = "상점: 간판에서 영업 시작 가능";
            else
                shopState = "상점: 준비 중 (낮 채집·진열 후 밤에 영업)";

            phaseText.text = $"{CurrentDay}일차 {TimeLabel(CurrentHour)} · {phaseName}\n{shopState}";
        }

        if (activityText != null)
        {
            activityText.text = _phase == PADayNightPhase.Settlement && SalesLogManager.Instance != null
                ? SalesLogManager.Instance.BuildDailyDecisionSummary(CurrentDay)
                : _lastActivityResult;
        }
    }

    static string TimeLabel(float hour)
    {
        int h = Mathf.FloorToInt(hour);
        int m = Mathf.FloorToInt((hour - h) * 60f);
        return $"{h:D2}:{m:D2}";
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("DayNightShopLoopCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 57;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Visual Demo Integration Pass v2 — 상단 중앙 겹침 해소: 페이즈 스트립을
        // 좌측 컬럼(ClockHUD 아래)으로 옮기고 컴팩트하게 줄인다.
        var panel = new GameObject("DayNightShopLoopPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)panel.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -100f);
        rt.sizeDelta = new Vector2(340f, 76f);

        var bg = panel.GetComponent<Image>();
        bg.color = new Color(0.06f, 0.07f, 0.06f, 0.55f);
        bg.raycastTarget = false;

        phaseText = CreateText(panel.transform, "DayNightPhaseText",
            new Vector2(12f, -7f), new Vector2(316f, 38f), 13f, FontStyles.Bold);
        phaseText.color = new Color(1f, 0.94f, 0.72f, 1f);

        activityText = CreateText(panel.transform, "DayNightActivityText",
            new Vector2(12f, -48f), new Vector2(316f, 22f), 11f, FontStyles.Normal);
        activityText.color = new Color(0.78f, 0.92f, 0.82f, 1f);
    }

    TextMeshProUGUI CreateText(Transform parent, string name, Vector2 topLeftOffset, Vector2 size, float fontSize, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = topLeftOffset;
        rt.sizeDelta = size;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }
}
