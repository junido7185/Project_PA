using TMPro;
using UnityEngine;
using UnityEngine.UI;

// B09 생활형 외부 창고와 일반 StorageBox가 공유하는 보관 화면.
// 씬에 직렬화 UI가 있으면 그대로 사용하고, 없으면 PA_UIRoot 아래에 제품용 폴백을 만든다.
public class StorageUI : MonoBehaviour
{
    public static StorageUI instance;

    [Header("UI 연결")]
    public GameObject uiPanel;
    public Transform itemsParent;
    public GameObject slotPrefab;
    public TextMeshProUGUI titleText;

    public bool IsOpen => currentBox != null && uiPanel != null && uiPanel.activeSelf;
    public StorageBox CurrentBox => currentBox;

    StorageBox currentBox;
    TextMeshProUGUI _capacityText;
    TextMeshProUGUI _statusText;
    TextMeshProUGUI _storeButtonText;
    Button _storeButton;
    ScrollRect _storageScroll;
    PlayerInputHandler _input;
    bool _runtimeGenerated;
    bool _cursorCaptured;
    CursorLockMode _cursorLockBeforeOpen = CursorLockMode.Locked;
    bool _cursorVisibleBeforeOpen;
    float _nextSelectionRefreshAt;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        EnsureRuntimeUI();
    }

    void Start()
    {
        EnsureRuntimeUI();
        if (uiPanel != null) uiPanel.SetActive(false);

        _input = PlayerInputHandler.Instance;
        if (_input == null) return;
        _input.OnInventoryToggle += CloseForOtherPanel;
        _input.OnPhoneToggle += CloseForOtherPanel;
        _input.OnCraftToggle += CloseForOtherPanel;
    }

    void Update()
    {
        if (!IsOpen) return;

        if (currentBox == null || !currentBox.gameObject.activeInHierarchy)
        {
            CloseBox();
            return;
        }

        // 데모: 상자에서 걸어 나가면 닫는다(이동으로 닫기).
        if (Demo && Inventory.instance != null &&
            Vector3.Distance(Inventory.instance.transform.position, currentBox.transform.position) > 3.5f)
        {
            CloseBox();
            return;
        }

        if (Time.unscaledTime >= _nextSelectionRefreshAt)
        {
            _nextSelectionRefreshAt = Time.unscaledTime + 0.15f;
            RefreshStoreControl();
        }
    }

    void OnDestroy()
    {
        if (_input != null)
        {
            _input.OnInventoryToggle -= CloseForOtherPanel;
            _input.OnPhoneToggle -= CloseForOtherPanel;
            _input.OnCraftToggle -= CloseForOtherPanel;
        }

        if (_cursorCaptured) RestoreCursor();
        if (instance == this) instance = null;
    }

    public void OpenBox(StorageBox box)
    {
        if (box == null) return;
        EnsureRuntimeUI();
        if (uiPanel == null || itemsParent == null)
        {
            Debug.LogWarning("[StorageUI] 보관 화면을 구성하지 못했습니다.");
            return;
        }

        if (!IsOpen) CaptureCursor();
        CloseConflictingPanels();

        currentBox = box;
        uiPanel.transform.SetAsLastSibling();
        uiPanel.SetActive(true);
        UpdateUI();
        if (_storageScroll != null) _storageScroll.verticalNormalizedPosition = 1f;
        SetStatus(Demo
            ? "핫바에서 물품을 고르면 한 칸 전체를 보관할 수 있어요."
            : "핫바에서 물품을 고르면 1개씩 보관할 수 있어요.");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseBox()
    {
        CloseBox(restoreCursor: true);
    }

    void CloseBox(bool restoreCursor)
    {
        currentBox = null;
        if (uiPanel != null) uiPanel.SetActive(false);

        if (restoreCursor) RestoreCursor();
        else _cursorCaptured = false;
    }

    void CloseForOtherPanel()
    {
        if (IsOpen)
            CloseBox(restoreCursor: false);
    }

    void CloseConflictingPanels()
    {
        if (SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen)
            SmartphoneUI.instance.Toggle();

        if (InventoryUI.instance != null && InventoryUI.instance.gameObject.activeSelf)
            InventoryUI.instance.Toggle();
    }

    void CaptureCursor()
    {
        _cursorLockBeforeOpen = Cursor.lockState;
        _cursorVisibleBeforeOpen = Cursor.visible;
        _cursorCaptured = true;
    }

    void RestoreCursor()
    {
        if (!_cursorCaptured) return;
        Cursor.lockState = _cursorLockBeforeOpen;
        Cursor.visible = _cursorVisibleBeforeOpen;
        _cursorCaptured = false;
    }

    void UpdateUI()
    {
        if (currentBox == null || itemsParent == null) return;

        float scrollPosition = _storageScroll != null ? _storageScroll.verticalNormalizedPosition : 1f;
        foreach (Transform child in itemsParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        int itemCount = currentBox.items != null ? currentBox.items.Count : 0;
        int visibleSlots = _runtimeGenerated
            ? Mathf.Max(1, currentBox.maxSlotCount)
            : itemCount;

        for (int i = 0; i < visibleSlots; i++)
        {
            int index = i;
            ItemInstance inst = i < itemCount ? currentBox.items[i] : null;

            if (_runtimeGenerated)
            {
                CreateRuntimeSlot(index, inst);
                continue;
            }

            if (slotPrefab == null || inst == null || inst.data == null) continue;
            GameObject newSlot = Instantiate(slotPrefab, itemsParent);
            Transform iconTransform = newSlot.transform.Find("Icon");
            if (iconTransform != null && iconTransform.TryGetComponent(out Image icon))
            {
                icon.sprite = inst.data.icon;
                icon.enabled = icon.sprite != null;
            }

            if (newSlot.TryGetComponent(out Button btn))
                btn.onClick.AddListener(() => OnClickTakeItem(index));
        }

        RefreshHeader();
        RefreshStoreControl();
        if (_storageScroll != null && itemsParent is RectTransform content)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Canvas.ForceUpdateCanvases();
            _storageScroll.verticalNormalizedPosition = scrollPosition;
        }
    }

    void CreateRuntimeSlot(int index, ItemInstance inst)
    {
        bool occupied = inst != null && inst.data != null && inst.count > 0;
        var slot = new GameObject($"StorageSlot_{index + 1:00}",
            typeof(RectTransform), typeof(Image), typeof(Button));
        slot.transform.SetParent(itemsParent, false);
        SetSurface(slot.GetComponent<Image>(), occupied
            ? new Color(1f, 1f, .98f)
            : new Color(.90f, .89f, .82f));

        Button button = slot.GetComponent<Button>();
        button.interactable = occupied;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(.86f, .95f, .89f);
        colors.pressedColor = new Color(.74f, .86f, .79f);
        colors.disabledColor = Color.white;
        colors.fadeDuration = .08f;
        button.colors = colors;
        if (occupied) button.onClick.AddListener(() => OnClickTakeItem(index));

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(slot.transform, false);
        RectTransform iconRt = (RectTransform)iconGo.transform;
        SetFixed(iconRt, new Vector2(-41f, 34f), new Vector2(64f, 64f));
        Image itemIcon = iconGo.GetComponent<Image>();
        itemIcon.preserveAspect = true;
        itemIcon.raycastTarget = false;
        itemIcon.sprite = occupied ? inst.data.icon : null;
        itemIcon.enabled = itemIcon.sprite != null;

        string itemName = occupied ? Name(inst.data) : "빈 칸";
        var nameText = CreateText(slot.transform, "ItemName", itemName,
            occupied ? new Vector2(35f, 45f) : new Vector2(0f, 24f),
            occupied ? new Vector2(78f, 48f) : new Vector2(140f, 48f), 19f,
            occupied ? PAUiTheme.Ink : new Color(.40f, .47f, .44f), FontStyles.Bold);
        nameText.alignment = occupied ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
        nameText.textWrappingMode = TextWrappingModes.Normal;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        var countText = CreateText(slot.transform, "ItemCount", occupied ? $"×{inst.count}" : "",
            new Vector2(35f, 7f), new Vector2(78f, 28f), 19f, PAUiTheme.Teal, FontStyles.Bold);
        countText.alignment = TextAlignmentOptions.MidlineLeft;

        string detail = $"보관 칸 {index + 1:00}";
        if (occupied)
        {
            string price = inst.currentPrice > 0 ? "책정" : "기본";
            detail = $"{price} {inst.EffectivePrice}G\n품질 {Mathf.RoundToInt(inst.quality * 100f)}%";
            int maxDurability = ToolDurability.MaxFor(inst.data);
            if (maxDurability > 0)
                detail += $"\n내구도 {ToolDurability.Remaining(inst)} / {maxDurability}";
        }
        var detailText = CreateText(slot.transform, "ItemDetail", detail,
            new Vector2(0f, -42f), new Vector2(148f, 66f), 16f,
            occupied ? PAUiTheme.Ink : new Color(.40f, .47f, .44f),
            FontStyles.Normal);
        detailText.alignment = TextAlignmentOptions.Center;
        detailText.overflowMode = TextOverflowModes.Ellipsis;
    }

    void RefreshHeader()
    {
        if (currentBox == null) return;

        int occupied = 0;
        if (currentBox.items != null)
            foreach (ItemInstance item in currentBox.items)
                if (item != null && item.data != null && item.count > 0) occupied++;

        int max = Mathf.Max(1, currentBox.maxSlotCount);
        if (titleText != null) titleText.text = $"{currentBox.boxName} · {occupied}/{max}칸";
        if (_capacityText != null)
            _capacityText.text = Demo
                ? "핫바 물품 한 칸을 통째로 보관 · 보관 칸을 클릭해 꺼내기"
                : "핫바 물품을 1개씩 보관 · 보관 칸을 클릭해 가방으로 꺼내기";
    }

    void RefreshStoreControl()
    {
        if (_storeButton == null || _storeButtonText == null || currentBox == null) return;

        InventorySlot selected = GetSelectedHotbarSlot();
        bool hasSelected = selected != null && !selected.IsEmpty && selected.instance != null;
        bool hasSpace = currentBox.items != null
            && (currentBox.items.Count < Mathf.Max(1, currentBox.maxSlotCount) || Demo && hasSelected && MergeRoom(selected.instance) >= selected.instance.count);

        _storeButton.interactable = hasSelected && hasSpace;
        if (!hasSelected)
            _storeButtonText.text = "핫바에서 보관할 물품을 골라주세요";
        else if (!hasSpace)
            _storeButtonText.text = "창고가 가득 찼습니다";
        else
            _storeButtonText.text = Demo
                ? $"보관하기 · {Name(selected.item)} ×{selected.count}"
                : $"보관하기 · {selected.item.itemName} ×1";
    }

    // 창고 한 칸을 클릭하면 해당 ItemInstance 스택을 가방으로 옮긴다.
    void OnClickTakeItem(int index)
    {
        if (currentBox == null || currentBox.items == null) return;
        if (index < 0 || index >= currentBox.items.Count) return;
        if (Inventory.instance == null)
        {
            SetStatus("플레이어 가방을 찾지 못했습니다.", true);
            return;
        }

        ItemInstance inst = currentBox.items[index];
        if (inst == null || inst.data == null || inst.count <= 0) return;

        string itemName = Name(inst.data);
        int beforeCount = inst.count;
        bool added = Inventory.instance.AddInstance(inst);
        int remaining = Mathf.Max(0, inst.count);

        if (added || remaining <= 0)
        {
            currentBox.RemoveItem(index);
            int hotbarSlot = DemoHotbarPreference.Prefer(inst.data);
            SetStatus(hotbarSlot >= 0
                ? $"{itemName} ×{beforeCount}을(를) 꺼냈습니다 · 핫바 {hotbarSlot + 1}번"
                : $"{itemName} ×{beforeCount}을(를) 가방으로 꺼냈습니다.");
        }
        else if (remaining < beforeCount)
        {
            SetStatus($"{itemName} 일부를 꺼냈습니다. 창고에 {remaining}개 남았습니다.");
        }
        else
        {
            SetStatus("가방이 가득 차서 꺼낼 수 없습니다.", true);
        }

        UpdateUI();
        Inventory.instance.RefreshAllUI();
    }

    // 현재 선택된 핫바 스택에서 정확히 1개를 분리해 보관한다.
    // Item 원형 기준 RemoveItems를 쓰지 않아 다른 품질/가격 스택이 대신 차감되지 않는다.
    public void OnClickStoreItem()
    {
        if (currentBox == null) return;
        if (Inventory.instance == null)
        {
            SetStatus("플레이어 가방을 찾지 못했습니다.", true);
            return;
        }

        InventorySlot selected = GetSelectedHotbarSlot();
        ItemInstance heldInst = selected != null ? selected.instance : null;
        if (heldInst == null || heldInst.data == null || heldInst.count <= 0)
        {
            SetStatus("먼저 핫바에서 보관할 물품을 선택하세요.", true);
            return;
        }

        if (Demo)
        {
            // 데모: 선택한 칸 전체를 그대로 옮긴다(수량·품질·가격·도구 내구도 보존).
            string shown = Name(heldInst.data);
            int moved = heldInst.count;
            if (!StoreWhole(heldInst))
            {
                SetStatus("창고가 가득 차서 보관할 수 없습니다.", true);
                RefreshStoreControl();
                return;
            }
            selected.AddCount(-moved);
            SetStatus($"{shown} {moved}개를 창고에 보관했습니다.");
            UpdateUI();
            Inventory.instance.RefreshAllUI();
            return;
        }

        var splitInst = new ItemInstance(heldInst.data, 1)
        {
            quality = heldInst.quality,
            currentPrice = heldInst.currentPrice,
        };

        if (!currentBox.AddInstance(splitInst))
        {
            SetStatus("창고가 가득 차서 보관할 수 없습니다.", true);
            RefreshStoreControl();
            return;
        }

        string itemName = heldInst.data.itemName;
        selected.AddCount(-1);
        SetStatus($"{itemName} 1개를 창고에 보관했습니다.");
        UpdateUI();
        Inventory.instance.RefreshAllUI();
    }

    static bool Demo => PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.FirstDayControls;
    static string Name(Item item) => Demo ? ItemDisplayName.For(item) : item.itemName;

    // 같은 물품·품질·가격의 기존 칸에 더 들어갈 수 있는 수량. 도구는 개별 상태가 있어 합치지 않는다.
    int MergeRoom(ItemInstance held)
    {
        if (held == null || held.data == null || ToolDurability.MaxFor(held.data) > 0) return 0;
        int stack = Mathf.Max(1, held.data.maxStack), room = 0;
        foreach (ItemInstance entry in currentBox.items)
            if (Mergeable(entry, held)) room += Mathf.Max(0, stack - entry.count);
        return room;
    }

    static bool Mergeable(ItemInstance entry, ItemInstance held) =>
        entry != null && entry != held && entry.data == held.data && Mathf.Approximately(entry.quality, held.quality) &&
        entry.currentPrice == held.currentPrice;

    bool StoreWhole(ItemInstance held)
    {
        if (MergeRoom(held) >= held.count)
        {
            int stack = Mathf.Max(1, held.data.maxStack), left = held.count;
            foreach (ItemInstance entry in currentBox.items)
            {
                if (left <= 0 || !Mergeable(entry, held)) continue;
                int add = Mathf.Min(left, stack - entry.count);
                if (add > 0) { entry.count += add; left -= add; }
            }
            return true;
        }
        return currentBox.items.Count < Mathf.Max(1, currentBox.maxSlotCount) && currentBox.AddInstance(held);
    }

    static InventorySlot GetSelectedHotbarSlot()
    {
        Inventory inventory = Inventory.instance;
        if (inventory == null || inventory.hotbar == null) return null;
        return inventory.hotbar.GetSlot(inventory.selectedHotbarIndex);
    }

    void SetStatus(string message, bool isError = false)
    {
        if (_statusText == null) return;
        _statusText.text = message;
        _statusText.color = _runtimeGenerated
            ? (isError ? new Color(.61f, .27f, .17f) : PAUiTheme.Ink)
            : (isError ? new Color(1f, .68f, .55f) : new Color(.78f, .86f, .80f));
    }

    void EnsureRuntimeUI()
    {
        if (uiPanel != null && itemsParent != null && titleText != null) return;

        _runtimeGenerated = true;
        Transform parent = ResolveUiParent();

        uiPanel = new GameObject("StorageOverlay", typeof(RectTransform), typeof(Image));
        RectTransform overlayRt = (RectTransform)uiPanel.transform;
        overlayRt.SetParent(parent, false);
        Stretch(overlayRt);
        uiPanel.GetComponent<Image>().color = new Color(.04f, .07f, .08f, .38f);

        var panel = new GameObject("StoragePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(uiPanel.transform, false);
        RectTransform panelRt = (RectTransform)panel.transform;
        SetFixed(panelRt, Vector2.zero, new Vector2(960f, 700f));
        SetSurface(panel.GetComponent<Image>(), PAUiTheme.Cream);

        var header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(panel.transform, false);
        SetFixed((RectTransform)header.transform, new Vector2(0f, 310f), new Vector2(960f, 80f));
        SetSurface(header.GetComponent<Image>(), PAUiTheme.Teal);
        header.GetComponent<Image>().raycastTarget = false;

        titleText = CreateText(panel.transform, "Title", "창고",
            new Vector2(-72f, 310f), new Vector2(704f, 56f), 34f, Color.white, FontStyles.Bold);
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 24f;
        titleText.fontSizeMax = 34f;

        Button closeButton = CreateButton(panel.transform, "CloseButton", "닫기  Esc",
            new Vector2(388f, 310f), new Vector2(128f, 42f),
            new Color(1f, 1f, 1f, .18f), CloseBox);
        closeButton.GetComponentInChildren<TextMeshProUGUI>().fontSize = 18f;

        _capacityText = CreateText(panel.transform, "CapacityText", "",
            new Vector2(0f, 234f), new Vector2(880f, 36f), 20f,
            PAUiTheme.Ink, FontStyles.Normal);
        _capacityText.alignment = TextAlignmentOptions.Center;

        var scrollGo = new GameObject("StorageScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(panel.transform, false);
        SetFixed((RectTransform)scrollGo.transform, new Vector2(0f, 8f), new Vector2(880f, 360f));
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        Stretch((RectTransform)viewport.transform);
        viewport.GetComponent<Image>().color = PAUiTheme.Cream;

        var grid = new GameObject("StorageGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        grid.transform.SetParent(viewport.transform, false);
        RectTransform gridRt = (RectTransform)grid.transform;
        gridRt.anchorMin = new Vector2(0f, 1f);
        gridRt.anchorMax = Vector2.one;
        gridRt.pivot = new Vector2(.5f, 1f);
        gridRt.sizeDelta = Vector2.zero;
        var layout = grid.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(164f, 164f);
        layout.spacing = new Vector2(10f, 10f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 5;
        layout.padding = new RectOffset(0, 16, 4, 4);
        layout.childAlignment = TextAnchor.UpperCenter;
        grid.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        itemsParent = grid.transform;
        _storageScroll = scrollGo.GetComponent<ScrollRect>();
        _storageScroll.viewport = (RectTransform)viewport.transform;
        _storageScroll.content = gridRt;
        _storageScroll.horizontal = false;
        _storageScroll.scrollSensitivity = 34f;

        var rail = new GameObject("StorageScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        var railRt = (RectTransform)rail.transform;
        railRt.SetParent(scrollGo.transform, false);
        railRt.anchorMin = new Vector2(1f, 0f);
        railRt.anchorMax = Vector2.one;
        railRt.pivot = new Vector2(1f, .5f);
        railRt.sizeDelta = new Vector2(10f, -8f);
        SetSurface(rail.GetComponent<Image>(), new Color(.85f, .84f, .76f));
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        var handleRt = (RectTransform)handle.transform;
        handleRt.SetParent(rail.transform, false);
        Stretch(handleRt);
        SetSurface(handle.GetComponent<Image>(), PAUiTheme.Teal);
        var scrollbar = rail.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRt;
        scrollbar.targetGraphic = handle.GetComponent<Image>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        _storageScroll.verticalScrollbar = scrollbar;
        _storageScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        _statusText = CreateText(panel.transform, "StatusText", "",
            new Vector2(0f, -213f), new Vector2(880f, 48f), 20f,
            PAUiTheme.Ink, FontStyles.Normal);
        _statusText.alignment = TextAlignmentOptions.Center;
        _statusText.textWrappingMode = TextWrappingModes.Normal;

        _storeButton = CreateButton(panel.transform, "StoreSelectedButton", "선택 핫바 보관",
            new Vector2(0f, -290f), new Vector2(600f, 56f),
            PAUiTheme.Teal, OnClickStoreItem);
        _storeButtonText = _storeButton.GetComponentInChildren<TextMeshProUGUI>();

        uiPanel.SetActive(false);
    }

    Transform ResolveUiParent()
    {
        GameObject root = GameObject.Find("PA_UIRoot");
        if (root != null) return root.transform;

        var canvasGo = new GameObject("StorageUI_Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 145;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvasGo.transform;
    }

    static Button CreateButton(Transform parent, string name, string label,
        Vector2 position, Vector2 size, Color background, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        SetFixed(rt, position, size);
        SetSurface(go.GetComponent<Image>(), background);

        Button button = go.GetComponent<Button>();
        button.onClick.AddListener(onClick);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(.90f, .96f, .93f);
        colors.pressedColor = new Color(0.82f, 0.88f, 0.84f, 1f);
        colors.disabledColor = new Color(.58f, .62f, .60f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TextMeshProUGUI text = CreateText(go.transform, "Label", label,
            Vector2.zero, size - new Vector2(20f, 10f), 20f, Color.white, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string value,
        Vector2 position, Vector2 size, float fontSize, Color color, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        SetFixed(rt, position, size);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    static void SetSurface(Image image, Color color)
    {
        image.sprite = PAUiTheme.RoundedSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
    }

    static void SetFixed(RectTransform rt, Vector2 position, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
