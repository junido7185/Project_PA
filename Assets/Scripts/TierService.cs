using System;
using System.Collections.Generic;
using UnityEngine;

// 본사 승인 등급(Tier) 시스템 — Docs/05 참조.
//
// 5단계 등급: 0(생존자) → 1(지점장) → 2(관리자) → 3(사업가) → 4(파트너)
//
// 설계 의도:
// - EconomyService.OnCumulativeRevenueChanged 를 구독하여 매출이 쌓일 때마다 자동으로 승급 조건을 평가한다.
// - IsUnlocked(requiredTier) 한 줄로 건물/아이템/UI 어디서든 잠금 여부를 확인할 수 있다.
// - 조건 정의는 TierDefinition ScriptableObject 에 있어 코드 수정 없이 기획 조정 가능.
// - 저장/로드는 ForceSetTier() 단일 경로로 복구한다.
//
// FSM 없이 단순 int 비교로 충분한 이유:
// 티어는 단조증가(절대 내려가지 않음)이므로 currentTier >= required 비교면 충분하다.
[DefaultExecutionOrder(-90)]   // EconomyService(-100) 보다 늦게, 나머지보다 이른 초기화
public class TierService : MonoBehaviour
{
    public static TierService Instance { get; private set; }

    [Header("등급 정의 에셋 (Tier 0~4 를 가진 TierDefinition SO 5개 등록)")]
    [SerializeField] private List<TierDefinition> tierDefinitions = new List<TierDefinition>();

    [Header("현재 상태 (Inspector 읽기 전용)")]
    [SerializeField] private int _currentTier = 0;
    [SerializeField] private int _reputation = 0;

    /// <summary>현재 등급 (0~4).</summary>
    public int CurrentTier => _currentTier;

    /// <summary>마을 평판 포인트.</summary>
    public int Reputation => _reputation;

    // (이전 티어, 새 티어) — UI 연출·저장 트리거 등에서 구독한다.
    public event Action<int, int> OnTierAdvanced;

    // -------- Unity 생명주기 --------

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // EconomyService 의 누적 매출 갱신 이벤트를 구독 → 자동 승급 평가
        if (EconomyService.Instance != null)
            EconomyService.Instance.OnCumulativeRevenueChanged += OnRevenueChanged;
    }

    void OnDestroy()
    {
        if (EconomyService.Instance != null)
            EconomyService.Instance.OnCumulativeRevenueChanged -= OnRevenueChanged;
    }

    // -------- 공개 API --------

    /// <summary>현재 티어 ≥ requiredTier 이면 true. 건물/아이템 잠금 확인에 사용한다.</summary>
    public bool IsUnlocked(int requiredTier) => _currentTier >= requiredTier;

    /// <summary>다음 티어로 자동 승급 가능한지 확인한다 (requiresManualApproval = true 인 티어는 항상 false).</summary>
    public bool CanAdvanceToNext()
    {
        TierDefinition def = GetDefinition(_currentTier + 1);
        if (def == null) return false;                 // 이미 최고 티어
        if (def.requiresManualApproval) return false;  // 이벤트 전용 승급 티어

        if (def.requiredCumulativeRevenue > 0 &&
            (EconomyService.Instance == null ||
             EconomyService.Instance.CumulativeRevenue < def.requiredCumulativeRevenue))
            return false;

        if (def.requiredReputation > 0 && _reputation < def.requiredReputation)
            return false;

        return true;
    }

    /// <summary>
    /// 조건을 평가하고 충족되면 자동으로 승급한다.
    /// 복수 단계를 한 번에 뛰어넘을 수 있도록 while 루프를 사용한다.
    /// </summary>
    public void EvaluateTierConditions()
    {
        while (CanAdvanceToNext()) AdvanceToNext();
    }

    /// <summary>
    /// 본사 감사 통과 같은 수동 이벤트로만 승급하는 경우 호출한다.
    /// requiresManualApproval 여부와 관계없이 나머지 조건(매출/평판)은 여전히 검사한다.
    /// </summary>
    public bool TryManualAdvance()
    {
        TierDefinition def = GetDefinition(_currentTier + 1);
        if (def == null)
        {
            Debug.Log("🏆 이미 최고 등급입니다.");
            return false;
        }

        if (def.requiredCumulativeRevenue > 0 &&
            (EconomyService.Instance == null ||
             EconomyService.Instance.CumulativeRevenue < def.requiredCumulativeRevenue))
        {
            Debug.LogWarning($"🔒 티어 승급 조건 미달: 누적 매출 {def.requiredCumulativeRevenue}G 필요 " +
                             $"(현재 {EconomyService.Instance?.CumulativeRevenue ?? 0}G)");
            return false;
        }

        if (def.requiredReputation > 0 && _reputation < def.requiredReputation)
        {
            Debug.LogWarning($"🔒 티어 승급 조건 미달: 평판 {def.requiredReputation} 필요 (현재 {_reputation})");
            return false;
        }

        AdvanceToNext();
        return true;
    }

    /// <summary>평판 포인트 변경. 이벤트/NPC 행복도 시스템에서 호출한다.</summary>
    public void AddReputation(int delta)
    {
        _reputation = Mathf.Max(0, _reputation + delta);
        Debug.Log($"⭐ 평판 변경: {delta:+#;-#;0} → {_reputation}");
        EvaluateTierConditions();
    }

    /// <summary>저장/로드 전용 강제 세팅. 일반 게임플레이 코드가 직접 호출해서는 안 된다.</summary>
    public void ForceSetTier(int tier, int reputation, string reason)
    {
        _currentTier = Mathf.Clamp(tier, 0, 4);
        _reputation = Mathf.Max(0, reputation);
        Debug.Log($"💾 TierService[{reason}]: 티어={_currentTier}, 평판={_reputation}");
        // 로드 후에는 자동 평가하지 않는다 — 로드 데이터가 정답이다.
    }

    /// <summary>현재 등급의 TierDefinition 을 반환한다.</summary>
    public TierDefinition GetCurrentDefinition() => GetDefinition(_currentTier);

    // -------- 상점 슬롯 헬퍼 (Docs/05 §3) --------

    /// <summary>
    /// tier 0 부터 인자 tier 까지를 순회하며 마지막으로 설정된 shopSlotCount 를 반환한다.
    /// shopSlotCount=0 인 티어는 이전 값을 유지 (캐스케이드).
    /// 결과가 0 이면 TierDefinition 에 값이 하나도 설정되지 않은 것이다.
    /// </summary>
    public int GetEffectiveShopSlotCount(int tier)
    {
        int result = 0;
        for (int t = 0; t <= Mathf.Clamp(tier, 0, 4); t++)
        {
            TierDefinition def = GetDefinition(t);
            if (def != null && def.shopSlotCount > 0)
                result = def.shopSlotCount;
        }
        return result;
    }

    /// <summary>지정 tier 의 TierDefinition 을 반환한다. 등록되지 않은 tier 이면 null.</summary>
    public TierDefinition GetDefinition(int tier)
    {
        foreach (var def in tierDefinitions)
            if (def != null && def.tier == tier) return def;
        return null;
    }

    // -------- 내부 --------

    private void OnRevenueChanged(long _) => EvaluateTierConditions();

    private void AdvanceToNext()
    {
        int oldTier = _currentTier;
        _currentTier++;

        TierDefinition def = GetDefinition(_currentTier);
        string name = def != null ? def.tierName : $"Tier{_currentTier}";
        Debug.Log($"🏆 티어 승급! Tier{oldTier} → Tier{_currentTier} [{name}]");
        if (def != null && !string.IsNullOrEmpty(def.unlockDescription))
            Debug.Log($"🔓 해금: {def.unlockDescription}");

        OnTierAdvanced?.Invoke(oldTier, _currentTier);
    }

}
