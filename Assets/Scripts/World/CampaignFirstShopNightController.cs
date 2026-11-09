using System.Linq;
using UnityEngine;

// CONTENT-002 / A02. 기존 개점·가격 확정·판단·실제 판매·정산 결과만 기록한다.
// 손님 초대, 가격 판단, 재고와 돈의 변경은 기존 권위가 수행한다.
[DisallowMultipleComponent]
public sealed class CampaignFirstShopNightController : MonoBehaviour
{
    CampaignOpeningController _opening;
    CampaignFirstNightSaveData _progress = new CampaignFirstNightSaveData();
    WorldGameplayAdapterService Adapter => WorldGameplayAdapterService.Instance;
    bool Active => _opening != null && _opening.HasStarted;
    int Day => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    public bool HasObservedDecision => _progress.firstDecisionDay > 0;
    public bool HasFirstSale => _progress.firstSaleDay > 0;
    public bool Complete => _progress.priceConfirmedDay > 0 && _progress.firstOpenedDay > 0 &&
        HasObservedDecision && _progress.firstSettlementDay >= Mathf.Max(
            _progress.priceConfirmedDay, _progress.firstDecisionDay, _progress.firstOpenedDay);
    public bool NeedsGuidance => Active && !Complete;

    public string Objective
    {
        get
        {
            var loop = DayNightShopLoopController.Instance;
            if (loop?.CurrentPhase == PADayNightPhase.Settlement)
                return "간판에서 오늘 영업을 정산하세요. 매출이 없어도 내일을 시작할 수 있어요.";
            if (Adapter != null && Adapter.RuntimeShopSlots.All(slot => slot == null || slot.IsEmpty))
                return HasFirstSale ? "첫 물건이 팔렸어요. 더 진열하거나 오늘 정산을 기다려 보세요."
                    : "잡화점의 빈 판매대에 Space로 준비한 상품을 진열하세요.";
            if (_progress.priceConfirmedDay == 0)
                return "진열한 상품을 다시 Space로 살펴보고 가격을 확정하세요. 추천가도 참고할 수 있어요.";
            if (loop?.CurrentPhase == PADayNightPhase.DayPreparation)
                return "첫 진열을 마쳤어요. 낮에는 마을을 둘러보고, 해가 지면 간판에서 영업을 시작하세요.";
            if (loop != null && !loop.PlayerHasOpenedShopToday)
                return "해가 졌어요. 가게 간판에서 Space로 영업을 시작해 보세요.";
            return HasObservedDecision
                ? "주민의 선택을 확인했어요. 재고와 가격을 살피며 영업하고, 정산 때 하루를 마무리하세요."
                : "이웃이 상품과 가격을 살피고 있어요. 구매하거나 보류하는 이유를 지켜보세요.";
        }
    }

    public string Summary =>
        (_progress.priceConfirmedDay > 0 ? "가격 확인 완료" : "진열한 상품의 가격을 확인해 보세요") + "\n" +
        (HasObservedDecision ? (!string.IsNullOrEmpty(_progress.firstDecisionReason) ? _progress.firstDecisionReason :
            $"{_progress.firstDecisionName}의 첫 판단을 확인했어요.") : "첫 손님의 선택을 기다리고 있어요.") + "\n" +
        (HasFirstSale ? $"첫 매출 {_progress.firstSaleAmount}G · Day {_progress.firstSaleDay}" : "첫 매출은 아직이에요. 구매 보류도 다음 진열의 단서가 돼요.");

    void Awake()
    {
        ShopPriceUI.OnPriceConfirmed += OnPriceConfirmed;
        PurchaseFeedbackPresentationController.OnDecisionRecorded += OnDecision;
        SalesLogManager.OnSaleRecorded += OnSale;
        DayNightShopLoopController.OnShopDaySettled += OnSettled;
    }

    void OnDestroy()
    {
        ShopPriceUI.OnPriceConfirmed -= OnPriceConfirmed;
        PurchaseFeedbackPresentationController.OnDecisionRecorded -= OnDecision;
        SalesLogManager.OnSaleRecorded -= OnSale;
        DayNightShopLoopController.OnShopDaySettled -= OnSettled;
    }

    public void Begin(CampaignOpeningController opening) => _opening = opening;

    public void Restore(CampaignOpeningController opening, CampaignFirstNightSaveData data)
    {
        _opening = opening;
        _progress = Active && data != null ? data.Copy() : new CampaignFirstNightSaveData();
    }

    public CampaignFirstNightSaveData Capture()
    {
        ObserveOpenedShop();
        return _progress.Copy();
    }

    void Update() => ObserveOpenedShop();

    void ObserveOpenedShop()
    {
        if (Active && _progress.firstOpenedDay == 0 &&
            DayNightShopLoopController.Instance?.PlayerHasOpenedShopToday == true)
            _progress.firstOpenedDay = Day;
    }

    void OnPriceConfirmed(ShopSlot slot)
    {
        if (!Active || _progress.priceConfirmedDay > 0 || slot == null || slot.IsEmpty ||
            Adapter == null || !Adapter.RuntimeShopSlots.Contains(slot)) return;
        _progress.priceConfirmedDay = Day;
        _progress.confirmedItemId = slot.currentItem.data.id;
        _progress.confirmedUnitPrice = slot.EffectiveDisplayPrice;
    }

    void OnDecision(NpcProfile profile, PurchaseEvaluator.Result result, Item item, int price, string name)
    {
        if (!Active || HasObservedDecision || item == null ||
            DayNightShopLoopController.Instance?.IsShopOpenForCustomers != true) return;
        ObserveOpenedShop();
        _progress.firstDecisionDay = Day;
        _progress.firstDecisionResidentId = profile == _opening.Bori?.GetComponent<NpcController>()?.profile
            ? CampaignOpeningController.BoriId : profile != null ? profile.name : "visitor";
        _progress.firstDecisionName = name;
        _progress.firstDecisionReason = PurchaseFeedbackPresentationController.Instance?.LastReactionLine ?? "";
        _progress.firstDecisionItemId = item.id;
        _progress.firstDecisionWantedToBuy = result.willBuy;
    }

    void OnSale(SaleRecord sale)
    {
        if (!Active || HasFirstSale || sale == null || sale.price <= 0 ||
            DayNightShopLoopController.Instance?.IsShopOpenForCustomers != true) return;
        _progress.firstSaleDay = sale.gameDay;
        _progress.firstSaleAmount = sale.price;
    }

    void OnSettled(int closingDay)
    {
        if (!Active || Complete || _progress.priceConfirmedDay <= 0 || !HasObservedDecision ||
            _progress.firstOpenedDay <= 0 || closingDay < Mathf.Max(_progress.priceConfirmedDay, _progress.firstDecisionDay)) return;
        _progress.firstSettlementDay = closingDay;
        _progress.firstSettlementRevenue = SalesLogManager.Instance != null
            ? SalesLogManager.Instance.GetRecent(int.MaxValue).Where(sale => sale.gameDay == closingDay).Sum(sale => sale.price) : 0;
    }
}
