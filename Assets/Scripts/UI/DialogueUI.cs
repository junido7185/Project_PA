using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// §3 DialogueUI — 하단 바 대화창. NpcDialogue.ShowLine() 에서 호출.
// 자동빌드 싱글톤 — MoneyHUD 패턴.
// Show(name, line) → 타이핑 효과 → autoHide 후 자동 닫힘.
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI instance;

    [Header("직접 연결 (선택)")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI bodyText;

    [Header("설정")]
    public float typeSpeed  = 0.03f; // 글자당 딜레이(초)
    public float autoHide   = 4f;    // 마지막 글자 출력 후 자동 숨김(초)

    GameObject _panel;
    Coroutine  _typingCoroutine;
    Coroutine  _hideCoroutine;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (nameText == null || bodyText == null) BuildUI();
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
            var canvasGO = new GameObject("DialogueUI_Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas             = canvasGO.GetComponent<Canvas>();
            canvas.renderMode      = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder    = 80;
            var scaler             = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode     = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            parent = canvasGO.transform;
        }

        // 하단 바 패널
        _panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
        var rt  = (RectTransform)_panel.transform;
        rt.SetParent(parent, false);
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = new Vector2(0f, 160f);
        rt.anchoredPosition = Vector2.zero;

        var bg   = _panel.GetComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.88f);
        bg.raycastTarget = false;

        // NPC 이름 라벨 (상단)
        var nameGO = new GameObject("NpcNameText", typeof(TextMeshProUGUI));
        var nameRT = (RectTransform)nameGO.transform;
        nameRT.SetParent(_panel.transform, false);
        nameRT.anchorMin        = new Vector2(0f, 1f);
        nameRT.anchorMax        = new Vector2(1f, 1f);
        nameRT.pivot            = new Vector2(0f, 1f);
        nameRT.sizeDelta        = new Vector2(0f, 36f);
        nameRT.anchoredPosition = new Vector2(20f, 0f);

        nameText              = nameGO.GetComponent<TextMeshProUGUI>();
        nameText.fontSize     = 18;
        nameText.fontStyle    = FontStyles.Bold;
        nameText.color        = new Color(1f, 0.85f, 0.4f);
        nameText.raycastTarget = false;

        // 대사 본문 (중하단)
        var bodyGO = new GameObject("DialogueBodyText", typeof(TextMeshProUGUI));
        var bodyRT = (RectTransform)bodyGO.transform;
        bodyRT.SetParent(_panel.transform, false);
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(20f, 12f);
        bodyRT.offsetMax = new Vector2(-20f, -40f);

        bodyText              = bodyGO.GetComponent<TextMeshProUGUI>();
        bodyText.fontSize     = 22;
        bodyText.color        = Color.white;
        bodyText.alignment    = TextAlignmentOptions.TopLeft;
        bodyText.raycastTarget = false;
    }

    // NpcDialogue.ShowLine() 에서 호출
    public void Show(string npcName, string line)
    {
        if (_panel == null) return;
        _panel.SetActive(true);

        if (nameText != null) nameText.text = npcName;

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        if (_hideCoroutine   != null) StopCoroutine(_hideCoroutine);
        _typingCoroutine = StartCoroutine(TypeLine(line));
    }

    IEnumerator TypeLine(string line)
    {
        if (bodyText == null) yield break;
        bodyText.text = "";
        foreach (char c in line)
        {
            bodyText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        _hideCoroutine = StartCoroutine(AutoHide());
    }

    IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(autoHide);
        if (_panel != null) _panel.SetActive(false);
    }

    // Space 키 등으로 즉시 닫기
    public void Hide()
    {
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        if (_hideCoroutine   != null) StopCoroutine(_hideCoroutine);
        if (_panel != null) _panel.SetActive(false);
    }
}
