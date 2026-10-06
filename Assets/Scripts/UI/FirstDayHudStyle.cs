using TMPro;
using UnityEngine;
using UnityEngine.UI;

// D4 — 데모(FirstDay) 경로 전용 HUD 한 벌 디자인. 기존 HUD 권위(ClockHUD/MoneyHUD/InteractPromptUI/HotbarUI)는
// 그대로 두고, 이미 만들어진 오브젝트의 겉모양만 크림색 둥근 칩으로 맞춘다(Golden 경로 미적용).
public sealed class FirstDayHudStyle : MonoBehaviour
{
    static readonly Color Chip = new Color(.98f, .95f, .87f, .94f);
    static readonly Color Ink = PAUiTheme.Ink;
    static readonly Color Teal = PAUiTheme.Teal;
    static readonly Color Gold = new Color(.93f, .70f, .22f);
    float _next;

    void Update()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + .5f; // HUD는 지연 생성될 수 있어 짧게 재적용한다(이미 적용된 것은 건너뜀).
        StyleClock();
        StyleMoney();
        StylePrompt();
        StyleHotbarFrame();
    }

    static bool MarkOnce(GameObject target)
    {
        if (target == null || target.transform.Find("__PAStyled") != null) return false;
        var marker = new GameObject("__PAStyled", typeof(RectTransform));
        marker.transform.SetParent(target.transform, false);
        return true;
    }

    static void Round(Image image, Color color)
    {
        image.sprite = PAUiTheme.RoundedSprite; image.type = Image.Type.Sliced; image.color = color;
    }

    void StyleClock()
    {
        var hud = ClockHUD.instance;
        if (hud == null || hud.clockText == null) return;
        var panel = hud.clockText.transform.parent as RectTransform;
        if (panel == null || !MarkOnce(panel.gameObject)) return;
        Round(panel.GetComponent<Image>(), Chip);
        panel.sizeDelta = new Vector2(190, 76);
        panel.anchoredPosition = new Vector2(24, -22);
        Text(hud.clockText, 32, Ink, new Vector2(20, 30), new Vector2(-12, -4));
        Text(hud.dateText, 17, Teal, new Vector2(20, 8), new Vector2(-12, -44));
    }

    void StyleMoney()
    {
        var hud = MoneyHUD.instance;
        if (hud == null || hud.moneyText == null) return;
        var panel = hud.moneyText.transform.parent as RectTransform;
        if (panel == null || !MarkOnce(panel.gameObject)) return;
        Round(panel.GetComponent<Image>(), Chip);
        panel.sizeDelta = new Vector2(196, 60);
        panel.anchoredPosition = new Vector2(-24, -22);
        if (hud.tierText != null) hud.tierText.gameObject.SetActive(false);
        if (hud.tierGoalText != null) hud.tierGoalText.gameObject.SetActive(false);
        Text(hud.moneyText, 30, Ink, new Vector2(62, 4), new Vector2(-20, -4));
        hud.moneyText.alignment = TextAlignmentOptions.MidlineRight;
        // 동전 아이콘. 숫자는 기존 MoneyHUD가 계속 갱신한다.
        var coin = new GameObject("Coin", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)coin.transform;
        rt.SetParent(panel, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0, .5f); rt.pivot = new Vector2(0, .5f);
        rt.anchoredPosition = new Vector2(16, 0); rt.sizeDelta = new Vector2(36, 36);
        Round(coin.GetComponent<Image>(), Gold);
        coin.GetComponent<Image>().raycastTarget = false;
        // 동전 안쪽 밝은 원(글자 없이 동전으로 읽힘). 숫자 뒤 "G"는 기존 MoneyHUD 표기를 따른다.
        var shine = new GameObject("Shine", typeof(RectTransform), typeof(Image));
        var srt = (RectTransform)shine.transform;
        srt.SetParent(rt, false);
        srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one; srt.offsetMin = new Vector2(7, 7); srt.offsetMax = new Vector2(-7, -7);
        Round(shine.GetComponent<Image>(), new Color(1f, .86f, .45f));
        shine.GetComponent<Image>().raycastTarget = false;
    }

    void StylePrompt()
    {
        var prompt = InteractPromptUI.instance;
        if (prompt == null || prompt.promptText == null) return;
        var panel = prompt.promptText.transform.parent as RectTransform;
        if (panel == null || !MarkOnce(panel.gameObject)) return;
        Round(panel.GetComponent<Image>(), Chip);
        panel.sizeDelta = new Vector2(560, 52);
        panel.anchoredPosition = new Vector2(0, 138);
        Text(prompt.promptText, 22, Ink, new Vector2(16, 4), new Vector2(-16, -4));
    }

    void StyleHotbarFrame()
    {
        var frame = GameObject.Find("Hotbar_1_to_9");
        if (frame == null || !MarkOnce(frame)) return;
        var image = frame.GetComponent<Image>();
        if (image != null) Round(image, new Color(.10f, .22f, .24f, .55f));
    }

    static void Text(TextMeshProUGUI text, int size, Color color, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (text == null) return;
        var rt = text.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        text.fontSize = size; text.color = color;
    }
}
