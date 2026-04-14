using UnityEngine;
using UnityEngine.AI;

// NPC 하루 일과를 GameClock 에 맞춰 조율하는 오케스트레이터.
//
// 역할:
// - GameClock.OnHourTick 을 구독해 정각마다 현재 페이즈를 재평가한다.
// - NpcDailySchedule.GetPhaseAt() 으로 스케줄상 페이즈를 확인한 뒤,
//   J/P 축 drift 확률을 적용해 실제 수행 페이즈를 결정한다.
// - 결정된 페이즈에 따라 NpcController(소비형) / ProducerNpcController(생산형) 을
//   Pause / Resume 으로 게이팅한다.
//
// J/P 축 반영 (Docs/03 §2.1):
//   J형(traitJP=-1): 스케줄을 엄격하게 준수, 이탈 확률 0%.
//   P형(traitJP=+1): 스케줄에서 이탈해 Shopping 또는 Afternoon 으로 전환. 최대 30%.
//
// 씬 설정:
//   이 컴포넌트를 NPC GameObject 에 붙이고,
//   profile / scheduleData / consumerController / producerController 를 연결한다.
//   GameClock 이 씬에 없으면 일과 기능이 비활성 (경고 출력).
public class NpcScheduleController : MonoBehaviour
{
    [Header("프로필 & 스케줄 에셋")]
    [Tooltip("MBTI 성향. null이면 J형 기본값 사용 (스케줄 엄격 준수)")]
    public NpcProfile profile;

    [Tooltip("일과표 ScriptableObject. null이면 스케줄 기능 비활성")]
    public NpcDailySchedule scheduleData;

    [Header("서브 컨트롤러 참조")]
    [Tooltip("소비형 NPC (배회·쇼핑). 없어도 무방")]
    public NpcController consumerController;

    [Tooltip("생산형 NPC (채집·납품). 없어도 무방")]
    public ProducerNpcController producerController;

    [Tooltip("전문가 NPC (Workbench 자동 가공). 없어도 무방")]
    public SpecialistNpcController specialistController;

    [Header("귀가 설정")]
    [Tooltip("Sleep / Rest 페이즈에 NPC 가 걸어갈 목적지. null 이면 제자리 정지.")]
    public Transform homePoint;

    [Header("디버그 (읽기 전용)")]
    [SerializeField] private SchedulePhase _scheduledPhase;  // 스케줄상 원래 페이즈
    [SerializeField] private SchedulePhase _activePhase;     // 실제 수행 중 (P형 이탈 포함)
    [SerializeField] private string _debugStatus = "초기화 전";

    private System.Random _rng;
    private NavMeshAgent _agent;

    // -------- Unity 생명주기 --------

    void Awake()
    {
        // 결정론 시드 — 다른 RNG 와 동일한 원칙 (UnityEngine.Random 금지)
        string seedStr = (profile != null ? profile.npcName : gameObject.name)
                         + "::Schedule::" + gameObject.GetInstanceID();
        _rng = new System.Random(seedStr.GetHashCode());

        _agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (scheduleData == null)
        {
            _debugStatus = "scheduleData 없음 — 비활성";
            return;
        }

        if (GameClock.Instance == null)
        {
            Debug.LogWarning($"⏰ [{DisplayName}] NpcScheduleController: 씬에 GameClock 이 없어 일과 기능이 비활성됩니다.");
            _debugStatus = "GameClock 없음 — 비활성";
            return;
        }

        // 이벤트 구독
        GameClock.Instance.OnHourTick += OnHourTick;
        GameClock.Instance.OnNewDay   += OnNewDay;

        // 씬 시작 시점의 페이즈 즉시 적용
        EvaluateAndApply();
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null)
        {
            GameClock.Instance.OnHourTick -= OnHourTick;
            GameClock.Instance.OnNewDay   -= OnNewDay;
        }
    }

    // -------- 이벤트 구독 --------

    private void OnHourTick(int hour) => EvaluateAndApply();

    private void OnNewDay(int day)
    {
        Debug.Log($"⏰ [{DisplayName}] Day {day} — 일과 재평가");
        EvaluateAndApply();
    }

    // -------- 핵심 평가 로직 --------

    void EvaluateAndApply()
    {
        if (scheduleData == null || GameClock.Instance == null) return;

        float hour = GameClock.Instance.CurrentHour;
        SchedulePhase scheduled = scheduleData.GetPhaseAt(hour);
        _scheduledPhase = scheduled;

        // J/P 축 drift:
        // traitJP: -1(J,엄격) ~ +1(P,돌발). max(0, JP) × 0.30 = 이탈 확률
        float jp = profile != null ? profile.traitJP : -1f;  // 기본값: J형 (이탈 없음)
        float driftChance = Mathf.Max(0f, jp) * 0.30f;

        SchedulePhase resolved = scheduled;

        if (driftChance > 0f && _rng.NextDouble() < driftChance)
        {
            // P형 돌발: 쇼핑(60%) 또는 자유 배회(40%) 로 이탈
            resolved = _rng.NextDouble() < 0.6
                ? SchedulePhase.Shopping
                : SchedulePhase.Afternoon;

            _debugStatus = $"P형 이탈! 예정:{scheduled} → 실제:{resolved} ({hour:F1}시)";
            Debug.Log($"⏰ [{DisplayName}] {_debugStatus}");
        }
        else
        {
            _debugStatus = $"페이즈:{resolved} ({hour:F1}시)";
        }

        _activePhase = resolved;
        ApplyPhase(resolved);
    }

    // -------- 페이즈 적용 --------

    void ApplyPhase(SchedulePhase phase)
    {
        // 1. 전체 정지
        consumerController?.Pause();
        producerController?.Pause();
        specialistController?.Pause();

        // 2. 페이즈별 서브 컨트롤러 활성화
        switch (phase)
        {
            // ---- 정지 상태 — homePoint 로 귀가 후 대기 ----
            case SchedulePhase.Sleep:
            case SchedulePhase.Rest:
                ReturnHome();
                break;

            // ---- 기상·준비 ----
            case SchedulePhase.WakeUp:
                // 낮은 강도의 배회 (쇼핑 우선도 없음)
                if (consumerController != null)
                {
                    consumerController.SetShoppingPriority(false);
                    consumerController.Resume();
                }
                break;

            // ---- 작업·생산 ----
            case SchedulePhase.Work:
                // 생산 NPC + 전문가 NPC 활성화 (소비 NPC 는 작업 중)
                producerController?.Resume();
                specialistController?.Resume();
                break;

            // ---- 자유 배회 ----
            case SchedulePhase.Lunch:
            case SchedulePhase.Afternoon:
            case SchedulePhase.Evening:
                if (consumerController != null)
                {
                    consumerController.SetShoppingPriority(false);
                    consumerController.Resume();
                }
                break;

            // ---- 쇼핑 우선 ----
            case SchedulePhase.Shopping:
                if (consumerController != null)
                {
                    consumerController.SetShoppingPriority(true);
                    consumerController.Resume();
                    // 이미 Idle 이면 즉시 쇼핑 시작 유도
                    consumerController.TryForceShop();
                }
                break;
        }
    }

    // -------- 귀가 --------

    // Sub-controller 들이 Pause() 에서 agent.ResetPath() 를 호출한 직후 실행된다.
    // homePoint 가 연결되어 있으면 NavMeshAgent 로 걸어가고,
    // 도착 후에는 서브 컨트롤러가 비활성 상태이므로 자연히 그 자리에 멈춘다.
    void ReturnHome()
    {
        if (_agent == null || homePoint == null) return;
        if (!_agent.isOnNavMesh) return;

        _agent.isStopped = false;
        _agent.SetDestination(homePoint.position);
        _debugStatus = $"귀가 중 → {homePoint.name}";
    }

    // -------- 공개 API (외부 이벤트 트리거용) --------

    /// <summary>
    /// 외부 이벤트(감사, 특수 행사 등)로 스케줄을 즉시 재평가한다.
    /// GameClock 의 정각 이벤트 없이도 페이즈를 갱신할 때 사용한다.
    /// </summary>
    public void ForceReevaluate() => EvaluateAndApply();

    // -------- 헬퍼 --------

    private string DisplayName =>
        profile != null && !string.IsNullOrEmpty(profile.npcName)
            ? profile.npcName
            : gameObject.name;
}
