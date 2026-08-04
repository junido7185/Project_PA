using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// P4 — ShopSlot 앞의 명시적 접근 셀을 실제 NavMesh 목적지로 연결하는 런타임 사이드카.
// 기존 ShopSlot claim은 결제 원자성을 계속 담당하고, 이 클래스는 이동 중 군집 방지만 담당한다.
[DefaultExecutionOrder(90)]
public sealed class ShopCustomerApproachController : MonoBehaviour
{
    const float SampleRadius = 0.9f;
    const float SourcePointTolerance = 0.2f;

    public static ShopCustomerApproachController Instance { get; private set; }
    public int ActiveReservationCount => _bySlot.Count;

    sealed class Reservation
    {
        public NpcController owner;
        public ShopSlot slot;
        public Vector3 sourcePoint;
        public Vector3 navPoint;
    }

    readonly Dictionary<ShopSlot, Reservation> _bySlot = new Dictionary<ShopSlot, Reservation>();
    readonly Dictionary<int, Reservation> _byOwner = new Dictionary<int, Reservation>();
    readonly List<Vector3> _sourcePoints = new List<Vector3>(4);
    readonly List<Vector2Int> _zoneCells = new List<Vector2Int>(4);
    float _nextCleanupAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindFirstObjectByType<ShopCustomerApproachController>() != null) return;
        new GameObject("PA_ShopCustomerApproach").AddComponent<ShopCustomerApproachController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (Time.unscaledTime < _nextCleanupAt) return;
        _nextCleanupAt = Time.unscaledTime + 1f;

        var stale = new List<NpcController>();
        foreach (var reservation in _bySlot.Values)
        {
            if (reservation == null || reservation.owner == null || reservation.slot == null
                || !reservation.slot.gameObject.activeInHierarchy)
                stale.Add(reservation != null ? reservation.owner : null);
        }

        foreach (var owner in stale)
        {
            if (owner != null) Release(owner);
        }

        // 파괴된 owner는 Unity null 비교 때문에 위 목록에서 ID를 복구할 수 없으므로 직접 정리한다.
        var staleOwnerIds = new List<int>();
        foreach (var pair in _byOwner)
            if (pair.Value == null || pair.Value.owner == null || pair.Value.slot == null)
                staleOwnerIds.Add(pair.Key);
        foreach (int ownerId in staleOwnerIds)
        {
            if (_byOwner.TryGetValue(ownerId, out var reservation) && reservation != null && reservation.slot != null)
                _bySlot.Remove(reservation.slot);
            _byOwner.Remove(ownerId);
        }
    }

    void OnDestroy()
    {
        _bySlot.Clear();
        _byOwner.Clear();
        if (Instance == this) Instance = null;
    }

    public bool TryReserveReachableSlot(NpcController owner, NavMeshAgent agent,
        IList<ShopSlot> candidates, System.Random rng, out ShopSlot slot, out Vector3 approachPoint,
        out string reason)
    {
        slot = null;
        approachPoint = Vector3.zero;
        reason = string.Empty;

        if (owner == null || agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
        { reason = "NPC가 NavMesh 위에 있지 않습니다."; return false; }
        if (candidates == null || candidates.Count == 0)
        { reason = "접근 가능한 진열 후보가 없습니다."; return false; }

        Release(owner);
        int start = rng != null ? rng.Next(candidates.Count) : 0;
        for (int offset = 0; offset < candidates.Count; offset++)
        {
            ShopSlot candidate = candidates[(start + offset) % candidates.Count];
            if (candidate == null || candidate.IsEmpty || !candidate.gameObject.activeInHierarchy) continue;
            if (_bySlot.TryGetValue(candidate, out var occupied) && occupied.owner != owner) continue;

            if (!TryResolveReachablePoint(agent, candidate, out Vector3 sourcePoint,
                    out Vector3 navPoint, out _))
                continue;

            var reservation = new Reservation
            {
                owner = owner,
                slot = candidate,
                sourcePoint = sourcePoint,
                navPoint = navPoint
            };
            _bySlot[candidate] = reservation;
            _byOwner[owner.GetInstanceID()] = reservation;
            slot = candidate;
            approachPoint = navPoint;
            return true;
        }

        reason = "비어 있고 완전한 NavMesh 경로를 가진 진열대 앞자리가 없습니다.";
        return false;
    }

    public void Release(NpcController owner)
    {
        if (owner == null) return;
        int ownerId = owner.GetInstanceID();
        if (!_byOwner.TryGetValue(ownerId, out var reservation)) return;
        if (reservation != null && reservation.slot != null
            && _bySlot.TryGetValue(reservation.slot, out var current) && current == reservation)
            _bySlot.Remove(reservation.slot);
        _byOwner.Remove(ownerId);
    }

    public bool IsReservationValid(NpcController owner, ShopSlot slot)
    {
        if (owner == null || slot == null
            || !_byOwner.TryGetValue(owner.GetInstanceID(), out var reservation)
            || reservation == null || reservation.slot != slot || !slot.gameObject.activeInHierarchy)
            return false;

        ResolveSourcePoints(slot, _sourcePoints, _zoneCells, out _);
        foreach (Vector3 current in _sourcePoints)
        {
            Vector2 delta = new Vector2(current.x - reservation.sourcePoint.x, current.z - reservation.sourcePoint.z);
            if (delta.sqrMagnitude <= SourcePointTolerance * SourcePointTolerance) return true;
        }
        return false;
    }

    public bool TryGetReservation(NpcController owner, out ShopSlot slot, out Vector3 approachPoint)
    {
        slot = null;
        approachPoint = Vector3.zero;
        if (owner == null || !_byOwner.TryGetValue(owner.GetInstanceID(), out var reservation)
            || reservation == null || reservation.slot == null)
            return false;
        slot = reservation.slot;
        approachPoint = reservation.navPoint;
        return true;
    }

    public bool IsReservedBy(ShopSlot slot, NpcController owner)
    {
        return slot != null && owner != null && _bySlot.TryGetValue(slot, out var reservation)
            && reservation != null && reservation.owner == owner;
    }

    // 검증기와 진단 UI가 실제 SamplePosition/CalculatePath 결과를 같은 코드 경로로 확인한다.
    public bool TryResolveReachablePoint(NavMeshAgent agent, ShopSlot slot,
        out Vector3 sourcePoint, out Vector3 navPoint, out NavMeshPathStatus pathStatus)
    {
        sourcePoint = Vector3.zero;
        navPoint = Vector3.zero;
        pathStatus = NavMeshPathStatus.PathInvalid;
        if (agent == null || slot == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return false;

        ResolveSourcePoints(slot, _sourcePoints, _zoneCells, out _);
        int areaMask = agent.areaMask != 0 ? agent.areaMask : NavMesh.AllAreas;
        float bestLength = float.PositiveInfinity;
        bool found = false;

        foreach (Vector3 candidate in _sourcePoints)
        {
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, SampleRadius, areaMask)) continue;
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(agent.transform.position, hit.position, areaMask, path)
                || path.status != NavMeshPathStatus.PathComplete)
                continue;

            float length = PathLength(path);
            if (length >= bestLength) continue;
            bestLength = length;
            sourcePoint = candidate;
            navPoint = hit.position;
            pathStatus = path.status;
            found = true;
        }
        return found;
    }

    void ResolveSourcePoints(ShopSlot slot, List<Vector3> points, List<Vector2Int> cells,
        out bool usesAuthoredGrid)
    {
        points.Clear();
        cells.Clear();
        usesAuthoredGrid = false;

        var customization = ShopCustomizationController.Instance;
        if (customization != null
            && customization.TryGetNpcApproachPoints(slot, points, cells, out _))
        {
            usesAuthoredGrid = true;
            return;
        }

        // 구형 야외 ShopSlot은 P2 zone 장부에 없으므로 모델의 -forward 앞면을 호환점으로 쓴다.
        Vector3 fallback = slot.transform.position - slot.transform.forward * 1.15f;
        fallback.y = slot.transform.position.y;
        points.Add(fallback);
    }

    static float PathLength(NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length < 2) return 0f;
        float result = 0f;
        for (int i = 1; i < path.corners.Length; i++)
            result += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        return result;
    }
}
