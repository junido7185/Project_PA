using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Read-only village-change signal for Milestone 1.
// It observes recent sales by category and explains what direction the village
// economy appears to be leaning toward, without changing money, demand, tier,
// NPC behavior, saves, or shop rules.
public class VillageChangeSignalController : MonoBehaviour
{
    class CategoryStats
    {
        public int count;
        public int revenue;
    }

    public static VillageChangeSignalController Instance { get; private set; }

    [Header("Village Signal")]
    public bool autoCreateUI = true;
    public int maxRecentSales = 40;
    public int visibleFromDay = 1;

    [Header("UI")]
    public TextMeshProUGUI signalText;

    readonly Dictionary<ItemCategory, CategoryStats> _stats = new Dictionary<ItemCategory, CategoryStats>();

    Canvas _canvas;
    GameObject _panel;
    string _lastSignalText = "Village Direction\nNo sales signal yet.";
    float _nextRefreshAt;

    public string CurrentSignalText => signalText != null ? signalText.text : _lastSignalText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (autoCreateUI && signalText == null)
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

    public void RefreshNow()
    {
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        bool shouldShow = day >= visibleFromDay;
        if (_panel != null && _panel.activeSelf != shouldShow)
            _panel.SetActive(shouldShow);

        RebuildStats();
        _lastSignalText = BuildSignalText();

        if (signalText != null)
            signalText.text = _lastSignalText;
    }

    public string GetLeadingSignalSummary()
    {
        if (!TryGetLeadingCategory(out var category, out var stats))
            return "No village change signal yet.";

        return $"{category}: {stats.count} sale(s), {stats.revenue}G influence";
    }

    void RebuildStats()
    {
        _stats.Clear();

        if (SalesLogManager.Instance == null) return;

        List<SaleRecord> records = SalesLogManager.Instance.GetRecent(Mathf.Max(1, maxRecentSales));
        foreach (var record in records)
        {
            if (record == null) continue;
            if (!Enum.TryParse(record.category, true, out ItemCategory category)) continue;
            if (category == ItemCategory.Tool) continue;

            if (!_stats.TryGetValue(category, out var stats))
            {
                stats = new CategoryStats();
                _stats[category] = stats;
            }

            stats.count++;
            stats.revenue += Mathf.Max(0, record.price);
        }
    }

    string BuildSignalText()
    {
        var lines = new List<string> { "Village Direction" };

        if (!TryGetLeadingCategory(out var category, out var stats))
        {
            lines.Add("Sell products to reveal what the village is becoming.");
            lines.Add("Category impact stays advisory in this first pass.");
            return string.Join("\n", lines);
        }

        lines.Add(GetCategoryMeaning(category));
        lines.Add($"{category}: {stats.count} sale(s), {stats.revenue}G signal");
        return string.Join("\n", lines);
    }

    bool TryGetLeadingCategory(out ItemCategory category, out CategoryStats stats)
    {
        category = ItemCategory.Raw;
        stats = null;

        float bestScore = float.NegativeInfinity;
        foreach (var kv in _stats)
        {
            var candidate = kv.Value;
            if (candidate == null || candidate.count <= 0) continue;

            float score = candidate.count * 1000f + candidate.revenue;
            if (score <= bestScore) continue;

            bestScore = score;
            category = kv.Key;
            stats = candidate;
        }

        return stats != null;
    }

    static string GetCategoryMeaning(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Raw => "Raw goods point toward producer demand.",
            ItemCategory.Processed => "Processed goods point toward food/workshop growth.",
            ItemCategory.Utility => "Utility goods point toward practical village upgrades.",
            ItemCategory.Luxury => "Luxury goods point toward culture and reputation.",
            _ => "Sales are shaping the village economy."
        };
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("VillageChangeSignalCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 51;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _panel = new GameObject("VillageChangeSignalPanel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)_panel.transform;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -286f);
        rt.sizeDelta = new Vector2(470f, 92f);

        var bg = _panel.GetComponent<Image>();
        bg.color = new Color(0.055f, 0.045f, 0.035f, 0.58f);
        bg.raycastTarget = false;

        var textGo = new GameObject("VillageChangeSignalText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(_panel.transform, false);
        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(14f, 8f);
        textRt.offsetMax = new Vector2(-14f, -8f);

        signalText = textGo.GetComponent<TextMeshProUGUI>();
        signalText.fontSize = 14f;
        signalText.fontStyle = FontStyles.Bold;
        signalText.alignment = TextAlignmentOptions.TopLeft;
        signalText.textWrappingMode = TextWrappingModes.Normal;
        signalText.overflowMode = TextOverflowModes.Ellipsis;
        signalText.color = new Color(1f, 0.88f, 0.66f, 1f);
        signalText.raycastTarget = false;
    }
}
