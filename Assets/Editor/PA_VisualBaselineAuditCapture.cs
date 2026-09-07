#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Audit-only capture route for the 2026-08-25 visual/player-experience baseline.
// It drives the existing player-facing startup buttons and uses an isolated save
// repository. It does not edit or save scenes, prefabs, or runtime assets.
[InitializeOnLoad]
public static class PA_VisualBaselineAuditCapture
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.VisualBaselineAudit.Active";
    const string EnteredKey = "PA.VisualBaselineAudit.Entered";
    const string RanKey = "PA.VisualBaselineAudit.Ran";
    const string FailedKey = "PA.VisualBaselineAudit.Failed";

    static bool _entered;
    static bool _ran;
    static bool _failed;
    static double _startedAt;
    static Task _runtimeTask;

    static PA_VisualBaselineAuditCapture()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _failed = SessionState.GetBool(FailedKey, false);
        RegisterCallbacks();
        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Run Visual Baseline Audit Capture")]
    public static void RunVisualBaselineAuditCapture()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA Visual Baseline: failed to open {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        _entered = false;
        _ran = false;
        _failed = false;
        _runtimeTask = null;
        _startedAt = EditorApplication.timeSinceStartup;
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(FailedKey, false);
        RegisterCallbacks();
        Debug.Log($"PA Visual Baseline: entering Play Mode. Output={OutputDirectory}");
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
            SessionState.SetBool(EnteredKey, true);
        }
        else if (state == PlayModeStateChange.EnteredEditMode &&
                 SessionState.GetBool(ActiveKey, false) &&
                 SessionState.GetBool(RanKey, false))
        {
            Finish();
        }
    }

    static void OnEditorUpdate()
    {
        double elapsed = EditorApplication.timeSinceStartup - _startedAt;
        if (!_entered)
        {
            if (elapsed > 60d) Fail("timed out before entering Play Mode");
            return;
        }

        if (!_ran && EditorApplication.isPlaying && _runtimeTask == null && elapsed > 2d)
        {
            _runtimeTask = RunRuntimeAuditAsync();
            return;
        }

        if (!_ran && _runtimeTask != null && _runtimeTask.IsCompleted)
        {
            if (_runtimeTask.IsFaulted)
            {
                _failed = true;
                SessionState.SetBool(FailedKey, true);
                Debug.LogError($"PA Visual Baseline failed: {_runtimeTask.Exception?.GetBaseException()}");
            }
            _ran = true;
            SessionState.SetBool(RanKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }

        if (!_ran && elapsed > 120d) Fail("timed out before completing captures");
    }

    static async Task RunRuntimeAuditAsync()
    {
        Directory.CreateDirectory(OutputDirectory);
        Time.timeScale = 1f;

        SaveManager save = RequireOne<SaveManager>("SaveManager");
        FieldInfo repositoryField = typeof(SaveManager).GetField("_repository",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(repositoryField != null, "SaveManager repository is injectable");
        repositoryField.SetValue(save, new LocalJsonSaveRepository(OutputDirectory));

        PlayableDayScenarioController scenario =
            RequireOne<PlayableDayScenarioController>("PlayableDayScenarioController");
        InvokePrivate(scenario, "BeginContinueAvailabilityCheck");
        await Task.Delay(350);

        await CaptureAsync("01_launch");

        // Title -> Name -> Briefing -> Map -> Phone -> Controls.
        for (int i = 0; i < 5; i++)
        {
            PressPrimary();
            await Task.Delay(100);
        }
        await CaptureAsync("02_controls");

        // Controls -> Supplies -> Arrival -> live Day 1.
        for (int i = 0; i < 3; i++)
        {
            PressPrimary();
            await Task.Delay(100);
        }
        Require(scenario.StartupFlowCompleted, "player-facing startup flow completed");
        await CaptureAsync("03_day_spawn");

        InventoryUI inventory = RequireOne<InventoryUI>("InventoryUI");
        if (!inventory.gameObject.activeSelf) inventory.Toggle();
        await CaptureAsync("04_inventory");
        if (inventory.gameObject.activeSelf) inventory.Toggle();

        Transform player = RequireOne<PlayerController>("PlayerController").transform;
        Vector3 savedPosition = player.position;
        await save.SaveGameAsync();
        Require(File.Exists(Path.Combine(OutputDirectory, "savegame.json")),
            "isolated save was written");
        player.position = savedPosition + new Vector3(1f, 0f, 0f);
        await save.LoadGameAsync();
        await Task.Delay(500);
        Require(Vector3.Distance(player.position, savedPosition) < 0.15f,
            "isolated save restored the player position");
        await CaptureAsync("11_save_reload");

        Debug.Log("PA Visual Baseline capture passed with isolated save and existing player UI route.");
    }

    static async Task CaptureAsync(string name)
    {
        Camera camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        Require(camera != null, "game camera exists");
        string outputPath = Path.Combine(OutputDirectory, name + ".png");
        await PA_SafeGameViewCapture.CaptureAsync(outputPath, camera, null, 1920, 1080, 800);
        Require(File.Exists(outputPath) && new FileInfo(outputPath).Length > 1024,
            $"capture generated: {name}.png");
    }

    static void PressPrimary()
    {
        GameObject go = GameObject.Find("PrimaryButton");
        Button button = go != null ? go.GetComponent<Button>() : null;
        Require(button != null && button.gameObject.activeInHierarchy && button.interactable,
            "startup primary button is available");
        button.onClick.Invoke();
    }

    static object InvokePrivate(object target, string method)
    {
        MethodInfo info = target.GetType().GetMethod(method,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(info != null, $"private audit hook resolved: {method}");
        return info.Invoke(target, null);
    }

    static T RequireOne<T>(string label) where T : Object
    {
        T value = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (value == null) throw new InvalidOperationException(label + " not found");
        return value;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log("PA Visual Baseline Check OK: " + message);
    }

    static string OutputDirectory => Path.GetFullPath(Path.Combine(
        Application.dataPath, "..", "Docs", "VisualAudit", "2026-08-25-29fb98f"));

    static void Fail(string reason)
    {
        _failed = true;
        _ran = true;
        SessionState.SetBool(FailedKey, true);
        SessionState.SetBool(RanKey, true);
        Debug.LogError("PA Visual Baseline: " + reason);
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else Finish();
    }

    static void Finish()
    {
        bool failed = _failed || SessionState.GetBool(FailedKey, false);
        bool entered = _entered || SessionState.GetBool(EnteredKey, false);
        bool ran = _ran || SessionState.GetBool(RanKey, false);
        Cleanup();
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(EnteredKey);
        SessionState.EraseBool(RanKey);
        SessionState.EraseBool(FailedKey);
        if (!entered || !ran || failed)
        {
            Debug.LogError("PA Visual Baseline audit capture failed. Check the first error.");
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log("PA Visual Baseline audit capture finished successfully.");
        EditorApplication.Exit(0);
    }

    static void Cleanup()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        _runtimeTask = null;
    }
}
#endif
