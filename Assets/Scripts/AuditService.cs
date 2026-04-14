using UnityEngine;

// 본사 감사(Audit) 시스템 — Docs/05 §1 Tier 4 "파트너" 승급 조건.
//
// 설계 의도:
// - Tier 4 는 `requiresManualApproval = true` 이므로 자동 승급되지 않는다.
// - 대신 "본사 감사" 이벤트가 주기적으로 발생하여 조건을 충족하면 TryManualAdvance() 를 호출.
// - 감사 조건:
//   1) 누적 매출 >= requiredRevenueForAudit
//   2) 평판 >= requiredReputationForAudit
//   3) 고용 NPC 수 >= requiredHiredNpcs
//   모두 충족해야 통과.
//
// 실행 흐름:
//   GameClock.OnNewDay 구독 → auditIntervalDays 마다 감사 실행 →
//   조건 미달: 경고 로그 → 조건 충족: TierService.TryManualAdvance()
//
// 감사 "예고":
//   감사 1일 전에 Debug.Log 를 남겨 플레이어에게 암시한다 (향후 UI 알림으로 교체).
//
// 저장/로드:
//   lastAuditDay 를 SaveData 에 보관 → ForceSetLastAuditDay() 로 복구 (Task #24).
[DefaultExecutionOrder(-40)]
public class AuditService : MonoBehaviour
{
    public static AuditService Instance { get; private set; }

    [Header("감사 주기")]
    [Tooltip("N 일마다 감사 실행. 기본 7일 (1 계절)")]
    public int auditIntervalDays = 7;

    [Header("통과 조건 (모두 AND)")]
    [Tooltip("누적 매출 기준")]
    public long requiredRevenueForAudit = 500000;

    [Tooltip("평판 기준")]
    public int requiredReputationForAudit = 5;

    [Tooltip("고용 NPC 최소 수")]
    public int requiredHiredNpcs = 3;

    [Header("상태 (Inspector 읽기 전용)")]
    [SerializeField] private int _lastAuditDay = 0;
    [SerializeField] private string _debugLastResult = "미실시";

    /// <summary>마지막 감사가 실시된 게임 일수.</summary>
    public int LastAuditDay => _lastAuditDay;

    // -------- Unity 생명주기 --------

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.OnNewDay += OnNewDay;
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.OnNewDay -= OnNewDay;
    }

    // -------- 이벤트 구독 --------

    private void OnNewDay(int day)
    {
        // 감사 1일 전 예고
        if (auditIntervalDays > 1 && (day - _lastAuditDay) == auditIntervalDays - 1)
        {
            Debug.Log($"🏛️ 본사 통보: 내일 정기 감사가 예정되어 있습니다. (Day {day + 1})");
        }

        // 감사 실행 여부 확인
        if (day - _lastAuditDay < auditIntervalDays) return;

        RunAudit(day);
    }

    // -------- 공개 API --------

    /// <summary>강제 감사 실행 (디버그/이벤트 트리거용).</summary>
    public void ForceRunAudit()
    {
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : _lastAuditDay + auditIntervalDays;
        RunAudit(day);
    }

    /// <summary>저장/로드용 강제 세팅.</summary>
    public void ForceSetLastAuditDay(int day) => _lastAuditDay = Mathf.Max(0, day);

    // -------- 감사 실행 --------

    private void RunAudit(int day)
    {
        _lastAuditDay = day;
        Debug.Log($"🏛️ ===== 본사 정기 감사 실시 (Day {day}) =====");

        // Tier 4 가 이미 달성되었으면 축하만 하고 종료
        if (TierService.Instance != null && TierService.Instance.CurrentTier >= 4)
        {
            _debugLastResult = $"Day {day}: 이미 최고 등급";
            Debug.Log("🏛️ 결과: 이미 파트너(Tier 4) 등급입니다. 축하드립니다!");
            return;
        }

        // 조건 평가
        bool revenueOk = true;
        bool reputationOk = true;
        bool hiringOk = true;

        long currentRevenue = EconomyService.Instance != null ? EconomyService.Instance.CumulativeRevenue : 0;
        int currentReputation = TierService.Instance != null ? TierService.Instance.Reputation : 0;
        int hiredCount = HiringService.Instance != null ? HiringService.Instance.HiredCount : 0;

        if (currentRevenue < requiredRevenueForAudit)
        {
            Debug.Log($"🏛️ ❌ 누적 매출 미달: {currentRevenue:N0}G / {requiredRevenueForAudit:N0}G");
            revenueOk = false;
        }
        if (currentReputation < requiredReputationForAudit)
        {
            Debug.Log($"🏛️ ❌ 평판 미달: {currentReputation} / {requiredReputationForAudit}");
            reputationOk = false;
        }
        if (hiredCount < requiredHiredNpcs)
        {
            Debug.Log($"🏛️ ❌ 고용 NPC 미달: {hiredCount}명 / {requiredHiredNpcs}명");
            hiringOk = false;
        }

        if (!revenueOk || !reputationOk || !hiringOk)
        {
            _debugLastResult = $"Day {day}: 감사 미달";
            Debug.Log("🏛️ 결과: 본사 감사 미통과. 다음 감사를 준비하세요.");
            return;
        }

        // 모든 조건 충족 — 수동 승급 시도
        Debug.Log("🏛️ ✅ 모든 감사 조건 충족! 승급 심사를 진행합니다...");
        if (TierService.Instance != null && TierService.Instance.TryManualAdvance())
        {
            _debugLastResult = $"Day {day}: 감사 통과 → 승급!";
            Debug.Log("🏛️ 🎉 축하합니다! 본사 감사를 통과하여 등급이 승급되었습니다!");
        }
        else
        {
            // TierService 측 조건(매출/평판 기준이 다를 수 있음)에서 막힌 경우
            _debugLastResult = $"Day {day}: 감사 통과했으나 TierService 조건 미달";
            Debug.Log("🏛️ 감사는 통과했지만 본사 내부 기준이 추가로 필요합니다.");
        }
    }
}
