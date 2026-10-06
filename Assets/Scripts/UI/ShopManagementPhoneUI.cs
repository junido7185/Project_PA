using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 기존 영업·정착 권위를 보여 주고 호출하는 스마트폰 화면.
public sealed class ShopManagementPhoneUI : MonoBehaviour
{
    static readonly Color Ink = SmartphoneUI.Ink;
    static readonly Color Soft = new Color(.34f, .43f, .40f);
    static readonly Color Teal = SmartphoneUI.Teal;
    static readonly Color Pale = new Color(.91f, .94f, .86f);
    static readonly Color Disabled = new Color(.61f, .67f, .63f);
    TextMeshProUGUI state, reason, sales, report, growth, feedback;
    Button open, close, claim;
    Button[] roots;
    float nextRefresh;
    bool built;
    string lastPhase;

    void OnEnable()
    {
        if (!built) Build();
        Refresh();
    }

    void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .3f;
        Refresh();
    }

    static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        return rt;
    }

    static TextMeshProUGUI Text(Transform parent, string name, string value, int size, Color color,
        Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, bool bold = false)
    {
        var rt = Rect(parent, name, min, max, offsetMin, offsetMax);
        var tx = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tx.text = value; tx.fontSize = size; tx.color = color;
        tx.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tx.alignment = TextAlignmentOptions.MidlineLeft;
        tx.textWrappingMode = TextWrappingModes.Normal; tx.overflowMode = TextOverflowModes.Ellipsis;
        tx.raycastTarget = false;
        return tx;
    }

    static Button Action(Transform parent, string name, string label, Vector2 min, Vector2 max,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = Rect(parent, name, min, max, offsetMin, offsetMax);
        var bg = rt.gameObject.AddComponent<Image>(); bg.color = Teal;
        bg.sprite = SmartphoneUI.RoundedSprite; bg.type = Image.Type.Sliced;
        var button = rt.gameObject.AddComponent<Button>();
        button.colors = new ColorBlock { normalColor = Color.white,
            highlightedColor = new Color(.88f, 1f, .94f), pressedColor = new Color(.67f, .88f, .81f),
            selectedColor = Color.white, disabledColor = Color.white, colorMultiplier = 1f, fadeDuration = .12f };
        var tx = Text(rt, "Label", label, 18, Color.white, Vector2.zero, Vector2.one,
            new Vector2(8, 0), new Vector2(-8, 0), true);
        tx.alignment = TextAlignmentOptions.Center;
        return button;
    }

    void Build()
    {
        built = true;
        var rt = (RectTransform)transform;
        var back = Action(rt, "Back", "‹  홈", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(12, -51), new Vector2(91, -12));
        back.onClick.AddListener(() => SmartphoneUI.instance?.ReturnToHome());
        Text(rt, "Title", "상점", 27, Ink, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(108, -53), new Vector2(-12, -8), true);

        var viewport = Rect(rt, "Viewport", Vector2.zero, Vector2.one,
            new Vector2(9, 6), new Vector2(-9, -65));
        viewport.gameObject.AddComponent<RectMask2D>();
        // P7: 카드 사이 빈 곳에서도 휠이 스크롤 영역에 닿게 투명한 레이캐스트 면을 둔다(없으면 ShopPanel 배경이 휠을 받는다).
        var wheelSurface = viewport.gameObject.AddComponent<Image>();
        wheelSurface.color = new Color(1f, 1f, 1f, 0f);
        wheelSurface.raycastTarget = true;
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
        // P7: 휠 한 칸에 실제로 내려가게 한다(기본 1은 Input System 휠에서 거의 움직이지 않음, CraftingUI와 같은 값).
        scroll.scrollSensitivity = 34f;
        var content = Rect(viewport, "Content", new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -786), Vector2.zero);
        content.pivot = new Vector2(.5f, 1);
        scroll.viewport = viewport; scroll.content = content;

        Card(content, "StateCard", -8, -253, new Color(.91f, .95f, .88f));
        Card(content, "SalesCard", -265, -429, new Color(.95f, .96f, .91f));
        Card(content, "GrowthCard", -441, -770, new Color(.95f, .96f, .91f));

        Text(content, "StatusHeading", "오늘의 상점", 19, Teal, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(19, -47), new Vector2(-19, -19), true);
        state = Text(content, "State", "", 23, Ink, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(19, -88), new Vector2(-19, -49), true);
        reason = Text(content, "Reason", "", 17, Soft, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(19, -141), new Vector2(-19, -91));

        open = Action(content, "Open", "영업 시작", new Vector2(0, 1), new Vector2(.49f, 1),
            new Vector2(19, -199), new Vector2(-5, -154));
        close = Action(content, "Close", "영업 마감", new Vector2(.51f, 1), new Vector2(1, 1),
            new Vector2(5, -199), new Vector2(-19, -154));
        open.onClick.AddListener(OpenShop); close.onClick.AddListener(CloseShop);
        feedback = Text(content, "Feedback", "", 16, Soft, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(19, -237), new Vector2(-19, -202));

        Text(content, "SalesHeading", "매출 · 최근 Report", 19, Teal, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(19, -304), new Vector2(-19, -276), true);
        sales = Text(content, "Sales", "", 18, Ink, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(19, -356), new Vector2(-19, -307));
        report = Text(content, "Report", "", 17, Soft, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(19, -414), new Vector2(-19, -357));

        Text(content, "GrowthHeading", "성장 · 전문 분야", 19, Teal, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(19, -481), new Vector2(-19, -452), true);
        growth = Text(content, "Growth", "", 17, Ink, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(19, -527), new Vector2(-19, -482));
        roots = new Button[4];
        string[] names = { "임업", "광업", "수산업", "농업" };
        for (int i = 0; i < 4; i++)
        {
            int selected = i;
            int row = i / 2; bool left = i % 2 == 0;
            roots[i] = Action(content, "Root" + i, names[i],
                new Vector2(left ? 0 : .51f, 1), new Vector2(left ? .49f : 1, 1),
                new Vector2(left ? 19 : 5, -580 - row * 53),
                new Vector2(left ? -5 : -19, -535 - row * 53));
            roots[i].onClick.AddListener(() => ChooseRoot((DemoSpecialization)(selected + 1)));
        }
        claim = Action(content, "ClaimStands", "남은 가판대 받기", new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(19, -728), new Vector2(-19, -683));
        claim.onClick.AddListener(ClaimStands);
    }

    static void Card(Transform parent, string name, int top, int bottom, Color color)
    {
        var rt = Rect(parent, name, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(5, bottom), new Vector2(-5, top));
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = SmartphoneUI.RoundedSprite; image.type = Image.Type.Sliced;
        image.color = color; image.raycastTarget = false;
    }

    void Refresh()
    {
        if (!built) return;
        var settlement = DemoSettlementController.Instance;
        var loop = DayNightShopLoopController.Instance;
        bool installed = settlement != null && settlement.HasShopBase && settlement.OperatingShop != null;
        bool opened = loop != null && loop.IsShopOpenForCustomers && !loop.OpeningSessionCompleted;
        state.text = !installed ? "상점 미설치" : opened ? "영업 중" : loop != null && loop.OpeningSessionCompleted ? "오늘 영업 완료" : "영업 준비 중";
        // 버튼 결과 문구는 상태가 바뀌면 새 상태로 교체한다(22:00 자동 마감 뒤 '영업을 시작했어요.'가 남지 않게).
        string phase = !installed ? "none" : opened ? "open" : loop != null && loop.OpeningSessionCompleted ? "done" : "ready";
        if (lastPhase != null && phase != lastPhase)
            feedback.text = phase == "open" ? "영업 중이에요. 22:00에 자동으로 문을 닫아요." :
                phase == "done" ? "오늘 영업을 마쳤어요. 아래에서 Report를 확인하세요." : string.Empty;
        lastPhase = phase;
        reason.text = !installed ? "핫바의 상점 설계도로 먼저 상점을 설치하세요." :
            loop == null ? "영업 시스템을 찾을 수 없어요." :
            opened ? "손님을 맞이하고 마감할 수 있어요." :
            loop.OpeningSessionCompleted ? "최근 영업 결과를 아래에서 확인하세요." :
            !settlement.NightReady ? "밤이 되면 영업을 시작할 수 있어요." :
            !settlement.Established ? "주민 텐트 두 개로 정착을 완료하세요." :
            "가판대에 상품을 진열한 뒤 영업을 시작하세요.";
        open.interactable = installed && loop != null && !opened && !loop.OpeningSessionCompleted &&
            (loop.IsOpeningDemo ? settlement.NightReady : loop.CanPlayerOpenShop);
        close.interactable = installed && loop != null && loop.IsOpeningDemo && opened;
        open.GetComponent<Image>().color = open.interactable ? Teal : Disabled;
        close.GetComponent<Image>().color = close.interactable ? Teal : Disabled;

        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        var history = SalesLogManager.Instance;
        var records = history != null ? history.GetRecent(100) : null;
        int revenue = records != null ? records.Where(r => r != null && r.gameDay == day).Sum(r => r.price) : 0;
        var stats = history != null ? history.GetDailyDecisionStats(day) : default;
        sales.text = $"오늘 매출 {revenue:N0} G  ·  판매 {stats.purchases}건\n구매 보류 {stats.rejections}회";
        var latest = settlement != null ? settlement.PioneerReport : null;
        report.text = latest == null ? "최근 개척 보고서는 영업 마감 후 표시됩니다." :
            $"최근 Report  ·  {latest.total} / 100점\n판매 {latest.sales}건  ·  매출 {latest.revenue:N0} G";
        // P7: 네 분야는 처음부터 읽히고, 동행 궁합과 고른 분야의 다음 1단계를 보인다.
        string synergy = settlement == null ? "" : string.Join("·", new[] { DemoSpecialization.Forestry, DemoSpecialization.Mining,
            DemoSpecialization.Fisheries, DemoSpecialization.Agriculture }.Where(settlement.HasSynergy).Select(RootName));
        growth.text = settlement == null ? "정착 정보가 없습니다." :
            settlement.SelectedRoot != DemoSpecialization.None
                ? $"선택 {RootName(settlement.SelectedRoot)}  ·  {DemoSettlementController.NextPreview(settlement.SelectedRoot)}"
                : $"개척 포인트 {settlement.LicensePoints}  ·  " + (settlement.Established ? "한 분야를 고르세요" : "정착을 마치면 고를 수 있어요") +
                  (synergy.Length > 0 ? $"\n★ 동행 궁합: {synergy}" : "");
        bool canChoose = settlement != null && settlement.Established && settlement.LicensePoints == 1 &&
            settlement.SelectedRoot == DemoSpecialization.None && latest == null;
        for (int i = 0; i < roots.Length; i++)
        {
            roots[i].interactable = canChoose;
            var rootLabel = roots[i].GetComponentInChildren<TMPro.TMP_Text>();
            if (rootLabel != null && settlement != null)
                rootLabel.text = RootName((DemoSpecialization)(i + 1)) + (settlement.HasSynergy((DemoSpecialization)(i + 1)) ? " ★" : "");
            roots[i].GetComponent<Image>().color = settlement != null &&
                settlement.SelectedRoot == (DemoSpecialization)(i + 1)
                ? new Color(.69f, .48f, .28f) : canChoose ? Teal : Disabled;
        }
        claim.interactable = settlement != null && installed && settlement.GrantedStands < 3;
        claim.GetComponent<Image>().color = claim.interactable ? Teal : Disabled;
    }

    static string RootName(DemoSpecialization root) => root switch
    {
        DemoSpecialization.Forestry => "임업", DemoSpecialization.Mining => "광업",
        DemoSpecialization.Fisheries => "수산업", DemoSpecialization.Agriculture => "농업", _ => "미선택"
    };

    void OpenShop()
    {
        var loop = DayNightShopLoopController.Instance;
        if (loop == null || !loop.TryOpenShop()) feedback.text = "영업 조건을 확인하세요: 밤 시간과 진열된 상품이 필요합니다.";
        else feedback.text = "영업을 시작했어요.";
        Refresh();
    }
    void CloseShop()
    {
        var loop = DayNightShopLoopController.Instance;
        feedback.text = loop != null && loop.TryCloseOpeningShop() ? "마감했어요. Report를 확인하세요." : "지금은 마감할 수 없어요.";
        Refresh();
    }
    void ChooseRoot(DemoSpecialization root)
    {
        var settlement = DemoSettlementController.Instance;
        feedback.text = settlement != null && settlement.TryChooseRoot(root) ? "전문 분야를 선택했어요." : "정착 완료와 개척 포인트가 필요합니다.";
        Refresh();
    }
    void ClaimStands()
    {
        DemoSettlementController.Instance?.TryClaimStands();
        feedback.text = "가판대 지급 상태를 확인하세요.";
        Refresh();
    }
}
