using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 휴대폰 '도감' 앱(Docs/01_GAME_DESIGN/Content/COLLECTION.md). DiscoveryLog의 기록만 읽어 보여 준다.
// 발견한 것은 종류 색 받침(PAUiTheme.CategoryColor) 위 아이콘 + 이름, 아직이면 어두운 실루엣 + "???"(어디서 찾는지 한 줄 힌트).
public sealed class CollectionPhoneUI : MonoBehaviour
{
    static readonly Color Ink = SmartphoneUI.Ink, Teal = SmartphoneUI.Teal;
    static readonly Color Soft = new Color(.34f, .43f, .40f), Locked = new Color(.84f, .85f, .80f);
    static readonly (string section, string hint)[] Hints =
    {
        ("바다", "낚싯대로 물가에서"), ("곤충", "잠자리채로 풀밭에서"),
        ("해변", "아침마다 물가 모래밭에"), ("땅속", "흙이 들썩이는 자리를 삽으로"),
    };

    bool _built;
    int _shownRevision = -1;
    TextMeshProUGUI _progress;
    readonly System.Collections.Generic.List<(int id, Image tile, Image icon, TextMeshProUGUI name)> _cells =
        new System.Collections.Generic.List<(int, Image, Image, TextMeshProUGUI)>();

    void OnEnable()
    {
        if (!_built) Build();
        _shownRevision = -1;
        Refresh();
    }

    void Update()
    {
        if (_shownRevision != DiscoveryLog.Revision) Refresh();
    }

    static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rt.SetParent(parent, false);
        rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        return rt;
    }

    static TextMeshProUGUI Text(Transform parent, string name, string value, float size, Color color, Vector2 min, Vector2 max,
        Vector2 offsetMin, Vector2 offsetMax, bool bold = false, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
    {
        var tx = Rect(parent, name, min, max, offsetMin, offsetMax).gameObject.AddComponent<TextMeshProUGUI>();
        tx.text = value; tx.fontSize = size; tx.color = color; tx.alignment = align;
        tx.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tx.textWrappingMode = TextWrappingModes.NoWrap; tx.overflowMode = TextOverflowModes.Ellipsis; tx.raycastTarget = false;
        return tx;
    }

    void Build()
    {
        _built = true;
        var rt = (RectTransform)transform;
        var back = Rect(rt, "Back", new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -51), new Vector2(91, -12));
        var backImage = back.gameObject.AddComponent<Image>(); backImage.color = Teal;
        backImage.sprite = SmartphoneUI.RoundedSprite; backImage.type = Image.Type.Sliced;
        back.gameObject.AddComponent<Button>().onClick.AddListener(() => SmartphoneUI.instance?.ReturnToHome());
        Text(back, "Label", "‹  홈", 18, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true, TextAlignmentOptions.Center);
        Text(rt, "Title", "도감", 27, Ink, new Vector2(0, 1), new Vector2(1, 1), new Vector2(108, -53), new Vector2(-12, -8), true);
        _progress = Text(rt, "Progress", "", 17, Soft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(19, -88), new Vector2(-19, -60));

        const float cell = 52f, gap = 24f, sectionHeight = 116f;
        float top = -98f;
        for (int s = 0; s < DiscoveryLog.Sections.Length; s++)
        {
            var (section, ids) = DiscoveryLog.Sections[s];
            float y = top - s * sectionHeight;
            var card = Rect(rt, "Card_" + section, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, y - sectionHeight + 8), new Vector2(-10, y));
            var cardImage = card.gameObject.AddComponent<Image>(); cardImage.color = new Color(.95f, .96f, .91f);
            cardImage.sprite = SmartphoneUI.RoundedSprite; cardImage.type = Image.Type.Sliced; cardImage.raycastTarget = false;
            Text(card, "Heading", section, 19, Teal, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -32), new Vector2(-12, -6), true);
            Text(card, "Hint", Hints[s].hint, 14, Soft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(70, -32), new Vector2(-12, -6),
                false, TextAlignmentOptions.MidlineRight);
            for (int i = 0; i < ids.Length; i++)
            {
                float x = 16f + i * (cell + gap);
                var tileRect = Rect(card, "Tile_" + ids[i], new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -38 - cell), new Vector2(x + cell, -38));
                var tile = tileRect.gameObject.AddComponent<Image>();
                tile.sprite = SmartphoneUI.RoundedSprite; tile.type = Image.Type.Sliced; tile.raycastTarget = false;
                var icon = Rect(tileRect, "Icon", Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8)).gameObject.AddComponent<Image>();
                icon.preserveAspect = true; icon.raycastTarget = false;
                var name = Text(card, "Name_" + ids[i], "", 13, Ink, new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(x - gap * .5f + 1, -38 - cell - 26), new Vector2(x + cell + gap * .5f - 1, -38 - cell - 4), false, TextAlignmentOptions.Center);
                name.enableAutoSizing = true; name.fontSizeMin = 9; name.fontSizeMax = 13; name.overflowMode = TextOverflowModes.Overflow;
                _cells.Add((ids[i], tile, icon, name));
            }
        }
    }

    void Refresh()
    {
        _shownRevision = DiscoveryLog.Revision;
        if (_progress != null) _progress.text = $"발견 {DiscoveryLog.FoundCount} / {DiscoveryLog.Total}";
        var registry = ItemRegistry.Instance;
        foreach (var (id, tile, icon, name) in _cells)
        {
            var item = registry != null ? registry.Find(id, null) : null;
            bool found = DiscoveryLog.IsFound(id);
            tile.color = found && item != null ? PAUiTheme.CategoryColor(item) : Locked;
            icon.sprite = item != null ? item.icon : null;
            icon.enabled = icon.sprite != null;
            icon.color = found ? Color.white : new Color(.2f, .24f, .24f, .45f); // 실루엣
            name.text = found && item != null ? ItemDisplayName.For(item) : "???";
            name.color = found ? Ink : Soft;
        }
    }
}
