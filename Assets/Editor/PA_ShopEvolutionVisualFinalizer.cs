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
using Object = UnityEngine.Object;

// B02-B04 Tripo shop evolution source audit.
// The imported FBX files and generated wrapper prefabs are read-only inputs.
[InitializeOnLoad]
public static class PA_ShopEvolutionVisualFinalizer
{
    const string CaptureDirectory = "Logs/ShopEvolutionAudit";
    const string CharacterPath = "Assets/Art/Character/C-01.fbx";
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string FinalVisualDirectory = "Assets/Resources/VisualFinalization/ShopEvolution";
    const string RuntimeActiveKey = "PA.ShopEvolutionVisual.Active";
    const string RuntimeModeKey = "PA.ShopEvolutionVisual.Mode";
    const string RuntimeEnteredKey = "PA.ShopEvolutionVisual.Entered";
    const string RuntimeRanKey = "PA.ShopEvolutionVisual.Ran";
    const string RuntimeErrorKey = "PA.ShopEvolutionVisual.Error";

    static bool _runtimeEntered;
    static bool _runtimeRan;
    static bool _runtimeError;
    static double _runtimeStartedAt;
    static double _runtimeNextAt;
    static int _runtimeTierIndex;
    static int[] _runtimeTiers = Array.Empty<int>();
    static string _placementSnapshot = string.Empty;
    static Task _runtimeTask;
    static bool _assetAuditRunning;

    static readonly ShopStage[] Stages =
    {
        new("B02", "Assets/Models/Buildings/B02_GeneralStore.fbx",
            "Assets/Prefabs/Buildings/B02_GeneralStore.prefab", 1, 8, 1.45f, 3.30f),
        new("B03", "Assets/Models/Buildings/B03_ConvenienceStore.fbx",
            "Assets/Prefabs/Buildings/B03_ConvenienceStore.prefab", 2, 16, 0f, 3.35f),
        new("B04", "Assets/Models/Buildings/B04_DepartmentStore.fbx",
            "Assets/Prefabs/Buildings/B04_DepartmentStore.prefab", 3, 32, 0f, 3.30f)
    };

    static readonly View[] Views =
    {
        new("minus_z", new Vector3(0f, 0f, -1f)),
        new("plus_z", new Vector3(0f, 0f, 1f)),
        new("minus_x", new Vector3(-1f, 0f, 0f)),
        new("plus_x", new Vector3(1f, 0f, 0f))
    };

    static PA_ShopEvolutionVisualFinalizer()
    {
        if (!SessionState.GetBool(RuntimeActiveKey, false)) return;
        _runtimeEntered = SessionState.GetBool(RuntimeEnteredKey, false);
        _runtimeRan = SessionState.GetBool(RuntimeRanKey, false);
        _runtimeError = SessionState.GetBool(RuntimeErrorKey, false);
        string mode = SessionState.GetString(RuntimeModeKey, "before");
        _runtimeTiers = mode == "after" ? new[] { 1, 2, 3 } : new[] { 1 };
        RegisterRuntimeCallbacks();
        if (_runtimeRan && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += FinishRuntimeCapture;
    }

    [MenuItem("Project PA/Audit/Audit B02-B04 Shop Evolution Assets")]
    public static async void RunAssetAuditBatch()
    {
        if (_assetAuditRunning)
        {
            Debug.LogWarning("[Shop Evolution Asset Audit] capture is already running");
            return;
        }

        _assetAuditRunning = true;
        Directory.CreateDirectory(CaptureDirectory);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildLighting();
        GameObject ground = BuildGround();
        try
        {
            foreach (ShopStage stage in Stages)
            {
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(stage.modelPath);
                GameObject wrapper = AssetDatabase.LoadAssetAtPath<GameObject>(stage.prefabPath);
                Require(source != null, $"{stage.id} source FBX loads");
                Require(wrapper != null, $"{stage.id} wrapper prefab loads");

                AuditSource(stage, source);
                GameObject instance = PrefabUtility.InstantiatePrefab(wrapper, scene) as GameObject;
                Require(instance != null, $"{stage.id} wrapper instantiates");
                try
                {
                    instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    instance.transform.localScale = Vector3.one;
                    Transform visual = instance.transform.Find("Visual")
                        ?? instance.GetComponentsInChildren<Transform>(true)
                            .FirstOrDefault(item => item.name == "Visual");
                    Require(visual != null, $"{stage.id} wrapper has isolated Visual child");

                    ShopSlot[] slots = instance.GetComponentsInChildren<ShopSlot>(true);
                    Require(slots.Length == stage.expectedSlots,
                        $"{stage.id} wrapper has authored {stage.expectedSlots} ShopSlots (found {slots.Length})");
                    Require(instance.GetComponent<Shop>() != null, $"{stage.id} wrapper contains a functional Shop");

                    Bounds visualBounds = CalculateRendererBounds(visual.gameObject, includeInactive: true);
                    AuditWrapper(stage, instance, visualBounds, slots);
                    SetOnlyVisualRenderers(instance, visual);
                    await CaptureTurntableAsync(stage, instance, visualBounds, scene);
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }

            Debug.Log($"[Shop Evolution Asset Audit] PASS captures={CaptureDirectory}");
        }
        finally
        {
            if (ground != null) Object.DestroyImmediate(ground);
            _assetAuditRunning = false;
        }
    }

    [MenuItem("Project PA/Fix/Build B02-B04 Shop Evolution Visuals")]
    public static void BuildFinalAssetsBatch()
    {
        EnsureAssetFolder(FinalVisualDirectory);
        foreach (ShopStage stage in Stages)
        {
            GameObject wrapperAsset = AssetDatabase.LoadAssetAtPath<GameObject>(stage.prefabPath);
            Require(wrapperAsset != null, $"{stage.id} wrapper loads for derived visual build");
            GameObject wrapper = PrefabUtility.InstantiatePrefab(wrapperAsset) as GameObject;
            Require(wrapper != null, $"{stage.id} wrapper instantiates for derived visual build");
            var root = new GameObject($"{stage.id}_ShopEvolutionVisual");
            try
            {
                Transform sourceVisual = wrapper.transform.Find("Visual")
                    ?? wrapper.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == "Visual");
                Require(sourceVisual != null, $"{stage.id} source Visual found for non-destructive derivation");
                GameObject visual = Object.Instantiate(sourceVisual.gameObject, root.transform, false);
                visual.name = "Visual";

                Bounds localBounds = CalculateLocalRendererBounds(root.transform, visual);
                var collider = root.AddComponent<BoxCollider>();
                collider.center = localBounds.center;
                collider.size = localBounds.size;

                var obstacle = root.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = localBounds.center;
                obstacle.size = localBounds.size;
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;

                var entrance = new GameObject("EntranceAnchor").transform;
                entrance.SetParent(root.transform, false);
                entrance.localPosition = new Vector3(localBounds.min.x - 0.08f, 0.5f, stage.entranceZ);
                entrance.localRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);

                var sign = new GameObject("SignAnchor").transform;
                sign.SetParent(root.transform, false);
                sign.localPosition = new Vector3(localBounds.min.x - 0.12f, stage.signY, stage.entranceZ);
                sign.localRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);

                string output = $"{FinalVisualDirectory}/{stage.id}_ShopEvolutionVisual.prefab";
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, output);
                Require(saved != null, $"{stage.id} visual-only Resources prefab saved");
                Require(saved.GetComponent<Shop>() == null && saved.GetComponentsInChildren<ShopSlot>(true).Length == 0,
                    $"{stage.id} derived prefab contains no duplicate Shop or ShopSlot");
                Require(saved.GetComponent<BoxCollider>() != null && saved.GetComponent<NavMeshObstacle>() != null,
                    $"{stage.id} derived prefab owns mesh-matched collider and carving obstacle");
                Debug.Log($"[Shop Evolution Final Asset] {stage.id} output={output} " +
                          $"boundsCenter={V(localBounds.center)} boundsSize={V(localBounds.size)} " +
                          $"entrance={V(entrance.localPosition)} sign={V(sign.localPosition)} facade=local-X");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(wrapper);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Shop Evolution Final Assets] PASS directory={FinalVisualDirectory}");
    }

    [MenuItem("Project PA/Audit/Capture Shop Evolution Runtime Baseline")]
    public static void CaptureRuntimeBaselineBatch()
    {
        StartRuntimeCapture("before");
    }

    [MenuItem("Project PA/Validation/Run Shop Evolution Visual Validation")]
    public static void RunFinalRuntimeValidationBatch()
    {
        StartRuntimeCapture("after");
    }

    static void StartRuntimeCapture(string mode)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Require(scene.IsValid() && scene.isLoaded, $"main scene loads: {ScenePath}");
        Directory.CreateDirectory(CaptureDirectory);
        _runtimeEntered = false;
        _runtimeRan = false;
        _runtimeError = false;
        _runtimeStartedAt = EditorApplication.timeSinceStartup;
        _runtimeNextAt = double.MaxValue;
        _runtimeTierIndex = 0;
        _runtimeTiers = mode == "after" ? new[] { 1, 2, 3 } : new[] { 1 };
        _placementSnapshot = string.Empty;
        _runtimeTask = null;
        SessionState.SetBool(RuntimeActiveKey, true);
        SessionState.SetString(RuntimeModeKey, mode);
        SessionState.SetBool(RuntimeEnteredKey, false);
        SessionState.SetBool(RuntimeRanKey, false);
        SessionState.SetBool(RuntimeErrorKey, false);
        RegisterRuntimeCallbacks();
        Debug.Log($"[Shop Evolution Runtime] entering Play Mode mode={mode}");
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
            _runtimeNextAt = _runtimeStartedAt + 4d;
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

        if (!EditorApplication.isPlaying || _runtimeRan || EditorApplication.timeSinceStartup < _runtimeNextAt)
            return;

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
                _runtimeTask = null;
                _runtimeTierIndex++;
                if (_runtimeTierIndex < _runtimeTiers.Length)
                {
                    TierService.Instance.ForceSetTier(_runtimeTiers[_runtimeTierIndex],
                        TierService.Instance.Reputation,
                        $"PA_ShopEvolutionVisualFinalizer tier {_runtimeTiers[_runtimeTierIndex]}");
                    _runtimeNextAt = EditorApplication.timeSinceStartup + 0.75d;
                    return;
                }

                ShopCustomizationController finalCustomization = ShopCustomizationController.Instance
                    ?? Object.FindFirstObjectByType<ShopCustomizationController>();
                var finalSnapshot = new SaveData();
                finalCustomization.WriteSaveFields(finalSnapshot);
                Require(JsonUtility.ToJson(finalSnapshot) == _placementSnapshot,
                    "tier visual transitions preserve every existing shop placement/save field");
                string completedMode = SessionState.GetString(RuntimeModeKey, "before");
                Debug.Log($"[Shop Evolution Runtime] PASS mode={completedMode} " +
                          $"tiers={string.Join(",", _runtimeTiers)}");
            }
            catch (Exception exception)
            {
                _runtimeTask = null;
                _runtimeError = true;
                SessionState.SetBool(RuntimeErrorKey, true);
                Debug.LogError($"[Shop Evolution Runtime] FAIL {exception.Message}\n{exception}");
            }

            _runtimeRan = true;
            SessionState.SetBool(RuntimeRanKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }

        try
        {
            if (_runtimeTierIndex == 0 && string.IsNullOrEmpty(_placementSnapshot))
            {
                PlayableDayScenarioController scenario = Object.FindFirstObjectByType<PlayableDayScenarioController>();
                Require(scenario != null, "PlayableDayScenarioController exists for runtime staging");
                scenario.RestoreSavedSession("상점진화검증", "green_bay", 0);
                Time.timeScale = 1f;
                if (GameClock.Instance != null)
                    GameClock.Instance.ForceSet(15.25f, 1, "PA_ShopEvolutionVisualFinalizer");
                foreach (DayNightVisual visual in Object.FindObjectsByType<DayNightVisual>(FindObjectsSortMode.None))
                    visual.ApplyHour(15);

                ShopCustomizationController customization = ShopCustomizationController.Instance
                    ?? Object.FindFirstObjectByType<ShopCustomizationController>();
                Require(customization != null && customization.IsReady,
                    "shop customization is ready before tier transitions");
                var snapshot = new SaveData();
                customization.WriteSaveFields(snapshot);
                _placementSnapshot = JsonUtility.ToJson(snapshot);
            }

            int tier = _runtimeTiers[_runtimeTierIndex];
            if (TierService.Instance.CurrentTier != tier)
            {
                TierService.Instance.ForceSetTier(tier, TierService.Instance.Reputation,
                    $"PA_ShopEvolutionVisualFinalizer tier {tier}");
                _runtimeNextAt = EditorApplication.timeSinceStartup + 0.75d;
                return;
            }

            string mode = SessionState.GetString(RuntimeModeKey, "before");
            _runtimeTask = CaptureAndValidateRuntimeStageAsync(tier, mode);
            return;
        }
        catch (Exception exception)
        {
            _runtimeError = true;
            SessionState.SetBool(RuntimeErrorKey, true);
            Debug.LogError($"[Shop Evolution Runtime] FAIL {exception.Message}\n{exception}");
        }

        _runtimeRan = true;
        SessionState.SetBool(RuntimeRanKey, true);
        EditorApplication.ExitPlaymode();
    }

    static async Task CaptureAndValidateRuntimeStageAsync(int tier, string mode)
    {
        if (mode == "after") ValidateFinalRuntimeStage(tier);
        await CaptureRuntimeStageAsync(tier, mode);
    }

    static async Task CaptureRuntimeStageAsync(int tier, string mode)
    {
        GameObject cottage = GameObject.Find("B10_Cottage_01");
        GameObject door = GameObject.Find("PA_StoreDoor_Out");
        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        Camera camera = Camera.main ?? Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
        Require(cottage != null && door != null && player != null && camera != null,
            $"tier {tier} shop/camera/player staging objects exist");

        CharacterController characterController = player.GetComponent<CharacterController>();
        bool controllerEnabled = characterController != null && characterController.enabled;
        if (characterController != null) characterController.enabled = false;
        player.transform.SetPositionAndRotation(door.transform.position + door.transform.forward * 1.5f,
            Quaternion.LookRotation(-door.transform.forward, Vector3.up));
        if (characterController != null) characterController.enabled = controllerEnabled;

        Renderer[] activeRenderers = cottage.GetComponentsInChildren<Renderer>(false)
            .Where(renderer => renderer.enabled && !(renderer is ParticleSystemRenderer))
            .ToArray();
        Require(activeRenderers.Length > 0, $"tier {tier} has visible exterior renderers");
        Bounds bounds = activeRenderers[0].bounds;
        for (int index = 1; index < activeRenderers.Length; index++) bounds.Encapsulate(activeRenderers[index].bounds);
        bounds.Encapsulate(CalculateRendererBounds(player, includeInactive: false));

        Vector3 outward = door.transform.forward;
        Vector3 target = bounds.center + Vector3.up * 0.2f;
        Vector3 cameraPosition =
            target + outward * 13f + door.transform.right * 4.5f + Vector3.up * 10.5f;

        string fileName = mode == "before"
            ? "shop_evolution_runtime_before.png"
            : $"shop_evolution_tier{tier}_after.png";
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
                captureCamera.orthographicSize = 6.6f;
                captureCamera.clearFlags = CameraClearFlags.Skybox;
            }, 1600, 900, 1000);
        }
        finally
        {
            camera.clearFlags = previousClearFlags;
            if (cameraController != null) cameraController.enabled = controllerWasEnabled;
        }

        Require(File.Exists(output) && new FileInfo(output).Length > 1024,
            $"tier {tier} runtime capture written: {fileName}");
    }

    static void FailRuntimeCapture(string message)
    {
        _runtimeError = true;
        _runtimeRan = true;
        SessionState.SetBool(RuntimeErrorKey, true);
        SessionState.SetBool(RuntimeRanKey, true);
        Debug.LogError($"[Shop Evolution Runtime] FAIL {message}");
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
        SessionState.EraseString(RuntimeModeKey);
        SessionState.EraseBool(RuntimeEnteredKey);
        SessionState.EraseBool(RuntimeRanKey);
        SessionState.EraseBool(RuntimeErrorKey);
        Debug.Log(!error && ran
            ? "[Shop Evolution Runtime] finished successfully"
            : "[Shop Evolution Runtime] finished with failure");
        EditorApplication.Exit(!error && ran ? 0 : 1);
    }

    static void ValidateFinalRuntimeStage(int tier)
    {
        ShopEvolutionController controller = ShopEvolutionController.Instance
            ?? Object.FindFirstObjectByType<ShopEvolutionController>();
        Require(controller != null, $"tier {tier} ShopEvolutionController exists");
        Require(controller.HasAllExteriorStageAssets, "all B02-B04 visual-only stage assets load");
        Require(controller.ActiveExteriorStage == tier,
            $"tier {tier} selects exterior stage {tier} (actual {controller.ActiveExteriorStage})");
        Require(controller.ActiveEvolutionVisualCount == 1,
            $"tier {tier} has exactly one evolution visual active");
        GameObject active = controller.ActiveExteriorVisual;
        Transform entrance = controller.ActiveEntranceAnchor;
        Transform sign = controller.ActiveSignAnchor;
        Require(active != null && entrance != null && sign != null,
            $"tier {tier} active visual, entrance, and sign anchors exist");
        Require(active.GetComponent<Shop>() == null && active.GetComponentsInChildren<ShopSlot>(true).Length == 0,
            $"tier {tier} exterior adds no duplicate Shop or ShopSlot");

        BoxCollider collider = active.GetComponent<BoxCollider>();
        NavMeshObstacle obstacle = active.GetComponent<NavMeshObstacle>();
        Require(collider != null && collider.enabled && obstacle != null && obstacle.enabled && obstacle.carving,
            $"tier {tier} uses an enabled mesh-matched collider and carving obstacle");
        Bounds rendererBounds = CalculateRendererBounds(active, includeInactive: false);
        Require(Mathf.Abs(rendererBounds.min.y - active.transform.position.y) <= 0.04f,
            $"tier {tier} visual is grounded (offset {rendererBounds.min.y - active.transform.position.y:0.###})");
        Require((collider.bounds.size - rendererBounds.size).magnitude <= 0.12f,
            $"tier {tier} physical bounds match rendered bounds");

        GameObject cottage = GameObject.Find("B10_Cottage_01");
        GameObject door = GameObject.Find("PA_StoreDoor_Out");
        Require(cottage != null && door != null, $"tier {tier} preserved B10 host and exterior door contract");
        Require(cottage.GetComponent<Collider>() == null || !cottage.GetComponent<Collider>().enabled,
            $"tier {tier} disables the superseded B10 invisible collider");
        Renderer[] enabledRenderers = cottage.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
            .ToArray();
        Require(enabledRenderers.Length > 0 && enabledRenderers.All(renderer => renderer.transform.IsChildOf(active.transform)),
            $"tier {tier} hides the superseded B10 shell and other stage visuals");
        Require(Vector3.Distance(door.transform.position, entrance.position) <= 0.02f &&
                Vector3.Dot(door.transform.forward, entrance.forward) >= 0.999f,
            $"tier {tier} BuildingEntrance follows the modeled doorway");
        Transform doorSign = door.transform.Find("B10_EntranceVisualAnchor");
        Require(doorSign != null && Vector3.Distance(doorSign.position, sign.position) <= 0.02f &&
                Vector3.Dot(doorSign.forward, sign.forward) >= 0.999f,
            $"tier {tier} fixed Project P.A. sign follows the facade sign anchor");
        Require(controller.CurrentStoreSign.Contains("OPEN"),
            $"tier {tier} fixed shop sign reports OPEN");
    }

    static void AuditSource(ShopStage stage, GameObject source)
    {
        Mesh[] meshes = source.GetComponentsInChildren<MeshFilter>(true)
            .Select(filter => filter.sharedMesh)
            .Where(mesh => mesh != null)
            .Distinct()
            .ToArray();
        int vertices = meshes.Sum(mesh => mesh.vertexCount);
        int triangles = meshes.Sum(mesh => Enumerable.Range(0, mesh.subMeshCount)
            .Sum(subMesh => (int)mesh.GetIndexCount(subMesh) / 3));
        int subMeshes = meshes.Sum(mesh => mesh.subMeshCount);
        string materials = string.Join(", ", source.GetComponentsInChildren<Renderer>(true)
            .SelectMany(renderer => renderer.sharedMaterials ?? Array.Empty<Material>())
            .Where(material => material != null)
            .Select(material => material.name)
            .Distinct(StringComparer.Ordinal));
        Debug.Log($"[Shop Evolution Source] {stage.id} meshes={meshes.Length} vertices={vertices} " +
                  $"triangles={triangles} subMeshes={subMeshes} materials=[{materials}]");
    }

    static void AuditWrapper(ShopStage stage, GameObject wrapper, Bounds visualBounds, ShopSlot[] slots)
    {
        BoxCollider rootCollider = wrapper.GetComponent<BoxCollider>();
        NavMeshObstacle obstacle = wrapper.GetComponent<NavMeshObstacle>();
        Bounds colliderBounds = rootCollider != null
            ? new Bounds(wrapper.transform.TransformPoint(rootCollider.center),
                Vector3.Scale(rootCollider.size, Abs(wrapper.transform.lossyScale)))
            : default;
        float floorOffset = visualBounds.min.y - wrapper.transform.position.y;
        Debug.Log($"[Shop Evolution Wrapper] {stage.id} visualCenter={V(visualBounds.center)} " +
                  $"visualSize={V(visualBounds.size)} minY={visualBounds.min.y:0.###} floorOffset={floorOffset:0.###} " +
                  $"colliderCenter={V(colliderBounds.center)} colliderSize={V(colliderBounds.size)} " +
                  $"obstacle={(obstacle != null ? $"{V(obstacle.center)}/{V(obstacle.size)} carve={obstacle.carving}" : "missing")} " +
                  $"slots={slots.Length}");

        Vector3 slotCenter = slots.Aggregate(Vector3.zero, (sum, slot) => sum + slot.transform.position) /
                             Mathf.Max(1, slots.Length);
        Debug.Log($"[Shop Evolution Orientation] {stage.id} authored slot bank center={V(slotCenter)} " +
                  $"relative={V(wrapper.transform.InverseTransformPoint(slotCenter))} (slot bank indicates interaction facade)");
    }

    static async Task CaptureTurntableAsync(
        ShopStage stage, GameObject wrapper, Bounds visualBounds, Scene scene)
    {
        GameObject character = CreateScaleCharacter(scene);
        try
        {
            foreach (View view in Views)
            {
                PositionCharacter(character, visualBounds, view.direction);
                Bounds combined = visualBounds;
                combined.Encapsulate(CalculateRendererBounds(character, includeInactive: false));

                var cameraObject = new GameObject($"{stage.id}_{view.name}_Camera", typeof(Camera));
                Camera camera = cameraObject.GetComponent<Camera>();
                try
                {
                    Vector3 target = combined.center + Vector3.up * (combined.size.y * 0.03f);
                    float distance = Mathf.Max(12f, combined.size.magnitude * 1.4f);
                    camera.transform.position = target + view.direction * distance + Vector3.up * combined.size.y * 0.26f;
                    camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position, Vector3.up);
                    camera.orthographic = true;
                    camera.orthographicSize = Mathf.Max(3.2f, combined.size.y * 0.62f,
                        ProjectedWidth(combined.size, camera.transform.right) / (2f * (16f / 9f)) * 1.12f);
                    camera.clearFlags = CameraClearFlags.Color;
                    camera.backgroundColor = new Color(0.69f, 0.78f, 0.70f);
                    camera.nearClipPlane = 0.05f;
                    camera.farClipPlane = 250f;
                    character.transform.rotation = Quaternion.LookRotation(
                        Vector3.ProjectOnPlane(camera.transform.position - character.transform.position, Vector3.up),
                        Vector3.up);

                    string output = Path.Combine(CaptureDirectory,
                        $"{stage.id.ToLowerInvariant()}_{view.name}.png");
                    await CaptureAsync(camera, output);
                    Require(File.Exists(output) && new FileInfo(output).Length > 1024,
                        $"{stage.id} {view.name} capture written");
                }
                finally
                {
                    Object.DestroyImmediate(cameraObject);
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
    }

    static GameObject CreateScaleCharacter(Scene scene)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
        Require(source != null, "C-01 scale-reference character loads");
        GameObject character = PrefabUtility.InstantiatePrefab(source, scene) as GameObject;
        Require(character != null, "C-01 scale-reference character instantiates");
        character.name = "PA_1_85m_ScaleReference";
        Bounds bounds = CalculateRendererBounds(character, includeInactive: true);
        Require(bounds.size.y > 0.01f, "C-01 scale-reference bounds are valid");
        character.transform.localScale *= 1.85f / bounds.size.y;
        bounds = CalculateRendererBounds(character, includeInactive: true);
        character.transform.position += Vector3.up * -bounds.min.y;
        foreach (Animator animator in character.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        return character;
    }

    static void PositionCharacter(GameObject character, Bounds building, Vector3 viewDirection)
    {
        Vector3 screenRight = Vector3.Cross(Vector3.up, viewDirection).normalized;
        float projectedHalfWidth = ProjectedWidth(building.extents, screenRight);
        Vector3 position = building.center - screenRight * (projectedHalfWidth + 1.2f);
        position -= viewDirection * 0.35f;
        position.y = 0f;
        character.transform.position = position;
    }

    static void SetOnlyVisualRenderers(GameObject wrapper, Transform visual)
    {
        HashSet<Renderer> visualRenderers = visual.GetComponentsInChildren<Renderer>(true).ToHashSet();
        foreach (Renderer renderer in wrapper.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = visualRenderers.Contains(renderer);
    }

    static GameObject BuildGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "ShopEvolutionAuditGround";
        ground.transform.localScale = Vector3.one * 5f;
        Object.DestroyImmediate(ground.GetComponent<Collider>());
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.name = "ShopEvolutionAuditGroundMaterial";
        material.color = new Color(0.62f, 0.70f, 0.54f);
        ground.GetComponent<Renderer>().sharedMaterial = material;
        return ground;
    }

    static void BuildLighting()
    {
        var keyObject = new GameObject("ShopEvolutionAuditKey", typeof(Light));
        keyObject.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
        Light key = keyObject.GetComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color(1f, 0.90f, 0.76f);
        key.intensity = 1.2f;
        key.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.48f, 0.52f, 0.47f);
    }

    static Bounds CalculateRendererBounds(GameObject root, bool includeInactive)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive)
            .Where(renderer => renderer.enabled && !(renderer is ParticleSystemRenderer))
            .ToArray();
        Require(renderers.Length > 0, $"{root.name} has visible renderers");
        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
        return bounds;
    }

    static Bounds CalculateLocalRendererBounds(Transform root, GameObject visual)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => !(renderer is ParticleSystemRenderer))
            .ToArray();
        Require(renderers.Length > 0, $"{visual.name} has renderers for local bounds");
        bool initialized = false;
        Bounds bounds = default;
        foreach (Renderer renderer in renderers)
        {
            Bounds world = renderer.bounds;
            Vector3 min = world.min;
            Vector3 max = world.max;
            Vector3[] corners =
            {
                new(min.x, min.y, min.z), new(min.x, min.y, max.z),
                new(min.x, max.y, min.z), new(min.x, max.y, max.z),
                new(max.x, min.y, min.z), new(max.x, min.y, max.z),
                new(max.x, max.y, min.z), new(max.x, max.y, max.z)
            };
            foreach (Vector3 corner in corners)
            {
                Vector3 local = root.InverseTransformPoint(corner);
                if (!initialized)
                {
                    bounds = new Bounds(local, Vector3.zero);
                    initialized = true;
                }
                else bounds.Encapsulate(local);
            }
        }
        return bounds;
    }

    static void EnsureAssetFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = $"{current}/{parts[index]}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
            current = next;
        }
    }

    static float ProjectedWidth(Vector3 size, Vector3 axis)
    {
        Vector3 absolute = Abs(axis.normalized);
        return Mathf.Abs(size.x * absolute.x) + Mathf.Abs(size.y * absolute.y) + Mathf.Abs(size.z * absolute.z);
    }

    static Vector3 Abs(Vector3 value) => new(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

    static Task CaptureAsync(Camera camera, string outputPath)
    {
        return PA_SafeGameViewCapture.CaptureAsync(
            outputPath, camera, configureCamera: null, width: 1600, height: 900, settleMilliseconds: 1000);
    }

    static string V(Vector3 value) => $"({value.x:0.###},{value.y:0.###},{value.z:0.###})";

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[Shop Evolution Check] OK {message}");
    }

    readonly struct ShopStage
    {
        public readonly string id;
        public readonly string modelPath;
        public readonly string prefabPath;
        public readonly int tier;
        public readonly int expectedSlots;
        public readonly float entranceZ;
        public readonly float signY;

        public ShopStage(string id, string modelPath, string prefabPath, int tier, int expectedSlots,
            float entranceZ, float signY)
        {
            this.id = id;
            this.modelPath = modelPath;
            this.prefabPath = prefabPath;
            this.tier = tier;
            this.expectedSlots = expectedSlots;
            this.entranceZ = entranceZ;
            this.signY = signY;
        }
    }

    readonly struct View
    {
        public readonly string name;
        public readonly Vector3 direction;

        public View(string name, Vector3 direction)
        {
            this.name = name;
            this.direction = direction;
        }
    }
}
#endif
