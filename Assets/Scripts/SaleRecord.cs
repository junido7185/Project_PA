// §5 SaleRecord — 단일 판매 이력 DTO.
// SalesLogManager 가 보관하며 FeedUI 가 목록으로 표시한다.
[System.Serializable]
public class SaleRecord
{
    public string itemName;
    public string category;
    public int    price;
    public float  quality;
    public string buyerName;
    public int    gameDay;
    public int    gameHour;
}
