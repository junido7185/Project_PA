using TMPro;
using UnityEngine;

public class ShopSlot : MonoBehaviour, IInteractable
{
    const string DisplayRootName = "ShopSlot_Display";

    [Header("진열 상태")]
    public ItemInstance currentItem;
    public int displayPrice = 0;

    [Header("프로토타입 표시")]
    public Vector3 displayOffset = new Vector3(0f, 0.45f, 0f);
    public float fallbackDisplayScale = 0.38f;

    public bool IsEmpty => currentItem == null || currentItem.count <= 0 || currentItem.data == null;

    public int EffectiveDisplayPrice
    {
        get
        {
            if (IsEmpty) return 0;
            return displayPrice > 0 ? displayPrice : currentItem.data.basePrice;
        }
    }

    string _claimedBy;
    Shop _parentShop;

    public bool IsClaimed => !string.IsNullOrEmpty(_claimedBy);
    public bool IsClaimedBy(string buyerTag) => !string.IsNullOrEmpty(buyerTag) && _claimedBy == buyerTag;

    public bool TryClaim(string buyerTag)
    {
        if (string.IsNullOrEmpty(buyerTag)) return false;
        if (!string.IsNullOrEmpty(_claimedBy) && _claimedBy != buyerTag) return false;
        _claimedBy = buyerTag;
        return true;
    }

    public void ReleaseClaim(string buyerTag)
    {
        if (_claimedBy == buyerTag) _claimedBy = null;
    }

    void Awake()
    {
        _parentShop = GetComponentInParent<Shop>();
        if (_parentShop != null)
        {
            _parentShop.RegisterSlot(this);
        }
        else
        {
            Debug.LogWarning($"ShopSlot '{name}' is not under a Shop. NPCs may not find this slot.");
        }
    }

    void Start()
    {
        RefreshDisplay();
    }

    void OnDestroy()
    {
        if (_parentShop != null)
            _parentShop.UnregisterSlot(this);
    }

    public void Interact(GameObject interactor)
    {
        if (IsEmpty)
        {
            TryStockFromPlayer();
        }
        else
        {
            if (ShopPriceUI.instance != null)
                ShopPriceUI.instance.Open(this);
            else
                TryTakeBackToPlayer();
        }
    }

    public string GetInteractPrompt()
    {
        if (IsEmpty) return "판매대에 상품 진열";
        return $"가격 확정/조정 ({currentItem.data.itemName} / {EffectiveDisplayPrice}G)";
    }

    void TryStockFromPlayer()
    {
        if (Inventory.instance == null)
        {
            Debug.LogWarning("ShopSlot: Inventory.instance가 없어 진열할 수 없습니다.");
            return;
        }

        if (!TryFindStockCandidate(out var sourceSlot, out var sourceInstance, out string sourceHint))
        {
            Debug.LogWarning("ShopSlot: 핫바/가방에 진열 가능한 판매 아이템이 없습니다.");
            return;
        }

        if (!CanStock(sourceInstance.data, logReason: true))
            return;

        currentItem = new ItemInstance(sourceInstance.data, 1)
        {
            quality = sourceInstance.quality,
            currentPrice = sourceInstance.currentPrice
        };

        sourceSlot.AddCount(-1);
        if (displayPrice <= 0) displayPrice = currentItem.data.basePrice;

        Inventory.instance.RefreshAllUI();
        RefreshDisplay();
        Debug.Log($"진열 완료: {currentItem.data.itemName} @ {displayPrice}G ({sourceHint})");
    }

    bool TryFindStockCandidate(out InventorySlot sourceSlot, out ItemInstance sourceInstance, out string sourceHint)
    {
        sourceSlot = null;
        sourceInstance = null;
        sourceHint = "";

        var inv = Inventory.instance;
        if (inv == null) return false;

        InventorySlot selected = inv.hotbar != null ? inv.hotbar.GetSlot(inv.selectedHotbarIndex) : null;
        if (IsUsableStockSlot(selected))
        {
            sourceSlot = selected;
            sourceInstance = selected.instance;
            sourceHint = $"hotbar {inv.selectedHotbarIndex + 1}";
            return true;
        }

        if (TryFindFirstSellable(inv.hotbar != null ? inv.hotbar.slots : null, "hotbar", out sourceSlot, out sourceInstance, out sourceHint))
            return true;

        return TryFindFirstSellable(inv.slots, "inventory", out sourceSlot, out sourceInstance, out sourceHint);
    }

    bool TryFindFirstSellable(System.Collections.Generic.List<InventorySlot> slots, string owner,
        out InventorySlot sourceSlot, out ItemInstance sourceInstance, out string sourceHint)
    {
        sourceSlot = null;
        sourceInstance = null;
        sourceHint = "";

        if (slots == null) return false;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (!IsUsableStockSlot(slot)) continue;

            sourceSlot = slot;
            sourceInstance = slot.instance;
            sourceHint = $"{owner} {i + 1}";
            return true;
        }

        return false;
    }

    bool IsUsableStockSlot(InventorySlot slot)
    {
        return slot != null
            && !slot.IsEmpty
            && slot.instance != null
            && slot.instance.data != null
            && CanStock(slot.instance.data, logReason: false);
    }

    bool CanStock(Item item, bool logReason)
    {
        if (item == null) return false;

        if (item.category == ItemCategory.Tool || item.toolType != ToolType.None)
        {
            if (logReason) Debug.LogWarning($"{item.itemName}은(는) 도구라 판매대에 진열할 수 없습니다.");
            return false;
        }

        if (TierService.Instance != null && !TierService.Instance.IsUnlocked(item.requiredTier))
        {
            if (logReason)
                Debug.LogWarning($"{item.itemName}은(는) Tier {item.requiredTier} 이상에서만 진열 가능합니다.");
            return false;
        }

        return true;
    }

    public void RetrieveItem() => TryTakeBackToPlayer();

    void TryTakeBackToPlayer()
    {
        if (Inventory.instance == null || currentItem == null || currentItem.data == null) return;

        bool added = Inventory.instance.AddInstance(currentItem);
        if (!added)
        {
            Debug.LogWarning("가방이 꽉 차서 회수할 수 없습니다.");
            return;
        }

        Debug.Log($"회수 완료: {currentItem.data.itemName}");
        currentItem = null;
        RefreshDisplay();
    }

    public bool TryPurchaseByNpc(string buyerTag, out int paidAmount)
    {
        paidAmount = 0;
        if (string.IsNullOrEmpty(buyerTag)) return false;
        if (!TryClaim(buyerTag)) return false;

        try
        {
            if (IsEmpty) return false;

            int unitPrice = EffectiveDisplayPrice;
            paidAmount = unitPrice * currentItem.count;

            if (EconomyService.Instance != null)
                EconomyService.Instance.Deposit(paidAmount, $"Shop sale[{buyerTag}]: {currentItem.data.itemName}");

            if (SalesLogManager.Instance != null && currentItem.data != null)
            {
                int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
                int hour = GameClock.Instance != null ? GameClock.Instance.CurrentHourInt : 0;
                SalesLogManager.Instance.RecordSale(
                    currentItem.data.itemName,
                    currentItem.data.category.ToString(),
                    paidAmount,
                    currentItem.quality,
                    buyerTag,
                    day,
                    hour);
            }

            currentItem = null;
            RefreshDisplay();
            return true;
        }
        finally
        {
            ReleaseClaim(buyerTag);
        }
    }

    public void RefreshDisplay()
    {
        ClearDisplay();
        if (IsEmpty) return;

        var root = new GameObject(DisplayRootName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = displayOffset;
        root.transform.localRotation = Quaternion.identity;

        GameObject visual = null;
        if (currentItem.data.model != null)
        {
            visual = Instantiate(currentItem.data.model, root.transform);
            visual.name = "ItemModel";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * fallbackDisplayScale;
        }
        else
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "ItemCube";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * fallbackDisplayScale;
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = CreateDisplayMaterial(currentItem.data);
        }

        foreach (var col in visual.GetComponentsInChildren<Collider>(true))
            DestroyUnityObject(col);

        var labelGo = new GameObject("ItemLabel");
        labelGo.transform.SetParent(root.transform, false);
        labelGo.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        var label = labelGo.AddComponent<PrototypeWorldLabel>();
        label.Set($"{currentItem.data.itemName}\n{EffectiveDisplayPrice}G", new Color(1f, 0.94f, 0.62f), 1.8f);
    }

    void ClearDisplay()
    {
        var existing = transform.Find(DisplayRootName);
        if (existing != null)
            DestroyUnityObject(existing.gameObject);
    }

    static Material CreateDisplayMaterial(Item item)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader) { name = $"Mat_Display_{(item != null ? item.name : "Item")}" };
        mat.color = ResolveDisplayColor(item);
        return mat;
    }

    static Color ResolveDisplayColor(Item item)
    {
        if (item == null) return new Color(0.90f, 0.80f, 0.56f);

        return item.category switch
        {
            ItemCategory.Raw => new Color(0.55f, 0.78f, 0.42f),
            ItemCategory.Processed => new Color(0.95f, 0.66f, 0.42f),
            ItemCategory.Utility => new Color(0.64f, 0.50f, 0.36f),
            ItemCategory.Luxury => new Color(0.70f, 0.64f, 0.86f),
            _ => new Color(0.90f, 0.80f, 0.56f)
        };
    }

    static void DestroyUnityObject(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}
