using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// SPY-002 — 고객 타입 / 성향 프레젠테이션 (A. 고객 성향 힌트).
//
// 설계 의도 (PROJECT_PA_CREATIVE_NORTH_STAR.md "NPC Roles" / "Shop Operation Fantasy"):
// - NPC 가 익명의 군중이 아니라, 저마다 취향을 가진 "주민 손님"으로 읽히게 한다.
// - 구매 확률/경제 계산을 절대 바꾸지 않는 읽기 전용(presentation-only) 사이드카다.
//
// 데이터 출처 (실제 존재하는 NpcProfile / PurchaseEvaluator 데이터만 사용):
// - traitSN  : 카테고리 선호 (S=실용재 / N=장식·고급품). PurchaseEvaluator §2 categoryBonus 와 동일 축.
// - traitTF  : 구매 스타일 (F=감성 구매 증폭 / T=냉정한 가격 판단). PurchaseEvaluator §3-a, §4.
// - traitEI  : 구매 적극성 (E=충동·잦은 외출 / I=신중). NpcController 쇼핑 확률 / PurchaseEvaluator §5 와 동일 축.
// - priceSensitivity : 프로필별 실제 값이 기본값과 충분히 다를 때만 표시한다.
//                      → 데이터가 없는 성향을 임의로 만들어 표시하지 않는다는 SPY-002 원칙 준수.
// - NpcScheduleController.scheduleData : 현재 씬 자동 생성 계약에서 상주 주민 8명 모두가 가진
//                      마을 일과표다. 별도 관광객 데이터가 생기기 전까지 표시 전용 계층 기준으로만
//                      사용하며, 일과표가 없는 임시 방문 손님은 [관광객]으로 표시한다.
//
// 표시 시점:
// - NpcController.currentState 가 MovingToShop / BrowsingShop 인 손님(=가게로 향하거나 둘러보는 손님)을
//   "관심 손님"으로 보고 그 성향을 패널에 보여준다.
// - 활동 중인 손님이 없으면 주민마다 취향이 다르다는 안내 문구를 보여준다(개념 학습).
public class CustomerPreferencePresentationController : MonoBehaviour
{
    public static CustomerPreferencePresentationController Instance { get; private set; }

    [Header("Preference Hint")]
    public bool autoCreateUI = true;
    public int maxVisibleCustomers = 3;

    [Header("UI")]
    public TextMeshProUGUI preferenceText;

    Canvas _canvas;
    GameObject _panel;
    string _lastPreferenceText = "관심 손님 성향\n[주민]/[관광객] 계층과 취향을 확인해 보세요.";
    float _nextRefreshAt;

    public string CurrentPreferenceText => preferenceText != null ? preferenceText.text : _lastPreferenceText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (autoCreateUI && preferenceText == null)
            BuildUI();
    }

    void Start()
    {
        RefreshNow();
    }

    void Update()
    {
        if (Time.unscaledTime < _nextRefreshAt) return;
        _nextRefreshAt = Time.unscaledTime + 0.75f;
        RefreshNow();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RefreshNow()
    {
        _lastPreferenceText = BuildPreferenceText();
        if (preferenceText != null)
            preferenceText.text = _lastPreferenceText;
    }

    string BuildPreferenceText()
    {
        var lines = new List<string> { "관심 손님 성향" };

        int shown = 0;
        foreach (var npc in FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            if (npc == null) continue;
            if (npc.currentState == NpcController.State.Idle) continue; // 가게로 향하거나 둘러보는 손님만

            string name = ResolveName(npc);
            string customerClass = DescribeCustomerClass(npc);
            string hint = DescribePreference(npc.profile);
            lines.Add($"{name} {customerClass} · {hint}");

            shown++;
            if (shown >= Mathf.Max(1, maxVisibleCustomers)) break;
        }

        if (shown == 0)
            lines.Add("밤에 가게를 열면 [주민]/[관광객]과 취향이 표시됩니다.");

        return string.Join("\n", lines);
    }

    static string ResolveName(NpcController npc)
    {
        if (npc == null) return "손님";
        if (npc.profile != null && !string.IsNullOrEmpty(npc.profile.npcName))
            return npc.profile.npcName;
        return npc.gameObject.name;
    }

    // Task 031 — 손님 계층은 구매 수학이나 FSM을 바꾸지 않는 표시 전용 파생값이다.
    // 현재 영구 주민 생성 경로는 모두 실제 NpcDailySchedule을 연결한다. 반대로 일과표가 없는
    // NpcController는 마을에 상주하지 않는 임시 방문 손님으로 읽는다. 향후 명시적 관광객 데이터가
    // 추가되면 이 한 곳만 그 필드로 교체하면 되며, 현재 주민을 임의로 관광객으로 꾸미지 않는다.
    public static string DescribeCustomerClass(NpcController npc)
    {
        if (npc == null)
            return "[손님]";

        var schedule = npc.GetComponent<NpcScheduleController>();
        return DescribeCustomerClass(schedule != null && schedule.scheduleData != null);
    }

    public static string DescribeCustomerClass(bool hasVillageSchedule)
    {
        return hasVillageSchedule ? "[주민]" : "[관광객]";
    }

    // NpcProfile 의 실제 값만으로 짧은 성향 힌트를 만든다.
    // 형식: "<카테고리 선호>[ · <구매 스타일>]" (디버그 수치/확률 노출 금지).
    public static string DescribePreference(NpcProfile profile)
    {
        if (profile == null)
            return "취향 정보 없음";

        // 1) 카테고리 선호 — traitSN (PurchaseEvaluator categoryBonus 와 동일 기준)
        string category;
        if (profile.traitSN <= -0.2f)
            category = "실용재(식료품·도구) 선호";
        else if (profile.traitSN >= 0.2f)
            category = "장식·고급품 선호";
        else
            category = "실용·고급 두루 구매";

        // 2) 구매 스타일 한 가지만 덧붙인다(짧게 유지).
        //    우선순위: 가격 민감도(데이터가 기본값과 다를 때만) > 감성/실리(traitTF) > 충동/신중(traitEI).
        string style = null;

        if (profile.priceSensitivity >= 1.25f)
            style = "가격에 민감";
        else if (profile.priceSensitivity <= 0.75f)
            style = "가격에 관대";
        else if (profile.traitTF >= 0.4f)
            style = "감성 구매형";
        else if (profile.traitTF <= -0.4f)
            style = "실리 판단형";
        else if (profile.traitEI >= 0.25f)
            style = "충동구매형";
        else if (profile.traitEI <= -0.25f)
            style = "신중형";

        return style == null ? category : $"{category} · {style}";
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("CustomerPreferenceCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 55;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 우상단 인사이트 스택(Money / Demand / Village) 바로 아래.
        // Village 패널이 y=-286, 높이 92 → 하단 -378. 16px 간격을 두고 -394 에 배치한다.
        _panel = new GameObject("CustomerPreferencePanel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)_panel.transform;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -394f);
        rt.sizeDelta = new Vector2(470f, 110f);

        var bg = _panel.GetComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.07f, 0.58f);
        bg.raycastTarget = false;

        var textGo = new GameObject("CustomerPreferenceText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(_panel.transform, false);
        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(14f, 8f);
        textRt.offsetMax = new Vector2(-14f, -8f);

        preferenceText = textGo.GetComponent<TextMeshProUGUI>();
        preferenceText.fontSize = 14f;
        preferenceText.fontStyle = FontStyles.Bold;
        preferenceText.alignment = TextAlignmentOptions.TopLeft;
        preferenceText.textWrappingMode = TextWrappingModes.Normal;
        preferenceText.overflowMode = TextOverflowModes.Ellipsis;
        preferenceText.color = new Color(0.96f, 0.86f, 0.98f, 1f);
        preferenceText.raycastTarget = false;
    }
}
