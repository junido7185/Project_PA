#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// D3D11 play-mode validator for P5 shop growth, display capacity and theme persistence.
[InitializeOnLoad]
public static class PA_ShopProgressionUnlockValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string Prefix = "PA.ShopProgressionUnlock.";
    const string ActiveKey = Prefix + "Active";
    const string EnteredKey = Prefix + "Entered";
    const string RanKey = Prefix + "Ran";
    const string FailedKey = Prefix + "Failed";
    const string OutputKey = Prefix + "Output";

    static bool _entered;
    static bool _ran;
    static bool _failed;
    static double _startedAt;
    static Task _runtimeTask;

    static PA_ShopProgressionUnlockValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _failed = SessionState.GetBool(FailedKey, false);
        RegisterCallbacks();
        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Run Shop Progression Unlock Validation")]
    public static void RunShopProgressionUnlockValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA ShopProgressionUnlock: failed to open {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "ShopProgressionUnlock", stamp));
        Directory.CreateDirectory(output);
        _entered = false;
        _ran = false;
        _failed = false;
        _runtimeTask = null;
        _startedAt = EditorApplication.timeSinceStartup;
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(FailedKey, false);
        SessionState.SetString(OutputKey, output);
        RegisterCallbacks();
        Debug.Log($"PA ShopProgressionUnlock: entering Play Mode. Output={output}");
        EditorApplication.EnterPlaymode();
    }

    static void RegisterCallbacks()
    {
        Application.logMessageReceived -= OnLog;
        EditorApplication.playModeStateChanged -= OnPlayModeState;
        EditorApplication.update -= OnUpdate;
        Application.logMessageReceived += OnLog;
        EditorApplication.playModeStateChanged += OnPlayModeState;
        EditorApplication.update += OnUpdate;
    }

    static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        _failed = true;
        SessionState.SetBool(FailedKey, true);
    }

    static void OnPlayModeState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _entered = true;
            _startedAt = EditorApplication.timeSinceStartup;
            SessionState.SetBool(EnteredKey, true);
        }
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(ActiveKey, false)
            && SessionState.GetBool(RanKey, false)) Finish();
    }

    static void OnUpdate()
    {
        double elapsed = EditorApplication.timeSinceStartup - _startedAt;
        if (!_entered)
        {
            if (elapsed > 60d) FailAndExit("timed out entering Play Mode");
            return;
        }
        if (!_ran && EditorApplication.isPlaying && _runtimeTask == null && elapsed > 4d)
        {
            _runtimeTask = RunChecksAsync();
            return;
        }
        if (!_ran && _runtimeTask != null && _runtimeTask.IsCompleted)
        {
            if (_runtimeTask.IsFaulted)
            {
                _failed = true;
                SessionState.SetBool(FailedKey, true);
                Debug.LogError($"PA Shop Progression Unlock Validation failed: {_runtimeTask.Exception?.GetBaseException()}");
            }
            _ran = true;
            SessionState.SetBool(RanKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }
        if (!_ran && elapsed > 150d) FailAndExit("timed out before runtime checks completed");
    }

    static async Task RunChecksAsync()
    {
        Time.timeScale = 1f;
        ShopCustomizationController controller = RequireOne<ShopCustomizationController>("ShopCustomizationController");
        TierService tiers = RequireOne<TierService>("TierService");
        for (int i = 0; i < 120 && !controller.IsReady; i++) await Task.Delay(50);
        Require(controller.IsReady, "shop customization controller initialized");

        string output = SessionState.GetString(OutputKey, string.Empty);
        Require(!string.IsNullOrWhiteSpace(output), "isolated output directory exists");
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Require(player != null, "player exists for ledger rewards and capture");

        tiers.ForceSetTier(0, 0, "P5 validation tier 0");
        controller.RefreshProgressionForValidation();
        await Task.Delay(150);
        Require(controller.CurrentZoneSize == new Vector2Int(5, 4), "Tier 0 uses the authored 5x4 placement zone");
        Require(controller.CurrentDisplayLimit == 6 && controller.ActiveDisplayCount == 6,
            "Tier 0 starts with six authored functional displays and no fake capacity");
        Require(!controller.IsDefinitionUnlockedForValidation("Blueprint_B05_Workbench"),
            "B05 workbench remains locked before Tier 1");
        string tier0Capture = Path.Combine(output, "tier0_shop.png");
        await CaptureInteriorAsync(tier0Capture);
        Require(File.Exists(tier0Capture) && new FileInfo(tier0Capture).Length > 1024,
            "Tier 0 game-camera capture generated");

        tiers.ForceSetTier(1, 0, "P5 validation tier 1");
        controller.RefreshProgressionForValidation();
        controller.ToggleFromTerminal(player);
        controller.CloseCustomization();
        Require(controller.CurrentZoneSize == new Vector2Int(5, 4), "Tier 1 preserves the compact 5x4 shop");
        Require(controller.IsDefinitionUnlockedForValidation("Blueprint_B05_Workbench")
            && controller.IsDefinitionUnlockedForValidation("Blueprint_B07_BlacksmithForge")
            && !controller.IsDefinitionUnlockedForValidation("Blueprint_B06_KitchenStation")
            && !controller.IsDefinitionUnlockedForValidation("Blueprint_B08_SewingTable"),
            "Tier 1 unlocks the B05 preparation bench and authored Tier 1 B07 forge");
        Require(controller.HasDefinitionBlueprintInInventory("Blueprint_B05_Workbench")
            && controller.HasDefinitionBlueprintInInventory("Blueprint_B07_BlacksmithForge"),
            "opening the Tier 1 ledger grants the real B05 and B07 blueprint rewards");

        tiers.ForceSetTier(2, 0, "P5 validation tier 2");
        controller.RefreshProgressionForValidation();
        controller.ToggleFromTerminal(player);
        controller.CloseCustomization();
        await WaitForExpansionAsync(controller);
        Require(controller.CurrentZoneSize == new Vector2Int(6, 5), "Tier 2 expands the placement zone to 6x5");
        Require(controller.CurrentDisplayLimit == 8, "Tier 2 raises functional display capacity to eight");
        Require(controller.IsDefinitionUnlockedForValidation("Blueprint_B06_KitchenStation")
            && controller.IsDefinitionUnlockedForValidation("Blueprint_B07_BlacksmithForge")
            && !controller.IsDefinitionUnlockedForValidation("Blueprint_B08_SewingTable"),
            "Tier 2 adds B06 while the authored Tier 1 B07 remains available and B08 stays locked");
        Require(controller.HasDefinitionBlueprintInInventory("Blueprint_B06_KitchenStation"),
            "opening the Tier 2 ledger grants the B06 blueprint reward");
        Require(controller.GetCatalogDefinitionIdsForValidation().Contains("shop.shelf"),
            "Tier 2 catalog offers a functional display cloned from the authored shelf style");
        RequireExpansionPath(controller, new Vector2Int(5, 4), "Tier 2 far expansion cell");

        Require(controller.TryPlaceDefinitionForValidation("shop.shelf", new Vector2Int(5, 2), 0,
            false, out string shelfA, out string shelfReasonA), $"seventh display places in expanded floor ({shelfReasonA})");
        Require(controller.TryPlaceDefinitionForValidation("shop.shelf", new Vector2Int(5, 4), 0,
            false, out string shelfB, out string shelfReasonB), $"eighth display places in expanded floor ({shelfReasonB})");
        Require(controller.ActiveDisplayCount == 8, "Tier 2 reaches exactly eight active functional displays");
        Require(!controller.TryPlaceDefinitionForValidation("shop.shelf", new Vector2Int(4, 4), 0,
            false, out _, out string capReason) && !string.IsNullOrWhiteSpace(capReason),
            "Tier 2 rejects a ninth display at the authored capacity limit");

        tiers.ForceSetTier(3, 0, "P5 validation tier 3");
        controller.RefreshProgressionForValidation();
        controller.ToggleFromTerminal(player);
        controller.CloseCustomization();
        await WaitForExpansionAsync(controller);
        Require(controller.CurrentZoneSize == new Vector2Int(7, 6), "Tier 3 expands the placement zone to 7x6");
        Require(controller.CurrentDisplayLimit == 12, "Tier 3 raises functional display capacity to twelve");
        Require(controller.IsDefinitionUnlockedForValidation("Blueprint_B07_BlacksmithForge")
            && controller.IsDefinitionUnlockedForValidation("Blueprint_B08_SewingTable"),
            "Tier 3 retains B07 and unlocks the B08 preparation bench");
        Require(controller.HasDefinitionBlueprintInInventory("Blueprint_B07_BlacksmithForge")
            && controller.HasDefinitionBlueprintInInventory("Blueprint_B08_SewingTable"),
            "opening the Tier 3 ledger retains B07 and grants the real B08 blueprint reward");
        RequireExpansionPath(controller, new Vector2Int(6, 5), "Tier 3 far expansion cell");
        Require(controller.TryPlaceDefinitionForValidation("shop.shelf", new Vector2Int(6, 5), 0,
            false, out string savedShelf, out string shelfReasonC), $"ninth display places in Tier 3 expansion ({shelfReasonC})");
        Require(controller.GetPlacementGameObject(savedShelf)?.GetComponent<ShopSlot>() != null,
            "dynamic display retains the real ShopSlot interaction component");

        VillageCultureVisualController culture = RequireOne<VillageCultureVisualController>("VillageCultureVisualController");
        culture.RestoreSavedState(false, 0, string.Empty, true, ItemCategory.Processed.ToString(), true);
        Require(controller.ProcessedThemeUnlocked && controller.TryCycleThemeForValidation()
            && controller.ActiveThemeId == "processed.warm",
            "processed-goods village change unlocks and applies the warm workshop theme");

        var snapshot = new SaveData { version = 10 };
        controller.WriteSaveFields(snapshot);
        Require(snapshot.placeables.Any(record => record.definitionId == "shop.theme"
            && record.instanceId == "fixed.shop.theme" && record.functionalState == "theme:processed.warm"),
            "v10 flexible placement record persists the active theme without a schema bump");
        Require(snapshot.placeables.Any(record => record.instanceId == savedShelf
            && record.gridX == 6 && record.gridY == 5),
            "v10 placement record persists a dynamically unlocked display");

        controller.RestoreSavedState(null, false);
        Require(controller.GetPlacementGameObject(savedShelf) == null && controller.ActiveThemeId == "default",
            "mutation clears dynamic display and theme before restoration");
        controller.RestoreSavedState(snapshot.placeables, snapshot.placementStarterGranted);
        Require(controller.TryGetPlacementSnapshot(savedShelf, out Vector2Int restoredCell, out int restoredRotation,
                out bool restoredRecovered) && restoredCell == new Vector2Int(6, 5)
            && restoredRotation == 0 && !restoredRecovered,
            "P5 display cell and rotation restore from the existing v10 placement contract");
        Require(controller.GetPlacementGameObject(savedShelf)?.GetComponent<ShopSlot>() != null
            && controller.ActiveThemeId == "processed.warm",
            "restored dynamic display remains functional and the culture theme returns");

        tiers.ForceSetTier(4, 0, "P5 validation tier 4");
        controller.RefreshProgressionForValidation();
        Require(controller.CurrentZoneSize == new Vector2Int(7, 6) && controller.CurrentDisplayLimit == 20,
            "Tier 4 preserves the 7x6 room while raising display capacity to twenty");
        Require(GridService.Instance.HasZonePath(controller.ZoneId),
            "expanded placement graph preserves the protected entry-to-service route");

        string tier3Capture = Path.Combine(output, "tier3_expanded_shop.png");
        controller.ToggleFromTerminal(player);
        await CaptureInteriorAsync(tier3Capture);
        controller.CloseCustomization();
        Require(File.Exists(tier3Capture) && new FileInfo(tier3Capture).Length > 1024,
            "expanded warm-theme game-camera capture with progression ledger generated");
        File.WriteAllText(Path.Combine(output, "summary.txt"),
            $"PASS\nzone={controller.CurrentZoneSize.x}x{controller.CurrentZoneSize.y}\n" +
            $"displays={controller.ActiveDisplayCount}/{controller.CurrentDisplayLimit}\n" +
            $"theme={controller.ActiveThemeId}\nrestoredShelf={savedShelf}@{restoredCell}\n");

        Debug.Log($"PA Shop Progression Unlock Validation passed. zone={controller.CurrentZoneSize}, " +
            $"displays={controller.ActiveDisplayCount}/{controller.CurrentDisplayLimit}, theme={controller.ActiveThemeId}, " +
            $"shelves={shelfA},{shelfB},{savedShelf}, output={output}");
    }

    static async Task WaitForExpansionAsync(ShopCustomizationController controller)
    {
        for (int i = 0; i < 80 && !controller.ExpansionNavMeshReady; i++) await Task.Delay(50);
        Require(controller.ExpansionNavMeshReady, "runtime expansion NavMesh build completed");
    }

    static void RequireExpansionPath(ShopCustomizationController controller, Vector2Int cell, string label)
    {
        Transform spawn = GameObject.Find("PA_StoreInterior")?.transform.Find("PlayerSpawn_Inside");
        Require(spawn != null, "interior spawn exists for expansion path validation");
        Vector3 target = GridService.Instance.ZoneCellToWorld(controller.ZoneId, cell, 0.1f);
        bool startOk = NavMesh.SamplePosition(spawn.position, out NavMeshHit startHit, 2.5f, NavMesh.AllAreas);
        bool targetOk = NavMesh.SamplePosition(target, out NavMeshHit targetHit, 1.5f, NavMesh.AllAreas);
        var path = new NavMeshPath();
        bool calculated = startOk && targetOk && NavMesh.CalculatePath(startHit.position, targetHit.position,
            NavMesh.AllAreas, path);
        Require(calculated && path.status == NavMeshPathStatus.PathComplete,
            $"{label} has a complete path from the authored entrance");
    }

    static async Task CaptureInteriorAsync(string outputPath)
    {
        Camera camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        Transform interior = GameObject.Find("PA_StoreInterior")?.transform;
        Require(camera != null && interior != null, "game camera and shop interior exist for capture");

        await PA_SafeGameViewCapture.CaptureAsync(outputPath, camera, captureCamera =>
        {
            captureCamera.transform.position = interior.TransformPoint(new Vector3(1.5f, 10.5f, -3.75f));
            captureCamera.transform.rotation = Quaternion.LookRotation(
                interior.TransformPoint(new Vector3(1.5f, 0.25f, 2.0f)) - captureCamera.transform.position,
                interior.up);
            captureCamera.orthographic = true;
            captureCamera.orthographicSize = 7.0f;
        });
    }

    static T RequireOne<T>(string label) where T : Object
    {
        T value = Object.FindFirstObjectByType<T>();
        if (value == null) throw new InvalidOperationException($"{label} not found");
        return value;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"PA ShopProgressionUnlock Check OK: {message}");
    }

    static void FailAndExit(string message)
    {
        _failed = true;
        SessionState.SetBool(FailedKey, true);
        Debug.LogError($"PA ShopProgressionUnlock: {message}");
        Cleanup();
        EditorApplication.Exit(1);
    }

    static void Finish()
    {
        bool failed = _failed || SessionState.GetBool(FailedKey, false);
        bool entered = _entered || SessionState.GetBool(EnteredKey, false);
        bool ran = _ran || SessionState.GetBool(RanKey, false);
        Cleanup();
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(EnteredKey);
        SessionState.EraseBool(RanKey);
        SessionState.EraseBool(FailedKey);
        SessionState.EraseString(OutputKey);
        if (!entered || !ran || failed)
        {
            Debug.LogError("PA Shop Progression Unlock Validation failed. Check the first error in Editor.log.");
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log("PA Shop Progression Unlock Validation finished successfully.");
        EditorApplication.Exit(0);
    }

    static void Cleanup()
    {
        Application.logMessageReceived -= OnLog;
        EditorApplication.playModeStateChanged -= OnPlayModeState;
        EditorApplication.update -= OnUpdate;
        _runtimeTask = null;
    }
}
#endif
