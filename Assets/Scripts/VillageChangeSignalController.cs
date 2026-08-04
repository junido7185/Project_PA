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
    public readonly struct NamedTrendSnapshot
    {
        public string trendId { get; }
        public string displayName { get; }
        public int transactions { get; }
        public int revenue { get; }
        public int score { get; }
        public int latestSaleDay { get; }
        public int latestSaleHour { get; }

        public NamedTrendSnapshot(string trendId, string displayName, int transactions, int revenue,
            int score, int latestSaleDay, int latestSaleHour)
        {
            this.trendId = trendId;
            this.displayName = displayName;
            this.transactions = transactions;
            this.revenue = revenue;
            this.score = score;
            this.latestSaleDay = latestSaleDay;
            this.latestSaleHour = latestSaleHour;
        }
    }

    class CategoryStats
    {
        public int count;
        public int revenue;
    }

    class NamedTrendStats
    {
        public string trendId;
        public string displayName;
        public int transactions;
        public int revenue;
        public int latestSaleDay;
        public int latestSaleHour;

        public int score => transactions * 1000 + Mathf.Min(revenue, 999);

        public NamedTrendSnapshot ToSnapshot()
        {
            return new NamedTrendSnapshot(trendId, displayName, transactions, revenue, score,
                latestSaleDay, latestSaleHour);
        }
    }

    public const string FishingTrendId = "trend.fishing";
    public const string CampingTrendId = "trend.camping";
    public const string FurnitureTrendId = "trend.furniture";

    public static VillageChangeSignalController Instance { get; private set; }

    [Header("Village Signal")]
    public bool autoCreateUI = true;
    public int maxRecentSales = 40;
    public int visibleFromDay = 1;

    [Header("UI")]
    public TextMeshProUGUI signalText;

    readonly Dictionary<ItemCategory, CategoryStats> _stats = new Dictionary<ItemCategory, CategoryStats>();
    readonly Dictionary<string, NamedTrendStats> _namedTrendStats = new Dictionary<string, NamedTrendStats>();

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
        RebuildStats();

        if (!TryGetLeadingCategory(out var category, out var stats))
            return "No village change signal yet.";

        string categorySummary = $"{category}: {stats.count} sale(s), {stats.revenue}G influence";
        if (!TryGetLeadingNamedTrend(out var namedTrend))
            return categorySummary;

        return categorySummary + "\n" + BuildNamedTrendLine(namedTrend);
    }

    // Task 093 — Task 047의 정확 매핑을 성공 SaleRecord에만 적용한다.
    // 호출자가 결산/감사 표시를 갱신하지 않았더라도 현재 일차 결과를 받도록 여기서 다시 집계한다.
    public bool TryGetNamedTrendSnapshot(string trendId, out NamedTrendSnapshot snapshot)
    {
        RebuildStats();
        if (!string.IsNullOrEmpty(trendId)
            && _namedTrendStats.TryGetValue(trendId, out var stats)
            && stats != null
            && stats.transactions > 0)
        {
            snapshot = stats.ToSnapshot();
            return true;
        }

        snapshot = default;
        return false;
    }

    public string GetLeadingNamedTrendSummary()
    {
        RebuildStats();
        return TryGetLeadingNamedTrend(out var namedTrend)
            ? BuildNamedTrendLine(namedTrend)
            : "생활 트렌드 · 오늘 확인된 판매 없음";
    }

    // Task 051 — 현재 판매 방향을 기존 티어/감사 화면이 읽을 수 있는
    // 시설 후보 예고로 번역한다. 실제 해금 여부나 티어 조건은 변경하지 않는다.
    public string GetFacilityUnlockPreview()
    {
        RebuildStats();

        if (!TryGetLeadingCategory(out var category, out var stats))
        {
            return "시설 방향 예고\n" +
                   "상품 판매 후 시설 후보 표시\n" +
                   "실제 해금: 티어·감사 조건";
        }

        string categoryName = GetCategoryDisplayName(category);
        string facilityDirection = category switch
        {
            ItemCategory.Raw => "생산자 보관·수거 공간",
            ItemCategory.Processed => "조리·가공 작업대",
            ItemCategory.Utility => "수리·공구 작업대",
            ItemCategory.Luxury => "포장·문화 진열 공간",
            _ => "마을 운영 지원 시설"
        };

        return $"시설 방향 예고 · {categoryName}\n" +
               $"다음 후보: {facilityDirection}\n" +
               $"판매 {stats.count}건 · {stats.revenue}G 신호\n" +
               "실제 해금: 티어·감사 조건";
    }

    void RebuildStats()
    {
        _stats.Clear();
        _namedTrendStats.Clear();

        if (SalesLogManager.Instance == null) return;

        int currentDay = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
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

            // 명명 트렌드는 결산 일차의 성공 거래만 읽는다. 이전 날 판매를 오늘 결과에 섞지 않는다.
            if (record.gameDay != currentDay) continue;
            if (!TryResolveNamedTrend(category, record.itemName, out string trendId, out string displayName))
                continue;

            if (!_namedTrendStats.TryGetValue(trendId, out var namedStats))
            {
                namedStats = new NamedTrendStats
                {
                    trendId = trendId,
                    displayName = displayName,
                    latestSaleDay = record.gameDay,
                    latestSaleHour = record.gameHour
                };
                _namedTrendStats[trendId] = namedStats;
            }

            namedStats.transactions++;
            namedStats.revenue += Mathf.Max(0, record.price);
            if (record.gameDay > namedStats.latestSaleDay
                || (record.gameDay == namedStats.latestSaleDay && record.gameHour > namedStats.latestSaleHour))
            {
                namedStats.latestSaleDay = record.gameDay;
                namedStats.latestSaleHour = record.gameHour;
            }
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
        if (TryGetLeadingNamedTrend(out var namedTrend))
            lines.Add(BuildNamedTrendLine(namedTrend));
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

    bool TryGetLeadingNamedTrend(out NamedTrendStats stats)
    {
        stats = null;
        foreach (var candidate in _namedTrendStats.Values)
        {
            if (candidate == null || candidate.transactions <= 0) continue;
            if (stats == null || IsBetterNamedTrend(candidate, stats))
                stats = candidate;
        }

        return stats != null;
    }

    static bool IsBetterNamedTrend(NamedTrendStats candidate, NamedTrendStats current)
    {
        if (candidate.transactions != current.transactions)
            return candidate.transactions > current.transactions;
        if (candidate.revenue != current.revenue)
            return candidate.revenue > current.revenue;
        if (candidate.latestSaleDay != current.latestSaleDay)
            return candidate.latestSaleDay > current.latestSaleDay;
        if (candidate.latestSaleHour != current.latestSaleHour)
            return candidate.latestSaleHour > current.latestSaleHour;

        return string.CompareOrdinal(candidate.trendId, current.trendId) < 0;
    }

    static bool TryResolveNamedTrend(ItemCategory category, string itemName,
        out string trendId, out string displayName)
    {
        // SaleRecord에는 아직 Item.id가 없으므로 Task 047에서 확정한 (category, itemName)
        // 정확 쌍만 fail-closed로 허용한다. 부분 검색이나 프리팹/설명 이름 추론은 금지한다.
        if ((category == ItemCategory.Raw && string.Equals(itemName, "Fish", StringComparison.Ordinal))
            || (category == ItemCategory.Processed && string.Equals(itemName, "생선구이", StringComparison.Ordinal)))
        {
            trendId = FishingTrendId;
            displayName = "낚시 생활";
            return true;
        }

        if (category == ItemCategory.Luxury && string.Equals(itemName, "목제 가구", StringComparison.Ordinal))
        {
            trendId = FurnitureTrendId;
            displayName = "가구 문화";
            return true;
        }

        trendId = null;
        displayName = null;
        return false;
    }

    static string BuildNamedTrendLine(NamedTrendStats stats)
    {
        return $"생활 트렌드 · {stats.displayName}: 판매 {stats.transactions}건 / {stats.revenue}G / {stats.score}점";
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

    static string GetCategoryDisplayName(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Raw => "원자재",
            ItemCategory.Processed => "가공품",
            ItemCategory.Utility => "실용품",
            ItemCategory.Luxury => "고급품",
            _ => "판매 흐름"
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
        rt.sizeDelta = new Vector2(470f, 112f);

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
