#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// SPY-002 검증 — 고객 성향 힌트 + 구매/거절 이유 프레젠테이션이 실제 데이터로 동작하는지 확인한다.
// 기존 구매/경제/NPC 로직은 건드리지 않으므로, 이 검증기는 읽기 전용 프레젠테이션만 점검한다.
[InitializeOnLoad]
public static class PA_CustomerPresentationValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.CustomerPresentation.Active";
    const string EnteredKey = "PA.CustomerPresentation.Entered";
    const string RanKey = "PA.CustomerPresentation.Ran";
    const string HadErrorKey = "PA.CustomerPresentation.HadError";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;

    static PA_CustomerPresentationValidator()
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

    [MenuItem("Project PA/Validation/Run Customer Presentation Validation")]
    public static void RunCustomerPresentationValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA CustomerPresentation: failed to open scene at {ScenePath}");
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

        Debug.Log("PA CustomerPresentation: entering Play Mode.");
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
                Debug.LogError("PA CustomerPresentation: timed out before entering Play Mode.");
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
            Debug.LogError("PA CustomerPresentation: timed out before runtime checks.");
            MarkFailedAndExit();
        }
    }

    static void RunRuntimeChecks()
    {
        try
        {
            Time.timeScale = 1f;

            // 1) 사이드카 컨트롤러가 런타임 바인더로 생성되었는지.
            var preference = RequireOne<CustomerPreferencePresentationController>("CustomerPreferencePresentationController");
            var feedback = RequireOne<PurchaseFeedbackPresentationController>("PurchaseFeedbackPresentationController");

            // 2) 실제 NpcProfile 데이터로 성향 힌트가 만들어지는지(임의 데이터 금지 원칙 확인).
            var snUtility = Resources.Load<NpcProfile>("NPCs/Profile_Tailor");   // traitSN -0.2 → 실용재, priceSensitivity 0.65 → 가격에 관대
            var snLuxury = Resources.Load<NpcProfile>("NPCs/Profile_Blacksmith"); // traitSN  0.6 → 장식·고급품, priceSensitivity 0.9 → 가격 라벨 없음(성격 폴백)
            var priceSensitive = Resources.Load<NpcProfile>("NPCs/Profile_Miner"); // traitSN 0.3, priceSensitivity 1.45 → 가격에 민감
            Require(snUtility != null && snLuxury != null && priceSensitive != null, "real NpcProfile assets load from Resources/NPCs");

            string utilHint = CustomerPreferencePresentationController.DescribePreference(snUtility);
            string luxHint = CustomerPreferencePresentationController.DescribePreference(snLuxury);
            string priceHint = CustomerPreferencePresentationController.DescribePreference(priceSensitive);
            Require(utilHint.Contains("실용재"), $"S-leaning profile reads as utility preference ({utilHint})");
            Require(luxHint.Contains("장식·고급품"), $"N-leaning profile reads as luxury preference ({luxHint})");
            Require(luxHint.Contains("신중형"), $"price-neutral profile falls back to a personality style ({luxHint})");

            // SPY-003 — priceSensitivity 가 NPC별로 달라져 "가격에 민감/관대" 힌트가 데이터 기반으로 나오는지.
            Require(priceHint.Contains("가격에 민감"), $"price-sensitive profile (priceSensitivity 1.45) shows a price hint ({priceHint})");
            Require(utilHint.Contains("가격에 관대"), $"price-tolerant profile (priceSensitivity 0.65) shows a price hint ({utilHint})");
            Require(preference.CurrentPreferenceText.Contains("관심 손님 성향"), "preference panel title is visible");

            // 3) 구매 결정 → 귀여운 이유 + 마을 변화 연결, 디버그 수치 노출 없음.
            var item = Resources.Load<Item>("Items/Item_BreadLoaf")
                ?? Resources.Load<Item>("Items/Item_Fish");
            Require(item != null, "validation item exists");
            int basePrice = Mathf.Max(1, item.basePrice);

            // 구매(저렴) — "샀어요" + 마을 변화 연결.
            feedback.RecordDecision(snLuxury, new PurchaseEvaluator.Result
            {
                willBuy = true,
                probability = 0.88f,
                reason = "validator-buy"
            }, item, Mathf.Max(1, basePrice / 2), "ValidatorBuyer");

            Require(feedback.LastReactionLine.Contains("샀어요") || feedback.LastReactionLine.Contains("구매"),
                $"buy decision produces cozy buy reason ({feedback.LastReactionLine})");
            Require(feedback.LastVillageTie.Contains("마을 변화"), "buy decision adds a village-change tie line");
            Require(feedback.CurrentFeedbackText.Contains("손님 반응"), "feedback panel title is visible");

            // 거절(고가) — "다음에" + 진열/가격 안내, 확률 수치/퍼센트 노출 없음.
            feedback.RecordDecision(snUtility, new PurchaseEvaluator.Result
            {
                willBuy = false,
                probability = 0.18f,
                reason = "validator-pass"
            }, item, basePrice * 2, "ValidatorPasser");

            Require(feedback.LastReactionLine.Contains("다음에") || feedback.LastReactionLine.Contains("물건은 아니"),
                $"reject decision produces cozy reject reason ({feedback.LastReactionLine})");
            Require(!feedback.CurrentFeedbackText.Contains("%"), "feedback panel never shows raw percentage values");
            Require(!feedback.CurrentFeedbackText.Contains("probability"), "feedback panel never shows debug fields");

            Debug.Log($"PA Customer Presentation Validation passed. utilHint='{utilHint}', luxHint='{luxHint}', lastBuy='{feedback.LastReactionLine}'");
        }
        catch (Exception ex)
        {
            _hadError = true;
            SessionState.SetBool(HadErrorKey, true);
            Debug.LogError($"PA Customer Presentation Validation failed: {ex.Message}\n{ex}");
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

        Debug.Log($"PA CustomerPresentation Check OK: {message}");
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
            Debug.LogError("PA Customer Presentation Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA Customer Presentation Validation finished successfully.");
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
