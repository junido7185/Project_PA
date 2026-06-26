#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PA_LongPlayProgressionValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.LongPlayValidation.Active";
    const string EnteredKey = "PA.LongPlayValidation.Entered";
    const string RanKey = "PA.LongPlayValidation.Ran";
    const string HadErrorKey = "PA.LongPlayValidation.HadError";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;

    static PA_LongPlayProgressionValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;

        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _hadError = SessionState.GetBool(HadErrorKey, false);
        RegisterCallbacks();

        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Run Long Play Progression Validation")]
    public static void RunLongPlayProgressionValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA Long Play: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        _entered = false;
        _ran = false;
        _hadError = false;
        _startedAt = EditorApplication.timeSinceStartup;

        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(HadErrorKey, false);

        RegisterCallbacks();

        Debug.Log("PA Long Play: entering Play Mode.");
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
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            _hadError = true;
            SessionState.SetBool(HadErrorKey, true);
        }
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
        {
            Finish();
        }
    }

    static void OnEditorUpdate()
    {
        double elapsed = EditorApplication.timeSinceStartup - _startedAt;

        if (!_entered)
        {
            if (elapsed > 60.0)
            {
                Debug.LogError("PA Long Play: timed out before entering Play Mode.");
                MarkFailedAndExit();
            }
            return;
        }

        if (!_ran && EditorApplication.isPlaying && elapsed > 2.5)
        {
            _ran = true;
            SessionState.SetBool(RanKey, true);
            RunRuntimeChecks();
            EditorApplication.ExitPlaymode();
            return;
        }

        if (_entered && !_ran && elapsed > 90.0)
        {
            Debug.LogError("PA Long Play: timed out before runtime checks.");
            MarkFailedAndExit();
        }
    }

    static void RunRuntimeChecks()
    {
        try
        {
            Time.timeScale = 1f;

            Require(EconomyService.Instance != null, "EconomyService exists");
            Require(GameClock.Instance != null, "GameClock exists");
            Require(Inventory.instance != null, "Inventory exists");
            Require(SaveManager.instance != null, "SaveManager exists");

            var controller = RequireOne<LongPlayProgressionController>("LongPlayProgressionController");
            Require(controller.gameObject.scene.IsValid(), "long-play controller is bound to the loaded scene");
            ConfigureProjectLocalSaveRepository();

            ClearRuntimeInventory();
            EconomyService.Instance.ForceSet(5000, "LongPlayProgressionValidator");

            for (int day = 2; day <= 7; day++)
            {
                int before = CountSellableItems();
                bool result = controller.SimulateNewDayForValidation(day);
                int after = CountSellableItems();
                Require(result, $"Day {day} producer delivery simulation returns true");
                Require(after > before, $"Day {day} producer delivery increases sellable inventory");

                if (day == 3)
                    ValidateDay3SaveLoad(controller);
            }

            Require(controller.LastSupplyDay == 7, "last processed producer delivery day is Day 7");
            Require(GameClock.Instance.CurrentDay == 7, "GameClock reaches Day 7 during validation");
            Require(controller.CurrentGoalText.Contains("Long Play Day 7"), "long-play HUD shows Day 7 objective");
            Require(SaveDataHasLongPlayFields(), "SaveData has v7 long-play fields");

            Debug.Log($"PA Long Play Validation passed. sellableInventory={CountSellableItems()}, money={EconomyService.Instance.Money}G");
        }
        catch (Exception ex)
        {
            _hadError = true;
            SessionState.SetBool(HadErrorKey, true);
            Debug.LogError($"PA Long Play Validation failed: {ex.Message}\n{ex}");
        }
    }

    static void ConfigureProjectLocalSaveRepository()
    {
        string root = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "LongPlayValidationSaves");
        Directory.CreateDirectory(root);

        var field = typeof(SaveManager).GetField("_repository", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new MissingFieldException(nameof(SaveManager), "_repository");

        field.SetValue(SaveManager.instance, new LocalJsonSaveRepository(root));
        Debug.Log($"PA Long Play: using project-local validation save root {root}");
    }

    static void ValidateDay3SaveLoad(LongPlayProgressionController controller)
    {
        int expectedDay = GameClock.Instance.CurrentDay;
        int expectedMoney = EconomyService.Instance.Money;
        int expectedSupplyDay = controller.LastSupplyDay;
        int expectedSellableCount = CountSellableItems();
        long expectedDayStartRevenue = controller.DayStartRevenue;
        int expectedDayStartMoney = controller.DayStartMoney;

        SaveManager.instance.SaveGameAsync().GetAwaiter().GetResult();

        ClearRuntimeInventory();
        EconomyService.Instance.ForceSet(1, "LongPlayProgressionValidator.MutateBeforeLoad");
        GameClock.Instance.ForceSet(8f, 1, "LongPlayProgressionValidator.MutateBeforeLoad");
        controller.RestoreSavedSession(0, 0L, 0);

        SaveManager.instance.LoadGameAsync().GetAwaiter().GetResult();

        Require(GameClock.Instance.CurrentDay == expectedDay, "Day 3 save/load restores current day");
        Require(EconomyService.Instance.Money == expectedMoney, "Day 3 save/load restores money");
        Require(controller.LastSupplyDay == expectedSupplyDay, "Day 3 save/load restores long-play supply day");
        Require(controller.DayStartRevenue == expectedDayStartRevenue, "Day 3 save/load restores long-play day-start revenue");
        Require(controller.DayStartMoney == expectedDayStartMoney, "Day 3 save/load restores long-play day-start money");
        Require(CountSellableItems() >= expectedSellableCount, "Day 3 save/load restores delivered inventory");
        Require(controller.CurrentGoalText.Contains("Long Play Day 3"), "Day 3 save/load refreshes long-play HUD");
    }

    static void ClearRuntimeInventory()
    {
        foreach (var slot in Inventory.instance.slots)
            slot?.Clear();

        if (Inventory.instance.hotbar != null)
        {
            foreach (var slot in Inventory.instance.hotbar.slots)
                slot?.Clear();
        }

        Inventory.instance.RefreshAllUI();
    }

    static int CountSellableItems()
    {
        int count = 0;
        CountSellable(Inventory.instance.slots, ref count);

        if (Inventory.instance.hotbar != null)
            CountSellable(Inventory.instance.hotbar.slots, ref count);

        return count;
    }

    static void CountSellable(System.Collections.Generic.List<InventorySlot> slots, ref int count)
    {
        if (slots == null) return;

        foreach (var slot in slots)
        {
            if (slot == null || slot.IsEmpty || slot.item == null) continue;
            if (slot.item.category != ItemCategory.Tool && slot.item.toolType == ToolType.None)
                count += slot.count;
        }
    }

    static bool SaveDataHasLongPlayFields()
    {
        Type type = typeof(SaveData);
        return type.GetField("longPlayLastSupplyDay") != null
            && type.GetField("longPlayDayStartRevenue") != null
            && type.GetField("longPlayDayStartMoney") != null;
    }

    static T RequireOne<T>(string label) where T : Object
    {
        foreach (var obj in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            if (obj != null) return obj;

        throw new InvalidOperationException($"{label} not found.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);

        Debug.Log($"PA Long Play Check OK: {message}");
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

        if (!entered || !ran || hadError)
        {
            Debug.LogError("PA Long Play Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA Long Play Validation finished successfully.");
        EditorApplication.Exit(0);
    }

    static void Cleanup()
    {
        Application.logMessageReceived -= OnLogMessage;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
    }
}
#endif
