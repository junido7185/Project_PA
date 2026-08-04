#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Tripo 추정 C-01~C-09의 원본을 덮어쓰지 않고 Avatar/접지/보행/물리 정합을
// Unity Editor API와 실제 Play Mode 카메라로 감사하는 전용 도구다.
[InitializeOnLoad]
public static class PA_CharacterFinalizer
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string CharacterFolder = "Assets/Art/Character";
    const string CaptureDirectory = "Logs/CharacterFinalization";
    const string RuntimeActiveKey = "PA.CharacterFinalization.Active";
    const string RuntimeEnteredKey = "PA.CharacterFinalization.Entered";
    const string RuntimeRanKey = "PA.CharacterFinalization.Ran";
    const string RuntimeErrorKey = "PA.CharacterFinalization.Error";
    const string RuntimeModeKey = "PA.CharacterFinalization.Mode";

    static readonly string[] CharacterPaths = Enumerable.Range(1, 9)
        .Select(index => $"{CharacterFolder}/C-{index:00}.fbx")
        .ToArray();

    static bool _runtimeEntered;
    static bool _runtimeRan;
    static bool _runtimeError;
    static double _runtimeStartedAt;
    static float _phaseStartedAt;
    static GameObject _player;
    static Animator _playerAnimator;
    static Task _runtimeTask;
    static readonly List<NpcController> StagedNpcs = new List<NpcController>();
    static readonly Dictionary<NpcController, Vector3> NpcTargets = new Dictionary<NpcController, Vector3>();

    static PA_CharacterFinalizer()
    {
        if (!SessionState.GetBool(RuntimeActiveKey, false)) return;
        _runtimeEntered = SessionState.GetBool(RuntimeEnteredKey, false);
        _runtimeRan = SessionState.GetBool(RuntimeRanKey, false);
        _runtimeError = SessionState.GetBool(RuntimeErrorKey, false);
        RegisterRuntimeCallbacks();
        if (_runtimeRan && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += FinishRuntimeCapture;
    }

    [MenuItem("Project PA/Audit/Audit C-01-C-09 Character Assets")]
    public static async void AuditCharacterAssets()
    {
        Directory.CreateDirectory(CaptureDirectory);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var staged = new List<GameObject>();
        int totalVertices = 0;
        int totalTriangles = 0;

        for (int index = 0; index < CharacterPaths.Length; index++)
        {
            string path = CharacterPaths[index];
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(source != null, $"character source exists: {path}");

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            Require(importer != null && importer.animationType == ModelImporterAnimationType.Human,
                $"{Path.GetFileName(path)} imports as Humanoid");
            Require(importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel,
                $"{Path.GetFileName(path)} creates its own Avatar");

            GameObject instance = PrefabUtility.InstantiatePrefab(source, scene) as GameObject;
            if (instance == null) instance = UnityEngine.Object.Instantiate(source);
            instance.name = $"Audit_C-{index + 1:00}";
            Bounds rawBounds = CalculateRendererBounds(instance);
            MeshAudit mesh = AuditMeshes(instance);
            Animator animator = instance.GetComponentInChildren<Animator>(true);
            Avatar avatar = animator != null ? animator.avatar : null;
            Require(avatar != null && avatar.isValid && avatar.isHuman,
                $"{Path.GetFileName(path)} Avatar is valid Humanoid");

            string textureNames = string.Join(",",
                instance.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(renderer => renderer.sharedMaterials ?? Array.Empty<Material>())
                    .Where(material => material != null && material.mainTexture != null)
                    .Select(material => material.mainTexture.name)
                    .Distinct());
            Debug.Log(
                $"[Character Asset] C-{index + 1:00} rawHeight={rawBounds.size.y:0.###} " +
                $"minY={rawBounds.min.y:0.###} vertices={mesh.vertices} triangles={mesh.triangles} " +
                $"avatar={avatar.name} human={avatar.isHuman} texture={textureNames}");

            totalVertices += mesh.vertices;
            totalTriangles += mesh.triangles;
            NormalizeForLineup(instance.transform, index == 0 ? 1.85f : 1.75f);
            int row = index < 5 ? 0 : 1;
            int rowCount = row == 0 ? 5 : 4;
            int column = row == 0 ? index : index - 5;
            instance.transform.position += new Vector3((column - (rowCount - 1) * 0.5f) * 1.55f, 0f,
                row == 0 ? -0.9f : 1.15f);
            instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            staged.Add(instance);
        }

        AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{CharacterFolder}/Idle.anim");
        AnimationClip walk = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{CharacterFolder}/Walk.anim");
        Require(idle != null && walk != null, "existing Idle and Walk clips load");
        Require(idle.humanMotion && walk.humanMotion, "Idle and Walk clips are Humanoid motion");
        Debug.Log($"[Character Clips] Idle length={idle.length:0.###}s fps={idle.frameRate:0.#} loop={idle.isLooping}; " +
                  $"Walk length={walk.length:0.###}s fps={walk.frameRate:0.#} loop={walk.isLooping} " +
                  $"averageSpeed={walk.averageSpeed.magnitude:0.###}");

        BuildAuditLighting();
        await CaptureLineupAsync(staged, Path.Combine(CaptureDirectory, "character_source_lineup.png"));
        Debug.Log($"[Character Asset Audit] PASS characters={staged.Count} vertices={totalVertices} " +
                  $"triangles={totalTriangles} capture={CaptureDirectory}/character_source_lineup.png");
    }

    [MenuItem("Project PA/Audit/Capture Character Runtime Baseline")]
    public static void CaptureRuntimeBaseline()
    {
        StartRuntimeCapture("baseline");
    }

    [MenuItem("Project PA/Validation/Run Character Final Validation")]
    public static void RunFinalValidation()
    {
        StartRuntimeCapture("final");
    }

    static void StartRuntimeCapture(string mode)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException($"Failed to open {ScenePath}");

        Directory.CreateDirectory(CaptureDirectory);
        _runtimeEntered = false;
        _runtimeRan = false;
        _runtimeError = false;
        _runtimeStartedAt = EditorApplication.timeSinceStartup;
        _runtimeTask = null;
        SessionState.SetBool(RuntimeActiveKey, true);
        SessionState.SetBool(RuntimeEnteredKey, false);
        SessionState.SetBool(RuntimeRanKey, false);
        SessionState.SetBool(RuntimeErrorKey, false);
        SessionState.SetString(RuntimeModeKey, mode);
        RegisterRuntimeCallbacks();
        Debug.Log($"[Character Runtime] entering Play Mode mode={mode}");
        EditorApplication.EnterPlaymode();
    }

    static void RegisterRuntimeCallbacks()
    {
        EditorApplication.playModeStateChanged -= OnRuntimePlayModeStateChanged;
        EditorApplication.update -= OnRuntimeEditorUpdate;
        EditorApplication.playModeStateChanged += OnRuntimePlayModeStateChanged;
        EditorApplication.update += OnRuntimeEditorUpdate;
    }

    static void OnRuntimePlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(RuntimeActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _runtimeEntered = true;
            _runtimeStartedAt = EditorApplication.timeSinceStartup;
            SessionState.SetBool(RuntimeEnteredKey, true);
        }
        else if (state == PlayModeStateChange.EnteredEditMode && _runtimeRan)
        {
            FinishRuntimeCapture();
        }
    }

    static void OnRuntimeEditorUpdate()
    {
        if (!SessionState.GetBool(RuntimeActiveKey, false)) return;
        double elapsed = EditorApplication.timeSinceStartup - _runtimeStartedAt;
        if (!_runtimeEntered)
        {
            if (elapsed > 60d) FailRuntimeCapture("timed out before Play Mode");
            return;
        }

        if (!EditorApplication.isPlaying || _runtimeRan) return;

        if (_runtimeTask != null)
        {
            if (!_runtimeTask.IsCompleted)
            {
                if (elapsed > 90d) FailRuntimeCapture("timed out during runtime capture");
                return;
            }

            try
            {
                _runtimeTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _runtimeError = true;
                SessionState.SetBool(RuntimeErrorKey, true);
                Debug.LogError($"[Character Runtime] FAIL {ex.Message}\n{ex}");
            }

            _runtimeTask = null;
            _runtimeRan = true;
            SessionState.SetBool(RuntimeRanKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }

        if (elapsed > 4d)
        {
            string mode = SessionState.GetString(RuntimeModeKey, "baseline");
            _runtimeTask = RunRuntimeValidationAsync(mode == "final");
        }
        else if (elapsed > 90d)
        {
            FailRuntimeCapture("timed out before runtime capture");
        }
    }

    static async Task RunRuntimeValidationAsync(bool final)
    {
        StageRuntimeCharacters();
        AuditRuntimeCharacters(final, "idle");
        await CaptureRuntimeAsync($"character_idle_{(final ? "after" : "before")}.png");

        _phaseStartedAt = Time.realtimeSinceStartup;
        await Task.Delay(350);

        StartRuntimeWalking();
        _phaseStartedAt = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - _phaseStartedAt <= 0.6f)
        {
            MoveValidationPlayer();
            await Task.Delay(16);
        }

        AuditRuntimeCharacters(final, "walking");
        await CaptureRuntimeAsync($"character_walk_{(final ? "after" : "before")}.png");
        _phaseStartedAt = Time.realtimeSinceStartup;
        await Task.Delay(250);

        StopRuntimeWalking();
        Debug.Log(final ? "[Character Runtime Final] PASS" : "[Character Runtime Baseline] PASS");
    }

    static void StageRuntimeCharacters()
    {
        // 시작 온보딩은 세계 시간을 0으로 멈춘다. 다른 Play 검증기와 같은
        // 공개 복원 경로로 모달을 닫아 실제 Animator/NavMesh 시간을 검증한다.
        var scenario = UnityEngine.Object.FindFirstObjectByType<PlayableDayScenarioController>();
        Require(scenario != null, "PlayableDayScenarioController exists for character staging");
        scenario.RestoreSavedSession("캐릭터검증", "green_bay", 0);
        Time.timeScale = 1f;
        Require(Time.timeScale > 0.9f, "game time is running for character locomotion");

        _player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        Require(_player != null, "runtime Player exists");
        _playerAnimator = _player.GetComponentInChildren<Animator>(true);
        Require(_playerAnimator != null && _playerAnimator.avatar != null && _playerAnimator.avatar.isHuman,
            "runtime Player owns a Humanoid Animator");

        var playerController = _player.GetComponent<PlayerController>();
        if (playerController != null) playerController.enabled = false;
        var characterController = _player.GetComponent<CharacterController>();
        if (characterController != null) characterController.enabled = false;

        StagedNpcs.Clear();
        NpcTargets.Clear();
        StagedNpcs.AddRange(UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None)
            .Where(npc => npc != null)
            .OrderBy(npc => ResolveCharacterIndex(npc))
            .ThenBy(npc => npc.name)
            .Take(8));
        Require(StagedNpcs.Count == 8, "all eight resident NPCs exist");

        Renderer road = GameObject.Find("Road_EW")?.GetComponent<Renderer>();
        Vector3 anchor = road != null ? road.bounds.center : Vector3.zero;
        // 상점 정면은 기둥과 진열물이 보행 자세를 가린다. 같은 동서 도로의
        // 서쪽 빈 구간으로 옮겨 실제 게임 배경을 유지하면서 전신을 읽게 한다.
        anchor += Vector3.left * 11f;
        anchor.y += 2f;

        // 같은 방향에서 더 빠른 플레이어가 NPC를 따라잡지 않도록 주민을 먼저,
        // 플레이어를 맨 오른쪽에 둔다. 한 줄 배치로 전신 겹침도 제거한다.
        var roots = StagedNpcs.Select(npc => npc.gameObject).ToList();
        roots.Add(_player);
        for (int index = 0; index < roots.Count; index++)
        {
            float column = index - (roots.Count - 1) * 0.5f;
            Vector3 desired = anchor + new Vector3(column * 1.45f, 0f, -1f);
            Require(NavMesh.SamplePosition(desired, out NavMeshHit hit, 4f, NavMesh.AllAreas),
                $"lineup position {index + 1} resolves on NavMesh");

            GameObject root = roots[index];
            if (root == _player)
            {
                root.transform.SetPositionAndRotation(hit.position + Vector3.up * 0.02f,
                    Quaternion.LookRotation(Vector3.right, Vector3.up));
                continue;
            }

            NpcController npc = root.GetComponent<NpcController>();
            npc.enabled = false;
            DisableIfPresent<NpcScheduleController>(root);
            DisableIfPresent<ProducerNpcController>(root);
            DisableIfPresent<SpecialistNpcController>(root);
            NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
            Require(agent != null && agent.enabled, $"{npc.name} has enabled NavMeshAgent");
            if (!agent.isOnNavMesh) Require(agent.Warp(hit.position), $"{npc.name} warps onto NavMesh");
            else agent.Warp(hit.position);
            agent.ResetPath();
            agent.isStopped = true;
            agent.updateRotation = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            root.transform.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
            Vector3 intendedTarget = hit.position + Vector3.right * 3.8f;
            Require(NavMesh.SamplePosition(intendedTarget, out NavMeshHit targetHit, 3f, NavMesh.AllAreas),
                $"{npc.name} walking target resolves on NavMesh");
            NpcTargets[npc] = targetHit.position;
        }

        foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
            canvas.enabled = false;

        if (GameClock.Instance != null)
            GameClock.Instance.ForceSet(10.5f, 1, "PA_CharacterFinalizer capture");
        foreach (DayNightVisual visual in UnityEngine.Object.FindObjectsByType<DayNightVisual>(FindObjectsSortMode.None))
            visual.ApplyHour(10);
        Physics.SyncTransforms();
    }

    static void StartRuntimeWalking()
    {
        _playerAnimator.speed = 1f;
        SetAnimatorFloat(_playerAnimator, "Speed", 1f);
        foreach (NpcController npc in StagedNpcs)
        {
            NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.isOnNavMesh) continue;
            agent.isStopped = false;
            agent.speed = 2.5f;
            agent.acceleration = 30f;
            Require(agent.SetDestination(NpcTargets[npc]), $"{npc.name} accepts walking destination");
        }
    }

    static void MoveValidationPlayer()
    {
        if (_player == null) return;
        _player.transform.position += Vector3.right * (5f * Time.unscaledDeltaTime);
    }

    static void StopRuntimeWalking()
    {
        if (_playerAnimator != null) SetAnimatorFloat(_playerAnimator, "Speed", 0f);
        foreach (NpcController npc in StagedNpcs)
        {
            NavMeshAgent agent = npc != null ? npc.GetComponent<NavMeshAgent>() : null;
            if (agent == null || !agent.isOnNavMesh) continue;
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    static void AuditRuntimeCharacters(bool final, string phase)
    {
        Bounds playerBounds = CalculateCharacterBounds(_player);
        float playerFootOffset = playerBounds.min.y - _player.transform.position.y;
        PlayerFootIkStabilizer playerIk = _player.GetComponentInChildren<PlayerFootIkStabilizer>(true);
        Debug.Log($"[Character Runtime {phase}] Player height={playerBounds.size.y:0.###} " +
                  $"footOffset={playerFootOffset:0.###} animatorSpeed={_playerAnimator.speed:0.###} " +
                  $"playback={(playerIk != null ? playerIk.CurrentPlaybackSpeed : 0f):0.###} " +
                  $"timeScale={Time.timeScale:0.###} avatar={_playerAnimator.avatar.name} " +
                  $"footIK={(playerIk != null && playerIk.enabled)}");
        if (final)
        {
            Require(playerBounds.size.y >= 1.7f && playerBounds.size.y <= 2.05f,
                "Player visible height stays within the 1.7-2.05m art range");
            Require(playerFootOffset >= -0.06f && playerFootOffset <= 0.12f,
                "Player rendered feet align with the physical root");
            Require(playerIk != null && playerIk.enabled, "Player foot IK/cadence stabilizer is active");
            if (phase == "walking")
            {
                Require(_playerAnimator.speed >= 1.45f && _playerAnimator.speed <= 2.75f &&
                        playerIk.CurrentPlaybackSpeed >= 1.45f,
                    "Player Walk playback scales with full movement speed");
            }
        }

        foreach (NpcController npc in StagedNpcs)
        {
            Bounds bounds = CalculateCharacterBounds(npc.gameObject);
            float footOffset = bounds.min.y - npc.transform.position.y;
            CapsuleCollider capsule = npc.GetComponent<CapsuleCollider>();
            NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
            Animator animator = npc.GetComponentInChildren<Animator>(true);
            NpcHumanoidProceduralAnimator procedural = npc.GetComponent<NpcHumanoidProceduralAnimator>();
            Debug.Log($"[Character Runtime {phase}] {npc.name} model=C-{ResolveModelIndex(animator):00} " +
                      $"height={bounds.size.y:0.###} footOffset={footOffset:0.###} " +
                      $"capsule={(capsule != null ? $"{capsule.height:0.##}/{capsule.radius:0.##}" : "missing")} " +
                      $"agent={(agent != null ? $"{agent.height:0.##}/{agent.radius:0.##}/{agent.stoppingDistance:0.##}" : "missing")} " +
                      $"speed={(agent != null ? agent.velocity.magnitude : 0f):0.###} " +
                      $"cadence={(procedural != null ? procedural.CurrentCadence : 0f):0.###} " +
                      $"avatar={(animator != null && animator.avatar != null ? animator.avatar.name : "missing")} " +
                      $"profile={(procedural != null ? procedural.resolvedProfile : "missing")}");

            if (!final) continue;
            Require(animator != null && animator.avatar != null && animator.avatar.isHuman && animator.avatar.isValid,
                $"{npc.name} keeps a valid Humanoid Avatar");
            Require(bounds.size.y >= 1.6f && bounds.size.y <= 1.95f,
                $"{npc.name} visible height stays within resident scale");
            Require(footOffset >= -0.06f && footOffset <= 0.10f,
                $"{npc.name} rendered feet align with NavMesh root");
            Require(capsule != null && Mathf.Abs(capsule.height - 1.8f) < 0.01f &&
                    Mathf.Abs(capsule.radius - 0.4f) < 0.01f &&
                    Vector3.Distance(capsule.center, new Vector3(0f, 0.9f, 0f)) < 0.01f,
                $"{npc.name} CapsuleCollider matches the 1.8m body");
            Require(agent != null && Mathf.Abs(agent.height - 1.8f) < 0.01f &&
                    Mathf.Abs(agent.radius - 0.4f) < 0.01f && Mathf.Abs(agent.baseOffset) < 0.01f &&
                    agent.stoppingDistance >= 0.74f,
                $"{npc.name} NavMeshAgent matches body and personal space");
            Require(procedural != null && procedural.enabled,
                $"{npc.name} uses the existing Humanoid procedural locomotion");
            if (phase == "walking")
                Require(agent.velocity.magnitude >= 0.25f && procedural.CurrentCadence >= 0.7f,
                    $"{npc.name} advances a distance-synchronized walking cycle");
            Require(npc.GetComponentsInChildren<SkinnedMeshRenderer>(true).All(renderer =>
                    renderer.shadowCastingMode != ShadowCastingMode.Off && renderer.receiveShadows),
                $"{npc.name} keeps grounding shadows");
        }
    }

    static async Task CaptureRuntimeAsync(string fileName)
    {
        var roots = new List<GameObject> { _player };
        roots.AddRange(StagedNpcs.Select(npc => npc.gameObject));
        Bounds bounds = CalculateCharacterBounds(roots[0]);
        foreach (GameObject root in roots.Skip(1)) bounds.Encapsulate(CalculateCharacterBounds(root));

        Camera camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
        Require(camera != null, "MainCamera exists for character capture");
        Vector3 target = bounds.center + Vector3.up * 0.15f;
        float aspect = 16f / 9f;
        float orthographicSize = Mathf.Max(3.25f, bounds.size.y * 1.65f,
            bounds.size.x / (2f * aspect) + 0.7f);
        Vector3 cameraPosition = target + new Vector3(-1.6f, 3.4f, -11f);
        string output = Path.Combine(CaptureDirectory, fileName);
        CameraController cameraController = camera.GetComponent<CameraController>()
            ?? camera.GetComponentInParent<CameraController>();
        bool controllerWasEnabled = cameraController != null && cameraController.enabled;
        CameraClearFlags previousClearFlags = camera.clearFlags;
        try
        {
            if (cameraController != null) cameraController.enabled = false;
            await PA_SafeGameViewCapture.CaptureAsync(output, camera, captureCamera =>
            {
                captureCamera.transform.position = cameraPosition;
                captureCamera.transform.rotation =
                    Quaternion.LookRotation(target - cameraPosition, Vector3.up);
                captureCamera.orthographic = true;
                captureCamera.orthographicSize = orthographicSize;
                captureCamera.clearFlags = CameraClearFlags.Skybox;
                captureCamera.cullingMask = -1;
            }, 1600, 900, 1000);
        }
        finally
        {
            camera.clearFlags = previousClearFlags;
            if (cameraController != null) cameraController.enabled = controllerWasEnabled;
        }
        Require(File.Exists(output) && new FileInfo(output).Length > 1024,
            $"character capture written: {fileName}");
    }

    static async Task CaptureLineupAsync(List<GameObject> staged, string output)
    {
        Bounds bounds = CalculateRendererBounds(staged[0]);
        foreach (GameObject go in staged.Skip(1)) bounds.Encapsulate(CalculateRendererBounds(go));
        var cameraObject = new GameObject("CharacterAuditCamera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        Vector3 target = bounds.center + Vector3.up * 0.05f;
        Vector3 cameraPosition = target + new Vector3(-1.2f, 2.7f, -10f);
        float orthographicSize = Mathf.Max(3.2f, bounds.size.x / (2f * (16f / 9f)) + 0.6f);
        await PA_SafeGameViewCapture.CaptureAsync(output, camera, captureCamera =>
        {
            captureCamera.transform.position = cameraPosition;
            captureCamera.transform.rotation =
                Quaternion.LookRotation(target - cameraPosition, Vector3.up);
            captureCamera.orthographic = true;
            captureCamera.orthographicSize = orthographicSize;
            captureCamera.clearFlags = CameraClearFlags.Color;
            captureCamera.backgroundColor = new Color(0.76f, 0.84f, 0.72f);
            captureCamera.cullingMask = -1;
        }, 1600, 900, 1000);
    }

    static void BuildAuditLighting()
    {
        var key = new GameObject("CharacterAuditKey", typeof(Light));
        key.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
        Light light = key.GetComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.92f, 0.78f);
        light.intensity = 1.25f;
        light.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.55f);
    }

    static void NormalizeForLineup(Transform root, float targetHeight)
    {
        Bounds bounds = CalculateRendererBounds(root.gameObject);
        if (bounds.size.y > 0.001f)
            root.localScale *= targetHeight / bounds.size.y;
        bounds = CalculateRendererBounds(root.gameObject);
        root.position += Vector3.up * -bounds.min.y;
    }

    static int ResolveCharacterIndex(NpcController npc)
    {
        if (npc == null) return 99;
        string key = ((npc.profile != null ? npc.profile.name + " " + npc.profile.npcName : "") + " " + npc.name)
            .ToLowerInvariant();
        if (key.Contains("farmer") || key.Contains("bori")) return 2;
        if (key.Contains("lumber")) return 3;
        if (key.Contains("miner")) return 4;
        if (key.Contains("fisher")) return 5;
        if (key.Contains("chef")) return 6;
        if (key.Contains("blacksmith")) return 7;
        if (key.Contains("tailor")) return 8;
        if (key.Contains("carpenter")) return 9;
        return 99;
    }

    static int ResolveModelIndex(Animator animator)
    {
        string name = animator != null && animator.avatar != null ? animator.avatar.name : "";
        for (int index = 1; index <= 9; index++)
            if (name.IndexOf($"C-{index:00}", StringComparison.OrdinalIgnoreCase) >= 0) return index;
        return 99;
    }

    static void DisableIfPresent<T>(GameObject root) where T : Behaviour
    {
        T component = root != null ? root.GetComponent<T>() : null;
        if (component != null) component.enabled = false;
    }

    static void SetAnimatorFloat(Animator animator, string parameter, float value)
    {
        if (animator == null) return;
        if (animator.parameters.Any(item => item.type == AnimatorControllerParameterType.Float && item.name == parameter))
            animator.SetFloat(parameter, value);
    }

    static Bounds CalculateRendererBounds(GameObject root)
    {
        Renderer[] renderers = root != null
            ? root.GetComponentsInChildren<Renderer>(true).Where(renderer => !(renderer is ParticleSystemRenderer)).ToArray()
            : Array.Empty<Renderer>();
        if (renderers.Length == 0) return new Bounds(root != null ? root.transform.position : Vector3.zero, Vector3.zero);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    static Bounds CalculateCharacterBounds(GameObject root)
    {
        SkinnedMeshRenderer[] skinned = root != null
            ? root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray()
            : Array.Empty<SkinnedMeshRenderer>();
        if (skinned.Length == 0) return CalculateRendererBounds(root);
        Bounds bounds = skinned[0].bounds;
        for (int i = 1; i < skinned.Length; i++) bounds.Encapsulate(skinned[i].bounds);
        return bounds;
    }

    static MeshAudit AuditMeshes(GameObject root)
    {
        var meshes = new HashSet<Mesh>();
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            if (filter.sharedMesh != null) meshes.Add(filter.sharedMesh);
        foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (renderer.sharedMesh != null) meshes.Add(renderer.sharedMesh);
        int vertices = meshes.Sum(mesh => mesh.vertexCount);
        int triangles = meshes.Sum(mesh =>
        {
            int count = 0;
            for (int sub = 0; sub < mesh.subMeshCount; sub++) count += (int)mesh.GetIndexCount(sub) / 3;
            return count;
        });
        return new MeshAudit(vertices, triangles);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[Character Check] OK {message}");
    }

    static void FailRuntimeCapture(string message)
    {
        _runtimeError = true;
        _runtimeRan = true;
        SessionState.SetBool(RuntimeErrorKey, true);
        SessionState.SetBool(RuntimeRanKey, true);
        Debug.LogError($"[Character Runtime] FAIL {message}");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else FinishRuntimeCapture();
    }

    static void FinishRuntimeCapture()
    {
        bool error = _runtimeError || SessionState.GetBool(RuntimeErrorKey, false);
        bool ran = _runtimeRan || SessionState.GetBool(RuntimeRanKey, false);
        EditorApplication.playModeStateChanged -= OnRuntimePlayModeStateChanged;
        EditorApplication.update -= OnRuntimeEditorUpdate;
        _runtimeTask = null;
        SessionState.EraseBool(RuntimeActiveKey);
        SessionState.EraseBool(RuntimeEnteredKey);
        SessionState.EraseBool(RuntimeRanKey);
        SessionState.EraseBool(RuntimeErrorKey);
        SessionState.EraseString(RuntimeModeKey);
        Debug.Log(!error && ran
            ? "[Character Runtime] finished successfully"
            : "[Character Runtime] finished with failure");
        EditorApplication.Exit(!error && ran ? 0 : 1);
    }

    readonly struct MeshAudit
    {
        public readonly int vertices;
        public readonly int triangles;

        public MeshAudit(int vertices, int triangles)
        {
            this.vertices = vertices;
            this.triangles = triangles;
        }
    }
}
#endif
