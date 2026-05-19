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
// NPC 반응 힌트: displayPrice / basePrice 비율로 즉각 피드백 제공.
//
// PlayerController 이동 차단: PlayerController.Update() 에
//   if (ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen) return;
// 이미 추가돼 있어야 한다. (기능_명세서.md §씬구성 참조)
public class ShopPriceUI : MonoBehaviour
{
    public static ShopPriceUI instance;

    public bool IsOpen { get; private set; }
    public int ConfirmCount { get; private set; }

    // ── UI 참조 ─────────────────────────────────────────────────────────────────
    Canvas          _canvas;
    RectTransform   _panel;
    TextMeshProUGUI _titleTxt;       // "가격 설정" / "진열하기"
    TextMeshProUGUI _itemNameTxt;    // 아이템 이름
    TextMeshProUGUI _priceTxt;       // 현재 설정 가격
    TextMeshProUGUI _reactionTxt;    // NPC 반응 힌트
    Image           _reactionBar;    // 색상으로 반응 강도 표현
    TextMeshProUGUI _reactionBarTxt; // 반응 바 안 설명
    Button          _confirmBtn;
    Button          _retrieveBtn;
    Button          _closeBtn;

    // ── 상태 ─────────────────────────────────────────────────────────────────────
    ShopSlot _slot;
    int      _pendingPrice;

    // ── 색상 팔레트 (레퍼런스.html 기준) ────────────────────────────────────────
    static readonly Color C_PanelBG     = new Color(0.13f, 0.13f, 0.13f, 0.96f);
    static readonly Color C_Header      = new Color(0.941f, 0.502f, 0.439f, 1f);  // #F08070 coral
    static readonly Color C_Gold        = new Color(1f,    0.92f,  0.38f,  1f);   // #FFEB61
    static readonly Color C_BtnGreen    = new Color(0.28f, 0.68f,  0.40f,  1f);
    static readonly Color C_BtnOrange   = new Color(0.86f, 0.48f,  0.22f,  1f);
    static readonly Color C_BtnGray     = new Color(0.30f, 0.30f,  0.30f,  1f);
    static readonly Color C_ReactionGood= new Color(0.25f, 0.75f,  0.35f,  1f);
    static readonly Color C_ReactionMid = new Color(0.90f, 0.78f,  0.22f,  1f);
    static readonly Color C_ReactionBad = new Color(0.85f, 0.22f,  0.22f,  1f);

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
        blockerGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.45f);
        blockerGO.GetComponent<Button>().onClick.AddListener(Close);

        // ── 메인 패널 (중앙 고정, 400×340) ────────────────────────────────────
        var panelGO = new GameObject("ShopPricePanel", typeof(RectTransform), typeof(Image));
        _panel      = (RectTransform)panelGO.transform;
        _panel.SetParent(canvasParent, false);
        _panel.anchorMin        = new Vector2(0.5f, 0.5f);
        _panel.anchorMax        = new Vector2(0.5f, 0.5f);
        _panel.pivot            = new Vector2(0.5f, 0.5f);
        _panel.sizeDelta        = new Vector2(400, 360);
        _panel.anchoredPosition = Vector2.zero;
        panelGO.GetComponent<Image>().color = C_PanelBG;

        float y = 155f; // 상단부터 내려가며 배치 (anchoredPosition y 기준)

        // ── 헤더 타이틀 ──────────────────────────────────────────────────────────
        _titleTxt = CreateLabel(panelGO.transform, "TitleTxt", "가격 설정",
            new Rect(-180, y, 360, 36), 20, FontStyles.Bold, C_Header, TextAlignmentOptions.Center);
        y -= 44;

        // ── 아이템 이름 ──────────────────────────────────────────────────────────
        _itemNameTxt = CreateLabel(panelGO.transform, "ItemNameTxt", "—",
            new Rect(-180, y, 360, 28), 17, FontStyles.Normal,
            Color.white, TextAlignmentOptions.Center);
        y -= 36;

        // ── 가격 표시 ─────────────────────────────────────────────────────────
        _priceTxt = CreateLabel(panelGO.transform, "PriceTxt", "0 G",
            new Rect(-180, y, 360, 44), 28, FontStyles.Bold,
            C_Gold, TextAlignmentOptions.Center);
        y -= 50;

        // ── +/- 버튼 행 ───────────────────────────────────────────────────────
        BuildAdjustRow(panelGO.transform, y, new[] { -1000, -100, -10, +10, +100, +1000 });
        y -= 44;

        // ── NPC 반응 바 ───────────────────────────────────────────────────────
        var barBgGO = new GameObject("ReactionBarBg", typeof(RectTransform), typeof(Image));
        var barBgRT = (RectTransform)barBgGO.transform;
        barBgRT.SetParent(panelGO.transform, false);
        barBgRT.anchorMin        = new Vector2(0.5f, 0.5f);
        barBgRT.anchorMax        = new Vector2(0.5f, 0.5f);
        barBgRT.pivot            = new Vector2(0.5f, 0.5f);
        barBgRT.sizeDelta        = new Vector2(340, 26);
        barBgRT.anchoredPosition = new Vector2(0, y);
        barBgGO.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

        var barFillGO = new GameObject("ReactionBarFill", typeof(RectTransform), typeof(Image));
        _reactionBar  = barFillGO.GetComponent<Image>();
        var barFillRT = (RectTransform)barFillGO.transform;
        barFillRT.SetParent(barBgGO.transform, false);
        barFillRT.anchorMin        = Vector2.zero;
        barFillRT.anchorMax        = new Vector2(0.5f, 1f); // 너비 동적 조정
        barFillRT.pivot            = Vector2.zero;
        barFillRT.offsetMin        = Vector2.zero;
        barFillRT.offsetMax        = Vector2.zero;
        _reactionBar.color         = C_ReactionMid;

        _reactionBarTxt = CreateLabel(barBgGO.transform, "BarTxt", "적당한 가격",
            new Rect(-170, -13, 340, 26), 12, FontStyles.Normal,
            Color.white, TextAlignmentOptions.Center);
        y -= 34;

        // ── NPC 반응 설명 ─────────────────────────────────────────────────────
        _reactionTxt = CreateLabel(panelGO.transform, "ReactionTxt",
            "NPC 반응을 예측합니다...",
            new Rect(-180, y, 360, 22), 13, FontStyles.Normal,
            new Color(0.75f, 0.75f, 0.75f), TextAlignmentOptions.Center);
        y -= 30;

        // ── 확정 / 회수 / 닫기 버튼 행 ───────────────────────────────────────
        _confirmBtn  = BuildActionBtn(panelGO.transform, "가격 확정",  C_BtnGreen,  new Vector2(-130, y - 8), OnConfirm);
        _retrieveBtn = BuildActionBtn(panelGO.transform, "아이템 회수", C_BtnOrange, new Vector2(  0,  y - 8), OnRetrieve);
        _closeBtn    = BuildActionBtn(panelGO.transform, "닫기",        C_BtnGray,   new Vector2( 130, y - 8), Close);
    }

    // ── UI 헬퍼 ─────────────────────────────────────────────────────────────────
    void BuildAdjustRow(Transform parent, float y, int[] deltas)
    {
        float totalW = 320f;
        float btnW   = totalW / deltas.Length - 4;
        float startX = -totalW * 0.5f + btnW * 0.5f;

        for (int i = 0; i < deltas.Length; i++)
        {
            int d = deltas[i];
            string label = d > 0 ? $"+{d}" : $"{d}";
            Color col    = d > 0
                ? new Color(0.25f, 0.55f, 0.82f)   // 파랑 계열
                : new Color(0.72f, 0.28f, 0.28f);  // 빨강 계열

            var btn = BuildActionBtn(parent, label, col,
                new Vector2(startX + i * (btnW + 4), y), null);
            btn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 14;
            var rt  = (RectTransform)btn.transform;
            rt.sizeDelta = new Vector2(btnW, 32);

            btn.onClick.AddListener(() => AdjustPrice(d));
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
        rt.sizeDelta        = new Vector2(100, 36);
        rt.anchoredPosition = pos;

        go.GetComponent<Image>().color = bg;

        var txt = new GameObject("Text", typeof(TextMeshProUGUI));
        txt.transform.SetParent(go.transform, false);
        StretchFull((RectTransform)txt.transform);
        var tmp     = txt.GetComponent<TextMeshProUGUI>();
        tmp.text    = label;
        tmp.fontSize = 15;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color   = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        var btn = go.GetComponent<Button>();
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
        tmp.text          = text;
        tmp.fontSize      = size;
        tmp.fontStyle     = style;
        tmp.color         = color;
        tmp.alignment     = align;
        tmp.raycastTarget = false;
        return tmp;
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
    }

    public void Close()
    {
        _slot  = null;
        IsOpen = false;
        SetCanvasActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
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
        Close();
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
            _titleTxt.text = occupied ? "가격 설정" : "진열하기";

        // 아이템 이름
        string itemName = occupied && _slot.currentItem?.data != null
            ? _slot.currentItem.data.itemName : "—";
        if (_itemNameTxt != null) _itemNameTxt.text = itemName;

        // 회수 버튼 활성 (빈 슬롯이면 회수 불필요)
        if (_retrieveBtn != null)
            _retrieveBtn.gameObject.SetActive(occupied);

        // 가격 표시
        if (_priceTxt != null)
            _priceTxt.text = $"{_pendingPrice:N0} G";

        // NPC 반응 계산
        ComputeReaction(out string reaction, out Color barColor, out float barFill);

        if (_reactionBar != null)
        {
            _reactionBar.color = barColor;
            // 바 너비 = anchorMax.x 조정 (0~1)
            var barRT        = (RectTransform)_reactionBar.transform;
            barRT.anchorMax  = new Vector2(barFill, 1f);
            barRT.offsetMax  = Vector2.zero;
        }
        if (_reactionBarTxt != null) _reactionBarTxt.text = reaction;
        if (_reactionTxt    != null)
        {
            int approxPct = Mathf.RoundToInt(barFill * 100);
            _reactionTxt.text = $"예상 구매율: {approxPct}%";
        }
    }

    // 가격 비율 기반 NPC 반응 힌트 (PurchaseEvaluator 의 ratio 로직 단순화 버전)
    void ComputeReaction(out string label, out Color color, out float fill)
    {
        if (_slot == null || _slot.IsEmpty || _slot.currentItem?.data == null)
        {
            label = "아이템을 먼저 진열하세요";
            color = C_ReactionMid;
            fill  = 0f;
            return;
        }

        float basePrice = _slot.currentItem.data.basePrice;
        float ratio     = basePrice > 0f ? (float)_pendingPrice / basePrice : 1f;

        if      (ratio > 3.0f) { label = "😤 너무 비싸요! 아무도 안 사요";   color = C_ReactionBad;  fill = 0.05f; }
        else if (ratio > 2.2f) { label = "😤 많이 비싸요";                   color = C_ReactionBad;  fill = 0.15f; }
        else if (ratio > 1.8f) { label = "😐 좀 비싸네요";                   color = C_ReactionMid;  fill = 0.30f; }
        else if (ratio > 1.4f) { label = "😊 약간 비싸지만 살 수도 있어요";  color = C_ReactionMid;  fill = 0.45f; }
        else if (ratio > 1.0f) { label = "😊 적당한 가격이에요";             color = C_ReactionGood; fill = 0.60f; }
        else if (ratio > 0.8f) { label = "😍 저렴해요! 잘 팔릴 거예요";      color = C_ReactionGood; fill = 0.78f; }
        else                    { label = "😍 너무 저렴해요 — 수익이 줄어요"; color = C_ReactionGood; fill = 0.92f; }
    }

    void SetCanvasActive(bool active)
    {
        if (_canvas != null) _canvas.gameObject.SetActive(active);
    }
}
