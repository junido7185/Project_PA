using UnityEngine;
using UnityEngine.UI;
using TMPro;

// §6 PauseManager — ESC 일시정지. TimeScale=0 / 자동빌드 오버레이 패널.
// PlayerInputHandler.OnPauseToggle 구독.
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    public bool IsPaused { get; private set; }

    GameObject _overlay;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildOverlay();
    }

    void Start()
    {
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnPauseToggle += Toggle;
    }

    void OnDestroy()
    {
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnPauseToggle -= Toggle;
    }

    public void Toggle()
    {
        if (IsPaused) Resume(); else Pause();
    }

    public void Pause()
    {
        IsPaused          = true;
        Time.timeScale    = 0f;
        if (_overlay != null) _overlay.SetActive(true);
    }

    public void Resume()
    {
        IsPaused          = false;
        Time.timeScale    = 1f;
        if (_overlay != null) _overlay.SetActive(false);
    }

    void BuildOverlay()
    {
        var uiRoot = GameObject.Find("PA_UIRoot");
        Transform parent;

        if (uiRoot != null)
        {
            parent = uiRoot.transform;
        }
        else
        {
            var canvasGO = new GameObject("PauseMenu_Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas          = canvasGO.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            var scaler          = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode  = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            parent = canvasGO.transform;
        }

        // 반투명 전체화면 오버레이
        _overlay = new GameObject("PauseOverlay", typeof(RectTransform), typeof(Image));
        var rt   = (RectTransform)_overlay.transform;
        rt.SetParent(parent, false);
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
        _overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        // "일시정지" 텍스트
        var textGO = new GameObject("PauseLabel", typeof(TextMeshProUGUI));
        var trt    = (RectTransform)textGO.transform;
        trt.SetParent(_overlay.transform, false);
        trt.anchorMin        = new Vector2(0.3f, 0.55f);
        trt.anchorMax        = new Vector2(0.7f, 0.7f);
        trt.offsetMin        = Vector2.zero;
        trt.offsetMax        = Vector2.zero;
        var tmp              = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text             = "일시정지";
        tmp.fontSize         = 52;
        tmp.fontStyle        = FontStyles.Bold;
        tmp.color            = Color.white;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.raycastTarget    = false;

        // 재개 버튼
        var btnGO = new GameObject("ResumeButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var brt   = (RectTransform)btnGO.transform;
        brt.SetParent(_overlay.transform, false);
        brt.anchorMin        = new Vector2(0.4f, 0.4f);
        brt.anchorMax        = new Vector2(0.6f, 0.5f);
        brt.offsetMin        = Vector2.zero;
        brt.offsetMax        = Vector2.zero;
        btnGO.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.3f);

        var btnLabel = new GameObject("Label", typeof(TextMeshProUGUI));
        var blrt     = (RectTransform)btnLabel.transform;
        blrt.SetParent(btnGO.transform, false);
        blrt.anchorMin = Vector2.zero;
        blrt.anchorMax = Vector2.one;
        blrt.offsetMin = Vector2.zero;
        blrt.offsetMax = Vector2.zero;
        var btnTmp       = btnLabel.GetComponent<TextMeshProUGUI>();
        btnTmp.text      = "계속하기 (ESC)";
        btnTmp.fontSize  = 22;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.color     = Color.white;
        btnTmp.alignment = TextAlignmentOptions.Center;

        btnGO.GetComponent<Button>().onClick.AddListener(Resume);

        _overlay.SetActive(false);
    }
}
