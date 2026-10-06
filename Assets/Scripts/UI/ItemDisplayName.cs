// 데모 화면용 한국어 이름. 저장·검색 키(Item.id/itemName)는 그대로 두고 표시 계층에서만 바꾼다.
public static class ItemDisplayName
{
    public static string For(Item item)
    {
        if (item == null) return string.Empty;
        switch (item.id)
        {
            case 1: return "밀";
            case 2: return "당근";
            case 3: return "나무";
            case 4: return "광석";
            case 5: return "빵";
            case 6: return "철괴";
            case 7: return "판재";
            case 8: return "참치";
            case 2001: return "도끼";
            case 2002: return "곡괭이";
            case 2003: return "잠자리채";
            case 2004: return "나비";
            case 2011: return "낚싯대";
            case 2020: return "가판대";
            case 2021: return "의자";
            case 2022: return "개선 곡괭이";
            default: return item.itemName;
        }
    }
}
