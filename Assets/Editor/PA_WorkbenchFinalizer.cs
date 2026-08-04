#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// B05 원본을 덮어쓰지 않고 실제 크기/실루엣/사용 방향을 감사하는 Editor 도구.
// 최종 파생 메시 제작과 Play Mode 검증도 이 파일에 단계적으로 통합한다.
[InitializeOnLoad]
public static class PA_WorkbenchFinalizer
{
    const string PrefabPath = "Assets/Prefabs/Buildings/B05_Workbench.prefab";
    const string SourcePath = "Assets/Models/Buildings/B05_Workbench.fbx";
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string CaptureDirectory = "Logs/B05_WorkbenchAudit";
    const string FinalMeshPath = "Assets/Art/ProjectPA/Buildings/B05/B05_Workbench_PreparationKit.asset";
    const string FinalPrefabPath = "Assets/Resources/VisualFinalization/B05_Workbench_PreparationKit.prefab";
    const string RuntimeActiveKey = "PA.B05RuntimeCapture.Active";
    const string RuntimeEnteredKey = "PA.B05RuntimeCapture.Entered";
    const string RuntimeRanKey = "PA.B05RuntimeCapture.Ran";
    const string RuntimeErrorKey = "PA.B05RuntimeCapture.Error";
    const string RuntimeModeKey = "PA.B05RuntimeCapture.Mode";

    static bool _runtimeEntered;
    static bool _runtimeRan;
    static bool _runtimeError;
    static double _runtimeStartedAt;
    static Task _runtimeTask;

    static PA_WorkbenchFinalizer()
    {
        if (!SessionState.GetBool(RuntimeActiveKey, false)) return;
        _runtimeEntered = SessionState.GetBool(RuntimeEnteredKey, false);
        _runtimeRan = SessionState.GetBool(RuntimeRanKey, false);
        _runtimeError = SessionState.GetBool(RuntimeErrorKey, false);
        RegisterRuntimeCallbacks();
        if (_runtimeRan && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += FinishRuntimeCapture;
    }

    [MenuItem("Project PA/Audit/Audit B05 Workbench Visual")]
    public static async void AuditB05Source()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        if (prefab == null || source == null)
            throw new InvalidOperationException("B05 Workbench source or wrapper prefab is missing.");

        Directory.CreateDirectory(CaptureDirectory);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        Bounds bounds = CalculateRendererBounds(instance);
        MeshAudit audit = AuditMeshes(instance);
        BoxCollider collider = instance.GetComponent<BoxCollider>();
        NavMeshObstacle nav = instance.GetComponent<NavMeshObstacle>();
        Workbench workbench = instance.GetComponent<Workbench>();

        Debug.Log(
            $"[B05 Audit] source={SourcePath} wrapper={PrefabPath}\n" +
            $"[B05 Audit] bounds center={bounds.center:F3} size={bounds.size:F3} minY={bounds.min.y:F3}\n" +
            $"[B05 Audit] meshes={audit.meshCount} renderers={audit.rendererCount} " +
            $"vertices={audit.vertexCount} triangles={audit.triangleCount} submeshes={audit.subMeshCount}\n" +
            $"[B05 Audit] collider center={(collider != null ? collider.center.ToString("F3") : "missing")} " +
            $"size={(collider != null ? collider.size.ToString("F3") : "missing")} " +
            $"navCarve={(nav != null && nav.carving)} workbench={workbench?.workbenchType}");

        await CaptureTurntableAsync(instance, bounds, "b05_isolated");
        Debug.Log($"[B05 Audit] PASS captures={CaptureDirectory}");
    }

    [MenuItem("Project PA/Fix/Build B05 Workbench Final Assets")]
    public static void BuildFinalAssets()
    {
        EnsureAssetFolder("Assets/Art/ProjectPA/Buildings/B05");
        EnsureAssetFolder("Assets/Resources/VisualFinalization");

        Mesh generatedStatic = BuildPreparationStaticMesh();
        Mesh staticMesh = AssetDatabase.LoadAssetAtPath<Mesh>(FinalMeshPath);
        if (staticMesh == null)
        {
            staticMesh = generatedStatic;
            AssetDatabase.CreateAsset(staticMesh, FinalMeshPath);
        }
        else
        {
            EditorUtility.CopySerialized(generatedStatic, staticMesh);
            UnityEngine.Object.DestroyImmediate(generatedStatic);
            EditorUtility.SetDirty(staticMesh);
        }

        Mesh generatedHandle = BuildProcessHandleMesh();
        Mesh handleMesh = AssetDatabase.LoadAllAssetsAtPath(FinalMeshPath).OfType<Mesh>()
            .FirstOrDefault(mesh => mesh != staticMesh && mesh.name == "B05_Workbench_ProcessHandle_Final");
        if (handleMesh == null)
        {
            handleMesh = generatedHandle;
            AssetDatabase.AddObjectToAsset(handleMesh, staticMesh);
        }
        else
        {
            EditorUtility.CopySerialized(generatedHandle, handleMesh);
            UnityEngine.Object.DestroyImmediate(generatedHandle);
            EditorUtility.SetDirty(handleMesh);
        }

        Material wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Market/PA_Market_Wood.mat");
        Material cream = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Market/PA_Market_AwningCream.mat");
        Material coral = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Market/PA_Market_AwningCoral.mat");
        Material gold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Market/PA_Market_PriceGold.mat");
        Require(wood != null && cream != null && coral != null && gold != null,
            "Project PA wood/cream/coral/gold materials exist");
        Material[] materials = { wood, cream, coral, gold };

        var root = new GameObject("B05_Workbench_PreparationKit");
        try
        {
            MeshFilter filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = staticMesh;
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            var handle = new GameObject("B05_ProcessHandle", typeof(MeshFilter), typeof(MeshRenderer));
            handle.transform.SetParent(root.transform, false);
            handle.transform.localPosition = new Vector3(0.82f, 1.28f, -0.34f);
            handle.GetComponent<MeshFilter>().sharedMesh = handleMesh;
            handle.GetComponent<MeshRenderer>().sharedMaterials = materials;

            var successLight = new GameObject("B05_SuccessLight", typeof(Light));
            successLight.transform.SetParent(root.transform, false);
            successLight.transform.localPosition = new Vector3(0.35f, 1.52f, -0.24f);
            Light light = successLight.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.76f, 0.28f);
            light.range = 1.6f;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            light.enabled = false;

            var direction = new GameObject("B05_WorkDirection");
            direction.transform.SetParent(root.transform, false);
            direction.transform.localPosition = new Vector3(0f, 0f, -1.25f);
            direction.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            Require(PrefabUtility.SaveAsPrefabAsset(root, FinalPrefabPath) != null,
                $"saved derived prefab {FinalPrefabPath}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[B05 Final Assets] PASS mesh={FinalMeshPath} prefab={FinalPrefabPath}");
    }

    [MenuItem("Project PA/Validation/Validate B05 Workbench Final Assets")]
    public static async void ValidateFinalAssets()
    {
        BuildFinalAssets();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Require(prefab != null, "B05 wrapper prefab remains available");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Workbench workbench = instance.GetComponent<Workbench>();
        Require(workbench != null && workbench.ApplyFunctionalArt(), "derived functional art applies to wrapper instance");
        Require(workbench.FunctionalArtRoot != null && workbench.FunctionalArtRoot.name == "B05_Workbench_PreparationKit",
            "derived kit is attached without modifying the source prefab");
        Transform direction = workbench.FunctionalArtRoot.Find("B05_WorkDirection");
        Require(direction != null && Vector3.Dot(direction.forward, -workbench.transform.forward) > 0.999f,
            "visible work direction matches placement-system -Z approach");
        BoxCollider box = instance.GetComponent<BoxCollider>();
        Require(box != null && Mathf.Abs(box.size.x - 2.5f) < 0.001f && Mathf.Abs(box.size.z - 2.55f) < 0.001f,
            "runtime collider fits the 2.26 x 2.51m visible model while prefab footprint remains 2x2");
        NavMeshObstacle obstacle = instance.GetComponent<NavMeshObstacle>();
        Require(obstacle != null && obstacle.carving && Vector3.Distance(obstacle.size, box.size) < 0.001f,
            "carving obstacle matches the corrected collider");

        Mesh staticMesh = AssetDatabase.LoadAssetAtPath<Mesh>(FinalMeshPath);
        Mesh handleMesh = AssetDatabase.LoadAllAssetsAtPath(FinalMeshPath).OfType<Mesh>()
            .FirstOrDefault(mesh => mesh != staticMesh && mesh.name == "B05_Workbench_ProcessHandle_Final");
        Require(staticMesh != null && staticMesh.vertexCount > 0 && staticMesh.subMeshCount == 4,
            "purpose-built four-material preparation mesh exists");
        Require(handleMesh != null && handleMesh.vertexCount > 0, "separate animated clamp handle mesh exists");
        Require(staticMesh.triangles.Length / 3 + handleMesh.triangles.Length / 3 < 1800,
            "derived functional kit stays low-poly");

        Bounds bounds = CalculateRendererBounds(instance);
        await CaptureTurntableAsync(instance, bounds, "b05_final");
        Debug.Log("[B05 Final Validation] PASS");
    }

    [MenuItem("Project PA/Validation/Run B05 Workbench Final Play Validation")]
    public static void RunFinalPlayValidation()
    {
        BuildFinalAssets();
        StartRuntimeCapture("final");
    }

    [MenuItem("Project PA/Audit/Capture B05 Workbench Runtime Baseline")]
    public static void CaptureRuntimeBaseline()
    {
        StartRuntimeCapture("baseline");
    }

    static void StartRuntimeCapture(string mode)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException($"Failed to open {ScenePath}");

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
        Debug.Log($"[B05 Runtime] entering Play Mode mode={mode}");
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
                Debug.LogError($"[B05 Runtime] FAIL {ex.Message}\n{ex}");
            }

            _runtimeTask = null;
            _runtimeRan = true;
            SessionState.SetBool(RuntimeRanKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }

        if (!_runtimeRan && EditorApplication.isPlaying && elapsed > 4d)
        {
            ShopCustomizationController controller =
                UnityEngine.Object.FindFirstObjectByType<ShopCustomizationController>();
            if ((controller == null || !controller.IsReady) && elapsed < 25d) return;
            _runtimeTask = RunRuntimeValidationAsync(
                controller, SessionState.GetString(RuntimeModeKey, "baseline"));
            return;
        }

        if (!_runtimeRan && elapsed > 90d) FailRuntimeCapture("timed out before runtime capture");
    }

    static async Task RunRuntimeValidationAsync(ShopCustomizationController controller, string mode)
    {
        if (mode == "final")
        {
            await RunRuntimeFinalAsync(controller);
            Debug.Log("[B05 Runtime Final] PASS");
        }
        else
        {
            await RunRuntimeBaselineAsync(controller);
            Debug.Log("[B05 Runtime Baseline] PASS");
        }
    }

    static async Task RunRuntimeBaselineAsync(ShopCustomizationController controller)
    {
        Require(controller != null && controller.IsReady, "shop customization controller is ready");
        string definition = controller.GetWorkbenchDefinitionId();
        Require(!string.IsNullOrEmpty(definition), "B05 definition resolves from existing blueprint");

        RecoverShelfAt(controller, new Vector2Int(3, 1));
        RecoverShelfAt(controller, new Vector2Int(3, 2));
        Require(controller.TryPlaceDefinitionForValidation(definition, new Vector2Int(3, 2), 0,
            false, out string placementId, out string reason), $"B05 places in existing 2x2 zone ({reason})");

        GameObject workbenchObject = controller.GetPlacementGameObject(placementId);
        Workbench workbench = workbenchObject != null ? workbenchObject.GetComponent<Workbench>() : null;
        Require(workbench != null, "placed B05 retains Workbench interaction");

        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        Require(player != null, "player exists for game-camera scale reference");
        // ShopCustomization의 FrontOffsets는 로컬 -Z다. 원본 B05는 반대로 +Z가
        // 실제 작업면이므로, baseline은 예약된 접근면(-Z)에서 그 불일치를 보여준다.
        Vector3 approachDirection = -workbench.transform.forward;
        player.transform.position = workbench.transform.position + approachDirection * 1.75f + Vector3.up * 0.05f;
        player.transform.rotation = Quaternion.LookRotation(-approachDirection, Vector3.up);
        Physics.SyncTransforms();

        if (GameClock.Instance != null)
            GameClock.Instance.ForceSet(15.25f, 1, "PA_WorkbenchFinalizer baseline");
        foreach (DayNightVisual visual in UnityEngine.Object.FindObjectsByType<DayNightVisual>(FindObjectsSortMode.None))
            visual.ApplyHour(15);

        string output = Path.Combine(CaptureDirectory, "b05_runtime_before.png");
        await CaptureRuntimeWithGameCameraAsync(workbenchObject, player, output);
        Require(File.Exists(output) && new FileInfo(output).Length > 1024, "runtime baseline capture written");
        Debug.Log($"[B05 Runtime Baseline] capture={output} placement={placementId}");
    }

    static async Task RunRuntimeFinalAsync(ShopCustomizationController controller)
    {
        Require(controller != null && controller.IsReady, "shop customization controller is ready");
        string definition = controller.GetWorkbenchDefinitionId();
        Require(!string.IsNullOrEmpty(definition), "B05 definition resolves from existing blueprint");

        RecoverShelfAt(controller, new Vector2Int(3, 1));
        RecoverShelfAt(controller, new Vector2Int(3, 2));
        Require(controller.TryPlaceDefinitionForValidation(definition, new Vector2Int(3, 2), 0,
            false, out string placementId, out string reason), $"B05 places in existing 2x2 zone ({reason})");
        GameObject workbenchObject = controller.GetPlacementGameObject(placementId);
        Workbench workbench = workbenchObject != null ? workbenchObject.GetComponent<Workbench>() : null;
        Require(workbench != null && workbench.IsFunctionalArtReady, "placed B05 owns final functional art");
        Require(workbench.FunctionalArtRoot != null &&
                workbench.FunctionalArtRoot.Find("B05_ProcessHandle") != null &&
                workbench.FunctionalArtRoot.Find("B05_SuccessLight") != null,
            "purpose-built input/output/clamp kit and feedback parts are attached");

        Transform workDirection = workbench.FunctionalArtRoot.Find("B05_WorkDirection");
        Require(workDirection != null && Vector3.Dot(workDirection.forward, -workbench.transform.forward) > 0.999f,
            "modeled work side faces the placement interaction cells");
        BoxCollider box = workbench.GetComponent<BoxCollider>();
        NavMeshObstacle obstacle = workbench.GetComponent<NavMeshObstacle>();
        Require(box != null && obstacle != null && obstacle.carving && Vector3.Distance(box.size, obstacle.size) < 0.001f,
            "collider and carving obstacle align after finalization");

        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        Require(player != null, "player exists for actual interaction");
        Vector3 approachDirection = -workbench.transform.forward;
        player.transform.position = workbench.transform.position + approachDirection * 1.75f + Vector3.up * 0.05f;
        player.transform.rotation = Quaternion.LookRotation(-approachDirection, Vector3.up);
        Physics.SyncTransforms();
        Require(Vector3.Dot(player.transform.forward, (workbench.transform.position - player.transform.position).normalized) > 0.98f,
            "player stands on the reserved front side and faces the work surface");

        if (GameClock.Instance != null)
            GameClock.Instance.ForceSet(15.25f, 1, "PA_WorkbenchFinalizer final");
        foreach (DayNightVisual visual in UnityEngine.Object.FindObjectsByType<DayNightVisual>(FindObjectsSortMode.None))
            visual.ApplyHour(15);

        ClearRuntimeInventory();
        Item wood = Resources.Load<Item>("Items/Item_Wood");
        Item plank = Resources.Load<Item>("Items/Item_Plank");
        RecipeData recipe = Resources.Load<RecipeData>("Recipes/Recipe_Plank");
        Require(wood != null && plank != null && recipe != null, "existing Wood, Plank, and Recipe_Plank assets load");
        Require(Inventory.instance.AddInstance(new ItemInstance(wood, 2)
        {
            quality = 1f,
            currentPrice = wood.basePrice
        }), "two existing Wood inputs enter the player inventory");

        int woodBefore = CountItem(wood);
        int plankBefore = CountItem(plank);
        int feedbackBefore = workbench.CraftFeedbackCount;
        workbench.Interact(player);
        Require(CraftingUI.instance != null && CraftingUI.instance.craftingPanel != null &&
                CraftingUI.instance.craftingPanel.activeSelf,
            "actual Workbench interaction opens CraftingUI");

        Button plankButton = CraftingUI.instance.craftingPanel.GetComponentsInChildren<Button>(true)
            .FirstOrDefault(button =>
            {
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                return label != null && label.text.StartsWith(recipe.recipeName + "\n",
                    StringComparison.OrdinalIgnoreCase);
            });
        Require(plankButton != null, "CraftingUI shows the Workbench-filtered Plank recipe");
        plankButton.onClick.Invoke();
        Require(CountItem(wood) == woodBefore - 2, "Plank crafting consumes two Wood");
        Require(CountItem(plank) == plankBefore + 1, "Plank crafting creates one sellable Plank");
        Require(workbench.CraftFeedbackCount == feedbackBefore + 1 &&
                workbench.LastCraftedItem == plank.itemName && workbench.IsCraftFeedbackActive,
            "craft success drives the clamp/light feedback on the same Workbench");

        CraftingUI.instance.ToggleUI();
        Require(!CraftingUI.instance.craftingPanel.activeSelf, "CraftingUI closes back to the playable world");
        string output = Path.Combine(CaptureDirectory, "b05_runtime_after.png");
        await CaptureRuntimeWithGameCameraAsync(workbenchObject, player, output);
        Require(File.Exists(output) && new FileInfo(output).Length > 1024, "runtime final capture written");
        Debug.Log($"[B05 Runtime Final] capture={output} placement={placementId} crafted={plank.itemName}");
    }

    static void ClearRuntimeInventory()
    {
        Require(Inventory.instance != null, "Inventory exists");
        foreach (InventorySlot slot in Inventory.instance.slots) slot?.Clear();
        if (Inventory.instance.hotbar != null)
            foreach (InventorySlot slot in Inventory.instance.hotbar.slots) slot?.Clear();
        Inventory.instance.RefreshAllUI();
    }

    static int CountItem(Item item)
    {
        int count = 0;
        if (Inventory.instance == null || item == null) return count;
        CountItemInSlots(Inventory.instance.slots, item, ref count);
        if (Inventory.instance.hotbar != null)
            CountItemInSlots(Inventory.instance.hotbar.slots, item, ref count);
        return count;
    }

    static void CountItemInSlots(IEnumerable<InventorySlot> slots, Item item, ref int count)
    {
        if (slots == null) return;
        foreach (InventorySlot slot in slots)
            if (slot != null && !slot.IsEmpty && slot.item == item) count += slot.count;
    }

    static void RecoverShelfAt(ShopCustomizationController controller, Vector2Int cell)
    {
        string id = controller.FindPlacementIdAt(cell);
        if (string.IsNullOrEmpty(id)) return;
        GameObject target = controller.GetPlacementGameObject(id);
        ShopSlot slot = target != null ? target.GetComponent<ShopSlot>() : null;
        if (slot != null)
        {
            slot.currentItem = null;
            slot.displayPrice = 0;
            slot.RefreshDisplay();
        }
        Require(controller.TryRecoverPlacementForValidation(id, false, out string reason),
            $"temporary capture shelf at {cell} recovers ({reason})");
    }

    static async Task CaptureRuntimeWithGameCameraAsync(
        GameObject workbench, GameObject player, string outputPath)
    {
        Camera camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
        Require(camera != null, "MainCamera exists");
        Bounds bounds = CalculateRendererBounds(workbench);
        foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>(true)) bounds.Encapsulate(renderer.bounds);

        Vector3 direction = workbench.transform.TransformDirection(new Vector3(1f, 0.95f, -1f)).normalized;
        Vector3 cameraPosition = bounds.center + direction * 12f;
        float orthographicSize = Mathf.Max(3.8f, bounds.size.y * 1.65f);
        CameraController cameraController = camera.GetComponent<CameraController>()
            ?? camera.GetComponentInParent<CameraController>();
        bool controllerWasEnabled = cameraController != null && cameraController.enabled;
        CameraClearFlags previousClearFlags = camera.clearFlags;
        try
        {
            if (cameraController != null) cameraController.enabled = false;
            await PA_SafeGameViewCapture.CaptureAsync(outputPath, camera, captureCamera =>
            {
                captureCamera.transform.position = cameraPosition;
                captureCamera.transform.rotation =
                    Quaternion.LookRotation(bounds.center - cameraPosition, Vector3.up);
                captureCamera.orthographic = true;
                captureCamera.orthographicSize = orthographicSize;
                captureCamera.clearFlags = CameraClearFlags.Skybox;
                captureCamera.cullingMask = -1;
            }, 1920, 1080, 1000);
        }
        finally
        {
            camera.clearFlags = previousClearFlags;
            if (cameraController != null) cameraController.enabled = controllerWasEnabled;
        }
    }

    static void FailRuntimeCapture(string message)
    {
        _runtimeError = true;
        _runtimeRan = true;
        SessionState.SetBool(RuntimeErrorKey, true);
        SessionState.SetBool(RuntimeRanKey, true);
        Debug.LogError($"[B05 Runtime] FAIL {message}");
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
            ? "[B05 Runtime] finished successfully"
            : "[B05 Runtime] finished with failure");
        EditorApplication.Exit(!error && ran ? 0 : 1);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[B05 Check] OK {message}");
    }

    static async Task CaptureTurntableAsync(GameObject target, Bounds bounds, string prefix)
    {
        var views = new Dictionary<string, Vector3>
        {
            { "front_plus_z", new Vector3(0f, 0.28f, 1f) },
            { "back_minus_z", new Vector3(0f, 0.28f, -1f) },
            { "left_minus_x", new Vector3(-1f, 0.28f, 0f) },
            { "right_plus_x", new Vector3(1f, 0.28f, 0f) }
        };

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "B05_AuditGround";
        ground.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.01f, bounds.center.z);
        ground.transform.localScale = Vector3.one * Mathf.Max(0.4f, Mathf.Max(bounds.size.x, bounds.size.z) / 5f);
        Material groundMaterial = CreateTemporaryMaterial(new Color(0.64f, 0.70f, 0.43f, 1f));
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        GameObject sunObject = new GameObject("B05_AuditSun", typeof(Light));
        Light sun = sunObject.GetComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.35f;
        sun.color = new Color(1f, 0.86f, 0.68f);
        sunObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.47f, 0.52f, 0.58f);

        GameObject cameraObject = new GameObject("B05_AuditCamera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.72f, 0.84f, 0.88f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(1.7f, bounds.size.y * 0.72f);
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 100f;

        Vector3 focus = bounds.center + Vector3.up * bounds.size.y * 0.04f;
        float distance = Mathf.Max(bounds.size.magnitude * 1.8f, 5f);
        try
        {
            foreach (KeyValuePair<string, Vector3> view in views)
            {
                Vector3 direction = view.Value.normalized;
                Vector3 cameraPosition = focus + direction * distance;
                await PA_SafeGameViewCapture.CaptureAsync(
                    Path.Combine(CaptureDirectory, $"{prefix}_{view.Key}.png"), camera, captureCamera =>
                    {
                        captureCamera.transform.position = cameraPosition;
                        captureCamera.transform.rotation =
                            Quaternion.LookRotation(focus - cameraPosition, Vector3.up);
                        captureCamera.orthographic = true;
                        captureCamera.orthographicSize = Mathf.Max(1.7f, bounds.size.y * 0.72f);
                        captureCamera.clearFlags = CameraClearFlags.SolidColor;
                        captureCamera.backgroundColor = new Color(0.72f, 0.84f, 0.88f, 1f);
                        captureCamera.cullingMask = -1;
                    }, 1920, 1080, 1000);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(sunObject);
            UnityEngine.Object.DestroyImmediate(ground);
            UnityEngine.Object.DestroyImmediate(groundMaterial);
        }
    }

    static Bounds CalculateRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position + Vector3.up, new Vector3(3.2f, 2f, 2.6f));

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    static MeshAudit AuditMeshes(GameObject root)
    {
        var result = new MeshAudit
        {
            rendererCount = root.GetComponentsInChildren<Renderer>(true).Length
        };
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null) continue;
            result.meshCount++;
            result.vertexCount += mesh.vertexCount;
            result.subMeshCount += mesh.subMeshCount;
            for (int i = 0; i < mesh.subMeshCount; i++)
                result.triangleCount += (int)mesh.GetIndexCount(i) / 3;
        }
        return result;
    }

    static Material CreateTemporaryMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.12f);
        return material;
    }

    static Mesh BuildPreparationStaticMesh()
    {
        var builder = new MeshBuilder(4);

        // 가운데의 크림 작업판과 금색 가이드는 실제 Wood → Plank 공정을 읽게 한다.
        builder.AddBeveledBox(new Vector3(-0.06f, 1.075f, -0.34f),
            new Vector3(0.82f, 0.055f, 0.58f), 0.035f, Quaternion.identity, 1);
        builder.AddBeveledBox(new Vector3(-0.06f, 1.112f, -0.34f),
            new Vector3(0.055f, 0.035f, 0.54f), 0.014f, Quaternion.identity, 3);
        builder.AddBeveledBox(new Vector3(-0.41f, 1.11f, -0.34f),
            new Vector3(0.045f, 0.045f, 0.56f), 0.012f, Quaternion.identity, 0);
        builder.AddBeveledBox(new Vector3(0.29f, 1.11f, -0.34f),
            new Vector3(0.045f, 0.045f, 0.56f), 0.012f, Quaternion.identity, 0);

        // 왼쪽은 원재료, 오른쪽은 완성 판재다. 가짜 공구를 흩뿌리지 않는다.
        builder.AddBeveledBox(new Vector3(-0.73f, 1.13f, -0.33f),
            new Vector3(0.17f, 0.13f, 0.67f), 0.035f, Quaternion.Euler(0f, -4f, 0f), 0);
        builder.AddBeveledBox(new Vector3(-0.91f, 1.13f, -0.30f),
            new Vector3(0.14f, 0.11f, 0.57f), 0.03f, Quaternion.Euler(0f, 5f, 0f), 0);
        builder.AddBeveledBox(new Vector3(0.49f, 1.105f, -0.33f),
            new Vector3(0.24f, 0.065f, 0.70f), 0.025f, Quaternion.Euler(0f, 2f, 0f), 3);
        builder.AddBeveledBox(new Vector3(0.50f, 1.175f, -0.31f),
            new Vector3(0.22f, 0.065f, 0.64f), 0.025f, Quaternion.Euler(0f, -2f, 0f), 0);

        // 오른쪽 고정 클램프: 별도 handle 메시가 짧게 눌리며 제작 성공을 보여준다.
        builder.AddBeveledBox(new Vector3(0.82f, 1.11f, -0.34f),
            new Vector3(0.25f, 0.12f, 0.34f), 0.035f, Quaternion.identity, 2);
        builder.AddBeveledBox(new Vector3(0.82f, 1.24f, -0.47f),
            new Vector3(0.25f, 0.24f, 0.08f), 0.025f, Quaternion.identity, 2);
        builder.AddBeveledBox(new Vector3(0.82f, 1.24f, -0.21f),
            new Vector3(0.25f, 0.24f, 0.08f), 0.025f, Quaternion.identity, 2);
        builder.AddCylinder(new Vector3(0.82f, 1.35f, -0.34f), 0.055f, 0.20f,
            Vector3.up, 10, 3);

        return builder.Build("B05_Workbench_PreparationKit_Final");
    }

    static Mesh BuildProcessHandleMesh()
    {
        var builder = new MeshBuilder(4);
        builder.AddCylinder(new Vector3(0f, -0.08f, 0f), 0.035f, 0.28f,
            Vector3.up, 10, 3);
        builder.AddCylinder(new Vector3(0f, 0.04f, 0f), 0.032f, 0.42f,
            Vector3.right, 10, 2);
        builder.AddCylinder(new Vector3(-0.235f, 0.04f, 0f), 0.058f, 0.075f,
            Vector3.right, 10, 0);
        builder.AddCylinder(new Vector3(0.235f, 0.04f, 0f), 0.058f, 0.075f,
            Vector3.right, 10, 0);
        return builder.Build("B05_Workbench_ProcessHandle_Final");
    }

    static void EnsureAssetFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    sealed class MeshBuilder
    {
        readonly List<Vector3> _vertices = new List<Vector3>();
        readonly List<Vector3> _normals = new List<Vector3>();
        readonly List<Vector2> _uv = new List<Vector2>();
        readonly List<int>[] _triangles;

        public MeshBuilder(int materialCount)
        {
            _triangles = Enumerable.Range(0, materialCount).Select(_ => new List<int>()).ToArray();
        }

        public void AddBeveledBox(Vector3 center, Vector3 size, float bevel, Quaternion rotation, int material)
        {
            float hx = Mathf.Abs(size.x) * 0.5f;
            float hy = Mathf.Abs(size.y) * 0.5f;
            float hz = Mathf.Abs(size.z) * 0.5f;
            float b = Mathf.Clamp(bevel, 0.001f, Mathf.Min(hx, Mathf.Min(hy, hz)) * 0.49f);
            float ix = hx - b;
            float iy = hy - b;
            float iz = hz - b;

            // 여섯 중심면.
            AddFace(center, rotation, new[] { new Vector3(hx,-iy,-iz), new Vector3(hx,-iy,iz), new Vector3(hx,iy,iz), new Vector3(hx,iy,-iz) }, Vector3.right, material);
            AddFace(center, rotation, new[] { new Vector3(-hx,-iy,iz), new Vector3(-hx,-iy,-iz), new Vector3(-hx,iy,-iz), new Vector3(-hx,iy,iz) }, Vector3.left, material);
            AddFace(center, rotation, new[] { new Vector3(-ix,hy,-iz), new Vector3(ix,hy,-iz), new Vector3(ix,hy,iz), new Vector3(-ix,hy,iz) }, Vector3.up, material);
            AddFace(center, rotation, new[] { new Vector3(-ix,-hy,iz), new Vector3(ix,-hy,iz), new Vector3(ix,-hy,-iz), new Vector3(-ix,-hy,-iz) }, Vector3.down, material);
            AddFace(center, rotation, new[] { new Vector3(-ix,-iy,hz), new Vector3(-ix,iy,hz), new Vector3(ix,iy,hz), new Vector3(ix,-iy,hz) }, Vector3.forward, material);
            AddFace(center, rotation, new[] { new Vector3(ix,-iy,-hz), new Vector3(ix,iy,-hz), new Vector3(-ix,iy,-hz), new Vector3(-ix,-iy,-hz) }, Vector3.back, material);

            // 열두 모서리 면.
            foreach (int sx in new[] { -1, 1 })
            foreach (int sy in new[] { -1, 1 })
                AddFace(center, rotation, new[]
                {
                    new Vector3(sx * hx, sy * iy, -iz), new Vector3(sx * ix, sy * hy, -iz),
                    new Vector3(sx * ix, sy * hy, iz), new Vector3(sx * hx, sy * iy, iz)
                }, new Vector3(sx, sy, 0f).normalized, material);

            foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
                AddFace(center, rotation, new[]
                {
                    new Vector3(sx * hx, -iy, sz * iz), new Vector3(sx * ix, -iy, sz * hz),
                    new Vector3(sx * ix, iy, sz * hz), new Vector3(sx * hx, iy, sz * iz)
                }, new Vector3(sx, 0f, sz).normalized, material);

            foreach (int sy in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
                AddFace(center, rotation, new[]
                {
                    new Vector3(-ix, sy * hy, sz * iz), new Vector3(-ix, sy * iy, sz * hz),
                    new Vector3(ix, sy * iy, sz * hz), new Vector3(ix, sy * hy, sz * iz)
                }, new Vector3(0f, sy, sz).normalized, material);

            // 여덟 삼각 코너.
            foreach (int sx in new[] { -1, 1 })
            foreach (int sy in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
                AddFace(center, rotation, new[]
                {
                    new Vector3(sx * hx, sy * iy, sz * iz),
                    new Vector3(sx * ix, sy * hy, sz * iz),
                    new Vector3(sx * ix, sy * iy, sz * hz)
                }, new Vector3(sx, sy, sz).normalized, material);
        }

        public void AddCylinder(Vector3 center, float radius, float length, Vector3 axis, int segments, int material)
        {
            segments = Mathf.Max(6, segments);
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, axis.normalized);
            float half = length * 0.5f;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments;
                float a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 r0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
                Vector3 r1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
                AddFace(center, rotation, new[]
                {
                    r0 + Vector3.down * half, r1 + Vector3.down * half,
                    r1 + Vector3.up * half, r0 + Vector3.up * half
                }, new Vector3(Mathf.Cos((a0 + a1) * 0.5f), 0f, Mathf.Sin((a0 + a1) * 0.5f)), material);
            }

            Vector3[] top = Enumerable.Range(0, segments)
                .Select(i => new Vector3(Mathf.Cos(i * Mathf.PI * 2f / segments) * radius, half,
                    Mathf.Sin(i * Mathf.PI * 2f / segments) * radius)).ToArray();
            Vector3[] bottom = top.Select(v => new Vector3(v.x, -half, v.z)).Reverse().ToArray();
            AddFace(center, rotation, top, Vector3.up, material);
            AddFace(center, rotation, bottom, Vector3.down, material);
        }

        void AddFace(Vector3 center, Quaternion rotation, IReadOnlyList<Vector3> localPoints,
            Vector3 localNormal, int material)
        {
            var points = localPoints.Select(p => center + rotation * p).ToList();
            Vector3 normal = (rotation * localNormal).normalized;
            if (points.Count >= 3 && Vector3.Dot(Vector3.Cross(points[1] - points[0], points[2] - points[0]), normal) < 0f)
                points.Reverse();

            int start = _vertices.Count;
            foreach (Vector3 point in points)
            {
                _vertices.Add(point);
                _normals.Add(normal);
                _uv.Add(new Vector2(point.x, point.z));
            }
            for (int i = 1; i < points.Count - 1; i++)
            {
                _triangles[material].Add(start);
                _triangles[material].Add(start + i);
                _triangles[material].Add(start + i + 1);
            }
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uv);
            mesh.subMeshCount = _triangles.Length;
            for (int i = 0; i < _triangles.Length; i++) mesh.SetTriangles(_triangles[i], i, true);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);
            return mesh;
        }
    }

    struct MeshAudit
    {
        public int meshCount;
        public int rendererCount;
        public int vertexCount;
        public int triangleCount;
        public int subMeshCount;
    }
}
#endif
