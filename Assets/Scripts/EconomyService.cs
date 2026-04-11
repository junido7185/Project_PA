using System;
using UnityEngine;

// 프로젝트 전체의 '공유 지갑'을 소유하고 모든 돈 변경의 단일 경로가 되는 서비스.
//
// 설계 의도:
// - 멀티플레이 전환 시 "스타듀밸리형 공유 지갑"을 그대로 유지한다.
//   현재는 단일 인스턴스가 int 를 들고 있지만,
//   나중에 내부 _money 를 NetworkVariable<int> 로만 교체하면 호스트 권한 공유 지갑이 된다.
// - 모든 변경은 TryModifyMoney 한 경로로만 발생한다.
//   나중에 이 함수만 [ServerRpc] 로 감싸면 치팅 방지 + 권한 검증이 완료된다.
// - OnMoneyChanged 이벤트로 UI/시스템이 구독한다 (Observer 패턴, Docs/04 원칙).
// - _cumulativeRevenue 는 입금(Deposit)만 누적한다 — TierService 의 승급 조건 기준.
[DefaultExecutionOrder(-100)]
public class EconomyService : MonoBehaviour
{
    public static EconomyService Instance { get; private set; }

    [SerializeField] private int _money = 0;

    // 누적 총매출 — Deposit 호출 시만 증가한다. TierService 의 승급 조건에 사용.
    [SerializeField] private long _cumulativeRevenue = 0;

    // 현재 잔액(읽기 전용).
    public int Money => _money;

    // 지금까지 벌어들인 누적 총매출(읽기 전용).
    public long CumulativeRevenue => _cumulativeRevenue;

    // 잔액이 변경될 때마다 호출된다. 파라미터: 변경 후 잔액.
    public event Action<int> OnMoneyChanged;

    // 누적 매출이 변경될 때마다 호출된다. TierService 가 구독하여 승급 조건을 평가한다.
    public event Action<long> OnCumulativeRevenueChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 초기 구독자들에게 현재 잔액을 브로드캐스트.
        OnMoneyChanged?.Invoke(_money);
    }

    // 모든 돈 변경의 단일 진입점.
    // 음수 delta가 잔액을 0 밑으로 만들면 실패를 반환하고 값을 변경하지 않는다.
    // reason은 디버깅/감사 로그 용도.
    public bool TryModifyMoney(int delta, string reason)
    {
        int next = _money + delta;
        if (next < 0)
        {
            Debug.LogWarning($"💸 EconomyService: 잔액 부족 ({reason}): 요청 {delta}, 현재 {_money}");
            return false;
        }

        _money = next;
        Debug.Log($"💰 EconomyService[{reason}]: {delta:+#;-#;0}G → {_money}G");
        OnMoneyChanged?.Invoke(_money);
        return true;
    }

    // 지출 편의 함수 — 양수를 받아 내부적으로 음수 delta로 변환.
    public bool TrySpend(int amount, string reason)
    {
        if (amount < 0) return false;
        return TryModifyMoney(-amount, reason);
    }

    // 수입 편의 함수 — 양수 입력 전용. 누적 매출에도 반영된다.
    public bool Deposit(int amount, string reason)
    {
        if (amount < 0) return false;
        if (!TryModifyMoney(amount, reason)) return false;

        // 누적 매출 기록 — Deposit 경로만 누적한다 (지출은 포함하지 않음).
        _cumulativeRevenue += amount;
        OnCumulativeRevenueChanged?.Invoke(_cumulativeRevenue);
        return true;
    }

    // 저장/로드에서만 쓰는 강제 세팅. 일반 게임플레이 코드가 호출해서는 안 된다.
    // (MVP 단계에서는 public이지만, 멀티 전환 시 [ServerOnly] 권한이 부여될 자리)
    public void ForceSet(int value, string reason)
    {
        _money = Mathf.Max(0, value);
        Debug.Log($"💾 EconomyService[{reason}]: 강제 세팅 → {_money}G");
        OnMoneyChanged?.Invoke(_money);
    }

    // 누적 매출 복구 전용 — 저장/로드 이외 용도 사용 금지.
    public void ForceSetCumulativeRevenue(long value, string reason)
    {
        _cumulativeRevenue = Math.Max(0L, value);
        Debug.Log($"💾 EconomyService[{reason}]: 누적 매출 복구 → {_cumulativeRevenue}G");
        OnCumulativeRevenueChanged?.Invoke(_cumulativeRevenue);
    }
}
