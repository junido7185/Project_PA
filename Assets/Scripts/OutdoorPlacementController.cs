using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

// Zone-aware outdoor placement authority. It reuses GridService rather than creating
// another grid and treats the authored B09 shed as the protected, movable village store.
[DefaultExecutionOrder(70)]
public class OutdoorPlacementController : MonoBehaviour
{
    public const string OutdoorZoneId = "village.outdoor";
    static readonly Vector3 ZoneOrigin = new Vector3(-46f, 0f, -46f);
    static readonly Vector2Int ZoneSize = new Vector2Int(47, 47);
    static readonly Vector2Int EntryCell = new Vector2Int(23, 0);
    static readonly Vector2Int ServiceCell = new Vector2Int(23, 46);
    const float InteractionDistance = 4.8f;

    public static OutdoorPlacementController Instance { get; private set; }
    public bool IsReady => _ready;
    public string ZoneId => OutdoorZoneId;
    public int ProtectedCellCount => _protectedCells.Count;
    public int ActivePlacementCount => _placements.Count(p => p.gameObject != null && p.gameObject.activeSelf);
    public int SuppressedLegacyStorageCount => _suppressedLegacyStorageCount;

    public sealed class PlacementHandle
    {
        internal string ownerId;
        internal BuildingData data;
        internal GameObject gameObject;
        internal Vector2Int anchor;
        internal int rotation;
        internal Vector2Int[] footprint;
        internal Vector2Int[] clearance;
        internal bool isFixed;
        internal bool recoverable;

        public string InstanceId => ownerId;
        public BuildingData Data => data;
        public GameObject GameObject => gameObject;
        public Vector2Int Cell => anchor;
        public int RotationQuarterTurns => rotation;
        public bool IsFixed => isFixed;
        public int FootprintCellCount => footprint != null ? footprint.Length : 0;
    }

    readonly List<Vector2Int> _protectedCells = new List<Vector2Int>();
    readonly List<PlacementHandle> _placements = new List<PlacementHandle>();
    readonly List<string> _staticOwnerIds = new List<string>();
    readonly List<GameObject> _previewCells = new List<GameObject>();
    readonly Dictionary<string, BuildingData> _buildingData = new Dictionary<string, BuildingData>();

    bool _ready;
    int _suppressedLegacyStorageCount;
    PlacementHandle _primaryStorage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindFirstObjectByType<OutdoorPlacementController>() != null) return;
        new GameObject("PA_OutdoorPlacement").AddComponent<OutdoorPlacementController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    IEnumerator Start()
    {
        for (int i = 0; i < 180; i++)
        {
            if (GridService.Instance != null && BuildingRegistry.Instance != null) break;
            yield return null;
        }
        if (GridService.Instance == null)
        {
            Debug.LogWarning("[OutdoorPlacement] GridService unavailable; outdoor placement remains inactive.");
            yield break;
        }

        CacheBuildingData();
        BuildProtectedCells();
        GridService.Instance.RegisterZone(OutdoorZoneId, transform, ZoneOrigin, ZoneSize,
            _protectedCells, EntryCell, ServiceCell);
        RebuildRuntimeState(null);
        _ready = true;
        Debug.Log($"[OutdoorPlacement] Ready: zone={OutdoorZoneId}, protected={ProtectedCellCount}, " +
            $"placeables={ActivePlacementCount}, legacyStorageDisabled={_suppressedLegacyStorageCount}.");
    }

    void OnDestroy()
    {
        ClearPreview();
        if (Instance == this) Instance = null;
    }

    void CacheBuildingData()
    {
        _buildingData.Clear();
        foreach (BuildingData data in Resources.LoadAll<BuildingData>("Buildings"))
        {
            if (data == null || data.prefab == null) continue;
            _buildingData[data.prefab.name] = data;
        }
    }

    void BuildProtectedCells()
    {
        _protectedCells.Clear();
        for (int y = 0; y < ZoneSize.y; y++)
        for (int x = 0; x < ZoneSize.x; x++)
        {
            var cell = new Vector2Int(x, y);
            Vector3 world = ZoneOrigin + new Vector3(x * 2f, 0f, y * 2f);
            bool northSouthRoad = Mathf.Abs(world.x) <= 2.01f && world.z >= -36f && world.z <= 34f;
            bool eastWestRoad = Mathf.Abs(world.z) <= 2.01f && world.x >= -34f && world.x <= 34f;
            bool shopPlaza = world.x >= -5f && world.x <= 5f && world.z >= 1f && world.z <= 7f;
            if (northSouthRoad || eastWestRoad || shopPlaza) _protectedCells.Add(cell);
        }

        foreach (BuildingEntrance entrance in FindObjectsByType<BuildingEntrance>(FindObjectsSortMode.None))
        {
            if (entrance == null || entrance.transform.position.y >= 50f) continue;
            AddProtectedWorldCell(entrance.transform.position);
            AddProtectedWorldCell(entrance.transform.position + entrance.transform.forward * 2f);
        }

        foreach (string markerName in new[] { "PlayerSpawn_Default", "PlayerSpawn_Outside", "PlayerSpawn", "HiringSpawn" })
        {
            GameObject marker = GameObject.Find(markerName);
            if (marker != null) AddProtectedWorldCell(marker.transform.position);
        }
    }

    void AddProtectedWorldCell(Vector3 world)
    {
        Vector2Int cell = WorldToCellWithoutZone(world);
        if (cell.x < 0 || cell.y < 0 || cell.x >= ZoneSize.x || cell.y >= ZoneSize.y) return;
        if (!_protectedCells.Contains(cell)) _protectedCells.Add(cell);
    }

    static Vector2Int WorldToCellWithoutZone(Vector3 world)
    {
        return new Vector2Int(
            Mathf.RoundToInt((world.x - ZoneOrigin.x) / 2f),
            Mathf.RoundToInt((world.z - ZoneOrigin.z) / 2f));
    }

    void SuppressLegacyStorageDuplicate(StorageBox primary)
    {
        _suppressedLegacyStorageCount = 0;
        foreach (StorageBox storage in FindObjectsByType<StorageBox>(FindObjectsSortMode.None))
        {
            if (storage == null || storage == primary || storage.transform.position.y >= 50f) continue;
            string path = StablePath(storage.transform);
            if (!path.Contains("[WorldBuildings]", StringComparison.Ordinal)) continue;
            storage.gameObject.SetActive(false);
            _suppressedLegacyStorageCount++;
            Debug.LogWarning($"[OutdoorPlacement] Disabled legacy duplicate storage at {path}; main-map B09 remains authoritative.");
        }
    }

    StorageBox FindPrimaryStorage()
    {
        return FindObjectsByType<StorageBox>(FindObjectsSortMode.None)
            .Where(s => s != null && s.gameObject.activeInHierarchy && s.transform.position.y < 50f)
            .OrderByDescending(s => StablePath(s.transform).Contains("[PA_MapRoot]", StringComparison.Ordinal))
            .ThenBy(s => (s.transform.position - new Vector3(-10f, 0f, 8f)).sqrMagnitude)
            .FirstOrDefault();
    }

    void RebuildRuntimeState(List<PlaceableSaveData> saved)
    {
        ReleaseAllOwners();
        _placements.Clear();
        _primaryStorage = null;

        StorageBox primary = FindPrimaryStorage();
        SuppressLegacyStorageDuplicate(primary);
        var savedOutdoor = saved != null
            ? saved.Where(p => p != null && p.zoneId == OutdoorZoneId && !p.recovered).ToList()
            : new List<PlaceableSaveData>();

        if (primary != null)
        {
            BuildingData data = ResolveBuildingData(primary.gameObject, "B09_StorageShed");
            PlaceableSaveData fixedSave = savedOutdoor.FirstOrDefault(p => p.isFixed && p.instanceId == "fixed.outdoor.B09_StorageShed");
            _primaryStorage = AdoptPlacement(primary.gameObject, data, "fixed.outdoor.B09_StorageShed", true, false, fixedSave);
            if (fixedSave != null) RestoreStorage(primary, fixedSave.storedItems);
        }

        HashSet<GameObject> dynamicObjects = new HashSet<GameObject>();
        if (BuildingRegistry.Instance != null)
        {
            foreach (BuildingInstance building in BuildingRegistry.Instance.Buildings)
            {
                if (building?.gameObject == null) continue;
                dynamicObjects.Add(building.gameObject);
                BuildingData data = ResolveBuildingData(building.gameObject, building.prefabName);
                if (data == null) continue;
                PlaceableSaveData record = FindBestSavedRecord(savedOutdoor, data, building.gameObject.transform.position);
                string owner = record != null && !string.IsNullOrWhiteSpace(record.instanceId)
                    ? record.instanceId : $"placed.outdoor.{Guid.NewGuid():N}";
                PlacementHandle handle = AdoptPlacement(building.gameObject, data, owner, false, true, record);
                if (handle != null && record != null)
                    RestoreStorage(building.gameObject.GetComponent<StorageBox>(), record.storedItems);
            }
        }

        RegisterAuthoredWorldStructures(dynamicObjects, primary != null ? primary.gameObject : null);
    }

    PlaceableSaveData FindBestSavedRecord(List<PlaceableSaveData> saved, BuildingData data, Vector3 currentPosition)
    {
        if (saved == null || data == null || data.prefab == null) return null;
        return saved.Where(p => !p.isFixed && p.definitionId == data.prefab.name)
            .OrderBy(p => (GridService.Instance.ZoneCellToWorld(OutdoorZoneId,
                new Vector2Int(p.gridX, p.gridY)) - currentPosition).sqrMagnitude)
            .FirstOrDefault(p => !_placements.Any(existing => existing.ownerId == p.instanceId));
    }

    PlacementHandle AdoptPlacement(GameObject target, BuildingData data, string ownerId,
        bool isFixed, bool recoverable, PlaceableSaveData saved)
    {
        if (target == null || data == null) return null;
        Vector2Int[] footprint = BuildFootprint(data.prefab, target);
        Vector2Int[] clearance = BuildFrontClearance(footprint);
        int rotation = saved != null ? NormaliseRotation(saved.rotationQuarterTurns)
            : NormaliseRotation(Mathf.RoundToInt(target.transform.eulerAngles.y / 90f));
        Vector2Int anchor = saved != null ? new Vector2Int(saved.gridX, saved.gridY)
            : ResolveAnchor(target.transform.position, footprint, rotation);

        if (!GridService.Instance.TryOccupyZone(OutdoorZoneId, ownerId, anchor, footprint, clearance,
                rotation, false, out string reason))
        {
            Debug.LogWarning($"[OutdoorPlacement] Could not adopt {target.name}: {reason}");
            return null;
        }

        target.transform.SetPositionAndRotation(
            GridService.Instance.GetZonePlacementWorld(OutdoorZoneId, anchor, footprint, rotation),
            Quaternion.Euler(0f, rotation * 90f, 0f));
        EnsureCarvingObstacle(target);
        var handle = new PlacementHandle
        {
            ownerId = ownerId,
            data = data,
            gameObject = target,
            anchor = anchor,
            rotation = rotation,
            footprint = footprint,
            clearance = clearance,
            isFixed = isFixed,
            recoverable = recoverable
        };
        _placements.Add(handle);
        return handle;
    }

    void RegisterAuthoredWorldStructures(HashSet<GameObject> dynamicObjects, GameObject primaryStorage)
    {
        GameObject[] tagged;
        try { tagged = GameObject.FindGameObjectsWithTag("Building"); }
        catch (UnityException) { tagged = Array.Empty<GameObject>(); }

        foreach (GameObject target in tagged)
        {
            if (target == null || target == primaryStorage || dynamicObjects.Contains(target)
                || target.transform.position.y >= 50f || !target.activeInHierarchy) continue;
            string path = StablePath(target.transform);
            if (path.Contains("[WorldBuildings]", StringComparison.Ordinal)) continue;
            Vector2Int[] footprint = BuildFootprint(null, target);
            int rotation = NormaliseRotation(Mathf.RoundToInt(target.transform.eulerAngles.y / 90f));
            Vector2Int anchor = ResolveAnchor(target.transform.position, footprint, rotation);
            string owner = $"fixed.structure.{path}";
            if (GridService.Instance.TryOccupyZone(OutdoorZoneId, owner, anchor, footprint,
                    Array.Empty<Vector2Int>(), rotation, false, out _))
                _staticOwnerIds.Add(owner);
        }
    }

    void ReleaseAllOwners()
    {
        if (GridService.Instance == null) return;
        foreach (PlacementHandle placement in _placements)
            if (placement != null) GridService.Instance.ReleaseZoneOwner(OutdoorZoneId, placement.ownerId);
        foreach (string owner in _staticOwnerIds)
            GridService.Instance.ReleaseZoneOwner(OutdoorZoneId, owner);
        _staticOwnerIds.Clear();
    }

    public bool TryResolvePreview(BuildingData data, Vector3 desiredWorld, int quarterTurns,
        PlacementHandle moving, out Vector2Int anchor, out Vector3 placementWorld, out string reason)
    {
        anchor = Vector2Int.zero;
        placementWorld = desiredWorld;
        reason = string.Empty;
        if (!_ready || data == null || data.prefab == null)
        { reason = "야외 배치 데이터를 준비하지 못했습니다."; return false; }

        Vector2Int[] footprint = moving != null ? moving.footprint : BuildFootprint(data.prefab, null);
        Vector2Int[] clearance = moving != null ? moving.clearance : BuildFrontClearance(footprint);
        int rotation = NormaliseRotation(quarterTurns);
        anchor = ResolveAnchor(desiredWorld, footprint, rotation);
        placementWorld = GridService.Instance.GetZonePlacementWorld(OutdoorZoneId, anchor, footprint, rotation);
        string owner = moving != null ? moving.ownerId : "preview.outdoor";
        return GridService.Instance.CanOccupyZone(OutdoorZoneId, owner, anchor, footprint, clearance,
            rotation, false, out reason);
    }

    public bool TryCommitNew(BuildingData data, GameObject target, Vector2Int anchor, int quarterTurns,
        out PlacementHandle handle, out string reason)
    {
        handle = null;
        reason = string.Empty;
        if (!_ready || data == null || target == null)
        { reason = "야외 배치 상태가 준비되지 않았습니다."; return false; }
        Vector2Int[] footprint = BuildFootprint(data.prefab, target);
        Vector2Int[] clearance = BuildFrontClearance(footprint);
        int rotation = NormaliseRotation(quarterTurns);
        string owner = $"placed.outdoor.{Guid.NewGuid():N}";
        if (!GridService.Instance.TryOccupyZone(OutdoorZoneId, owner, anchor, footprint, clearance,
                rotation, false, out reason)) return false;
        target.transform.SetPositionAndRotation(
            GridService.Instance.GetZonePlacementWorld(OutdoorZoneId, anchor, footprint, rotation),
            Quaternion.Euler(0f, rotation * 90f, 0f));
        EnsureCarvingObstacle(target);
        handle = new PlacementHandle
        {
            ownerId = owner, data = data, gameObject = target, anchor = anchor, rotation = rotation,
            footprint = footprint, clearance = clearance, isFixed = false, recoverable = true
        };
        _placements.Add(handle);
        return true;
    }

    public bool TryApplyMove(PlacementHandle handle, Vector2Int anchor, int quarterTurns, out string reason)
    {
        reason = string.Empty;
        if (handle == null || handle.gameObject == null || !_placements.Contains(handle))
        { reason = "이동할 건물을 찾지 못했습니다."; return false; }
        int rotation = NormaliseRotation(quarterTurns);
        if (!GridService.Instance.TryOccupyZone(OutdoorZoneId, handle.ownerId, anchor,
                handle.footprint, handle.clearance, rotation, false, out reason)) return false;
        handle.anchor = anchor;
        handle.rotation = rotation;
        handle.gameObject.transform.SetPositionAndRotation(
            GridService.Instance.GetZonePlacementWorld(OutdoorZoneId, anchor, handle.footprint, rotation),
            Quaternion.Euler(0f, rotation * 90f, 0f));
        return true;
    }

    public bool TryFindNearestMovable(Vector3 world, out PlacementHandle handle)
    {
        float maxDistance = InteractionDistance * InteractionDistance;
        handle = _placements.Where(p => p?.gameObject != null && p.gameObject.activeSelf)
            .OrderBy(p => (p.gameObject.transform.position - world).sqrMagnitude)
            .FirstOrDefault(p => (p.gameObject.transform.position - world).sqrMagnitude <= maxDistance);
        return handle != null;
    }

    public bool TryRecoverNearest(Vector3 world, bool returnBlueprint, out string reason)
    {
        if (!TryFindNearestMovable(world, out PlacementHandle handle))
        { reason = "가까운 이동 가능 건물이 없습니다."; return false; }
        return TryRecover(handle, returnBlueprint, out reason);
    }

    public bool TryRecover(PlacementHandle handle, bool returnBlueprint, out string reason)
    {
        reason = string.Empty;
        if (handle == null || handle.gameObject == null || !_placements.Contains(handle))
        { reason = "회수할 건물을 찾지 못했습니다."; return false; }
        if (handle.isFixed || !handle.recoverable)
        { reason = "마을의 기본 창고는 마지막 저장 공간이므로 이동만 할 수 있습니다."; return false; }
        StorageBox storage = handle.gameObject.GetComponent<StorageBox>();
        if (storage != null && storage.items != null && storage.items.Any(i => i != null && i.count > 0))
        { reason = "창고 안의 물품을 먼저 비워야 안전하게 회수할 수 있습니다."; return false; }

        if (returnBlueprint)
        {
            Item blueprint = FindBlueprint(handle.data);
            if (blueprint == null || Inventory.instance == null || !Inventory.instance.AddItem(blueprint, 1))
            { reason = "설계도를 돌려놓을 가방 공간이 없습니다."; return false; }
        }

        GridService.Instance.ReleaseZoneOwner(OutdoorZoneId, handle.ownerId);
        BuildingRegistry.Instance?.Unregister(handle.gameObject);
        _placements.Remove(handle);
        Destroy(handle.gameObject);
        reason = "건물을 회수하고 점유 공간을 비웠습니다.";
        return true;
    }

    public void ShowPreview(BuildingData data, Vector2Int anchor, int quarterTurns, bool valid,
        PlacementHandle moving = null)
    {
        ClearPreview();
        if (!_ready || data == null) return;
        Vector2Int[] footprint = moving != null ? moving.footprint : BuildFootprint(data.prefab, null);
        Vector2Int[] clearance = moving != null ? moving.clearance : BuildFrontClearance(footprint);
        Color body = valid ? new Color(0.35f, 0.82f, 0.49f, 0.38f) : new Color(0.95f, 0.30f, 0.24f, 0.46f);
        foreach (Vector2Int cell in GridService.Instance.GetZoneFootprintCells(anchor, footprint, quarterTurns))
            CreatePreviewCell(cell, body);
        foreach (Vector2Int cell in GridService.Instance.GetZoneFootprintCells(anchor, clearance, quarterTurns))
            CreatePreviewCell(cell, new Color(0.97f, 0.73f, 0.28f, 0.28f));
    }

    void CreatePreviewCell(Vector2Int cell, Color color)
    {
        if (!GridService.Instance.IsZoneCellInBounds(OutdoorZoneId, cell)) return;
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "OutdoorPlacementCell";
        quad.transform.position = GridService.Instance.ZoneCellToWorld(OutdoorZoneId, cell, 0.04f);
        quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = Vector3.one * 1.76f;
        Collider collider = quad.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        var material = new Material(Shader.Find("Sprites/Default")) { color = color };
        quad.GetComponent<Renderer>().material = material;
        _previewCells.Add(quad);
    }

    public void ClearPreview()
    {
        foreach (GameObject cell in _previewCells) if (cell != null) Destroy(cell);
        _previewCells.Clear();
    }

    public string GetFootprintLabel(BuildingData data)
    {
        Vector2Int[] cells = BuildFootprint(data != null ? data.prefab : null, null);
        if (cells.Length == 0) return "0칸";
        int width = cells.Max(c => c.x) - cells.Min(c => c.x) + 1;
        int depth = cells.Max(c => c.y) - cells.Min(c => c.y) + 1;
        return $"{width}×{depth}칸";
    }

    Vector2Int ResolveAnchor(Vector3 desiredWorld, Vector2Int[] footprint, int quarterTurns)
    {
        IReadOnlyList<Vector2Int> rotated = GridService.Instance.GetZoneFootprintCells(Vector2Int.zero, footprint, quarterTurns);
        float averageX = rotated.Count > 0 ? rotated.Sum(c => c.x) / (float)rotated.Count : 0f;
        float averageY = rotated.Count > 0 ? rotated.Sum(c => c.y) / (float)rotated.Count : 0f;
        float cellSize = Mathf.Max(0.01f, GridService.Instance.cellSize);
        Vector3 local = transform.InverseTransformPoint(desiredWorld) - ZoneOrigin;
        return new Vector2Int(
            Mathf.RoundToInt(local.x / cellSize - averageX),
            Mathf.RoundToInt(local.z / cellSize - averageY));
    }

    Vector2Int[] BuildFootprint(GameObject prefab, GameObject instance)
    {
        float width = 2f;
        float depth = 2f;
        BoxCollider box = instance != null ? instance.GetComponent<BoxCollider>() : null;
        if (box != null)
        {
            width = Mathf.Abs(box.size.x * box.transform.lossyScale.x);
            depth = Mathf.Abs(box.size.z * box.transform.lossyScale.z);
        }
        else if (prefab != null)
        {
            box = prefab.GetComponent<BoxCollider>();
            if (box != null)
            {
                width = Mathf.Abs(box.size.x * prefab.transform.localScale.x);
                depth = Mathf.Abs(box.size.z * prefab.transform.localScale.z);
            }
            else
            {
                Renderer renderer = prefab.GetComponentInChildren<Renderer>(true);
                if (renderer != null) { width = renderer.bounds.size.x; depth = renderer.bounds.size.z; }
            }
        }
        float cellSize = GridService.Instance != null ? Mathf.Max(0.01f, GridService.Instance.cellSize) : 2f;
        int cellsX = Mathf.Max(1, Mathf.CeilToInt(width / cellSize));
        int cellsY = Mathf.Max(1, Mathf.CeilToInt(depth / cellSize));
        var result = new Vector2Int[cellsX * cellsY];
        int index = 0;
        for (int y = 0; y < cellsY; y++)
        for (int x = 0; x < cellsX; x++) result[index++] = new Vector2Int(x, y);
        return result;
    }

    static Vector2Int[] BuildFrontClearance(Vector2Int[] footprint)
    {
        if (footprint == null || footprint.Length == 0) return Array.Empty<Vector2Int>();
        int minX = footprint.Min(c => c.x);
        int maxX = footprint.Max(c => c.x);
        var result = new Vector2Int[maxX - minX + 1];
        for (int x = minX; x <= maxX; x++) result[x - minX] = new Vector2Int(x, -1);
        return result;
    }

    BuildingData ResolveBuildingData(GameObject target, string fallbackName)
    {
        string normalized = target != null ? target.name.Replace("(Clone)", string.Empty).Trim() : string.Empty;
        if (_buildingData.TryGetValue(normalized, out BuildingData data)) return data;
        if (!string.IsNullOrEmpty(fallbackName) && _buildingData.TryGetValue(fallbackName, out data)) return data;
        return null;
    }

    static Item FindBlueprint(BuildingData data)
    {
        if (data == null) return null;
        return Resources.LoadAll<Item>("Items/Blueprints").FirstOrDefault(item => item != null && item.buildingToBuild == data);
    }

    static void EnsureCarvingObstacle(GameObject target)
    {
        if (target == null || target.GetComponentInChildren<NavMeshObstacle>() != null) return;
        BoxCollider box = target.GetComponent<BoxCollider>();
        if (box == null) return;
        var obstacle = target.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = box.center;
        obstacle.size = box.size;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
    }

    static int NormaliseRotation(int value) => ((value % 4) + 4) % 4;

    static string StablePath(Transform value)
    {
        var names = new List<string>();
        for (Transform current = value; current != null; current = current.parent) names.Add(current.name);
        names.Reverse();
        return string.Join("/", names);
    }

    public void WriteSaveFields(SaveData data)
    {
        if (data == null) return;
        data.placeables ??= new List<PlaceableSaveData>();
        foreach (PlacementHandle placement in _placements)
        {
            if (placement?.gameObject == null || placement.data == null || placement.data.prefab == null) continue;
            data.placeables.Add(new PlaceableSaveData
            {
                zoneId = OutdoorZoneId,
                definitionId = placement.data.prefab.name,
                instanceId = placement.ownerId,
                gridX = placement.anchor.x,
                gridY = placement.anchor.y,
                rotationQuarterTurns = placement.rotation,
                isFixed = placement.isFixed,
                recovered = false,
                functionalState = placement.gameObject.GetComponent<StorageBox>() != null ? "storage" : "building",
                storedItems = SerializeStorage(placement.gameObject.GetComponent<StorageBox>())
            });
        }
    }

    public void RestoreSavedState(List<PlaceableSaveData> saved)
    {
        if (!_ready) return;
        RebuildRuntimeState(saved);
    }

    static List<PlaceableStoredItemSaveData> SerializeStorage(StorageBox storage)
    {
        var result = new List<PlaceableStoredItemSaveData>();
        if (storage?.items == null) return result;
        foreach (ItemInstance item in storage.items)
        {
            if (item?.data == null || item.count <= 0) continue;
            result.Add(new PlaceableStoredItemSaveData
            {
                itemId = item.data.id,
                itemName = item.data.itemName,
                count = item.count,
                quality = item.quality,
                currentPrice = item.currentPrice
            });
        }
        return result;
    }

    static void RestoreStorage(StorageBox storage, List<PlaceableStoredItemSaveData> saved)
    {
        if (storage == null) return;
        storage.items.Clear();
        if (saved == null) return;
        foreach (PlaceableStoredItemSaveData record in saved)
        {
            if (record == null || record.count <= 0) continue;
            Item item = ItemRegistry.Instance != null
                ? ItemRegistry.Instance.Find(record.itemId, record.itemName)
                : Resources.LoadAll<Item>("Items").FirstOrDefault(candidate => candidate != null
                    && ((record.itemId != 0 && candidate.id == record.itemId)
                        || (!string.IsNullOrEmpty(record.itemName) && candidate.itemName == record.itemName)));
            if (item == null) continue;
            storage.items.Add(new ItemInstance(item, record.count)
            {
                quality = record.quality,
                currentPrice = record.currentPrice
            });
        }
    }

    // Validation hooks keep the production path intact while avoiding input synthesis.
    public PlacementHandle GetPrimaryStorageForValidation() => _primaryStorage;
    public PlacementHandle FindPlacementForValidation(string instanceId) =>
        _placements.FirstOrDefault(p => p.ownerId == instanceId);
}
