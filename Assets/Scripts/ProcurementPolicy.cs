using UnityEngine;

public enum ProcurementMode { PaidDirectPurchase, SettlementSupport, Contract, DelegatedProcurement }

// Docs/02_IMPLEMENTATION/PROJECT_PA_CODE_REUSE_MAP_v1.md §6.1:
// 조달 자격만 소유한다. 생산·재고는 Producer, 플레이어 아이템은 Inventory가 소유한다.
public sealed class ProcurementPolicy
{
    readonly ProducerNpcController _producer;
    bool _transferring;

    internal ProcurementPolicy(ProducerNpcController producer) { _producer = producer; }

    // 유료 납품은 기존 Producer 경로로 유지하며, 나머지 두 모드는 이름만 예약한다.
    public ProcurementMode Mode { get; private set; } = ProcurementMode.PaidDirectPurchase;
    public bool Claimed { get; private set; }
    public bool BatchReady => Mode == ProcurementMode.SettlementSupport && !Claimed && _producer.StockCount > 0;
    internal bool HoldProduction => Mode == ProcurementMode.SettlementSupport && (Claimed || BatchReady || _transferring);

    internal bool BeginSettlementSupport()
    {
        // 같은 생산자를 다시 바인딩해도 이미 소비한 자격이나 보관 재고를 초기화하지 않는다.
        if (Mode == ProcurementMode.SettlementSupport) return false;
        Mode = ProcurementMode.SettlementSupport;
        return true;
    }

    // 기존 저장 어댑터 전용. 정상 재바인딩과 저장 시점 복원을 분리한다.
    internal void RestoreSettlementSupport(bool claimed)
    {
        Mode = ProcurementMode.SettlementSupport;
        Claimed = claimed;
    }

    public bool TryClaimSettlementSupport(Inventory inventory)
    {
        if (!BatchReady || _transferring || inventory == null) return false;
        _transferring = true;
        try
        {
            // Inventory의 UI 콜백이 재진입해도 동일 재고를 두 번 수령할 수 없다.
            if (!_producer.TryTransferSingleStock(inventory)) return false;
            Claimed = true;
            _producer.Pause();
            Debug.Log("[VS-P4] STARTER_CLAIM_ONCE " + _producer.productionData.producedItem.itemName);
            return true;
        }
        finally { _transferring = false; }
    }
}
