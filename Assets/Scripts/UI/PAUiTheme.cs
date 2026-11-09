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

    public static void Slot(InventorySlotUI ui, InventorySlot slot, bool selected = false, bool holstered = false, bool hotbar = false)
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
        CategoryTile(ui.transform, empty ? null : slot.item);
        Durability(ui.transform,empty ? null : slot.instance);
        // 2026-11-04 레퍼런스 10·13: 고른 핫바 칸은 한 단계 크게(배치는 레이아웃 그룹 소유라 크기만, 가방 칸은 selected=false라 그대로).
        // 형제 순서는 레이아웃 그룹의 칸 순서라 건드리지 않는다.
        // 칸 자체의 새로 고침(InventorySlotUI, hotbar=false)은 핫바가 정한 크기를 건드리지 않는다.
        if (hotbar) ui.transform.localScale = Vector3.one * (selected && !holstered ? 1.1f : selected ? 1.05f : 1f);
    }

    // 2026-11-03 레퍼런스 08(가방·핫바): 아이콘 뒤 둥근 받침을 종류 색으로 칠해 가방을 훑어볼 때 도구·바다 생물·귀중품·가공품·설비가
    // 색으로 먼저 갈린다. 색은 한 단계 연하게(이름 글자 Ink 대비 유지), 종류는 글자(이름)로도 읽힌다(DESIGN.md §2 색).
    public static readonly Color TileTool = new Color(.97f,.74f,.5f), TileSea = new Color(.56f,.82f,.76f), TileValuable = new Color(1f,.92f,.68f),
        TileGoods = new Color(.72f,.86f,.58f), TileKit = new Color(.8f,.75f,.92f), TileMaterial = new Color(.88f,.8f,.66f);

    public static Color CategoryColor(Item item)
    {
        if (item == null) return Color.clear;
        if (item.toolType == ToolType.Building || item.id == 1001 || item.id == 2005 || item.id == 2012 || item.id == 2013 || item.id == 2014 ||
            item.id == 2020 || item.id == 2023 || item.id == 2024 || item.id == 2021 || item.id == 11) return TileKit;
        if (item.toolType != ToolType.None || item.category == ItemCategory.Tool) return TileTool;
        if (item.category == ItemCategory.Luxury || item.id == 2040 || item.id == 2041 || item.id == 2052 || item.id == 2055) return TileValuable;
        if (item.category == ItemCategory.Processed || item.category == ItemCategory.Utility) return TileGoods;
        bool sea = item.id == 8 || item.id >= 3000 && item.id < 3100 || item.id == 2004 || item.id == 3101 || item.id == 3102 || item.id == 3103 || item.id == 3104 || item.id == 3105 || item.id == 3106 || item.id == 3107 || item.id == 3108 || item.id == 3109 || item.id == 3110 || item.id == 3111 || item.id >= 2034 && item.id <= 2036 || item.id == 2045 || item.id == 2047 || item.id == 2054;
        return sea ? TileSea : TileMaterial;
    }

    static void CategoryTile(Transform slot, Item item)
    {
        var tile = slot.Find("CategoryTile");
        if (item == null) { if (tile != null) tile.gameObject.SetActive(false); return; }
        if (tile == null)
        {
            var image = Panel(slot, "CategoryTile", Color.white);
            image.raycastTarget = false;
            tile = image.transform;
            var rect = (RectTransform)tile;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(9, 25); rect.offsetMax = new Vector2(-9, -9);
            tile.SetAsFirstSibling();
        }
        tile.gameObject.SetActive(true);
        tile.GetComponent<Image>().color = CategoryColor(item);
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
        // 2026-11-04 ui-r119: 설명 + 조작 줄이 길면(튼튼한 낚싯대) 고정 168px 상자 밖으로 넘쳐 핫바를 덮었다 → 설명 높이에 맞춰 상자를 늘린다(최소 그대로).
        var description = tooltip.descriptionText;
        description.enableAutoSizing = false; description.fontSize = CaptionSize;
        float needed = description.GetPreferredValues(description.text, 294f, 0f).y + 4f;
        if (needed > 76f)
        {
            description.rectTransform.sizeDelta = new Vector2(294f, needed);
            rect.sizeDelta = new Vector2(330f, 78f + needed + 16f);
        }
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
