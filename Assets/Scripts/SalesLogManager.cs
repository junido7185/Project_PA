using System.Collections.Generic;
using UnityEngine;

// §5 SalesLogManager — 판매 이력 싱글톤.
// ShopSlot.TryPurchaseByNpc 이후 RecordSale() 을 호출해 이력을 누적한다.
// FeedUI.GetRecent(count) 로 최신 N건을 조회한다.
[DefaultExecutionOrder(-40)]
public class SalesLogManager : MonoBehaviour
{
    public struct DailyDecisionStats
    {
        public int purchases;
        public int rejections;

        public int evaluations => purchases + rejections;
        public int purchaseRatePercent => evaluations > 0
            ? Mathf.RoundToInt(purchases * 100f / evaluations)
            : 0;
    }

    public static SalesLogManager Instance { get; private set; }

    [Tooltip("최대 보관 건수. 초과 시 가장 오래된 항목부터 삭제.")]
    public int maxRecords = 100;

    readonly List<SaleRecord> _records = new List<SaleRecord>();
    readonly Dictionary<int, int> _purchasesByDay = new Dictionary<int, int>();
    readonly Dictionary<int, int> _rejectionsByDay = new Dictionary<int, int>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RecordSale(string itemName, string category, int price, float quality,
                           string buyerName, int gameDay, int gameHour)
    {
        _records.Add(new SaleRecord
        {
            itemName  = itemName,
            category  = category,
            price     = price,
            quality   = quality,
            buyerName = buyerName,
            gameDay   = gameDay,
            gameHour  = gameHour,
        });

        while (_records.Count > maxRecords)
            _records.RemoveAt(0);

        int safeDay = Mathf.Max(1, gameDay);
        _purchasesByDay.TryGetValue(safeDay, out int purchases);
        _purchasesByDay[safeDay] = purchases + 1;
        LogDailyStats(safeDay, $"구매: {buyerName} / {itemName}");
    }

    // Task 034 — 구매 판단 결과가 보류일 때 기존 읽기 전용 관찰 훅이 호출한다.
    // 구매는 실제 거래가 성공한 RecordSale만 집계해 구매 의사와 결제 성공을 혼동하지 않는다.
    public void RecordRejection(string itemName, string category, string customerName, int gameDay)
    {
        int safeDay = Mathf.Max(1, gameDay);
        _rejectionsByDay.TryGetValue(safeDay, out int rejections);
        _rejectionsByDay[safeDay] = rejections + 1;
        LogDailyStats(safeDay, $"보류: {customerName} / {itemName} ({category})");
    }

    public DailyDecisionStats GetDailyDecisionStats(int gameDay)
    {
        int safeDay = Mathf.Max(1, gameDay);
        _purchasesByDay.TryGetValue(safeDay, out int purchases);
        _rejectionsByDay.TryGetValue(safeDay, out int rejections);
        return new DailyDecisionStats { purchases = purchases, rejections = rejections };
    }

    public string BuildDailyDecisionSummary(int gameDay)
    {
        DailyDecisionStats stats = GetDailyDecisionStats(gameDay);
        if (stats.evaluations <= 0)
            return "손님 판단 기록 없음 · 진열과 영업 시간을 확인하세요.";

        return $"구매 {stats.purchases}건 · 보류 {stats.rejections}건 · 구매율 {stats.purchaseRatePercent}%";
    }

    public string BuildNextDayAdvice(int gameDay)
    {
        DailyDecisionStats stats = GetDailyDecisionStats(gameDay);
        if (stats.evaluations <= 0)
            return "다음 준비: 오늘은 손님 판단 기록이 없어요. 밤 영업 전에 상품을 진열하고 간판을 여세요.";

        if (stats.rejections >= stats.purchases)
            return "다음 준비: 보류가 많았어요. 가격을 낮추거나 다른 종류의 상품을 섞어보세요.";

        return "다음 준비: 구매가 더 많았어요. 잘 팔린 상품을 보충하고 가공품도 함께 준비하세요.";
    }

    // 최신 N건 (최신순)
    public List<SaleRecord> GetRecent(int count)
    {
        int start = Mathf.Max(0, _records.Count - count);
        var result = new List<SaleRecord>(_records.GetRange(start, _records.Count - start));
        result.Reverse();
        return result;
    }

    void LogDailyStats(int gameDay, string latest)
    {
        DailyDecisionStats stats = GetDailyDecisionStats(gameDay);
        Debug.Log($"📊 [일일 상점 통계] Day {gameDay}: {BuildDailyDecisionSummary(gameDay)} · 최근 {latest}");
    }
}
