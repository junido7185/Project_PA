using System.Collections.Generic;
using UnityEngine;

// 그리드 점유맵 서비스.
//
// 역할:
// - 월드 좌표 ↔ 그리드 셀 좌표 변환.
// - 셀 점유 상태 관리 (건물이 차지하고 있는 칸을 기록).
// - BuildManager 가 건설 전에 IsOccupied() 로 빠르게 점유 여부를 확인할 수 있다.
//   기존 Physics.OverlapBox 판정은 시각적 안내에만 유지하고,
//   실제 건설 가부 판정은 GridService 를 1차 필터로 사용한다.
//
// 좌표 체계:
//   셀 (0,0) 은 월드 원점 근처. cellSize=2.0 이면 셀 (3,5) 는 월드 (6, ?, 10) 에 대응.
//   Y 축은 무시 — 2D 평면 점유만 추적.
//
// 저장/로드:
//   직접 직렬화하지 않는다. SaveManager 가 buildings 를 복원할 때
//   RegisterBuilding(pos) 로 점유를 재등록하면 된다.
[DefaultExecutionOrder(-50)]
public class GridService : MonoBehaviour
{
    public static GridService Instance { get; private set; }

    [Header("그리드 설정")]
    [Tooltip("셀 한 변의 크기 (미터). BuildManager.gridSize 와 일치시킨다.")]
    public float cellSize = 2.0f;

    // 점유된 셀 집합.
    private readonly HashSet<Vector2Int> _occupied = new HashSet<Vector2Int>();

    /// <summary>현재 점유된 셀 수.</summary>
    public int OccupiedCount => _occupied.Count;

    // -------- Unity 생명주기 --------

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // -------- 좌표 변환 --------

    /// <summary>월드 좌표 → 셀 좌표. Y 축은 무시된다.</summary>
    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        float safe = Mathf.Max(0.01f, cellSize);
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / safe),
            Mathf.RoundToInt(worldPos.z / safe)
        );
    }

    /// <summary>셀 좌표 → 월드 좌표 (Y=0).</summary>
    public Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
    }

    /// <summary>월드 좌표를 그리드에 스냅한다 (Y 는 원본 유지).</summary>
    public Vector3 SnapToGrid(Vector3 worldPos)
    {
        float safe = Mathf.Max(0.01f, cellSize);
        float x = Mathf.Round(worldPos.x / safe) * safe;
        float z = Mathf.Round(worldPos.z / safe) * safe;
        return new Vector3(x, worldPos.y, z);
    }

    // -------- 점유 관리 --------

    /// <summary>셀이 점유 중인지 확인.</summary>
    public bool IsOccupied(Vector2Int cell) => _occupied.Contains(cell);

    /// <summary>월드 좌표 기반 점유 확인 편의 함수.</summary>
    public bool IsOccupiedWorld(Vector3 worldPos) => IsOccupied(WorldToCell(worldPos));

    /// <summary>
    /// 셀 점유를 시도한다. 이미 점유 중이면 false.
    /// BuildManager.BuildIt() 에서 성공 후 호출한다.
    /// </summary>
    public bool TryOccupy(Vector2Int cell)
    {
        if (_occupied.Contains(cell)) return false;
        _occupied.Add(cell);
        return true;
    }

    /// <summary>월드 좌표 기반 점유 편의 함수.</summary>
    public bool TryOccupyWorld(Vector3 worldPos) => TryOccupy(WorldToCell(worldPos));

    /// <summary>셀 점유 해제 (건물 철거 시).</summary>
    public void Release(Vector2Int cell) => _occupied.Remove(cell);

    /// <summary>월드 좌표 기반 해제 편의 함수.</summary>
    public void ReleaseWorld(Vector3 worldPos) => Release(WorldToCell(worldPos));

    /// <summary>모든 점유 해제 (씬 로드 전 초기화).</summary>
    public void Clear() => _occupied.Clear();

    // -------- 디버그 --------

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        foreach (var cell in _occupied)
        {
            Vector3 center = CellToWorld(cell) + Vector3.up * 0.1f;
            Gizmos.DrawCube(center, new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
        }
    }
}
