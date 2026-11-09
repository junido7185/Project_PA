using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// P4-R5: one bounded Play session only.
// Scope: real ProducerNpcController timing, full-inventory SettlementSupport failure persistence,
// then fresh-scene reentry. Player-activity completion is supplied as a fixture; producer progress
// (timer / stock / claim) is never injected.
[InitializeOnLoad]
public static class PA_FirstProductionR5Checks
{
    const string Key = "PA.P4R5.Integration";
    const string Output = "Logs/P4-R5";
    const string ScenePath = "Assets/Scenes/PA_DepartureTutorial.unity";
    static readonly string[] Ids = { "Lumberjack_01", "Miner_01" };

    static Task _play;
    static double _deadline;
    static bool _runtimeError;
    static int _checks;
    static bool _previousRunInBackground;

    static readonly FieldInfo PendingAmountField =
        typeof(ProducerNpcController).GetField("_pendingProductionAmount", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly MethodInfo EffectiveIntervalMethod =
        typeof(ProducerNpcController).GetMethod("CalculateEffectiveInterval", BindingFlags.Instance | BindingFlags.NonPublic);

    static PA_FirstProductionR5Checks()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Application.logMessageReceived += OnLog;
    }

    [MenuItem("Project PA/Validation/P4 R5 Integration")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, "evidence.txt"),
            "P4-R5: actual producer timing + full inventory claim persistence + fresh-scene reentry\n");
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + ".Done", false);
        SessionState.SetBool(Key + ".Fail", false);
        _play = null;
        _checks = 0;
        _runtimeError = false;
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.EnterPlaymode();
    }

    static void OnPlayModeChanged(PlayModeStateChange mode)
    {
        if (!SessionState.GetBool(Key, false)) return;

        if (mode == PlayModeStateChange.EnteredPlayMode)
        {
            _previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            _deadline = EditorApplication.timeSinceStartup + 240.0;
            _play = null;
            _checks = 0;
            _runtimeError = false;
        }
        else if (mode == PlayModeStateChange.EnteredEditMode &&
                 SessionState.GetBool(Key + ".Done", false))
        {
            Application.runInBackground = _previousRunInBackground;
            SessionState.SetBool(Key, false);
            int code = SessionState.GetBool(Key + ".Fail", false) ? 1 : 0;
            if (Application.isBatchMode) EditorApplication.Exit(code);
            else Debug.Log(code == 0 ? "[P4-R5] PASS" : "[P4-R5] FAIL — see Logs/P4-R5/result.txt");
        }
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        _runtimeError = true;
        Directory.CreateDirectory(Output);
        File.AppendAllText(Path.Combine(Output, "runtime-errors.txt"),
            type + ": " + message + "\n" + stack + "\n");
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) ||
            SessionState.GetBool(Key + ".Done", false) ||
            !EditorApplication.isPlaying) return;

        var p = Object.FindFirstObjectByType<FirstProductionController>();
        if (_play == null &&
            p != null &&
            p.Settlement != null &&
            p.Settlement.Selection != null &&
            p.Settlement.Selection.Tutorial != null &&
            p.Settlement.Selection.Tutorial.IsReady)
        {
            _play = Checks(p);
        }

        if (_play != null && _play.IsCompleted)
        {
            Finish(_play.IsFaulted
                ? _play.Exception.GetBaseException().ToString()
                : _runtimeError ? "Unity runtime error logged" : null);
        }
        else if (EditorApplication.timeSinceStartup > _deadline)
        {
            Finish("240 second targeted timeout");
        }
    }

    static async Task Checks(FirstProductionController p)
    {
        // Direct P3 fixture only. This is not a P0-P3 regression pass.
        var s = p.Settlement;
        Check(await s.PrepareRestoreAsync(new FirstSettlementSaveData { companionIds = Ids }),
            "direct P3 fixture arrival");

        var save = s.EnsureSaveManager();
        ConfigureRepository(save);

        var anchors = new[]
        {
            new Vector2Int(6, 5),
            new Vector2Int(9, 4),
            new Vector2Int(8, 7)
        };
        for (int i = 0; i < 3; i++)
            Check(s.Placement.TryPlace(s.BuildingIds[i], anchors[i], i == 1 ? 1 : 0).Succeeded,
                "P3 prerequisite placement " + i);

        await Until(() => p.IsReady, "P3 completion unlocks P4", 10f);
        Check(p.enabled && p.gameObject.activeInHierarchy, "P4 remains active");

        // Place the two real P4 worksites through the existing placement authority.
        foreach (string owner in Ids)
        {
            string id = FirstProductionController.WorksiteId(owner);
            Vector2Int cell = FindValid(s, id);
            Check(s.Placement.TryPlace(id, cell, 0).Succeeded, "worksite placed " + owner);
            await Until(() => p.Sites.ContainsKey(owner), "worksite attached " + owner, 10f);
        }
        Check(p.Sites.Count == 2, "two worksite bindings only");

        int moneyBeforeSupport = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;

        // Isolate producer timing: mark only the player-activity prerequisite as complete.
        // No elapsed time, pending stock, ready state, or claim state is injected.
        var planned = new Dictionary<string, int>();
        float maxInterval = 0f;
        foreach (string owner in Ids)
        {
            var site = p.Sites[owner];
            var record = p.CaptureState().worksites.First(r => r.companionId == owner);
            record.playerActivityCompleted = true;
            record.producer = new StarterProducerSaveData();
            site.Restore(record);

            site.Producer.StartStarterWork();
            int pending = PendingAmount(site.Producer);
            float interval = EffectiveInterval(site.Producer);
            planned[owner] = Mathf.Min(pending, site.Producer.productionData.maxInventoryCount);
            maxInterval = Mathf.Max(maxInterval, interval);

            Check(site.PlayerActivityCompleted, "activity prerequisite fixture " + owner);
            Check(pending > 0, "real producer pending amount initialized " + owner);
            Check(site.Producer.StarterWorking || site.Producer.StarterBatchReady,
                "real producer session started " + owner);
            Evidence($"{owner}: effectiveInterval={interval:F3}s pending={pending} expectedStock={planned[owner]}");
        }

        // Prove the live FSM reaches Working and its real timer advances.
        await Until(() => p.Sites.Values.All(site =>
                site.Producer.StarterBatchReady || site.Producer.GetFsmState() == "Working"),
            "real producer FSM reached Working", 20f);

        var elapsedBefore = p.Sites.ToDictionary(kv => kv.Key,
            kv => kv.Value.Producer.CaptureStarterState().elapsed);
        await Task.Delay(400);
        foreach (string owner in Ids)
        {
            var producer = p.Sites[owner].Producer;
            float elapsedAfter = producer.CaptureStarterState().elapsed;
            Check(producer.StarterBatchReady || elapsedAfter > elapsedBefore[owner],
                "real production timer advanced " + owner);
        }

        float productionTimeout = Mathf.Clamp(maxInterval + 25f, 30f, 150f);
        await Until(() => p.Sites.Values.All(site => site.Producer.StarterBatchReady),
            "real producer timers created first batches", productionTimeout);

        foreach (string owner in Ids)
        {
            var site = p.Sites[owner];
            Check(site.Producer.StarterStockCount == planned[owner],
                "natural first-batch stock matches planned amount " + owner);
            Check(!site.Producer.StarterClaimed, "natural first batch remains unclaimed " + owner);
            Evidence($"{owner}: naturalStock={site.Producer.StarterStockCount}");
        }

        // Full inventory: failed claim must preserve stock and eligibility, including save/load.
        string blockedOwner = Ids[0];
        var blockedSite = p.Sites[blockedOwner];
        var inv = Inventory.instance;
        Check(inv != null && inv.slots != null && inv.slots.Count > 1, "player inventory available");
        FillGeneralInventory(inv, blockedSite.Item);
        Check(inv.slots.All(slot => slot != null && !slot.IsEmpty), "general inventory filled");

        int blockedStock = blockedSite.Producer.StarterStockCount;
        int blockedCountBefore = inv.CountItems(blockedSite.Item);
        int moneyBeforeBlockedClaim = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;

        Check(!blockedSite.Producer.TryClaimStarterBatch(inv), "full inventory rejects support claim");
        Check(blockedSite.Producer.StarterBatchReady &&
              !blockedSite.Producer.StarterClaimed &&
              blockedSite.Producer.StarterStockCount == blockedStock,
            "failed claim preserves stock and eligibility");
        Check(inv.CountItems(blockedSite.Item) == blockedCountBefore,
            "failed claim transfers no partial stock");
        Check(EconomyService.Instance == null || EconomyService.Instance.Money == moneyBeforeBlockedClaim,
            "failed support claim changes no money");

        await save.SaveGameAsync();
        await save.LoadGameAsync();
        await Until(() => p.Sites.Count == 2 &&
                          p.Sites.Values.All(site => site.Producer.StarterBatchReady),
            "ready support state restored after full-inventory save/load", 15f);

        inv = Inventory.instance;
        blockedSite = p.Sites[blockedOwner];
        Check(inv.slots.All(slot => slot != null && !slot.IsEmpty),
            "full inventory restored");
        Check(blockedSite.Producer.StarterStockCount == blockedStock &&
              !blockedSite.Producer.StarterClaimed,
            "save/load preserves blocked stock and eligibility");
        Check(!blockedSite.Producer.TryClaimStarterBatch(inv),
            "restored full inventory still rejects claim");
        Check(blockedSite.Producer.StarterStockCount == blockedStock,
            "second failed claim still preserves stock");

        // Free exactly one general slot, then the preserved stock must transfer once.
        FreeSlot(inv, 0);
        int beforeSuccessfulClaim = inv.CountItems(blockedSite.Item);
        Check(blockedSite.Producer.TryClaimStarterBatch(inv),
            "claim succeeds after capacity is freed");
        Check(blockedSite.Producer.StarterClaimed &&
              blockedSite.Producer.StarterStockCount == 0 &&
              inv.CountItems(blockedSite.Item) == beforeSuccessfulClaim + blockedStock,
            "preserved stock transfers exactly once");
        Check(!blockedSite.Producer.TryClaimStarterBatch(inv),
            "immediate repeat claim rejected");

        // Claim the second naturally-produced batch once as well.
        string secondOwner = Ids[1];
        var secondSite = p.Sites[secondOwner];
        int secondStock = secondSite.Producer.StarterStockCount;
        FreeSlot(inv, 1);
        int secondBefore = inv.CountItems(secondSite.Item);
        Check(secondSite.Producer.TryClaimStarterBatch(inv),
            "second support claim succeeds once");
        Check(secondSite.Producer.StarterClaimed &&
              secondSite.Producer.StarterStockCount == 0 &&
              inv.CountItems(secondSite.Item) == secondBefore + secondStock,
            "second stock transfers exactly once");
        Check(!secondSite.Producer.TryClaimStarterBatch(inv),
            "second immediate repeat claim rejected");

        await Until(() => p.Complete, "P4 complete after both real support claims", 5f);
        Check(EconomyService.Instance == null || EconomyService.Instance.Money == moneyBeforeSupport,
            "SettlementSupport path remains zero-G");

        // Save completed real state, load a completely fresh departure scene in the SAME Play session,
        // then restore through the normal ResumeAsync/SaveManager path.
        await save.SaveGameAsync();
        string expectedP4 = JsonUtility.ToJson(p.CaptureState());
        var expectedCounts = p.Sites.Values.ToDictionary(site => site.Owner,
            site => Inventory.instance.CountItems(site.Item));
        int expectedMoney = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;

        await FreshScene();
        p = Object.FindFirstObjectByType<FirstProductionController>();
        s = p.Settlement;
        save = s.EnsureSaveManager();
        ConfigureRepository(save);
        await s.ResumeAsync();

        await Until(() => p.IsReady && p.Sites.Count == 2, "fresh-scene P4 restored", 20f);
        Check(p.Complete, "fresh-scene completed P4 retained");
        Check(JsonUtility.ToJson(p.CaptureState()) == expectedP4,
            "fresh-scene exact P4 ownership/production/claim state");
        Check(s.Placement.RegisteredCount == 5 &&
              s.Voyage.Companions.Count == 2,
            "fresh-scene has no duplicate structures or companions");

        foreach (var site in p.Sites.Values)
        {
            Check(Inventory.instance.CountItems(site.Item) == expectedCounts[site.Owner],
                "fresh-scene inventory count retained " + site.Owner);
            Check(!site.Producer.TryClaimStarterBatch(Inventory.instance),
                "fresh-scene duplicate support rejected " + site.Owner);
        }
        Check(EconomyService.Instance == null || EconomyService.Instance.Money == expectedMoney,
            "fresh-scene money retained");
        Check(!_runtimeError, "runtime errors zero");

        Evidence($"PASS checks={_checks}; one Play session; one fresh scene load inside Play; producer stock was not injected.");
    }

    static Vector2Int FindValid(FirstIslandSettlementController s, string id)
    {
        var grid = s.Voyage.IslandGrid.Definition;
        foreach (var cell in Enumerable.Range(0, grid.Height)
                     .SelectMany(z => Enumerable.Range(0, grid.Width)
                         .Select(x => new Vector2Int(x, z))))
            if (s.Placement.Evaluate(id, cell, 0).Succeeded)
                return cell;

        throw new InvalidOperationException("No valid worksite cell for " + id);
    }

    static void FillGeneralInventory(Inventory inv, Item filler)
    {
        if (filler == null) throw new InvalidOperationException("Missing filler item.");
        int count = Mathf.Max(1, filler.maxStack);
        foreach (var slot in inv.slots)
        {
            if (slot == null) throw new InvalidOperationException("Null inventory slot.");
            slot.Clear();
            slot.Set(filler, count);
        }
        inv.RefreshAllUI();
    }

    static void FreeSlot(Inventory inv, int index)
    {
        if (inv == null || inv.slots == null || index < 0 || index >= inv.slots.Count)
            throw new InvalidOperationException("Cannot free requested inventory slot " + index);
        inv.slots[index].Clear();
        inv.RefreshAllUI();
    }

    static int PendingAmount(ProducerNpcController producer)
    {
        if (PendingAmountField == null)
            throw new MissingFieldException(nameof(ProducerNpcController), "_pendingProductionAmount");
        return (int)PendingAmountField.GetValue(producer);
    }

    static float EffectiveInterval(ProducerNpcController producer)
    {
        if (EffectiveIntervalMethod == null)
            throw new MissingMethodException(nameof(ProducerNpcController), "CalculateEffectiveInterval");
        return (float)EffectiveIntervalMethod.Invoke(producer, null);
    }

    static async Task FreshScene()
    {
        var operation = EditorSceneManager.LoadSceneAsyncInPlayMode(
            ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        while (!operation.isDone) await Task.Delay(50);

        await Until(() =>
        {
            var p = Object.FindFirstObjectByType<FirstProductionController>();
            return p != null &&
                   p.Settlement != null &&
                   p.Settlement.Selection != null &&
                   p.Settlement.Selection.Tutorial != null &&
                   p.Settlement.Selection.Tutorial.IsReady;
        }, "fresh departure scene initialized", 20f);
    }

    static void ConfigureRepository(SaveManager save)
    {
        var method = typeof(SaveManager).GetMethod(
            "SetRepositoryForValidation", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
            throw new MissingMethodException(nameof(SaveManager), "SetRepositoryForValidation");
        method.Invoke(save, new object[]
        {
            new LocalJsonSaveRepository(Path.GetFullPath(Path.Combine(Output, "Save")))
        });
    }

    static async Task Until(Func<bool> predicate, string label, float seconds)
    {
        double end = EditorApplication.timeSinceStartup + seconds;
        while (!predicate() && EditorApplication.timeSinceStartup < end)
            await Task.Delay(50);
        Check(predicate(), label);
    }

    static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        _checks++;
        Debug.Log("[P4-R5] PASS " + label);
    }

    static void Evidence(string text)
    {
        Directory.CreateDirectory(Output);
        File.AppendAllText(Path.Combine(Output, "evidence.txt"), text + "\n");
    }

    static void Finish(string error)
    {
        bool failed = error != null;
        Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, "result.txt"),
            failed
                ? "FAIL\n" + error + $"\nchecks={_checks}\n"
                : $"PASS\nchecks={_checks}\nPlay sessions=1\nFresh-scene loads inside Play=1\nRuntime errors=0\n");
        SessionState.SetBool(Key + ".Fail", failed);
        SessionState.SetBool(Key + ".Done", true);
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }
}
