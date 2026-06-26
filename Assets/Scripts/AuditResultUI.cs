using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Smartphone audit app. Builds its own content under the generated phone panel
// without touching the panel root layout, so the back button and home navigation
// remain stable.
public class AuditResultUI : MonoBehaviour
{
    [Header("Optional direct references")]
    public TextMeshProUGUI tierNameText;
    public TextMeshProUGUI revenueText;
    public TextMeshProUGUI nextTierText;
    public Slider revenueSlider;
    public TextMeshProUGUI auditCountdownText;

    const string ContentRootName = "PA_AuditContentRoot";

    static readonly Color TextDark = new Color(0.23f, 0.20f, 0.16f, 1f);
    static readonly Color TextSoft = new Color(0.50f, 0.43f, 0.35f, 1f);
    static readonly Color Card = new Color(1.00f, 0.97f, 0.90f, 0.96f);
    static readonly Color Track = new Color(0.78f, 0.70f, 0.58f, 0.65f);
    static readonly Color Fill = new Color(0.42f, 0.80f, 0.56f, 1f);

    bool _built;

    void OnEnable()
    {
        if (!_built)
        {
            BuildLayout();
            _built = true;
        }

        Refresh();
    }

    public void Refresh()
    {
        int tier = TierService.Instance != null ? TierService.Instance.CurrentTier : 0;
        long revenue = EconomyService.Instance != null ? EconomyService.Instance.CumulativeRevenue : 0;
        int reputation = TierService.Instance != null ? TierService.Instance.Reputation : 0;

        string tierName = "생존자";
        TierDefinition nextDefinition = null;

        if (TierService.Instance != null)
        {
            var current = TierService.Instance.GetDefinition(tier);
            if (current != null && !string.IsNullOrEmpty(current.tierName))
                tierName = current.tierName;

            nextDefinition = TierService.Instance.GetDefinition(tier + 1);
        }

        if (tierNameText != null)
            tierNameText.text = $"Tier {tier} · {tierName}";

        if (revenueText != null)
            revenueText.text = $"누적 매출 {revenue:N0} G";

        if (nextDefinition != null)
        {
            float progress = CalculateNextTierProgress(nextDefinition, revenue, reputation);
            string nextName = string.IsNullOrEmpty(nextDefinition.tierName)
                ? $"Tier {nextDefinition.tier}"
                : nextDefinition.tierName;

            if (revenueSlider != null) revenueSlider.value = progress;
            if (nextTierText != null)
                nextTierText.text = $"다음: Tier {nextDefinition.tier} · {nextName} / {BuildNextTierRequirementText(nextDefinition, revenue, reputation)}";
        }
        else
        {
            if (revenueSlider != null) revenueSlider.value = 1f;
            if (nextTierText != null) nextTierText.text = "최고 티어 달성 · 운영 안정화 단계";
        }

        if (auditCountdownText != null && AuditService.Instance != null && GameClock.Instance != null)
        {
            int nextAuditDay = AuditService.Instance.LastAuditDay + AuditService.Instance.auditIntervalDays;
            int remainingDays = nextAuditDay - GameClock.Instance.CurrentDay;
            auditCountdownText.text = remainingDays > 0
                ? $"다음 감사까지 {remainingDays}일"
                : "오늘 감사 예정";
        }
    }

    static float CalculateNextTierProgress(TierDefinition next, long revenue, int reputation)
    {
        if (next == null) return 1f;

        float total = 0f;
        int count = 0;

        if (next.requiredCumulativeRevenue > 0)
        {
            total += Mathf.Clamp01((float)revenue / next.requiredCumulativeRevenue);
            count++;
        }

        if (next.requiredReputation > 0)
        {
            total += Mathf.Clamp01(reputation / (float)next.requiredReputation);
            count++;
        }

        if (next.requiresManualApproval)
        {
            total += 0f;
            count++;
        }

        return count == 0 ? 1f : total / count;
    }

    static string BuildNextTierRequirementText(TierDefinition next, long revenue, int reputation)
    {
        if (next == null) return "다음 목표 없음";

        var parts = new List<string>();
        if (next.requiredCumulativeRevenue > 0)
        {
            long remaining = next.requiredCumulativeRevenue - revenue;
            if (remaining < 0) remaining = 0;
            parts.Add($"누적 매출 {remaining:N0}G 남음");
        }

        if (next.requiredReputation > 0)
        {
            int remaining = next.requiredReputation - reputation;
            if (remaining < 0) remaining = 0;
            parts.Add($"평판 {remaining} 남음");
        }

        if (next.requiresManualApproval)
            parts.Add("감사 승인 필요");

        return parts.Count > 0 ? string.Join(" · ", parts) : "운영 조건 확인";
    }

    void BuildLayout()
    {
        RemoveRootLayout();
        RemoveChild("Placeholder");
        RemoveChild(ContentRootName);

        var root = new GameObject(ContentRootName,
            typeof(RectTransform),
            typeof(VerticalLayoutGroup));
        var rootRT = (RectTransform)root.transform;
        rootRT.SetParent(transform, false);
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = new Vector2(18f, 24f);
        rootRT.offsetMax = new Vector2(-18f, -54f);

        var layout = root.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(6, 6, 54, 8);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        tierNameText = AddLabel(root.transform, "TierName", 28f, FontStyles.Bold, TextDark, 42f);

        var progressCard = AddCard(root.transform, "RevenueCard", 142f);
        revenueText = AddLabel(progressCard.transform, "RevenueText", 18f, FontStyles.Bold, TextDark, 28f);
        revenueSlider = AddSlider(progressCard.transform, "RevenueSlider");
        nextTierText = AddLabel(progressCard.transform, "NextTierText", 15f, FontStyles.Bold, Fill, 46f);
        nextTierText.textWrappingMode = TextWrappingModes.Normal;

        var auditCard = AddCard(root.transform, "AuditCard", 58f);
        auditCountdownText = AddLabel(auditCard.transform, "AuditCountdown", 18f, FontStyles.Bold, TextSoft, 34f);
    }

    GameObject AddCard(Transform parent, string name, float height)
    {
        var go = new GameObject(name,
            typeof(RectTransform),
            typeof(Image),
            typeof(VerticalLayoutGroup),
            typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.color = Card;
        img.raycastTarget = false;

        var le = go.GetComponent<LayoutElement>();
        le.preferredHeight = height;

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return go;
    }

    TextMeshProUGUI AddLabel(Transform parent, string name, float size, FontStyles style, Color color, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var le = go.GetComponent<LayoutElement>();
        le.preferredHeight = height;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    Slider AddSlider(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var le = go.GetComponent<LayoutElement>();
        le.preferredHeight = 18f;

        var slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        var bgRT = (RectTransform)bg.transform;
        bgRT.SetParent(go.transform, false);
        Stretch(bgRT);
        bg.GetComponent<Image>().color = Track;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        var fillAreaRT = (RectTransform)fillArea.transform;
        fillAreaRT.SetParent(go.transform, false);
        Stretch(fillAreaRT);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fillRT = (RectTransform)fill.transform;
        fillRT.SetParent(fillArea.transform, false);
        Stretch(fillRT);
        fill.GetComponent<Image>().color = Fill;

        slider.fillRect = fillRT;
        slider.targetGraphic = null;
        return slider;
    }

    void RemoveRootLayout()
    {
        var layout = GetComponent<VerticalLayoutGroup>();
        if (layout == null) return;

        layout.enabled = false;
        DestroyUnityObject(layout);
    }

    void RemoveChild(string childName)
    {
        var child = transform.Find(childName);
        if (child == null) return;

        child.gameObject.SetActive(false);
        DestroyUnityObject(child.gameObject);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void DestroyUnityObject(Object obj)
    {
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}
