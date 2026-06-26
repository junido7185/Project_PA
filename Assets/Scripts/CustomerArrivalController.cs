using System.Collections.Generic;
using UnityEngine;

// IL/CDN follow-up — 손님 도착 페이싱.
//
// 설계 의도 (PROJECT_PA_CREATIVE_NORTH_STAR.md "Shop Operation Fantasy",
// Docs/IslandLife/GATHERING_AND_SHOP_GATE.md 다음 추천 작업):
// - 플레이어가 밤에 가게를 "열면"(CDN-002 게이트가 손님에게 열리면) 주민 손님이
//   한 명씩 자연스럽게 가게로 모여들게 한다. 영업 시작이 실제로 손님을 부른다는 체감을 준다.
// - 구매 확률/경제/PurchaseEvaluator/NPC FSM 내부는 바꾸지 않는다.
//   기존 NpcController.SetShoppingPriority / TryForceShop (NpcScheduleController 가 쓰는 멱등 공개 API)만 호출한다.
//
// 안전장치:
// - Day 1 튜토리얼(IsTutorialAlwaysOpen)에는 능동 초대를 하지 않는다 — 기존 PlayableDayScenarioController 가
//   Day 1 손님 흐름을 이미 관리하므로 그 페이싱을 보존한다.
// - 일시정지(수면/근무) 또는 이미 쇼핑 중인 NPC 는 TryForceShop 이 스스로 무시하므로 깨우지 않는다.
public class CustomerArrivalController : MonoBehaviour
{
    public static CustomerArrivalController Instance { get; private set; }

    [Header("Arrival Pacing")]
    [Tooltip("상태 폴링 간격(초)")]
    public float pollInterval = 0.5f;
    [Tooltip("손님을 한 명씩 초대하는 간격(초)")]
    public float inviteInterval = 1.6f;
    [Tooltip("동시에 가게로 향하는 손님 최대 수")]
    public int maxConcurrentCustomers = 6;

    bool _wasOpen;
    float _nextPollAt;
    float _nextInviteAt;
    int _invitedThisOpening;

    readonly List<NpcController> _npcBuffer = new List<NpcController>();

    public bool IsOpenForCustomers
    {
        get
        {
            var loop = DayNightShopLoopController.Instance;
            return loop != null && loop.IsShopOpenForCustomers;
        }
    }

    // Day 1 튜토리얼은 능동 초대를 하지 않는다(시나리오 컨트롤러가 담당).
    public bool ShouldActivelyInvite
    {
        get
        {
            var loop = DayNightShopLoopController.Instance;
            return loop != null && loop.IsShopOpenForCustomers && !loop.IsTutorialAlwaysOpen;
        }
    }

    public int InvitedThisOpening => _invitedThisOpening;
    public int ActiveShopperCount => CountActiveShoppers();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (Time.unscaledTime < _nextPollAt) return;
        _nextPollAt = Time.unscaledTime + Mathf.Max(0.1f, pollInterval);

        bool open = ShouldActivelyInvite;
        if (open && !_wasOpen)
            OnShopOpened();
        else if (!open && _wasOpen)
            OnShopClosed();
        _wasOpen = open;

        if (open && Time.time >= _nextInviteAt)
        {
            _nextInviteAt = Time.time + Mathf.Max(0.2f, inviteInterval);
            TryInviteWave(1);
        }
    }

    void OnShopOpened()
    {
        _invitedThisOpening = 0;
        _nextInviteAt = 0f; // 즉시 첫 손님 초대
    }

    void OnShopClosed()
    {
        DisperseCustomers();
    }

    /// <summary>
    /// 활성·대기 중인 손님을 최대 max 명까지 즉시 가게로 초대한다.
    /// 실제로 쇼핑을 시작한(상태가 Idle 이 아닌) 손님 수를 반환한다.
    /// 게이트가 닫혀 있거나 Day 1 튜토리얼이면 0 을 반환한다(능동 초대 안 함).
    /// </summary>
    public int TryInviteWave(int max)
    {
        if (!ShouldActivelyInvite) return 0;

        int slots = maxConcurrentCustomers - CountActiveShoppers();
        if (slots <= 0) return 0;

        int target = Mathf.Min(Mathf.Max(1, max), slots);

        _npcBuffer.Clear();
        _npcBuffer.AddRange(FindObjectsByType<NpcController>(FindObjectsSortMode.None));

        int invited = 0;
        foreach (var npc in _npcBuffer)
        {
            if (invited >= target) break;
            if (npc == null) continue;
            if (npc.currentState != NpcController.State.Idle) continue;

            npc.SetShoppingPriority(true);
            npc.TryForceShop(); // 일시정지/비-Idle 이면 내부에서 무시됨

            if (npc.currentState != NpcController.State.Idle)
            {
                invited++;
                _invitedThisOpening++;
            }
        }

        return invited;
    }

    void DisperseCustomers()
    {
        foreach (var npc in FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            if (npc == null) continue;
            npc.SetShoppingPriority(false);
        }
    }

    int CountActiveShoppers()
    {
        int count = 0;
        foreach (var npc in FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            if (npc == null) continue;
            if (npc.currentState != NpcController.State.Idle)
                count++;
        }
        return count;
    }
}
