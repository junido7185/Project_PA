#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// IL-001 + CDN-002 프레젠테이션 캡처 — 낮 채집 -> 밤 영업 -> 손님 반응 -> 정산 루프의
// 1920x1080 Game-view 스크린샷을 생성한다(사람 검토용). 게임 로직은 바꾸지 않는다.
[InitializeOnLoad]
public static class PA_GatheringShopReview
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.GatheringReview.Active";
    const string EnteredKey = "PA.GatheringReview.Entered";
    const string RanKey = "PA.GatheringReview.Ran";
    const string HadErrorKey = "PA.GatheringReview.HadError";
    const string OutputKey = "PA.GatheringReview.OutputDir";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static int _step;
    static double _startedAt;
    static double _nextStepAt;
    static string _outputDir;

    static PA_GatheringShopReview()
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

    [MenuItem("Project PA/Validation/Run Gathering + Shop Review (Screenshots)")]
    public static void RunGatheringShopReview()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA GatheringReview: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        _entered = false;
        _ran = false;
        _hadError = false;
        _step = 0;
        _startedAt = EditorApplication.timeSinceStartup;
        _nextStepAt = _startedAt;
        _outputDir = CreateOutputDirectory();

        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(HadErrorKey, false);
        SessionState.SetString(OutputKey, _outputDir);

        RegisterCallbacks();

        Debug.Log($"PA GatheringReview: entering Play Mode. Output={_outputDir}");
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
            _nextStepAt = _startedAt + 2.5;
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
                Debug.LogError("PA GatheringReview: timed out before entering Play Mode.");
                MarkFailedAndExit();
            }
            return;
        }

        if (!_ran && EditorApplication.isPlaying)
        {
            if (EditorApplication.timeSinceStartup < _nextStepAt)
                return;

            RunNextStep();
            return;
        }

        if (_entered && !_ran && elapsed > 150.0)
        {
            Debug.LogError("PA GatheringReview: timed out before completing review.");
            MarkFailedAndExit();
        }
    }

    static void RunNextStep()
    {
        try
        {
            switch (_step)
            {
                case 0:
                    PrepareRuntimeState();
                    SetDayPrep();
                    FrameForagePoint();
                    Capture("01_day_forage_point");
                    break;
                case 1:
                    GatherAll();
                    Capture("02_after_gather");
                    break;
                case 2:
                    OpenNightShop();
                    FrameMarketHub();
                    Capture("03_night_shop_open");
                    break;
                case 3:
                    ShowCustomerReaction();
                    Capture("04_customer_reaction");
                    break;
                case 4:
                    ShowSettlementAndReset();
                    Capture("05_settlement_next_day");
                    _ran = true;
                    SessionState.SetBool(RanKey, true);
                    EditorApplication.ExitPlaymode();
                    break;
            }

            _step++;
            _nextStepAt = EditorApplication.timeSinceStartup + 1.0;
        }
        catch (Exception ex)
        {
            _hadError = true;
            SessionState.SetBool(HadErrorKey, true);
            Debug.LogError($"PA GatheringReview failed: {ex.Message}\n{ex}");
            _ran = true;
            SessionState.SetBool(RanKey, true);
            EditorApplication.ExitPlaymode();
        }
    }

    static void PrepareRuntimeState()
    {
        Time.timeScale = 1f;
        Screen.SetResolution(1920, 1080, false);

        var scenario = Object.FindFirstObjectByType<PlayableDayScenarioController>();
        if (scenario != null)
            scenario.RestoreSavedSession("하늘", "green_bay", 0);
    }

    static void SetDayPrep()
    {
        var loop = DayNightShopLoopController.Instance;
        if (loop != null)
        {
            loop.SimulatePhaseForValidation(9f, 2);
            loop.ResetDayPrepForValidation();
        }
    }

    static void GatherAll()
    {
        var loop = DayNightShopLoopController.Instance;
        if (loop == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        foreach (var p in Object.FindObjectsByType<DaytimeStockPrepPoint>(FindObjectsSortMode.None))
            loop.TryCollectDayPrepStock(p, player);
    }

    static void OpenNightShop()
    {
        var loop = DayNightShopLoopController.Instance;
        if (loop == null) return;
        loop.SimulatePhaseForValidation(20f, 2);
        loop.SetShopOpenedForValidation(false);
        loop.TryOpenShop();
    }

    static void ShowCustomerReaction()
    {
        var feedback = Object.FindFirstObjectByType<PurchaseFeedbackPresentationController>();
        var item = Resources.Load<Item>("Items/Item_Fish") ?? Resources.Load<Item>("Items/Item_BreadLoaf");
        var buyer = Resources.Load<NpcProfile>("NPCs/Profile_Miner");
        var passer = Resources.Load<NpcProfile>("NPCs/Profile_Tailor");
        if (feedback != null && item != null)
        {
            int basePrice = Mathf.Max(1, item.basePrice);
            feedback.RecordDecision(buyer, new PurchaseEvaluator.Result { willBuy = true, probability = 0.84f },
                item, Mathf.Max(1, basePrice / 2), buyer != null ? buyer.npcName : "Miner_01");
            feedback.RecordDecision(passer, new PurchaseEvaluator.Result { willBuy = false, probability = 0.22f },
                item, basePrice * 2, passer != null ? passer.npcName : "Tailor_01");
        }

        var preference = Object.FindFirstObjectByType<CustomerPreferencePresentationController>();
        int forced = 0;
        foreach (var npc in Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            if (npc == null || npc.profile == null) continue;
            npc.currentState = NpcController.State.BrowsingShop;
            if (++forced >= 3) break;
        }
        if (preference != null)
            preference.RefreshNow();
    }

    static void ShowSettlementAndReset()
    {
        var loop = DayNightShopLoopController.Instance;
        if (loop == null) return;
        // 다음날 아침 — 채집 포인트가 다시 활성화된 상태.
        loop.SimulatePhaseForValidation(8f, 3);
        FrameForagePoint();
    }

    static void FrameForagePoint()
    {
        var cam = Camera.main;
        if (cam == null) return;

        DaytimeStockPrepPoint target = null;
        foreach (var p in Object.FindObjectsByType<DaytimeStockPrepPoint>(FindObjectsSortMode.None))
        {
            if (p == null) continue;
            if (p.activityId == "shore-forage") { target = p; break; }
            if (target == null) target = p;
        }
        if (target == null) return;

        Vector3 tp = target.transform.position;
        cam.transform.position = tp + new Vector3(0.5f, 2.4f, -3.8f);
        cam.transform.LookAt(tp + Vector3.up * 0.4f);
        cam.fieldOfView = 52f;
    }

    static void FrameMarketHub()
    {
        var cam = Camera.main;
        var marker = GameObject.Find("PA_ScreenshotCameraMarker_MarketHub");
        if (cam == null || marker == null) return;
        cam.transform.SetPositionAndRotation(marker.transform.position, marker.transform.rotation);
        cam.fieldOfView = 46f;
    }

    static void Capture(string name)
    {
        try
        {
            if (string.IsNullOrEmpty(_outputDir))
                _outputDir = CreateOutputDirectory();

            string file = Path.Combine(_outputDir, $"{name}.png");
            WriteCameraCapture(file);
            Debug.Log($"PA GatheringReview Capture: {file}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"PA GatheringReview: capture '{name}' skipped: {ex.Message}");
        }
    }

    static void WriteCameraCapture(string file)
    {
        var camera = Camera.main;
        if (camera == null)
            throw new InvalidOperationException("Main camera not found for capture.");

        const int width = 1920;
        const int height = 1080;

        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var modes = new RenderMode[canvases.Length];
        var canvasCameras = new Camera[canvases.Length];
        var planeDistances = new float[canvases.Length];

        var oldTarget = camera.targetTexture;
        int oldCullingMask = camera.cullingMask;
        var oldActive = RenderTexture.active;

        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        try
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas == null) continue;

                modes[i] = canvas.renderMode;
                canvasCameras[i] = canvas.worldCamera;
                planeDistances[i] = canvas.planeDistance;

                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = Mathf.Max(camera.nearClipPlane + 0.5f, 1f);
                }
            }

            camera.cullingMask = -1;
            camera.targetTexture = rt;
            RenderTexture.active = rt;
            camera.Render();

            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = oldTarget;
            camera.cullingMask = oldCullingMask;
            RenderTexture.active = oldActive;

            for (int i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas == null) continue;
                canvas.renderMode = modes[i];
                canvas.worldCamera = canvasCameras[i];
                canvas.planeDistance = planeDistances[i];
            }

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }

    static string CreateOutputDirectory()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "GatheringShopReview"));
        string dir = Path.Combine(root, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(dir);
        return dir;
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
            Debug.LogError("PA GatheringReview finished with errors. Check log.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA GatheringReview finished successfully.");
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
