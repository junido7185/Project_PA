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
        nameText.text = item.itemName;
        descriptionText.text = PAUiTheme.Active ? item.description + "\n기본 가격  " + item.basePrice + " G" : item.description;
        if (PAUiTheme.Active) PAUiTheme.Tooltip(this);
        icon.sprite = item.icon;
        gameObject.SetActive(true);
        UpdatePosition(screenPos);
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