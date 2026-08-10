using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Stopwatch = System.Diagnostics.Stopwatch;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
#endif

public enum WorldNavigationAgentState
{
    Idle = 0,
    Moving = 1,
    PausedForRebuild = 2,
    Arrived = 3,
    Unreachable = 4
}

public static class WorldCellReachability
{
    static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.left,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.up
    };

    public static bool CanReachAll(
        WorldGridService grid,
        Vector2Int start,
        IEnumerable<Vector2Int> targets,
        ISet<Vector2Int> additionallyBlocked,
        ISet<Vector2Int> reopenedOccupied,
        out int reachableCount)
    {
        if (grid == null)
        {
            reachableCount = 0;
            return false;
        }
        return CanReachAll(grid.Definition, grid.Cells, start, targets,
            additionallyBlocked, reopenedOccupied, out reachableCount);
    }

    public static bool CanReachAll(
        WorldGridDefinition definition,
        IReadOnlyList<WorldCellData> cells,
        Vector2Int start,
        IEnumerable<Vector2Int> targets,
        ISet<Vector2Int> additionallyBlocked,
        ISet<Vector2Int> reopenedOccupied,
        out int reachableCount)
    {
        reachableCount = 0;
        if (definition == null || cells == null ||
            cells.Count != definition.TotalCellCount ||
            !TryGetWalkable(definition, cells, start, additionallyBlocked,
                reopenedOccupied, out _))
        {
            return false;
        }

        var visited = new bool[cells.Count];
        var queue = new Queue<Vector2Int>();
        int startIndex = start.y * definition.Width + start.x;
        visited[startIndex] = true;
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            Vector2Int currentCoordinate = queue.Dequeue();
            WorldCellData current = cells[currentCoordinate.y * definition.Width + currentCoordinate.x];
            reachableCount++;
            foreach (Vector2Int direction in CardinalDirections)
            {
                Vector2Int nextCoordinate = currentCoordinate + direction;
                if (!TryGetWalkable(definition, cells, nextCoordinate, additionallyBlocked,
                        reopenedOccupied, out WorldCellData next))
                {
                    continue;
                }
                int nextIndex = nextCoordinate.y * definition.Width + nextCoordinate.x;
                if (visited[nextIndex] ||
                    Mathf.Abs(next.ElevationLevel - current.ElevationLevel) > 1)
                {
                    continue;
                }
                visited[nextIndex] = true;
                queue.Enqueue(nextCoordinate);
            }
        }

        foreach (Vector2Int target in targets ?? Array.Empty<Vector2Int>())
        {
            if (target.x < 0 || target.x >= definition.Width ||
                target.y < 0 || target.y >= definition.Height ||
                !visited[target.y * definition.Width + target.x])
            {
                return false;
            }
        }
        return true;
    }

    static bool TryGetWalkable(
        WorldGridDefinition definition,
        IReadOnlyList<WorldCellData> cells,
        Vector2Int coordinate,
        ISet<Vector2Int> additionallyBlocked,
        ISet<Vector2Int> reopenedOccupied,
        out WorldCellData cell)
    {
        if (coordinate.x < 0 || coordinate.x >= definition.Width ||
            coordinate.y < 0 || coordinate.y >= definition.Height ||
            (additionallyBlocked != null && additionallyBlocked.Contains(coordinate)))
        {
            cell = default;
            return false;
        }

        cell = cells[coordinate.y * definition.Width + coordinate.x];
        if (cell.HasWater) return false;
        return cell.Occupancy == WorldCellOccupancy.Empty ||
               reopenedOccupied != null && reopenedOccupied.Contains(coordinate);
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(WorldGridService), typeof(WorldBuildingPlacementService))]
public sealed class WorldNavigationService : MonoBehaviour
{
    public const int SectorSizeChunks = 2;
    public const int SectorOverlapCells = 1;
    public const int NavigationProxyLayer = 2; // built-in Ignore Raycast
    public const float MaximumAgentReprojectionDistance = 2f;
    public const float RebuildTimeoutSeconds = 10f;

    const string RuntimeRootName = "WorldNavigation_Runtime";
    const string LinkRootName = "WorldNavigationLinks_Runtime";
    const string TestAgentName = "WorldNavigationNpcTestAgent_Runtime";

    sealed class SectorRuntime
    {
        public Vector2Int Coordinate;
        public GameObject Root;
        public MeshCollider ProxyCollider;
        public Mesh ProxyMesh;
        public NavMeshSurface Surface;
        public int Revision;
    }

    readonly Dictionary<Vector2Int, SectorRuntime> _sectors =
        new Dictionary<Vector2Int, SectorRuntime>();
    readonly Queue<Vector2Int> _dirtyQueue = new Queue<Vector2Int>();
    readonly HashSet<Vector2Int> _queuedSectors = new HashSet<Vector2Int>();
    readonly List<Vector2Int> _lastRebuiltSectors = new List<Vector2Int>();
    readonly List<Vector2Int> _criticalTargets = new List<Vector2Int>();
    readonly Dictionary<string, Vector2Int[]> _knownBuildingFootprints =
        new Dictionary<string, Vector2Int[]>(StringComparer.Ordinal);

    WorldGridService _grid;
    WorldBuildingPlacementService _buildings;
    WorldPersistenceService _persistence;
    WorldTerraformService _subscribedTerraform;
    WorldSurfaceEditService _subscribedSurface;
    WorldGridDefinition _activeDefinition;
    GameObject _runtimeRoot;
    GameObject _linkRoot;
    Coroutine _rebuildRoutine;
    NavMeshAgent _testAgent;
    Vector3 _testDestination;
    Vector2Int _criticalStart;
    long _criticalSeed = long.MinValue;
    bool _buildingSubscribed;
    bool _placementGateEnabled;
    bool _validationAnchorsConfigured;
    bool _agentHadDestinationBeforePause;
    float _lastReprojectionDistance;
    int _navigationRevision;
    int _linkCount;
    int _agentPauseCount;
    int _agentRepathCount;
    long _initialBuildMilliseconds;
    long _lastRebuildMilliseconds;
    string _lastFailure = string.Empty;
    WorldNavigationAgentState _testAgentState;

    public int SectorCount => _sectors.Count;
    public int NavigationRevision => _navigationRevision;
    public int LinkCount => _linkCount;
    public int PendingSectorCount => _queuedSectors.Count;
    public bool IsRebuilding => _rebuildRoutine != null;
    public IReadOnlyList<Vector2Int> LastRebuiltSectors => _lastRebuiltSectors;
    public long InitialBuildMilliseconds => _initialBuildMilliseconds;
    public long LastRebuildMilliseconds => _lastRebuildMilliseconds;
    public string LastFailure => _lastFailure;
    public NavMeshAgent TestAgent => _testAgent;
    public Vector3 TestDestination => _testDestination;
    public WorldNavigationAgentState TestAgentState => _testAgentState;
    public int TestAgentPauseCount => _agentPauseCount;
    public int TestAgentRepathCount => _agentRepathCount;
    public float LastAgentReprojectionDistance => _lastReprojectionDistance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapWorldSandbox()
    {
        if (SceneManager.GetActiveScene().name != "WorldSandbox" ||
            FindFirstObjectByType<WorldNavigationService>() != null)
        {
            return;
        }

        WorldGridService grid = FindFirstObjectByType<WorldGridService>();
        if (grid != null) grid.gameObject.AddComponent<WorldNavigationService>();
    }

    void Awake()
    {
        ResolveDependencies();
    }

    IEnumerator Start()
    {
        yield return null;
        EnsureSubscriptions();
        RefreshCriticalAnchors();
        if (BuildAllNow(out _)) TryStartDefaultTestAgent();
    }

    void Update()
    {
        ResolveDependencies();
        EnsureSubscriptions();
        RefreshCriticalAnchors();

        if (_rebuildRoutine == null && _activeDefinition != _grid.Definition)
        {
            BuildAllNow(out _);
            TryStartDefaultTestAgent();
        }
        if (_rebuildRoutine == null && _dirtyQueue.Count > 0)
            _rebuildRoutine = StartCoroutine(ProcessDirtyQueue());
        UpdateTestAgentState();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void OnDestroy()
    {
        Unsubscribe();
        DisposeNavigationRuntime();
    }

    void OnGUI()
    {
        if (!Application.isPlaying || SceneManager.GetActiveScene().name != "WorldSandbox")
            return;
        GUILayout.BeginArea(new Rect(Mathf.Max(8f, Screen.width - 338f), 12f, 326f, 104f),
            GUI.skin.box);
        GUILayout.Label("WORLD-008 LOCAL NAVIGATION");
        GUILayout.Label($"Sectors {SectorCount}  Links {LinkCount}  Nav rev {NavigationRevision}");
        GUILayout.Label($"Queue {PendingSectorCount}  Rebuilding {IsRebuilding}  Last {_lastRebuildMilliseconds}ms");
        GUILayout.Label($"Test NPC {TestAgentState}  pause {_agentPauseCount}  repath {_agentRepathCount}");
        GUILayout.EndArea();
    }

    public bool TryValidateBuildingPlacement(
        IReadOnlyCollection<Vector2Int> newFootprint,
        Vector2Int entrance,
        IReadOnlyCollection<Vector2Int> movingOldFootprint,
        out string reason)
    {
        reason = string.Empty;
        RefreshCriticalAnchors();
        if (!_placementGateEnabled) return true;

        var blocked = new HashSet<Vector2Int>(newFootprint ?? Array.Empty<Vector2Int>());
        var reopened = new HashSet<Vector2Int>(movingOldFootprint ?? Array.Empty<Vector2Int>());
        var targets = new List<Vector2Int>(_criticalTargets) { entrance };
        if (WorldCellReachability.CanReachAll(_grid, _criticalStart, targets,
                blocked, reopened, out _))
        {
            return true;
        }

        reason = "The building would isolate a critical anchor or its entrance.";
        return false;
    }

    public bool BuildAllNow(out string reason)
    {
        reason = string.Empty;
        ResolveDependencies();
        StopQueuedRebuild();
        DisposeNavigationRuntime();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _runtimeRoot = new GameObject(RuntimeRootName)
            {
                hideFlags = HideFlags.DontSave
            };
            _runtimeRoot.transform.SetParent(transform, false);
            CreateSectorObjects();
            foreach (SectorRuntime sector in _sectors.Values)
                UpdateSectorProxyMesh(sector);
            foreach (SectorRuntime sector in _sectors.Values.OrderBy(entry => entry.Coordinate.y)
                         .ThenBy(entry => entry.Coordinate.x))
            {
                sector.Surface.BuildNavMesh();
                if (sector.Surface.navMeshData == null)
                    throw new InvalidOperationException($"Sector {sector.Coordinate} produced no NavMeshData.");
                sector.Revision++;
            }
            RefreshSeamLinks();
            _navigationRevision++;
            _activeDefinition = _grid.Definition;
            _lastFailure = string.Empty;
            stopwatch.Stop();
            _initialBuildMilliseconds = stopwatch.ElapsedMilliseconds;
            _lastRebuildMilliseconds = stopwatch.ElapsedMilliseconds;
            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _lastFailure = ex.Message;
            reason = ex.Message;
            DisposeNavigationRuntime();
            return false;
        }
    }

    public bool StartTestAgent(
        Vector2Int start,
        Vector2Int destination,
        float speed,
        out string reason)
    {
        reason = string.Empty;
        DestroyTestAgent();
        if (!TrySampleCell(start, out NavMeshHit startHit) ||
            !TrySampleCell(destination, out NavMeshHit destinationHit))
        {
            reason = "Test agent endpoints are not on the runtime NavMesh.";
            _testAgentState = WorldNavigationAgentState.Unreachable;
            return false;
        }

        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(startHit.position, destinationHit.position,
                NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
        {
            reason = "Test agent endpoints do not have a complete NavMesh path.";
            _testAgentState = WorldNavigationAgentState.Unreachable;
            return false;
        }

        var agentObject = new GameObject(TestAgentName)
        {
            hideFlags = HideFlags.DontSave
        };
        agentObject.transform.SetParent(_runtimeRoot != null ? _runtimeRoot.transform : transform, false);
        agentObject.transform.position = startHit.position;
        _testAgent = agentObject.AddComponent<NavMeshAgent>();
        _testAgent.radius = 0.34f;
        _testAgent.height = 1.5f;
        _testAgent.baseOffset = 0f;
        _testAgent.speed = Mathf.Max(0.5f, speed);
        _testAgent.acceleration = Mathf.Max(8f, speed * 4f);
        _testAgent.angularSpeed = 720f;
        _testAgent.stoppingDistance = 0.18f;
        _testAgent.autoRepath = false;
        _testAgent.autoBraking = true;
        if (!_testAgent.isOnNavMesh || !_testAgent.SetDestination(destinationHit.position))
        {
            reason = "Test agent could not accept its complete destination.";
            DestroyTestAgent();
            _testAgentState = WorldNavigationAgentState.Unreachable;
            return false;
        }

        _testDestination = destinationHit.position;
        _testAgentState = WorldNavigationAgentState.Moving;
        return true;
    }

    public int GetSectorRevision(Vector2Int coordinate)
    {
        return _sectors.TryGetValue(coordinate, out SectorRuntime sector)
            ? sector.Revision
            : -1;
    }

    public static IReadOnlyList<Vector2Int> ResolveSectorsForDirtyChunks(
        WorldGridDefinition definition,
        IEnumerable<Vector2Int> dirtyChunks)
    {
        var result = new List<Vector2Int>();
        if (definition == null || dirtyChunks == null) return result.AsReadOnly();
        int sectorCountX = Mathf.CeilToInt(definition.ChunkCountX / (float)SectorSizeChunks);
        int sectorCountZ = Mathf.CeilToInt(definition.ChunkCountZ / (float)SectorSizeChunks);
        foreach (Vector2Int chunk in dirtyChunks)
        {
            var sector = new Vector2Int(chunk.x / SectorSizeChunks, chunk.y / SectorSizeChunks);
            if (sector.x < 0 || sector.x >= sectorCountX ||
                sector.y < 0 || sector.y >= sectorCountZ || result.Contains(sector))
            {
                continue;
            }
            result.Add(sector);
        }
        result.Sort((left, right) => left.y != right.y
            ? left.y.CompareTo(right.y)
            : left.x.CompareTo(right.x));
        return result.AsReadOnly();
    }

    internal bool ConfigureForValidation(
        WorldGridDefinition definition,
        IReadOnlyList<WorldCellData> cells,
        Vector2Int start,
        IEnumerable<Vector2Int> criticalTargets,
        bool buildNavigation,
        out string reason)
    {
        reason = string.Empty;
        StopAllCoroutines();
        _rebuildRoutine = null;
        _dirtyQueue.Clear();
        _queuedSectors.Clear();
        DestroyTestAgent();
        DisposeNavigationRuntime();
        if (!_grid.TryRestoreSnapshot(definition, cells))
        {
            reason = "Validation grid snapshot was rejected.";
            return false;
        }
        _validationAnchorsConfigured = true;
        _placementGateEnabled = true;
        _criticalStart = start;
        _criticalTargets.Clear();
        _criticalTargets.AddRange((criticalTargets ?? Array.Empty<Vector2Int>()).Distinct());
        _knownBuildingFootprints.Clear();
        EnsureSubscriptions();
        return !buildNavigation || BuildAllNow(out reason);
    }

    void CreateSectorObjects()
    {
        WorldGridDefinition definition = _grid.Definition;
        int countX = Mathf.CeilToInt(definition.ChunkCountX / (float)SectorSizeChunks);
        int countZ = Mathf.CeilToInt(definition.ChunkCountZ / (float)SectorSizeChunks);
        int agentTypeId = ResolveAgentTypeId();
        for (int z = 0; z < countZ; z++)
        {
            for (int x = 0; x < countX; x++)
            {
                var coordinate = new Vector2Int(x, z);
                var root = new GameObject($"WorldNavSector_{x}_{z}")
                {
                    hideFlags = HideFlags.DontSave
                };
                root.transform.SetParent(_runtimeRoot.transform, false);
                var proxy = new GameObject("WalkableProxy")
                {
                    hideFlags = HideFlags.DontSave,
                    layer = NavigationProxyLayer
                };
                proxy.transform.SetParent(root.transform, false);
                MeshCollider collider = proxy.AddComponent<MeshCollider>();
                NavMeshSurface surface = root.AddComponent<NavMeshSurface>();
                surface.agentTypeID = agentTypeId;
                surface.collectObjects = CollectObjects.Volume;
                surface.layerMask = 1 << NavigationProxyLayer;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.ignoreNavMeshAgent = true;
                surface.ignoreNavMeshObstacle = true;
                surface.overrideVoxelSize = true;
                surface.voxelSize = 0.15f;
                surface.overrideTileSize = true;
                surface.tileSize = 128;
                surface.minRegionArea = 0.2f;
                ConfigureSurfaceVolume(surface, coordinate);
                _sectors.Add(coordinate, new SectorRuntime
                {
                    Coordinate = coordinate,
                    Root = root,
                    ProxyCollider = collider,
                    Surface = surface
                });
            }
        }
    }

    void ConfigureSurfaceVolume(NavMeshSurface surface, Vector2Int sector)
    {
        WorldGridDefinition definition = _grid.Definition;
        int sectorCells = definition.ChunkSize * SectorSizeChunks;
        int startX = sector.x * sectorCells;
        int startZ = sector.y * sectorCells;
        int endX = Mathf.Min(definition.Width, startX + sectorCells);
        int endZ = Mathf.Min(definition.Height, startZ + sectorCells);
        float overlap = definition.CellSize * SectorOverlapCells;
        float minX = definition.WorldOrigin.x + (startX - 0.5f) * definition.CellSize - overlap;
        float maxX = definition.WorldOrigin.x + (endX - 0.5f) * definition.CellSize + overlap;
        float minZ = definition.WorldOrigin.z + (startZ - 0.5f) * definition.CellSize - overlap;
        float maxZ = definition.WorldOrigin.z + (endZ - 0.5f) * definition.CellSize + overlap;
        float minY = definition.WorldOrigin.y - 2f;
        float maxY = definition.WorldOrigin.y +
                     definition.MaxElevationLevel * definition.ElevationStep + 4f;
        surface.center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f,
            (minZ + maxZ) * 0.5f);
        surface.size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
    }

    void UpdateSectorProxyMesh(SectorRuntime sector)
    {
        WorldGridDefinition definition = _grid.Definition;
        int sectorCells = definition.ChunkSize * SectorSizeChunks;
        int startX = Mathf.Max(0, sector.Coordinate.x * sectorCells - SectorOverlapCells);
        int startZ = Mathf.Max(0, sector.Coordinate.y * sectorCells - SectorOverlapCells);
        int endX = Mathf.Min(definition.Width,
            (sector.Coordinate.x + 1) * sectorCells + SectorOverlapCells);
        int endZ = Mathf.Min(definition.Height,
            (sector.Coordinate.y + 1) * sectorCells + SectorOverlapCells);
        var vertices = new List<Vector3>((endX - startX) * (endZ - startZ) * 4);
        var indices = new List<int>((endX - startX) * (endZ - startZ) * 6);
        float half = definition.CellSize * 0.5f;
        for (int z = startZ; z < endZ; z++)
        {
            for (int x = startX; x < endX; x++)
            {
                var coordinate = new Vector2Int(x, z);
                if (!_grid.TryGetCell(coordinate, out WorldCellData cell) || !cell.IsWalkable)
                    continue;
                _grid.CellToWorld(coordinate, out Vector3 center);
                center.y += 0.02f;
                int first = vertices.Count;
                vertices.Add(center + new Vector3(-half, 0f, -half));
                vertices.Add(center + new Vector3(-half, 0f, half));
                vertices.Add(center + new Vector3(half, 0f, half));
                vertices.Add(center + new Vector3(half, 0f, -half));
                indices.Add(first);
                indices.Add(first + 1);
                indices.Add(first + 2);
                indices.Add(first);
                indices.Add(first + 2);
                indices.Add(first + 3);
            }
        }

        var mesh = new Mesh
        {
            name = $"WorldNavProxy_{sector.Coordinate.x}_{sector.Coordinate.y}",
            hideFlags = HideFlags.DontSave,
            indexFormat = vertices.Count > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(indices, 0, true);
        mesh.RecalculateNormals();
        sector.ProxyCollider.sharedMesh = null;
        Mesh previous = sector.ProxyMesh;
        sector.ProxyMesh = mesh;
        sector.ProxyCollider.sharedMesh = mesh;
        ReleaseObject(previous);
    }

    IEnumerator ProcessDirtyQueue()
    {
        PauseTestAgent();
        var cycleRebuilt = new HashSet<Vector2Int>();
        var stopwatch = Stopwatch.StartNew();
        while (_dirtyQueue.Count > 0)
        {
            var batch = new List<Vector2Int>();
            while (_dirtyQueue.Count > 0)
            {
                Vector2Int coordinate = _dirtyQueue.Dequeue();
                _queuedSectors.Remove(coordinate);
                if (_sectors.ContainsKey(coordinate) && !batch.Contains(coordinate))
                    batch.Add(coordinate);
            }
            batch.Sort((left, right) => left.y != right.y
                ? left.y.CompareTo(right.y)
                : left.x.CompareTo(right.x));
            foreach (Vector2Int coordinate in batch)
                UpdateSectorProxyMesh(_sectors[coordinate]);

            foreach (Vector2Int coordinate in batch)
            {
                SectorRuntime sector = _sectors[coordinate];
                AsyncOperation operation = sector.Surface.navMeshData == null
                    ? null
                    : sector.Surface.UpdateNavMesh(sector.Surface.navMeshData);
                if (operation == null)
                {
                    sector.Surface.BuildNavMesh();
                    if (sector.Surface.navMeshData == null)
                    {
                        _lastFailure = $"Sector {coordinate} rebuild produced no NavMeshData.";
                        break;
                    }
                }
                else
                {
                    float started = Time.realtimeSinceStartup;
                    while (!operation.isDone)
                    {
                        if (Time.realtimeSinceStartup - started > RebuildTimeoutSeconds)
                        {
                            _lastFailure = $"Sector {coordinate} update exceeded {RebuildTimeoutSeconds:0}s.";
                            break;
                        }
                        yield return null;
                    }
                }
                if (!string.IsNullOrEmpty(_lastFailure)) break;
                sector.Revision++;
                cycleRebuilt.Add(coordinate);
            }
            if (!string.IsNullOrEmpty(_lastFailure)) break;
        }

        RefreshSeamLinks();
        stopwatch.Stop();
        _lastRebuildMilliseconds = stopwatch.ElapsedMilliseconds;
        _lastRebuiltSectors.Clear();
        _lastRebuiltSectors.AddRange(cycleRebuilt.OrderBy(value => value.y).ThenBy(value => value.x));
        if (cycleRebuilt.Count > 0) _navigationRevision++;
        _rebuildRoutine = null;
        if (string.IsNullOrEmpty(_lastFailure)) ResumeTestAgent();
        else _testAgentState = WorldNavigationAgentState.Unreachable;
    }

    void QueueDirtyChunks(IEnumerable<Vector2Int> dirtyChunks)
    {
        foreach (Vector2Int sector in ResolveSectorsForDirtyChunks(_grid.Definition, dirtyChunks))
            QueueSector(sector);
    }

    void QueueCells(IEnumerable<Vector2Int> cells)
    {
        if (cells == null) return;
        var chunks = new List<Vector2Int>();
        foreach (Vector2Int cell in cells.Distinct())
        {
            if (!_grid.IsValidCell(cell)) continue;
            foreach (Vector2Int chunk in WorldTerraformDirtyChunkResolver.Resolve(
                         _grid.Definition, cell))
            {
                if (!chunks.Contains(chunk)) chunks.Add(chunk);
            }
        }
        QueueDirtyChunks(chunks);
    }

    void QueueSector(Vector2Int sector)
    {
        if (!_sectors.ContainsKey(sector) || !_queuedSectors.Add(sector)) return;
        _dirtyQueue.Enqueue(sector);
        PauseTestAgent();
    }

    void OnTerraformChanged(WorldTerraformEditResult result)
    {
        if (result.Succeeded) QueueDirtyChunks(result.DirtyChunks);
    }

    void OnSurfaceChanged(WorldSurfaceEditResult result)
    {
        if (result.Succeeded) QueueDirtyChunks(result.DirtyChunks);
    }

    void OnBuildingChanged(WorldBuildingPlacementResult result)
    {
        var affected = new List<Vector2Int>();
        if (_knownBuildingFootprints.TryGetValue(result.InstanceId ?? string.Empty,
                out Vector2Int[] previous))
        {
            affected.AddRange(previous);
        }
        affected.AddRange(result.Footprint ?? Array.Empty<Vector2Int>());
        affected.Add(result.Entrance);
        if (_buildings.TryGetPlacement(result.InstanceId, out WorldPlacedBuildingRuntime current))
            _knownBuildingFootprints[result.InstanceId] = current.Footprint.ToArray();
        else
            _knownBuildingFootprints.Remove(result.InstanceId ?? string.Empty);
        QueueCells(affected);
    }

    void RefreshSeamLinks()
    {
        if (_linkRoot != null)
        {
            _linkRoot.SetActive(false);
            ReleaseObject(_linkRoot);
        }
        _linkCount = 0;
        if (_runtimeRoot == null) return;
        _linkRoot = new GameObject(LinkRootName) { hideFlags = HideFlags.DontSave };
        _linkRoot.transform.SetParent(_runtimeRoot.transform, false);

        WorldGridDefinition definition = _grid.Definition;
        int sectorCells = definition.ChunkSize * SectorSizeChunks;
        for (int rightX = sectorCells; rightX < definition.Width; rightX += sectorCells)
            CreateVerticalSeamLinks(rightX - 1, rightX);
        for (int northZ = sectorCells; northZ < definition.Height; northZ += sectorCells)
            CreateHorizontalSeamLinks(northZ - 1, northZ);
    }

    void CreateVerticalSeamLinks(int leftX, int rightX)
    {
        int runStart = -1;
        int runLevel = -1;
        for (int z = 0; z <= _grid.Definition.Height; z++)
        {
            int level = 0;
            bool valid = z < _grid.Definition.Height &&
                         TryGetLevelMatchedPair(new Vector2Int(leftX, z),
                             new Vector2Int(rightX, z), out level);
            if (valid && runStart >= 0 && level == runLevel) continue;
            if (runStart >= 0) CreateSeamLink(true, leftX, rightX, runStart, z - 1, runLevel);
            runStart = valid ? z : -1;
            runLevel = valid ? level : -1;
        }
    }

    void CreateHorizontalSeamLinks(int southZ, int northZ)
    {
        int runStart = -1;
        int runLevel = -1;
        for (int x = 0; x <= _grid.Definition.Width; x++)
        {
            int level = 0;
            bool valid = x < _grid.Definition.Width &&
                         TryGetLevelMatchedPair(new Vector2Int(x, southZ),
                             new Vector2Int(x, northZ), out level);
            if (valid && runStart >= 0 && level == runLevel) continue;
            if (runStart >= 0) CreateSeamLink(false, southZ, northZ, runStart, x - 1, runLevel);
            runStart = valid ? x : -1;
            runLevel = valid ? level : -1;
        }
    }

    bool TryGetLevelMatchedPair(Vector2Int first, Vector2Int second, out int level)
    {
        level = 0;
        if (!_grid.TryGetCell(first, out WorldCellData firstCell) || !firstCell.IsWalkable ||
            !_grid.TryGetCell(second, out WorldCellData secondCell) || !secondCell.IsWalkable ||
            firstCell.ElevationLevel != secondCell.ElevationLevel)
        {
            return false;
        }
        level = firstCell.ElevationLevel;
        return true;
    }

    void CreateSeamLink(
        bool vertical,
        int firstBoundary,
        int secondBoundary,
        int runStart,
        int runEnd,
        int level)
    {
        WorldGridDefinition definition = _grid.Definition;
        float midpoint = (runStart + runEnd) * 0.5f;
        float y = definition.WorldOrigin.y + level * definition.ElevationStep + 0.05f;
        Vector3 start;
        Vector3 end;
        if (vertical)
        {
            start = definition.WorldOrigin + new Vector3(
                firstBoundary * definition.CellSize, y - definition.WorldOrigin.y,
                midpoint * definition.CellSize);
            end = definition.WorldOrigin + new Vector3(
                secondBoundary * definition.CellSize, y - definition.WorldOrigin.y,
                midpoint * definition.CellSize);
        }
        else
        {
            start = definition.WorldOrigin + new Vector3(
                midpoint * definition.CellSize, y - definition.WorldOrigin.y,
                firstBoundary * definition.CellSize);
            end = definition.WorldOrigin + new Vector3(
                midpoint * definition.CellSize, y - definition.WorldOrigin.y,
                secondBoundary * definition.CellSize);
        }

        var linkObject = new GameObject($"SectorSeamLink_{_linkCount}")
        {
            hideFlags = HideFlags.DontSave
        };
        linkObject.SetActive(false);
        linkObject.transform.SetParent(_linkRoot.transform, false);
        NavMeshLink link = linkObject.AddComponent<NavMeshLink>();
        link.agentTypeID = ResolveAgentTypeId();
        link.startPoint = start;
        link.endPoint = end;
        link.width = Mathf.Max(definition.CellSize * 0.5f,
            (runEnd - runStart + 1) * definition.CellSize * 0.88f);
        link.bidirectional = true;
        link.costModifier = -1f;
        link.autoUpdate = false;
        linkObject.SetActive(true);
        _linkCount++;
    }

    void PauseTestAgent()
    {
        if (_testAgent == null || _testAgentState == WorldNavigationAgentState.PausedForRebuild)
            return;
        _agentHadDestinationBeforePause = _testAgentState == WorldNavigationAgentState.Moving;
        if (!_agentHadDestinationBeforePause) return;
        if (_testAgent.enabled && _testAgent.isOnNavMesh) _testAgent.isStopped = true;
        _testAgentState = WorldNavigationAgentState.PausedForRebuild;
        _agentPauseCount++;
    }

    void ResumeTestAgent()
    {
        if (_testAgent == null || !_agentHadDestinationBeforePause) return;
        _agentHadDestinationBeforePause = false;
        if (!NavMesh.SamplePosition(_testAgent.transform.position, out NavMeshHit currentHit,
                MaximumAgentReprojectionDistance, NavMesh.AllAreas))
        {
            _testAgentState = WorldNavigationAgentState.Unreachable;
            return;
        }
        _lastReprojectionDistance = Vector3.Distance(_testAgent.transform.position, currentHit.position);
        if (_lastReprojectionDistance > MaximumAgentReprojectionDistance + 0.001f ||
            !NavMesh.SamplePosition(_testDestination, out NavMeshHit destinationHit,
                MaximumAgentReprojectionDistance, NavMesh.AllAreas))
        {
            _testAgentState = WorldNavigationAgentState.Unreachable;
            return;
        }
        if (!_testAgent.isOnNavMesh && !_testAgent.Warp(currentHit.position))
        {
            _testAgentState = WorldNavigationAgentState.Unreachable;
            return;
        }

        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(currentHit.position, destinationHit.position,
                NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete ||
            !_testAgent.SetDestination(destinationHit.position))
        {
            _testAgentState = WorldNavigationAgentState.Unreachable;
            return;
        }
        _testDestination = destinationHit.position;
        _testAgent.isStopped = false;
        _testAgentState = WorldNavigationAgentState.Moving;
        _agentRepathCount++;
    }

    void UpdateTestAgentState()
    {
        if (_testAgent == null || _testAgentState != WorldNavigationAgentState.Moving) return;
        if (!_testAgent.enabled || !_testAgent.isOnNavMesh)
        {
            _testAgentState = WorldNavigationAgentState.Unreachable;
            return;
        }
        if (_testAgent.pathPending) return;
        if (_testAgent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            _testAgentState = WorldNavigationAgentState.Unreachable;
            return;
        }
        if (_testAgent.remainingDistance <= _testAgent.stoppingDistance + 0.05f &&
            _testAgent.velocity.sqrMagnitude <= 0.02f)
        {
            _testAgent.ResetPath();
            _testAgentState = WorldNavigationAgentState.Arrived;
        }
    }

    bool TryStartDefaultTestAgent()
    {
        Vector2Int start = FindFirstWalkableCell();
        if (!_grid.IsValidCell(start)) return false;
        foreach (WorldCellData candidate in _grid.Cells
                     .Where(cell => cell.IsWalkable &&
                                    cell.ElevationLevel == _grid.Cells[start.y * _grid.Definition.Width + start.x]
                                        .ElevationLevel)
                     .OrderBy(cell => Mathf.Abs(cell.Coordinate.x - start.x) +
                                      Mathf.Abs(cell.Coordinate.y - start.y)))
        {
            int distance = Mathf.Abs(candidate.Coordinate.x - start.x) +
                           Mathf.Abs(candidate.Coordinate.y - start.y);
            if (distance < 4 || distance > 12) continue;
            if (StartTestAgent(start, candidate.Coordinate, 2.4f, out _)) return true;
        }
        return false;
    }

    Vector2Int FindFirstWalkableCell()
    {
        foreach (WorldCellData cell in _grid.Cells)
            if (cell.IsWalkable) return cell.Coordinate;
        return new Vector2Int(-1, -1);
    }

    bool TrySampleCell(Vector2Int coordinate, out NavMeshHit hit)
    {
        hit = default;
        if (!_grid.CellToWorld(coordinate, out Vector3 world)) return false;
        return NavMesh.SamplePosition(world + Vector3.up * 0.25f, out hit,
            Mathf.Max(1f, _grid.Definition.CellSize), NavMesh.AllAreas);
    }

    void RefreshCriticalAnchors()
    {
        if (_validationAnchorsConfigured) return;
        _persistence ??= GetComponent<WorldPersistenceService>();
        if (_persistence == null || !_persistence.IsProceduralActive)
        {
            _placementGateEnabled = false;
            return;
        }
        if (_placementGateEnabled && _criticalSeed == _persistence.ActiveSeed) return;

        WorldGenerationResult generated = WorldIslandGenerator.Generate(_persistence.ActiveSeed);
        if (!generated.TryGetAnchor(WorldGenerationAnchorKind.Start,
                out WorldGenerationAnchor start))
        {
            _placementGateEnabled = false;
            return;
        }
        _criticalStart = start.Coordinate;
        _criticalTargets.Clear();
        foreach (WorldGenerationAnchor anchor in generated.Anchors)
        {
            Vector2Int target = anchor.Kind == WorldGenerationAnchorKind.Shop
                ? anchor.EntranceCoordinate
                : anchor.Coordinate;
            if (!_criticalTargets.Contains(target)) _criticalTargets.Add(target);
        }
        _criticalSeed = generated.Seed;
        _placementGateEnabled = true;
    }

    void ResolveDependencies()
    {
        if (_grid == null) _grid = GetComponent<WorldGridService>();
        if (_buildings == null) _buildings = GetComponent<WorldBuildingPlacementService>();
        if (_persistence == null) _persistence = GetComponent<WorldPersistenceService>();
        if (_grid == null || _buildings == null)
            throw new InvalidOperationException("World navigation requires grid and building services.");
    }

    void EnsureSubscriptions()
    {
        ResolveDependencies();
        WorldTerraformService terraform = _grid.Terraform;
        if (!ReferenceEquals(_subscribedTerraform, terraform))
        {
            if (_subscribedTerraform != null) _subscribedTerraform.Changed -= OnTerraformChanged;
            _subscribedTerraform = terraform;
            _subscribedTerraform.Changed += OnTerraformChanged;
        }
        WorldSurfaceEditService surface = _grid.SurfaceEditor;
        if (!ReferenceEquals(_subscribedSurface, surface))
        {
            if (_subscribedSurface != null) _subscribedSurface.Changed -= OnSurfaceChanged;
            _subscribedSurface = surface;
            _subscribedSurface.Changed += OnSurfaceChanged;
        }
        if (!_buildingSubscribed)
        {
            _buildings.Changed += OnBuildingChanged;
            _buildingSubscribed = true;
        }
    }

    void Unsubscribe()
    {
        if (_subscribedTerraform != null) _subscribedTerraform.Changed -= OnTerraformChanged;
        if (_subscribedSurface != null) _subscribedSurface.Changed -= OnSurfaceChanged;
        _subscribedTerraform = null;
        _subscribedSurface = null;
        if (_buildingSubscribed && _buildings != null) _buildings.Changed -= OnBuildingChanged;
        _buildingSubscribed = false;
    }

    void StopQueuedRebuild()
    {
        if (_rebuildRoutine != null) StopCoroutine(_rebuildRoutine);
        _rebuildRoutine = null;
        _dirtyQueue.Clear();
        _queuedSectors.Clear();
        _lastRebuiltSectors.Clear();
        _lastFailure = string.Empty;
    }

    void DisposeNavigationRuntime()
    {
        DestroyTestAgent();
        foreach (SectorRuntime sector in _sectors.Values)
        {
            if (sector.Root != null) sector.Root.SetActive(false);
            ReleaseObject(sector.ProxyMesh);
        }
        _sectors.Clear();
        if (_linkRoot != null) _linkRoot.SetActive(false);
        if (_runtimeRoot != null) _runtimeRoot.SetActive(false);
        ReleaseObject(_runtimeRoot);
        _runtimeRoot = null;
        _linkRoot = null;
        _linkCount = 0;
        _activeDefinition = null;
    }

    void DestroyTestAgent()
    {
        if (_testAgent != null) ReleaseObject(_testAgent.gameObject);
        _testAgent = null;
        _testAgentState = WorldNavigationAgentState.Idle;
        _agentHadDestinationBeforePause = false;
    }

    static int ResolveAgentTypeId()
    {
        return NavMesh.GetSettingsCount() > 0
            ? NavMesh.GetSettingsByIndex(0).agentTypeID
            : 0;
    }

    static void ReleaseObject(UnityEngine.Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}

#if UNITY_EDITOR
public static class PA_WorldNavigationTools
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.WORLD008.Active";
    const string FailedKey = "PA.WORLD008.Failed";
    const string ConsoleErrorKey = "PA.WORLD008.ConsoleErrors";
    const string WaitFramesKey = "PA.WORLD008.WaitFrames";
    const string StageKey = "PA.WORLD008.Stage";

    static WorldNavigationService _navigation;
    static WorldGridService _grid;
    static WorldBuildingPlacementService _buildings;
    static Vector3 _agentStartPosition;
    static Vector3 _arrivalPosition;
    static float _stageStarted;
    static int _sectorZeroRevision;
    static int _sectorOneRevision;

    [InitializeOnLoadMethod]
    static void ResumeAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        SubscribeCallbacks();
        if (EditorApplication.isPlaying) EditorApplication.update += ValidateRuntime;
    }

    [MenuItem("Project PA/World/WORLD-008/Validate Reachability And Navigation")]
    public static void RunWorld008Validation()
    {
        RunWorld008ValidationInternal();
    }

    public static void RunWorld008ValidationBatch()
    {
        RunWorld008ValidationInternal();
    }

    static void RunWorld008ValidationInternal()
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
            Require(scene.IsValid() && scene.isLoaded && !scene.isDirty,
                "WorldSandbox opens clean for navigation validation");
            ValidateLogicalGraphAndSectorMapping();
            Debug.Log("[WORLD-008] EDIT_MODE_PASS graph=true isolated=true sectors=true");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void ValidateLogicalGraphAndSectorMapping()
    {
        WorldGenerationResult generated = WorldIslandGenerator.Generate(8008L);
        Require(generated.TryGetAnchor(WorldGenerationAnchorKind.Start,
                out WorldGenerationAnchor start), "generated start anchor exists");
        Vector2Int[] targets = generated.Anchors.Select(anchor =>
            anchor.Kind == WorldGenerationAnchorKind.Shop
                ? anchor.EntranceCoordinate
                : anchor.Coordinate).ToArray();
        Require(WorldCellReachability.CanReachAll(generated.Definition,
                generated.TerrainCells, start.Coordinate, targets, null, null,
                out int reachable) && reachable > 64,
            $"logical graph reaches all generated critical anchors ({reachable} cells)");

        WorldGridDefinition isolatedDefinition = CreateDefinition(8, 8);
        WorldCellData[] isolated = CreateFlatCells(isolatedDefinition);
        for (int x = 0; x < isolatedDefinition.Width; x++)
            isolated[4 * isolatedDefinition.Width + x] = WaterCell(new Vector2Int(x, 4));
        Require(!WorldCellReachability.CanReachAll(isolatedDefinition, isolated,
                new Vector2Int(1, 1), new[] { new Vector2Int(6, 6) }, null, null, out _),
            "a complete water barrier is reported as an isolated region");

        WorldGridDefinition sectorDefinition = CreateDefinition(64, 32);
        IReadOnlyList<Vector2Int> interior = WorldNavigationService.ResolveSectorsForDirtyChunks(
            sectorDefinition, new[] { new Vector2Int(0, 0) });
        IReadOnlyList<Vector2Int> seam = WorldNavigationService.ResolveSectorsForDirtyChunks(
            sectorDefinition, new[] { new Vector2Int(1, 0), new Vector2Int(2, 0) });
        Require(interior.Count == 1 && interior[0] == Vector2Int.zero &&
                seam.Count == 2 && seam[0] == Vector2Int.zero && seam[1] == Vector2Int.right,
            "dirty chunk mapping selects only the owner sector and a seam neighbor");
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
        if (stage == 0 && frames < 5) return;

        try
        {
            if (stage == 0)
            {
                Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
                    $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
                _navigation = UnityEngine.Object.FindFirstObjectByType<WorldNavigationService>();
                Require(_navigation != null, "WorldSandbox bootstraps one navigation service");
                _grid = _navigation.GetComponent<WorldGridService>();
                _buildings = _navigation.GetComponent<WorldBuildingPlacementService>();
                ValidateBuildingReachabilityGate();
                BuildFlatSectorRuntime();
                _agentStartPosition = _navigation.TestAgent.transform.position;
                _sectorZeroRevision = _navigation.GetSectorRevision(Vector2Int.zero);
                _sectorOneRevision = _navigation.GetSectorRevision(Vector2Int.right);
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(1);
                return;
            }

            if (stage == 1)
            {
                if (Time.realtimeSinceStartup - _stageStarted < 0.2f ||
                    Vector3.Distance(_agentStartPosition, _navigation.TestAgent.transform.position) < 0.1f)
                {
                    if (Time.realtimeSinceStartup - _stageStarted >= 4f)
                        throw new InvalidOperationException(
                            "Test NPC did not begin moving within 4 seconds.");
                    return;
                }
                Require(_grid.Terraform.Raise(new Vector2Int(8, 8)).Succeeded,
                    "an interior cell edit commits while the NPC is moving");
                Require(_navigation.PendingSectorCount == 1 &&
                        _navigation.TestAgentState == WorldNavigationAgentState.PausedForRebuild,
                    "the affected test NPC pauses while exactly one sector is queued");
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(2);
                return;
            }

            if (stage == 2)
            {
                if (_navigation.IsRebuilding || _navigation.PendingSectorCount > 0)
                {
                    if (Time.realtimeSinceStartup - _stageStarted >= 10f)
                        throw new TimeoutException("Interior sector update exceeded 10 seconds.");
                    return;
                }
                Require(string.IsNullOrEmpty(_navigation.LastFailure),
                    $"interior async update has no failure ({_navigation.LastFailure})");
                Require(_navigation.GetSectorRevision(Vector2Int.zero) == _sectorZeroRevision + 1 &&
                        _navigation.GetSectorRevision(Vector2Int.right) == _sectorOneRevision &&
                        _navigation.LastRebuiltSectors.SequenceEqual(new[] { Vector2Int.zero }),
                    "an interior edit rebuilds only its owning sector");
                Require(_navigation.TestAgentPauseCount >= 1 &&
                        _navigation.TestAgentRepathCount >= 1 &&
                        _navigation.LastAgentReprojectionDistance <=
                        WorldNavigationService.MaximumAgentReprojectionDistance + 0.001f &&
                        (_navigation.TestAgentState == WorldNavigationAgentState.Moving ||
                         _navigation.TestAgentState == WorldNavigationAgentState.Arrived),
                    "the affected agent reprojects within limit and resumes one complete route");
                _sectorZeroRevision = _navigation.GetSectorRevision(Vector2Int.zero);
                _sectorOneRevision = _navigation.GetSectorRevision(Vector2Int.right);
                Require(_grid.Terraform.Raise(new Vector2Int(31, 8)).Succeeded,
                    "a sector-boundary edit commits");
                Require(_navigation.PendingSectorCount == 2,
                    "a boundary edit queues the owner and seam-sharing neighbor only");
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(3);
                return;
            }

            if (stage == 3)
            {
                if (_navigation.IsRebuilding || _navigation.PendingSectorCount > 0)
                {
                    if (Time.realtimeSinceStartup - _stageStarted >= 10f)
                        throw new TimeoutException("Boundary sector updates exceeded 10 seconds.");
                    return;
                }
                Require(string.IsNullOrEmpty(_navigation.LastFailure) &&
                        _navigation.GetSectorRevision(Vector2Int.zero) == _sectorZeroRevision + 1 &&
                        _navigation.GetSectorRevision(Vector2Int.right) == _sectorOneRevision + 1 &&
                        _navigation.LastRebuiltSectors.Count == 2,
                    "a boundary edit updates exactly two local sector surfaces");
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(4);
                return;
            }

            if (stage == 4)
            {
                if (_navigation.TestAgentState != WorldNavigationAgentState.Arrived)
                {
                    if (_navigation.TestAgentState == WorldNavigationAgentState.Unreachable ||
                        Time.realtimeSinceStartup - _stageStarted >= 8f)
                    {
                        throw new InvalidOperationException(
                            "Test NPC did not reach its seam-crossing destination within 8 seconds.");
                    }
                    return;
                }
                _arrivalPosition = _navigation.TestAgent.transform.position;
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(5);
                return;
            }

            if (Time.realtimeSinceStartup - _stageStarted < 0.25f) return;
            EditorApplication.update -= ValidateRuntime;
            Require(_navigation.TestAgentState == WorldNavigationAgentState.Arrived &&
                    Vector3.Distance(_arrivalPosition, _navigation.TestAgent.transform.position) < 0.03f,
                "arrived test NPC settles without destination jitter");
            Require(_navigation.SectorCount == 2 && _navigation.LinkCount > 0 &&
                    _navigation.InitialBuildMilliseconds < 10000 &&
                    _navigation.LastRebuildMilliseconds < 10000,
                "two sector surfaces, seam links and bounded build timings remain active");
            ValidateGeneratedIslandRuntime();
            Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                "blocking runtime Console Error/Exception/Assert count is 0");
            Debug.Log("[WORLD-008] PLAY_MODE_PASS localSectors=2 generatedSectors=16 " +
                      "seam=true async=true buildingGate=true agentPause=true " +
                      "repath=true settled=true");
            EditorApplication.ExitPlaymode();
        }
        catch (Exception ex)
        {
            EditorApplication.update -= ValidateRuntime;
            Fail(ex);
        }
    }

    static void ValidateBuildingReachabilityGate()
    {
        WorldGridDefinition definition = CreateDefinition(32, 16);
        WorldCellData[] cells = new WorldCellData[definition.TotalCellCount];
        for (int z = 0; z < definition.Height; z++)
        {
            for (int x = 0; x < definition.Width; x++)
                cells[z * definition.Width + x] = WaterCell(new Vector2Int(x, z));
        }
        for (int x = 0; x < definition.Width; x++)
            cells[6 * definition.Width + x] = DryCell(new Vector2Int(x, 6));
        for (int z = 7; z <= 9; z++)
            for (int x = 1; x <= 4; x++)
                cells[z * definition.Width + x] = DryCell(new Vector2Int(x, z));
        for (int z = 6; z <= 8; z++)
            for (int x = 12; x <= 15; x++)
                cells[z * definition.Width + x] = DryCell(new Vector2Int(x, z));
        cells[5 * definition.Width + 13] = DryCell(new Vector2Int(13, 5));

        var start = new Vector2Int(0, 6);
        var target = new Vector2Int(31, 6);
        Require(_navigation.ConfigureForValidation(definition, cells, start,
                new[] { target }, false, out string configureReason),
            $"corridor validation grid configures ({configureReason})");
        WorldBuildingPlacementResult placed = _buildings.TryPlace(
            WorldBuildingPlacementService.PrototypeInstanceId, new Vector2Int(1, 7), 0);
        Require(placed.Succeeded && _buildings.RegisteredCount == 1,
            "a building beside the protected corridor places successfully");
        WorldBuildingPlacementResult blockedMove = _buildings.TryMove(
            WorldBuildingPlacementService.PrototypeInstanceId, new Vector2Int(12, 6), 0);
        Require(!blockedMove.Succeeded &&
                blockedMove.Failure == WorldBuildingPlacementFailure.CriticalRouteBlocked &&
                _buildings.TryGetPlacement(WorldBuildingPlacementService.PrototypeInstanceId,
                    out WorldPlacedBuildingRuntime unchanged) &&
                unchanged.Anchor == new Vector2Int(1, 7),
            "a move that isolates the critical target and entrance is rejected before mutation");
        Require(_buildings.TryRemove(WorldBuildingPlacementService.PrototypeInstanceId).Succeeded &&
                _buildings.RegisteredCount == 0,
            "building reachability fixture cleans up without registry residue");
    }

    static void BuildFlatSectorRuntime()
    {
        WorldGridDefinition definition = CreateDefinition(64, 32);
        WorldCellData[] cells = CreateFlatCells(definition);
        var start = new Vector2Int(28, 4);
        var destination = new Vector2Int(36, 4);
        Require(_navigation.ConfigureForValidation(definition, cells, start,
                new[] { destination }, true, out string buildReason),
            $"two-sector runtime navigation builds ({buildReason})");
        Require(_navigation.SectorCount == 2 && _navigation.LinkCount > 0 &&
                _navigation.InitialBuildMilliseconds < 10000,
            $"two local NavMeshSurface sectors and seam links build in budget " +
            $"({_navigation.InitialBuildMilliseconds}ms)");
        _grid.CellToWorld(start, out Vector3 startWorld);
        _grid.CellToWorld(destination, out Vector3 destinationWorld);
        bool hasStart = NavMesh.SamplePosition(startWorld + Vector3.up * 0.25f,
            out NavMeshHit startHit, 2f, NavMesh.AllAreas);
        bool hasDestination = NavMesh.SamplePosition(destinationWorld + Vector3.up * 0.25f,
            out NavMeshHit destinationHit, 2f, NavMesh.AllAreas);
        Require(hasStart && hasDestination,
            "both sides of the sector seam project onto NavMesh");
        var path = new NavMeshPath();
        Require(NavMesh.CalculatePath(startHit.position, destinationHit.position,
                    NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete,
            "a complete runtime path crosses the sector seam");
        Require(_navigation.StartTestAgent(start, destination, 8f, out string agentReason),
            $"one runtime NPC test agent accepts the seam destination ({agentReason})");
    }

    static void ValidateGeneratedIslandRuntime()
    {
        WorldGenerationResult generated = WorldIslandGenerator.Generate(8008L);
        bool hasStart = generated.TryGetAnchor(WorldGenerationAnchorKind.Start,
            out WorldGenerationAnchor start);
        bool hasShop = generated.TryGetAnchor(WorldGenerationAnchorKind.Shop,
            out WorldGenerationAnchor shop);
        Require(hasStart && hasShop,
            "generated runtime has start and shop anchors");
        Vector2Int[] targets = generated.Anchors.Select(anchor =>
            anchor.Kind == WorldGenerationAnchorKind.Shop
                ? anchor.EntranceCoordinate
                : anchor.Coordinate).ToArray();
        Require(_navigation.ConfigureForValidation(generated.Definition,
                generated.TerrainCells, start.Coordinate, targets, true,
                out string buildReason),
            $"provisional 128x128 generated navigation builds ({buildReason})");
        Require(_navigation.SectorCount == 16 && _navigation.LinkCount > 0 &&
                _navigation.InitialBuildMilliseconds < 10000,
            $"128x128 world uses 4x4 local sectors within budget " +
            $"({_navigation.InitialBuildMilliseconds}ms)");
        _grid.CellToWorld(start.Coordinate, out Vector3 startWorld);
        _grid.CellToWorld(shop.EntranceCoordinate, out Vector3 shopWorld);
        bool startSampled = NavMesh.SamplePosition(startWorld + Vector3.up * 0.25f,
            out NavMeshHit startHit, 2f, NavMesh.AllAreas);
        bool shopSampled = NavMesh.SamplePosition(shopWorld + Vector3.up * 0.25f,
            out NavMeshHit shopHit, 2f, NavMesh.AllAreas);
        Require(startSampled && shopSampled,
            "generated start and shop entrance project onto sector NavMesh");
        var path = new NavMeshPath();
        Require(NavMesh.CalculatePath(startHit.position, shopHit.position,
                    NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete,
            "generated start reaches the shop entrance on runtime NavMesh");
    }

    static WorldGridDefinition CreateDefinition(int width, int height)
    {
        return new WorldGridDefinition(2f, width, height, 16, 1f, 0, 6, Vector3.zero);
    }

    static WorldCellData[] CreateFlatCells(WorldGridDefinition definition)
    {
        var cells = new WorldCellData[definition.TotalCellCount];
        for (int z = 0; z < definition.Height; z++)
            for (int x = 0; x < definition.Width; x++)
                cells[z * definition.Width + x] = DryCell(new Vector2Int(x, z));
        return cells;
    }

    static WorldCellData DryCell(Vector2Int coordinate)
    {
        return new WorldCellData(coordinate, 0, WorldGroundType.Default,
            WorldPathType.None, 0, 0, WorldCellOccupancy.Empty);
    }

    static WorldCellData WaterCell(Vector2Int coordinate)
    {
        return new WorldCellData(coordinate, 0, WorldGroundType.Default,
            WorldPathType.None, 1, 1, WorldCellOccupancy.Empty);
    }

    static void SetStage(int stage)
    {
        SessionState.SetInt(StageKey, stage);
        SessionState.SetInt(WaitFramesKey, 0);
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
        Debug.LogError($"[WORLD-008] FAIL {ex.Message}\n{ex}");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else FinishValidation();
    }

    static void FinishValidation()
    {
        EditorApplication.update -= ValidateRuntime;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        bool failed = SessionState.GetBool(FailedKey, false);
        int consoleErrors = SessionState.GetInt(ConsoleErrorKey, 0);
        SessionState.SetBool(ActiveKey, false);
        SessionState.SetBool(FailedKey, false);
        SessionState.SetInt(StageKey, 0);
        SessionState.SetInt(WaitFramesKey, 0);
        if (failed)
        {
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        Debug.Log($"[WORLD-008] FINISHED_PASS graph=true isolated=true entrance=true " +
                  $"localSectors=2 generatedSectors=16 seam=true async=true npc=true " +
                  $"console={consoleErrors}");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[WORLD-008] PASS {message}");
    }
}
#endif
