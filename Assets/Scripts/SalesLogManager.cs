using System.Collections.Generic;
using UnityEngine;

// §5 SalesLogManager — 판매 이력 싱글톤.
// ShopSlot.TryPurchaseByNpc 이후 RecordSale() 을 호출해 이력을 누적한다.
// FeedUI.GetRecent(count) 로 최신 N건을 조회한다.
[DefaultExecutionOrder(-40)]
public class SalesLogManager : MonoBehaviour
{
    public static SalesLogManager Instance { get; private set; }

    [Tooltip("최대 보관 건수. 초과 시 가장 오래된 항목부터 삭제.")]
    public int maxRecords = 100;

    readonly List<SaleRecord> _records = new List<SaleRecord>();

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
    }

    // 최신 N건 (최신순)
    public List<SaleRecord> GetRecent(int count)
    {
        int start = Mathf.Max(0, _records.Count - count);
        var result = new List<SaleRecord>(_records.GetRange(start, _records.Count - start));
        result.Reverse();
        return result;
    }
}
