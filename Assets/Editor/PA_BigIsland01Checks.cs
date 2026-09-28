using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// BIG-ISLAND-01 only. No prior ticket validators, save writes or scene saves.
public static class PA_BigIsland01Checks
{
    const string Active = "PA.BigIsland01.Active";
    const string Output = "Logs/BIG-ISLAND-01";
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    static WorldAlphaPlayableController alpha;
    static WorldGameplayAdapterService adapter;
    static WorldPersistenceService persistence;
    static WorldNavigationService navigation;
    static PlayerController player;
    static WorldStateSaveData demoSave;
    static WorldGenerationResult demo;
    static Keyboard keyboard;
    static int stage, direction, checks;
    static double stageAt;
    static float stageGameTime;
    static Vector3 movementStart, cameraStart;
    static readonly Key[][] Keys =
    {
        new[] { Key.W }, new[] { Key.S }, new[] { Key.A }, new[] { Key.D },
        new[] { Key.W, Key.A }, new[] { Key.W, Key.D },
        new[] { Key.S, Key.A }, new[] { Key.S, Key.D }
    };
    static readonly Vector2[] Directions =
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1)
    };

    [InitializeOnLoadMethod]
    static void Resume()
    {
        if (SessionState.GetBool(Active, false)) Subscribe();
        if (SessionState.GetBool(DiagnosticActive, false)) SubscribeDiagnostic();
        if (SessionState.GetBool(RecoveryActive, false)) SubscribeRecovery();
    }

    const string RecoveryActive = "PA.BigIsland01.MovementRecovery";
    const string RecoveryOutput = "Logs/BIG-ISLAND-01F2";
    static Keyboard recoveryKeyboard;
    static Vector2 recoveryInput;
    static int recoveryStage, recoveryDirection, recoveryFrame = -1, recoveryChecks;
    static float recoveryAt, measuredAt;
    static double recoveryDeadline;
    static Vector3 recoveryStart, recoveryCameraStart, releasePosition;
    static Vector2Int recoveryCell;
    static readonly float[] recoverySpeeds = new float[6];
    static int movementGroundedSamples, groundSamples, groundedSamples;
    static float minGroundY, maxGroundY, minRelativeY, maxRelativeY, minVerticalVelocity, maxVerticalVelocity;
    static float minClearance, maxClearance, firstGroundY, lastGroundY, fallingSince, fallingStartY;
    static bool fallingRun;

    public static void RunMovementRecovery()
    {
        Directory.CreateDirectory(RecoveryOutput);
        File.WriteAllText(RecoveryOutput + "/checks.txt", "Two diagonals and 0.5s grounding windows only; previous 33 and cardinal assertions are not rerun.\n");
        File.WriteAllText(RecoveryOutput + "/ground-samples.csv", "direction,elapsed,grounded,worldY,velocityY,groundY,capsuleClearance,collider\n");
        SessionState.SetBool(RecoveryActive, true);
        SessionState.SetBool(RecoveryActive + ".Failed", false);
        SessionState.SetInt(RecoveryActive + ".Errors", 0);
        SessionState.SetFloat(RecoveryActive + ".Started", (float)EditorApplication.timeSinceStartup);
        SubscribeRecovery();
        EditorSceneManager.OpenScene("Assets/Scenes/WorldSandbox.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    static void SubscribeRecovery()
    {
        EditorApplication.update -= RecoveryTick;
        EditorApplication.update += RecoveryTick;
        InputSystem.onAfterUpdate -= InjectRecoveryDynamicState;
        InputSystem.onAfterUpdate += InjectRecoveryDynamicState;
        Application.logMessageReceived -= RecoveryLog;
        Application.logMessageReceived += RecoveryLog;
        EditorApplication.playModeStateChanged -= RecoveryModeChanged;
        EditorApplication.playModeStateChanged += RecoveryModeChanged;
    }

    static void InjectRecoveryDynamicState()
    {
        if (!EditorApplication.isPlaying || recoveryKeyboard == null ||
            InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        // Editor and player have distinct input buffers. Write only the player Dynamic
        // buffer after native events, before the normal PlayerInputHandler.Update polls it.
        InputState.Change(recoveryKeyboard,
            recoveryInput == Vector2.zero ? new KeyboardState() : new KeyboardState(Keys[recoveryDirection]),
            InputUpdateType.Dynamic);
    }

    static void RecoveryTick()
    {
        if (!EditorApplication.isPlaying || recoveryStage == 99 || recoveryFrame == Time.frameCount) return;
        recoveryFrame = Time.frameCount;
        try
        {
            if (SessionState.GetInt(RecoveryActive + ".Errors", 0) != 0)
                throw new Exception("Runtime Error/Exception/Assert observed.");
            if (recoveryStage == 0)
            {
                alpha = WorldAlphaPlayableController.Instance;
                if (alpha == null || !alpha.IsReady)
                {
                    if (EditorApplication.timeSinceStartup - SessionState.GetFloat(RecoveryActive + ".Started", 0) > 60)
                        throw new Exception("Movement prerequisites did not become ready in 60 seconds.");
                    return;
                }
                player = alpha.Adapter.PlayerRoot.GetComponent<PlayerController>();
                RecoveryCheck(player != null && player.isActiveAndEnabled && PlayerInputHandler.Instance != null &&
                    PlayerInputHandler.Instance.isActiveAndEnabled, "existing input and movement authorities are active");
                var generated = WorldPersistenceService.Instance.ActiveGeneratedWorld;
                if (!generated.TryGetAnchor(WorldGenerationAnchorKind.Start, out var anchor))
                    throw new Exception("Existing movement start anchor unavailable.");
                recoveryCell = anchor.Coordinate + new Vector2Int(-5, -4);
                recoveryDirection = 4; // Only forward-left and forward-right remain.
                recoveryKeyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
                BeginRecoveryDirection();
                return;
            }
            if (EditorApplication.timeSinceStartup > recoveryDeadline)
                throw new Exception($"Movement stage {recoveryStage} timed out; expected {recoveryInput}, observed {PlayerInputHandler.Instance?.MoveInput}.");
            GroundCheck();
            var input = PlayerInputHandler.Instance.MoveInput;
            if (recoveryStage == 1)
            {
                if (input != recoveryInput) return;
                RecoveryCheck(input == recoveryInput, $"direction={recoveryDirection + 1} MoveInput={input} received before measurement");
                // Let the existing acceleration/rotation settle before comparing speeds.
                RecoveryStage(2);
                return;
            }
            if (recoveryStage == 2)
            {
                if (Time.time - recoveryAt < .3f) return;
                recoveryStart = player.transform.position;
                recoveryCameraStart = Camera.main.transform.position;
                measuredAt = Time.time;
                RecoveryStage(3);
                return;
            }
            if (recoveryStage == 3)
            {
                if (input != recoveryInput) throw new Exception("MoveInput lost the held Dynamic input.");
                if (player.GetComponent<CharacterController>().isGrounded) movementGroundedSamples++;
                if (Time.time - measuredAt < .4f) return;
                Vector3 delta = Vector3.ProjectOnPlane(player.transform.position - recoveryStart, Vector3.up);
                Vector3 expected = new Vector3(recoveryInput.x, 0, recoveryInput.y).normalized;
                float speed = delta.magnitude / (Time.time - measuredAt);
                recoverySpeeds[recoveryDirection] = speed;
                RecoveryCheck(delta.magnitude > .1f && Vector3.Dot(delta.normalized, expected) > .85f,
                    $"direction={recoveryDirection + 1} MoveInput={input} displacement={delta.magnitude:F4}m speed={speed:F4}m/s ActualPlanarSpeed={player.ActualPlanarSpeed:F4}");
                var camera = Camera.main;
                RecoveryCheck(!camera.orthographic && camera.GetComponent<CameraController>().target == player.transform &&
                    Vector3.Distance(camera.transform.position, recoveryCameraStart) > .1f,
                    "perspective CameraController follows actual player movement");
                recoveryInput = Vector2.zero;
                releasePosition = player.transform.position;
                RecoveryStage(4);
                return;
            }
            if (recoveryStage == 4)
            {
                if (input != Vector2.zero) return;
                RecoveryCheck(input == Vector2.zero, "release observed: MoveInput=(0,0)");
                BeginGroundWindow();
                RecoveryStage(5);
                return;
            }
            if (recoveryStage == 5)
            {
                ObserveGroundWindow();
                if (Time.time - recoveryAt < .5f) return;
                RecoveryCheck(input == Vector2.zero && player.ActualPlanarSpeed < .1f,
                    $"stopped during 0.5s grounding window; speed={player.ActualPlanarSpeed:F5} releaseTravel={Vector3.ProjectOnPlane(player.transform.position - releasePosition, Vector3.up).magnitude:F4}m");
                RecoveryCheck((movementGroundedSamples > 0 || groundedSamples > 0) && groundSamples >= 3 &&
                    maxRelativeY - minRelativeY <= .1f && firstGroundY - lastGroundY <= .05f,
                    $"grounding direction={recoveryDirection + 1}: window={Time.time - recoveryAt:F4}s groundedTrue={groundedSamples}/{groundSamples} " +
                    $"movementGrounded={movementGroundedSamples} Y=[{minGroundY:F5},{maxGroundY:F5}] " +
                    $"relativeY=[{minRelativeY:F5},{maxRelativeY:F5}] velocityY=[{minVerticalVelocity:F5},{maxVerticalVelocity:F5}] " +
                    $"capsuleClearance=[{minClearance:F5},{maxClearance:F5}] groundProximity=allSamples penetration=false sustainedFalling=false");
                if (++recoveryDirection < 6) { BeginRecoveryDirection(); return; }
                float ratio = recoverySpeeds[4] / recoverySpeeds[5];
                RecoveryCheck(ratio > .9f && ratio < 1.1f,
                    $"two diagonal speeds approximately equal: {recoverySpeeds[4]:F4}, {recoverySpeeds[5]:F4}m/s ratio={ratio:F4}");
                RecoveryCheck(recoverySpeeds[4] <= 5f * 1.05f && recoverySpeeds[5] <= 5f * 1.05f,
                    "diagonals are no more than 5% faster than accepted cardinal baseline 5.00m/s");
                FinishRecovery(false, "Diagonals and physical grounding passed; combine with previous 33 assertions and BIG-ISLAND-01F cardinal evidence.");
            }
        }
        catch (Exception ex) { FinishRecovery(true, ex.ToString()); }
    }

    static void BeginRecoveryDirection()
    {
        if (!alpha.MovePlayerToCellForValidation(recoveryCell)) throw new Exception("Movement start cell unavailable.");
        player.ResetMotionAfterTeleport();
        movementGroundedSamples = 0;
        recoveryInput = Directions[recoveryDirection];
        RecoveryStage(1);
    }
    static void RecoveryStage(int value) { recoveryStage = value; recoveryAt = Time.time; recoveryDeadline = EditorApplication.timeSinceStartup + 10; }

    static void BeginGroundWindow()
    {
        groundSamples = groundedSamples = 0;
        minGroundY = minRelativeY = minVerticalVelocity = minClearance = float.PositiveInfinity;
        maxGroundY = maxRelativeY = maxVerticalVelocity = maxClearance = float.NegativeInfinity;
        firstGroundY = lastGroundY = player.transform.position.y;
        fallingRun = false;
    }

    static void ObserveGroundWindow()
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        Bounds capsule = cc.bounds;
        float tolerance = Mathf.Max(.1f, cc.skinWidth + .04f);
        var hits = Physics.RaycastAll(capsule.center, Vector3.down, capsule.extents.y + .3f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            .Where(hit => hit.collider != cc && !hit.collider.transform.IsChildOf(player.transform) && hit.normal.y > .5f)
            .OrderBy(hit => hit.distance).ToArray();
        if (hits.Length == 0)
            throw new Exception("B-runtime grounding: no ground collider within 0.3m below capsule bounds.");
        RaycastHit ground = hits[0];
        float clearance = capsule.min.y - ground.point.y;
        if (clearance > tolerance || clearance < -tolerance)
            throw new Exception($"B-runtime grounding: capsule separation/penetration={clearance:F5}m exceeds skin tolerance={tolerance:F5}m; collider={ground.collider.name}.");
        float y = player.transform.position.y;
        float relativeY = y - ground.point.y;
        if (y < lastGroundY - .0001f)
        {
            if (!fallingRun) { fallingRun = true; fallingSince = Time.time; fallingStartY = lastGroundY; }
            if (Time.time - fallingSince > .15f && fallingStartY - y > .05f)
                throw new Exception($"B-runtime grounding: sustained falling {fallingStartY - y:F5}m over {Time.time - fallingSince:F4}s.");
        }
        else fallingRun = false;
        lastGroundY = y;
        groundSamples++;
        if (cc.isGrounded) groundedSamples++;
        minGroundY = Mathf.Min(minGroundY, y); maxGroundY = Mathf.Max(maxGroundY, y);
        minRelativeY = Mathf.Min(minRelativeY, relativeY); maxRelativeY = Mathf.Max(maxRelativeY, relativeY);
        minVerticalVelocity = Mathf.Min(minVerticalVelocity, cc.velocity.y); maxVerticalVelocity = Mathf.Max(maxVerticalVelocity, cc.velocity.y);
        minClearance = Mathf.Min(minClearance, clearance); maxClearance = Mathf.Max(maxClearance, clearance);
        File.AppendAllText(RecoveryOutput + "/ground-samples.csv",
            FormattableString.Invariant($"{recoveryDirection + 1},{Time.time - recoveryAt:F5},{cc.isGrounded},{y:F5},{cc.velocity.y:F5},{ground.point.y:F5},{clearance:F5},{ground.collider.name}\n"));
        if (maxRelativeY - minRelativeY > .1f || firstGroundY - y > .05f)
            throw new Exception($"B-runtime grounding: unstable Y relative to surface; relativeRange={maxRelativeY - minRelativeY:F5}m drop={firstGroundY - y:F5}m.");
    }
    static void RecoveryCheck(bool valid, string text)
    {
        if (!valid) throw new Exception(text);
        recoveryChecks++;
        File.AppendAllText(RecoveryOutput + "/checks.txt", "PASS " + text + "\n");
    }
    static void RecoveryLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        SessionState.SetInt(RecoveryActive + ".Errors", SessionState.GetInt(RecoveryActive + ".Errors", 0) + 1);
        File.AppendAllText(RecoveryOutput + "/checks.txt", $"{type}: {message}\n{stack}\n");
    }
    static void FinishRecovery(bool failed, string reason)
    {
        recoveryStage = 99;
        recoveryInput = Vector2.zero;
        SessionState.SetBool(RecoveryActive + ".Failed", failed);
        File.WriteAllText(RecoveryOutput + "/result.txt", $"BIG-ISLAND-01F2 {(failed ? "FAIL" : "PASS")}\nChecks={recoveryChecks}\nPlay sessions=1\n{reason}\n");
        EditorApplication.update -= RecoveryTick;
        EditorApplication.ExitPlaymode();
    }
    static void RecoveryModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(RecoveryActive, false)) return;
        int errors = SessionState.GetInt(RecoveryActive + ".Errors", 0);
        bool failed = SessionState.GetBool(RecoveryActive + ".Failed", false) || errors != 0;
        File.AppendAllText(RecoveryOutput + "/result.txt", $"Runtime errors/exceptions/asserts={errors}\n" +
            (failed ? "BIG-ISLAND-01 remains incomplete\n" : "BIG-ISLAND-01 FINAL PASS (original 33 + 01F cardinal + 01F2 diagonal/grounding evidence)\n"));
        SessionState.SetBool(RecoveryActive, false);
        InputSystem.onAfterUpdate -= InjectRecoveryDynamicState;
        Application.logMessageReceived -= RecoveryLog;
        EditorApplication.playModeStateChanged -= RecoveryModeChanged;
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    const string DiagnosticActive = "PA.BigIsland01.MovementDiagnostic";
    const string DiagnosticOutput = "Logs/BIG-ISLAND-01F-Diagnostic";
    static bool diagnosticInjected, diagnosticFinished;
    static int diagnosticFrame = -1, diagnosticStartFrame;
    static double diagnosticStarted;
    static float diagnosticGameTime;
    static Vector3 diagnosticPosition;

    // A separate entry point intentionally bypasses every previous world/save assertion.
    public static void RunMovementDiagnostic()
    {
        Directory.CreateDirectory(DiagnosticOutput);
        File.WriteAllText(DiagnosticOutput + "/states.txt", "One W attempt; observe only, no cause correction.\n");
        SessionState.SetBool(DiagnosticActive, true);
        SessionState.SetInt(DiagnosticActive + ".Errors", 0);
        SessionState.SetFloat(DiagnosticActive + ".Started", (float)EditorApplication.timeSinceStartup);
        SubscribeDiagnostic();
        EditorSceneManager.OpenScene("Assets/Scenes/WorldSandbox.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    static void SubscribeDiagnostic()
    {
        EditorApplication.update -= DiagnosticTick;
        EditorApplication.update += DiagnosticTick;
        EditorApplication.playModeStateChanged -= DiagnosticModeChanged;
        EditorApplication.playModeStateChanged += DiagnosticModeChanged;
        Application.logMessageReceived -= DiagnosticLog;
        Application.logMessageReceived += DiagnosticLog;
        InputSystem.onAfterUpdate -= DiagnosticInputUpdate;
        InputSystem.onAfterUpdate += DiagnosticInputUpdate;
    }

    static void DiagnosticModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(DiagnosticActive, false)) return;
        int errors = SessionState.GetInt(DiagnosticActive + ".Errors", 0);
        File.AppendAllText(DiagnosticOutput + "/result.txt", $"Runtime errors/exceptions/asserts={errors}\n");
        SessionState.SetBool(DiagnosticActive, false);
        EditorApplication.update -= DiagnosticTick;
        EditorApplication.playModeStateChanged -= DiagnosticModeChanged;
        Application.logMessageReceived -= DiagnosticLog;
        InputSystem.onAfterUpdate -= DiagnosticInputUpdate;
        if (Application.isBatchMode) EditorApplication.Exit(errors == 0 ? 0 : 1);
    }

    static void DiagnosticLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        SessionState.SetInt(DiagnosticActive + ".Errors", SessionState.GetInt(DiagnosticActive + ".Errors", 0) + 1);
        File.AppendAllText(DiagnosticOutput + "/states.txt", $"{type}: {message}\n{stack}\n");
    }

    static void DiagnosticInputUpdate()
    {
        if (diagnosticInjected && !diagnosticFinished && EditorApplication.isPlaying)
            DiagnosticState("input-after-" + InputState.currentUpdateType);
    }

    static string DiagnosticState(string phase)
    {
        var handler = PlayerInputHandler.Instance;
        var cc = player != null ? player.GetComponent<CharacterController>() : null;
        string state = $"{phase} frame={Time.frameCount} time={Time.time:F4} deltaTime={Time.deltaTime:F4} " +
            $"injected={(diagnosticInjected ? "(0,1)" : "none")} " +
            $"keyboardId={keyboard?.deviceId} currentKeyboardId={Keyboard.current?.deviceId} " +
            $"keyboardEnabled={keyboard?.enabled} W={keyboard?.wKey.isPressed} currentW={Keyboard.current?.wKey.isPressed} " +
            $"handlerExists={handler != null} handlerEnabled={handler?.isActiveAndEnabled} MoveInput={handler?.MoveInput} " +
            $"InventoryActive={InventoryUI.instance != null && InventoryUI.instance.gameObject.activeSelf} " +
            $"SmartphoneOpen={SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen} " +
            $"ShopPriceOpen={ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen} " +
            $"PlayerEnabled={player?.enabled} PlayerActive={player?.gameObject.activeInHierarchy} " +
            $"CharacterEnabled={cc?.enabled} isSitting={player?.isSitting} " +
            $"position={player?.transform.position.ToString("F5")} ActualPlanarSpeed={player?.ActualPlanarSpeed:F5} " +
            $"grounded={cc?.isGrounded} collisionFlags={cc?.collisionFlags} " +
            $"timeScale={Time.timeScale} HasStartedBeta={alpha?.HasStartedBeta} StartPromptVisible={alpha?.StartPromptVisible} " +
            $"focused={Application.isFocused} inputUpdateMode={InputSystem.settings.updateMode}";
        File.AppendAllText(DiagnosticOutput + "/states.txt", state + "\n");
        return state;
    }

    static void DiagnosticTick()
    {
        if (!EditorApplication.isPlaying || diagnosticFinished) return;
        try
        {
            if (SessionState.GetInt(DiagnosticActive + ".Errors", 0) > 0)
            { FinishDiagnostic("INCOMPLETE: runtime error observed; see states.txt."); return; }
            if (!diagnosticInjected)
            {
                alpha = WorldAlphaPlayableController.Instance;
                if (alpha == null || !alpha.IsReady)
                {
                    if (EditorApplication.timeSinceStartup - SessionState.GetFloat(DiagnosticActive + ".Started", 0) > 60)
                        FinishDiagnostic("INCOMPLETE: bootstrap did not become ready within 60 seconds.");
                    return;
                }
                player = alpha.Adapter.PlayerRoot.GetComponent<PlayerController>();
                var generated = WorldPersistenceService.Instance.ActiveGeneratedWorld;
                if (!generated.TryGetAnchor(WorldGenerationAnchorKind.Start, out var anchor) ||
                    !alpha.MovePlayerToCellForValidation(anchor.Coordinate + new Vector2Int(-5, -4)))
                    throw new Exception("Original movement starting cell unavailable.");
                player.ResetMotionAfterTeleport();
                keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
                DiagnosticState("before");
                diagnosticPosition = player.transform.position;
                diagnosticStarted = EditorApplication.timeSinceStartup;
                diagnosticGameTime = Time.time;
                diagnosticStartFrame = Time.frameCount;
                diagnosticInjected = true;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                DiagnosticState("queued-W-from-EditorApplication.update");
                return;
            }
            if (diagnosticFrame == Time.frameCount) return;
            diagnosticFrame = Time.frameCount;
            string state = DiagnosticState("during");
            float distance = Vector3.ProjectOnPlane(player.transform.position - diagnosticPosition, Vector3.up).magnitude;
            if (distance > .01f)
            { FinishDiagnostic($"A: movement occurred; original timing/injection instability. planarDistance={distance:F5}\n{state}"); return; }
            // Allow the same 0.45 seconds as the failed check before interpreting a mismatch.
            if ((Time.time - diagnosticGameTime < .45f || Time.frameCount - diagnosticStartFrame < 3) &&
                EditorApplication.timeSinceStartup - diagnosticStarted < 2) return;
            var handler = PlayerInputHandler.Instance;
            string classification = handler == null
                ? "B: PlayerInputHandler.Instance is missing; PlayerController reads zero when it is absent."
                : handler.MoveInput != Vector2.up
                    ? $"A: queued W=(0,1) differs from PlayerInputHandler.MoveInput={handler.MoveInput}."
                    : "B: PlayerInputHandler received W=(0,1), but the player did not move; captured blocker states follow.";
            FinishDiagnostic($"{classification}\nplanarDistance={distance:F5}\nbeforePosition={diagnosticPosition:F5}\n{state}");
        }
        catch (Exception ex) { FinishDiagnostic("INCOMPLETE: " + ex); }
    }

    static void FinishDiagnostic(string result)
    {
        diagnosticFinished = true;
        if (player != null) DiagnosticState("after-attempt");
        File.WriteAllText(DiagnosticOutput + "/result.txt", "BIG-ISLAND-01F DIAGNOSTIC\nPlay sessions=1\n" + result + "\n");
        if (keyboard != null) InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        EditorApplication.update -= DiagnosticTick;
        InputSystem.onAfterUpdate -= DiagnosticInputUpdate;
        EditorApplication.ExitPlaymode();
    }

    public static void Run()
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/checks.txt", "BIG-ISLAND-01\n");
        File.WriteAllText(Output + "/save-hashes-before.txt", SaveHashes());
        SessionState.SetBool(Active, true);
        SessionState.SetInt(Active + ".Errors", 0);
        SessionState.SetBool(Active + ".Failed", false);
        SessionState.SetFloat(Active + ".Started", (float)EditorApplication.timeSinceStartup);
        Subscribe();
        try
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/WorldSandbox.unity", OpenSceneMode.Single);
            Check(scene.IsValid() && !scene.isDirty, "saved WorldSandbox opened without scene edits");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex) { Fail(ex); }
    }

    static void Subscribe()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged -= ModeChanged;
        EditorApplication.playModeStateChanged += ModeChanged;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    static void ModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Active, false))
        {
            bool unchanged = File.ReadAllText(Output + "/save-hashes-before.txt") == SaveHashes();
            File.AppendAllText(Output + "/result.txt", $"Save bytes unchanged={unchanged}\n");
            bool failed = SessionState.GetBool(Active + ".Failed", false) || !unchanged ||
                          SessionState.GetInt(Active + ".Errors", 0) != 0;
            SessionState.SetBool(Active, false);
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= ModeChanged;
            Application.logMessageReceived -= OnLog;
            if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
        }
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying ||
            SessionState.GetBool(Active + ".Failed", false)) return;
        try
        {
            if (SessionState.GetInt(Active + ".Errors", 0) != 0)
                throw new Exception("Runtime Error/Exception/Assert observed; see unity.log.");
            if (stage > 0 && EditorApplication.timeSinceStartup - stageAt > 60)
                throw new Exception($"Stage {stage} exceeded 60s; STOP without performance fallback.");
            if (stage == 0)
            {
                alpha = WorldAlphaPlayableController.Instance;
                adapter = WorldGameplayAdapterService.Instance;
                persistence = WorldPersistenceService.Instance;
                if (alpha == null || !alpha.IsReady)
                {
                    if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Active + ".Started", 0) > 120)
                        throw new Exception("WorldSandbox bootstrap exceeded 120s; no fallback or second Play permitted.");
                    return;
                }
                navigation = alpha.GetComponent<WorldNavigationService>();
                if (navigation.IsRebuilding) return;
                typeof(SaveManager).GetMethod("SetRepositoryForValidation", PrivateInstance)
                    .Invoke(adapter.RuntimeSaveManager, new object[] { new LocalJsonSaveRepository(Output + "/isolated-save") });
                player = adapter.PlayerRoot.GetComponent<PlayerController>();
                demo = persistence.ActiveGeneratedWorld;
                Evidence($"Bootstrap including Play entry={EditorApplication.timeSinceStartup - SessionState.GetFloat(Active + ".Started", 0):F3}s; navigation={navigation.InitialBuildMilliseconds}ms; sectors={navigation.SectorCount}");
                ValidateDefinitionAndContent();
                ValidateSaveCandidates();
                keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
                StartDirection();
                return;
            }
            if (stage == 1)
            {
                GroundCheck();
                if (Time.time - stageGameTime < .45f) return;
                Vector3 delta = Vector3.ProjectOnPlane(player.transform.position - movementStart, Vector3.up);
                Vector3 expected = new Vector3(Directions[direction].x, 0, Directions[direction].y).normalized;
                Check(delta.magnitude > .3f && Vector3.Dot(delta.normalized, expected) > .8f,
                    $"actual movement direction {direction + 1}/8 delta={delta}");
                Check(Vector3.Distance(Camera.main.transform.position, cameraStart) > .1f, "perspective camera follows moving player");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                SetStage(2);
                return;
            }
            if (stage == 2)
            {
                GroundCheck();
                if (Time.time - stageGameTime < .3f) return;
                Check(player.ActualPlanarSpeed < .1f, "movement stops promptly after input release");
                direction++;
                if (direction < Keys.Length) { StartDirection(); return; }
                ScreenCapture.CaptureScreenshot(Output + "/worldsandbox.png");
                Restore(demoSave);
                SetStage(3);
                return;
            }
            if (stage == 3)
            {
                if (navigation.IsRebuilding || Time.time - stageGameTime < .5f) return;
                Check(persistence.ActiveGeneratedWorld.GenerationVersion == 2 && alpha.Grid.Definition.Width == 256,
                    "v2 capture restores its actual 256 definition");
                CheckSharedAuthority();
                WorldGenerationResult legacy = WorldIslandGenerator.Generate(demo.Seed);
                Restore(Payload(legacy));
                SetStage(4);
                return;
            }
            if (stage == 4)
            {
                if (navigation.IsRebuilding || Time.time - stageGameTime < .5f) return;
                Check(persistence.ActiveGeneratedWorld.GenerationVersion == 1 && alpha.Grid.Definition.Width == 128,
                    "same-seed v1 restore retains 128 definition");
                CheckSharedAuthority();
                Restore(demoSave);
                SetStage(5);
                return;
            }
            if (stage == 5)
            {
                if (navigation.IsRebuilding || Time.time - stageGameTime < .5f) return;
                CheckSharedAuthority();
                Check(alpha.Grid.Definition.Width == 256 && !Camera.main.orthographic,
                    "same-seed return to Demo256 retains perspective framing");
                Check(!SceneManager.GetActiveScene().isDirty, "WorldSandbox scene remains clean");
                Check(SessionState.GetInt(Active + ".Errors", 0) == 0, "runtime errors/exceptions/asserts=0");
                Finish(false, "");
            }
        }
        catch (Exception ex) { Fail(ex); }
    }

    static void ValidateDefinitionAndContent()
    {
        Check(demo != null && demo.GenerationVersion == 2 && demo.Definition.Width == 256 && demo.Definition.Height == 256,
            "active profile=Demo256 generation-v2");
        Check(demo.Definition.CellSize == 2f && demo.Definition.ChunkSize == 16 && navigation.SectorCount == 64,
            "512m square definition uses existing 2m cells, 16-cell chunks and 64 navigation sectors");
        Check(alpha.IslandView.GeneratedChunkCount == 256 && Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Any(r => r.enabled),
            "generated terrain has 256 projected chunks and enabled renderers");
        Check(WorldGenerationConnectivity.CanReachAllAnchors(demo, out int reachable), $"all generated anchors reachable; dry reachable cells={reachable}");
        foreach (WorldGenerationAnchorKind kind in Enum.GetValues(typeof(WorldGenerationAnchorKind)))
        {
            Check(demo.TryGetAnchor(kind, out var anchor) && demo.TryGetCell(anchor.Coordinate, out var cell) && cell.IsDryLand,
                $"{kind} anchor is dry land");
        }
        foreach (WorldBiomeType biome in new[] { WorldBiomeType.Forest, WorldBiomeType.Highland, WorldBiomeType.Meadow, WorldBiomeType.Coast })
            Check(demo.Cells.Count(c => c.IsDryLand && c.Biome == biome) > 100, $"{biome} covers substantial land");
        WorldGenerationResult legacy = WorldIslandGenerator.Generate(demo.Seed);
        Check(legacy.GenerationVersion == 1 && legacy.Definition.Width == 128, "default generator remains generation-v1/128");
        int[] counts = { 6, 8, 6, 4 };
        foreach (WorldResourceKind kind in Enum.GetValues(typeof(WorldResourceKind)))
        {
            int oldCount = legacy.ResourceSpawns.Count(s => s.Kind == kind);
            int newCount = demo.ResourceSpawns.Count(s => s.Kind == kind);
            Check(oldCount == counts[(int)kind] && newCount == oldCount * 4, $"{kind} resource records {oldCount} -> {newCount}");
        }
        Check(WorldIslandGenerator.Generate(demo.Seed, WorldIslandGenerationSettings.Demo256).Checksum == demo.Checksum,
            "Demo256 deterministic checksum");
        Check(legacy.ResourceSpawns.All(s => s.SpawnKey.StartsWith("g1:")), "legacy resource identities remain g1");
        Check(!Camera.main.orthographic && Mathf.Abs(Camera.main.fieldOfView - 34f) < .01f &&
              Mathf.Abs(Camera.main.transform.eulerAngles.x - 40f) < .1f &&
              Camera.main.GetComponent<CameraController>().OpeningDistance == 12.5f,
            "camera perspective distance=12.5 pitch=40 FOV=34");
        Check((float)typeof(PlayerController).GetField("acceleration", PrivateInstance).GetValue(player) == 30f &&
              (float)typeof(PlayerController).GetField("deceleration", PrivateInstance).GetValue(player) == 36f &&
              (float)typeof(PlayerController).GetField("rotationSpeed", PrivateInstance).GetValue(player) == 20f,
            "existing PlayerController opening movement preset");
        GroundCheck();
        CheckSharedAuthority();
    }

    static WorldStateSaveData Payload(WorldGenerationResult result)
    {
        return new WorldStateSaveData
        {
            worldMode = WorldPersistenceMigration.ProceduralMode, worldSeed = result.Seed,
            generationVersion = result.GenerationVersion, widthCells = result.Definition.Width,
            heightCells = result.Definition.Height, cellSizeMeters = result.Definition.CellSize,
            chunkSizeCells = result.Definition.ChunkSize,
            modifiedCells = new List<WorldModifiedCellSaveData>(), placedBuildings = new List<WorldPlacedBuildingSaveData>(),
            shopFurniture = new List<WorldShopFurnitureSaveData>(), resourceStates = new List<WorldResourceStateSaveData>()
        };
    }

    static void ValidateSaveCandidates()
    {
        demoSave = persistence.CaptureState(player.transform.position, new List<PlaceableSaveData>());
        Check(demoSave.generationVersion == 2 && demoSave.widthCells == 256 && demoSave.heightCells == 256 &&
              demoSave.cellSizeMeters == 2f && demoSave.chunkSizeCells == 16, "capture records all active definition fields");
        Check(Candidate(demoSave, out var restored) && restored.Checksum == demo.Checksum, "v2 save candidate preserves base generation");
        var legacy = WorldIslandGenerator.Generate(demo.Seed);
        var legacySave = Payload(legacy);
        Check(Candidate(legacySave, out restored) && restored.Checksum == legacy.Checksum, "v1 save candidate preserves legacy generation");
        legacySave.widthCells = 256;
        Check(!Candidate(legacySave, out _), "v1/256 mismatch explicitly rejected");
        var invalid = Payload(demo);
        invalid.generationVersion = 99;
        Check(!Candidate(invalid, out _), "unknown generation version explicitly rejected");
        invalid = Payload(demo); invalid.chunkSizeCells = 32;
        Check(!Candidate(invalid, out _), "incorrect saved chunk size rejected");
    }

    static bool Candidate(WorldStateSaveData state, out WorldGenerationResult generated)
    {
        object[] args = { state, null, null, null };
        bool valid = (bool)typeof(WorldPersistenceService).GetMethod("TryBuildCandidate", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
        generated = args[1] as WorldGenerationResult;
        return valid;
    }

    static void Restore(WorldStateSaveData state)
    {
        Check(persistence.TryRestore(state, out Vector3 position, out var furniture, out string reason), "in-memory world restore: " + reason);
        adapter.RestoreRuntimeWorldState(position, furniture);
        alpha.RequestProjectionRefresh();
    }

    static void CheckSharedAuthority()
    {
        Check(ReferenceEquals(typeof(WorldGameplayAdapterService).GetField("_generated", PrivateInstance).GetValue(adapter), persistence.ActiveGeneratedWorld),
            "adapter consumes persistence active result");
        Check(ReferenceEquals(typeof(WorldNavigationService).GetField("_criticalWorld", PrivateInstance).GetValue(navigation), persistence.ActiveGeneratedWorld),
            "navigation consumes persistence active result, including same-seed profile changes");
    }

    static void StartDirection()
    {
        demo.TryGetAnchor(WorldGenerationAnchorKind.Start, out var start);
        Check(alpha.MovePlayerToCellForValidation(start.Coordinate + new Vector2Int(-5, -4)), "movement test starts on central safe land");
        player.ResetMotionAfterTeleport();
        Camera.main.GetComponent<CameraController>().SnapToTarget();
        movementStart = player.transform.position;
        cameraStart = Camera.main.transform.position;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Keys[direction]));
        SetStage(1);
    }

    static void GroundCheck()
    {
        if (!alpha.Grid.WorldToCell(player.transform.position, out var coordinate) ||
            !alpha.Grid.TryGetCell(coordinate, out var cell) || !cell.IsWalkable || cell.HasWater ||
            !alpha.Grid.CellToWorld(coordinate, out var ground) || player.transform.position.y < ground.y - .12f)
            throw new Exception("Player ground penetration or non-walkable position.");
    }

    static void SetStage(int value) { stage = value; stageAt = EditorApplication.timeSinceStartup; stageGameTime = Time.time; }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; Evidence("PASS " + message); }
    static void Evidence(string text) { File.AppendAllText(Output + "/checks.txt", text + "\n"); Debug.Log("[BIG-ISLAND-01] " + text); }
    static void OnLog(string message, string stack, LogType type)
    {
        if (SessionState.GetBool(Active, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
            SessionState.SetInt(Active + ".Errors", SessionState.GetInt(Active + ".Errors", 0) + 1);
    }
    static void Fail(Exception ex) { Finish(true, ex.ToString()); }
    static void Finish(bool failed, string reason)
    {
        SessionState.SetBool(Active + ".Failed", failed);
        stage = 99;
        if (keyboard != null) InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        File.WriteAllText(Output + "/result.txt", $"BIG-ISLAND-01 {(failed ? "FAIL" : "PASS")}\nChecks={checks}\nPlay sessions=1\nRuntime errors/exceptions/asserts={SessionState.GetInt(Active + ".Errors", 0)}\n{reason}\n");
        EditorApplication.update -= Tick;
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else { SessionState.SetBool(Active, false); if (Application.isBatchMode) EditorApplication.Exit(1); }
    }
    static string SaveHashes()
    {
        if (!Directory.Exists(Application.persistentDataPath)) return "";
        using var hash = SHA256.Create();
        return string.Join("\n", Directory.GetFiles(Application.persistentDataPath, "*.json")
            .OrderBy(p => p).Select(p => Path.GetFileName(p) + ":" + Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(p)))));
    }
}
