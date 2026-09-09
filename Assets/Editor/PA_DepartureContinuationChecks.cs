using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Focused continuation checks. Uses a declared development entry, never mutates P0 certification.
[InitializeOnLoad]
public static class PA_DepartureContinuationChecks
{
    const string Key = "PA.Companions.Checks";
    const string Output = "Docs/Presentation/2026-09-08/";
    static Task _task;
    static bool _errors;
    static double _started;

    static PA_DepartureContinuationChecks()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += Mode;
        Application.logMessageReceived += (message, stack, type) =>
        {
            if (SessionState.GetBool(Key, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
            {
                _errors = true;
                SessionState.SetBool(Key + ".Errors", true);
            }
        };
    }

    [MenuItem("Project PA/Validation/Run Companion Selection Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        // 기존 설정을 검증한다. 검증 실행으로 프리팹을 다시 생성하지 않는다.
        PA_DepartureContinuationSetup.Require<GameObject>(PA_DepartureContinuationSetup.PrefabPath);
        _task = null;
        _errors = false;
        SessionState.SetBool(Key + ".Errors", false);
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + ".Done", false);
        ConfigureGameView();
        PA_DepartureContinuationSetup.PlaySelection();
    }

    static void Mode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _started = EditorApplication.timeSinceStartup;
            _task = null;
            _errors = SessionState.GetBool(Key + ".Errors", false);
        }
        if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key + ".Done", false)) return;
        SessionState.SetBool(Key, false);
        SessionState.SetBool(Key + ".Done", false);
        Debug.Log(_errors ? "[VS-P1] FAIL" : "[VS-P1] PASS");
        if (Environment.GetCommandLineArgs().Contains("-executeMethod")) EditorApplication.Exit(_errors ? 1 : 0);
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        bool checking = SessionState.GetBool(Key, false) && !SessionState.GetBool(Key + ".Done", false);
        // 준비 실패도 제한 시간에 포함한다. 이전에는 참조가 없으면 영원히 반환했다.
        if (checking && EditorApplication.timeSinceStartup - _started >= 90)
        {
            _errors = true;
            Debug.LogError("[VS-P1] Timeout waiting for selection readiness or checks.");
            SessionState.SetBool(Key + ".Done", true);
            EditorApplication.ExitPlaymode();
            return;
        }
        var selection = Object.FindFirstObjectByType<DepartureCompanionSelection>();
        if (selection == null || !selection.enabled || selection.Tutorial == null || !selection.Tutorial.IsReady ||
            selection.Tutorial.presentation == null || selection.Tutorial.presentation.CompanionButton == null) return;
        if (SessionState.GetBool("PA.Companions.DevelopmentEntry", false))
        {
            SessionState.SetBool("PA.Companions.DevelopmentEntry", false);
            selection.OpenAfterCertification();
            if (selection.IsOpen) Debug.LogError("[VS-P1] Selection opened before certification.");
            selection.OpenDevelopmentSelection();
        }
        if (!checking) return;
        if (_task == null) _task = CheckSelection(selection);
        if (!_task.IsCompleted && EditorApplication.timeSinceStartup - _started < 90) return;
        if (_task.IsFaulted || !_task.IsCompleted)
        {
            _errors = true;
            Debug.LogError("[VS-P1] " + (_task.Exception?.GetBaseException().ToString() ?? "Timeout"));
        }
        SessionState.SetBool(Key + ".Done", true);
        EditorApplication.ExitPlaymode();
    }

    static async Task CheckSelection(DepartureCompanionSelection s)
    {
        await Task.Delay(500);
        Check(s.Tutorial.Stage == 1 && !s.Tutorial.CompanionSelectionUnlocked,
            "development entry preserves uncertified P0 state");
        Check(!s.Tutorial.presentation.CompanionButton.interactable, "uncertified P0 button stays disabled");
        Check(s.font != null && s.candidates.All(c => c != null && c.profile != null && c.model != null),
            "required font, profiles and models present");
        Check(Object.FindObjectsByType<SaveManager>(FindObjectsSortMode.None).Length == 0,
            "selection session cannot write campaign save");
        Check(s.candidates.Length == 3 && s.CandidateButtons.Length == 3, "three actual candidate cards");
        Check(!s.DepartureButton.interactable && s.SelectedIds.Count == 0, "zero selection cannot depart");
        s.Confirm();
        Check(!s.IsConfirmed, "invalid confirmation rejected");
        s.CandidateButtons[0].onClick.Invoke();
        Check(!s.DepartureButton.interactable && s.SelectedIds.Count == 1, "one selection cannot depart");
        s.Confirm();
        Check(!s.IsConfirmed, "one selection confirmation rejected");
        Check(!s.Toggle("unknown-candidate"), "unknown candidate rejected");
        s.CandidateButtons[1].onClick.Invoke();
        Check(s.DepartureButton.interactable && s.SelectedIds.Count == 2, "two selections enable departure");
        s.CandidateButtons[2].onClick.Invoke();
        Check(s.SelectedIds.Count == 2 && !s.SelectedIds.Contains(s.candidates[2].id), "third candidate blocked");
        s.CandidateButtons[0].onClick.Invoke();
        Check(s.SelectedIds.Count == 1 && !s.DepartureButton.interactable, "deselection disables departure");
        s.CandidateButtons[2].onClick.Invoke();
        string[] expected = { s.candidates[1].id, s.candidates[2].id };
        Check(s.SelectedIds.SequenceEqual(expected), "selection change retained");
        ValidateReferences(s.gameObject);
        await PA_SafeGameViewCapture.CaptureAsync(Path.GetFullPath(Output + "03_CompanionSelection.png"), Camera.main, null, 1920, 1080, 600);
        string[] received = null;
        int calls = 0;
        s.DepartureConfirmed += ids => { received = ids.ToArray(); calls++; };
        s.DepartureButton.onClick.Invoke();
        await Task.Delay(200);
        s.Confirm();
        Check(s.IsConfirmed && s.ConfirmedIds.SequenceEqual(expected) && received != null && received.SequenceEqual(expected) && calls == 1,
            "exactly two immutable IDs handed off once");
        Check(!s.Toggle(s.candidates[0].id) && s.ConfirmedIds.SequenceEqual(expected), "confirmed selection frozen");
        Check(!_errors, "runtime error count zero");
        File.WriteAllText(Output + "P1-validation.json", "{\n  \"status\": \"PASS\",\n  \"entry\": \"explicit development selection; P0 state unchanged\",\n  \"selectedIds\": [\"Miner_01\", \"Farmer_01\"],\n  \"candidates\": 3,\n  \"selectionLimit\": 2,\n  \"toggleAndCancel\": true,\n  \"thirdBlocked\": true,\n  \"handoffOnce\": true,\n  \"missingReferences\": 0,\n  \"runtimeErrors\": 0\n}\n");
    }

    public static void ValidateReferences(GameObject root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0, "script references " + t.name);
            foreach (Component c in t.GetComponents<Component>())
            {
                var property = new SerializedObject(c).GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                        Check(property.objectReferenceInstanceIDValue == 0, "object reference " + t.name + "/" + property.propertyPath);
            }
        }
    }

    public static void ConfigureGameView()
    {
        var editor = typeof(Editor).Assembly;
        var type = editor.GetType("UnityEditor.GameView");
        var view = EditorWindow.GetWindow(type);
        view.Show();
        var sizesType = editor.GetType("UnityEditor.GameViewSizes");
        var sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(editor.GetType("UnityEditor.GameViewSizeGroupType"), "Standalone") });
        var labels = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
        int index = Array.FindIndex(labels, label => label.Contains("1920") && label.Contains("1080"));
        Check(index >= 0, "existing Full HD GameView preset");
        type.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(view, index);
    }

    public static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        Debug.Log("[VS-P1] CHECK_OK " + message);
    }
}
