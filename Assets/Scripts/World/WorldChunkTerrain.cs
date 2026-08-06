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

[Flags]
public enum WorldCliffMask
{
    None = 0,
    West = 1 << 0,
    East = 1 << 1,
    South = 1 << 2,
    North = 1 << 3
}

public static class WorldSurfaceMaterialSlots
{
    public const int Grass = 0;
    public const int Soil = 1;
    public const int Sand = 2;
    public const int Rock = 3;
    public const int DirtPath = 4;
    public const int StonePath = 5;
    public const int Cliff = 6;
    public const int Water = 7;
    public const int Count = 8;
}

public sealed class WorldChunkMeshData
{
    public Vector3[] Vertices { get; }
    public Vector3[] Normals { get; }
    public Vector2[] Uvs { get; }
    public int[] TopIndices { get; }
    public int[] CliffIndices { get; }
    public int[][] VisualSubmeshIndices { get; }
    public int[] WaterIndices { get; }
    public Bounds Bounds { get; }

    public int TopFaceCount => TopIndices.Length / 6;
    public int CliffFaceCount => CliffIndices.Length / 6;
    public int WaterSurfaceFaceCount { get; }
    public int ShorelineFaceCount { get; }

    public WorldChunkMeshData(
        Vector3[] vertices,
        Vector3[] normals,
        Vector2[] uvs,
        int[] topIndices,
        int[] cliffIndices,
        int[][] visualSubmeshIndices,
        int[] waterIndices,
        int waterSurfaceFaceCount,
        int shorelineFaceCount,
        Bounds bounds)
    {
        Vertices = vertices;
        Normals = normals;
        Uvs = uvs;
        TopIndices = topIndices;
        CliffIndices = cliffIndices;
        VisualSubmeshIndices = visualSubmeshIndices;
        WaterIndices = waterIndices;
        WaterSurfaceFaceCount = waterSurfaceFaceCount;
        ShorelineFaceCount = shorelineFaceCount;
        Bounds = bounds;
    }
}

public static class WorldChunkMeshBuilder
{
    public static WorldCliffMask ComputeCliffMask(
        WorldGridDefinition definition,
        IReadOnlyList<WorldCellData> cells,
        Vector2Int coordinate)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        if (cells == null) throw new ArgumentNullException(nameof(cells));
        if (cells.Count != definition.TotalCellCount)
            throw new ArgumentException("Cell count does not match the world definition.", nameof(cells));
        if (coordinate.x < 0 || coordinate.x >= definition.Width ||
            coordinate.y < 0 || coordinate.y >= definition.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate));
        }

        int elevation = cells[coordinate.y * definition.Width + coordinate.x].ElevationLevel;
        WorldCliffMask mask = WorldCliffMask.None;
        if (IsExposedToLowerNeighbor(definition, cells, coordinate, Vector2Int.left, elevation))
            mask |= WorldCliffMask.West;
        if (IsExposedToLowerNeighbor(definition, cells, coordinate, Vector2Int.right, elevation))
            mask |= WorldCliffMask.East;
        if (IsExposedToLowerNeighbor(definition, cells, coordinate, Vector2Int.down, elevation))
            mask |= WorldCliffMask.South;
        if (IsExposedToLowerNeighbor(definition, cells, coordinate, Vector2Int.up, elevation))
            mask |= WorldCliffMask.North;
        return mask;
    }

    static bool IsExposedToLowerNeighbor(
        WorldGridDefinition definition,
        IReadOnlyList<WorldCellData> cells,
        Vector2Int coordinate,
        Vector2Int offset,
        int elevation)
    {
        Vector2Int neighbor = coordinate + offset;
        if (neighbor.x < 0 || neighbor.x >= definition.Width ||
            neighbor.y < 0 || neighbor.y >= definition.Height)
        {
            return true;
        }

        return cells[neighbor.y * definition.Width + neighbor.x].ElevationLevel < elevation;
    }

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
        var waterIndices = new List<int>();
        var visualSubmeshIndices = new List<int>[WorldSurfaceMaterialSlots.Count];
        for (int i = 0; i < visualSubmeshIndices.Length; i++)
            visualSubmeshIndices[i] = new List<int>();
        int waterSurfaceFaceCount = 0;
        int shorelineFaceCount = 0;

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
                int topVertexStart = vertices.Count;
                AddQuad(vertices, normals, uvs, topIndices,
                    southWest, northWest, northEast, southEast, Vector3.up, 1f);
                AddQuadIndices(
                    visualSubmeshIndices[ResolveTopMaterialSlot(cell)],
                    topVertexStart);

                AddCliffIfNeeded(definition, cells, x, z, -1, 0, outsideBaseY, topY,
                    vertices, normals, uvs, cliffIndices,
                    visualSubmeshIndices[WorldSurfaceMaterialSlots.Cliff],
                    bottomY => new[]
                    {
                        new Vector3(centerX - halfCell, bottomY, centerZ - halfCell),
                        new Vector3(centerX - halfCell, bottomY, centerZ + halfCell),
                        northWest,
                        southWest
                    }, Vector3.left);
                AddCliffIfNeeded(definition, cells, x, z, 1, 0, outsideBaseY, topY,
                    vertices, normals, uvs, cliffIndices,
                    visualSubmeshIndices[WorldSurfaceMaterialSlots.Cliff],
                    bottomY => new[]
                    {
                        new Vector3(centerX + halfCell, bottomY, centerZ - halfCell),
                        southEast,
                        northEast,
                        new Vector3(centerX + halfCell, bottomY, centerZ + halfCell)
                    }, Vector3.right);
                AddCliffIfNeeded(definition, cells, x, z, 0, -1, outsideBaseY, topY,
                    vertices, normals, uvs, cliffIndices,
                    visualSubmeshIndices[WorldSurfaceMaterialSlots.Cliff],
                    bottomY => new[]
                    {
                        new Vector3(centerX - halfCell, bottomY, centerZ - halfCell),
                        southWest,
                        southEast,
                        new Vector3(centerX + halfCell, bottomY, centerZ - halfCell)
                    }, Vector3.back);
                AddCliffIfNeeded(definition, cells, x, z, 0, 1, outsideBaseY, topY,
                    vertices, normals, uvs, cliffIndices,
                    visualSubmeshIndices[WorldSurfaceMaterialSlots.Cliff],
                    bottomY => new[]
                    {
                        new Vector3(centerX - halfCell, bottomY, centerZ + halfCell),
                        new Vector3(centerX + halfCell, bottomY, centerZ + halfCell),
                        northEast,
                        northWest
                    }, Vector3.forward);

                if (cell.HasWater)
                {
                    AddWaterGeometry(
                        definition,
                        cells,
                        x,
                        z,
                        cell,
                        centerX,
                        centerZ,
                        halfCell,
                        topY,
                        vertices,
                        normals,
                        uvs,
                        waterIndices,
                        visualSubmeshIndices[WorldSurfaceMaterialSlots.Water],
                        ref waterSurfaceFaceCount,
                        ref shorelineFaceCount);
                }
            }
        }

        Bounds bounds = CalculateBounds(vertices);
        var visualIndices = new int[WorldSurfaceMaterialSlots.Count][];
        for (int i = 0; i < visualIndices.Length; i++)
            visualIndices[i] = visualSubmeshIndices[i].ToArray();
        return new WorldChunkMeshData(
            vertices.ToArray(),
            normals.ToArray(),
            uvs.ToArray(),
            topIndices.ToArray(),
            cliffIndices.ToArray(),
            visualIndices,
            waterIndices.ToArray(),
            waterSurfaceFaceCount,
            shorelineFaceCount,
            bounds);
    }

    static int ResolveTopMaterialSlot(WorldCellData cell)
    {
        if (cell.PathType == WorldPathType.Dirt) return WorldSurfaceMaterialSlots.DirtPath;
        if (cell.PathType == WorldPathType.Stone) return WorldSurfaceMaterialSlots.StonePath;
        return (int)cell.GroundType switch
        {
            1 => WorldSurfaceMaterialSlots.Soil,
            2 => WorldSurfaceMaterialSlots.Sand,
            3 => WorldSurfaceMaterialSlots.Rock,
            _ => WorldSurfaceMaterialSlots.Grass
        };
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
        List<int> visualIndices,
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
        int vertexStart = vertices.Count;
        AddQuad(vertices, normals, uvs, indices,
            face[0], face[1], face[2], face[3], normal, verticalUvScale);
        AddQuadIndices(visualIndices, vertexStart);
    }

    static void AddWaterGeometry(
        WorldGridDefinition definition,
        IReadOnlyList<WorldCellData> cells,
        int x,
        int z,
        WorldCellData cell,
        float centerX,
        float centerZ,
        float halfCell,
        float bedY,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> waterIndices,
        List<int> visualWaterIndices,
        ref int waterSurfaceFaceCount,
        ref int shorelineFaceCount)
    {
        float waterY = definition.WorldOrigin.y +
                       cell.WaterSurfaceLevel * definition.ElevationStep;
        Vector3 southWest = new Vector3(centerX - halfCell, waterY, centerZ - halfCell);
        Vector3 northWest = new Vector3(centerX - halfCell, waterY, centerZ + halfCell);
        Vector3 northEast = new Vector3(centerX + halfCell, waterY, centerZ + halfCell);
        Vector3 southEast = new Vector3(centerX + halfCell, waterY, centerZ - halfCell);
        int topStart = vertices.Count;
        AddQuad(vertices, normals, uvs, waterIndices,
            southWest, northWest, northEast, southEast, Vector3.up, 1f);
        AddQuadIndices(visualWaterIndices, topStart);
        waterSurfaceFaceCount++;

        AddShorelineIfNeeded(definition, cells, x, z, -1, 0, bedY, waterY,
            vertices, normals, uvs, waterIndices, visualWaterIndices,
            bottomY => new[]
            {
                new Vector3(centerX - halfCell, bottomY, centerZ - halfCell),
                new Vector3(centerX - halfCell, bottomY, centerZ + halfCell),
                northWest,
                southWest
            }, Vector3.left, ref shorelineFaceCount);
        AddShorelineIfNeeded(definition, cells, x, z, 1, 0, bedY, waterY,
            vertices, normals, uvs, waterIndices, visualWaterIndices,
            bottomY => new[]
            {
                new Vector3(centerX + halfCell, bottomY, centerZ - halfCell),
                southEast,
                northEast,
                new Vector3(centerX + halfCell, bottomY, centerZ + halfCell)
            }, Vector3.right, ref shorelineFaceCount);
        AddShorelineIfNeeded(definition, cells, x, z, 0, -1, bedY, waterY,
            vertices, normals, uvs, waterIndices, visualWaterIndices,
            bottomY => new[]
            {
                new Vector3(centerX - halfCell, bottomY, centerZ - halfCell),
                southWest,
                southEast,
                new Vector3(centerX + halfCell, bottomY, centerZ - halfCell)
            }, Vector3.back, ref shorelineFaceCount);
        AddShorelineIfNeeded(definition, cells, x, z, 0, 1, bedY, waterY,
            vertices, normals, uvs, waterIndices, visualWaterIndices,
            bottomY => new[]
            {
                new Vector3(centerX - halfCell, bottomY, centerZ + halfCell),
                new Vector3(centerX + halfCell, bottomY, centerZ + halfCell),
                northEast,
                northWest
            }, Vector3.forward, ref shorelineFaceCount);
    }

    static void AddShorelineIfNeeded(
        WorldGridDefinition definition,
        IReadOnlyList<WorldCellData> cells,
        int x,
        int z,
        int offsetX,
        int offsetZ,
        float bedY,
        float waterY,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> waterIndices,
        List<int> visualWaterIndices,
        Func<float, Vector3[]> corners,
        Vector3 normal,
        ref int shorelineFaceCount)
    {
        int neighborX = x + offsetX;
        int neighborZ = z + offsetZ;
        float bottomY = bedY;
        if (neighborX >= 0 && neighborX < definition.Width &&
            neighborZ >= 0 && neighborZ < definition.Height)
        {
            WorldCellData neighbor = cells[neighborZ * definition.Width + neighborX];
            if (neighbor.HasWater)
            {
                float neighborWaterY = definition.WorldOrigin.y +
                                       neighbor.WaterSurfaceLevel * definition.ElevationStep;
                if (neighborWaterY >= waterY - 0.0001f) return;
                bottomY = Mathf.Max(bedY, neighborWaterY);
            }
            else
            {
                float neighborGroundY = definition.WorldOrigin.y +
                                        neighbor.ElevationLevel * definition.ElevationStep;
                bottomY = Mathf.Max(bedY, neighborGroundY);
            }
        }

        if (bottomY >= waterY - 0.0001f) return;
        Vector3[] face = corners(bottomY);
        int start = vertices.Count;
        AddQuad(vertices, normals, uvs, waterIndices,
            face[0], face[1], face[2], face[3], normal,
            Mathf.Max(1f, (waterY - bottomY) / definition.CellSize));
        AddQuadIndices(visualWaterIndices, start);
        shorelineFaceCount++;
    }

    static void AddQuadIndices(List<int> indices, int start)
    {
        indices.Add(start);
        indices.Add(start + 1);
        indices.Add(start + 2);
        indices.Add(start);
        indices.Add(start + 2);
        indices.Add(start + 3);
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
        SubscribeTerraform();
        EnsureBuilt();
    }

    void OnEnable()
    {
        ResolveComponents();
        SubscribeTerraform();
        EnsureBuilt();
    }

    void OnDisable()
    {
        UnsubscribeTerraform();
    }

    void OnDestroy()
    {
        UnsubscribeTerraform();
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
            ApplyVisualMeshData(_visualMesh, data);
            _filter.sharedMesh = _visualMesh;
            EnsureMaterials();
            _renderer.sharedMaterials = _materials;
            VisualRevision++;
        }

        if ((dirtyFlags & WorldChunkDirtyFlags.Collider) != 0)
        {
            if (_colliderMesh == null) _colliderMesh = CreateMesh("WorldChunk_0_0_Collider");
            ApplyColliderMeshData(_colliderMesh, data);
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

    void SubscribeTerraform()
    {
        if (_grid == null) return;
        _grid.Terraform.Changed -= OnTerraformChanged;
        _grid.Terraform.Changed += OnTerraformChanged;
        _grid.SurfaceEditor.Changed -= OnSurfaceChanged;
        _grid.SurfaceEditor.Changed += OnSurfaceChanged;
    }

    void UnsubscribeTerraform()
    {
        if (_grid == null) return;
        _grid.Terraform.Changed -= OnTerraformChanged;
        _grid.SurfaceEditor.Changed -= OnSurfaceChanged;
    }

    void OnTerraformChanged(WorldTerraformEditResult result)
    {
        for (int i = 0; i < result.DirtyChunks.Count; i++)
        {
            if (result.DirtyChunks[i] != chunkCoordinate) continue;
            Rebuild(WorldChunkDirtyFlags.All);
            return;
        }
    }

    void OnSurfaceChanged(WorldSurfaceEditResult result)
    {
        for (int i = 0; i < result.DirtyChunks.Count; i++)
        {
            if (result.DirtyChunks[i] != chunkCoordinate) continue;
            Rebuild(WorldChunkDirtyFlags.Visual);
            return;
        }
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

    static void ApplyVisualMeshData(Mesh mesh, WorldChunkMeshData data)
    {
        mesh.Clear();
        mesh.vertices = data.Vertices;
        mesh.normals = data.Normals;
        mesh.uv = data.Uvs;
        mesh.subMeshCount = WorldSurfaceMaterialSlots.Count;
        for (int i = 0; i < WorldSurfaceMaterialSlots.Count; i++)
            mesh.SetTriangles(data.VisualSubmeshIndices[i], i, false);
        mesh.bounds = data.Bounds;
        mesh.UploadMeshData(false);
    }

    static void ApplyColliderMeshData(Mesh mesh, WorldChunkMeshData data)
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
        if (_materials != null && _materials.Length == WorldSurfaceMaterialSlots.Count)
        {
            bool valid = true;
            for (int i = 0; i < _materials.Length; i++) valid &= _materials[i] != null;
            if (valid) return;
        }
        _materials = new[]
        {
            CreateTerrainMaterial("WorldTerrainGrass_Runtime", topColor),
            CreateTerrainMaterial("WorldTerrainSoil_Runtime", new Color(0.48f, 0.31f, 0.18f, 1f)),
            CreateTerrainMaterial("WorldTerrainSand_Runtime", new Color(0.78f, 0.68f, 0.43f, 1f)),
            CreateTerrainMaterial("WorldTerrainRock_Runtime", new Color(0.42f, 0.44f, 0.43f, 1f)),
            CreateTerrainMaterial("WorldTerrainDirtPath_Runtime", new Color(0.56f, 0.39f, 0.23f, 1f)),
            CreateTerrainMaterial("WorldTerrainStonePath_Runtime", new Color(0.53f, 0.55f, 0.52f, 1f)),
            CreateTerrainMaterial("WorldTerrainCliff_Runtime", cliffColor),
            CreateTerrainMaterial("WorldTerrainWater_Runtime", new Color(0.22f, 0.55f, 0.72f, 1f))
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
            Require(terrain.VisualMesh.subMeshCount == WorldSurfaceMaterialSlots.Count,
                "runtime mesh preserves ground, path, cliff and water material slots");
            Require(terrain.VisualMesh.vertexCount > 1024, "runtime stepped mesh includes top and cliff vertices");
            Require(terrain.TerrainCollider != null && terrain.TerrainCollider.sharedMesh == terrain.ColliderMesh,
                "MeshCollider uses the generated collider mesh");
            Require(Approximately(terrain.VisualMesh.bounds, terrain.ColliderMesh.bounds),
                "visual and collider bounds match");
            Require(terrain.GetComponent<MeshRenderer>().sharedMaterials.Length == WorldSurfaceMaterialSlots.Count,
                "runtime terrain exposes all project-owned surface material slots");

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
        Require(first.VisualSubmeshIndices.Length == WorldSurfaceMaterialSlots.Count &&
                first.WaterIndices.Length == 0,
            "baseline terrain has stable surface slots and no water geometry");
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

public static class PA_WorldTerraformTools
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.WORLD003.Active";
    const string FailedKey = "PA.WORLD003.Failed";
    const string ConsoleErrorKey = "PA.WORLD003.ConsoleErrors";
    const string WaitFramesKey = "PA.WORLD003.WaitFrames";
    const string StageKey = "PA.WORLD003.Stage";

    [InitializeOnLoadMethod]
    static void ResumeValidationAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        SubscribeCallbacks();
        if (EditorApplication.isPlaying) EditorApplication.update += ValidateRuntime;
    }

    [MenuItem("Project PA/World/WORLD-003/Validate Single-Cell Terraforming")]
    public static void RunTerraformValidation()
    {
        RunTerraformValidationInternal();
    }

    public static void RunTerraformValidationBatch()
    {
        RunTerraformValidationInternal();
    }

    static void RunTerraformValidationInternal()
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
            Require(FindComponents<Terrain>(scene).Count == 0 &&
                    FindComponents<TerrainCollider>(scene).Count == 0,
                "terraforming continues to use no Unity Terrain component");

            WorldGridService grid = FindSingle<WorldGridService>(scene);
            ValidateEditTransactions(grid);
            ValidateBoundaryDirtyChunks();
            Require(!scene.isDirty, "Edit Mode transactions do not dirty the scene");
            Debug.Log("[WORLD-003] EDIT_MODE_PASS");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void ValidateEditTransactions(WorldGridService grid)
    {
        Require(grid.BootstrapProfile == WorldGridBootstrapProfile.World002Terraces,
            "WORLD-002 terrace data is the terraform baseline");
        Require(grid.ProtectedTerraformCell == Vector2Int.zero,
            "cell (0,0) is the explicit protected terraform cell");

        WorldTerraformService terraform = grid.Terraform;
        int changeEvents = 0;
        terraform.Changed += _ => changeEvents++;
        ulong baselineHash = ComputeCellHash(grid.Cells);
        int baselineRevision = terraform.Revision;

        AssertFailedWithoutMutation(
            terraform.Raise(new Vector2Int(-1, 0)),
            WorldTerraformFailure.OutOfBounds,
            baselineHash,
            baselineRevision,
            grid,
            terraform,
            "out-of-bounds edit");
        AssertFailedWithoutMutation(
            terraform.Raise(Vector2Int.zero),
            WorldTerraformFailure.ProtectedCell,
            baselineHash,
            baselineRevision,
            grid,
            terraform,
            "protected-cell edit");
        AssertFailedWithoutMutation(
            terraform.Lower(new Vector2Int(1, 0)),
            WorldTerraformFailure.MinimumElevation,
            baselineHash,
            baselineRevision,
            grid,
            terraform,
            "minimum-level edit");

        var target = new Vector2Int(7, 7);
        AssertFailedWithoutMutation(
            terraform.Raise(target),
            WorldTerraformFailure.MaximumElevation,
            baselineHash,
            baselineRevision,
            grid,
            terraform,
            "maximum-level edit");

        var westNeighbor = new Vector2Int(6, 7);
        WorldCliffMask beforeMask = WorldChunkMeshBuilder.ComputeCliffMask(
            grid.Definition, grid.Cells, westNeighbor);
        Require((beforeMask & WorldCliffMask.East) == 0,
            "equal-height plateau starts without an internal east cliff");

        WorldTerraformEditResult lowered = terraform.Lower(target);
        Require(lowered.Succeeded && lowered.Failure == WorldTerraformFailure.None,
            "one-cell lower transaction succeeds");
        Require(lowered.PreviousElevationLevel == 6 && lowered.CurrentElevationLevel == 5,
            "one-cell lower applies exactly one 1m elevation step");
        Require(lowered.DirtyChunks.Count == 1 && lowered.DirtyChunks[0] == Vector2Int.zero,
            "interior edit dirties only its owning chunk");
        Require(grid.TryGetCell(target, out WorldCellData loweredCell) &&
                loweredCell.ElevationLevel == 5 &&
                loweredCell.GroundType == WorldGroundType.Default &&
                !loweredCell.HasWater && !loweredCell.HasPath &&
                loweredCell.Occupancy == WorldCellOccupancy.Empty,
            "elevation edit preserves all non-elevation cell data");
        WorldCliffMask afterMask = WorldChunkMeshBuilder.ComputeCliffMask(
            grid.Definition, grid.Cells, westNeighbor);
        Require((afterMask & WorldCliffMask.East) != 0,
            "lowered cell immediately exposes the adjacent east cliff mask");
        Require(ComputeCellHash(grid.Cells) != baselineHash,
            "successful transaction changes the authoritative in-memory cell hash");

        WorldTerraformEditResult undone = terraform.UndoLast();
        Require(undone.Succeeded && undone.Coordinate == target &&
                undone.PreviousElevationLevel == 5 && undone.CurrentElevationLevel == 6,
            "single in-session undo restores the exact previous elevation");
        Require(ComputeCellHash(grid.Cells) == baselineHash,
            "undo restores the complete baseline cell hash");
        Require((WorldChunkMeshBuilder.ComputeCliffMask(
                    grid.Definition, grid.Cells, westNeighbor) & WorldCliffMask.East) == 0,
            "undo restores the adjacent cliff mask");
        Require(changeEvents == 2 && terraform.Revision == baselineRevision + 2,
            "only the successful edit and undo publish change revisions");

        ulong beforeSecondUndo = ComputeCellHash(grid.Cells);
        int revisionBeforeSecondUndo = terraform.Revision;
        AssertFailedWithoutMutation(
            terraform.UndoLast(),
            WorldTerraformFailure.NoUndoAvailable,
            beforeSecondUndo,
            revisionBeforeSecondUndo,
            grid,
            terraform,
            "second undo");
    }

    static void ValidateBoundaryDirtyChunks()
    {
        var definition = new WorldGridDefinition(2f, 32, 16, 16, 1f, 0, 6, Vector3.zero);
        IReadOnlyList<Vector2Int> westBoundary =
            WorldTerraformDirtyChunkResolver.Resolve(definition, new Vector2Int(15, 5));
        Require(westBoundary.Count == 2 &&
                westBoundary.Contains(new Vector2Int(0, 0)) &&
                westBoundary.Contains(new Vector2Int(1, 0)),
            "cell on an east chunk edge dirties both seam-sharing chunks");

        IReadOnlyList<Vector2Int> eastBoundary =
            WorldTerraformDirtyChunkResolver.Resolve(definition, new Vector2Int(16, 5));
        Require(eastBoundary.Count == 2 &&
                eastBoundary.Contains(new Vector2Int(0, 0)) &&
                eastBoundary.Contains(new Vector2Int(1, 0)),
            "cell on a west chunk edge dirties both seam-sharing chunks");

        IReadOnlyList<Vector2Int> interior =
            WorldTerraformDirtyChunkResolver.Resolve(definition, new Vector2Int(7, 5));
        Require(interior.Count == 1 && interior[0] == Vector2Int.zero,
            "interior cell does not dirty unrelated chunks");
    }

    static void AssertFailedWithoutMutation(
        WorldTerraformEditResult result,
        WorldTerraformFailure expectedFailure,
        ulong expectedHash,
        int expectedRevision,
        WorldGridService grid,
        WorldTerraformService terraform,
        string label)
    {
        Require(!result.Succeeded && result.Failure == expectedFailure,
            $"{label} fails with {expectedFailure}");
        Require(result.DirtyChunks.Count == 0 &&
                terraform.Revision == expectedRevision &&
                ComputeCellHash(grid.Cells) == expectedHash,
            $"{label} is atomic and publishes no dirty chunk");
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
        if (frames < 4) return;

        try
        {
            Scene scene = SceneManager.GetActiveScene();
            WorldGridService grid = FindSingle<WorldGridService>(scene);
            WorldChunkTerrain terrain = FindSingle<WorldChunkTerrain>(scene);
            WorldGridDebugView debug = FindSingle<WorldGridDebugView>(scene);
            int stage = SessionState.GetInt(StageKey, 0);
            if (stage == 0)
            {
                ValidateRuntimeEdit(grid, terrain, debug);
                SessionState.SetInt(StageKey, 1);
                SessionState.SetInt(WaitFramesKey, 0);
                return;
            }

            if (frames < 3) return;
            EditorApplication.update -= ValidateRuntime;
            Require(debug.IsRuntimeGeometryReady,
                "debug grid rebuilds after the terrain edit without a per-cell object");
            Require(terrain.GetComponentsInChildren<Transform>(true).Length <= 2,
                "terraforming keeps one chunk object and one debug mesh root");
            Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                "blocking runtime Console Error/Exception/Assert count is 0");
            Debug.Log("[WORLD-003] PLAY_MODE_PASS");
            EditorApplication.ExitPlaymode();
        }
        catch (Exception ex)
        {
            EditorApplication.update -= ValidateRuntime;
            Fail(ex);
        }
    }

    static void ValidateRuntimeEdit(
        WorldGridService grid,
        WorldChunkTerrain terrain,
        WorldGridDebugView debug)
    {
        Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
            $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
        Require(terrain.IsReady, "runtime terrain visual and collider meshes are ready");

        int visualStart = terrain.VisualRevision;
        int colliderStart = terrain.ColliderRevision;
        WorldTerraformEditResult noSelection = debug.RaiseSelected();
        Require(!noSelection.Succeeded && noSelection.Failure == WorldTerraformFailure.NoSelection,
            "debug raise requires an explicit selected cell");
        Require(terrain.VisualRevision == visualStart && terrain.ColliderRevision == colliderStart,
            "missing selection does not rebuild terrain");

        var target = new Vector2Int(7, 7);
        Require(debug.TrySelectCell(target) && debug.SelectedCell == target,
            "debug selection accepts a valid terrain cell");
        WorldTerraformEditResult lowered = debug.LowerSelected();
        Require(lowered.Succeeded && lowered.CurrentElevationLevel == 5,
            "debug lower applies one level to the selected cell");
        Require(terrain.VisualRevision == visualStart + 1 &&
                terrain.ColliderRevision == colliderStart + 1,
            "successful edit rebuilds visual and collider exactly once");
        Require(grid.CellToWorld(target, out Vector3 loweredWorld) &&
                Mathf.Approximately(loweredWorld.y, 5f),
            "selected cell world height immediately reflects the edit");
        Require(Approximately(terrain.VisualMesh.bounds, terrain.ColliderMesh.bounds),
            "edited visual and collider bounds remain identical");

        Physics.SyncTransforms();
        Ray ray = new Ray(loweredWorld + Vector3.up * 10f, Vector3.down);
        Require(terrain.TerrainCollider.Raycast(ray, out RaycastHit hit, 20f) &&
                Mathf.Abs(hit.point.y - loweredWorld.y) < 0.01f,
            "MeshCollider raycast follows the edited cell top");
        Require((WorldChunkMeshBuilder.ComputeCliffMask(
                    grid.Definition, grid.Cells, new Vector2Int(6, 7)) & WorldCliffMask.East) != 0,
            "runtime adjacent cliff mask follows the lowered cell");

        int visualAfterLower = terrain.VisualRevision;
        int colliderAfterLower = terrain.ColliderRevision;
        Require(debug.TrySelectCell(Vector2Int.zero), "debug can select the protected cell");
        WorldTerraformEditResult protectedResult = debug.RaiseSelected();
        Require(!protectedResult.Succeeded &&
                protectedResult.Failure == WorldTerraformFailure.ProtectedCell,
            "protected cell rejects runtime raise");
        Require(terrain.VisualRevision == visualAfterLower &&
                terrain.ColliderRevision == colliderAfterLower,
            "failed protected edit triggers no mesh or collider rebuild");

        WorldTerraformEditResult undone = debug.UndoLastTerraform();
        Require(undone.Succeeded && undone.Coordinate == target &&
                undone.CurrentElevationLevel == 6,
            "runtime one-step undo restores the edited cell");
        Require(terrain.VisualRevision == visualAfterLower + 1 &&
                terrain.ColliderRevision == colliderAfterLower + 1,
            "undo rebuilds visual and collider exactly once");

        int visualAfterUndo = terrain.VisualRevision;
        int colliderAfterUndo = terrain.ColliderRevision;
        WorldTerraformEditResult secondUndo = debug.UndoLastTerraform();
        Require(!secondUndo.Succeeded &&
                secondUndo.Failure == WorldTerraformFailure.NoUndoAvailable &&
                terrain.VisualRevision == visualAfterUndo &&
                terrain.ColliderRevision == colliderAfterUndo,
            "unavailable second undo is atomic and causes no rebuild");

        Require(debug.TrySelectCell(target), "debug can reselect the restored maximum cell");
        WorldTerraformEditResult maximumResult = debug.RaiseSelected();
        Require(!maximumResult.Succeeded &&
                maximumResult.Failure == WorldTerraformFailure.MaximumElevation &&
                terrain.VisualRevision == visualAfterUndo &&
                terrain.ColliderRevision == colliderAfterUndo,
            "maximum-level runtime raise is clamped without mutation");
    }

    static ulong ComputeCellHash(IReadOnlyList<WorldCellData> cells)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        unchecked
        {
            for (int i = 0; i < cells.Count; i++)
            {
                WorldCellData cell = cells[i];
                hash = (hash ^ (uint)cell.Coordinate.x) * prime;
                hash = (hash ^ (uint)cell.Coordinate.y) * prime;
                hash = (hash ^ (uint)cell.ElevationLevel) * prime;
                hash = (hash ^ (uint)cell.GroundType) * prime;
                hash = (hash ^ (cell.HasWater ? 1u : 0u)) * prime;
                hash = (hash ^ (cell.HasPath ? 1u : 0u)) * prime;
                hash = (hash ^ (uint)cell.Occupancy) * prime;
            }
        }
        return hash;
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
        Debug.LogError($"[WORLD-003] FAIL {ex.Message}\n{ex}");
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
            ? $"[WORLD-003] FINISHED_WITH_ERRORS consoleErrors={consoleErrors}"
            : "[WORLD-003] FINISHED_PASS protected=true minMax=true atomic=true dirtyChunks=true meshCollider=true undo=true");
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

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[WORLD-003] PASS {message}");
    }
}

public static class PA_WorldSurfaceTools
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.WORLD004.Active";
    const string FailedKey = "PA.WORLD004.Failed";
    const string ConsoleErrorKey = "PA.WORLD004.ConsoleErrors";
    const string WaitFramesKey = "PA.WORLD004.WaitFrames";

    [InitializeOnLoadMethod]
    static void ResumeValidationAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        SubscribeCallbacks();
        if (EditorApplication.isPlaying) EditorApplication.update += ValidateRuntime;
    }

    [MenuItem("Project PA/World/WORLD-004/Validate Ground Path Water Prototype")]
    public static void RunSurfaceValidation()
    {
        RunSurfaceValidationInternal();
    }

    public static void RunSurfaceValidationBatch()
    {
        RunSurfaceValidationInternal();
    }

    static void RunSurfaceValidationInternal()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetInt(ConsoleErrorKey, 0);
            SessionState.SetInt(WaitFramesKey, 0);
            SubscribeCallbacks();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "validator targets only WorldSandbox");
            Require(!scene.isDirty, "WorldSandbox starts clean");
            Require(FindComponents<Terrain>(scene).Count == 0 &&
                    FindComponents<TerrainCollider>(scene).Count == 0,
                "surface prototype uses no Unity Terrain component");
            WorldGridService grid = FindSingle<WorldGridService>(scene);
            ValidateSurfaceTransactions(grid);
            Require(!scene.isDirty, "surface transactions remain in memory and do not dirty the scene");
            Debug.Log("[WORLD-004] EDIT_MODE_PASS");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void ValidateSurfaceTransactions(WorldGridService grid)
    {
        WorldSurfaceEditService editor = grid.SurfaceEditor;
        ulong baselineHash = ComputeCellHash(grid.Cells);
        int baselineRevision = editor.Revision;
        int eventCount = 0;
        editor.Changed += _ => eventCount++;

        AssertFailure(editor.PaintGround(Vector2Int.zero, WorldGroundType.Soil),
            WorldSurfaceEditFailure.ProtectedCell, baselineHash, baselineRevision,
            grid, editor, "protected ground paint");
        AssertFailure(editor.FillWater(new Vector2Int(7, 7), 7),
            WorldSurfaceEditFailure.InvalidWaterLevel, baselineHash, baselineRevision,
            grid, editor, "water above maximum level");
        AssertFailure(editor.DrainWater(new Vector2Int(1, 0)),
            WorldSurfaceEditFailure.WaterNotPresent, baselineHash, baselineRevision,
            grid, editor, "drain on a dry cell");

        var groundCell = new Vector2Int(4, 4);
        WorldSurfaceEditResult soil = editor.PaintGround(groundCell, WorldGroundType.Soil);
        Require(soil.Succeeded && soil.Kind == WorldSurfaceEditKind.PaintGround,
            "soil paint transaction succeeds");
        Require(grid.TryGetCell(groundCell, out WorldCellData soilCell) &&
                soilCell.GroundType == WorldGroundType.Soil && soilCell.IsFarmable && soilCell.IsWalkable,
            "soil is visibly typed, farmable and walkable");
        WorldChunkMeshData soilMesh = WorldChunkMeshBuilder.Build(
            grid.Definition, grid.Cells, Vector2Int.zero);
        Require(soilMesh.VisualSubmeshIndices[WorldSurfaceMaterialSlots.Soil].Length == 6,
            "one soil cell occupies exactly one soil material quad");
        Require(editor.UndoLast().Succeeded && ComputeCellHash(grid.Cells) == baselineHash,
            "soil paint undo restores the baseline");

        WorldSurfaceEditResult rock = editor.PaintGround(groundCell, WorldGroundType.Rock);
        Require(rock.Succeeded && grid.TryGetCell(groundCell, out WorldCellData rockCell) &&
                rockCell.GroundType == WorldGroundType.Rock && !rockCell.IsFarmable && rockCell.IsWalkable,
            "rock paint updates non-farmable walkability derivation");
        Require(editor.UndoLast().Succeeded && ComputeCellHash(grid.Cells) == baselineHash,
            "rock paint undo restores the baseline");

        var pathCell = new Vector2Int(5, 5);
        WorldSurfaceEditResult dirtPath = editor.PaintPath(pathCell, WorldPathType.Dirt);
        Require(dirtPath.Succeeded && grid.TryGetCell(pathCell, out WorldCellData path) &&
                path.PathType == WorldPathType.Dirt && path.HasPath &&
                path.IsWalkable && !path.IsFarmable,
            "dirt path is walkable and not farmable");
        WorldChunkMeshData pathMesh = WorldChunkMeshBuilder.Build(
            grid.Definition, grid.Cells, Vector2Int.zero);
        Require(pathMesh.VisualSubmeshIndices[WorldSurfaceMaterialSlots.DirtPath].Length == 6,
            "one dirt path cell occupies exactly one path material quad");
        ulong pathHash = ComputeCellHash(grid.Cells);
        int pathRevision = editor.Revision;
        AssertFailure(editor.FillWaterOneLevel(pathCell),
            WorldSurfaceEditFailure.PathUnderWater, pathHash, pathRevision,
            grid, editor, "water fill on a path");
        Require(editor.UndoLast().Succeeded && ComputeCellHash(grid.Cells) == baselineHash,
            "path paint undo restores the baseline");

        var waterCell = new Vector2Int(1, 0);
        WorldSurfaceEditResult water = editor.FillWater(waterCell, 1);
        WorldCellData pond = default;
        Require(water.Succeeded, "one-level water edit succeeds");
        Require(grid.TryGetCell(waterCell, out pond), "edited water cell remains addressable");
        Require(pond.HasWater && pond.WaterSurfaceLevel == 1 && pond.WaterDepthLevels == 1,
            "one-level water cell satisfies surface/depth invariant");
        Require(!pond.IsWalkable && !pond.IsFarmable && !pond.HasPath,
            "water cell is non-walkable, non-farmable and path-free");
        WorldChunkMeshData waterMesh = WorldChunkMeshBuilder.Build(
            grid.Definition, grid.Cells, Vector2Int.zero);
        Require(waterMesh.WaterSurfaceFaceCount == 1 &&
                waterMesh.ShorelineFaceCount > 0 && waterMesh.WaterIndices.Length >= 12,
            "one water cell generates a top surface and visible shoreline faces");
        Require(waterMesh.VisualSubmeshIndices[WorldSurfaceMaterialSlots.Water].Length ==
                waterMesh.WaterIndices.Length,
            "water top and shoreline use the dedicated water material slot");
        var waterVertexIndices = new HashSet<int>(waterMesh.WaterIndices);
        Require(waterMesh.TopIndices.All(index => !waterVertexIndices.Contains(index)) &&
                waterMesh.CliffIndices.All(index => !waterVertexIndices.Contains(index)),
            "water geometry is excluded from terrain collider triangle streams");

        ulong waterHash = ComputeCellHash(grid.Cells);
        int waterRevision = editor.Revision;
        AssertFailure(editor.PaintPath(waterCell, WorldPathType.Stone),
            WorldSurfaceEditFailure.PathUnderWater, waterHash, waterRevision,
            grid, editor, "path paint under water");
        Require(editor.UndoLast().Succeeded && ComputeCellHash(grid.Cells) == baselineHash,
            "water fill undo removes water and restores the baseline");

        Require(editor.Revision == baselineRevision + 8 && eventCount == 8,
            "four surface edits and four undo operations are the only published revisions");
        Require(!editor.CanUndo, "surface undo is limited to the last successful edit");
    }

    static void AssertFailure(
        WorldSurfaceEditResult result,
        WorldSurfaceEditFailure expected,
        ulong expectedHash,
        int expectedRevision,
        WorldGridService grid,
        WorldSurfaceEditService editor,
        string label)
    {
        Require(!result.Succeeded && result.Failure == expected,
            $"{label} fails with {expected}");
        Require(result.DirtyChunks.Count == 0 &&
                editor.Revision == expectedRevision &&
                ComputeCellHash(grid.Cells) == expectedHash,
            $"{label} is atomic and publishes no dirty chunk");
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
        if (frames < 4) return;
        EditorApplication.update -= ValidateRuntime;

        try
        {
            Scene scene = SceneManager.GetActiveScene();
            WorldGridService grid = FindSingle<WorldGridService>(scene);
            WorldChunkTerrain terrain = FindSingle<WorldChunkTerrain>(scene);
            WorldGridDebugView debug = FindSingle<WorldGridDebugView>(scene);
            Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
                $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
            Require(terrain.IsReady &&
                    terrain.VisualMesh.subMeshCount == WorldSurfaceMaterialSlots.Count &&
                    terrain.GetComponent<MeshRenderer>().sharedMaterials.Length == WorldSurfaceMaterialSlots.Count,
                "runtime terrain exposes eight ground/path/cliff/water material slots");

            int visual = terrain.VisualRevision;
            int collider = terrain.ColliderRevision;
            var groundCell = new Vector2Int(4, 4);
            Require(debug.TrySelectCell(groundCell), "debug selects a ground cell");
            WorldSurfaceEditResult soil = debug.CycleSelectedGround();
            Require(soil.Succeeded && soil.CurrentCell.GroundType == WorldGroundType.Soil,
                "G cycles selected grass to soil");
            Require(terrain.VisualRevision == visual + 1 && terrain.ColliderRevision == collider,
                "ground paint rebuilds visual mesh only");
            Require(terrain.VisualMesh.GetTriangles(WorldSurfaceMaterialSlots.Soil).Length == 6,
                "runtime soil material receives one cell quad");
            Require(debug.UndoLastSurfaceEdit().Succeeded,
                "X restores the last surface edit");

            visual = terrain.VisualRevision;
            collider = terrain.ColliderRevision;
            var pathCell = new Vector2Int(5, 5);
            Require(debug.TrySelectCell(pathCell), "debug selects a path candidate");
            WorldSurfaceEditResult path = debug.CycleSelectedPath();
            Require(path.Succeeded && path.CurrentCell.PathType == WorldPathType.Dirt,
                "T cycles selected empty path to dirt");
            Require(terrain.VisualRevision == visual + 1 && terrain.ColliderRevision == collider &&
                    terrain.VisualMesh.GetTriangles(WorldSurfaceMaterialSlots.DirtPath).Length == 6,
                "path paint updates only its visual material slot");
            Require(debug.UndoLastSurfaceEdit().Succeeded,
                "surface undo restores the path cell");

            visual = terrain.VisualRevision;
            collider = terrain.ColliderRevision;
            var waterCell = new Vector2Int(1, 0);
            Require(debug.TrySelectCell(waterCell), "debug selects a water candidate");
            WorldSurfaceEditResult water = debug.ToggleSelectedWater();
            Require(water.Succeeded && water.CurrentCell.HasWater &&
                    water.CurrentCell.WaterSurfaceLevel == 1,
                "V fills selected low cell with one level of water");
            Require(terrain.VisualRevision == visual + 1 && terrain.ColliderRevision == collider,
                "water fill rebuilds visual shoreline but preserves ground collider");
            Require(terrain.VisualMesh.GetTriangles(WorldSurfaceMaterialSlots.Water).Length >= 12,
                "runtime water material contains surface and shoreline triangles");

            Require(grid.CellToWorld(waterCell, out Vector3 bedWorld),
                "water cell resolves its terrain bed position");
            Physics.SyncTransforms();
            Ray ray = new Ray(bedWorld + Vector3.up * 10f, Vector3.down);
            Require(terrain.TerrainCollider.Raycast(ray, out RaycastHit hit, 20f) &&
                    Mathf.Abs(hit.point.y - bedWorld.y) < 0.01f,
                "water is non-colliding and MeshCollider remains on the terrain bed");
            int visualBeforeBlockedPath = terrain.VisualRevision;
            WorldSurfaceEditResult blockedPath = debug.CycleSelectedPath();
            Require(!blockedPath.Succeeded &&
                    blockedPath.Failure == WorldSurfaceEditFailure.PathUnderWater &&
                    terrain.VisualRevision == visualBeforeBlockedPath,
                "path-under-water input is rejected without rebuild");
            Require(debug.UndoLastSurfaceEdit().Succeeded &&
                    grid.TryGetCell(waterCell, out WorldCellData restored) && !restored.HasWater,
                "surface undo drains the prototype water cell back to baseline");

            int visualAfterUndo = terrain.VisualRevision;
            WorldSurfaceEditResult secondUndo = debug.UndoLastSurfaceEdit();
            Require(!secondUndo.Succeeded &&
                    secondUndo.Failure == WorldSurfaceEditFailure.NoUndoAvailable &&
                    terrain.VisualRevision == visualAfterUndo,
                "second surface undo fails atomically");
            Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                "blocking runtime Console Error/Exception/Assert count is 0");
            Debug.Log("[WORLD-004] PLAY_MODE_PASS");
            EditorApplication.ExitPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static ulong ComputeCellHash(IReadOnlyList<WorldCellData> cells)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        unchecked
        {
            for (int i = 0; i < cells.Count; i++)
            {
                WorldCellData cell = cells[i];
                hash = (hash ^ (uint)cell.Coordinate.x) * prime;
                hash = (hash ^ (uint)cell.Coordinate.y) * prime;
                hash = (hash ^ (uint)cell.ElevationLevel) * prime;
                hash = (hash ^ (uint)cell.GroundType) * prime;
                hash = (hash ^ (uint)cell.PathType) * prime;
                hash = (hash ^ (uint)cell.WaterSurfaceLevel) * prime;
                hash = (hash ^ (uint)cell.WaterDepthLevels) * prime;
                hash = (hash ^ (uint)cell.Occupancy) * prime;
            }
        }
        return hash;
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
        Debug.LogError($"[WORLD-004] FAIL {ex.Message}\n{ex}");
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
        EditorApplication.update -= ValidateRuntime;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        Debug.Log(failed
            ? $"[WORLD-004] FINISHED_WITH_ERRORS consoleErrors={consoleErrors}"
            : "[WORLD-004] FINISHED_PASS ground=4 path=2 water=true shoreline=true walkability=true rollback=true");
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

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[WORLD-004] PASS {message}");
    }
}
#endif
