#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PA_CustomerDemandInsightValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.CustomerDemandValidation.Active";
    const string EnteredKey = "PA.CustomerDemandValidation.Entered";
    const string RanKey = "PA.CustomerDemandValidation.Ran";
    const string HadErrorKey = "PA.CustomerDemandValidation.HadError";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;

    static PA_CustomerDemandInsightValidator()
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

    [MenuItem("Project PA/Validation/Run Customer Demand Insight Validation")]
    public static void RunCustomerDemandInsightValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA Demand: failed to open scene at {ScenePath}");
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

        Debug.Log("PA Demand: entering Play Mode.");
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
                Debug.LogError("PA Demand: timed out before entering Play Mode.");
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
            Debug.LogError("PA Demand: timed out before runtime checks.");
            MarkFailedAndExit();
        }
    }

    static void RunRuntimeChecks()
    {
        try
        {
            Time.timeScale = 1f;

            Require(GameClock.Instance != null, "GameClock exists");
            var insight = RequireOne<CustomerDemandInsightController>("CustomerDemandInsightController");
            var item = Resources.Load<Item>("Items/Item_BreadLoaf")
                ?? Resources.Load<Item>("Items/Item_Fish");
            Require(item != null, "validation item exists");

            GameClock.Instance.ForceSet(8f, 3, "CustomerDemandInsightValidator");
            var sales = RequireOne<SalesLogManager>("SalesLogManager");

            insight.RecordEvaluation(new PurchaseEvaluator.Result
            {
                willBuy = true,
                probability = 0.72f,
                reason = "validator-buy"
            }, item, "ValidatorCustomerA", item.basePrice);

            insight.RecordEvaluation(new PurchaseEvaluator.Result
            {
                willBuy = false,
                probability = 0.28f,
                reason = "validator-pass"
            }, item, "ValidatorCustomerB", item.basePrice * 2);

            // 구매 건수는 구매 의사가 아니라 실제 결제 성공 경로인 RecordSale만 집계한다.
            sales.RecordSale(item.itemName, item.category.ToString(), item.basePrice,
                1f, "ValidatorCustomerA", 3, 19);

            insight.RefreshNow();

            Require(insight.CurrentInsightText.Contains("Demand Signals"), "demand insight HUD title is visible");
            Require(insight.CurrentInsightText.Contains(item.category.ToString()), "demand insight includes item category");
            Require(insight.CurrentInsightText.Contains("Latest:"), "demand insight includes latest signal");
            Require(insight.GetTopCategorySummary().Contains("1/2 bought"), "demand insight summarizes buy rate");

            var dailyStats = sales.GetDailyDecisionStats(3);
            Require(dailyStats.purchases == 1, "daily statistics count one completed purchase");
            Require(dailyStats.rejections == 1, "daily statistics count one rejected evaluation");
            Require(dailyStats.purchaseRatePercent == 50, "daily statistics calculate 50 percent purchase rate");

            var loop = RequireOne<DayNightShopLoopController>("DayNightShopLoopController");
            loop.SimulatePhaseForValidation(23.2f, 3);
            Require(loop.activityText != null && loop.activityText.text.Contains("구매 1건"),
                "settlement HUD shows completed purchase count");
            Require(loop.activityText.text.Contains("보류 1건"),
                "settlement HUD shows rejection count");
            Require(sales.BuildNextDayAdvice(3).Contains("가격"),
                "next-day advice reacts to a high rejection share");

            Debug.Log($"PA Customer Demand Insight Validation passed. item={item.itemName}, category={item.category}, daily=1 buy/1 reject");
        }
        catch (Exception ex)
        {
            _hadError = true;
            SessionState.SetBool(HadErrorKey, true);
            Debug.LogError($"PA Customer Demand Insight Validation failed: {ex.Message}\n{ex}");
        }
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

        Debug.Log($"PA Demand Check OK: {message}");
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
            Debug.LogError("PA Customer Demand Insight Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA Customer Demand Insight Validation finished successfully.");
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
