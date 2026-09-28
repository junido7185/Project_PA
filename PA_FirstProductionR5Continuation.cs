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

// P4-R5 continuation only.
// Requires Logs/P4-R5/Save/departure_settlement.json from the prior R5 run.
// Does not repeat real producer timing; it resumes at the full-inventory save/load boundary.
[InitializeOnLoad]
public static class PA_FirstProductionR5Continuation
{
    const string Key = "PA.P4R5.Continuation";
    const string Output = "Logs/P4-R5";
    const string ScenePath = "Assets/Scenes/PA_DepartureTutorial.unity";

    static Task _task;
    static double _deadline;
    static bool _runtimeError;
    static int _checks;
    static bool _previousRunInBackground;

    static string SaveRoot => Path.GetFullPath(Path.Combine(Output, "Save"));
    static string SaveFile => Path.Combine(SaveRoot, "departure_settlement.json");

    static PA_FirstProductionR5Continuation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Application.logMessageReceived += OnLog;
    }

    [MenuItem("Project PA/Validation/P4 R5 Continue From Saved Full Inventory")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        if (!File.Exists(SaveFile))
        {
            Debug.LogError("[P4-R5C] Preserved save not found: " + SaveFile);
            return;
        }

        File.WriteAllText(Path.Combine(Output, "continuation-evidence.txt"),
            "P4-R5 continuation: reuse preserved post-production/full-inventory save; production timing is not repeated.\n");

        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + ".Done", false);
        SessionState.SetBool(Key + ".Fail", false);
        _task = null;
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
            _deadline = EditorApplication.timeSinceStartup + 120.0;
            _task = null;
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
            else Debug.Log(code == 0
                ? "[P4-R5C] PASS"
                : "[P4-R5C] FAIL — see Logs/P4-R5/continuation-result.txt");
        }
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;

        _runtimeError = true;
        File.AppendAllText(Path.Combine(Output, "continuation-runtime-errors.txt"),
            type + ": " + message + "\n" + stack + "\n");
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) ||
            SessionState.GetBool(Key + ".Done", false) ||
            !EditorApplication.isPlaying) return;

        var p = Object.FindFirstObjectByType<FirstProductionController>();
        if (_task == null &&
            p != null &&
            p.Settlement != null &&
            p.Settlement.Selection != null &&
            p.Settlement.Selection.Tutorial != null &&
            p.Settlement.Selection.Tutorial.IsReady)
        {
            _task = Checks(p);
        }

        if (_task != null && _task.IsCompleted)
        {
            Finish(_task.IsFaulted
                ? _task.Exception.GetBaseException().ToString()
                : _runtimeError ? "Unity runtime error logged" : null);
        }
        else if (EditorApplication.timeSinceStartup > _deadline)
        {
            Finish("120 second continuation timeout");
        }
    }

    static async Task Checks(FirstProductionController p)
    {
        var s = p.Settlement;
        var save = s.EnsureSaveManager();
        ConfigureRepository(save);

        await s.ResumeAsync();
        await Until(() => p.IsReady && p.Sites.Count == 2,
            "preserved P4 ready state restored", 20f);

        Check(p.Sites.Values.All(site =>
                site.Producer.StarterBatchReady && !site.Producer.StarterClaimed),
            "both preserved natural batches remain ready and unclaimed");

        Check(ItemRegistry.Instance != null, "ItemRegistry available");
        foreach (var site in p.Sites.Values)
        {
            var resolved = ItemRegistry.Instance.Find(site.Item.id, site.Item.itemName);
            Check(resolved == site.Item, "canonical P4 item registered " + site.Item.itemName);
        }

        var inv = Inventory.instance;
        Check(inv != null && inv.slots != null && inv.slots.Count > 1,
            "player inventory available after load");
        Check(inv.slots.All(slot => slot != null && !slot.IsEmpty),
            "full inventory restored");

        int moneyBefore = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;

        string blockedOwner = p.Owners[0];
        var blockedSite = p.Sites[blockedOwner];
        int blockedStock = blockedSite.Producer.StarterStockCount;
        int blockedBefore = inv.CountItems(blockedSite.Item);

        Check(!blockedSite.Producer.TryClaimStarterBatch(inv),
            "restored full inventory rejects support claim");
        Check(blockedSite.Producer.StarterBatchReady &&
              !blockedSite.Producer.StarterClaimed &&
              blockedSite.Producer.StarterStockCount == blockedStock,
            "restored failed claim preserves stock and eligibility");
        Check(inv.CountItems(blockedSite.Item) == blockedBefore,
            "restored failed claim transfers nothing");
        Check(EconomyService.Instance == null || EconomyService.Instance.Money == moneyBefore,
            "restored failed claim changes no money");

        FreeSlot(inv, 0);
        int firstBefore = inv.CountItems(blockedSite.Item);
        Check(blockedSite.Producer.TryClaimStarterBatch(inv),
            "first claim succeeds after one slot freed");
        Check(blockedSite.Producer.StarterClaimed &&
              blockedSite.Producer.StarterStockCount == 0 &&
              inv.CountItems(blockedSite.Item) == firstBefore + blockedStock,
            "first preserved stock transfers exactly once");
        Check(!blockedSite.Producer.TryClaimStarterBatch(inv),
            "first duplicate claim rejected");

        string secondOwner = p.Owners[1];
        var secondSite = p.Sites[secondOwner];
        int secondStock = secondSite.Producer.StarterStockCount;
        FreeSlot(inv, 1);
        int secondBefore = inv.CountItems(secondSite.Item);
        Check(secondSite.Producer.TryClaimStarterBatch(inv),
            "second claim succeeds after capacity freed");
        Check(secondSite.Producer.StarterClaimed &&
              secondSite.Producer.StarterStockCount == 0 &&
              inv.CountItems(secondSite.Item) == secondBefore + secondStock,
            "second preserved stock transfers exactly once");
        Check(!secondSite.Producer.TryClaimStarterBatch(inv),
            "second duplicate claim rejected");

        await Until(() => p.Complete, "P4 complete after both claims", 5f);
        Check(EconomyService.Instance == null || EconomyService.Instance.Money == moneyBefore,
            "SettlementSupport remains zero-G");

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

        await Until(() => p.IsReady && p.Sites.Count == 2,
            "fresh-scene P4 restored", 20f);
        Check(p.Complete, "fresh-scene completed P4 retained");
        Check(JsonUtility.ToJson(p.CaptureState()) == expectedP4,
            "fresh-scene exact P4 ownership/production/claim state");
        Check(s.Placement.RegisteredCount == 5 && s.Voyage.Companions.Count == 2,
            "fresh-scene has no duplicate structures or companions");

        foreach (var site in p.Sites.Values)
        {
            Check(Inventory.instance.CountItems(site.Item) == expectedCounts[site.Owner],
                "fresh-scene inventory retained " + site.Owner);
            Check(!site.Producer.TryClaimStarterBatch(Inventory.instance),
                "fresh-scene duplicate support rejected " + site.Owner);
        }

        Check(EconomyService.Instance == null || EconomyService.Instance.Money == expectedMoney,
            "fresh-scene money retained");
        Check(!_runtimeError, "runtime errors zero");

        Evidence($"PASS checks={_checks}; reused existing post-production save; producer timing not rerun.");
    }

    static void FreeSlot(Inventory inv, int index)
    {
        if (inv == null || inv.slots == null || index < 0 || index >= inv.slots.Count)
            throw new InvalidOperationException("Cannot free inventory slot " + index);
        inv.slots[index].Clear();
        inv.RefreshAllUI();
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
        Directory.CreateDirectory(SaveRoot);
        var method = typeof(SaveManager).GetMethod(
            "SetRepositoryForValidation", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
            throw new MissingMethodException(nameof(SaveManager), "SetRepositoryForValidation");

        method.Invoke(save, new object[] { new LocalJsonSaveRepository(SaveRoot) });
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
        Debug.Log("[P4-R5C] PASS " + label);
    }

    static void Evidence(string text)
    {
        File.AppendAllText(Path.Combine(Output, "continuation-evidence.txt"),
            text + "\n");
    }

    static void Finish(string error)
    {
        bool failed = error != null;
        File.WriteAllText(Path.Combine(Output, "continuation-result.txt"),
            failed
                ? "FAIL\n" + error + $"\nchecks={_checks}\n"
                : $"PASS\nchecks={_checks}\nPlay sessions=1\nReused preserved production save=true\nFresh-scene loads inside Play=1\nRuntime errors=0\n");

        SessionState.SetBool(Key + ".Fail", failed);
        SessionState.SetBool(Key + ".Done", true);
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }
}
