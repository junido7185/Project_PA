using System;
using System.Collections.Generic;
using UnityEngine;

// Shared grid authority for both the legacy outdoor builder and zone-aware customization.
// The legacy one-cell API is intentionally preserved for BuildManager/SaveManager.
[DefaultExecutionOrder(-50)]
public class GridService : MonoBehaviour
{
    public static GridService Instance { get; private set; }

    [Header("Grid Settings")]
    [Tooltip("World size of one grid cell in metres. Must match BuildManager.gridSize.")]
    public float cellSize = 2.0f;

    readonly HashSet<Vector2Int> _occupied = new HashSet<Vector2Int>();
    readonly Dictionary<string, ZoneRuntime> _zones = new Dictionary<string, ZoneRuntime>();

    public int OccupiedCount => _occupied.Count;

    sealed class ZoneRuntime
    {
        public string id;
        public Transform root;
        public Vector3 localOrigin;
        public Vector2Int size;
        public Vector2Int entryCell;
        public Vector2Int serviceCell;
        public readonly HashSet<Vector2Int> protectedCells = new HashSet<Vector2Int>();
        public readonly Dictionary<Vector2Int, string> blockingOwners = new Dictionary<Vector2Int, string>();
        public readonly Dictionary<Vector2Int, HashSet<string>> clearanceOwners = new Dictionary<Vector2Int, HashSet<string>>();
        public readonly Dictionary<string, List<Vector2Int>> ownerBlocking = new Dictionary<string, List<Vector2Int>>();
        public readonly Dictionary<string, List<Vector2Int>> ownerClearance = new Dictionary<string, List<Vector2Int>>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // -------- Legacy world grid --------

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        float safe = Mathf.Max(0.01f, cellSize);
        return new Vector2Int(Mathf.RoundToInt(worldPos.x / safe), Mathf.RoundToInt(worldPos.z / safe));
    }

    public Vector3 CellToWorld(Vector2Int cell) => new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);

    public Vector3 SnapToGrid(Vector3 worldPos)
    {
        float safe = Mathf.Max(0.01f, cellSize);
        return new Vector3(Mathf.Round(worldPos.x / safe) * safe, worldPos.y,
            Mathf.Round(worldPos.z / safe) * safe);
    }

    public bool IsOccupied(Vector2Int cell) => _occupied.Contains(cell);
    public bool IsOccupiedWorld(Vector3 worldPos) => IsOccupied(WorldToCell(worldPos));

    public bool TryOccupy(Vector2Int cell)
    {
        if (_occupied.Contains(cell)) return false;
        _occupied.Add(cell);
        return true;
    }

    public bool TryOccupyWorld(Vector3 worldPos) => TryOccupy(WorldToCell(worldPos));
    public void Release(Vector2Int cell) => _occupied.Remove(cell);
    public void ReleaseWorld(Vector3 worldPos) => Release(WorldToCell(worldPos));

    // Save loads clear occupancy, but retain zone definitions registered by runtime controllers.
    public void Clear()
    {
        _occupied.Clear();
        foreach (var zone in _zones.Values)
        {
            zone.blockingOwners.Clear();
            zone.clearanceOwners.Clear();
            zone.ownerBlocking.Clear();
            zone.ownerClearance.Clear();
        }
    }

    // -------- Zone-aware grid --------

    public bool RegisterZone(string zoneId, Transform root, Vector3 localOrigin, Vector2Int size,
        IEnumerable<Vector2Int> protectedCells, Vector2Int entryCell, Vector2Int serviceCell)
    {
        if (string.IsNullOrWhiteSpace(zoneId) || root == null || size.x <= 0 || size.y <= 0)
            return false;

        if (!_zones.TryGetValue(zoneId, out var zone))
        {
            zone = new ZoneRuntime { id = zoneId };
            _zones.Add(zoneId, zone);
        }

        zone.root = root;
        zone.localOrigin = localOrigin;
        zone.size = size;
        zone.entryCell = entryCell;
        zone.serviceCell = serviceCell;
        zone.protectedCells.Clear();
        if (protectedCells != null)
            foreach (var cell in protectedCells)
                if (IsInBounds(zone, cell)) zone.protectedCells.Add(cell);
        return true;
    }

    public bool HasZone(string zoneId) => !string.IsNullOrEmpty(zoneId) && _zones.ContainsKey(zoneId);

    public Vector2Int GetZoneSize(string zoneId)
    {
        return TryGetZone(zoneId, out var zone) ? zone.size : Vector2Int.zero;
    }

    public bool IsZoneCellProtected(string zoneId, Vector2Int cell)
    {
        return TryGetZone(zoneId, out var zone) && zone.protectedCells.Contains(cell);
    }

    public bool IsZoneCellInBounds(string zoneId, Vector2Int cell)
    {
        return TryGetZone(zoneId, out var zone) && IsInBounds(zone, cell);
    }

    public Vector2Int WorldToZoneCell(string zoneId, Vector3 worldPosition)
    {
        if (!TryGetZone(zoneId, out var zone) || zone.root == null) return Vector2Int.zero;
        Vector3 local = zone.root.InverseTransformPoint(worldPosition) - zone.localOrigin;
        float safe = Mathf.Max(0.01f, cellSize);
        return new Vector2Int(Mathf.RoundToInt(local.x / safe), Mathf.RoundToInt(local.z / safe));
    }

    public Vector3 ZoneCellToWorld(string zoneId, Vector2Int cell, float localY = 0f)
    {
        if (!TryGetZone(zoneId, out var zone) || zone.root == null) return Vector3.zero;
        Vector3 local = zone.localOrigin + new Vector3(cell.x * cellSize, localY, cell.y * cellSize);
        return zone.root.TransformPoint(local);
    }

    public Vector3 GetZonePlacementWorld(string zoneId, Vector2Int anchor,
        IReadOnlyList<Vector2Int> footprint, int quarterTurns, float localY = 0f)
    {
        List<Vector2Int> cells = BuildCells(anchor, footprint, quarterTurns);
        if (cells.Count == 0) return ZoneCellToWorld(zoneId, anchor, localY);
        Vector3 sum = Vector3.zero;
        foreach (var cell in cells) sum += ZoneCellToWorld(zoneId, cell, localY);
        return sum / cells.Count;
    }

    public IReadOnlyList<Vector2Int> GetZoneFootprintCells(Vector2Int anchor,
        IReadOnlyList<Vector2Int> offsets, int quarterTurns)
    {
        return BuildCells(anchor, offsets, quarterTurns);
    }

    public string GetZoneOwner(string zoneId, Vector2Int cell)
    {
        if (!TryGetZone(zoneId, out var zone)) return null;
        return zone.blockingOwners.TryGetValue(cell, out string owner) ? owner : null;
    }

    public bool CanOccupyZone(string zoneId, string ownerId, Vector2Int anchor,
        IReadOnlyList<Vector2Int> footprint, IReadOnlyList<Vector2Int> clearance,
        int quarterTurns, bool preservePath, out string reason)
    {
        reason = string.Empty;
        if (!TryGetZone(zoneId, out var zone)) { reason = "배치 구역을 찾을 수 없습니다."; return false; }
        if (string.IsNullOrWhiteSpace(ownerId)) { reason = "배치 인스턴스 ID가 없습니다."; return false; }

        List<Vector2Int> blocking = BuildCells(anchor, footprint, quarterTurns);
        List<Vector2Int> access = BuildCells(anchor, clearance, quarterTurns);
        if (blocking.Count == 0) { reason = "점유 셀이 정의되지 않았습니다."; return false; }

        foreach (var cell in blocking)
        {
            if (!IsInBounds(zone, cell)) { reason = "상점 바닥 범위를 벗어납니다."; return false; }
            if (zone.protectedCells.Contains(cell)) { reason = "출입구 보호 셀에는 놓을 수 없습니다."; return false; }
            if (zone.blockingOwners.TryGetValue(cell, out string blocker) && blocker != ownerId)
            { reason = "다른 가구와 겹칩니다."; return false; }
            if (HasOtherClearanceOwner(zone, cell, ownerId))
            { reason = "다른 가구의 사용 공간을 가립니다."; return false; }
        }

        foreach (var cell in access)
        {
            if (!IsInBounds(zone, cell)) { reason = "가구 앞 사용 공간이 벽 밖으로 나갑니다."; return false; }
            if (zone.blockingOwners.TryGetValue(cell, out string blocker) && blocker != ownerId)
            { reason = "가구 앞 사용 공간이 막힙니다."; return false; }
        }

        if (preservePath && !HasConnectedPath(zone, ownerId, blocking))
        { reason = "출입구에서 상점 안쪽으로 이어지는 통로가 막힙니다."; return false; }

        return true;
    }

    public bool TryOccupyZone(string zoneId, string ownerId, Vector2Int anchor,
        IReadOnlyList<Vector2Int> footprint, IReadOnlyList<Vector2Int> clearance,
        int quarterTurns, bool preservePath, out string reason)
    {
        if (!CanOccupyZone(zoneId, ownerId, anchor, footprint, clearance, quarterTurns, preservePath, out reason))
            return false;

        ZoneRuntime zone = _zones[zoneId];
        ReleaseZoneOwner(zoneId, ownerId);

        List<Vector2Int> blocking = BuildCells(anchor, footprint, quarterTurns);
        List<Vector2Int> access = BuildCells(anchor, clearance, quarterTurns);
        zone.ownerBlocking[ownerId] = blocking;
        zone.ownerClearance[ownerId] = access;
        foreach (var cell in blocking) zone.blockingOwners[cell] = ownerId;
        foreach (var cell in access)
        {
            if (!zone.clearanceOwners.TryGetValue(cell, out var owners))
            {
                owners = new HashSet<string>();
                zone.clearanceOwners[cell] = owners;
            }
            owners.Add(ownerId);
        }
        return true;
    }

    public void ReleaseZoneOwner(string zoneId, string ownerId)
    {
        if (!TryGetZone(zoneId, out var zone) || string.IsNullOrEmpty(ownerId)) return;
        if (zone.ownerBlocking.TryGetValue(ownerId, out var blocking))
        {
            foreach (var cell in blocking)
                if (zone.blockingOwners.TryGetValue(cell, out string current) && current == ownerId)
                    zone.blockingOwners.Remove(cell);
            zone.ownerBlocking.Remove(ownerId);
        }
        if (zone.ownerClearance.TryGetValue(ownerId, out var access))
        {
            foreach (var cell in access)
            {
                if (!zone.clearanceOwners.TryGetValue(cell, out var owners)) continue;
                owners.Remove(ownerId);
                if (owners.Count == 0) zone.clearanceOwners.Remove(cell);
            }
            zone.ownerClearance.Remove(ownerId);
        }
    }

    public bool HasZonePath(string zoneId)
    {
        return TryGetZone(zoneId, out var zone) && HasConnectedPath(zone, null, null);
    }

    bool TryGetZone(string id, out ZoneRuntime zone)
    {
        zone = null;
        return !string.IsNullOrEmpty(id) && _zones.TryGetValue(id, out zone) && zone.root != null;
    }

    static bool IsInBounds(ZoneRuntime zone, Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < zone.size.x && cell.y < zone.size.y;
    }

    static bool HasOtherClearanceOwner(ZoneRuntime zone, Vector2Int cell, string ownerId)
    {
        if (!zone.clearanceOwners.TryGetValue(cell, out var owners)) return false;
        foreach (string owner in owners) if (owner != ownerId) return true;
        return false;
    }

    static List<Vector2Int> BuildCells(Vector2Int anchor, IReadOnlyList<Vector2Int> offsets, int turns)
    {
        var cells = new List<Vector2Int>();
        if (offsets == null) return cells;
        int rotation = ((turns % 4) + 4) % 4;
        for (int i = 0; i < offsets.Count; i++)
        {
            Vector2Int rotated = Rotate(offsets[i], rotation);
            Vector2Int cell = anchor + rotated;
            if (!cells.Contains(cell)) cells.Add(cell);
        }
        return cells;
    }

    static Vector2Int Rotate(Vector2Int value, int turns)
    {
        return turns switch
        {
            1 => new Vector2Int(-value.y, value.x),
            2 => new Vector2Int(-value.x, -value.y),
            3 => new Vector2Int(value.y, -value.x),
            _ => value
        };
    }

    static bool HasConnectedPath(ZoneRuntime zone, string ignoredOwner, List<Vector2Int> pendingBlocking)
    {
        if (!IsInBounds(zone, zone.entryCell) || !IsInBounds(zone, zone.serviceCell)) return false;
        var pending = pendingBlocking != null ? new HashSet<Vector2Int>(pendingBlocking) : null;
        bool IsBlocked(Vector2Int cell)
        {
            if (pending != null && pending.Contains(cell)) return true;
            return zone.blockingOwners.TryGetValue(cell, out string owner) && owner != ignoredOwner;
        }

        if (IsBlocked(zone.entryCell) || IsBlocked(zone.serviceCell)) return false;
        var queue = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int> { zone.entryCell };
        queue.Enqueue(zone.entryCell);
        Vector2Int[] directions = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == zone.serviceCell) return true;
            foreach (var direction in directions)
            {
                Vector2Int next = current + direction;
                if (!IsInBounds(zone, next) || visited.Contains(next) || IsBlocked(next)) continue;
                visited.Add(next);
                queue.Enqueue(next);
            }
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        foreach (var cell in _occupied)
            Gizmos.DrawCube(CellToWorld(cell) + Vector3.up * 0.1f,
                new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));

        foreach (var zone in _zones.Values)
        {
            if (zone.root == null) continue;
            foreach (var pair in zone.blockingOwners)
            {
                Gizmos.color = zone.protectedCells.Contains(pair.Key)
                    ? new Color(1f, 0.3f, 0.2f, 0.35f)
                    : new Color(0.45f, 0.75f, 0.45f, 0.28f);
                Gizmos.DrawCube(ZoneCellToWorld(zone.id, pair.Key, 0.08f),
                    new Vector3(cellSize * 0.88f, 0.08f, cellSize * 0.88f));
            }
        }
    }
}
