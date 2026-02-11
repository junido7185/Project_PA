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
        descriptionText.text = item.description;
        icon.sprite = item.icon;
        gameObject.SetActive(true);
        UpdatePosition(screenPos);
    }

    public void Hide() => gameObject.SetActive(false);

    public void UpdatePosition(Vector2 screenPos)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rootCanvas.transform as RectTransform, screenPos, rootCanvas.worldCamera, out localPoint);
        rectTransform.anchoredPosition = localPoint + offset;
    }
}