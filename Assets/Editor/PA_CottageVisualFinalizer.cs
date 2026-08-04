#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// B10 Cottage visual audit/finalization entry point.
/// The imported Tripo FBX and the generated B10 wrapper prefab are treated as read-only sources.
/// </summary>
[InitializeOnLoad]
public static class PA_CottageVisualFinalizer
{
    const string ModelPath = "Assets/Models/Buildings/B10_Cottage.fbx";
    const string PrefabPath = "Assets/Prefabs/Buildings/B10_Cottage.prefab";
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string CaptureDirectory = "Logs/B10_CottageAudit";
    const string FinalMeshPath = "Assets/Art/ProjectPA/Buildings/B10/B10_Cottage_ShopSign.asset";
    const string FinalSignPrefabPath = "Assets/Resources/VisualFinalization/B10_Cottage_ShopSign.prefab";
    const string RuntimeActiveKey = "PA.B10FinalCapture.Active";
    const string RuntimeEnteredKey = "PA.B10FinalCapture.Entered";
    const string RuntimeRanKey = "PA.B10FinalCapture.Ran";
    const string RuntimeErrorKey = "PA.B10FinalCapture.Error";

    static bool _runtimeEntered;
    static bool _runtimeRan;
    static bool _runtimeError;
    static double _runtimeStartedAt;
    static double _runtimeCaptureAt;
    static Task _runtimeTask;

    static PA_CottageVisualFinalizer()
    {
        if (!SessionState.GetBool(RuntimeActiveKey, false)) return;
        _runtimeEntered = SessionState.GetBool(RuntimeEnteredKey, false);
        _runtimeRan = SessionState.GetBool(RuntimeRanKey, false);
        _runtimeError = SessionState.GetBool(RuntimeErrorKey, false);
        RegisterRuntimeCallbacks();
        if (_runtimeRan && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += FinishRuntimeCapture;
    }

    [MenuItem("Project PA/Audit/Audit B10 Cottage Visual")]
    public static async void RunAuditBatch()
    {
        Debug.Log($"[B10 Audit] BEGIN Unity={Application.unityVersion}");

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Require(model != null, $"model missing: {ModelPath}");
        Require(prefab != null, $"prefab missing: {PrefabPath}");

        AuditHierarchy("SOURCE_MODEL", model);
        AuditHierarchy("WRAPPER_PREFAB", prefab);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Require(scene.IsValid() && scene.isLoaded, $"scene failed to load: {ScenePath}");

        Transform[] cottages = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t != null && t.name.StartsWith("B10_Cottage", StringComparison.Ordinal))
            .Where(t => t.parent == null || !t.parent.name.StartsWith("B10_Cottage", StringComparison.Ordinal))
            .OrderBy(t => t.name, StringComparer.Ordinal)
            .ToArray();

        Require(cottages.Length > 0, "no B10 Cottage scene instances found");
        foreach (Transform cottage in cottages)
            AuditSceneInstance(cottage.gameObject);
        AuditEntranceOverlay(cottages[0]);

        string capturePath = Path.Combine(CaptureDirectory, "b10_cottage_before.png");
        await CaptureWithGameCameraAsync(cottages[0].gameObject, capturePath);
        await CaptureIsolatedTurntableAsync(cottages[0].gameObject);
        Debug.Log($"[B10 Audit] CAPTURE {capturePath}");
        Debug.Log("[B10 Audit] PASS");
    }

    [MenuItem("Project PA/Fix/Build B10 Cottage Final Assets")]
    public static void BuildFinalAssetsBatch()
    {
        EnsureAssetFolder("Assets/Art/ProjectPA/Buildings/B10");
        EnsureAssetFolder("Assets/Resources/VisualFinalization");

        Mesh generated = BuildShopSignMesh();
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(FinalMeshPath);
        if (mesh == null)
        {
            mesh = generated;
            AssetDatabase.CreateAsset(mesh, FinalMeshPath);
        }
        else
        {
            EditorUtility.CopySerialized(generated, mesh);
            UnityEngine.Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(mesh);
        }

        Material darkWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Market/PA_Market_DarkWood.mat");
        Material cream = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Market/PA_Market_AwningCream.mat");
        Require(darkWood != null && cream != null, "market wood/cream materials missing");

        var root = new GameObject("B10_Cottage_ShopSign");
        try
        {
            var filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { darkWood, cream };
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            Require(PrefabUtility.SaveAsPrefabAsset(root, FinalSignPrefabPath) != null,
                $"failed to save {FinalSignPrefabPath}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[B10 Final Assets] PASS mesh={FinalMeshPath} prefab={FinalSignPrefabPath}");
    }

    [MenuItem("Project PA/Validation/Validate B10 Cottage Finalization")]
    public static async void RunFinalValidationBatch()
    {
        BuildFinalAssetsBatch();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Require(scene.IsValid() && scene.isLoaded, $"scene failed to load: {ScenePath}");

        var host = new GameObject("B10_Finalization_ValidationHost");
        CottageVisualFinalizationController controller = host.AddComponent<CottageVisualFinalizationController>();
        Require(controller.ApplyFinalization(), "runtime finalization applies in loaded main scene");
        Require(controller.IsApplied, "controller reports applied state");
        Require(controller.FinalizedCottageCount == 3, "three map cottages finalized");
        Require(controller.LegacyDuplicateDisabled, "legacy B10 duplicate disabled when map cottages exist");
        Require(controller.ExteriorDoor != null, "exterior BuildingEntrance object preserved");
        Require(controller.ExteriorDoor.GetComponent<BuildingEntrance>() != null, "BuildingEntrance component preserved");
        Require(controller.ExteriorDoor.GetComponent<MeshRenderer>() != null &&
                !controller.ExteriorDoor.GetComponent<MeshRenderer>().enabled,
            "detached primitive door renderer hidden while collider remains");
        Require(controller.ExteriorDoor.GetComponent<Collider>() != null &&
                controller.ExteriorDoor.GetComponent<Collider>().enabled,
            "exterior interaction collider remains enabled");
        MeshFilter finalSignFilter = controller.FinalShopSign != null
            ? controller.FinalShopSign.GetComponent<MeshFilter>()
            : null;
        MeshRenderer finalSignRenderer = controller.FinalShopSign != null
            ? controller.FinalShopSign.GetComponent<MeshRenderer>()
            : null;
        Require(finalSignFilter != null && finalSignFilter.sharedMesh != null &&
                finalSignFilter.sharedMesh.name == "B10_Cottage_ShopSign_Final" &&
                finalSignRenderer != null && finalSignRenderer.enabled,
            "purpose-built shop sign root mesh is attached");

        Shop plazaShop = PA_ShopLocator.FindPlazaShop();
        Require(plazaShop != null, "plaza shop anchor exists");
        foreach (string name in new[] { "B10_Cottage_01", "B10_Cottage_02", "B10_Cottage_03" })
        {
            GameObject cottage = GameObject.Find(name);
            Require(cottage != null, $"{name} exists");
            Vector3 toPlaza = plazaShop.transform.position - cottage.transform.position;
            toPlaza.y = 0f;
            Require(Vector3.Dot(cottage.transform.right * -1f, toPlaza.normalized) > 0.995f,
                $"{name} authored -X facade faces plaza");
        }

        GameObject target = GameObject.Find("B10_Cottage_01");
        string capture = Path.Combine(CaptureDirectory, "b10_cottage_after.png");
        await CaptureWithGameCameraAsync(target, capture);
        Require(File.Exists(capture) && new FileInfo(capture).Length > 1024, "after capture written");
        Debug.Log($"[B10 Final Validation] CAPTURE {capture}");
        Debug.Log("[B10 Final Validation] PASS");
    }

    [MenuItem("Project PA/Validation/Capture B10 Cottage In Play Mode")]
    public static void RunFinalGameCaptureBatch()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Require(scene.IsValid() && scene.isLoaded, $"scene failed to load: {ScenePath}");

        _runtimeEntered = false;
        _runtimeRan = false;
        _runtimeError = false;
        _runtimeStartedAt = EditorApplication.timeSinceStartup;
        _runtimeCaptureAt = double.MaxValue;
        _runtimeTask = null;
        SessionState.SetBool(RuntimeActiveKey, true);
        SessionState.SetBool(RuntimeEnteredKey, false);
        SessionState.SetBool(RuntimeRanKey, false);
        SessionState.SetBool(RuntimeErrorKey, false);
        RegisterRuntimeCallbacks();
        Debug.Log("[B10 Runtime Capture] entering Play Mode");
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
            _runtimeCaptureAt = _runtimeStartedAt + 3.0;
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
            if (elapsed > 60.0) FailRuntimeCapture("timed out before Play Mode");
            return;
        }

        if (_runtimeTask != null)
        {
            if (!_runtimeTask.IsCompleted)
            {
                if (elapsed > 90.0) FailRuntimeCapture("timed out during capture");
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
                Debug.LogError($"[B10 Runtime Capture] FAIL {ex.Message}\n{ex}");
            }

            _runtimeTask = null;
            _runtimeRan = true;
            SessionState.SetBool(RuntimeRanKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }

        if (!_runtimeRan && EditorApplication.isPlaying &&
            EditorApplication.timeSinceStartup >= _runtimeCaptureAt)
        {
            _runtimeTask = RunRuntimeCaptureAsync();
            return;
        }

        if (!_runtimeRan && elapsed > 90.0) FailRuntimeCapture("timed out before capture");
    }

    static async Task RunRuntimeCaptureAsync()
    {
        Time.timeScale = 1f;
        if (GameClock.Instance != null)
            GameClock.Instance.ForceSet(15.25f, 1, "PA_CottageVisualFinalizer runtime capture");
        foreach (DayNightVisual visual in UnityEngine.Object.FindObjectsByType<DayNightVisual>(
                     FindObjectsSortMode.None))
            visual.ApplyHour(15);
        CottageVisualFinalizationController controller = CottageVisualFinalizationController.Instance
            ?? UnityEngine.Object.FindFirstObjectByType<CottageVisualFinalizationController>();
        Require(controller != null && controller.IsApplied, "runtime B10 controller applied");
        Require(controller.ExteriorDoor != null &&
                controller.ExteriorDoor.GetComponent<BuildingEntrance>() != null,
            "runtime exterior entrance preserved");
        Require(controller.ExteriorDoor.GetComponentInChildren<PrototypeWorldLabel>(true) != null,
            "runtime tier label remains below exterior entrance");

        GameObject cottage = GameObject.Find("B10_Cottage_01");
        Require(cottage != null, "runtime shop cottage exists");
        string capture = Path.Combine(CaptureDirectory, "b10_cottage_runtime.png");
        await CaptureWithGameCameraAsync(cottage, capture);
        Require(File.Exists(capture) && new FileInfo(capture).Length > 1024,
            "runtime camera capture written");
        Debug.Log($"[B10 Runtime Capture] PASS {capture}");
    }

    static void FailRuntimeCapture(string message)
    {
        _runtimeError = true;
        _runtimeRan = true;
        SessionState.SetBool(RuntimeErrorKey, true);
        SessionState.SetBool(RuntimeRanKey, true);
        Debug.LogError($"[B10 Runtime Capture] FAIL {message}");
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
        Debug.Log(!error && ran
            ? "[B10 Runtime Capture] finished successfully"
            : "[B10 Runtime Capture] finished with failure");
        EditorApplication.Exit(!error && ran ? 0 : 1);
    }

    static Mesh BuildShopSignMesh()
    {
        Vector2[] outer =
        {
            new(-0.91f, -0.27f), new(0.91f, -0.27f),
            new(1.02f, -0.16f), new(1.02f, 0.16f),
            new(0.91f, 0.27f), new(-0.91f, 0.27f),
            new(-1.02f, 0.16f), new(-1.02f, -0.16f)
        };
        Vector2[] inner = outer.Select(p => new Vector2(p.x * 0.86f, p.y * 0.67f)).ToArray();
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var wood = new List<int>();
        var cream = new List<int>();

        int outerFront = AddLoop(vertices, uv, outer, 0.045f);
        int outerBack = AddLoop(vertices, uv, outer, -0.045f);
        int innerFront = AddLoop(vertices, uv, inner, 0.052f);
        int backCenter = AddVertex(vertices, uv, new Vector3(0f, 0f, -0.045f));
        int faceCenter = AddVertex(vertices, uv, new Vector3(0f, 0f, 0.055f));

        for (int i = 0; i < outer.Length; i++)
        {
            int next = (i + 1) % outer.Length;
            AddQuad(wood, outerFront + i, outerFront + next, outerBack + next, outerBack + i);
            AddQuad(wood, outerFront + i, innerFront + i, innerFront + next, outerFront + next);
            wood.Add(backCenter); wood.Add(outerBack + next); wood.Add(outerBack + i);
            cream.Add(faceCenter); cream.Add(innerFront + i); cream.Add(innerFront + next);
        }

        var mesh = new Mesh { name = "B10_Cottage_ShopSign_Final" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(wood, 0, true);
        mesh.SetTriangles(cream, 1, true);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    static int AddLoop(List<Vector3> vertices, List<Vector2> uv, Vector2[] points, float z)
    {
        int start = vertices.Count;
        foreach (Vector2 point in points)
            AddVertex(vertices, uv, new Vector3(point.x, point.y, z));
        return start;
    }

    static int AddVertex(List<Vector3> vertices, List<Vector2> uv, Vector3 point)
    {
        int index = vertices.Count;
        vertices.Add(point);
        uv.Add(new Vector2(Mathf.InverseLerp(-1.05f, 1.05f, point.x), Mathf.InverseLerp(-0.3f, 0.3f, point.y)));
        return index;
    }

    static void AddQuad(List<int> triangles, int a, int b, int c, int d)
    {
        triangles.Add(a); triangles.Add(b); triangles.Add(c);
        triangles.Add(a); triangles.Add(c); triangles.Add(d);
    }

    static void EnsureAssetFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static void AuditHierarchy(string label, GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        Debug.Log($"[B10 Audit] {label} root={root.name} renderers={renderers.Length} meshes={filters.Length}");

        if (TryGetRendererBounds(root, out Bounds rendererBounds))
            Debug.Log($"[B10 Audit] {label} rendererBounds center={V(rendererBounds.center)} size={V(rendererBounds.size)}");

        foreach (MeshFilter filter in filters)
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null) continue;
            Debug.Log($"[B10 Audit] {label} mesh path={PathOf(filter.transform, root.transform)} " +
                      $"name={mesh.name} vertices={mesh.vertexCount} subMeshes={mesh.subMeshCount} " +
                      $"localBounds.center={V(mesh.bounds.center)} localBounds.size={V(mesh.bounds.size)} " +
                      $"transform.pos={V(filter.transform.localPosition)} transform.rot={V(filter.transform.localEulerAngles)} " +
                      $"transform.scale={V(filter.transform.localScale)}");
            AuditConnectedComponents(label, filter, mesh);
        }
    }

    static void AuditConnectedComponents(string label, MeshFilter filter, Mesh mesh)
    {
        using Mesh.MeshDataArray dataArray = Mesh.AcquireReadOnlyMeshData(mesh);
        Mesh.MeshData data = dataArray[0];
        int vertexCount = data.vertexCount;
        if (vertexCount == 0) return;

        int[] parent = new int[vertexCount];
        bool[] used = new bool[vertexCount];
        for (int i = 0; i < vertexCount; i++) parent[i] = i;

        if (mesh.indexFormat == IndexFormat.UInt16)
        {
            NativeArray<ushort> indices = data.GetIndexData<ushort>();
            ConnectSubMeshes(data, indices, parent, used);
        }
        else
        {
            NativeArray<uint> indices = data.GetIndexData<uint>();
            ConnectSubMeshes(data, indices, parent, used);
        }

        using var positions = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
        data.GetVertices(positions);

        var components = new Dictionary<int, ComponentInfo>();
        for (int i = 0; i < vertexCount; i++)
        {
            if (!used[i]) continue;
            int root = Find(parent, i);
            if (!components.TryGetValue(root, out ComponentInfo info))
                info = new ComponentInfo(positions[i]);
            else
                info.bounds.Encapsulate(positions[i]);
            info.vertexCount++;
            components[root] = info;
        }

        ComponentInfo[] ordered = components.Values
            .OrderByDescending(c => c.bounds.size.x * c.bounds.size.y * c.bounds.size.z)
            .ThenByDescending(c => c.vertexCount)
            .ToArray();
        Debug.Log($"[B10 Audit] {label} mesh={mesh.name} connectedComponents={ordered.Length}");
        for (int i = 0; i < ordered.Length; i++)
        {
            ComponentInfo component = ordered[i];
            Bounds world = TransformBounds(filter.transform, component.bounds);
            Debug.Log($"[B10 Audit] {label} component[{i:00}] vertices={component.vertexCount} " +
                      $"local.center={V(component.bounds.center)} local.size={V(component.bounds.size)} " +
                      $"world.center={V(world.center)} world.size={V(world.size)}");
        }
    }

    static void ConnectSubMeshes<T>(Mesh.MeshData data, NativeArray<T> indices, int[] parent, bool[] used)
        where T : unmanaged
    {
        for (int subMeshIndex = 0; subMeshIndex < data.subMeshCount; subMeshIndex++)
        {
            SubMeshDescriptor sub = data.GetSubMesh(subMeshIndex);
            int primitiveSize = sub.topology == MeshTopology.Triangles ? 3
                : sub.topology == MeshTopology.Quads ? 4
                : sub.topology == MeshTopology.Lines ? 2
                : 1;
            int end = sub.indexStart + sub.indexCount;
            for (int i = sub.indexStart; i + primitiveSize - 1 < end; i += primitiveSize)
            {
                int first = ConvertIndex(indices[i]) + sub.baseVertex;
                if (!Valid(first, parent.Length)) continue;
                used[first] = true;
                for (int offset = 1; offset < primitiveSize; offset++)
                {
                    int next = ConvertIndex(indices[i + offset]) + sub.baseVertex;
                    if (!Valid(next, parent.Length)) continue;
                    used[next] = true;
                    Union(parent, first, next);
                }
            }
        }
    }

    static int ConvertIndex<T>(T value) where T : unmanaged
    {
        if (typeof(T) == typeof(ushort)) return Convert.ToUInt16(value);
        return checked((int)Convert.ToUInt32(value));
    }

    static void AuditSceneInstance(GameObject root)
    {
        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
        string sourceName = PrefabUtility.GetCorrespondingObjectFromSource(root)?.name ?? "none";
        string bounds = TryGetRendererBounds(root, out Bounds value)
            ? $"center={V(value.center)} size={V(value.size)} minY={value.min.y:F3}"
            : "none";
        Debug.Log($"[B10 Audit] SCENE name={root.name} path={PathOf(root.transform, null)} " +
                  $"position={V(root.transform.position)} rotation={V(root.transform.eulerAngles)} scale={V(root.transform.lossyScale)} " +
                  $"prefab={prefabPath} source={sourceName} bounds={bounds}");

        foreach (BoxCollider box in root.GetComponentsInChildren<BoxCollider>(true))
            Debug.Log($"[B10 Audit] COLLIDER path={PathOf(box.transform, root.transform)} center={V(box.center)} size={V(box.size)} enabled={box.enabled}");
    }

    static void AuditEntranceOverlay(Transform cottage)
    {
        GameObject door = GameObject.Find("PA_StoreDoor_Out");
        GameObject outsideSpawn = GameObject.Find("PlayerSpawn_Outside");
        Require(door != null, "PA_StoreDoor_Out missing");
        Vector3 localPosition = cottage.InverseTransformPoint(door.transform.position);
        float yawDelta = Mathf.DeltaAngle(cottage.eulerAngles.y, door.transform.eulerAngles.y);
        string doorBounds = TryGetRendererBounds(door, out Bounds bounds)
            ? $"center={V(bounds.center)} size={V(bounds.size)}"
            : "none";
        Debug.Log($"[B10 Audit] OVERLAY door.path={PathOf(door.transform, null)} parent={door.transform.parent?.name ?? "none"} " +
                  $"world.pos={V(door.transform.position)} localToCottage={V(localPosition)} yawDelta={yawDelta:F1} bounds={doorBounds}");
        if (outsideSpawn != null)
            Debug.Log($"[B10 Audit] OVERLAY outsideSpawn.path={PathOf(outsideSpawn.transform, null)} " +
                      $"position={V(outsideSpawn.transform.position)} rotation={V(outsideSpawn.transform.eulerAngles)}");

        Renderer[] nearby = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(r => r != null && Vector3.Distance(r.bounds.center, cottage.position) < 10f)
            .OrderBy(r => Vector3.Distance(r.bounds.center, cottage.position))
            .ToArray();
        foreach (Renderer renderer in nearby)
        {
            bool belongsToCottage = renderer.transform.IsChildOf(cottage);
            Debug.Log($"[B10 Audit] NEARBY cottage={cottage.name} belongs={belongsToCottage} " +
                      $"path={PathOf(renderer.transform, null)} center={V(renderer.bounds.center)} size={V(renderer.bounds.size)}");
        }
    }

    static Task CaptureWithGameCameraAsync(GameObject cottage, string outputPath)
    {
        return CaptureWithGameCameraAsync(cottage, outputPath, null);
    }

    static async Task CaptureIsolatedTurntableAsync(GameObject cottage)
    {
        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        var states = renderers.ToDictionary(r => r, r => r.enabled);
        try
        {
            foreach (Renderer renderer in renderers)
                if (!renderer.transform.IsChildOf(cottage.transform)) renderer.enabled = false;

            await CaptureWithGameCameraAsync(cottage,
                Path.Combine(CaptureDirectory, "b10_isolated_front.png"), new Vector3(0f, 0.65f, -1f));
            await CaptureWithGameCameraAsync(cottage,
                Path.Combine(CaptureDirectory, "b10_isolated_back.png"), new Vector3(0f, 0.65f, 1f));
            await CaptureWithGameCameraAsync(cottage,
                Path.Combine(CaptureDirectory, "b10_isolated_left.png"), new Vector3(-1f, 0.65f, 0f));
            await CaptureWithGameCameraAsync(cottage,
                Path.Combine(CaptureDirectory, "b10_isolated_right.png"), new Vector3(1f, 0.65f, 0f));
        }
        finally
        {
            foreach (KeyValuePair<Renderer, bool> state in states)
                if (state.Key != null) state.Key.enabled = state.Value;
        }
    }

    static async Task CaptureWithGameCameraAsync(
        GameObject cottage, string outputPath, Vector3? localViewDirection)
    {
        Camera camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
        Require(camera != null, "game camera missing");
        Require(TryGetRendererBounds(cottage, out Bounds bounds), "cottage renderer bounds missing");

        Vector3 viewDirection = localViewDirection.HasValue
            ? cottage.transform.TransformDirection(localViewDirection.Value).normalized
            : new Vector3(1f, 0.85f, -1f).normalized;
        Vector3 cameraPosition = bounds.center + viewDirection * 18f;
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
                captureCamera.orthographicSize = 5.5f;
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

    static Bounds TransformBounds(Transform transform, Bounds local)
    {
        Vector3 min = local.min;
        Vector3 max = local.max;
        Vector3[] corners =
        {
            new(min.x, min.y, min.z), new(min.x, min.y, max.z),
            new(min.x, max.y, min.z), new(min.x, max.y, max.z),
            new(max.x, min.y, min.z), new(max.x, min.y, max.z),
            new(max.x, max.y, min.z), new(max.x, max.y, max.z)
        };
        Bounds world = new Bounds(transform.TransformPoint(corners[0]), Vector3.zero);
        for (int i = 1; i < corners.Length; i++) world.Encapsulate(transform.TransformPoint(corners[i]));
        return world;
    }

    static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
            .Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy)
            .ToArray();
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    static string PathOf(Transform transform, Transform stopExclusive)
    {
        var names = new List<string>();
        for (Transform current = transform; current != null && current != stopExclusive; current = current.parent)
            names.Add(current.name);
        names.Reverse();
        return string.Join("/", names);
    }

    static string V(Vector3 value) => $"({value.x:F3},{value.y:F3},{value.z:F3})";
    static bool Valid(int index, int length) => index >= 0 && index < length;

    static int Find(int[] parent, int value)
    {
        int root = value;
        while (parent[root] != root) root = parent[root];
        while (parent[value] != value)
        {
            int next = parent[value];
            parent[value] = root;
            value = next;
        }
        return root;
    }

    static void Union(int[] parent, int a, int b)
    {
        int rootA = Find(parent, a);
        int rootB = Find(parent, b);
        if (rootA != rootB) parent[rootB] = rootA;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"[B10 Audit] FAIL: {message}");
    }

    struct ComponentInfo
    {
        public Bounds bounds;
        public int vertexCount;

        public ComponentInfo(Vector3 point)
        {
            bounds = new Bounds(point, Vector3.zero);
            vertexCount = 0;
        }
    }
}
#endif
