#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PA_VillageChangeSignalValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.VillageSignalValidation.Active";
    const string EnteredKey = "PA.VillageSignalValidation.Entered";
    const string RanKey = "PA.VillageSignalValidation.Ran";
    const string HadErrorKey = "PA.VillageSignalValidation.HadError";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;

    static PA_VillageChangeSignalValidator()
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

    [MenuItem("Project PA/Validation/Run Village Change Signal Validation")]
    public static void RunVillageChangeSignalValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA VillageSignal: failed to open scene at {ScenePath}");
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

        Debug.Log("PA VillageSignal: entering Play Mode.");
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
                Debug.LogError("PA VillageSignal: timed out before entering Play Mode.");
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
            Debug.LogError("PA VillageSignal: timed out before runtime checks.");
            MarkFailedAndExit();
        }
    }

    static void RunRuntimeChecks()
    {
        try
        {
            Time.timeScale = 1f;

            Require(GameClock.Instance != null, "GameClock exists");
            var sales = RequireOne<SalesLogManager>("SalesLogManager");
            var signal = RequireOne<VillageChangeSignalController>("VillageChangeSignalController");

            GameClock.Instance.ForceSet(19f, 2, "VillageChangeSignalValidator");

            sales.RecordSale("BreadLoaf", ItemCategory.Processed.ToString(), 34, 1.0f, "ValidatorCustomerA", 2, 19);
            sales.RecordSale("IronBar", ItemCategory.Processed.ToString(), 42, 1.0f, "ValidatorCustomerB", 2, 20);
            sales.RecordSale("Wood", ItemCategory.Raw.ToString(), 8, 1.0f, "ValidatorCustomerC", 2, 20);
            sales.RecordSale("Fish", ItemCategory.Raw.ToString(), 18, 1.0f, "ValidatorFisherA", 2, 20);

            signal.RefreshNow();

            Require(signal.CurrentSignalText.Contains("Village Direction"), "village signal HUD title is visible");
            Require(signal.CurrentSignalText.Contains("Processed"), "village signal includes leading category");
            Require(signal.CurrentSignalText.Contains("food/workshop"), "village signal explains category impact");
            Require(signal.GetLeadingSignalSummary().Contains("2 sale"), "village signal summarizes category count");
            Require(signal.TryGetNamedTrendSnapshot(VillageChangeSignalController.FishingTrendId, out var fishing)
                    && fishing.transactions == 1 && fishing.revenue == 18 && fishing.score == 1018,
                "one Fish sale creates fishing 1018 from one transaction and 18G");
            Require(signal.GetLeadingSignalSummary().Contains("낚시 생활"),
                "settlement-facing village summary includes the named fishing trend");

            // 정확한 (category, itemName) 쌍이 아니면 이름이 비슷해도 명명 트렌드가 아니다.
            sales.RecordSale("Fish", ItemCategory.Processed.ToString(), 18, 1.0f, "ValidatorWrongPair", 2, 20);
            sales.RecordSale("Shop_Tent_Kit", ItemCategory.Utility.ToString(), 30, 1.0f, "ValidatorTent", 2, 20);
            Require(signal.TryGetNamedTrendSnapshot(VillageChangeSignalController.FishingTrendId, out fishing)
                    && fishing.transactions == 1 && fishing.revenue == 18,
                "wrong category and unregistered camping-like names are fail-closed");
            Require(!signal.TryGetNamedTrendSnapshot(VillageChangeSignalController.CampingTrendId, out _),
                "camping remains inactive without a registered sellable product");

            // 거래 수 동률이면 매출, 거래 수가 다르면 가격보다 거래 수가 먼저다.
            sales.RecordSale("목제 가구", ItemCategory.Luxury.ToString(), 185, 1.0f, "ValidatorCarpenter", 2, 21);
            Require(signal.GetLeadingNamedTrendSummary().Contains("가구 문화"),
                "equal transaction counts select the higher-revenue furniture trend");
            sales.RecordSale("Fish", ItemCategory.Raw.ToString(), 18, 1.0f, "ValidatorFisherB", 2, 22);
            Require(signal.TryGetNamedTrendSnapshot(VillageChangeSignalController.FishingTrendId, out fishing)
                    && fishing.transactions == 2 && fishing.revenue == 36 && fishing.score == 2036,
                "two Fish transactions score 2036 and are not inferred as one stack sale");
            Require(signal.GetLeadingNamedTrendSummary().Contains("낚시 생활"),
                "transaction count outranks revenue when selecting the leading named trend");

            Debug.Log($"PA Village Change Signal Validation passed. summary={signal.GetLeadingSignalSummary()}");
        }
        catch (Exception ex)
        {
            _hadError = true;
            SessionState.SetBool(HadErrorKey, true);
            Debug.LogError($"PA Village Change Signal Validation failed: {ex.Message}\n{ex}");
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

        Debug.Log($"PA VillageSignal Check OK: {message}");
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
            Debug.LogError("PA Village Change Signal Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA Village Change Signal Validation finished successfully.");
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
