using UnityEngine;

// 상점 내부의 진열대 한 칸.
//
// 설계 의도 (Docs/02 Process 4.0):
// - 플레이어가 아이템을 진열하고 가격(DisplayPrice)을 책정하는 단위.
// - NPC가 "이 한 칸"을 MBTI 기반으로 평가해 구매 결정을 내리는 단위.
// - ShopSlot 은 GameObject 이므로 월드 위치를 가진다 → NPC 가 이 위치로 직접 걸어가서 평가한다.
// - 부모 Shop 에 자동 등록되어 Shop.GetAvailableSlots() 로 조회된다.
//
// Inspector 사용법:
// - 이 컴포넌트를 Collider 를 가진 GameObject 에 붙인다. (플레이어가 상호작용하려면 Collider 필수)
// - Shop 컴포넌트를 가진 GameObject 의 자식(또는 하위 자손)으로 배치한다.
// - displayPrice 는 런타임에 Inspector 로 조정 가능 (UI 없이도 가격 테스트 가능).
public class ShopSlot : MonoBehaviour, IInteractable
{
    [Header("진열 상태")]
    [Tooltip("현재 진열된 아이템 스택. null = 빈 진열대")]
    public ItemInstance currentItem;

    [Tooltip("플레이어가 책정한 진열 가격. 0 이면 basePrice 사용")]
    public int displayPrice = 0;

    public bool IsEmpty => currentItem == null || currentItem.count <= 0 || currentItem.data == null;

    // 시스템이 실제로 사용할 유효 가격 — 책정가가 없으면 basePrice 로 폴백.
    public int EffectiveDisplayPrice
    {
        get
        {
            if (IsEmpty) return 0;
            return displayPrice > 0 ? displayPrice : currentItem.data.basePrice;
        }
    }

    private Shop _parentShop;

    void Awake()
    {
        // 부모 계층에서 Shop 을 찾아 자동 등록.
        _parentShop = GetComponentInParent<Shop>();
        if (_parentShop != null)
        {
            _parentShop.RegisterSlot(this);
        }
        else
        {
            Debug.LogWarning($"⚠️ ShopSlot '{name}' 이 Shop 하위에 배치되지 않았습니다. NPC가 이 슬롯을 인식하지 못합니다.");
        }
    }

    void OnDestroy()
    {
        if (_parentShop != null)
        {
            _parentShop.UnregisterSlot(this);
        }
    }

    // -------- IInteractable (플레이어 Space 키) --------

    public void Interact(GameObject interactor)
    {
        if (IsEmpty) TryStockFromPlayer();
        else TryTakeBackToPlayer();
    }

    public string GetInteractPrompt()
    {
        if (IsEmpty) return "진열하기";
        return $"회수하기 ({currentItem.data.itemName} / {EffectiveDisplayPrice}G)";
    }

    // 플레이어가 들고 있는 아이템 1개를 이 슬롯에 진열한다.
    // ItemInstance 의 quality/currentPrice 메타를 보존한다.
    private void TryStockFromPlayer()
    {
        if (Inventory.instance == null) return;

        ItemInstance held = Inventory.instance.GetSelectedInstance();
        if (held == null || held.data == null)
        {
            Debug.Log("🛒 진열할 아이템이 없습니다.");
            return;
        }

        // 판매 불가 카테고리/도구 차단.
        if (held.data.category == ItemCategory.Tool || held.data.toolType != ToolType.None)
        {
            Debug.Log($"🚫 {held.data.itemName} 은(는) 진열할 수 없습니다 (도구/판매 불가).");
            return;
        }

        // 티어 잠금 확인 — 필요 티어에 도달하지 않으면 진열 불가.
        if (TierService.Instance != null && !TierService.Instance.IsUnlocked(held.data.requiredTier))
        {
            Debug.Log($"🔒 {held.data.itemName} 은(는) Tier {held.data.requiredTier} 이상에서만 진열 가능합니다 " +
                      $"(현재: Tier {TierService.Instance.CurrentTier})");
            return;
        }

        // 1개 차감 후 슬롯에 새 스택 생성 (메타 복사).
        Inventory.instance.RemoveItems(held.data, 1);

        currentItem = new ItemInstance(held.data, 1)
        {
            quality = held.quality,
            currentPrice = held.currentPrice
        };
        // 책정가가 설정되지 않았다면 basePrice 를 초기값으로.
        if (displayPrice <= 0) displayPrice = held.data.basePrice;

        Debug.Log($"🛒 진열: {held.data.itemName} @ {displayPrice}G");
    }

    // 진열된 아이템을 회수해 플레이어 인벤토리로 돌려놓는다.
    private void TryTakeBackToPlayer()
    {
        if (Inventory.instance == null || currentItem == null || currentItem.data == null) return;

        bool added = Inventory.instance.AddInstance(currentItem);
        if (!added)
        {
            Debug.Log("🚫 가방이 꽉 차서 회수할 수 없습니다.");
            return;
        }

        Debug.Log($"🛒 회수: {currentItem.data.itemName}");
        currentItem = null;
        // displayPrice 는 초기화하지 않음 — 플레이어가 같은 가격으로 재진열하려 할 수 있다.
    }

    // -------- NPC 측 API --------

    // NPC 가 이 슬롯을 구매할 때 호출한다.
    // 성공 시 EconomyService 에 매출을 적립하고 슬롯을 비운다.
    // paidAmount 는 실제로 지불된 금액(로그/UI용).
    public bool TryPurchaseByNpc(string buyerTag, out int paidAmount)
    {
        paidAmount = 0;
        if (IsEmpty) return false;

        int unitPrice = EffectiveDisplayPrice;
        paidAmount = unitPrice * currentItem.count;

        if (EconomyService.Instance != null)
        {
            EconomyService.Instance.Deposit(paidAmount, $"Shop 판매[{buyerTag}]: {currentItem.data.itemName}");
        }

        currentItem = null;
        // displayPrice 유지 — 플레이어가 같은 품목을 재진열할 수 있게 가격 책정을 보존.
        return true;
    }
}
