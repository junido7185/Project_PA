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

// SPY-002 패널 레이아웃/가독성 검증 (1920x1080).
//
// 자동 검증으로 다음을 확인한다(사람의 눈 대신 좌표 기반):
// - 신규 '관심 손님 성향'(CustomerPreferencePanel)과 '손님 반응'(PurchaseFeedbackPanel) 패널이
//   기존 HUD(Money/Demand/Village)·핫바·서로와 화면에서 겹치지 않는다.
// - 두 패널이 화면 경계 안에 들어온다.
// 또한 사람 검토용으로 1920x1080 Game-view 스크린샷을 캡처한다(캡처 실패는 경고로만 처리).
//
// 레이아웃/스크린샷 외 게임 로직은 바꾸지 않는다. 패널 위치/크기 조정이 필요하면
// 컨트롤러의 BuildUI 좌표만 손본다.
[InitializeOnLoad]
public static class PA_CustomerPanelLayoutValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.CustomerPanelLayout.Active";
    const string EnteredKey = "PA.CustomerPanelLayout.Entered";
    const string RanKey = "PA.CustomerPanelLayout.Ran";
    const string HadErrorKey = "PA.CustomerPanelLayout.HadError";
    const string OutputKey = "PA.CustomerPanelLayout.OutputDir";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;
    static string _outputDir;
    static Task _runtimeTask;

    static PA_CustomerPanelLayoutValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;

        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _hadError = SessionState.GetBool(HadErrorKey, false);
        _outputDir = SessionState.GetString(OutputKey, string.Empty);
        RegisterCallbacks();

        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Run Customer Panel Layout Validation")]
    public static void RunCustomerPanelLayoutValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA CustomerPanelLayout: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        _entered = false;
        _ran = false;
        _hadError = false;
        _runtimeTask = null;
        _startedAt = EditorApplication.timeSinceStartup;
        _outputDir = CreateOutputDirectory();

        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(HadErrorKey, false);
        SessionState.SetString(OutputKey, _outputDir);

        RegisterCallbacks();

        Debug.Log($"PA CustomerPanelLayout: entering Play Mode. Output={_outputDir}");
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
                Debug.LogError("PA CustomerPanelLayout: timed out before entering Play Mode.");
                MarkFailedAndExit();
            }
            return;
        }

        if (!_ran && EditorApplication.isPlaying && _runtimeTask == null && elapsed > 2.5)
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
                Debug.LogError($"PA CustomerPanelLayout failed: {_runtimeTask.Exception?.GetBaseException()}");
            }

            _ran = true;
            SessionState.SetBool(RanKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }

        if (_entered && !_ran && elapsed > 120.0)
        {
            Debug.LogError("PA CustomerPanelLayout: timed out before completing checks.");
            MarkFailedAndExit();
        }
    }

    static async Task RunRuntimeChecksAsync()
    {
        PrepareRuntimeState();
        await Task.Delay(1200);
        RunLayoutChecks();
        await CaptureAsync("customer_panels_1920x1080");
    }

    // 화면을 1920x1080 으로 고정하고, 두 패널에 실제 내용이 보이도록 채운다(스크린샷 가독성용).
    static void PrepareRuntimeState()
    {
        Time.timeScale = 1f;
        Screen.SetResolution(1920, 1080, false);

        // 온보딩 '개척자 등록' 모달을 닫아 코너 패널이 잘 보이는 구도를 만든다(스크린샷 가독성용).
        var scenario = Object.FindFirstObjectByType<PlayableDayScenarioController>();
        if (scenario != null)
        {
            try { scenario.RestoreSavedSession("하늘", "green_bay", 0); }
            catch (Exception ex) { Debug.LogWarning($"PA CustomerPanelLayout: RestoreSavedSession skipped: {ex.Message}"); }
        }

        // 데모 카메라를 시장 허브 마커로 이동(스크린샷 구도).
        var camera = Camera.main;
        var marker = GameObject.Find("PA_ScreenshotCameraMarker_MarketHub");
        if (camera != null && marker != null)
        {
            camera.transform.SetPositionAndRotation(marker.transform.position, marker.transform.rotation);
            camera.fieldOfView = 46f;
        }

        // '손님 반응' 패널 채우기 — 실제 RecordDecision 경로(구매 1건 + 거절 1건).
        var feedback = Object.FindFirstObjectByType<PurchaseFeedbackPresentationController>();
        var item = Resources.Load<Item>("Items/Item_BreadLoaf") ?? Resources.Load<Item>("Items/Item_Fish");
        var buyer = Resources.Load<NpcProfile>("NPCs/Profile_Miner");
        var passer = Resources.Load<NpcProfile>("NPCs/Profile_Tailor");
        if (feedback != null && item != null)
        {
            int basePrice = Mathf.Max(1, item.basePrice);
            feedback.RecordDecision(buyer, new PurchaseEvaluator.Result { willBuy = true, probability = 0.85f },
                item, Mathf.Max(1, basePrice / 2), buyer != null ? buyer.npcName : "Miner_01");
            feedback.RecordDecision(passer, new PurchaseEvaluator.Result { willBuy = false, probability = 0.2f },
                item, basePrice * 2, passer != null ? passer.npcName : "Tailor_01");
        }

        // '관심 손님 성향' 패널 채우기 — 일부 NPC를 쇼핑 상태로 두고 갱신(스크린샷용 표시).
        var preference = Object.FindFirstObjectByType<CustomerPreferencePresentationController>();
        var npcs = Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);
        int forced = 0;
        NpcController firstResident = null;
        foreach (var npc in npcs)
        {
            if (npc == null || npc.profile == null) continue;
            npc.currentState = NpcController.State.MovingToShop;
            firstResident ??= npc;
            if (++forced >= 3) break;
        }
        if (preference != null)
            preference.RefreshNow();

        // Task 031 — 기본 플레이에서 보이는 기존 머리 위 말풍선에 별도 주민 태그를 띄워 캡처한다.
        var bubble = firstResident != null ? firstResident.GetComponentInChildren<NpcBubbleUI>(true) : null;
        if (bubble != null)
            bubble.Show("진열대를 둘러보는 중...", 30f);
    }

    static void RunLayoutChecks()
    {
        var preference = RequireRect("CustomerPreferencePanel");
        var feedback = RequireRect("PurchaseFeedbackPanel");
        var bubble = Object.FindFirstObjectByType<NpcBubbleUI>();

        var money = OptionalRect("MoneyHudPanel");
        var demand = OptionalRect("CustomerDemandInsightPanel");
        var village = OptionalRect("VillageChangeSignalPanel");

        // 1) 두 신규 패널은 화면 경계 안에 있어야 한다.
        Require(IsWithinScreen(preference, 6f), $"preference panel is within 1920x1080 screen bounds ({RectInfo(preference)})");
        Require(IsWithinScreen(feedback, 6f), $"feedback panel is within 1920x1080 screen bounds ({RectInfo(feedback)})");

        // 2) '관심 손님 성향'(우상단)은 우상단 스택과 겹치지 않아야 한다.
        RequireNoOverlap(preference, money, "preference panel", "MoneyHUD");
        RequireNoOverlap(preference, demand, "preference panel", "Demand panel");
        RequireNoOverlap(preference, village, "preference panel", "Village panel");

        // 3) '손님 반응'(하단 우측)은 우상단 스택과 겹치지 않아야 한다(수직 분리 확인).
        RequireNoOverlap(feedback, money, "feedback panel", "MoneyHUD");
        RequireNoOverlap(feedback, demand, "feedback panel", "Demand panel");
        RequireNoOverlap(feedback, village, "feedback panel", "Village panel");

        // 4) 두 신규 패널은 서로 겹치지 않아야 한다.
        Require(!ScreenRectsOverlap(preference, feedback), "preference panel and feedback panel do not overlap each other");

        // Task 031 — 플레이어에게 실제 보이는 머리 위 계층 태그도 화면 경계 안에 있어야 한다.
        Require(bubble != null && bubble.CurrentCustomerClassLabel == "[주민]",
            "active player-facing NPC bubble displays the resident class label");
        Require(bubble.customerClassText != null && IsWithinScreen(bubble.customerClassText.rectTransform, 2f),
            "resident class tag stays within the visible screen bounds");

        // 5) '손님 반응'은 핫바(하단 중앙)와 겹치지 않아야 한다.
        if (TryGetHotbarRect(out Rect hotbar))
        {
            Rect fb = GetScreenRect(feedback);
            Require(!fb.Overlaps(hotbar),
                $"feedback panel does not overlap hotbar (hotbar x[{hotbar.xMin:0}-{hotbar.xMax:0}] y[{hotbar.yMin:0}-{hotbar.yMax:0}] vs {RectInfo(feedback)})");
        }
        else
        {
            Debug.LogWarning("PA CustomerPanelLayout: hotbar slots not found; skipped hotbar overlap check.");
        }

        Debug.Log($"PA Customer Panel Layout Validation passed. preference={RectInfo(preference)}, feedback={RectInfo(feedback)}, screen={Screen.width}x{Screen.height}");
    }

    static void RequireNoOverlap(RectTransform a, RectTransform b, string aName, string bName)
    {
        if (b == null)
        {
            Debug.LogWarning($"PA CustomerPanelLayout: {bName} not found; skipped overlap check vs {aName}.");
            return;
        }

        Require(!ScreenRectsOverlap(a, b), $"{aName} does not overlap {bName}");
    }

    // 핫바 화면 영역을 구한다. 먼저 InventoryUI.slotParent, 없으면 활성 InventorySlotUI 합집합을 사용한다.
    static bool TryGetHotbarRect(out Rect rect)
    {
        rect = new Rect();

        var inv = InventoryUI.instance ?? Object.FindFirstObjectByType<InventoryUI>();
        if (inv != null && inv.slotParent is RectTransform parentRt && parentRt.gameObject.activeInHierarchy)
        {
            Rect pr = GetScreenRect(parentRt);
            if (pr.width > 0f && pr.height > 0f)
            {
                rect = pr;
                return true;
            }
        }

        bool any = false;
        foreach (var slot in Object.FindObjectsByType<InventorySlotUI>(FindObjectsSortMode.None))
        {
            if (slot == null || !slot.gameObject.activeInHierarchy) continue;
            var rt = slot.transform as RectTransform;
            if (rt == null) continue;

            Rect r = GetScreenRect(rt);
            if (r.width <= 0f || r.height <= 0f) continue;

            rect = any ? UnionRect(rect, r) : r;
            any = true;
        }

        return any;
    }

    static Rect UnionRect(Rect a, Rect b)
    {
        float xMin = Mathf.Min(a.xMin, b.xMin);
        float yMin = Mathf.Min(a.yMin, b.yMin);
        float xMax = Mathf.Max(a.xMax, b.xMax);
        float yMax = Mathf.Max(a.yMax, b.yMax);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    // ---------- 스크린샷 (PA_FinalPresentationReviewer 와 동일 방식) ----------

    static async Task CaptureAsync(string name)
    {
        try
        {
            if (string.IsNullOrEmpty(_outputDir))
                _outputDir = CreateOutputDirectory();

            string file = Path.Combine(_outputDir, $"{name}.png");
            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (camera == null)
                throw new InvalidOperationException("Main camera not found for capture.");

            var marker = GameObject.Find("PA_ScreenshotCameraMarker_MarketHub");
            await PA_SafeGameViewCapture.CaptureAsync(file, camera, captureCamera =>
            {
                if (marker != null)
                {
                    captureCamera.transform.SetPositionAndRotation(marker.transform.position, marker.transform.rotation);
                    captureCamera.fieldOfView = 46f;
                }

                captureCamera.cullingMask = -1;
            }, 1920, 1080, 1000);
            Debug.Log($"PA CustomerPanelLayout Capture: {file}");
        }
        catch (Exception ex)
        {
            // 캡처 실패는 레이아웃 검증 실패로 보지 않는다(헤드리스 환경 등).
            Debug.LogWarning($"PA CustomerPanelLayout: screenshot capture skipped: {ex.Message}");
        }
    }

    // ---------- Rect 헬퍼 ----------

    static RectTransform RequireRect(string name)
    {
        var go = GameObject.Find(name);
        if (go == null)
            throw new InvalidOperationException($"{name} not found in scene.");
        var rt = go.GetComponent<RectTransform>();
        if (rt == null)
            throw new InvalidOperationException($"{name} has no RectTransform.");
        return rt;
    }

    static RectTransform OptionalRect(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go.GetComponent<RectTransform>() : null;
    }

    static bool ScreenRectsOverlap(RectTransform a, RectTransform b)
    {
        return GetScreenRect(a).Overlaps(GetScreenRect(b));
    }

    static bool IsWithinScreen(RectTransform rt, float padding)
    {
        Rect r = GetScreenRect(rt);
        return r.xMin >= padding
            && r.yMin >= padding
            && r.xMax <= Screen.width - padding
            && r.yMax <= Screen.height - padding;
    }

    static string RectInfo(RectTransform rt)
    {
        Rect r = GetScreenRect(rt);
        return $"x[{r.xMin:0}-{r.xMax:0}] y[{r.yMin:0}-{r.yMax:0}]";
    }

    static Rect GetScreenRect(RectTransform rt)
    {
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float minX = corners[0].x, maxX = corners[0].x, minY = corners[0].y, maxY = corners[0].y;
        for (int i = 1; i < corners.Length; i++)
        {
            minX = Mathf.Min(minX, corners[i].x);
            maxX = Mathf.Max(maxX, corners[i].x);
            minY = Mathf.Min(minY, corners[i].y);
            maxY = Mathf.Max(maxY, corners[i].y);
        }
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    static string CreateOutputDirectory()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "CustomerPanelReview"));
        string dir = Path.Combine(root, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);

        Debug.Log($"PA CustomerPanelLayout Check OK: {message}");
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
            Debug.LogError("PA Customer Panel Layout Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA Customer Panel Layout Validation finished successfully.");
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
