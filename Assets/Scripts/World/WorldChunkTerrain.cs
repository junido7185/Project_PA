using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

[Flags]
public enum WorldChunkDirtyFlags
{
    None = 0,
    Visual = 1 << 0,
    Collider = 1 << 1,
    All = Visual | Collider
}

public sealed class WorldChunkMeshData
{
    public Vector3[] Vertices { get; }
    public Vector3[] Normals { get; }
    public Vector2[] Uvs { get; }
    public int[] TopIndices { get; }
    public int[] CliffIndices { get; }
    public Bounds Bounds { get; }

    public int TopFaceCount => TopIndices.Length / 6;
    public int CliffFaceCount => CliffIndices.Length / 6;

    public WorldChunkMeshData(
        Vector3[] vertices,
        Vector3[] normals,
        Vector2[] uvs,
        int[] topIndices,
        int[] cliffIndices,
        Bounds bounds)
    {
        Vertices = vertices;
        Normals = normals;
        Uvs = uvs;
        TopIndices = topIndices;
        CliffIndices = cliffIndices;
        Bounds = bounds;
    }
}

public static class WorldChunkMeshBuilder
{
    public static WorldChunkMeshData Build(
        WorldGridDefinition definition,
        IReadOnlyList<WorldCellData> cells,
        Vector2Int chunkCoordinate)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        if (cells == null) throw new ArgumentNullException(nameof(cells));
        if (cells.Count != definition.TotalCellCount)
            throw new ArgumentException("Cell count does not match the world definition.", nameof(cells));
        if (chunkCoordinate.x < 0 || chunkCoordinate.y < 0 ||
            chunkCoordinate.x >= definition.ChunkCountX ||
            chunkCoordinate.y >= definition.ChunkCountZ)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkCoordinate));
        }

        int startX = chunkCoordinate.x * definition.ChunkSize;
        int startZ = chunkCoordinate.y * definition.ChunkSize;
        int endX = Mathf.Min(startX + definition.ChunkSize, definition.Width);
        int endZ = Mathf.Min(startZ + definition.ChunkSize, definition.Height);
        float halfCell = definition.CellSize * 0.5f;
        float outsideBaseY = definition.WorldOrigin.y - definition.ElevationStep;

        var vertices = new List<Vector3>((endX - startX) * (endZ - startZ) * 8);
        var normals = new List<Vector3>(vertices.Capacity);
        var uvs = new List<Vector2>(vertices.Capacity);
        var topIndices = new List<int>((endX - startX) * (endZ - startZ) * 6);
        var cliffIndices = new List<int>(topIndices.Capacity);

        for (int z = startZ; z < endZ; z++)
        {
            for (int x = startX; x < endX; x++)
            {
                WorldCellData cell = cells[z * definition.Width + x];
                float centerX = definition.WorldOrigin.x + x * definition.CellSize;
                float centerZ = definition.WorldOrigin.z + z * definition.CellSize;
                float topY = definition.WorldOrigin.y + cell.ElevationLevel * definition.ElevationStep;

                Vector3 southWest = new Vector3(centerX - halfCell, topY, centerZ - halfCell);
                Vector3 northWest = new Vector3(centerX - halfCell, topY, centerZ + halfCell);
                Vector3 northEast = new Vector3(centerX + halfCell, topY, centerZ + halfCell);
                Vector3 southEast = new Vector3(centerX + halfCell, topY, centerZ - halfCell);
                AddQuad(vertices, normals, uvs, topIndices,
                    southWest, northWest, northEast, southEast, Vector3.up, 1f);

                AddCliffIfNeeded(definition, cells, x, z, -1, 0, outsideBaseY, topY,
                    vertices, normals, uvs, cliffIndices,
                    bottomY => new[]
                    {
                        new Vector3(centerX - halfCell, bottomY, centerZ - halfCell),
                        new Vector3(centerX - halfCell, bottomY, centerZ + halfCell),
                        northWest,
                        southWest
                    }, Vector3.left);
                AddCliffIfNeeded(definition, cells, x, z, 1, 0, outsideBaseY, topY,
                    vertices, normals, uvs, cliffIndices,
                    bottomY => new[]
                    {
                        new Vector3(centerX + halfCell, bottomY, centerZ - halfCell),
                        southEast,
                        northEast,
                        new Vector3(centerX + halfCell, bottomY, centerZ + halfCell)
                    }, Vector3.right);
                AddCliffIfNeeded(definition, cells, x, z, 0, -1, outsideBaseY, topY,
                    vertices, normals, uvs, cliffIndices,
                    bottomY => new[]
                    {
                        new Vector3(centerX - halfCell, bottomY, centerZ - halfCell),
                        southWest,
                        southEast,
                        new Vector3(centerX + halfCell, bottomY, centerZ - halfCell)
                    }, Vector3.back);
                AddCliffIfNeeded(definition, cells, x, z, 0, 1, outsideBaseY, topY,
                    vertices, normals, uvs, cliffIndices,
                    bottomY => new[]
                    {
                        new Vector3(centerX - halfCell, bottomY, centerZ + halfCell),
                        new Vector3(centerX + halfCell, bottomY, centerZ + halfCell),
                        northEast,
                        northWest
                    }, Vector3.forward);
            }
        }

        Bounds bounds = CalculateBounds(vertices);
        return new WorldChunkMeshData(
            vertices.ToArray(),
            normals.ToArray(),
            uvs.ToArray(),
            topIndices.ToArray(),
            cliffIndices.ToArray(),
            bounds);
    }

    static void AddCliffIfNeeded(
        WorldGridDefinition definition,
        IReadOnlyList<WorldCellData> cells,
        int x,
        int z,
        int offsetX,
        int offsetZ,
        float outsideBaseY,
        float topY,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> indices,
        Func<float, Vector3[]> corners,
        Vector3 normal)
    {
        int neighborX = x + offsetX;
        int neighborZ = z + offsetZ;
        float bottomY = outsideBaseY;
        if (neighborX >= 0 && neighborX < definition.Width &&
            neighborZ >= 0 && neighborZ < definition.Height)
        {
            WorldCellData neighbor = cells[neighborZ * definition.Width + neighborX];
            bottomY = definition.WorldOrigin.y + neighbor.ElevationLevel * definition.ElevationStep;
        }

        if (bottomY >= topY - 0.0001f) return;
        Vector3[] face = corners(bottomY);
        float verticalUvScale = Mathf.Max(1f, (topY - bottomY) / definition.CellSize);
        AddQuad(vertices, normals, uvs, indices,
            face[0], face[1], face[2], face[3], normal, verticalUvScale);
    }

    static void AddQuad(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> indices,
        Vector3 first,
        Vector3 second,
        Vector3 third,
        Vector3 fourth,
        Vector3 normal,
        float verticalUvScale)
    {
        int start = vertices.Count;
        vertices.Add(first);
        vertices.Add(second);
        vertices.Add(third);
        vertices.Add(fourth);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(0f, verticalUvScale));
        uvs.Add(new Vector2(1f, verticalUvScale));
        uvs.Add(new Vector2(1f, 0f));
        indices.Add(start);
        indices.Add(start + 1);
        indices.Add(start + 2);
        indices.Add(start);
        indices.Add(start + 2);
        indices.Add(start + 3);
    }

    static Bounds CalculateBounds(IReadOnlyList<Vector3> vertices)
    {
        if (vertices.Count == 0) return new Bounds(Vector3.zero, Vector3.zero);
        var bounds = new Bounds(vertices[0], Vector3.zero);
        for (int i = 1; i < vertices.Count; i++) bounds.Encapsulate(vertices[i]);
        return bounds;
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(WorldGridService), typeof(MeshFilter), typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public sealed class WorldChunkTerrain : MonoBehaviour
{
    [SerializeField] Vector2Int chunkCoordinate = Vector2Int.zero;
    [SerializeField] Color topColor = new Color(0.38f, 0.64f, 0.33f, 1f);
    [SerializeField] Color cliffColor = new Color(0.46f, 0.30f, 0.19f, 1f);

    WorldGridService _grid;
    MeshFilter _filter;
    MeshRenderer _renderer;
    MeshCollider _collider;
    Mesh _visualMesh;
    Mesh _colliderMesh;
    Material[] _materials;

    public int VisualRevision { get; private set; }
    public int ColliderRevision { get; private set; }
    public bool IsReady => _visualMesh != null && _colliderMesh != null &&
                           _filter != null && _filter.sharedMesh == _visualMesh &&
                           _collider != null && _collider.sharedMesh == _colliderMesh;
    public Vector2Int ChunkCoordinate => chunkCoordinate;
    public Mesh VisualMesh => _visualMesh;
    public Mesh ColliderMesh => _colliderMesh;
    public MeshCollider TerrainCollider => _collider;

    void Awake()
    {
        ResolveComponents();
        EnsureBuilt();
    }

    void OnEnable()
    {
        ResolveComponents();
        EnsureBuilt();
    }

    void OnDestroy()
    {
        ReleaseRuntimeObjects();
    }

    public void Rebuild(WorldChunkDirtyFlags dirtyFlags)
    {
        if (dirtyFlags == WorldChunkDirtyFlags.None) return;
        ResolveComponents();
        WorldChunkMeshData data = WorldChunkMeshBuilder.Build(
            _grid.Definition, _grid.Cells, chunkCoordinate);

        if ((dirtyFlags & WorldChunkDirtyFlags.Visual) != 0)
        {
            if (_visualMesh == null) _visualMesh = CreateMesh("WorldChunk_0_0_Visual");
            ApplyMeshData(_visualMesh, data);
            _filter.sharedMesh = _visualMesh;
            EnsureMaterials();
            _renderer.sharedMaterials = _materials;
            VisualRevision++;
        }

        if ((dirtyFlags & WorldChunkDirtyFlags.Collider) != 0)
        {
            if (_colliderMesh == null) _colliderMesh = CreateMesh("WorldChunk_0_0_Collider");
            ApplyMeshData(_colliderMesh, data);
            _collider.sharedMesh = null;
            _collider.sharedMesh = _colliderMesh;
            ColliderRevision++;
        }
    }

    void EnsureBuilt()
    {
        if (!Application.isPlaying || IsReady) return;
        Rebuild(WorldChunkDirtyFlags.All);
    }

    void ResolveComponents()
    {
        if (_grid == null) _grid = GetComponent<WorldGridService>();
        if (_filter == null) _filter = GetComponent<MeshFilter>();
        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
        if (_collider == null) _collider = GetComponent<MeshCollider>();
        if (_grid == null || _filter == null || _renderer == null || _collider == null)
            throw new InvalidOperationException("WorldChunkTerrain required components are missing.");
    }

    static Mesh CreateMesh(string meshName)
    {
        return new Mesh
        {
            name = meshName,
            hideFlags = HideFlags.DontSave,
            indexFormat = IndexFormat.UInt16
        };
    }

    static void ApplyMeshData(Mesh mesh, WorldChunkMeshData data)
    {
        mesh.Clear();
        mesh.vertices = data.Vertices;
        mesh.normals = data.Normals;
        mesh.uv = data.Uvs;
        mesh.subMeshCount = 2;
        mesh.SetTriangles(data.TopIndices, 0, false);
        mesh.SetTriangles(data.CliffIndices, 1, false);
        mesh.bounds = data.Bounds;
        mesh.UploadMeshData(false);
    }

    void EnsureMaterials()
    {
        if (_materials != null && _materials.Length == 2 &&
            _materials[0] != null && _materials[1] != null) return;
        _materials = new[]
        {
            CreateTerrainMaterial("WorldTerrainTop_Runtime", topColor),
            CreateTerrainMaterial("WorldTerrainCliff_Runtime", cliffColor)
        };
    }

    static Material CreateTerrainMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                        Shader.Find("Universal Render Pipeline/Unlit") ??
                        Shader.Find("Standard");
        if (shader == null) throw new InvalidOperationException("No terrain shader is available.");
        var material = new Material(shader)
        {
            name = materialName,
            color = color,
            hideFlags = HideFlags.DontSave
        };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.08f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        return material;
    }

    void ReleaseRuntimeObjects()
    {
        DestroyRuntimeObject(_visualMesh);
        DestroyRuntimeObject(_colliderMesh);
        if (_materials != null)
        {
            foreach (Material material in _materials) DestroyRuntimeObject(material);
        }
        _visualMesh = null;
        _colliderMesh = null;
        _materials = null;
    }

    static void DestroyRuntimeObject(UnityEngine.Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}

#if UNITY_EDITOR
public static class PA_WorldChunkTerrainTools
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.WORLD002.Active";
    const string FailedKey = "PA.WORLD002.Failed";
    const string ConsoleErrorKey = "PA.WORLD002.ConsoleErrors";
    const string WaitFramesKey = "PA.WORLD002.WaitFrames";

    [InitializeOnLoadMethod]
    static void ResumeValidationAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        SubscribeValidationCallbacks();
        if (EditorApplication.isPlaying) EditorApplication.update += ValidateRuntimeAfterFrames;
    }

    [MenuItem("Project PA/World/WORLD-002/Build Terrain Prototype")]
    public static void BuildTerrainPrototype()
    {
        BuildTerrainPrototypeInternal();
    }

    public static void BuildTerrainPrototypeBatch()
    {
        BuildTerrainPrototypeInternal();
    }

    static void BuildTerrainPrototypeInternal()
    {
        try
        {
            Scene current = SceneManager.GetActiveScene();
            if (current.IsValid() && current.isDirty)
                throw new InvalidOperationException($"Active scene '{current.path}' has unsaved changes.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateDirectionalLight();
            CreateWorldGridRoot();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            ValidateAuthoredScene(scene);
            Require(!scene.isDirty, "builder leaves WorldSandbox saved and clean");
            Debug.Log("[WORLD-002] BUILD_PASS WorldSandbox stepped terrain prototype authored through Editor API");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WORLD-002] BUILD_FAIL {ex.Message}\n{ex}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    static void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 24f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 150f;
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraObject.transform.position = new Vector3(30f, 32f, -15f);
        cameraObject.transform.LookAt(new Vector3(15f, 2.5f, 15f));
    }

    static void CreateDirectionalLight()
    {
        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.94f, 0.84f);
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
    }

    static void CreateWorldGridRoot()
    {
        var worldGrid = new GameObject("WorldGrid");
        var grid = worldGrid.AddComponent<WorldGridService>();
        grid.ConfigureBootstrapProfileForEditor(WorldGridBootstrapProfile.World002Terraces);
        worldGrid.AddComponent<WorldGridDebugView>();
        worldGrid.AddComponent<WorldChunkTerrain>();
    }

    [MenuItem("Project PA/World/WORLD-002/Validate Terrain Prototype")]
    public static void RunTerrainValidation()
    {
        RunTerrainValidationInternal();
    }

    public static void RunTerrainValidationBatch()
    {
        RunTerrainValidationInternal();
    }

    static void RunTerrainValidationInternal()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetInt(ConsoleErrorKey, 0);
            SessionState.SetInt(WaitFramesKey, 0);
            SubscribeValidationCallbacks();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateAuthoredScene(scene);
            WorldGridService grid = FindSingle<WorldGridService>(scene);
            ValidateTerraceData(grid);
            ValidatePureMesh(grid);
            ValidateSyntheticChunkSeam();
            Debug.Log("[WORLD-002] EDIT_MODE_PASS");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void SubscribeValidationCallbacks()
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
            EditorApplication.update -= ValidateRuntimeAfterFrames;
            EditorApplication.update += ValidateRuntimeAfterFrames;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            FinishValidation();
        }
    }

    static void ValidateRuntimeAfterFrames()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
        int frames = SessionState.GetInt(WaitFramesKey, 0) + 1;
        SessionState.SetInt(WaitFramesKey, frames);
        if (frames < 4) return;
        EditorApplication.update -= ValidateRuntimeAfterFrames;

        try
        {
            Scene scene = SceneManager.GetActiveScene();
            WorldChunkTerrain terrain = FindSingle<WorldChunkTerrain>(scene);
            Require(terrain.IsReady, "runtime terrain visual and collider meshes are ready");
            Require(terrain.VisualMesh.subMeshCount == 2, "runtime mesh has top and cliff submeshes");
            Require(terrain.VisualMesh.vertexCount > 1024, "runtime stepped mesh includes top and cliff vertices");
            Require(terrain.TerrainCollider != null && terrain.TerrainCollider.sharedMesh == terrain.ColliderMesh,
                "MeshCollider uses the generated collider mesh");
            Require(Approximately(terrain.VisualMesh.bounds, terrain.ColliderMesh.bounds),
                "visual and collider bounds match");
            Require(terrain.GetComponent<MeshRenderer>().sharedMaterials.Length == 2,
                "runtime terrain exposes two readable material slots");

            int visualBefore = terrain.VisualRevision;
            int colliderBefore = terrain.ColliderRevision;
            terrain.Rebuild(WorldChunkDirtyFlags.Visual);
            Require(terrain.VisualRevision == visualBefore + 1 && terrain.ColliderRevision == colliderBefore,
                "visual-only dirty rebuild increments only visual revision");
            terrain.Rebuild(WorldChunkDirtyFlags.Collider);
            Require(terrain.VisualRevision == visualBefore + 1 && terrain.ColliderRevision == colliderBefore + 1,
                "collider-only dirty rebuild increments only collider revision");
            Require(terrain.GetComponentsInChildren<Transform>(true).Length <= 2,
                "terrain uses one chunk object rather than GameObject-per-cell");
            Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                "blocking runtime Console Error/Exception/Assert count is 0");
            Debug.Log("[WORLD-002] PLAY_MODE_PASS");
            EditorApplication.ExitPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void ValidateAuthoredScene(Scene scene)
    {
        Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
            "validator targets the saved WorldSandbox scene");
        GameObject[] roots = scene.GetRootGameObjects();
        Require(roots.Length == 3, $"authored scene has exactly three roots ({roots.Length})");
        Require(roots.Count(root => root.name == "Main Camera") == 1, "scene has one Main Camera root");
        Require(roots.Count(root => root.name == "Directional Light") == 1, "scene has one Directional Light root");
        Require(roots.Count(root => root.name == "WorldGrid") == 1, "scene has one WorldGrid root");
        Require(FindComponents<WorldGridService>(scene).Count == 1, "scene has one WorldGridService");
        Require(FindComponents<WorldGridDebugView>(scene).Count == 1, "scene has one WorldGridDebugView");
        Require(FindComponents<WorldChunkTerrain>(scene).Count == 1, "scene has one WorldChunkTerrain");
        Require(FindComponents<MeshFilter>(scene).Count == 1, "scene has one terrain MeshFilter");
        Require(FindComponents<MeshRenderer>(scene).Count == 1, "scene has one terrain MeshRenderer");
        Require(FindComponents<MeshCollider>(scene).Count == 1, "scene has one terrain MeshCollider");
        Require(FindComponents<Terrain>(scene).Count == 0 && FindComponents<TerrainCollider>(scene).Count == 0,
            "WorldSandbox uses no Unity Terrain component");
        int missingScripts = roots.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
        Require(missingScripts == 0, "Missing Script count is 0");
    }

    static void ValidateTerraceData(WorldGridService grid)
    {
        Require(grid.BootstrapProfile == WorldGridBootstrapProfile.World002Terraces,
            "scene grid uses the WORLD-002 terrace bootstrap profile");
        int[] histogram = new int[7];
        foreach (WorldCellData cell in grid.Cells)
        {
            Require(cell.ElevationLevel >= 0 && cell.ElevationLevel <= 6,
                $"cell {cell.Coordinate} elevation is within 0..6", false);
            histogram[cell.ElevationLevel]++;
        }
        Require(histogram.All(count => count > 0), "terrace profile contains every elevation level 0..6");
        Require(grid.TryGetCell(Vector2Int.zero, out WorldCellData corner) && corner.ElevationLevel == 0,
            "outer corner remains level 0");
        Require(grid.TryGetCell(new Vector2Int(7, 7), out WorldCellData center) && center.ElevationLevel == 6,
            "central plateau reaches level 6");
        Require(grid.CellToWorld(new Vector2Int(7, 7), out Vector3 centerWorld) &&
                Mathf.Approximately(centerWorld.y, 6f),
            "CellToWorld projects authoritative elevation into Y");
    }

    static void ValidatePureMesh(WorldGridService grid)
    {
        WorldChunkMeshData first = WorldChunkMeshBuilder.Build(grid.Definition, grid.Cells, Vector2Int.zero);
        WorldChunkMeshData second = WorldChunkMeshBuilder.Build(grid.Definition, grid.Cells, Vector2Int.zero);
        Require(first.TopFaceCount == 256, $"mesh has exactly 256 top faces ({first.TopFaceCount})");
        Require(first.CliffFaceCount > 64, $"mesh includes required outer and terrace cliff faces ({first.CliffFaceCount})");
        Require(first.Vertices.Length == first.Normals.Length && first.Vertices.Length == first.Uvs.Length,
            "vertex, normal, and UV streams have equal length");
        ValidateIndexBounds(first.TopIndices, first.Vertices.Length, "top");
        ValidateIndexBounds(first.CliffIndices, first.Vertices.Length, "cliff");
        ValidateTriangleWinding(first, first.TopIndices, "top");
        ValidateTriangleWinding(first, first.CliffIndices, "cliff");
        Require(first.Vertices.All(IsFinite) && first.Normals.All(IsFinite),
            "mesh contains no NaN or infinite positions/normals");
        ulong firstHash = ComputeMeshHash(first);
        ulong secondHash = ComputeMeshHash(second);
        Require(firstHash == secondHash, $"mesh generation is deterministic ({firstHash:X16})");
        Debug.Log($"[WORLD-002] PASS mesh checksum {firstHash:X16} vertices={first.Vertices.Length} top=256 cliffs={first.CliffFaceCount}");
    }

    static void ValidateSyntheticChunkSeam()
    {
        var definition = new WorldGridDefinition(2f, 32, 16, 16, 1f, 0, 6, Vector3.zero);
        var cells = new WorldCellData[definition.TotalCellCount];
        for (int z = 0; z < definition.Height; z++)
        {
            for (int x = 0; x < definition.Width; x++)
            {
                cells[z * definition.Width + x] = new WorldCellData(
                    new Vector2Int(x, z), 2, WorldGroundType.Default, false, false,
                    WorldCellOccupancy.Empty);
            }
        }
        WorldChunkMeshData west = WorldChunkMeshBuilder.Build(definition, cells, Vector2Int.zero);
        WorldChunkMeshData east = WorldChunkMeshBuilder.Build(definition, cells, new Vector2Int(1, 0));
        const float seamX = 31f;
        const float seamTopY = 2f;
        HashSet<string> westEdge = BoundaryTopPositions(west.Vertices, seamX, seamTopY);
        HashSet<string> eastEdge = BoundaryTopPositions(east.Vertices, seamX, seamTopY);
        Require(westEdge.Count == 17 && westEdge.SetEquals(eastEdge),
            "adjacent chunk top edges share identical seam positions with no gap");
    }

    static HashSet<string> BoundaryTopPositions(IEnumerable<Vector3> vertices, float x, float topY)
    {
        return new HashSet<string>(vertices
            .Where(vertex => Mathf.Abs(vertex.x - x) < 0.0001f &&
                             Mathf.Abs(vertex.y - topY) < 0.0001f)
            .Select(vertex => $"{vertex.z:F4}"));
    }

    static void ValidateIndexBounds(IReadOnlyList<int> indices, int vertexCount, string label)
    {
        Require(indices.Count > 0 && indices.Count % 3 == 0,
            $"{label} index stream contains complete triangles");
        for (int i = 0; i < indices.Count; i++)
        {
            if (indices[i] < 0 || indices[i] >= vertexCount)
                throw new InvalidOperationException($"{label} index {indices[i]} is outside {vertexCount} vertices");
        }
        Require(true, $"{label} indices are within vertex bounds");
    }

    static void ValidateTriangleWinding(WorldChunkMeshData data, IReadOnlyList<int> indices, string label)
    {
        for (int i = 0; i < indices.Count; i += 3)
        {
            int a = indices[i];
            int b = indices[i + 1];
            int c = indices[i + 2];
            Vector3 cross = Vector3.Cross(data.Vertices[b] - data.Vertices[a], data.Vertices[c] - data.Vertices[a]);
            if (cross.sqrMagnitude < 0.000001f || Vector3.Dot(cross.normalized, data.Normals[a]) < 0.999f)
                throw new InvalidOperationException($"{label} triangle {i / 3} has invalid winding or normal");
        }
        Require(true, $"{label} winding and normals are outward-facing");
    }

    static ulong ComputeMeshHash(WorldChunkMeshData data)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        unchecked
        {
            foreach (Vector3 vertex in data.Vertices)
            {
                hash = (hash ^ (uint)Mathf.RoundToInt(vertex.x * 1000f)) * prime;
                hash = (hash ^ (uint)Mathf.RoundToInt(vertex.y * 1000f)) * prime;
                hash = (hash ^ (uint)Mathf.RoundToInt(vertex.z * 1000f)) * prime;
            }
            foreach (int index in data.TopIndices) hash = (hash ^ (uint)index) * prime;
            foreach (int index in data.CliffIndices) hash = (hash ^ (uint)index) * prime;
        }
        return hash;
    }

    static bool IsFinite(Vector3 value)
    {
        return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    static bool Approximately(Bounds first, Bounds second)
    {
        return Vector3.Distance(first.center, second.center) < 0.001f &&
               Vector3.Distance(first.size, second.size) < 0.001f;
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
        Debug.LogError($"[WORLD-002] FAIL {ex.Message}\n{ex}");
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
        EditorApplication.update -= ValidateRuntimeAfterFrames;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        Debug.Log(failed
            ? $"[WORLD-002] FINISHED_WITH_ERRORS consoleErrors={consoleErrors}"
            : "[WORLD-002] FINISHED_PASS cells=256 chunk=16x16 elevations=0..6 topFaces=256 meshCollider=true seams=true dirtyRebuild=true");
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
            throw new InvalidOperationException($"Expected one {typeof(T).Name}, found {components.Count}.");
        return components[0];
    }

    static void Require(bool condition, string message, bool writePassLog = true)
    {
        if (!condition) throw new InvalidOperationException(message);
        if (writePassLog) Debug.Log($"[WORLD-002] PASS {message}");
    }
}
#endif
