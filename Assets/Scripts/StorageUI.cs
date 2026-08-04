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
        SetStatus("핫바에서 물품을 선택해 아래 버튼으로 1개씩 보관하세요.");

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

        foreach (Transform child in itemsParent)
            Destroy(child.gameObject);

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
    }

    void CreateRuntimeSlot(int index, ItemInstance inst)
    {
        bool occupied = inst != null && inst.data != null && inst.count > 0;
        var slot = new GameObject($"StorageSlot_{index + 1:00}",
            typeof(RectTransform), typeof(Image), typeof(Button));
        slot.transform.SetParent(itemsParent, false);
        slot.GetComponent<Image>().color = occupied
            ? new Color(0.18f, 0.27f, 0.22f, 0.98f)
            : new Color(0.12f, 0.17f, 0.14f, 0.76f);

        Button button = slot.GetComponent<Button>();
        button.interactable = occupied;
        if (occupied) button.onClick.AddListener(() => OnClickTakeItem(index));

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(slot.transform, false);
        RectTransform iconRt = (RectTransform)iconGo.transform;
        SetFixed(iconRt, new Vector2(-37f, 10f), new Vector2(42f, 42f));
        Image itemIcon = iconGo.GetComponent<Image>();
        itemIcon.preserveAspect = true;
        itemIcon.raycastTarget = false;
        itemIcon.sprite = occupied ? inst.data.icon : null;
        itemIcon.enabled = itemIcon.sprite != null;

        string itemName = occupied ? inst.data.itemName : "빈 칸";
        var nameText = CreateText(slot.transform, "ItemName", itemName,
            new Vector2(25f, 14f), new Vector2(72f, 38f), 14f,
            occupied ? Color.white : new Color(0.52f, 0.61f, 0.55f, 1f), FontStyles.Bold);
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.textWrappingMode = TextWrappingModes.Normal;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        string detail = occupied
            ? $"×{inst.count} · 품질 {Mathf.RoundToInt(inst.quality * 100f)}% · {inst.EffectivePrice}G"
            : $"{index + 1:00}";
        var detailText = CreateText(slot.transform, "ItemDetail", detail,
            new Vector2(0f, -28f), new Vector2(116f, 24f), 10.5f,
            occupied ? new Color(0.78f, 0.86f, 0.80f, 1f) : new Color(0.38f, 0.45f, 0.40f, 1f),
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
            _capacityText.text = "물품 칸을 클릭하면 가방으로 꺼냅니다 · 품질과 책정 가격이 그대로 보존됩니다";
    }

    void RefreshStoreControl()
    {
        if (_storeButton == null || _storeButtonText == null || currentBox == null) return;

        InventorySlot selected = GetSelectedHotbarSlot();
        bool hasSelected = selected != null && !selected.IsEmpty && selected.instance != null;
        bool hasSpace = currentBox.items != null
            && currentBox.items.Count < Mathf.Max(1, currentBox.maxSlotCount);

        _storeButton.interactable = hasSelected && hasSpace;
        if (!hasSelected)
            _storeButtonText.text = "선택 핫바 보관 · 선택된 물품 없음";
        else if (!hasSpace)
            _storeButtonText.text = "창고가 가득 찼습니다";
        else
            _storeButtonText.text = $"선택 핫바 보관 · {selected.item.itemName} 1개";
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

        string itemName = inst.data.itemName;
        int beforeCount = inst.count;
        bool added = Inventory.instance.AddInstance(inst);
        int remaining = Mathf.Max(0, inst.count);

        if (added || remaining <= 0)
        {
            currentBox.RemoveItem(index);
            SetStatus($"{itemName} ×{beforeCount}을(를) 가방으로 꺼냈습니다.");
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
        _statusText.color = isError
            ? new Color(1f, 0.68f, 0.55f, 1f)
            : new Color(0.78f, 0.86f, 0.80f, 1f);
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
        uiPanel.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.02f, 0.78f);

        var panel = new GameObject("StoragePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(uiPanel.transform, false);
        RectTransform panelRt = (RectTransform)panel.transform;
        SetFixed(panelRt, Vector2.zero, new Vector2(980f, 720f));
        panel.GetComponent<Image>().color = new Color(0.07f, 0.11f, 0.09f, 0.98f);

        titleText = CreateText(panel.transform, "Title", "창고",
            new Vector2(-25f, 310f), new Vector2(620f, 60f), 38f, Color.white, FontStyles.Bold);
        titleText.alignment = TextAlignmentOptions.Center;

        Button closeButton = CreateButton(panel.transform, "CloseButton", "닫기  (ESC)",
            new Vector2(410f, 310f), new Vector2(130f, 46f),
            new Color(0.35f, 0.24f, 0.19f, 1f), CloseBox);
        closeButton.GetComponentInChildren<TextMeshProUGUI>().fontSize = 16f;

        _capacityText = CreateText(panel.transform, "CapacityText", "",
            new Vector2(0f, 258f), new Vector2(850f, 34f), 15f,
            new Color(0.72f, 0.84f, 0.76f, 1f), FontStyles.Normal);
        _capacityText.alignment = TextAlignmentOptions.Center;

        var grid = new GameObject("StorageGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        grid.transform.SetParent(panel.transform, false);
        RectTransform gridRt = (RectTransform)grid.transform;
        SetFixed(gridRt, new Vector2(0f, 18f), new Vector2(820f, 390f));
        var layout = grid.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(126f, 88f);
        layout.spacing = new Vector2(10f, 10f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 6;
        layout.childAlignment = TextAnchor.UpperCenter;
        itemsParent = grid.transform;

        _statusText = CreateText(panel.transform, "StatusText", "",
            new Vector2(0f, -244f), new Vector2(850f, 52f), 16f,
            new Color(0.78f, 0.86f, 0.80f, 1f), FontStyles.Normal);
        _statusText.alignment = TextAlignmentOptions.Center;
        _statusText.textWrappingMode = TextWrappingModes.Normal;

        _storeButton = CreateButton(panel.transform, "StoreSelectedButton", "선택 핫바 보관",
            new Vector2(0f, -309f), new Vector2(500f, 58f),
            new Color(0.12f, 0.48f, 0.30f, 1f), OnClickStoreItem);
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
        go.GetComponent<Image>().color = background;

        Button button = go.GetComponent<Button>();
        button.onClick.AddListener(onClick);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.82f, 0.88f, 0.84f, 1f);
        colors.disabledColor = new Color(0.44f, 0.48f, 0.45f, 0.65f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TextMeshProUGUI text = CreateText(go.transform, "Label", label,
            Vector2.zero, size - new Vector2(20f, 10f), 19f, Color.white, FontStyles.Bold);
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
