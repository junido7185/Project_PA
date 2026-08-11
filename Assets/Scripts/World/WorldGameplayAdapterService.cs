using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
#endif

public enum WorldGameplayAdapterState
{
    Bootstrapping = 0,
    Ready = 1,
    CustomerMoving = 2,
    CustomerPurchased = 3,
    Failed = 4
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

    const string MarketBuildingResource = "Buildings/Building_B01_MarketStall";
    const string WorkbenchBuildingResource = "Buildings/Building_B05_Workbench";
    const string PlankRecipeResource = "Recipes/Recipe_Plank";
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
    GameObject _customerRoot;
    Inventory _inventory;
    Shop _shop;
    ShopSlot[] _shopSlots = Array.Empty<ShopSlot>();
    Workbench _workbench;
    EconomyService _economy;
    GameClock _clock;
    DayNightShopLoopController _dayLoop;
    SaveManager _saveManager;
    NpcController _customer;
    ShopSlot _customerTargetSlot;
    PlayerInteraction _playerInteraction;
    Hotbar _playerHotbar;
    DaytimeStockPrepPoint[] _daytimeActivityPoints = Array.Empty<DaytimeStockPrepPoint>();
    FarmPlotInteraction[] _farmPlots = Array.Empty<FarmPlotInteraction>();
    Transform _salesDisplayRoot;
    Vector3 _salesDisplayBaseLocalPosition;
    Quaternion _salesDisplayBaseLocalRotation;
    int _salesDisplayGridX;
    int _salesDisplayGridY;
    int _salesDisplayQuarterTurns;
    int _moneyBeforeCustomer;
    float _customerStartedAt;
    long _boundSeed = long.MinValue;
    string _lastAction = "World gameplay adapter is bootstrapping.";
    string _lastFailure = string.Empty;

    public static WorldGameplayAdapterService Instance { get; private set; }
    public WorldGameplayAdapterState State { get; private set; } =
        WorldGameplayAdapterState.Bootstrapping;
    public bool IsReady => State == WorldGameplayAdapterState.Ready ||
                           State == WorldGameplayAdapterState.CustomerMoving ||
                           State == WorldGameplayAdapterState.CustomerPurchased;
    public string LastAction => _lastAction;
    public string LastFailure => _lastFailure;
    public long BoundSeed => _boundSeed;
    public GameObject RuntimeRoot => _runtimeRoot;
    public GameObject PlayerRoot => _playerRoot;
    public Inventory PlayerInventory => _inventory;
    public Shop RuntimeShop => _shop;
    public IReadOnlyList<ShopSlot> RuntimeShopSlots => _shopSlots;
    public Workbench RuntimeWorkbench => _workbench;
    public NpcController RuntimeCustomer => _customer;
    public SaveManager RuntimeSaveManager => _saveManager;
    public PlayerInteraction PlayerInteraction => _playerInteraction;
    public Hotbar PlayerHotbar => _playerHotbar;
    public IReadOnlyList<DaytimeStockPrepPoint> DaytimeActivityPoints => _daytimeActivityPoints;
    public IReadOnlyList<FarmPlotInteraction> FarmPlots => _farmPlots;
    public bool DaytimeActivitiesBound => _playerInteraction != null && _playerHotbar != null &&
                                          _daytimeActivityPoints.Length >= 4 &&
                                          _farmPlots.Length >= FarmPlotInteraction.RuntimePlotCount;
    public Vector2Int SalesDisplayGrid => new Vector2Int(
        _salesDisplayGridX, _salesDisplayGridY);
    public int SalesDisplayQuarterTurns => _salesDisplayQuarterTurns;
    public bool CustomerPurchaseCompleted => State == WorldGameplayAdapterState.CustomerPurchased;
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

        State = WorldGameplayAdapterState.Ready;
        _lastAction = "Generated resources, B05 crafting, B01 sales, customer, economy and save are connected.";
        Debug.Log("[WORLD-009] RUNTIME_READY authorities=existing seed=9009 shop=B01 workbench=B05");
    }

    void Update()
    {
        if (!IsReady) return;

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
            _lastAction = $"Customer purchase completed through NpcController and ShopSlot (+{_economy.Money - _moneyBeforeCustomer}G).";
            return;
        }

        if (Time.realtimeSinceStartup - _customerStartedAt >= CustomerTimeoutSeconds)
            Fail("Existing NpcController did not complete the shop visit within the bounded timeout.");
    }

    void OnDestroy()
    {
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
        _clock.secondsPerGameHour = 99999f;
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
        _shopSlots = _shopRoot != null
            ? _shopRoot.GetComponentsInChildren<ShopSlot>(true)
                .OrderBy(slot => slot.name, StringComparer.Ordinal).ToArray()
            : Array.Empty<ShopSlot>();
        ResolveSalesDisplayRoot();

        if (_inventory == null || _economy == null || _clock == null || _dayLoop == null ||
            _saveManager == null || ItemRegistry.Instance == null || _shop == null ||
            _shopSlots.Length == 0 || _workbench == null)
        {
            reason = "One or more existing gameplay authorities failed to activate.";
            return false;
        }

        _clock.ForceSet(9f, 1, "BETA-001 WorldSandbox fresh session");
        _dayLoop.SimulatePhaseForValidation(9f, 1);
        AddRuntimeLabel(_playerRoot.transform, "새 생활 시작점", new Color(0.76f, 0.95f, 1f));
        AddRuntimeLabel(_shopRoot.transform, "P.A. 잡화점 · 밤 영업", new Color(1f, 0.88f, 0.48f));
        AddRuntimeLabel(_workbenchRoot.transform, "제작 작업대 · 상품 준비", new Color(0.72f, 1f, 0.72f));
        if (!ConfigurePlayerActivityInteraction(out reason) ||
            !BindDaytimeActivitiesToGeneratedWorld(out reason))
        {
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
        _lastAction = $"Moved the functional B01 sales display to ({gridX},{gridY}) rot {normalized}.";
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
        agent.speed = 12f;
        agent.acceleration = 40f;
        agent.angularSpeed = 720f;
        agent.stoppingDistance = 0.25f;
        _customer = _customerRoot.AddComponent<NpcController>();
        _customer.randomSeed = 9009;
        _customer.idleTickInterval = 999f;
        _customer.browseDurationAtSlot = 0.05f;
        _customer.maxSlotsPerVisit = 1;
        _customer.shopArriveDistance = 1.5f;
        _customer.slotArriveDistance = 0.55f;
        _customerRoot.SetActive(true);
        agent.speed = 12f;
        agent.acceleration = 40f;
        AddRuntimeLabel(_customerRoot.transform, "손님", new Color(1f, 0.72f, 0.8f));

        _customerTargetSlot = targetSlot;
        _moneyBeforeCustomer = _economy != null ? _economy.Money : 0;
        _customerStartedAt = Time.realtimeSinceStartup;
        State = WorldGameplayAdapterState.CustomerMoving;
        StartCoroutine(BeginCustomerVisitNextFrame());
        _lastAction = "Existing NpcController is walking from Start to the B01 shop destination.";
        return true;
    }

    IEnumerator BeginCustomerVisitNextFrame()
    {
        yield return null;
        if (_customer == null || !_customer.TryBeginShoppingVisitAt(_shop.transform))
            Fail("NpcController rejected the generated B01 shop destination.");
    }

    public void StopCustomer()
    {
        if (_customerRoot != null) ReleaseRuntimeObject(_customerRoot);
        _customerRoot = null;
        _customer = null;
        _customerTargetSlot = null;
        if (State == WorldGameplayAdapterState.CustomerMoving ||
            State == WorldGameplayAdapterState.CustomerPurchased)
            State = WorldGameplayAdapterState.Ready;
    }

    void BindRuntimeObjectsToSeed(long seed)
    {
        _generated = WorldIslandGenerator.Generate(seed);
        PositionExistingRuntimeObject(_shopRoot, WorldGenerationAnchorKind.Shop, true);
        PositionExistingRuntimeObject(_workbenchRoot, WorldGenerationAnchorKind.MeadowActivity, false);
        if (_generated.TryGetAnchor(WorldGenerationAnchorKind.Start, out WorldGenerationAnchor start) &&
            _grid.CellToWorld(start.Coordinate, out Vector3 playerPosition) && _playerRoot != null)
        {
            _playerRoot.transform.position = playerPosition + Vector3.up * 0.05f;
        }
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
#endif
