using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Rendering;

public enum WorldGroundType
{
    Default = 0,
    Grass = 0,
    Soil = 1,
    Sand = 2,
    Rock = 3
}

public enum WorldPathType
{
    None = 0,
    Dirt = 1,
    Stone = 2
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

public enum WorldTerraformFailure
{
    None = 0,
    NoSelection = 1,
    OutOfBounds = 2,
    ProtectedCell = 3,
    MinimumElevation = 4,
    MaximumElevation = 5,
    NoUndoAvailable = 6,
    StaleUndoRecord = 7
}

public readonly struct WorldTerraformEditResult
{
    public bool Succeeded { get; }
    public WorldTerraformFailure Failure { get; }
    public Vector2Int Coordinate { get; }
    public int PreviousElevationLevel { get; }
    public int CurrentElevationLevel { get; }
    public int Revision { get; }
    public IReadOnlyList<Vector2Int> DirtyChunks { get; }

    internal WorldTerraformEditResult(
        bool succeeded,
        WorldTerraformFailure failure,
        Vector2Int coordinate,
        int previousElevationLevel,
        int currentElevationLevel,
        int revision,
        IReadOnlyList<Vector2Int> dirtyChunks)
    {
        Succeeded = succeeded;
        Failure = failure;
        Coordinate = coordinate;
        PreviousElevationLevel = previousElevationLevel;
        CurrentElevationLevel = currentElevationLevel;
        Revision = revision;
        DirtyChunks = dirtyChunks ?? Array.Empty<Vector2Int>();
    }
}

public static class WorldTerraformDirtyChunkResolver
{
    public static IReadOnlyList<Vector2Int> Resolve(
        WorldGridDefinition definition,
        Vector2Int coordinate)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        if (coordinate.x < 0 || coordinate.x >= definition.Width ||
            coordinate.y < 0 || coordinate.y >= definition.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate));
        }

        var dirty = new List<Vector2Int>(3);
        var primary = new Vector2Int(
            coordinate.x / definition.ChunkSize,
            coordinate.y / definition.ChunkSize);
        AddUnique(dirty, primary);

        int localX = coordinate.x % definition.ChunkSize;
        int localZ = coordinate.y % definition.ChunkSize;
        if (localX == 0 && primary.x > 0)
            AddUnique(dirty, primary + Vector2Int.left);
        if (localX == definition.ChunkSize - 1 && primary.x + 1 < definition.ChunkCountX)
            AddUnique(dirty, primary + Vector2Int.right);
        if (localZ == 0 && primary.y > 0)
            AddUnique(dirty, primary + Vector2Int.down);
        if (localZ == definition.ChunkSize - 1 && primary.y + 1 < definition.ChunkCountZ)
            AddUnique(dirty, primary + Vector2Int.up);
        return dirty.AsReadOnly();
    }

    static void AddUnique(List<Vector2Int> chunks, Vector2Int coordinate)
    {
        if (!chunks.Contains(coordinate)) chunks.Add(coordinate);
    }
}

public enum WorldSurfaceEditKind
{
    None = 0,
    PaintGround = 1,
    PaintPath = 2,
    ClearPath = 3,
    FillWater = 4,
    DrainWater = 5,
    Undo = 6
}

public enum WorldSurfaceEditFailure
{
    None = 0,
    OutOfBounds = 1,
    ProtectedCell = 2,
    Unchanged = 3,
    PathUnderWater = 4,
    InvalidWaterLevel = 5,
    WaterAlreadyPresent = 6,
    WaterNotPresent = 7,
    NoUndoAvailable = 8,
    StaleUndoRecord = 9
}

public readonly struct WorldSurfaceEditResult
{
    public bool Succeeded { get; }
    public WorldSurfaceEditKind Kind { get; }
    public WorldSurfaceEditFailure Failure { get; }
    public Vector2Int Coordinate { get; }
    public WorldCellData PreviousCell { get; }
    public WorldCellData CurrentCell { get; }
    public int Revision { get; }
    public IReadOnlyList<Vector2Int> DirtyChunks { get; }

    internal WorldSurfaceEditResult(
        bool succeeded,
        WorldSurfaceEditKind kind,
        WorldSurfaceEditFailure failure,
        Vector2Int coordinate,
        WorldCellData previousCell,
        WorldCellData currentCell,
        int revision,
        IReadOnlyList<Vector2Int> dirtyChunks)
    {
        Succeeded = succeeded;
        Kind = kind;
        Failure = failure;
        Coordinate = coordinate;
        PreviousCell = previousCell;
        CurrentCell = currentCell;
        Revision = revision;
        DirtyChunks = dirtyChunks ?? Array.Empty<Vector2Int>();
    }
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
    public WorldPathType PathType { get; }
    public int WaterSurfaceLevel { get; }
    public int WaterDepthLevels { get; }
    public WorldCellOccupancy Occupancy { get; }
    public bool HasWater => WaterDepthLevels > 0;
    public bool HasPath => PathType != WorldPathType.None;
    public bool IsFarmable => !HasWater && !HasPath &&
                              (GroundType == WorldGroundType.Default ||
                               GroundType == WorldGroundType.Soil);
    public bool IsWalkable => !HasWater;

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
        PathType = hasPath ? WorldPathType.Dirt : WorldPathType.None;
        WaterSurfaceLevel = hasWater ? elevationLevel + 1 : elevationLevel;
        WaterDepthLevels = hasWater ? 1 : 0;
        Occupancy = occupancy;
    }

    public WorldCellData(
        Vector2Int coordinate,
        int elevationLevel,
        WorldGroundType groundType,
        WorldPathType pathType,
        int waterSurfaceLevel,
        int waterDepthLevels,
        WorldCellOccupancy occupancy)
    {
        Coordinate = coordinate;
        ElevationLevel = elevationLevel;
        GroundType = groundType;
        PathType = pathType;
        WaterSurfaceLevel = waterSurfaceLevel;
        WaterDepthLevels = waterDepthLevels;
        Occupancy = occupancy;
    }

    internal WorldCellData WithElevationLevel(int elevationLevel)
    {
        return new WorldCellData(
            Coordinate,
            elevationLevel,
            GroundType,
            PathType,
            WaterSurfaceLevel,
            WaterDepthLevels,
            Occupancy);
    }

    internal WorldCellData WithGroundType(WorldGroundType groundType)
    {
        return new WorldCellData(
            Coordinate, ElevationLevel, groundType, PathType,
            WaterSurfaceLevel, WaterDepthLevels, Occupancy);
    }

    internal WorldCellData WithPathType(WorldPathType pathType)
    {
        return new WorldCellData(
            Coordinate, ElevationLevel, GroundType, pathType,
            WaterSurfaceLevel, WaterDepthLevels, Occupancy);
    }

    internal WorldCellData WithWater(int surfaceLevel, int depthLevels)
    {
        return new WorldCellData(
            Coordinate, ElevationLevel, GroundType, PathType,
            surfaceLevel, depthLevels, Occupancy);
    }

    internal bool ContentEquals(WorldCellData other)
    {
        return Coordinate == other.Coordinate &&
               ElevationLevel == other.ElevationLevel &&
               GroundType == other.GroundType &&
               PathType == other.PathType &&
               WaterSurfaceLevel == other.WaterSurfaceLevel &&
               WaterDepthLevels == other.WaterDepthLevels &&
               Occupancy == other.Occupancy;
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
    [SerializeField] Vector2Int protectedTerraformCell = Vector2Int.zero;

    WorldGridDefinition _definition;
    WorldCellData[] _cells;
    ReadOnlyCollection<WorldCellData> _readOnlyCells;
    WorldTerraformService _terraform;
    WorldSurfaceEditService _surfaceEditor;
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
    public Vector2Int ProtectedTerraformCell => protectedTerraformCell;
    public WorldTerraformService Terraform
    {
        get
        {
            EnsureInitialized();
            return _terraform ??= new WorldTerraformService(this);
        }
    }
    public WorldSurfaceEditService SurfaceEditor
    {
        get
        {
            EnsureInitialized();
            return _surfaceEditor ??= new WorldSurfaceEditService(this);
        }
    }

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

    public bool IsTerraformProtected(Vector2Int coordinate)
    {
        return coordinate == protectedTerraformCell;
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

    internal bool TryCommitTerraformElevation(
        Vector2Int coordinate,
        int expectedElevationLevel,
        int newElevationLevel)
    {
        EnsureInitialized();
        if (!TryCellToIndex(coordinate, out int index)) return false;
        if (newElevationLevel < _definition.MinElevationLevel ||
            newElevationLevel > _definition.MaxElevationLevel)
        {
            return false;
        }

        WorldCellData current = _cells[index];
        if (current.ElevationLevel != expectedElevationLevel) return false;
        _cells[index] = current.WithElevationLevel(newElevationLevel);
        return true;
    }

    internal bool TryCommitSurfaceCell(WorldCellData expected, WorldCellData replacement)
    {
        EnsureInitialized();
        if (expected.Coordinate != replacement.Coordinate ||
            !TryCellToIndex(expected.Coordinate, out int index) ||
            !_cells[index].ContentEquals(expected))
        {
            return false;
        }

        if (replacement.ElevationLevel != expected.ElevationLevel ||
            replacement.Occupancy != expected.Occupancy ||
            replacement.WaterDepthLevels < 0 ||
            (replacement.HasWater &&
             (replacement.WaterSurfaceLevel <= replacement.ElevationLevel ||
              replacement.WaterSurfaceLevel > _definition.MaxElevationLevel)) ||
            (!replacement.HasWater &&
             replacement.WaterSurfaceLevel != replacement.ElevationLevel))
        {
            return false;
        }

        _cells[index] = replacement;
        return true;
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
        _terraform = null;
        _surfaceEditor = null;
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

public sealed class WorldTerraformService
{
    readonly WorldGridService _grid;
    WorldTerraformEditResult _lastCommittedEdit;
    bool _hasUndo;
    int _revision;

    public event Action<WorldTerraformEditResult> Changed;

    public int Revision => _revision;
    public bool CanUndo => _hasUndo;

    internal WorldTerraformService(WorldGridService grid)
    {
        _grid = grid ?? throw new ArgumentNullException(nameof(grid));
    }

    public WorldTerraformEditResult Raise(Vector2Int coordinate)
    {
        return ApplyDelta(coordinate, 1);
    }

    public WorldTerraformEditResult Lower(Vector2Int coordinate)
    {
        return ApplyDelta(coordinate, -1);
    }

    public WorldTerraformEditResult UndoLast()
    {
        if (!_hasUndo)
            return Failed(WorldTerraformFailure.NoUndoAvailable, default, 0);

        Vector2Int coordinate = _lastCommittedEdit.Coordinate;
        if (!_grid.TryGetCell(coordinate, out WorldCellData current) ||
            current.ElevationLevel != _lastCommittedEdit.CurrentElevationLevel)
        {
            return Failed(
                WorldTerraformFailure.StaleUndoRecord,
                coordinate,
                current.ElevationLevel);
        }

        int restoredLevel = _lastCommittedEdit.PreviousElevationLevel;
        if (!_grid.TryCommitTerraformElevation(
                coordinate,
                current.ElevationLevel,
                restoredLevel))
        {
            return Failed(
                WorldTerraformFailure.StaleUndoRecord,
                coordinate,
                current.ElevationLevel);
        }

        _revision++;
        _hasUndo = false;
        var result = Succeeded(
            coordinate,
            current.ElevationLevel,
            restoredLevel,
            WorldTerraformDirtyChunkResolver.Resolve(_grid.Definition, coordinate));
        Changed?.Invoke(result);
        return result;
    }

    WorldTerraformEditResult ApplyDelta(Vector2Int coordinate, int delta)
    {
        if (!_grid.TryGetCell(coordinate, out WorldCellData current))
            return Failed(WorldTerraformFailure.OutOfBounds, coordinate, 0);
        if (_grid.IsTerraformProtected(coordinate))
            return Failed(WorldTerraformFailure.ProtectedCell, coordinate, current.ElevationLevel);

        int targetLevel = current.ElevationLevel + delta;
        if (targetLevel < _grid.Definition.MinElevationLevel)
            return Failed(WorldTerraformFailure.MinimumElevation, coordinate, current.ElevationLevel);
        if (targetLevel > _grid.Definition.MaxElevationLevel)
            return Failed(WorldTerraformFailure.MaximumElevation, coordinate, current.ElevationLevel);

        IReadOnlyList<Vector2Int> dirtyChunks =
            WorldTerraformDirtyChunkResolver.Resolve(_grid.Definition, coordinate);
        if (!_grid.TryCommitTerraformElevation(
                coordinate,
                current.ElevationLevel,
                targetLevel))
        {
            return Failed(WorldTerraformFailure.StaleUndoRecord, coordinate, current.ElevationLevel);
        }

        _revision++;
        var result = Succeeded(
            coordinate,
            current.ElevationLevel,
            targetLevel,
            dirtyChunks);
        _lastCommittedEdit = result;
        _hasUndo = true;
        Changed?.Invoke(result);
        return result;
    }

    WorldTerraformEditResult Succeeded(
        Vector2Int coordinate,
        int previousLevel,
        int currentLevel,
        IReadOnlyList<Vector2Int> dirtyChunks)
    {
        return new WorldTerraformEditResult(
            true,
            WorldTerraformFailure.None,
            coordinate,
            previousLevel,
            currentLevel,
            _revision,
            dirtyChunks);
    }

    WorldTerraformEditResult Failed(
        WorldTerraformFailure failure,
        Vector2Int coordinate,
        int currentLevel)
    {
        return new WorldTerraformEditResult(
            false,
            failure,
            coordinate,
            currentLevel,
            currentLevel,
            _revision,
            Array.Empty<Vector2Int>());
    }
}

public sealed class WorldSurfaceEditService
{
    readonly WorldGridService _grid;
    WorldSurfaceEditResult _lastCommittedEdit;
    bool _hasUndo;
    int _revision;

    public event Action<WorldSurfaceEditResult> Changed;

    public int Revision => _revision;
    public bool CanUndo => _hasUndo;

    internal WorldSurfaceEditService(WorldGridService grid)
    {
        _grid = grid ?? throw new ArgumentNullException(nameof(grid));
    }

    public WorldSurfaceEditResult PaintGround(
        Vector2Int coordinate,
        WorldGroundType groundType)
    {
        if (!TryPreflight(coordinate, out WorldCellData current, out WorldSurfaceEditResult failed))
            return failed;
        if (current.GroundType == groundType)
            return Failed(WorldSurfaceEditKind.PaintGround, WorldSurfaceEditFailure.Unchanged, current);
        return Commit(WorldSurfaceEditKind.PaintGround, current, current.WithGroundType(groundType));
    }

    public WorldSurfaceEditResult PaintPath(
        Vector2Int coordinate,
        WorldPathType pathType)
    {
        if (pathType == WorldPathType.None) return ClearPath(coordinate);
        if (!TryPreflight(coordinate, out WorldCellData current, out WorldSurfaceEditResult failed))
            return failed;
        if (current.HasWater)
            return Failed(WorldSurfaceEditKind.PaintPath, WorldSurfaceEditFailure.PathUnderWater, current);
        if (current.PathType == pathType)
            return Failed(WorldSurfaceEditKind.PaintPath, WorldSurfaceEditFailure.Unchanged, current);
        return Commit(WorldSurfaceEditKind.PaintPath, current, current.WithPathType(pathType));
    }

    public WorldSurfaceEditResult ClearPath(Vector2Int coordinate)
    {
        if (!TryPreflight(coordinate, out WorldCellData current, out WorldSurfaceEditResult failed))
            return failed;
        if (!current.HasPath)
            return Failed(WorldSurfaceEditKind.ClearPath, WorldSurfaceEditFailure.Unchanged, current);
        return Commit(WorldSurfaceEditKind.ClearPath, current, current.WithPathType(WorldPathType.None));
    }

    public WorldSurfaceEditResult FillWater(Vector2Int coordinate, int waterSurfaceLevel)
    {
        if (!TryPreflight(coordinate, out WorldCellData current, out WorldSurfaceEditResult failed))
            return failed;
        if (current.HasPath)
            return Failed(WorldSurfaceEditKind.FillWater, WorldSurfaceEditFailure.PathUnderWater, current);
        if (current.HasWater)
            return Failed(WorldSurfaceEditKind.FillWater, WorldSurfaceEditFailure.WaterAlreadyPresent, current);
        if (waterSurfaceLevel <= current.ElevationLevel ||
            waterSurfaceLevel > _grid.Definition.MaxElevationLevel)
        {
            return Failed(WorldSurfaceEditKind.FillWater, WorldSurfaceEditFailure.InvalidWaterLevel, current);
        }

        return Commit(
            WorldSurfaceEditKind.FillWater,
            current,
            current.WithWater(waterSurfaceLevel, waterSurfaceLevel - current.ElevationLevel));
    }

    public WorldSurfaceEditResult FillWaterOneLevel(Vector2Int coordinate)
    {
        if (!_grid.TryGetCell(coordinate, out WorldCellData current))
            return Failed(
                WorldSurfaceEditKind.FillWater,
                WorldSurfaceEditFailure.OutOfBounds,
                default,
                coordinate);
        return FillWater(coordinate, current.ElevationLevel + 1);
    }

    public WorldSurfaceEditResult DrainWater(Vector2Int coordinate)
    {
        if (!TryPreflight(coordinate, out WorldCellData current, out WorldSurfaceEditResult failed))
            return failed;
        if (!current.HasWater)
            return Failed(WorldSurfaceEditKind.DrainWater, WorldSurfaceEditFailure.WaterNotPresent, current);
        return Commit(
            WorldSurfaceEditKind.DrainWater,
            current,
            current.WithWater(current.ElevationLevel, 0));
    }

    public WorldSurfaceEditResult UndoLast()
    {
        if (!_hasUndo)
            return Failed(
                WorldSurfaceEditKind.Undo,
                WorldSurfaceEditFailure.NoUndoAvailable,
                default,
                default);

        Vector2Int coordinate = _lastCommittedEdit.Coordinate;
        if (!_grid.TryGetCell(coordinate, out WorldCellData current) ||
            !current.ContentEquals(_lastCommittedEdit.CurrentCell) ||
            !_grid.TryCommitSurfaceCell(current, _lastCommittedEdit.PreviousCell))
        {
            return Failed(
                WorldSurfaceEditKind.Undo,
                WorldSurfaceEditFailure.StaleUndoRecord,
                current,
                coordinate);
        }

        _revision++;
        _hasUndo = false;
        var result = Succeeded(
            WorldSurfaceEditKind.Undo,
            current,
            _lastCommittedEdit.PreviousCell);
        Changed?.Invoke(result);
        return result;
    }

    bool TryPreflight(
        Vector2Int coordinate,
        out WorldCellData current,
        out WorldSurfaceEditResult failed)
    {
        if (!_grid.TryGetCell(coordinate, out current))
        {
            failed = Failed(
                WorldSurfaceEditKind.None,
                WorldSurfaceEditFailure.OutOfBounds,
                default,
                coordinate);
            return false;
        }
        if (_grid.IsTerraformProtected(coordinate))
        {
            failed = Failed(
                WorldSurfaceEditKind.None,
                WorldSurfaceEditFailure.ProtectedCell,
                current);
            return false;
        }

        failed = default;
        return true;
    }

    WorldSurfaceEditResult Commit(
        WorldSurfaceEditKind kind,
        WorldCellData previous,
        WorldCellData replacement)
    {
        if (!_grid.TryCommitSurfaceCell(previous, replacement))
            return Failed(kind, WorldSurfaceEditFailure.StaleUndoRecord, previous);

        _revision++;
        var result = Succeeded(kind, previous, replacement);
        _lastCommittedEdit = result;
        _hasUndo = true;
        Changed?.Invoke(result);
        return result;
    }

    WorldSurfaceEditResult Succeeded(
        WorldSurfaceEditKind kind,
        WorldCellData previous,
        WorldCellData current)
    {
        return new WorldSurfaceEditResult(
            true,
            kind,
            WorldSurfaceEditFailure.None,
            current.Coordinate,
            previous,
            current,
            _revision,
            WorldTerraformDirtyChunkResolver.Resolve(_grid.Definition, current.Coordinate));
    }

    WorldSurfaceEditResult Failed(
        WorldSurfaceEditKind kind,
        WorldSurfaceEditFailure failure,
        WorldCellData current,
        Vector2Int? coordinateOverride = null)
    {
        Vector2Int coordinate = coordinateOverride ?? current.Coordinate;
        return new WorldSurfaceEditResult(
            false,
            kind,
            failure,
            coordinate,
            current,
            current,
            _revision,
            Array.Empty<Vector2Int>());
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
    WorldChunkTerrain _terrain;
    Camera _debugCamera;
    GameObject _runtimeRoot;
    Mesh _lineMesh;
    Material[] _lineMaterials;
    GUIStyle _panelStyle;
    GUIStyle _coordinateStyle;
    Vector2Int _hoveredCell;
    Vector2Int _selectedCell;
    bool _hasHoveredCell;
    bool _hasSelectedCell;
    WorldTerraformEditResult _lastTerraformResult;
    WorldSurfaceEditResult _lastSurfaceResult;
    bool _lastEditWasSurface;

    public bool IsRuntimeGeometryReady =>
        _runtimeRoot != null && _lineMesh != null && _lineMesh.vertexCount > 0;
    public int DebugLineVertexCount => _lineMesh != null ? _lineMesh.vertexCount : 0;
    public bool CoordinateOverlayEnabled => showCoordinateOverlay;
    public bool HasSelectedCell => _hasSelectedCell;
    public Vector2Int SelectedCell => _selectedCell;
    public WorldTerraformEditResult LastTerraformResult => _lastTerraformResult;
    public WorldSurfaceEditResult LastSurfaceEditResult => _lastSurfaceResult;

    protected void Awake()
    {
        ResolveReferences();
        SubscribeTerraform();
        BuildRuntimeGrid();
    }

    protected void OnEnable()
    {
        ResolveReferences();
        SubscribeTerraform();
        if (Application.isPlaying) BuildRuntimeGrid();
    }

    protected void OnDisable()
    {
        UnsubscribeTerraform();
    }

    protected void LateUpdate()
    {
        if (!IsRuntimeGeometryReady) BuildRuntimeGrid();
        if (!Application.isPlaying) return;
        UpdateHoveredCell();
        ProcessTerraformInput();
    }

    protected void OnDestroy()
    {
        UnsubscribeTerraform();
        ReleaseRuntimeGrid();
    }

    void ResolveReferences()
    {
        if (_grid == null) _grid = GetComponent<WorldGridService>();
        if (_terrain == null) _terrain = GetComponent<WorldChunkTerrain>();
        if (_debugCamera == null) _debugCamera = Camera.main;
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
        _lastTerraformResult = result;
        _lastEditWasSurface = false;
        ReleaseRuntimeGrid();
    }

    void OnSurfaceChanged(WorldSurfaceEditResult result)
    {
        _lastSurfaceResult = result;
        _lastEditWasSurface = true;
    }

    void UpdateHoveredCell()
    {
        ResolveReferences();
        _hasHoveredCell = false;
        if (_debugCamera == null) return;

        Ray ray = _debugCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 pointerWorld;
        if (_terrain != null && _terrain.TerrainCollider != null &&
            _terrain.TerrainCollider.Raycast(ray, out RaycastHit terrainHit, 500f))
        {
            pointerWorld = terrainHit.point;
        }
        else
        {
            var plane = new Plane(Vector3.up, _grid.Definition.WorldOrigin);
            if (!plane.Raycast(ray, out float distance)) return;
            pointerWorld = ray.GetPoint(distance);
        }

        _hasHoveredCell = _grid.WorldToCell(pointerWorld, out _hoveredCell);
    }

    void ProcessTerraformInput()
    {
        if (Input.GetMouseButtonDown(0) && _hasHoveredCell)
            TrySelectCell(_hoveredCell);
        if (Input.GetKeyDown(KeyCode.R)) RaiseSelected();
        if (Input.GetKeyDown(KeyCode.F)) LowerSelected();
        if (Input.GetKeyDown(KeyCode.Z)) UndoLastTerraform();
        if (Input.GetKeyDown(KeyCode.G)) CycleSelectedGround();
        if (Input.GetKeyDown(KeyCode.T)) CycleSelectedPath();
        if (Input.GetKeyDown(KeyCode.V)) ToggleSelectedWater();
        if (Input.GetKeyDown(KeyCode.X)) UndoLastSurfaceEdit();
    }

    public bool TrySelectCell(Vector2Int coordinate)
    {
        if (_grid == null || !_grid.IsValidCell(coordinate)) return false;
        _selectedCell = coordinate;
        _hasSelectedCell = true;
        return true;
    }

    public WorldTerraformEditResult RaiseSelected()
    {
        if (!_hasSelectedCell)
            return StoreNoSelectionResult();
        _lastEditWasSurface = false;
        _lastTerraformResult = _grid.Terraform.Raise(_selectedCell);
        return _lastTerraformResult;
    }

    public WorldTerraformEditResult LowerSelected()
    {
        if (!_hasSelectedCell)
            return StoreNoSelectionResult();
        _lastEditWasSurface = false;
        _lastTerraformResult = _grid.Terraform.Lower(_selectedCell);
        return _lastTerraformResult;
    }

    public WorldTerraformEditResult UndoLastTerraform()
    {
        _lastEditWasSurface = false;
        _lastTerraformResult = _grid.Terraform.UndoLast();
        return _lastTerraformResult;
    }

    public WorldSurfaceEditResult CycleSelectedGround()
    {
        if (!_hasSelectedCell || !_grid.TryGetCell(_selectedCell, out WorldCellData current))
            return StoreNoSurfaceSelectionResult();
        var next = (WorldGroundType)(((int)current.GroundType + 1) % 4);
        _lastEditWasSurface = true;
        _lastSurfaceResult = _grid.SurfaceEditor.PaintGround(_selectedCell, next);
        return _lastSurfaceResult;
    }

    public WorldSurfaceEditResult CycleSelectedPath()
    {
        if (!_hasSelectedCell || !_grid.TryGetCell(_selectedCell, out WorldCellData current))
            return StoreNoSurfaceSelectionResult();
        WorldPathType next = current.PathType switch
        {
            WorldPathType.None => WorldPathType.Dirt,
            WorldPathType.Dirt => WorldPathType.Stone,
            _ => WorldPathType.None
        };
        _lastEditWasSurface = true;
        _lastSurfaceResult = next == WorldPathType.None
            ? _grid.SurfaceEditor.ClearPath(_selectedCell)
            : _grid.SurfaceEditor.PaintPath(_selectedCell, next);
        return _lastSurfaceResult;
    }

    public WorldSurfaceEditResult ToggleSelectedWater()
    {
        if (!_hasSelectedCell || !_grid.TryGetCell(_selectedCell, out WorldCellData current))
            return StoreNoSurfaceSelectionResult();
        _lastEditWasSurface = true;
        _lastSurfaceResult = current.HasWater
            ? _grid.SurfaceEditor.DrainWater(_selectedCell)
            : _grid.SurfaceEditor.FillWaterOneLevel(_selectedCell);
        return _lastSurfaceResult;
    }

    public WorldSurfaceEditResult UndoLastSurfaceEdit()
    {
        _lastEditWasSurface = true;
        _lastSurfaceResult = _grid.SurfaceEditor.UndoLast();
        return _lastSurfaceResult;
    }

    WorldTerraformEditResult StoreNoSelectionResult()
    {
        _lastTerraformResult = new WorldTerraformEditResult(
            false,
            WorldTerraformFailure.NoSelection,
            default,
            0,
            0,
            _grid != null ? _grid.Terraform.Revision : 0,
            Array.Empty<Vector2Int>());
        return _lastTerraformResult;
    }

    WorldSurfaceEditResult StoreNoSurfaceSelectionResult()
    {
        _lastEditWasSurface = true;
        _lastSurfaceResult = new WorldSurfaceEditResult(
            false,
            WorldSurfaceEditKind.None,
            WorldSurfaceEditFailure.OutOfBounds,
            default,
            default,
            default,
            _grid != null ? _grid.SurfaceEditor.Revision : 0,
            Array.Empty<Vector2Int>());
        return _lastSurfaceResult;
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
        if (_hasHoveredCell &&
            _grid.TryGetCell(_hoveredCell, out WorldCellData hovered) &&
            _grid.CellToChunk(_hoveredCell, out Vector2Int hoveredChunk))
        {
            pointerText = $"Pointer ({_hoveredCell.x},{_hoveredCell.y})  level {hovered.ElevationLevel}  chunk ({hoveredChunk.x},{hoveredChunk.y})";
        }

        string selectionText = "Selected: none — left click a cell";
        if (_hasSelectedCell && _grid.TryGetCell(_selectedCell, out WorldCellData selected))
        {
            string water = selected.HasWater
                ? $"water L{selected.WaterSurfaceLevel}/D{selected.WaterDepthLevels}"
                : "dry";
            selectionText = $"Selected ({_selectedCell.x},{_selectedCell.y})  level {selected.ElevationLevel}  {selected.GroundType}/{selected.PathType}/{water}  farm {selected.IsFarmable} walk {selected.IsWalkable}" +
                            (_grid.IsTerraformProtected(_selectedCell) ? "  PROTECTED" : string.Empty);
        }

        string resultText;
        if (_lastEditWasSurface)
        {
            resultText = _lastSurfaceResult.Succeeded
                ? $"Surface {_lastSurfaceResult.Kind} applied  revision {_lastSurfaceResult.Revision}"
                : _lastSurfaceResult.Failure != WorldSurfaceEditFailure.None
                    ? $"Surface blocked: {_lastSurfaceResult.Failure}"
                    : "G ground  |  T path  |  V water  |  X surface undo";
        }
        else
        {
            resultText = _lastTerraformResult.Succeeded
                ? $"Height {_lastTerraformResult.PreviousElevationLevel}→{_lastTerraformResult.CurrentElevationLevel}  revision {_lastTerraformResult.Revision}"
                : _lastTerraformResult.Failure != WorldTerraformFailure.None
                    ? $"Height blocked: {_lastTerraformResult.Failure}"
                    : "R raise  |  F lower  |  Z height undo";
        }

        GUILayout.BeginArea(new Rect(18f, 18f, 690f, 184f), _panelStyle);
        GUILayout.Label("WORLD-004  GROUND / PATH / WATER CELL PROTOTYPE");
        GUILayout.Label($"{definition.Width}×{definition.Height} cells = {_grid.TotalCellCount}  |  cell {definition.CellSize:0.##}m  |  chunk {definition.ChunkSize}×{definition.ChunkSize}");
        GUILayout.Label($"Origin = cell (0,0) center {definition.WorldOrigin}  |  elevation {definition.MinElevationLevel}..{definition.MaxElevationLevel}");
        GUILayout.Label(pointerText);
        GUILayout.Label(selectionText);
        GUILayout.Label(resultText);
        GUILayout.Label("R/F height  Z undo  |  G ground  T path  V water  X surface undo");
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
