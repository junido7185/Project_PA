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
            case 4: return "구리 광석"; // 2026-11-02 도구 단계(TOOL_PROGRESSION.md): 일반 바위의 광석
            case 5: return "빵";
            case 6: return "철 주괴";
            case 7: return "판재";
            case 8: return "참치";
            case 1001: return "가판대 키트";
            case 2001: return "도끼";
            case 2002: return "곡괭이";
            case 2003: return "잠자리채";
            case 2004: return "나비";
            case 2005: return "관리 허브 키트";
            case 2011: return "낚싯대";
            case 2020: return "가판대";
            case 2021: return "의자";
            case 2022: return "구리 곡괭이";
            case 2028: return "철 광석";
            case 2029: return "구리 주괴";
            case 2030: return "구리 도끼";
            case 2031: return "철 곡괭이";
            default: return item.itemName;
        }
    }
}
