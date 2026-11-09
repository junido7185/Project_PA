using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// P2 shop-interior customization. This is an extension of GridService, Inventory,
// ShopSlot, Workbench and SaveManager; it deliberately does not create parallel gameplay systems.
[DefaultExecutionOrder(80)]
public class ShopCustomizationController : MonoBehaviour
{
    public const string ShopInteriorZoneId = "shop.interior";
    const float PlacementHeight = 0.12f;
    const string DefaultThemeId = "default";
    const string ProcessedWarmThemeId = "processed.warm";
    const string ThemeRecordDefinitionId = "shop.theme";
    const string ThemeRecordInstanceId = "fixed.shop.theme";
    static readonly Vector2Int TierOneZoneSize = new Vector2Int(5, 4);
    static readonly Vector2Int TierTwoZoneSize = new Vector2Int(6, 5);
    static readonly Vector2Int TierThreeZoneSize = new Vector2Int(7, 6);
    static readonly int[] ProgressionDisplayLimits = { 6, 6, 8, 12, 20 };
    static readonly Vector3 ZoneOriginLocal = new Vector3(-4f, 0f, -3f);
    static readonly Vector2Int EntryCell = new Vector2Int(2, 0);
    static readonly Vector2Int[] OneCell = { Vector2Int.zero };
    static readonly Vector2Int[] OneCellFront = { new Vector2Int(0, -1) };

    public static ShopCustomizationController Instance { get; private set; }
    public bool IsReady => _ready;
    public string ZoneId => ShopInteriorZoneId;
    public float GridCellSize => GridService.Instance != null ? GridService.Instance.cellSize : 2f;
    public int FixedPlacementCount => _placements.Count(p => p.isFixed);
    public int ActivePlacementCount => _placements.Count(p => !p.recovered && p.gameObject != null && p.gameObject.activeSelf);
    public Vector2Int CurrentZoneSize => _currentZoneSize;
    public int CurrentProgressionTier => TierService.Instance != null ? TierService.Instance.CurrentTier : 0;
    public int CurrentProgressionStage => _highestProgressionStage;
    public int ActiveDisplayCount => CountActiveDisplays();
    public int CurrentDisplayLimit => ResolveDisplayLimit(Mathf.Max(CurrentProgressionTier, _highestProgressionTier));
    public string ActiveThemeId => _activeThemeId;
    public bool ProcessedThemeUnlocked => IsProcessedThemeUnlocked();
    public bool ExpansionNavMeshReady => _expansionNavMeshReady;

    enum PlacementMode { Closed, Browse, New, Move }

    sealed class PlaceableDefinition
    {
        public string id;
        public string displayName;
        public string category;
        public string allowedZone;
        public string surfaceType;
        public string[] tags;
        public int minimumTier;
        public GameObject prefab;
        public Item blueprint;
        public Vector2Int[] footprint;
        public Vector2Int[] clearance;
        public Vector2Int[] interaction;
        public bool rotate90;
        public bool movable;
        public bool recoverable;
        public bool blocksNavigation;
    }

    sealed class PlacementRuntime
    {
        public string instanceId;
        public PlaceableDefinition definition;
        public GameObject gameObject;
        public Vector2Int anchor;
        public int rotation;
        public bool isFixed;
        public bool protectedGameplayAsset;
        public bool recovered;
        public Vector3 originalPosition;
        public Quaternion originalRotation;
        public bool originalActive;
    }

    struct CatalogEntry
    {
        public PlaceableDefinition definition;
        public PlacementRuntime recoveredPlacement;
        public string Label => recoveredPlacement != null
            ? $"회수 가구 · {definition.displayName}"
            : definition.displayName;
    }

    sealed class LightBaseline
    {
        public Color color;
        public float intensity;
        public float range;
    }

    readonly Dictionary<string, PlaceableDefinition> _definitions = new Dictionary<string, PlaceableDefinition>();
    readonly List<PlacementRuntime> _placements = new List<PlacementRuntime>();
    readonly List<CatalogEntry> _catalog = new List<CatalogEntry>();
    readonly Dictionary<Collider, bool> _temporarilyDisabledColliders = new Dictionary<Collider, bool>();
    readonly Dictionary<NavMeshObstacle, bool> _temporarilyDisabledObstacles = new Dictionary<NavMeshObstacle, bool>();
    readonly List<GameObject> _cellHighlights = new List<GameObject>();

    Transform _interior;
    Transform _placedRoot;
    Transform _player;
    GameObject _terminal;
    GameObject _gridOverlay;
    GameObject _panel;
    TextMeshProUGUI _selectionText;
    TextMeshProUGUI _statusText;
    TextMeshProUGUI _progressText;
    TextMeshProUGUI _themeButtonText;
    PlacementMode _mode;
    PlacementRuntime _activePlacement;
    PlaceableDefinition _activeDefinition;
    GameObject _previewObject;
    Vector2Int _previewAnchor;
    int _previewRotation;
    bool _previewValid;
    string _previewReason;
    Vector3 _moveStartPosition;
    Quaternion _moveStartRotation;
    bool _moveStartCaptured;
    int _catalogIndex;
    int _savedHotbarIndex = -1;
    bool _starterGranted;
    bool _ready;
    bool _inputBound;
    Vector2Int _currentZoneSize = TierOneZoneSize;
    int _appliedProgressionTier = int.MinValue;
    int _highestProgressionTier;
    int _highestProgressionStage = 1;
    string _activeThemeId = DefaultThemeId;
    bool _themeUnlockKnown;
    float _nextPresentationRefresh;
    Transform _floor;
    Transform _wallNorth;
    Transform _wallSouth;
    Transform _wallEast;
    Transform _wallWest;
    GameObject _templateRoot;
    GameObject _expansionNavRoot;
    BoxCollider _eastExpansionFloor;
    BoxCollider _northExpansionFloor;
    NavMeshSurface _expansionSurface;
    NavMeshLink _eastExpansionLink;
    NavMeshLink _northExpansionLink;
    bool _expansionNavMeshReady;
    Coroutine _navMeshRefreshRoutine;
    readonly Dictionary<Light, LightBaseline> _lightBaselines = new Dictionary<Light, LightBaseline>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindFirstObjectByType<ShopCustomizationController>() != null) return;
        new GameObject("PA_ShopCustomization").AddComponent<ShopCustomizationController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    IEnumerator Start()
    {
        for (int i = 0; i < 180; i++)
        {
            _interior = GameObject.Find("PA_StoreInterior")?.transform;
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (_interior != null && _player != null && GridService.Instance != null && Inventory.instance != null)
                break;
            yield return null;
        }

        if (_interior == null || GridService.Instance == null)
        {
            Debug.LogWarning("[ShopCustomization] PA_StoreInterior/GridService unavailable; controller remains inactive.");
            yield break;
        }

        InitialiseDefinitions();
        InitialiseZone();
        RegisterExistingShopFurniture();
        BuildPlacementTerminal();
        BuildScreenUI();
        CacheInteriorPresentation();
        ApplyProgressionState(true);
        BuildGridOverlay();
        BindInput();
        _themeUnlockKnown = IsProcessedThemeUnlocked();
        ApplyThemePreset();
        RefreshProgressText();
        _ready = true;
        Debug.Log($"[ShopCustomization] Ready: zone={ShopInteriorZoneId}, size={_currentZoneSize}, fixed={FixedPlacementCount}, definitions={_definitions.Count}.");
    }

    void OnDestroy()
    {
        UnbindInput();
        if (_navMeshRefreshRoutine != null) StopCoroutine(_navMeshRefreshRoutine);
        if (_expansionSurface != null) _expansionSurface.RemoveData();
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!_inputBound) BindInput();
        if (_ready && CurrentProgressionTier != _appliedProgressionTier) ApplyProgressionState(false);
        bool themeUnlocked = IsProcessedThemeUnlocked();
        if (_ready && themeUnlocked != _themeUnlockKnown)
        {
            _themeUnlockKnown = themeUnlocked;
            if (!themeUnlocked && _activeThemeId == ProcessedWarmThemeId)
                _activeThemeId = DefaultThemeId;
            ApplyThemePreset();
            RefreshPanelText();
        }
        if (_ready && Time.unscaledTime >= _nextPresentationRefresh)
        {
            _nextPresentationRefresh = Time.unscaledTime + 1.5f;
            UpdateExpansionDressing();
            ApplyThemePreset();
        }
        if (_mode == PlacementMode.New || _mode == PlacementMode.Move) UpdatePreview();
    }

    void BindInput()
    {
        if (_inputBound || PlayerInputHandler.Instance == null) return;
        PlayerInputHandler.Instance.OnBuildRotate += OnRotatePressed;
        PlayerInputHandler.Instance.OnBuildPlace += OnPlacePressed;
        _inputBound = true;
    }

    void UnbindInput()
    {
        if (!_inputBound || PlayerInputHandler.Instance == null) return;
        PlayerInputHandler.Instance.OnBuildRotate -= OnRotatePressed;
        PlayerInputHandler.Instance.OnBuildPlace -= OnPlacePressed;
        _inputBound = false;
    }

    void InitialiseZone()
    {
        GridService.Instance.RegisterZone(ShopInteriorZoneId, _interior, ZoneOriginLocal, TierOneZoneSize,
            new[] { EntryCell }, EntryCell, ResolveServiceCell(TierOneZoneSize));
        var root = new GameObject("PA_PlacedInteriorFurniture");
        root.transform.SetParent(_interior, false);
        _placedRoot = root.transform;

        _templateRoot = new GameObject("PA_RuntimeFurnitureTemplates");
        _templateRoot.transform.SetParent(transform, false);
        _templateRoot.SetActive(false);
    }

    void InitialiseDefinitions()
    {
        AddWorkbenchDefinition("Items/Blueprints/Blueprint_B05_Workbench", "가공 준비", new[] { "workbench", "processing", "tier0" });
        AddWorkbenchDefinition("Items/Blueprints/Blueprint_B06_KitchenStation", "조리 준비", new[] { "workbench", "cooking", "tier1" });
        AddWorkbenchDefinition("Items/Blueprints/Blueprint_B07_BlacksmithForge", "수리 준비", new[] { "workbench", "forge", "tier1" });
        AddWorkbenchDefinition("Items/Blueprints/Blueprint_B08_SewingTable", "포장 준비", new[] { "workbench", "sewing", "tier2" });

        _definitions["shop.shelf"] = new PlaceableDefinition
        {
            id = "shop.shelf",
            displayName = "상품 진열대",
            category = "Display",
            allowedZone = ShopInteriorZoneId,
            surfaceType = "Floor",
            tags = new[] { "shop", "shelf", "npc-target" },
            minimumTier = 2,
            footprint = OneCell,
            clearance = OneCellFront,
            interaction = OneCellFront,
            rotate90 = true,
            movable = true,
            recoverable = true,
            blocksNavigation = true
        };
    }

    void AddWorkbenchDefinition(string resourcePath, string category, string[] tags)
    {
        Item blueprint = Resources.Load<Item>(resourcePath);
        if (blueprint == null || blueprint.buildingToBuild == null || blueprint.buildingToBuild.prefab == null)
        {
            Debug.LogWarning($"[ShopCustomization] Placeable blueprint unavailable: Resources/{resourcePath}");
            return;
        }

        GameObject prefab = blueprint.buildingToBuild.prefab;
        BoxCollider box = prefab.GetComponent<BoxCollider>();
        float width = box != null ? Mathf.Abs(box.size.x * prefab.transform.localScale.x) : 2f;
        float depth = box != null ? Mathf.Abs(box.size.z * prefab.transform.localScale.z) : 2f;
        int cellsX = Mathf.Max(1, Mathf.CeilToInt(width / 2f));
        int cellsY = Mathf.Max(1, Mathf.CeilToInt(depth / 2f));

        string id = blueprint.name;
        _definitions[id] = new PlaceableDefinition
        {
            id = id,
            displayName = blueprint.buildingToBuild.buildingName,
            category = category,
            allowedZone = ShopInteriorZoneId,
            surfaceType = "Floor",
            tags = tags,
            minimumTier = ResolveMinimumTier(id),
            prefab = prefab,
            blueprint = blueprint,
            footprint = RectangleOffsets(cellsX, cellsY),
            clearance = FrontOffsets(cellsX),
            interaction = FrontOffsets(cellsX),
            rotate90 = true,
            movable = true,
            recoverable = true,
            blocksNavigation = true
        };
    }

    static int ResolveMinimumTier(string definitionId)
    {
        switch (definitionId)
        {
            case "Blueprint_B06_KitchenStation": return 2;
            case "Blueprint_B07_BlacksmithForge": return 1;
            case "Blueprint_B08_SewingTable": return 3;
            case "Blueprint_B05_Workbench": return 1;
            default: return 0;
        }
    }

    static Vector2Int[] RectangleOffsets(int width, int depth)
    {
        var result = new List<Vector2Int>();
        for (int y = 0; y < depth; y++)
            for (int x = 0; x < width; x++) result.Add(new Vector2Int(x, y));
        return result.ToArray();
    }

    static Vector2Int[] FrontOffsets(int width)
    {
        var result = new Vector2Int[Mathf.Max(1, width)];
        for (int x = 0; x < result.Length; x++) result[x] = new Vector2Int(x, -1);
        return result;
    }

    void RegisterExistingShopFurniture()
    {
        ShopSlot[] slots = _interior.GetComponentsInChildren<ShopSlot>(true)
            .Where(slot => slot != null && slot.gameObject.activeSelf)
            .OrderBy(slot => BuildStablePath(slot.transform)).ToArray();

        for (int i = 0; i < slots.Length; i++)
        {
            ShopSlot slot = slots[i];
            string id = $"fixed.{BuildStablePath(slot.transform)}";
            var placement = new PlacementRuntime
            {
                instanceId = id,
                definition = _definitions["shop.shelf"],
                gameObject = slot.gameObject,
                anchor = GridService.Instance.WorldToZoneCell(ShopInteriorZoneId, slot.transform.position),
                rotation = NormaliseRotation(Mathf.RoundToInt(slot.transform.eulerAngles.y / 90f)),
                isFixed = true,
                protectedGameplayAsset = i < 2,
                originalPosition = slot.transform.position,
                originalRotation = slot.transform.rotation,
                originalActive = slot.gameObject.activeSelf
            };
            _placements.Add(placement);
            EnsureCarvingObstacle(slot.gameObject);

            // Existing layout is grandfathered with footprint-only occupancy. Once moved,
            // its explicit interaction clearance is enforced like every new placement.
            if (!GridService.Instance.TryOccupyZone(ShopInteriorZoneId, id, placement.anchor,
                    placement.definition.footprint, Array.Empty<Vector2Int>(), placement.rotation, false, out string reason))
                Debug.LogWarning($"[ShopCustomization] Fixed shelf registration skipped ({id}): {reason}");
        }

        CreateShelfTemplate(slots.FirstOrDefault());
    }

    void CreateShelfTemplate(ShopSlot source)
    {
        if (source == null || _templateRoot == null || !_definitions.TryGetValue("shop.shelf", out var shelf)) return;
        GameObject template = Instantiate(source.gameObject, _templateRoot.transform);
        template.name = "PA_TierDisplayShelfTemplate";
        template.transform.localPosition = Vector3.zero;
        template.transform.localRotation = Quaternion.identity;
        template.SetActive(true);
        ShopSlot slot = template.GetComponent<ShopSlot>();
        if (slot != null)
        {
            slot.currentItem = null;
            slot.displayPrice = 0;
            slot.RefreshDisplay();
        }
        EnsureCarvingObstacle(template);
        shelf.prefab = template;
    }

    static string BuildStablePath(Transform value)
    {
        if (value == null) return "missing";
        var parts = new List<string>();
        Transform current = value;
        while (current != null && current.name != "PA_StoreInterior")
        {
            parts.Add($"{current.name}[{current.GetSiblingIndex()}]");
            current = current.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    static void EnsureCarvingObstacle(GameObject target)
    {
        if (target == null || target.GetComponentInChildren<NavMeshObstacle>() != null) return;
        BoxCollider box = target.GetComponent<BoxCollider>();
        if (box == null) return;
        var obstacle = target.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = box.center;
        obstacle.size = box.size;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
    }

    // -------- Player entry / UI --------

    public void ToggleFromTerminal(GameObject interactor)
    {
        if (!_ready) return;
        if (_mode != PlacementMode.Closed) { CloseCustomization(); return; }
        _player = interactor != null ? interactor.transform : GameObject.FindGameObjectWithTag("Player")?.transform;
        string starterMessage = EnsureStarterKit();
        string tierRewardMessage = EnsureTierUnlockKits();
        _mode = PlacementMode.Browse;
        _panel.SetActive(true);
        _gridOverlay.SetActive(true);
        SelectSafeHotbarSlot();
        BuildManager.instance?.StopBuildMode();
        RebuildCatalog();
        string openMessage = !string.IsNullOrEmpty(tierRewardMessage) ? tierRewardMessage : starterMessage;
        SetStatus(string.IsNullOrEmpty(openMessage)
            ? "가구를 고르거나 가까운 기존 가구를 이동·회수하세요."
            : openMessage);
    }

    public string GetTerminalPrompt()
    {
        return _mode == PlacementMode.Closed ? "상점 배치 장부 열기" : "상점 배치 장부 닫기";
    }

    string EnsureStarterKit()
    {
        if (_starterGranted || !_definitions.TryGetValue("Blueprint_B05_Workbench", out var starter)
            || !IsDefinitionUnlocked(starter) || starter.blueprint == null || Inventory.instance == null) return string.Empty;
        if (Inventory.instance.HasItems(starter.blueprint, 1)
            || _placements.Any(p => !p.isFixed && p.definition == starter))
        {
            _starterGranted = true;
            return string.Empty;
        }
        if (Inventory.instance.AddItem(starter.blueprint, 1))
        {
            _starterGranted = true;
            Debug.Log("[ShopCustomization] Tier 1 starter kit granted: Blueprint_B05_Workbench x1.");
            return "첫 배치 장부 보상으로 기본 작업대 설계도 1개를 받았습니다.";
        }
        return "가방이 가득 찼습니다. 한 칸을 비우고 장부를 다시 열면 기본 작업대 설계도를 받습니다.";
    }

    string EnsureTierUnlockKits()
    {
        if (Inventory.instance == null) return string.Empty;
        var granted = new List<string>();
        bool inventoryFull = false;
        foreach (PlaceableDefinition definition in _definitions.Values
                     .Where(d => d.blueprint != null
                         && d.id != "Blueprint_B05_Workbench"
                         && d.minimumTier >= 1
                         && IsDefinitionUnlocked(d))
                     .OrderBy(d => d.minimumTier).ThenBy(d => d.id))
        {
            if (Inventory.instance.HasItems(definition.blueprint, 1)
                || _placements.Any(p => p.definition == definition)) continue;
            if (Inventory.instance.AddItem(definition.blueprint, 1))
            {
                granted.Add(definition.displayName);
                Debug.Log($"[ShopCustomization] Tier {definition.minimumTier} unlock kit granted: {definition.id} x1.");
            }
            else inventoryFull = true;
        }

        if (granted.Count > 0)
            return $"상점 성장 보상: {string.Join(", ", granted)} 설계도를 받았습니다.";
        return inventoryFull
            ? "가방이 가득 차 성장 설계도를 받지 못했습니다. 한 칸을 비우고 장부를 다시 여세요."
            : string.Empty;
    }

    void SelectSafeHotbarSlot()
    {
        if (Inventory.instance == null || Inventory.instance.hotbar == null) return;
        _savedHotbarIndex = Inventory.instance.selectedHotbarIndex;
        for (int i = 0; i < Inventory.instance.hotbar.slots.Count; i++)
        {
            var slot = Inventory.instance.hotbar.GetSlot(i);
            if (slot == null || slot.IsEmpty || slot.item == null || slot.item.buildingToBuild == null)
            {
                Inventory.instance.selectedHotbarIndex = i;
                return;
            }
        }
    }

    void RestoreHotbarSelection()
    {
        if (_savedHotbarIndex < 0 || Inventory.instance == null) return;
        Inventory.instance.selectedHotbarIndex = _savedHotbarIndex;
        _savedHotbarIndex = -1;
        Inventory.instance.RefreshAllUI();
    }

    public void CloseCustomization()
    {
        CancelActivePlacement();
        _mode = PlacementMode.Closed;
        if (_panel != null) _panel.SetActive(false);
        if (_gridOverlay != null) _gridOverlay.SetActive(false);
        ClearHighlights();
        RestoreHotbarSelection();
    }

    void RebuildCatalog()
    {
        _catalog.Clear();
        foreach (var definition in _definitions.Values.OrderBy(d => d.id))
        {
            if (!IsDefinitionUnlocked(definition)) continue;
            if (definition.blueprint != null && Inventory.instance != null
                && Inventory.instance.HasItems(definition.blueprint, 1))
                _catalog.Add(new CatalogEntry { definition = definition });
            else if (definition.id == "shop.shelf" && definition.prefab != null
                     && CountActiveDisplays() < CurrentDisplayLimit)
                _catalog.Add(new CatalogEntry { definition = definition });
        }
        foreach (var placement in _placements.Where(p => p.isFixed && p.recovered).OrderBy(p => p.instanceId))
            _catalog.Add(new CatalogEntry { definition = placement.definition, recoveredPlacement = placement });
        _catalogIndex = _catalog.Count == 0 ? 0 : Mathf.Clamp(_catalogIndex, 0, _catalog.Count - 1);
        RefreshPanelText();
    }

    bool IsDefinitionUnlocked(PlaceableDefinition definition)
    {
        return definition != null && Mathf.Max(CurrentProgressionTier, _highestProgressionTier) >= definition.minimumTier;
    }

    void PreviousCatalog()
    {
        if (_catalog.Count == 0) return;
        _catalogIndex = (_catalogIndex - 1 + _catalog.Count) % _catalog.Count;
        RefreshPanelText();
    }

    void NextCatalog()
    {
        if (_catalog.Count == 0) return;
        _catalogIndex = (_catalogIndex + 1) % _catalog.Count;
        RefreshPanelText();
    }

    void BeginSelectedPlacement()
    {
        if (_catalog.Count == 0) { SetStatus("배치 가능한 설계도나 회수 가구가 없습니다."); return; }
        CancelActivePlacement();
        CatalogEntry entry = _catalog[_catalogIndex];
        _activeDefinition = entry.definition;
        _activePlacement = entry.recoveredPlacement;
        _previewRotation = _activePlacement != null ? _activePlacement.rotation : 0;
        _mode = _activePlacement != null ? PlacementMode.Move : PlacementMode.New;
        if (_activePlacement != null)
        {
            CaptureMoveStartTransform();
            _activePlacement.gameObject.SetActive(true);
            SetPlacementCollision(_activePlacement.gameObject, false);
        }
        else CreatePreviewObject(_activeDefinition.prefab);
        SetStatus("WASD로 셀을 고르고 R로 회전한 뒤 바닥을 클릭해 확정하세요.");
    }

    void BeginMoveNearest()
    {
        PlacementRuntime nearest = FindNearestPlacement(requireRecoverable: false);
        if (nearest == null) { SetStatus("3.8m 안에 이동 가능한 가구가 없습니다."); return; }
        CancelActivePlacement();
        _activePlacement = nearest;
        _activeDefinition = nearest.definition;
        _previewRotation = nearest.rotation;
        _mode = PlacementMode.Move;
        CaptureMoveStartTransform();
        SetPlacementCollision(nearest.gameObject, false);
        SetStatus($"{nearest.definition.displayName} 이동 중 · R 회전 · 바닥 클릭 확정");
    }

    void CaptureMoveStartTransform()
    {
        _moveStartCaptured = _activePlacement != null && _activePlacement.gameObject != null;
        if (!_moveStartCaptured) return;
        _moveStartPosition = _activePlacement.gameObject.transform.position;
        _moveStartRotation = _activePlacement.gameObject.transform.rotation;
    }

    void RecoverNearest()
    {
        PlacementRuntime nearest = FindNearestPlacement(requireRecoverable: true);
        if (nearest == null) { SetStatus("3.8m 안에 회수 가능한 가구가 없습니다."); return; }
        if (TryRecoverPlacement(nearest, true, out string reason))
        {
            SetStatus($"{nearest.definition.displayName}을 안전하게 회수했습니다.");
            RebuildCatalog();
        }
        else SetStatus(reason);
    }

    PlacementRuntime FindNearestPlacement(bool requireRecoverable)
    {
        if (_player == null) return null;
        PlacementRuntime nearest = null;
        float best = 3.8f * 3.8f;
        foreach (var placement in _placements)
        {
            if (placement.recovered || placement.gameObject == null || !placement.definition.movable) continue;
            if (requireRecoverable && !placement.definition.recoverable) continue;
            float distance = (placement.gameObject.transform.position - _player.position).sqrMagnitude;
            if (distance >= best) continue;
            best = distance;
            nearest = placement;
        }
        return nearest;
    }

    void OnRotatePressed()
    {
        if (_mode != PlacementMode.New && _mode != PlacementMode.Move) return;
        if (_activeDefinition == null || !_activeDefinition.rotate90) return;
        _previewRotation = NormaliseRotation(_previewRotation + 1);
        UpdatePreview(force: true);
    }

    void OnPlacePressed()
    {
        if (_mode != PlacementMode.New && _mode != PlacementMode.Move) return;
        CommitActivePlacement();
    }

    void UpdatePreview(bool force = false)
    {
        if (_player == null || _activeDefinition == null || GridService.Instance == null) return;
        Vector3 target = _player.position + _player.forward * 2.6f;
        Vector2Int anchor = GridService.Instance.WorldToZoneCell(ShopInteriorZoneId, target);
        if (!force && anchor == _previewAnchor) return;
        _previewAnchor = anchor;

        string owner = _activePlacement != null ? _activePlacement.instanceId : "preview.new";
        _previewValid = GridService.Instance.CanOccupyZone(ShopInteriorZoneId, owner, anchor,
            _activeDefinition.footprint, _activeDefinition.clearance, _previewRotation, true, out _previewReason);

        GameObject visual = _activePlacement != null ? _activePlacement.gameObject : _previewObject;
        if (visual != null)
        {
            visual.transform.position = GetPlacementWorld(_activeDefinition, anchor, _previewRotation);
            visual.transform.rotation = GetPlacementRotation(_previewRotation);
            TintPreview(visual, _previewValid);
        }
        DrawHighlights(anchor, _activeDefinition, _previewRotation, _previewValid);
        SetStatus(_previewValid ? "배치 가능 · 바닥 클릭으로 확정" : _previewReason);
    }

    void CommitActivePlacement()
    {
        if (!_previewValid || _activeDefinition == null) { SetStatus(_previewReason); return; }
        if (_mode == PlacementMode.New)
        {
            if (!TryPlaceDefinition(_activeDefinition, _previewAnchor, _previewRotation, true,
                    out _, out string reason))
            { SetStatus(reason); return; }
            DestroyPreviewObject();
        }
        else if (_activePlacement != null)
        {
            if (!TryMovePlacement(_activePlacement, _previewAnchor, _previewRotation, out string reason))
            { SetStatus(reason); return; }
            _activePlacement.recovered = false;
            SetPlacementCollision(_activePlacement.gameObject, true);
            ClearPreviewTint(_activePlacement.gameObject);
        }

        _activePlacement = null;
        _activeDefinition = null;
        _moveStartCaptured = false;
        _mode = PlacementMode.Browse;
        ClearHighlights();
        RebuildCatalog();
        SetStatus("배치를 저장했습니다. F5 저장에도 위치와 회전이 포함됩니다.");
    }

    void CancelActivePlacement()
    {
        if (_mode == PlacementMode.Move && _activePlacement != null)
        {
            if (_activePlacement.recovered)
            {
                SetPlacementCollision(_activePlacement.gameObject, true);
                _activePlacement.gameObject.SetActive(false);
            }
            else
            {
                _activePlacement.gameObject.transform.position = _moveStartCaptured
                    ? _moveStartPosition
                    : GetPlacementWorld(_activePlacement.definition, _activePlacement.anchor, _activePlacement.rotation);
                _activePlacement.gameObject.transform.rotation = _moveStartCaptured
                    ? _moveStartRotation
                    : GetPlacementRotation(_activePlacement.rotation);
                SetPlacementCollision(_activePlacement.gameObject, true);
                ClearPreviewTint(_activePlacement.gameObject);
            }
        }
        DestroyPreviewObject();
        _activePlacement = null;
        _activeDefinition = null;
        _moveStartCaptured = false;
        ClearHighlights();
        if (_mode != PlacementMode.Closed) _mode = PlacementMode.Browse;
    }

    // -------- Core placement operations --------

    bool TryPlaceDefinition(PlaceableDefinition definition, Vector2Int anchor, int rotation,
        bool consumeBlueprint, out string instanceId, out string reason)
    {
        instanceId = string.Empty;
        reason = string.Empty;
        if (definition == null || definition.prefab == null) { reason = "배치 프리팹이 없습니다."; return false; }
        if (!IsDefinitionUnlocked(definition) && (consumeBlueprint || definition.blueprint == null))
        { reason = $"Tier {definition.minimumTier} 상점에서 해금되는 가구입니다."; return false; }
        if (definition.allowedZone != ShopInteriorZoneId || definition.surfaceType != "Floor")
        { reason = "이 가구는 상점 실내 바닥에 놓을 수 없습니다."; return false; }
        if (definition.id == "shop.shelf" && CountActiveDisplays() >= CurrentDisplayLimit)
        { reason = $"현재 Tier의 진열대 한도({CurrentDisplayLimit}개)에 도달했습니다."; return false; }
        if (consumeBlueprint && definition.blueprint != null
            && (Inventory.instance == null || !Inventory.instance.HasItems(definition.blueprint, 1)))
        { reason = "필요한 가구 설계도가 없습니다."; return false; }

        instanceId = $"placed.{Guid.NewGuid():N}";
        if (!GridService.Instance.TryOccupyZone(ShopInteriorZoneId, instanceId, anchor,
                definition.footprint, definition.clearance, rotation, true, out reason))
        { instanceId = string.Empty; return false; }

        GameObject placed = Instantiate(definition.prefab, _placedRoot);
        if (placed == null)
        {
            GridService.Instance.ReleaseZoneOwner(ShopInteriorZoneId, instanceId);
            instanceId = string.Empty;
            reason = "가구 생성에 실패했습니다.";
            return false;
        }
        placed.name = $"PA_Placed_{definition.id}_{instanceId.Substring(instanceId.Length - 6)}";
        placed.SetActive(true);
        placed.transform.position = GetPlacementWorld(definition, anchor, rotation);
        placed.transform.rotation = GetPlacementRotation(rotation);
        ShopSlot placedSlot = placed.GetComponent<ShopSlot>();
        if (placedSlot != null)
        {
            placedSlot.currentItem = null;
            placedSlot.displayPrice = 0;
            placedSlot.RefreshDisplay();
        }
        EnsureCarvingObstacle(placed);

        var runtime = new PlacementRuntime
        {
            instanceId = instanceId,
            definition = definition,
            gameObject = placed,
            anchor = anchor,
            rotation = NormaliseRotation(rotation),
            isFixed = false,
            originalPosition = placed.transform.position,
            originalRotation = placed.transform.rotation,
            originalActive = true
        };
        _placements.Add(runtime);
        if (consumeBlueprint && definition.blueprint != null) Inventory.instance.RemoveItems(definition.blueprint, 1);
        ApplyThemePreset();
        RefreshProgressText();
        Debug.Log($"[ShopCustomization] Placed {definition.id} at {anchor}, r={runtime.rotation}, id={instanceId}.");
        return true;
    }

    bool TryMovePlacement(PlacementRuntime placement, Vector2Int anchor, int rotation, out string reason)
    {
        reason = string.Empty;
        if (placement == null || placement.gameObject == null || !placement.definition.movable)
        { reason = "이 가구는 이동할 수 없습니다."; return false; }
        if (placement.recovered && placement.definition.id == "shop.shelf"
            && CountActiveDisplays() >= CurrentDisplayLimit)
        { reason = $"현재 Tier의 진열대 한도({CurrentDisplayLimit}개)에 도달했습니다."; return false; }
        if (!GridService.Instance.TryOccupyZone(ShopInteriorZoneId, placement.instanceId, anchor,
                placement.definition.footprint, placement.definition.clearance, rotation, true, out reason))
            return false;
        placement.anchor = anchor;
        placement.rotation = NormaliseRotation(rotation);
        placement.gameObject.transform.position = GetPlacementWorld(placement.definition, anchor, rotation);
        placement.gameObject.transform.rotation = GetPlacementRotation(rotation);
        placement.gameObject.SetActive(true);
        ApplyThemePreset();
        RefreshProgressText();
        return true;
    }

    bool TryRecoverPlacement(PlacementRuntime placement, bool returnBlueprint, out string reason)
    {
        reason = string.Empty;
        if (placement == null || placement.gameObject == null || !placement.definition.recoverable)
        { reason = "이 가구는 회수할 수 없습니다."; return false; }
        if (placement.protectedGameplayAsset)
        { reason = "상점 운영에 필요한 핵심 진열대 2개는 회수할 수 없지만 이동은 가능합니다."; return false; }

        ShopSlot shopSlot = placement.gameObject.GetComponent<ShopSlot>();
        if (shopSlot != null && !shopSlot.IsEmpty)
        {
            shopSlot.RetrieveItem();
            if (!shopSlot.IsEmpty) { reason = "가방이 가득 차 진열 상품을 먼저 회수할 수 없습니다."; return false; }
        }
        StorageBox storage = placement.gameObject.GetComponent<StorageBox>();
        if (storage != null && storage.items != null && storage.items.Count > 0)
        { reason = "보관함 안의 물건을 먼저 비워야 합니다."; return false; }

        if (!placement.isFixed && returnBlueprint && placement.definition.blueprint != null)
        {
            if (Inventory.instance == null || !Inventory.instance.AddItem(placement.definition.blueprint, 1))
            { reason = "가방이 가득 차 설계도를 돌려받을 수 없습니다."; return false; }
        }

        GridService.Instance.ReleaseZoneOwner(ShopInteriorZoneId, placement.instanceId);
        if (placement.isFixed)
        {
            placement.recovered = true;
            placement.gameObject.SetActive(false);
        }
        else
        {
            _placements.Remove(placement);
            Destroy(placement.gameObject);
        }
        RefreshProgressText();
        return true;
    }

    Vector3 GetPlacementWorld(PlaceableDefinition definition, Vector2Int anchor, int rotation)
    {
        return GridService.Instance.GetZonePlacementWorld(ShopInteriorZoneId, anchor,
            definition.footprint, rotation, PlacementHeight);
    }

    Quaternion GetPlacementRotation(int rotation)
    {
        return _interior.rotation * Quaternion.Euler(0f, NormaliseRotation(rotation) * 90f, 0f);
    }

    static int NormaliseRotation(int value) => ((value % 4) + 4) % 4;

    void SetPlacementCollision(GameObject target, bool enabled)
    {
        if (target == null) return;
        if (!enabled)
        {
            _temporarilyDisabledColliders.Clear();
            _temporarilyDisabledObstacles.Clear();
            foreach (var collider in target.GetComponentsInChildren<Collider>(true))
            {
                _temporarilyDisabledColliders[collider] = collider.enabled;
                collider.enabled = false;
            }
            foreach (var obstacle in target.GetComponentsInChildren<NavMeshObstacle>(true))
            {
                _temporarilyDisabledObstacles[obstacle] = obstacle.enabled;
                obstacle.enabled = false;
            }
            return;
        }
        foreach (var pair in _temporarilyDisabledColliders)
            if (pair.Key != null) pair.Key.enabled = pair.Value;
        foreach (var pair in _temporarilyDisabledObstacles)
            if (pair.Key != null) pair.Key.enabled = pair.Value;
        _temporarilyDisabledColliders.Clear();
        _temporarilyDisabledObstacles.Clear();
    }

    // -------- Save v10 sidecar --------

    public void WriteSaveFields(SaveData data)
    {
        if (data == null) return;
        data.placementStarterGranted = _starterGranted;
        data.placeables = new List<PlaceableSaveData>();
        foreach (var placement in _placements)
        {
            if (placement == null || placement.definition == null) continue;
            var record = new PlaceableSaveData
            {
                zoneId = ShopInteriorZoneId,
                definitionId = placement.definition.id,
                instanceId = placement.instanceId,
                gridX = placement.anchor.x,
                gridY = placement.anchor.y,
                rotationQuarterTurns = placement.rotation,
                isFixed = placement.isFixed,
                recovered = placement.recovered,
                functionalState = ResolveFunctionalState(placement.gameObject),
                storedItems = SerializeStorage(placement.gameObject)
            };
            data.placeables.Add(record);
        }
        data.placeables.Add(new PlaceableSaveData
        {
            zoneId = ShopInteriorZoneId,
            definitionId = ThemeRecordDefinitionId,
            instanceId = ThemeRecordInstanceId,
            functionalState = $"theme:{_activeThemeId}"
        });
    }

    public void RestoreSavedState(List<PlaceableSaveData> saved, bool starterGranted)
    {
        _starterGranted = starterGranted;
        _activeThemeId = DefaultThemeId;
        PlaceableSaveData themeRecord = saved?.FirstOrDefault(record => record != null
            && record.zoneId == ShopInteriorZoneId
            && record.definitionId == ThemeRecordDefinitionId
            && record.instanceId == ThemeRecordInstanceId);
        if (themeRecord != null && themeRecord.functionalState == $"theme:{ProcessedWarmThemeId}"
            && IsProcessedThemeUnlocked())
            _activeThemeId = ProcessedWarmThemeId;
        CancelActivePlacement();
        foreach (var placement in _placements)
            GridService.Instance.ReleaseZoneOwner(ShopInteriorZoneId, placement.instanceId);

        foreach (var dynamicPlacement in _placements.Where(p => !p.isFixed).ToList())
        {
            if (dynamicPlacement.gameObject != null)
            {
                dynamicPlacement.gameObject.SetActive(false);
                Destroy(dynamicPlacement.gameObject);
            }
            _placements.Remove(dynamicPlacement);
        }

        foreach (var fixedPlacement in _placements)
        {
            fixedPlacement.gameObject.transform.position = fixedPlacement.originalPosition;
            fixedPlacement.gameObject.transform.rotation = fixedPlacement.originalRotation;
            fixedPlacement.gameObject.SetActive(fixedPlacement.originalActive);
            fixedPlacement.recovered = false;
        }

        if (saved == null || saved.Count == 0)
        {
            foreach (var fixedPlacement in _placements)
                GridService.Instance.TryOccupyZone(ShopInteriorZoneId, fixedPlacement.instanceId,
                    fixedPlacement.anchor, fixedPlacement.definition.footprint, Array.Empty<Vector2Int>(),
                    fixedPlacement.rotation, false, out _);
            ApplyThemePreset();
            RebuildCatalog();
            RefreshProgressText();
            return;
        }

        foreach (var record in saved)
        {
            if (record == null || record.zoneId != ShopInteriorZoneId) continue;
            if (record.definitionId == ThemeRecordDefinitionId && record.instanceId == ThemeRecordInstanceId) continue;
            if (record.isFixed)
            {
                PlacementRuntime fixedPlacement = _placements.FirstOrDefault(p => p.isFixed && p.instanceId == record.instanceId);
                if (fixedPlacement == null) continue;
                fixedPlacement.anchor = new Vector2Int(record.gridX, record.gridY);
                fixedPlacement.rotation = NormaliseRotation(record.rotationQuarterTurns);
                fixedPlacement.recovered = record.recovered;
                fixedPlacement.gameObject.SetActive(!record.recovered && fixedPlacement.originalActive);
                if (!record.recovered) RestorePlacementTransformAndOccupancy(fixedPlacement, record, false);
                RestoreStorage(fixedPlacement.gameObject, record.storedItems);
                continue;
            }

            if (!_definitions.TryGetValue(record.definitionId, out var definition) || definition.prefab == null) continue;
            GameObject placed = Instantiate(definition.prefab, _placedRoot);
            placed.name = $"PA_Placed_{definition.id}_{ShortId(record.instanceId)}";
            placed.SetActive(true);
            ShopSlot placedSlot = placed.GetComponent<ShopSlot>();
            if (placedSlot != null)
            {
                placedSlot.currentItem = null;
                placedSlot.displayPrice = 0;
                placedSlot.RefreshDisplay();
            }
            EnsureCarvingObstacle(placed);
            var runtime = new PlacementRuntime
            {
                instanceId = string.IsNullOrWhiteSpace(record.instanceId) ? $"placed.{Guid.NewGuid():N}" : record.instanceId,
                definition = definition,
                gameObject = placed,
                anchor = new Vector2Int(record.gridX, record.gridY),
                rotation = NormaliseRotation(record.rotationQuarterTurns),
                isFixed = false,
                recovered = record.recovered,
                originalActive = true
            };
            _placements.Add(runtime);
            if (runtime.recovered) placed.SetActive(false);
            else
            {
                RestorePlacementTransformAndOccupancy(runtime, record, true);
                RestoreStorage(placed, record.storedItems);
            }
        }
        ApplyThemePreset();
        RebuildCatalog();
        RefreshProgressText();
    }

    void RestorePlacementTransformAndOccupancy(PlacementRuntime placement, PlaceableSaveData record, bool strict)
    {
        if (!GridService.Instance.TryOccupyZone(ShopInteriorZoneId, placement.instanceId, placement.anchor,
                placement.definition.footprint, strict ? placement.definition.clearance : Array.Empty<Vector2Int>(),
                placement.rotation, strict, out string reason))
        {
            placement.recovered = true;
            placement.gameObject.SetActive(false);
            Debug.LogWarning($"[ShopCustomization] Rejected unsafe saved placement {placement.instanceId}: {reason}");
            return;
        }
        placement.gameObject.transform.position = GetPlacementWorld(placement.definition, placement.anchor, placement.rotation);
        placement.gameObject.transform.rotation = GetPlacementRotation(placement.rotation);
    }

    static string ResolveFunctionalState(GameObject target)
    {
        if (target == null) return "missing";
        ShopSlot slot = target.GetComponent<ShopSlot>();
        if (slot != null) return slot.IsEmpty ? "shop-slot:empty" : "shop-slot:stocked";
        Workbench workbench = target.GetComponent<Workbench>();
        if (workbench != null) return $"workbench:{workbench.workbenchType}";
        StorageBox storage = target.GetComponent<StorageBox>();
        if (storage != null) return $"storage:{(storage.items != null ? storage.items.Count : 0)}";
        return "visual";
    }

    static List<PlaceableStoredItemSaveData> SerializeStorage(GameObject target)
    {
        var result = new List<PlaceableStoredItemSaveData>();
        StorageBox storage = target != null ? target.GetComponent<StorageBox>() : null;
        if (storage == null || storage.items == null) return result;
        foreach (var item in storage.items)
        {
            if (item == null || item.data == null || item.count <= 0) continue;
            result.Add(new PlaceableStoredItemSaveData
            {
                itemId = item.data.id,
                itemName = item.data.itemName,
                count = item.count,
                quality = item.quality,
                currentPrice = item.currentPrice
            });
        }
        return result;
    }

    static void RestoreStorage(GameObject target, List<PlaceableStoredItemSaveData> saved)
    {
        StorageBox storage = target != null ? target.GetComponent<StorageBox>() : null;
        if (storage == null) return;
        storage.items.Clear();
        if (saved == null) return;
        foreach (var record in saved)
        {
            Item item = ItemRegistry.Instance != null ? ItemRegistry.Instance.Find(record.itemId, record.itemName) : null;
            if (item == null || record.count <= 0) continue;
            storage.items.Add(new ItemInstance(item, record.count)
            {
                quality = record.quality,
                currentPrice = record.currentPrice
            });
        }
    }

    static string ShortId(string value)
    {
        if (string.IsNullOrEmpty(value)) return "restored";
        return value.Length <= 6 ? value : value.Substring(value.Length - 6);
    }

    // -------- Validation/public integration API --------

    public string GetWorkbenchDefinitionId() => _definitions.ContainsKey("Blueprint_B05_Workbench")
        ? "Blueprint_B05_Workbench" : string.Empty;

    public List<string> GetMovableShopSlotIds()
    {
        return _placements.Where(p => p.isFixed && p.definition.id == "shop.shelf" && p.definition.movable)
            .Select(p => p.instanceId).ToList();
    }

    public string FindPlacementIdAt(Vector2Int cell)
    {
        return _placements.FirstOrDefault(p => !p.recovered && p.anchor == cell)?.instanceId;
    }

    public GameObject GetPlacementGameObject(string instanceId)
    {
        return _placements.FirstOrDefault(p => p.instanceId == instanceId)?.gameObject;
    }

    public bool TryGetPlacementSnapshot(string instanceId, out Vector2Int cell, out int rotation, out bool recovered)
    {
        PlacementRuntime placement = _placements.FirstOrDefault(p => p.instanceId == instanceId);
        cell = placement != null ? placement.anchor : Vector2Int.zero;
        rotation = placement != null ? placement.rotation : 0;
        recovered = placement != null && placement.recovered;
        return placement != null;
    }

    // Theme-corner presentation reads the authoritative placement footprint through this
    // narrow projection. Placement ownership and mutation remain private to this controller.
    public bool TryGetShopSlotPlacementSnapshot(ShopSlot slot, List<Vector2Int> footprintCells,
        out string placementId, out bool recovered)
    {
        placementId = string.Empty;
        recovered = false;
        footprintCells?.Clear();
        if (!_ready || slot == null || footprintCells == null || GridService.Instance == null) return false;

        PlacementRuntime placement = _placements.FirstOrDefault(p => p.definition != null
            && p.definition.id == "shop.shelf" && p.gameObject != null
            && (slot.gameObject == p.gameObject || slot.transform.IsChildOf(p.gameObject.transform)));
        if (placement == null) return false;

        placementId = placement.instanceId;
        recovered = placement.recovered;
        IReadOnlyList<Vector2Int> cells = GridService.Instance.GetZoneFootprintCells(
            placement.anchor, placement.definition.footprint, placement.rotation);
        foreach (Vector2Int cell in cells) footprintCells.Add(cell);
        return true;
    }

    public void RefreshCornerPresentation()
    {
        RefreshProgressText();
    }

    // P4 — 배치 정의의 interaction 셀을 NPC가 사용할 실제 월드 접근점으로 노출한다.
    // PlaceableDefinition/PlacementRuntime 자체는 계속 private로 유지해 배치 권한을 우회하지 않는다.
    public bool TryGetNpcApproachPoints(ShopSlot slot, List<Vector3> worldPoints,
        List<Vector2Int> zoneCells, out string placementId)
    {
        return TryGetNpcApproachPoints(slot != null ? slot.transform : null,
            worldPoints, zoneCells, out placementId);
    }

    // B05~B08 전문 주민도 고객 진열대와 같은 authored interaction 셀을 사용한다.
    // Workbench/배치 런타임 자체를 노출하지 않고 읽기 전용 좌표만 투영한다.
    public bool TryGetNpcApproachPoints(Workbench workbench, List<Vector3> worldPoints,
        List<Vector2Int> zoneCells, out string placementId)
    {
        return TryGetNpcApproachPoints(workbench != null ? workbench.transform : null,
            worldPoints, zoneCells, out placementId);
    }

    bool TryGetNpcApproachPoints(Transform target, List<Vector3> worldPoints,
        List<Vector2Int> zoneCells, out string placementId)
    {
        placementId = string.Empty;
        worldPoints?.Clear();
        zoneCells?.Clear();
        if (!_ready || target == null || worldPoints == null || zoneCells == null) return false;

        PlacementRuntime placement = _placements.FirstOrDefault(p => !p.recovered && p.gameObject != null
            && (target.gameObject == p.gameObject || target.IsChildOf(p.gameObject.transform)));
        if (placement == null || placement.definition == null) return false;

        placementId = placement.instanceId;
        IReadOnlyList<Vector2Int> cells = GridService.Instance.GetZoneFootprintCells(
            placement.anchor, placement.definition.interaction, placement.rotation);
        foreach (Vector2Int cell in cells)
        {
            if (!GridService.Instance.IsZoneCellInBounds(ShopInteriorZoneId, cell)) continue;
            string blockingOwner = GridService.Instance.GetZoneOwner(ShopInteriorZoneId, cell);
            if (!string.IsNullOrEmpty(blockingOwner) && blockingOwner != placement.instanceId) continue;
            zoneCells.Add(cell);
            worldPoints.Add(GridService.Instance.ZoneCellToWorld(ShopInteriorZoneId, cell, 0.04f));
        }
        // 등록된 가구이지만 앞 셀이 막힌 경우에도 true를 반환한다. 호출자는 임의 fallback으로
        // 우회하지 않고 해당 슬롯을 실제로 접근 불가한 후보로 처리해야 한다.
        return true;
    }

    public bool TryPlaceDefinitionForValidation(string definitionId, Vector2Int cell, int rotation,
        bool consumeBlueprint, out string instanceId, out string reason)
    {
        if (!_definitions.TryGetValue(definitionId, out var definition))
        { instanceId = string.Empty; reason = "definition missing"; return false; }
        return TryPlaceDefinition(definition, cell, rotation, consumeBlueprint, out instanceId, out reason);
    }

    public bool TryMovePlacementForValidation(string instanceId, Vector2Int cell, int rotation, out string reason)
    {
        PlacementRuntime placement = _placements.FirstOrDefault(p => p.instanceId == instanceId);
        if (!TryMovePlacement(placement, cell, rotation, out reason)) return false;
        // 실제 CommitActivePlacement와 같은 완료 상태를 제공한다. 회수 가구를 다시
        // 배치한 검증이 preview 상태(recovered=true)에 머무르지 않게 한다.
        placement.recovered = false;
        return true;
    }

    public bool TryRecoverPlacementForValidation(string instanceId, bool returnBlueprint, out string reason)
    {
        return TryRecoverPlacement(_placements.FirstOrDefault(p => p.instanceId == instanceId), returnBlueprint, out reason);
    }

    public bool IsDefinitionUnlockedForValidation(string definitionId)
    {
        return _definitions.TryGetValue(definitionId, out var definition) && IsDefinitionUnlocked(definition);
    }

    public bool HasActiveDefinitionPlacement(string definitionId)
    {
        return !string.IsNullOrWhiteSpace(definitionId)
            && _placements.Any(placement => placement.definition != null
                && placement.definition.id == definitionId
                && !placement.recovered
                && placement.gameObject != null
                && placement.gameObject.activeInHierarchy);
    }

    public int GetDefinitionMinimumTierForValidation(string definitionId)
    {
        return _definitions.TryGetValue(definitionId, out var definition) ? definition.minimumTier : -1;
    }

    public bool HasDefinitionBlueprintInInventory(string definitionId)
    {
        return _definitions.TryGetValue(definitionId, out var definition) && definition.blueprint != null
            && Inventory.instance != null && Inventory.instance.HasItems(definition.blueprint, 1);
    }

    public List<string> GetCatalogDefinitionIdsForValidation()
    {
        RebuildCatalog();
        return _catalog.Select(entry => entry.definition.id).ToList();
    }

    public void RefreshProgressionForValidation() => ApplyProgressionState(true);

    public bool TryCycleThemeForValidation()
    {
        string before = _activeThemeId;
        CycleThemePreset();
        return before != _activeThemeId;
    }

    // -------- Tier progression / presentation --------

    static Vector2Int ResolveServiceCell(Vector2Int zoneSize)
    {
        return new Vector2Int(2, Mathf.Max(0, zoneSize.y - 1));
    }

    static int ResolveProgressionStage(int tier)
    {
        if (tier >= 3) return 3;
        if (tier >= 2) return 2;
        return 1;
    }

    static Vector2Int ResolveZoneSize(int stage)
    {
        if (stage >= 3) return TierThreeZoneSize;
        if (stage >= 2) return TierTwoZoneSize;
        return TierOneZoneSize;
    }

    int ResolveDisplayLimit(int tier)
    {
        int clampedTier = Mathf.Clamp(tier, 0, ProgressionDisplayLimits.Length - 1);
        return ProgressionDisplayLimits[clampedTier];
    }

    int CountActiveDisplays()
    {
        return _placements.Count(placement => placement != null && placement.definition != null
            && placement.definition.id == "shop.shelf" && !placement.recovered
            && placement.gameObject != null && placement.gameObject.activeSelf);
    }

    void ApplyProgressionState(bool force)
    {
        if (_interior == null || GridService.Instance == null) return;
        int currentTier = Mathf.Clamp(CurrentProgressionTier, 0, 4);
        _highestProgressionTier = Mathf.Max(_highestProgressionTier, currentTier);
        int stage = ResolveProgressionStage(_highestProgressionTier);
        if (!force && currentTier == _appliedProgressionTier && stage == _highestProgressionStage) return;

        _highestProgressionStage = Mathf.Max(_highestProgressionStage, stage);
        _currentZoneSize = ResolveZoneSize(_highestProgressionStage);
        GridService.Instance.RegisterZone(ShopInteriorZoneId, _interior, ZoneOriginLocal, _currentZoneSize,
            new[] { EntryCell }, EntryCell, ResolveServiceCell(_currentZoneSize));
        ApplyInteriorGeometry(_highestProgressionStage);
        PositionPlacementTerminal(_highestProgressionStage);
        ConfigureExpansionNavigation(_highestProgressionStage);
        UpdateExpansionDressing();
        if (_gridOverlay != null) BuildGridOverlay();
        _appliedProgressionTier = currentTier;
        ApplyThemePreset();
        RebuildCatalog();
        RefreshProgressText();
        Debug.Log($"[ShopCustomization] Progression applied: tier={currentTier}, stage={_highestProgressionStage}, zone={_currentZoneSize}, displays={CountActiveDisplays()}/{CurrentDisplayLimit}.");
    }

    void CacheInteriorPresentation()
    {
        if (_interior == null) return;
        _floor = _interior.Find("Floor");
        _wallNorth = _interior.Find("Wall_N");
        _wallSouth = _interior.Find("Wall_S");
        _wallEast = _interior.Find("Wall_E");
        _wallWest = _interior.Find("Wall_W");
        foreach (Light light in _interior.GetComponentsInChildren<Light>(true)) RegisterLightBaseline(light);
    }

    void RegisterLightBaseline(Light light)
    {
        if (light == null || _lightBaselines.ContainsKey(light)) return;
        _lightBaselines[light] = new LightBaseline
        {
            color = light.color,
            intensity = light.intensity,
            range = light.range
        };
    }

    void ApplyInteriorGeometry(int stage)
    {
        float extension = Mathf.Max(0, stage - 1) * 2f;
        float width = 12f + extension;
        float depth = 9f + extension;
        float centerOffset = extension * 0.5f;
        float east = 5.9f + extension;
        float north = 4.4f + extension;

        SetLocalTransform(_floor, new Vector3(centerOffset, -0.1f, centerOffset), new Vector3(width, 0.2f, depth));
        SetLocalTransform(_wallNorth, new Vector3(centerOffset, 1.5f, north), new Vector3(width, 3f, 0.2f));
        SetLocalTransform(_wallSouth, new Vector3(centerOffset, 1.5f, -4.4f), new Vector3(width, 3f, 0.2f));
        SetLocalTransform(_wallEast, new Vector3(east, 1.5f, centerOffset), new Vector3(0.2f, 3f, depth));
        SetLocalTransform(_wallWest, new Vector3(-5.9f, 1.5f, centerOffset), new Vector3(0.2f, 3f, depth));
        EnsureExpansionLights(stage, east, north);
    }

    static void SetLocalTransform(Transform target, Vector3 position, Vector3 scale)
    {
        if (target == null) return;
        target.localPosition = position;
        target.localScale = scale;
    }

    void PositionPlacementTerminal(int stage)
    {
        if (_terminal == null) return;
        float extension = Mathf.Max(0, stage - 1) * 2f;
        _terminal.transform.localPosition = new Vector3(4.15f + extension, 1.55f, 4.13f + extension);
    }

    void EnsureExpansionLights(int stage, float east, float north)
    {
        EnsureExpansionLight("PA_TierExpansionLight_2", 2, stage,
            new Vector3(Mathf.Min(east - 1.8f, 5.8f), 2.45f, Mathf.Min(north - 1.8f, 4.8f)));
        EnsureExpansionLight("PA_TierExpansionLight_3", 3, stage,
            new Vector3(east - 2.0f, 2.45f, north - 2.0f));
    }

    void EnsureExpansionLight(string name, int minimumStage, int currentStage, Vector3 position)
    {
        Transform existing = _interior.Find(name);
        GameObject host = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null) host.transform.SetParent(_interior, false);
        host.transform.localPosition = position;
        Light light = host.GetComponent<Light>();
        if (light == null) light = host.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.84f, 0.58f);
        light.intensity = 2.0f;
        light.range = 8f;
        RegisterLightBaseline(light);
        host.SetActive(currentStage >= minimumStage);
    }

    void ConfigureExpansionNavigation(int stage)
    {
        EnsureExpansionNavigationObjects();
        if (_expansionNavRoot == null || _expansionSurface == null) return;
        if (stage <= 1)
        {
            if (_navMeshRefreshRoutine != null) StopCoroutine(_navMeshRefreshRoutine);
            _navMeshRefreshRoutine = null;
            _expansionSurface.RemoveData();
            _expansionNavRoot.SetActive(false);
            _expansionNavMeshReady = true;
            return;
        }

        _expansionNavRoot.SetActive(true);
        float extension = (stage - 1) * 2f;
        float east = 5.9f + extension;
        float north = 4.4f + extension;
        SetExpansionCollider(_eastExpansionFloor, new Vector3((5.1f + east - 0.15f) * 0.5f, -0.1f, 0f),
            new Vector3(east - 5.25f, 0.2f, 8.2f));
        SetExpansionCollider(_northExpansionFloor,
            new Vector3((east - 5.25f) * 0.5f, -0.1f, (3.6f + north - 0.15f) * 0.5f),
            new Vector3(east + 5.25f, 0.2f, north - 3.75f));
        ConfigureExpansionLink(_eastExpansionLink,
            new Vector3(4.65f, 0.05f, 0f), new Vector3(6.15f, 0.05f, 0f), 5.5f);
        ConfigureExpansionLink(_northExpansionLink,
            new Vector3(0f, 0.05f, 3.25f), new Vector3(0f, 0.05f, 4.75f), 7.0f);

        if (_navMeshRefreshRoutine != null) StopCoroutine(_navMeshRefreshRoutine);
        _navMeshRefreshRoutine = StartCoroutine(RebuildExpansionNavMesh());
    }

    void EnsureExpansionNavigationObjects()
    {
        if (_expansionNavRoot != null) return;
        _expansionNavRoot = new GameObject("PA_ShopTierExpansionNavMesh");
        _expansionNavRoot.transform.SetParent(_interior, false);
        _expansionSurface = _expansionNavRoot.AddComponent<NavMeshSurface>();
        _expansionSurface.collectObjects = CollectObjects.Children;
        _expansionSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

        var east = new GameObject("EastExpansionFloor");
        east.transform.SetParent(_expansionNavRoot.transform, false);
        _eastExpansionFloor = east.AddComponent<BoxCollider>();
        var north = new GameObject("NorthExpansionFloor");
        north.transform.SetParent(_expansionNavRoot.transform, false);
        _northExpansionFloor = north.AddComponent<BoxCollider>();

        var eastLink = new GameObject("EastExpansionLink");
        eastLink.transform.SetParent(_expansionNavRoot.transform, false);
        _eastExpansionLink = eastLink.AddComponent<NavMeshLink>();
        var northLink = new GameObject("NorthExpansionLink");
        northLink.transform.SetParent(_expansionNavRoot.transform, false);
        _northExpansionLink = northLink.AddComponent<NavMeshLink>();
        _expansionNavRoot.SetActive(false);
    }

    static void ConfigureExpansionLink(NavMeshLink link, Vector3 start, Vector3 end, float width)
    {
        if (link == null) return;
        link.agentTypeID = 0;
        link.startPoint = start;
        link.endPoint = end;
        link.width = Mathf.Max(0f, width);
        link.bidirectional = true;
        link.autoUpdate = true;
    }

    static void SetExpansionCollider(BoxCollider collider, Vector3 localPosition, Vector3 size)
    {
        if (collider == null) return;
        collider.transform.localPosition = localPosition;
        collider.transform.localRotation = Quaternion.identity;
        collider.transform.localScale = Vector3.one;
        collider.center = Vector3.zero;
        collider.size = new Vector3(Mathf.Max(0.2f, size.x), Mathf.Max(0.1f, size.y), Mathf.Max(0.2f, size.z));
        collider.enabled = true;
    }

    IEnumerator RebuildExpansionNavMesh()
    {
        _expansionNavMeshReady = false;
        yield return null;
        if (_expansionSurface == null || !_expansionSurface.gameObject.activeInHierarchy)
        {
            _navMeshRefreshRoutine = null;
            yield break;
        }
        Physics.SyncTransforms();
        _expansionSurface.RemoveData();
        _expansionSurface.BuildNavMesh();
        _eastExpansionLink?.UpdateLink();
        _northExpansionLink?.UpdateLink();
        yield return null;
        Vector2Int sampleCell = new Vector2Int(_currentZoneSize.x - 1, _currentZoneSize.y - 1);
        Vector3 sample = GridService.Instance.ZoneCellToWorld(ShopInteriorZoneId, sampleCell, 0.1f);
        bool targetReady = NavMesh.SamplePosition(sample, out NavMeshHit targetHit, 1.35f, NavMesh.AllAreas);
        Transform spawn = _interior.Find("PlayerSpawn_Inside");
        NavMeshHit startHit = default;
        bool startReady = spawn != null && NavMesh.SamplePosition(spawn.position, out startHit,
            2.5f, NavMesh.AllAreas);
        var path = new NavMeshPath();
        _expansionNavMeshReady = startReady && targetReady
            && NavMesh.CalculatePath(startHit.position, targetHit.position, NavMesh.AllAreas, path)
            && path.status == NavMeshPathStatus.PathComplete;
        _navMeshRefreshRoutine = null;
        Debug.Log($"[ShopCustomization] Expansion NavMesh ready={_expansionNavMeshReady}, sampleCell={sampleCell}.");
    }

    void UpdateExpansionDressing()
    {
        if (_interior == null) return;
        float extension = Mathf.Max(0, _highestProgressionStage - 1) * 2f;
        float east = 5.9f + extension;
        float north = 4.4f + extension;
        float centerOffset = extension * 0.5f;
        foreach (Transform child in _interior.GetComponentsInChildren<Transform>(true))
        {
            if (child == _interior || child.IsChildOf(_placedRoot) || (_templateRoot != null && child.IsChildOf(_templateRoot.transform))) continue;
            if (child.name.StartsWith("WallShelf_", StringComparison.Ordinal))
            {
                Vector3 p = child.localPosition; p.x = centerOffset; p.z = north - 0.25f; child.localPosition = p;
                Vector3 scale = child.localScale; scale.x = 7.5f + extension; child.localScale = scale;
            }
            else if (child.name.StartsWith("ShelfGoods_", StringComparison.Ordinal))
            {
                string[] parts = child.name.Split('_');
                int shelfTier = parts.Length > 1 && int.TryParse(parts[1], out int parsedTier) ? parsedTier : 0;
                int itemIndex = parts.Length > 2 && int.TryParse(parts[2], out int parsedIndex) ? parsedIndex : 0;
                Vector3 p = child.localPosition;
                p.x = -3.0f + itemIndex * 1.5f + shelfTier * 0.4f + centerOffset;
                p.z = north - 0.25f;
                child.localPosition = p;
            }
            else if (child.name == "WallTrim_N")
            {
                Vector3 p = child.localPosition; p.x = centerOffset; p.z = north - 0.12f; child.localPosition = p;
                Vector3 scale = child.localScale; scale.x = 12f + extension; child.localScale = scale;
            }
            else if (child.name == "Prop_BushBerries")
            {
                Vector3 p = child.localPosition; p.x = east - 0.8f; p.z = north - 0.8f; child.localPosition = p;
            }
            else if (child.name == "Prop_Flowers")
            {
                Vector3 p = child.localPosition; p.z = north - 0.8f; child.localPosition = p;
            }
        }
    }

    bool IsProcessedThemeUnlocked()
    {
        VillageCultureVisualController culture = VillageCultureVisualController.Instance;
        return culture != null && culture.HasActiveCategory && culture.ActiveCategory == ItemCategory.Processed;
    }

    void CycleThemePreset()
    {
        if (_activeThemeId == DefaultThemeId)
        {
            if (!IsProcessedThemeUnlocked())
            {
                SetStatus("가공품 판매로 마을 변화가 활성화되면 따뜻한 공방 테마가 열립니다.");
                RefreshProgressText();
                return;
            }
            _activeThemeId = ProcessedWarmThemeId;
            SetStatus("따뜻한 공방 테마를 적용했습니다.");
        }
        else
        {
            _activeThemeId = DefaultThemeId;
            SetStatus("기본 잡화점 테마를 적용했습니다.");
        }
        ApplyThemePreset();
        RefreshPanelText();
    }

    void ApplyThemePreset()
    {
        if (_interior == null) return;
        bool warm = _activeThemeId == ProcessedWarmThemeId && IsProcessedThemeUnlocked();
        if (!warm) _activeThemeId = DefaultThemeId;

        ApplyColorBlock(_wallNorth, warm, new Color(0.96f, 0.77f, 0.54f));
        ApplyColorBlock(_wallSouth, warm, new Color(0.96f, 0.77f, 0.54f));
        ApplyColorBlock(_wallEast, warm, new Color(0.91f, 0.65f, 0.43f));
        ApplyColorBlock(_wallWest, warm, new Color(0.91f, 0.65f, 0.43f));

        foreach (PlacementRuntime placement in _placements.Where(p => p.definition != null && p.definition.id == "shop.shelf"
                     && p.gameObject != null && !p.recovered))
        {
            foreach (Renderer renderer in placement.gameObject.GetComponentsInChildren<Renderer>(true))
            {
                if (IsShopSlotDisplayRenderer(renderer.transform)) continue;
                Color color = renderer.name == "Top"
                    ? new Color(0.92f, 0.67f, 0.32f)
                    : new Color(0.57f, 0.28f, 0.15f);
                SetRendererColor(renderer, warm, color);
            }
        }

        foreach (var pair in _lightBaselines.ToList())
        {
            Light light = pair.Key;
            if (light == null) continue;
            LightBaseline baseline = pair.Value;
            light.color = warm ? Color.Lerp(baseline.color, new Color(1f, 0.67f, 0.34f), 0.32f) : baseline.color;
            light.intensity = warm ? baseline.intensity * 1.08f : baseline.intensity;
            light.range = baseline.range;
        }

        PrototypeWorldLabel label = GameObject.Find("PA_StoreDoor_Out")?.GetComponentInChildren<PrototypeWorldLabel>(true);
        if (label != null)
            label.Set(label.label, warm ? new Color(0.56f, 0.22f, 0.10f) : new Color(0.35f, 0.22f, 0.12f), label.fontSize);
        RefreshProgressText();
    }

    static bool IsShopSlotDisplayRenderer(Transform value)
    {
        Transform current = value;
        while (current != null)
        {
            if (current.name.StartsWith("ShopSlot_Display", StringComparison.Ordinal)) return true;
            current = current.parent;
        }
        return false;
    }

    static void ApplyColorBlock(Transform target, bool enabled, Color color)
    {
        if (target == null) return;
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            SetRendererColor(renderer, enabled, color);
    }

    static void SetRendererColor(Renderer renderer, bool enabled, Color color)
    {
        if (renderer == null) return;
        if (!enabled) { renderer.SetPropertyBlock(null); return; }
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        renderer.SetPropertyBlock(block);
    }

    void RefreshProgressText()
    {
        if (_progressText != null)
        {
            string theme = _activeThemeId == ProcessedWarmThemeId ? "따뜻한 공방" : "기본 잡화점";
            string corner = MerchandisingCornerController.Instance != null
                ? MerchandisingCornerController.Instance.BuildCompactCornerSummary()
                : "코너: 같은 분류 2칸+";
            _progressText.text = $"Tier {CurrentProgressionTier} · 배치 구역 {_currentZoneSize.x}×{_currentZoneSize.y} · 진열대 {CountActiveDisplays()}/{CurrentDisplayLimit}\n<size=13>테마: {theme} · {corner}</size>";
        }
        if (_themeButtonText != null)
        {
            string themeLabel = _activeThemeId == ProcessedWarmThemeId ? "따뜻한 공방" : "기본";
            string lockMark = IsProcessedThemeUnlocked() ? string.Empty : " · 잠금";
            _themeButtonText.text = $"테마: {themeLabel}{lockMark}";
        }
    }

    // -------- Visual presentation --------

    void BuildPlacementTerminal()
    {
        _terminal = new GameObject("PA_PlacementLedger", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(BoxCollider));
        _terminal.transform.SetParent(_interior, false);
        _terminal.transform.localPosition = new Vector3(4.15f, 1.55f, 4.13f);
        _terminal.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        _terminal.transform.localScale = Vector3.one * 0.006f;

        var rect = (RectTransform)_terminal.transform;
        rect.sizeDelta = new Vector2(250f, 150f);
        var canvas = _terminal.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 12;
        var collider = _terminal.GetComponent<BoxCollider>();
        collider.size = new Vector3(250f, 150f, 5f);

        var panel = new GameObject("LedgerBoard", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(_terminal.transform, false);
        Stretch((RectTransform)panel.transform);
        panel.GetComponent<Image>().color = new Color(0.24f, 0.14f, 0.07f, 0.98f);

        var paper = new GameObject("LedgerPaper", typeof(RectTransform), typeof(Image));
        paper.transform.SetParent(panel.transform, false);
        var paperRt = (RectTransform)paper.transform;
        paperRt.anchorMin = new Vector2(0.5f, 0.5f); paperRt.anchorMax = paperRt.anchorMin;
        paperRt.sizeDelta = new Vector2(224f, 124f);
        paper.GetComponent<Image>().color = new Color(0.94f, 0.88f, 0.70f, 1f);

        var label = CreateText(paper.transform, "LedgerLabel", Vector2.zero, new Vector2(205f, 105f), 24f,
            new Color(0.12f, 0.31f, 0.21f), FontStyles.Bold);
        label.text = "상점 배치 장부\n<color=#6D4932><size=16>[Space] 가구 정리</size></color>";
        label.alignment = TextAlignmentOptions.Center;
        _terminal.AddComponent<ShopCustomizationTerminal>().Bind(this);
    }

    void BuildScreenUI()
    {
        var canvasGo = new GameObject("ShopCustomizationCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 180;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        _panel = new GameObject("PlacementPanel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)_panel.transform;
        rt.anchorMin = new Vector2(1f, 0.5f); rt.anchorMax = rt.anchorMin; rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-24f, 0f); rt.sizeDelta = new Vector2(500f, 650f);
        _panel.GetComponent<Image>().color = new Color(0.96f, 0.91f, 0.79f, 0.97f);

        var title = CreateText(_panel.transform, "Title", new Vector2(0f, 278f), new Vector2(450f, 54f), 30f,
            new Color(0.10f, 0.38f, 0.25f), FontStyles.Bold);
        title.text = "상점 가구 배치";
        _progressText = CreateText(_panel.transform, "Progress", new Vector2(0f, 225f), new Vector2(440f, 54f), 16f,
            new Color(0.36f, 0.24f, 0.15f), FontStyles.Bold);
        _progressText.textWrappingMode = TextWrappingModes.Normal;
        _selectionText = CreateText(_panel.transform, "Selection", new Vector2(0f, 158f), new Vector2(440f, 72f), 21f,
            new Color(0.16f, 0.13f, 0.10f), FontStyles.Bold);
        _statusText = CreateText(_panel.transform, "Status", new Vector2(0f, -153f), new Vector2(430f, 56f), 16.5f,
            new Color(0.28f, 0.20f, 0.14f), FontStyles.Normal);
        _statusText.alignment = TextAlignmentOptions.TopLeft;
        _statusText.textWrappingMode = TextWrappingModes.Normal;

        CreateButton(_panel.transform, "Previous", "◀ 이전", new Vector2(-120f, 92f), new Vector2(200f, 48f), PreviousCatalog);
        CreateButton(_panel.transform, "Next", "다음 ▶", new Vector2(120f, 92f), new Vector2(200f, 48f), NextCatalog);
        CreateButton(_panel.transform, "Place", "선택 가구 배치", new Vector2(0f, 28f), new Vector2(420f, 52f), BeginSelectedPlacement);
        CreateButton(_panel.transform, "Move", "가까운 가구 이동", new Vector2(-110f, -42f), new Vector2(205f, 48f), BeginMoveNearest);
        CreateButton(_panel.transform, "Recover", "가까운 가구 회수", new Vector2(110f, -42f), new Vector2(205f, 48f), RecoverNearest);
        Button themeButton = CreateButton(_panel.transform, "Theme", "테마: 기본", new Vector2(0f, -91f), new Vector2(420f, 42f), CycleThemePreset,
            new Color(0.52f, 0.33f, 0.20f));
        _themeButtonText = themeButton.GetComponentInChildren<TextMeshProUGUI>();
        CreateButton(_panel.transform, "Close", "장부 닫기", new Vector2(0f, -272f), new Vector2(260f, 48f), CloseCustomization,
            new Color(0.55f, 0.28f, 0.20f));

        var help = CreateText(_panel.transform, "Help", new Vector2(0f, -223f), new Vector2(430f, 36f), 15f,
            new Color(0.20f, 0.35f, 0.27f), FontStyles.Bold);
        help.text = "WASD 위치 · R 90° 회전 · 바닥 클릭 확정";
        _panel.SetActive(false);
    }

    void BuildGridOverlay()
    {
        bool wasActive = _gridOverlay != null && _gridOverlay.activeSelf;
        if (_gridOverlay != null) Destroy(_gridOverlay);
        _gridOverlay = new GameObject("PA_ShopGridOverlay");
        _gridOverlay.transform.SetParent(_interior, false);
        Material lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.color = new Color(0.45f, 0.78f, 0.57f, 0.72f);
        float half = GridCellSize * 0.5f;
        float xMin = ZoneOriginLocal.x - half;
        float xMax = ZoneOriginLocal.x + (_currentZoneSize.x - 1) * GridCellSize + half;
        float zMin = ZoneOriginLocal.z - half;
        float zMax = ZoneOriginLocal.z + (_currentZoneSize.y - 1) * GridCellSize + half;
        for (int x = 0; x <= _currentZoneSize.x; x++)
            AddGridLine(new Vector3(xMin + x * GridCellSize, PlacementHeight + 0.025f, zMin),
                new Vector3(xMin + x * GridCellSize, PlacementHeight + 0.025f, zMax), lineMaterial);
        for (int y = 0; y <= _currentZoneSize.y; y++)
            AddGridLine(new Vector3(xMin, PlacementHeight + 0.025f, zMin + y * GridCellSize),
                new Vector3(xMax, PlacementHeight + 0.025f, zMin + y * GridCellSize), lineMaterial);
        _gridOverlay.SetActive(wasActive);
    }

    void AddGridLine(Vector3 a, Vector3 b, Material material)
    {
        var go = new GameObject("GridLine", typeof(LineRenderer));
        go.transform.SetParent(_gridOverlay.transform, false);
        var line = go.GetComponent<LineRenderer>();
        line.useWorldSpace = false; line.positionCount = 2; line.startWidth = 0.035f; line.endWidth = 0.035f;
        line.material = material; line.startColor = material.color; line.endColor = material.color;
        line.SetPosition(0, a); line.SetPosition(1, b);
    }

    void DrawHighlights(Vector2Int anchor, PlaceableDefinition definition, int rotation, bool valid)
    {
        ClearHighlights();
        Color footprintColor = valid ? new Color(0.35f, 0.82f, 0.49f, 0.42f) : new Color(0.95f, 0.30f, 0.24f, 0.48f);
        foreach (var cell in GridService.Instance.GetZoneFootprintCells(anchor, definition.footprint, rotation))
            CreateCellHighlight(cell, footprintColor);
        foreach (var cell in GridService.Instance.GetZoneFootprintCells(anchor, definition.clearance, rotation))
            CreateCellHighlight(cell, new Color(0.97f, 0.73f, 0.28f, 0.30f));
        CreateCellHighlight(EntryCell, new Color(0.96f, 0.45f, 0.32f, 0.30f));
    }

    void CreateCellHighlight(Vector2Int cell, Color color)
    {
        if (!GridService.Instance.IsZoneCellInBounds(ShopInteriorZoneId, cell)) return;
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "PlacementCellHighlight";
        quad.transform.position = GridService.Instance.ZoneCellToWorld(ShopInteriorZoneId, cell, PlacementHeight + 0.035f);
        quad.transform.rotation = _interior.rotation * Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = Vector3.one * (GridCellSize * 0.88f);
        Destroy(quad.GetComponent<Collider>());
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.color = color;
        quad.GetComponent<Renderer>().material = material;
        _cellHighlights.Add(quad);
    }

    void ClearHighlights()
    {
        foreach (var highlight in _cellHighlights) if (highlight != null) Destroy(highlight);
        _cellHighlights.Clear();
    }

    void CreatePreviewObject(GameObject prefab)
    {
        DestroyPreviewObject();
        if (prefab == null) return;
        if (prefab.GetComponentInChildren<ShopSlot>(true) != null)
        {
            _previewObject = CreateRendererOnlyPreview(prefab);
            return;
        }
        _previewObject = Instantiate(prefab, _placedRoot);
        _previewObject.name = "PA_PlacementPreview";
        foreach (var collider in _previewObject.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (var obstacle in _previewObject.GetComponentsInChildren<NavMeshObstacle>(true)) obstacle.enabled = false;
        foreach (var behaviour in _previewObject.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
    }

    GameObject CreateRendererOnlyPreview(GameObject source)
    {
        var root = new GameObject("PA_PlacementPreview");
        root.transform.SetParent(_placedRoot, false);
        foreach (MeshRenderer sourceRenderer in source.GetComponentsInChildren<MeshRenderer>(true))
        {
            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null) continue;
            if (sourceRenderer.GetComponentInParent<Canvas>() != null) continue;

            var visual = new GameObject($"Preview_{sourceRenderer.name}", typeof(MeshFilter), typeof(MeshRenderer));
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = source.transform.InverseTransformPoint(sourceRenderer.transform.position);
            visual.transform.localRotation = Quaternion.Inverse(source.transform.rotation) * sourceRenderer.transform.rotation;
            Vector3 rootScale = source.transform.lossyScale;
            Vector3 childScale = sourceRenderer.transform.lossyScale;
            visual.transform.localScale = new Vector3(
                SafeScaleRatio(childScale.x, rootScale.x),
                SafeScaleRatio(childScale.y, rootScale.y),
                SafeScaleRatio(childScale.z, rootScale.z));
            visual.GetComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = sourceRenderer.sharedMaterials;
            renderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
            renderer.receiveShadows = sourceRenderer.receiveShadows;
        }
        return root;
    }

    static float SafeScaleRatio(float value, float divisor)
    {
        return Mathf.Abs(divisor) < 0.0001f ? value : value / divisor;
    }

    void DestroyPreviewObject()
    {
        if (_previewObject != null) Destroy(_previewObject);
        _previewObject = null;
    }

    static void TintPreview(GameObject target, bool valid)
    {
        if (target == null) return;
        Color tint = valid ? new Color(0.42f, 1f, 0.58f, 0.82f) : new Color(1f, 0.34f, 0.28f, 0.82f);
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", tint); block.SetColor("_Color", tint);
        foreach (var renderer in target.GetComponentsInChildren<Renderer>(true)) renderer.SetPropertyBlock(block);
    }

    static void ClearPreviewTint(GameObject target)
    {
        if (target == null) return;
        foreach (var renderer in target.GetComponentsInChildren<Renderer>(true)) renderer.SetPropertyBlock(null);
    }

    void RefreshPanelText()
    {
        RefreshProgressText();
        if (_selectionText == null) return;
        if (_catalog.Count == 0)
        {
            _selectionText.text = "배치 가능 가구 없음\n<size=15>설계도를 보유하거나 기존 가구를 회수하세요.</size>";
            return;
        }
        CatalogEntry entry = _catalog[_catalogIndex];
        string footprint = FootprintLabel(entry.definition.footprint);
        _selectionText.text = $"{entry.Label}\n<size=15>{footprint} · {entry.definition.category}</size>";
    }

    static string FootprintLabel(Vector2Int[] cells)
    {
        if (cells == null || cells.Length == 0) return "0셀";
        int minX = cells.Min(c => c.x), maxX = cells.Max(c => c.x);
        int minY = cells.Min(c => c.y), maxY = cells.Max(c => c.y);
        return $"{maxX - minX + 1}×{maxY - minY + 1}셀";
    }

    void SetStatus(string message)
    {
        if (_statusText != null) _statusText.text = message ?? string.Empty;
    }

    Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size,
        UnityEngine.Events.UnityAction action, Color? color = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position; rt.sizeDelta = size;
        go.GetComponent<Image>().color = color ?? new Color(0.12f, 0.45f, 0.29f);
        var button = go.GetComponent<Button>(); button.onClick.AddListener(action);
        var text = CreateText(go.transform, "Label", Vector2.zero, size - new Vector2(14f, 8f), 18f, Color.white, FontStyles.Bold);
        text.text = label;
        return button;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, Vector2 position, Vector2 size,
        float fontSize, Color color, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position; rt.sizeDelta = size;
        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize; text.fontStyle = style; text.color = color;
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        return text;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }
}

// Collider-facing adapter keeps the controller itself independent from the ledger presentation object.
public class ShopCustomizationTerminal : MonoBehaviour, IInteractable
{
    ShopCustomizationController _controller;
    public void Bind(ShopCustomizationController controller) => _controller = controller;
    public void Interact(GameObject interactor) => _controller?.ToggleFromTerminal(interactor);
    public string GetInteractPrompt() => _controller != null ? _controller.GetTerminalPrompt() : "상점 배치 장부";
}
