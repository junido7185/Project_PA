using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PA_DepartureVoyageChecks
{
    const string Key = "PA.Voyage.Checks";
    const string Output = "Docs/Presentation/2026-09-08/";
    static Task _task;
    static double _started;
    static Keyboard _keyboard;

    static PA_DepartureVoyageChecks()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode) _started = EditorApplication.timeSinceStartup;
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key + ".Done", false)) return;
            SessionState.SetBool(Key, false);
            SessionState.SetBool(Key + ".Done", false);
            bool failed = SessionState.GetBool(Key + ".Errors", false);
            Debug.Log(failed ? "[VS-P2] FAIL" : "[VS-P2] PASS");
            if (Environment.GetCommandLineArgs().Contains("-executeMethod")) EditorApplication.Exit(failed ? 1 : 0);
        };
        Application.logMessageReceived += (message, stack, type) =>
        {
            if (SessionState.GetBool(Key, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                SessionState.SetBool(Key + ".Errors", true);
        };
    }

    [MenuItem("Project PA/Validation/Run Departure Voyage Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + ".Errors", false);
        SessionState.SetBool(Key + ".Done", false);
        _task = null;
        PA_DepartureContinuationChecks.ConfigureGameView();
        PA_DepartureContinuationSetup.PlaySelection();
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool(Key, false) || SessionState.GetBool(Key + ".Done", false)) return;
        var s = Object.FindFirstObjectByType<DepartureCompanionSelection>();
        if (_task == null && s != null && s.IsOpen) _task = Checks(s);
        if ((_task == null || !_task.IsCompleted) && EditorApplication.timeSinceStartup - _started < 75) return;
        if (_task == null || !_task.IsCompleted || _task.IsFaulted)
        {
            SessionState.SetBool(Key + ".Errors", true);
            Debug.LogError("[VS-P2] " + (_task?.Exception?.GetBaseException().ToString() ?? "Timeout"));
        }
        if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
        _keyboard = null;
        SessionState.SetBool(Key + ".Done", true);
        EditorApplication.ExitPlaymode();
    }

    static async Task Checks(DepartureCompanionSelection s)
    {
        var voyage = s.GetComponent<DepartureVoyagePresentation>();
        Check(voyage != null && voyage.boatPrefab != null && voyage.treePrefab != null && voyage.birchPrefab != null &&
            voyage.rockPrefab != null && voyage.dockPrefab != null && voyage.cratePrefab != null && voyage.waterMaterial != null, "existing presentation references");
        // 발표 이미지 03과 같은 조합을 실제 선택하여 04/05의 연속성이 읽히게 한다.
        s.CandidateButtons[1].onClick.Invoke();
        s.CandidateButtons[2].onClick.Invoke();
        string[] ids = s.SelectedIds.ToArray();
        s.DepartureButton.onClick.Invoke();
        await Until(() => voyage.Sailing, "confirm starts departure");
        Check(voyage.Companions.Count == 2 && voyage.Companions.Select(n => n.name).SequenceEqual(ids.Select(id => "Companion_" + id)), "exact selected two NPC models on board");
        Check(voyage.Companions.All(n => n.GetComponentsInChildren<Renderer>().Length > 0), "companions have real rendered models");
        var passengers = voyage.Companions.ToArray();
        var player = s.Tutorial.player;
        Vector3 boatStart = voyage.Boat.position;
        Vector3 localStart = voyage.Boat.InverseTransformPoint(player.position);
        _keyboard = InputSystem.AddDevice<Keyboard>("VoyageValidationKeyboard");
        _keyboard.MakeCurrent();
        await PressKey(UnityEngine.InputSystem.Key.A, 700);
        Vector3 localMoved = voyage.Boat.InverseTransformPoint(player.position);
        Check(Mathf.Abs(localMoved.x - localStart.x) > .25f, "existing player moves on deck");
        Check(Mathf.Abs(localMoved.x) < 1.5f && Mathf.Abs(localMoved.z) < 2.3f, "deck rail bounds movement");
        await PressKey(UnityEngine.InputSystem.Key.D, 300);
        await Task.Delay(1700);
        await PA_SafeGameViewCapture.CaptureAsync(Path.GetFullPath(Output + "04_Boat_To_Island.png"), Camera.main, null, 1920, 1080, 600);
        Check(voyage.Sailing && Vector3.Distance(boatStart, voyage.Boat.position) > .5f, "unsteered voyage advances in real time");
        await Until(() => voyage.Arrived, "fade and island arrival");
        await Task.Delay(1100);
        Check(voyage.Elapsed >= 10 && voyage.Elapsed <= 15, "voyage lasts ten to fifteen seconds");
        Check(voyage.ArrivedIds.SequenceEqual(ids) && voyage.Companions.SequenceEqual(passengers), "same companion objects and IDs arrive");
        Check(passengers.All(n => n.activeInHierarchy && n.transform.parent != voyage.Boat), "companions stand on island");
        Check(voyage.IslandGrid.WorldToCell(player.position, out Vector2Int cell) && voyage.IslandGrid.TryGetCell(cell, out var ground) && ground.IsWalkable,
            "player arrived on existing dry WorldGrid cell");
        Check(voyage.IslandTerrain.IsReady && voyage.IslandTerrain.TerrainCollider.sharedMesh != null, "existing terrain mesh and collider ready");
        Check(voyage.IslandGrid.Cells.Any(c => c.HasWater) && voyage.IslandGrid.Cells.Select(c => c.ElevationLevel).Distinct().Count() >= 4, "water beach and stepped cells");
        Check(Object.FindObjectsByType<WorldGridService>(FindObjectsSortMode.None).Length == 1 && Object.FindObjectsByType<SaveManager>(FindObjectsSortMode.None).Length == 0,
            "one existing WorldGrid and no new save authority");
        foreach (Transform child in voyage.GetComponentsInChildren<Transform>(true))
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                throw new InvalidOperationException("Missing script: " + child.name);
        Vector3 arrival = player.position;
        await PressKey(UnityEngine.InputSystem.Key.W, 700);
        Check(Vector3.Distance(player.position, arrival) > .4f, "existing player explores island after arrival");
        await PA_SafeGameViewCapture.CaptureAsync(Path.GetFullPath(Output + "05_Island_FirstArrival.png"), Camera.main, null, 1920, 1080, 600);
        Check(!SessionState.GetBool(Key + ".Errors", false), "blocking Console error zero");
        File.WriteAllText(Output + "P2-validation.json", JsonUtility.ToJson(new Evidence { status = "PASS", ids = ids, seconds = voyage.Elapsed,
            sameCompanionObjects = true, deckMovement = true, islandMovement = true, existingGrid = true, runtimeErrors = 0 }, true));
    }

    static async Task PressKey(UnityEngine.InputSystem.Key key, int milliseconds)
    {
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
        await Task.Delay(milliseconds);
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        await Task.Delay(150);
    }

    static async Task Until(Func<bool> condition, string message)
    {
        double deadline = EditorApplication.timeSinceStartup + 25;
        while (!condition() && EditorApplication.timeSinceStartup < deadline) await Task.Delay(100);
        Check(condition(), message);
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log("[VS-P2] CHECK_OK " + message);
    }

    [Serializable] class Evidence
    {
        public string status;
        public string[] ids;
        public float seconds;
        public bool sameCompanionObjects, deckMovement, islandMovement, existingGrid;
        public int runtimeErrors;
    }
}
