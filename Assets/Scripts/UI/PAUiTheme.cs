using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Opening Canon §23 / D2: 표현만 공유한다. 입력·아이템·저장 권위는 기존 UI에 있다.
public static class PAUiTheme
{
    public static Color Cream => SmartphoneUI.Cream;
    public static Color Ink => SmartphoneUI.Ink;
    public static Color Teal => SmartphoneUI.Teal;
    public static Sprite RoundedSprite => SmartphoneUI.RoundedSprite;
    public static readonly Color Gold = new Color(.96f,.79f,.38f);
    public static readonly Color Muted = new Color(.42f,.49f,.45f);
    public static readonly Color EmptySlot = new Color(.84f,.86f,.77f,.72f);
    public static readonly Color FilledSlot = new Color(1f,.985f,.94f);
    public static readonly Color Success = new Color(.31f,.62f,.45f);
    public static readonly Color Warning = new Color(.72f,.36f,.25f);
    public static readonly Color SoftSuccess = new Color(.82f,.91f,.81f);
    public static readonly Color SoftWarning = new Color(.98f,.85f,.73f);
    public const float BodySize = 22f, CaptionSize = 18f, TitleSize = 32f;
    public const float SlotSize = 96f, Gap = 12f, ButtonHeight = 52f;
    public static bool Active => PlayerInputHandler.Instance?.FirstDayControls == true
        || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == DepartureTutorialController.SceneName;

    public static void Surface(Image image, Color color)
    {
        if (image == null) return;
        image.sprite = RoundedSprite; image.type = Image.Type.Sliced; image.color = color;
    }

    public static Image Panel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent,false);
        var image = go.GetComponent<Image>(); Surface(image,color); return image;
    }

    public static void Button(Button button, Color color)
    {
        if (button == null) return;
        Surface(button.GetComponent<Image>(),color);
        var colors = button.colors;
        colors.normalColor = Color.white; colors.highlightedColor = new Color(.88f,1f,.94f);
        colors.pressedColor = new Color(.72f,.87f,.81f); colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(.67f,.71f,.66f,.7f); colors.fadeDuration = .12f;
        button.colors = colors;
    }

    public static void Slot(InventorySlotUI ui, InventorySlot slot, bool selected = false, bool holstered = false)
    {
        if (ui == null) return;
        bool empty = slot == null || slot.IsEmpty;
        Surface(ui.GetComponent<Image>(), selected ? (holstered ? Color.Lerp(Cream,Gold,.4f) : Gold)
            : empty ? EmptySlot : FilledSlot);
        if (ui.countText != null)
        {
            var count = ui.countText.rectTransform;
            count.anchorMin = count.anchorMax = count.pivot = Vector2.one;
            count.anchoredPosition = new Vector2(-7,-5); count.sizeDelta = new Vector2(38,22);
            ui.countText.fontSize = CaptionSize; ui.countText.color = Ink;
            ui.countText.alignment = TextAlignmentOptions.TopRight;
            ui.countText.margin = Vector4.zero; ui.countText.raycastTarget = false;
        }
        Durability(ui.transform,empty ? null : slot.instance);
    }

    public static void Durability(Transform slot, ItemInstance item)
    {
        int max = item != null ? ToolDurability.MaxFor(item.data) : 0;
        var bar = slot.Find("DurabilityBar");
        if (max <= 0) { if (bar != null) bar.gameObject.SetActive(false); return; }
        if (bar == null)
        {
            bar = Panel(slot,"DurabilityBar",Muted).transform;
            var rect = (RectTransform)bar;
            rect.anchorMin = new Vector2(.16f,.28f); rect.anchorMax = new Vector2(.84f,.28f);
            rect.offsetMin = new Vector2(0,-2.5f); rect.offsetMax = new Vector2(0,2.5f);
            bar.GetComponent<Image>().raycastTarget = false;
            var fill = Panel(bar,"Fill",Success); fill.raycastTarget = false;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
        }
        bar.gameObject.SetActive(true);
        float ratio = Mathf.Clamp01(ToolDurability.Remaining(item)/(float)max);
        var fillRect = (RectTransform)bar.Find("Fill"); fillRect.anchorMax = new Vector2(ratio,1);
        fillRect.GetComponent<Image>().color = ratio > .5f ? Success : ratio > .25f ? Gold : Warning;
    }

    public static ItemTooltip EnsureTooltip(Canvas canvas)
    {
        if (canvas == null) return null;
        var existing = canvas.GetComponentInChildren<ItemTooltip>(true);
        if (existing != null) return existing;
        var surface = Panel(canvas.transform, "ItemTooltip", Cream);
        var tooltip = surface.gameObject.AddComponent<ItemTooltip>();
        tooltip.rectTransform = surface.rectTransform;
        var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(surface.transform, false);
        tooltip.icon = icon.GetComponent<Image>();
        var name = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        name.transform.SetParent(surface.transform, false);
        tooltip.nameText = name.GetComponent<TextMeshProUGUI>();
        var description = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
        description.transform.SetParent(surface.transform, false);
        tooltip.descriptionText = description.GetComponent<TextMeshProUGUI>();
        Tooltip(tooltip); tooltip.Hide();
        return tooltip;
    }

    public static void Tooltip(ItemTooltip tooltip)
    {
        if (tooltip == null) return;
        var rect = tooltip.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
        rect.pivot = new Vector2(0,1); rect.sizeDelta = new Vector2(330,168);
        var background = tooltip.GetComponent<Image>();
        if (background == null) background = tooltip.transform.Find("Background")?.GetComponent<Image>();
        Surface(background,Cream);
        tooltip.transform.SetAsLastSibling();
        foreach (var graphic in tooltip.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        var icon = tooltip.icon.rectTransform;
        icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0,1);
        icon.anchoredPosition = new Vector2(16,-16); icon.sizeDelta = new Vector2(56,56);
        tooltip.icon.preserveAspect = true;
        Position(tooltip.nameText,new Vector2(84,-16),new Vector2(230,40),BodySize,Teal);
        Position(tooltip.descriptionText,new Vector2(18,-78),new Vector2(294,76),CaptionSize,Ink);
    }

    static void Position(TMP_Text text, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        text.fontSize = fontSize; text.color = color; text.alignment = TextAlignmentOptions.TopLeft;
        text.enableAutoSizing = true; text.fontSizeMin = 15; text.fontSizeMax = fontSize;
        text.textWrappingMode = TextWrappingModes.Normal;
    }
}
