using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// SPY-002 — 고객 타입 / 성향 프레젠테이션 (B. 구매/거절 이유 + D. 마을 정체성 연결).
//
// 설계 의도 (PROJECT_PA_CREATIVE_NORTH_STAR.md "Village-Change Principle"):
// - 손님이 왜 샀는지/왜 거절했는지를 짧고 귀여운 생활형 톤으로 보여준다.
// - 단순 "판매 성공/실패 알림"이 아니라, 그 거래가 마을 경제·주민 생활로 이어진다는 점을 한 줄 덧붙인다.
// - 구매 확률, 경제 계산, PurchaseEvaluator 로직은 절대 바꾸지 않는 읽기 전용 사이드카다.
//
// 데이터 출처 (실제 PurchaseEvaluator.Result + ShopSlot 진열 데이터만 사용):
// - willBuy / probability : PurchaseEvaluator 결과 그대로.
// - displayPrice / basePrice 비율 : 거절·구매 이유 분기(가격/선호)에 사용. NpcController.BuildPurchaseFeedback 과 동일 기준.
// - item.category : 마을 변화 연결 문구(VillageChangeSignalController 의 카테고리 의미와 일관).
//
// 디버그 수치/확률값/계산식은 화면에 그대로 노출하지 않는다(SPY-002 §5-B 톤 규칙).
public class PurchaseFeedbackPresentationController : MonoBehaviour
{
    class FeedbackEntry
    {
        public string reaction;     // "이름: 귀여운 이유"
        public string villageTie;   // "마을 변화: ..." (최신 항목에만 표시)
    }

    public static PurchaseFeedbackPresentationController Instance { get; private set; }

    [Header("Feedback Feed")]
    public bool autoCreateUI = true;
    public int maxVisibleEntries = 3;

    [Header("UI")]
    public TextMeshProUGUI feedbackText;

    readonly List<FeedbackEntry> _entries = new List<FeedbackEntry>(); // index 0 = 최신
    Canvas _canvas;
    GameObject _panel;
    string _lastFeedbackText = "손님 반응\n첫 손님을 기다리는 중...";
    string _lastReactionLine = string.Empty;
    string _lastVillageTie = string.Empty;

    public string CurrentFeedbackText => feedbackText != null ? feedbackText.text : _lastFeedbackText;
    public string LastReactionLine => _lastReactionLine;
    public string LastVillageTie => _lastVillageTie;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (autoCreateUI && feedbackText == null)
            BuildUI();
    }

    void Start()
    {
        RefreshUI();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // NpcController 가 구매 평가 직후 호출하는 읽기 전용 훅.
    // 결과/판매/돈/FSM 에 영향을 주지 않고, 플레이어용 설명 한 줄만 만든다.
    public void RecordDecision(NpcProfile profile, PurchaseEvaluator.Result result, Item item, int displayPrice, string customerName)
    {
        string name = string.IsNullOrEmpty(customerName) ? "손님" : customerName;
        string reaction = BuildReactionLine(name, result, item, displayPrice);
        string villageTie = BuildVillageTie(result.willBuy, item);

        _lastReactionLine = reaction;
        _lastVillageTie = villageTie;

        _entries.Insert(0, new FeedbackEntry { reaction = reaction, villageTie = villageTie });
        int cap = Mathf.Max(1, maxVisibleEntries);
        while (_entries.Count > cap)
            _entries.RemoveAt(_entries.Count - 1);

        RefreshUI();
    }

    static string BuildReactionLine(string name, PurchaseEvaluator.Result result, Item item, int displayPrice)
    {
        if (item == null)
            return $"{name}: 진열대를 살펴봐요.";

        int basePrice = Mathf.Max(1, item.basePrice);
        float ratio = Mathf.Max(1, displayPrice) / (float)basePrice;
        string categoryWord = CategoryWord(item.category);

        if (result.willBuy)
        {
            if (ratio <= 0.85f)
                return $"{name}: 값이 착해서 바로 샀어요!";
            if (ratio <= 1.15f)
                return $"{name}: 적당한 값이라 기분 좋게 구매!";
            return $"{name}: 찾던 {categoryWord}라 망설임 없이 구매!";
        }

        if (ratio >= 1.3f)
            return $"{name}: 가격이 조금 부담돼 다음에 올게요.";
        if (result.probability < 0.35f)
            return $"{name}: 지금 찾는 물건은 아니네요.";
        return $"{name}: 한참 고민하다 살며시 내려놨어요.";
    }

    // 거래를 마을 변화로 연결 — 단순 알림을 넘어 "무엇이 팔리면 마을이 어떻게 바뀌는가"를 보여준다.
    static string BuildVillageTie(bool bought, Item item)
    {
        if (!bought)
            return "마을 변화: 진열·가격을 손보면 손님 마음이 열려요.";

        ItemCategory category = item != null ? item.category : ItemCategory.Raw;
        return category switch
        {
            ItemCategory.Processed => "마을 변화: 가공품이 팔리며 마을 식문화가 깨어나요.",
            ItemCategory.Luxury    => "마을 변화: 고급품 인기가 마을의 멋과 평판을 키워요.",
            ItemCategory.Utility   => "마을 변화: 실용품 수요가 마을 살림을 단단하게 해요.",
            ItemCategory.Raw       => "마을 변화: 원자재 거래가 생산자들을 들썩이게 해요.",
            _                      => "마을 변화: 오늘의 거래가 마을 경제에 스며들어요."
        };
    }

    static string CategoryWord(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Raw => "재료",
            ItemCategory.Processed => "가공품",
            ItemCategory.Utility => "생활용품",
            ItemCategory.Luxury => "귀한 물건",
            ItemCategory.Tool => "도구",
            _ => "물건"
        };
    }

    void RefreshUI()
    {
        _lastFeedbackText = BuildFeedbackText();
        if (feedbackText != null)
            feedbackText.text = _lastFeedbackText;
    }

    string BuildFeedbackText()
    {
        var lines = new List<string> { "손님 반응" };

        if (_entries.Count == 0)
        {
            lines.Add("첫 손님을 기다리는 중...");
            return string.Join("\n", lines);
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry == null) continue;

            lines.Add(entry.reaction);
            // 최신(맨 위) 항목에만 마을 변화 연결을 붙여 패널을 차분하게 유지한다.
            if (i == 0 && !string.IsNullOrEmpty(entry.villageTie))
                lines.Add(entry.villageTie);
        }

        return string.Join("\n", lines);
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("PurchaseFeedbackCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 56;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 하단 우측 — 우상단 인사이트 스택(Money/Demand/Village/Preference)과 핫바(하단 중앙)를 모두 피한다.
        // 핫바(하단 중앙)의 오른쪽 끝이 1920x1080 에서 ref y≈120 까지 올라오므로,
        // 패널 하단(anchoredPosition.y)을 170 으로 올려 핫바 위로 분리한다.
        _panel = new GameObject("PurchaseFeedbackPanel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)_panel.transform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-20f, 170f);
        rt.sizeDelta = new Vector2(480f, 150f);

        var bg = _panel.GetComponent<Image>();
        bg.color = new Color(0.06f, 0.05f, 0.05f, 0.6f);
        bg.raycastTarget = false;

        var textGo = new GameObject("PurchaseFeedbackText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(_panel.transform, false);
        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(14f, 10f);
        textRt.offsetMax = new Vector2(-14f, -10f);

        feedbackText = textGo.GetComponent<TextMeshProUGUI>();
        feedbackText.fontSize = 14f;
        feedbackText.fontStyle = FontStyles.Bold;
        feedbackText.alignment = TextAlignmentOptions.TopLeft;
        feedbackText.textWrappingMode = TextWrappingModes.Normal;
        feedbackText.overflowMode = TextOverflowModes.Ellipsis;
        feedbackText.color = new Color(1f, 0.93f, 0.86f, 1f);
        feedbackText.raycastTarget = false;
    }
}
