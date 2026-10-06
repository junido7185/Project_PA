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
    enum HandLesson { SelectApple, HolsterApple, ReselectApple, StockApple }
    HandLesson _handLesson;
    bool _handLessonRequired;
    PlayerInputHandler _lessonInput;
    public bool HandTrainingComplete => !_handLessonRequired || _handLesson == HandLesson.StockApple ||
        (trainingSlot != null && HasFruitOnShelf());

    string HandObjective => _handLesson switch
    {
        HandLesson.SelectApple => "사과 슬롯의 숫자로 손에 들어보세요.",
        HandLesson.HolsterApple => "X로 빈손을 만들어보세요.",
        HandLesson.ReselectApple => "사과 슬롯의 숫자를 다시 누르세요.",
        _ => "사과를 들고 가판대 앞에서 E로 진열하세요."
    };

    public string ObjectiveText => Stage switch
    {
        1 => "WASD로 표식까지 이동하고\nSpace로 점프하세요.",
        2 => !fruitTree.FruitDropped ? "빈손으로 나무 앞에서 E로 흔드세요."
            : $"사과를 향해 E로 줍기 ({Mathf.Min(FruitCount, 3)}/3)\n" +
              (EquipmentSystem.CurrentHeld(player.gameObject) != null ? "X로 손을 비운 뒤 주울 수 있어요." : "가까이 다가가 사과를 바라보세요."),
        3 => _handLessonRequired ? HandObjective : "열매를 실습 진열대에 올려보세요.",
        4 => "가판대 앞에서 E로 가격을 정하세요.",
        5 => "손님의 반응과 판매 금액을 확인하세요.",
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
        if (FirstDayStudioAssets.Load() != null)
        {
            PlayerInputHandler.Instance.FirstDayControls = true;
            GameClock.Instance.enabled = false;
            fruitTree.ConfigurePhysicalFruit();
            var equipment = player.GetComponent<EquipmentSystem>() ?? player.gameObject.AddComponent<EquipmentSystem>();
            equipment.ConfigureFirstDay();
            // 씬에 직렬화된 NPC 절차 애니메이터 대신 Demo256과 같은 클립 로코모션을 사용한다.
            if (player.GetComponent<PlayerLocomotionAnimator>() == null) player.gameObject.AddComponent<PlayerLocomotionAnimator>();
            WorldGameplayAdapterService.ComposeInventoryUI(gameObject, _inventory, _inventory.hotbar);
            // Equipment 콜백 뒤에서 선택/빈손의 실제 결과를 관찰한다. 아이템은 변경하지 않는다.
            _handLessonRequired = true;
            _lessonInput = PlayerInputHandler.Instance;
            _inventory.SelectionChanged += ObserveHandSelection;
            _lessonInput.OnHolster += ObserveHandHolster;
        }
        IsReady = true;
        ApplyStagePresentation();
        Debug.Log("[VS-PRESENT-001] READY existing authorities bound; stage=1");
    }

    void OnDestroy()
    {
        if (_inventory != null) _inventory.onItemChangedCallback -= ObserveFruit;
        if (_inventory != null) _inventory.SelectionChanged -= ObserveHandSelection;
        if (_lessonInput != null) _lessonInput.OnHolster -= ObserveHandHolster;
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
            if (delta.sqrMagnitude <= checkpointRadius * checkpointRadius &&
                (!(PlayerInputHandler.Instance?.FirstDayControls ?? false) || player.GetComponent<PlayerController>().JumpCount > 0)) SetStage(2);
        }
        else if (Stage == 2 && FruitCount >= 3) SetStage(3);
        else if (Stage == 3 && HandTrainingComplete && HasFruitOnShelf()) SetStage(4);
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
        }
    }

    void ObserveFruit()
    {
        if (_inventory == null || fruit == null) return;
        FruitCount = Mathf.Max(FruitCount, _inventory.CountItems(fruit) - _initialFruit);
    }

    bool HasFruitOnShelf() => !trainingSlot.IsEmpty && trainingSlot.currentItem.data == fruit;

    void ObserveHandSelection(int index)
    {
        if (!IsReady || Stage != 3 || !_handLessonRequired || _inventory.selectedHotbarIndex != index
            || _inventory.GetSelectedItem() != fruit) return;
        var equipment = player.GetComponent<EquipmentSystem>();
        if (equipment == null || equipment.IsHolstered || EquipmentSystem.CurrentHeld(player.gameObject) != fruit) return;
        if (_handLesson == HandLesson.SelectApple) _handLesson = HandLesson.HolsterApple;
        else if (_handLesson == HandLesson.ReselectApple) _handLesson = HandLesson.StockApple;
    }

    void ObserveHandHolster()
    {
        if (!IsReady || Stage != 3 || _handLesson != HandLesson.HolsterApple || _inventory.GetSelectedItem() != fruit) return;
        var equipment = player.GetComponent<EquipmentSystem>();
        if (equipment != null && equipment.IsHolstered && EquipmentSystem.CurrentHeld(player.gameObject) == null)
            _handLesson = HandLesson.ReselectApple;
    }

    bool _firstPriceConfirmDiagnosed;

    void OnPriceConfirmed(ShopSlot slot)
    {
        if (!_firstPriceConfirmDiagnosed)
        {
            _firstPriceConfirmDiagnosed = true;
            bool uiSlotMatch = slot == trainingSlot;
            bool fruitOnShelf = HasFruitOnShelf();
            Shop shopRef = trainingSlot != null ? trainingSlot.GetComponentInParent<Shop>() : null;
            Debug.Log($"PRICE_CONFIRM:\nuiSlotInstanceId={(slot != null ? slot.GetInstanceID().ToString() : "null")}\ntrainingSlotInstanceId={(trainingSlot != null ? trainingSlot.GetInstanceID().ToString() : "null")}\nsameSlot={uiSlotMatch}\ntutorialStage={Stage}\nhasFruit={fruitOnShelf}\nhandlerReceived=true\ncustomerStartCalled={(buyer != null && shopRef != null && (Stage == 4 || Stage == 5 || (Stage == 3 && HandTrainingComplete)) && fruitOnShelf && uiSlotMatch)}");
        }
        if (!IsReady || Complete || slot != trainingSlot || !HasFruitOnShelf()) return;
        // Stock and confirmation can finish before the next Update observes the
        // stocked shelf. Advance the existing stage at the event boundary.
        if (Stage == 3 && HandTrainingComplete) SetStage(4);
        if (Stage != 4 && Stage != 5) return;
        buyer.Pause();
        _moneyBeforeVisit = _economy.Money;
        _revenueBeforeVisit = _economy.CumulativeRevenue;
        _recordedSale = 0;
        _acceptedDecision = false;
        _returnToPriceAt = 0f;
        SetStage(5);
        buyer.Resume();
        Shop shop = trainingSlot.GetComponentInParent<Shop>();
        if (shop == null || !buyer.TryBeginShoppingVisitAt(shop.transform))
            Debug.LogError("[VS-PRESENT-001] First confirmed price could not start the existing buyer visit.");
        Debug.Log($"[VS-PRESENT-001] PRICE_CONFIRMED price={trainingSlot.EffectiveDisplayPrice}G");
    }

    void OnDecision(NpcProfile profile, PurchaseEvaluator.Result result, Item item, int price, string customer)
    {
        if (!IsReady || Stage != 5 || profile != buyer.profile || item != fruit || customer != BuyerName) return;
        DecisionCount++;
        _acceptedDecision = result.willBuy;
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
                if (fruitVisual != null) fruitVisual.SetActive(FruitCount < 3 && !fruitTree.FruitDropped);
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
