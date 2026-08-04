#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PA_VillageCultureVisualValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.VillageCultureVisual.Active";
    const string EnteredKey = "PA.VillageCultureVisual.Entered";
    const string RanKey = "PA.VillageCultureVisual.Ran";
    const string HadErrorKey = "PA.VillageCultureVisual.HadError";
    const string OutputKey = "PA.VillageCultureVisual.OutputDir";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;
    static string _outputDir;
    static Task _runtimeTask;

    static PA_VillageCultureVisualValidator()
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

    [MenuItem("Project PA/Validation/Run Village Culture Visual Validation")]
    public static void RunVillageCultureVisualValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA VillageCultureVisual: failed to open scene at {ScenePath}");
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

        Debug.Log($"PA VillageCultureVisual: entering Play Mode. Output={_outputDir}");
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
                Debug.LogError("PA VillageCultureVisual: timed out before entering Play Mode.");
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
                Debug.LogError($"PA VillageCultureVisual failed: {_runtimeTask.Exception?.GetBaseException()}");
            }

            _ran = true;
            SessionState.SetBool(RanKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }

        if (_entered && !_ran && elapsed > 120.0)
        {
            Debug.LogError("PA VillageCultureVisual: timed out before completing checks.");
            MarkFailedAndExit();
        }
    }

    static async Task RunRuntimeChecksAsync()
    {
        PrepareDefaultDayStart();
        await CaptureAsync("vc001a_day1_default_no_change");
        await Task.Delay(1000);

        RecordProcessedSaleSameDay();
        await CaptureAsync("vc001a_after_processed_sale_same_day");
        await Task.Delay(1000);

        EnterNextDayPreparation();
        await CaptureAsync("vc001a_day2_preparation_visual_active");
        await Task.Delay(1000);

        ValidateSafetyAndLayout();
    }

    static void PrepareDefaultDayStart()
    {
        Time.timeScale = 1f;
        Screen.SetResolution(1920, 1080, false);

        Require(GameClock.Instance != null, "GameClock exists");
        Require(SalesLogManager.Instance != null, "SalesLogManager exists");
        Require(DayNightShopLoopController.Instance != null, "DayNightShopLoopController exists");
        Require(SaveManager.instance != null, "SaveManager exists");
        Require(Object.FindFirstObjectByType<ShopSlot>() != null, "ShopSlot still exists");
        Require(Object.FindFirstObjectByType<NpcController>() != null, "NpcController still exists");

        MoveCameraToMarketMarker();

        DayNightShopLoopController.Instance.SimulatePhaseForValidation(8f, 1);
        var culture = RequireOne<VillageCultureVisualController>("VillageCultureVisualController");
        culture.ResetForValidation();
        culture.RefreshNow();

        Require(culture.VisualRoot != null, "culture visual root is created at runtime");
        Require(culture.VisualRoot.name == VillageCultureVisualController.ProcessedVisualRootName,
            "culture visual uses the Project_PA processed-category name");
        Require(!culture.VisualActive, "Day 1 default start visual is inactive");
        Require(!culture.HasPendingChange, "Day 1 default start has no pending culture change");
    }

    static void RecordProcessedSaleSameDay()
    {
        var culture = RequireOne<VillageCultureVisualController>("VillageCultureVisualController");
        var signal = RequireOne<VillageChangeSignalController>("VillageChangeSignalController");

        GameClock.Instance.ForceSet(19f, 1, "VillageCultureVisualValidator.Sale");
        SalesLogManager.Instance.RecordSale(
            "BreadLoaf",
            ItemCategory.Processed.ToString(),
            30,
            1.0f,
            "VC001AValidatorCustomer",
            1,
            19);

        signal.RefreshNow();
        culture.RefreshNow();

        Require(culture.HasPendingChange, "processed sale creates a pending next-day change");
        Require(culture.PendingSaleDay == 1, "pending culture change records the sale day");
        Require(!culture.VisualActive, "culture visual does not activate immediately after sale");
        Require(signal.GetLeadingSignalSummary().Contains("Processed"), "existing Village Direction data sees the processed sale");
    }

    static void EnterNextDayPreparation()
    {
        var culture = RequireOne<VillageCultureVisualController>("VillageCultureVisualController");

        DayNightShopLoopController.Instance.SimulatePhaseForValidation(8f, 2);
        culture.RefreshNow();
        culture.EvaluateForDayPreparation(2, true);

        Require(culture.VisualActive, "processed market visual activates on next DayPreparation");
        Require(culture.HasActiveCategory && culture.ActiveCategory == ItemCategory.Processed,
            "active culture category is Processed");
        Require(culture.HintDisplayCount == 1, "culture hint is shown once when the next-day change appears");
        Require(culture.HintPanel != null && culture.HintPanel.activeInHierarchy,
            "culture hint panel is visible for screenshot evidence");

        culture.EvaluateForDayPreparation(2, true);
        Require(culture.HintDisplayCount == 1, "culture hint is not duplicated by repeated next-day evaluation");
    }

    static void ValidateSafetyAndLayout()
    {
        var culture = RequireOne<VillageCultureVisualController>("VillageCultureVisualController");
        RequireNoBlockingColliders(culture.VisualRoot);
        RequireSafeDistanceFromPlayerAndSlots(culture.VisualRoot);
        RequireNoVillageCultureSaveFields();

        var hint = culture.HintPanel != null ? culture.HintPanel.GetComponent<RectTransform>() : null;
        Require(hint != null, "culture hint has a RectTransform");
        Require(IsWithinScreen(hint, 4f), $"culture hint stays within screen bounds ({RectInfo(hint)})");
        RequireNoOverlap(hint, OptionalRect("MoneyHudPanel"), "culture hint", "MoneyHUD");
        RequireNoOverlap(hint, OptionalRect("CustomerPreferencePanel"), "culture hint", "CustomerPreferencePanel");
        RequireNoOverlap(hint, OptionalRect("PurchaseFeedbackPanel"), "culture hint", "PurchaseFeedbackPanel");

        if (TryGetHotbarRect(out Rect hotbar))
        {
            Rect hintRect = GetScreenRect(hint);
            Require(!hintRect.Overlaps(hotbar), "culture hint does not overlap the hotbar");
        }
        else
        {
            Debug.LogWarning("PA VillageCultureVisual: hotbar rect not found; skipped hotbar overlap check.");
        }

        Require(typeof(ShopSlot).GetMethod("TryPurchaseByNpc") != null, "ShopSlot purchase entry point remains available");
        Require(typeof(PurchaseEvaluator).GetMethod("Evaluate") != null, "PurchaseEvaluator entry point remains available");

        Debug.Log($"PA Village Culture Visual Validation passed. screenshots={_outputDir}");
    }

    static void RequireNoBlockingColliders(GameObject root)
    {
        Require(root != null, "culture visual root exists for collider check");
        var colliders = root.GetComponentsInChildren<Collider>(true);
        foreach (var collider in colliders)
            Require(collider == null || !collider.enabled || collider.isTrigger,
                "culture visual has no blocking collider");
    }

    static void RequireSafeDistanceFromPlayerAndSlots(GameObject root)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        if (player != null)
            Require(Vector3.Distance(root.transform.position, player.transform.position) > 1.0f,
                "culture visual is not on top of the player start");

        foreach (var slot in Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
        {
            if (slot == null) continue;
            Require(Vector3.Distance(root.transform.position, slot.transform.position) > 0.45f,
                "culture visual is not on top of a ShopSlot");
        }
    }

    // VC-001A 당시에는 "저장 필드를 추가하지 않는다"가 티켓 규칙이었으나,
    // Task 057(v8→v9 승인)에서 마을 변화 지속성을 위해 정식으로 필드가 추가됐다.
    // 이제는 반대로 v9 필드 6종이 존재하는지 확인한다.
    static void RequireNoVillageCultureSaveFields()
    {
        string[] required =
        {
            "villageCultureHasPendingChange", "villageCulturePendingSaleDay", "villageCulturePendingCategory",
            "villageCultureHasActiveChange", "villageCultureActiveCategory", "villageCultureHintShown"
        };

        foreach (string fieldName in required)
        {
            Require(typeof(SaveData).GetField(fieldName, BindingFlags.Public | BindingFlags.Instance) != null,
                $"Task 057 SaveData v9 field exists: {fieldName}");
        }
    }

    static void MoveCameraToMarketMarker()
    {
        var camera = Camera.main;
        var marker = GameObject.Find("PA_ScreenshotCameraMarker_MarketHub");
        if (camera != null && marker != null)
        {
            camera.transform.SetPositionAndRotation(marker.transform.position, marker.transform.rotation);
            camera.fieldOfView = 46f;
        }
    }

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
            Debug.Log($"PA VillageCultureVisual Capture: {file}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"PA VillageCultureVisual: screenshot capture skipped: {ex.Message}");
        }
    }

    static bool TryGetHotbarRect(out Rect rect)
    {
        rect = new Rect();
        bool any = false;

        foreach (var slot in Object.FindObjectsByType<InventorySlotUI>(FindObjectsSortMode.None))
        {
            if (slot == null || !slot.gameObject.activeInHierarchy) continue;
            if (slot.transform is not RectTransform rt) continue;

            Rect r = GetScreenRect(rt);
            if (r.width <= 0f || r.height <= 0f) continue;

            rect = any ? UnionRect(rect, r) : r;
            any = true;
        }

        return any;
    }

    static Rect UnionRect(Rect a, Rect b)
    {
        return Rect.MinMaxRect(
            Mathf.Min(a.xMin, b.xMin),
            Mathf.Min(a.yMin, b.yMin),
            Mathf.Max(a.xMax, b.xMax),
            Mathf.Max(a.yMax, b.yMax));
    }

    static RectTransform OptionalRect(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go.GetComponent<RectTransform>() : null;
    }

    static void RequireNoOverlap(RectTransform a, RectTransform b, string aName, string bName)
    {
        if (b == null)
        {
            Debug.LogWarning($"PA VillageCultureVisual: {bName} not found; skipped overlap check vs {aName}.");
            return;
        }

        Require(!GetScreenRect(a).Overlaps(GetScreenRect(b)), $"{aName} does not overlap {bName}");
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

    static T RequireOne<T>(string label) where T : Object
    {
        foreach (var obj in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            if (obj != null) return obj;

        throw new InvalidOperationException($"{label} not found.");
    }

    static string CreateOutputDirectory()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "VillageCultureVisual"));
        string dir = Path.Combine(root, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);

        Debug.Log($"PA VillageCultureVisual Check OK: {message}");
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
            Debug.LogError("PA Village Culture Visual Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA Village Culture Visual Validation finished successfully.");
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
