using TMPro;
using UnityEngine;

public class ShopSlot : MonoBehaviour, IInteractable
{
    const string DisplayRootName = "ShopSlot_Display";

    [Header("진열 상태")]
    public ItemInstance currentItem;
    public int displayPrice = 0;
    [Tooltip("같은 상품을 한 가판대에 쌓을 수 있는 최대 수량. 1이면 기존처럼 한 개만 진열한다")]
    public int stockCapacity = 1;

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
    bool _purchaseInProgress;

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

    public void BindOperatingShop(Shop shop)
    {
        if (_parentShop == shop) return;
        _parentShop?.UnregisterSlot(this);
        _parentShop = shop;
        _parentShop?.RegisterSlot(this);
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
        if (!isActiveAndEnabled || _purchaseInProgress) return;
        if (IsEmpty)
        {
            TryStockFromPlayer();
        }
        else if (CanTopUpFromHeld(out var heldSlot))
        {
            TopUp(heldSlot);
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
        if (CanTopUpFromHeld(out _))
            return $"{ShownName(currentItem.data)} 더 진열 ({currentItem.count}/{Capacity})";
        string stockText = currentItem.count > 1 ? $" ×{currentItem.count}" : "";
        return $"가격 확정/조정 ({ShownName(currentItem.data)}{stockText} / {EffectiveDisplayPrice}G)";
    }

    // P6: 데모 화면은 저장 ID/이름을 그대로 두고 표시 계층에서만 한국어 이름을 쓴다.
    static string ShownName(Item item) =>
        WorldGameplayAdapterService.Instance?.FirstDay == true ? ItemDisplayName.For(item) : item.itemName;

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

        int moved = Mathf.Clamp(sourceInstance.count, 1, Capacity);
        currentItem = new ItemInstance(sourceInstance.data, moved)
        {
            quality = sourceInstance.quality,
            currentPrice = sourceInstance.currentPrice
        };

        sourceSlot.AddCount(-moved);
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

        if (PlayerInputHandler.Instance?.FirstDayControls == true && EquipmentSystem.CurrentHeld(inv.gameObject) == null) return false;
        InventorySlot selected = inv.hotbar != null ? inv.hotbar.GetSlot(inv.selectedHotbarIndex) : null;
        if (IsUsableStockSlot(selected))
        {
            sourceSlot = selected;
            sourceInstance = selected.instance;
            sourceHint = $"hotbar {inv.selectedHotbarIndex + 1}";
            return true;
        }

        if (PlayerInputHandler.Instance?.FirstDayControls == true) return false;
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

    int Capacity => Mathf.Max(1, stockCapacity);

    // 같은 상품을 손에 들고 있고 자리가 남으면 가격 UI 대신 보충한다(D2 영업 밀도).
    bool CanTopUpFromHeld(out InventorySlot heldSlot)
    {
        heldSlot = null;
        if (IsEmpty || currentItem.count >= Capacity || Inventory.instance == null) return false;
        var inv = Inventory.instance;
        var selected = inv.hotbar != null ? inv.hotbar.GetSlot(inv.selectedHotbarIndex) : null;
        if (selected == null || selected.IsEmpty || selected.instance == null || selected.instance.data != currentItem.data) return false;
        heldSlot = selected;
        return true;
    }

    void TopUp(InventorySlot heldSlot)
    {
        int moved = Mathf.Min(heldSlot.instance.count, Capacity - currentItem.count);
        if (moved <= 0) return;
        currentItem.count += moved;
        heldSlot.AddCount(-moved);
        Inventory.instance.RefreshAllUI();
        RefreshDisplay();
        Debug.Log($"진열 보충: {currentItem.data.itemName} ×{currentItem.count}");
    }

    void TryTakeBackToPlayer()
    {
        if (_purchaseInProgress) return;
        if (Inventory.instance == null || currentItem == null || currentItem.data == null) return;

        ItemInstance reclaimed = currentItem;
        bool added = Inventory.instance.AddInstance(reclaimed);
        if (!added)
        {
            Debug.LogWarning("가방이 꽉 차서 회수할 수 없습니다.");
            return;
        }

        Debug.Log($"회수 완료: {reclaimed.data.itemName}");
        currentItem = null;
        // AddInstance의 중간 콜백 뒤, 진열대까지 비워진 최종 거래 상태를 UI에 전파한다.
        Inventory.instance.RefreshAllUI();
        RefreshDisplay();
    }

    public bool TryPurchaseByNpc(string buyerTag, out int paidAmount)
    {
        paidAmount = 0;
        if (!isActiveAndEnabled || _purchaseInProgress ||
            (DayNightShopLoopController.Instance != null && !DayNightShopLoopController.Instance.IsShopOpenForCustomers)) return false;
        if (string.IsNullOrEmpty(buyerTag)) return false;
        if (!TryClaim(buyerTag)) return false;

        try
        {
            if (IsEmpty) return false;
            if (EconomyService.Instance == null || SalesLogManager.Instance == null) return false;
            _purchaseInProgress = true;

            // 손님 한 명은 한 개를 산다. 쌓인 재고는 다음 손님에게 남는다(용량 1이면 기존과 동일).
            int unitPrice = EffectiveDisplayPrice;
            long total = unitPrice;
            if (total < 0 || total > int.MaxValue - (long)EconomyService.Instance.Money) return false;
            int amount = (int)total;

            if (!EconomyService.Instance.Deposit(amount, $"Shop sale[{buyerTag}]: {currentItem.data.itemName}")) return false;
            paidAmount = amount;

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

            currentItem.count -= 1;
            if (currentItem.count <= 0)
            {
                currentItem = null;
                _soldOutDay = day; // Task 019 — 오늘 다 팔린 슬롯 표시
            }
            RefreshDisplay();
            return true;
        }
        finally
        {
            _purchaseInProgress = false;
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
                soldOutLabel.Set("품절 · 보충하세요", new Color(1f, 0.80f, 0.66f), WorldGameplayAdapterService.Instance?.FirstDay == true ? 2.6f : 1.2f);
            }
            return;
        }

        _soldOutDay = -1; // 재진열되면 품절 상태 해제

        var root = new GameObject(DisplayRootName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = displayOffset;
        root.transform.localRotation = Quaternion.identity;

        GameObject visual = null;
        bool firstDay = WorldGameplayAdapterService.Instance?.FirstDay == true;
        var model = firstDay ? FirstDayStudioAssets.Load()?.ModelFor(currentItem.data) : currentItem.data.model;
        if (model != null)
        {
            visual = Instantiate(model, root.transform);
            visual.name = "ItemModel";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = model.transform.localRotation;
            visual.transform.localScale = Vector3.one * fallbackDisplayScale;
            if (firstDay)
            {
                var renderers = visual.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    visual.transform.localScale *= .65f / Mathf.Max(.001f, bounds.size.x, bounds.size.y, bounds.size.z);
                    bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    visual.transform.position += root.transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                }
            }
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
        // 이름·가격은 진열 모델 꼭대기 위에 띄운다(모델이 가격을 가리지 않게).
        float labelHeight = 0.55f;
        var shown = visual.GetComponentsInChildren<Renderer>();
        if (firstDay && shown.Length > 0)
        {
            Bounds top = shown[0].bounds;
            foreach (var renderer in shown) top.Encapsulate(renderer.bounds);
            labelHeight = Mathf.Max(labelHeight, root.transform.InverseTransformPoint(new Vector3(top.center.x, top.max.y, top.center.z)).y + 0.65f);
        }
        labelGo.transform.localPosition = new Vector3(0f, labelHeight, 0f);
        var label = labelGo.AddComponent<PrototypeWorldLabel>();
        string stock = currentItem.count > 1 ? $" ×{currentItem.count}" : "";
        label.Set($"{ShownName(currentItem.data)}{stock}\n{EffectiveDisplayPrice}G", new Color(1f, 0.94f, 0.62f), firstDay ? 2.6f : 1.8f);
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
