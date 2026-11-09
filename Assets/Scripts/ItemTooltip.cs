using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltip : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Image icon;
    public RectTransform rectTransform;
    public Vector2 offset = new Vector2(10, -10);

    private Canvas rootCanvas;

    void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        Hide();
    }

    static readonly int[] DecorOnlyIds = { 2043, 2048, 2050, 2051, 2053, 2057, 2060, 2061 };

    public void Show(Item item, Vector2 screenPos)
    {
        nameText.text = PAUiTheme.Active ? ItemDisplayName.For(item) : item.itemName;
        // 2026-11-04 꾸밈 테마 세트(ACNH 소품 연구 H2): 세트 조각이면 세트 이름·짝을 한 줄 더(Teal).
        string setLine = PAUiTheme.Active ? DecorThemeSets.TooltipLine(item) : null;
        // decorset-r154 툴팁: 배치 꾸밈(조개 장식·유리 모빌·모자이크·기념패·광물 받침)은 0G라 "기본 가격 0 G"가 어색했다 → 꾸밈 전용 안내(도구 등은 그대로).
        string priceLine = item.basePrice == 0 && System.Array.IndexOf(DecorOnlyIds, item.id) >= 0 ? "꾸밈 전용 · 가게에 두는 물건이에요" : "기본 가격  " + item.basePrice + " G";
        descriptionText.text = PAUiTheme.Active ? DemoDescription(item) + (setLine != null ? "\n<color=#2E7D74>" + setLine + "</color>" : "") + "\n" + priceLine : item.description;
        if (PAUiTheme.Active) PAUiTheme.Tooltip(this);
        icon.sprite = item.icon;
        gameObject.SetActive(true);
        UpdatePosition(screenPos);
    }

    // 첫날 조작에서는 상호작용 키가 E이므로 데이터에 남은 Space 안내를 표시 계층에서만 바꾼다.
    // 첫날 도구·키트는 가방에서 볼 때도 맞도록 "핫바에 놓고 선택"부터 안내하는 한국어 설명으로 대신한다.
    static string DemoDescription(Item item)
    {
        string description = item.description;
        if (PlayerInputHandler.Instance == null || !PlayerInputHandler.Instance.FirstDayControls)
            return description;
        string known = FirstDayUsage(item.id);
        if (known != null) return known;
        // 설명이 따로 있는 단계 도구(튼튼한 낚싯대·삽)는 설명 아래에 조작 한 줄을 덧붙인다.
        if (item.id == 2027) return description + "\n" + FishingSteps;
        if (item.id == 2038) return description + "\n흙이 들썩이는 자리 앞에서 E를 눌러 파요.";
        if (string.IsNullOrEmpty(description)) return description;
        return Regex.Replace(description, @"\bSpace\b", "E");
    }

    const string FishingSteps = "E로 던지고 찌가 잠기면 E로 당겨요.\nSpace·마우스로 초록 띠를 맞추고, 뭍에선 E로 잡아요.";

    // 키는 PlayerInputHandler·WorldHotbarPlacementController·FirstDayFishingMinigame의 첫날 조작과 같다.
    static string FirstDayUsage(int id)
    {
        const string Equip = "핫바에 놓고 선택한 뒤\n";
        const string Place = "핫바에 놓고 선택하면 설치 미리보기가 떠요.\n마우스로 위치 · E 설치 · R 회전 · Esc 취소";
        switch (id)
        {
            case 2001: return Equip + "나무 앞에서 E를 눌러 베어요.";
            case 2002: return Equip + "바위 앞에서 E를 눌러 캐요.";
            case 2003: return Equip + "곤충을 바라보고 E를 눌러 잡아요.";
            // 2026-11-04 실제 흐름(던지기 E → 찌가 잠기면 E로 당기기 → 릴: Space·마우스 왼쪽 → 뭍에 오른 물고기 E)으로 맞춤.
            case 2011: return Equip + FishingSteps;
            case 2012:
            case 2013:
            case 2014: return Place;
            default: return null;
        }
    }

    public void Hide() => gameObject.SetActive(false);

    public void UpdatePosition(Vector2 screenPos)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rootCanvas.transform as RectTransform, screenPos, rootCanvas.worldCamera, out localPoint);
        var point = localPoint + offset;
        if (PAUiTheme.Active)
        {
            var bounds = ((RectTransform)rootCanvas.transform).rect;
            point.x = Mathf.Clamp(point.x, bounds.xMin + 12, bounds.xMax - rectTransform.rect.width - 12);
            point.y = Mathf.Clamp(point.y, bounds.yMin + rectTransform.rect.height + 12, bounds.yMax - 12);
        }
        rectTransform.anchoredPosition = point;
    }
}