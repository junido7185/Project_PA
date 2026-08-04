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
    public TextMeshProUGUI facilityDirectionText;
    public TextMeshProUGUI auditCountdownText;
    public TextMeshProUGUI auditRequirementsText;
    public TextMeshProUGUI auditResultText;

    const string ContentRootName = "PA_AuditContentRoot";

    static readonly Color TextDark = new Color(0.23f, 0.20f, 0.16f, 1f);
    static readonly Color TextSoft = new Color(0.50f, 0.43f, 0.35f, 1f);
    static readonly Color Card = new Color(1.00f, 0.97f, 0.90f, 0.96f);
    static readonly Color Track = new Color(0.78f, 0.70f, 0.58f, 0.65f);
    static readonly Color Fill = new Color(0.42f, 0.80f, 0.56f, 1f);
    static readonly Color Warning = new Color(0.78f, 0.38f, 0.28f, 1f);

    bool _built;
    AuditService _subscribedAudit;

    void OnEnable()
    {
        if (!_built)
        {
            BuildLayout();
            _built = true;
        }

        BindAuditService();
        Refresh();
    }

    void OnDisable()
    {
        UnbindAuditService();
    }

    public void Refresh()
    {
        BindAuditService();

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

        if (facilityDirectionText != null)
        {
            if (VillageChangeSignalController.Instance != null)
            {
                VillageChangeSignalController.Instance.RefreshNow();
                facilityDirectionText.text = VillageChangeSignalController.Instance.GetFacilityUnlockPreview();
            }
            else
            {
                facilityDirectionText.text = "시설 방향 예고\n" +
                                             "판매 신호를 불러오지 못했습니다.\n" +
                                             "실제 해금: 티어·감사 조건";
            }
        }

        RefreshAuditStatus(tier);
    }

    static float CalculateNextTierProgress(TierDefinition next, long revenue, int reputation)
    {
        if (next == null) return 1f;

        if (next.requiresManualApproval && AuditService.Instance != null)
        {
            AuditService audit = AuditService.Instance;
            float revenueProgress = audit.requiredRevenueForAudit > 0
                ? Mathf.Clamp01((float)audit.CurrentRevenue / audit.requiredRevenueForAudit)
                : 1f;
            float reputationProgress = audit.requiredReputationForAudit > 0
                ? Mathf.Clamp01(audit.CurrentReputation / (float)audit.requiredReputationForAudit)
                : 1f;
            float hiringProgress = audit.requiredHiredNpcs > 0
                ? Mathf.Clamp01(audit.CurrentHiredCount / (float)audit.requiredHiredNpcs)
                : 1f;
            return (revenueProgress + reputationProgress + hiringProgress) / 3f;
        }

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
        {
            AuditService audit = AuditService.Instance;
            parts.Add(audit != null && audit.AreAllRequirementsMet
                ? "감사 조건 충족"
                : $"본사 감사 준비 {audit?.MetRequirementCount ?? 0}/3");
        }

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
        rootRT.offsetMax = new Vector2(-18f, -50f);

        var layout = root.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(6, 6, 46, 4);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        tierNameText = AddLabel(root.transform, "TierName", 24f, FontStyles.Bold, TextDark, 32f);

        var progressCard = AddCard(root.transform, "RevenueCard", 105f, 6f, 10f);
        revenueText = AddLabel(progressCard.transform, "RevenueText", 16f, FontStyles.Bold, TextDark, 24f);
        revenueSlider = AddSlider(progressCard.transform, "RevenueSlider");
        nextTierText = AddLabel(progressCard.transform, "NextTierText", 13.2f, FontStyles.Bold, Fill, 35f);
        nextTierText.textWrappingMode = TextWrappingModes.Normal;

        var facilityCard = AddCard(root.transform, "FacilityDirectionCard", 76f, 6f, 10f);
        facilityDirectionText = AddLabel(facilityCard.transform, "FacilityDirectionText", 13f, FontStyles.Bold, TextDark, 56f);
        facilityDirectionText.textWrappingMode = TextWrappingModes.Normal;
        facilityDirectionText.overflowMode = TextOverflowModes.Ellipsis;

        var auditCard = AddCard(root.transform, "AuditCard", 170f, 6f, 10f);
        auditCountdownText = AddLabel(auditCard.transform, "AuditCountdown", 15.5f, FontStyles.Bold, TextSoft, 22f);
        auditRequirementsText = AddLabel(auditCard.transform, "AuditRequirements", 12.5f, FontStyles.Bold, TextDark, 56f);
        auditRequirementsText.textWrappingMode = TextWrappingModes.Normal;
        auditResultText = AddLabel(auditCard.transform, "AuditResult", 12.5f, FontStyles.Bold, TextSoft, 60f);
        auditResultText.textWrappingMode = TextWrappingModes.Normal;
    }

    GameObject AddCard(Transform parent, string name, float height, float spacing = 8f, float verticalPadding = 12f)
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
        layout.spacing = spacing;
        layout.padding = new RectOffset(14, 14, Mathf.RoundToInt(verticalPadding), Mathf.RoundToInt(verticalPadding));
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return go;
    }

    void RefreshAuditStatus(int tier)
    {
        AuditService audit = AuditService.Instance;
        if (audit == null)
        {
            if (auditCountdownText != null) auditCountdownText.text = "감사 일정을 불러오는 중";
            if (auditRequirementsText != null) auditRequirementsText.text = "감사 조건을 불러오지 못했습니다.";
            if (auditResultText != null) auditResultText.text = "P.A. Phone을 다시 열어 주세요.";
            return;
        }

        int currentDay = GameClock.Instance != null ? GameClock.Instance.CurrentDay : audit.LastAuditDay;
        int remainingDays = audit.NextAuditDay - currentDay;
        if (auditCountdownText != null)
        {
            if (tier >= 4)
                auditCountdownText.text = "본사 감사 완료 · Tier 4 자유 운영";
            else if (remainingDays > 0)
                auditCountdownText.text = $"다음 감사 Day {audit.NextAuditDay} · {remainingDays}일 남음";
            else
                auditCountdownText.text = audit.AreAllRequirementsMet
                    ? "오늘 감사 예정 · 조건 충족"
                    : "오늘 감사 예정 · 준비 미달";
        }

        if (auditRequirementsText != null)
        {
            auditRequirementsText.text =
                $"매출 {BuildRequirementState(audit.RevenueRequirementMet)}  {audit.CurrentRevenue:N0}/{audit.requiredRevenueForAudit:N0}G\n" +
                $"평판 {BuildRequirementState(audit.ReputationRequirementMet)}  {audit.CurrentReputation}/{audit.requiredReputationForAudit}  ·  " +
                $"고용 {BuildRequirementState(audit.HiringRequirementMet)}  {audit.CurrentHiredCount}/{audit.requiredHiredNpcs}명";
        }

        if (auditResultText == null) return;

        auditResultText.color = audit.LastOutcome == AuditService.AuditOutcome.Failed
                             || audit.LastOutcome == AuditService.AuditOutcome.AdvancementBlocked
            ? Warning
            : TextSoft;

        switch (audit.LastOutcome)
        {
            case AuditService.AuditOutcome.Failed:
                auditResultText.text = $"최근 결과  Day {audit.LastAuditDay} 감사 미통과\n{BuildNextAuditAction(audit)}";
                break;
            case AuditService.AuditOutcome.Passed:
                auditResultText.text = $"최근 결과  Day {audit.LastAuditDay} 감사 통과\nTier 4 본사임원 해금 · 마을 파트너 자유 운영";
                break;
            case AuditService.AuditOutcome.AlreadyComplete:
                auditResultText.text = $"최근 결과  Day {audit.LastAuditDay} 최고 등급 확인\nTier 4 운영이 유지되고 있습니다.";
                break;
            case AuditService.AuditOutcome.AdvancementBlocked:
                auditResultText.text = $"최근 결과  Day {audit.LastAuditDay} 승급 보류\n감사 조건은 충족했지만 Tier 상태를 다시 확인하세요.";
                break;
            default:
                auditResultText.text = audit.LastAuditDay > 0
                    ? $"최근 감사  Day {audit.LastAuditDay} 기록 복원\n{BuildNextAuditAction(audit)}"
                    : $"최근 결과  아직 감사 전\n{BuildNextAuditAction(audit)}";
                break;
        }
    }

    static string BuildRequirementState(bool met) => met ? "[완료]" : "[부족]";

    static string BuildNextAuditAction(AuditService audit)
    {
        int reputationRemaining = Mathf.Max(0, audit.requiredReputationForAudit - audit.CurrentReputation);
        if (reputationRemaining > 0)
            return $"다음 행동  전문 주민 낮 요청 · 평판 {reputationRemaining} 필요";

        int hiringRemaining = Mathf.Max(0, audit.requiredHiredNpcs - audit.CurrentHiredCount);
        if (hiringRemaining > 0)
            return $"다음 행동  P.A. Phone 채용 · 지원 인력 {hiringRemaining}명 필요";

        long revenueRemaining = audit.requiredRevenueForAudit - audit.CurrentRevenue;
        if (revenueRemaining > 0)
            return $"다음 행동  낮 준비→밤 판매 · 매출 {revenueRemaining:N0}G 필요";

        return $"모든 조건 충족 · Day {audit.NextAuditDay} 정기 감사를 기다리세요.";
    }

    void BindAuditService()
    {
        AuditService audit = AuditService.Instance;
        if (_subscribedAudit == audit) return;

        UnbindAuditService();
        _subscribedAudit = audit;
        if (_subscribedAudit != null)
            _subscribedAudit.OnAuditStateChanged += Refresh;
    }

    void UnbindAuditService()
    {
        if (_subscribedAudit != null)
            _subscribedAudit.OnAuditStateChanged -= Refresh;
        _subscribedAudit = null;
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
