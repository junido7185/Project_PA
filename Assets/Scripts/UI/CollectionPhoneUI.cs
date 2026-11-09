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
        ("물고기", "낚싯대로 바다·강·하구·연못에서"), ("곤충", "잠자리채로 꽃밭·물가·숲·강가에서"),
        ("해변", "아침마다 물가 모래밭에"), ("땅속", "흙이 들썩이는 자리를 삽으로"),
    };

    // 2026-11-04 칸 상세(아이템 생태 연구 §8.1): 미발견은 그 대상만의 찾는 곳 힌트, 발견은 한 줄 해설 + 쓰임(판매·재료).
    // 정확한 좌표·내부 확률은 알려 주지 않는다(연구: 정답표처럼 공개하지 않음).
    static readonly System.Collections.Generic.Dictionary<int, string> ItemHints = new System.Collections.Generic.Dictionary<int, string>
    {
        [8] = "물가 어디서나 흔한 작은 그림자", [3001] = "큰 그림자 — 잡기 어려워요", [3002] = "중간 크기 그림자", [3003] = "작은 그림자 셋이 몰려다녀요", [3004] = "길쭉한 그림자 — 휙 달렸다가 쉬어요", [3005] = "넓적한 그림자 — 모래 바닥에 오래 멈춰 있어요",
        [2004] = "풀밭 위를 팔랑팔랑 날아다녀요", [3101] = "나무 밑동에 붙어 있어요 — 천천히 다가가요",
        [3102] = "해 질 녘부터 가게 앞 등불 가까이", [3103] = "낮에 풀밭 가장자리 꽃 위 — 살금살금", [3104] = "낮에 물가 바위 위를 맴돌아요", [3105] = "낮에 꽃 사이를 옮겨 다녀요 — 앉았을 때", [3106] = "낮에 풀밭과 모래 가장자리 — 뛴 다음 멈췄을 때", [3107] = "오후부터 밤까지 숲 나무 밑동 — 큰 턱", [3108] = "낮에 숲 나무 밑동 — 긴 더듬이를 흔들어요", [3109] = "해 질 녘·이른 아침 나무 그늘 — 우는 쪽 흔들리는 풀", [3110] = "낮에 꽃 위 — 바람처럼 살랑, 앞다리를 모아요", [3006] = "항구 앞·하구 — 길쭉한 은빛 그림자", [3111] = "밤 강가 풀 위 — 깜빡이다 멈춘 빛을 보고 다가가요", [3010] = "고지대 바위 사이 연못 — 위에서 낚싯대를 드리워요", [3008] = "낮에 섬을 흐르는 강 — 물길 따라 오르내리는 그림자", [3009] = "오후~밤 강어귀(하구) — 길게 굽이치는 그림자", [3007] = "물가 바위 곁 — 동글동글 느린 그림자",
        [2034] = "아침마다 물가 모래밭에 가장 흔해요", [2035] = "물가 모래밭, 가리비보다 드물어요", [2036] = "물가 모래밭에 가끔 올라와요",
        [2045] = "물가 모래에 길게 밀려와요", [2047] = "물가 모래밭에 드물게 반짝여요",
        [2039] = "흙 자국을 파면 가장 자주 나와요", [2040] = "흙 자국에서 가끔 나와요", [2041] = "흙 자국에서 드물게 나와요",
        [2049] = "흙 자국에서 가끔, 띠 무늬가 있어요", [2052] = "흙 자국에서 드물게, 바위 가까이",
        [2054] = "물가 모래밭에 드물게 떠밀려 와요", [2055] = "흙 자국에서 가장 드물게 나와요",
    };

    bool _built;
    int _shownRevision = -1;
    RectTransform _detail;
    Image _detailTile, _detailIcon;
    TextMeshProUGUI _detailName, _detailBody, _detailUse;
    static RecipeData[] _recipes;
    public int DetailId { get; private set; } = -1;
    readonly System.Collections.Generic.List<(Image image, TextMeshProUGUI label)> _tabs = new System.Collections.Generic.List<(Image, TextMeshProUGUI)>();
    readonly System.Collections.Generic.List<GameObject> _panels = new System.Collections.Generic.List<GameObject>();
    static int _selectedSection;
    public int SelectedSection => _selectedSection;

    public void SelectSection(int index)
    {
        _selectedSection = index;
        for (int s = 0; s < _panels.Count; s++) _panels[s].SetActive(s == index);
        HideDetail();
        PaintTabs();
    }

    void PaintTabs()
    {
        for (int s = 0; s < _tabs.Count && s < DiscoveryLog.Sections.Length; s++)
        {
            var (section, ids) = DiscoveryLog.Sections[s];
            int found = 0; foreach (int id in ids) if (DiscoveryLog.IsFound(id)) found++;
            bool on = s == _selectedSection;
            _tabs[s].image.color = on ? Teal : new Color(.9f, .92f, .86f);
            _tabs[s].label.color = on ? Color.white : Ink;
            _tabs[s].label.text = $"{section} {found}/{ids.Length}";
        }
    }
    public bool DetailOpen => _detail != null && _detail.gameObject.activeSelf;
    public string DetailText => DetailOpen ? $"{_detailName.text} | {_detailBody.text} | {_detailUse.text}" : "";
    // 쓰임 줄이 늘어(판매·재료·선물·소장·찾는 곳) 칸을 넘치는지 — 캡처 검사용.
    public bool DetailOverflow
    {
        get
        {
            if (!DetailOpen) return false;
            _detailUse.ForceMeshUpdate();
            _detailBody.ForceMeshUpdate();
            return _detailUse.isTextOverflowing || _detailBody.isTextOverflowing ||
                _detailUse.textBounds.size.y > ((RectTransform)_detailUse.transform).rect.height + 2f;
        }
    }
    TextMeshProUGUI _progress;
    readonly System.Collections.Generic.List<(int id, Image tile, Image icon, TextMeshProUGUI name)> _cells =
        new System.Collections.Generic.List<(int, Image, Image, TextMeshProUGUI)>();

    void OnEnable()
    {
        if (!_built) Build();
        HideDetail();
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

        // 2026-11-04 섹션 탭 + 3열 칸(아이템 생태 연구 §8.1): 한 줄 카드 4장은 섹션당 5칸이 한계였다(6칸째 산호·호박).
        // 위에 섹션 탭(이름 · 찾은 수/전체), 아래에 고른 섹션만 3열로 크게. 섹션당 9칸까지 3열, 10~12칸은 4열.
        var tabs = Rect(rt, "Tabs", new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -136), new Vector2(-10, -96));
        int count = DiscoveryLog.Sections.Length;
        // eel-r215: 섹션 이름이 "물고기"(세 글자)가 되자 같은 폭 탭에서 글자만 작아졌다 → 탭 폭을 "이름 99/99" 길이에 비례해 나눈다.
        float total = 0f;
        foreach (var sec in DiscoveryLog.Sections) total += sec.section.Length + 5f;
        float run = 0f;
        for (int s = 0; s < count; s++)
        {
            var (section, ids) = DiscoveryLog.Sections[s];
            float a0 = run / total; run += section.Length + 5f; float a1 = run / total;
            var tab = Rect(tabs, "Tab_" + section, new Vector2(a0, 0), new Vector2(a1, 1), new Vector2(3, 0), new Vector2(-3, 0));
            var tabImage = tab.gameObject.AddComponent<Image>();
            tabImage.sprite = SmartphoneUI.RoundedSprite; tabImage.type = Image.Type.Sliced;
            int index = s;
            tab.gameObject.AddComponent<Button>().onClick.AddListener(() => SelectSection(index));
            var label = Text(tab, "Label", section, 16, Ink, Vector2.zero, Vector2.one, new Vector2(2, 0), new Vector2(-2, 0), true, TextAlignmentOptions.Center);
            label.enableAutoSizing = true; label.fontSizeMin = 11; label.fontSizeMax = 16;
            _tabs.Add((tabImage, label));

            var panel = Rect(rt, "Section_" + section, new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 8), new Vector2(-10, -144));
            var panelImage = panel.gameObject.AddComponent<Image>(); panelImage.color = new Color(.95f, .96f, .91f);
            panelImage.sprite = SmartphoneUI.RoundedSprite; panelImage.type = Image.Type.Sliced; panelImage.raycastTarget = false;
            Text(panel, "Hint", Hints[s].hint, 15, Soft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -34), new Vector2(-14, -8));
            // 2026-11-09 섹션이 9칸을 넘으면(곤충 10종~) 4열·조금 작은 칸으로 12칸까지. 9칸 이하 섹션은 그대로 3열.
            int cols = ids.Length > 9 ? 4 : 3;
            float cell = cols == 4 ? 60f : 70f, rowHeight = cols == 4 ? 100f : 112f;
            for (int k = 0; k < ids.Length; k++)
            {
                int col = k % cols, line = k / cols;
                float c0 = col / (float)cols, c1 = (col + 1) / (float)cols, mid = (c0 + c1) * .5f, y = -44f - line * rowHeight;
                var tileRect = Rect(panel, "Tile_" + ids[k], new Vector2(mid, 1), new Vector2(mid, 1), new Vector2(-cell * .5f, y - cell), new Vector2(cell * .5f, y));
                var tile = tileRect.gameObject.AddComponent<Image>();
                tile.sprite = SmartphoneUI.RoundedSprite; tile.type = Image.Type.Sliced; tile.raycastTarget = true;
                int cellId = ids[k];
                tileRect.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowDetail(cellId));
                var icon = Rect(tileRect, "Icon", Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10)).gameObject.AddComponent<Image>();
                icon.preserveAspect = true; icon.raycastTarget = false;
                var name = Text(panel, "Name_" + ids[k], "", 15, Ink, new Vector2(c0, 1), new Vector2(c1, 1),
                    new Vector2(4, y - cell - 30), new Vector2(-4, y - cell - 6), false, TextAlignmentOptions.Center);
                name.enableAutoSizing = true; name.fontSizeMin = 11; name.fontSizeMax = 15;
                _cells.Add((ids[k], tile, icon, name));
            }
            _panels.Add(panel.gameObject);
        }
        SelectSection(Mathf.Clamp(_selectedSection, 0, count - 1));
        BuildDetail(rt);
    }

    void BuildDetail(RectTransform rt)
    {
        _detail = Rect(rt, "Detail", Vector2.zero, Vector2.one, new Vector2(10, 2), new Vector2(-10, -60)); // 아래 끝까지 덮어 뒤 카드 글자가 비치지 않게(ui-r104)
        var bg = _detail.gameObject.AddComponent<Image>(); bg.color = new Color(.97f, .96f, .91f);
        bg.sprite = SmartphoneUI.RoundedSprite; bg.type = Image.Type.Sliced;
        var back = Rect(_detail, "DetailBack", new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -46), new Vector2(104, -10));
        var backImage = back.gameObject.AddComponent<Image>(); backImage.color = Teal;
        backImage.sprite = SmartphoneUI.RoundedSprite; backImage.type = Image.Type.Sliced;
        back.gameObject.AddComponent<Button>().onClick.AddListener(HideDetail);
        Text(back, "Label", "‹  도감", 17, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true, TextAlignmentOptions.Center);
        var tileRect = Rect(_detail, "DetailTile", new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-56, -180), new Vector2(56, -68));
        _detailTile = tileRect.gameObject.AddComponent<Image>(); _detailTile.sprite = SmartphoneUI.RoundedSprite; _detailTile.type = Image.Type.Sliced;
        _detailIcon = Rect(tileRect, "Icon", Vector2.zero, Vector2.one, new Vector2(14, 14), new Vector2(-14, -14)).gameObject.AddComponent<Image>();
        _detailIcon.preserveAspect = true; _detailIcon.raycastTarget = false;
        _detailName = Text(_detail, "DetailName", "", 24, Ink, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -222), new Vector2(-16, -188), true, TextAlignmentOptions.Center);
        _detailBody = Text(_detail, "DetailBody", "", 16, Ink, new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -340), new Vector2(-22, -232), false, TextAlignmentOptions.TopLeft);
        _detailBody.textWrappingMode = TextWrappingModes.Normal; _detailBody.overflowMode = TextOverflowModes.Ellipsis;
        _detailUse = Text(_detail, "DetailUse", "", 15, Soft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -440), new Vector2(-22, -348), false, TextAlignmentOptions.TopLeft);
        // 2026-11-09 쓰임이 다섯 줄까지 늘 수 있어(선물·소장 추가) 칸 안에서 12pt까지 줄여 맞춘다.
        _detailUse.enableAutoSizing = true; _detailUse.fontSizeMin = 12; _detailUse.fontSizeMax = 15;
        _detailUse.textWrappingMode = TextWrappingModes.Normal; _detailUse.overflowMode = TextOverflowModes.Ellipsis;
        _detail.gameObject.SetActive(false);
    }

    public void HideDetail()
    {
        DetailId = -1;
        if (_detail != null) _detail.gameObject.SetActive(false);
    }

    static readonly System.Collections.Generic.Dictionary<int, string> RealNotes = new System.Collections.Generic.Dictionary<int, string>
    {
        [8] = "다랑어 무리 — 쉬지 않고 헤엄치는 빠른 물고기예요.",
        [3001] = "참돔 — 붉은 몸빛의 우리 바다 대표 도미예요.",
        [3002] = "산호초에 사는 쥐돔과의 노란 열대어예요.",
        [3003] = "큰 무리를 지어 다니는 은빛 물고기예요.",
        [3004] = "등의 푸른 물결무늬가 특징, 무리 지어 다녀요.",
        [3005] = "광어라고도 해요. 두 눈이 몸 한쪽에 몰려 있어요.",
        [2004] = "앞다리 한 쌍이 짧아 다리가 넷으로 보이는 주황 나비예요.",
        [3101] = "수컷 머리에 긴 뿔이 있고 참나무 수액을 먹어요.",
        [3102] = "밤 불빛에 모이는 흰 나방이에요. 애벌레는 나뭇잎을 갉아요.",
        [3103] = "진딧물을 잡아먹는 고마운 딱정벌레예요.",
        [3104] = "수컷은 몸이 새빨개요. 날면서 모기 같은 작은 벌레를 잡아요.",
        [3105] = "우리나라 토박이 뒤영벌이에요. 꽃가루를 옮겨 토마토 같은 열매가 잘 맺히게 도와요.",
        [3106] = "뒷다리를 잡으면 방아를 찧듯 몸을 끄덕여서 붙은 이름이에요. 풀색인 것도, 갈색인 것도 있어요.",
        [3107] = "우리나라에서 가장 흔한 사슴벌레예요. 밤에 참나무 진을 먹으러 나와요.",
        [3108] = "더듬이가 몸보다 훨씬 길고 마디마다 흰 고리가 있어요. 애벌레는 나무 속을 갉아 먹고 자라요.",
        [3109] = "수컷만 날개를 비벼 '또르르르' 울어요. 가을밤을 대표하는 소리예요.",
        [3110] = "앞다리를 모은 모습이 기도하는 것 같아요. 작은 곤충을 잡아먹는 사냥꾼이에요.",
        [3006] = "항구와 강어귀에 많이 살아요. 물 위로 펄쩍 뛰어오르기도 해요.",
        [3007] = "몸에 독이 있어 함부로 먹으면 안 돼요. 놀라면 몸을 동그랗게 부풀려요.",
        [3008] = "맑고 찬 물을 좋아하는 연어 친구예요. 강에서 자란 것을 산천어라고도 해요.",
        [3111] = "애벌레가 맑은 물속에서 다슬기를 먹고 자라요. 꽁무니 빛으로 짝을 찾아요.",
        [3010] = "연못·저수지에 흔한 민물고기예요. 금붕어와 같은 무리예요.",
        [3009] = "바다에서 태어나 강을 거슬러 올라와 살아요. 밤에 더 활발해요.",
        [2034] = "껍데기를 여닫으며 헤엄치듯 움직이는 조개예요.",
        [2035] = "뿔 달린 두꺼운 껍데기의 바다 고둥이에요.",
        [2036] = "짧은 다섯 팔의 불가사리, 우리 갯바위에 흔해요.",
        [2054] = "작은 동물(폴립)들이 모여 만든 단단한 뼈대예요.",
    };

    public void ShowDetail(int id)
    {
        if (!_built) Build();
        var item = ItemRegistry.Instance != null ? ItemRegistry.Instance.Find(id, null) : null;
        bool found = DiscoveryLog.IsFound(id);
        DetailId = id;
        _detail.gameObject.SetActive(true);
        _detail.SetAsLastSibling();
        _detailTile.color = found && item != null ? PAUiTheme.CategoryColor(item) : Locked;
        _detailIcon.sprite = item != null ? item.icon : null;
        _detailIcon.enabled = _detailIcon.sprite != null;
        _detailIcon.color = found ? Color.white : new Color(.2f, .24f, .24f, .45f);
        ItemHints.TryGetValue(id, out string hint);
        if (!found || item == null)
        {
            _detailName.text = "???";
            _detailBody.text = "아직 찾지 못했어요.\n<color=#2F6F6A><b>찾는 곳</b></color>  " + (hint ?? SectionHint(id));
            _detailUse.text = "";
            return;
        }
        _detailName.text = ItemDisplayName.For(item);
        _detailBody.text = item.description;
        // 2026-11-07 실제 종 한 줄(박물관 설명처럼 짧게, 확인된 사실만).
        if (RealNotes.TryGetValue(id, out string real)) _detailBody.text += "\n<color=#2F6F6A><b>실제로는</b></color>  " + real;
        var uses = new System.Collections.Generic.List<string>();
        if (item.basePrice > 0) uses.Add($"<b>판매</b>  기본가 {item.basePrice}G");
        _recipes ??= Resources.LoadAll<RecipeData>("");
        var consumers = new System.Collections.Generic.List<string>();
        foreach (var recipe in _recipes)
        {
            if (recipe == null || recipe.ingredients == null || recipe.outputItem == null) continue;
            foreach (var ingredient in recipe.ingredients)
                if (ingredient != null && ingredient.item != null && ingredient.item.id == id)
                { string n = ItemDisplayName.For(recipe.outputItem); if (!consumers.Contains(n)) consumers.Add(n); break; }
        }
        if (consumers.Count > 0) uses.Add("<b>재료</b>  " + string.Join(", ", consumers.GetRange(0, Mathf.Min(3, consumers.Count))) + (consumers.Count > 3 ? $" 외 {consumers.Count - 3}" : ""));
        // 2026-11-09 쓰임 보강(연구 §8.1 "판매/요리/선물/소장 쓰임"): 특히 좋아하는 동행, 가게 책상 위에 올려 두기.
        string likedBy = DemoResident.LikedByLabel(item);
        if (likedBy != null) uses.Add("<b>선물</b>  " + likedBy);
        if (ShopDeskShelf.Accepts(item)) uses.Add("<b>소장</b>  가게 책상 위에 올려 둘 수 있어요");
        if (hint != null) uses.Add("<b>찾는 곳</b>  " + hint);
        _detailUse.text = string.Join("\n", uses);
    }

    static string SectionHint(int id)
    {
        for (int s = 0; s < DiscoveryLog.Sections.Length; s++)
            if (System.Array.IndexOf(DiscoveryLog.Sections[s].ids, id) >= 0) return Hints[s].hint;
        return "";
    }

    void Refresh()
    {
        _shownRevision = DiscoveryLog.Revision;
        if (_progress != null) _progress.text = $"발견 {DiscoveryLog.FoundCount} / {DiscoveryLog.Total}";
        PaintTabs();
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
