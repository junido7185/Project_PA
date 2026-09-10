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

    // Task 019 — 표시 전용 품절 상태. NPC 구매로 오늘 비워진 슬롯을 기록한다.
    // 런타임 전용(저장 안 함): 재진열 또는 다음날이 되면 자연히 풀린다.
    int _soldOutDay = -1;
    public bool IsSoldOutToday => IsEmpty && _soldOutDay >= 0
        && GameClock.Instance != null && GameClock.Instance.CurrentDay == _soldOutDay;

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

        // Task 019 — 날이 바뀌면 남아 있는 품절 라벨을 지운다 (표시 갱신 전용).
        if (GameClock.Instance != null)
            GameClock.Instance.OnNewDay += OnNewDayRefreshDisplay;
    }

    void OnDestroy()
    {
        if (_parentShop != null)
            _parentShop.UnregisterSlot(this);

        if (GameClock.Instance != null)
            GameClock.Instance.OnNewDay -= OnNewDayRefreshDisplay;
    }

    void OnNewDayRefreshDisplay(int _) => RefreshDisplay();

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
        if (IsEmpty)
            return IsSoldOutToday ? "판매대에 상품 진열 (오늘 품절)" : "판매대에 상품 진열";
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

            int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

            if (SalesLogManager.Instance != null && currentItem.data != null)
            {
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
            _soldOutDay = day; // Task 019 — 오늘 다 팔린 슬롯 표시
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

        // Task 019 — 오늘 품절된 빈 슬롯은 보충을 유도하는 작은 라벨을 보여준다.
        if (IsEmpty)
        {
            if (IsSoldOutToday)
            {
                var soldOutRoot = new GameObject(DisplayRootName);
                soldOutRoot.transform.SetParent(transform, false);
                soldOutRoot.transform.localPosition = displayOffset;

                var soldOutLabelGo = new GameObject("SoldOutLabel");
                soldOutLabelGo.transform.SetParent(soldOutRoot.transform, false);
                soldOutLabelGo.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                var soldOutLabel = soldOutLabelGo.AddComponent<PrototypeWorldLabel>();
                soldOutLabel.Set("품절 · 보충하세요", new Color(1f, 0.62f, 0.55f), 1.2f);
            }
            return;
        }

        _soldOutDay = -1; // 재진열되면 품절 상태 해제

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
            // S5 — 모델 없는 아이템: 원색 큐브 대신 낮은 받침 + 실제 아이템 아이콘 빌보드.
            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "ItemCube";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(fallbackDisplayScale, fallbackDisplayScale * 0.45f, fallbackDisplayScale);
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = CreateDisplayMaterial(currentItem.data);

            if (currentItem.data.icon != null)
            {
                var iconGo = new GameObject("ItemIcon", typeof(SpriteRenderer));
                iconGo.transform.SetParent(root.transform, false);
                iconGo.transform.localPosition = new Vector3(0f, 0.33f, 0f);
                var sprite = iconGo.GetComponent<SpriteRenderer>();
                sprite.sprite = currentItem.data.icon;
                float worldSize = Mathf.Max(sprite.sprite.bounds.size.x, sprite.sprite.bounds.size.y);
                if (worldSize > 0.01f)
                    iconGo.transform.localScale = Vector3.one * (0.32f / worldSize);
                iconGo.AddComponent<DisplayIconBillboard>();
            }
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
        // Task 019 — 같은 프레임에 Refresh 가 두 번 일어나면 지연 Destroy 대기 중인
        // 이전 루트가 Find 에 걸려 새 루트가 살아남는 문제가 있었다. 전부 순회 제거.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child != null && child.name == DisplayRootName)
            {
                child.gameObject.SetActive(false); // 지연 Destroy 프레임에도 판매된 모델을 표시하지 않는다.
                DestroyUnityObject(child.gameObject);
            }
        }
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

    // S5 — 진열 아이콘이 항상 카메라를 보게 하는 표시 전용 빌보드.
    class DisplayIconBillboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 toCamera = transform.position - cam.transform.position;
            if (toCamera.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }
    }
}
