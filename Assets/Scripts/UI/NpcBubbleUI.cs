using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// §4 NpcBubbleUI — NPC 머리 위 WorldSpace 말풍선.
// NPC GameObject 의 자식으로 배치한다. 빌보드(항상 카메라를 향함).
// Show(text, duration) — 일반 대사 말풍선.
// ShowReaction(bought, price, basePrice) — CustomerReaction 통합 (구매결정 반응).
[RequireComponent(typeof(Canvas))]
public class NpcBubbleUI : MonoBehaviour
{
    [Header("오프셋 (NPC 머리 위)")]
    public Vector3 offset = new Vector3(0f, 2.2f, 0f);

    [Header("직접 연결 (선택)")]
    public TextMeshProUGUI bubbleText;

    Canvas   _canvas;
    Image    _bg;
    Coroutine _hideCoroutine;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvas.renderMode    = RenderMode.WorldSpace;
        _canvas.worldCamera   = Camera.main;

        var rt          = GetComponent<RectTransform>();
        rt.sizeDelta    = new Vector2(260f, 70f);
        rt.localScale   = new Vector3(0.004f, 0.004f, 0.004f);
        rt.localPosition = offset;

        if (bubbleText == null) BuildBubble();
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        // 빌보드 — 항상 카메라 방향을 바라봄
        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;
    }

    void BuildBubble()
    {
        // 배경
        var bgGO = new GameObject("BubbleBG", typeof(Image));
        bgGO.transform.SetParent(transform, false);
        var bgRT        = (RectTransform)bgGO.transform;
        bgRT.anchorMin  = Vector2.zero;
        bgRT.anchorMax  = Vector2.one;
        bgRT.offsetMin  = Vector2.zero;
        bgRT.offsetMax  = Vector2.zero;
        _bg             = bgGO.GetComponent<Image>();
        _bg.color       = new Color(1f, 1f, 1f, 0.92f);

        // 텍스트
        var textGO = new GameObject("BubbleText", typeof(TextMeshProUGUI));
        textGO.transform.SetParent(transform, false);
        var textRT     = (RectTransform)textGO.transform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(8f, 6f);
        textRT.offsetMax = new Vector2(-8f, -6f);

        bubbleText           = textGO.GetComponent<TextMeshProUGUI>();
        bubbleText.fontSize  = 28; // WorldSpace 이므로 크게
        bubbleText.color     = new Color(0.1f, 0.1f, 0.1f);
        bubbleText.alignment = TextAlignmentOptions.Center;
        bubbleText.raycastTarget = false;
    }

    // ── 공개 API ────────────────────────────────────────────────────────────────

    public void Show(string text, float duration = 2.5f)
    {
        if (bubbleText != null) bubbleText.text = text;
        if (_bg != null) _bg.color = new Color(1f, 1f, 1f, 0.92f);
        gameObject.SetActive(true);
        RestartHide(duration);
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

        if (_bg != null) _bg.color = bgColor;
        Show(emoji, 2f);
    }

    public void HideBubble()
    {
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        gameObject.SetActive(false);
    }

    // ── 내부 ────────────────────────────────────────────────────────────────────

    void RestartHide(float delay)
    {
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(HideAfter(delay));
    }

    IEnumerator HideAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}
