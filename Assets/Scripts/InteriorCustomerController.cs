using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// S3 — 실내 잡화점 손님 사이드카.
//
// 목적: PA_StoreInterior(BuildingEntrance Y+100 실내)가 "진짜 상점"이 되도록,
// 영업 중(밤, 간판 개점)이고 실내 진열대에 상품이 있으면 지상 주민 한 명을
// 실내로 들여보내 기존 NpcController FSM(MovingToShop→Browsing→구매/거절)으로
// 쇼핑시키고, 끝나면 원래 자리로 돌려보낸다.
//
// 안전 규칙:
// - NPC FSM/구매 수학 재작성 없음 — NpcController.TryBeginShoppingVisitAt/RetargetShop 훅만 사용.
// - Day 1 튜토리얼(항상 열림)에는 개입하지 않는다 → 검증된 첫 판매 루트 보존.
// - 씬 무수정. 실내 NavMesh 아일랜드는 에디터 리베이크로 준비되고, 진입/복귀는 agent.Warp.
public class InteriorCustomerController : MonoBehaviour
{
    public static InteriorCustomerController Instance { get; private set; }

    [Header("Interior Visit")]
    public float inviteInterval = 14f;      // 초대 최소 간격
    public float visitTimeout = 45f;        // 이 시간 넘으면 강제 복귀
    public int maxConcurrentVisitors = 2; // S5 — 실내 동시 손님 2명 (붐비는 가게 인상)

    Transform _interiorShop;                // Shop 컴포넌트 홀더 (PA_StoreInterior)
    Shop _interiorShopComponent;
    Transform _insideSpawn;
    float _nextInviteAt;
    float _nextPollAt;

    class Visitor
    {
        public NpcController npc;
        public Vector3 returnPosition;
        public Transform originalShopLocation;
        public float startedAt;
    }

    readonly List<Visitor> _visitors = new List<Visitor>();

    public int ActiveVisitorCount => _visitors.Count;
    public NpcController CurrentVisitor => _visitors.Count > 0 ? _visitors[0].npc : null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (Time.unscaledTime < _nextPollAt) return;
        _nextPollAt = Time.unscaledTime + 1.0f;

        if (!TryCacheInterior()) return;

        ReturnFinishedVisitors();

        if (!IsInteriorOpenForVisits()) { RecallAllVisitors("영업 종료"); return; }

        if (_visitors.Count < Mathf.Max(1, maxConcurrentVisitors)
            && Time.timeSinceLevelLoad >= _nextInviteAt)
        {
            if (TryInviteOne() != null)
                _nextInviteAt = Time.timeSinceLevelLoad + Mathf.Max(4f, inviteInterval);
        }
    }

    bool TryCacheInterior()
    {
        if (_interiorShopComponent != null && _insideSpawn != null) return true;

        var interiorRoot = GameObject.Find("PA_StoreInterior");
        if (interiorRoot == null) return false;

        _interiorShopComponent = interiorRoot.GetComponent<Shop>();
        _interiorShop = interiorRoot.transform;

        var spawn = interiorRoot.transform.Find("PlayerSpawn_Inside");
        _insideSpawn = spawn != null ? spawn : interiorRoot.transform;

        return _interiorShopComponent != null;
    }

    // 실내 방문 조건: Day 1 튜토리얼 제외 + 손님 구매 게이트 열림 + 실내에 진열 상품 존재.
    bool IsInteriorOpenForVisits()
    {
        var loop = DayNightShopLoopController.Instance;
        if (loop == null) return false;
        if (loop.IsTutorialAlwaysOpen) return false;
        if (!loop.IsShopOpenForCustomers) return false;

        return _interiorShopComponent != null
            && _interiorShopComponent.GetAvailableSlots().Count > 0;
    }

    // 지상의 Idle 주민 한 명을 실내로 들여보낸다. 성공 시 해당 NPC 반환.
    public NpcController TryInviteOne()
    {
        if (!TryCacheInterior()) return null;

        foreach (var npc in FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            if (npc == null || npc.currentState != NpcController.State.Idle) continue;
            if (npc.transform.position.y > 50f) continue;                       // 이미 실내
            if (npc.name.Contains("Bori") || npc.name.Contains("FirstSettler")) continue;
            if (IsVisitor(npc)) continue;

            var agent = npc.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) continue;

            var visitor = new Visitor
            {
                npc = npc,
                returnPosition = npc.transform.position,
                originalShopLocation = npc.shopLocation,
                startedAt = Time.timeSinceLevelLoad
            };

            // 실내 NavMesh 아일랜드로 워프 후 기존 FSM 으로 쇼핑 시작.
            Vector3 target = _insideSpawn.position;
            bool sampled = NavMesh.SamplePosition(target, out var hit, 2.5f, NavMesh.AllAreas);
            if (sampled) target = hit.position;
            bool warped = agent.Warp(target);
            Debug.Log($"🚪 [InteriorCustomer] warp diag: sampled={sampled} hit={(sampled ? hit.position.ToString() : "-")} warped={warped} agentPos={agent.transform.position} onMesh={agent.isOnNavMesh}");

            if (!npc.TryBeginShoppingVisitAt(_interiorShop))
            {
                // 시작 실패 — 원위치 복구.
                agent.Warp(visitor.returnPosition);
                npc.RetargetShop(visitor.originalShopLocation);
                continue;
            }

            _visitors.Add(visitor);
            Debug.Log($"🚪 [InteriorCustomer] {npc.name} 실내 잡화점 입장");
            return npc;
        }

        return null;
    }

    bool IsVisitor(NpcController npc)
    {
        foreach (var v in _visitors)
            if (v.npc == npc) return true;
        return false;
    }

    void ReturnFinishedVisitors()
    {
        for (int i = _visitors.Count - 1; i >= 0; i--)
        {
            var v = _visitors[i];
            if (v.npc == null) { _visitors.RemoveAt(i); continue; }

            bool finished = v.npc.currentState == NpcController.State.Idle;
            bool timedOut = Time.timeSinceLevelLoad - v.startedAt > Mathf.Max(10f, visitTimeout);
            if (!finished && !timedOut) continue;

            ReturnVisitor(v, timedOut ? "시간 초과" : "쇼핑 종료");
            _visitors.RemoveAt(i);
        }
    }

    void RecallAllVisitors(string reason)
    {
        for (int i = _visitors.Count - 1; i >= 0; i--)
        {
            if (_visitors[i].npc != null)
                ReturnVisitor(_visitors[i], reason);
            _visitors.RemoveAt(i);
        }
    }

    void ReturnVisitor(Visitor v, string reason)
    {
        var agent = v.npc.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isActiveAndEnabled)
            agent.Warp(v.returnPosition);
        else
            v.npc.transform.position = v.returnPosition;

        // 광장 상점 참조 복구 — 스테일 _activeShop 방지 (RetargetShop 필수).
        v.npc.RetargetShop(v.originalShopLocation);
        if (v.npc.currentState != NpcController.State.Idle)
            v.npc.currentState = NpcController.State.Idle;

        Debug.Log($"🚪 [InteriorCustomer] {v.npc.name} 퇴장 ({reason})");
    }
}
