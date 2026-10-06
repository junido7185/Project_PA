using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// §5 FeedUI — SmartphoneUI 피드 탭(index 2) 에 붙이는 컴포넌트.
// OnEnable 때마다 SalesLogManager 최신 10건을 ScrollRect 에 표시한다.
public class FeedUI : MonoBehaviour
{
    [Header("설정")]
    public int displayCount = 10;

    [Header("직접 연결 (선택)")]
    public ScrollRect scrollRect;
    public Transform  cardParent;

    readonly List<GameObject> _cards = new List<GameObject>();
    bool _built;
    bool _subscribed;

    public int VisibleSaleCardCount { get; private set; }
    public bool HasVisibleEmptyState { get; private set; }
    public string CurrentVillageSummary { get; private set; } = string.Empty;
    static bool OpeningDemo => DemoSettlementController.Instance != null;

    void OnEnable()
    {
        if (!_built)
        {
            if (scrollRect == null) BuildScrollView();
            _built = true;
        }
        Subscribe();
        Refresh();
    }

    void OnDisable()
    {
        Unsubscribe();
        ClearCards();
    }

    void OnDestroy() => Unsubscribe();

    public void Refresh()
    {
        ClearCards();
        Transform parent = cardParent != null ? cardParent
                         : scrollRect != null ? scrollRect.content
                         : transform;

        // Canon v2 §22: 놓친 알림은 휴대폰에서 다시 본다(데모 경로에서 알림이 있을 때만).
        if (FirstDayWorldPresentation.DispatchLog.Count > 0)
            _cards.Add(BuildDispatchCard(parent));

        if (SalesLogManager.Instance == null)
        {
            BuildEmptyState(parent, "판매 기록 서비스를 불러오는 중입니다.");
            RebuildLayout();
            return;
        }

        VillageChangeSignalController.Instance?.RefreshNow();
        VillageCultureVisualController.Instance?.RefreshNow();
        CurrentVillageSummary = ResolveVillageSummary();

        List<SaleRecord> records = SalesLogManager.Instance.GetRecent(displayCount);
        if (records.Count == 0)
        {
            BuildEmptyState(parent, OpeningDemo
                ? "판매 기록은 밤 영업 뒤 여기에 쌓여요."
                : "아직 판매 기록이 없습니다.\n밤에 상품을 판매하면 주민 반응과 마을 방향이 여기에 쌓입니다.");
            RebuildLayout();
            return;
        }

        foreach (var r in records)
            _cards.Add(BuildCard(parent, r));

        VisibleSaleCardCount = records.Count;
        RebuildLayout();
    }

    void ClearCards()
    {
        foreach (var go in _cards)
            if (go != null)
            {
                go.SetActive(false);
                Destroy(go);
            }
        _cards.Clear();
        VisibleSaleCardCount = 0;
        HasVisibleEmptyState = false;
    }

    GameObject BuildDispatchCard(Transform parent)
    {
        var lines = FirstDayWorldPresentation.DispatchLog;
        int shown = Mathf.Min(5, lines.Count);
        var card = new GameObject("DispatchCard", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var rt = (RectTransform)card.transform;
        rt.SetParent(parent, false);
        var image = card.GetComponent<Image>();
        image.sprite = PAUiTheme.RoundedSprite; image.type = Image.Type.Sliced;
        image.color = new Color(.91f, .95f, .88f, 1f);
        // 제목과 기록을 줄마다 따로 두고, 각 안내는 실제 너비에서 필요한 줄 수만큼 높이를 준다.
        // 한 줄 말줄임(…)으로는 놓친 목표를 다시 읽을 수 없다.
        float width = ((RectTransform)parent).rect.width;
        width = width > 100f ? width - 44f : 260f; // 카드 좌우 여백·레이아웃 패딩을 넉넉히 뺀 안쪽 폭
        float top = 8f + AddDispatchLine(card.transform, "<b><color=#1F6365>P.A. 안내 · 최근</color></b>", 17, 8f, width, false) + 6f;
        for (int i = 0; i < shown; i++) top += AddDispatchLine(card.transform, lines[i], 15, top, width, true) + 4f;
        float height = top + 6f;
        rt.sizeDelta = new Vector2(0f, height);
        card.GetComponent<LayoutElement>().preferredHeight = height;
        return card;
    }

    static float AddDispatchLine(Transform card, string text, float size, float top, float width, bool wrapWords)
    {
        var line = new GameObject("DispatchLine", typeof(TextMeshProUGUI));
        var rt = (RectTransform)line.transform;
        rt.SetParent(card, false);
        var tmp = line.GetComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = PAUiTheme.Ink;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.textWrappingMode = TextWrappingModes.Normal; tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        // 한글은 TMP가 글자 단위로 끊는다("빈/손"). 토스트와 같은 띄어쓰기 단위 줄바꿈을 쓴다.
        if (wrapWords) { text = FirstDayWorldPresentation.WrapWords(tmp, text, width); tmp.text = text; }
        float lineHeight = Mathf.Clamp(Mathf.Ceil(tmp.GetPreferredValues(text, width, 0f).y) + 2f, size + 6f, size * 5f);
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(.5f, 1f);
        rt.offsetMin = new Vector2(12f, -top - lineHeight); rt.offsetMax = new Vector2(-12f, -top);
        return lineHeight;
    }

    GameObject BuildCard(Transform parent, SaleRecord r)
    {
        var card = new GameObject("FeedCard", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var rt   = (RectTransform)card.transform;
        rt.SetParent(parent, false);
        bool demo = OpeningDemo;
        float cardHeight = demo ? 74f : 112f;
        rt.sizeDelta = new Vector2(0f, cardHeight);
        card.GetComponent<LayoutElement>().preferredHeight = cardHeight;
        var cardImage = card.GetComponent<Image>();
        cardImage.color = demo ? new Color(.95f, .96f, .91f, 1f) : new Color(0.12f, 0.12f, 0.18f, 0.9f);
        if (demo) { cardImage.sprite = PAUiTheme.RoundedSprite; cardImage.type = Image.Type.Sliced; }

        // 아이템명 + 가격
        var topGO  = new GameObject("TopLine", typeof(TextMeshProUGUI));
        var topRT  = (RectTransform)topGO.transform;
        topRT.SetParent(card.transform, false);
        topRT.anchorMin        = new Vector2(0f, 0.67f);
        topRT.anchorMax        = new Vector2(1f, 1f);
        topRT.offsetMin        = new Vector2(10f, 2f);
        topRT.offsetMax        = new Vector2(-10f, -2f);
        var topTmp             = topGO.GetComponent<TextMeshProUGUI>();
        topTmp.text            = $"{r.itemName}  {r.price:N0} G";
        topTmp.fontSize        = 17;
        topTmp.fontStyle       = FontStyles.Bold;
        topTmp.color           = demo ? PAUiTheme.Ink : Color.white;
        topTmp.raycastTarget   = false;

        // 구매자 + 날짜 + 실제 기록 메타
        var botGO  = new GameObject("BotLine", typeof(TextMeshProUGUI));
        var botRT  = (RectTransform)botGO.transform;
        botRT.SetParent(card.transform, false);
        botRT.anchorMin        = new Vector2(0f, 0f);
        botRT.anchorMax        = new Vector2(1f, 0.67f);
        botRT.offsetMin        = new Vector2(10f, 2f);
        botRT.offsetMax        = new Vector2(-10f, -2f);
        var botTmp             = botGO.GetComponent<TextMeshProUGUI>();
        botTmp.text            = demo
            ? $"{r.buyerName}  ·  {r.gameHour:00}:00  ·  {ResolveCategoryLabel(r.category)}"
            : $"{r.buyerName}  ·  Day {r.gameDay} {r.gameHour:00}:00\n" +
              $"{ResolveCategoryLabel(r.category)} · 품질 {r.quality:0.00}";
        botTmp.fontSize        = 14;
        botTmp.color           = demo ? new Color(.34f, .43f, .40f) : new Color(0.7f, 0.7f, 0.7f);
        botTmp.raycastTarget   = false;
        botTmp.textWrappingMode = TextWrappingModes.Normal;

        // 데모 첫날에는 다음 날 마을 변화 집계(영문 내부 요약)를 보여 주지 않는다.
        if (demo) return card;
        var villageGO = new GameObject("VillageLine", typeof(TextMeshProUGUI));
        var villageRT = (RectTransform)villageGO.transform;
        villageRT.SetParent(card.transform, false);
        villageRT.anchorMin = new Vector2(0f, 0f);
        villageRT.anchorMax = new Vector2(1f, 0.27f);
        villageRT.offsetMin = new Vector2(10f, 2f);
        villageRT.offsetMax = new Vector2(-10f, -2f);
        var villageTmp = villageGO.GetComponent<TextMeshProUGUI>();
        villageTmp.text = $"마을 방향 · {CurrentVillageSummary}";
        villageTmp.fontSize = 12f;
        villageTmp.color = new Color(0.62f, 0.9f, 0.68f);
        villageTmp.raycastTarget = false;
        villageTmp.overflowMode = TextOverflowModes.Ellipsis;

        return card;
    }

    void BuildScrollView()
    {
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var vpRT     = (RectTransform)viewport.transform;
        vpRT.SetParent(transform, false);
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;
        // Transparent Mask images cull all descendants because alpha participates
        // in the stencil. Keep the graphic opaque while hiding the mask artwork.
        viewport.GetComponent<Image>().color = Color.white;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform));
        var cRT     = (RectTransform)content.transform;
        cRT.SetParent(viewport.transform, false);
        cRT.anchorMin = new Vector2(0f, 1f);
        cRT.anchorMax = new Vector2(1f, 1f);
        cRT.pivot     = new Vector2(0.5f, 1f);
        cRT.sizeDelta = new Vector2(0f, 0f);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing   = 6f;
        vlg.padding   = new RectOffset(6, 6, 6, 6);

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect          = gameObject.GetComponent<ScrollRect>() ?? gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = vpRT;
        scrollRect.content  = cRT;
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;
        scrollRect.scrollSensitivity = 34f; // 휠 한 칸에 실제로 내려가게(CraftingUI와 같은 값)

        cardParent = cRT;
    }

    void Subscribe()
    {
        if (_subscribed) return;
        SalesLogManager.OnSaleRecorded += OnSaleRecorded;
        SalesLogManager.OnHistoryRestored += OnHistoryRestored;
        _subscribed = true;
    }

    void Unsubscribe()
    {
        if (!_subscribed) return;
        SalesLogManager.OnSaleRecorded -= OnSaleRecorded;
        SalesLogManager.OnHistoryRestored -= OnHistoryRestored;
        _subscribed = false;
    }

    void OnSaleRecorded(SaleRecord _) => Refresh();
    void OnHistoryRestored() => Refresh();

    void BuildEmptyState(Transform parent, string message)
    {
        if (parent == null) return;
        var card = new GameObject("FeedEmptyState", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        card.transform.SetParent(parent, false);
        bool demo = OpeningDemo;
        var cardImage = card.GetComponent<Image>();
        cardImage.color = demo ? new Color(.95f, .96f, .91f, 1f) : new Color(0.14f, 0.15f, 0.2f, 0.94f);
        if (demo) { cardImage.sprite = PAUiTheme.RoundedSprite; cardImage.type = Image.Type.Sliced; }
        card.GetComponent<LayoutElement>().preferredHeight = demo ? 64f : 132f;

        var textGO = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        var textRect = (RectTransform)textGO.transform;
        textRect.SetParent(card.transform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 12f);
        textRect.offsetMax = new Vector2(-16f, -12f);
        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = message;
        text.fontSize = 15f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = demo ? new Color(.34f, .43f, .40f) : new Color(0.82f, 0.85f, 0.9f);
        text.textWrappingMode = TextWrappingModes.Normal;

        _cards.Add(card);
        HasVisibleEmptyState = true;
    }

    string ResolveVillageSummary()
    {
        VillageChangeSignalController signal = VillageChangeSignalController.Instance;
        if (signal == null) return "판매 신호 집계 중";

        string summary = signal.GetLeadingSignalSummary();
        if (string.IsNullOrWhiteSpace(summary)) return "판매 신호 집계 중";
        return summary.Replace("\r", " ").Replace("\n", " · ");
    }

    static string ResolveCategoryLabel(string raw)
    {
        if (!System.Enum.TryParse(raw, true, out ItemCategory category))
            return string.IsNullOrWhiteSpace(raw) ? "기타" : raw;

        return category switch
        {
            ItemCategory.Raw => "원재료",
            ItemCategory.Processed => "가공품",
            ItemCategory.Utility => "생활 도구",
            ItemCategory.Luxury => "고급품",
            ItemCategory.Tool => "도구",
            _ => category.ToString()
        };
    }

    void RebuildLayout()
    {
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null && scrollRect.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
    }
}
