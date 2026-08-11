#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PA_WorldSandboxTools
{
    public const string ScenePath = "Assets/Scenes/WorldSandbox.unity";

    const string ActiveKey = "PA.World001.Active";
    const string RanKey = "PA.World001.Ran";
    const string ErrorKey = "PA.World001.Error";
    const string StageKey = "PA.World001.Stage";
    const string WaitFramesKey = "PA.World001.WaitFrames";
    const string RuntimeHashKey = "PA.World001.RuntimeHash";
    const string ConsoleErrorCountKey = "PA.World001.ConsoleErrorCount";

    static readonly string[] ForbiddenManagerTypeNames =
    {
        "GameManager",
        "SaveManager",
        "EconomyService",
        "Inventory",
        "Hotbar",
        "Shop",
        "NpcController",
        "PA_RuntimeSceneBinder"
    };

    static double _runtimeDeadline;

    static PA_WorldSandboxTools()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        RegisterCallbacks();
        if (SessionState.GetBool(RanKey, false) &&
            !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += FinishValidation;
        }
    }

    [MenuItem("Project PA/World/Build WorldSandbox")]
    public static void BuildWorldSandbox()
    {
        try
        {
            GuardCurrentSceneBeforeBuild();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateDirectionalLight();
            CreateWorldGridRoot();

            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.48f, 0.52f, 1f);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException($"Failed to save {ScenePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateScene(scene, false);
            Require(!scene.isDirty, "builder leaves the saved WorldSandbox scene clean");
            Debug.Log($"[WORLD-001 Builder] PASS saved {ScenePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WORLD-001 Builder] FAIL {ex.Message}\n{ex}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    [MenuItem("Project PA/Validation/Run WORLD-001 World Grid Validation")]
    public static void RunValidation()
    {
        try
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene, false);
            WorldGridService service = FindSingleSceneComponent<WorldGridService>(scene);
            ulong editHash = ValidateGridContract(service, false);

            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(RanKey, false);
            SessionState.SetBool(ErrorKey, false);
            SessionState.SetInt(StageKey, 0);
            SessionState.SetInt(WaitFramesKey, 0);
            SessionState.SetString(RuntimeHashKey, editHash.ToString("X16"));
            SessionState.SetInt(ConsoleErrorCountKey, 0);
            _runtimeDeadline = EditorApplication.timeSinceStartup + 30d;
            RegisterCallbacks();
            Debug.Log("[WORLD-001] Entering D3D11 Play Mode validation");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WORLD-001] FAIL before Play Mode: {ex.Message}\n{ex}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    static void GuardCurrentSceneBeforeBuild()
    {
        Scene current = EditorSceneManager.GetActiveScene();
        if (!current.IsValid() || !current.isDirty) return;
        throw new InvalidOperationException(
            $"Active scene '{current.path}' has unsaved changes. Save or discard them explicitly before building WorldSandbox.");
    }

    static void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(15f, 32f, 15f);
        cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 17.5f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.075f, 0.095f, 1f);
        cameraObject.AddComponent<AudioListener>();
    }

    static void CreateDirectionalLight()
    {
        var lightObject = new GameObject("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.94f, 0.84f, 1f);
        light.intensity = 1.05f;
        light.shadows = LightShadows.Soft;
    }

    static void CreateWorldGridRoot()
    {
        var gridObject = new GameObject("WorldGrid");
        gridObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        gridObject.AddComponent<WorldGridService>();
        gridObject.AddComponent<WorldGridDebugView>();
    }

    static void RegisterCallbacks()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        Application.logMessageReceived -= OnLogMessage;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
        Application.logMessageReceived += OnLogMessage;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _runtimeDeadline = EditorApplication.timeSinceStartup + 30d;
            SessionState.SetInt(StageKey, 10);
            SessionState.SetInt(WaitFramesKey, 0);
        }
        else if (state == PlayModeStateChange.EnteredEditMode &&
                 SessionState.GetBool(RanKey, false))
        {
            FinishValidation();
        }
    }

    static void OnEditorUpdate()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
        if (EditorApplication.timeSinceStartup > _runtimeDeadline)
        {
            FailValidation(new TimeoutException("WORLD-001 runtime validator exceeded 30 seconds."));
            return;
        }

        int waitFrames = SessionState.GetInt(WaitFramesKey, 0);
        int requiredFrames = SessionState.GetInt(StageKey, 0) == 10 ? 3 : 8;
        if (waitFrames < requiredFrames)
        {
            SessionState.SetInt(WaitFramesKey, waitFrames + 1);
            return;
        }

        try
        {
            switch (SessionState.GetInt(StageKey, 0))
            {
                case 10:
                    ValidateRuntimeStart();
                    break;
                case 20:
                    ValidateRuntimeReadOnlyStability();
                    break;
            }
        }
        catch (Exception ex)
        {
            FailValidation(ex);
        }
    }

    static void ValidateRuntimeStart()
    {
        Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
            $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
        Scene scene = SceneManager.GetActiveScene();
        Require(scene.path == ScenePath, $"WorldSandbox is the active Play Mode scene ({scene.path})");
        ValidateScene(scene, true);

        WorldGridService service = FindSingleSceneComponent<WorldGridService>(scene);
        bool generatedRuntime = WorldGameplayAdapterService.Instance != null &&
                                WorldGameplayAdapterService.Instance.IsReady;
        ulong runtimeHash = ValidateGridContract(service, generatedRuntime);
        string editHash = SessionState.GetString(RuntimeHashKey, string.Empty);
        if (generatedRuntime)
        {
            ulong generatedHash = ComputeCellHash(WorldIslandGenerator.Generate(
                WorldGameplayAdapterService.DefaultWorldSeed).TerrainCells);
            Require(runtimeHash == generatedHash,
                $"runtime grid matches deterministic seed 9009 ({runtimeHash:X16})");
        }
        else
        {
            Require(runtimeHash.ToString("X16") == editHash,
                $"Edit/Play deterministic cell checksum matches ({runtimeHash:X16})");
        }

        WorldGridDebugView debugView = FindSingleSceneComponent<WorldGridDebugView>(scene);
        Require(debugView.IsRuntimeGeometryReady,
            $"runtime debug line mesh is ready ({debugView.DebugLineVertexCount} vertices)");
        Require(debugView.CoordinateOverlayEnabled, "runtime coordinate overlay is enabled");
        MeshFilter debugFilter = debugView.GetComponentsInChildren<MeshFilter>(true)
            .SingleOrDefault(filter => filter.sharedMesh != null &&
                                       filter.sharedMesh.name == "WorldGridDebugLineMesh");
        Require(debugFilter != null && debugFilter.sharedMesh != null, "debug mesh renderer has a mesh");
        Require(debugFilter.sharedMesh.subMeshCount == 3, "grid/chunk/origin use three debug submeshes");
        Require(debugFilter.sharedMesh.GetIndexCount(1) > 0, "chunk boundary has differentiated line indices");

        SessionState.SetString(RuntimeHashKey, runtimeHash.ToString("X16"));
        SessionState.SetInt(StageKey, 20);
        SessionState.SetInt(WaitFramesKey, 0);
    }

    static void ValidateRuntimeReadOnlyStability()
    {
        Scene scene = SceneManager.GetActiveScene();
        WorldGridService service = FindSingleSceneComponent<WorldGridService>(scene);
        ulong afterFramesHash = ComputeCellHash(service.Cells);
        string beforeFramesHash = SessionState.GetString(RuntimeHashKey, string.Empty);
        Require(afterFramesHash.ToString("X16") == beforeFramesHash,
            $"runtime cell data remains unchanged across frames ({afterFramesHash:X16})");
        Require(SessionState.GetInt(ConsoleErrorCountKey, 0) == 0,
            "blocking runtime Console Error/Exception/Assert count is 0");

        SessionState.SetBool(RanKey, true);
        Debug.Log("[WORLD-001] PLAY_MODE_PASS");
        EditorApplication.ExitPlaymode();
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        SessionState.SetInt(ConsoleErrorCountKey, SessionState.GetInt(ConsoleErrorCountKey, 0) + 1);
    }

    static void FailValidation(Exception ex)
    {
        SessionState.SetBool(ErrorKey, true);
        SessionState.SetBool(RanKey, true);
        Debug.LogError($"[WORLD-001] FAIL {ex.Message}\n{ex}");
        EditorApplication.ExitPlaymode();
    }

    static void FinishValidation()
    {
        bool failed = SessionState.GetBool(ErrorKey, false);
        int consoleErrors = SessionState.GetInt(ConsoleErrorCountKey, 0);
        if (consoleErrors != 0) failed = true;

        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(RanKey);
        SessionState.EraseBool(ErrorKey);
        SessionState.EraseInt(StageKey);
        SessionState.EraseInt(WaitFramesKey);
        SessionState.EraseString(RuntimeHashKey);
        SessionState.EraseInt(ConsoleErrorCountKey);
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        Application.logMessageReceived -= OnLogMessage;

        Debug.Log(failed
            ? $"[WORLD-001] FINISHED_WITH_ERRORS consoleErrors={consoleErrors}"
            : "[WORLD-001] FINISHED_PASS width=16 height=16 cells=256 cellSize=2 chunkSize=16 elevation=0..6 runtimeReadOnly=true");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void ValidateScene(Scene scene, bool runtime)
    {
        Require(scene.IsValid() && scene.isLoaded, "WorldSandbox scene is valid and loaded");
        Require(scene.path == ScenePath, $"validator targets only {ScenePath}");

        GameObject[] roots = scene.GetRootGameObjects();
        if (runtime)
            Require(roots.Length >= 3,
                $"runtime keeps the 3 authored roots after global bootstrap ({roots.Length} total roots)");
        else
            Require(roots.Length == 3, $"minimal authored scene has exactly 3 roots ({roots.Length})");
        Require(roots.Count(root => root.name == "Main Camera") == 1, "scene has one Main Camera root");
        Require(roots.Count(root => root.name == "Directional Light") == 1, "scene has one Directional Light root");
        Require(roots.Count(root => root.name == "WorldGrid") == 1, "scene has one WorldGrid root");
        Require(FindSceneComponents<Camera>(scene).Count == 1, "scene has exactly one Camera");
        List<Light> lights = FindSceneComponents<Light>(scene);
        if (runtime)
        {
            Require(lights.Count(candidate => candidate != null &&
                        candidate.transform.root.name == "Directional Light" &&
                        candidate.type == LightType.Directional) == 1 &&
                    lights.All(candidate => candidate != null &&
                        (candidate.transform.root.name == "Directional Light" ||
                         candidate.transform.root.name == WorldGameplayAdapterService.RuntimeRootName)),
                $"runtime keeps one authored Directional Light and only adapter-local helper lights ({lights.Count} total)");
        }
        else
        {
            Require(lights.Count == 1, "authored scene has exactly one Light");
        }
        Require(FindSceneComponents<WorldGridService>(scene).Count == 1,
            "scene has exactly one WorldGridService");
        Require(FindSceneComponents<WorldGridDebugView>(scene).Count == 1,
            "scene has exactly one WorldGridDebugView");

        Camera camera = FindSingleSceneComponent<Camera>(scene);
        Require(camera.CompareTag("MainCamera"), "camera uses MainCamera tag");
        Require(camera.orthographic, "WorldSandbox camera is orthographic");
        Light light = lights.Single(candidate => candidate.transform.root.name == "Directional Light");
        Require(light.type == LightType.Directional, "WorldSandbox light is directional");

        List<MonoBehaviour> behaviours = FindSceneComponents<MonoBehaviour>(scene);
        if (runtime)
        {
            int duplicateManagers = ForbiddenManagerTypeNames.Sum(typeName =>
                Mathf.Max(0, behaviours.Count(component => component.GetType().Name == typeName) - 1));
            int shopInstances = behaviours.Count(component => component.GetType().Name == "Shop");
            int npcInstances = behaviours.Count(component => component.GetType().Name == "NpcController");
            int adapterInstances = behaviours.Count(component =>
                component.GetType().Name == "WorldGameplayAdapterService");
            Require(duplicateManagers == 0,
                "global runtime bootstrap creates no duplicate gameplay managers");
            Require((shopInstances == 0 && npcInstances == 0) ||
                    (adapterInstances == 1 && shopInstances == 1 && npcInstances <= 1),
                "WorldSandbox runtime contains only the approved WORLD-009 Shop/NPC adapter instances");
        }
        else
        {
            int authoredManagers = behaviours.Count(component =>
                ForbiddenManagerTypeNames.Contains(component.GetType().Name));
            Require(authoredManagers == 0,
                "authored scene contains no shop/NPC/save/gameplay manager copy");
        }

        if (!runtime)
        {
            int missingScripts = roots.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
            int missingReferences = CountBrokenObjectReferences(roots);
            Require(missingScripts == 0, "Missing Script count is 0");
            Require(missingReferences == 0, "Missing Reference count is 0");
        }
    }

    static ulong ValidateGridContract(WorldGridService service, bool generatedRuntime)
    {
        Require(service != null, "WorldGridService exists");
        WorldGridDefinition definition = service.Definition;
        int expectedWidth = generatedRuntime ? 128 : 16;
        int expectedHeight = generatedRuntime ? 128 : 16;
        int expectedCellCount = expectedWidth * expectedHeight;
        Require(Mathf.Approximately(definition.CellSize, 2f), "cell size is 2m");
        Require(definition.Width == expectedWidth && definition.Height == expectedHeight,
            $"grid dimensions are {expectedWidth}x{expectedHeight} ({definition.Width}x{definition.Height})");
        Require(definition.ChunkSize == 16, "chunk size is 16x16 cells");
        Require(Mathf.Approximately(definition.ElevationStep, 1f), "elevation step is 1m");
        Require(definition.MinElevationLevel == 0 && definition.MaxElevationLevel == 6,
            "elevation range is level 0..6");
        Require(definition.WorldOrigin == Vector3.zero, "explicit world origin is Vector3.zero at cell (0,0) center");
        Require(service.TotalCellCount == expectedCellCount && service.Cells.Count == expectedCellCount,
            $"total/read-only cell count is {expectedCellCount} ({service.TotalCellCount}/{service.Cells.Count})");
        Require(service.Cells is ICollection<WorldCellData> collection && collection.IsReadOnly,
            "cell collection rejects mutation through collection API");
        Require(typeof(WorldCellData).IsValueType &&
                typeof(WorldCellData).IsDefined(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), false),
            "WorldCellData is a readonly value type");

        var coordinates = new HashSet<Vector2Int>();
        var elevationHistogram = new int[7];
        foreach (WorldCellData cell in service.EnumerateCells())
        {
            Require(cell.ElevationLevel >= definition.MinElevationLevel &&
                    cell.ElevationLevel <= definition.MaxElevationLevel,
                $"cell {cell.Coordinate} elevation is within definition bounds", false);
            elevationHistogram[cell.ElevationLevel]++;
            if (!generatedRuntime)
            {
                Require(cell.GroundType == WorldGroundType.Default,
                    $"cell {cell.Coordinate} ground is Default", false);
                Require(!cell.HasWater && !cell.HasPath,
                    $"cell {cell.Coordinate} has no water/path", false);
            }
            else
            {
                Require(Enum.IsDefined(typeof(WorldGroundType), cell.GroundType) &&
                        Enum.IsDefined(typeof(WorldPathType), cell.PathType),
                    $"generated cell {cell.Coordinate} surface enums are valid", false);
            }
            Require(cell.Occupancy == WorldCellOccupancy.Empty, $"cell {cell.Coordinate} occupancy is Empty", false);
            if (!coordinates.Add(cell.Coordinate))
                throw new InvalidOperationException($"Duplicate cell coordinate {cell.Coordinate}");
        }
        Require(coordinates.Count == expectedCellCount,
            $"all {expectedCellCount} coordinates are unique");
        if (!generatedRuntime && service.BootstrapProfile == WorldGridBootstrapProfile.Flat)
            Require(elevationHistogram[0] == expectedCellCount,
                $"flat bootstrap keeps all {expectedCellCount} cells at level 0");
        else
            Require(elevationHistogram.All(count => count > 0),
                "terrain bootstrap keeps authoritative elevations across levels 0..6");

        var corners = new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(expectedWidth - 1, 0),
            new Vector2Int(0, expectedHeight - 1),
            new Vector2Int(expectedWidth - 1, expectedHeight - 1)
        };
        foreach (Vector2Int corner in corners)
        {
            Require(service.TryGetCell(corner, out WorldCellData cell) && cell.Coordinate == corner,
                $"corner cell {corner} resolves");
        }

        Require(service.CellToWorld(Vector2Int.zero, out Vector3 originWorld) && originWorld == Vector3.zero,
            "cell (0,0) center equals worldOrigin");
        var farCoordinate = new Vector2Int(expectedWidth - 1, expectedHeight - 1);
        Require(service.TryGetCell(farCoordinate, out WorldCellData farCell) &&
                service.CellToWorld(farCoordinate, out Vector3 farWorld) &&
                farWorld == new Vector3((expectedWidth - 1) * definition.CellSize,
                    farCell.ElevationLevel * definition.ElevationStep,
                    (expectedHeight - 1) * definition.CellSize),
            $"far corner {farCoordinate} resolves through its authoritative elevation");

        for (int index = 0; index < service.TotalCellCount; index++)
        {
            if (!service.TryIndexToCell(index, out Vector2Int coordinate) ||
                !service.TryCellToIndex(coordinate, out int roundTripIndex) ||
                roundTripIndex != index)
            {
                throw new InvalidOperationException($"Index round trip failed at {index}");
            }

            var expectedChunk = new Vector2Int(
                coordinate.x / definition.ChunkSize,
                coordinate.y / definition.ChunkSize);
            if (!service.CellToChunk(coordinate, out Vector2Int chunk) || chunk != expectedChunk)
                throw new InvalidOperationException($"Chunk mapping failed at {coordinate}: {chunk}");
        }
        Require(true, $"all {expectedCellCount} index/cell round trips pass");
        Require(true, generatedRuntime
            ? "all generated cells map to their 8x8 chunk authority"
            : "all authored cells map to the single test chunk (0,0)");

        var random = new System.Random(1001);
        for (int iteration = 0; iteration < 10000; iteration++)
        {
            var coordinate = new Vector2Int(
                random.Next(0, expectedWidth), random.Next(0, expectedHeight));
            if (!service.CellToWorld(coordinate, out Vector3 center))
                throw new InvalidOperationException($"CellToWorld failed at {coordinate}");
            float offsetX = ((float)random.NextDouble() * 0.98f - 0.49f) * definition.CellSize;
            float offsetZ = ((float)random.NextDouble() * 0.98f - 0.49f) * definition.CellSize;
            Vector3 sample = center + new Vector3(offsetX, 3.25f, offsetZ);
            if (!service.WorldToCell(sample, out Vector2Int roundTrip) || roundTrip != coordinate)
                throw new InvalidOperationException(
                    $"World/cell round trip failed at iteration {iteration}: {coordinate} -> {sample} -> {roundTrip}");
        }
        Require(true, "10,000 deterministic world/cell round trips pass");

        float halfCell = definition.CellSize * 0.5f;
        Require(!service.IsValidCell(new Vector2Int(-1, 0)) &&
                !service.IsValidCell(new Vector2Int(0, -1)) &&
                !service.IsValidCell(new Vector2Int(expectedWidth, 0)) &&
                !service.IsValidCell(new Vector2Int(0, expectedHeight)),
            "negative and upper-bound cell coordinates fail safely");
        Require(!service.TryGetCell(new Vector2Int(-1, -1), out _), "out-of-bounds TryGetCell fails safely");
        Require(!service.TryIndexToCell(-1, out _) && !service.TryIndexToCell(expectedCellCount, out _),
            "out-of-bounds indices fail safely");
        Require(!service.CellToWorld(new Vector2Int(expectedWidth, expectedHeight), out _),
            "out-of-bounds CellToWorld fails safely");
        Require(!service.CellToChunk(new Vector2Int(expectedWidth, expectedHeight), out _),
            "out-of-bounds CellToChunk fails safely");

        Vector3 origin = definition.WorldOrigin;
        float maxXBoundary = origin.x + (definition.Width - 0.5f) * definition.CellSize;
        float maxZBoundary = origin.z + (definition.Height - 0.5f) * definition.CellSize;
        Require(!service.WorldToCell(new Vector3(origin.x - halfCell - 0.001f, 0f, origin.z), out _),
            "world position below minimum X fails safely");
        Require(!service.WorldToCell(new Vector3(origin.x, 0f, origin.z - halfCell - 0.001f), out _),
            "world position below minimum Z fails safely");
        Require(!service.WorldToCell(new Vector3(maxXBoundary, 0f, origin.z), out _),
            "world position at exclusive maximum X fails safely");
        Require(!service.WorldToCell(new Vector3(origin.x, 0f, maxZBoundary), out _),
            "world position at exclusive maximum Z fails safely");

        MethodInfo[] publicDeclaredMethods = typeof(WorldGridService).GetMethods(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        string[] mutationPrefixes = { "SetCell", "Raise", "Lower", "SetGround", "SetWater", "SetPath", "SetOccupancy" };
        Require(!publicDeclaredMethods.Any(method =>
                mutationPrefixes.Any(prefix => method.Name.StartsWith(prefix, StringComparison.Ordinal))),
            "WorldGridService exposes no runtime cell mutation API");

        ulong hash = ComputeCellHash(service.Cells);
        Debug.Log($"[WORLD-001] PASS deterministic cell checksum {hash:X16}");
        return hash;
    }

    static ulong ComputeCellHash(IReadOnlyList<WorldCellData> cells)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offsetBasis;
        unchecked
        {
            for (int i = 0; i < cells.Count; i++)
            {
                WorldCellData cell = cells[i];
                hash = (hash ^ (uint)cell.Coordinate.x) * prime;
                hash = (hash ^ (uint)cell.Coordinate.y) * prime;
                hash = (hash ^ (uint)cell.ElevationLevel) * prime;
                hash = (hash ^ (uint)cell.GroundType) * prime;
                hash = (hash ^ (cell.HasWater ? 1UL : 0UL)) * prime;
                hash = (hash ^ (cell.HasPath ? 1UL : 0UL)) * prime;
                hash = (hash ^ (uint)cell.Occupancy) * prime;
            }
        }
        return hash;
    }

    static int CountBrokenObjectReferences(IEnumerable<GameObject> roots)
    {
        int count = 0;
        foreach (GameObject root in roots)
        {
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                var serializedObject = new SerializedObject(component);
                SerializedProperty property = serializedObject.GetIterator();
                bool enterChildren = true;
                while (property.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                        count++;
                }
            }
        }
        return count;
    }

    static List<T> FindSceneComponents<T>(Scene scene) where T : Component
    {
        var result = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects())
            result.AddRange(root.GetComponentsInChildren<T>(true));
        return result;
    }

    static T FindSingleSceneComponent<T>(Scene scene) where T : Component
    {
        List<T> components = FindSceneComponents<T>(scene);
        if (components.Count != 1)
            throw new InvalidOperationException(
                $"Expected exactly one {typeof(T).Name} in {scene.path}, found {components.Count}.");
        return components[0];
    }

    static void Require(bool condition, string message, bool writePassLog = true)
    {
        if (!condition) throw new InvalidOperationException(message);
        if (writePassLog) Debug.Log($"[WORLD-001] PASS {message}");
    }
}
#endif

[UnityEngine.DisallowMultipleComponent]
[UnityEngine.RequireComponent(typeof(WorldGridService))]
public sealed class WorldGridDebugView : WorldGridDebugViewBase
{
}
