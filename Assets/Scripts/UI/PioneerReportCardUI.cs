using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Canon v2 §20: SHOP CLOSE 직후 한 번 보여 주는 데모 결말 카드(점수·Rank·행동 기반 댓글 2~4개).
// 점수 계산은 DemoPioneerReport가 한다. 이 화면은 보여 주기만 한다.
public sealed class PioneerReportCardUI : MonoBehaviour
{
    public static PioneerReportCardUI Instance { get; private set; }
    public bool IsOpen => _root != null && _root.activeSelf;

    static readonly Color Ink = PAUiTheme.Ink;
    static readonly Color Teal = PAUiTheme.Teal;
    static readonly Color Cream = PAUiTheme.Cream;
    static readonly Color Soft = new Color(.40f, .47f, .45f);
    static readonly Color Track = new Color(.86f, .84f, .76f);

    GameObject _root;
    CanvasGroup _group;
    RectTransform _card;
    TextMeshProUGUI _rank, _total, _summary;
    readonly Image[] _fills = new Image[4];
    readonly TextMeshProUGUI[] _values = new TextMeshProUGUI[4];
    readonly float[] _targets = new float[4];
    RectTransform _comments;
    Image _badge;
    float _openedAt;
    Action _onClosed;

    public static PioneerReportCardUI Show(DemoPioneerReportData report, Action onClosed)
    {
        if (report == null) return null;
        if (Instance == null) Instance = new GameObject("PioneerReportCardUI").AddComponent<PioneerReportCardUI>();
        Instance.Present(report, onClosed);
        return Instance;
    }

    public void Hide()
    {
        if (!IsOpen) return;
        _root.SetActive(false);
        var callback = _onClosed; _onClosed = null;
        callback?.Invoke();
    }

    void Awake()
    {
        Instance = this;
        Build();
        _root.SetActive(false);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (!IsOpen) return;
        float t = Mathf.Clamp01((Time.unscaledTime - _openedAt) / .28f);
        float ease = 1f - Mathf.Pow(1f - t, 3f);
        _group.alpha = ease;
        _card.localScale = Vector3.one * Mathf.Lerp(.94f, 1f, ease);
        float bars = Mathf.Clamp01((Time.unscaledTime - _openedAt - .2f) / .6f);
        for (int i = 0; i < _fills.Length; i++) _fills[i].fillAmount = _targets[i] * (1f - Mathf.Pow(1f - bars, 2f));

        var keyboard = Keyboard.current;
        if (Time.unscaledTime - _openedAt > .4f && keyboard != null &&
            (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
            Hide();
    }

    void Present(DemoPioneerReportData report, Action onClosed)
    {
        _onClosed = onClosed;
        string rank = RankLetter(report.rankKey);
        _rank.text = rank;
        _badge.color = RankColor(rank);
        _total.text = $"<size=46><b>{report.total}</b></size><size=24> / 100</size>";
        _summary.text = $"판매 <b>{report.sales}</b>건   ·   매출 <b>{report.revenue:N0} G</b>   ·   구매 보류 {report.rejections}회";
        int[] values = { report.settlement, report.commerce, report.development, report.exploration };
        int[] maxima = { 25, 30, 25, 20 };
        for (int i = 0; i < 4; i++)
        {
            _targets[i] = Mathf.Clamp01(values[i] / (float)maxima[i]);
            _values[i].text = $"{values[i]} / {maxima[i]}";
            _fills[i].fillAmount = 0f;
        }
        for (int i = _comments.childCount - 1; i >= 0; i--) Destroy(_comments.GetChild(i).gameObject);
        var keys = report.commentKeys ?? Array.Empty<string>();
        for (int i = 0; i < keys.Length && i < 4; i++) Post(keys[i], i);
        _openedAt = Time.unscaledTime;
        _group.alpha = 0f;
        _root.SetActive(true);
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    void Build()
    {
        var canvasGo = new GameObject("PioneerReportCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 80;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        _root = canvasGo;
        _group = canvasGo.AddComponent<CanvasGroup>();

        var dim = Panel(canvasGo.transform, "Dim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(.04f, .07f, .08f, .62f), false);
        dim.GetComponent<Image>().raycastTarget = true;

        _card = Panel(canvasGo.transform, "Card", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-400, -400), new Vector2(400, 460), Cream, true);
        var header = Panel(_card, "Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -96), Vector2.zero, Teal, true);
        Label(header, "Title", "P.A. 개척 보고서", 34, Color.white, new Vector2(0, 0), new Vector2(1, 1), new Vector2(36, 0), new Vector2(-36, 6), TextAlignmentOptions.MidlineLeft, true);
        Label(header, "Sub", "DAY 1 · 첫 영업을 마치고", 20, new Color(.82f, .93f, .90f), new Vector2(0, 0), new Vector2(1, 1), new Vector2(36, 0), new Vector2(-36, 6), TextAlignmentOptions.MidlineRight);

        _badge = Panel(_card, "RankBadge", new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, -330), new Vector2(244, -130), Teal, true).GetComponent<Image>();
        _rank = Label(_badge.transform, "Rank", "B", 128, Color.white, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, 8), TextAlignmentOptions.Center, true);
        Label(_card, "RankCaption", "개척 등급", 20, Soft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, -372), new Vector2(244, -336), TextAlignmentOptions.Center);
        _total = Label(_card, "Total", "", 30, Ink, new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, -430), new Vector2(244, -372), TextAlignmentOptions.Center);

        string[] names = { "정착", "상업", "발전", "탐험" };
        for (int i = 0; i < 4; i++)
        {
            float top = -140 - i * 70;
            Label(_card, "Area" + i, names[i], 24, Ink, new Vector2(0, 1), new Vector2(1, 1), new Vector2(290, top - 34), new Vector2(-40, top), TextAlignmentOptions.MidlineLeft, true);
            _values[i] = Label(_card, "Value" + i, "", 22, Soft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(290, top - 34), new Vector2(-40, top), TextAlignmentOptions.MidlineRight);
            var track = Panel(_card, "Track" + i, new Vector2(0, 1), new Vector2(1, 1), new Vector2(290, top - 58), new Vector2(-40, top - 40), Track, true);
            var fill = Panel(track, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, i == 1 ? new Color(.86f, .56f, .30f) : Teal, true).GetComponent<Image>();
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0f;
            _fills[i] = fill;
        }

        _summary = Label(_card, "Summary", "", 23, Ink, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -490), new Vector2(-40, -446), TextAlignmentOptions.Center);
        Panel(_card, "Rule", new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -500), new Vector2(-40, -498), Track, false);
        Label(_card, "CommentsHeading", "섬 소식 · 오늘의 반응", 22, Teal, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -548), new Vector2(-40, -512), TextAlignmentOptions.MidlineLeft, true);
        _comments = Panel(_card, "Comments", new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 96), new Vector2(-40, -556), new Color(0, 0, 0, 0), false);

        var close = Panel(_card, "Close", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-150, 26), new Vector2(150, 78), Teal, true);
        close.GetComponent<Image>().raycastTarget = true;
        var button = close.gameObject.AddComponent<Button>();
        button.onClick.AddListener(Hide);
        Label(close, "Label", "확인  ·  E / Esc", 24, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, true);
    }

    void Post(string key, int index)
    {
        (string handle, string text) = CommentPost(key);
        float top = -index * 58f;
        var row = Panel(_comments, "Post" + index, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, top - 52), new Vector2(0, top), new Color(1f, 1f, 1f, .72f), true);
        var avatar = Panel(row, "Avatar", new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(10, -18), new Vector2(46, 18), AvatarColor(index), true);
        Label(avatar, "Initial", handle.Substring(1, 1), 18, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, true);
        Label(row, "Text", $"<b><color=#1F5F62>{handle}</color></b>  {text}", 19, Ink, Vector2.zero, Vector2.one, new Vector2(58, 0), new Vector2(-12, 0), TextAlignmentOptions.MidlineLeft);
    }

    // Canon v2 §20.3: 행동 기반 규칙 댓글(LLM 없음).
    static (string, string) CommentPost(string key) => key switch
    {
        "settlement.established" => ("@P.A.본사", "상점과 텐트 두 채 확인! 첫 정착 인증"),
        "settlement.in_progress" => ("@P.A.본사", "아직 정착 중이네요. 내일은 텐트까지 세워 봐요."),
        "commerce.first_customers" => ("@밤장터_여행자", "섬 가게 첫 영업 다녀왔어요. 또 올게요!"),
        "commerce.no_sale" => ("@밤장터_여행자", "불은 켜졌는데 살 게 없었어요… 다음엔 꼭!"),
        "commerce.price_experiment" => ("@알뜰장바구니", "가격표가 계속 바뀌던데, 실험 중인가요?"),
        "development.shared_tools" => ("@섬마을_동료", "새 도구 받았어요! 내일은 두 배로 일할게요."),
        "development.chosen_path" => ("@P.A.본사", "전문 분야 선택 완료. 성장 경로가 열렸습니다."),
        "development.crafted_goods" => ("@섬마을_장인", "작업대에서 직접 만든 물건이 가판대에 올라왔네요!"),
        "exploration.curiosity" => ("@섬생물_도감", "낚시에 곤충 채집까지… 호기심 대장 인정!"),
        "exploration.traveller" => ("@섬지도_기록", "첫날부터 섬 곳곳을 누볐네요."),
        _ => ("@P.A.본사", key)
    };

    static string RankLetter(string key) => key switch { "rank.s" => "S", "rank.a" => "A", "rank.b" => "B", _ => "C" };

    static Color RankColor(string rank) => rank switch
    {
        "S" => new Color(.90f, .66f, .20f),
        "A" => new Color(.12f, .39f, .41f),
        "B" => new Color(.35f, .55f, .36f),
        _ => new Color(.66f, .46f, .36f)
    };

    static Color AvatarColor(int index) => index switch
    {
        0 => new Color(.12f, .39f, .41f), 1 => new Color(.86f, .56f, .30f), 2 => new Color(.35f, .55f, .36f), _ => new Color(.55f, .45f, .70f)
    };

    static RectTransform Panel(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color color, bool rounded)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        var image = go.GetComponent<Image>();
        image.color = color; image.raycastTarget = false;
        if (rounded) { image.sprite = PAUiTheme.RoundedSprite; image.type = Image.Type.Sliced; }
        return rt;
    }

    static TextMeshProUGUI Label(Transform parent, string name, string text, int size, Color color, Vector2 min, Vector2 max,
        Vector2 offsetMin, Vector2 offsetMax, TextAlignmentOptions alignment, bool bold = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text; label.fontSize = size; label.color = color; label.alignment = alignment;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.textWrappingMode = TextWrappingModes.Normal; label.raycastTarget = false;
        return label;
    }
}
