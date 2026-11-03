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

    public void Show(Item item, Vector2 screenPos)
    {
        nameText.text = PAUiTheme.Active ? ItemDisplayName.For(item) : item.itemName;
        descriptionText.text = PAUiTheme.Active ? DemoDescription(item) + "\n기본 가격  " + item.basePrice + " G" : item.description;
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
        if (string.IsNullOrEmpty(description)) return description;
        return Regex.Replace(description, @"\bSpace\b", "E");
    }

    // 키는 PlayerInputHandler·WorldHotbarPlacementController·FirstDayFishingMinigame의 첫날 조작과 같다.
    static string FirstDayUsage(int id)
    {
        const string Equip = "핫바에 놓고 선택한 뒤\n";
        const string Place = "핫바에 놓고 선택하면 설치 미리보기가 떠요.\n마우스로 위치 · E 설치 · R 회전 · Esc 취소";
        switch (id)
        {
            case 2001: return Equip + "나무 앞에서 E를 눌러 베어요.";
            case 2002: return Equip + "바위 앞에서 E를 눌러 캐요.";
            case 2003: return Equip + "나비를 바라보고 E를 눌러 잡아요.";
            case 2011: return Equip + "낚시터에서 E를 눌러 던져요.\n입질이 오면 Space·마우스 왼쪽으로 끌어올려요.";
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