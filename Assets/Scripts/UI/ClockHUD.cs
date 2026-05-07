using UnityEngine;
using UnityEngine.UI;
using TMPro;

// §6 ClockHUD — 화면 좌상단 시각·계절 표시. MoneyHUD 패턴으로 자동빌드.
// GameClock.OnHourTick / OnNewDay 구독 → 시각·날짜 갱신.
public class ClockHUD : MonoBehaviour
{
    public static ClockHUD instance;

    [Header("직접 연결 (선택)")]
    public TextMeshProUGUI clockText;
    public TextMeshProUGUI dateText;

    Canvas _ownCanvas;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (clockText == null || dateText == null) BuildHUD();
    }

    void BuildHUD()
    {
        var uiRoot = GameObject.Find("PA_UIRoot");
        Transform parent;

        if (uiRoot != null)
        {
            parent = uiRoot.transform;
        }
        else
        {
            var canvasGO = new GameObject("ClockHUD_Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _ownCanvas                 = canvasGO.GetComponent<Canvas>();
            _ownCanvas.renderMode      = RenderMode.ScreenSpaceOverlay;
            _ownCanvas.sortingOrder    = 50;
            var scaler                 = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            parent = canvasGO.transform;
        }

        // 좌상단 패널
        var hudGO = new GameObject("ClockHudPanel", typeof(RectTransform), typeof(Image));
        var rt    = (RectTransform)hudGO.transform;
        rt.SetParent(parent, false);
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.sizeDelta        = new Vector2(200, 70);
        rt.anchoredPosition = new Vector2(20, -20);

        var bg   = hudGO.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);
        bg.raycastTarget = false;

        // 시각 텍스트 (상단)
        var clockGO  = new GameObject("ClockText", typeof(TextMeshProUGUI));
        var clockRT  = (RectTransform)clockGO.transform;
        clockRT.SetParent(hudGO.transform, false);
        clockRT.anchorMin = Vector2.zero;
        clockRT.anchorMax = Vector2.one;
        clockRT.offsetMin = new Vector2(10, 36);
        clockRT.offsetMax = new Vector2(-10, -4);

        clockText           = clockGO.GetComponent<TextMeshProUGUI>();
        clockText.text      = "07:00";
        clockText.fontSize  = 22;
        clockText.fontStyle = FontStyles.Bold;
        clockText.color     = Color.white;
        clockText.alignment = TextAlignmentOptions.Left;
        clockText.raycastTarget = false;

        // 날짜·계절 텍스트 (하단)
        var dateGO  = new GameObject("DateText", typeof(TextMeshProUGUI));
        var dateRT  = (RectTransform)dateGO.transform;
        dateRT.SetParent(hudGO.transform, false);
        dateRT.anchorMin = Vector2.zero;
        dateRT.anchorMax = Vector2.one;
        dateRT.offsetMin = new Vector2(10, 4);
        dateRT.offsetMax = new Vector2(-10, -38);

        dateText           = dateGO.GetComponent<TextMeshProUGUI>();
        dateText.text      = "Day 1 · 봄";
        dateText.fontSize  = 15;
        dateText.color     = new Color(0.8f, 0.8f, 0.8f);
        dateText.alignment = TextAlignmentOptions.Left;
        dateText.raycastTarget = false;
    }

    void Start()
    {
        if (GameClock.Instance != null)
        {
            GameClock.Instance.OnHourTick += OnHourTick;
            GameClock.Instance.OnNewDay   += OnNewDay;
            Refresh(GameClock.Instance.CurrentHour, GameClock.Instance.CurrentDay,
                    GameClock.Instance.CurrentSeason);
        }
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null)
        {
            GameClock.Instance.OnHourTick -= OnHourTick;
            GameClock.Instance.OnNewDay   -= OnNewDay;
        }
    }

    void OnHourTick(int hour)
    {
        if (GameClock.Instance != null)
            Refresh(GameClock.Instance.CurrentHour, GameClock.Instance.CurrentDay,
                    GameClock.Instance.CurrentSeason);
    }

    void OnNewDay(int day)
    {
        if (GameClock.Instance != null)
            Refresh(GameClock.Instance.CurrentHour, day, GameClock.Instance.CurrentSeason);
    }

    void Refresh(float hour, int day, Season season)
    {
        int h = Mathf.FloorToInt(hour);
        int m = Mathf.FloorToInt((hour - h) * 60f);

        if (clockText != null) clockText.text = $"{h:D2}:{m:D2}";
        if (dateText  != null) dateText.text  = $"Day {day} · {SeasonName(season)}";
    }

    static string SeasonName(Season s) => s switch
    {
        Season.Spring => "봄",
        Season.Summer => "여름",
        Season.Autumn => "가을",
        Season.Winter => "겨울",
        _             => "봄",
    };
}
