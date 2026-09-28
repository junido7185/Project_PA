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

// 완료 저장만 복원한다. 생산/저장 호출 없이 Play 1회, 실제 재로드 1회로 제한한다.
[InitializeOnLoad]
public static class PA_FirstProductionR5AcceptanceMicroCheck
{
    const string Key = "PA.P4R5.AcceptanceMicro";
    const string Output = "Logs/P4-R5/AcceptanceMicro-20260915";
    const string ScenePath = "Assets/Scenes/PA_DepartureTutorial.unity";
    static string SaveRoot => Path.GetFullPath("Logs/P4-R5/Save");
    static string SaveFile => Path.Combine(SaveRoot, "departure_settlement.json");
    static Task _task;
    static double _deadline;
    static int _checks;
    static bool _finishing;

    [Serializable]
    sealed class CompletedSaveProbe
    {
        public FirstProductionSaveData firstProduction;
    }

    // PA_RuntimeSceneBinder.EnsureCoreServices/EnsureUiServices 및 전역 Find 가드에서만 도출.
    // NPC별 훅, 플레이어별 장비/발 IK, 패널별 앱은 전역 서비스 목록에 포함하지 않는다.
    static readonly Type[] BinderTypes =
    {
        typeof(EconomyService), typeof(TierService), typeof(GameClock), typeof(GridService),
        typeof(PlayerInputHandler), typeof(AuditService), typeof(FriendshipService),
        typeof(BuildingRegistry), typeof(SalesLogManager), typeof(DayNightShopLoopController),
        typeof(LongPlayProgressionController), typeof(ProcessingOpportunityController),
        typeof(CustomerDemandInsightController), typeof(VillageChangeSignalController),
        typeof(MerchandisingCornerController), typeof(CustomerPreferencePresentationController),
        typeof(PurchaseFeedbackPresentationController), typeof(CustomerArrivalController),
        typeof(InteriorCustomerController), typeof(CoreSlicePresentationMode),
        typeof(DemoVisualDressingController), typeof(SaveManager), typeof(GameManager),
        typeof(AudioManager), typeof(ScreenFader), typeof(ItemRegistry), typeof(HiringService),
        typeof(BuildManager), typeof(DayNightVisual), typeof(InteractPromptUI), typeof(DialogueUI),
        typeof(FriendshipUI), typeof(ClockHUD), typeof(CraftingUI), typeof(StorageUI),
        typeof(PauseManager), typeof(MoneyHUD), typeof(ShopPriceUI), typeof(SmartphoneUI),
        typeof(UnityEngine.EventSystems.EventSystem)
    };

    static PA_FirstProductionR5AcceptanceMicroCheck()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Application.logMessageReceived += OnLog;
    }

    [MenuItem("Project PA/Validation/P4 R5 Final Acceptance Micro Check")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Micro-check must start in Edit mode.");
        var saved = JsonUtility.FromJson<CompletedSaveProbe>(File.ReadAllText(SaveFile));
        if (saved.firstProduction == null || !saved.firstProduction.completed)
            throw new InvalidOperationException("Existing completed P4 save is required.");
        Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, "evidence.txt"),
            "P4-R5 final acceptance; completed save read only; no production waiting.\n" +
            "Binder Departure branch bootstraps ItemRegistry only. Other Binder types absent on initial restore are N/A, expected to remain absent.\n");
        SessionState.SetString(Key + ".Input", Convert.ToBase64String(File.ReadAllBytes(SaveFile)));
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + ".Done", false);
        SessionState.SetString(Key + ".Failure", "");
        SessionState.SetInt(Key + ".Errors", 0);
        SessionState.SetInt(Key + ".Plays", 0);
        SessionState.SetInt(Key + ".Reloads", 0);
        SessionState.SetBool(Key + ".Background", Application.runInBackground);
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.EnterPlaymode();
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (!SessionState.GetBool(Key, false) ||
            (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
        SessionState.SetInt(Key + ".Errors", SessionState.GetInt(Key + ".Errors", 0) + 1);
        File.AppendAllText(Path.Combine(Output, "runtime-errors.txt"), type + ": " + message + "\n" + stack + "\n");
    }

    static void OnPlayModeChanged(PlayModeStateChange mode)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (mode == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetInt(Key + ".Plays", SessionState.GetInt(Key + ".Plays", 0) + 1);
            _task = null;
            _checks = 0;
            _finishing = false;
            _deadline = EditorApplication.timeSinceStartup + 90;
            Application.runInBackground = true;
        }
        else if (mode == PlayModeStateChange.EnteredEditMode)
        {
            Application.runInBackground = SessionState.GetBool(Key + ".Background", false);
            string failure = SessionState.GetString(Key + ".Failure", "");
            if (!SessionState.GetBool(Key + ".Done", false)) failure += "Unexpected Play exit.\n";
            if (SessionState.GetInt(Key + ".Errors", 0) != 0) failure += "Runtime errors observed.\n";
            if (Convert.ToBase64String(File.ReadAllBytes(SaveFile)) != SessionState.GetString(Key + ".Input", ""))
                failure += "Completed input save bytes changed.\n";
            bool passed = failure.Length == 0;
            File.WriteAllText(Path.Combine(Output, "result.txt"),
                (passed ? "P4-R5 PASS\n" : "P4-R5 BLOCKED\n" + failure) +
                "Checks=" + SessionState.GetInt(Key + ".Checks", 0) +
                "\nPlay sessions=" + SessionState.GetInt(Key + ".Plays", 0) +
                "\nFresh-scene reloads=" + SessionState.GetInt(Key + ".Reloads", 0) +
                "\nRuntime errors/exceptions/asserts=" + SessionState.GetInt(Key + ".Errors", 0) +
                "\nSave bytes unchanged=" + (Convert.ToBase64String(File.ReadAllBytes(SaveFile)) == SessionState.GetString(Key + ".Input", "")) + "\n");
            SessionState.SetBool(Key, false);
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        }
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || _finishing || !EditorApplication.isPlaying) return;
        if (_task == null && ReadyForResume()) _task = Checks();
        if (_task != null && _task.IsCompleted)
            Finish(_task.IsFaulted ? _task.Exception.GetBaseException().ToString() : "");
        else if (EditorApplication.timeSinceStartup > _deadline) Finish("Micro-check restore timeout.");
    }

    static bool ReadyForResume()
    {
        var p = Object.FindFirstObjectByType<FirstProductionController>();
        return p != null && p.Settlement != null && p.Settlement.Selection != null &&
            p.Settlement.Selection.Tutorial != null && p.Settlement.Selection.Tutorial.IsReady;
    }

    static async Task<FirstProductionController> Resume()
    {
        var p = Object.FindFirstObjectByType<FirstProductionController>();
        var save = p.Settlement.EnsureSaveManager();
        var configure = typeof(SaveManager).GetMethod("SetRepositoryForValidation", BindingFlags.Instance | BindingFlags.NonPublic);
        if (configure == null) throw new MissingMethodException("SetRepositoryForValidation");
        configure.Invoke(save, new object[] { new LocalJsonSaveRepository(SaveRoot) });
        await p.Settlement.ResumeAsync();
        await Until(() => p.IsReady && p.Sites.Count == 2, "P4 save restored");
        Check(p.Complete, "restored P4 already completed; no production waiting");
        return p;
    }

    static async Task Checks()
    {
        var p = await Resume();
        var before = BinderTypes.ToDictionary(t => t, t => Live(t).Length);
        string expectedP4 = JsonUtility.ToJson(p.CaptureState());
        int initialController = p.GetInstanceID();
        Check(SessionState.GetInt(Key + ".Reloads", 0) == 0, "one reload budget available");
        SessionState.SetInt(Key + ".Reloads", 1);
        var operation = EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        while (!operation.isDone) await Task.Delay(50);
        await Until(ReadyForResume, "fresh departure scene initialized");

        // 재로드 직후 ResumeAsync에 의존하지 않고 Registry를 직접 검사한다.
        CheckRegistry("immediately after reload, before ResumeAsync");
        p = await Resume();
        Check(p.GetInstanceID() != initialController, "fresh controller instance");
        CheckRegistry("after fresh ResumeAsync");
        foreach (var type in BinderTypes)
        {
            int count = Live(type).Length;
            Evidence("SERVICE " + type.Name + " initial=" + before[type] + " fresh=" + count);
            Check(before[type] <= 1, "initial singleton cardinality " + type.Name);
            if (type == typeof(ItemRegistry) || before[type] == 1)
                Check(count == 1, "fresh singleton count=1 " + type.Name);
            else Check(count == 0, "Departure N/A remains absent " + type.Name);
        }

        var bindings = Live(typeof(WorksiteBinding)).Cast<WorksiteBinding>().ToArray();
        Check(p.Owners.Length == 2 && p.Owners.Distinct().Count() == 2 && p.Sites.Count == 2,
            "two unique restored owners/sites");
        var producers = new HashSet<int>();
        Check(Inventory.instance != null, "inventory present for repeat-claim check");
        foreach (string owner in p.Owners)
        {
            var site = p.Sites[owner];
            var binding = site.Binding;
            Check(binding != null && binding.Assigned && binding.CompanionId == owner, "assigned owner binding " + owner);
            var ownerBindings = bindings.Where(b => b.CompanionId == owner).ToArray();
            Check(ownerBindings.Length == 1 && ownerBindings[0] == binding, "one live binding per owner " + owner);
            Check(bindings.Count(b => b.WorksiteInstanceId == binding.WorksiteInstanceId) == 1,
                "one live binding per worksite " + owner);
            var attached = binding.GetComponents<ProducerNpcController>();
            Check(attached.Length == 1 && attached[0] == binding.Producer && binding.Producer == site.Producer,
                "one bound producer per owner/site " + owner);
            Check(producers.Add(binding.Producer.GetInstanceID()), "unique producer reference " + owner);
            Evidence("OWNER " + owner + " bindings=1 producers=1 producerId=" + binding.Producer.GetInstanceID());
            Check(site.Producer.StarterClaimed, "support previously claimed " + owner);
            Check(!site.Producer.TryClaimStarterBatch(Inventory.instance), "duplicate SettlementSupport rejected " + owner);
        }
        Check(p.Complete && JsonUtility.ToJson(p.CaptureState()) == expectedP4, "completed P4 state preserved");
        Check(SessionState.GetInt(Key + ".Errors", 0) == 0, "runtime errors/exceptions/asserts=0");
        Check(SessionState.GetInt(Key + ".Plays", 0) == 1 && SessionState.GetInt(Key + ".Reloads", 0) == 1,
            "Play sessions=1 fresh reloads=1");
        Check(Convert.ToBase64String(File.ReadAllBytes(SaveFile)) == SessionState.GetString(Key + ".Input", ""),
            "completed save bytes unchanged");
    }

    static void CheckRegistry(string phase)
    {
        var registries = Live(typeof(ItemRegistry)).Cast<ItemRegistry>().ToArray();
        Check(registries.Length == 1, phase + ": ItemRegistry count=1");
        Check(ItemRegistry.Instance != null && ItemRegistry.Instance == registries[0], phase + ": Instance is sole live registry");
        var items = Resources.LoadAll<Item>("Items");
        var wood = items.Single(i => i.id == 3 && i.itemName == "Wood");
        var ore = items.Single(i => i.id == 4 && i.itemName == "Ore");
        Check(ItemRegistry.Instance.Find(3, "Wood") == wood, phase + ": Find(3,Wood) canonical");
        Check(ItemRegistry.Instance.Find(4, "Ore") == ore, phase + ": Find(4,Ore) canonical");
    }

    // 비활성 객체와 DontDestroyOnLoad 객체를 포함하며 자산/프리팹은 제외한다.
    static Component[] Live(Type type) => Resources.FindObjectsOfTypeAll(type).OfType<Component>()
        .Where(c => c != null && c.gameObject.scene.IsValid() && c.gameObject.scene.isLoaded).ToArray();

    static async Task Until(Func<bool> predicate, string label)
    {
        double end = EditorApplication.timeSinceStartup + 20;
        while (!predicate() && EditorApplication.timeSinceStartup < end) await Task.Delay(50);
        Check(predicate(), label);
    }

    static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        _checks++;
        Evidence("PASS " + label);
    }

    static void Evidence(string value) => File.AppendAllText(Path.Combine(Output, "evidence.txt"), value + "\n");

    static void Finish(string error)
    {
        _finishing = true;
        SessionState.SetString(Key + ".Failure", error);
        SessionState.SetInt(Key + ".Checks", _checks);
        SessionState.SetBool(Key + ".Done", true);
        EditorApplication.ExitPlaymode();
    }
}
