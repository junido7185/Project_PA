using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 전시용 연출(항해·상륙) 공용 화면 오버레이. 레터박스, 타이틀 카드, 하단 자막, 길게 눌러 건너뛰기만 소유한다.
// 입력 권위(PlayerInputHandler)는 건드리지 않고 같은 Input System 장치 상태(E / Space / 패드 South)만 읽는다.
// GraphicRaycaster가 없어 클릭을 막지 않으며, 소유자가 파괴하면 함께 사라진다.
public sealed class VoyageCinematicOverlay : MonoBehaviour
{
    public const float BarHeight = 118f, SkipHoldSeconds = .8f;
    public static readonly Color FadeColor = new Color(.12f, .2f, .22f, 1f);
    static readonly Color BarColor = new Color(.055f, .075f, .085f, 1f);
    static readonly Color Paper = new Color(.98f, .95f, .87f, 1f);

    public Canvas Canvas { get; private set; }
    public float Letterbox { get; private set; }
    public float LetterboxTarget { get; set; }
    public float LetterboxSeconds { get; set; } = .8f;
    public bool SkipEnabled { get; set; } = true;
    public bool SkipRequested { get; private set; }
    public float SkipProgress { get; private set; }
    public bool TitleVisible => _cardTarget > 0f;
    public string CurrentLine => _line != null ? _line.text : string.Empty;
    public float FadeAlpha => _fade != null ? _fade.color.a : 0f;

    TMP_FontAsset _font;
    RectTransform _top, _bottom, _card, _tagPill, _skipFill;
    CanvasGroup _cardGroup, _lineGroup, _topGroup, _skipGroup;
    TextMeshProUGUI _header, _tag, _title, _caption, _speaker, _line;
    Image _fade;
    float _cardAlpha, _cardTarget, _lineAlpha, _lineTarget, _skipAlpha;
    string _nextSpeaker, _nextLine;
    Color _nextAccent;
    bool _swap, _armed;

    public static VoyageCinematicOverlay Create(Transform parent, string name, int sortingOrder, string header, TMP_FontAsset font = null)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        if (parent != null) root.transform.SetParent(parent, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        var overlay = root.AddComponent<VoyageCinematicOverlay>();
        overlay.Canvas = canvas;
        overlay.Build(header, font != null ? font : TMP_Settings.defaultFontAsset);
        return overlay;
    }

    public static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
    public static float Smoother(float t) { t = Mathf.Clamp01(t); return t * t * t * (t * (t * 6f - 15f) + 10f); }

    public void SetLetterboxImmediate(float amount)
    {
        Letterbox = LetterboxTarget = Mathf.Clamp01(amount);
        ApplyBars();
    }

    public void SetFade(float alpha)
    {
        if (_fade == null) return;
        var color = FadeColor; color.a = Mathf.Clamp01(alpha);
        _fade.color = color;
    }

    public void SetHeader(string header) { if (_header != null) _header.text = header; }

    public void ShowTitle(string tag, string title, string caption)
    {
        _tag.text = tag ?? string.Empty; _title.text = title ?? string.Empty; _caption.text = caption ?? string.Empty;
        _tagPill.gameObject.SetActive(_tag.text.Length > 0);
        _tagPill.sizeDelta = new Vector2(Mathf.Max(120f, _tag.GetPreferredValues(_tag.text).x + 48f), 40f);
        _cardTarget = 1f;
    }

    public void HideTitle() => _cardTarget = 0f;

    // 자막이 보이는 중이면 짧게 사라졌다가 새 줄로 바뀐다.
    public void ShowLine(string speaker, string line, Color accent)
    {
        if (_lineAlpha <= .01f && !_swap) { ApplyLine(speaker, line, accent); _lineTarget = 1f; return; }
        _nextSpeaker = speaker; _nextLine = line; _nextAccent = accent; _swap = true; _lineTarget = 0f;
    }

    public void HideLine() { _swap = false; _lineTarget = 0f; }

    void ApplyLine(string speaker, string line, Color accent)
    {
        _speaker.text = speaker ?? string.Empty;
        _speaker.color = Color.Lerp(accent, Color.white, .35f);
        _line.text = line ?? string.Empty;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        Letterbox = Mathf.MoveTowards(Letterbox, Mathf.Clamp01(LetterboxTarget), dt / Mathf.Max(.05f, LetterboxSeconds));
        ApplyBars();

        _cardAlpha = Mathf.MoveTowards(_cardAlpha, _cardTarget, dt / .45f);
        float card = Ease(_cardAlpha);
        _cardGroup.alpha = card;
        _card.anchoredPosition = new Vector2(0f, -(BarHeight + 40f) - (1f - card) * 22f);
        _card.localScale = Vector3.one * Mathf.Lerp(.965f, 1f, card);

        if (_swap && _lineAlpha <= 0f)
        {
            ApplyLine(_nextSpeaker, _nextLine, _nextAccent);
            _swap = false; _lineTarget = 1f;
        }
        _lineAlpha = Mathf.MoveTowards(_lineAlpha, _lineTarget, dt / .22f);
        // 자막은 아래 띠 위에서만 읽히게 띠가 들어온 만큼만 보인다.
        _lineGroup.alpha = Ease(_lineAlpha) * Ease(Letterbox);

        UpdateSkip(dt);
    }

    void ApplyBars()
    {
        if (_top == null) return;
        float bars = Ease(Letterbox) * BarHeight;
        _top.sizeDelta = new Vector2(0f, bars);
        _bottom.sizeDelta = new Vector2(0f, bars);
        if (_topGroup != null) _topGroup.alpha = Ease(Letterbox);
    }

    // 시작 순간 이미 눌려 있던 키(출항 확정·앞 연출 건너뛰기)는 한 번 뗀 뒤에만 센다.
    void UpdateSkip(float dt)
    {
        var keyboard = Keyboard.current;
        var pad = Gamepad.current;
        bool held = keyboard != null && (keyboard.eKey.isPressed || keyboard.spaceKey.isPressed) ||
                    pad != null && pad.buttonSouth.isPressed;
        if (!held) _armed = true;
        if (!SkipRequested)
        {
            if (SkipEnabled && _armed && held) SkipProgress = Mathf.Min(1f, SkipProgress + dt / SkipHoldSeconds);
            else SkipProgress = Mathf.MoveTowards(SkipProgress, 0f, dt * 2.5f);
            if (SkipProgress >= 1f) SkipRequested = true;
        }
        _skipFill.anchorMax = new Vector2(SkipProgress, 1f);
        _skipAlpha = Mathf.MoveTowards(_skipAlpha, SkipEnabled && !SkipRequested ? 1f : 0f, dt / .3f);
        _skipGroup.alpha = _skipAlpha;
    }

    void Build(string header, TMP_FontAsset font)
    {
        _font = font;
        _top = Rect("LetterboxTop", transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), Vector2.zero, new Vector2(0, 0));
        Fill(_top, BarColor);
        _bottom = Rect("LetterboxBottom", transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 0));
        Fill(_bottom, BarColor);

        // 위 띠: 회사 태그(왼쪽)와 건너뛰기 안내(오른쪽).
        var topContent = Rect("TopContent", _top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), Vector2.zero, new Vector2(0, BarHeight));
        _topGroup = topContent.gameObject.AddComponent<CanvasGroup>();
        _topGroup.interactable = false; _topGroup.blocksRaycasts = false;
        _header = Text("Header", topContent, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(56, 0), new Vector2(980, 40),
            header, 21, new Color(Paper.r, Paper.g, Paper.b, .82f), TextAlignmentOptions.MidlineLeft);
        _header.characterSpacing = 2f;
        var skip = Rect("SkipHint", topContent, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-56, 0), new Vector2(520, 60));
        _skipGroup = skip.gameObject.AddComponent<CanvasGroup>();
        _skipGroup.interactable = false; _skipGroup.blocksRaycasts = false; _skipGroup.alpha = 0f;
        Text("Label", skip, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, -6), new Vector2(520, 30),
            "E / Space 길게 눌러 건너뛰기", 19, new Color(Paper.r, Paper.g, Paper.b, .78f), TextAlignmentOptions.MidlineRight);
        var track = Rect("Track", skip, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 12), new Vector2(230, 5));
        Fill(track, new Color(Paper.r, Paper.g, Paper.b, .2f));
        _skipFill = Rect("Fill", track, Vector2.zero, new Vector2(0, 1), new Vector2(0, .5f), Vector2.zero, Vector2.zero);
        _skipFill.offsetMin = _skipFill.offsetMax = Vector2.zero;
        Fill(_skipFill, PAUiTheme.Gold);

        // 아래 띠: 화자(직업 색)와 대사.
        var lines = Rect("Subtitle", _bottom, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(1640, BarHeight));
        _lineGroup = lines.gameObject.AddComponent<CanvasGroup>();
        _lineGroup.interactable = false; _lineGroup.blocksRaycasts = false; _lineGroup.alpha = 0f;
        _speaker = Text("Speaker", lines, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -14), new Vector2(1600, 32),
            "", 21, PAUiTheme.Gold, TextAlignmentOptions.Center);
        _speaker.characterSpacing = 1.5f;
        _line = Text("Line", lines, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -48), new Vector2(1600, 54),
            "", 33, Paper, TextAlignmentOptions.Center);
        _line.enableAutoSizing = true; _line.fontSizeMin = 24f; _line.fontSizeMax = 33f;
        _line.textWrappingMode = TextWrappingModes.NoWrap;

        // 타이틀 카드: 크림 카드 + 청록 태그 + 잉크 제목(잘난체 기본 TMP 글꼴).
        _card = Rect("TitleCard", transform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1),
            new Vector2(0, -(BarHeight + 40f)), new Vector2(860, 214));
        PAUiTheme.Surface(_card.gameObject.AddComponent<Image>(), new Color(Paper.r, Paper.g, Paper.b, .96f));
        _card.GetComponent<Image>().raycastTarget = false;
        _cardGroup = _card.gameObject.AddComponent<CanvasGroup>();
        _cardGroup.interactable = false; _cardGroup.blocksRaycasts = false; _cardGroup.alpha = 0f;
        _tagPill = Rect("Tag", _card, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -24), new Vector2(160, 40));
        PAUiTheme.Surface(_tagPill.gameObject.AddComponent<Image>(), PAUiTheme.Teal);
        _tagPill.GetComponent<Image>().raycastTarget = false;
        _tag = Text("Label", _tagPill, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero,
            "", 21, Paper, TextAlignmentOptions.Center);
        _tag.rectTransform.offsetMin = _tag.rectTransform.offsetMax = Vector2.zero;
        _tag.characterSpacing = 2f;
        _title = Text("Title", _card, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -70), new Vector2(820, 84),
            "", 68, PAUiTheme.Ink, TextAlignmentOptions.Center);
        _title.enableAutoSizing = true; _title.fontSizeMin = 44f; _title.fontSizeMax = 68f;
        _title.textWrappingMode = TextWrappingModes.NoWrap;
        var rule = Rect("Rule", _card, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -160), new Vector2(120, 4));
        Fill(rule, PAUiTheme.Gold);
        _caption = Text("Caption", _card, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -170), new Vector2(820, 32),
            "", 21, PAUiTheme.Teal, TextAlignmentOptions.Center);
        _caption.characterSpacing = 1.5f;

        // 자체 페이드(상륙 연출용). 항해는 기존 HUD 페이드를 쓰므로 0으로 둔다.
        var fade = Rect("Fade", transform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        fade.offsetMin = fade.offsetMax = Vector2.zero;
        _fade = fade.gameObject.AddComponent<Image>();
        _fade.raycastTarget = false;
        SetFade(0f);
        ApplyBars();
    }

    static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = pivot;
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    static void Fill(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = false;
    }

    TextMeshProUGUI Text(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size,
        string text, float fontSize, Color color, TextAlignmentOptions alignment)
        => Text(name, parent, anchorMin, anchorMax, anchorMin, position, size, text, fontSize, color, alignment);

    TextMeshProUGUI Text(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size,
        string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        var rect = Rect(name, parent, anchorMin, anchorMax, pivot, position, size);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) label.font = _font;
        label.text = text; label.fontSize = fontSize; label.color = color;
        label.alignment = alignment; label.raycastTarget = false;
        return label;
    }
}
