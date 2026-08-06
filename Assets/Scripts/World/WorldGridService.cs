using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Rendering;

public enum WorldGroundType
{
    Default = 0
}

public enum WorldCellOccupancy
{
    Empty = 0,
    Occupied = 1
}

public enum WorldGridBootstrapProfile
{
    Flat = 0,
    World002Terraces = 1
}

[Serializable]
public sealed class WorldGridDefinition
{
    public float CellSize { get; }
    public int Width { get; }
    public int Height { get; }
    public int ChunkSize { get; }
    public float ElevationStep { get; }
    public int MinElevationLevel { get; }
    public int MaxElevationLevel { get; }
    public Vector3 WorldOrigin { get; }

    public int TotalCellCount => Width * Height;
    public int ChunkCountX => (Width + ChunkSize - 1) / ChunkSize;
    public int ChunkCountZ => (Height + ChunkSize - 1) / ChunkSize;

    public WorldGridDefinition(
        float cellSize,
        int width,
        int height,
        int chunkSize,
        float elevationStep,
        int minElevationLevel,
        int maxElevationLevel,
        Vector3 worldOrigin)
    {
        if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
        if (elevationStep <= 0f) throw new ArgumentOutOfRangeException(nameof(elevationStep));
        if (minElevationLevel > maxElevationLevel)
            throw new ArgumentException("Minimum elevation cannot exceed maximum elevation.");

        CellSize = cellSize;
        Width = width;
        Height = height;
        ChunkSize = chunkSize;
        ElevationStep = elevationStep;
        MinElevationLevel = minElevationLevel;
        MaxElevationLevel = maxElevationLevel;
        WorldOrigin = worldOrigin;
    }
}

[Serializable]
public readonly struct WorldCellData
{
    public Vector2Int Coordinate { get; }
    public int ElevationLevel { get; }
    public WorldGroundType GroundType { get; }
    public bool HasWater { get; }
    public bool HasPath { get; }
    public WorldCellOccupancy Occupancy { get; }

    public WorldCellData(
        Vector2Int coordinate,
        int elevationLevel,
        WorldGroundType groundType,
        bool hasWater,
        bool hasPath,
        WorldCellOccupancy occupancy)
    {
        Coordinate = coordinate;
        ElevationLevel = elevationLevel;
        GroundType = groundType;
        HasWater = hasWater;
        HasPath = hasPath;
        Occupancy = occupancy;
    }
}

[DisallowMultipleComponent]
public sealed class WorldGridService : MonoBehaviour
{
    public const float World001CellSize = 2f;
    public const int World001Width = 16;
    public const int World001Height = 16;
    public const int World001ChunkSize = 16;
    public const float World001ElevationStep = 1f;
    public const int World001MinElevation = 0;
    public const int World001MaxElevation = 6;
    public const int World001InitialElevation = 0;

    [Header("WORLD-001 Read-Only Grid")]
    [SerializeField] Vector3 worldOrigin = Vector3.zero;
    [SerializeField, Min(0.01f)] float cellSize = World001CellSize;
    [SerializeField, Min(1)] int width = World001Width;
    [SerializeField, Min(1)] int height = World001Height;
    [SerializeField, Min(1)] int chunkSize = World001ChunkSize;
    [SerializeField, Min(0.01f)] float elevationStep = World001ElevationStep;
    [SerializeField] int minElevationLevel = World001MinElevation;
    [SerializeField] int maxElevationLevel = World001MaxElevation;
    [SerializeField] int initialElevationLevel = World001InitialElevation;
    [SerializeField] WorldGroundType initialGroundType = WorldGroundType.Default;
    [SerializeField] WorldGridBootstrapProfile bootstrapProfile = WorldGridBootstrapProfile.Flat;

    WorldGridDefinition _definition;
    WorldCellData[] _cells;
    ReadOnlyCollection<WorldCellData> _readOnlyCells;
    bool _initialized;

    public WorldGridDefinition Definition
    {
        get
        {
            EnsureInitialized();
            return _definition;
        }
    }

    public IReadOnlyList<WorldCellData> Cells
    {
        get
        {
            EnsureInitialized();
            return _readOnlyCells;
        }
    }

    public int TotalCellCount
    {
        get
        {
            EnsureInitialized();
            return _cells.Length;
        }
    }

    public int InitialElevationLevel => initialElevationLevel;
    public WorldGroundType InitialGroundType => initialGroundType;
    public WorldGridBootstrapProfile BootstrapProfile => bootstrapProfile;

    void Awake()
    {
        EnsureInitialized();
    }

    void OnEnable()
    {
        EnsureInitialized();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        _initialized = false;
    }

    internal void ConfigureBootstrapProfileForEditor(WorldGridBootstrapProfile profile)
    {
        bootstrapProfile = profile;
        _initialized = false;
    }
#endif

    public bool IsValidCell(Vector2Int coordinate)
    {
        EnsureInitialized();
        return coordinate.x >= 0 && coordinate.x < _definition.Width &&
               coordinate.y >= 0 && coordinate.y < _definition.Height;
    }

    public bool TryGetCell(Vector2Int coordinate, out WorldCellData cell)
    {
        if (TryCellToIndex(coordinate, out int index))
        {
            cell = _cells[index];
            return true;
        }

        cell = default;
        return false;
    }

    public bool WorldToCell(Vector3 worldPosition, out Vector2Int coordinate)
    {
        EnsureInitialized();
        float halfCell = _definition.CellSize * 0.5f;
        int x = Mathf.FloorToInt(
            (worldPosition.x - _definition.WorldOrigin.x + halfCell) / _definition.CellSize);
        int z = Mathf.FloorToInt(
            (worldPosition.z - _definition.WorldOrigin.z + halfCell) / _definition.CellSize);
        coordinate = new Vector2Int(x, z);
        return IsValidCell(coordinate);
    }

    public bool TryWorldToCell(Vector3 worldPosition, out Vector2Int coordinate)
    {
        return WorldToCell(worldPosition, out coordinate);
    }

    public bool CellToWorld(Vector2Int coordinate, out Vector3 worldPosition)
    {
        if (!TryGetCell(coordinate, out WorldCellData cell))
        {
            worldPosition = default;
            return false;
        }

        WorldGridDefinition definition = Definition;
        worldPosition = definition.WorldOrigin + new Vector3(
            coordinate.x * definition.CellSize,
            cell.ElevationLevel * definition.ElevationStep,
            coordinate.y * definition.CellSize);
        return true;
    }

    public bool TryCellToWorld(Vector2Int coordinate, out Vector3 worldPosition)
    {
        return CellToWorld(coordinate, out worldPosition);
    }

    public bool CellToChunk(Vector2Int coordinate, out Vector2Int chunkCoordinate)
    {
        if (!IsValidCell(coordinate))
        {
            chunkCoordinate = default;
            return false;
        }

        chunkCoordinate = new Vector2Int(
            coordinate.x / _definition.ChunkSize,
            coordinate.y / _definition.ChunkSize);
        return true;
    }

    public bool TryCellToChunk(Vector2Int coordinate, out Vector2Int chunkCoordinate)
    {
        return CellToChunk(coordinate, out chunkCoordinate);
    }

    public bool TryCellToIndex(Vector2Int coordinate, out int index)
    {
        EnsureInitialized();
        if (!IsValidCell(coordinate))
        {
            index = -1;
            return false;
        }

        index = coordinate.y * _definition.Width + coordinate.x;
        return true;
    }

    public bool TryIndexToCell(int index, out Vector2Int coordinate)
    {
        EnsureInitialized();
        if (index < 0 || index >= _cells.Length)
        {
            coordinate = default;
            return false;
        }

        coordinate = new Vector2Int(index % _definition.Width, index / _definition.Width);
        return true;
    }

    public IEnumerable<WorldCellData> EnumerateCells()
    {
        EnsureInitialized();
        return _readOnlyCells;
    }

    void EnsureInitialized()
    {
        if (_initialized) return;
        if (cellSize <= 0f || width <= 0 || height <= 0 || chunkSize <= 0 ||
            elevationStep <= 0f || minElevationLevel > maxElevationLevel ||
            initialElevationLevel < minElevationLevel || initialElevationLevel > maxElevationLevel)
        {
            throw new InvalidOperationException("WorldGridService has an invalid serialized definition.");
        }

        _definition = new WorldGridDefinition(
            cellSize,
            width,
            height,
            chunkSize,
            elevationStep,
            minElevationLevel,
            maxElevationLevel,
            worldOrigin);

        _cells = new WorldCellData[_definition.TotalCellCount];
        for (int z = 0; z < _definition.Height; z++)
        {
            for (int x = 0; x < _definition.Width; x++)
            {
                int index = z * _definition.Width + x;
                _cells[index] = new WorldCellData(
                    new Vector2Int(x, z),
                    ResolveBootstrapElevation(x, z),
                    initialGroundType,
                    false,
                    false,
                    WorldCellOccupancy.Empty);
            }
        }

        _readOnlyCells = Array.AsReadOnly(_cells);
        _initialized = true;
    }

    int ResolveBootstrapElevation(int x, int z)
    {
        if (bootstrapProfile != WorldGridBootstrapProfile.World002Terraces)
            return initialElevationLevel;

        int distanceFromEdge = Mathf.Min(x, z, width - 1 - x, height - 1 - z);
        return Mathf.Clamp(distanceFromEdge, minElevationLevel, maxElevationLevel);
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(WorldGridService))]
public class WorldGridDebugViewBase : MonoBehaviour
{
    const string RuntimeRootName = "WorldGridDebugLines_Runtime";

    [Header("Read-Only Runtime Debug")]
    [SerializeField] bool showCoordinateOverlay = true;
    [SerializeField, Range(1, 16)] int coordinateLabelStride = 4;
    [SerializeField, Min(0f)] float lineHeightOffset = 0.03f;
    [SerializeField] Color gridLineColor = new Color(0.34f, 0.78f, 0.72f, 0.78f);
    [SerializeField] Color chunkLineColor = new Color(1f, 0.68f, 0.22f, 1f);
    [SerializeField] Color originLineColor = new Color(1f, 0.32f, 0.30f, 1f);

    WorldGridService _grid;
    Camera _debugCamera;
    GameObject _runtimeRoot;
    Mesh _lineMesh;
    Material[] _lineMaterials;
    GUIStyle _panelStyle;
    GUIStyle _coordinateStyle;

    public bool IsRuntimeGeometryReady =>
        _runtimeRoot != null && _lineMesh != null && _lineMesh.vertexCount > 0;
    public int DebugLineVertexCount => _lineMesh != null ? _lineMesh.vertexCount : 0;
    public bool CoordinateOverlayEnabled => showCoordinateOverlay;

    protected void Awake()
    {
        ResolveReferences();
        BuildRuntimeGrid();
    }

    protected void OnEnable()
    {
        ResolveReferences();
        if (Application.isPlaying) BuildRuntimeGrid();
    }

    protected void LateUpdate()
    {
        if (!IsRuntimeGeometryReady) BuildRuntimeGrid();
    }

    protected void OnDestroy()
    {
        ReleaseRuntimeGrid();
    }

    void ResolveReferences()
    {
        if (_grid == null) _grid = GetComponent<WorldGridService>();
        if (_debugCamera == null) _debugCamera = Camera.main;
    }

    void BuildRuntimeGrid()
    {
        if (!Application.isPlaying || _runtimeRoot != null) return;
        ResolveReferences();
        if (_grid == null) return;

        WorldGridDefinition definition = _grid.Definition;
        var vertices = new List<Vector3>();
        var regularIndices = new List<int>();
        var chunkIndices = new List<int>();
        var originIndices = new List<int>();

        float halfCell = definition.CellSize * 0.5f;
        for (int z = 0; z < definition.Height; z++)
        {
            for (int x = 0; x < definition.Width; x++)
            {
                var coordinate = new Vector2Int(x, z);
                if (!_grid.CellToWorld(coordinate, out Vector3 center)) continue;
                float y = center.y + lineHeightOffset;
                Vector3 southWest = new Vector3(center.x - halfCell, y, center.z - halfCell);
                Vector3 northWest = new Vector3(center.x - halfCell, y, center.z + halfCell);
                Vector3 northEast = new Vector3(center.x + halfCell, y, center.z + halfCell);
                Vector3 southEast = new Vector3(center.x + halfCell, y, center.z - halfCell);

                List<int> westTarget = x % definition.ChunkSize == 0 ? chunkIndices : regularIndices;
                List<int> eastTarget = (x + 1) % definition.ChunkSize == 0 || x == definition.Width - 1
                    ? chunkIndices
                    : regularIndices;
                List<int> southTarget = z % definition.ChunkSize == 0 ? chunkIndices : regularIndices;
                List<int> northTarget = (z + 1) % definition.ChunkSize == 0 || z == definition.Height - 1
                    ? chunkIndices
                    : regularIndices;
                AddLine(vertices, westTarget, southWest, northWest);
                AddLine(vertices, eastTarget, southEast, northEast);
                AddLine(vertices, southTarget, southWest, southEast);
                AddLine(vertices, northTarget, northWest, northEast);
            }
        }

        float originArm = definition.CellSize * 0.42f;
        Vector3 origin = definition.WorldOrigin + Vector3.up * (lineHeightOffset + 0.01f);
        AddLine(vertices, originIndices, origin - Vector3.right * originArm, origin + Vector3.right * originArm);
        AddLine(vertices, originIndices, origin - Vector3.forward * originArm, origin + Vector3.forward * originArm);

        _lineMesh = new Mesh { name = "WorldGridDebugLineMesh" };
        _lineMesh.SetVertices(vertices);
        _lineMesh.subMeshCount = 3;
        _lineMesh.SetIndices(regularIndices, MeshTopology.Lines, 0);
        _lineMesh.SetIndices(chunkIndices, MeshTopology.Lines, 1);
        _lineMesh.SetIndices(originIndices, MeshTopology.Lines, 2);
        _lineMesh.RecalculateBounds();
        _lineMesh.UploadMeshData(false);

        _runtimeRoot = new GameObject(RuntimeRootName)
        {
            hideFlags = HideFlags.DontSave
        };
        _runtimeRoot.transform.SetParent(transform, false);
        var filter = _runtimeRoot.AddComponent<MeshFilter>();
        var renderer = _runtimeRoot.AddComponent<MeshRenderer>();
        filter.sharedMesh = _lineMesh;

        _lineMaterials = new[]
        {
            CreateLineMaterial("WorldGridLines", gridLineColor),
            CreateLineMaterial("WorldChunkBoundary", chunkLineColor),
            CreateLineMaterial("WorldGridOrigin", originLineColor)
        };
        renderer.sharedMaterials = _lineMaterials;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    protected void OnGUI()
    {
        if (!showCoordinateOverlay || _grid == null) return;
        ResolveStyles();
        ResolveReferences();

        WorldGridDefinition definition = _grid.Definition;
        string pointerText = "Pointer: outside grid";
        if (_debugCamera != null)
        {
            Vector2 guiMouse = Event.current.mousePosition;
            var screenMouse = new Vector3(guiMouse.x, Screen.height - guiMouse.y, 0f);
            Ray ray = _debugCamera.ScreenPointToRay(screenMouse);
            Vector3 pointerWorld = default;
            bool hasPointerWorld = false;
            WorldChunkTerrain terrain = GetComponent<WorldChunkTerrain>();
            if (terrain != null && terrain.TerrainCollider != null &&
                terrain.TerrainCollider.Raycast(ray, out RaycastHit terrainHit, 500f))
            {
                pointerWorld = terrainHit.point;
                hasPointerWorld = true;
            }
            else
            {
                var plane = new Plane(Vector3.up, definition.WorldOrigin);
                if (plane.Raycast(ray, out float distance))
                {
                    pointerWorld = ray.GetPoint(distance);
                    hasPointerWorld = true;
                }
            }

            if (hasPointerWorld &&
                _grid.WorldToCell(pointerWorld, out Vector2Int coordinate) &&
                _grid.TryGetCell(coordinate, out WorldCellData cell) &&
                _grid.CellToChunk(coordinate, out Vector2Int chunk))
            {
                pointerText = $"Pointer cell ({coordinate.x},{coordinate.y})  elevation {cell.ElevationLevel}  chunk ({chunk.x},{chunk.y})";
            }
        }

        GUILayout.BeginArea(new Rect(18f, 18f, 430f, 116f), _panelStyle);
        GUILayout.Label(_grid.BootstrapProfile == WorldGridBootstrapProfile.World002Terraces
            ? "WORLD-002  STEPPED CHUNK TERRAIN"
            : "WORLD-001  READ-ONLY WORLD CELL GRID");
        GUILayout.Label($"{definition.Width}×{definition.Height} cells = {_grid.TotalCellCount}  |  cell {definition.CellSize:0.##}m  |  chunk {definition.ChunkSize}×{definition.ChunkSize}");
        GUILayout.Label($"Origin = cell (0,0) center {definition.WorldOrigin}  |  elevation {definition.MinElevationLevel}..{definition.MaxElevationLevel}");
        GUILayout.Label(pointerText);
        GUILayout.EndArea();

        DrawCoordinateLabels(definition);
    }

    void DrawCoordinateLabels(WorldGridDefinition definition)
    {
        if (_debugCamera == null || Event.current.type != EventType.Repaint) return;

        int stride = Mathf.Max(1, coordinateLabelStride);
        for (int z = 0; z < definition.Height; z++)
        {
            if (z % stride != 0 && z != definition.Height - 1) continue;
            for (int x = 0; x < definition.Width; x++)
            {
                if (x % stride != 0 && x != definition.Width - 1) continue;
                var coordinate = new Vector2Int(x, z);
                if (!_grid.CellToWorld(coordinate, out Vector3 worldPosition)) continue;
                Vector3 screen = _debugCamera.WorldToScreenPoint(
                    worldPosition + Vector3.up * (lineHeightOffset + 0.02f));
                if (screen.z <= 0f) continue;
                var rect = new Rect(screen.x - 26f, Screen.height - screen.y - 10f, 52f, 20f);
                GUI.Label(rect, $"{x},{z}", _coordinateStyle);
            }
        }
    }

    void ResolveStyles()
    {
        if (_panelStyle == null)
        {
            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(12, 12, 9, 9),
                fontSize = 13,
                normal = { textColor = Color.white }
            };
        }

        if (_coordinateStyle == null)
        {
            _coordinateStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.92f, 1f, 0.98f, 0.95f) }
            };
        }
    }

    static void AddLine(List<Vector3> vertices, List<int> indices, Vector3 start, Vector3 end)
    {
        int first = vertices.Count;
        vertices.Add(start);
        vertices.Add(end);
        indices.Add(first);
        indices.Add(first + 1);
    }

    static Material CreateLineMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                        Shader.Find("Sprites/Default") ??
                        Shader.Find("Hidden/Internal-Colored");
        if (shader == null) throw new InvalidOperationException("No debug line shader is available.");

        var material = new Material(shader)
        {
            name = materialName,
            color = color,
            hideFlags = HideFlags.DontSave
        };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        return material;
    }

    void ReleaseRuntimeGrid()
    {
        if (_runtimeRoot != null) DestroyRuntimeObject(_runtimeRoot);
        if (_lineMesh != null) DestroyRuntimeObject(_lineMesh);
        if (_lineMaterials != null)
        {
            foreach (Material material in _lineMaterials)
                if (material != null) DestroyRuntimeObject(material);
        }

        _runtimeRoot = null;
        _lineMesh = null;
        _lineMaterials = null;
    }

    static void DestroyRuntimeObject(UnityEngine.Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(target);
        else UnityEngine.Object.DestroyImmediate(target);
    }
}
