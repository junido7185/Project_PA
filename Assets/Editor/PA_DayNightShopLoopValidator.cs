#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PA_DayNightShopLoopValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.DayNightShopLoop.Active";
    const string EnteredKey = "PA.DayNightShopLoop.Entered";
    const string RanKey = "PA.DayNightShopLoop.Ran";
    const string HadErrorKey = "PA.DayNightShopLoop.HadError";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;

    static PA_DayNightShopLoopValidator()
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

    [MenuItem("Project PA/Validation/Run Day Night Shop Loop Validation")]
    public static void RunDayNightShopLoopValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA DayNight: failed to open scene at {ScenePath}");
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

        Debug.Log("PA DayNight: entering Play Mode.");
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
                Debug.LogError("PA DayNight: timed out before entering Play Mode.");
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
            Debug.LogError("PA DayNight: timed out before runtime checks.");
            MarkFailedAndExit();
        }
    }

    static void RunRuntimeChecks()
    {
        try
        {
            Time.timeScale = 1f;

            Require(GameClock.Instance != null, "GameClock exists");
            Require(Inventory.instance != null, "Inventory exists");

            var controller = RequireOne<DayNightShopLoopController>("DayNightShopLoopController");
            Require(controller.gameObject.scene.IsValid(), "day/night controller is bound to the loaded scene");
            DaytimeStockPrepPoint[] prepPoints = Object.FindObjectsByType<DaytimeStockPrepPoint>(FindObjectsSortMode.None);
            Require(prepPoints.Length >= 2, "two runtime day prep stock points exist");

            ClearRuntimeInventory();
            controller.ResetDayPrepForValidation();

            controller.SimulatePhaseForValidation(8f, 1);
            Require(controller.CurrentPhase == PADayNightPhase.DayPreparation, "Day 1 08:00 resolves to Day Prep");
            Require(controller.IsShopOpen, "Day 1 tutorial keeps shop access open");
            Require(controller.CanCollectDayPrepStock, "day prep stock can be collected in Day Prep");

            int before = CountSellableItems();
            Require(controller.TryCollectDayPrepStock(prepPoints[0], null), "first day prep stock interaction succeeds");
            int after = CountSellableItems();
            Require(after > before, "first day prep stock increases sellable inventory");
            // Visual Demo Integration Pass — HUD 문구 한국어화에 맞춰 성공 키워드를 동기화한다.
            Require(controller.LastActivityResult.Contains("낮 준비 완료"), "day prep result explains prepared stock");

            int afterSecondTry = CountSellableItems();
            Require(!controller.TryCollectDayPrepStock(prepPoints[0], null), "same day prep point can only be collected once per day");
            Require(CountSellableItems() == afterSecondTry, "same point second try does not duplicate inventory");

            Require(controller.TryCollectDayPrepStock(prepPoints[1], null), "second day prep stock interaction succeeds");
            Require(CountSellableItems() > afterSecondTry, "second day prep stock increases sellable inventory");
            // IL-001: 분산 야생 채집 포인트가 추가되어 포인트가 2개보다 많을 수 있다.
            // 남은 모든 포인트를 채집한 뒤 소진 여부를 확인한다.
            for (int i = 2; i < prepPoints.Length; i++)
                controller.TryCollectDayPrepStock(prepPoints[i], null);
            Require(!controller.CanCollectDayPrepStock, "all day prep stock points are exhausted after collecting every activity");

            controller.SimulatePhaseForValidation(8f, 2);
            Require(controller.CurrentPhase == PADayNightPhase.DayPreparation, "Day 2 08:00 resolves to Day Prep");
            Require(!controller.IsShopOpen, "Day 2 morning shop is closed");
            Require(controller.CanCollectDayPrepStock, "Day 2 day prep stock resets");

            controller.SimulatePhaseForValidation(19f, 2);
            Require(controller.CurrentPhase == PADayNightPhase.ShopOpen, "Day 2 19:00 resolves to Night Shop Open");
            Require(controller.IsShopOpen, "Night shop phase reports shop open");
            Require(!controller.CanCollectDayPrepStock, "stock prep is not available during night shop");

            controller.SimulatePhaseForValidation(23.2f, 2);
            Require(controller.CurrentPhase == PADayNightPhase.Settlement, "Day 2 23:12 resolves to Settlement");
            Require(!controller.IsShopOpen, "settlement phase reports shop closed");
            Require(controller.CanPlayerStartNextDay, "settlement enables player next-day action");

            var sign = RequireOne<ShopOpenSign>("ShopOpenSign");
            Require(sign.GetInteractPrompt().Contains("다음 날"), "settlement sign explains next-day action");
            sign.Interact(null);
            Require(GameClock.Instance.CurrentDay == 3, "settlement sign advances Day 2 to Day 3");
            Require(Mathf.Abs(GameClock.Instance.CurrentHour - controller.dayStartHour) < 0.01f,
                "Day 3 starts at configured morning hour");
            Require(controller.CurrentPhase == PADayNightPhase.DayPreparation,
                "Day 3 starts in day preparation phase");
            Require(controller.CanCollectDayPrepStock, "Day 3 preparation stock is available after rest");

            Require(HasPublicMember(typeof(DayNightShopLoopController), "IsShopOpen"), "shop open state is exposed");
            Require(HasPublicMember(typeof(DayNightShopLoopController), "CurrentPhase"), "day/night phase state is exposed");

            Debug.Log($"PA Day Night Shop Loop Validation passed. sellableInventory={CountSellableItems()}");
        }
        catch (Exception ex)
        {
            _hadError = true;
            SessionState.SetBool(HadErrorKey, true);
            Debug.LogError($"PA Day Night Shop Loop Validation failed: {ex.Message}\n{ex}");
        }
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

    static bool HasPublicMember(Type type, string name)
    {
        return type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) != null
            || type.GetField(name, BindingFlags.Public | BindingFlags.Instance) != null;
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

        Debug.Log($"PA DayNight Check OK: {message}");
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
            Debug.LogError("PA Day Night Shop Loop Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA Day Night Shop Loop Validation finished successfully.");
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
