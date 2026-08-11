using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
#endif

// WORLD-010: player-facing composition of the already validated WORLD-001~009
// seams. It remains WorldSandbox-only and delegates every mutation to the
// existing grid, placement, crafting, shop, economy and save authorities.
[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public sealed class WorldAlphaPlayableController : MonoBehaviour
{
    const float LandmarkReachDistance = 4f;

    WorldGridService _grid;
    WorldGridDebugView _gridDebug;
    WorldGeneratedIslandDebugView _islandView;
    WorldBuildingPlacementService _buildings;
    WorldGameplayAdapterService _adapter;
    WorldPlayerTraversalGuard _traversalGuard;
    Vector3 _movementOrigin;
    bool _projectionRefreshRequested;
    bool _developmentOverlayVisible;
    bool _startPromptVisible;
    string _lastAction = "플레이 가능한 섬 생활을 준비하고 있습니다.";

    public static WorldAlphaPlayableController Instance { get; private set; }
    public bool IsReady { get; private set; }
    public bool HasMoved { get; private set; }
    public bool HasGathered { get; private set; }
    public bool HasCrafted { get; private set; }
    public bool HasTerraformed { get; private set; }
    public bool HasPlacedBuilding { get; private set; }
    public bool HasMovedBuilding { get; private set; }
    public bool HasMovedSalesDisplay { get; private set; }
    public bool HasStockedProduct { get; private set; }
    public bool HasOpenedShop { get; private set; }
    public bool HasCustomerPurchase { get; private set; }
    public bool HasRevenue { get; private set; }
    public bool HasSaved { get; private set; }
    public bool HasRestored { get; private set; }
    public bool HasStartedBeta { get; private set; }
    public bool HasReachedShop { get; private set; }
    public bool HasReachedWorkbench { get; private set; }
    public bool DevelopmentOverlayVisible => _developmentOverlayVisible;
    public bool StartPromptVisible => IsReady && _startPromptVisible;
    public bool PlayerFacingHudVisible => IsReady && HasStartedBeta && !_developmentOverlayVisible;
    public string CurrentPlayerObjective => ResolvePlayerObjective();
    public string LastAction => _lastAction;
    public WorldGameplayAdapterService Adapter => _adapter;
    public WorldGridService Grid => _grid;
    public WorldBuildingPlacementService Buildings => _buildings;
    public WorldGeneratedIslandDebugView IslandView => _islandView;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapWorldSandbox()
    {
        if (SceneManager.GetActiveScene().name != "WorldSandbox" ||
            FindFirstObjectByType<WorldAlphaPlayableController>() != null)
        {
            return;
        }

        WorldGridService grid = FindFirstObjectByType<WorldGridService>();
        if (grid != null) grid.gameObject.AddComponent<WorldAlphaPlayableController>();
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
        _grid = GetComponent<WorldGridService>();
        _gridDebug = GetComponent<WorldGridDebugView>();
        _islandView = GetComponent<WorldGeneratedIslandDebugView>();
        _buildings = GetComponent<WorldBuildingPlacementService>();

        float started = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - started < 20f)
        {
            _adapter = WorldGameplayAdapterService.Instance ??
                       FindFirstObjectByType<WorldGameplayAdapterService>();
            if (_adapter != null && _adapter.IsReady) break;
            yield return null;
        }

        if (_grid == null || _gridDebug == null || _islandView == null ||
            _buildings == null || _adapter == null || !_adapter.IsReady)
        {
            _lastAction = "World Alpha could not resolve one of its validated adapters.";
            Debug.LogError($"[WORLD-010] RUNTIME_FAIL {_lastAction}");
            yield break;
        }

        ConfigurePlayerTraversal();
        SubscribeWorldChanges();
        if (!_islandView.DisplayGridSnapshot(_adapter.BoundSeed))
        {
            _lastAction = "The generated 128x128 projection could not be displayed.";
            Debug.LogError($"[WORLD-010] RUNTIME_FAIL {_lastAction}");
            yield break;
        }
        ConfigureCameraFollow();

        _movementOrigin = _adapter.PlayerRoot.transform.position;
        IsReady = true;
        _startPromptVisible = true;
        ApplyDevelopmentMode(false);
        _lastAction = "새 섬 생활을 시작해 첫날 동선을 익혀 보세요.";
        Debug.Log("[WORLD-010] RUNTIME_READY playableWorldAlpha=true movement=true projection=64chunks");
    }

    void Update()
    {
        if (!IsReady) return;
        if (WasDevelopmentTogglePressed())
            SetDevelopmentOverlayVisible(!_developmentOverlayVisible);
        if (!HasStartedBeta && (Input.GetKeyDown(KeyCode.Return) ||
                                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                                Input.GetKeyDown(KeyCode.Space)))
        {
            BeginNewGame();
        }
        if (!HasMoved && _adapter.PlayerRoot != null)
        {
            Vector3 delta = _adapter.PlayerRoot.transform.position - _movementOrigin;
            delta.y = 0f;
            HasMoved = delta.sqrMagnitude >= 2.25f;
        }
        RefreshPlayerFacingProgress();
        if (_adapter.CustomerPurchaseCompleted)
        {
            HasCustomerPurchase = true;
            HasRevenue = EconomyService.Instance != null &&
                         EconomyService.Instance.CumulativeRevenue > 0;
        }
    }

    void LateUpdate()
    {
        if (!IsReady || !_projectionRefreshRequested) return;
        _projectionRefreshRequested = false;
        if (_islandView.DisplayGridSnapshot(_adapter.BoundSeed))
            ConfigureCameraFollow(false);
    }

    void OnDestroy()
    {
        UnsubscribeWorldChanges();
        if (Instance == this) Instance = null;
    }

    void ConfigurePlayerTraversal()
    {
        GameObject player = _adapter.PlayerRoot;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null) controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.42f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.stepOffset = 0.35f;
        controller.slopeLimit = 45f;
        if (player.GetComponent<PlayerController>() == null)
            player.AddComponent<PlayerController>();
        _traversalGuard = player.GetComponent<WorldPlayerTraversalGuard>();
        if (_traversalGuard == null)
            _traversalGuard = player.AddComponent<WorldPlayerTraversalGuard>();
        _traversalGuard.Configure(_grid);
    }

    void ConfigureCameraFollow(bool resetPose = true)
    {
        Camera camera = Camera.main;
        if (camera == null || _adapter?.PlayerRoot == null) return;
        Transform target = _adapter.PlayerRoot.transform;
        camera.orthographic = true;
        camera.orthographicSize = 22f;
        if (resetPose)
        {
            camera.transform.position = target.position + new Vector3(18f, 26f, -18f);
            camera.transform.LookAt(target.position + Vector3.up * 0.75f);
        }
        CameraController follow = camera.GetComponent<CameraController>();
        if (follow == null) follow = camera.gameObject.AddComponent<CameraController>();
        follow.target = target;
        follow.smoothSpeed = 6f;
    }

    void SubscribeWorldChanges()
    {
        _grid.Terraform.Changed -= OnTerraformChanged;
        _grid.Terraform.Changed += OnTerraformChanged;
        _grid.SurfaceEditor.Changed -= OnSurfaceChanged;
        _grid.SurfaceEditor.Changed += OnSurfaceChanged;
    }

    void UnsubscribeWorldChanges()
    {
        if (_grid == null) return;
        _grid.Terraform.Changed -= OnTerraformChanged;
        _grid.SurfaceEditor.Changed -= OnSurfaceChanged;
    }

    void OnTerraformChanged(WorldTerraformEditResult result)
    {
        if (result.Succeeded)
        {
            HasTerraformed = true;
            RequestProjectionRefresh();
        }
    }

    void OnSurfaceChanged(WorldSurfaceEditResult result)
    {
        if (result.Succeeded) RequestProjectionRefresh();
    }

    public void RequestProjectionRefresh()
    {
        _projectionRefreshRequested = true;
    }

    public bool TryGatherTimber(out string reason)
    {
        reason = string.Empty;
        bool success = _adapter != null &&
                       _adapter.TryGatherNext(WorldResourceKind.Timber, out _, out reason);
        HasGathered |= success;
        _lastAction = success ? "Timber entered the existing Inventory." : reason;
        return success;
    }

    public bool TryCraftProduct(out string reason)
    {
        reason = string.Empty;
        bool success = _adapter != null && _adapter.TryCraftPlank(out reason);
        HasCrafted |= success;
        _lastAction = success ? "The existing B05 workbench crafted a Plank." : reason;
        return success;
    }

    public bool TryStockProduct(out ShopSlot slot, out string reason)
    {
        slot = null;
        reason = string.Empty;
        bool success = _adapter != null &&
                       _adapter.TryStockCraftedProduct(out slot, out reason);
        HasStockedProduct |= success;
        _lastAction = success ? "The crafted Plank is visible in a functional ShopSlot." : reason;
        return success;
    }

    public bool TryOpenShop(out string reason)
    {
        reason = string.Empty;
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 2;
        bool success = _adapter != null && _adapter.TryOpenShopForNight(day, out reason);
        HasOpenedShop |= success;
        _lastAction = success ? "The existing Day/Night authority opened the night shop." : reason;
        return success;
    }

    public bool TrySendCustomer(out string reason)
    {
        reason = string.Empty;
        ShopSlot target = _adapter?.RuntimeShopSlots.FirstOrDefault(slot =>
            slot != null && !slot.IsEmpty);
        bool success = _adapter != null && _adapter.TryBeginCustomerVisit(target, out reason);
        _lastAction = success ? "A customer is walking to the moved functional display." : reason;
        return success;
    }

    public WorldTerraformEditResult RaiseCell(Vector2Int coordinate)
    {
        WorldTerraformEditResult result = _grid.Terraform.Raise(coordinate);
        _lastAction = result.Succeeded
            ? $"Raised cell {coordinate} and refreshed its chunk projection."
            : $"Terraform blocked safely: {result.Failure}.";
        return result;
    }

    public WorldBuildingPlacementResult PlaceOrMoveShed(
        Vector2Int coordinate,
        int quarterTurns)
    {
        WorldBuildingPlacementResult result;
        if (_buildings.TryGetPlacement(WorldBuildingPlacementService.PrototypeInstanceId,
                out _))
        {
            result = _buildings.TryMove(
                WorldBuildingPlacementService.PrototypeInstanceId, coordinate, quarterTurns);
            HasMovedBuilding |= result.Succeeded;
        }
        else
        {
            result = _buildings.TryPlace(
                WorldBuildingPlacementService.PrototypeInstanceId, coordinate, quarterTurns);
            HasPlacedBuilding |= result.Succeeded;
        }
        _lastAction = result.Succeeded
            ? $"Storage shed transaction committed at {coordinate}."
            : $"Storage shed transaction rolled back safely: {result.Failure}.";
        return result;
    }

    public bool TryMoveSalesDisplay(
        int gridX,
        int gridY,
        int quarterTurns,
        out string reason)
    {
        reason = string.Empty;
        bool success = _adapter != null &&
                       _adapter.TryMoveSalesDisplay(gridX, gridY, quarterTurns, out reason);
        HasMovedSalesDisplay |= success;
        _lastAction = success ? "Moved the same functional B01 ShopSlots; stock authority was preserved." : reason;
        return success;
    }

    public bool MovePlayerToCellForValidation(Vector2Int coordinate)
    {
        if (_traversalGuard == null || !_grid.CellToWorld(coordinate, out Vector3 world))
            return false;
        return _traversalGuard.TryTeleportTo(world + Vector3.up * 0.05f);
    }

    public bool BeginNewGame()
    {
        if (!IsReady || _adapter?.PlayerRoot == null) return false;
        HasStartedBeta = true;
        _startPromptVisible = false;
        _movementOrigin = _adapter.PlayerRoot.transform.position;
        HasMoved = false;
        HasReachedShop = false;
        HasReachedWorkbench = false;
        _lastAction = "Day 1 시작 · 먼저 움직인 뒤 잡화점과 제작 작업대를 확인하세요.";
        return GameClock.Instance != null && GameClock.Instance.CurrentDay == 1;
    }

    public void SetDevelopmentOverlayVisible(bool visible)
    {
        _developmentOverlayVisible = visible;
        ApplyDevelopmentMode(visible);
    }

    void ApplyDevelopmentMode(bool visible)
    {
        if (_gridDebug != null) _gridDebug.enabled = visible;
        Transform gridLines = transform.Find("WorldGridDebugLines_Runtime");
        if (gridLines != null) gridLines.gameObject.SetActive(visible);

        WorldBuildingPlacementDebugController placementDebug =
            GetComponent<WorldBuildingPlacementDebugController>();
        if (placementDebug != null) placementDebug.enabled = visible;
        if (_islandView != null) _islandView.enabled = visible;

        WorldNavigationService navigation = GetComponent<WorldNavigationService>();
        if (navigation != null) navigation.SetDebugOverlayVisible(visible);
        if (CoreSlicePresentationMode.Instance != null)
            CoreSlicePresentationMode.Instance.SetDevelopmentOverlaysVisible(visible);
    }

    bool WasDevelopmentTogglePressed()
    {
        try
        {
            return Input.GetKeyDown(KeyCode.F10);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    void RefreshPlayerFacingProgress()
    {
        if (!HasStartedBeta || _adapter?.PlayerRoot == null) return;
        Vector3 player = _adapter.PlayerRoot.transform.position;
        if (!HasReachedShop && _adapter.RuntimeShop != null &&
            FlatDistance(player, _adapter.RuntimeShop.transform.position) <= LandmarkReachDistance)
        {
            HasReachedShop = true;
            _lastAction = "잡화점 위치를 확인했습니다. 낮에 준비한 상품은 이곳에서 밤에 판매합니다.";
        }
        if (!HasReachedWorkbench && _adapter.RuntimeWorkbench != null &&
            FlatDistance(player, _adapter.RuntimeWorkbench.transform.position) <= LandmarkReachDistance)
        {
            HasReachedWorkbench = true;
            _lastAction = "제작 작업대 위치를 확인했습니다. 자원을 가공해 더 가치 있는 상품을 만드세요.";
        }
    }

    string ResolvePlayerObjective()
    {
        if (!IsReady) return "섬 생활을 준비하고 있습니다.";
        if (!HasStartedBeta) return "새 섬 생활을 시작하세요.";
        if (!HasMoved) return "WASD로 움직여 이동 방법을 익히세요.";
        if (!HasReachedShop)
            return $"노란 표지의 P.A. 잡화점을 찾으세요 · {TargetHint(_adapter?.RuntimeShop?.transform)}";
        if (!HasReachedWorkbench)
            return $"초록 표지의 제작 작업대를 찾으세요 · {TargetHint(_adapter?.RuntimeWorkbench?.transform)}";
        return "첫 동선 확인 완료 · 낮 자원을 모아 상품 준비를 시작하세요.";
    }

    string TargetHint(Transform target)
    {
        if (target == null || _adapter?.PlayerRoot == null) return "위치 확인 중";
        Vector3 delta = target.position - _adapter.PlayerRoot.transform.position;
        delta.y = 0f;
        float distance = delta.magnitude;
        if (distance < 0.1f) return "도착";
        float x = delta.x / distance;
        float z = delta.z / distance;
        string direction;
        if (Mathf.Abs(x) > 0.72f) direction = x > 0f ? "동쪽" : "서쪽";
        else if (Mathf.Abs(z) > 0.72f) direction = z > 0f ? "북쪽" : "남쪽";
        else if (x > 0f) direction = z > 0f ? "북동쪽" : "남동쪽";
        else direction = z > 0f ? "북서쪽" : "남서쪽";
        return $"{direction} {distance:0}m";
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    async void SaveFromUi()
    {
        try
        {
            await _adapter.RuntimeSaveManager.SaveGameAsync();
            HasSaved = true;
            _lastAction = "Saved seed, sparse deltas, building, display, player and gameplay state.";
        }
        catch (Exception ex)
        {
            _lastAction = $"Save failed: {ex.Message}";
            Debug.LogException(ex);
        }
    }

    async void LoadFromUi()
    {
        try
        {
            await _adapter.RuntimeSaveManager.LoadGameAsync();
            HasRestored = true;
            _lastAction = "Restored the playable World Alpha through the existing SaveManager.";
        }
        catch (Exception ex)
        {
            _lastAction = $"Load failed: {ex.Message}";
            Debug.LogException(ex);
        }
    }

    void OnGUI()
    {
        if (!Application.isPlaying || !IsReady) return;
        if (!HasStartedBeta)
        {
            DrawStartPrompt();
            return;
        }
        if (!_developmentOverlayVisible)
        {
            DrawPlayerFacingHud();
            return;
        }
        float left = Mathf.Max(740f, Screen.width - 474f);
        GUILayout.BeginArea(new Rect(left, 18f, 456f, 525f), GUI.skin.box);
        GUILayout.Label("M70 PLAYABLE WORLD ALPHA");
        GUILayout.Label($"Seed {_adapter.BoundSeed}  |  128x128 cells  |  {_islandView.GeneratedChunkCount} chunks");
        GUILayout.Label("WASD move · click a cell · R/F terraform · B/M shed · Q/E rotate · Enter commit");
        GUILayout.Space(4f);
        GUILayout.Label(ChecklistSummary());
        GUILayout.Space(4f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Gather Timber")) TryGatherTimber(out _);
        if (GUILayout.Button("Craft Plank")) TryCraftProduct(out _);
        if (GUILayout.Button("Stock B01")) TryStockProduct(out _, out _);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Move Display"))
        {
            int nextX = _adapter.SalesDisplayGrid.x >= 1
                ? -1
                : _adapter.SalesDisplayGrid.x + 1;
            TryMoveSalesDisplay(nextX, 0,
                (_adapter.SalesDisplayQuarterTurns + 1) % 4, out _);
        }
        if (GUILayout.Button("Open Night")) TryOpenShop(out _);
        if (GUILayout.Button("Send Customer")) TrySendCustomer(out _);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save (F5)")) SaveFromUi();
        if (GUILayout.Button("Restore (F9)")) LoadFromUi();
        GUILayout.EndHorizontal();
        GUILayout.Space(4f);
        GUILayout.Label(_lastAction);
        GUILayout.EndArea();
    }

    void DrawStartPrompt()
    {
        float width = Mathf.Min(620f, Screen.width - 40f);
        float height = 260f;
        float left = (Screen.width - width) * 0.5f;
        float top = (Screen.height - height) * 0.5f;
        GUILayout.BeginArea(new Rect(left, top, width, height), GUI.skin.box);
        GUILayout.Space(12f);
        GUILayout.Label("PROJECT P.A. · 새로운 섬 생활");
        GUILayout.Space(12f);
        GUILayout.Label("낮에는 섬을 돌아다니며 자원과 상품을 준비하고,\n밤에는 마을의 잡화점을 열어 주민을 맞이합니다.");
        GUILayout.Space(12f);
        GUILayout.Label("첫날 목표 · 이동 방법을 익히고 잡화점과 제작 작업대 위치를 확인하세요.");
        GUILayout.Space(12f);
        if (GUILayout.Button("새 섬 생활 시작  [Enter / Space]", GUILayout.Height(44f)))
            BeginNewGame();
        GUILayout.Space(8f);
        GUILayout.Label("저장: F5 또는 Esc 메뉴 · 불러오기: F9 · 개발 정보: F10");
        GUILayout.EndArea();
    }

    void DrawPlayerFacingHud()
    {
        float width = Mathf.Min(720f, Screen.width - 420f);
        float left = (Screen.width - width) * 0.5f;
        GUILayout.BeginArea(new Rect(left, 18f, width, 126f), GUI.skin.box);
        GUILayout.Label($"Day 1 · 첫 마을 동선   |   {CurrentPlayerObjective}");
        GUILayout.Space(4f);
        GUILayout.Label(_lastAction);
        GUILayout.Space(4f);
        GUILayout.Label("WASD 이동 · 가까운 오브젝트 Space 상호작용 · F5 저장 · F9 불러오기 · Esc 메뉴");
        GUILayout.EndArea();
    }

    string ChecklistSummary()
    {
        return $"{Mark(HasMoved)} move   {Mark(HasGathered)} gather   {Mark(HasCrafted)} craft   " +
               $"{Mark(HasTerraformed)} terraform\n" +
               $"{Mark(HasPlacedBuilding)} place shed   {Mark(HasMovedBuilding)} move shed   " +
               $"{Mark(HasMovedSalesDisplay)} move display\n" +
               $"{Mark(HasStockedProduct)} stock   {Mark(HasOpenedShop)} open   " +
               $"{Mark(HasCustomerPurchase)} purchase   {Mark(HasRevenue)} revenue\n" +
               $"{Mark(HasSaved)} save   {Mark(HasRestored)} restore";
    }

    static string Mark(bool value) => value ? "[x]" : "[ ]";
}

[DisallowMultipleComponent]
public sealed class WorldPlayerTraversalGuard : MonoBehaviour
{
    WorldGridService _grid;
    CharacterController _controller;
    Vector3 _lastSafePosition;
    bool _configured;

    public Vector3 LastSafePosition => _lastSafePosition;

    public void Configure(WorldGridService grid)
    {
        _grid = grid;
        _controller = GetComponent<CharacterController>();
        _configured = TryResolveSafe(transform.position, out _lastSafePosition);
    }

    void LateUpdate()
    {
        if (!_configured || _grid == null) return;
        if (TryResolveSafe(transform.position, out Vector3 safe))
        {
            _lastSafePosition = safe;
            return;
        }
        Teleport(_lastSafePosition);
    }

    public bool TryTeleportTo(Vector3 requested)
    {
        if (!TryResolveSafe(requested, out Vector3 safe)) return false;
        _lastSafePosition = safe;
        _configured = true;
        Teleport(safe);
        return true;
    }

    bool TryResolveSafe(Vector3 position, out Vector3 safe)
    {
        safe = position;
        if (_grid == null || !_grid.WorldToCell(position, out Vector2Int coordinate) ||
            !_grid.TryGetCell(coordinate, out WorldCellData cell) ||
            !cell.IsWalkable || cell.HasWater ||
            !_grid.CellToWorld(coordinate, out Vector3 ground))
        {
            return false;
        }
        if (position.y < ground.y - 1.25f || position.y > ground.y + 4f)
            return false;
        safe = new Vector3(position.x, ground.y + 0.05f, position.z);
        return true;
    }

    void Teleport(Vector3 position)
    {
        if (_controller == null) _controller = GetComponent<CharacterController>();
        bool wasEnabled = _controller != null && _controller.enabled;
        if (wasEnabled) _controller.enabled = false;
        transform.position = position;
        if (wasEnabled) _controller.enabled = true;
    }
}

#if UNITY_EDITOR
public static class PA_WorldAlphaIntegrationTools
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.WORLD010.Active";
    const string FailedKey = "PA.WORLD010.Failed";
    const string ConsoleErrorKey = "PA.WORLD010.ConsoleErrors";
    const string WaitFramesKey = "PA.WORLD010.WaitFrames";
    const string StageKey = "PA.WORLD010.Stage";
    const string SaveDirectoryKey = "PA.WORLD010.SaveDirectory";
    const string ExpectedChecksumKey = "PA.WORLD010.ExpectedChecksum";
    const string ExpectedBuildingXKey = "PA.WORLD010.BuildingX";
    const string ExpectedBuildingZKey = "PA.WORLD010.BuildingZ";
    const string ExpectedBuildingRotationKey = "PA.WORLD010.BuildingRotation";
    const string ExpectedPlayerXKey = "PA.WORLD010.PlayerX";
    const string ExpectedPlayerZKey = "PA.WORLD010.PlayerZ";
    const string ExpectedMoneyKey = "PA.WORLD010.Money";
    const string ExpectedRevenueKey = "PA.WORLD010.Revenue";
    const string ExpectedResourcesKey = "PA.WORLD010.Resources";

    static WorldAlphaPlayableController _alpha;
    static WorldGameplayAdapterService _adapter;
    static WorldPersistenceService _persistence;
    static ShopSlot _saleSlot;
    static Task _ioTask;
    static float _stageStarted;

    [InitializeOnLoadMethod]
    static void ResumeAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        SubscribeCallbacks();
        if (EditorApplication.isPlaying)
        {
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
    }

    [MenuItem("Project PA/World/WORLD-010/Validate M70 Playable World Alpha")]
    public static void RunWorld010Validation()
    {
        RunWorld010ValidationInternal();
    }

    public static void RunWorld010ValidationBatch()
    {
        RunWorld010ValidationInternal();
    }

    static void RunWorld010ValidationInternal()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetInt(ConsoleErrorKey, 0);
            SessionState.SetInt(WaitFramesKey, 0);
            SessionState.SetInt(StageKey, 0);
            string saveDirectory = Path.Combine(Application.dataPath, "..", "Logs",
                "WorldAlpha", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(saveDirectory);
            SessionState.SetString(SaveDirectoryKey, saveDirectory);
            SubscribeCallbacks();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && !scene.isDirty,
                "WorldSandbox opens saved and clean");
            Require(UnityEngine.Object.FindObjectsByType<WorldAlphaPlayableController>(
                    FindObjectsSortMode.None).Length == 0,
                "M70 controller remains runtime-only and scene YAML stays untouched");
            Require(Resources.Load<BuildingData>("Buildings/Building_B09_StorageShed")?.prefab != null &&
                    Resources.Load<BuildingData>("Buildings/Building_B01_MarketStall")?.prefab != null &&
                    Resources.Load<RecipeData>("Recipes/Recipe_Plank") != null,
                "integration assets resolve through their existing Resources identities");
            Debug.Log("[WORLD-010] EDIT_MODE_PASS sceneUntouched=true assets=true");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
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
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            if (SessionState.GetInt(StageKey, 0) == 3 &&
                !SessionState.GetBool(FailedKey, false))
            {
                EditorApplication.delayCall += RestartPlayMode;
            }
            else
            {
                FinishValidation();
            }
        }
    }

    static void RestartPlayMode()
    {
        if (!SessionState.GetBool(ActiveKey, false) ||
            SessionState.GetBool(FailedKey, false))
        {
            FinishValidation();
            return;
        }
        try
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && !scene.isDirty,
                "restart reopens the saved WorldSandbox scene cleanly");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
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
                ResolveRuntime();
                if ((_alpha == null || !_alpha.IsReady) && frames < 900) return;
                Require(_alpha != null && _alpha.IsReady,
                    "playable alpha reaches Ready after the existing gameplay adapter");
                Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
                    $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
                Require(_alpha.Grid.Definition.Width == 128 &&
                        _alpha.Grid.Definition.Height == 128 &&
                        _alpha.Grid.Definition.ChunkSize == 16 &&
                        _alpha.Grid.Definition.CellSize == 2f &&
                        _alpha.IslandView.GeneratedChunkCount == 64,
                    "seed 9009 projects a visible 128x128 world as 8x8 bounded chunks");
                Require(_adapter.PlayerRoot.GetComponent<CharacterController>() != null &&
                        _adapter.PlayerRoot.GetComponent<PlayerController>() != null &&
                        _adapter.PlayerRoot.GetComponent<WorldPlayerTraversalGuard>() != null &&
                        Camera.main != null && Camera.main.GetComponent<CameraController>()?.target ==
                        _adapter.PlayerRoot.transform,
                    "existing input drives a guarded CharacterController with camera follow");
                ValidateSingleAuthorities();

                _adapter.RuntimeSaveManager.SetRepositoryForValidation(
                    new LocalJsonSaveRepository(SessionState.GetString(SaveDirectoryKey, string.Empty)));

                Vector2Int movementCell = FindWalkableMovementCell();
                Require(_alpha.MovePlayerToCellForValidation(movementCell) &&
                        _alpha.Grid.WorldToCell(_adapter.PlayerRoot.transform.position,
                            out Vector2Int movedCell) && movedCell == movementCell,
                    "player traverses to another safe generated cell");

                Vector2Int terraformCell = FindTerraformCell();
                WorldTerraformEditResult terraform = _alpha.RaiseCell(terraformCell);
                Require(terraform.Succeeded && _persistence.CurrentSparseDeltaCount == 1,
                    "runtime terraform commits one sparse cell delta");

                Vector2Int placeCell = FindBuildingCell(false, default);
                WorldBuildingPlacementResult placed = _alpha.PlaceOrMoveShed(placeCell, 0);
                Require(placed.Succeeded &&
                        _alpha.Buildings.TryGetPlacement(
                            WorldBuildingPlacementService.PrototypeInstanceId, out _),
                    "B09 storage shed placement commits through occupancy and reachability gates");
                Vector2Int moveCell = FindBuildingCell(true, placeCell);
                WorldBuildingPlacementResult moved = _alpha.PlaceOrMoveShed(moveCell, 1);
                Require(moved.Succeeded && moved.Anchor != placeCell,
                    "the same B09 instance moves transactionally without duplication");

                Require(_alpha.TryMoveSalesDisplay(1, 0, 1, out string displayReason) &&
                        _adapter.SalesDisplayGrid == new Vector2Int(1, 0) &&
                        _adapter.SalesDisplayQuarterTurns == 1,
                    $"the functional B01 ShopSlot group moves and rotates ({displayReason})");

                Require(_adapter.BeginDayForValidation(9f, 2),
                    "day preparation opens through the existing clock and loop authority");
                bool gatheredA = _alpha.TryGatherTimber(out string gatherA);
                bool gatheredB = _alpha.TryGatherTimber(out string gatherB);
                Require(gatheredA && gatheredB &&
                        _adapter.PlayerInventory.CountItems(
                            Resources.Load<Item>("Items/Item_Wood")) == 2,
                    $"two generated Timber spawns enter Inventory ({gatherA}; {gatherB})");
                Require(_alpha.TryCraftProduct(out string craftReason) &&
                        _adapter.PlayerInventory.CountItems(
                            Resources.Load<Item>("Items/Item_Plank")) == 1,
                    $"B05 CraftingService produces the sale item ({craftReason})");
                Require(_alpha.TryStockProduct(out _saleSlot, out string stockReason) &&
                        _saleSlot != null && !_saleSlot.IsEmpty,
                    $"the crafted product enters the moved ShopSlot ({stockReason})");
                _saleSlot.displayPrice = 1;
                _saleSlot.RefreshDisplay();
                Require(_alpha.TryOpenShop(out string openReason),
                    $"the player opens the night shop ({openReason})");
                Require(_alpha.TrySendCustomer(out string customerReason),
                    $"one existing customer starts a path to the moved display ({customerReason})");
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(1);
                return;
            }

            if (stage == 1)
            {
                ResolveRuntime();
                if (!_adapter.CustomerPurchaseCompleted)
                {
                    if (_adapter.State == WorldGameplayAdapterState.Failed ||
                        Time.realtimeSinceStartup - _stageStarted > 28f)
                    {
                        throw new InvalidOperationException(
                            $"customer purchase did not complete: {_adapter.LastFailure}");
                    }
                    return;
                }

                Require(_saleSlot.IsEmpty && EconomyService.Instance.Money == 1 &&
                        EconomyService.Instance.CumulativeRevenue == 1,
                    "customer purchase clears stock and deposits revenue through EconomyService");
                bool beganDayThree = _adapter.BeginDayForValidation(9f, 3);
                bool gatheredFish = _adapter.TryGatherNext(
                    WorldResourceKind.Fish, out _, out string fishReason);
                Require(beganDayThree && gatheredFish &&
                        _adapter.PlayerInventory.CountItems(
                            Resources.Load<Item>("Items/Item_Fish")) == 1,
                    $"post-sale inventory state remains meaningful for restart proof ({fishReason})");

                WorldStateSaveData expected = _adapter.CaptureWorldState();
                Require(expected != null && expected.modifiedCells.Count == 1 &&
                        expected.placedBuildings.Count == 1 && expected.shopFurniture.Count == 1 &&
                        expected.resourceStates.Count == 3,
                    "one save payload contains terrain, shed, display and gathered resource state");
                Require(_alpha.Grid.WorldToCell(_adapter.PlayerRoot.transform.position,
                        out Vector2Int playerCell),
                    "player position resolves to a stable save cell");
                WorldPlacedBuildingRuntime building = GetPlacedBuilding();
                SessionState.SetString(ExpectedChecksumKey,
                    WorldPersistenceService.ComputePayloadChecksum(expected).ToString());
                SessionState.SetInt(ExpectedBuildingXKey, building.Anchor.x);
                SessionState.SetInt(ExpectedBuildingZKey, building.Anchor.y);
                SessionState.SetInt(ExpectedBuildingRotationKey, building.QuarterTurns);
                SessionState.SetInt(ExpectedPlayerXKey, playerCell.x);
                SessionState.SetInt(ExpectedPlayerZKey, playerCell.y);
                SessionState.SetInt(ExpectedMoneyKey, EconomyService.Instance.Money);
                SessionState.SetString(ExpectedRevenueKey,
                    EconomyService.Instance.CumulativeRevenue.ToString());
                SessionState.SetInt(ExpectedResourcesKey, _adapter.ConsumedResourceCount);
                _ioTask = _adapter.RuntimeSaveManager.SaveGameAsync();
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(2);
                return;
            }

            if (stage == 2)
            {
                if (_ioTask != null && !_ioTask.IsCompleted)
                {
                    if (Time.realtimeSinceStartup - _stageStarted > 8f)
                        throw new TimeoutException("M70 isolated save exceeded eight seconds");
                    return;
                }
                if (_ioTask != null && _ioTask.IsFaulted) throw _ioTask.Exception;
                string saveFile = Path.Combine(
                    SessionState.GetString(SaveDirectoryKey, string.Empty), "savegame.json");
                Require(File.Exists(saveFile),
                    "SaveManager writes the integrated alpha to an isolated repository");

                _adapter.StopCustomer();
                foreach (InventorySlot slot in _adapter.PlayerInventory.slots) slot?.Clear();
                _adapter.PlayerInventory.RefreshAllUI();
                EconomyService.Instance.ForceSet(777, "WORLD-010 restart mutation");
                EconomyService.Instance.ForceSetCumulativeRevenue(777,
                    "WORLD-010 restart mutation");
                Require(_persistence.StartProceduralWorld(
                        WorldGameplayAdapterService.DefaultWorldSeed + 55, out string mutateReason),
                    $"live world is replaced before restart ({mutateReason})");

                SetStage(3);
                EditorApplication.update -= ValidateRuntime;
                EditorApplication.ExitPlaymode();
                return;
            }

            if (stage == 3)
            {
                ResolveRuntime();
                if ((_alpha == null || !_alpha.IsReady) && frames < 900) return;
                Require(_alpha != null && _alpha.IsReady,
                    "WorldSandbox restarts into a fresh runtime composition");
                _adapter.RuntimeSaveManager.SetRepositoryForValidation(
                    new LocalJsonSaveRepository(SessionState.GetString(SaveDirectoryKey, string.Empty)));
                _ioTask = _adapter.RuntimeSaveManager.LoadGameAsync();
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(4);
                return;
            }

            if (stage == 4)
            {
                if (_ioTask != null && !_ioTask.IsCompleted)
                {
                    if (Time.realtimeSinceStartup - _stageStarted > 12f)
                        throw new TimeoutException("M70 restart restore exceeded twelve seconds");
                    return;
                }
                if (_ioTask != null && _ioTask.IsFaulted) throw _ioTask.Exception;
                ResolveRuntime();
                if ((_adapter.BoundSeed != WorldGameplayAdapterService.DefaultWorldSeed ||
                     _alpha.IslandView.GeneratedChunkCount != 64) &&
                    Time.realtimeSinceStartup - _stageStarted < 15f)
                {
                    return;
                }

                Require(_persistence.ActiveSeed == WorldGameplayAdapterService.DefaultWorldSeed &&
                        _adapter.BoundSeed == WorldGameplayAdapterService.DefaultWorldSeed,
                    "restart restore reinstates deterministic seed 9009 and gameplay anchors");
                Require(_persistence.CurrentSparseDeltaCount == 1 &&
                        _alpha.IslandView.GeneratedChunkCount == 64,
                    "sparse terraform state restores into the visible 64-chunk projection");
                WorldPlacedBuildingRuntime restoredBuilding = GetPlacedBuilding();
                Require(restoredBuilding.Anchor == new Vector2Int(
                            SessionState.GetInt(ExpectedBuildingXKey, -1),
                            SessionState.GetInt(ExpectedBuildingZKey, -1)) &&
                        restoredBuilding.QuarterTurns ==
                            SessionState.GetInt(ExpectedBuildingRotationKey, -1),
                    "the moved B09 instance restores once at its committed rotation");
                Require(_adapter.SalesDisplayGrid == new Vector2Int(1, 0) &&
                        _adapter.SalesDisplayQuarterTurns == 1,
                    "the moved functional sales display restores with ShopSlot identity intact");
                Require(_alpha.Grid.WorldToCell(_adapter.PlayerRoot.transform.position,
                            out Vector2Int restoredPlayerCell) &&
                        restoredPlayerCell == new Vector2Int(
                            SessionState.GetInt(ExpectedPlayerXKey, -1),
                            SessionState.GetInt(ExpectedPlayerZKey, -1)),
                    "player restarts at the saved safe world cell");
                Require(EconomyService.Instance.Money ==
                            SessionState.GetInt(ExpectedMoneyKey, -1) &&
                        EconomyService.Instance.CumulativeRevenue.ToString() ==
                            SessionState.GetString(ExpectedRevenueKey, string.Empty) &&
                        _adapter.PlayerInventory.CountItems(
                            Resources.Load<Item>("Items/Item_Fish")) == 1,
                    "revenue and existing Inventory restore after the runtime scene restart");
                Require(_adapter.ConsumedResourceCount ==
                            SessionState.GetInt(ExpectedResourcesKey, -1) &&
                        _adapter.RuntimeShopSlots.All(slot => slot == null || slot.IsEmpty),
                    "resource consumption and post-sale empty stock restore without duplication");
                string restoredChecksum = WorldPersistenceService.ComputePayloadChecksum(
                    _adapter.CaptureWorldState()).ToString();
                Require(restoredChecksum ==
                        SessionState.GetString(ExpectedChecksumKey, string.Empty),
                    "the complete World v11 payload round-trips exactly after restart");
                Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                    "blocking runtime Console Error/Exception/Assert count is 0");
                Require(!SceneManager.GetActiveScene().isDirty,
                    "runtime integration leaves WorldSandbox scene clean");
                Debug.Log("[WORLD-010] PLAY_MODE_PASS seed=true movement=true gather=true " +
                          "craft=true terraform=true buildingMove=true displayMove=true " +
                          "stock=true shopOpen=true customerPurchase=true revenue=true " +
                          "saveRestartRestore=true projection=64chunks schema=11 console=0");
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
        _persistence = WorldPersistenceService.Instance ??
                       UnityEngine.Object.FindFirstObjectByType<WorldPersistenceService>();
    }

    static void ValidateSingleAuthorities()
    {
        int inventoryCount = Resources.FindObjectsOfTypeAll<Inventory>().Count(inventory =>
            inventory != null && inventory.gameObject.scene.IsValid() &&
            inventory.gameObject.scene.isLoaded);
        Require(UnityEngine.Object.FindObjectsByType<WorldAlphaPlayableController>(
                    FindObjectsSortMode.None).Length == 1 &&
                UnityEngine.Object.FindObjectsByType<WorldGameplayAdapterService>(
                    FindObjectsSortMode.None).Length == 1 && inventoryCount == 1 &&
                UnityEngine.Object.FindObjectsByType<EconomyService>(
                    FindObjectsSortMode.None).Length == 1 &&
                UnityEngine.Object.FindObjectsByType<SaveManager>(
                    FindObjectsSortMode.None).Length == 1,
            "M70 composes one alpha, adapter, Inventory, Economy and Save authority");
    }

    static Vector2Int FindWalkableMovementCell()
    {
        Require(_alpha.Grid.WorldToCell(_adapter.PlayerRoot.transform.position,
                out Vector2Int origin), "player start resolves to the generated grid");
        Require(_alpha.Grid.TryGetCell(origin, out WorldCellData originData),
            "player start cell remains readable");
        foreach (WorldCellData cell in _alpha.Grid.Cells
                     .Where(cell => cell.IsWalkable && !cell.HasWater &&
                                    cell.ElevationLevel == originData.ElevationLevel &&
                                    cell.Coordinate != origin)
                     .OrderBy(cell => Mathf.Abs(cell.Coordinate.x - origin.x) +
                                      Mathf.Abs(cell.Coordinate.y - origin.y)))
        {
            if (Mathf.Abs(cell.Coordinate.x - origin.x) +
                Mathf.Abs(cell.Coordinate.y - origin.y) <= 8)
                return cell.Coordinate;
        }
        throw new InvalidOperationException("No nearby safe movement cell was generated.");
    }

    static Vector2Int FindTerraformCell()
    {
        foreach (WorldCellData cell in _alpha.Grid.Cells.OrderBy(cell => cell.Coordinate.y)
                     .ThenBy(cell => cell.Coordinate.x))
        {
            if (!_alpha.Grid.IsTerraformProtected(cell.Coordinate) &&
                cell.Occupancy == WorldCellOccupancy.Empty && !cell.HasWater && !cell.HasPath &&
                cell.ElevationLevel < _alpha.Grid.Definition.MaxElevationLevel)
                return cell.Coordinate;
        }
        throw new InvalidOperationException("No legal terraform cell exists.");
    }

    static Vector2Int FindBuildingCell(bool moving, Vector2Int excluded)
    {
        string instanceId = WorldBuildingPlacementService.PrototypeInstanceId;
        for (int z = 0; z < _alpha.Grid.Definition.Height; z++)
        {
            for (int x = 0; x < _alpha.Grid.Definition.Width; x++)
            {
                var coordinate = new Vector2Int(x, z);
                if (coordinate == excluded) continue;
                WorldBuildingPlacementResult result = _alpha.Buildings.Evaluate(
                    instanceId, coordinate, moving ? 1 : 0, moving ? instanceId : null);
                if (result.Succeeded) return coordinate;
            }
        }
        throw new InvalidOperationException(moving
            ? "No legal moved building anchor exists."
            : "No legal building anchor exists.");
    }

    static WorldPlacedBuildingRuntime GetPlacedBuilding()
    {
        if (_alpha.Buildings.TryGetPlacement(
                WorldBuildingPlacementService.PrototypeInstanceId,
                out WorldPlacedBuildingRuntime building))
            return building;
        throw new InvalidOperationException("The integrated B09 instance is missing.");
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
        SessionState.SetInt(ConsoleErrorKey,
            SessionState.GetInt(ConsoleErrorKey, 0) + 1);
    }

    static void Fail(Exception ex)
    {
        SessionState.SetBool(FailedKey, true);
        Debug.LogError($"[WORLD-010] FAIL {ex.Message}\n{ex}");
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
        EraseSession();
        Debug.Log(failed
            ? $"[WORLD-010] FINISHED_WITH_ERRORS consoleErrors={consoleErrors}"
            : "[WORLD-010] FINISHED_PASS M70_PLAYABLE_WORLD_ALPHA_COMPLETE " +
              "movement=true worldLoop=true restartRestore=true goldenReady=true console=0");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void EraseSession()
    {
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(FailedKey);
        SessionState.EraseInt(ConsoleErrorKey);
        SessionState.EraseInt(WaitFramesKey);
        SessionState.EraseInt(StageKey);
        SessionState.EraseString(SaveDirectoryKey);
        SessionState.EraseString(ExpectedChecksumKey);
        SessionState.EraseInt(ExpectedBuildingXKey);
        SessionState.EraseInt(ExpectedBuildingZKey);
        SessionState.EraseInt(ExpectedBuildingRotationKey);
        SessionState.EraseInt(ExpectedPlayerXKey);
        SessionState.EraseInt(ExpectedPlayerZKey);
        SessionState.EraseInt(ExpectedMoneyKey);
        SessionState.EraseString(ExpectedRevenueKey);
        SessionState.EraseInt(ExpectedResourcesKey);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[WORLD-010] PASS {message}");
    }
}

public static class PA_Beta001OnboardingValidator
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.BETA001.Active";
    const string FailedKey = "PA.BETA001.Failed";
    const string ConsoleErrorKey = "PA.BETA001.ConsoleErrors";
    const string FrameKey = "PA.BETA001.Frames";
    const string StageKey = "PA.BETA001.Stage";

    static WorldAlphaPlayableController _alpha;
    static WorldGameplayAdapterService _adapter;

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

    [MenuItem("Project PA/Beta/BETA-001/Validate Player Onboarding and World Readability")]
    public static void RunBeta001Validation()
    {
        RunInternal();
    }

    public static void RunBeta001ValidationBatch()
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
            Require(UnityEngine.Object.FindObjectsByType<WorldAlphaPlayableController>(
                    FindObjectsSortMode.None).Length == 0,
                "BETA onboarding remains runtime-only without scene serialization");
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
            SessionState.SetInt(FrameKey, 0);
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
            if ((_alpha == null || !_alpha.IsReady) && frames < 900) return;
            Require(_alpha != null && _alpha.IsReady,
                "WorldSandbox reaches the connected playable runtime");

            if (stage == 0)
            {
                Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
                    $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
                Require(GameClock.Instance != null && GameClock.Instance.CurrentDay == 1 &&
                        Mathf.Abs(GameClock.Instance.CurrentHour - 9f) < 0.1f,
                    "a fresh WorldSandbox session starts safely on Day 1 at 09:00");
                Require(_alpha.StartPromptVisible && !_alpha.HasStartedBeta &&
                        !_alpha.DevelopmentOverlayVisible,
                    "the player sees a new-game prompt while development panels default hidden");
                Require(!_alpha.GetComponent<WorldGridDebugView>().enabled &&
                        !_alpha.GetComponent<WorldBuildingPlacementDebugController>().enabled &&
                        !_alpha.IslandView.enabled &&
                        !_alpha.GetComponent<WorldNavigationService>().DebugOverlayVisible,
                    "WORLD grid, placement, generator and navigation debug views are hidden");
                Require(_adapter.PlayerRoot.GetComponent<CharacterController>() != null &&
                        _adapter.PlayerRoot.GetComponent<WorldPlayerTraversalGuard>() != null &&
                        Camera.main != null && Camera.main.GetComponent<CameraController>()?.target ==
                        _adapter.PlayerRoot.transform,
                    "safe player start, existing movement and camera follow remain connected");

                PrototypeWorldLabel[] shopLabels = _adapter.RuntimeShop.transform.root
                    .GetComponentsInChildren<PrototypeWorldLabel>(true);
                PrototypeWorldLabel[] workbenchLabels = _adapter.RuntimeWorkbench.transform.root
                    .GetComponentsInChildren<PrototypeWorldLabel>(true);
                Require(shopLabels.Any(label => label.label.Contains("P.A. 잡화점")) &&
                        workbenchLabels.Any(label => label.label.Contains("제작 작업대")),
                    "player-facing shop and workbench landmarks identify their roles");
                Require(_alpha.BeginNewGame() && _alpha.PlayerFacingHudVisible &&
                        _alpha.CurrentPlayerObjective.Contains("WASD"),
                    "new game begins with a concrete first movement objective");

                Require(MoveToNearbyWalkable(_adapter.PlayerRoot.transform.position, 3f),
                    "validator moves the player through a nearby safe cell");
                SetStage(1);
                return;
            }

            if (frames < 3) return;
            if (stage == 1)
            {
                Require(_alpha.HasMoved && _alpha.CurrentPlayerObjective.Contains("잡화점"),
                    "movement advances the objective toward the shop landmark");
                Require(MoveNear(_adapter.RuntimeShop.transform.position, 10f),
                    "a walkable cell exists near the shop");
                SetStage(2);
                return;
            }

            if (stage == 2)
            {
                Require(_alpha.HasReachedShop && _alpha.CurrentPlayerObjective.Contains("제작 작업대"),
                    "reaching the shop explains its night-sale role and advances the route");
                Require(MoveNear(_adapter.RuntimeWorkbench.transform.position, 10f),
                    "a walkable cell exists near the workbench");
                SetStage(3);
                return;
            }

            Require(_alpha.HasReachedShop && _alpha.HasReachedWorkbench &&
                    _alpha.CurrentPlayerObjective.Contains("첫 동선 확인 완료"),
                "the player can finish the first shop/workbench orientation route");
            Require(_alpha.CurrentPlayerObjective.Contains("낮 자원"),
                "the completed route hands off to daytime resource play");

            _alpha.SetDevelopmentOverlayVisible(true);
            Require(_alpha.DevelopmentOverlayVisible &&
                    _alpha.GetComponent<WorldGridDebugView>().enabled &&
                    _alpha.GetComponent<WorldBuildingPlacementDebugController>().enabled &&
                    _alpha.IslandView.enabled &&
                    _alpha.GetComponent<WorldNavigationService>().DebugOverlayVisible,
                "F10 development mode can restore every WORLD diagnostic surface");
            _alpha.SetDevelopmentOverlayVisible(false);
            Require(_alpha.PlayerFacingHudVisible &&
                    !_alpha.GetComponent<WorldGridDebugView>().enabled &&
                    !_alpha.GetComponent<WorldBuildingPlacementDebugController>().enabled &&
                    !_alpha.IslandView.enabled &&
                    !_alpha.GetComponent<WorldNavigationService>().DebugOverlayVisible,
                "returning to player view removes debug panels and inputs again");
            Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                "blocking runtime Console Error/Exception/Assert count is 0");
            Require(!SceneManager.GetActiveScene().isDirty,
                "BETA-001 runtime presentation leaves WorldSandbox scene clean");
            Debug.Log("[BETA-001] PLAY_MODE_PASS day1=true safeStart=true objective=true " +
                      "shopLandmark=true workbenchLandmark=true devOverlayDefaultHidden=true " +
                      "f10Recovery=true console=0");
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.ExitPlaymode();
        }
        catch (Exception ex)
        {
            EditorApplication.update -= ValidateRuntime;
            Fail(ex);
        }
    }

    static bool MoveToNearbyWalkable(Vector3 originWorld, float minimumDistance)
    {
        foreach (WorldCellData cell in _alpha.Grid.Cells
                     .Where(cell => cell.IsWalkable && !cell.HasWater)
                     .OrderBy(cell => FlatDistance(originWorld,
                         CellWorld(cell.Coordinate))))
        {
            Vector3 world = CellWorld(cell.Coordinate);
            float distance = FlatDistance(originWorld, world);
            if (distance >= minimumDistance && distance <= 16f)
                return _alpha.MovePlayerToCellForValidation(cell.Coordinate);
        }
        return false;
    }

    static bool MoveNear(Vector3 targetWorld, float maximumDistance)
    {
        foreach (WorldCellData cell in _alpha.Grid.Cells
                     .Where(cell => cell.IsWalkable && !cell.HasWater)
                     .OrderBy(cell => FlatDistance(targetWorld,
                         CellWorld(cell.Coordinate))))
        {
            Vector3 world = CellWorld(cell.Coordinate);
            if (FlatDistance(targetWorld, world) <= maximumDistance)
                return _alpha.MovePlayerToCellForValidation(cell.Coordinate);
            break;
        }
        return false;
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

    static void ResolveRuntime()
    {
        _alpha = WorldAlphaPlayableController.Instance ??
                 UnityEngine.Object.FindFirstObjectByType<WorldAlphaPlayableController>();
        _adapter = WorldGameplayAdapterService.Instance ??
                   UnityEngine.Object.FindFirstObjectByType<WorldGameplayAdapterService>();
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
        Debug.LogError($"[BETA-001] FAIL {ex.Message}\n{ex}");
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
            ? $"[BETA-001] FINISHED_WITH_ERRORS consoleErrors={errors}"
            : "[BETA-001] FINISHED_PASS playerOnboarding=true worldReadability=true " +
              "devOverlayHidden=true console=0");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[BETA-001] PASS {message}");
    }
}
#endif
