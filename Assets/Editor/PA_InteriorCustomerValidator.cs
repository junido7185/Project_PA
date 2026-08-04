#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// S3 검증 — 실내 잡화점 손님 루프: 초대(워프 입장) → 기존 FSM 둘러보기 → 구매(돈 증가) → 퇴장 복귀.
[InitializeOnLoad]
public static class PA_InteriorCustomerValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.InteriorCustomer.Active";
    const string EnteredKey = "PA.InteriorCustomer.Entered";
    const string RanKey = "PA.InteriorCustomer.Ran";
    const string HadErrorKey = "PA.InteriorCustomer.HadError";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static int _step;
    static double _startedAt;
    static double _nextStepAt;
    static NpcController _visitor;
    static int _baselineMoney;
    static bool _purchaseSeen;
    static bool _approachReservationSeen;
    static bool _completeApproachPathSeen;
    static bool _approachArrivalSeen;

    static PA_InteriorCustomerValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;

        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _hadError = SessionState.GetBool(HadErrorKey, false);
        RegisterCallbacks();

        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Run Interior Customer Validation")]
    public static void RunInteriorCustomerValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA InteriorCustomer: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        _entered = false; _ran = false; _hadError = false; _step = 0;
        _visitor = null; _purchaseSeen = false;
        _approachReservationSeen = false;
        _completeApproachPathSeen = false;
        _approachArrivalSeen = false;
        _startedAt = EditorApplication.timeSinceStartup;

        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(HadErrorKey, false);

        RegisterCallbacks();
        Debug.Log("PA InteriorCustomer: entering Play Mode.");
        EditorApplication.EnterPlaymode();
    }

    static void RegisterCallbacks()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _entered = true;
            _startedAt = EditorApplication.timeSinceStartup;
            _nextStepAt = _startedAt + 3.0;
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
            if (elapsed > 60.0) { Debug.LogError("PA InteriorCustomer: timed out entering Play Mode."); MarkFailedAndExit(); }
            return;
        }

        if (!_ran && EditorApplication.isPlaying)
        {
            if (EditorApplication.timeSinceStartup < _nextStepAt) return;

            try
            {
                bool done = RunStep(_step);
                _step++;
                _nextStepAt = EditorApplication.timeSinceStartup + 0.35;

                if (done)
                {
                    _ran = true;
                    SessionState.SetBool(RanKey, true);
                    EditorApplication.ExitPlaymode();
                }
            }
            catch (Exception ex)
            {
                _hadError = true;
                SessionState.SetBool(HadErrorKey, true);
                Debug.LogError($"PA InteriorCustomer failed: {ex.Message}\n{ex}");
                _ran = true;
                SessionState.SetBool(RanKey, true);
                EditorApplication.ExitPlaymode();
            }
            return;
        }

        if (_entered && !_ran && elapsed > 150.0)
        {
            Debug.LogError("PA InteriorCustomer: timed out before completing checks.");
            MarkFailedAndExit();
        }
    }

    static bool RunStep(int step)
    {
        if (step == 0)
        {
            // Day 1 온보딩 모달이 Time.timeScale=0 으로 세계를 멈추므로 먼저 해제한다.
            var scenario = Object.FindFirstObjectByType<PlayableDayScenarioController>();
            if (scenario != null)
                scenario.RestoreSavedSession("실내검증", "green_bay", 2);
            Require(Time.timeScale > 0.9f, "game time is running (onboarding dismissed)");

            var loop = DayNightShopLoopController.Instance;
            Require(loop != null, "day/night loop exists");
            Require(InteriorCustomerController.Instance != null, "interior customer controller exists");
            Require(EconomyService.Instance != null && GameClock.Instance != null, "economy/clock exist");

            var interior = GameObject.Find("PA_StoreInterior");
            Require(interior != null, "store interior exists");
            var interiorShop = interior.GetComponent<Shop>();
            Require(interiorShop != null, "interior Shop component registered (S2)");

            // Day 2 밤 + 간판 개점 → 손님 구매 게이트 열림 (Day 1 튜토리얼 제외 규칙 검증 포함)
            // 18:15 = ShopOpen(18~) 직후이자 전문직 Shopping 창(18~20시)의 초입.
            // 19.5 로 시작하면 20:00 Rest 강제 귀가까지 실시간 30초뿐이라 레이스가 났었다.
            loop.SimulatePhaseForValidation(18.25f, 2);
            loop.SetShopOpenedForValidation(true);
            Require(!loop.IsTutorialAlwaysOpen, "day 2 is not tutorial-always-open");
            Require(loop.IsShopOpenForCustomers, "customer purchase gate is open");

            // 실내 6슬롯 전부 저가 진열 → 구매 확률 최대화 (구매 수학은 무변경)
            var bread = Resources.Load<Item>("Items/Item_BreadLoaf");
            Require(bread != null, "BreadLoaf asset exists");
            var slots = interior.GetComponentsInChildren<ShopSlot>(true);
            Require(slots.Length == 6, "interior grid has 6 slots");
            foreach (var slot in slots)
            {
                slot.currentItem = new ItemInstance(bread, 1) { quality = 1f, currentPrice = bread.basePrice };
                slot.displayPrice = 15; // basePrice 30 의 절반 — 우호적 가격
                slot.RefreshDisplay();
            }
            Require(interiorShop.GetAvailableSlots().Count == 6, "interior shop reports 6 available slots");

            _baselineMoney = EconomyService.Instance.Money;

            _visitor = InteriorCustomerController.Instance.TryInviteOne();
            Require(_visitor != null, "an idle resident was invited inside");
            Require(_visitor.transform.position.y > 50f, $"visitor is inside (y={_visitor.transform.position.y:0.#})");
            Debug.Log($"PA InteriorCustomer: visitor={_visitor.name}, baseline={_baselineMoney}G");
            return false;
        }

        // 폴링: 구매(돈 증가) → 컨트롤러가 Idle 복귀 손님을 지상으로 되돌림
        Require(_visitor != null, "visitor reference alive");

        if (!_purchaseSeen && EconomyService.Instance.Money > _baselineMoney)
        {
            _purchaseSeen = true;
            Debug.Log($"PA InteriorCustomer Check OK: purchase inside increased money ({_baselineMoney} -> {EconomyService.Instance.Money}G)");
        }

        bool visitorOutside = _visitor.transform.position.y < 50f;

        var agentDiag = _visitor.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agentDiag != null)
        {
            Debug.Log($"PA InteriorCustomer poll: state={_visitor.currentState} pos={_visitor.transform.position} "
                + $"onMesh={agentDiag.isOnNavMesh} pathPending={agentDiag.pathPending} pathStatus={agentDiag.pathStatus} "
                + $"remain={(float.IsInfinity(agentDiag.remainingDistance) ? -1f : agentDiag.remainingDistance):0.##} dest={agentDiag.destination}");

            if (_visitor.HasShopApproachReservation && _visitor.CurrentShopSlotTarget != null)
            {
                _approachReservationSeen = ShopCustomerApproachController.Instance != null
                    && ShopCustomerApproachController.Instance.IsReservedBy(_visitor.CurrentShopSlotTarget, _visitor);
                _completeApproachPathSeen |= !agentDiag.pathPending
                    && agentDiag.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete;
                _approachArrivalSeen |= Vector3.Distance(_visitor.transform.position,
                    _visitor.CurrentShopApproachPoint) < 0.75f;
            }
        }

        if (_purchaseSeen && visitorOutside)
        {
            Require(_approachReservationSeen, "visitor reserved an explicit ShopSlot approach owner");
            Require(_completeApproachPathSeen, "visitor used a complete NavMesh path to the approach point");
            Require(_approachArrivalSeen, "visitor stopped at the reserved front position before purchase");
            Require(_visitor.currentState == NpcController.State.Idle, "visitor FSM returned to Idle after visit");
            Debug.Log("PA Interior Customer Validation passed. invite→reserve→approach→browse→buy→return");
            return true;
        }

        if (step > 100)
            throw new InvalidOperationException(
                $"interior visit did not complete (purchase={_purchaseSeen}, outside={visitorOutside}, state={_visitor.currentState})");

        return false;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"PA InteriorCustomer Check OK: {message}");
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
            Debug.LogError("PA Interior Customer Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA Interior Customer Validation finished successfully.");
        EditorApplication.Exit(0);
    }

    static void Cleanup()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
    }
}
#endif
