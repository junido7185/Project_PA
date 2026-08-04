#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// D3D11 batch validator for the P3 outdoor grid and functional B09 storage loop.
[InitializeOnLoad]
public static class PA_OutdoorPlacementValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.OutdoorPlacement.Active";
    const string EnteredKey = "PA.OutdoorPlacement.Entered";
    const string RanKey = "PA.OutdoorPlacement.Ran";
    const string ErrorKey = "PA.OutdoorPlacement.Error";
    const string OutputKey = "PA.OutdoorPlacement.Output";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;
    static Task _runtimeTask;

    static PA_OutdoorPlacementValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _hadError = SessionState.GetBool(ErrorKey, false);
        RegisterCallbacks();
        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Run Outdoor Placement Validation")]
    public static void RunOutdoorPlacementValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA OutdoorPlacement: failed to open {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "OutdoorPlacement",
            DateTime.Now.ToString("yyyyMMdd_HHmmss")));
        Directory.CreateDirectory(output);
        _entered = false; _ran = false; _hadError = false; _runtimeTask = null;
        _startedAt = EditorApplication.timeSinceStartup;
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(ErrorKey, false);
        SessionState.SetString(OutputKey, output);
        RegisterCallbacks();
        Debug.Log($"PA OutdoorPlacement: entering Play Mode. Output={output}");
        EditorApplication.EnterPlaymode();
    }

    static void RegisterCallbacks()
    {
        Application.logMessageReceived -= OnLog;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.update -= OnUpdate;
        Application.logMessageReceived += OnLog;
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.update += OnUpdate;
    }

    static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        _hadError = true;
        SessionState.SetBool(ErrorKey, true);
    }

    static void OnPlayMode(PlayModeStateChange state)
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
                _hadError = true;
                SessionState.SetBool(ErrorKey, true);
                Debug.LogError($"PA Outdoor Placement Validation failed: {_runtimeTask.Exception?.GetBaseException()}");
            }
            _ran = true;
            SessionState.SetBool(RanKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }
        if (!_ran && elapsed > 140d) FailAndExit("timed out before checks completed");
    }

    static async Task RunChecksAsync()
    {
        Time.timeScale = 1f;
        var controller = RequireOne<OutdoorPlacementController>("OutdoorPlacementController");
        for (int i = 0; i < 120 && !controller.IsReady; i++) await Task.Delay(50);
        Require(controller.IsReady, "outdoor controller initialized");
        Require(GridService.Instance != null && GridService.Instance.HasZone(controller.ZoneId),
            "shared GridService owns village.outdoor");
        Require(GridService.Instance.GetZoneSize(controller.ZoneId) == new Vector2Int(47, 47),
            "outdoor zone covers the 96m authored ground with 2m cells");
        Require(controller.ProtectedCellCount >= 100, "roads, plaza, spawn and entrances form a protected mask");
        Require(GridService.Instance.IsZoneCellProtected(controller.ZoneId,
            GridService.Instance.WorldToZoneCell(controller.ZoneId, new Vector3(0f, 0f, 20f))),
            "north-south road is protected");
        Require(GridService.Instance.IsZoneCellProtected(controller.ZoneId,
            GridService.Instance.WorldToZoneCell(controller.ZoneId, new Vector3(20f, 0f, 0f))),
            "east-west road is protected");
        Require(GridService.Instance.IsZoneCellProtected(controller.ZoneId,
            GridService.Instance.WorldToZoneCell(controller.ZoneId, new Vector3(0f, 0f, 4f))),
            "shop plaza is protected");

        var fixedStorage = controller.GetPrimaryStorageForValidation();
        Require(fixedStorage != null && fixedStorage.GameObject != null, "main-map B09 is adopted as a fixed functional placeable");
        Require(fixedStorage.GameObject.GetComponent<StorageBox>() != null, "B09 retains the real StorageBox interaction");
        Require(fixedStorage.FootprintCellCount >= 9, "B09 uses a multi-cell collider-derived footprint");
        NavMeshObstacle obstacle = fixedStorage.GameObject.GetComponentInChildren<NavMeshObstacle>();
        Require(obstacle != null && obstacle.carving, "B09 collider keeps a carving NavMesh obstacle");
        Require(Object.FindObjectsByType<StorageBox>(FindObjectsSortMode.None).All(storage =>
            !BuildPath(storage.transform).Contains("[WorldBuildings]", StringComparison.Ordinal)),
            "no active legacy B09 duplicate or invisible duplicate collider remains");

        BuildingData storageData = fixedStorage.Data;
        Require(storageData != null && storageData.prefab != null, "B09 BuildingData resolves from Resources");
        bool roadRejected = !controller.TryResolvePreview(storageData, new Vector3(0f, 0f, 20f), 0, null,
            out _, out _, out string roadReason);
        Require(roadRejected && !string.IsNullOrWhiteSpace(roadReason), "multi-cell B09 cannot block the protected road");

        string output = SessionState.GetString(OutputKey, string.Empty);
        Require(!string.IsNullOrWhiteSpace(output), "isolated validation output exists");
        string baseline = Path.Combine(output, "b09_outdoor_baseline.png");
        await CaptureStorageCameraAsync(fixedStorage.GameObject, baseline);
        Require(File.Exists(baseline) && new FileInfo(baseline).Length > 1024, "baseline B09 camera capture generated");

        Require(TryFindValid(controller, storageData, fixedStorage,
            new[] { new Vector3(-14f, 0f, 14f), new Vector3(-16f, 0f, 16f), new Vector3(-12f, 0f, 18f) },
            3, out Vector2Int fixedCell, out _, out string fixedReason), $"fixed B09 move target found ({fixedReason})");
        Require(controller.TryApplyMove(fixedStorage, fixedCell, 3, out string moveReason),
            $"authored B09 moves on the outdoor grid ({moveReason})");

        StorageBox fixedBox = fixedStorage.GameObject.GetComponent<StorageBox>();
        Item bread = Resources.Load<Item>("Items/Item_BreadLoaf");
        Require(bread != null, "storage test item resolves");
        fixedBox.items.Clear();
        fixedBox.items.Add(new ItemInstance(bread, 2) { quality = 0.88f, currentPrice = 43 });

        Require(TryFindValid(controller, storageData, null,
            new[] { new Vector3(14f, 0f, 14f), new Vector3(16f, 0f, 16f), new Vector3(12f, 0f, 18f) },
            1, out Vector2Int dynamicCell, out Vector3 dynamicWorld, out string dynamicReason),
            $"dynamic B09 build target found ({dynamicReason})");
        GameObject dynamicGo = Object.Instantiate(storageData.prefab, dynamicWorld, Quaternion.Euler(0f, 90f, 0f));
        BuildingRegistry.Instance.Register(storageData, dynamicGo);
        Require(controller.TryCommitNew(storageData, dynamicGo, dynamicCell, 1,
            out var dynamicStorage, out string commitReason), $"player B09 commits as a multi-cell building ({commitReason})");
        StorageBox dynamicBox = dynamicGo.GetComponent<StorageBox>();
        dynamicBox.items.Clear();
        dynamicBox.items.Add(new ItemInstance(bread, 1) { quality = 0.73f, currentPrice = 37 });
        Require(!controller.TryRecover(dynamicStorage, false, out string occupiedRecoverReason)
            && occupiedRecoverReason.Contains("비워", StringComparison.Ordinal),
            "occupied storage refuses unsafe recovery");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) MovePlayer(player, fixedStorage.GameObject.transform.position + new Vector3(0f, 0.1f, -3.2f));
        controller.ShowPreview(storageData, fixedStorage.Cell, fixedStorage.RotationQuarterTurns, true);
        await Task.Delay(150);
        string finalCapture = Path.Combine(output, "b09_outdoor_final.png");
        await CaptureStorageCameraAsync(fixedStorage.GameObject, finalCapture);
        controller.ClearPreview();
        Require(File.Exists(finalCapture) && new FileInfo(finalCapture).Length > 1024,
            "same-angle B09 final capture includes placement feedback");

        var save = RequireOne<SaveManager>("SaveManager");
        FieldInfo repositoryField = typeof(SaveManager).GetField("_repository", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(repositoryField != null, "SaveManager repository is injectable");
        repositoryField.SetValue(save, new LocalJsonSaveRepository(output));
        await save.SaveGameAsync();
        string saveFile = Path.Combine(output, "savegame.json");
        Require(File.Exists(saveFile), "isolated v10 save written");
        SaveData saved = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveFile));
        Require(saved != null && saved.version == 10, "outdoor placement reuses schema v10");
        PlaceableSaveData fixedRecord = saved.placeables.FirstOrDefault(p => p.zoneId == controller.ZoneId && p.isFixed);
        PlaceableSaveData dynamicRecord = saved.placeables.FirstOrDefault(p => p.instanceId == dynamicStorage.InstanceId);
        Require(fixedRecord != null && fixedRecord.gridX == fixedCell.x && fixedRecord.gridY == fixedCell.y
            && fixedRecord.rotationQuarterTurns == 3 && fixedRecord.storedItems.Sum(i => i.count) == 2,
            "fixed B09 transform and contents save by zone/cell/rotation");
        Require(dynamicRecord != null && dynamicRecord.gridX == dynamicCell.x && dynamicRecord.gridY == dynamicCell.y
            && dynamicRecord.rotationQuarterTurns == 1 && dynamicRecord.storedItems.Sum(i => i.count) == 1,
            "player-built B09 transform and contents save");

        fixedStorage.GameObject.transform.position += Vector3.right * 8f;
        fixedBox.items.Clear();
        dynamicBox.items.Clear();
        await save.LoadGameAsync();

        var restoredFixed = controller.GetPrimaryStorageForValidation();
        var restoredDynamic = controller.FindPlacementForValidation(dynamicStorage.InstanceId);
        Require(restoredFixed != null && restoredFixed.Cell == fixedCell && restoredFixed.RotationQuarterTurns == 3,
            "load restores authored B09 grid transform");
        Require(restoredFixed.GameObject.GetComponent<StorageBox>().items.Sum(i => i.count) == 2,
            "load restores authored B09 contents");
        Require(restoredDynamic != null && restoredDynamic.Cell == dynamicCell && restoredDynamic.RotationQuarterTurns == 1,
            "load restores player-built B09 grid transform");
        StorageBox restoredDynamicBox = restoredDynamic.GameObject.GetComponent<StorageBox>();
        Require(restoredDynamicBox.items.Sum(i => i.count) == 1, "load restores player-built B09 contents");
        Require(!controller.TryRecover(restoredFixed, false, out string fixedRecoverReason)
            && fixedRecoverReason.Contains("마지막", StringComparison.Ordinal),
            "authored village storage cannot be removed");
        restoredDynamicBox.items.Clear();
        Require(controller.TryRecover(restoredDynamic, false, out string recoverReason),
            $"empty player-built B09 recovers safely ({recoverReason})");
        Require(controller.FindPlacementForValidation(dynamicStorage.InstanceId) == null,
            "recovery releases the outdoor placement owner");

        Debug.Log($"PA Outdoor Placement Validation passed. protected={controller.ProtectedCellCount}, " +
            $"B09={fixedStorage.FootprintCellCount} cells, fixed={fixedCell}/r3, dynamic={dynamicCell}/r1, " +
            $"storage=2+1, schema=v10, output={output}");
    }

    static bool TryFindValid(OutdoorPlacementController controller, BuildingData data,
        OutdoorPlacementController.PlacementHandle moving, Vector3[] candidates, int rotation,
        out Vector2Int cell, out Vector3 world, out string reason)
    {
        reason = string.Empty;
        foreach (Vector3 candidate in candidates)
        {
            if (controller.TryResolvePreview(data, candidate, rotation, moving, out cell, out world, out reason)) return true;
        }
        cell = Vector2Int.zero;
        world = Vector3.zero;
        return false;
    }

    static void MovePlayer(GameObject player, Vector3 position)
    {
        CharacterController character = player.GetComponent<CharacterController>();
        if (character != null) character.enabled = false;
        player.transform.position = position;
        if (character != null) character.enabled = true;
    }

    static string BuildPath(Transform value)
    {
        string path = value != null ? value.name : string.Empty;
        for (Transform parent = value != null ? value.parent : null; parent != null; parent = parent.parent)
            path = parent.name + "/" + path;
        return path;
    }

    static async Task CaptureStorageCameraAsync(GameObject storage, string outputPath)
    {
        Camera camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        Require(camera != null && storage != null, "game camera and storage exist for capture");
        const int width = 1280;
        const int height = 720;

        var cameraController = camera.GetComponent<CameraController>()
            ?? camera.GetComponentInParent<CameraController>();
        bool controllerWasEnabled = cameraController != null && cameraController.enabled;
        if (cameraController != null)
            cameraController.enabled = false;
        try
        {
            await PA_SafeGameViewCapture.CaptureAsync(outputPath, camera, captureCamera =>
            {
                Vector3 focus = storage.transform.position + Vector3.up * 1.8f;
                captureCamera.transform.position = storage.transform.position + new Vector3(10f, 10f, -12f);
                captureCamera.transform.rotation = Quaternion.LookRotation(
                    focus - captureCamera.transform.position, Vector3.up);
                captureCamera.orthographic = true;
                captureCamera.orthographicSize = 7.2f;
            }, width, height, 1000);
        }
        finally
        {
            if (cameraController != null)
                cameraController.enabled = controllerWasEnabled;
        }
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
        Debug.Log($"PA OutdoorPlacement Check OK: {message}");
    }

    static void FailAndExit(string reason)
    {
        _hadError = true;
        SessionState.SetBool(ErrorKey, true);
        Debug.LogError($"PA OutdoorPlacement: {reason}");
        Cleanup();
        EditorApplication.Exit(1);
    }

    static void Finish()
    {
        bool failed = _hadError || SessionState.GetBool(ErrorKey, false);
        bool entered = _entered || SessionState.GetBool(EnteredKey, false);
        bool ran = _ran || SessionState.GetBool(RanKey, false);
        Cleanup();
        SessionState.EraseBool(ActiveKey); SessionState.EraseBool(EnteredKey);
        SessionState.EraseBool(RanKey); SessionState.EraseBool(ErrorKey); SessionState.EraseString(OutputKey);
        if (!entered || !ran || failed)
        {
            Debug.LogError("PA Outdoor Placement Validation failed. Check the first error.");
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log("PA Outdoor Placement Validation finished successfully.");
        EditorApplication.Exit(0);
    }

    static void Cleanup()
    {
        Application.logMessageReceived -= OnLog;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.update -= OnUpdate;
        _runtimeTask = null;
    }
}
#endif
