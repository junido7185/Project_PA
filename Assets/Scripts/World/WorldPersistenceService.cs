using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public static class WorldPersistenceMigration
{
    public const int AdditiveWorldSaveVersion = 11;
    public const string LegacyFixedMode = "LegacyFixed";
    public const string ProceduralMode = "Procedural";

    public static WorldStateSaveData CreateLegacyFixed()
    {
        return new WorldStateSaveData
        {
            worldMode = LegacyFixedMode,
            worldSeed = 0L,
            generationVersion = 0,
            modifiedCells = new List<WorldModifiedCellSaveData>(),
            placedBuildings = new List<WorldPlacedBuildingSaveData>(),
            shopFurniture = new List<WorldShopFurnitureSaveData>(),
            resourceStates = new List<WorldResourceStateSaveData>(),
            hasSafePlayerPosition = false
        };
    }

    public static void UpgradeV10ToV11(SaveData data)
    {
        if (data == null) return;
        data.worldState = CreateLegacyFixed();
        data.version = AdditiveWorldSaveVersion;
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(WorldGridService), typeof(WorldBuildingPlacementService))]
public sealed class WorldPersistenceService : MonoBehaviour
{
    readonly List<WorldResourceStateSaveData> _resourceStates =
        new List<WorldResourceStateSaveData>();
    readonly List<WorldShopFurnitureSaveData> _shopFurniture =
        new List<WorldShopFurnitureSaveData>();

    WorldGridService _grid;
    WorldBuildingPlacementService _buildings;
    WorldGenerationResult _baseWorld;

    public static WorldPersistenceService Instance { get; private set; }
    public bool IsProceduralActive => _baseWorld != null;
    public long ActiveSeed => _baseWorld != null ? _baseWorld.Seed : 0L;
    public int ActiveGenerationVersion => _baseWorld != null ? _baseWorld.GenerationVersion : 0;
    public int CurrentSparseDeltaCount => IsProceduralActive
        ? BuildCellDeltas().Count
        : 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapWorldSandbox()
    {
        if (SceneManager.GetActiveScene().name != "WorldSandbox" ||
            FindFirstObjectByType<WorldPersistenceService>() != null)
        {
            return;
        }

        WorldGridService grid = FindFirstObjectByType<WorldGridService>();
        if (grid != null) grid.gameObject.AddComponent<WorldPersistenceService>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        ResolveDependencies();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool StartProceduralWorld(long seed, out string reason)
    {
        reason = string.Empty;
        if (!Application.isPlaying)
        {
            reason = "Procedural world restore is runtime-only.";
            return false;
        }

        ResolveDependencies();
        WorldGenerationResult generated = WorldIslandGenerator.Generate(
            seed, WorldIslandGenerationSettings.Provisional128);
        if (!RemovePrototypeBuilding(out reason)) return false;
        if (!_grid.TryRestoreSnapshot(generated.Definition, generated.TerrainCells))
        {
            reason = "Generated base cells failed grid validation.";
            return false;
        }

        _baseWorld = generated;
        _resourceStates.Clear();
        _shopFurniture.Clear();
        RefreshTerrainProjection();
        return true;
    }

    public bool SetResourceState(string spawnKey, bool consumed, int respawnDay)
    {
        if (!IsProceduralActive || string.IsNullOrWhiteSpace(spawnKey) || respawnDay < 0 ||
            !_baseWorld.ResourceSpawns.Any(spawn => spawn.SpawnKey == spawnKey))
        {
            return false;
        }

        WorldResourceStateSaveData state = _resourceStates.FirstOrDefault(
            entry => entry.spawnKey == spawnKey);
        if (state == null)
        {
            state = new WorldResourceStateSaveData { spawnKey = spawnKey };
            _resourceStates.Add(state);
        }
        state.consumed = consumed;
        state.respawnDay = respawnDay;
        return true;
    }

    public WorldStateSaveData CaptureState(
        Vector3 requestedPlayerPosition,
        IReadOnlyList<PlaceableSaveData> shopFurniture)
    {
        if (!IsProceduralActive)
            return WorldPersistenceMigration.CreateLegacyFixed();

        ResolveDependencies();
        Vector3 safePosition = ResolveSafePlayerPosition(requestedPlayerPosition,
            out Vector2Int safeCell);
        var state = new WorldStateSaveData
        {
            worldMode = WorldPersistenceMigration.ProceduralMode,
            worldSeed = _baseWorld.Seed,
            generationVersion = _baseWorld.GenerationVersion,
            widthCells = _baseWorld.Definition.Width,
            heightCells = _baseWorld.Definition.Height,
            cellSizeMeters = _baseWorld.Definition.CellSize,
            chunkSizeCells = _baseWorld.Definition.ChunkSize,
            modifiedCells = BuildCellDeltas(),
            placedBuildings = CaptureBuildings(),
            shopFurniture = ProjectShopFurniture(shopFurniture),
            resourceStates = CloneResourceStates(_resourceStates),
            hasSafePlayerPosition = true,
            safePlayerPosition = safePosition,
            safePlayerCellX = safeCell.x,
            safePlayerCellZ = safeCell.y
        };
        return state;
    }

    public bool TryRestore(
        WorldStateSaveData state,
        out Vector3 safePlayerPosition,
        out List<PlaceableSaveData> shopFurniture,
        out string reason)
    {
        safePlayerPosition = Vector3.zero;
        shopFurniture = new List<PlaceableSaveData>();
        reason = string.Empty;
        if (!Application.isPlaying)
        {
            reason = "Procedural world restore is runtime-only.";
            return false;
        }
        if (!TryBuildCandidate(state, out WorldGenerationResult generated,
                out WorldCellData[] candidateCells, out reason))
        {
            return false;
        }

        ResolveDependencies();
        if (!RemovePrototypeBuilding(out reason)) return false;
        if (!_grid.TryRestoreSnapshot(generated.Definition, candidateCells))
        {
            reason = "Validated world snapshot could not be installed.";
            return false;
        }

        _baseWorld = generated;
        _resourceStates.Clear();
        _resourceStates.AddRange(CloneResourceStates(state.resourceStates));
        _shopFurniture.Clear();
        _shopFurniture.AddRange(CloneFurniture(state.shopFurniture));

        foreach (WorldPlacedBuildingSaveData building in state.placedBuildings)
        {
            WorldBuildingPlacementResult result = _buildings.TryPlace(
                building.instanceId,
                new Vector2Int(building.anchorX, building.anchorZ),
                building.rotationQuarterTurns);
            if (!result.Succeeded)
            {
                reason = $"Validated building restore failed: {result.Failure}.";
                return false;
            }
        }

        safePlayerPosition = ResolveRestoredSafePlayerPosition(state);
        shopFurniture = ExpandShopFurniture(_shopFurniture);
        RefreshTerrainProjection();
        return true;
    }

    public ulong ComputeCurrentChecksum()
    {
        if (!IsProceduralActive) return 0UL;
        WorldStateSaveData state = CaptureState(
            ResolveSafePlayerPosition(Vector3.zero, out _),
            ExpandShopFurniture(_shopFurniture));
        return ComputePayloadChecksum(state);
    }

    public static ulong ComputePayloadChecksum(WorldStateSaveData state)
    {
        const ulong offset = 1469598103934665603UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        void Add(long value)
        {
            unchecked
            {
                hash = (hash ^ (ulong)value) * prime;
            }
        }
        void AddString(string value)
        {
            if (value == null) { Add(-1); return; }
            foreach (char character in value) Add(character);
            Add(0);
        }

        if (state == null) return 0UL;
        AddString(state.worldMode);
        Add(state.worldSeed);
        Add(state.generationVersion);
        foreach (WorldModifiedCellSaveData cell in (state.modifiedCells ?? new List<WorldModifiedCellSaveData>())
                     .Where(cell => cell != null).OrderBy(cell => cell.z).ThenBy(cell => cell.x))
        {
            Add(cell.x); Add(cell.z); Add(cell.elevationLevel); Add(cell.groundType);
            Add(cell.pathType); Add(cell.waterSurfaceLevel); Add(cell.waterDepthLevels);
        }
        foreach (WorldPlacedBuildingSaveData building in (state.placedBuildings ?? new List<WorldPlacedBuildingSaveData>())
                     .Where(building => building != null).OrderBy(building => building.instanceId))
        {
            AddString(building.instanceId); AddString(building.buildingId);
            Add(building.anchorX); Add(building.anchorZ); Add(building.rotationQuarterTurns);
        }
        foreach (WorldShopFurnitureSaveData furniture in (state.shopFurniture ?? new List<WorldShopFurnitureSaveData>())
                     .Where(furniture => furniture != null).OrderBy(furniture => furniture.instanceId))
        {
            AddString(furniture.zoneId); AddString(furniture.definitionId);
            AddString(furniture.instanceId); Add(furniture.gridX); Add(furniture.gridY);
            Add(furniture.rotationQuarterTurns); Add(furniture.recovered ? 1 : 0);
        }
        foreach (WorldResourceStateSaveData resource in (state.resourceStates ?? new List<WorldResourceStateSaveData>())
                     .Where(resource => resource != null).OrderBy(resource => resource.spawnKey))
        {
            AddString(resource.spawnKey); Add(resource.consumed ? 1 : 0); Add(resource.respawnDay);
        }
        return hash;
    }

    internal static bool TryBuildCandidate(
        WorldStateSaveData state,
        out WorldGenerationResult generated,
        out WorldCellData[] cells,
        out string reason)
    {
        generated = null;
        cells = null;
        reason = string.Empty;
        if (state == null || state.worldMode != WorldPersistenceMigration.ProceduralMode)
        {
            reason = "World payload is not Procedural.";
            return false;
        }
        if (state.generationVersion != WorldIslandGenerationSettings.CurrentGenerationVersion)
        {
            reason = "Missing or unsupported generationVersion.";
            return false;
        }

        WorldIslandGenerationSettings settings = WorldIslandGenerationSettings.Provisional128;
        if (state.widthCells != settings.WidthCells ||
            state.heightCells != settings.HeightCells ||
            Mathf.Abs(state.cellSizeMeters - settings.CellSize) > 0.001f ||
            state.chunkSizeCells != settings.ChunkSize)
        {
            reason = "Saved WorldDefinition does not match its generationVersion.";
            return false;
        }

        generated = WorldIslandGenerator.Generate(state.worldSeed, settings);
        cells = generated.TerrainCells.Select(cell => cell.WithOccupancy(
            WorldCellOccupancy.Empty)).ToArray();
        List<WorldModifiedCellSaveData> deltas = state.modifiedCells ??
                                                 new List<WorldModifiedCellSaveData>();
        if (deltas.Count > generated.Definition.TotalCellCount / 2)
        {
            reason = "Modified cell payload is no longer sparse.";
            return false;
        }

        var coordinates = new HashSet<Vector2Int>();
        foreach (WorldModifiedCellSaveData delta in deltas)
        {
            if (delta == null)
            {
                reason = "Null modified cell record.";
                return false;
            }
            var coordinate = new Vector2Int(delta.x, delta.z);
            if (!coordinates.Add(coordinate) ||
                coordinate.x < 0 || coordinate.x >= generated.Definition.Width ||
                coordinate.y < 0 || coordinate.y >= generated.Definition.Height ||
                delta.elevationLevel < generated.Definition.MinElevationLevel ||
                delta.elevationLevel > generated.Definition.MaxElevationLevel ||
                !Enum.IsDefined(typeof(WorldGroundType), delta.groundType) ||
                !Enum.IsDefined(typeof(WorldPathType), delta.pathType) ||
                delta.waterDepthLevels < 0 ||
                (delta.waterDepthLevels == 0 &&
                 delta.waterSurfaceLevel != delta.elevationLevel) ||
                (delta.waterDepthLevels > 0 &&
                 (delta.waterSurfaceLevel <= delta.elevationLevel ||
                  delta.waterSurfaceLevel > generated.Definition.MaxElevationLevel ||
                  delta.pathType != (int)WorldPathType.None)))
            {
                reason = $"Invalid modified cell at ({delta.x},{delta.z}).";
                return false;
            }
            int index = coordinate.y * generated.Definition.Width + coordinate.x;
            cells[index] = new WorldCellData(
                coordinate,
                delta.elevationLevel,
                (WorldGroundType)delta.groundType,
                (WorldPathType)delta.pathType,
                delta.waterSurfaceLevel,
                delta.waterDepthLevels,
                WorldCellOccupancy.Empty);
        }

        if (!ValidateBuildings(state.placedBuildings, generated.Definition, cells, out reason) ||
            !ValidateFurniture(state.shopFurniture, out reason) ||
            !ValidateResources(state.resourceStates, generated, out reason))
        {
            return false;
        }
        return true;
    }

    static bool ValidateBuildings(
        List<WorldPlacedBuildingSaveData> records,
        WorldGridDefinition definition,
        IReadOnlyList<WorldCellData> cells,
        out string reason)
    {
        reason = string.Empty;
        records ??= new List<WorldPlacedBuildingSaveData>();
        if (records.Count > 1)
        {
            reason = "WORLD-007 MVP supports one persisted B09 building.";
            return false;
        }
        if (records.Count == 0) return true;

        WorldPlacedBuildingSaveData record = records[0];
        if (record == null ||
            record.instanceId != WorldBuildingPlacementService.PrototypeInstanceId ||
            record.buildingId != WorldBuildingPlacementService.StorageShedDefinition.StableId)
        {
            reason = "Unsupported or unstable building identity.";
            return false;
        }
        BuildingData data = Resources.Load<BuildingData>(
            WorldBuildingPlacementService.StorageShedDefinition.BuildingResourcePath);
        if (data == null || data.prefab == null)
        {
            reason = "Persisted building prefab is unavailable.";
            return false;
        }

        var anchor = new Vector2Int(record.anchorX, record.anchorZ);
        Vector2Int[] footprint = WorldBuildingPlacementService.StorageShedDefinition
            .ResolveFootprint(anchor, record.rotationQuarterTurns);
        Vector2Int entrance = WorldBuildingPlacementService.StorageShedDefinition
            .ResolveEntrance(anchor, record.rotationQuarterTurns);
        int? flat = null;
        foreach (Vector2Int coordinate in footprint)
        {
            if (!TryGetCell(definition, cells, coordinate, out WorldCellData cell) ||
                cell.HasWater || cell.HasPath || cell.Occupancy != WorldCellOccupancy.Empty ||
                (cell.GroundType != WorldGroundType.Default &&
                 cell.GroundType != WorldGroundType.Soil))
            {
                reason = "Persisted building footprint is invalid.";
                return false;
            }
            flat ??= cell.ElevationLevel;
            if (flat.Value != cell.ElevationLevel)
            {
                reason = "Persisted building footprint is uneven.";
                return false;
            }
        }
        if (!TryGetCell(definition, cells, entrance, out WorldCellData entranceCell) ||
            footprint.Contains(entrance) || entranceCell.HasWater || entranceCell.HasPath ||
            !flat.HasValue || Mathf.Abs(entranceCell.ElevationLevel - flat.Value) > 1)
        {
            reason = "Persisted building entrance is blocked.";
            return false;
        }
        return true;
    }

    static bool ValidateFurniture(
        List<WorldShopFurnitureSaveData> records,
        out string reason)
    {
        reason = string.Empty;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldShopFurnitureSaveData record in records ??
                     new List<WorldShopFurnitureSaveData>())
        {
            if (record == null || record.zoneId != ShopCustomizationController.ShopInteriorZoneId ||
                string.IsNullOrWhiteSpace(record.definitionId) ||
                string.IsNullOrWhiteSpace(record.instanceId) ||
                !ids.Add(record.instanceId) ||
                record.rotationQuarterTurns < 0 || record.rotationQuarterTurns > 3)
            {
                reason = "Invalid shop furniture projection.";
                return false;
            }
        }
        return true;
    }

    static bool ValidateResources(
        List<WorldResourceStateSaveData> records,
        WorldGenerationResult generated,
        out string reason)
    {
        reason = string.Empty;
        var valid = new HashSet<string>(generated.ResourceSpawns.Select(spawn => spawn.SpawnKey));
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldResourceStateSaveData record in records ??
                     new List<WorldResourceStateSaveData>())
        {
            if (record == null || !valid.Contains(record.spawnKey) ||
                !ids.Add(record.spawnKey) || record.respawnDay < 0)
            {
                reason = "Invalid resource state projection.";
                return false;
            }
        }
        return true;
    }

    static bool TryGetCell(
        WorldGridDefinition definition,
        IReadOnlyList<WorldCellData> cells,
        Vector2Int coordinate,
        out WorldCellData cell)
    {
        if (coordinate.x < 0 || coordinate.x >= definition.Width ||
            coordinate.y < 0 || coordinate.y >= definition.Height)
        {
            cell = default;
            return false;
        }
        cell = cells[coordinate.y * definition.Width + coordinate.x];
        return true;
    }

    List<WorldModifiedCellSaveData> BuildCellDeltas()
    {
        var result = new List<WorldModifiedCellSaveData>();
        if (!IsProceduralActive || _grid.Cells.Count != _baseWorld.TerrainCells.Count)
            return result;

        for (int index = 0; index < _grid.Cells.Count; index++)
        {
            WorldCellData current = _grid.Cells[index];
            WorldCellData baseline = _baseWorld.TerrainCells[index];
            if (TerrainEqualsIgnoringOccupancy(current, baseline)) continue;
            result.Add(new WorldModifiedCellSaveData
            {
                x = current.Coordinate.x,
                z = current.Coordinate.y,
                elevationLevel = current.ElevationLevel,
                groundType = (int)current.GroundType,
                pathType = (int)current.PathType,
                waterSurfaceLevel = current.WaterSurfaceLevel,
                waterDepthLevels = current.WaterDepthLevels
            });
        }
        return result;
    }

    List<WorldPlacedBuildingSaveData> CaptureBuildings()
    {
        var result = new List<WorldPlacedBuildingSaveData>();
        if (_buildings.TryGetPlacement(WorldBuildingPlacementService.PrototypeInstanceId,
                out WorldPlacedBuildingRuntime placement))
        {
            result.Add(new WorldPlacedBuildingSaveData
            {
                instanceId = placement.InstanceId,
                buildingId = placement.Definition.StableId,
                anchorX = placement.Anchor.x,
                anchorZ = placement.Anchor.y,
                rotationQuarterTurns = placement.QuarterTurns
            });
        }
        return result;
    }

    static bool TerrainEqualsIgnoringOccupancy(WorldCellData first, WorldCellData second)
    {
        return first.Coordinate == second.Coordinate &&
               first.ElevationLevel == second.ElevationLevel &&
               first.GroundType == second.GroundType &&
               first.PathType == second.PathType &&
               first.WaterSurfaceLevel == second.WaterSurfaceLevel &&
               first.WaterDepthLevels == second.WaterDepthLevels;
    }

    Vector3 ResolveSafePlayerPosition(Vector3 requested, out Vector2Int cell)
    {
        if (_grid.WorldToCell(requested, out cell) &&
            _grid.TryGetCell(cell, out WorldCellData requestedCell) && requestedCell.IsWalkable)
        {
            _grid.CellToWorld(cell, out Vector3 centre);
            return centre + Vector3.up * 0.08f;
        }
        return ResolveStartPosition(out cell);
    }

    Vector3 ResolveRestoredSafePlayerPosition(WorldStateSaveData state)
    {
        var savedCell = new Vector2Int(state.safePlayerCellX, state.safePlayerCellZ);
        if (state.hasSafePlayerPosition &&
            _grid.TryGetCell(savedCell, out WorldCellData cell) && cell.IsWalkable &&
            _grid.CellToWorld(savedCell, out Vector3 world))
        {
            return world + Vector3.up * 0.08f;
        }
        return ResolveStartPosition(out _);
    }

    Vector3 ResolveStartPosition(out Vector2Int cell)
    {
        cell = Vector2Int.zero;
        if (_baseWorld != null &&
            _baseWorld.TryGetAnchor(WorldGenerationAnchorKind.Start,
                out WorldGenerationAnchor start))
        {
            cell = start.Coordinate;
        }
        if (_grid.CellToWorld(cell, out Vector3 world)) return world + Vector3.up * 0.08f;
        return Vector3.up * 0.08f;
    }

    bool RemovePrototypeBuilding(out string reason)
    {
        reason = string.Empty;
        if (!_buildings.TryGetPlacement(WorldBuildingPlacementService.PrototypeInstanceId, out _))
            return true;
        WorldBuildingPlacementResult removed = _buildings.TryRemove(
            WorldBuildingPlacementService.PrototypeInstanceId);
        if (removed.Succeeded) return true;
        reason = $"Existing prototype building could not be cleared: {removed.Failure}.";
        return false;
    }

    void RefreshTerrainProjection()
    {
        foreach (WorldChunkTerrain terrain in GetComponents<WorldChunkTerrain>())
        {
            bool enabled = terrain.enabled;
            if (enabled) terrain.enabled = false;
            if (enabled) terrain.enabled = true;
            terrain.Rebuild(WorldChunkDirtyFlags.All);
        }
    }

    void ResolveDependencies()
    {
        if (_grid == null) _grid = GetComponent<WorldGridService>();
        if (_buildings == null) _buildings = GetComponent<WorldBuildingPlacementService>();
        if (_grid == null || _buildings == null)
            throw new InvalidOperationException("World persistence requires grid and building services.");
    }

    static List<WorldShopFurnitureSaveData> ProjectShopFurniture(
        IReadOnlyList<PlaceableSaveData> source)
    {
        var result = new List<WorldShopFurnitureSaveData>();
        if (source == null) return result;
        foreach (PlaceableSaveData record in source)
        {
            if (record == null || record.zoneId != ShopCustomizationController.ShopInteriorZoneId)
                continue;
            result.Add(new WorldShopFurnitureSaveData
            {
                zoneId = record.zoneId,
                definitionId = record.definitionId,
                instanceId = record.instanceId,
                gridX = record.gridX,
                gridY = record.gridY,
                rotationQuarterTurns = record.rotationQuarterTurns,
                isFixed = record.isFixed,
                recovered = record.recovered,
                functionalState = record.functionalState,
                storedItems = CloneStoredItems(record.storedItems)
            });
        }
        return result.OrderBy(record => record.instanceId).ToList();
    }

    static List<PlaceableSaveData> ExpandShopFurniture(
        IReadOnlyList<WorldShopFurnitureSaveData> source)
    {
        var result = new List<PlaceableSaveData>();
        if (source == null) return result;
        foreach (WorldShopFurnitureSaveData record in source)
        {
            if (record == null) continue;
            result.Add(new PlaceableSaveData
            {
                zoneId = record.zoneId,
                definitionId = record.definitionId,
                instanceId = record.instanceId,
                gridX = record.gridX,
                gridY = record.gridY,
                rotationQuarterTurns = record.rotationQuarterTurns,
                isFixed = record.isFixed,
                recovered = record.recovered,
                functionalState = record.functionalState,
                storedItems = CloneStoredItems(record.storedItems)
            });
        }
        return result;
    }

    static List<WorldShopFurnitureSaveData> CloneFurniture(
        IReadOnlyList<WorldShopFurnitureSaveData> source)
    {
        return ProjectShopFurniture(ExpandShopFurniture(source));
    }

    static List<WorldResourceStateSaveData> CloneResourceStates(
        IReadOnlyList<WorldResourceStateSaveData> source)
    {
        var result = new List<WorldResourceStateSaveData>();
        if (source == null) return result;
        foreach (WorldResourceStateSaveData record in source)
        {
            if (record == null) continue;
            result.Add(new WorldResourceStateSaveData
            {
                spawnKey = record.spawnKey,
                consumed = record.consumed,
                respawnDay = record.respawnDay
            });
        }
        return result.OrderBy(record => record.spawnKey).ToList();
    }

    static List<PlaceableStoredItemSaveData> CloneStoredItems(
        IReadOnlyList<PlaceableStoredItemSaveData> source)
    {
        var result = new List<PlaceableStoredItemSaveData>();
        if (source == null) return result;
        foreach (PlaceableStoredItemSaveData item in source)
        {
            if (item == null) continue;
            result.Add(new PlaceableStoredItemSaveData
            {
                itemId = item.itemId,
                itemName = item.itemName,
                count = item.count,
                quality = item.quality,
                currentPrice = item.currentPrice
            });
        }
        return result;
    }
}

#if UNITY_EDITOR
public static class PA_WorldPersistenceTools
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.WORLD007.Active";
    const string FailedKey = "PA.WORLD007.Failed";
    const string ConsoleErrorKey = "PA.WORLD007.ConsoleErrors";
    const string WaitFramesKey = "PA.WORLD007.WaitFrames";
    const long ValidationSeed = 7007L;

    [InitializeOnLoadMethod]
    static void ResumeAfterReload()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        SubscribeCallbacks();
        if (EditorApplication.isPlaying) EditorApplication.update += ValidateRuntime;
    }

    [MenuItem("Project PA/World/WORLD-007/Validate World Persistence")]
    public static void RunWorld007Validation()
    {
        RunWorld007ValidationInternal();
    }

    public static void RunWorld007ValidationBatch()
    {
        RunWorld007ValidationInternal();
    }

    static void RunWorld007ValidationInternal()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetInt(ConsoleErrorKey, 0);
            SessionState.SetInt(WaitFramesKey, 0);
            SubscribeCallbacks();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && !scene.isDirty,
                "WorldSandbox opens clean for persistence validation");
            ValidateLegacyMigrationAndJson();
            Debug.Log("[WORLD-007] EDIT_MODE_PASS v10Legacy=true v11Json=true");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void ValidateLegacyMigrationAndJson()
    {
        var legacy = new SaveData
        {
            version = 10,
            money = 1234,
            playerName = "Legacy",
            placeables = new List<PlaceableSaveData>
            {
                new PlaceableSaveData
                {
                    zoneId = ShopCustomizationController.ShopInteriorZoneId,
                    definitionId = "shop.shelf",
                    instanceId = "fixed.legacy",
                    gridX = 4,
                    gridY = 0,
                    rotationQuarterTurns = 3,
                    isFixed = true
                }
            }
        };
        WorldPersistenceMigration.UpgradeV10ToV11(legacy);
        Require(legacy.version == 11 && legacy.money == 1234 && legacy.playerName == "Legacy" &&
                legacy.placeables.Count == 1 && legacy.placeables[0].instanceId == "fixed.legacy" &&
                legacy.worldState != null &&
                legacy.worldState.worldMode == WorldPersistenceMigration.LegacyFixedMode &&
                legacy.worldState.modifiedCells.Count == 0 &&
                legacy.worldState.placedBuildings.Count == 0,
            "v10 upgrades additively to v11 LegacyFixed without converting legacy records");
        SaveData roundTrip = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(legacy));
        Require(roundTrip != null && roundTrip.version == 11 && roundTrip.money == 1234 &&
                roundTrip.worldState.worldMode == WorldPersistenceMigration.LegacyFixedMode &&
                roundTrip.placeables.Count == 1,
            "JsonUtility preserves additive v11 and legacy placement data");
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
        WorldPersistenceService service = WorldPersistenceService.Instance ??
                                          UnityEngine.Object.FindFirstObjectByType<WorldPersistenceService>();
        if (service == null && frames < 180) return;

        EditorApplication.update -= ValidateRuntime;
        try
        {
            Require(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11,
                $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
            Require(service != null, "WorldSandbox bootstraps one persistence adapter");
            Require(service.StartProceduralWorld(ValidationSeed, out string startReason),
                $"seed world starts from deterministic generation ({startReason})");

            WorldGridService grid = service.GetComponent<WorldGridService>();
            WorldBuildingPlacementService buildings = service.GetComponent<WorldBuildingPlacementService>();
            Require(grid.Definition.Width == 128 && grid.Definition.Height == 128 &&
                    grid.Cells.Count == 16384,
                "generated seed installs a 128x128 runtime cell authority");

            List<Vector2Int> editable = FindEditableDryCells(grid, 4);
            Require(editable.Count == 4, "four independent dry cells are available for sparse edits");
            Require(grid.Terraform.Raise(editable[0]).Succeeded,
                "height delta commits through the terraform transaction");
            WorldGroundType changedGround = grid.TryGetCell(editable[1], out WorldCellData groundBefore) &&
                                            groundBefore.GroundType == WorldGroundType.Rock
                ? WorldGroundType.Soil
                : WorldGroundType.Rock;
            Require(grid.SurfaceEditor.PaintGround(editable[1], changedGround).Succeeded,
                "ground delta commits through the surface transaction");
            Require(grid.SurfaceEditor.PaintPath(editable[2], WorldPathType.Dirt).Succeeded,
                "path delta commits through the surface transaction");
            Require(grid.SurfaceEditor.FillWaterOneLevel(editable[3]).Succeeded,
                "water delta commits through the surface transaction");

            Require(TryFindBuildingLocation(buildings, grid, out Vector2Int buildingAnchor,
                    out int buildingRotation),
                "a valid generated-island B09 footprint and entrance are found");
            WorldBuildingPlacementResult placed = buildings.TryPlace(
                WorldBuildingPlacementService.PrototypeInstanceId,
                buildingAnchor,
                buildingRotation);
            Require(placed.Succeeded && buildings.RegisteredCount == 1,
                "one real B09 building is placed before persistence capture");

            WorldGenerationResult generated = WorldIslandGenerator.Generate(ValidationSeed);
            string resourceKey = generated.ResourceSpawns[0].SpawnKey;
            Require(service.SetResourceState(resourceKey, true, 3),
                "stable generated resource state is marked consumed");
            var furniture = new List<PlaceableSaveData>
            {
                new PlaceableSaveData
                {
                    zoneId = ShopCustomizationController.ShopInteriorZoneId,
                    definitionId = "shop.shelf",
                    instanceId = "fixed.world007.shelf",
                    gridX = 4,
                    gridY = 0,
                    rotationQuarterTurns = 3,
                    isFixed = true,
                    functionalState = "shop-slot:stocked"
                }
            };
            generated.TryGetAnchor(WorldGenerationAnchorKind.Start, out WorldGenerationAnchor start);
            grid.CellToWorld(start.Coordinate, out Vector3 requestedSafePosition);
            WorldStateSaveData captured = service.CaptureState(requestedSafePosition, furniture);
            Require(captured.worldMode == WorldPersistenceMigration.ProceduralMode &&
                    captured.modifiedCells.Count == 4 && captured.modifiedCells.Count < 16384 &&
                    captured.placedBuildings.Count == 1 && captured.shopFurniture.Count == 1 &&
                    captured.resourceStates.Count == 1 && captured.hasSafePlayerPosition,
                "capture stores seed metadata and four sparse deltas plus bounded world records");

            ulong capturedChecksum = WorldPersistenceService.ComputePayloadChecksum(captured);
            string output = Path.Combine(Application.dataPath, "..", "Logs", "WorldPersistence",
                DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(output);
            var repository = new LocalJsonSaveRepository(output);
            var envelope = new SaveData
            {
                version = WorldPersistenceMigration.AdditiveWorldSaveVersion,
                worldState = captured,
                placeables = furniture,
                playerPosition = requestedSafePosition
            };
            repository.SaveAsync("world007", JsonUtility.ToJson(envelope, true)).GetAwaiter().GetResult();
            string loadedJson = repository.LoadAsync("world007").GetAwaiter().GetResult();
            SaveData loaded = JsonUtility.FromJson<SaveData>(loadedJson);
            Require(loaded != null && loaded.version == 11 && loaded.worldState != null &&
                    WorldPersistenceService.ComputePayloadChecksum(loaded.worldState) == capturedChecksum,
                "existing repository and JsonUtility round-trip the additive payload deterministically");

            Require(service.StartProceduralWorld(ValidationSeed + 1, out string resetReason),
                $"runtime state changes before restore ({resetReason})");
            Require(service.TryRestore(loaded.worldState, out Vector3 restoredPlayer,
                    out List<PlaceableSaveData> restoredFurniture, out string restoreReason),
                $"seed plus sparse delta restores atomically ({restoreReason})");
            ValidateRestoredState(grid, buildings, editable, changedGround, buildingAnchor,
                buildingRotation, restoredPlayer, restoredFurniture, start.Coordinate);
            ulong firstRestoreChecksum = service.ComputeCurrentChecksum();

            Require(service.TryRestore(loaded.worldState, out _, out List<PlaceableSaveData> secondFurniture,
                    out string duplicateReason) && buildings.RegisteredCount == 1 &&
                    secondFurniture.Count == 1 && service.ComputeCurrentChecksum() == firstRestoreChecksum,
                $"restoring the same payload twice creates no duplicate world objects ({duplicateReason})");

            WorldStateSaveData invalidDelta = JsonUtility.FromJson<WorldStateSaveData>(
                JsonUtility.ToJson(loaded.worldState));
            invalidDelta.modifiedCells.Add(new WorldModifiedCellSaveData
            {
                x = 999,
                z = 999,
                elevationLevel = 1,
                groundType = (int)WorldGroundType.Default,
                pathType = (int)WorldPathType.None,
                waterSurfaceLevel = 1,
                waterDepthLevels = 0
            });
            Require(!service.TryRestore(invalidDelta, out _, out _, out string invalidReason) &&
                    !string.IsNullOrWhiteSpace(invalidReason) &&
                    service.ComputeCurrentChecksum() == firstRestoreChecksum &&
                    buildings.RegisteredCount == 1,
                "out-of-range delta fails before mutating the live world");

            WorldStateSaveData missingVersion = JsonUtility.FromJson<WorldStateSaveData>(
                JsonUtility.ToJson(loaded.worldState));
            missingVersion.generationVersion = 0;
            Require(!service.TryRestore(missingVersion, out _, out _, out string versionReason) &&
                    !string.IsNullOrWhiteSpace(versionReason) &&
                    service.ComputeCurrentChecksum() == firstRestoreChecksum,
                "missing generationVersion fails safely without world mutation");
            Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                "blocking runtime Console Error/Exception/Assert count is 0");
            Debug.Log("[WORLD-007] PLAY_MODE_PASS seed=true delta=4 building=1 furniture=1 " +
                      "resource=1 safePlayer=true duplicate=false corruptionSafe=true schema=11");
            EditorApplication.ExitPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static List<Vector2Int> FindEditableDryCells(WorldGridService grid, int count)
    {
        var result = new List<Vector2Int>();
        foreach (WorldCellData cell in grid.Cells)
        {
            if (result.Count >= count) break;
            if (cell.HasWater || cell.HasPath || cell.Occupancy != WorldCellOccupancy.Empty ||
                cell.ElevationLevel >= grid.Definition.MaxElevationLevel ||
                grid.IsTerraformProtected(cell.Coordinate))
            {
                continue;
            }
            if (result.All(existing => Mathf.Abs(existing.x - cell.Coordinate.x) > 2 ||
                                       Mathf.Abs(existing.y - cell.Coordinate.y) > 2))
                result.Add(cell.Coordinate);
        }
        return result;
    }

    static bool TryFindBuildingLocation(
        WorldBuildingPlacementService buildings,
        WorldGridService grid,
        out Vector2Int anchor,
        out int rotation)
    {
        for (int z = 4; z < grid.Definition.Height - 4; z++)
        {
            for (int x = 4; x < grid.Definition.Width - 4; x++)
            {
                for (int candidateRotation = 0; candidateRotation < 4; candidateRotation++)
                {
                    var candidate = new Vector2Int(x, z);
                    if (buildings.Evaluate(WorldBuildingPlacementService.PrototypeInstanceId,
                            candidate, candidateRotation).Succeeded)
                    {
                        anchor = candidate;
                        rotation = candidateRotation;
                        return true;
                    }
                }
            }
        }
        anchor = default;
        rotation = 0;
        return false;
    }

    static void ValidateRestoredState(
        WorldGridService grid,
        WorldBuildingPlacementService buildings,
        IReadOnlyList<Vector2Int> edited,
        WorldGroundType changedGround,
        Vector2Int buildingAnchor,
        int buildingRotation,
        Vector3 restoredPlayer,
        IReadOnlyList<PlaceableSaveData> furniture,
        Vector2Int startCell)
    {
        Require(grid.TryGetCell(edited[0], out WorldCellData raised) &&
                grid.TryGetCell(edited[1], out WorldCellData ground) &&
                grid.TryGetCell(edited[2], out WorldCellData path) &&
                grid.TryGetCell(edited[3], out WorldCellData water) &&
                raised.ElevationLevel > 0 && ground.GroundType == changedGround &&
                path.PathType == WorldPathType.Dirt && water.HasWater,
            "height, ground, path and water deltas restore exactly");
        Require(buildings.TryGetPlacement(WorldBuildingPlacementService.PrototypeInstanceId,
                    out WorldPlacedBuildingRuntime building) &&
                building.Anchor == buildingAnchor && building.QuarterTurns == buildingRotation &&
                building.Footprint.All(cell => grid.TryGetCell(cell, out WorldCellData occupied) &&
                                                    occupied.Occupancy == WorldCellOccupancy.Occupied),
            "building transform, rotation and derived occupancy restore exactly");
        Require(furniture.Count == 1 && furniture[0].instanceId == "fixed.world007.shelf" &&
                furniture[0].gridX == 4 && furniture[0].gridY == 0 &&
                furniture[0].rotationQuarterTurns == 3,
            "shop furniture projection restores stable identity, cell and rotation");
        Require(grid.WorldToCell(restoredPlayer, out Vector2Int restoredCell) &&
                restoredCell == startCell && grid.TryGetCell(restoredCell, out WorldCellData safe) &&
                safe.IsWalkable,
            "restored player position resolves to a safe walkable start cell");
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
        Debug.LogError($"[WORLD-007] FAIL {ex.Message}\n{ex}");
        EditorApplication.update -= ValidateRuntime;
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
            ? $"[WORLD-007] FINISHED_WITH_ERRORS consoleErrors={consoleErrors}"
            : "[WORLD-007] FINISHED_PASS schema=11 legacyV10=true seed=true sparseDelta=4 " +
              "building=1 furniture=1 resource=1 safePlayer=true duplicate=false corruptionSafe=true");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[WORLD-007] PASS {message}");
    }
}
#endif
