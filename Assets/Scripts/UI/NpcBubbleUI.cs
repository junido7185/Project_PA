using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// §4 NpcBubbleUI — NPC 머리 위 피드백 말풍선.
// NPC GameObject 의 자식으로 배치한다. 화면 공간에서 NPC 위치를 따라가며 최종 시연 가독성을 보장한다.
// Show(text, duration) — 일반 대사 말풍선.
// ShowReaction(bought, price, basePrice) — CustomerReaction 통합 (구매결정 반응).
[RequireComponent(typeof(Canvas))]
public class NpcBubbleUI : MonoBehaviour
{
    [Header("오프셋 (NPC 머리 위)")]
    public Vector3 offset = new Vector3(0f, 2.4f, 0f);

    [Header("직접 연결 (선택)")]
    public TextMeshProUGUI bubbleText;
    [Tooltip("Task 031 — 구매/거절 본문과 분리된 주민/관광객 표시 태그")]
    public TextMeshProUGUI customerClassText;

    Canvas   _canvas;
    Image    _bg;
    Coroutine _hideCoroutine;
    RectTransform _rectTransform;
    RectTransform _bubbleRoot;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder  = 260;

        _rectTransform = GetComponent<RectTransform>();
        _rectTransform.localScale   = Vector3.one;

        if (bubbleText == null) BuildBubble();
        else _bubbleRoot = bubbleText.transform.parent as RectTransform;
        EnsureCustomerClassTag();

        UpdateScreenPosition();
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        UpdateScreenPosition();
    }

    void BuildBubble()
    {
        var rootGO = new GameObject("BubblePanel", typeof(RectTransform), typeof(Image));
        rootGO.transform.SetParent(transform, false);
        _bubbleRoot = (RectTransform)rootGO.transform;
        _bubbleRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _bubbleRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _bubbleRoot.pivot = new Vector2(0.5f, 0.5f);
        _bubbleRoot.sizeDelta = new Vector2(360f, 112f);

        // 배경
        var bgGO = rootGO;
        var bgRT        = (RectTransform)bgGO.transform;
        _bg             = bgGO.GetComponent<Image>();
        _bg.color       = new Color(1f, 1f, 1f, 0.92f);

        // 텍스트
        var textGO = new GameObject("BubbleText", typeof(TextMeshProUGUI));
        textGO.transform.SetParent(rootGO.transform, false);
        var textRT     = (RectTransform)textGO.transform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(8f, 6f);
        textRT.offsetMax = new Vector2(-8f, -28f);

        bubbleText           = textGO.GetComponent<TextMeshProUGUI>();
        bubbleText.fontSize  = 21;
        bubbleText.color     = new Color(0.1f, 0.1f, 0.1f);
        bubbleText.alignment = TextAlignmentOptions.Center;
        bubbleText.textWrappingMode = TextWrappingModes.Normal;
        bubbleText.overflowMode = TextOverflowModes.Ellipsis;
        bubbleText.raycastTarget = false;
    }

    void EnsureCustomerClassTag()
    {
        if (_bubbleRoot == null || customerClassText != null) return;

        var tagGO = new GameObject("CustomerClassTag", typeof(RectTransform), typeof(TextMeshProUGUI));
        tagGO.transform.SetParent(_bubbleRoot, false);
        var tagRT = (RectTransform)tagGO.transform;
        tagRT.anchorMin = new Vector2(0f, 1f);
        tagRT.anchorMax = new Vector2(1f, 1f);
        tagRT.pivot = new Vector2(0.5f, 1f);
        tagRT.anchoredPosition = new Vector2(0f, -4f);
        tagRT.sizeDelta = new Vector2(-16f, 22f);

        customerClassText = tagGO.GetComponent<TextMeshProUGUI>();
        customerClassText.fontSize = 15f;
        customerClassText.fontStyle = FontStyles.Bold;
        customerClassText.alignment = TextAlignmentOptions.Center;
        customerClassText.textWrappingMode = TextWrappingModes.NoWrap;
        customerClassText.overflowMode = TextOverflowModes.Ellipsis;
        customerClassText.raycastTarget = false;
        RefreshCustomerClassLabel();
    }

    // ── 공개 API ────────────────────────────────────────────────────────────────

    public void Show(string text, float duration = 2.5f)
    {
        RefreshCustomerClassLabel();
        if (bubbleText != null) bubbleText.text = text;
        if (_bg != null) _bg.color = new Color(1f, 1f, 1f, 0.92f);
        gameObject.SetActive(true);
        UpdateScreenPosition();
        RestartHide(duration);
    }

    public string CurrentCustomerClassLabel =>
        customerClassText != null ? customerClassText.text : string.Empty;

    // 구매 결과 본문과 독립된 표시 전용 태그. 기존 말풍선 문자열 계약은 바꾸지 않는다.
    public void RefreshCustomerClassLabel()
    {
        if (customerClassText == null) return;

        var npc = GetComponentInParent<NpcController>();
        string label = CustomerPreferencePresentationController.DescribeCustomerClass(npc);
        customerClassText.text = label;
        customerClassText.color = label == "[관광객]"
            ? new Color(0.72f, 0.42f, 0.08f, 1f)
            : new Color(0.12f, 0.42f, 0.24f, 1f);
    }

    // CustomerReaction 통합 — 구매 결정 후 감정 반응 표시
    // price / basePrice 비율로 색상과 이모지 변경
    public void ShowReaction(bool bought, float price, float basePrice)
    {
        if (!bought)
        {
            Show("너무 비싸 ...", 2f);
            if (_bg != null) _bg.color = new Color(1f, 0.5f, 0.5f, 0.9f);
            return;
        }

        float ratio = basePrice > 0f ? price / basePrice : 1f;
        string emoji;
        Color  bgColor;

        if (ratio <= 0.8f)       { emoji = "대박이야!";    bgColor = new Color(0.5f, 1f, 0.5f, 0.9f); }
        else if (ratio <= 1.0f)  { emoji = "좋아!";        bgColor = new Color(0.8f, 1f, 0.8f, 0.9f); }
        else if (ratio <= 1.2f)  { emoji = "흠...";        bgColor = new Color(1f, 1f, 0.7f, 0.9f);   }
        else                     { emoji = "비싸긴 한데."; bgColor = new Color(1f, 0.85f, 0.6f, 0.9f); }

        Show(emoji, 2f);
        if (_bg != null) _bg.color = bgColor;
    }

    public void HideBubble()
    {
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        gameObject.SetActive(false);
    }

    public static void HideAll()
    {
        foreach (var bubble in FindObjectsByType<NpcBubbleUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (bubble != null)
                bubble.HideBubble();
        }
    }

    // ── 내부 ────────────────────────────────────────────────────────────────────

    void RestartHide(float delay)
    {
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(HideAfter(delay));
    }

    void UpdateScreenPosition()
    {
        if (_bubbleRoot == null || Camera.main == null) return;

        Transform anchor = transform.parent != null ? transform.parent : transform;
        Vector3 appliedOffset = offset;
        if (appliedOffset.y > 3.0f)
            appliedOffset.y = 2.4f;

        Vector3 screen = Camera.main.WorldToScreenPoint(anchor.position + appliedOffset);
        if (screen.z <= 0f) return;

        float halfWidth = Mathf.Min(_bubbleRoot.rect.width * 0.5f, Screen.width * 0.45f);
        float halfHeight = Mathf.Min(_bubbleRoot.rect.height * 0.5f, Screen.height * 0.45f);
        screen.x = Mathf.Clamp(screen.x, halfWidth, Screen.width - halfWidth);
        screen.y = Mathf.Clamp(screen.y, halfHeight, Screen.height - halfHeight);
        screen.z = 0f;

        _bubbleRoot.position = screen;
        _bubbleRoot.rotation = Quaternion.identity;
        _bubbleRoot.localScale = Vector3.one;
    }

    IEnumerator HideAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}
