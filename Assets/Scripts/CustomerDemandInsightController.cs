using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Read-only customer demand insight layer.
//
// This observes PurchaseEvaluator results and recent customer reactions without
// changing purchase probability, NPC behavior, prices, or sale logic.
public class CustomerDemandInsightController : MonoBehaviour
{
    class DemandStats
    {
        public int evaluations;
        public int buys;
        public float probabilityTotal;
    }

    class DemandSignal
    {
        public string itemName;
        public ItemCategory category;
        public string customerName;
        public bool bought;
        public float probability;
        public int displayPrice;
    }

    public static CustomerDemandInsightController Instance { get; private set; }

    [Header("Demand Insight")]
    public bool autoCreateUI = true;
    public int visibleFromDay = 3;
    public int maxRecentSignals = 12;

    [Header("UI")]
    public TextMeshProUGUI insightText;

    readonly Dictionary<ItemCategory, DemandStats> _statsByCategory = new Dictionary<ItemCategory, DemandStats>();
    readonly List<DemandSignal> _recentSignals = new List<DemandSignal>();

    Canvas _canvas;
    GameObject _panel;
    string _lastInsightText = string.Empty;
    float _nextRefreshAt;

    public string CurrentInsightText => insightText != null ? insightText.text : _lastInsightText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (autoCreateUI && insightText == null)
            BuildUI();
    }

    void Start()
    {
        RefreshNow();
    }

    void Update()
    {
        if (Time.unscaledTime < _nextRefreshAt) return;
        _nextRefreshAt = Time.unscaledTime + 1.0f;
        RefreshNow();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RecordEvaluation(PurchaseEvaluator.Result result, Item item, string customerName, int displayPrice)
    {
        if (item == null || item.category == ItemCategory.Tool) return;

        // Task 034 — 구매는 ShopSlot.RecordSale의 실제 결제 성공만 집계하고,
        // 여기서는 기존 평가 관찰 훅을 이용해 보류 판단만 일일 통계에 전달한다.
        if (!result.willBuy && SalesLogManager.Instance != null)
        {
            int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
            SalesLogManager.Instance.RecordRejection(
                string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName,
                item.category.ToString(),
                string.IsNullOrEmpty(customerName) ? "손님" : customerName,
                day);
        }

        if (!_statsByCategory.TryGetValue(item.category, out var stats))
        {
            stats = new DemandStats();
            _statsByCategory[item.category] = stats;
        }

        stats.evaluations++;
        if (result.willBuy) stats.buys++;
        stats.probabilityTotal += Mathf.Clamp01(result.probability);

        _recentSignals.Add(new DemandSignal
        {
            itemName = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName,
            category = item.category,
            customerName = string.IsNullOrEmpty(customerName) ? "Customer" : customerName,
            bought = result.willBuy,
            probability = Mathf.Clamp01(result.probability),
            displayPrice = Mathf.Max(0, displayPrice)
        });

        while (_recentSignals.Count > maxRecentSignals)
            _recentSignals.RemoveAt(0);

        RefreshNow();
    }

    public string GetTopCategorySummary()
    {
        ItemCategory bestCategory = ItemCategory.Raw;
        DemandStats bestStats = null;
        float bestScore = float.NegativeInfinity;

        foreach (var kv in _statsByCategory)
        {
            var stats = kv.Value;
            if (stats == null || stats.evaluations <= 0) continue;

            float buyRate = stats.buys / (float)stats.evaluations;
            float avgProbability = stats.probabilityTotal / stats.evaluations;
            float score = buyRate * 0.7f + avgProbability * 0.3f;
            if (score > bestScore)
            {
                bestScore = score;
                bestCategory = kv.Key;
                bestStats = stats;
            }
        }

        if (bestStats == null)
            return "No demand signal yet.";

        float rate = bestStats.buys / (float)bestStats.evaluations;
        float avg = bestStats.probabilityTotal / bestStats.evaluations;
        return $"{bestCategory}: {bestStats.buys}/{bestStats.evaluations} bought, avg interest {avg:P0}, buy rate {rate:P0}";
    }

    public void RefreshNow()
    {
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        bool shouldShow = day >= visibleFromDay;
        if (_panel != null && _panel.activeSelf != shouldShow)
            _panel.SetActive(shouldShow);

        _lastInsightText = BuildInsightText(day);
        if (insightText != null)
            insightText.text = _lastInsightText;
    }

    string BuildInsightText(int day)
    {
        if (day < visibleFromDay)
            return "Demand Signals unlock on Day 3.";

        var lines = new List<string> { "Demand Signals" };
        lines.Add(GetTopCategorySummary());

        if (_recentSignals.Count > 0)
        {
            var latest = _recentSignals[_recentSignals.Count - 1];
            string result = latest.bought ? "buy" : "pass";
            lines.Add($"Latest: {latest.itemName} {result} ({latest.probability:P0}) at {latest.displayPrice}G");
        }
        else
        {
            lines.Add("Stock an item and watch customer reactions.");
        }

        return string.Join("\n", lines);
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("CustomerDemandInsightCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 52;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _panel = new GameObject("CustomerDemandInsightPanel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)_panel.transform;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -178f);
        rt.sizeDelta = new Vector2(470f, 92f);

        var bg = _panel.GetComponent<Image>();
        bg.color = new Color(0.04f, 0.055f, 0.06f, 0.58f);
        bg.raycastTarget = false;

        var textGo = new GameObject("CustomerDemandInsightText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(_panel.transform, false);
        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(14f, 8f);
        textRt.offsetMax = new Vector2(-14f, -8f);

        insightText = textGo.GetComponent<TextMeshProUGUI>();
        insightText.fontSize = 14f;
        insightText.fontStyle = FontStyles.Bold;
        insightText.alignment = TextAlignmentOptions.TopLeft;
        insightText.textWrappingMode = TextWrappingModes.Normal;
        insightText.overflowMode = TextOverflowModes.Ellipsis;
        insightText.color = new Color(0.82f, 0.94f, 1f, 1f);
        insightText.raycastTarget = false;

        _panel.SetActive(false);
    }
}
