#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Day 1-3 core-slice presentation check.
// This is editor-only QA: it verifies the player-facing HUD remains available
// while development/advisor overlays are hidden by default and restorable.
[InitializeOnLoad]
public static class PA_CoreSlicePlayabilityValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.CoreSlicePlayability.Active";
    const string EnteredKey = "PA.CoreSlicePlayability.Entered";
    const string RanKey = "PA.CoreSlicePlayability.Ran";
    const string HadErrorKey = "PA.CoreSlicePlayability.HadError";
    const string OutputKey = "PA.CoreSlicePlayability.Output";

    static readonly string[] DevelopmentCanvasNames =
    {
        "LongPlayProgressionCanvas",
        "ProcessingOpportunityCanvas",
        "CustomerDemandInsightCanvas",
        "VillageChangeSignalCanvas",
        "CustomerPreferenceCanvas",
        "PurchaseFeedbackCanvas"
    };

    static readonly string[] DevelopmentWorldNames =
    {
        "PA_DemoRoute_VisualMarkers",
        "PA_DemoRoute_Label",
        "PA_CustomerApproach_Label",
        "PA_Reinvestment_Label",
        "PA_ScreenshotCameraMarker_MarketHub",
        "PA_EconomicRoleBadge"
    };

    static readonly string[] DevelopmentWorldPrefixes =
    {
        "PA_PathStep_"
    };

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static int _step;
    static double _startedAt;
    static double _nextStepAt;
    static string _outputDir;

    static PA_CoreSlicePlayabilityValidator()
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

    [MenuItem("Project PA/Validation/Run Core Slice Playability Validation")]
    public static void RunCoreSlicePlayabilityValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA Core Slice: failed to open scene at {ScenePath}");
            ExitIfBatch(1);
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

        Debug.Log($"PA Core Slice: entering Play Mode. Output={_outputDir}");
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
                Debug.LogError("PA Core Slice: timed out before entering Play Mode.");
                MarkFailedAndExitPlayMode();
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

        if (_entered && !_ran && elapsed > 120.0)
        {
            Debug.LogError("PA Core Slice: timed out before completing checks.");
            MarkFailedAndExitPlayMode();
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
                    break;
                case 1:
                    ValidateDefaultPlayerView();
                    Capture("core_slice_default_player_view");
                    break;
                case 2:
                    ValidateDevelopmentToggleOn();
                    break;
                case 3:
                    ValidateDevelopmentToggleOff();
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
            Debug.LogError($"PA Core Slice Playability Validation failed: {ex.Message}\n{ex}");
            _ran = true;
            SessionState.SetBool(RanKey, true);
            EditorApplication.ExitPlaymode();
        }
    }

    static void PrepareRuntimeState()
    {
        Time.timeScale = 1f;
        Screen.SetResolution(1920, 1080, false);

        var mode = RequireOne<CoreSlicePresentationMode>("CoreSlicePresentationMode");
        mode.SetDevelopmentOverlaysVisible(false);

        var loop = RequireOne<DayNightShopLoopController>("DayNightShopLoopController");
        loop.SimulatePhaseForValidation(9f, 1);
    }

    static void ValidateDefaultPlayerView()
    {
        var mode = RequireOne<CoreSlicePresentationMode>("CoreSlicePresentationMode");
        Require(!mode.DevelopmentOverlaysVisible, "development overlays are hidden by default");

        Require(GameClock.Instance != null, "GameClock exists");
        Require(EconomyService.Instance != null, "EconomyService exists");
        Require(Inventory.instance != null, "Inventory exists");
        Require(MoneyHUD.instance != null, "MoneyHUD exists");
        Require(ClockHUD.instance != null, "ClockHUD exists");
        Require(InteractPromptUI.instance != null, "InteractPromptUI exists");
        Require(ShopPriceUI.instance != null, "ShopPriceUI exists");
        Require(Object.FindFirstObjectByType<PlayableDayScenarioController>() != null, "Day 1 objective controller exists");
        Require(Object.FindFirstObjectByType<DayNightShopLoopController>() != null, "Day/Night loop controller exists");

        RequireUiVisible("MoneyHudPanel", "money/tier HUD is visible");
        RequireUiVisible("ClockHudPanel", "clock/date HUD is visible");
        RequireUiVisible("PlayableDayGuideCanvas", "Day 1 objective HUD is visible");
        RequireUiVisible("DayNightShopLoopCanvas", "Day/Night HUD is visible");
        RequireUiVisible("DayNightShopLoopPanel", "Day/Night panel is visible");

        RequireOptionalUiExists("InteractPromptPanel", "interaction prompt panel exists even when hidden until needed");
        RequireOptionalUiExists("ShopPriceUI_Canvas", "ShopPriceUI canvas exists even when closed");

        foreach (string canvasName in DevelopmentCanvasNames)
            RequireCanvasHidden(canvasName);

        int hiddenMarkers = CountHiddenDevelopmentWorldMarkers();
        Debug.Log($"PA Core Slice Check OK: hidden development world markers={hiddenMarkers}");

        var dayNightPanel = RequireSceneObject("DayNightShopLoopPanel");
        var objectiveText = RequireSceneObject("ObjectiveText");
        Require(!ScreenRectsOverlap(dayNightPanel, objectiveText), "Day/Night HUD does not overlap objective text");

        Debug.Log("PA Core Slice default player view passed.");
    }

    static void ValidateDevelopmentToggleOn()
    {
        var mode = RequireOne<CoreSlicePresentationMode>("CoreSlicePresentationMode");
        mode.SetDevelopmentOverlaysVisible(true);

        Require(mode.DevelopmentOverlaysVisible, "development overlay toggle-on state is recorded");
        foreach (string canvasName in DevelopmentCanvasNames)
            RequireCanvasShown(canvasName);

        Debug.Log("PA Core Slice development overlay toggle-on passed.");
    }

    static void ValidateDevelopmentToggleOff()
    {
        var mode = RequireOne<CoreSlicePresentationMode>("CoreSlicePresentationMode");
        mode.SetDevelopmentOverlaysVisible(false);

        Require(!mode.DevelopmentOverlaysVisible, "development overlay toggle-off state is recorded");
        foreach (string canvasName in DevelopmentCanvasNames)
            RequireCanvasHidden(canvasName);

        Debug.Log("PA Core Slice development overlay toggle-off passed.");
    }

    static void Capture(string fileName)
    {
        try
        {
            Directory.CreateDirectory(_outputDir);
            string file = Path.Combine(_outputDir, fileName + ".png");
            ScreenCapture.CaptureScreenshot(file);
            Debug.Log($"PA Core Slice screenshot requested: {file}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"PA Core Slice screenshot skipped: {ex.Message}");
        }
    }

    static void RequireCanvasHidden(string name)
    {
        var go = RequireSceneObject(name);
        var group = go.GetComponent<CanvasGroup>();
        Require(group != null, $"{name} has CanvasGroup for presentation filtering");
        Require(group.alpha <= 0.01f, $"{name} is visually hidden");
        Require(!group.interactable, $"{name} is not interactable while hidden");
        Require(!group.blocksRaycasts, $"{name} does not block player UI while hidden");
    }

    static void RequireCanvasShown(string name)
    {
        var go = RequireSceneObject(name);
        var group = go.GetComponent<CanvasGroup>();
        Require(group != null, $"{name} has CanvasGroup for presentation filtering");
        Require(group.alpha >= 0.99f, $"{name} is restored by development toggle");
    }

    static void RequireUiVisible(string name, string message)
    {
        var go = RequireSceneObject(name);
        Require(go.activeInHierarchy, $"{message}: active in hierarchy");
        Require(EffectiveCanvasAlpha(go.transform) > 0.5f, $"{message}: canvas alpha visible");
    }

    static void RequireOptionalUiExists(string name, string message)
    {
        RequireSceneObject(name);
        Debug.Log($"PA Core Slice Check OK: {message}");
    }

    static int CountHiddenDevelopmentWorldMarkers()
    {
        int count = 0;
        foreach (var transform in FindSceneTransforms())
        {
            if (!IsDevelopmentWorldObject(transform.name)) continue;
            count++;

            foreach (var renderer in transform.GetComponentsInChildren<Renderer>(true))
                Require(!renderer.enabled, $"{transform.name} renderer hidden by default");

            foreach (var text in transform.GetComponentsInChildren<TMP_Text>(true))
                Require(!text.enabled, $"{transform.name} TMP text hidden by default");

            foreach (var graphic in transform.GetComponentsInChildren<Graphic>(true))
                Require(!graphic.enabled, $"{transform.name} UI graphic hidden by default");

            foreach (var collider in transform.GetComponentsInChildren<Collider>(true))
                Require(!collider.enabled, $"{transform.name} collider hidden by default");
        }

        return count;
    }

    static bool IsDevelopmentWorldObject(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;

        foreach (string exact in DevelopmentWorldNames)
            if (objectName == exact) return true;

        foreach (string prefix in DevelopmentWorldPrefixes)
            if (objectName.StartsWith(prefix, StringComparison.Ordinal)) return true;

        return false;
    }

    static bool ScreenRectsOverlap(GameObject a, GameObject b)
    {
        var aRect = a.GetComponent<RectTransform>();
        var bRect = b.GetComponent<RectTransform>();
        if (aRect == null || bRect == null) return false;

        return GetScreenRect(aRect).Overlaps(GetScreenRect(bRect));
    }

    static Rect GetScreenRect(RectTransform rt)
    {
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float minX = corners[0].x;
        float maxX = corners[0].x;
        float minY = corners[0].y;
        float maxY = corners[0].y;

        for (int i = 1; i < corners.Length; i++)
        {
            minX = Mathf.Min(minX, corners[i].x);
            maxX = Mathf.Max(maxX, corners[i].x);
            minY = Mathf.Min(minY, corners[i].y);
            maxY = Mathf.Max(maxY, corners[i].y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    static float EffectiveCanvasAlpha(Transform transform)
    {
        float alpha = 1f;
        var current = transform;
        while (current != null)
        {
            var group = current.GetComponent<CanvasGroup>();
            if (group != null)
                alpha *= group.alpha;

            current = current.parent;
        }

        return alpha;
    }

    static GameObject RequireSceneObject(string name)
    {
        foreach (var transform in FindSceneTransforms())
            if (transform.name == name)
                return transform.gameObject;

        throw new InvalidOperationException($"{name} not found in loaded scene.");
    }

    static IEnumerable<Transform> FindSceneTransforms()
    {
        var activeScene = SceneManager.GetActiveScene();
        foreach (var transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform == null) continue;
            if (!transform.gameObject.scene.IsValid()) continue;
            if (transform.gameObject.scene != activeScene) continue;
            yield return transform;
        }
    }

    static T RequireOne<T>(string label) where T : Object
    {
        foreach (var obj in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (obj != null) return obj;

        throw new InvalidOperationException($"{label} not found.");
    }

    static string CreateOutputDirectory()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "CoreSlicePlayability"));
        string dir = Path.Combine(root, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);

        Debug.Log($"PA Core Slice Check OK: {message}");
    }

    static void MarkFailedAndExitPlayMode()
    {
        _hadError = true;
        SessionState.SetBool(HadErrorKey, true);
        _ran = true;
        SessionState.SetBool(RanKey, true);

        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();
        else
            Finish();
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
            Debug.LogError("PA Core Slice Playability Validation failed. Check log for the first failed check.");
            ExitIfBatch(1);
            return;
        }

        Debug.Log("PA Core Slice Playability Validation finished successfully.");
        ExitIfBatch(0);
    }

    static void Cleanup()
    {
        Application.logMessageReceived -= OnLogMessage;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
    }

    static void ExitIfBatch(int code)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }
}
#endif
