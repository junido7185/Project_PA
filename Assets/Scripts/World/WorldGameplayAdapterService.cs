using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
#endif

public enum WorldGameplayAdapterState
{
    Bootstrapping = 0,
    Ready = 1,
    CustomerMoving = 2,
    CustomerPurchased = 3,
    Failed = 4,
    CustomerDeclined = 5
}

// WORLD-009 — reversible WorldSandbox-only seam between the generated world and
// the existing gameplay authorities. It owns no inventory, economy, crafting,
// purchase, clock or save rules; it only gives those systems generated anchors.
[DisallowMultipleComponent]
[DefaultExecutionOrder(120)]
public sealed class WorldGameplayAdapterService : MonoBehaviour
{
    public const long DefaultWorldSeed = 9009L;
    public const string RuntimeRootName = "WorldGameplay_Runtime";
    public const string PlayerRootName = "WorldGameplay_Player_Runtime";
    public const string CustomerRootName = "WorldGameplay_Customer_Runtime";
    public const float PlayableSecondsPerGameHour = 15f;
    const float BootstrapHoldSecondsPerGameHour = 99999f;

    const string MarketBuildingResource = "Buildings/Building_B01_MarketStall";
    const string WorkbenchBuildingResource = "Buildings/Building_B05_Workbench";
    const string KitchenBuildingResource = "Buildings/Building_B06_KitchenStation";
    const string ForgeBuildingResource = "Buildings/Building_B07_BlacksmithForge";
    const string SewingBuildingResource = "Buildings/Building_B08_SewingTable";
    const string PlankRecipeResource = "Recipes/Recipe_Plank";
    const string MinerProfileResource = "NPCs/Profile_Miner";
    const string TailorProfileResource = "NPCs/Profile_Tailor";
    const string ResourceActivityPrefix = "world-resource:";
    const float CustomerTimeoutSeconds = 24f;
    public const string SalesDisplayDefinitionId = "world.b01.sales-display";
    public const string SalesDisplayInstanceId = "world-b01-sales-display-001";

    WorldGridService _grid;
    WorldPersistenceService _persistence;
    WorldNavigationService _navigation;
    WorldGenerationResult _generated;
    GameObject _runtimeRoot;
    GameObject _playerRoot;
    GameObject _shopRoot;
    GameObject _workbenchRoot;
    GameObject _kitchenRoot;
    GameObject _forgeRoot;
    GameObject _sewingRoot;
    GameObject _customerRoot;
    Inventory _inventory;
    Shop _shop;
    ShopSlot[] _shopSlots = Array.Empty<ShopSlot>();
    Workbench _workbench;
    Workbench _kitchen;
    Workbench _forge;
    Workbench _sewing;
    EconomyService _economy;
    GameClock _clock;
    DayNightShopLoopController _dayLoop;
    SaveManager _saveManager;
    NpcController _customer;
    Coroutine _customerVisitRoutine;
    ShopSlot _customerTargetSlot;
    PlayerInteraction _playerInteraction;
    Hotbar _playerHotbar;
    DaytimeStockPrepPoint[] _daytimeActivityPoints = Array.Empty<DaytimeStockPrepPoint>();
    FarmPlotInteraction[] _farmPlots = Array.Empty<FarmPlotInteraction>();
    WorldSalesDisplayReadability _salesDisplayReadability;
    Transform _salesDisplayRoot;
    Transform _shopSignTarget;
    GameObject _residentAnchorRoot;
    readonly List<Transform> _residentSpawnAnchors = new List<Transform>();
    HiringService _hiringService;
    bool _residentAnchorsSettled;
    int _residentAnchorNavigationRevision = -1;
    Vector3 _salesDisplayBaseLocalPosition;
    Quaternion _salesDisplayBaseLocalRotation;
    int _salesDisplayGridX;
    int _salesDisplayGridY;
    int _salesDisplayQuarterTurns;
    int _moneyBeforeCustomer;
    int _purchasesBeforeCustomer;
    int _rejectionsBeforeCustomer;
    int _customerVisitSequence;
    float _customerStartedAt;
    long _boundSeed = long.MinValue;
    string _lastAction = "World gameplay adapter is bootstrapping.";
    string _lastFailure = string.Empty;
    string _lastCustomerOutcome = "밤 영업을 열면 실제 주민 성향과 구매 반응을 확인할 수 있습니다.";

    public static WorldGameplayAdapterService Instance { get; private set; }
    public WorldGameplayAdapterState State { get; private set; } =
        WorldGameplayAdapterState.Bootstrapping;
    public bool IsReady => State == WorldGameplayAdapterState.Ready ||
                           State == WorldGameplayAdapterState.CustomerMoving ||
                           State == WorldGameplayAdapterState.CustomerPurchased ||
                           State == WorldGameplayAdapterState.CustomerDeclined;
    public string LastAction => _lastAction;
    public string LastFailure => _lastFailure;
    public long BoundSeed => _boundSeed;
    public GameObject RuntimeRoot => _runtimeRoot;
    public GameObject PlayerRoot => _playerRoot;
    public Inventory PlayerInventory => _inventory;
    public Shop RuntimeShop => _shop;
    public IReadOnlyList<ShopSlot> RuntimeShopSlots => _shopSlots;
    public Workbench RuntimeWorkbench => _workbench;
    public Workbench RuntimeKitchen => _kitchen;
    public Workbench RuntimeForge => _forge;
    public Workbench RuntimeSewing => _sewing;
    public IReadOnlyList<Transform> ResidentSpawnAnchors => _residentSpawnAnchors;
    public bool ResidentSpawnAnchorsReady => _residentAnchorsSettled &&
                                              _residentSpawnAnchors.Count > 0 &&
                                              _navigation != null &&
                                              !_navigation.IsRebuilding &&
                                              _residentAnchorNavigationRevision == _navigation.NavigationRevision &&
                                              _residentSpawnAnchors.All(IsResidentAnchorOnNavMesh);
    public NpcController RuntimeCustomer => _customer;
    public SaveManager RuntimeSaveManager => _saveManager;
    public PlayerInteraction PlayerInteraction => _playerInteraction;
    public Hotbar PlayerHotbar => _playerHotbar;
    public IReadOnlyList<DaytimeStockPrepPoint> DaytimeActivityPoints => _daytimeActivityPoints;
    public IReadOnlyList<FarmPlotInteraction> FarmPlots => _farmPlots;
    public WorldSalesDisplayReadability SalesDisplayReadability => _salesDisplayReadability;
    public Transform SalesDisplayTarget => _salesDisplayRoot;
    public Transform ShopSignTarget => _shopSignTarget;
    public bool DaytimeActivitiesBound => _playerInteraction != null && _playerHotbar != null &&
                                          _daytimeActivityPoints.Length >= 4 &&
                                          _farmPlots.Length >= FarmPlotInteraction.RuntimePlotCount;
    public bool ProductionFacilitiesBound =>
        _workbench != null && _workbench.workbenchType == WorkbenchType.BasicWorkbench &&
        _kitchen != null && _kitchen.workbenchType == WorkbenchType.Kitchen &&
        _forge != null && _forge.workbenchType == WorkbenchType.Forge &&
        _sewing != null && _sewing.workbenchType == WorkbenchType.SewingTable;
    public Vector2Int SalesDisplayGrid => new Vector2Int(
        _salesDisplayGridX, _salesDisplayGridY);
    public int SalesDisplayQuarterTurns => _salesDisplayQuarterTurns;
    public bool CustomerPurchaseCompleted => State == WorldGameplayAdapterState.CustomerPurchased;
    public bool CustomerDecisionCompleted => CustomerPurchaseCompleted ||
                                             State == WorldGameplayAdapterState.CustomerDeclined;
    public bool CustomerDeclined => State == WorldGameplayAdapterState.CustomerDeclined;
    public string CustomerStrategySummary
    {
        get
        {
            if (State == WorldGameplayAdapterState.CustomerMoving && _customer?.profile != null)
            {
                return $"방문 중 · {_customer.profile.npcName} · " +
                       $"{CustomerPreferencePresentationController.DescribePreference(_customer.profile)} · " +
                       "상품 종류와 가격을 판단하고 있습니다.";
            }

            return _lastCustomerOutcome;
        }
    }
    public int ConsumedResourceCount => CaptureWorldState()?.resourceStates?
        .Count(state => state != null && state.consumed) ?? 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapWorldSandbox()
    {
        if (SceneManager.GetActiveScene().name != "WorldSandbox" ||
            FindFirstObjectByType<WorldGameplayAdapterService>() != null)
        {
            return;
        }

        WorldGridService grid = FindFirstObjectByType<WorldGridService>();
        if (grid != null) grid.gameObject.AddComponent<WorldGameplayAdapterService>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    IEnumerator Start()
    {
        yield return null;
        yield return null;
        if (!InitializeRuntime(out string reason))
        {
            Fail(reason);
            yield break;
        }

        // Runtime building obstacles become active during InitializeRuntime. Give
        // carving and any queued navigation rebuild a chance to settle before the
        // resident spawn contract samples the generated Start neighbourhood.
        int navigationWaitFrames = 0;
        while (_navigation != null && _navigation.IsRebuilding && navigationWaitFrames++ < 120)
            yield return null;
        yield return null;
        yield return null;
        if (_navigation != null && _navigation.IsRebuilding)
        {
            Fail("Resident spawn anchors could not wait for the generated NavMesh rebuild.");
            yield break;
        }
        if (!ConfigureResidentWorldBindings(out reason))
        {
            Fail(reason);
            yield break;
        }

        State = WorldGameplayAdapterState.Ready;
        VillageCultureVisualController.Instance?.RefreshNow();
        _lastAction = "Generated resources, B05/B06/B07/B08 production, B01 sales, residents, economy and save are connected.";
        Debug.Log("[BETA-007] RESIDENT_WORLD_READY roles=8 anchors=generated facilities=B05+B06+B07+B08");
        Debug.Log("[BETA-003] PRODUCTION_READY authorities=existing basic=B05 kitchen=B06 forge=B07");
        Debug.Log("[WORLD-009] RUNTIME_READY authorities=existing seed=9009 shop=B01 workbench=B05");
    }

    void Update()
    {
        if (!IsReady) return;

        if (!ResidentSpawnAnchorsReady)
        {
            _residentAnchorsSettled = false;
            if (_residentSpawnAnchors.Count == 0 ||
                (_navigation != null &&
                 _residentAnchorNavigationRevision != _navigation.NavigationRevision))
            {
                ConfigureResidentWorldBindings(out _);
            }
            else
            {
                SettleResidentSpawnAnchors();
                SynchronizeHiringSpawnAnchors();
            }
        }

        if (_persistence != null && _persistence.IsProceduralActive &&
            _persistence.ActiveSeed != _boundSeed)
        {
            BindRuntimeObjectsToSeed(_persistence.ActiveSeed);
        }

        if (State != WorldGameplayAdapterState.CustomerMoving) return;
        if (_customerTargetSlot != null && _customerTargetSlot.IsEmpty &&
            _economy != null && _economy.Money > _moneyBeforeCustomer)
        {
            State = WorldGameplayAdapterState.CustomerPurchased;
            string name = ResolveCustomerName();
            _lastCustomerOutcome = $"구매 · {name} · +{_economy.Money - _moneyBeforeCustomer}G · " +
                                   "손님 반응과 수요 신호를 다음 진열에 활용하세요.";
            _lastAction = _lastCustomerOutcome;
            return;
        }


        SalesLogManager.DailyDecisionStats decisions = CurrentCustomerDayStats();
        if (decisions.rejections > _rejectionsBeforeCustomer &&
            decisions.purchases == _purchasesBeforeCustomer)
        {
            string name = ResolveCustomerName();
            State = WorldGameplayAdapterState.CustomerDeclined;
            _lastCustomerOutcome = $"보류 · {name} · 상품은 유지됨 · " +
                                   "가격을 낮추거나 다른 종류의 상품을 준비해 보세요.";
            _lastAction = _lastCustomerOutcome;
            return;
        }

        if (Time.realtimeSinceStartup - _customerStartedAt >= CustomerTimeoutSeconds)
            Fail("Existing NpcController did not complete the shop visit within the bounded timeout.");
    }

    void OnDestroy()
    {
        if (_hiringService != null)
            _hiringService.OnHired -= OnResidentHired;
        if (Instance == this) Instance = null;
    }

    bool InitializeRuntime(out string reason)
    {
        reason = string.Empty;
        if (SceneManager.GetActiveScene().name != "WorldSandbox")
        {
            reason = "The gameplay adapter is restricted to WorldSandbox.";
            return false;
        }

        _grid = GetComponent<WorldGridService>() ?? FindFirstObjectByType<WorldGridService>();
        if (_grid == null)
        {
            reason = "WorldGridService is unavailable.";
            return false;
        }

        _persistence = GetComponent<WorldPersistenceService>();
        if (_persistence == null) _persistence = gameObject.AddComponent<WorldPersistenceService>();
        _navigation = GetComponent<WorldNavigationService>();
        if (_navigation == null) _navigation = gameObject.AddComponent<WorldNavigationService>();

        if (!_persistence.StartProceduralWorld(DefaultWorldSeed, out reason)) return false;
        _generated = WorldIslandGenerator.Generate(DefaultWorldSeed);
        if (!_navigation.BuildAllNow(out reason)) return false;
        if (!BuildGameplayRuntime(out reason)) return false;
        _boundSeed = DefaultWorldSeed;
        return true;
    }

    bool BuildGameplayRuntime(out string reason)
    {
        reason = string.Empty;
        ReleaseRuntimeObject(_runtimeRoot);
        _residentAnchorRoot = null;
        _residentSpawnAnchors.Clear();
        _residentAnchorsSettled = false;
        _residentAnchorNavigationRevision = -1;
        _runtimeRoot = new GameObject(RuntimeRootName)
        {
            hideFlags = HideFlags.DontSave
        };
        _runtimeRoot.SetActive(false);

        // PA_RuntimeSceneBinder is the existing authority composition root for every
        // loaded scene. WORLD-009 adopts its instances instead of creating a second
        // manager stack under the generated world runtime root.
        _economy = EconomyService.Instance ?? FindFirstObjectByType<EconomyService>();
        _clock = GameClock.Instance ?? FindFirstObjectByType<GameClock>();
        _dayLoop = DayNightShopLoopController.Instance ??
                   FindFirstObjectByType<DayNightShopLoopController>();
        _saveManager = SaveManager.instance ?? FindFirstObjectByType<SaveManager>();
        ItemRegistry registry = ItemRegistry.Instance ?? FindFirstObjectByType<ItemRegistry>();
        SalesLogManager sales = SalesLogManager.Instance ?? FindFirstObjectByType<SalesLogManager>();
        if (_economy == null || _clock == null || _dayLoop == null || _saveManager == null ||
            registry == null || sales == null)
        {
            reason = "PA_RuntimeSceneBinder did not provide the existing gameplay authorities.";
            return false;
        }
        // Keep the clock deterministic while runtime anchors are composed. The
        // player-facing controller starts the real week clock after New Game.
        _clock.secondsPerGameHour = BootstrapHoldSecondsPerGameHour;
        _dayLoop.keepDay1TutorialShopOpen = false;

        _playerRoot = new GameObject(PlayerRootName);
        _playerRoot.hideFlags = HideFlags.DontSave;
        _playerRoot.tag = "Player";
        _playerRoot.transform.SetParent(_runtimeRoot.transform, false);
        _inventory = _playerRoot.AddComponent<Inventory>();
        _inventory.size = 24;

        if (!TryInstantiateBuilding(MarketBuildingResource, "WORLD009_MarketStall_Runtime",
                WorldGenerationAnchorKind.Shop, true, out _shopRoot, out reason))
        {
            return false;
        }
        if (!TryInstantiateBuilding(WorkbenchBuildingResource, "WORLD009_Workbench_Runtime",
                WorldGenerationAnchorKind.MeadowActivity, false, out _workbenchRoot, out reason))
        {
            return false;
        }
        if (!TryInstantiateBuilding(KitchenBuildingResource, "BETA003_Kitchen_Runtime",
                WorldGenerationAnchorKind.MeadowActivity, false, out _kitchenRoot, out reason))
        {
            return false;
        }
        if (!TryInstantiateBuilding(ForgeBuildingResource, "BETA003_Forge_Runtime",
                WorldGenerationAnchorKind.HighlandActivity, false, out _forgeRoot, out reason))
        {
            return false;
        }
        if (!TryInstantiateBuilding(SewingBuildingResource, "BETA007_Sewing_Runtime",
                WorldGenerationAnchorKind.MeadowActivity, false, out _sewingRoot, out reason))
        {
            return false;
        }

        if (!_generated.TryGetAnchor(WorldGenerationAnchorKind.Start, out WorldGenerationAnchor start) ||
            !_grid.CellToWorld(start.Coordinate, out Vector3 playerPosition))
        {
            reason = "Generated player start anchor is unavailable.";
            return false;
        }
        _playerRoot.transform.position = playerPosition + Vector3.up * 0.05f;

        _runtimeRoot.SetActive(true);
        _inventory = Inventory.instance ?? _inventory;
        _economy = EconomyService.Instance ?? _economy;
        _clock = GameClock.Instance ?? _clock;
        _dayLoop = DayNightShopLoopController.Instance ?? _dayLoop;
        _saveManager = SaveManager.instance ?? _saveManager;
        _shop = _shopRoot != null ? _shopRoot.GetComponentInChildren<Shop>(true) : null;
        _workbench = _workbenchRoot != null
            ? _workbenchRoot.GetComponentInChildren<Workbench>(true)
            : null;
        _kitchen = _kitchenRoot != null
            ? _kitchenRoot.GetComponentInChildren<Workbench>(true)
            : null;
        _forge = _forgeRoot != null
            ? _forgeRoot.GetComponentInChildren<Workbench>(true)
            : null;
        _sewing = _sewingRoot != null
            ? _sewingRoot.GetComponentInChildren<Workbench>(true)
            : null;
        _shopSlots = _shopRoot != null
            ? _shopRoot.GetComponentsInChildren<ShopSlot>(true)
                .OrderBy(slot => slot.name, StringComparer.Ordinal).ToArray()
            : Array.Empty<ShopSlot>();
        ResolveSalesDisplayRoot();

        if (_inventory == null || _economy == null || _clock == null || _dayLoop == null ||
            _saveManager == null || ItemRegistry.Instance == null || _shop == null ||
            _shopSlots.Length == 0 || !ProductionFacilitiesBound)
        {
            reason = "One or more existing gameplay authorities failed to activate.";
            return false;
        }

        _salesDisplayReadability = _shopRoot.GetComponent<WorldSalesDisplayReadability>() ??
                                   _shopRoot.AddComponent<WorldSalesDisplayReadability>();
        if (!_salesDisplayReadability.Configure(_salesDisplayRoot, _shopSlots, out reason))
            return false;

        if (!PositionProductionFacilities(out reason) || !BindShopSignToRuntimeShop(out reason))
            return false;

        _clock.ForceSet(9f, 1, "BETA-001 WorldSandbox fresh session");
        _dayLoop.SimulatePhaseForValidation(9f, 1);
        AddRuntimeLabel(_playerRoot.transform, "새 생활 시작점", new Color(0.76f, 0.95f, 1f));
        AddRuntimeLabel(_shopRoot.transform, "P.A. 잡화점 · 밤 영업", new Color(1f, 0.88f, 0.48f));
        AddRuntimeLabel(_workbenchRoot.transform, "제작 작업대 · 상품 준비", new Color(0.72f, 1f, 0.72f));
        AddRuntimeLabel(_kitchenRoot.transform, "주방 가공대 · 식재료 요리", new Color(1f, 0.72f, 0.46f));
        AddRuntimeLabel(_forgeRoot.transform, "대장간 용광로 · 광석 가공", new Color(1f, 0.48f, 0.34f));
        AddRuntimeLabel(_sewingRoot.transform, "재봉 작업대 · 생활 공예", new Color(0.95f, 0.64f, 0.92f));
        if (!ConfigurePlayerActivityInteraction(out reason) ||
            !BindDaytimeActivitiesToGeneratedWorld(out reason))
        {
            return false;
        }
        return true;
    }

    public bool BeginPlayableWeek(out string reason)
    {
        reason = string.Empty;
        if (!IsReady || _clock == null || _dayLoop == null)
        {
            reason = "WorldSandbox clock authorities are not ready.";
            return false;
        }

        _clock.secondsPerGameHour = PlayableSecondsPerGameHour;
        _dayLoop.keepDay1TutorialShopOpen = false;
        if (!BindShopSignToRuntimeShop(out reason)) return false;

        _lastAction = $"Playable week clock started at {_clock.GetTimeString()} " +
                      $"({PlayableSecondsPerGameHour:0} seconds per game hour).";
        return true;
    }

    bool BindShopSignToRuntimeShop(out string reason)
    {
        reason = string.Empty;
        if (_shopRoot == null)
        {
            reason = "The runtime B01 shop is unavailable for sign binding.";
            return false;
        }

        ShopOpenSign sign = FindFirstObjectByType<ShopOpenSign>();
        if (sign == null)
        {
            reason = "The existing shop-open sign was not created by the day/night authority.";
            return false;
        }

        Vector3 desired = _shopRoot.transform.position +
                          _shopRoot.transform.forward * 2.0f +
                          _shopRoot.transform.right * 1.5f;
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            desired = hit.position;
        else
        {
            Vector3 rayStart = desired + Vector3.up * 8f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit ground, 24f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                desired = ground.point;
        }

        sign.transform.SetPositionAndRotation(desired + Vector3.up * 0.45f,
            Quaternion.LookRotation(-_shopRoot.transform.forward, Vector3.up));
        _shopSignTarget = sign.transform;
        return true;
    }

    bool PositionProductionFacilities(out string reason)
    {
        reason = string.Empty;
        if (!TryPositionExistingRuntimeObjectAtOffset(_kitchenRoot,
                WorldGenerationAnchorKind.MeadowActivity, new Vector2Int(6, -2)))
        {
            reason = "The generated meadow could not provide a safe B06 Kitchen cell.";
            return false;
        }
        if (!TryPositionExistingRuntimeObjectAtOffset(_forgeRoot,
                WorldGenerationAnchorKind.HighlandActivity, new Vector2Int(4, 3)))
        {
            reason = "The generated highland could not provide a safe B07 Forge cell.";
            return false;
        }
        if (!TryPositionExistingRuntimeObjectAtOffset(_sewingRoot,
                WorldGenerationAnchorKind.MeadowActivity, new Vector2Int(-6, -2)))
        {
            reason = "The generated meadow could not provide a safe B08 Sewing cell.";
            return false;
        }
        return true;
    }

    void ResolveSalesDisplayRoot()
    {
        _salesDisplayRoot = null;
        if (_shopSlots.Length == 0) return;
        Transform candidate = _shopSlots[0] != null ? _shopSlots[0].transform.parent : null;
        if (candidate == null || _shopSlots.Any(slot =>
                slot == null || slot.transform.parent != candidate))
        {
            return;
        }

        _salesDisplayRoot = candidate;
        _salesDisplayBaseLocalPosition = candidate.localPosition;
        _salesDisplayBaseLocalRotation = candidate.localRotation;
        _salesDisplayGridX = 0;
        _salesDisplayGridY = 0;
        _salesDisplayQuarterTurns = 0;
    }

    bool TryInstantiateBuilding(
        string resourcePath,
        string runtimeName,
        WorldGenerationAnchorKind anchorKind,
        bool faceEntrance,
        out GameObject instance,
        out string reason)
    {
        instance = null;
        reason = string.Empty;
        BuildingData data = Resources.Load<BuildingData>(resourcePath);
        if (data == null || data.prefab == null)
        {
            reason = $"Existing BuildingData or prefab is missing: {resourcePath}.";
            return false;
        }
        if (!_generated.TryGetAnchor(anchorKind, out WorldGenerationAnchor anchor) ||
            !_grid.CellToWorld(anchor.Coordinate, out Vector3 worldPosition))
        {
            reason = $"Generated anchor is missing: {anchorKind}.";
            return false;
        }

        instance = Instantiate(data.prefab, _runtimeRoot.transform);
        instance.name = runtimeName;
        instance.hideFlags = HideFlags.DontSave;
        Vector3 lookDirection = faceEntrance
            ? new Vector3(anchor.EntranceCoordinate.x - anchor.Coordinate.x, 0f,
                anchor.EntranceCoordinate.y - anchor.Coordinate.y)
            : ResolveLookDirection(anchor.Coordinate, WorldGenerationAnchorKind.Start);
        if (lookDirection.sqrMagnitude > 0.01f)
            instance.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        AlignObjectToGround(instance, worldPosition);
        return true;
    }

    Vector3 ResolveLookDirection(Vector2Int from, WorldGenerationAnchorKind towardKind)
    {
        if (!_generated.TryGetAnchor(towardKind, out WorldGenerationAnchor target))
            return Vector3.forward;
        return new Vector3(target.Coordinate.x - from.x, 0f, target.Coordinate.y - from.y);
    }

    static void AlignObjectToGround(GameObject root, Vector3 groundPosition)
    {
        root.transform.position = groundPosition;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        float minimumY = renderers.Min(renderer => renderer.bounds.min.y);
        root.transform.position += Vector3.up * (groundPosition.y - minimumY);
    }

    static void AddRuntimeLabel(Transform parent, string text, Color color)
    {
        if (parent == null) return;
        var labelObject = new GameObject("WORLD009_Label");
        labelObject.hideFlags = HideFlags.DontSave;
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        var label = labelObject.AddComponent<PrototypeWorldLabel>();
        label.Set(text, color, 1.35f);
    }

    bool ConfigurePlayerActivityInteraction(out string reason)
    {
        reason = string.Empty;
        if (_playerRoot == null || _inventory == null)
        {
            reason = "The WorldSandbox player or Inventory is unavailable for daytime activity input.";
            return false;
        }

        _playerHotbar = _playerRoot.GetComponent<Hotbar>() ?? _playerRoot.AddComponent<Hotbar>();
        _inventory.hotbar = _playerHotbar;
        _playerInteraction = _playerRoot.GetComponent<PlayerInteraction>() ??
                             _playerRoot.AddComponent<PlayerInteraction>();
        if (InventoryUI.instance != null)
        {
            InventoryUI.instance.inventory = _inventory;
            InventoryUI.instance.hotbar = _playerHotbar;
        }

        return _playerInteraction != null && _playerHotbar != null;
    }

    bool BindDaytimeActivitiesToGeneratedWorld(out string reason)
    {
        reason = string.Empty;
        _daytimeActivityPoints = FindObjectsByType<DaytimeStockPrepPoint>(
                FindObjectsSortMode.None)
            .Where(point => point != null)
            .OrderBy(point => point.activityId, StringComparer.Ordinal)
            .ToArray();
        _farmPlots = FindObjectsByType<FarmPlotInteraction>(FindObjectsSortMode.None)
            .Where(plot => plot != null)
            .OrderBy(plot => plot.plotId, StringComparer.Ordinal)
            .ToArray();

        string[] requiredActivities =
        {
            "forest-forage", "farm-seed-pouch", "quarry-mining", "shore-forage"
        };
        if (requiredActivities.Any(id => FindDaytimeActivity(id) == null) ||
            _farmPlots.Length < FarmPlotInteraction.RuntimePlotCount)
        {
            reason = "Existing Gathering, Farming, Mining or Fishing runtime activities are missing.";
            return false;
        }

        if (!TryResolveAnchorCoordinate(WorldGenerationAnchorKind.Shop, new Vector2Int(-3, 3),
                out Vector2Int garden) ||
            !TryResolveAnchorCoordinate(WorldGenerationAnchorKind.Shop, new Vector2Int(4, 3),
                out Vector2Int producer) ||
            !TryResolveResourceCoordinate(WorldResourceKind.Forage,
                WorldGenerationAnchorKind.ForestActivity, out Vector2Int forest) ||
            !TryResolveResourceCoordinate(WorldResourceKind.Fish,
                WorldGenerationAnchorKind.PondActivity, out Vector2Int fishing) ||
            !TryResolveResourceCoordinate(WorldResourceKind.Stone,
                WorldGenerationAnchorKind.HighlandActivity, out Vector2Int quarry) ||
            !TryResolveAnchorCoordinate(WorldGenerationAnchorKind.MeadowActivity,
                new Vector2Int(3, 3), out Vector2Int meadow) ||
            !TryResolveAnchorCoordinate(WorldGenerationAnchorKind.MeadowActivity,
                new Vector2Int(-3, 3), out Vector2Int seedPouch) ||
            !TryResolveAnchorCoordinate(WorldGenerationAnchorKind.MeadowActivity,
                new Vector2Int(-4, 1), out Vector2Int farmA) ||
            !TryResolveAnchorCoordinate(WorldGenerationAnchorKind.MeadowActivity,
                new Vector2Int(-1, 4), out Vector2Int farmB))
        {
            reason = "Generated world anchors could not provide safe daytime activity cells.";
            return false;
        }

        RelocateActivity("garden-basket", garden, "상점 앞 준비 바구니");
        RelocateActivity("producer-dropbox", producer, "생산자 납품함");
        RelocateActivity("forest-forage", forest, "숲 채집터 · 당근 x2");
        RelocateActivity("shore-forage", fishing, "연못 낚시터 · 물고기 x2");
        RelocateActivity("meadow-forage", meadow, "초원 채집터 · 밀 x2");
        RelocateActivity("quarry-mining", quarry, "고지대 광맥 · 광석 x2");
        RelocateActivity("farm-seed-pouch", seedPouch, "농장 씨앗 주머니 · 씨앗 x2");
        RelocateFarmPlot(_farmPlots[0], farmA);
        RelocateFarmPlot(_farmPlots[1], farmB);

        _lastAction = "숲 채집, 농사, 광질과 낚시가 생성 섬의 실제 활동 구역에 연결됐습니다.";
        Debug.Log("[BETA-002] DAYTIME_READY gathering=true farming=true mining=true fishing=true");
        return true;
    }

    public DaytimeStockPrepPoint FindDaytimeActivity(string activityId)
    {
        if (string.IsNullOrWhiteSpace(activityId)) return null;
        return _daytimeActivityPoints.FirstOrDefault(point => point != null &&
            string.Equals(point.activityId, activityId, StringComparison.Ordinal));
    }

    public Transform GetVillageCultureAnchor(ItemCategory category, string itemName = "")
    {
        if (TryResolveVillageResponseSpecialty(itemName, out NpcSpecialty specialty))
        {
            Transform roleAnchor = GetResidentRoleAnchor(specialty);
            if (roleAnchor != null)
                return roleAnchor;
        }

        return category switch
        {
            ItemCategory.Raw => GetResidentRoleAnchor(NpcSpecialty.Farmer),
            ItemCategory.Processed => _workbench != null ? _workbench.transform : null,
            ItemCategory.Utility => _forge != null ? _forge.transform : null,
            ItemCategory.Luxury => _sewing != null ? _sewing.transform : null,
            _ => _shop != null ? _shop.transform : null
        };
    }

    public bool TryResolveVillageResponseSpecialty(string itemName, out NpcSpecialty specialty)
    {
        specialty = NpcSpecialty.None;
        if (string.IsNullOrWhiteSpace(itemName))
            return false;

        foreach (NpcCandidateData candidate in Resources.LoadAll<NpcCandidateData>("Candidates")
                     .Where(candidate => candidate != null && candidate.spawnPrefab != null)
                     .OrderBy(candidate => candidate.specialty))
        {
            ProducerNpcController producer =
                candidate.spawnPrefab.GetComponentInChildren<ProducerNpcController>(true);
            if (producer?.productionData?.producedItem != null &&
                string.Equals(producer.productionData.producedItem.itemName, itemName,
                    StringComparison.OrdinalIgnoreCase))
            {
                specialty = candidate.specialty;
                return true;
            }

            SpecialistNpcController specialist =
                candidate.spawnPrefab.GetComponentInChildren<SpecialistNpcController>(true);
            if (specialist?.assignedRecipes == null)
                continue;
            if (specialist.assignedRecipes.Any(recipe => recipe?.outputItem != null &&
                    string.Equals(recipe.outputItem.itemName, itemName,
                        StringComparison.OrdinalIgnoreCase)))
            {
                specialty = candidate.specialty;
                return true;
            }
        }

        return false;
    }

    public Transform GetResidentRoleAnchor(NpcSpecialty specialty)
    {
        return specialty switch
        {
            NpcSpecialty.Farmer => _farmPlots.FirstOrDefault(plot => plot != null)?.transform,
            NpcSpecialty.Lumberjack => FindDaytimeActivity("forest-forage")?.transform,
            NpcSpecialty.Miner => FindDaytimeActivity("quarry-mining")?.transform,
            NpcSpecialty.Fisher => FindDaytimeActivity("shore-forage")?.transform,
            NpcSpecialty.Chef => _kitchen != null ? _kitchen.transform : null,
            NpcSpecialty.Blacksmith => _forge != null ? _forge.transform : null,
            NpcSpecialty.Tailor => _sewing != null ? _sewing.transform : null,
            NpcSpecialty.Carpenter => _workbench != null ? _workbench.transform : null,
            _ => null
        };
    }

    bool ConfigureResidentWorldBindings(out string reason)
    {
        reason = string.Empty;
        _hiringService = HiringService.Instance ?? FindFirstObjectByType<HiringService>();
        if (_hiringService == null)
        {
            reason = "The existing HiringService is unavailable for resident world binding.";
            return false;
        }

        if (!CreateResidentSpawnAnchors(out reason))
            return false;

        SettleResidentSpawnAnchors();
        if (!ResidentSpawnAnchorsReady)
        {
            reason = "Generated Start neighbourhood did not retain a valid resident NavMesh spawn.";
            return false;
        }

        _hiringService.OnHired -= OnResidentHired;
        _hiringService.OnHired += OnResidentHired;
        SynchronizeHiringSpawnAnchors();

        foreach (HiringService.HiredNpcRuntimeRecord record in _hiringService.GetHiredRuntimeRecords())
            ConfigureHiredResident(record.Candidate, record.Instance);

        return _residentSpawnAnchors.Count > 0;
    }

    bool CreateResidentSpawnAnchors(out string reason)
    {
        reason = string.Empty;
        _residentSpawnAnchors.Clear();
        _residentAnchorsSettled = false;
        if (_residentAnchorRoot != null)
            ReleaseRuntimeObject(_residentAnchorRoot);

        _residentAnchorRoot = new GameObject("BETA007_ResidentSpawnAnchors")
        {
            hideFlags = HideFlags.DontSave
        };
        _residentAnchorRoot.transform.SetParent(_runtimeRoot.transform, false);

        Vector2Int[] offsets =
        {
            Vector2Int.zero,
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(3, 2), new Vector2Int(-3, 2),
            new Vector2Int(3, -2), new Vector2Int(-3, -2)
        };
        for (int i = 0; i < offsets.Length; i++)
        {
            if (!TryResolveAnchorCoordinate(WorldGenerationAnchorKind.Start, offsets[i],
                    out Vector2Int coordinate) || !_grid.CellToWorld(coordinate, out Vector3 world) ||
                !NavMesh.SamplePosition(world + Vector3.up * 0.25f, out NavMeshHit hit, 2.5f,
                    NavMesh.AllAreas))
                continue;

            if (_residentSpawnAnchors.Any(anchor => anchor != null &&
                    FlatDistance(anchor.position, hit.position) < 0.75f))
                continue;

            var anchorObject = new GameObject($"ResidentSpawn_{_residentSpawnAnchors.Count + 1}")
            {
                hideFlags = HideFlags.DontSave
            };
            anchorObject.transform.SetParent(_residentAnchorRoot.transform, false);
            anchorObject.transform.position = hit.position;
            Vector3 towardShop = _shop != null
                ? _shop.transform.position - hit.position
                : Vector3.forward;
            towardShop.y = 0f;
            if (towardShop.sqrMagnitude > 0.01f)
                anchorObject.transform.rotation = Quaternion.LookRotation(towardShop.normalized, Vector3.up);
            _residentSpawnAnchors.Add(anchorObject.transform);
        }

        if (_residentSpawnAnchors.Count == 0)
        {
            reason = "Generated Start anchor did not provide a resident NavMesh spawn.";
            return false;
        }
        return true;
    }

    void SettleResidentSpawnAnchors()
    {
        if (_residentSpawnAnchors.Count == 0 || _navigation == null || _navigation.IsRebuilding)
            return;

        float settleRadius = _grid != null
            ? Mathf.Max(3f, _grid.Definition.CellSize * 3f)
            : 3f;
        for (int i = _residentSpawnAnchors.Count - 1; i >= 0; i--)
        {
            Transform anchor = _residentSpawnAnchors[i];
            if (anchor == null || !NavMesh.SamplePosition(anchor.position, out NavMeshHit hit,
                    settleRadius, NavMesh.AllAreas))
            {
                if (anchor != null)
                    ReleaseRuntimeObject(anchor.gameObject);
                _residentSpawnAnchors.RemoveAt(i);
                continue;
            }
            anchor.position = hit.position;
        }

        _residentAnchorNavigationRevision = _navigation.NavigationRevision;
        _residentAnchorsSettled = _residentSpawnAnchors.Count > 0 &&
                                  _residentSpawnAnchors.All(IsResidentAnchorOnNavMesh);
    }

    void SynchronizeHiringSpawnAnchors()
    {
        if (_hiringService == null)
            return;

        _hiringService.spawnPoint = null;
        _hiringService.spawnPointRotation ??= new List<Transform>();
        _hiringService.spawnPointRotation.Clear();
        _hiringService.spawnPointRotation.AddRange(
            _residentSpawnAnchors.Where(anchor => anchor != null && IsResidentAnchorOnNavMesh(anchor)));
    }

    static bool IsResidentAnchorOnNavMesh(Transform anchor)
    {
        if (anchor == null || !NavMesh.SamplePosition(anchor.position, out NavMeshHit hit,
                0.75f, NavMesh.AllAreas))
            return false;
        return FlatDistance(anchor.position, hit.position) <= 0.75f;
    }

    void OnResidentHired(NpcCandidateData candidate, GameObject resident)
    {
        ConfigureHiredResident(candidate, resident);
    }

    void ConfigureHiredResident(NpcCandidateData candidate, GameObject resident)
    {
        if (candidate == null || resident == null)
            return;

        Transform roleAnchor = GetResidentRoleAnchor(candidate.specialty);
        Transform home = ClosestResidentSpawn(resident.transform.position);
        NavMeshAgent agent = resident.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isActiveAndEnabled && !agent.isOnNavMesh && home != null &&
            NavMesh.SamplePosition(home.position, out NavMeshHit hit, 2f, agent.areaMask))
            agent.Warp(hit.position);

        ProducerNpcController producer = resident.GetComponent<ProducerNpcController>();
        if (producer != null)
        {
            producer.workSpot = roleAnchor;
            DaytimeStockPrepPoint dropOff = FindDaytimeActivity("producer-dropbox");
            producer.dropOffPoint = dropOff != null
                ? dropOff.transform
                : _shop != null ? _shop.transform : null;
        }

        SpecialistNpcController specialist = resident.GetComponent<SpecialistNpcController>();
        if (specialist != null)
        {
            WorkbenchType expected = NpcSpecialtyMapping.GetWorkbenchType(candidate.specialty);
            specialist.targetWorkbench = expected switch
            {
                WorkbenchType.BasicWorkbench => _workbench,
                WorkbenchType.Kitchen => _kitchen,
                WorkbenchType.Forge => _forge,
                WorkbenchType.SewingTable => _sewing,
                _ => null
            };
        }

        NpcScheduleController schedule = resident.GetComponent<NpcScheduleController>();
        if (schedule != null)
            schedule.homePoint = home;

        Debug.Log($"[BETA-007] RESIDENT_BOUND name={candidate.ResolveDisplayName()} " +
                  $"role={candidate.specialty} home={home?.name ?? "none"} " +
                  $"work={roleAnchor?.name ?? "none"} navmesh={agent != null && agent.isOnNavMesh}");
    }

    Transform ClosestResidentSpawn(Vector3 position)
    {
        return _residentSpawnAnchors.Where(anchor => anchor != null)
            .OrderBy(anchor => FlatDistance(anchor.position, position))
            .FirstOrDefault();
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    bool TryResolveResourceCoordinate(WorldResourceKind kind,
        WorldGenerationAnchorKind nearbyAnchor, out Vector2Int coordinate)
    {
        coordinate = default;
        if (_generated == null || !_generated.TryGetAnchor(nearbyAnchor,
                out WorldGenerationAnchor anchor))
        {
            return false;
        }

        WorldResourceSpawnRecord spawn = _generated.ResourceSpawns
            .Where(record => record.Kind == kind)
            .OrderBy(record => (record.Coordinate - anchor.Coordinate).sqrMagnitude)
            .ThenBy(record => record.SpawnKey, StringComparer.Ordinal)
            .FirstOrDefault();
        Vector2Int desired = string.IsNullOrWhiteSpace(spawn.SpawnKey)
            ? anchor.Coordinate
            : spawn.Coordinate;
        return TryFindNearestWalkableCell(desired, out coordinate);
    }

    bool TryResolveAnchorCoordinate(WorldGenerationAnchorKind kind, Vector2Int offset,
        out Vector2Int coordinate)
    {
        coordinate = default;
        return _generated != null && _generated.TryGetAnchor(kind, out WorldGenerationAnchor anchor) &&
               TryFindNearestWalkableCell(anchor.Coordinate + offset, out coordinate);
    }

    bool TryFindNearestWalkableCell(Vector2Int desired, out Vector2Int coordinate)
    {
        coordinate = default;
        if (_grid == null) return false;
        foreach (WorldCellData cell in _grid.Cells
                     .Where(cell => cell.IsWalkable && !cell.HasWater)
                     .OrderBy(cell => (cell.Coordinate - desired).sqrMagnitude)
                     .ThenBy(cell => cell.Coordinate.y)
                     .ThenBy(cell => cell.Coordinate.x))
        {
            coordinate = cell.Coordinate;
            return true;
        }
        return false;
    }

    void RelocateActivity(string activityId, Vector2Int coordinate, string displayName)
    {
        DaytimeStockPrepPoint point = FindDaytimeActivity(activityId);
        if (point == null || !_grid.CellToWorld(coordinate, out Vector3 world)) return;
        point.transform.position = world + Vector3.up * 0.35f;
        point.Configure(point.activityId, point.itemResourcePath, point.grantCount, displayName);

        MeshRenderer primitiveRenderer = point.GetComponent<MeshRenderer>();
        if (primitiveRenderer != null) primitiveRenderer.enabled = false;

        GameObject dressing = GameObject.Find($"PA_DemoDressing_Prep_{activityId}");
        if (dressing != null) dressing.transform.position = point.transform.position;
    }

    void RelocateFarmPlot(FarmPlotInteraction plot, Vector2Int coordinate)
    {
        if (plot == null || !_grid.CellToWorld(coordinate, out Vector3 world)) return;
        plot.transform.position = world + Vector3.up * 0.03f;
    }

    public bool BeginDayForValidation(float hour = 9f, int day = 2)
    {
        if (!IsReady || _clock == null || _dayLoop == null) return false;
        _dayLoop.SimulatePhaseForValidation(hour, day);
        _dayLoop.ResetDayPrepForValidation();
        return _dayLoop.CurrentPhase == PADayNightPhase.DayPreparation;
    }

    public bool TryGatherNext(WorldResourceKind kind, out string spawnKey, out string reason)
    {
        spawnKey = string.Empty;
        reason = string.Empty;
        if (!IsReady || _generated == null || _inventory == null || _dayLoop == null ||
            _persistence == null)
        {
            reason = "Gameplay adapter is not ready.";
            return false;
        }

        int currentDay = _clock != null ? _clock.CurrentDay : 1;
        WorldResourceSpawnRecord? candidate = null;
        foreach (WorldResourceSpawnRecord spawn in _generated.ResourceSpawns
                     .Where(spawn => spawn.Kind == kind).OrderBy(spawn => spawn.SpawnKey))
        {
            if (!IsResourceConsumed(spawn.SpawnKey, currentDay))
            {
                candidate = spawn;
                break;
            }
        }
        if (!candidate.HasValue)
        {
            reason = $"No available {kind} resource remains for this day.";
            return false;
        }

        Item item = Resources.Load<Item>(ItemResourcePathFor(kind));
        if (item == null || !_inventory.CanAddItems(item, 1))
        {
            reason = item == null ? $"Item mapping is missing for {kind}." : "Inventory is full.";
            return false;
        }

        spawnKey = candidate.Value.SpawnKey;
        string activityId = ResourceActivityPrefix + spawnKey;
        if (!_dayLoop.TryCompleteDailyActivity(activityId))
        {
            reason = "Gathering is allowed once per generated resource during day preparation.";
            return false;
        }
        if (!_persistence.SetResourceState(spawnKey, true, currentDay + 1))
        {
            reason = "World resource state rejected the stable spawn key.";
            return false;
        }

        var instance = new ItemInstance(item, 1)
        {
            quality = 1.05f,
            currentPrice = item.basePrice
        };
        if (!_inventory.AddInstance(instance))
        {
            _persistence.SetResourceState(spawnKey, false, currentDay);
            reason = "Inventory changed after capacity preflight.";
            return false;
        }

        _lastAction = $"Gathered {item.itemName} from {spawnKey} into the existing Inventory.";
        return true;
    }

    bool IsResourceConsumed(string spawnKey, int currentDay)
    {
        WorldResourceStateSaveData state = CaptureWorldState()?.resourceStates?
            .FirstOrDefault(entry => entry != null && entry.spawnKey == spawnKey);
        if (state == null || !state.consumed) return false;
        if (currentDay < state.respawnDay) return true;
        _persistence.SetResourceState(spawnKey, false, currentDay);
        return false;
    }

    public bool TryCraftPlank(out string reason)
    {
        reason = string.Empty;
        if (!IsReady || _workbench == null)
        {
            reason = "B05 workbench is unavailable.";
            return false;
        }
        RecipeData recipe = Resources.Load<RecipeData>(PlankRecipeResource);
        if (recipe == null)
        {
            reason = "Existing Plank recipe is unavailable.";
            return false;
        }
        if (!CraftingService.TryCraft(recipe, _workbench))
        {
            reason = "CraftingService rejected the Plank recipe.";
            return false;
        }
        _lastAction = "Crafted Plank through Recipe_Plank and CraftingService.";
        return true;
    }

    public bool TryStockCraftedProduct(out ShopSlot stockedSlot, out string reason)
    {
        stockedSlot = null;
        reason = string.Empty;
        if (!IsReady || _inventory == null)
        {
            reason = "Inventory is unavailable.";
            return false;
        }
        stockedSlot = _shopSlots.FirstOrDefault(slot => slot != null && slot.gameObject.activeInHierarchy && slot.IsEmpty);
        if (stockedSlot == null)
        {
            reason = "No active empty B01 ShopSlot is available.";
            return false;
        }

        stockedSlot.Interact(_playerRoot);
        if (stockedSlot.IsEmpty)
        {
            reason = "ShopSlot did not accept the crafted inventory item.";
            return false;
        }
        _lastAction = $"Stocked {stockedSlot.currentItem.data.itemName} through ShopSlot.Interact.";
        return true;
    }

    public bool TryOpenShopForNight(int day, out string reason)
    {
        reason = string.Empty;
        if (!IsReady || _dayLoop == null)
        {
            reason = "Day/night authority is unavailable.";
            return false;
        }
        _dayLoop.SimulatePhaseForValidation(19f, Mathf.Max(1, day));
        if (!_dayLoop.TryOpenShop() || !_dayLoop.IsShopOpenForCustomers)
        {
            reason = "DayNightShopLoopController did not open the customer gate.";
            return false;
        }
        _lastAction = "Night shop opened through DayNightShopLoopController.";
        return true;
    }

    public bool TryMoveSalesDisplay(
        int gridX,
        int gridY,
        int quarterTurns,
        out string reason)
    {
        reason = string.Empty;
        if (!IsReady || _salesDisplayRoot == null || _shopSlots.Length == 0)
        {
            reason = "B01 sales display root is unavailable.";
            return false;
        }
        if (State == WorldGameplayAdapterState.CustomerMoving)
        {
            reason = "The sales display cannot move while a customer owns a visit target.";
            return false;
        }
        if (gridX < -1 || gridX > 1 || gridY < 0 || gridY > 1)
        {
            reason = "The display must remain inside the bounded B01 placement zone.";
            return false;
        }

        int normalized = WorldBuildingPlacementDefinition.NormalizeQuarterTurns(quarterTurns);
        ApplySalesDisplayPose(gridX, gridY, normalized);
        _salesDisplayReadability?.RefreshNow();
        _lastAction = $"기능 판매대를 ({gridX},{gridY}) 위치로 옮기고 {normalized * 90}° 회전했습니다.";
        return true;
    }

    void ApplySalesDisplayPose(int gridX, int gridY, int quarterTurns)
    {
        if (_salesDisplayRoot == null) return;
        _salesDisplayGridX = gridX;
        _salesDisplayGridY = gridY;
        _salesDisplayQuarterTurns =
            WorldBuildingPlacementDefinition.NormalizeQuarterTurns(quarterTurns);
        _salesDisplayRoot.localPosition = _salesDisplayBaseLocalPosition +
                                          new Vector3(gridX * 0.75f, 0f, gridY * 0.55f);
        _salesDisplayRoot.localRotation = _salesDisplayBaseLocalRotation *
                                          Quaternion.Euler(0f, _salesDisplayQuarterTurns * 90f, 0f);
    }

    public bool TryBeginCustomerVisit(ShopSlot targetSlot, out string reason)
    {
        reason = string.Empty;
        if (!IsReady || _navigation == null || _shop == null || targetSlot == null ||
            targetSlot.IsEmpty || _dayLoop == null || !_dayLoop.IsShopOpenForCustomers)
        {
            reason = "Shop, stock, navigation or open gate is unavailable.";
            return false;
        }
        StopCustomer();
        if (!_generated.TryGetAnchor(WorldGenerationAnchorKind.Start, out WorldGenerationAnchor start) ||
            !_grid.CellToWorld(start.Coordinate, out Vector3 startWorld) ||
            !NavMesh.SamplePosition(startWorld + Vector3.up * 0.3f,
                out NavMeshHit startHit, 2f, NavMesh.AllAreas))
        {
            reason = "Generated player start does not project onto the runtime NavMesh.";
            return false;
        }

        _customerRoot = new GameObject(CustomerRootName)
        {
            hideFlags = HideFlags.DontSave
        };
        _customerRoot.SetActive(false);
        _customerRoot.transform.SetParent(_runtimeRoot.transform, false);
        _customerRoot.transform.position = startHit.position;
        var agent = _customerRoot.AddComponent<NavMeshAgent>();
        agent.speed = 7.5f;
        agent.acceleration = 40f;
        agent.angularSpeed = 720f;
        agent.stoppingDistance = 0.25f;
        _customer = _customerRoot.AddComponent<NpcController>();
        _customer.profile = LoadCustomerProfile(_customerVisitSequence);
        if (_customer.profile == null)
        {
            ReleaseRuntimeObject(_customerRoot);
            _customerRoot = null;
            _customer = null;
            reason = "Existing Miner/Tailor NpcProfile resources are unavailable.";
            return false;
        }
        _customer.randomSeed = 9009 + _customerVisitSequence;
        _customer.idleTickInterval = 999f;
        _customer.browseDurationAtSlot = 0.65f;
        _customer.maxSlotsPerVisit = 1;
        _customer.shopArriveDistance = 1.5f;
        _customer.slotArriveDistance = 0.55f;
        _customerRoot.SetActive(true);
        agent.speed = 7.5f;
        agent.acceleration = 40f;
        string customerName = ResolveCustomerName();
        string preference = CustomerPreferencePresentationController.DescribePreference(
            _customer.profile);
        AddRuntimeLabel(_customerRoot.transform,
            $"{customerName} · {preference}\n가격과 상품 종류를 보고 결정",
            new Color(1f, 0.72f, 0.8f));

        _customerTargetSlot = targetSlot;
        _moneyBeforeCustomer = _economy != null ? _economy.Money : 0;
        SalesLogManager.DailyDecisionStats decisions = CurrentCustomerDayStats();
        _purchasesBeforeCustomer = decisions.purchases;
        _rejectionsBeforeCustomer = decisions.rejections;
        _customerStartedAt = Time.realtimeSinceStartup;
        _customerVisitSequence++;
        State = WorldGameplayAdapterState.CustomerMoving;
        _customerVisitRoutine = StartCoroutine(BeginCustomerVisitNextFrame());
        _lastCustomerOutcome = $"방문 중 · {customerName} · {preference}";
        _lastAction = _lastCustomerOutcome;
        return true;
    }

    static NpcProfile LoadCustomerProfile(int visitSequence)
    {
        string resource = visitSequence % 2 == 0
            ? MinerProfileResource
            : TailorProfileResource;
        return Resources.Load<NpcProfile>(resource);
    }

    SalesLogManager.DailyDecisionStats CurrentCustomerDayStats()
    {
        int day = _clock != null ? _clock.CurrentDay : 1;
        return SalesLogManager.Instance != null
            ? SalesLogManager.Instance.GetDailyDecisionStats(day)
            : default;
    }

    string ResolveCustomerName()
    {
        return _customer?.profile != null && !string.IsNullOrWhiteSpace(_customer.profile.npcName)
            ? _customer.profile.npcName
            : "손님";
    }

    IEnumerator BeginCustomerVisitNextFrame()
    {
        yield return null;
        _customerVisitRoutine = null;
        if (_customer == null)
            yield break;
        if (!_customer.TryBeginShoppingVisitAt(_shop.transform))
        {
            Fail("NpcController rejected the generated B01 shop destination.");
            yield break;
        }

        // The existing preference panel only lists non-idle customers. Refresh on
        // the first real FSM frame so a short WorldSandbox visit is readable before
        // the panel's normal periodic refresh runs.
        CustomerPreferencePresentationController.Instance?.RefreshNow();
    }

    public void StopCustomer()
    {
        if (_customerVisitRoutine != null)
        {
            StopCoroutine(_customerVisitRoutine);
            _customerVisitRoutine = null;
        }
        if (_customerRoot != null) ReleaseRuntimeObject(_customerRoot);
        _customerRoot = null;
        _customer = null;
        _customerTargetSlot = null;
        if (State == WorldGameplayAdapterState.CustomerMoving ||
            State == WorldGameplayAdapterState.CustomerPurchased ||
            State == WorldGameplayAdapterState.CustomerDeclined)
            State = WorldGameplayAdapterState.Ready;
    }

    public void PrepareForStateRestore()
    {
        StopCustomer();
        _lastFailure = string.Empty;
    }

    void BindRuntimeObjectsToSeed(long seed)
    {
        _generated = WorldIslandGenerator.Generate(seed);
        PositionExistingRuntimeObject(_shopRoot, WorldGenerationAnchorKind.Shop, true);
        PositionExistingRuntimeObject(_workbenchRoot, WorldGenerationAnchorKind.MeadowActivity, false);
        TryPositionExistingRuntimeObjectAtOffset(_kitchenRoot,
            WorldGenerationAnchorKind.MeadowActivity, new Vector2Int(6, -2));
        TryPositionExistingRuntimeObjectAtOffset(_forgeRoot,
            WorldGenerationAnchorKind.HighlandActivity, new Vector2Int(4, 3));
        TryPositionExistingRuntimeObjectAtOffset(_sewingRoot,
            WorldGenerationAnchorKind.MeadowActivity, new Vector2Int(-6, -2));
        if (!BindShopSignToRuntimeShop(out string signReason))
        {
            Fail($"Restored world shop-sign binding failed: {signReason}");
            return;
        }
        if (_generated.TryGetAnchor(WorldGenerationAnchorKind.Start, out WorldGenerationAnchor start) &&
            _grid.CellToWorld(start.Coordinate, out Vector3 playerPosition) && _playerRoot != null)
        {
            _playerRoot.transform.position = playerPosition + Vector3.up * 0.05f;
        }
        if (!BindDaytimeActivitiesToGeneratedWorld(out string activityReason))
        {
            Fail($"Restored world activity binding failed: {activityReason}");
            return;
        }
        if (!ConfigureResidentWorldBindings(out string residentReason))
        {
            Fail($"Restored world resident binding failed: {residentReason}");
            return;
        }
        VillageCultureVisualController.Instance?.RefreshNow();
        _boundSeed = seed;
        _lastAction = $"Existing gameplay anchors rebound to restored world seed {seed}.";
    }

    void PositionExistingRuntimeObject(
        GameObject instance,
        WorldGenerationAnchorKind anchorKind,
        bool faceEntrance)
    {
        if (instance == null || !_generated.TryGetAnchor(anchorKind, out WorldGenerationAnchor anchor) ||
            !_grid.CellToWorld(anchor.Coordinate, out Vector3 worldPosition)) return;
        Vector3 lookDirection = faceEntrance
            ? new Vector3(anchor.EntranceCoordinate.x - anchor.Coordinate.x, 0f,
                anchor.EntranceCoordinate.y - anchor.Coordinate.y)
            : ResolveLookDirection(anchor.Coordinate, WorldGenerationAnchorKind.Start);
        if (lookDirection.sqrMagnitude > 0.01f)
            instance.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        AlignObjectToGround(instance, worldPosition);
    }

    bool TryPositionExistingRuntimeObjectAtOffset(
        GameObject instance,
        WorldGenerationAnchorKind anchorKind,
        Vector2Int offset)
    {
        if (instance == null ||
            !TryResolveAnchorCoordinate(anchorKind, offset, out Vector2Int coordinate) ||
            !_grid.CellToWorld(coordinate, out Vector3 worldPosition))
        {
            return false;
        }

        Vector3 lookDirection = ResolveLookDirection(coordinate,
            WorldGenerationAnchorKind.Start);
        if (lookDirection.sqrMagnitude > 0.01f)
            instance.transform.rotation = Quaternion.LookRotation(
                lookDirection.normalized, Vector3.up);
        AlignObjectToGround(instance, worldPosition);
        return true;
    }

    public WorldStateSaveData CaptureWorldState()
    {
        if (_persistence == null || !_persistence.IsProceduralActive) return null;
        Vector3 playerPosition = _playerRoot != null ? _playerRoot.transform.position : Vector3.zero;
        return _persistence.CaptureState(playerPosition, CaptureWorldShopFurniture());
    }

    public void WriteSaveFields(SaveData data)
    {
        if (data == null) return;
        data.placeables ??= new List<PlaceableSaveData>();
        data.placeables.RemoveAll(record => record != null &&
            record.instanceId == SalesDisplayInstanceId);
        data.placeables.AddRange(CaptureWorldShopFurniture());
    }

    public void RestoreRuntimeWorldState(
        Vector3 safePlayerPosition,
        IReadOnlyList<PlaceableSaveData> restoredFurniture)
    {
        if (_persistence != null && _persistence.IsProceduralActive &&
            _persistence.ActiveSeed != _boundSeed)
        {
            BindRuntimeObjectsToSeed(_persistence.ActiveSeed);
        }

        if (_playerRoot != null)
        {
            CharacterController controller = _playerRoot.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            _playerRoot.transform.position = safePlayerPosition;
            if (controller != null) controller.enabled = true;
        }

        PlaceableSaveData display = restoredFurniture?.FirstOrDefault(record =>
            record != null && record.instanceId == SalesDisplayInstanceId);
        if (display != null)
            ApplySalesDisplayPose(display.gridX, display.gridY, display.rotationQuarterTurns);
        else
            ApplySalesDisplayPose(0, 0, 0);

        WorldAlphaPlayableController.Instance?.RequestProjectionRefresh();
        _lastAction = "Restored player, generated anchors and movable B01 display from SaveManager.";
    }

    List<PlaceableSaveData> CaptureWorldShopFurniture()
    {
        return new List<PlaceableSaveData>
        {
            new PlaceableSaveData
            {
                zoneId = ShopCustomizationController.ShopInteriorZoneId,
                definitionId = SalesDisplayDefinitionId,
                instanceId = SalesDisplayInstanceId,
                gridX = _salesDisplayGridX,
                gridY = _salesDisplayGridY,
                rotationQuarterTurns = _salesDisplayQuarterTurns,
                isFixed = true,
                recovered = false,
                functionalState = "shop-slot-authority",
                storedItems = new List<PlaceableStoredItemSaveData>()
            }
        };
    }

    public static string ItemResourcePathFor(WorldResourceKind kind)
    {
        return kind switch
        {
            WorldResourceKind.Forage => "Items/Item_Carrot",
            WorldResourceKind.Timber => "Items/Item_Wood",
            WorldResourceKind.Stone => "Items/Item_Ore",
            WorldResourceKind.Fish => "Items/Item_Fish",
            _ => string.Empty
        };
    }

    void Fail(string reason)
    {
        _lastFailure = string.IsNullOrWhiteSpace(reason) ? "Unknown WORLD-009 adapter failure." : reason;
        _lastAction = _lastFailure;
        State = WorldGameplayAdapterState.Failed;
        Debug.LogError($"[WORLD-009] RUNTIME_FAIL {_lastFailure}");
    }

    void OnGUI()
    {
        if (!Application.isPlaying || SceneManager.GetActiveScene().name != "WorldSandbox") return;
        if (WorldAlphaPlayableController.Instance != null) return;
        GUILayout.BeginArea(new Rect(18f, Mathf.Max(360f, Screen.height - 238f), 520f, 222f), GUI.skin.box);
        GUILayout.Label("WORLD-009 EXISTING GAMEPLAY ADAPTER");
        GUILayout.Label($"State {State} · seed {_boundSeed} · resources consumed {ConsumedResourceCount}");
        GUILayout.Label($"Inventory wood {CountItem("Items/Item_Wood")} · plank {CountItem("Items/Item_Plank")} · money {(_economy != null ? _economy.Money : 0)}G");
        GUILayout.Label(_lastAction);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Gather Timber")) TryGatherNext(WorldResourceKind.Timber, out _, out _lastAction);
        if (GUILayout.Button("Craft Plank")) TryCraftPlank(out _lastAction);
        if (GUILayout.Button("Stock B01")) TryStockCraftedProduct(out _, out _lastAction);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Open Night")) TryOpenShopForNight(_clock != null ? _clock.CurrentDay : 2, out _lastAction);
        if (GUILayout.Button("Send Customer"))
        {
            ShopSlot slot = _shopSlots.FirstOrDefault(candidate => candidate != null && !candidate.IsEmpty);
            TryBeginCustomerVisit(slot, out _lastAction);
        }
        if (GUILayout.Button("Save")) _ = _saveManager != null ? _saveManager.SaveGameAsync() : Task.CompletedTask;
        if (GUILayout.Button("Load")) _ = _saveManager != null ? _saveManager.LoadGameAsync() : Task.CompletedTask;
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    int CountItem(string resourcePath)
    {
        Item item = Resources.Load<Item>(resourcePath);
        return _inventory != null && item != null ? _inventory.CountItems(item) : 0;
    }

    static void ReleaseRuntimeObject(UnityEngine.Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}

// BETA-004 — player-facing, read-only presentation over the existing B01 ShopSlots.
// Stock, price, purchase, movement and persistence remain owned by their existing authorities.
[DisallowMultipleComponent]
public sealed class WorldSalesDisplayReadability : MonoBehaviour
{
    const string PresentationRootName = "BETA004_SalesDisplayReadability";

    readonly List<PrototypeWorldLabel> _slotLabels = new List<PrototypeWorldLabel>();
    ShopSlot[] _slots = Array.Empty<ShopSlot>();
    Transform _displayRoot;
    Transform _presentationRoot;
    PrototypeWorldLabel _headerLabel;
    string _lastSignature = string.Empty;
    float _nextRefresh;

    public bool IsReady => _displayRoot != null && _headerLabel != null &&
                           _slotLabels.Count == _slots.Length && _slots.Length > 0;
    public Transform DisplayRoot => _displayRoot;
    public Transform PresentationRoot => _presentationRoot;
    public string HeaderText => _headerLabel != null ? _headerLabel.label : string.Empty;
    public int SlotCount => _slots.Length;
    public int StockedSlotCount => _slots.Count(slot => slot != null && !slot.IsEmpty);
    public int EmptySlotCount => Mathf.Max(0, SlotCount - StockedSlotCount);
    public int VisibleWorldLabelCount => (_headerLabel != null && _headerLabel.gameObject.activeInHierarchy ? 1 : 0) +
                                         _slotLabels.Count(label => label != null && label.gameObject.activeInHierarchy);

    public bool Configure(Transform displayRoot, IReadOnlyList<ShopSlot> slots, out string reason)
    {
        reason = string.Empty;
        if (displayRoot == null || slots == null || slots.Count == 0 ||
            slots.Any(slot => slot == null || slot.transform.parent != displayRoot))
        {
            reason = "B01 sales display readability requires one shared functional ShopSlot root.";
            return false;
        }

        _displayRoot = displayRoot;
        _slots = slots.OrderBy(slot => slot.name, StringComparer.Ordinal).ToArray();
        EnsurePresentationObjects();
        RefreshNow();
        return IsReady;
    }

    void Update()
    {
        if (!IsReady || Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + 0.15f;
        string signature = BuildSignature();
        if (!string.Equals(signature, _lastSignature, StringComparison.Ordinal)) RefreshNow();
    }

    public void RefreshNow()
    {
        if (_displayRoot == null || _slots.Length == 0) return;
        EnsurePresentationObjects();
        _headerLabel.Set($"밤 영업 구역 · 상품 판매대 {_slots.Length}칸\nSpace: 진열 / 가격 설정",
            new Color(1f, 0.88f, 0.48f), 1.05f);

        for (int i = 0; i < _slots.Length; i++)
        {
            ShopSlot slot = _slots[i];
            PrototypeWorldLabel label = _slotLabels[i];
            label.transform.localPosition = slot.transform.localPosition + new Vector3(0f, 0.82f, 0f);
            label.Set(BuildSlotStatus(slot), ResolveStatusColor(slot), 0.72f);
        }

        _lastSignature = BuildSignature();
    }

    public string BuildSummary()
    {
        if (!IsReady) return "판매대 상태 확인 중";
        if (StockedSlotCount == 0) return $"판매대 0/{SlotCount}칸 · 빈 칸에서 Space로 상품 진열";

        string[] stocked = _slots.Where(slot => slot != null && !slot.IsEmpty)
            .Take(2)
            .Select(slot => $"{slot.currentItem.data.itemName} {slot.EffectiveDisplayPrice}G")
            .ToArray();
        string remainder = StockedSlotCount > stocked.Length ? $" 외 {StockedSlotCount - stocked.Length}칸" : string.Empty;
        return $"판매대 {StockedSlotCount}/{SlotCount}칸 · {string.Join(" · ", stocked)}{remainder}";
    }

    public string GetSlotStatus(int index)
    {
        return index >= 0 && index < _slots.Length ? BuildSlotStatus(_slots[index]) : string.Empty;
    }

    public Transform GetSlotLabelTransform(int index)
    {
        return index >= 0 && index < _slotLabels.Count && _slotLabels[index] != null
            ? _slotLabels[index].transform
            : null;
    }

    void EnsurePresentationObjects()
    {
        if (_presentationRoot == null)
        {
            GameObject root = new GameObject(PresentationRootName)
            {
                hideFlags = HideFlags.DontSave
            };
            root.transform.SetParent(_displayRoot, false);
            _presentationRoot = root.transform;
        }

        if (_headerLabel == null)
        {
            _headerLabel = CreateLabel("SalesDisplay_Header", _presentationRoot);
            Vector3 center = _slots.Aggregate(Vector3.zero,
                (sum, slot) => sum + slot.transform.localPosition) / _slots.Length;
            _headerLabel.transform.localPosition = center + new Vector3(0f, 1.75f, 0.15f);
        }

        while (_slotLabels.Count < _slots.Length)
            _slotLabels.Add(CreateLabel($"SalesDisplay_SlotStatus_{_slotLabels.Count + 1}", _presentationRoot));
    }

    static PrototypeWorldLabel CreateLabel(string objectName, Transform parent)
    {
        var labelObject = new GameObject(objectName)
        {
            hideFlags = HideFlags.DontSave
        };
        labelObject.transform.SetParent(parent, false);
        PrototypeWorldLabel label = labelObject.AddComponent<PrototypeWorldLabel>();
        TextMeshPro text = labelObject.GetComponent<TextMeshPro>();
        if (text != null)
        {
            text.fontStyle = FontStyles.Bold;
            text.sortingOrder = 24;
        }
        return label;
    }

    static string BuildSlotStatus(ShopSlot slot)
    {
        if (slot == null) return "사용 불가";
        if (slot.IsEmpty)
            return slot.IsSoldOutToday ? "오늘 품절\n다음 상품 진열" : "빈 칸\nSpace로 상품 진열";

        ItemInstance item = slot.currentItem;
        int qualityPercent = Mathf.RoundToInt(Mathf.Max(0f, item.quality) * 100f);
        return $"{item.data.itemName} ×{item.count}\n{slot.EffectiveDisplayPrice}G · 품질 {qualityPercent}%";
    }

    static Color ResolveStatusColor(ShopSlot slot)
    {
        if (slot == null) return Color.gray;
        if (slot.IsSoldOutToday) return new Color(1f, 0.58f, 0.52f);
        if (slot.IsEmpty) return new Color(0.88f, 0.84f, 0.72f);
        return slot.currentItem.data.category switch
        {
            ItemCategory.Raw => new Color(0.63f, 0.88f, 0.48f),
            ItemCategory.Processed => new Color(1f, 0.72f, 0.44f),
            ItemCategory.Utility => new Color(0.86f, 0.68f, 0.46f),
            ItemCategory.Luxury => new Color(0.82f, 0.74f, 1f),
            _ => new Color(1f, 0.90f, 0.64f)
        };
    }

    string BuildSignature()
    {
        return string.Join("|", _slots.Select(slot =>
        {
            if (slot == null) return "missing";
            if (slot.IsEmpty) return slot.IsSoldOutToday ? "sold-out" : "empty";
            return $"{slot.currentItem.instanceId}:{slot.currentItem.count}:" +
                   $"{slot.currentItem.quality:F3}:{slot.EffectiveDisplayPrice}";
        }));
    }
}

#if UNITY_EDITOR
public static class PA_WorldGameplayAdapterTools
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.WORLD009.Active";
    const string FailedKey = "PA.WORLD009.Failed";
    const string ConsoleErrorKey = "PA.WORLD009.ConsoleErrors";
    const string WaitFramesKey = "PA.WORLD009.WaitFrames";
    const string StageKey = "PA.WORLD009.Stage";

    static WorldGameplayAdapterService _adapter;
    static WorldPersistenceService _persistence;
    static WorldNavigationService _navigation;
    static ShopSlot _saleSlot;
    static Task _ioTask;
    static float _stageStarted;
    static int _expectedMoney;
    static long _expectedRevenue;
    static ulong _expectedWorldChecksum;
    static int _expectedConsumedResources;
    static string _validationSaveDirectory;

    [InitializeOnLoadMethod]
    static void ResumeAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        SubscribeCallbacks();
        if (EditorApplication.isPlaying) EditorApplication.update += ValidateRuntime;
    }

    [MenuItem("Project PA/World/WORLD-009/Validate Existing Gameplay Adapter")]
    public static void RunWorld009Validation()
    {
        RunWorld009ValidationInternal();
    }

    public static void RunWorld009ValidationBatch()
    {
        RunWorld009ValidationInternal();
    }

    static void RunWorld009ValidationInternal()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetInt(ConsoleErrorKey, 0);
            SessionState.SetInt(WaitFramesKey, 0);
            SessionState.SetInt(StageKey, 0);
            SubscribeCallbacks();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && !scene.isDirty,
                "WorldSandbox opens saved and clean");
            Require(UnityEngine.Object.FindObjectsByType<WorldGameplayAdapterService>(
                    FindObjectsSortMode.None).Length == 0,
                "adapter remains runtime-only and does not alter scene YAML");
            ValidateExistingAssets();
            Debug.Log("[WORLD-009] EDIT_MODE_PASS assets=true authorityMap=true sceneUntouched=true");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void ValidateExistingAssets()
    {
        BuildingData shop = Resources.Load<BuildingData>("Buildings/Building_B01_MarketStall");
        BuildingData workbench = Resources.Load<BuildingData>("Buildings/Building_B05_Workbench");
        RecipeData recipe = Resources.Load<RecipeData>("Recipes/Recipe_Plank");
        Item wood = Resources.Load<Item>(WorldGameplayAdapterService.ItemResourcePathFor(
            WorldResourceKind.Timber));
        Item plank = Resources.Load<Item>("Items/Item_Plank");
        Require(shop != null && shop.prefab != null &&
                shop.prefab.GetComponentInChildren<Shop>(true) != null &&
                shop.prefab.GetComponentsInChildren<ShopSlot>(true).Length >= 1,
            "B01 BuildingData retains the existing Shop and ShopSlot authority");
        Require(workbench != null && workbench.prefab != null &&
                workbench.prefab.GetComponentInChildren<Workbench>(true) != null,
            "B05 BuildingData retains the existing Workbench authority");
        Require(recipe != null && recipe.outputItem == plank && recipe.ingredients != null &&
                recipe.ingredients.Any(ingredient => ingredient != null && ingredient.item == wood &&
                                                     ingredient.count == 2),
            "Recipe_Plank remains 2 Wood to 1 Plank through CraftingService");
    }

    static void SubscribeCallbacks()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        Application.logMessageReceived += OnLogMessage;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetInt(WaitFramesKey, 0);
            SessionState.SetInt(StageKey, 0);
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            FinishValidation();
        }
    }

    static void ValidateRuntime()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
        int frames = SessionState.GetInt(WaitFramesKey, 0) + 1;
        SessionState.SetInt(WaitFramesKey, frames);
        int stage = SessionState.GetInt(StageKey, 0);
        try
        {
            if (stage == 0)
            {
                _adapter = WorldGameplayAdapterService.Instance ??
                           UnityEngine.Object.FindFirstObjectByType<WorldGameplayAdapterService>();
                if ((_adapter == null || !_adapter.IsReady) && frames < 600) return;
                Require(_adapter != null && _adapter.IsReady,
                    $"runtime adapter reaches Ready ({_adapter?.LastFailure})");
                Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
                    $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
                ValidateRuntimeAuthorities();

                _persistence = WorldPersistenceService.Instance;
                _navigation = UnityEngine.Object.FindFirstObjectByType<WorldNavigationService>();
                _validationSaveDirectory = Path.Combine(Application.dataPath, "..", "Logs",
                    "WorldGameplay", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(_validationSaveDirectory);
                _adapter.RuntimeSaveManager.SetRepositoryForValidation(
                    new LocalJsonSaveRepository(_validationSaveDirectory));

                Require(_adapter.BeginDayForValidation(9f, 2),
                    "day preparation opens through the existing day/night authority");
                Item wood = Resources.Load<Item>("Items/Item_Wood");
                Item plank = Resources.Load<Item>("Items/Item_Plank");
                bool gatheredA = _adapter.TryGatherNext(WorldResourceKind.Timber,
                    out string timberA, out string gatherReasonA);
                bool gatheredB = _adapter.TryGatherNext(WorldResourceKind.Timber,
                    out string timberB, out string gatherReasonB);
                Require(gatheredA && gatheredB && timberA != timberB &&
                        _adapter.PlayerInventory.CountItems(wood) == 2 &&
                        _adapter.ConsumedResourceCount == 2,
                    $"two stable Timber spawns enter Inventory ({gatherReasonA}; {gatherReasonB})");
                int feedbackBefore = _adapter.RuntimeWorkbench.CraftFeedbackCount;
                Require(_adapter.TryCraftPlank(out string craftReason) &&
                        _adapter.PlayerInventory.CountItems(wood) == 0 &&
                        _adapter.PlayerInventory.CountItems(plank) == 1 &&
                        _adapter.RuntimeWorkbench.CraftFeedbackCount == feedbackBefore + 1,
                    $"CraftingService consumes Wood and grants Plank ({craftReason})");
                Require(_adapter.TryStockCraftedProduct(out _saleSlot, out string stockReason) &&
                        _saleSlot != null && !_saleSlot.IsEmpty &&
                        _adapter.PlayerInventory.CountItems(plank) == 0,
                    $"ShopSlot.Interact moves the crafted Plank to B01 ({stockReason})");
                _saleSlot.displayPrice = 1;
                _saleSlot.RefreshDisplay();
                Require(_adapter.TryOpenShopForNight(2, out string openReason) &&
                        DayNightShopLoopController.Instance.IsShopOpenForCustomers,
                    $"explicit night opening enables the customer gate ({openReason})");
                Require(_adapter.TryBeginCustomerVisit(_saleSlot, out string customerReason),
                    $"NpcController accepts the generated Start-to-Shop destination ({customerReason})");
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(1);
                return;
            }

            if (stage == 1)
            {
                if (!_adapter.CustomerPurchaseCompleted)
                {
                    if (_adapter.State == WorldGameplayAdapterState.Failed ||
                        Time.realtimeSinceStartup - _stageStarted >= 24f)
                        throw new InvalidOperationException(
                            $"customer visit failed: {_adapter.LastFailure}");
                    return;
                }

                _expectedMoney = EconomyService.Instance.Money;
                _expectedRevenue = EconomyService.Instance.CumulativeRevenue;
                Require(_saleSlot.IsEmpty && _expectedMoney == 1 && _expectedRevenue == 1,
                    "existing NpcController purchase clears ShopSlot and deposits through EconomyService");
                Require(_adapter.BeginDayForValidation(9f, 3),
                    "post-sale Day 3 preparation is available before save");
                Item fish = Resources.Load<Item>("Items/Item_Fish");
                Require(_adapter.TryGatherNext(WorldResourceKind.Fish, out _, out string fishReason) &&
                        _adapter.PlayerInventory.CountItems(fish) == 1,
                    $"a second resource category remains in Inventory for save proof ({fishReason})");
                _expectedConsumedResources = _adapter.ConsumedResourceCount;
                _expectedWorldChecksum = WorldPersistenceService.ComputePayloadChecksum(
                    _adapter.CaptureWorldState());
                _ioTask = _adapter.RuntimeSaveManager.SaveGameAsync();
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(2);
                return;
            }

            if (stage == 2)
            {
                if (_ioTask != null && !_ioTask.IsCompleted)
                {
                    if (Time.realtimeSinceStartup - _stageStarted >= 5f)
                        throw new TimeoutException("isolated SaveManager write exceeded 5 seconds");
                    return;
                }
                if (_ioTask != null && _ioTask.IsFaulted) throw _ioTask.Exception;
                Require(File.Exists(Path.Combine(_validationSaveDirectory, "savegame.json")),
                    "SaveManager writes through an isolated LocalJsonSaveRepository");

                _adapter.StopCustomer();
                foreach (InventorySlot slot in _adapter.PlayerInventory.slots) slot?.Clear();
                _adapter.PlayerInventory.RefreshAllUI();
                EconomyService.Instance.ForceSet(0, "WORLD-009 restart mutation");
                EconomyService.Instance.ForceSetCumulativeRevenue(0, "WORLD-009 restart mutation");
                GameClock.Instance.ForceSet(15f, 8, "WORLD-009 restart mutation");
                Item plank = Resources.Load<Item>("Items/Item_Plank");
                _saleSlot.currentItem = new ItemInstance(plank, 1);
                _saleSlot.displayPrice = 99;
                _saleSlot.RefreshDisplay();
                Require(_persistence.StartProceduralWorld(WorldGameplayAdapterService.DefaultWorldSeed + 1,
                        out string restartReason),
                    $"runtime state is replaced before reload ({restartReason})");
                _ioTask = _adapter.RuntimeSaveManager.LoadGameAsync();
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(3);
                return;
            }

            if (stage == 3)
            {
                if (_ioTask != null && !_ioTask.IsCompleted)
                {
                    if (Time.realtimeSinceStartup - _stageStarted >= 8f)
                        throw new TimeoutException("SaveManager restore exceeded 8 seconds");
                    return;
                }
                if (_ioTask != null && _ioTask.IsFaulted) throw _ioTask.Exception;
                if ((_adapter.BoundSeed != WorldGameplayAdapterService.DefaultWorldSeed ||
                     _navigation.IsRebuilding) && Time.realtimeSinceStartup - _stageStarted < 12f)
                    return;

                Item fish = Resources.Load<Item>("Items/Item_Fish");
                Require(_persistence.ActiveSeed == WorldGameplayAdapterService.DefaultWorldSeed &&
                        _adapter.BoundSeed == WorldGameplayAdapterService.DefaultWorldSeed,
                    "restart reload restores the deterministic seed and rebinds gameplay anchors");
                Require(EconomyService.Instance.Money == _expectedMoney &&
                        EconomyService.Instance.CumulativeRevenue == _expectedRevenue,
                    "SaveManager restores EconomyService balance and cumulative revenue");
                Require(_adapter.PlayerInventory.CountItems(fish) == 1 &&
                        GameClock.Instance.CurrentDay == 3 &&
                        Mathf.Abs(GameClock.Instance.CurrentHour - 9f) < 0.05f,
                    "SaveManager restores Inventory and GameClock through existing authorities");
                Require(_saleSlot.IsEmpty,
                    "SaveManager restores the post-sale empty ShopSlot over mutated runtime stock");
                Require(_adapter.ConsumedResourceCount == _expectedConsumedResources &&
                        _persistence.ComputeCurrentChecksum() == _expectedWorldChecksum,
                    "world resource consumption round-trips with the unchanged v11 world payload");
                Require(_navigation.SectorCount == 16 && !_navigation.IsRebuilding &&
                        string.IsNullOrEmpty(_navigation.LastFailure),
                    "restored 128x128 world returns to sixteen local navigation sectors");
                Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                    "blocking runtime Console Error/Exception/Assert count is 0");
                Require(!SceneManager.GetActiveScene().isDirty,
                    "runtime adapter leaves WorldSandbox scene clean");
                Debug.Log("[WORLD-009] PLAY_MODE_PASS gather=true inventory=true craft=true " +
                          "shopOpen=true npcDestination=true purchase=true economy=true " +
                          "saveRestartRestore=true schema=11 sectors=16 console=0");
                EditorApplication.update -= ValidateRuntime;
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception ex)
        {
            EditorApplication.update -= ValidateRuntime;
            Fail(ex);
        }
    }

    static void ValidateRuntimeAuthorities()
    {
        int adapterCount = UnityEngine.Object.FindObjectsByType<WorldGameplayAdapterService>(
            FindObjectsSortMode.None).Length;
        int inventoryCount = Resources.FindObjectsOfTypeAll<Inventory>().Count(inventory =>
            inventory != null && inventory.gameObject.scene.IsValid() &&
            inventory.gameObject.scene.isLoaded);
        int economyCount = UnityEngine.Object.FindObjectsByType<EconomyService>(FindObjectsSortMode.None).Length;
        int clockCount = UnityEngine.Object.FindObjectsByType<GameClock>(FindObjectsSortMode.None).Length;
        int loopCount = UnityEngine.Object.FindObjectsByType<DayNightShopLoopController>(
            FindObjectsSortMode.None).Length;
        int saveCount = UnityEngine.Object.FindObjectsByType<SaveManager>(FindObjectsSortMode.None).Length;
        Require(adapterCount == 1 && inventoryCount == 1 &&
                Inventory.instance == _adapter.PlayerInventory && economyCount == 1 &&
                clockCount == 1 && loopCount == 1 && saveCount == 1,
            $"WorldSandbox adopts one existing authority each " +
            $"(adapter={adapterCount}, inventory={inventoryCount}, economy={economyCount}, " +
            $"clock={clockCount}, loop={loopCount}, save={saveCount})");
        Require(_adapter.RuntimeRoot != null && _adapter.PlayerRoot != null &&
                _adapter.PlayerRoot.CompareTag("Player") && _adapter.RuntimeShop != null &&
                _adapter.RuntimeShopSlots.Count >= 1 && _adapter.RuntimeWorkbench != null,
            "generated anchors host one player inventory, B01 Shop and B05 Workbench");
        Require(_adapter.BoundSeed == WorldGameplayAdapterService.DefaultWorldSeed &&
                WorldPersistenceService.Instance.ActiveSeed == WorldGameplayAdapterService.DefaultWorldSeed &&
                _adapter.CaptureWorldState()?.worldMode == WorldPersistenceMigration.ProceduralMode,
            "adapter starts on the procedural persistence authority without a save schema change");
    }

    static void SetStage(int stage)
    {
        SessionState.SetInt(StageKey, stage);
        SessionState.SetInt(WaitFramesKey, 0);
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        SessionState.SetInt(ConsoleErrorKey, SessionState.GetInt(ConsoleErrorKey, 0) + 1);
    }

    static void Fail(Exception ex)
    {
        SessionState.SetBool(FailedKey, true);
        Debug.LogError($"[WORLD-009] FAIL {ex.Message}\n{ex}");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else FinishValidation();
    }

    static void FinishValidation()
    {
        EditorApplication.update -= ValidateRuntime;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        bool failed = SessionState.GetBool(FailedKey, false) ||
                      SessionState.GetInt(ConsoleErrorKey, 0) != 0;
        int consoleErrors = SessionState.GetInt(ConsoleErrorKey, 0);
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(FailedKey);
        SessionState.EraseInt(ConsoleErrorKey);
        SessionState.EraseInt(WaitFramesKey);
        SessionState.EraseInt(StageKey);
        Debug.Log(failed
            ? $"[WORLD-009] FINISHED_WITH_ERRORS consoleErrors={consoleErrors}"
            : "[WORLD-009] FINISHED_PASS authorities=existing gather=true craft=true " +
              "shop=true npc=true economy=true saveRestartRestore=true schema=11 console=0");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[WORLD-009] PASS {message}");
    }
}

public static class PA_Beta002DaytimeActivityValidator
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.BETA002.Active";
    const string FailedKey = "PA.BETA002.Failed";
    const string ConsoleErrorKey = "PA.BETA002.ConsoleErrors";
    const string FrameKey = "PA.BETA002.Frames";
    const string StageKey = "PA.BETA002.Stage";

    static WorldAlphaPlayableController _alpha;
    static WorldGameplayAdapterService _adapter;
    static DayNightShopLoopController _loop;
    static FarmPlotInteraction _farmPlot;
    static float _stageStartedAt;

    [InitializeOnLoadMethod]
    static void ResumeAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        Subscribe();
        if (EditorApplication.isPlaying)
        {
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
    }

    [MenuItem("Project PA/Beta/BETA-002/Validate Daytime Activity Completion")]
    public static void RunBeta002Validation()
    {
        RunInternal();
    }

    public static void RunBeta002ValidationBatch()
    {
        RunInternal();
    }

    static void RunInternal()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetInt(ConsoleErrorKey, 0);
            SessionState.SetInt(FrameKey, 0);
            SessionState.SetInt(StageKey, 0);
            Subscribe();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && !scene.isDirty,
                "WorldSandbox opens saved and clean");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void Subscribe()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        Application.logMessageReceived += OnLogMessage;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            Finish();
        }
    }

    static void ValidateRuntime()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
        int frames = SessionState.GetInt(FrameKey, 0) + 1;
        SessionState.SetInt(FrameKey, frames);
        int stage = SessionState.GetInt(StageKey, 0);

        try
        {
            ResolveRuntime();
            bool runtimeReady = _alpha != null && _alpha.IsReady && _adapter != null &&
                                _adapter.DaytimeActivitiesBound;
            if (!runtimeReady && frames < 900)
            {
                return;
            }
            if (!runtimeReady)
                throw new InvalidOperationException(
                    "WorldSandbox did not bind existing daytime activities to the playable runtime.");

            if (stage == 0)
            {
                Require(true,
                    "WorldSandbox binds existing daytime activities to the playable runtime");
                Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
                    $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
                Require(_adapter.PlayerInteraction != null && _adapter.PlayerHotbar != null &&
                        Inventory.instance == _adapter.PlayerInventory &&
                        Inventory.instance.hotbar == _adapter.PlayerHotbar,
                    "the M70 player uses existing Space interaction, Inventory and Hotbar authorities");

                string[] coreIds =
                {
                    "forest-forage", "farm-seed-pouch", "quarry-mining", "shore-forage"
                };
                foreach (string id in coreIds)
                {
                    DaytimeStockPrepPoint point = _adapter.FindDaytimeActivity(id);
                    Require(point != null && _alpha.Grid.WorldToCell(point.transform.position,
                                out Vector2Int coordinate) &&
                            _alpha.Grid.TryGetCell(coordinate, out WorldCellData cell) &&
                            cell.IsWalkable && !cell.HasWater,
                        $"{id} resolves to a generated walkable island cell");
                    Require(point.GetComponent<MeshRenderer>() == null ||
                            !point.GetComponent<MeshRenderer>().enabled,
                        $"{id} does not expose its raw runtime cube");
                }
                Require(_adapter.FarmPlots.Count >= FarmPlotInteraction.RuntimePlotCount &&
                        _adapter.FarmPlots.All(plot => plot != null &&
                            _alpha.Grid.WorldToCell(plot.transform.position, out _)),
                    "two interactive farm plots resolve inside the generated meadow");

                ClearInventory();
                _loop.SimulatePhaseForValidation(9f, 1);
                _loop.ResetDayPrepForValidation();
                Require(_alpha.BeginNewGame() && MoveAwayFromStart(3f),
                    "the player starts the onboarding route before daytime play");
                SetStage(1);
                return;
            }

            if (frames < 3) return;
            if (stage == 1)
            {
                Require(_alpha.HasMoved && MoveNear(_adapter.RuntimeShop.transform, 4f),
                    "the player can reach the shop landmark before daytime activities");
                SetStage(2);
                return;
            }

            if (stage == 2)
            {
                Require(_alpha.HasReachedShop && MoveNear(_adapter.RuntimeWorkbench.transform, 4f),
                    "the player can reach the workbench landmark before daytime activities");
                SetStage(3);
                return;
            }

            if (stage == 3)
            {
                Require(_alpha.HasReachedWorkbench &&
                        _alpha.CurrentPlayerObjective.Contains("숲 채집터"),
                    "the player-facing objective hands off from onboarding to Gathering");

                DaytimeStockPrepPoint forest = _adapter.FindDaytimeActivity("forest-forage");
                Require(MoveNear(forest.transform, 2.25f) && InvokePlayerInteraction(),
                    "existing PlayerInteraction executes the forest Space interaction path");
                Item carrot = Resources.Load<Item>("Items/Item_Carrot");
                Require(Count(carrot) == 2 && _loop.IsDailyActivityCompleted("forest-forage"),
                    "Gathering grants Carrot x2 to Inventory and records daily completion");

                DaytimeStockPrepPoint seeds = _adapter.FindDaytimeActivity("farm-seed-pouch");
                seeds.Interact(_adapter.PlayerRoot);
                Item seed = Resources.Load<Item>("Items/Item_15_Seed");
                Require(Count(seed) == 2,
                    "the generated farm seed pouch grants two existing Seed items");
                _farmPlot = _adapter.FarmPlots.First(plot => plot != null && plot.CurrentCrop == null);
                _farmPlot.growthSecondsPerStage = 0.1f;
                _farmPlot.Interact(_adapter.PlayerRoot);
                Require(_farmPlot.CurrentCrop != null && Count(seed) == 1,
                    "Farming consumes one Seed and starts the existing Wheat crop");
                SetStage(4);
                return;
            }

            if (stage == 4)
            {
                DaytimeStockPrepPoint forest = _adapter.FindDaytimeActivity("forest-forage");
                Item carrot = Resources.Load<Item>("Items/Item_Carrot");
                if (_farmPlot?.CurrentCrop != null && !_farmPlot.CurrentCrop.isFullyGrown &&
                    Time.realtimeSinceStartup - _stageStartedAt < 3f)
                    return;
                Require(_farmPlot?.CurrentCrop != null && _farmPlot.CurrentCrop.isFullyGrown,
                    "the planted Wheat reaches its harvestable stage");
                _farmPlot.Interact(_adapter.PlayerRoot);
                Item wheat = Resources.Load<Item>("Items/Item_Wheat");
                Require(Count(wheat) == 3 &&
                        _loop.IsDailyActivityCompleted(FarmPlotInteraction.DailyHarvestActivityId),
                    "Farming grants Wheat x3 and records a daily harvest");

                DaytimeStockPrepPoint quarry = _adapter.FindDaytimeActivity("quarry-mining");
                MiningSpot mining = quarry.GetComponentInChildren<MiningSpot>(true);
                mining.Interact(_adapter.PlayerRoot);
                Require(mining.IsMining && mining.CompleteMiningForValidation(_adapter.PlayerRoot),
                    "Mining performs its strike-and-collect action");
                Item ore = Resources.Load<Item>("Items/Item_Ore");
                Require(Count(ore) == 2 && _loop.IsDailyActivityCompleted("quarry-mining"),
                    "Mining grants Ore x2 to Inventory and records daily completion");

                DaytimeStockPrepPoint shore = _adapter.FindDaytimeActivity("shore-forage");
                FishingSpot fishing = shore.GetComponentInChildren<FishingSpot>(true);
                fishing.Interact(_adapter.PlayerRoot);
                Require(fishing.IsFishing && fishing.CompleteCatchForValidation(_adapter.PlayerRoot),
                    "the existing Fishing foundation performs its cast-and-catch action");
                Item fish = Resources.Load<Item>("Items/Item_Fish");
                Require(Count(fish) == 2 && _loop.IsDailyActivityCompleted("shore-forage"),
                    "Fishing grants Fish x2 to Inventory and records daily completion");

                Require(new[] { carrot, wheat, ore, fish }.All(item =>
                            item != null && item.category == ItemCategory.Raw && item.basePrice > 0),
                    "all four daytime results are existing sellable Raw economy items");
                int value = carrot.basePrice * 2 + wheat.basePrice * 3 +
                            ore.basePrice * 2 + fish.basePrice * 2;
                Require(value > 0, $"the completed daytime route creates positive shop value ({value}G)");
                Require(_alpha.CurrentPlayerObjective.Contains("오늘 낮 활동 완료"),
                    "the player-facing objective recognizes the completed daytime route");

                int carrotBefore = Count(carrot);
                forest.Interact(_adapter.PlayerRoot);
                mining.Interact(_adapter.PlayerRoot);
                fishing.Interact(_adapter.PlayerRoot);
                Require(Count(carrot) == carrotBefore && !mining.IsMining && !fishing.IsFishing,
                    "same-day duplicate Gathering, Mining and Fishing rewards are blocked");

                _loop.SimulatePhaseForValidation(9f, 2);
                Require(_loop.IsDayPrepPointAvailable(forest) &&
                        _loop.IsDayPrepPointAvailable(quarry) &&
                        _loop.IsDayPrepPointAvailable(shore),
                    "daily activity points reactivate on the next morning");
                Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                    "blocking runtime Console Error/Exception/Assert count is 0");
                Require(!SceneManager.GetActiveScene().isDirty,
                    "BETA-002 remains runtime-only and leaves WorldSandbox scene clean");
                Debug.Log($"[BETA-002] PLAY_MODE_PASS gathering=true farming=true mining=true " +
                          $"fishing=true inventory=true economyValue={value} console=0");
                EditorApplication.update -= ValidateRuntime;
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception ex)
        {
            EditorApplication.update -= ValidateRuntime;
            Fail(ex);
        }
    }

    static void ResolveRuntime()
    {
        _alpha = WorldAlphaPlayableController.Instance ??
                 UnityEngine.Object.FindFirstObjectByType<WorldAlphaPlayableController>();
        _adapter = WorldGameplayAdapterService.Instance ??
                   UnityEngine.Object.FindFirstObjectByType<WorldGameplayAdapterService>();
        _loop = DayNightShopLoopController.Instance ??
                UnityEngine.Object.FindFirstObjectByType<DayNightShopLoopController>();
    }

    static bool MoveAwayFromStart(float minimumDistance)
    {
        Vector3 origin = _adapter.PlayerRoot.transform.position;
        foreach (WorldCellData cell in _alpha.Grid.Cells
                     .Where(cell => cell.IsWalkable && !cell.HasWater)
                     .OrderBy(cell => FlatDistance(origin, CellWorld(cell.Coordinate))))
        {
            float distance = FlatDistance(origin, CellWorld(cell.Coordinate));
            if (distance >= minimumDistance && distance <= 16f)
                return _alpha.MovePlayerToCellForValidation(cell.Coordinate);
        }
        return false;
    }

    static bool MoveNear(Transform target, float maximumDistance)
    {
        if (target == null) return false;
        foreach (WorldCellData cell in _alpha.Grid.Cells
                     .Where(cell => cell.IsWalkable && !cell.HasWater)
                     .OrderBy(cell => FlatDistance(target.position, CellWorld(cell.Coordinate))))
        {
            Vector3 world = CellWorld(cell.Coordinate);
            if (FlatDistance(target.position, world) > maximumDistance) return false;
            if (!_alpha.MovePlayerToCellForValidation(cell.Coordinate)) return false;
            Vector3 facing = target.position - _adapter.PlayerRoot.transform.position;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.001f)
                _adapter.PlayerRoot.transform.rotation = Quaternion.LookRotation(facing.normalized);
            Physics.SyncTransforms();
            return true;
        }
        return false;
    }

    static bool InvokePlayerInteraction()
    {
        var method = typeof(PlayerInteraction).GetMethod("TryInteract",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (method == null || _adapter.PlayerInteraction == null) return false;
        method.Invoke(_adapter.PlayerInteraction, null);
        return true;
    }

    static Vector3 CellWorld(Vector2Int coordinate)
    {
        return _alpha.Grid.CellToWorld(coordinate, out Vector3 world)
            ? world
            : new Vector3(float.MaxValue, 0f, float.MaxValue);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    static int Count(Item item)
    {
        return item != null && Inventory.instance != null ? Inventory.instance.CountItems(item) : 0;
    }

    static void ClearInventory()
    {
        if (Inventory.instance == null) return;
        foreach (InventorySlot slot in Inventory.instance.slots) slot?.Clear();
        if (Inventory.instance.hotbar != null)
            foreach (InventorySlot slot in Inventory.instance.hotbar.slots) slot?.Clear();
        Inventory.instance.RefreshAllUI();
    }

    static void SetStage(int stage)
    {
        SessionState.SetInt(StageKey, stage);
        SessionState.SetInt(FrameKey, 0);
        _stageStartedAt = Time.realtimeSinceStartup;
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        SessionState.SetInt(ConsoleErrorKey,
            SessionState.GetInt(ConsoleErrorKey, 0) + 1);
    }

    static void Fail(Exception ex)
    {
        SessionState.SetBool(FailedKey, true);
        Debug.LogError($"[BETA-002] FAIL {ex.Message}\n{ex}");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else Finish();
    }

    static void Finish()
    {
        EditorApplication.update -= ValidateRuntime;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        bool failed = SessionState.GetBool(FailedKey, false) ||
                      SessionState.GetInt(ConsoleErrorKey, 0) != 0;
        int errors = SessionState.GetInt(ConsoleErrorKey, 0);
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(FailedKey);
        SessionState.EraseInt(ConsoleErrorKey);
        SessionState.EraseInt(FrameKey);
        SessionState.EraseInt(StageKey);
        Debug.Log(failed
            ? $"[BETA-002] FINISHED_WITH_ERRORS consoleErrors={errors}"
            : "[BETA-002] FINISHED_PASS gathering=true farming=true mining=true " +
              "fishing=true inventory=true economy=true console=0");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[BETA-002] PASS {message}");
    }
}

public static class PA_Beta003CraftingProductionValidator
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.BETA003.Active";
    const string FailedKey = "PA.BETA003.Failed";
    const string ConsoleErrorKey = "PA.BETA003.ConsoleErrors";
    const string FrameKey = "PA.BETA003.Frames";
    const string StageKey = "PA.BETA003.Stage";

    static readonly string[] RecipePaths =
    {
        "Recipes/Recipe_BakedPotato",
        "Recipes/Recipe_Bread",
        "Recipes/Recipe_GrilledFish",
        "Recipes/Recipe_IronBar",
        "Recipes/Recipe_Plank"
    };

    static WorldAlphaPlayableController _alpha;
    static WorldGameplayAdapterService _adapter;
    static CraftingUI _craftingUi;

    [InitializeOnLoadMethod]
    static void ResumeAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        Subscribe();
        if (EditorApplication.isPlaying)
        {
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
    }

    [MenuItem("Project PA/Beta/BETA-003/Validate Crafting and Production Expansion")]
    public static void RunBeta003Validation()
    {
        RunInternal();
    }

    public static void RunBeta003ValidationBatch()
    {
        RunInternal();
    }

    static void RunInternal()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetInt(ConsoleErrorKey, 0);
            SessionState.SetInt(FrameKey, 0);
            SessionState.SetInt(StageKey, 0);
            Subscribe();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && !scene.isDirty,
                "WorldSandbox opens saved and clean");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void Subscribe()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        Application.logMessageReceived += OnLogMessage;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            Finish();
        }
    }

    static void ValidateRuntime()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
        int frames = SessionState.GetInt(FrameKey, 0) + 1;
        SessionState.SetInt(FrameKey, frames);
        int stage = SessionState.GetInt(StageKey, 0);

        try
        {
            ResolveRuntime();
            bool runtimeReady = _alpha != null && _alpha.IsReady && _adapter != null &&
                                _adapter.ProductionFacilitiesBound && _craftingUi != null;
            if (!runtimeReady && frames < 900) return;
            if (!runtimeReady)
                throw new InvalidOperationException(
                    "WorldSandbox did not bind the existing B05/B06/B07 production facilities.");

            if (stage == 0)
            {
                Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
                    $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
                Require(_adapter.RuntimeWorkbench.workbenchType == WorkbenchType.BasicWorkbench &&
                        _adapter.RuntimeKitchen.workbenchType == WorkbenchType.Kitchen &&
                        _adapter.RuntimeForge.workbenchType == WorkbenchType.Forge,
                    "existing B05 Basic, B06 Kitchen and B07 Forge workbench authorities are active");
                Require(IsGeneratedWalkable(_adapter.RuntimeWorkbench.transform) &&
                        IsGeneratedWalkable(_adapter.RuntimeKitchen.transform) &&
                        IsGeneratedWalkable(_adapter.RuntimeForge.transform),
                    "all three production facilities occupy generated walkable island cells");
                Require(FlatDistance(_adapter.RuntimeWorkbench.transform.position,
                            _adapter.RuntimeKitchen.transform.position) >= 4f,
                    "the Kitchen is separated from the Basic workbench interaction space");
                DaytimeStockPrepPoint quarry = _adapter.FindDaytimeActivity("quarry-mining");
                Require(quarry != null && FlatDistance(quarry.transform.position,
                            _adapter.RuntimeForge.transform.position) >= 4f,
                    "the Forge is separated from the generated mining interaction space");
                Require(HasRoleLabel(_adapter.RuntimeKitchen, "주방 가공대") &&
                        HasRoleLabel(_adapter.RuntimeForge, "대장간 용광로"),
                    "player-facing labels explain the Kitchen and Forge roles");

                ClearInventory();
                OpenWorkbenchCards(_adapter.RuntimeKitchen);
                SetStage(1);
                return;
            }

            if (frames < 3) return;
            if (stage == 1)
            {
                ValidateOpenWorkbenchCards(WorkbenchType.Kitchen,
                    expectedMinimum: 3, requireShortageText: true);
                OpenWorkbenchCards(_adapter.RuntimeForge);
                SetStage(2);
                return;
            }

            if (frames < 3) return;
            if (stage == 2)
            {
                ValidateOpenWorkbenchCards(WorkbenchType.Forge,
                    expectedMinimum: 1, requireShortageText: true);
                OpenWorkbenchCards(_adapter.RuntimeWorkbench);
                SetStage(3);
                return;
            }

            if (frames < 3) return;
            if (stage == 3)
            {
                ValidateOpenWorkbenchCards(WorkbenchType.BasicWorkbench,
                    expectedMinimum: 2, requireShortageText: true);
                Require(VisibleRecipeCardNames().Contains("Recipe_Recipe_Plank") &&
                        VisibleRecipeCardNames().Contains("Recipe_Recipe_Furniture"),
                    "the two existing Basic recipe cards remain visible");
                RecipeData baked = LoadRecipe(RecipePaths[0]);
                int outputBefore = Count(baked.outputItem);
                Require(!CraftingService.TryCraft(baked, _adapter.RuntimeKitchen) &&
                        Count(baked.outputItem) == outputBefore,
                    "insufficient ingredients block execution without hiding or granting the recipe output");
                _craftingUi.Close();
                RecipeData[] recipes = RecipePaths.Select(LoadRecipe).ToArray();
                foreach (RecipeData recipe in recipes) SeedIngredients(recipe, 1.05f);
                OpenWorkbenchCards(_adapter.RuntimeKitchen);
                SetStage(4);
                return;
            }

            if (frames < 3) return;
            if (stage == 4)
            {
                ValidateOpenWorkbenchCards(WorkbenchType.Kitchen,
                    expectedMinimum: 3, requireShortageText: false);
                Require(VisibleRecipeCardNames().Contains("Recipe_Recipe_BakedPotato") &&
                        VisibleRecipeCardNames().Contains("Recipe_Recipe_Bread") &&
                        VisibleRecipeCardNames().Contains("Recipe_Recipe_GrilledFish"),
                    "the actual ingredient-ready Kitchen recipes remain visible and selectable");
                Require(_craftingUi.slotParent.Cast<Transform>()
                        .Where(card => card != null && card.name.StartsWith("Recipe_", StringComparison.Ordinal))
                        .All(card => card.GetComponent<UnityEngine.UI.Button>()?.interactable == true),
                    "ingredient-ready Tier 0 Kitchen recipe cards are selectable");
                _craftingUi.Close();
                RecipeData[] recipes = RecipePaths.Select(LoadRecipe).ToArray();

                ProcessingOpportunityController advisor =
                    ProcessingOpportunityController.Instance ??
                    UnityEngine.Object.FindFirstObjectByType<ProcessingOpportunityController>();
                Require(advisor != null, "the existing processing value advisor is active");

                int totalInputValue = 0;
                int totalOutputBaseValue = 0;
                int kitchenFeedbackBefore = _adapter.RuntimeKitchen.CraftFeedbackCount;
                int basicFeedbackBefore = _adapter.RuntimeWorkbench.CraftFeedbackCount;
                foreach (RecipeData recipe in recipes)
                {
                    Workbench workbench = WorkbenchFor(recipe);
                    ProcessingOpportunityController.Opportunity opportunity = advisor.Evaluate(recipe);
                    Require(opportunity != null && opportunity.expectedMargin > 0 &&
                            opportunity.expectedOutputValue > opportunity.inputBaseValue,
                        $"{recipe.recipeName} exposes a positive quality-adjusted processing margin");
                    totalInputValue += opportunity.inputBaseValue;
                    totalOutputBaseValue += recipe.outputItem.basePrice * recipe.outputCount;

                    int before = Count(recipe.outputItem);
                    Require(CraftingService.TryCraft(recipe, workbench) &&
                            Count(recipe.outputItem) == before + recipe.outputCount,
                        $"CraftingService completes {recipe.recipeName} at its existing workbench");
                    ItemInstance output = FindInstance(recipe.outputItem);
                    Require(output != null && output.quality > 1f &&
                            output.quality >= recipe.baseOutputQuality &&
                            output.EffectivePrice == recipe.outputItem.basePrice,
                        $"{recipe.outputItem.itemName} preserves recipe quality and base price metadata in ItemInstance");
                }

                Require(totalOutputBaseValue > totalInputValue,
                    $"the five product set raises raw base value ({totalInputValue}G -> {totalOutputBaseValue}G)");
                Require(_adapter.RuntimeKitchen.CraftFeedbackCount == kitchenFeedbackBefore + 3 &&
                        _adapter.RuntimeWorkbench.CraftFeedbackCount == basicFeedbackBefore + 1,
                    "Kitchen and Basic workbench provide success feedback for completed products");
                Require(recipes.All(recipe => recipe.outputItem.category == ItemCategory.Processed &&
                                      recipe.outputItem.basePrice > 0),
                    "all five outputs are existing sellable Processed economy items");

                int moneyBefore = EconomyService.Instance.Money;
                Require(_adapter.TryStockCraftedProduct(out ShopSlot saleSlot,
                            out string stockReason) && saleSlot != null && !saleSlot.IsEmpty,
                    $"an actual processed ItemInstance enters the B01 display ({stockReason})");
                float stockedQuality = saleSlot.currentItem.quality;
                Require(stockedQuality > 1f && saleSlot.EffectiveDisplayPrice > 0,
                    "the B01 display preserves processed quality and a positive player-facing price");
                Require(_adapter.TryOpenShopForNight(1, out string openReason),
                    $"the existing day/night gate opens the shop ({openReason})");
                Require(saleSlot.TryPurchaseByNpc("BETA003_VALIDATOR", out int paid) &&
                        paid > 0 && EconomyService.Instance.Money == moneyBefore + paid,
                    "the processed product completes the existing sale and economy deposit path");

                Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                    "blocking runtime Console Error/Exception/Assert count is 0");
                Require(!SceneManager.GetActiveScene().isDirty,
                    "BETA-003 remains runtime-only and leaves WorldSandbox scene clean");
                Debug.Log($"[BETA-003] PLAY_MODE_PASS facilities=3 recipes={recipes.Length} " +
                          $"rawValue={totalInputValue} productValue={totalOutputBaseValue} " +
                          $"quality=true ui=true sale={paid}G console=0");
                EditorApplication.update -= ValidateRuntime;
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception ex)
        {
            EditorApplication.update -= ValidateRuntime;
            Fail(ex);
        }
    }

    static void ResolveRuntime()
    {
        _alpha = WorldAlphaPlayableController.Instance ??
                 UnityEngine.Object.FindFirstObjectByType<WorldAlphaPlayableController>();
        _adapter = WorldGameplayAdapterService.Instance ??
                   UnityEngine.Object.FindFirstObjectByType<WorldGameplayAdapterService>();
        _craftingUi = CraftingUI.instance ??
                      UnityEngine.Object.FindFirstObjectByType<CraftingUI>();
    }

    static void OpenWorkbenchCards(Workbench workbench)
    {
        if (workbench == null)
            throw new InvalidOperationException("Production workbench context is missing.");
        _craftingUi.OpenForWorkbench(workbench);
    }

    static void ValidateOpenWorkbenchCards(WorkbenchType type,
        int expectedMinimum, bool requireShortageText)
    {
        Require(_craftingUi.IsOpen && _craftingUi.ActiveWorkbench != null &&
                _craftingUi.ActiveWorkbench.workbenchType == type,
            $"{type} is the active crafting UI context after the frame boundary");
        Canvas.ForceUpdateCanvases();
        var cards = _craftingUi.slotParent.Cast<Transform>()
            .Where(child => child != null && child.name.StartsWith("Recipe_", StringComparison.Ordinal))
            .ToArray();
        int expected = Resources.LoadAll<RecipeData>("Recipes")
            .Count(recipe => recipe != null && recipe.requiredWorkbench == type);
        Require(expected >= expectedMinimum && cards.Length == expected,
            $"{type} UI card count matches its existing recipe set ({cards.Length})");
        Require(cards.All(card => card.gameObject.activeInHierarchy &&
                                  card is RectTransform rect &&
                                  rect.rect.width > 0f && rect.rect.height > 0f),
            $"{type} recipe cards are active with non-zero layout bounds");
        RectTransform viewport = _craftingUi.slotParent.parent as RectTransform;
        Require(viewport != null && cards.All(card => IntersectsViewport(
                    viewport, (RectTransform)card)),
            $"{type} recipe cards intersect the visible ScrollRect viewport");
        if (requireShortageText)
        {
            Require(cards.Any(card => card.GetComponentInChildren<TMPro.TMP_Text>(true)?.text
                        .Contains("0/") == true),
                $"{type} cards remain visible and show owned/required counts with no materials");
        }
    }

    static HashSet<string> VisibleRecipeCardNames()
    {
        return _craftingUi.slotParent.Cast<Transform>()
            .Where(child => child != null && child.gameObject.activeInHierarchy &&
                            child.name.StartsWith("Recipe_", StringComparison.Ordinal))
            .Select(child => child.name)
            .ToHashSet(StringComparer.Ordinal);
    }

    static bool IntersectsViewport(RectTransform viewport, RectTransform card)
    {
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, card);
        Rect rect = viewport.rect;
        return bounds.max.x >= rect.xMin && bounds.min.x <= rect.xMax &&
               bounds.max.y >= rect.yMin && bounds.min.y <= rect.yMax;
    }

    static bool IsGeneratedWalkable(Transform target)
    {
        return target != null && _alpha.Grid.WorldToCell(target.position,
                   out Vector2Int coordinate) &&
               _alpha.Grid.TryGetCell(coordinate, out WorldCellData cell) &&
               cell.IsWalkable && !cell.HasWater;
    }

    static bool HasRoleLabel(Workbench workbench, string text)
    {
        return workbench != null && workbench.transform.root
            .GetComponentsInChildren<PrototypeWorldLabel>(true)
            .Any(label => label != null && label.label.Contains(text));
    }

    static RecipeData LoadRecipe(string path)
    {
        RecipeData recipe = Resources.Load<RecipeData>(path);
        if (recipe == null || recipe.outputItem == null || recipe.ingredients == null ||
            recipe.ingredients.Count == 0)
            throw new InvalidOperationException($"Existing production recipe is invalid: {path}");
        return recipe;
    }

    static Workbench WorkbenchFor(RecipeData recipe)
    {
        return recipe.requiredWorkbench switch
        {
            WorkbenchType.BasicWorkbench => _adapter.RuntimeWorkbench,
            WorkbenchType.Kitchen => _adapter.RuntimeKitchen,
            WorkbenchType.Forge => _adapter.RuntimeForge,
            _ => null
        };
    }

    static void SeedIngredients(RecipeData recipe, float quality)
    {
        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null || ingredient.count <= 0) continue;
            var instance = new ItemInstance(ingredient.item, ingredient.count)
            {
                quality = quality,
                currentPrice = ingredient.item.basePrice
            };
            Require(Inventory.instance.AddInstance(instance),
                $"seeded {ingredient.item.itemName} x{ingredient.count} from the BETA-002 resource contract");
        }
    }

    static ItemInstance FindInstance(Item item)
    {
        if (item == null || Inventory.instance == null) return null;
        InventorySlot slot = Inventory.instance.slots
            .FirstOrDefault(candidate => candidate != null && !candidate.IsEmpty &&
                                         candidate.item == item);
        if (slot != null) return slot.instance;
        return Inventory.instance.hotbar?.slots?
            .FirstOrDefault(candidate => candidate != null && !candidate.IsEmpty &&
                                         candidate.item == item)?.instance;
    }

    static int Count(Item item)
    {
        return item != null && Inventory.instance != null
            ? Inventory.instance.CountItems(item)
            : 0;
    }

    static void ClearInventory()
    {
        if (Inventory.instance == null) return;
        foreach (InventorySlot slot in Inventory.instance.slots) slot?.Clear();
        if (Inventory.instance.hotbar != null)
            foreach (InventorySlot slot in Inventory.instance.hotbar.slots) slot?.Clear();
        Inventory.instance.RefreshAllUI();
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    static void SetStage(int stage)
    {
        SessionState.SetInt(StageKey, stage);
        SessionState.SetInt(FrameKey, 0);
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        SessionState.SetInt(ConsoleErrorKey,
            SessionState.GetInt(ConsoleErrorKey, 0) + 1);
    }

    static void Fail(Exception ex)
    {
        SessionState.SetBool(FailedKey, true);
        Debug.LogError($"[BETA-003] FAIL {ex.Message}\n{ex}");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else Finish();
    }

    static void Finish()
    {
        EditorApplication.update -= ValidateRuntime;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        bool failed = SessionState.GetBool(FailedKey, false) ||
                      SessionState.GetInt(ConsoleErrorKey, 0) != 0;
        int errors = SessionState.GetInt(ConsoleErrorKey, 0);
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(FailedKey);
        SessionState.EraseInt(ConsoleErrorKey);
        SessionState.EraseInt(FrameKey);
        SessionState.EraseInt(StageKey);
        Debug.Log(failed
            ? $"[BETA-003] FINISHED_WITH_ERRORS consoleErrors={errors}"
            : "[BETA-003] FINISHED_PASS facilities=3 recipes=5 quality=true " +
              "ui=true sale=true console=0");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[BETA-003] PASS {message}");
    }
}

public static class PA_Beta004ShopReadabilityValidator
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.BETA004.Active";
    const string FailedKey = "PA.BETA004.Failed";
    const string ConsoleErrorKey = "PA.BETA004.ConsoleErrors";
    const string FrameKey = "PA.BETA004.Frames";
    const string StageKey = "PA.BETA004.Stage";

    static WorldAlphaPlayableController _alpha;
    static WorldGameplayAdapterService _adapter;
    static WorldSalesDisplayReadability _readability;
    static ShopSlot _saleSlot;
    static int _moneyBefore;
    static float _stageStarted;

    [InitializeOnLoadMethod]
    static void ResumeAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        Subscribe();
        if (EditorApplication.isPlaying)
        {
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
    }

    [MenuItem("Project PA/Beta/BETA-004/Validate Shop Readability")]
    public static void RunBeta004Validation() => RunInternal();

    public static void RunBeta004ValidationBatch() => RunInternal();

    static void RunInternal()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetInt(ConsoleErrorKey, 0);
            SessionState.SetInt(FrameKey, 0);
            SessionState.SetInt(StageKey, 0);
            Subscribe();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && !scene.isDirty,
                "WorldSandbox opens saved and clean");
            Require(UnityEngine.Object.FindObjectsByType<WorldSalesDisplayReadability>(
                    FindObjectsSortMode.None).Length == 0,
                "sales-display readability remains runtime-only and does not alter scene YAML");

            BuildingData market = Resources.Load<BuildingData>("Buildings/Building_B01_MarketStall");
            ShopSlot[] authoredSlots = market != null && market.prefab != null
                ? market.prefab.GetComponentsInChildren<ShopSlot>(true)
                : Array.Empty<ShopSlot>();
            Require(market != null && market.prefab != null &&
                    market.prefab.GetComponentInChildren<Shop>(true) != null &&
                    authoredSlots.Length == 4,
                "existing B01 prefab remains the four-slot shop authority");
            Require(WorldPersistenceMigration.AdditiveWorldSaveVersion == 11,
                "save schema remains unchanged at additive world version 11");
            Debug.Log("[BETA-004] EDIT_MODE_PASS b01Slots=4 runtimeOnly=true saveSchema=v11");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void Subscribe()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        Application.logMessageReceived += OnLogMessage;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SetStage(0);
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            Finish();
        }
    }

    static void ValidateRuntime()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
        int frames = SessionState.GetInt(FrameKey, 0) + 1;
        SessionState.SetInt(FrameKey, frames);
        int stage = SessionState.GetInt(StageKey, 0);

        try
        {
            ResolveRuntime();
            if ((_alpha == null || !_alpha.IsReady || _readability == null || !_readability.IsReady) &&
                frames < 900) return;

            if (stage == 0)
            {
                Require(_alpha != null && _alpha.IsReady && _adapter != null && _adapter.IsReady,
                    "playable WorldSandbox and existing gameplay adapter reach Ready");
                Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
                    $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
                Require(_readability != null && _readability.IsReady &&
                        _readability.DisplayRoot == _adapter.SalesDisplayTarget &&
                        _readability.PresentationRoot.parent == _adapter.SalesDisplayTarget,
                    "readability presentation follows the same movable B01 ShopSlot root");
                Require(_readability.SlotCount == 4 && _readability.EmptySlotCount == 4 &&
                        _readability.VisibleWorldLabelCount == 5,
                    "four empty functional slots expose one header and four visible state labels");
                Require(_readability.HeaderText.Contains("밤 영업 구역") &&
                        _readability.HeaderText.Contains("상품 판매대") &&
                        _readability.HeaderText.Contains("Space"),
                    "the shop boundary, display role and interaction are explicit in world space");
                Require(Enumerable.Range(0, 4).All(index =>
                        _readability.GetSlotStatus(index).Contains("빈 칸") &&
                        _readability.GetSlotStatus(index).Contains("상품 진열")) &&
                        _alpha.ShopMerchandisingSummary.Contains("0/4칸"),
                    "empty stock state is readable in both world labels and player HUD");

                Item bread = Resources.Load<Item>("Items/Item_BreadLoaf");
                Require(bread != null && _adapter.PlayerInventory.AddInstance(new ItemInstance(bread, 1)
                        { quality = 1.25f }),
                    "an existing processed ItemInstance enters Inventory with quality metadata");
                Require(_adapter.TryStockCraftedProduct(out _saleSlot, out string stockReason) &&
                        _saleSlot != null && !_saleSlot.IsEmpty,
                    $"ShopSlot.Interact stocks the real B01 display ({stockReason})");
                _saleSlot.displayPrice = 37;
                _saleSlot.RefreshDisplay();
                _readability.RefreshNow();
                int slotIndex = Array.IndexOf(_adapter.RuntimeShopSlots.ToArray(), _saleSlot);
                string stockStatus = _readability.GetSlotStatus(slotIndex);
                Require(stockStatus.Contains(bread.itemName) && stockStatus.Contains("×1") &&
                        stockStatus.Contains("37G") && stockStatus.Contains("품질 125%") &&
                        _alpha.ShopMerchandisingSummary.Contains(bread.itemName) &&
                        _alpha.ShopMerchandisingSummary.Contains("37G"),
                    "stocked item name, count, price and quality remain readable");
                Require(_saleSlot.GetInteractPrompt().Contains(bread.itemName) &&
                        _saleSlot.GetInteractPrompt().Contains("37G"),
                    "existing Space interaction prompt still exposes item and price authority");

                _saleSlot.displayPrice = 43;
                _saleSlot.RefreshDisplay();
                _readability.RefreshNow();
                Require(_readability.GetSlotStatus(slotIndex).Contains("43G") &&
                        _alpha.ShopMerchandisingSummary.Contains("43G"),
                    "player price changes refresh world and HUD merchandising state");

                Transform displayRoot = _adapter.SalesDisplayTarget;
                Vector3 displayBefore = displayRoot.position;
                Transform slotLabel = _readability.GetSlotLabelTransform(slotIndex);
                Require(_alpha.TryMoveSalesDisplay(1, 1, 3, out string moveReason) &&
                        _adapter.SalesDisplayGrid == new Vector2Int(1, 1) &&
                        _adapter.SalesDisplayQuarterTurns == 3 &&
                        Vector3.Distance(displayBefore, displayRoot.position) > 0.1f,
                    $"the same functional display moves and rotates inside its bounded zone ({moveReason})");
                Require(slotLabel != null && slotLabel.parent == _readability.PresentationRoot &&
                        _readability.PresentationRoot.parent == displayRoot &&
                        _saleSlot.transform.parent == displayRoot && !_saleSlot.IsEmpty &&
                        _saleSlot.EffectiveDisplayPrice == 43,
                    "labels, ShopSlot hierarchy, stock and price all follow the moved display");

                Vector3 acceptedPosition = displayRoot.position;
                Quaternion acceptedRotation = displayRoot.rotation;
                Require(!_alpha.TryMoveSalesDisplay(2, 1, 0, out string rejectedReason) &&
                        !string.IsNullOrWhiteSpace(rejectedReason) &&
                        Vector3.Distance(acceptedPosition, displayRoot.position) < 0.001f &&
                        Quaternion.Angle(acceptedRotation, displayRoot.rotation) < 0.01f,
                    "out-of-zone furniture movement is rejected atomically");

                WorldStateSaveData state = _adapter.CaptureWorldState();
                WorldShopFurnitureSaveData furniture = state?.shopFurniture?.SingleOrDefault();
                Require(furniture != null &&
                        furniture.instanceId == WorldGameplayAdapterService.SalesDisplayInstanceId &&
                        furniture.definitionId == WorldGameplayAdapterService.SalesDisplayDefinitionId &&
                        furniture.gridX == 1 && furniture.gridY == 1 &&
                        furniture.rotationQuarterTurns == 3,
                    "existing v11 projection captures the readable display pose without a schema change");

                _saleSlot.displayPrice = 1;
                _saleSlot.RefreshDisplay();
                _readability.RefreshNow();
                _moneyBefore = EconomyService.Instance.Money;
                Require(_adapter.TryOpenShopForNight(2, out string openReason),
                    $"existing day/night gate opens the readable shop ({openReason})");
                Require(_adapter.TryBeginCustomerVisit(_saleSlot, out string customerReason),
                    $"existing NpcController paths to the moved functional display ({customerReason})");
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(1);
                return;
            }

            if (stage == 1)
            {
                if (!_adapter.CustomerPurchaseCompleted)
                {
                    if (_adapter.State == WorldGameplayAdapterState.Failed ||
                        Time.realtimeSinceStartup - _stageStarted >= 28f)
                        throw new InvalidOperationException(
                            $"customer purchase did not complete: {_adapter.LastFailure}");
                    return;
                }

                _readability.RefreshNow();
                int slotIndex = Array.IndexOf(_adapter.RuntimeShopSlots.ToArray(), _saleSlot);
                Require(_saleSlot.IsEmpty && EconomyService.Instance.Money == _moneyBefore + 1,
                    "customer purchase clears stock and deposits the displayed price");
                Require(_readability.GetSlotStatus(slotIndex).Contains("오늘 품절") &&
                        _readability.GetSlotStatus(slotIndex).Contains("다음 상품 진열") &&
                        _alpha.ShopMerchandisingSummary.Contains("0/4칸"),
                    "post-sale sold-out state and next stocking action are immediately readable");
                Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                    "blocking runtime Console Error/Exception/Assert count is 0");
                Require(!SceneManager.GetActiveScene().isDirty,
                    "BETA-004 remains runtime-only and leaves WorldSandbox scene clean");
                Debug.Log("[BETA-004] PLAY_MODE_PASS slots=4 labels=5 stock=true price=true " +
                          "quality=true move=true saveProjection=v11 customerSale=true console=0");
                EditorApplication.update -= ValidateRuntime;
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception ex)
        {
            EditorApplication.update -= ValidateRuntime;
            Fail(ex);
        }
    }

    static void ResolveRuntime()
    {
        _alpha = WorldAlphaPlayableController.Instance ??
                 UnityEngine.Object.FindFirstObjectByType<WorldAlphaPlayableController>();
        _adapter = WorldGameplayAdapterService.Instance ??
                   UnityEngine.Object.FindFirstObjectByType<WorldGameplayAdapterService>();
        _readability = _adapter?.SalesDisplayReadability;
    }

    static void SetStage(int stage)
    {
        SessionState.SetInt(StageKey, stage);
        SessionState.SetInt(FrameKey, 0);
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(ActiveKey, false) ||
            (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
        SessionState.SetInt(ConsoleErrorKey, SessionState.GetInt(ConsoleErrorKey, 0) + 1);
    }

    static void Fail(Exception ex)
    {
        SessionState.SetBool(FailedKey, true);
        Debug.LogError($"[BETA-004] FAIL {ex.Message}\n{ex}");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else Finish();
    }

    static void Finish()
    {
        EditorApplication.update -= ValidateRuntime;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        bool failed = SessionState.GetBool(FailedKey, false) ||
                      SessionState.GetInt(ConsoleErrorKey, 0) != 0;
        int errors = SessionState.GetInt(ConsoleErrorKey, 0);
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(FailedKey);
        SessionState.EraseInt(ConsoleErrorKey);
        SessionState.EraseInt(FrameKey);
        SessionState.EraseInt(StageKey);
        Debug.Log(failed
            ? $"[BETA-004] FINISHED_WITH_ERRORS consoleErrors={errors}"
            : "[BETA-004] FINISHED_PASS readability=true merchandising=true " +
              "movableFurniture=true customerSale=true console=0");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[BETA-004] PASS {message}");
    }
}

public static class PA_Beta005CustomerStrategyValidator
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.BETA005.Active";
    const string FailedKey = "PA.BETA005.Failed";
    const string ConsoleErrorKey = "PA.BETA005.ConsoleErrors";
    const string FrameKey = "PA.BETA005.Frames";
    const string StageKey = "PA.BETA005.Stage";

    static WorldAlphaPlayableController _alpha;
    static WorldGameplayAdapterService _adapter;
    static CustomerPreferencePresentationController _preference;
    static PurchaseFeedbackPresentationController _feedback;
    static CustomerDemandInsightController _demand;
    static ShopSlot _saleSlot;
    static NpcProfile _miner;
    static NpcProfile _tailor;
    static int _moneyBefore;
    static int _purchasesBefore;
    static int _rejectionsBefore;
    static float _stageStarted;
    static bool _minerPreferenceObserved;
    static bool _tailorPreferenceObserved;

    [InitializeOnLoadMethod]
    static void ResumeAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        Subscribe();
        if (EditorApplication.isPlaying)
        {
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
    }

    [MenuItem("Project PA/Beta/BETA-005/Validate Customer Strategy and Feedback")]
    public static void RunBeta005Validation() => RunInternal();

    public static void RunBeta005ValidationBatch() => RunInternal();

    static void RunInternal()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetInt(ConsoleErrorKey, 0);
            SessionState.SetInt(FrameKey, 0);
            SessionState.SetInt(StageKey, 0);
            _minerPreferenceObserved = false;
            _tailorPreferenceObserved = false;
            Subscribe();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && !scene.isDirty,
                "WorldSandbox opens saved and clean");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void Subscribe()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        Application.logMessageReceived += OnLogMessage;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SetStage(0);
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            Finish();
        }
    }

    static void ValidateRuntime()
    {
        if (!EditorApplication.isPlaying) return;
        int frames = SessionState.GetInt(FrameKey, 0) + 1;
        SessionState.SetInt(FrameKey, frames);
        int stage = SessionState.GetInt(StageKey, 0);
        // Only runtime bootstrap needs the initial frame cushion. Customer stages
        // must observe from their first update because the real NpcController can
        // walk, browse and decide before four editor updates have elapsed.
        if (stage == 0 && frames < 4) return;

        try
        {
            ResolveRuntime();
            if (_adapter == null || !_adapter.IsReady || _alpha == null || !_alpha.IsReady ||
                _preference == null || _feedback == null || _demand == null)
            {
                if (frames > 300)
                    throw new TimeoutException("WorldSandbox customer presentation authorities did not initialize.");
                return;
            }

            if (stage == 0)
            {
                Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
                    $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
                Require(_alpha.BeginNewGame() && _alpha.PlayerFacingHudVisible,
                    "the real player session exposes the player-facing WorldSandbox HUD");
                _miner = Resources.Load<NpcProfile>("NPCs/Profile_Miner");
                _tailor = Resources.Load<NpcProfile>("NPCs/Profile_Tailor");
                Require(_miner != null && _tailor != null &&
                        _miner.priceSensitivity >= 1.25f && _tailor.priceSensitivity <= 0.75f,
                    "existing Miner and Tailor profiles provide honest contrasting price sensitivity");
                Require(CustomerPreferencePresentationController.DescribePreference(_miner)
                            .Contains("가격에 민감") &&
                        CustomerPreferencePresentationController.DescribePreference(_tailor)
                            .Contains("가격에 관대"),
                    "existing profile traits produce distinct player-facing strategy hints");

                _saleSlot = _adapter.RuntimeShopSlots.First();
                ValidateCategoryResponse(_saleSlot, _miner);

                foreach (ShopSlot slot in _adapter.RuntimeShopSlots)
                {
                    slot.currentItem = null;
                    slot.displayPrice = 0;
                    slot.RefreshDisplay();
                }
                StockPlank(_saleSlot, 250);
                _moneyBefore = EconomyService.Instance.Money;
                SalesLogManager.DailyDecisionStats before =
                    SalesLogManager.Instance.GetDailyDecisionStats(2);
                _purchasesBefore = before.purchases;
                _rejectionsBefore = before.rejections;
                Require(_adapter.TryOpenShopForNight(2, out string openReason),
                    $"existing night-shop gate opens ({openReason})");
                Require(_adapter.TryBeginCustomerVisit(_saleSlot, out string customerReason) &&
                        _adapter.RuntimeCustomer.profile == _miner,
                    $"first visit uses the existing price-sensitive Miner profile ({customerReason})");
                ObserveLivePreferenceImmediately(_miner, ref _minerPreferenceObserved);
                Require(_minerPreferenceObserved,
                    "the live player HUD exposes the Miner preference before the real visit can end");
                Require(_adapter.CustomerStrategySummary.Contains("Miner_01") &&
                        _adapter.CustomerStrategySummary.Contains("가격에 민감") &&
                        HasCustomerLabel("Miner_01", "가격에 민감"),
                    "world label and player HUD explain the active customer's real strategy");
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(1);
                return;
            }

            if (stage == 1)
            {
                ObservePreference(_miner, ref _minerPreferenceObserved);
                if (!_adapter.CustomerDecisionCompleted)
                {
                    if (_adapter.State == WorldGameplayAdapterState.Failed ||
                        Time.realtimeSinceStartup - _stageStarted >= 28f)
                        throw new InvalidOperationException(
                            $"price-sensitive rejection did not complete: {_adapter.LastFailure}");
                    return;
                }

                SalesLogManager.DailyDecisionStats afterReject =
                    SalesLogManager.Instance.GetDailyDecisionStats(2);
                Require(_adapter.CustomerDeclined && _minerPreferenceObserved,
                    "the visible Miner preference remains readable during the actual visit");
                Require(!_saleSlot.IsEmpty && _saleSlot.currentItem.count == 1 &&
                        _saleSlot.displayPrice == 250 && EconomyService.Instance.Money == _moneyBefore,
                    "an unaffordable rejection preserves stock and money without a transaction");
                Require(afterReject.rejections == _rejectionsBefore + 1 &&
                        afterReject.purchases == _purchasesBefore,
                    "existing SalesLogManager records one real rejection and no purchase");
                string rejectDemandSummary = _demand.GetTopCategorySummary();
                Debug.Log("[BETA-005] REJECTION_SURFACES " +
                          $"feedbackLast=\"{EvidenceText(_feedback.LastReactionLine)}\" " +
                          $"feedbackCurrent=\"{EvidenceText(_feedback.CurrentFeedbackText)}\" " +
                          $"demandTop=\"{EvidenceText(rejectDemandSummary)}\" " +
                          $"demandCurrent=\"{EvidenceText(_demand.CurrentInsightText)}\" " +
                          $"hud=\"{EvidenceText(_alpha.CustomerStrategySummary)}\"");
                Require(_feedback.LastReactionLine.Contains("Miner_01") &&
                        _feedback.CurrentFeedbackText.Contains("Miner_01"),
                    "existing purchase feedback identifies the rejecting Miner");
                Require(rejectDemandSummary.Contains("Processed: 0/1 bought"),
                    "the existing demand authority records the Processed rejection");
                Require(_alpha.CustomerStrategySummary.Contains("보류"),
                    "the player-facing WorldSandbox HUD explains the rejection outcome");

                _adapter.StopCustomer();
                StockPlank(_saleSlot, 1);
                Require(_adapter.TryBeginCustomerVisit(_saleSlot, out string customerReason) &&
                        _adapter.RuntimeCustomer.profile == _tailor,
                    $"second visit uses the existing price-tolerant Tailor profile ({customerReason})");
                ObserveLivePreferenceImmediately(_tailor, ref _tailorPreferenceObserved);
                Require(_tailorPreferenceObserved,
                    "the live player HUD exposes the Tailor preference before the real visit can end");
                Require(_adapter.CustomerStrategySummary.Contains("Tailor_01") &&
                        _adapter.CustomerStrategySummary.Contains("가격에 관대"),
                    "the next customer's contrasting strategy is visible before evaluation");
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(2);
                return;
            }

            if (stage == 2)
            {
                ObservePreference(_tailor, ref _tailorPreferenceObserved);
                if (!_adapter.CustomerDecisionCompleted)
                {
                    if (_adapter.State == WorldGameplayAdapterState.Failed ||
                        Time.realtimeSinceStartup - _stageStarted >= 28f)
                        throw new InvalidOperationException(
                            $"price-tolerant purchase did not complete: {_adapter.LastFailure}");
                    return;
                }

                SalesLogManager.DailyDecisionStats afterBuy =
                    SalesLogManager.Instance.GetDailyDecisionStats(2);
                Require(_adapter.CustomerPurchaseCompleted && _tailorPreferenceObserved,
                    "the visible Tailor preference remains readable during the actual visit");
                Require(_saleSlot.IsEmpty && EconomyService.Instance.Money == _moneyBefore + 1,
                    "the affordable strategy completes the existing ShopSlot and Economy transaction");
                Require(afterBuy.rejections == _rejectionsBefore + 1 &&
                        afterBuy.purchases == _purchasesBefore + 1 &&
                        afterBuy.evaluations == _purchasesBefore + _rejectionsBefore + 2,
                    "the same shop session records one rejection and one purchase");
                Require(_feedback.LastReactionLine.Contains("Tailor_01") &&
                        _feedback.CurrentFeedbackText.Contains("Miner_01") &&
                        _feedback.CurrentFeedbackText.Contains("Tailor_01") &&
                        _demand.GetTopCategorySummary().Contains("1/2 bought") &&
                        _alpha.CustomerStrategySummary.Contains("구매") &&
                        _alpha.CustomerStrategySummary.Contains("+1G"),
                    "existing feedback and demand surfaces turn both decisions into next-stock strategy");
                Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                    "blocking runtime Console Error/Exception/Assert count is 0");
                Require(!SceneManager.GetActiveScene().isDirty,
                    "BETA-005 remains runtime-only and leaves WorldSandbox scene clean");
                Debug.Log("[BETA-005] PLAY_MODE_PASS profiles=Miner+Tailor reject=true " +
                          "purchase=true stockAtomic=true money=1G preference=true " +
                          "feedback=true demand=true console=0");
                EditorApplication.update -= ValidateRuntime;
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception ex)
        {
            EditorApplication.update -= ValidateRuntime;
            Fail(ex);
        }
    }

    static void ResolveRuntime()
    {
        _alpha = WorldAlphaPlayableController.Instance ??
                 UnityEngine.Object.FindFirstObjectByType<WorldAlphaPlayableController>();
        _adapter = WorldGameplayAdapterService.Instance ??
                   UnityEngine.Object.FindFirstObjectByType<WorldGameplayAdapterService>();
        _preference = CustomerPreferencePresentationController.Instance ??
                      UnityEngine.Object.FindFirstObjectByType<CustomerPreferencePresentationController>();
        _feedback = PurchaseFeedbackPresentationController.Instance ??
                    UnityEngine.Object.FindFirstObjectByType<PurchaseFeedbackPresentationController>();
        _demand = CustomerDemandInsightController.Instance ??
                  UnityEngine.Object.FindFirstObjectByType<CustomerDemandInsightController>();
    }

    static void StockPlank(ShopSlot slot, int price)
    {
        Item plank = Resources.Load<Item>("Items/Item_Plank");
        if (slot == null || plank == null)
            throw new InvalidOperationException("Existing Plank or B01 ShopSlot is unavailable.");
        slot.currentItem = new ItemInstance(plank, 1) { quality = 1f, currentPrice = plank.basePrice };
        slot.displayPrice = price;
        slot.RefreshDisplay();
    }

    static void ValidateCategoryResponse(ShopSlot slot, NpcProfile profile)
    {
        Item plank = Resources.Load<Item>("Items/Item_Plank");
        Item clothes = Resources.Load<Item>("Items/Item_13_Clothes");
        if (slot == null || plank == null || clothes == null)
            throw new InvalidOperationException(
                "Existing Processed and Luxury category fixtures are unavailable.");

        slot.currentItem = new ItemInstance(plank, 1);
        slot.displayPrice = plank.basePrice;
        PurchaseEvaluator.Result processed = PurchaseEvaluator.Evaluate(
            profile, slot, new System.Random(505));
        slot.currentItem = new ItemInstance(clothes, 1);
        slot.displayPrice = clothes.basePrice;
        PurchaseEvaluator.Result luxury = PurchaseEvaluator.Evaluate(
            profile, slot, new System.Random(505));

        Require(Mathf.Abs(processed.probability - luxury.probability) > 0.001f,
            "the existing PurchaseEvaluator responds differently to Processed and Luxury categories");
    }

    static void ObservePreference(NpcProfile profile, ref bool observed)
    {
        if (observed || _adapter?.RuntimeCustomer == null ||
            _adapter.RuntimeCustomer.currentState == NpcController.State.Idle) return;
        _preference.RefreshNow();
        observed = _preference.CurrentPreferenceText.Contains(profile.npcName) &&
                   _preference.CurrentPreferenceText.Contains(
                       CustomerPreferencePresentationController.DescribePreference(profile));
    }

    // The BETA player surface is the WorldSandbox IMGUI HUD. The older
    // CustomerPreferenceCanvas is intentionally a development overlay, so it is
    // logged for diagnosis but never used as the player-visibility authority.
    static void ObserveLivePreferenceImmediately(NpcProfile profile, ref bool observed)
    {
        NpcController customer = _adapter?.RuntimeCustomer;
        GameObject customerObject = customer != null ? customer.gameObject : null;
        string expectedPreference =
            CustomerPreferencePresentationController.DescribePreference(profile);
        string playerText = _alpha?.CustomerStrategySummary ?? string.Empty;
        Rect hudBounds = _alpha != null ? _alpha.PlayerFacingHudScreenRect : default;
        Rect screenBounds = new Rect(0f, 0f, Screen.width, Screen.height);

        bool identityMatches = customer != null && customer.profile == profile;
        bool visitActive = _adapter != null &&
                           _adapter.State == WorldGameplayAdapterState.CustomerMoving;
        bool customerActiveSelf = customerObject != null && customerObject.activeSelf;
        bool customerActiveInHierarchy = customerObject != null && customerObject.activeInHierarchy;
        bool textMatches = profile != null &&
                           playerText.Contains(profile.npcName) &&
                           playerText.Contains(expectedPreference);
        bool hudVisible = _alpha != null && _alpha.PlayerFacingHudVisible;
        bool hudHasBounds = hudBounds.width > 0f && hudBounds.height > 0f;
        bool hudOnScreen = hudHasBounds && screenBounds.Overlaps(hudBounds, true);

        TMP_Text legacyText = _preference != null ? _preference.preferenceText : null;
        Canvas legacyCanvas = legacyText != null ? legacyText.canvas : null;
        GameObject legacyUiObject = legacyText != null ? legacyText.gameObject : null;
        CanvasGroup legacyGroup = legacyCanvas != null
            ? legacyCanvas.GetComponent<CanvasGroup>()
            : null;
        RectTransform legacyRect = legacyText != null ? legacyText.rectTransform : null;
        bool legacyRectOnScreen = IsRectTransformOnScreen(legacyRect, legacyCanvas);
        Camera mainCamera = Camera.main;

        observed = identityMatches && visitActive && customerActiveSelf &&
                   customerActiveInHierarchy && hudVisible && textMatches && hudOnScreen;

        Debug.Log(
            $"[BETA-005] LIVE_PREFERENCE_SAMPLE " +
            $"profile={profile?.npcName ?? "null"} identityMatches={identityMatches} " +
            $"adapterState={_adapter?.State.ToString() ?? "null"} visitActive={visitActive} " +
            $"npcState={customer?.currentState.ToString() ?? "null"} " +
            $"customerActiveSelf={customerActiveSelf} " +
            $"customerActiveInHierarchy={customerActiveInHierarchy} " +
            $"playerHudVisible={hudVisible} playerText=\"{EvidenceText(playerText)}\" " +
            $"hudScreenBounds={FormatRect(hudBounds)} screenVisible={hudOnScreen} " +
            $"visibilityBasis=ScreenSpaceIMGUI camera={mainCamera?.name ?? "none-required"} " +
            $"legacyUiActiveSelf={legacyUiObject != null && legacyUiObject.activeSelf} " +
            $"legacyUiActiveInHierarchy={legacyUiObject != null && legacyUiObject.activeInHierarchy} " +
            $"legacyCanvasEnabled={legacyCanvas != null && legacyCanvas.enabled} " +
            $"legacyCanvasGroupAlpha={(legacyGroup != null ? legacyGroup.alpha : 1f):0.###} " +
            $"legacyPreferenceText=\"{EvidenceText(legacyText != null ? legacyText.text : string.Empty)}\" " +
            $"legacyRectBounds={FormatRect(legacyRect != null ? legacyRect.rect : default)} " +
            $"legacyScreenVisible={legacyRectOnScreen} observed={observed}");
    }

    static bool IsRectTransformOnScreen(RectTransform rect, Canvas canvas)
    {
        if (rect == null || canvas == null || !canvas.enabled) return false;
        CanvasGroup group = canvas.GetComponent<CanvasGroup>();
        if (group != null && group.alpha <= 0.001f) return false;

        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 max = min;
        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        return new Rect(min, max - min).Overlaps(
            new Rect(0f, 0f, Screen.width, Screen.height), true);
    }

    static string EvidenceText(string value)
    {
        return (value ?? string.Empty).Replace("\r", string.Empty).Replace("\n", " / ");
    }

    static string FormatRect(Rect value)
    {
        return $"({value.x:0.#},{value.y:0.#},{value.width:0.#},{value.height:0.#})";
    }

    static bool HasCustomerLabel(string name, string preference)
    {
        return _adapter?.RuntimeCustomer != null &&
               _adapter.RuntimeCustomer.GetComponentsInChildren<PrototypeWorldLabel>(true)
                   .Any(label => label != null && label.label.Contains(name) &&
                                 label.label.Contains(preference));
    }

    static void SetStage(int stage)
    {
        SessionState.SetInt(StageKey, stage);
        SessionState.SetInt(FrameKey, 0);
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(ActiveKey, false) ||
            (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
        SessionState.SetInt(ConsoleErrorKey, SessionState.GetInt(ConsoleErrorKey, 0) + 1);
    }

    static void Fail(Exception ex)
    {
        SessionState.SetBool(FailedKey, true);
        Debug.LogError($"[BETA-005] FAIL {ex.Message}\n{ex}");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else Finish();
    }

    static void Finish()
    {
        EditorApplication.update -= ValidateRuntime;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        bool failed = SessionState.GetBool(FailedKey, false) ||
                      SessionState.GetInt(ConsoleErrorKey, 0) != 0;
        int errors = SessionState.GetInt(ConsoleErrorKey, 0);
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(FailedKey);
        SessionState.EraseInt(ConsoleErrorKey);
        SessionState.EraseInt(FrameKey);
        SessionState.EraseInt(StageKey);
        Debug.Log(failed
            ? $"[BETA-005] FINISHED_WITH_ERRORS consoleErrors={errors}"
            : "[BETA-005] FINISHED_PASS customerStrategy=true reject=true " +
              "purchase=true feedback=true demand=true console=0");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[BETA-005] PASS {message}");
    }
}

public static class PA_Beta006PhoneHiringFeedValidator
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.BETA006.Active";
    const string FailedKey = "PA.BETA006.Failed";
    const string ConsoleErrorKey = "PA.BETA006.ConsoleErrors";
    const string FrameKey = "PA.BETA006.Frames";
    const string StageKey = "PA.BETA006.Stage";

    static WorldAlphaPlayableController _alpha;
    static WorldGameplayAdapterService _adapter;
    static SmartphoneUI _phone;
    static HiringUI _hiringUi;
    static FeedUI _feedUi;
    static NpcCandidateData _candidate;
    static int _moneyBeforeHire;
    static int _hiredBefore;
    static int _moneyBeforeSale;
    static int _expectedSalePrice;
    static string _saleItemName;
    static float _stageStarted;

    [InitializeOnLoadMethod]
    static void ResumeAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        Subscribe();
        if (EditorApplication.isPlaying)
        {
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
    }

    [MenuItem("Project PA/Beta/BETA-006/Validate Phone Hiring and Feed")]
    public static void RunBeta006Validation() => RunInternal();

    public static void RunBeta006ValidationBatch() => RunInternal();

    static void RunInternal()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetInt(ConsoleErrorKey, 0);
            SessionState.SetInt(FrameKey, 0);
            SessionState.SetInt(StageKey, 0);
            Subscribe();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && !scene.isDirty,
                "WorldSandbox opens saved and clean");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }

    static void Subscribe()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        Application.logMessageReceived += OnLogMessage;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SetStage(0);
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            Finish();
        }
    }

    static void ValidateRuntime()
    {
        if (!EditorApplication.isPlaying) return;
        int frames = SessionState.GetInt(FrameKey, 0) + 1;
        SessionState.SetInt(FrameKey, frames);
        int stage = SessionState.GetInt(StageKey, 0);
        if (stage == 0 && frames < 5) return;

        try
        {
            ResolveRuntime();
            if (_alpha == null || !_alpha.IsReady || _adapter == null || !_adapter.IsReady ||
                _phone == null || HiringService.Instance == null || EconomyService.Instance == null)
            {
                if (frames > 360)
                    throw new TimeoutException("WorldSandbox phone/hiring authorities did not initialize.");
                return;
            }

            if (stage == 0)
            {
                Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
                    $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
                Require(_alpha.BeginNewGame() && _alpha.PlayerFacingHudVisible &&
                        _alpha.PlayerControlHint.Contains("P") &&
                        _alpha.PlayerControlHint.Contains("휴대폰"),
                    "the real player session explains the P-key Phone entry point");
                Require(EventSystem.current != null &&
                        EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() != null,
                    "WorldSandbox has a clickable Input System EventSystem");
                Require(_phone.tabPanels != null && _phone.tabPanels.Length == 4 &&
                        _phone.tabButtons != null && _phone.tabButtons.Length == 4,
                    "the runtime Phone exposes Audit, Hiring, Feed and Settings tabs");

                _phone.Toggle();
                Require(_phone.IsOpen,
                    "the same public toggle subscribed to P opens the Phone");
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(1);
                return;
            }

            if (stage == 1)
            {
                if (Time.realtimeSinceStartup - _stageStarted < 0.4f) return;
                Canvas.ForceUpdateCanvases();
                Require(IsRectOnScreen(_phone.root) &&
                        _phone.root.rect.width > 0f && _phone.root.rect.height > 0f,
                    "the opened Phone has non-zero bounds inside the Game view");

                _phone.SelectTab(2);
                ResolveRuntime();
                Require(_feedUi != null && _phone.CurrentTabIndex == 2 &&
                        IsOnlyPanelActive(2),
                    "the real Feed tab opens exclusively");
                _feedUi.Refresh();
                Require(_feedUi.HasVisibleEmptyState && _feedUi.VisibleSaleCardCount == 0 &&
                        ContainsVisibleText(_feedUi.gameObject, "아직 판매 기록"),
                    "Feed presents an honest pre-sale empty state");

                _phone.SelectTab(1);
                ResolveRuntime();
                SetStage(2);
                return;
            }

            if (stage == 2)
            {
                Require(_hiringUi != null && _phone.CurrentTabIndex == 1 &&
                        IsOnlyPanelActive(1),
                    "the real Hiring tab opens exclusively");

                HiringService hiring = HiringService.Instance;
                List<NpcCandidateData> candidates = hiring.availableCandidates
                    .Where(value => value != null)
                    .OrderBy(value => value.requiredTier)
                    .ThenBy(value => value.hireCost)
                    .ThenBy(value => value.name)
                    .ToList();
                _hiringUi.Refresh();
                Canvas.ForceUpdateCanvases();
                Require(candidates.Count == 8 && _hiringUi.VisibleCardCount == candidates.Count,
                    "all eight authoritative candidates have visible cards");
                Require(candidates.All(HasRealRoleTemplate),
                    "every candidate uses an existing C-02..C-09 SkinnedMesh role template");
                Require(HiringMaskIsVisible() && candidates.All(CandidateCardHasContentAndBounds),
                    "candidate cards expose name, role, cost and non-zero masked layout bounds");

                _candidate = candidates.First();
                EconomyService.Instance.ForceSet(0, "BETA-006 insufficient-funds fixture");
                _hiringUi.Refresh();
                Require(_candidate != null &&
                        !hiring.CanHire(_candidate, out string insufficientReason) &&
                        insufficientReason.Contains("부족") &&
                        TryGetCandidateButton(_candidate, out Button blockedButton) &&
                        !blockedButton.interactable &&
                        EconomyService.Instance.Money == 0 && hiring.HiredCount == 0,
                    "insufficient funds keep the card visible and block spending/hiring");

                Require(EconomyService.Instance.Deposit(_candidate.hireCost + 125,
                        "BETA-006 earned hiring funds fixture"),
                    "funds enter through the existing EconomyService authority");
                _hiringUi.Refresh();
                _moneyBeforeHire = EconomyService.Instance.Money;
                _hiredBefore = hiring.HiredCount;
                bool hasHireButton = TryGetCandidateButton(_candidate, out Button hireButton);
                Require(hiring.CanHire(_candidate, out string readyReason) &&
                        string.IsNullOrEmpty(readyReason) &&
                        hasHireButton &&
                        hireButton.interactable,
                    "an affordable candidate exposes an actionable real Hire button");
                hireButton.onClick.Invoke();
                SetStage(3);
                return;
            }

            if (stage == 3)
            {
                HiringService hiring = HiringService.Instance;
                HiringService.HiredNpcRuntimeRecord hired = hiring.GetHiredRuntimeRecords()
                    .FirstOrDefault(record => record.Candidate == _candidate);
                GameObject instance = hired.Instance;
                Require(hiring.HiredCount == _hiredBefore + 1 && hiring.IsHired(_candidate) &&
                        EconomyService.Instance.Money == _moneyBeforeHire - _candidate.hireCost,
                    "UI hiring spends the exact candidate cost and increments the real roster once");
                Require(instance != null && instance.activeInHierarchy &&
                        instance.GetComponent<NpcController>()?.profile == _candidate.profile &&
                        instance.GetComponentInChildren<SkinnedMeshRenderer>(true) != null &&
                        HasExpectedRuntimeRole(instance, _candidate.specialty),
                    "the hired resident keeps the candidate identity, SkinnedMesh and exact role controller");
                Require(!hiring.CanHire(_candidate, out string duplicateReason) &&
                        duplicateReason.Contains("이미") &&
                        TryGetCandidateButton(_candidate, out Button hiredButton) &&
                        !hiredButton.interactable &&
                        ContainsVisibleText(hiredButton.gameObject, "고용됨") &&
                        _hiringUi.CurrentStatusText.Contains("고용 1/8") &&
                        _hiringUi.CurrentFeedbackText.Contains("고용 완료"),
                    "the after-hire card, roster and feedback prevent duplicate hiring visibly");

                _phone.SelectTab(2);
                ResolveRuntime();
                Require(_feedUi != null && _feedUi.HasVisibleEmptyState,
                    "Feed remains empty before the first actual ShopSlot sale");
                ShopSlot slot = _adapter.RuntimeShopSlots.FirstOrDefault();
                Item plank = Resources.Load<Item>("Items/Item_Plank");
                Require(slot != null && plank != null, "existing ShopSlot and Plank sale data are available");
                _saleItemName = plank.itemName;
                slot.currentItem = new ItemInstance(plank, 1) { quality = 1.25f, currentPrice = plank.basePrice };
                _expectedSalePrice = Mathf.Max(1, plank.basePrice);
                slot.displayPrice = _expectedSalePrice;
                slot.RefreshDisplay();
                _moneyBeforeSale = EconomyService.Instance.Money;
                Require(slot.TryPurchaseByNpc(_candidate.ResolveDisplayName(), out int paid) &&
                        paid == _expectedSalePrice,
                    "an actual ShopSlot transaction records the first Feed sale");
                SetStage(4);
                return;
            }

            if (stage == 4)
            {
                Canvas.ForceUpdateCanvases();
                Require(EconomyService.Instance.Money == _moneyBeforeSale + _expectedSalePrice &&
                        _feedUi.VisibleSaleCardCount == 1 && !_feedUi.HasVisibleEmptyState,
                    "the open Feed refreshes immediately after the successful sale");
                GameObject feedCard = FindActiveDescendant(_feedUi.transform, "FeedCard");
                string feedText = CollectText(feedCard);
                Require(feedCard != null && ((RectTransform)feedCard.transform).rect.height > 0f &&
                        feedText.Contains(_saleItemName) && feedText.Contains($"{_expectedSalePrice:N0} G") &&
                        feedText.Contains(_candidate.ResolveDisplayName()) && feedText.Contains("Day") &&
                        feedText.Contains("가공품") && feedText.Contains("마을 방향") &&
                        _feedUi.CurrentVillageSummary.Contains("Processed"),
                    "the Feed card shows real item, price, buyer, time, category and village direction");

                _phone.SelectTab(0);
                AuditResultUI auditUi = UnityEngine.Object.FindFirstObjectByType<AuditResultUI>();
                auditUi?.Refresh();
                Require(auditUi != null && IsOnlyPanelActive(0) &&
                        AuditService.Instance != null && AuditService.Instance.CurrentHiredCount == 1 &&
                        auditUi.auditRequirementsText != null &&
                        auditUi.auditRequirementsText.text.Contains("고용") &&
                        auditUi.auditRequirementsText.text.Contains("1/") &&
                        auditUi.facilityDirectionText != null &&
                        auditUi.facilityDirectionText.text.Contains("가공"),
                    "Audit preserves hired-roster and sale-driven facility direction feedback");

                _phone.SelectTab(3);
                SettingsUI settings = UnityEngine.Object.FindFirstObjectByType<SettingsUI>();
                Require(settings != null && IsOnlyPanelActive(3) &&
                        settings.bgmSlider != null && settings.sfxSlider != null &&
                        settings.GetComponentsInChildren<Button>(true).Length >= 2,
                    "Settings preserves BGM, SFX, Save and Load controls");

                _phone.ReturnToHome();
                Require(_phone.homeScreen != null && _phone.homeScreen.activeInHierarchy &&
                        !_phone.tabPanels.Any(panel => panel != null && panel.activeSelf),
                    "Phone returns to a populated home menu without empty tabs");
                _phone.Close();
                Require(!_phone.IsOpen, "Phone closes through its player-facing navigation");
                Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                    "blocking runtime Console Error/Exception/Assert count is 0");
                Require(!SceneManager.GetActiveScene().isDirty,
                    "BETA-006 remains runtime-only and leaves WorldSandbox scene clean");
                Debug.Log("[BETA-006] PLAY_MODE_PASS phone=true candidates=8 hire=true " +
                          "exactCost=true roles=true feedEmpty=true saleFeed=true village=true " +
                          "audit=true settings=true console=0");
                EditorApplication.update -= ValidateRuntime;
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception exception)
        {
            EditorApplication.update -= ValidateRuntime;
            Fail(exception);
        }
    }

    static void ResolveRuntime()
    {
        _alpha = WorldAlphaPlayableController.Instance ??
                 UnityEngine.Object.FindFirstObjectByType<WorldAlphaPlayableController>();
        _adapter = WorldGameplayAdapterService.Instance ??
                   UnityEngine.Object.FindFirstObjectByType<WorldGameplayAdapterService>();
        _phone = SmartphoneUI.instance ?? UnityEngine.Object.FindFirstObjectByType<SmartphoneUI>();
        _hiringUi = UnityEngine.Object.FindFirstObjectByType<HiringUI>();
        _feedUi = UnityEngine.Object.FindFirstObjectByType<FeedUI>();
    }

    static bool HasRealRoleTemplate(NpcCandidateData candidate)
    {
        if (candidate == null || candidate.spawnPrefab == null ||
            candidate.spawnPrefab.GetComponent<NpcController>() == null ||
            candidate.spawnPrefab.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
            return false;
        return HasExpectedRuntimeRole(candidate.spawnPrefab, candidate.specialty);
    }

    static bool HasExpectedRuntimeRole(GameObject resident, NpcSpecialty specialty)
    {
        if (resident == null) return false;
        if (NpcSpecialtyMapping.IsCraftingSpecialty(specialty))
        {
            SpecialistNpcController specialist = resident.GetComponent<SpecialistNpcController>();
            return specialist != null && specialist.specialty == specialty;
        }

        ProducerNpcController producer = resident.GetComponent<ProducerNpcController>();
        return producer != null && producer.specialty == specialty;
    }

    static bool CandidateCardHasContentAndBounds(NpcCandidateData candidate)
    {
        if (!_hiringUi.TryGetCandidateCard(candidate, out GameObject card) ||
            card == null || !card.activeInHierarchy) return false;
        RectTransform rect = card.transform as RectTransform;
        string text = CollectText(card);
        return rect != null && rect.rect.width > 0f && rect.rect.height > 0f &&
               text.Contains(candidate.ResolveDisplayName()) &&
               text.Contains(candidate.hireCost.ToString("N0")) &&
               text.Contains(ResolveRoleLabel(candidate.specialty));
    }

    static bool HiringMaskIsVisible()
    {
        if (_hiringUi?.scrollRect?.viewport == null) return false;
        Image image = _hiringUi.scrollRect.viewport.GetComponent<Image>();
        Mask mask = _hiringUi.scrollRect.viewport.GetComponent<Mask>();
        return image != null && image.color.a > 0.9f && mask != null &&
               !mask.showMaskGraphic && _hiringUi.scrollRect.viewport.rect.width > 0f &&
               _hiringUi.scrollRect.viewport.rect.height > 0f;
    }

    static bool TryGetCandidateButton(NpcCandidateData candidate, out Button button)
    {
        button = null;
        if (_hiringUi == null || !_hiringUi.TryGetCandidateCard(candidate, out GameObject card))
            return false;
        button = card.GetComponentInChildren<Button>(true);
        return button != null;
    }

    static bool IsOnlyPanelActive(int index)
    {
        if (_phone?.tabPanels == null || index < 0 || index >= _phone.tabPanels.Length) return false;
        for (int i = 0; i < _phone.tabPanels.Length; i++)
        {
            GameObject panel = _phone.tabPanels[i];
            if (panel == null || panel.activeSelf != (i == index)) return false;
        }
        return true;
    }

    static bool IsRectOnScreen(RectTransform rect)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy) return false;
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector2 min = corners[0];
        Vector2 max = corners[0];
        for (int i = 1; i < corners.Length; i++)
        {
            min = Vector2.Min(min, corners[i]);
            max = Vector2.Max(max, corners[i]);
        }
        return new Rect(min, max - min).Overlaps(
            new Rect(0f, 0f, Screen.width, Screen.height), true);
    }

    static bool ContainsVisibleText(GameObject root, string value)
    {
        if (root == null || string.IsNullOrEmpty(value)) return false;
        return root.GetComponentsInChildren<TMP_Text>(true)
            .Any(text => text != null && text.gameObject.activeInHierarchy &&
                         text.enabled && text.text.Contains(value));
    }

    static string CollectText(GameObject root)
    {
        if (root == null) return string.Empty;
        return string.Join(" | ", root.GetComponentsInChildren<TMP_Text>(true)
            .Where(text => text != null && text.gameObject.activeInHierarchy && text.enabled)
            .Select(text => text.text));
    }

    static GameObject FindActiveDescendant(Transform root, string objectName)
    {
        if (root == null) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child != null && child.name == objectName && child.gameObject.activeInHierarchy)
                return child.gameObject;
        return null;
    }

    static string ResolveRoleLabel(NpcSpecialty specialty)
    {
        return specialty switch
        {
            NpcSpecialty.Farmer => "농부",
            NpcSpecialty.Miner => "광부",
            NpcSpecialty.Lumberjack => "벌목꾼",
            NpcSpecialty.Fisher => "어부",
            NpcSpecialty.Chef => "요리사",
            NpcSpecialty.Blacksmith => "대장장이",
            NpcSpecialty.Tailor => "재단사",
            NpcSpecialty.Carpenter => "목수",
            _ => "주민"
        };
    }

    static void SetStage(int stage)
    {
        SessionState.SetInt(StageKey, stage);
        SessionState.SetInt(FrameKey, 0);
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(ActiveKey, false) ||
            (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
        SessionState.SetInt(ConsoleErrorKey, SessionState.GetInt(ConsoleErrorKey, 0) + 1);
    }

    static void Fail(Exception exception)
    {
        SessionState.SetBool(FailedKey, true);
        Debug.LogError($"[BETA-006] FAIL {exception.Message}\n{exception}");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else Finish();
    }

    static void Finish()
    {
        EditorApplication.update -= ValidateRuntime;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        bool failed = SessionState.GetBool(FailedKey, false) ||
                      SessionState.GetInt(ConsoleErrorKey, 0) != 0;
        int errors = SessionState.GetInt(ConsoleErrorKey, 0);
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(FailedKey);
        SessionState.EraseInt(ConsoleErrorKey);
        SessionState.EraseInt(FrameKey);
        SessionState.EraseInt(StageKey);
        Debug.Log(failed
            ? $"[BETA-006] FINISHED_WITH_ERRORS consoleErrors={errors}"
            : "[BETA-006] FINISHED_PASS phone=true hire=true feed=true village=true " +
              "audit=true settings=true console=0");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[BETA-006] PASS {message}");
    }
}
#endif
