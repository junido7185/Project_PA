using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// P4-R4 only: native JsonUtility/contract checks, then at most one targeted Play session.
// Production progress fixtures are injected through the existing restore adapter; this is
// save-contract evidence, not proof of player input, production timing or a full opening route.
[InitializeOnLoad]
public static class PA_FirstProductionSaveContractChecks
{
    const string Key = "PA.P4R4.SaveContract";
    static string Output => SessionState.GetBool(Key + ".Resume", false) ? "Logs/P4-R4F" : "Logs/P4-R4";
    const string DiagnosticOutput = "Logs/P4-R4D";
    static bool Diagnostic => SessionState.GetBool(Key + ".Diagnostic", false);
    static int _checks;
    static Task _play;
    static double _deadline;
    static bool _runtimeError;
    static readonly string[] Ids = { "Lumberjack_01", "Miner_01" };

    static PA_FirstProductionSaveContractChecks()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += mode =>
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (mode == PlayModeStateChange.EnteredPlayMode)
            {
                _deadline = EditorApplication.timeSinceStartup + 120;
                _checks = 0;
                _runtimeError = false;
                _play = null;
                Application.runInBackground = true;
            }
            if (mode == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key + ".Done", false))
            {
                SessionState.SetBool(Key, false);
                EditorApplication.Exit(SessionState.GetBool(Key + ".Fail", false) ? 1 : 0);
            }
        };
        Application.logMessageReceived += (message, stack, type) =>
        {
            if (SessionState.GetBool(Key, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
            {
                _runtimeError = true;
                if (Diagnostic) File.AppendAllText(DiagnosticOutput + "/errors.txt", type + ": " + message + "\n" + stack + "\n");
            }
        };
    }

    public static void Run()
    {
        SessionState.SetBool(Key + ".Resume", false);
        SessionState.SetBool(Key + ".Diagnostic", false);
        Directory.CreateDirectory(Output);
        try
        {
            ContractChecks();
            File.WriteAllText(Output + "/native-contract-checks.txt", "PASS " + _checks + " native EditMode contract checks; actual JsonUtility and migration.\n");
        }
        catch (Exception ex)
        {
            File.WriteAllText(Output + "/native-contract-checks.txt", "FAIL " + ex + "\n");
            Debug.LogError(ex);
            EditorApplication.Exit(1);
            return;
        }
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + ".Done", false);
        SessionState.SetBool(Key + ".Fail", false);
        EditorSceneManager.OpenScene("Assets/Scenes/PA_DepartureTutorial.unity");
        EditorApplication.EnterPlaymode();
    }

    // P4-R4D: observation only. Do not rerun the recorded compile/contract/functional assertions.
    public static void RunSaveContinuation()
    {
        SessionState.SetBool(Key + ".Resume", true);
        SessionState.SetBool(Key + ".Diagnostic", false);
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + ".Done", false);
        SessionState.SetBool(Key + ".Fail", false);
        EditorSceneManager.OpenScene("Assets/Scenes/PA_DepartureTutorial.unity");
        EditorApplication.EnterPlaymode();
    }

    public static void RunReadinessDiagnostic()
    {
        Directory.CreateDirectory(DiagnosticOutput);
        SessionState.SetBool(Key + ".Diagnostic", true);
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + ".Done", false);
        SessionState.SetBool(Key + ".Fail", false);
        EditorSceneManager.OpenScene("Assets/Scenes/PA_DepartureTutorial.unity");
        EditorApplication.EnterPlaymode();
    }

    static string VoyageObservation(object voyage, string property) => voyage == null ? "missing" :
        Convert.ToString(voyage.GetType().GetProperty(property)?.GetValue(voyage) ?? "property unavailable");

    static void ObserveReadiness(FirstProductionController p, string point, double completedAt = -1)
    {
        var s = p != null ? p.Settlement : null;
        bool complete = s != null && s.IsReady && s.SettlementCompleted;
        float timer = p != null ? (float)typeof(FirstProductionController).GetField("_completedAt", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(p) : -1;
        var found = Object.FindFirstObjectByType<FirstProductionController>();
        string placed = s != null && s.IsReady ? string.Join(",", s.BuildingIds.Select(id => id + "=" + s.Placement.TryGetPlacement(id, out _))) : "unavailable";
        string state = $"{point}: editorTime={EditorApplication.timeSinceStartup:F3} frame={Time.frameCount} unscaled={Time.unscaledTime:F3} realtime={Time.realtimeSinceStartup:F3} paused={EditorApplication.isPaused} " +
            $"exists={p != null} instance={(p != null ? p.GetInstanceID() : 0)} foundInstance={(found != null ? found.GetInstanceID() : 0)} " +
            $"enabled={(p != null && p.enabled)} active={(p != null && p.gameObject.activeInHierarchy)} ready={(p != null && p.IsReady)} " +
            $"settlementExists={s != null} settlementReady={(s != null && s.IsReady)} complete={complete} " +
            $"arrived={VoyageObservation(s != null ? s.Voyage : null, "Arrived")} fadeFinished={VoyageObservation(s != null ? s.Voyage : null, "ArrivalFadeFinished")} " +
            $"updateGate={complete} observedCompletionAge={(completedAt < 0 ? -1 : EditorApplication.timeSinceStartup - completedAt):F3} " +
            $"p4Timer={timer:F3} initializeGate={(complete && timer >= 0 && Time.unscaledTime - timer >= 3)} errors={_runtimeError} buildings=[{placed}]";
        File.AppendAllText(DiagnosticOutput + "/readiness.txt", state + "\n");
    }

    static async Task DiagnoseReadiness(FirstProductionController p)
    {
        ObserveReadiness(p, "before-setup");
        var s = p.Settlement;
        if (!await s.PrepareRestoreAsync(new FirstSettlementSaveData { companionIds = Ids }))
            throw new InvalidOperationException("Diagnostic prerequisite arrival failed");
        ObserveReadiness(p, "arrival");
        var save = s.EnsureSaveManager();
        Directory.CreateDirectory(DiagnosticOutput + "/Save");
        typeof(SaveManager).GetMethod("SetRepositoryForValidation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(save,
            new object[] { new LocalJsonSaveRepository(Path.GetFullPath(DiagnosticOutput + "/Save")) });
        // Reproduce the original setup's load transition, without repeating its passed assertions.
        await save.SaveGameAsync();
        await save.LoadGameAsync();
        ObserveReadiness(p, "after-setup-load");
        var anchors = new[] { new Vector2Int(6, 5), new Vector2Int(9, 4), new Vector2Int(8, 7) };
        for (int i = 0; i < 3; i++)
        {
            var result = s.Placement.TryPlace(s.BuildingIds[i], anchors[i], i == 1 ? 1 : 0);
            ObserveReadiness(p, "placement-" + i + "-" + result.Succeeded);
            if (!result.Succeeded) throw new InvalidOperationException("Diagnostic placement failed: " + result.Failure);
        }
        double start = EditorApplication.timeSinceStartup;
        double completedAt = s.SettlementCompleted ? start : -1;
        int checkpoint = 0;
        var seconds = new[] { 1, 3, 10 };
        while (p != null && !p.IsReady && EditorApplication.timeSinceStartup - start < 25)
        {
            if (completedAt < 0 && s != null && s.SettlementCompleted)
            {
                completedAt = EditorApplication.timeSinceStartup;
                ObserveReadiness(p, "prerequisite-transition", completedAt);
            }
            if (checkpoint < seconds.Length && EditorApplication.timeSinceStartup - start >= seconds[checkpoint])
                ObserveReadiness(p, "wait-" + seconds[checkpoint++], completedAt);
            await Task.Delay(50);
        }
        ObserveReadiness(p, p != null && p.IsReady ? "ready-transition" : "timeout", completedAt);
    }

    static FirstSettlementSaveData Settlement(string[] ids = null) => new FirstSettlementSaveData
    {
        companionIds = ids ?? Ids, settlementCompleted = true
    };
    static FirstProductionSaveData Started(string[] ids = null, bool placed = false, bool claimed = false)
    {
        var state = new FirstProductionSaveData { started = true, completed = claimed };
        var profiles = Resources.Load<GameObject>("DepartureTutorial/DepartureContinuation").GetComponent<FirstProductionController>().worksiteProfiles;
        foreach (var id in ids ?? Ids)
        {
            var profile = StarterWorksiteProfile.Find(profiles, id);
            var record = new StarterWorksiteSaveData { companionId = id, role = profile.profileId };
            if (placed)
            {
                record.placement = new WorldPlacedBuildingSaveData { instanceId = FirstProductionController.WorksiteId(id), buildingId = profile.buildingId, anchorX = 20, anchorZ = 30 };
                record.producer = new StarterProducerSaveData { claimed = claimed };
                record.playerActivityCompleted = claimed;
                if (profile.activityKind == WorksiteActivityKind.Farming)
                {
                    record.farm = new FarmPlotSaveData { plotId = "p4-farm-" + id };
                    record.seedIssued = claimed;
                }
            }
            state.worksites.Add(record);
        }
        return state;
    }
    static T RoundTrip<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
    static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        _checks++;
        Debug.Log("[P4-R4] PASS " + label);
    }
    static void Bad(Action<FirstProductionSaveData> change, string label)
    {
        var state = Started(placed: true);
        change(state);
        Check(!FirstProductionController.IsValidSave(state, Settlement()), label);
    }
    static void ContractChecks()
    {
        _checks = 0;
        Check(SaveManager.CurrentSaveVersion == 16, "schema remains v16");
        var empty = RoundTrip(new SaveData()).firstProduction;
        Check(empty != null && !empty.started && !empty.completed && empty.worksites.Count == 0 && FirstProductionController.IsValidSave(empty, null), "not-started native JSON roundtrip");
        var go = new GameObject("P4R4_EditModeContract") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var controller = go.AddComponent<FirstProductionController>();
            Check(controller.RestoreState(empty) && !controller.IsReady && controller.Sites.Count == 0 && go.transform.childCount == 0 && go.GetComponent<ProducerNpcController>() == null && go.GetComponent<WorksiteBinding>() == null, "not-started restore creates no P4 structures/progress");
            Check(!controller.CaptureState().started && controller.CaptureState().worksites.Count == 0, "not-ready capture is explicit not-started");
            Check(!controller.RestoreState(new FirstProductionSaveData { completed = true }) && !controller.IsReady && go.transform.childCount == 0, "malformed direct restore rejected before mutation");
            var manager = go.AddComponent<SaveManager>();
            var p3 = Settlement();
            p3.buildings.Add(new WorldPlacedBuildingSaveData { instanceId = "p3-preserved", anchorX = 7, anchorZ = 9 });
            var before = JsonUtility.ToJson(p3);
            var v15 = new SaveData { version = 15, firstSettlement = p3, firstProduction = Started(placed: true, claimed: true) };
            var migrated = (SaveData)typeof(SaveManager).GetMethod("MigrateSaveData", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, new object[] { v15 });
            Check(migrated.version == 16 && ReferenceEquals(migrated.firstSettlement, p3) && JsonUtility.ToJson(p3) == before, "v15 migration preserves P3 exactly");
            Check(!migrated.firstProduction.started && !migrated.firstProduction.completed && migrated.firstProduction.worksites.Count == 0, "migration invents no placement/activity/stock/support consumption");
            Check(FirstProductionController.IsValidSave(RoundTrip(migrated).firstProduction, p3), "migrated not-started native JSON valid");
        }
        finally { Object.DestroyImmediate(go); }
        var pairs = new[] { Ids, new[] { "Miner_01", "Farmer_01" }, new[] { "Lumberjack_01", "Farmer_01" } };
        foreach (var pair in pairs)
        {
            Check(FirstProductionController.IsValidSave(RoundTrip(Started(pair)), Settlement(pair)), "started unplaced JSON " + string.Join(",", pair));
            Check(FirstProductionController.IsValidSave(RoundTrip(Started(pair, true)), Settlement(pair)), "started partial valid beyond 16x16 " + string.Join(",", pair));
            Check(FirstProductionController.IsValidSave(RoundTrip(Started(pair, true, true)), Settlement(pair)), "completed native JSON " + string.Join(",", pair));
        }
        Check(!FirstProductionController.IsValidSave(null, null), "null is not the discriminator");
        Check(!FirstProductionController.IsValidSave(new FirstProductionSaveData { worksites = null }, null), "null worksites rejected");
        Bad(s => s.started = false, "not-started with records rejected");
        Bad(s => s.completed = true, "completion without both claims rejected");
        Bad(s => s.version = 2, "unsupported payload version rejected");
        Bad(s => s.worksites.RemoveAt(1), "missing selected owner rejected");
        Bad(s => s.worksites[1] = s.worksites[0], "duplicate owner rejected");
        Bad(s => s.worksites[0] = null, "null owner record rejected");
        Bad(s => s.worksites[0].companionId = "Other", "unselected owner rejected");
        Bad(s => s.worksites[0].role = "Other", "role mismatch rejected");
        Bad(s => s.worksites[0].placement.instanceId = "Other", "worksite ID mismatch rejected");
        Bad(s => s.worksites[0].placement.buildingId = "Other", "building ID mismatch rejected");
        Bad(s => s.worksites[0].placement.rotationQuarterTurns = 4, "invalid rotation rejected");
        Bad(s => s.worksites[0].producer.elapsed = float.NaN, "NaN elapsed rejected");
        Bad(s => s.worksites[0].producer.elapsed = float.PositiveInfinity, "infinite elapsed rejected");
        Bad(s => s.worksites[0].producer.elapsed = -1, "negative elapsed rejected");
        Bad(s => s.worksites[0].producer.pendingAmount = -1, "negative pending amount rejected");
        Bad(s => s.worksites[0].producer.stockCount = -1, "negative stock rejected");
        Bad(s => s.worksites[0].producer.stockCount = int.MaxValue, "over-capacity stock rejected");
        Bad(s => s.worksites[0].producer.ready = true, "ready without stock rejected");
        Bad(s => s.worksites[0].producer.claimed = true, "claim without activity rejected");
        Bad(s => { var r = s.worksites[0]; r.playerActivityCompleted = true; r.producer.claimed = r.producer.ready = true; r.producer.stockCount = 1; }, "claimed stock rejected");
        Bad(s => { var r = s.worksites[0]; r.playerActivityCompleted = true; r.producer.active = r.producer.ready = true; r.producer.stockCount = 1; }, "working and ready rejected");
        Bad(s => { s.worksites[0].placement = null; s.worksites[0].producer.claimed = true; }, "unplaced support consumption rejected");
        Bad(s => { s.worksites[0].placement = new WorldPlacedBuildingSaveData(); s.worksites[0].playerActivityCompleted = true; }, "empty placement cannot hide activity");
        Bad(s => s.worksites[0].placement = new WorldPlacedBuildingSaveData { anchorX = 1 }, "partially populated placement rejected");
        Check(!FirstProductionController.IsValidSave(Started(), new FirstSettlementSaveData { settlementCompleted = true }), "missing selected IDs rejected");
        Check(!FirstProductionController.IsValidSave(Started(), new FirstSettlementSaveData { companionIds = Ids }), "P4 requires completed P3");
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || SessionState.GetBool(Key + ".Done", false) || !EditorApplication.isPlaying) return;
        var p = Object.FindFirstObjectByType<FirstProductionController>();
        if (_play == null && p != null && p.Settlement.Selection.Tutorial != null && p.Settlement.Selection.Tutorial.IsReady)
            _play = Diagnostic ? DiagnoseReadiness(p) : PlayChecks(p);
        if (_play != null && _play.IsCompleted)
            Finish(_play.IsFaulted ? _play.Exception.GetBaseException().ToString() : _runtimeError ? "Unity runtime error logged" : null);
        else if (EditorApplication.timeSinceStartup > _deadline) Finish("120 second targeted timeout");
    }
    static void Finish(string error)
    {
        if (Diagnostic)
            File.WriteAllText(DiagnosticOutput + "/result.txt", (error == null ? "Observation completed; inspect readiness.txt for classification." : "INCOMPLETE " + error) + "\nPlay sessions: 1. Functional assertions: 0.\n");
        else
            File.WriteAllText(Output + "/targeted-play-checks.txt", (error == null ? "PASS " + _checks + " targeted checks; runtime errors 0" : "FAIL " + error) + "\nPlay sessions: 1. Injected producer progress; no full opening regression.\n");
        SessionState.SetBool(Key + ".Fail", error != null);
        SessionState.SetBool(Key + ".Done", true);
        EditorApplication.ExitPlaymode();
    }
    static async Task Until(Func<bool> condition)
    {
        double end = EditorApplication.timeSinceStartup + 25;
        while (!condition() && EditorApplication.timeSinceStartup < end) await Task.Delay(50);
        Check(condition(), "bounded fixture readiness");
    }
    static async Task RoundTripLive(SaveManager save, FirstProductionController p, string stage)
    {
        await save.SaveGameAsync();
        var expected = p.CaptureState();
        int money = EconomyService.Instance.Money;
        EconomyService.Instance.ForceSet(money + 7, "P4R4 validation perturbation");
        await save.LoadGameAsync();
        var restored = p.CaptureState();
        Check(EconomyService.Instance.Money == money, stage + " SaveManager actually loaded");
        Check(restored.started == expected.started && restored.completed == expected.completed && FirstProductionController.IsValidSave(restored, p.Settlement.CaptureState()), stage + " contract retained");
        // Existing Producer.RestoreStarterState prepares a future pending amount when saved zero.
        // It must retain actual stock/claim/activity; that idle preparation is not new production.
        foreach (var record in expected.worksites.Where(r => r.producer != null && r.producer.pendingAmount == 0))
            record.producer.pendingAmount = restored.worksites.First(r => r.companionId == record.companionId).producer.pendingAmount;
        Check(JsonUtility.ToJson(restored) == JsonUtility.ToJson(expected), stage + " exact P4 state");
    }
    static async Task PlayChecks(FirstProductionController p)
    {
        var s = p.Settlement;
        Check(await s.PrepareRestoreAsync(new FirstSettlementSaveData { companionIds = Ids }), "direct P3 fixture arrival (no P0-P3 regression)");
        var save = s.EnsureSaveManager();
        Directory.CreateDirectory(Output + "/Save");
        var repository = new LocalJsonSaveRepository(Path.GetFullPath(Output + "/Save"));
        typeof(SaveManager).GetMethod("SetRepositoryForValidation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(save, new object[] { repository });
        if (!SessionState.GetBool(Key + ".Resume", false))
        {
            await RoundTripLive(save, p, "before P4");
            Check(!p.IsReady && p.Sites.Count == 0, "not-started reload creates no P4 sites");
        }

        // Use existing placement authority directly to supply only the P3 prerequisite fixture.
        var anchors = new[] { new Vector2Int(6, 5), new Vector2Int(9, 4), new Vector2Int(8, 7) };
        for (int i = 0; i < 3; i++) Check(s.Placement.TryPlace(s.BuildingIds[i], anchors[i], i == 1 ? 1 : 0).Succeeded, "prerequisite placement " + i);
        Check(p.enabled && p.gameObject.activeInHierarchy && s.SettlementCompleted, "P4 enabled and P3 prerequisites complete");
        await Until(() => (float)typeof(FirstProductionController).GetField("_completedAt", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(p) >= 0);
        Check(p.enabled, "P4 remains enabled and readiness timer started");
        await Until(() => p.IsReady);
        Check(p.IsReady && p.enabled, "P4 naturally initialized");
        await RoundTripLive(save, p, "unlocked before placement");
        foreach (var owner in Ids)
        {
            var grid = s.Voyage.IslandGrid.Definition;
            var anchor = Enumerable.Range(0, grid.Height).SelectMany(z => Enumerable.Range(0, grid.Width).Select(x => new Vector2Int(x, z)))
                .First(cell => s.Placement.Evaluate(FirstProductionController.WorksiteId(owner), cell, 0).Succeeded);
            Check(s.Placement.TryPlace(FirstProductionController.WorksiteId(owner), anchor, 0).Succeeded, "fixture worksite " + owner);
            await Until(() => p.Sites.ContainsKey(owner));
        }
        await RoundTripLive(save, p, "placed before activity");
        foreach (var site in p.Sites.Values)
        {
            var record = p.CaptureState().worksites.First(r => r.companionId == site.Owner);
            record.playerActivityCompleted = true;
            record.producer = new StarterProducerSaveData { elapsed = 1.25f, pendingAmount = 2 };
            site.Restore(record);
        }
        await RoundTripLive(save, p, "mid P4 paused producer fixture");
        foreach (var site in p.Sites.Values)
        {
            var record = p.CaptureState().worksites.First(r => r.companionId == site.Owner);
            record.producer = new StarterProducerSaveData { ready = true, stockCount = 2, pendingAmount = 2 };
            site.Restore(record);
        }
        await RoundTripLive(save, p, "batch ready");
        foreach (var site in p.Sites.Values)
        {
            int before = Inventory.instance.CountItems(site.Item);
            Check(site.Producer.TryClaimStarterBatch(Inventory.instance), "actual support claim " + site.Owner);
            Check(Inventory.instance.CountItems(site.Item) == before + 2, "exact one stock transfer " + site.Owner);
        }
        await RoundTripLive(save, p, "successful claims");
        Check(p.Complete && p.Sites.Values.All(site => !site.Producer.TryClaimStarterBatch(Inventory.instance)), "reload cannot repeat either support claim");

        string beforeBad = JsonUtility.ToJson(p.CaptureState());
        int beforeMoney = EconomyService.Instance.Money;
        var bad = JsonUtility.FromJson<SaveData>(await repository.LoadAsync("departure_settlement"));
        bad.money += 123;
        bad.firstProduction.started = false;
        await repository.SaveAsync("departure_settlement", JsonUtility.ToJson(bad));
        await save.LoadGameAsync();
        Check(EconomyService.Instance.Money == beforeMoney && JsonUtility.ToJson(p.CaptureState()) == beforeBad, "structurally malformed P4 rejected before live mutation");

        // Spatial rejection is deliberately last: no global rollback is claimed or attempted.
        // Make the SECOND placement invalid so the first succeeds and rollback of this P4 restore attempt is proven.
        var spatial = p.CaptureState();
        spatial.worksites[1].placement.anchorX = s.Voyage.IslandGrid.Definition.Width;
        Check(FirstProductionController.IsValidSave(spatial, s.CaptureState()), "spatial invalidity deferred to placement authority");
        p.ClearWorksitesForRestore();
        Check(!p.RestoreState(spatial), "placement rejection returns false without throwing");
        Check(p.Sites.Count == 0 && Ids.All(owner => !s.Placement.TryGetPlacement(FirstProductionController.WorksiteId(owner), out _)),
            "second-placement rejection rolls back partial P4 restore");
        Check(!_runtimeError, "runtime errors zero");
    }
}
