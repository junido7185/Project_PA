using UnityEngine;
using UnityEngine.UI;
using TMPro;

// §3 InteractPromptUI — 상호작용 가능한 오브젝트 근처에서 "[Space] {프롬프트}" 표시.
// 자동빌드 싱글톤 — MoneyHUD 패턴. 씬에 배치만 하면 동작.
public class InteractPromptUI : MonoBehaviour
{
    public static InteractPromptUI instance;

    [Header("직접 연결 (선택)")]
    public TextMeshProUGUI promptText;

    GameObject _panel;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (promptText == null) BuildUI();
        _panel.SetActive(false);
    }

    void BuildUI()
    {
        var uiRoot = GameObject.Find("PA_UIRoot");
        Transform parent;

        if (uiRoot != null)
        {
            parent = uiRoot.transform;
        }
        else
        {
            var canvasGO = new GameObject("InteractPrompt_Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas             = canvasGO.GetComponent<Canvas>();
            canvas.renderMode      = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder    = 60;
            var scaler             = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode     = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            parent = canvasGO.transform;
        }

        // 하단 중앙 패널
        _panel = new GameObject("InteractPromptPanel", typeof(RectTransform), typeof(Image));
        var rt  = (RectTransform)_panel.transform;
        rt.SetParent(parent, false);
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = new Vector2(400, 50);
        rt.anchoredPosition = new Vector2(0f, 120f);

        var bg        = _panel.GetComponent<Image>();
        bg.color      = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = false;

        var textGO = new GameObject("PromptText", typeof(TextMeshProUGUI));
        var trt    = (RectTransform)textGO.transform;
        trt.SetParent(_panel.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12f, 4f);
        trt.offsetMax = new Vector2(-12f, -4f);

        promptText              = textGO.GetComponent<TextMeshProUGUI>();
        promptText.text         = "[Space] 상호작용";
        promptText.fontSize     = 20;
        promptText.fontStyle    = FontStyles.Bold;
        promptText.color        = Color.white;
        promptText.alignment    = TextAlignmentOptions.Center;
        promptText.raycastTarget = false;
    }

    // PlayerInteraction.Update() 에서 호출
    public void SetPrompt(string prompt)
    {
        if (promptText != null) promptText.text = prompt;
        if (_panel != null) _panel.SetActive(true);
    }

    public void ClearPrompt()
    {
        if (_panel != null) _panel.SetActive(false);
    }
}
