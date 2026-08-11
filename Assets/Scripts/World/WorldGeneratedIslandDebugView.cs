using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Stopwatch = System.Diagnostics.Stopwatch;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

[DisallowMultipleComponent]
public sealed class WorldGeneratedIslandDebugView : MonoBehaviour
{
    const string RuntimeRootName = "WorldGeneratedIsland_Runtime";

    readonly List<Mesh> _meshes = new List<Mesh>();
    readonly List<Material> _materials = new List<Material>();
    GameObject _runtimeRoot;
    Camera _camera;
    Vector3 _cameraPositionBeforeDisplay;
    Quaternion _cameraRotationBeforeDisplay;
    float _cameraSizeBeforeDisplay;
    bool _cameraStateCaptured;
    long _seed = 20260810L;

    public WorldGenerationResult CurrentResult { get; private set; }
    public int GeneratedChunkCount { get; private set; }
    public int TotalRenderedTopFaces { get; private set; }
    public int TotalRenderedWaterFaces { get; private set; }
    public int TotalRenderedShorelineFaces { get; private set; }
    public long LastGenerationMilliseconds { get; private set; }
    public GameObject RuntimeRoot => _runtimeRoot;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftBracket)) _seed--;
        if (Input.GetKeyDown(KeyCode.RightBracket)) _seed++;
        if (Input.GetKeyDown(KeyCode.J)) GenerateAndDisplay(_seed);
        if (Input.GetKeyDown(KeyCode.K)) ClearGeneratedIsland();
    }

    public WorldGenerationResult GenerateAndDisplay(long seed)
    {
        var stopwatch = Stopwatch.StartNew();
        ClearGeneratedIsland();
        _seed = seed;
        CurrentResult = WorldIslandGenerator.Generate(seed);
        BuildChunkViews(CurrentResult.Definition, CurrentResult.TerrainCells);
        FrameGeneratedIsland(CurrentResult.Definition);
        stopwatch.Stop();
        LastGenerationMilliseconds = stopwatch.ElapsedMilliseconds;
        return CurrentResult;
    }

    public bool DisplayGridSnapshot(long seed, bool frameCamera = false)
    {
        WorldGridService grid = GetComponent<WorldGridService>();
        if (grid == null || grid.Definition == null || grid.Cells == null ||
            grid.Cells.Count != grid.Definition.TotalCellCount)
        {
            return false;
        }

        var stopwatch = Stopwatch.StartNew();
        ClearGeneratedIsland();
        _seed = seed;
        BuildChunkViews(grid.Definition, grid.Cells);
        if (frameCamera) FrameGeneratedIsland(grid.Definition);
        stopwatch.Stop();
        LastGenerationMilliseconds = stopwatch.ElapsedMilliseconds;
        return true;
    }

    public void ClearGeneratedIsland()
    {
        if (_runtimeRoot != null)
        {
            _runtimeRoot.SetActive(false);
            ReleaseObject(_runtimeRoot);
        }
        foreach (Mesh mesh in _meshes) ReleaseObject(mesh);
        foreach (Material material in _materials) ReleaseObject(material);
        _meshes.Clear();
        _materials.Clear();
        _runtimeRoot = null;
        CurrentResult = null;
        GeneratedChunkCount = 0;
        TotalRenderedTopFaces = 0;
        TotalRenderedWaterFaces = 0;
        TotalRenderedShorelineFaces = 0;
        LastGenerationMilliseconds = 0;
        RestoreCamera();
    }

    void BuildChunkViews(
        WorldGridDefinition definition,
        IReadOnlyList<WorldCellData> cells)
    {
        Material[] materials = CreateMaterials();
        _runtimeRoot = new GameObject(RuntimeRootName)
        {
            hideFlags = HideFlags.DontSave
        };
        _runtimeRoot.transform.SetParent(transform, false);

        for (int chunkZ = 0; chunkZ < definition.ChunkCountZ; chunkZ++)
        {
            for (int chunkX = 0; chunkX < definition.ChunkCountX; chunkX++)
            {
                var chunkCoordinate = new Vector2Int(chunkX, chunkZ);
                WorldChunkMeshData data = WorldChunkMeshBuilder.Build(
                    definition,
                    cells,
                    chunkCoordinate);
                var chunkObject = new GameObject($"GeneratedChunk_{chunkX}_{chunkZ}");
                chunkObject.transform.SetParent(_runtimeRoot.transform, false);
                var filter = chunkObject.AddComponent<MeshFilter>();
                var renderer = chunkObject.AddComponent<MeshRenderer>();
                Mesh mesh = CreateMesh($"GeneratedChunk_{chunkX}_{chunkZ}_Visual", data);
                filter.sharedMesh = mesh;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                _meshes.Add(mesh);
                GeneratedChunkCount++;
                TotalRenderedTopFaces += data.TopFaceCount;
                TotalRenderedWaterFaces += data.WaterSurfaceFaceCount;
                TotalRenderedShorelineFaces += data.ShorelineFaceCount;
            }
        }
    }

    Material[] CreateMaterials()
    {
        var colors = new[]
        {
            new Color(0.39f, 0.67f, 0.35f, 1f),
            new Color(0.49f, 0.33f, 0.20f, 1f),
            new Color(0.82f, 0.72f, 0.46f, 1f),
            new Color(0.43f, 0.46f, 0.45f, 1f),
            new Color(0.58f, 0.41f, 0.24f, 1f),
            new Color(0.54f, 0.57f, 0.55f, 1f),
            new Color(0.46f, 0.31f, 0.20f, 1f),
            new Color(0.19f, 0.52f, 0.73f, 1f)
        };
        var names = new[]
        {
            "GeneratedGrass", "GeneratedSoil", "GeneratedSand", "GeneratedRock",
            "GeneratedDirtPath", "GeneratedStonePath", "GeneratedCliff", "GeneratedWater"
        };
        var result = new Material[WorldSurfaceMaterialSlots.Count];
        for (int i = 0; i < result.Length; i++)
        {
            Material material = CreateMaterial(names[i], colors[i]);
            result[i] = material;
            _materials.Add(material);
        }
        return result;
    }

    static Mesh CreateMesh(string meshName, WorldChunkMeshData data)
    {
        var mesh = new Mesh
        {
            name = meshName,
            indexFormat = IndexFormat.UInt32,
            hideFlags = HideFlags.DontSave
        };
        mesh.vertices = data.Vertices;
        mesh.normals = data.Normals;
        mesh.uv = data.Uvs;
        mesh.subMeshCount = WorldSurfaceMaterialSlots.Count;
        for (int i = 0; i < WorldSurfaceMaterialSlots.Count; i++)
            mesh.SetTriangles(data.VisualSubmeshIndices[i], i, false);
        mesh.bounds = data.Bounds;
        mesh.UploadMeshData(false);
        return mesh;
    }

    static Material CreateMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                        Shader.Find("Standard") ??
                        Shader.Find("Sprites/Default");
        if (shader == null)
            throw new InvalidOperationException("No generated-island debug shader is available.");
        var material = new Material(shader)
        {
            name = materialName,
            color = color,
            hideFlags = HideFlags.DontSave
        };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.07f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        return material;
    }

    void FrameGeneratedIsland(WorldGridDefinition definition)
    {
        _camera = Camera.main;
        if (_camera == null) return;
        if (!_cameraStateCaptured)
        {
            _cameraPositionBeforeDisplay = _camera.transform.position;
            _cameraRotationBeforeDisplay = _camera.transform.rotation;
            _cameraSizeBeforeDisplay = _camera.orthographicSize;
            _cameraStateCaptured = true;
        }
        Vector3 center = definition.WorldOrigin + new Vector3(
            (definition.Width - 1) * definition.CellSize * 0.5f,
            2f,
            (definition.Height - 1) * definition.CellSize * 0.5f);
        _camera.orthographic = true;
        _camera.orthographicSize = Mathf.Max(definition.Width, definition.Height) *
                                   definition.CellSize * 0.58f;
        _camera.transform.position = center + new Vector3(95f, 180f, -95f);
        _camera.transform.LookAt(center);
    }

    void RestoreCamera()
    {
        if (!_cameraStateCaptured || _camera == null) return;
        _camera.transform.SetPositionAndRotation(
            _cameraPositionBeforeDisplay,
            _cameraRotationBeforeDisplay);
        _camera.orthographicSize = _cameraSizeBeforeDisplay;
        _cameraStateCaptured = false;
    }

    void OnGUI()
    {
        string summary = CurrentResult == null && _runtimeRoot != null
            ? $"Seed {_seed} live grid snapshot  chunks {GeneratedChunkCount}  " +
              $"top {TotalRenderedTopFaces}  {LastGenerationMilliseconds}ms"
            : CurrentResult == null
            ? $"Seed {_seed} ready — J generate, [ / ] change seed"
            : $"Seed {CurrentResult.Seed}  checksum {CurrentResult.Checksum:X16}  " +
              $"land {CurrentResult.LandRatio:P0}  chunks {GeneratedChunkCount}  " +
              $"resources {CurrentResult.ResourceSpawns.Count}  {LastGenerationMilliseconds}ms";
        GUILayout.BeginArea(new Rect(18f, 280f, 760f, 68f), GUI.skin.box);
        GUILayout.Label("WORLD-006  DETERMINISTIC ISLAND GENERATOR");
        GUILayout.Label(summary + "  |  K clear");
        GUILayout.EndArea();
    }

    void OnDestroy()
    {
        ClearGeneratedIsland();
    }

    static void ReleaseObject(UnityEngine.Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}

#if UNITY_EDITOR
public static class PA_WorldIslandGenerationTools
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.WORLD006.Active";
    const string FailedKey = "PA.WORLD006.Failed";
    const string ConsoleErrorKey = "PA.WORLD006.ConsoleErrors";
    const string WaitFramesKey = "PA.WORLD006.WaitFrames";
    const string StageKey = "PA.WORLD006.Stage";
    const int CorpusSeedCount = 128;
    const long SampleSeed = 20260810L;

    [InitializeOnLoadMethod]
    static void ResumeValidationAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        SubscribeCallbacks();
        if (EditorApplication.isPlaying) EditorApplication.update += ValidateRuntime;
    }

    [MenuItem("Project PA/World/WORLD-006/Build Island Generator Debug")]
    public static void BuildWorld006Sandbox()
    {
        BuildWorld006SandboxInternal();
    }

    public static void BuildWorld006SandboxBatch()
    {
        BuildWorld006SandboxInternal();
    }

    static void BuildWorld006SandboxInternal()
    {
        try
        {
            Scene current = SceneManager.GetActiveScene();
            if (current.IsValid() && current.isDirty)
                throw new InvalidOperationException(
                    $"Active scene '{current.path}' has unsaved changes.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            WorldGridService grid = FindSingle<WorldGridService>(scene);
            WorldGeneratedIslandDebugView[] existing =
                grid.GetComponents<WorldGeneratedIslandDebugView>();
            if (existing.Length == 0)
                grid.gameObject.AddComponent<WorldGeneratedIslandDebugView>();
            Require(grid.GetComponents<WorldGeneratedIslandDebugView>().Length == 1,
                "WorldGrid owns one generated-island debug view");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Failed to save WorldSandbox.");
            Debug.Log("[WORLD-006] BUILD_PASS WorldSandbox island generator debug ready");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WORLD-006] BUILD_FAIL {ex.Message}\n{ex}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    [MenuItem("Project PA/World/WORLD-006/Validate Island Generator")]
    public static void RunWorld006Validation()
    {
        RunWorld006ValidationInternal();
    }

    public static void RunWorld006ValidationBatch()
    {
        RunWorld006ValidationInternal();
    }

    static void RunWorld006ValidationInternal()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetInt(ConsoleErrorKey, 0);
            SessionState.SetInt(WaitFramesKey, 0);
            SessionState.SetInt(StageKey, 0);
            SubscribeCallbacks();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "validator targets only WorldSandbox");
            Require(!scene.isDirty, "WorldSandbox starts clean");
            Require(FindComponents<WorldGeneratedIslandDebugView>(scene).Count == 1,
                "scene has one generated-island debug view");
            ValidateSeedCorpus();
            Require(!scene.isDirty, "pure generation corpus does not dirty the scene");
            Debug.Log("[WORLD-006] EDIT_MODE_PASS");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void ValidateSeedCorpus()
    {
        WorldIslandGenerationSettings settings =
            WorldIslandGenerationSettings.Provisional128;
        Require(settings.GenerationVersion == 1 &&
                settings.WidthCells == 128 && settings.HeightCells == 128 &&
                settings.ChunkSize == 16,
            "generationVersion 1 uses the provisional 128x128, 16-cell-chunk definition");

        var uniqueChecksums = new HashSet<ulong>();
        var stopwatch = Stopwatch.StartNew();
        float minimumLandRatio = 1f;
        float maximumLandRatio = 0f;
        for (int i = 0; i < CorpusSeedCount; i++)
        {
            long seed = i - CorpusSeedCount / 2L;
            WorldGenerationResult first = WorldIslandGenerator.Generate(seed, settings);
            WorldGenerationResult second = WorldIslandGenerator.Generate(seed, settings);
            ValidateGeneratedWorld(first, false);
            Require(first.Checksum == second.Checksum,
                $"seed {seed} regenerates the exact checksum", false);
            uniqueChecksums.Add(first.Checksum);
            minimumLandRatio = Mathf.Min(minimumLandRatio, first.LandRatio);
            maximumLandRatio = Mathf.Max(maximumLandRatio, first.LandRatio);
        }
        stopwatch.Stop();
        Require(uniqueChecksums.Count >= 124,
            $"128 fixed seeds produce diverse deterministic checksums ({uniqueChecksums.Count})");
        Require(stopwatch.ElapsedMilliseconds < 15000,
            $"256 full 128x128 generations stay within the 15s editor budget ({stopwatch.ElapsedMilliseconds}ms)");
        Debug.Log($"[WORLD-006] CORPUS_PASS seeds={CorpusSeedCount} " +
                  $"land={minimumLandRatio:P1}..{maximumLandRatio:P1} " +
                  $"elapsedMs={stopwatch.ElapsedMilliseconds}");
    }

    static void ValidateGeneratedWorld(WorldGenerationResult result, bool writePassLogs)
    {
        Require(result != null && result.Definition.Width == 128 &&
                result.Definition.Height == 128 && result.Cells.Count == 16384,
            "generated world has 128x128 row-major cells", writePassLogs);
        Require(result.LandRatio >= 0.32f && result.LandRatio <= 0.68f,
            $"land ratio is bounded ({result.LandRatio:P1})", writePassLogs);
        Require(HasOceanBorder(result),
            "all outer border cells remain ocean", writePassLogs);
        Require(result.Anchors.Count == Enum.GetValues(typeof(WorldGenerationAnchorKind)).Length,
            "all required role/activity anchors exist", writePassLogs);
        ValidateShopCandidate(result, writePassLogs);
        Require(WorldGenerationConnectivity.CanReachAllAnchors(result, out int reachable) &&
                reachable >= 64,
            $"start reaches shop, beach and every activity anchor ({reachable} dry cells)",
            writePassLogs);
        Require(result.Cells.Count(cell => cell.Biome == WorldBiomeType.Coast) >= 20 &&
                result.Cells.Count(cell => cell.Biome == WorldBiomeType.Meadow) >= 20 &&
                result.Cells.Count(cell => cell.Biome == WorldBiomeType.Forest) >= 20 &&
                result.Cells.Count(cell => cell.Biome == WorldBiomeType.Highland) >= 20,
            "coast, meadow, forest and highland candidates all exist", writePassLogs);
        Require(result.Cells.Count(cell => cell.WaterType == WorldWaterType.Ocean) > 100 &&
                result.Cells.Count(cell => cell.WaterType == WorldWaterType.River) >= 8 &&
                result.Cells.Count(cell => cell.WaterType == WorldWaterType.Pond) >= 9,
            "ocean, minimal river and pond cells all exist", writePassLogs);
        Require(result.ResourceSpawns.Select(spawn => spawn.Kind).Distinct().Count() ==
                Enum.GetValues(typeof(WorldResourceKind)).Length &&
                result.ResourceSpawns.Select(spawn => spawn.SpawnKey).Distinct().Count() ==
                result.ResourceSpawns.Count,
            "all resource kinds have unique deterministic spawn keys", writePassLogs);
        Require(result.Cells.Select(cell => cell.Terrain.Coordinate).Distinct().Count() ==
                result.Cells.Count,
            "generated cell coordinates are unique", writePassLogs);
    }

    static void ValidateShopCandidate(WorldGenerationResult result, bool writePassLogs)
    {
        Require(result.TryGetAnchor(WorldGenerationAnchorKind.Shop,
                    out WorldGenerationAnchor shop) &&
                shop.FootprintSize == new Vector2Int(4, 3),
            "shop candidate reserves an independent 4x3 footprint contract", writePassLogs);
        int? level = null;
        for (int z = 0; z < shop.FootprintSize.y; z++)
        {
            for (int x = 0; x < shop.FootprintSize.x; x++)
            {
                Vector2Int coordinate = shop.Coordinate + new Vector2Int(x, z);
                Require(result.TryGetCell(coordinate, out WorldGeneratedCell cell) &&
                        cell.IsDryLand && cell.Terrain.IsWalkable,
                    $"shop footprint cell {coordinate} is dry and walkable", false);
                level ??= cell.Terrain.ElevationLevel;
                Require(cell.Terrain.ElevationLevel == level.Value,
                    "shop footprint is flat", false);
            }
        }
        Require(result.TryGetCell(shop.EntranceCoordinate,
                    out WorldGeneratedCell entrance) &&
                entrance.IsDryLand && entrance.Terrain.IsWalkable &&
                Mathf.Abs(entrance.Terrain.ElevationLevel - level.GetValueOrDefault()) <= 1,
            "shop entrance is dry, walkable and height-compatible", writePassLogs);
    }

    static bool HasOceanBorder(WorldGenerationResult result)
    {
        int width = result.Definition.Width;
        int height = result.Definition.Height;
        for (int x = 0; x < width; x++)
        {
            if (!result.TryGetCell(new Vector2Int(x, 0), out WorldGeneratedCell south) ||
                !south.IsOcean ||
                !result.TryGetCell(new Vector2Int(x, height - 1), out WorldGeneratedCell north) ||
                !north.IsOcean)
            {
                return false;
            }
        }
        for (int z = 0; z < height; z++)
        {
            if (!result.TryGetCell(new Vector2Int(0, z), out WorldGeneratedCell west) ||
                !west.IsOcean ||
                !result.TryGetCell(new Vector2Int(width - 1, z), out WorldGeneratedCell east) ||
                !east.IsOcean)
            {
                return false;
            }
        }
        return true;
    }

    static void SubscribeCallbacks()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        Application.logMessageReceived += OnLogMessage;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetInt(WaitFramesKey, 0);
            SessionState.SetInt(StageKey, 0);
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            FinishValidation();
        }
    }

    static void ValidateRuntime()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
        int frames = SessionState.GetInt(WaitFramesKey, 0) + 1;
        SessionState.SetInt(WaitFramesKey, frames);
        int stage = SessionState.GetInt(StageKey, 0);
        if ((stage == 0 && frames < 4) || (stage > 0 && frames < 3)) return;

        try
        {
            Scene scene = SceneManager.GetActiveScene();
            WorldGeneratedIslandDebugView view =
                FindSingle<WorldGeneratedIslandDebugView>(scene);
            if (stage == 0)
            {
                Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
                    $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
                WorldGenerationResult first = view.GenerateAndDisplay(SampleSeed);
                ValidateGeneratedWorld(first, true);
                ValidateRenderedIsland(view, first);
                SessionState.SetInt(StageKey, 1);
                SessionState.SetInt(WaitFramesKey, 0);
                return;
            }
            if (stage == 1)
            {
                ulong previousChecksum = view.CurrentResult.Checksum;
                WorldGenerationResult second = view.GenerateAndDisplay(SampleSeed + 1);
                Require(second.Checksum != previousChecksum,
                    "changing the seed changes the generated island checksum");
                ValidateRenderedIsland(view, second);
                Require(FindComponents<Transform>(scene).Count(transform =>
                            transform.gameObject.activeInHierarchy &&
                            transform.name == RuntimeRootNameForValidation()) == 1,
                    "seed replacement keeps one active generated runtime root");
                view.ClearGeneratedIsland();
                SessionState.SetInt(StageKey, 2);
                SessionState.SetInt(WaitFramesKey, 0);
                return;
            }

            EditorApplication.update -= ValidateRuntime;
            Require(view.CurrentResult == null && view.GeneratedChunkCount == 0 &&
                    view.RuntimeRoot == null,
                "runtime debug cleanup releases generated state");
            Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                "blocking runtime Console Error/Exception/Assert count is 0");
            Debug.Log("[WORLD-006] PLAY_MODE_PASS");
            EditorApplication.ExitPlaymode();
        }
        catch (Exception ex)
        {
            EditorApplication.update -= ValidateRuntime;
            Fail(ex);
        }
    }

    static void ValidateRenderedIsland(
        WorldGeneratedIslandDebugView view,
        WorldGenerationResult result)
    {
        Require(view.GeneratedChunkCount == 64,
            "128x128 result renders as exactly 8x8 chunk objects");
        Require(view.TotalRenderedTopFaces == result.Definition.TotalCellCount,
            "all generated cells route through chunk top geometry");
        Require(view.TotalRenderedWaterFaces > 100 &&
                view.TotalRenderedShorelineFaces > 0,
            "ocean, river and pond produce water surfaces and shoreline geometry");
        Require(view.RuntimeRoot != null && view.RuntimeRoot.activeInHierarchy &&
                view.RuntimeRoot.GetComponentsInChildren<MeshFilter>(true).Length == 64 &&
                view.RuntimeRoot.GetComponentsInChildren<MeshRenderer>(true).All(renderer =>
                    renderer.sharedMaterials.Length == WorldSurfaceMaterialSlots.Count),
            "64 chunks have meshes and all eight project-owned surface materials");
        Require(view.RuntimeRoot.GetComponentsInChildren<Transform>(true).Length == 65,
            "generated view uses one root plus 64 chunks and no per-cell GameObjects");
        Require(view.LastGenerationMilliseconds < 5000,
            $"data plus 64-chunk debug mesh stays within 5s ({view.LastGenerationMilliseconds}ms)");
    }

    static string RuntimeRootNameForValidation()
    {
        return "WorldGeneratedIsland_Runtime";
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        SessionState.SetInt(ConsoleErrorKey, SessionState.GetInt(ConsoleErrorKey, 0) + 1);
    }

    static void Fail(Exception ex)
    {
        SessionState.SetBool(FailedKey, true);
        Debug.LogError($"[WORLD-006] FAIL {ex.Message}\n{ex}");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else FinishValidation();
    }

    static void FinishValidation()
    {
        bool failed = SessionState.GetBool(FailedKey, false) ||
                      SessionState.GetInt(ConsoleErrorKey, 0) != 0;
        int consoleErrors = SessionState.GetInt(ConsoleErrorKey, 0);
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(FailedKey);
        SessionState.EraseInt(ConsoleErrorKey);
        SessionState.EraseInt(WaitFramesKey);
        SessionState.EraseInt(StageKey);
        EditorApplication.update -= ValidateRuntime;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        Debug.Log(failed
            ? $"[WORLD-006] FINISHED_WITH_ERRORS consoleErrors={consoleErrors}"
            : "[WORLD-006] FINISHED_PASS seeds=128 deterministic=true cells=128x128 chunks=64 anchors=7 connectivity=true resources=true");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static List<T> FindComponents<T>(Scene scene) where T : Component
    {
        var result = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects())
            result.AddRange(root.GetComponentsInChildren<T>(true));
        return result;
    }

    static T FindSingle<T>(Scene scene) where T : Component
    {
        List<T> components = FindComponents<T>(scene);
        if (components.Count != 1)
            throw new InvalidOperationException(
                $"Expected one {typeof(T).Name}, found {components.Count}.");
        return components[0];
    }

    static void Require(bool condition, string message, bool writePassLog = true)
    {
        if (!condition) throw new InvalidOperationException(message);
        if (writePassLog) Debug.Log($"[WORLD-006] PASS {message}");
    }
}
#endif
