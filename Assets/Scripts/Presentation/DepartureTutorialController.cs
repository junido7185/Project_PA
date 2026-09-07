using System;
using UnityEngine;

// VS-PRESENT-001: 기존 채집/재고/가격/구매/경제 결과를 관찰하는 출항 인증 진행이다.
// 개발용 독립 씬의 세션 상태이며, 저장 데이터나 거래 권한을 소유하지 않는다.
[DefaultExecutionOrder(150)]
public sealed class DepartureTutorialController : MonoBehaviour
{
    public const string SceneName = "PA_DepartureTutorial";

    public Transform player;
    public Transform movementCheckpoint;
    public float checkpointRadius = 1.4f;
    public Item fruit;
    public DaytimeStockPrepPoint fruitTree;
    public ShopSlot trainingSlot;
    public NpcController buyer;
    public GameObject[] harvestFruitVisuals;
    public Renderer[] checkpointLights;
    public DepartureTutorialPresentation presentation;

    public int Stage { get; private set; } = 1;
    public int FruitCount { get; private set; }
    public int DecisionCount { get; private set; }
    public int SaleAmount { get; private set; }
    public bool Complete => Stage == 6;
    public bool CompanionSelectionUnlocked => Complete;
    public bool IsReady { get; private set; }
    public string LastReaction { get; private set; } = string.Empty;
    public float ReactionUntil { get; private set; }
    public event Action OnCompleted;

    public string ObjectiveText => Stage switch
    {
        1 => "출항 인증 지점으로 이동하세요.",
        2 => $"실습용 열매를 확보하세요. ({Mathf.Min(FruitCount, 3)}/3)",
        3 => "열매를 실습 진열대에 올려보세요.",
        4 => "판매 가격을 정하세요.",
        5 => "손님의 반응을 확인하세요.",
        _ => "출항 인증 완료"
    };

    Inventory _inventory;
    EconomyService _economy;
    Collider[] _treeColliders;
    Collider[] _shelfColliders;
    int _initialFruit;
    int _moneyBeforeVisit;
    long _revenueBeforeVisit;
    int _recordedSale;
    bool _acceptedDecision;
    bool _visitPending;
    float _retryAt;
    float _returnToPriceAt;
    string BuyerName => buyer != null && buyer.profile != null && !string.IsNullOrEmpty(buyer.profile.npcName)
        ? buyer.profile.npcName : buyer != null ? buyer.gameObject.name : string.Empty;

    void Start()
    {
        if (gameObject.scene.name != SceneName) { enabled = false; return; }
        _inventory = Inventory.instance;
        _economy = EconomyService.Instance;
        if (player == null || movementCheckpoint == null || fruit == null || fruitTree == null
            || trainingSlot == null || buyer == null || _inventory == null || _economy == null
            || ShopPriceUI.instance == null || SalesLogManager.Instance == null
            || PurchaseFeedbackPresentationController.Instance == null)
        {
            Debug.LogError("[VS-PRESENT-001] 출항 인증의 기존 시스템 참조가 누락되었습니다.");
            enabled = false;
            return;
        }

        _initialFruit = _inventory.CountItems(fruit);
        _treeColliders = fruitTree.GetComponents<Collider>();
        _shelfColliders = trainingSlot.GetComponents<Collider>();
        buyer.Pause();
        _inventory.onItemChangedCallback += ObserveFruit;
        ShopPriceUI.OnPriceConfirmed += OnPriceConfirmed;
        PurchaseFeedbackPresentationController.OnDecisionRecorded += OnDecision;
        SalesLogManager.OnSaleRecorded += OnSale;
        if (presentation != null) presentation.tutorial = this;
        IsReady = true;
        ApplyStagePresentation();
        Debug.Log("[VS-PRESENT-001] READY existing authorities bound; stage=1");
    }

    void OnDestroy()
    {
        if (_inventory != null) _inventory.onItemChangedCallback -= ObserveFruit;
        ShopPriceUI.OnPriceConfirmed -= OnPriceConfirmed;
        PurchaseFeedbackPresentationController.OnDecisionRecorded -= OnDecision;
        SalesLogManager.OnSaleRecorded -= OnSale;
    }

    void Update()
    {
        if (!IsReady || Complete) return;
        ObserveFruit();
        if (Stage == 1)
        {
            Vector3 delta = player.position - movementCheckpoint.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= checkpointRadius * checkpointRadius) SetStage(2);
        }
        else if (Stage == 2 && FruitCount >= 3) SetStage(3);
        else if (Stage == 3 && HasFruitOnShelf()) SetStage(4);
        else if (Stage == 4 && !HasFruitOnShelf()) SetStage(3);
        else if (Stage == 5)
        {
            // 판매 이벤트는 슬롯 비우기 직전에 오므로 다음 프레임에서 결제 결과까지 확인한다.
            if (_acceptedDecision && _recordedSale > 0 && trainingSlot.IsEmpty
                && _economy.Money - _moneyBeforeVisit == _recordedSale
                && _economy.CumulativeRevenue - _revenueBeforeVisit == _recordedSale)
            {
                SaleAmount = _recordedSale;
                buyer.Pause();
                SetStage(6);
                Debug.Log($"[VS-PRESENT-001] COMPLETE sale={SaleAmount}G decisions={DecisionCount} companionUnlock=true");
                OnCompleted?.Invoke();
            }
            else if (_returnToPriceAt > 0f && Time.unscaledTime >= _returnToPriceAt)
            {
                _returnToPriceAt = 0f;
                buyer.Pause();
                SetStage(HasFruitOnShelf() ? 4 : 3);
            }
            else if (_visitPending && Time.unscaledTime >= _retryAt)
            {
                _retryAt = Time.unscaledTime + 1f;
                _visitPending = !buyer.TryBeginShoppingVisitAt(trainingSlot.GetComponentInParent<Shop>().transform);
            }
        }
    }

    void ObserveFruit()
    {
        if (_inventory == null || fruit == null) return;
        FruitCount = Mathf.Max(FruitCount, _inventory.CountItems(fruit) - _initialFruit);
    }

    bool HasFruitOnShelf() => !trainingSlot.IsEmpty && trainingSlot.currentItem.data == fruit;

    void OnPriceConfirmed(ShopSlot slot)
    {
        if (!IsReady || Complete || slot != trainingSlot || (Stage != 4 && Stage != 5) || !HasFruitOnShelf()) return;
        buyer.Pause();
        _moneyBeforeVisit = _economy.Money;
        _revenueBeforeVisit = _economy.CumulativeRevenue;
        _recordedSale = 0;
        _acceptedDecision = false;
        _returnToPriceAt = 0f;
        SetStage(5);
        buyer.Resume();
        Shop shop = trainingSlot.GetComponentInParent<Shop>();
        _visitPending = shop != null && !buyer.TryBeginShoppingVisitAt(shop.transform);
        _retryAt = Time.unscaledTime + 1f;
        Debug.Log($"[VS-PRESENT-001] PRICE_CONFIRMED price={trainingSlot.EffectiveDisplayPrice}G");
    }

    void OnDecision(NpcProfile profile, PurchaseEvaluator.Result result, Item item, int price, string customer)
    {
        if (!IsReady || Stage != 5 || profile != buyer.profile || item != fruit || customer != BuyerName) return;
        DecisionCount++;
        _acceptedDecision = result.willBuy;
        _visitPending = false;
        LastReaction = PurchaseFeedbackPresentationController.Instance.LastReactionLine;
        ReactionUntil = Time.unscaledTime + 25f;
        if (!result.willBuy) _returnToPriceAt = Time.unscaledTime + 3f;
        Debug.Log($"[VS-PRESENT-001] DECISION buy={result.willBuy} probability={result.probability:F3} price={price}");
    }

    void OnSale(SaleRecord sale)
    {
        if (!IsReady || Stage != 5 || sale == null || sale.itemName != fruit.itemName || sale.buyerName != BuyerName) return;
        _recordedSale = sale.price;
    }

    void SetStage(int stage)
    {
        if (Stage == stage) return;
        Stage = stage;
        ApplyStagePresentation();
        Debug.Log($"[VS-PRESENT-001] STAGE={Stage} objective={ObjectiveText}");
    }

    void ApplyStagePresentation()
    {
        foreach (Collider col in _treeColliders) if (col != null) col.enabled = Stage == 2;
        foreach (Collider col in _shelfColliders) if (col != null) col.enabled = Stage >= 3;
        if (harvestFruitVisuals != null)
            foreach (GameObject fruitVisual in harvestFruitVisuals)
                if (fruitVisual != null) fruitVisual.SetActive(FruitCount < 3);
        if (checkpointLights == null) return;
        for (int i = 0; i < checkpointLights.Length; i++)
        {
            if (checkpointLights[i] == null) continue;
            bool passed = i == 0 ? Stage > 1 : i == 1 ? Stage > 2 : Complete;
            Color color = passed ? new Color(0.49f, 0.73f, 0.38f) : new Color(0.93f, 0.64f, 0.24f);
            checkpointLights[i].material.color = color;
        }
    }
}
