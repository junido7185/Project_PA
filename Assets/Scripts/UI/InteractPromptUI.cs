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

    // PlayerInteraction.Update() 에서 호출. anchor가 있으면(대상) 그 위에 말풍선처럼 붙여 띄운다(레퍼런스 13·11 — 대상 옆 안내).
    public void SetPrompt(string prompt, Transform anchor = null)
    {
        if (promptText != null) promptText.text = prompt;
        _wanted = true;
        _anchor = anchor;
        ApplyVisibility();
    }

    // 2026-11-04 대상 위 안내: 대상 몸 높이(최대 2m) + .35m 위 화면 점에 띄운다. 대상이 없으면 원래 자리(화면 아래)로.
    Transform _anchor, _measured;
    float _anchorHeight;
    bool _floating;
    Vector2 _homeMin, _homeMax, _homePos, _homePivot, _homeSize;
    public bool Floating => _floating;
    public Transform Anchor => _anchor;

    void PlaceNearAnchor()
    {
        var rt = (RectTransform)_panel.transform;
        var cam = Camera.main;
        var canvas = _panel.GetComponentInParent<Canvas>();
        bool can = _anchor != null && cam != null && canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay;
        Vector3 screen = Vector3.zero;
        if (can)
        {
            if (_measured != _anchor)
            {
                _measured = _anchor; _anchorHeight = 1.2f;
                var renderers = _anchor.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    float top = float.MinValue;
                    foreach (var r in renderers) if (r.enabled) top = Mathf.Max(top, r.bounds.max.y);
                    if (top > float.MinValue) _anchorHeight = top - _anchor.position.y;
                }
                // 플레이어 머리(1.75m)를 덮지 않게 최소 2.05m, 큰 나무·건물은 2.5m까지만.
                _anchorHeight = Mathf.Clamp(_anchorHeight, 2.05f, 2.5f);
            }
            screen = cam.WorldToScreenPoint(_anchor.position + Vector3.up * (_anchorHeight + .2f));
            can = screen.z > 0f;
        }
        if (!can)
        {
            if (_floating) { rt.anchorMin = _homeMin; rt.anchorMax = _homeMax; rt.pivot = _homePivot; rt.anchoredPosition = _homePos; rt.sizeDelta = _homeSize; _floating = false; }
            return;
        }
        if (!_floating) { _homeMin = rt.anchorMin; _homeMax = rt.anchorMax; _homePos = rt.anchoredPosition; _homePivot = rt.pivot; _homeSize = rt.sizeDelta; _floating = true; }
        // 말풍선은 글자 폭에 맞춘 작은 상자(레퍼런스 13의 대상 옆 안내처럼).
        if (promptText != null) rt.sizeDelta = new Vector2(Mathf.Clamp(promptText.GetPreferredValues(promptText.text).x + 48f, 180f, 640f), _homeSize.y);
        float scale = Mathf.Max(.01f, canvas.scaleFactor);
        Vector2 size = rt.rect.size, screenSize = new Vector2(Screen.width, Screen.height) / scale;
        Vector2 p = new Vector2(screen.x, screen.y) / scale;
        // 핫바(아래 ~130)와 위쪽 HUD(시계·알림 ~190)를 피해 화면 안에 둔다.
        p.x = Mathf.Clamp(p.x, size.x * .5f + 16f, screenSize.x - size.x * .5f - 16f);
        p.y = Mathf.Clamp(p.y, 140f, screenSize.y - size.y - 200f);
        rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(.5f, 0f);
        rt.anchoredPosition = p;
    }

    public void ClearPrompt()
    {
        _wanted = false;
        _anchor = null;
        ApplyVisibility();
    }

    // D2 — 테마 경로에서 대화창이 실제로 떠 있는 동안만 프롬프트를 가린다(하단 겹침 방지).
    // 요청 상태(_wanted)는 PlayerInteraction 이 그대로 소유하고, 대화창이 닫히면 자동 복원된다.
    bool _wanted;

    static bool SuppressedByDialogue => DialogueUI.IsOpen && PAUiTheme.Active;

    void LateUpdate()
    {
        ApplyVisibility();
        if (_panel != null && _panel.activeSelf) PlaceNearAnchor();
    }

    void ApplyVisibility()
    {
        if (_panel == null) return;
        bool visible = _wanted && !SuppressedByDialogue;
        if (_panel.activeSelf != visible) _panel.SetActive(visible);
    }
}
