using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#endif

public enum WorldBuildingPlacementFailure
{
    None = 0,
    InvalidDefinition = 1,
    RuntimeOnly = 2,
    DuplicateInstance = 3,
    MissingInstance = 4,
    OutOfBounds = 5,
    ProtectedCell = 6,
    OccupiedCell = 7,
    WaterCell = 8,
    PathCell = 9,
    InvalidGround = 10,
    UnevenFootprint = 11,
    EntranceBlocked = 12,
    MissingPrefab = 13,
    CommitConflict = 14,
    SpawnFailed = 15,
    NoPreview = 16
}

public sealed class WorldBuildingPlacementDefinition
{
    readonly Vector2Int[] _footprintOffsets;

    public string StableId { get; }
    public string BuildingResourcePath { get; }
    public IReadOnlyList<Vector2Int> FootprintOffsets => _footprintOffsets;
    public Vector2Int EntranceOffset { get; }

    public WorldBuildingPlacementDefinition(
        string stableId,
        string buildingResourcePath,
        IEnumerable<Vector2Int> footprintOffsets,
        Vector2Int entranceOffset)
    {
        StableId = stableId;
        BuildingResourcePath = buildingResourcePath;
        _footprintOffsets = footprintOffsets?.ToArray() ?? Array.Empty<Vector2Int>();
        EntranceOffset = entranceOffset;
    }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(StableId) &&
               !string.IsNullOrWhiteSpace(BuildingResourcePath) &&
               _footprintOffsets.Length > 0 &&
               _footprintOffsets.Distinct().Count() == _footprintOffsets.Length &&
               !_footprintOffsets.Contains(EntranceOffset);
    }

    public Vector2Int[] ResolveFootprint(Vector2Int anchor, int quarterTurns)
    {
        int normalized = NormalizeQuarterTurns(quarterTurns);
        var cells = new Vector2Int[_footprintOffsets.Length];
        for (int i = 0; i < _footprintOffsets.Length; i++)
            cells[i] = anchor + Rotate(_footprintOffsets[i], normalized);
        return cells;
    }

    public Vector2Int ResolveEntrance(Vector2Int anchor, int quarterTurns)
    {
        return anchor + Rotate(EntranceOffset, NormalizeQuarterTurns(quarterTurns));
    }

    public static int NormalizeQuarterTurns(int quarterTurns)
    {
        int normalized = quarterTurns % 4;
        return normalized < 0 ? normalized + 4 : normalized;
    }

    static Vector2Int Rotate(Vector2Int offset, int quarterTurns)
    {
        return quarterTurns switch
        {
            1 => new Vector2Int(-offset.y, offset.x),
            2 => new Vector2Int(-offset.x, -offset.y),
            3 => new Vector2Int(offset.y, -offset.x),
            _ => offset
        };
    }
}

public readonly struct WorldBuildingPlacementResult
{
    public bool Succeeded { get; }
    public WorldBuildingPlacementFailure Failure { get; }
    public string InstanceId { get; }
    public Vector2Int Anchor { get; }
    public int QuarterTurns { get; }
    public IReadOnlyList<Vector2Int> Footprint { get; }
    public Vector2Int Entrance { get; }
    public int Revision { get; }

    internal WorldBuildingPlacementResult(
        bool succeeded,
        WorldBuildingPlacementFailure failure,
        string instanceId,
        Vector2Int anchor,
        int quarterTurns,
        IReadOnlyList<Vector2Int> footprint,
        Vector2Int entrance,
        int revision)
    {
        Succeeded = succeeded;
        Failure = failure;
        InstanceId = instanceId;
        Anchor = anchor;
        QuarterTurns = WorldBuildingPlacementDefinition.NormalizeQuarterTurns(quarterTurns);
        Footprint = footprint ?? Array.Empty<Vector2Int>();
        Entrance = entrance;
        Revision = revision;
    }
}

public sealed class WorldPlacedBuildingRuntime
{
    public string InstanceId { get; }
    public WorldBuildingPlacementDefinition Definition { get; }
    public Vector2Int Anchor { get; internal set; }
    public int QuarterTurns { get; internal set; }
    public IReadOnlyList<Vector2Int> Footprint { get; internal set; }
    public Vector2Int Entrance { get; internal set; }
    public GameObject GameObject { get; internal set; }

    internal WorldPlacedBuildingRuntime(
        string instanceId,
        WorldBuildingPlacementDefinition definition,
        Vector2Int anchor,
        int quarterTurns,
        IReadOnlyList<Vector2Int> footprint,
        Vector2Int entrance,
        GameObject gameObject)
    {
        InstanceId = instanceId;
        Definition = definition;
        Anchor = anchor;
        QuarterTurns = quarterTurns;
        Footprint = footprint;
        Entrance = entrance;
        GameObject = gameObject;
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(WorldGridService))]
public sealed class WorldBuildingPlacementService : MonoBehaviour
{
    public const string PrototypeInstanceId = "world-b09-storage-shed-001";

    static readonly Vector2Int[] StorageShedFootprint =
    {
        new Vector2Int(0, 0), new Vector2Int(1, 0),
        new Vector2Int(2, 0), new Vector2Int(3, 0),
        new Vector2Int(0, 1), new Vector2Int(1, 1),
        new Vector2Int(2, 1), new Vector2Int(3, 1),
        new Vector2Int(0, 2), new Vector2Int(1, 2),
        new Vector2Int(2, 2), new Vector2Int(3, 2)
    };

    public static readonly WorldBuildingPlacementDefinition StorageShedDefinition =
        new WorldBuildingPlacementDefinition(
            "B09_STORAGE_SHED",
            "Buildings/Building_B09_StorageShed",
            StorageShedFootprint,
            new Vector2Int(1, -1));

    readonly Dictionary<string, WorldPlacedBuildingRuntime> _placements =
        new Dictionary<string, WorldPlacedBuildingRuntime>(StringComparer.Ordinal);

    WorldGridService _grid;
    GameObject _runtimeRoot;
    GameObject _stagingRoot;
    GameObject _previewGhost;
    string _previewInstanceId;
    Vector2Int _previewAnchor;
    int _previewQuarterTurns;
    bool _previewIsMove;
    bool _hasPreview;
    int _revision;
    WorldBuildingPlacementResult _lastResult;

    public event Action<WorldBuildingPlacementResult> Changed;

    public int RegisteredCount => _placements.Count;
    public int Revision => _revision;
    public bool HasPreview => _hasPreview;
    public bool PreviewIsMove => _previewIsMove;
    public GameObject PreviewGhost => _previewGhost;
    public WorldBuildingPlacementResult LastResult => _lastResult;
    public WorldBuildingPlacementDefinition PrototypeDefinition => StorageShedDefinition;

    void Awake()
    {
        _grid = GetComponent<WorldGridService>();
    }

    public bool TryGetPlacement(string instanceId, out WorldPlacedBuildingRuntime placement)
    {
        return _placements.TryGetValue(instanceId ?? string.Empty, out placement);
    }

    public WorldBuildingPlacementResult Evaluate(
        string instanceId,
        Vector2Int anchor,
        int quarterTurns,
        string movingInstanceId = null)
    {
        ResolveGrid();
        WorldBuildingPlacementDefinition definition = StorageShedDefinition;
        Vector2Int[] footprint = definition.ResolveFootprint(anchor, quarterTurns);
        Vector2Int entrance = definition.ResolveEntrance(anchor, quarterTurns);

        if (!definition.IsValid())
            return Failed(WorldBuildingPlacementFailure.InvalidDefinition, instanceId, anchor,
                quarterTurns, footprint, entrance);
        if (string.IsNullOrWhiteSpace(instanceId))
            return Failed(WorldBuildingPlacementFailure.InvalidDefinition, instanceId, anchor,
                quarterTurns, footprint, entrance);
        if (movingInstanceId == null && _placements.ContainsKey(instanceId))
            return Failed(WorldBuildingPlacementFailure.DuplicateInstance, instanceId, anchor,
                quarterTurns, footprint, entrance);

        HashSet<Vector2Int> ignoredOwnCells = null;
        if (movingInstanceId != null)
        {
            if (!_placements.TryGetValue(movingInstanceId, out WorldPlacedBuildingRuntime moving))
                return Failed(WorldBuildingPlacementFailure.MissingInstance, instanceId, anchor,
                    quarterTurns, footprint, entrance);
            ignoredOwnCells = new HashSet<Vector2Int>(moving.Footprint);
            foreach (Vector2Int oldCell in ignoredOwnCells)
            {
                if (!_grid.TryGetCell(oldCell, out WorldCellData oldData) ||
                    oldData.Occupancy != WorldCellOccupancy.Occupied)
                {
                    return Failed(WorldBuildingPlacementFailure.CommitConflict, instanceId, anchor,
                        quarterTurns, footprint, entrance);
                }
            }
        }

        int? flatLevel = null;
        foreach (Vector2Int coordinate in footprint)
        {
            if (!_grid.TryGetCell(coordinate, out WorldCellData cell))
                return Failed(WorldBuildingPlacementFailure.OutOfBounds, instanceId, anchor,
                    quarterTurns, footprint, entrance);
            if (_grid.IsTerraformProtected(coordinate))
                return Failed(WorldBuildingPlacementFailure.ProtectedCell, instanceId, anchor,
                    quarterTurns, footprint, entrance);
            if (cell.Occupancy != WorldCellOccupancy.Empty &&
                (ignoredOwnCells == null || !ignoredOwnCells.Contains(coordinate)))
            {
                return Failed(WorldBuildingPlacementFailure.OccupiedCell, instanceId, anchor,
                    quarterTurns, footprint, entrance);
            }
            if (cell.HasWater)
                return Failed(WorldBuildingPlacementFailure.WaterCell, instanceId, anchor,
                    quarterTurns, footprint, entrance);
            if (cell.HasPath)
                return Failed(WorldBuildingPlacementFailure.PathCell, instanceId, anchor,
                    quarterTurns, footprint, entrance);
            if (cell.GroundType != WorldGroundType.Default &&
                cell.GroundType != WorldGroundType.Soil)
            {
                return Failed(WorldBuildingPlacementFailure.InvalidGround, instanceId, anchor,
                    quarterTurns, footprint, entrance);
            }

            if (!flatLevel.HasValue) flatLevel = cell.ElevationLevel;
            else if (flatLevel.Value != cell.ElevationLevel)
                return Failed(WorldBuildingPlacementFailure.UnevenFootprint, instanceId, anchor,
                    quarterTurns, footprint, entrance);
        }

        if (!_grid.TryGetCell(entrance, out WorldCellData entranceCell) ||
            footprint.Contains(entrance) ||
            entranceCell.Occupancy != WorldCellOccupancy.Empty ||
            entranceCell.HasWater ||
            !entranceCell.IsWalkable ||
            !flatLevel.HasValue ||
            Mathf.Abs(entranceCell.ElevationLevel - flatLevel.Value) > 1)
        {
            return Failed(WorldBuildingPlacementFailure.EntranceBlocked, instanceId, anchor,
                quarterTurns, footprint, entrance);
        }

        return Succeeded(instanceId, anchor, quarterTurns, footprint, entrance);
    }

    public WorldBuildingPlacementResult TryPlace(
        string instanceId,
        Vector2Int anchor,
        int quarterTurns)
    {
        if (!Application.isPlaying)
            return Store(Failed(WorldBuildingPlacementFailure.RuntimeOnly, instanceId, anchor,
                quarterTurns, Array.Empty<Vector2Int>(), default));

        WorldBuildingPlacementResult preview = Evaluate(instanceId, anchor, quarterTurns);
        if (!preview.Succeeded) return Store(preview);

        BuildingData data = Resources.Load<BuildingData>(StorageShedDefinition.BuildingResourcePath);
        if (data == null || data.prefab == null)
            return Store(Failed(WorldBuildingPlacementFailure.MissingPrefab, instanceId, anchor,
                quarterTurns, preview.Footprint, preview.Entrance));

        GameObject candidate = null;
        try
        {
            candidate = InstantiateInactive(data.prefab, instanceId);
            if (!TryCommitFootprint(Array.Empty<Vector2Int>(), preview.Footprint))
            {
                DestroyRuntimeObject(candidate);
                return Store(Failed(WorldBuildingPlacementFailure.CommitConflict, instanceId, anchor,
                    quarterTurns, preview.Footprint, preview.Entrance));
            }

            ResolvePose(preview.Footprint, quarterTurns, out Vector3 position, out Quaternion rotation);
            candidate.transform.SetParent(ResolveRuntimeRoot().transform, false);
            candidate.transform.SetPositionAndRotation(position, rotation);
            candidate.SetActive(true);

            int normalized = WorldBuildingPlacementDefinition.NormalizeQuarterTurns(quarterTurns);
            var placement = new WorldPlacedBuildingRuntime(
                instanceId,
                StorageShedDefinition,
                anchor,
                normalized,
                preview.Footprint.ToArray(),
                preview.Entrance,
                candidate);
            _placements.Add(instanceId, placement);
            _revision++;
            return Publish(Succeeded(instanceId, anchor, normalized,
                placement.Footprint, placement.Entrance));
        }
        catch (Exception)
        {
            if (candidate != null) DestroyRuntimeObject(candidate);
            RollBackPlacedFootprint(preview.Footprint);
            return Store(Failed(WorldBuildingPlacementFailure.SpawnFailed, instanceId, anchor,
                quarterTurns, preview.Footprint, preview.Entrance));
        }
    }

    public WorldBuildingPlacementResult TryMove(
        string instanceId,
        Vector2Int anchor,
        int quarterTurns)
    {
        if (!Application.isPlaying)
            return Store(Failed(WorldBuildingPlacementFailure.RuntimeOnly, instanceId, anchor,
                quarterTurns, Array.Empty<Vector2Int>(), default));
        if (!_placements.TryGetValue(instanceId ?? string.Empty,
                out WorldPlacedBuildingRuntime placement))
        {
            return Store(Failed(WorldBuildingPlacementFailure.MissingInstance, instanceId, anchor,
                quarterTurns, Array.Empty<Vector2Int>(), default));
        }

        WorldBuildingPlacementResult preview = Evaluate(
            instanceId, anchor, quarterTurns, instanceId);
        if (!preview.Succeeded) return Store(preview);

        Vector2Int oldAnchor = placement.Anchor;
        int oldQuarterTurns = placement.QuarterTurns;
        Vector2Int oldEntrance = placement.Entrance;
        Vector2Int[] oldFootprint = placement.Footprint.ToArray();
        Vector3 oldPosition = placement.GameObject != null
            ? placement.GameObject.transform.position
            : default;
        Quaternion oldRotation = placement.GameObject != null
            ? placement.GameObject.transform.rotation
            : Quaternion.identity;

        if (!TryCommitFootprint(oldFootprint, preview.Footprint))
            return Store(Failed(WorldBuildingPlacementFailure.CommitConflict, instanceId, anchor,
                quarterTurns, preview.Footprint, preview.Entrance));

        try
        {
            if (placement.GameObject == null)
                throw new MissingReferenceException($"Placed building '{instanceId}' has no GameObject.");
            ResolvePose(preview.Footprint, quarterTurns, out Vector3 position, out Quaternion rotation);
            placement.GameObject.transform.SetPositionAndRotation(position, rotation);
            placement.Anchor = anchor;
            placement.QuarterTurns = WorldBuildingPlacementDefinition.NormalizeQuarterTurns(quarterTurns);
            placement.Footprint = preview.Footprint.ToArray();
            placement.Entrance = preview.Entrance;
            _revision++;
            return Publish(Succeeded(instanceId, anchor, placement.QuarterTurns,
                placement.Footprint, placement.Entrance));
        }
        catch (Exception)
        {
            TryCommitFootprint(preview.Footprint, oldFootprint);
            if (placement.GameObject != null)
                placement.GameObject.transform.SetPositionAndRotation(oldPosition, oldRotation);
            placement.Anchor = oldAnchor;
            placement.QuarterTurns = oldQuarterTurns;
            placement.Footprint = oldFootprint;
            placement.Entrance = oldEntrance;
            return Store(Failed(WorldBuildingPlacementFailure.SpawnFailed, instanceId, anchor,
                quarterTurns, preview.Footprint, preview.Entrance));
        }
    }

    public WorldBuildingPlacementResult TryRemove(string instanceId)
    {
        if (!Application.isPlaying)
            return Store(Failed(WorldBuildingPlacementFailure.RuntimeOnly, instanceId, default,
                0, Array.Empty<Vector2Int>(), default));
        if (!_placements.TryGetValue(instanceId ?? string.Empty,
                out WorldPlacedBuildingRuntime placement))
        {
            return Store(Failed(WorldBuildingPlacementFailure.MissingInstance, instanceId, default,
                0, Array.Empty<Vector2Int>(), default));
        }
        if (!TryCommitFootprint(placement.Footprint, Array.Empty<Vector2Int>()))
        {
            return Store(Failed(WorldBuildingPlacementFailure.CommitConflict, instanceId,
                placement.Anchor, placement.QuarterTurns, placement.Footprint, placement.Entrance));
        }

        _placements.Remove(instanceId);
        if (placement.GameObject != null) DestroyRuntimeObject(placement.GameObject);
        _revision++;
        return Publish(Succeeded(instanceId, placement.Anchor, placement.QuarterTurns,
            Array.Empty<Vector2Int>(), placement.Entrance));
    }

    public WorldBuildingPlacementResult BeginPlacementPreview(
        string instanceId,
        Vector2Int anchor,
        int quarterTurns)
    {
        return BeginPreview(instanceId, anchor, quarterTurns, false);
    }

    public WorldBuildingPlacementResult BeginMovePreview(
        string instanceId,
        Vector2Int anchor,
        int quarterTurns)
    {
        return BeginPreview(instanceId, anchor, quarterTurns, true);
    }

    public WorldBuildingPlacementResult UpdatePreview(Vector2Int anchor, int quarterTurns)
    {
        if (!_hasPreview)
            return Store(Failed(WorldBuildingPlacementFailure.NoPreview, string.Empty, anchor,
                quarterTurns, Array.Empty<Vector2Int>(), default));
        _previewAnchor = anchor;
        _previewQuarterTurns = WorldBuildingPlacementDefinition.NormalizeQuarterTurns(quarterTurns);
        WorldBuildingPlacementResult result = _previewIsMove
            ? Evaluate(_previewInstanceId, anchor, _previewQuarterTurns, _previewInstanceId)
            : Evaluate(_previewInstanceId, anchor, _previewQuarterTurns);
        UpdateGhost(result);
        return Store(result);
    }

    public WorldBuildingPlacementResult CommitPreview()
    {
        if (!_hasPreview)
            return Store(Failed(WorldBuildingPlacementFailure.NoPreview, string.Empty, default,
                0, Array.Empty<Vector2Int>(), default));

        string instanceId = _previewInstanceId;
        Vector2Int anchor = _previewAnchor;
        int quarterTurns = _previewQuarterTurns;
        bool isMove = _previewIsMove;
        WorldBuildingPlacementResult result = isMove
            ? TryMove(instanceId, anchor, quarterTurns)
            : TryPlace(instanceId, anchor, quarterTurns);
        if (result.Succeeded) ClearPreview();
        return Store(result);
    }

    public void CancelPreview()
    {
        ClearPreview();
    }

    WorldBuildingPlacementResult BeginPreview(
        string instanceId,
        Vector2Int anchor,
        int quarterTurns,
        bool isMove)
    {
        if (!Application.isPlaying)
            return Store(Failed(WorldBuildingPlacementFailure.RuntimeOnly, instanceId, anchor,
                quarterTurns, Array.Empty<Vector2Int>(), default));
        if (isMove && !_placements.ContainsKey(instanceId ?? string.Empty))
            return Store(Failed(WorldBuildingPlacementFailure.MissingInstance, instanceId, anchor,
                quarterTurns, Array.Empty<Vector2Int>(), default));

        ClearPreview();
        _hasPreview = true;
        _previewIsMove = isMove;
        _previewInstanceId = instanceId;
        _previewAnchor = anchor;
        _previewQuarterTurns = WorldBuildingPlacementDefinition.NormalizeQuarterTurns(quarterTurns);

        if (!TryCreateGhost())
        {
            ClearPreview();
            return Store(Failed(WorldBuildingPlacementFailure.MissingPrefab, instanceId, anchor,
                quarterTurns, Array.Empty<Vector2Int>(), default));
        }

        return UpdatePreview(anchor, _previewQuarterTurns);
    }

    bool TryCommitFootprint(
        IReadOnlyList<Vector2Int> oldFootprint,
        IReadOnlyList<Vector2Int> newFootprint)
    {
        ResolveGrid();
        var oldCells = new HashSet<Vector2Int>(oldFootprint ?? Array.Empty<Vector2Int>());
        var newCells = new HashSet<Vector2Int>(newFootprint ?? Array.Empty<Vector2Int>());
        var union = oldCells.Union(newCells)
            .OrderBy(cell => cell.y)
            .ThenBy(cell => cell.x)
            .ToArray();
        if (union.Length == 0) return false;

        var expected = new List<WorldCellData>(union.Length);
        var replacements = new List<WorldCellData>(union.Length);
        foreach (Vector2Int coordinate in union)
        {
            if (!_grid.TryGetCell(coordinate, out WorldCellData current)) return false;
            if (oldCells.Contains(coordinate) &&
                current.Occupancy != WorldCellOccupancy.Occupied)
            {
                return false;
            }
            if (!oldCells.Contains(coordinate) && newCells.Contains(coordinate) &&
                current.Occupancy != WorldCellOccupancy.Empty)
            {
                return false;
            }

            expected.Add(current);
            replacements.Add(current.WithOccupancy(
                newCells.Contains(coordinate)
                    ? WorldCellOccupancy.Occupied
                    : WorldCellOccupancy.Empty));
        }
        return _grid.TryCommitOccupancyBatch(expected, replacements);
    }

    void RollBackPlacedFootprint(IReadOnlyList<Vector2Int> footprint)
    {
        if (footprint == null || footprint.Count == 0) return;
        TryCommitFootprint(footprint, Array.Empty<Vector2Int>());
    }

    GameObject InstantiateInactive(GameObject prefab, string instanceId)
    {
        if (_stagingRoot == null)
        {
            _stagingRoot = new GameObject("WorldBuildingPlacement_Staging")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _stagingRoot.transform.SetParent(transform, false);
            _stagingRoot.SetActive(false);
        }

        GameObject instance = Instantiate(prefab, _stagingRoot.transform, false);
        instance.name = $"{prefab.name}_{instanceId}";
        return instance;
    }

    GameObject ResolveRuntimeRoot()
    {
        if (_runtimeRoot != null) return _runtimeRoot;
        _runtimeRoot = new GameObject("WorldPlacedBuildings_Runtime")
        {
            hideFlags = HideFlags.DontSave
        };
        _runtimeRoot.transform.SetParent(transform, false);
        return _runtimeRoot;
    }

    bool TryCreateGhost()
    {
        BuildingData data = Resources.Load<BuildingData>(StorageShedDefinition.BuildingResourcePath);
        if (data == null || data.prefab == null) return false;

        _previewGhost = InstantiateInactive(data.prefab, "PREVIEW");
        _previewGhost.name = "B09_StorageShed_PlacementGhost";
        foreach (Collider collider in _previewGhost.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (MonoBehaviour behaviour in _previewGhost.GetComponentsInChildren<MonoBehaviour>(true))
            behaviour.enabled = false;
        foreach (NavMeshObstacle obstacle in _previewGhost.GetComponentsInChildren<NavMeshObstacle>(true))
            obstacle.enabled = false;
        foreach (Transform child in _previewGhost.GetComponentsInChildren<Transform>(true))
            child.gameObject.tag = "Untagged";

        _previewGhost.transform.SetParent(ResolveRuntimeRoot().transform, false);
        _previewGhost.SetActive(true);
        return true;
    }

    void UpdateGhost(WorldBuildingPlacementResult result)
    {
        if (_previewGhost == null) return;
        ResolvePose(result.Footprint, result.QuarterTurns, out Vector3 position,
            out Quaternion rotation, result.Anchor);
        _previewGhost.transform.SetPositionAndRotation(position, rotation);
        Color tint = result.Succeeded
            ? new Color(0.34f, 0.92f, 0.54f, 1f)
            : new Color(1f, 0.28f, 0.24f, 1f);
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", tint);
        block.SetColor("_Color", tint);
        foreach (Renderer renderer in _previewGhost.GetComponentsInChildren<Renderer>(true))
            renderer.SetPropertyBlock(block);
    }

    void ResolvePose(
        IReadOnlyList<Vector2Int> footprint,
        int quarterTurns,
        out Vector3 position,
        out Quaternion rotation,
        Vector2Int fallbackAnchor = default)
    {
        ResolveGrid();
        float x = 0f;
        float z = 0f;
        float y = 0f;
        int validCount = 0;
        if (footprint != null)
        {
            foreach (Vector2Int coordinate in footprint)
            {
                WorldGridDefinition definition = _grid.Definition;
                x += definition.WorldOrigin.x + coordinate.x * definition.CellSize;
                z += definition.WorldOrigin.z + coordinate.y * definition.CellSize;
                if (_grid.TryGetCell(coordinate, out WorldCellData cell))
                {
                    y += definition.WorldOrigin.y +
                         cell.ElevationLevel * definition.ElevationStep;
                    validCount++;
                }
            }
        }

        int count = footprint?.Count ?? 0;
        if (count > 0)
        {
            x /= count;
            z /= count;
            y = validCount > 0 ? y / validCount : _grid.Definition.WorldOrigin.y;
        }
        else
        {
            x = _grid.Definition.WorldOrigin.x + fallbackAnchor.x * _grid.Definition.CellSize;
            z = _grid.Definition.WorldOrigin.z + fallbackAnchor.y * _grid.Definition.CellSize;
            y = _grid.Definition.WorldOrigin.y;
        }

        position = new Vector3(x, y, z);
        rotation = Quaternion.Euler(
            0f,
            -90f * WorldBuildingPlacementDefinition.NormalizeQuarterTurns(quarterTurns),
            0f);
    }

    void ResolveGrid()
    {
        if (_grid == null) _grid = GetComponent<WorldGridService>();
        if (_grid == null) throw new InvalidOperationException("WorldGridService is required.");
    }

    WorldBuildingPlacementResult Succeeded(
        string instanceId,
        Vector2Int anchor,
        int quarterTurns,
        IReadOnlyList<Vector2Int> footprint,
        Vector2Int entrance)
    {
        return new WorldBuildingPlacementResult(
            true,
            WorldBuildingPlacementFailure.None,
            instanceId,
            anchor,
            quarterTurns,
            footprint,
            entrance,
            _revision);
    }

    WorldBuildingPlacementResult Failed(
        WorldBuildingPlacementFailure failure,
        string instanceId,
        Vector2Int anchor,
        int quarterTurns,
        IReadOnlyList<Vector2Int> footprint,
        Vector2Int entrance)
    {
        return new WorldBuildingPlacementResult(
            false,
            failure,
            instanceId,
            anchor,
            quarterTurns,
            footprint,
            entrance,
            _revision);
    }

    WorldBuildingPlacementResult Store(WorldBuildingPlacementResult result)
    {
        _lastResult = result;
        return result;
    }

    WorldBuildingPlacementResult Publish(WorldBuildingPlacementResult result)
    {
        _lastResult = result;
        Changed?.Invoke(result);
        return result;
    }

    void ClearPreview()
    {
        if (_previewGhost != null) DestroyRuntimeObject(_previewGhost);
        _previewGhost = null;
        _previewInstanceId = null;
        _previewAnchor = default;
        _previewQuarterTurns = 0;
        _previewIsMove = false;
        _hasPreview = false;
    }

    static void DestroyRuntimeObject(UnityEngine.Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}

#if UNITY_EDITOR
public static class PA_WorldBuildingPlacementTools
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.WORLD005.Active";
    const string FailedKey = "PA.WORLD005.Failed";
    const string ConsoleErrorKey = "PA.WORLD005.ConsoleErrors";
    const string WaitFramesKey = "PA.WORLD005.WaitFrames";

    [InitializeOnLoadMethod]
    static void ResumeValidationAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        SubscribeCallbacks();
        if (EditorApplication.isPlaying) EditorApplication.update += ValidateRuntime;
    }

    [MenuItem("Project PA/World/WORLD-005/Build Placement Prototype")]
    public static void BuildWorld005Sandbox()
    {
        BuildWorld005SandboxInternal();
    }

    public static void BuildWorld005SandboxBatch()
    {
        BuildWorld005SandboxInternal();
    }

    static void BuildWorld005SandboxInternal()
    {
        try
        {
            Scene current = SceneManager.GetActiveScene();
            if (current.IsValid() && current.isDirty)
                throw new InvalidOperationException(
                    $"Active scene '{current.path}' has unsaved changes.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            WorldGridService grid = FindSingle<WorldGridService>(scene);
            foreach (WorldBuildingPlacementDebugController controller in
                     grid.GetComponents<WorldBuildingPlacementDebugController>())
            {
                UnityEngine.Object.DestroyImmediate(controller);
            }
            foreach (WorldBuildingPlacementService service in
                     grid.GetComponents<WorldBuildingPlacementService>())
            {
                UnityEngine.Object.DestroyImmediate(service);
            }
            grid.gameObject.AddComponent<WorldBuildingPlacementService>();
            grid.gameObject.AddComponent<WorldBuildingPlacementDebugController>();
            Require(grid.GetComponents<WorldBuildingPlacementService>().Length == 1,
                "WorldGrid owns one placement service");
            Require(grid.GetComponents<WorldBuildingPlacementDebugController>().Length == 1,
                "WorldGrid owns one placement debug controller");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Failed to save WorldSandbox.");
            Debug.Log("[WORLD-005] BUILD_PASS WorldSandbox placement prototype ready");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WORLD-005] BUILD_FAIL {ex.Message}\n{ex}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    [MenuItem("Project PA/World/WORLD-005/Validate Relocatable Building")]
    public static void RunWorld005Validation()
    {
        RunWorld005ValidationInternal();
    }

    public static void RunWorld005ValidationBatch()
    {
        RunWorld005ValidationInternal();
    }

    static void RunWorld005ValidationInternal()
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
            WorldGridService grid = FindSingle<WorldGridService>(scene);
            WorldBuildingPlacementService placement =
                FindSingle<WorldBuildingPlacementService>(scene);
            Require(FindComponents<WorldBuildingPlacementDebugController>(scene).Count == 1,
                "scene has one placement debug controller");
            Require(FindComponents<BuildingRegistry>(scene).Count == 0,
                "legacy BuildingRegistry is not duplicated in WorldSandbox");
            ValidateDefinitionAndEditPreflight(grid, placement);
            ValidateLegacyPlacementContract();
            Require(!scene.isDirty, "Edit Mode preflight does not dirty the scene");
            Debug.Log("[WORLD-005] EDIT_MODE_PASS");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void ValidateDefinitionAndEditPreflight(
        WorldGridService grid,
        WorldBuildingPlacementService placement)
    {
        WorldBuildingPlacementDefinition definition = placement.PrototypeDefinition;
        Require(definition.IsValid() && definition.StableId == "B09_STORAGE_SHED",
            "sidecar definition identifies the existing B09 storage shed");
        Require(definition.FootprintOffsets.Count == 12,
            "7m x 5.5m shed uses a conservative 4x3 footprint on 2m cells");
        BuildingData data = Resources.Load<BuildingData>(definition.BuildingResourcePath);
        Require(data != null && data.prefab != null && data.prefab.name == "B09_StorageShed",
            "sidecar resolves the existing storage shed prefab without source changes");

        var anchor = new Vector2Int(6, 6);
        WorldBuildingPlacementResult valid = placement.Evaluate("edit-preview", anchor, 0);
        Require(valid.Succeeded && valid.Footprint.Count == 12 &&
                valid.Entrance == new Vector2Int(7, 5),
            "flat central plateau accepts the 4x3 footprint and south entrance");
        WorldBuildingPlacementResult rotated = placement.Evaluate("edit-preview", new Vector2Int(9, 6), 1);
        Require(rotated.Succeeded && rotated.Entrance == new Vector2Int(10, 7) &&
                rotated.Footprint.Contains(new Vector2Int(7, 9)),
            "quarter-turn rotates footprint and entrance together");
        Require(placement.Evaluate("edit-preview", new Vector2Int(5, 5), 0).Failure ==
                WorldBuildingPlacementFailure.UnevenFootprint,
            "cliff-spanning footprint is rejected as uneven");
        Require(placement.Evaluate("edit-preview", Vector2Int.zero, 0).Failure ==
                WorldBuildingPlacementFailure.ProtectedCell,
            "protected origin rejects placement");
        Require(placement.Evaluate("edit-preview", new Vector2Int(15, 15), 0).Failure ==
                WorldBuildingPlacementFailure.OutOfBounds,
            "out-of-bounds footprint is rejected atomically");
        Require(grid.Cells.All(cell => cell.Occupancy == WorldCellOccupancy.Empty),
            "Edit Mode preview keeps authoritative occupancy empty");
    }

    static void ValidateLegacyPlacementContract()
    {
        Require(typeof(OutdoorPlacementController).GetMethod("TryApplyMove") != null &&
                typeof(OutdoorPlacementController).GetMethod("TryRecover") != null,
            "legacy outdoor move/recover API remains intact");
        Require(typeof(BuildingRegistry).GetMethod("Register") != null &&
                typeof(BuildingRegistry).GetMethod("Unregister") != null,
            "legacy BuildingRegistry registration API remains intact");
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
        if (frames < 5) return;
        EditorApplication.update -= ValidateRuntime;

        try
        {
            Scene scene = SceneManager.GetActiveScene();
            WorldGridService grid = FindSingle<WorldGridService>(scene);
            WorldBuildingPlacementService placement =
                FindSingle<WorldBuildingPlacementService>(scene);
            Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
                $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
            Require(placement.RegisteredCount == 0 && !placement.HasPreview,
                "runtime starts with an empty session placement registry");
            ulong baselineHash = ComputeCellHash(grid.Cells);

            var waterCell = new Vector2Int(1, 0);
            Require(grid.SurfaceEditor.FillWaterOneLevel(waterCell).Succeeded,
                "validator prepares one legal water cell");
            WorldBuildingPlacementResult waterPreview =
                placement.Evaluate("water-preview", waterCell, 0);
            Require(!waterPreview.Succeeded &&
                    waterPreview.Failure == WorldBuildingPlacementFailure.WaterCell,
                "water footprint is rejected with a typed reason");
            Require(grid.SurfaceEditor.UndoLast().Succeeded,
                "water preflight fixture rolls back to the terrain baseline");

            var startAnchor = new Vector2Int(6, 6);
            WorldBuildingPlacementResult ghost = placement.BeginPlacementPreview(
                WorldBuildingPlacementService.PrototypeInstanceId, startAnchor, 0);
            Require(ghost.Succeeded && placement.HasPreview &&
                    placement.PreviewGhost != null && placement.PreviewGhost.activeInHierarchy &&
                    placement.PreviewGhost.GetComponentsInChildren<Renderer>(true).Length > 0,
                "existing shed prefab appears as an active valid placement ghost");
            WorldBuildingPlacementResult rotatedGhost =
                placement.UpdatePreview(new Vector2Int(9, 6), 1);
            Require(rotatedGhost.Succeeded && rotatedGhost.Entrance == new Vector2Int(10, 7),
                "ghost rotation keeps the rotated entrance contract");
            placement.CancelPreview();
            Require(!placement.HasPreview && placement.PreviewGhost == null &&
                    placement.RegisteredCount == 0 && ComputeCellHash(grid.Cells) == baselineHash,
                "cancel removes the ghost without cell or registry mutation");

            WorldBuildingPlacementResult placed = placement.TryPlace(
                WorldBuildingPlacementService.PrototypeInstanceId, startAnchor, 0);
            Require(placed.Succeeded && placement.RegisteredCount == 1,
                "valid footprint commits exactly one registered building");
            Require(placement.TryGetPlacement(
                        WorldBuildingPlacementService.PrototypeInstanceId,
                        out WorldPlacedBuildingRuntime runtimeBuilding) &&
                    runtimeBuilding.GameObject != null &&
                    runtimeBuilding.GameObject.activeInHierarchy &&
                    runtimeBuilding.GameObject.GetComponent<StorageBox>() != null,
                "committed object preserves the existing storage function");
            Require(placed.Footprint.All(cell =>
                    grid.TryGetCell(cell, out WorldCellData data) &&
                    data.Occupancy == WorldCellOccupancy.Occupied && !data.IsWalkable),
                "all committed footprint cells are occupied and non-walkable");

            ulong placedHash = ComputeCellHash(grid.Cells);
            Vector2Int occupiedCell = placed.Footprint[0];
            Require(grid.Terraform.Lower(occupiedCell).Failure ==
                    WorldTerraformFailure.OccupiedCell &&
                    ComputeCellHash(grid.Cells) == placedHash,
                "occupied cell rejects elevation editing without mutation");
            Require(grid.SurfaceEditor.PaintGround(occupiedCell, WorldGroundType.Soil).Failure ==
                    WorldSurfaceEditFailure.OccupiedCell &&
                    ComputeCellHash(grid.Cells) == placedHash,
                "occupied cell rejects surface editing without mutation");
            Require(placement.TryPlace("overlap-test", startAnchor, 0).Failure ==
                    WorldBuildingPlacementFailure.OccupiedCell &&
                    placement.RegisteredCount == 1 && ComputeCellHash(grid.Cells) == placedHash,
                "overlapping second building is rejected atomically");

            Vector3 positionBeforeFailedMove = runtimeBuilding.GameObject.transform.position;
            WorldBuildingPlacementResult failedMove = placement.TryMove(
                WorldBuildingPlacementService.PrototypeInstanceId, new Vector2Int(5, 5), 0);
            Require(!failedMove.Succeeded &&
                    failedMove.Failure == WorldBuildingPlacementFailure.UnevenFootprint &&
                    placement.RegisteredCount == 1 &&
                    runtimeBuilding.GameObject.transform.position == positionBeforeFailedMove &&
                    placed.Footprint.All(cell =>
                        grid.TryGetCell(cell, out WorldCellData data) &&
                        data.Occupancy == WorldCellOccupancy.Occupied),
                "invalid move rolls back with old transform and occupancy intact");

            Vector2Int[] oldFootprint = runtimeBuilding.Footprint.ToArray();
            var moveAnchor = new Vector2Int(9, 6);
            WorldBuildingPlacementResult moved = placement.TryMove(
                WorldBuildingPlacementService.PrototypeInstanceId, moveAnchor, 1);
            Require(moved.Succeeded && placement.RegisteredCount == 1 &&
                    moved.Entrance == new Vector2Int(10, 7),
                "valid move rotates and commits one registry record");
            var movedSet = new HashSet<Vector2Int>(moved.Footprint);
            Require(oldFootprint.Where(cell => !movedSet.Contains(cell)).All(cell =>
                        grid.TryGetCell(cell, out WorldCellData data) &&
                        data.Occupancy == WorldCellOccupancy.Empty) &&
                    moved.Footprint.All(cell =>
                        grid.TryGetCell(cell, out WorldCellData data) &&
                        data.Occupancy == WorldCellOccupancy.Occupied),
                "old and new occupancy switch atomically, including overlap cells");

            WorldBuildingPlacementResult removed = placement.TryRemove(
                WorldBuildingPlacementService.PrototypeInstanceId);
            Require(removed.Succeeded && placement.RegisteredCount == 0 &&
                    grid.Cells.All(cell => cell.Occupancy == WorldCellOccupancy.Empty) &&
                    ComputeCellHash(grid.Cells) == baselineHash,
                "session cleanup restores the exact baseline cell checksum");
            Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                "blocking runtime Console Error/Exception/Assert count is 0");
            Debug.Log("[WORLD-005] PLAY_MODE_PASS");
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
            foreach (WorldCellData cell in cells)
            {
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
        Debug.LogError($"[WORLD-005] FAIL {ex.Message}\n{ex}");
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
            ? $"[WORLD-005] FINISHED_WITH_ERRORS consoleErrors={consoleErrors}"
            : "[WORLD-005] FINISHED_PASS footprint=4x3 ghost=true rotation=true occupancyAtomic=true rollback=true registry=1");
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

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[WORLD-005] PASS {message}");
    }
}
#endif
