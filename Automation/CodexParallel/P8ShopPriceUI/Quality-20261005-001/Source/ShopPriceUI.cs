using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// §2 ShopPriceUI — 문라이터2 스타일 가격 탐색 UI.
//
// ShopSlot.Interact() 가 호출하는 싱글톤 패널.
// ─ 빈 슬롯:    인벤토리 선택 모드 (핫바 아이템 → 슬롯에 진열)
// ─ 찬 슬롯:    가격 조정 모드 (현재 진열 아이템의 displayPrice 변경 + 회수)
//
// 🐾 자기완결 싱글톤: Awake 에서 Canvas 를 직접 빌드한다.
// 가격은 플레이어가 정한다. 구매 판단의 내부 수치는 노출하지 않는다.
//
// PlayerController 이동 차단: PlayerController.Update() 에
//   if (ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen) return;
// 이미 추가돼 있어야 한다. (기능_명세서.md §씬구성 참조)
public class ShopPriceUI : MonoBehaviour
{
    public Slider TutorialPriceDrag { get; private set; }
    public bool TutorialPriceDragged { get; private set; }
    bool FirstDayTutorial => gameObject.scene.name == DepartureTutorialController.SceneName && FirstDayStudioAssets.Load() != null;
    void ConfigureTutorialPriceDrag()
    {
        if (TutorialPriceDrag != null) return;
        var root = new GameObject("PriceDrag", typeof(RectTransform), typeof(Image), typeof(Slider));
        root.transform.SetParent(_panel, false);
        var rect = (RectTransform)root.transform;
        rect.sizeDelta = new Vector2(440, 20);
        rect.anchoredPosition = new Vector2(0, -62);
        root.GetComponent<Image>().color = new Color(.28f, .36f, .34f);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(root.transform, false);
        var handleRect = (RectTransform)handle.transform;
        handleRect.anchorMin = handleRect.anchorMax = new Vector2(0, .5f);
        handleRect.sizeDelta = new Vector2(24, 34);
        handle.GetComponent<Image>().color = C_Gold;
        TutorialPriceDrag = root.GetComponent<Slider>();
        TutorialPriceDrag.minValue = 1;
        TutorialPriceDrag.maxValue = FirstDayTutorial ? 40 : 100;
        TutorialPriceDrag.wholeNumbers = true;
        TutorialPriceDrag.handleRect = handleRect;
        TutorialPriceDrag.targetGraphic = handle.GetComponent<Image>();
        TutorialPriceDrag.onValueChanged.AddListener(v =>
        {
            _pendingPrice = Mathf.RoundToInt(v);
            TutorialPriceDragged = true;
            RefreshUI();
        });
    }
    public static ShopPriceUI instance;

    public bool IsOpen { get; private set; }
    public int ConfirmCount { get; private set; }
    public static event System.Action<ShopSlot> OnPriceConfirmed;

    // ── UI 참조 ─────────────────────────────────────────────────────────────────
    Canvas          _canvas;
    RectTransform   _panel;
    TextMeshProUGUI _titleTxt;       // "가격 설정" / "진열하기"
    TextMeshProUGUI _itemNameTxt;    // 아이템 이름
    TextMeshProUGUI _priceTxt;       // 현재 설정 가격
    TextMeshProUGUI _currentPriceTxt;
    TextMeshProUGUI _pendingLabelTxt;
    TextMeshProUGUI _modeHintTxt;
    Image          _itemIcon;
    Button[]       _adjustButtons;
    Button          _confirmBtn;
    Button          _retrieveBtn;
    Button          _closeBtn;

    // ── 상태 ─────────────────────────────────────────────────────────────────────
    ShopSlot _slot;
    int      _pendingPrice;

    // ── 색상 팔레트 (레퍼런스.html 기준) ────────────────────────────────────────
    static readonly Color C_PanelBG     = SmartphoneUI.Cream;
    static readonly Color C_Header      = SmartphoneUI.Teal;
    static readonly Color C_Gold        = new Color(.96f, .84f, .43f);
    static readonly Color C_BtnGreen    = SmartphoneUI.Teal;
    static readonly Color C_BtnOrange   = new Color(.71f, .36f, .26f);
    static readonly Color C_BtnGray     = new Color(.42f, .45f, .43f);

    // ──────────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        BuildUI();
        SetCanvasActive(false);
    }

    // ── UI 빌드 ─────────────────────────────────────────────────────────────────
    void BuildUI()
    {
        // 🎯 항상 전용 Canvas 생성 — sortOrder 200 으로 다른 UI 위에 표시.
        // PA_UIRoot 에 붙이면 sortOrder 가 0 이어서 다른 UI 에 가릴 수 있으므로 독립 생성.
        var cGO = new GameObject("ShopPriceUI_Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas                       = cGO.GetComponent<Canvas>();
        _canvas.renderMode            = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder          = 200; // 최상위 — ScreenFader(9999) 보다는 낮음
        var scaler                    = cGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution    = new Vector2(1920, 1080);
        scaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight     = 0.5f;
        Transform canvasParent        = cGO.transform;

        // ── 배경 블로커 (클릭 시 닫기) ────────────────────────────────────────
        var blockerGO = new GameObject("Blocker", typeof(RectTransform), typeof(Image), typeof(Button));
        var blockerRT = (RectTransform)blockerGO.transform;
        blockerRT.SetParent(canvasParent, false);
        StretchFull(blockerRT);
        blockerGO.GetComponent<Image>().color = new Color(.04f, .07f, .08f, .38f);
        blockerGO.GetComponent<Button>().onClick.AddListener(Close);

        // 기존 C4 표시 개선을 최신 가격/커서 계약에 통합한다. 거래 경로는 그대로다.
        var panelGO = new GameObject("ShopPricePanel", typeof(RectTransform), typeof(Image));
        _panel      = (RectTransform)panelGO.transform;
        _panel.SetParent(canvasParent, false);
        _panel.anchorMin        = new Vector2(0.5f, 0.5f);
        _panel.anchorMax        = new Vector2(0.5f, 0.5f);
        _panel.pivot            = new Vector2(0.5f, 0.5f);
        _panel.sizeDelta        = new Vector2(680, 640);
        _panel.anchoredPosition = Vector2.zero;
        SetSurface(panelGO.GetComponent<Image>(), C_PanelBG);

        var header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        var headerRT = (RectTransform)header.transform;
        headerRT.SetParent(_panel, false);
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = Vector2.one;
        headerRT.pivot = new Vector2(.5f, 1f);
        headerRT.sizeDelta = new Vector2(0f, 88f);
        SetSurface(header.GetComponent<Image>(), C_Header);
        header.GetComponent<Image>().raycastTarget = false;

        var preview = new GameObject("PendingPriceCard", typeof(RectTransform), typeof(Image));
        var previewRT = (RectTransform)preview.transform;
        previewRT.SetParent(_panel, false);
        previewRT.anchorMin = previewRT.anchorMax = new Vector2(.5f, .5f);
        previewRT.pivot = new Vector2(.5f, .5f);
        previewRT.sizeDelta = new Vector2(624f, 120f);
        previewRT.anchoredPosition = new Vector2(0f, 36f);
        SetSurface(preview.GetComponent<Image>(), new Color(.98f, .91f, .66f));
        preview.GetComponent<Image>().raycastTarget = false;

        var icon = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image));
        var iconRT = (RectTransform)icon.transform;
        iconRT.SetParent(_panel, false);
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(.5f, .5f);
        iconRT.pivot = new Vector2(.5f, .5f);
        iconRT.sizeDelta = new Vector2(80f, 80f);
        iconRT.anchoredPosition = new Vector2(-244f, 181f);
        _itemIcon = icon.GetComponent<Image>();
        _itemIcon.preserveAspect = true;
        _itemIcon.raycastTarget = false;
        _itemIcon.enabled = false;

        _titleTxt = CreateLabel(_panel, "TitleTxt", "가격 설정",
            new Rect(-300, 300, 600, 56), 32, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
        _itemNameTxt = CreateLabel(_panel, "ItemNameTxt", "—",
            new Rect(-178, 222, 478, 66), 26, FontStyles.Bold, SmartphoneUI.Ink, TextAlignmentOptions.MidlineLeft);
        _itemNameTxt.enableAutoSizing = true;
        _itemNameTxt.fontSizeMin = 20f;
        _itemNameTxt.fontSizeMax = 26f;
        _currentPriceTxt = CreateLabel(_panel, "CurrentPriceTxt", "",
            new Rect(-300, 133, 600, 30), 20, FontStyles.Normal,
            SmartphoneUI.Teal, TextAlignmentOptions.Center);
        _pendingLabelTxt = CreateLabel(_panel, "PendingPriceLabel", "확정 전 임시 가격",
            new Rect(-300, 88, 600, 26), 19, FontStyles.Normal,
            SmartphoneUI.Ink, TextAlignmentOptions.Center);
        _priceTxt = CreateLabel(_panel, "PriceTxt", "0 G",
            new Rect(-300, 58, 600, 62), 48, FontStyles.Bold, SmartphoneUI.Ink, TextAlignmentOptions.Center);
        _priceTxt.enableAutoSizing = true;
        _priceTxt.fontSizeMin = 30f;
        _priceTxt.fontSizeMax = 48f;
        ConfigureTutorialPriceDrag();
        BuildAdjustRow(_panel, -120, new[] { -10, -1, 1, 10 });
        _modeHintTxt = CreateLabel(_panel, "DragHint", "버튼 또는 드래그로 가격 조절",
            new Rect(-300, -160, 600, 62), 18, FontStyles.Normal,
            SmartphoneUI.Ink, TextAlignmentOptions.Center);
        _confirmBtn = BuildActionBtn(_panel, "가격 확정", C_BtnGreen, new Vector2(-210, -272), OnConfirm);
        _retrieveBtn = BuildActionBtn(_panel, "상품 회수", C_BtnOrange, new Vector2(0, -272), OnRetrieve);
        _closeBtn = BuildActionBtn(_panel, "닫기", C_BtnGray, new Vector2(210, -272), Close);
        // 기존 GameObject 이름과 listener를 유지하고 화면 문구만 명확히 한다.
        _confirmBtn.GetComponentInChildren<TextMeshProUGUI>().text = "가격 적용";
        _closeBtn.GetComponentInChildren<TextMeshProUGUI>().text = "취소 / 닫기";
    }

    // ── UI 헬퍼 ─────────────────────────────────────────────────────────────────
    void BuildAdjustRow(Transform parent, float y, int[] deltas)
    {
        float totalW = 440f;
        float btnW   = totalW / deltas.Length - 8;
        float startX = -totalW * 0.5f + btnW * 0.5f;

        _adjustButtons = new Button[deltas.Length];
        for (int i = 0; i < deltas.Length; i++)
        {
            int d = deltas[i];
            string label = d > 0 ? $"+{d}" : $"{d}";
            Color col    = d > 0
                ? SmartphoneUI.Teal
                : new Color(.67f, .38f, .28f);

            var btn = BuildActionBtn(parent, label, col,
                new Vector2(startX + i * (btnW + 8), y), null);
            btn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 22;
            var rt  = (RectTransform)btn.transform;
            rt.sizeDelta = new Vector2(btnW, 48);

            btn.onClick.AddListener(() => AdjustPrice(d));
            _adjustButtons[i] = btn;
        }
    }

    Button BuildActionBtn(Transform parent, string label, Color bg,
                          Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go  = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt  = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(196, 56);
        rt.anchoredPosition = pos;

        SetSurface(go.GetComponent<Image>(), bg);

        var txt = new GameObject("Text", typeof(TextMeshProUGUI));
        txt.transform.SetParent(go.transform, false);
        StretchFull((RectTransform)txt.transform);
        var tmp     = txt.GetComponent<TextMeshProUGUI>();
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text    = label;
        tmp.fontSize = 21;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color   = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        var btn = go.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(.90f, .96f, .93f);
        colors.pressedColor = new Color(.78f, .86f, .82f);
        colors.disabledColor = new Color(.58f, .62f, .60f);
        colors.fadeDuration = .08f;
        btn.colors = colors;
        if (onClick != null) btn.onClick.AddListener(onClick);
        return btn;
    }

    TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
                                Rect rect, float size, FontStyles style,
                                Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(rect.width, rect.height);
        rt.anchoredPosition = new Vector2(rect.x + rect.width * 0.5f, rect.y - rect.height * 0.5f);

        var tmp           = go.GetComponent<TextMeshProUGUI>();
        tmp.font          = TMP_Settings.defaultFontAsset;
        tmp.text          = text;
        tmp.fontSize      = size;
        tmp.fontStyle     = style;
        tmp.color         = color;
        tmp.alignment     = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void SetSurface(Image image, Color color)
    {
        image.sprite = SmartphoneUI.RoundedSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }

    // ── 공개 API ─────────────────────────────────────────────────────────────────

    // SlotSlot.Interact() 에서 호출. 이미 진열된 아이템의 가격을 조정하거나 회수한다.
    public void Open(ShopSlot slot)
    {
        ConfigureTutorialPriceDrag();
        _slot        = slot;
        _pendingPrice = (slot.displayPrice > 0)
            ? slot.displayPrice
            : (slot.currentItem?.data != null ? slot.currentItem.data.basePrice : 100);

        IsOpen = true;
        SetCanvasActive(true);

        // 커서 잠금 해제 (UI 조작 가능)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // 인벤토리/스마트폰 열려있으면 닫기 (상호배타)
        if (InventoryUI.instance != null && InventoryUI.instance.gameObject.activeSelf)
            InventoryUI.instance.Toggle();
        if (SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen)
            SmartphoneUI.instance.Toggle();

        RefreshUI();
        if (TutorialPriceDrag != null) TutorialPriceDrag.SetValueWithoutNotify(_pendingPrice);
    }

    public void Close()
    {
        _slot  = null;
        IsOpen = false;
        SetCanvasActive(false);
        PlayerInputHandler.RestoreGameplayCursor();
    }

    // ── 내부 로직 ────────────────────────────────────────────────────────────────

    void AdjustPrice(int delta)
    {
        _pendingPrice = Mathf.Max(1, _pendingPrice + delta);
        RefreshUI();
    }

    void OnConfirm()
    {
        if (_slot == null) { Close(); return; }
        _slot.displayPrice = _pendingPrice;
        _slot.RefreshDisplay();
        ConfirmCount++;
        Debug.Log($"🏷️ 가격 확정: {_slot.currentItem?.data?.itemName} @ {_pendingPrice}G");
        ShopSlot confirmedSlot = _slot;
        Close();
        OnPriceConfirmed?.Invoke(confirmedSlot);
    }

    void OnRetrieve()
    {
        if (_slot == null) { Close(); return; }
        _slot.RetrieveItem(); // ShopSlot 에 공개 메서드 추가됨
        Close();
    }

    void RefreshUI()
    {
        if (_slot == null) return;

        // 타이틀
        bool occupied = !_slot.IsEmpty;
        if (_titleTxt != null)
            _titleTxt.text = occupied ? "진열 상품 · 가격 설정" : "진열하기";

        // 아이템 이름
        string itemName = occupied && _slot.currentItem?.data != null
            ? (WorldGameplayAdapterService.Instance?.FirstDay == true ? ItemDisplayName.For(_slot.currentItem.data) : _slot.currentItem.data.itemName)
            : "진열된 상품이 없어요";
        if (_itemNameTxt != null)
        {
            // Task 018 — 이미 저장·판매에 쓰이는 실제 진열 수량을 가격 결정 화면에 노출한다.
            // 별도 재고 모델을 만들지 않고 ShopSlot.currentItem.count를 그대로 읽는다.
            _itemNameTxt.text = occupied
                ? $"{itemName}  ·  재고 {_slot.currentItem.count:N0}개"
                : itemName;
        }

        // 회수 버튼 활성 (빈 슬롯이면 회수 불필요)
        if (_retrieveBtn != null)
            _retrieveBtn.gameObject.SetActive(occupied);

        if (_itemIcon != null)
        {
            _itemIcon.sprite = occupied ? _slot.currentItem.data.icon : null;
            _itemIcon.enabled = _itemIcon.sprite != null;
        }
        if (_currentPriceTxt != null)
            _currentPriceTxt.text = occupied ? $"현재 적용 가격  {_slot.EffectiveDisplayPrice:N0} G / 개" : "상품 진열 후 가격을 설정할 수 있어요";
        if (_pendingLabelTxt != null)
            _pendingLabelTxt.text = occupied ? "확정 전 임시 가격 · 적용 버튼으로 확정" : "먼저 상품을 진열하세요";
        if (_modeHintTxt != null)
            _modeHintTxt.text = occupied
                ? "±1 / ±10 또는 드래그로 1개 가격을 정하세요.\n취소하면 임시 가격만 버리고 상품은 유지해요."
                : "상품을 선택해 진열대를 이용하세요.\n이 화면을 닫아도 재고와 가격은 바뀌지 않아요.";
        if (_confirmBtn != null) _confirmBtn.interactable = occupied;
        if (_adjustButtons != null)
            foreach (Button button in _adjustButtons) button.interactable = occupied;

        // 가격 표시
        if (_priceTxt != null)
            _priceTxt.text = occupied ? $"{_pendingPrice:N0} G / 개" : "—";

        if (TutorialPriceDrag != null)
        {
            TutorialPriceDrag.interactable = occupied;
            // 버튼으로 범위를 넘겼을 때만 확장한다. 드래그 중 눈금은 고정한다.
            if (_pendingPrice > TutorialPriceDrag.maxValue)
                TutorialPriceDrag.maxValue = _pendingPrice * 2f;
            TutorialPriceDrag.SetValueWithoutNotify(_pendingPrice);
        }
    }

    void SetCanvasActive(bool active)
    {
        if (_canvas != null) _canvas.gameObject.SetActive(active);
    }
}
