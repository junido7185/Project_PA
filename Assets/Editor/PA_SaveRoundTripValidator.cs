#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Task 011 — 실제 SaveManager + LocalJsonSaveRepository 왕복 검증.
// 사용자 persistentDataPath 대신 Logs/SaveRoundTrip/<timestamp>를 주입해 실제 savegame을 보호한다.
[InitializeOnLoad]
public static class PA_SaveRoundTripValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.SaveRoundTrip.Active";
    const string EnteredKey = "PA.SaveRoundTrip.Entered";
    const string RanKey = "PA.SaveRoundTrip.Ran";
    const string HadErrorKey = "PA.SaveRoundTrip.HadError";
    const string OutputKey = "PA.SaveRoundTrip.Output";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;
    static Task _runtimeTask;

    static PA_SaveRoundTripValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;

        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _hadError = SessionState.GetBool(HadErrorKey, false);
        RegisterCallbacks();

        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Run Save Round Trip Validation")]
    public static void RunSaveRoundTripValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA SaveRoundTrip: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "SaveRoundTrip", stamp));
        Directory.CreateDirectory(output);

        _entered = false;
        _ran = false;
        _hadError = false;
        _runtimeTask = null;
        _startedAt = EditorApplication.timeSinceStartup;

        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(HadErrorKey, false);
        SessionState.SetString(OutputKey, output);

        RegisterCallbacks();
        Debug.Log($"PA SaveRoundTrip: entering Play Mode. Output={output}");
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
            && SessionState.GetBool(RanKey, false))
            Finish();
    }

    static void OnEditorUpdate()
    {
        double elapsed = EditorApplication.timeSinceStartup - _startedAt;
        if (!_entered)
        {
            if (elapsed > 60.0)
            {
                Debug.LogError("PA SaveRoundTrip: timed out before entering Play Mode.");
                MarkFailedAndExit();
            }
            return;
        }

        if (!_ran && EditorApplication.isPlaying && _runtimeTask == null && elapsed > 3.0)
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
                Debug.LogError($"PA SaveRoundTrip Validation failed: {_runtimeTask.Exception?.GetBaseException()}");
            }

            _ran = true;
            SessionState.SetBool(RanKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }

        if (!_ran && elapsed > 100.0)
        {
            Debug.LogError("PA SaveRoundTrip: timed out before runtime checks completed.");
            MarkFailedAndExit();
        }
    }

    static async Task RunRuntimeChecksAsync()
    {
        Time.timeScale = 1f;

        var save = RequireOne<SaveManager>("SaveManager");
        Require(EconomyService.Instance != null, "EconomyService exists");
        Require(GameClock.Instance != null, "GameClock exists");
        Require(Inventory.instance != null && Inventory.instance.hotbar != null, "Inventory and Hotbar exist");
        var loop = RequireOne<DayNightShopLoopController>("DayNightShopLoopController");

        string output = SessionState.GetString(OutputKey, string.Empty);
        Require(!string.IsNullOrWhiteSpace(output), "isolated output path exists");
        Directory.CreateDirectory(output);

        var repositoryField = typeof(SaveManager).GetField("_repository", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(repositoryField != null, "SaveManager repository field is available for isolated validation");
        repositoryField.SetValue(save, new LocalJsonSaveRepository(output));

        ClearSlots(Inventory.instance.slots);
        ClearSlots(Inventory.instance.hotbar.slots);
        foreach (var shopSlot in Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
        {
            shopSlot.currentItem = null;
            shopSlot.displayPrice = 0;
            shopSlot.RefreshDisplay();
        }

        var bread = Resources.Load<Item>("Items/Item_BreadLoaf");
        var carrot = Resources.Load<Item>("Items/Item_Carrot");
        Require(bread != null && carrot != null, "round-trip item assets exist");

        Inventory.instance.slots[0].SetInstance(new ItemInstance(bread, 4)
        {
            quality = 0.75f,
            currentPrice = 44
        });
        Inventory.instance.hotbar.GetSlot(0).SetInstance(new ItemInstance(carrot, 3)
        {
            quality = 0.8f,
            currentPrice = 13
        });

        var targetShopSlot = RequireOne<ShopSlot>("ShopSlot");
        targetShopSlot.currentItem = new ItemInstance(bread, 2)
        {
            quality = 0.9f,
            currentPrice = 44
        };
        targetShopSlot.displayPrice = 77;
        targetShopSlot.RefreshDisplay();

        EconomyService.Instance.ForceSet(1234, "PA_SaveRoundTrip seed");
        EconomyService.Instance.ForceSetCumulativeRevenue(5678, "PA_SaveRoundTrip seed");
        GameClock.Instance.ForceSet(9.5f, 2, "PA_SaveRoundTrip seed");
        loop.RestoreSavedState(2, new List<string> { "shore-forage" });

        var scenario = Object.FindFirstObjectByType<PlayableDayScenarioController>();
        if (scenario != null)
            scenario.RestoreSavedSession("저장검증", "green_bay", 2);

        // Task 057 — 마을 변화 상태 시드: Day 2에 Processed 판매(다음날 변화 대기) 상태.
        var villageCulture = VillageCultureVisualController.Instance
            ?? Object.FindFirstObjectByType<VillageCultureVisualController>();
        Require(villageCulture != null, "VillageCultureVisualController exists");
        villageCulture.RestoreSavedState(true, 2, "Processed", false, "", false);

        var player = GameObject.FindGameObjectWithTag("Player");
        Vector3 savedPlayerPosition = player != null ? player.transform.position : Vector3.zero;

        await save.SaveGameAsync();

        string saveFile = Path.Combine(output, "savegame.json");
        Require(File.Exists(saveFile), "isolated savegame.json was written");
        var savedData = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveFile));
        Require(savedData != null && savedData.version == SaveManager.CurrentSaveVersion,
            $"saved JSON uses gameplay schema v{SaveManager.CurrentSaveVersion}");
        Require(savedData.worldState != null &&
                savedData.worldState.worldMode == WorldPersistenceMigration.LegacyFixedMode &&
                savedData.worldState.modifiedCells.Count == 0 &&
                savedData.worldState.placedBuildings.Count == 0,
            "Golden Regression save remains LegacyFixed without automatic world conversion");
        Require(savedData.villageCultureHasPendingChange
            && savedData.villageCulturePendingSaleDay == 2
            && savedData.villageCulturePendingCategory == "Processed",
            "saved JSON contains pending village change state");
        Require(savedData.money == 1234 && savedData.cumulativeRevenue == 5678, "saved JSON contains economy state");
        Require(savedData.shopSlots.Exists(x => x != null && x.occupied && x.displayPrice == 77 && x.count == 2),
            "saved JSON contains stocked ShopSlot quantity and price");
        Require(savedData.dayPrepCollectedDay == 2
            && savedData.dayPrepCollectedActivities.Contains("shore-forage"),
            "saved JSON contains Day Prep completion state");

        EconomyService.Instance.ForceSet(1, "PA_SaveRoundTrip mutate");
        EconomyService.Instance.ForceSetCumulativeRevenue(2, "PA_SaveRoundTrip mutate");
        GameClock.Instance.ForceSet(18f, 5, "PA_SaveRoundTrip mutate");
        ClearSlots(Inventory.instance.slots);
        ClearSlots(Inventory.instance.hotbar.slots);
        targetShopSlot.currentItem = null;
        targetShopSlot.displayPrice = 0;
        targetShopSlot.RefreshDisplay();
        loop.RestoreSavedState(5, new List<string>());
        villageCulture.ResetForValidation(); // 변조: 대기 변화 소거 → 로드가 되살려야 함
        if (scenario != null)
            scenario.RestoreSavedSession("변조상태", "green_bay", 0);
        if (player != null) player.transform.position = savedPlayerPosition + Vector3.right * 2f;

        await save.LoadGameAsync();

        Require(EconomyService.Instance.Money == 1234, "money restores after repository round trip");
        Require(EconomyService.Instance.CumulativeRevenue == 5678, "cumulative revenue restores after repository round trip");
        Require(GameClock.Instance.CurrentDay == 2 && Mathf.Abs(GameClock.Instance.CurrentHour - 9.5f) < 0.01f,
            "day and hour restore after repository round trip");
        RequireSlot(Inventory.instance.slots[0], bread, 4, 0.75f, 44, "inventory slot");
        RequireSlot(Inventory.instance.hotbar.GetSlot(0), carrot, 3, 0.8f, 13, "hotbar slot");

        bool restoredShop = false;
        foreach (var shopSlot in Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
        {
            if (shopSlot == null || shopSlot.IsEmpty || shopSlot.currentItem.data != bread) continue;
            if (shopSlot.currentItem.count == 2 && shopSlot.displayPrice == 77)
            {
                restoredShop = true;
                break;
            }
        }
        Require(restoredShop, "ShopSlot item, quantity, and display price restore");

        var dayPrepProbe = new SaveData();
        loop.WriteSaveFields(dayPrepProbe);
        Require(dayPrepProbe.dayPrepCollectedDay == 2
            && dayPrepProbe.dayPrepCollectedActivities.Contains("shore-forage"),
            "Day Prep completion state restores");

        if (scenario != null)
        {
            Require(scenario.PlayerName == "저장검증", "Day 1 player name restores");
            Require(scenario.CurrentStageIndex == 2, "Day 1 scenario stage restores");
        }
        if (player != null)
            Require(Vector3.Distance(player.transform.position, savedPlayerPosition) < 0.01f, "player position restores");

        // Task 057 — 대기 마을 변화가 왕복 후 살아 있고, 다음날 아침 평가에서 실제로 활성화된다.
        Require(villageCulture.HasPendingChange && villageCulture.PendingSaleDay == 2,
            "pending village change restores after repository round trip");
        villageCulture.EvaluateForDayPreparation(3, true);
        Require(villageCulture.HasActiveCategory && villageCulture.ActiveCategoryName == "Processed",
            "restored pending change activates on the next morning");
        Require(villageCulture.VisualActive, "village change visual is active after next-morning evaluation");

        // 활성 상태 자체도 왕복되는지 확인 (2차 저장/로드).
        await save.SaveGameAsync();
        var savedData2 = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveFile));
        Require(savedData2.villageCultureHasActiveChange && savedData2.villageCultureActiveCategory == "Processed",
            "saved JSON contains active village change state");
        villageCulture.ResetForValidation();
        await save.LoadGameAsync();
        Require(villageCulture.HasActiveCategory && villageCulture.VisualActive,
            "active village change restores after second round trip");

        Debug.Log($"PA Save Round Trip Validation passed. money=1234, revenue=5678, inventory=4, hotbar=3, shop=2@77G, village=Processed(pending→active), output={output}");
    }

    static void ClearSlots(List<InventorySlot> slots)
    {
        if (slots == null) return;
        foreach (var slot in slots) slot?.Clear();
        Inventory.instance?.RefreshAllUI();
    }

    static void RequireSlot(InventorySlot slot, Item item, int count, float quality, int currentPrice, string label)
    {
        Require(slot != null && !slot.IsEmpty, $"{label} restores as occupied");
        Require(slot.item == item && slot.count == count, $"{label} item and count restore");
        Require(Mathf.Abs(slot.instance.quality - quality) < 0.001f && slot.instance.currentPrice == currentPrice,
            $"{label} quality and current price restore");
    }

    static T RequireOne<T>(string label) where T : Object
    {
        foreach (var obj in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            if (obj != null) return obj;
        throw new InvalidOperationException($"{label} not found.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"PA SaveRoundTrip Check OK: {message}");
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
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(EnteredKey);
        SessionState.EraseBool(RanKey);
        SessionState.EraseBool(HadErrorKey);
        SessionState.EraseString(OutputKey);

        if (!entered || !ran || hadError)
        {
            Debug.LogError("PA Save Round Trip Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("PA Save Round Trip Validation finished successfully.");
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
