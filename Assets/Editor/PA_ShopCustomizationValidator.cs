#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// D3D11 batch validator for the P2 shop-interior customization loop.
[InitializeOnLoad]
public static class PA_ShopCustomizationValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.ShopCustomization.Active";
    const string EnteredKey = "PA.ShopCustomization.Entered";
    const string RanKey = "PA.ShopCustomization.Ran";
    const string HadErrorKey = "PA.ShopCustomization.HadError";
    const string OutputKey = "PA.ShopCustomization.Output";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;
    static Task _runtimeTask;

    static PA_ShopCustomizationValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _hadError = SessionState.GetBool(HadErrorKey, false);
        RegisterCallbacks();
        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Run Shop Customization Validation")]
    public static void RunShopCustomizationValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA ShopCustomization: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "ShopCustomization", stamp));
        Directory.CreateDirectory(output);
        _entered = false; _ran = false; _hadError = false; _runtimeTask = null;
        _startedAt = EditorApplication.timeSinceStartup;
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(HadErrorKey, false);
        SessionState.SetString(OutputKey, output);
        RegisterCallbacks();
        Debug.Log($"PA ShopCustomization: entering Play Mode. Output={output}");
        EditorApplication.EnterPlaymode();
    }

    static void RegisterCallbacks()
    {
        Application.logMessageReceived -= OnLogMessage;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        Application.logMessageReceived += OnLogMessage;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        _hadError = true;
        SessionState.SetBool(HadErrorKey, true);
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _entered = true;
            _startedAt = EditorApplication.timeSinceStartup;
            SessionState.SetBool(EnteredKey, true);
        }
        if (state == PlayModeStateChange.EnteredEditMode
            && SessionState.GetBool(ActiveKey, false)
            && SessionState.GetBool(RanKey, false)) Finish();
    }

    static void OnEditorUpdate()
    {
        double elapsed = EditorApplication.timeSinceStartup - _startedAt;
        if (!_entered)
        {
            if (elapsed > 60d) { Debug.LogError("PA ShopCustomization: timed out entering Play Mode."); MarkFailedAndExit(); }
            return;
        }
        if (!_ran && EditorApplication.isPlaying && _runtimeTask == null && elapsed > 4d)
        {
            _runtimeTask = RunRuntimeChecksAsync();
            return;
        }
        if (!_ran && _runtimeTask != null && _runtimeTask.IsCompleted)
        {
            if (_runtimeTask.IsFaulted)
            {
                _hadError = true;
                SessionState.SetBool(HadErrorKey, true);
                Debug.LogError($"PA Shop Customization Validation failed: {_runtimeTask.Exception?.GetBaseException()}");
            }
            _ran = true;
            SessionState.SetBool(RanKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }
        if (!_ran && elapsed > 120d)
        {
            Debug.LogError("PA ShopCustomization: timed out before runtime checks completed.");
            MarkFailedAndExit();
        }
    }

    static async Task RunRuntimeChecksAsync()
    {
        Time.timeScale = 1f;
        var controller = RequireOne<ShopCustomizationController>("ShopCustomizationController");
        for (int i = 0; i < 100 && !controller.IsReady; i++) await Task.Delay(50);
        Require(controller.IsReady, "customization controller initialized");
        Require(GridService.Instance != null && GridService.Instance.HasZone(controller.ZoneId), "shared GridService owns shop zone");
        Require(Mathf.Abs(controller.GridCellSize - 2f) < 0.01f, "shop zone reuses the existing 2m cell size");
        Require(GridService.Instance.GetZoneSize(controller.ZoneId) == new Vector2Int(5, 4), "shop zone is 5x4 cells");
        Require(GridService.Instance.IsZoneCellProtected(controller.ZoneId, new Vector2Int(2, 0)), "entry cell is protected");
        Require(GridService.Instance.HasZonePath(controller.ZoneId), "authored interior keeps entry-to-service path");
        Require(controller.FixedPlacementCount >= 6, "six authored ShopSlots registered as fixed placeables");

        string workbenchDefinition = controller.GetWorkbenchDefinitionId();
        Require(!string.IsNullOrEmpty(workbenchDefinition), "B05 workbench definition resolved from real blueprint");
        bool protectedRejected = !controller.TryPlaceDefinitionForValidation(workbenchDefinition,
            new Vector2Int(2, 0), 0, false, out _, out string protectedReason);
        Require(protectedRejected && !string.IsNullOrEmpty(protectedReason), "protected doorway rejects placement");
        bool overlapRejected = !controller.TryPlaceDefinitionForValidation(workbenchDefinition,
            new Vector2Int(1, 2), 0, false, out _, out string overlapReason);
        Require(overlapRejected && !string.IsNullOrEmpty(overlapReason), "occupied shelf cells reject overlap");

        string rightFront = controller.FindPlacementIdAt(new Vector2Int(3, 1));
        string rightBack = controller.FindPlacementIdAt(new Vector2Int(3, 2));
        Require(!string.IsNullOrEmpty(rightFront) && !string.IsNullOrEmpty(rightBack), "right-column authored shelves located");
        EmptyShopSlot(controller.GetPlacementGameObject(rightFront));
        EmptyShopSlot(controller.GetPlacementGameObject(rightBack));
        Require(controller.TryRecoverPlacementForValidation(rightFront, false, out string recoverReasonA),
            $"right-front shelf recovers safely ({recoverReasonA})");
        Require(controller.TryRecoverPlacementForValidation(rightBack, false, out string recoverReasonB),
            $"right-back shelf recovers safely ({recoverReasonB})");

        Require(controller.TryPlaceDefinitionForValidation(workbenchDefinition, new Vector2Int(3, 2), 0,
            false, out string workbenchId, out string placeReason), $"2x2 B05 workbench places ({placeReason})");
        GameObject workbenchGo = controller.GetPlacementGameObject(workbenchId);
        Workbench workbench = workbenchGo != null ? workbenchGo.GetComponent<Workbench>() : null;
        Require(workbench != null && !string.IsNullOrWhiteSpace(workbench.GetInteractPrompt()),
            "placed workbench retains functional interaction");
        NavMeshObstacle workbenchObstacle = workbenchGo.GetComponent<NavMeshObstacle>();
        Require(workbenchObstacle != null && workbenchObstacle.carving, "placed workbench keeps carving NavMesh obstacle");

        string moveId = controller.FindPlacementIdAt(new Vector2Int(1, 1));
        Require(!string.IsNullOrEmpty(moveId), "movable 1x1 shelf located");
        Require(controller.TryMovePlacementForValidation(moveId, new Vector2Int(4, 0), 3, out string moveReason),
            $"1x1 shelf moves and rotates 270 degrees ({moveReason})");
        Require(controller.TryGetPlacementSnapshot(moveId, out Vector2Int movedCell, out int movedRotation, out bool movedRecovered)
            && movedCell == new Vector2Int(4, 0) && movedRotation == 3 && !movedRecovered,
            "moved shelf runtime state stores cell and rotation");
        Require(GridService.Instance.HasZonePath(controller.ZoneId), "entry-to-service path survives furniture changes");

        ShopSlot movedSlot = controller.GetPlacementGameObject(moveId)?.GetComponent<ShopSlot>();
        Item bread = Resources.Load<Item>("Items/Item_BreadLoaf");
        Require(movedSlot != null && bread != null, "moved functional ShopSlot and bread item resolve");
        movedSlot.currentItem = new ItemInstance(bread, 1) { quality = 0.85f, currentPrice = 44 };
        movedSlot.displayPrice = 61;
        movedSlot.RefreshDisplay();

        // P4 — 이동/회전된 진열대의 interaction 셀이 실제 NavMesh 접근점과 예약 owner가 된다.
        await Task.Delay(500);
        var approach = RequireOne<ShopCustomerApproachController>("ShopCustomerApproachController");
        var approachPoints = new List<Vector3>();
        var approachCells = new List<Vector2Int>();
        Require(controller.TryGetNpcApproachPoints(movedSlot, approachPoints, approachCells, out string approachPlacementId)
            && approachPlacementId == moveId && approachCells.Count == 1
            && approachCells[0] == new Vector2Int(3, 0),
            "rotated shelf interaction offset rotates to explicit front cell (3,0)");

        ShopSlot secondReachableSlot = null;
        foreach (ShopSlot candidate in Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
        {
            if (candidate == null || candidate == movedSlot || !candidate.gameObject.activeInHierarchy
                || !candidate.transform.IsChildOf(GameObject.Find("PA_StoreInterior").transform)) continue;
            approachPoints.Clear();
            approachCells.Clear();
            if (controller.TryGetNpcApproachPoints(candidate, approachPoints, approachCells, out _)
                && approachPoints.Count > 0)
            {
                secondReachableSlot = candidate;
                break;
            }
        }
        Require(secondReachableSlot != null, "a second authored shelf has an unblocked interaction cell");
        secondReachableSlot.currentItem = new ItemInstance(bread, 1) { quality = 1f, currentPrice = bread.basePrice };
        secondReachableSlot.displayPrice = 15;
        secondReachableSlot.RefreshDisplay();

        NpcController[] npcCandidates = Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);
        NpcController firstNpc = null, secondNpc = null;
        foreach (NpcController npc in npcCandidates)
        {
            NavMeshAgent candidateAgent = npc != null ? npc.GetComponent<NavMeshAgent>() : null;
            if (candidateAgent == null || !candidateAgent.isActiveAndEnabled || !candidateAgent.isOnNavMesh) continue;
            if (firstNpc == null) firstNpc = npc;
            else { secondNpc = npc; break; }
        }
        Require(firstNpc != null && secondNpc != null, "two active NPC NavMesh agents exist for reservation checks");

        var insideSpawn = GameObject.Find("PA_StoreInterior")?.transform.Find("PlayerSpawn_Inside");
        NavMeshHit insideHit = default;
        bool insideSampled = insideSpawn != null && NavMesh.SamplePosition(insideSpawn.position, out insideHit,
            2.5f, NavMesh.AllAreas);
        Require(insideSampled, "interior spawn samples onto NavMesh");
        var firstAgent = firstNpc.GetComponent<NavMeshAgent>();
        var secondAgent = secondNpc.GetComponent<NavMeshAgent>();
        Vector3 firstOriginal = firstNpc.transform.position;
        Vector3 secondOriginal = secondNpc.transform.position;
        Require(firstAgent.Warp(insideHit.position), "first NPC warps to the interior NavMesh island");
        Require(secondAgent.Warp(insideHit.position), "second NPC warps to the interior NavMesh island");

        Require(approach.TryResolveReachablePoint(firstAgent, movedSlot, out _, out Vector3 movedApproach,
            out NavMeshPathStatus movedPathStatus) && movedPathStatus == NavMeshPathStatus.PathComplete,
            "moved shelf front cell has a complete sampled NavMesh path");
        Require(approach.TryReserveReachableSlot(firstNpc, firstAgent, new List<ShopSlot> { movedSlot },
            new System.Random(101), out ShopSlot firstReserved, out Vector3 firstPoint, out string firstReason)
            && firstReserved == movedSlot && Vector3.Distance(firstPoint, movedApproach) < 0.1f,
            $"first customer reserves moved shelf approach ({firstReason})");
        Require(!approach.TryReserveReachableSlot(secondNpc, secondAgent, new List<ShopSlot> { movedSlot },
            new System.Random(202), out _, out _, out _),
            "second customer cannot reserve the occupied approach owner");
        Require(approach.TryReserveReachableSlot(secondNpc, secondAgent,
            new List<ShopSlot> { movedSlot, secondReachableSlot }, new System.Random(303),
            out ShopSlot secondReserved, out _, out string secondReason)
            && secondReserved == secondReachableSlot && approach.ActiveReservationCount == 2,
            $"second customer reserves a different reachable shelf ({secondReason})");

        approach.Release(firstNpc);
        approach.Release(secondNpc);
        Require(approach.ActiveReservationCount == 0, "approach reservations release after browsing");
        firstAgent.Warp(firstOriginal);
        secondAgent.Warp(secondOriginal);

        int baselineMoney = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;
        Require(movedSlot.TryPurchaseByNpc("GridValidator", out int paid) && paid == 61,
            "NPC purchase function follows moved ShopSlot transform");
        if (EconomyService.Instance != null)
            Require(EconomyService.Instance.Money == baselineMoney + 61, "moved shelf sale deposits economy revenue");

        // Re-stock before the real SaveManager round trip to verify functional state and hierarchy-key restore.
        movedSlot.currentItem = new ItemInstance(bread, 2) { quality = 0.91f, currentPrice = 47 };
        movedSlot.displayPrice = 73;
        movedSlot.RefreshDisplay();

        string output = SessionState.GetString(OutputKey, string.Empty);
        Require(!string.IsNullOrWhiteSpace(output), "isolated validation output exists");
        var save = RequireOne<SaveManager>("SaveManager");
        var repositoryField = typeof(SaveManager).GetField("_repository", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(repositoryField != null, "SaveManager repository is injectable for isolated validation");
        repositoryField.SetValue(save, new LocalJsonSaveRepository(output));

        MovePlayerInsideForCapture();
        await save.SaveGameAsync();
        string saveFile = Path.Combine(output, "savegame.json");
        Require(File.Exists(saveFile), "isolated v10 save file written");
        SaveData saved = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveFile));
        Require(saved != null && saved.version == 10, "saved JSON uses schema v10");
        Require(saved.placeables != null && saved.placeables.Exists(p => p.instanceId == workbenchId
            && p.gridX == 3 && p.gridY == 2 && p.rotationQuarterTurns == 0),
            "save stores workbench definition, instance, cell and rotation");
        Require(saved.placeables.Exists(p => p.instanceId == moveId && p.gridX == 4 && p.gridY == 0
            && p.rotationQuarterTurns == 3), "save stores moved fixed shelf cell and rotation");
        Require(saved.placeables.Exists(p => p.instanceId == rightFront && p.recovered)
            && saved.placeables.Exists(p => p.instanceId == rightBack && p.recovered),
            "save stores recovered authored furniture");

        controller.RestoreSavedState(null, false);
        Require(controller.GetPlacementGameObject(workbenchId) == null, "mutation removes dynamic workbench before load");
        await save.LoadGameAsync();

        Require(controller.TryGetPlacementSnapshot(workbenchId, out Vector2Int restoredWorkbenchCell,
            out int restoredWorkbenchRotation, out bool restoredWorkbenchRecovered)
            && restoredWorkbenchCell == new Vector2Int(3, 2) && restoredWorkbenchRotation == 0 && !restoredWorkbenchRecovered,
            "SaveManager load restores workbench placement");
        Require(controller.GetPlacementGameObject(workbenchId)?.GetComponent<Workbench>() != null,
            "restored workbench remains functional");
        Require(controller.TryGetPlacementSnapshot(moveId, out Vector2Int restoredMoveCell,
            out int restoredMoveRotation, out bool restoredMoveRecovered)
            && restoredMoveCell == new Vector2Int(4, 0) && restoredMoveRotation == 3 && !restoredMoveRecovered,
            "SaveManager load restores moved shelf placement");
        ShopSlot restoredSlot = controller.GetPlacementGameObject(moveId)?.GetComponent<ShopSlot>();
        Require(restoredSlot != null && !restoredSlot.IsEmpty && restoredSlot.currentItem.data == bread
            && restoredSlot.currentItem.count == 2 && restoredSlot.displayPrice == 73,
            "moved shelf stock and display price restore after restart-equivalent load");
        Require(GridService.Instance.HasZonePath(controller.ZoneId), "restored layout preserves protected route");

        string screenshot = Path.Combine(output, "shop_customization_game_camera.png");
        controller.ToggleFromTerminal(GameObject.FindGameObjectWithTag("Player"));
        await CaptureGameCameraAsync(screenshot);
        controller.CloseCustomization();
        Require(File.Exists(screenshot) && new FileInfo(screenshot).Length > 1024,
            "game-camera customization capture generated");

        Require(controller.TryRecoverPlacementForValidation(workbenchId, false, out string workbenchRecoverReason),
            $"placed workbench can be recovered ({workbenchRecoverReason})");
        Require(controller.GetPlacementGameObject(workbenchId) == null, "recovered workbench removes runtime placement and occupancy owner");

        Debug.Log($"PA Shop Customization Validation passed. fixed={controller.FixedPlacementCount}, " +
            $"moved={moveId}@{restoredMoveCell}/r{restoredMoveRotation}, workbench=2x2, sale=61G, schema=v10, output={output}");
    }

    static void EmptyShopSlot(GameObject target)
    {
        ShopSlot slot = target != null ? target.GetComponent<ShopSlot>() : null;
        if (slot == null) return;
        slot.currentItem = null;
        slot.displayPrice = 0;
        slot.RefreshDisplay();
    }

    static void MovePlayerInsideForCapture()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Transform interior = GameObject.Find("PA_StoreInterior")?.transform;
        if (player == null || interior == null) return;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = interior.TransformPoint(new Vector3(0f, 0.08f, -2.7f));
        player.transform.rotation = interior.rotation * Quaternion.LookRotation(Vector3.forward, Vector3.up);
        if (controller != null) controller.enabled = true;
    }

    static async Task CaptureGameCameraAsync(string outputPath)
    {
        Camera camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        Require(camera != null, "game camera exists for capture");
        Transform interior = GameObject.Find("PA_StoreInterior")?.transform;
        Require(interior != null, "shop interior exists for capture");

        await PA_SafeGameViewCapture.CaptureAsync(outputPath, camera, captureCamera =>
        {
            // Stay just inside the south wall so the authored wall does not hide the
            // floor route, while retaining the production isometric camera language.
            captureCamera.transform.position = interior.TransformPoint(new Vector3(0f, 8.2f, -3.75f));
            captureCamera.transform.rotation = Quaternion.LookRotation(
                interior.TransformPoint(new Vector3(0f, 0.30f, 0.45f)) - captureCamera.transform.position,
                interior.up);
            captureCamera.orthographic = true;
            captureCamera.orthographicSize = 5.55f;
        });
    }

    static T RequireOne<T>(string label) where T : Object
    {
        T value = Object.FindFirstObjectByType<T>();
        if (value == null) throw new InvalidOperationException($"{label} not found.");
        return value;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"PA ShopCustomization Check OK: {message}");
    }

    static void MarkFailedAndExit()
    {
        _hadError = true;
        SessionState.SetBool(HadErrorKey, true);
        Cleanup();
        EditorApplication.Exit(1);
    }

    static void Finish()
    {
        bool hadError = _hadError || SessionState.GetBool(HadErrorKey, false);
        bool entered = _entered || SessionState.GetBool(EnteredKey, false);
        bool ran = _ran || SessionState.GetBool(RanKey, false);
        Cleanup();
        SessionState.EraseBool(ActiveKey); SessionState.EraseBool(EnteredKey);
        SessionState.EraseBool(RanKey); SessionState.EraseBool(HadErrorKey); SessionState.EraseString(OutputKey);
        if (!entered || !ran || hadError)
        {
            Debug.LogError("PA Shop Customization Validation failed. Check the first error in Editor.log.");
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log("PA Shop Customization Validation finished successfully.");
        EditorApplication.Exit(0);
    }

    static void Cleanup()
    {
        Application.logMessageReceived -= OnLogMessage;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        _runtimeTask = null;
    }
}
#endif
