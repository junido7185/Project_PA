using UnityEngine;
using UnityEngine.UI;
using TMPro;

// §5 AuditResultUI — SmartphoneUI 감사 탭(index 0) 에 붙이는 컴포넌트.
// OnEnable 때마다 TierService/AuditService/EconomyService 현재 값으로 갱신.
// Inspector 에서 직접 연결하거나 BuildLayout() 으로 자동 생성.
public class AuditResultUI : MonoBehaviour
{
    [Header("직접 연결 (선택)")]
    public TextMeshProUGUI tierNameText;
    public TextMeshProUGUI revenueText;
    public TextMeshProUGUI nextTierText;
    public Slider          revenueSlider;
    public TextMeshProUGUI auditCountdownText;

    bool _built;

    void OnEnable()
    {
        if (!_built) { BuildLayout(); _built = true; }
        Refresh();
    }

    public void Refresh()
    {
        int tier = TierService.Instance != null ? TierService.Instance.CurrentTier : 0;
        long rev  = EconomyService.Instance != null ? EconomyService.Instance.CumulativeRevenue : 0;

        // 현재 티어 이름
        string tierName = "생존자";
        long   nextReq  = 0;
        if (TierService.Instance != null)
        {
            var def = TierService.Instance.GetDefinition(tier);
            if (def != null) tierName = def.tierName;
            var nextDef = TierService.Instance.GetDefinition(tier + 1);
            if (nextDef != null) nextReq = nextDef.requiredCumulativeRevenue;
        }

        if (tierNameText != null) tierNameText.text = $"Tier {tier} · {tierName}";
        if (revenueText  != null) revenueText.text  = $"누적 매출: {rev:N0} G";

        if (nextReq > 0)
        {
            float progress = Mathf.Clamp01((float)rev / nextReq);
            if (revenueSlider != null) revenueSlider.value = progress;
            if (nextTierText  != null) nextTierText.text  = $"다음 티어까지: {nextReq - rev:N0} G";
        }
        else
        {
            if (revenueSlider != null) revenueSlider.value = 1f;
            if (nextTierText  != null) nextTierText.text  = "최고 티어 달성!";
        }

        // 감사 카운트다운
        if (auditCountdownText != null && AuditService.Instance != null && GameClock.Instance != null)
        {
            int nextAudit = AuditService.Instance.LastAuditDay + AuditService.Instance.auditIntervalDays;
            int remaining = nextAudit - GameClock.Instance.CurrentDay;
            auditCountdownText.text = remaining > 0
                ? $"다음 감사까지 {remaining}일"
                : "곧 감사 예정";
        }
    }

    void BuildLayout()
    {
        var vlg = gameObject.GetComponent<VerticalLayoutGroup>()
                  ?? gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing        = 10f;
        vlg.padding        = new RectOffset(12, 12, 12, 12);

        tierNameText       = AddLabel("TierName", 22, FontStyles.Bold, new Color(1f, 0.85f, 0.4f));
        revenueText        = AddLabel("RevenueText", 18, FontStyles.Normal, Color.white);
        revenueSlider      = AddSlider("RevenueSlider");
        nextTierText       = AddLabel("NextTierText", 16, FontStyles.Normal, new Color(0.7f, 0.9f, 0.7f));
        auditCountdownText = AddLabel("AuditCountdown", 16, FontStyles.Normal, new Color(0.9f, 0.7f, 0.7f));
    }

    TextMeshProUGUI AddLabel(string goName, float size, FontStyles style, Color color)
    {
        var go  = new GameObject(goName, typeof(TextMeshProUGUI));
        go.transform.SetParent(transform, false);
        var le  = go.AddComponent<LayoutElement>();
        le.preferredHeight = 30f;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.color     = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    Slider AddSlider(string goName)
    {
        var go = new GameObject(goName, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(transform, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 24f;

        var slider = go.GetComponent<Slider>();
        slider.minValue     = 0f;
        slider.maxValue     = 1f;
        slider.value        = 0f;
        slider.interactable = false;

        // 배경 + fill
        var bg    = new GameObject("Background", typeof(Image));
        bg.transform.SetParent(go.transform, false);
        ((RectTransform)bg.transform).anchorMin = Vector2.zero;
        ((RectTransform)bg.transform).anchorMax = Vector2.one;
        bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var faRT     = (RectTransform)fillArea.transform;
        faRT.anchorMin = Vector2.zero;
        faRT.anchorMax = Vector2.one;
        faRT.offsetMin = Vector2.zero;
        faRT.offsetMax = Vector2.zero;

        var fill  = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRT = (RectTransform)fill.transform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.sizeDelta = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.3f, 0.8f, 0.5f);

        slider.fillRect = fillRT;
        return slider;
    }
}
