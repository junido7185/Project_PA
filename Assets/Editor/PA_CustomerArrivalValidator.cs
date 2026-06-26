#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// 손님 도착 페이싱 검증 — 영업을 시작하면(CDN-002 게이트가 열리면) 손님이 가게로 초대되고,
// 닫혀 있거나 Day 1 튜토리얼이면 능동 초대를 하지 않는지 확인한다.
// 기존 경제/구매/NPC FSM 내부는 호출하지 않고, 컨트롤러 공개 API 만 사용한다.
[InitializeOnLoad]
public static class PA_CustomerArrivalValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.CustomerArrival.Active";
    const string EnteredKey = "PA.CustomerArrival.Entered";
    const string RanKey = "PA.CustomerArrival.Ran";
    const string HadErrorKey = "PA.CustomerArrival.HadError";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;

    static PA_CustomerArrivalValidator()
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

    [MenuItem("Project PA/Validation/Run Customer Arrival Validation")]
    public static void RunCustomerArrivalValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA CustomerArrival: failed to open scene at {ScenePath}");
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

        Debug.Log("PA CustomerArrival: entering Play Mode.");
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
                Debug.LogError("PA CustomerArrival: timed out before entering Play Mode.");
                MarkFailedAndExit();
            }
            return;
        }

        if (!_ran && EditorApplication.isPlaying && elapsed > 3.0)
        {
            _ran = true;
            SessionState.SetBool(RanKey, true);
            RunRuntimeChecks();
            EditorApplication.ExitPlaymode();
            return;
        }

        if (_entered && !_ran && elapsed > 100.0)
        {
            Debug.LogError("PA CustomerArrival: timed out before runtime checks.");
            MarkFailedAndExit();
        }
    }

    static void RunRuntimeChecks()
    {
        try
        {
            Time.timeScale = 1f;

            var loop = RequireOne<DayNightShopLoopController>("DayNightShopLoopController");
            var arrival = RequireOne<CustomerArrivalController>("CustomerArrivalController");
            Require(GameClock.Instance != null, "GameClock exists");

            var npcs = Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);
            Require(npcs.Length > 0, $"scene has NPC customers ({npcs.Length})");

            // 1) Day 2 낮(DayPreparation): 게이트가 닫혀 능동 초대를 하지 않는다.
            loop.SimulatePhaseForValidation(8f, 2);
            Require(!loop.IsShopOpenForCustomers, "Day 2 daytime is gated closed");
            Require(!arrival.ShouldActivelyInvite, "no active customer invites while the shop is closed");
            ResetAllNpcsToIdle(npcs);
            Require(arrival.TryInviteWave(3) == 0, "inviting does nothing while the shop is closed");

            // 2) Day 1 튜토리얼: 손님은 구매 가능하지만 능동 초대는 시나리오 컨트롤러에 맡긴다(이중 구동 방지).
            loop.SimulatePhaseForValidation(8f, 1);
            Require(loop.IsShopOpenForCustomers, "Day 1 tutorial keeps the shop open for customers");
            Require(!arrival.ShouldActivelyInvite, "Day 1 tutorial leaves active invites to the scenario controller");
            Require(arrival.TryInviteWave(3) == 0, "no extra invites on Day 1 tutorial");

            // 3) Day 2 밤(ShopOpen) + 플레이어가 영업 시작: 손님이 가게로 초대된다.
            loop.SimulatePhaseForValidation(20f, 2);
            loop.SetShopOpenedForValidation(false);
            Require(!arrival.ShouldActivelyInvite, "before opening, customers are not invited at night either");
            Require(loop.TryOpenShop(), "player opens the shop at night");
            Require(arrival.ShouldActivelyInvite, "after opening, the shop actively invites customers");

            ResetAllNpcsToIdle(npcs);
            Require(arrival.ActiveShopperCount == 0, "all customers start idle before the invite wave");
            int invited = arrival.TryInviteWave(3);
            Require(invited >= 1, $"opening the shop pulls customers toward it ({invited} invited)");
            Require(arrival.ActiveShopperCount == invited, "invited customers are now heading to the shop");
            Require(arrival.InvitedThisOpening >= invited, "invited count is tracked for this opening");

            // 4) 동시 손님 수 상한이 지켜진다.
            int originalCap = arrival.maxConcurrentCustomers;
            arrival.maxConcurrentCustomers = 2;
            ResetAllNpcsToIdle(npcs);
            int capped = arrival.TryInviteWave(10);
            Require(capped <= 2, $"concurrent customer cap is respected ({capped} <= 2)");
            arrival.maxConcurrentCustomers = originalCap;

            Debug.Log($"PA Customer Arrival Validation passed. npcs={npcs.Length}, invited={invited}, capped={capped}");
        }
        catch (Exception ex)
        {
            _hadError = true;
            SessionState.SetBool(HadErrorKey, true);
            Debug.LogError($"PA Customer Arrival Validation failed: {ex.Message}\n{ex}");
        }
    }

    static void ResetAllNpcsToIdle(NpcController[] npcs)
    {
        foreach (var npc in npcs)
        {
            if (npc == null) continue;
            npc.Resume();
            npc.SetShoppingPriority(false);
            npc.currentState = NpcController.State.Idle;
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

        Debug.Log($"PA CustomerArrival Check OK: {message}");
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
            Debug.LogError("PA Customer Arrival Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA Customer Arrival Validation finished successfully.");
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
