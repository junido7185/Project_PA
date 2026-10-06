using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// FishingSpot owns the catch. This panel measures reel control; it never grants inventory.
public sealed class FirstDayFishingMinigame : MonoBehaviour
{
    public static FirstDayFishingMinigame Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance._active;
    public float FishPosition { get; private set; }
    public float BandPosition { get; private set; }
    public float Progress { get; private set; }
    public const float BandSize = .30f;

    GameObject _canvas;
    RectTransform _band, _fish, _fill;
    TextMeshProUGUI _status;
    Action<bool> _finished;
    bool _active;
    float _elapsed, _velocity, _seed;
    static readonly Color Ink = PAUiTheme.Ink;
    static readonly Color Teal = PAUiTheme.Teal;

    public void Open(Item item, Action<bool> finished)
    {
        if (IsOpen) return;
        if (_canvas == null) Build();
        Instance = this;
        _finished = finished; _elapsed = 0; _velocity = 0;
        _seed = UnityEngine.Random.Range(0f, 6f);
        BandPosition = FishPosition = .5f; Progress = .30f;
        _active = true;
        _canvas.SetActive(true);
        _fish.GetComponent<Image>().sprite = item != null ? item.icon : null;
        Paint();
        PlayerInputHandler.RestoreGameplayCursor();
    }

    void Update()
    {
        if (!_active || Time.timeScale == 0) return;
        float dt = Mathf.Min(Time.deltaTime, .05f);
        _elapsed += dt;
        bool held = PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.FishingReelHeld;
        _velocity = Mathf.MoveTowards(_velocity, held ? .62f : -.50f, dt * 2.4f);
        BandPosition = Mathf.Clamp(BandPosition + _velocity * dt, BandSize / 2, 1 - BandSize / 2);
        if (BandPosition == BandSize / 2 || BandPosition == 1 - BandSize / 2) _velocity = 0;
        FishPosition = Mathf.Clamp(.5f + .20f * Mathf.Sin(_elapsed * .75f) +
            .08f * Mathf.Sin(_elapsed * 1.8f + _seed), .12f, .88f);
        bool inBand = Mathf.Abs(FishPosition - BandPosition) <= BandSize / 2;
        Progress = Mathf.Clamp01(Progress + dt * (inBand ? .16f : -.10f));
        _status.text = inBand ? "좋아요! 물고기를 따라가세요" : "초록 영역 안에 물고기를 두세요";
        _band.GetComponent<Image>().color = inBand ? new Color(.39f, .73f, .46f) : new Color(.62f, .74f, .54f);
        Paint();
        if (Progress >= 1) Complete(true);
        else if (Progress <= 0 || _elapsed >= 35) Complete(false);
    }

    void Paint()
    {
        _band.anchoredPosition = new Vector2(0, BandPosition * 300);
        _fish.anchoredPosition = new Vector2(0, FishPosition * 300);
        _fill.anchorMax = new Vector2(1, Progress);
    }

    public void Cancel() => Complete(false);
    public void CloseSilently()
    {
        _active = false; _finished = null;
        if (_canvas != null) _canvas.SetActive(false);
    }
    void Complete(bool caught)
    {
        if (!_active) return;
        var callback = _finished;
        CloseSilently();
        callback?.Invoke(caught);
    }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Build()
    {
        _canvas = new GameObject("FishingReelCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas.transform.SetParent(transform, false);
        var canvas = _canvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 75;
        var scaler = _canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var inputShield = Box(_canvas.transform, "InputShield", Vector2.zero, Vector2.zero, Vector2.zero, Color.clear);
        inputShield.anchorMin = Vector2.zero; inputShield.anchorMax = Vector2.one;
        inputShield.offsetMin = inputShield.offsetMax = Vector2.zero;
        inputShield.GetComponent<Image>().raycastTarget = true;
        var card = Box(_canvas.transform, "ReelCard", new Vector2(.79f, .5f),
            new Vector2(440, 660), Vector2.zero, new Color(.98f, .95f, .86f));
        var header = Box(card, "Header", new Vector2(.5f, 1), new Vector2(440, 80), new Vector2(0, -40), Teal);
        Label(header, "Title", "물고기 끌어올리기", 30, Color.white, new Vector2(0, 0), new Vector2(410, 64));
        Label(card, "HowTo", "[Space / 마우스 왼쪽]\n누르면 ↑ · 놓으면 ↓", 20, Ink, new Vector2(0, 218), new Vector2(410, 64));
        var track = Box(card, "Track", new Vector2(.5f, .5f), new Vector2(100, 324), new Vector2(-55, -24), new Color(.12f, .30f, .34f));
        var lane = Box(track, "Lane", new Vector2(.5f, 0), new Vector2(88, 300), new Vector2(0, 12), Color.clear);
        lane.pivot = new Vector2(.5f, 0);
        _band = Box(lane, "ControlBand", new Vector2(.5f, 0), new Vector2(82, 300 * BandSize), Vector2.zero, Teal);
        _fish = Box(lane, "Fish", new Vector2(.5f, 0), new Vector2(70, 46), Vector2.zero, Color.white);
        _fish.GetComponent<Image>().preserveAspect = true;
        _fish.GetComponent<Image>().type = Image.Type.Simple;
        var progress = Box(card, "ProgressTrack", new Vector2(.5f, .5f), new Vector2(30, 324), new Vector2(54, -24), new Color(.80f, .82f, .73f));
        _fill = Box(progress, "ProgressFill", Vector2.zero, Vector2.zero, Vector2.zero, new Color(.94f, .65f, .25f));
        _fill.anchorMin = Vector2.zero; _fill.anchorMax = new Vector2(1, .3f);
        _fill.offsetMin = new Vector2(3, 3); _fill.offsetMax = new Vector2(-3, -3);
        Label(card, "ProgressCaption", "성공", 20, Ink, new Vector2(54, 165), new Vector2(60, 28));
        _status = Label(card, "Status", "", 22, Ink, new Vector2(0, -230), new Vector2(410, 50));
        Label(card, "Next", "끌어올린 뒤 도끼·곡괭이로 포획하세요\n[Esc] 낚시 취소", 21, Ink, new Vector2(0, -285), new Vector2(410, 65));
    }

    static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 position, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor; rt.sizeDelta = size; rt.anchoredPosition = position;
        var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
        image.sprite = PAUiTheme.RoundedSprite; image.type = Image.Type.Sliced;
        return rt;
    }
    static TextMeshProUGUI Label(Transform parent, string name, string text, int size, Color color, Vector2 position, Vector2 dimensions)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.sizeDelta = dimensions; rt.anchoredPosition = position;
        var label = go.GetComponent<TextMeshProUGUI>(); label.font = TMP_Settings.defaultFontAsset;
        label.text = text; label.fontSize = size; label.color = color;
        label.alignment = TextAlignmentOptions.Center; label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return label;
    }
}
