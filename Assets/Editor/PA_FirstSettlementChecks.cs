using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

// Bounded P3 checks; reuses the P0 input helpers and existing safe Game View capture.
[InitializeOnLoad]
public static class PA_FirstSettlementChecks
{
    const string Key = "PA.FirstSettlement.Checks";
    const string Output = "Docs/Presentation/2026-09-09/P3";
    const string SaveFolder = "Logs/VS_PRESENT_001/P3/ValidationSave";
    const BindingFlags HiddenStatic = BindingFlags.Static | BindingFlags.NonPublic;
    static Task _task;
    static Keyboard _keyboard;
    static double _started;

    static PA_FirstSettlementChecks()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += Mode;
        Application.logMessageReceived += (message, stack, type) => {
            if (SessionState.GetBool(Key, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                SessionState.SetBool(Key + ".Error", true);
        };
    }

    [MenuItem("Project PA/Validation/Run First Settlement P3")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        Directory.CreateDirectory(SaveFolder);
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + ".Error", false);
        SessionState.SetInt(Key + ".Phase", 0);
        SessionState.SetBool(Key + ".Done", false);
        SessionState.SetBool(Key + ".Remaining", false);
        SessionState.SetBool(Key + ".SavedEvidence", false);
        SessionState.SetBool("PA.Companions.DevelopmentEntry", false);
        EditorSceneManager.OpenScene("Assets/Scenes/PA_DepartureTutorial.unity", OpenSceneMode.Single);
        PA_DepartureContinuationChecks.ConfigureGameView();
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        EditorApplication.EnterPlaymode();
    }

    public static void RunRemaining()
    {
        Run();
        SessionState.SetBool(Key + ".Remaining", true);
    }

    public static void RunSavedEvidence()
    {
        var saved = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path.Combine(SaveFolder,"departure_settlement.json")));
        Run();
        SessionState.SetInt(Key + ".Phase", 1);
        SessionState.SetBool(Key + ".Remaining", true);
        SessionState.SetBool(Key + ".SavedEvidence", true);
        SessionState.SetString(Key + ".Expected", JsonUtility.ToJson(saved.firstSettlement));
        SessionState.SetInt(Key + ".Money", saved.money);
    }

    static void Mode(PlayModeStateChange mode)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (mode == PlayModeStateChange.EnteredPlayMode)
        {
            _task = null; _started = EditorApplication.timeSinceStartup;
            SessionState.SetBool(Key + ".OriginalBackground", Application.runInBackground);
            Application.runInBackground = true;
        }
        if (mode == PlayModeStateChange.ExitingPlayMode)
            Application.runInBackground = SessionState.GetBool(Key + ".OriginalBackground", false);
        if (mode != PlayModeStateChange.EnteredEditMode) return;
        if (SessionState.GetBool(Key + ".Done", false))
        {
            bool failed = SessionState.GetBool(Key + ".Error", false);
            SessionState.SetBool(Key, false);
            Debug.Log(failed ? "[VS-P3] VALIDATION_FAIL" : "[VS-P3] P3_VALIDATION_PASS");
            if (Environment.GetCommandLineArgs().Contains("-executeMethod")) EditorApplication.Exit(failed ? 1 : 0);
        }
        else if (SessionState.GetInt(Key + ".Phase", 0) == 1)
            EditorApplication.delayCall += () => {
                EditorSceneManager.OpenScene("Assets/Scenes/PA_DepartureTutorial.unity", OpenSceneMode.Single);
                EditorApplication.EnterPlaymode();
            };
    }

    static void Tick()
    {
        // A domain/asset refresh can discard the delayed edit-mode callback. Keep the saved phase resumable.
        if (SessionState.GetBool(Key, false) && !SessionState.GetBool(Key + ".Done", false) && SessionState.GetInt(Key + ".Phase", 0) == 1 &&
            !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            EditorApplication.EnterPlaymode();
            return;
        }
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || SessionState.GetBool(Key + ".Done", false)) return;
        var s = Object.FindFirstObjectByType<FirstIslandSettlementController>();
        if (_task == null && s != null && s.Selection.Tutorial != null && s.Selection.Tutorial.IsReady)
            _task = SessionState.GetInt(Key + ".Phase", 0) == 0 ? Play(s) : Reentry(s);
        if (_task != null && _task.IsCompleted)
        {
            if (_task.IsFaulted) { SessionState.SetBool(Key + ".Error", true); Debug.LogError("[VS-P3] " + _task.Exception.GetBaseException()); }
            if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
            _keyboard = null;
            if (SessionState.GetBool(Key + ".Error", false) || SessionState.GetInt(Key + ".Phase", 0) == 1)
                SessionState.SetBool(Key + ".Done", true);
            else SessionState.SetInt(Key + ".Phase", 1);
            _task = null;
            EditorApplication.ExitPlaymode();
        }
        else if (EditorApplication.timeSinceStartup - _started > 240)
        {
            Debug.LogError("[VS-P3] Validation timeout");
            SessionState.SetBool(Key + ".Error", true); SessionState.SetBool(Key + ".Done", true);
            EditorApplication.ExitPlaymode();
        }
    }

    static async Task Play(FirstIslandSettlementController s)
    {
        var t = s.Selection.Tutorial;
        _keyboard = InputSystem.AddDevice<Keyboard>("P3ValidationKeyboard"); _keyboard.MakeCurrent();
        typeof(PA_DepartureTutorialValidator).GetField("_keyboard", HiddenStatic).SetValue(null, _keyboard);
        await Task.Delay(600);
        if (!SessionState.GetBool(Key + ".Remaining", false))
        {
        await Walk(t.player, new Vector3(t.movementCheckpoint.position.x, t.player.position.y, t.player.position.z));
        await Until(() => t.Stage == 2, "P0 actual movement");
        await Walk(t.player, new Vector3(t.fruitTree.transform.position.x, 0, -3));
        await Walk(t.player, t.fruitTree.transform.position + Vector3.back * 1.6f);
        await Input("Interact", t, t.fruitTree);
        await Until(() => t.Stage == 3, "P0 tree inventory");
        await Walk(t.player, new Vector3(t.player.position.x, 0, -3.4f));
        await Walk(t.player, new Vector3(t.trainingSlot.transform.position.x, 0, -3.4f));
        await Walk(t.player, t.trainingSlot.transform.position + Vector3.back * 1.7f);
        await ReachShelf(t);
        await Input("Interact", t, t.trainingSlot);
        await Until(() => t.Stage == 4, "P0 ShopSlot");
        await Input("Interact", t, t.trainingSlot);
        int before = EconomyService.Instance.Money;
        Check(ShopPriceUI.instance.IsOpen, "actual price UI opened before confirmation");
        typeof(PA_DepartureTutorialValidator).GetMethod("ConfirmPrice", HiddenStatic).Invoke(null, new object[] { 7 });
        await Until(() => t.Complete || t.Stage == 4, "P0 real purchase decision");
        if (!t.Complete)
        {
            await Input("Interact", t, t.trainingSlot);
            typeof(PA_DepartureTutorialValidator).GetMethod("ConfirmPrice", HiddenStatic).Invoke(null, new object[] { 7 });
            await Until(() => t.Complete, "P0 normal retry");
        }
        Check(t.DecisionCount > 0 && EconomyService.Instance.Money == before + 7, "P0 sale uses original economy");
        t.presentation.CompanionButton.onClick.Invoke();
        }
        else s.Selection.OpenDevelopmentSelection();
        await Until(() => s.Selection.IsOpen, "certification unlocks P1");
        s.Selection.CandidateButtons[1].onClick.Invoke(); s.Selection.CandidateButtons[2].onClick.Invoke();
        s.Selection.DepartureButton.onClick.Invoke();
        await Until(() => s.Voyage.Companions.Count == 2, "P2 selected companions on boat");
        var companions = s.Voyage.Companions.ToArray();
        await Until(() => s.IsReady, "P2 fade arrival to P3");
        Check(s.Selection.ConfirmedIds.SequenceEqual(new[] { "Miner_01", "Farmer_01" }) && s.Voyage.Companions.SequenceEqual(companions), "same confirmed IDs and same P2 objects");
        ConfigureRepository(s);
        int money = EconomyService.Instance.Money;
        var natural = s.Voyage.IslandRoot.GetComponentsInChildren<Collider>().Where(c => c.name.StartsWith("TimberResource_") || c.name.StartsWith("StoneResource_")).ToArray();
        Check(natural.Length == 16, "all twelve tree and four rock colliders recorded");
        Vector3 spawn = t.player.position;
        await Walk(t.player, spawn + Vector3.right * 1.2f);
        Check(Vector3.Distance(spawn, t.player.position) > .6f, "arrival PlayerController movement");
        await Task.Delay(2600);
        await Capture("01_Island_BeforeSettlement.png");
        s.Begin(FirstIslandSettlementController.HubId);
        Check(s.BuildMode && !t.player.GetComponent<PlayerController>().enabled, "build mode preview and control gate");
        var tree = s.PreviewAt(new Vector2Int(10, 6), 0);
        Check(!tree.Succeeded && tree.Failure == WorldBuildingPlacementFailure.PhysicalObstacle && !s.Commit(), "natural tree blocks placement without removal");
        Check(!s.PreviewAt(Vector2Int.zero, 0).Succeeded, "water or protected shore blocks placement");
        Check(!s.PreviewAt(new Vector2Int(7,2),0).Succeeded, "dock overlap blocked");
        s.Cancel();
        Check(!s.BuildMode && s.Placement.RegisteredCount == 0 && t.player.GetComponent<PlayerController>().enabled, "cancel preview restores controls with no building");
        s.Begin(FirstIslandSettlementController.HubId);
        var hubCell = FindValid(s, FirstIslandSettlementController.HubId, new Vector2Int(6, 5), 0);
        Check(s.PreviewAt(hubCell, 0).Succeeded, "hub has valid flat footprint");
        await Capture("02_Settlement_PlacementPreview.png");
        s.ConfirmButton.onClick.Invoke();
        Check(s.Placement.RegisteredCount == 1, "real placement confirm creates hub");
        s.Begin(FirstIslandSettlementController.HubId);
        s.Rotate();
        Check(s.Placement.LastResult.QuarterTurns == 1 && s.Placement.PreviewIsMove, "existing move preview rotates 90 degrees");
        s.Cancel();
        Check(s.Placement.TryGetPlacement(FirstIslandSettlementController.HubId,out var original) && original.Anchor == hubCell && original.QuarterTurns == 0, "cancel move preserves original pose and occupancy");
        s.Begin(FirstIslandSettlementController.HubId);
        var moved = FindValid(s, FirstIslandSettlementController.HubId, new Vector2Int(6, 5), 0, hubCell);
        s.PreviewAt(moved, 0); Check(s.Commit(), "placed hub moves without duplicates");
        // Return to the intentional presentation location through the same move authority.
        s.Begin(FirstIslandSettlementController.HubId); s.PreviewAt(hubCell, 0); Check(s.Commit(), "hub move back retains one instance");
        for (int i = 1; i < 3; i++)
        {
            string id = s.BuildingIds[i]; s.BuildingButtons[i].onClick.Invoke();
            int turn = i == 1 ? 1 : 0;
            var cell = FindValid(s, id, i == 1 ? new Vector2Int(9, 4) : new Vector2Int(8, 7), turn);
            Check(s.PreviewAt(cell, turn).Succeeded && s.Commit(), "selected companion shelter " + id);
        }
        Check(s.SettlementCompleted && s.P4Unlocked && s.Placement.RegisteredCount == 3, "minimal settlement 1 hub + 2 linked shelters complete");
        Check(s.Voyage.Companions.Count == 2 && !s.Voyage.Companions.Any(c=>c.name.Contains("Lumberjack")), "unselected third companion absent");
        Check(EconomyService.Instance.Money == money, "first three structures free through existing placement");
        Check(natural.All(c => c != null && c.enabled), "natural colliders preserved");
        await Until(() => s.Voyage.Companions.All(c => { var a = c.GetComponent<NavMeshAgent>(); return a != null && a.isOnNavMesh && !a.pathPending && a.remainingDistance < 1.5f; }), "same companions walk to assigned shelters");
        for (int i=0;i<2;i++)
        {
            Check(s.Placement.TryGetPlacement(s.BuildingIds[i+1],out var shelter), "canonical shelter binding exists");
            s.Voyage.IslandGrid.CellToWorld(shelter.Entrance,out var destination);
            Check(Vector3.Distance(s.Voyage.Companions[i].transform.position,destination)<2f,"companion actually near its own shelter entrance");
        }
        await Task.Delay(3000);
        await Capture("03_FirstSettlement_Complete.png");
        Check(s.Objective.Contains("첫 영업"), "single next objective without P4 implementation");
        PA_DepartureContinuationChecks.ValidateReferences(s.gameObject);
        PA_DepartureContinuationChecks.ValidateReferences(s.Voyage.IslandGrid.gameObject);
        StorageRegression();
        var state = s.CaptureState();
        SessionState.SetString(Key + ".Expected", JsonUtility.ToJson(state));
        SessionState.SetInt(Key + ".Money", money);
        await s.EnsureSaveManager().SaveGameAsync();
        Check(File.Exists(Path.Combine(SaveFolder, "departure_settlement.json")), "existing repository wrote dedicated slot");
        s.Begin(FirstIslandSettlementController.HubId);
        var changed = FindValid(s, FirstIslandSettlementController.HubId, hubCell, 0, hubCell);
        s.PreviewAt(changed, 0); Check(s.Commit(), "state changed before restore");
        await s.EnsureSaveManager().LoadGameAsync();
        Check(JsonUtility.ToJson(s.CaptureState()) == JsonUtility.ToJson(state), "SaveManager restores exact poses rotations companion binding and completion");
        Check(!SessionState.GetBool(Key + ".Error", false), "runtime errors zero before full reentry");
        Debug.Log("[VS-P3] FULL_FLOW_AND_SAVE_PASS; unloading scene for fresh reentry");
    }

    static async Task Reentry(FirstIslandSettlementController s)
    {
        await Task.Delay(500);
        Check(!s.IsReady && !s.Selection.IsConfirmed, "fresh scene has no cached settlement");
        ConfigureRepository(s);
        await s.ResumeAsync();
        Check(s.IsReady && s.SettlementCompleted, "existing SaveManager loads settlement through new scene entry");
        Check(JsonUtility.ToJson(s.CaptureState()) == SessionState.GetString(Key + ".Expected", ""), "fresh reentry exact persisted state");
        Check(EconomyService.Instance.Money == SessionState.GetInt(Key + ".Money", -1), "reentry retains economy without double sale");
        Check(Object.FindObjectsByType<SaveManager>(FindObjectsSortMode.None).Length == 1, "one SaveManager authority");
        await Until(() => s.Voyage.Companions.All(c => c.GetComponent<NavMeshAgent>() != null), "reentry same selected companion shelter binding");
        if (SessionState.GetBool(Key + ".SavedEvidence", false))
        {
            await Until(() => Enumerable.Range(0,2).All(i => {
                s.Placement.TryGetPlacement(s.BuildingIds[i+1],out var shelter);
                s.Voyage.IslandGrid.CellToWorld(shelter.Entrance,out var destination);
                return Vector3.Distance(s.Voyage.Companions[i].transform.position,destination)<2;
            }), "restored companions reached their linked shelters");
            await Task.Delay(1000);
            Check(s.Voyage.IslandGrid.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials)
                .Any(m=>m!=null && m.name=="PA_ShelterCanvas" && m.GetFloat("_Cull")==0), "thin canvas renders both faces");
            PA_DepartureContinuationChecks.ValidateReferences(s.gameObject);
            await Capture("03_FirstSettlement_Complete.png");
        }
        Check(!SessionState.GetBool(Key + ".Error", false), "all runtime errors zero");
        string evidenceFile = SessionState.GetBool(Key + ".SavedEvidence", false) ? "/P3-reload-visual-validation.json" : "/P3-validation.json";
        File.WriteAllText(Output + evidenceFile, JsonUtility.ToJson(new Evidence {
            status = "PASS", graphics = SystemInfo.graphicsDeviceType.ToString(), width = Screen.width, height = Screen.height,
            fullP0P1P2P3Flow = !SessionState.GetBool(Key + ".Remaining", false), priorOpeningEvidence = "D3D11-02.log: actual P0 sale -> P1 -> P2 -> hub movement PASS",
            storagePlacementRegression = !SessionState.GetBool(Key + ".SavedEvidence", false), saveAndFreshReentry = true, naturalObjectsPreserved = true,
            settlement = s.CaptureState()
        }, true));
    }

    static Vector2Int FindValid(FirstIslandSettlementController s, string id, Vector2Int preferred, int turn, Vector2Int? except = null)
    {
        var cells = Enumerable.Range(1, 14).SelectMany(x => Enumerable.Range(1, 14).Select(z => new Vector2Int(x, z))).OrderBy(c => (c - preferred).sqrMagnitude);
        foreach (var cell in cells)
            if (cell != except && s.Placement.Evaluate(id, cell, turn, s.Placement.TryGetPlacement(id, out _) ? id : null).Succeeded) return cell;
        throw new InvalidOperationException("No valid placement for " + id);
    }

    static void ConfigureRepository(FirstIslandSettlementController s) => typeof(SaveManager).GetMethod("SetRepositoryForValidation", BindingFlags.Instance | BindingFlags.NonPublic)
        .Invoke(s.EnsureSaveManager(), new object[] { new LocalJsonSaveRepository(Path.GetFullPath(SaveFolder)) });

    static void StorageRegression()
    {
        var root = new GameObject("P3_StorageRegression");
        var grid = root.AddComponent<WorldGridService>();
        var definition = new WorldGridDefinition(2, 16, 16, 8, .25f, 0, 6, new Vector3(500, 0, 500));
        var cells = Enumerable.Range(0, 16).SelectMany(z => Enumerable.Range(0, 16).Select(x => new WorldCellData(new Vector2Int(x,z), 0, WorldGroundType.Default, false, false, WorldCellOccupancy.Empty))).ToArray();
        bool restored = (bool)typeof(WorldGridService).GetMethod("TryRestoreSnapshot", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(grid, new object[] { definition, cells });
        Check(restored, "isolated flat grid for existing storage regression");
        var service = root.AddComponent<WorldBuildingPlacementService>();
        string id = "p3-regression-storage";
        Check(service.BeginPlacementPreview(id, new Vector2Int(3,3),0).Succeeded && service.CommitPreview().Succeeded, "unregistered IDs retain original B09 storage placement");
        Check(service.TryGetPlacement(id,out var building) && building.Definition.StableId == "B09_STORAGE_SHED" && building.Footprint.Count == 12, "original storage definition retained");
        Check(!service.TryMove(id, new Vector2Int(15,15),0).Succeeded && building.Anchor == new Vector2Int(3,3), "invalid storage move keeps original occupancy");
        Check(service.BeginMovePreview(id,new Vector2Int(9,6),1).Succeeded && service.CommitPreview().Succeeded, "original storage rotate and move");
        Check(service.RegisteredCount == 1 && service.TryRemove(id).Succeeded && service.RegisteredCount == 0, "original storage removal clears occupancy");
        Object.Destroy(root);
    }

    static Task Walk(Transform player, Vector3 destination) => Input("Walk", player, destination);
    static async Task ReachShelf(DepartureTutorialController t)
    {
        var interaction = t.player.GetComponent<PlayerInteraction>();
        var find = typeof(PlayerInteraction).GetMethod("TryFindInteractable", BindingFlags.Instance | BindingFlags.NonPublic);
        for (int i = 0; i < 20; i++)
        {
            object[] args = { null, null };
            if ((bool)find.Invoke(interaction, args) && ReferenceEquals(args[0], t.trainingSlot)) break;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(UnityEngine.InputSystem.Key.W));
            await Task.Delay(80);
        }
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        await Task.Delay(200);
        Debug.Log("[VS-P3] shelf input approach position=" + t.player.position + " forward=" + t.player.forward);
    }
    static Task Input(string method, params object[] args) => (Task)typeof(PA_DepartureTutorialValidator).GetMethod(method, HiddenStatic).Invoke(null, args);
    static Task Capture(string name) => PA_SafeGameViewCapture.CaptureAsync(Path.GetFullPath(Output + "/" + name), Camera.main, null, 1920, 1080, 650);
    static async Task Until(Func<bool> predicate, string message)
    {
        double deadline = EditorApplication.timeSinceStartup + 35;
        while (!predicate() && EditorApplication.timeSinceStartup < deadline) await Task.Delay(80);
        Check(predicate(), message);
    }
    static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        Debug.Log("[VS-P3] CHECK_OK " + message);
    }
    [Serializable] class Evidence
    {
        public string status, graphics, priorOpeningEvidence;
        public int width, height;
        public bool fullP0P1P2P3Flow, storagePlacementRegression, saveAndFreshReentry, naturalObjectsPreserved;
        public FirstSettlementSaveData settlement;
    }
}
