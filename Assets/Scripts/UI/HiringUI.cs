using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// §4 HiringUI — SmartphoneUI 채용 탭(index 1) 에 붙이는 컴포넌트.
// OnEnable 때마다 HiringService.GetAvailableCandidates() 를 갱신해
// 후보 카드 목록을 ScrollRect 에 채운다.
// 자동빌드 지원: scrollRect/cardParent/cardPrefab 이 null 이면 코드로 생성.
public class HiringUI : MonoBehaviour
{
    [Header("연결 (선택 — null 이면 자동 빌드)")]
    public ScrollRect scrollRect;
    public Transform  cardParent;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI feedbackText;

    // 후보 카드 프리팹 (null 이면 코드로 동적 생성)
    public GameObject cardPrefab;

    // 생성된 카드 인스턴스 풀
    readonly List<GameObject> _cards = new List<GameObject>();
    readonly Dictionary<NpcCandidateData, GameObject> _cardByCandidate =
        new Dictionary<NpcCandidateData, GameObject>();

    public int VisibleCardCount => _cardByCandidate.Values.Count(card =>
        card != null && card.activeInHierarchy);
    public string CurrentStatusText => statusText != null ? statusText.text : string.Empty;
    public string CurrentFeedbackText => feedbackText != null ? feedbackText.text : string.Empty;

    public bool TryGetCandidateCard(NpcCandidateData candidate, out GameObject card)
    {
        card = null;
        return candidate != null && _cardByCandidate.TryGetValue(candidate, out card) && card != null;
    }

    void Awake()
    {
        if (scrollRect == null) BuildScrollView();
        EnsureStatusText();
        EnsureFeedbackText();
    }

    void OnEnable()
    {
        SetFeedback("역할과 비용을 확인한 뒤 마을 지원 인력을 고용하세요.", false);
        Refresh();
    }

    void OnDisable() => ClearCards();

    // HiringService.OnHired 때도 Refresh 를 호출해 고용 완료 후 목록 자동 갱신
    void Start()
    {
        if (HiringService.Instance != null)
            HiringService.Instance.OnHired += OnHired;

        if (scrollRect == null) BuildScrollView();
        EnsureFeedbackText();
    }

    void OnDestroy()
    {
        if (HiringService.Instance != null)
            HiringService.Instance.OnHired -= OnHired;
    }

    void OnHired(NpcCandidateData candidate, GameObject _)
    {
        SetFeedback($"{candidate.ResolveDisplayName()} 고용 완료! 마을 지원 인력으로 합류했습니다.", false);
        Refresh();
    }

    // ── 카드 목록 갱신 ──────────────────────────────────────────────────────────
    public void Refresh()
    {
        ClearCards();

        EnsureStatusText();
        EnsureFeedbackText();

        if (HiringService.Instance == null)
        {
            BuildEmptyState(cardParent != null ? cardParent : transform, "채용 서비스를 불러오는 중입니다.");
            statusText.text = "채용 서비스 준비 중";
            RebuildLayout();
            return;
        }

        // 이미 고용했거나 잠긴 후보도 숨기지 않는다. 플레이어가 전체 역할과
        // 다음 목표를 확인할 수 있도록 configured candidate authority를 그대로 표시한다.
        List<NpcCandidateData> candidates = HiringService.Instance.availableCandidates
            .Where(candidate => candidate != null)
            .OrderBy(candidate => candidate.requiredTier)
            .ThenBy(candidate => candidate.hireCost)
            .ThenBy(candidate => candidate.name)
            .ToList();
        Transform parent = cardParent != null ? cardParent
                          : scrollRect != null ? scrollRect.content
                          : transform;

        foreach (var c in candidates)
        {
            GameObject card = BuildCard(parent, c);
            _cards.Add(card);
            _cardByCandidate[c] = card;
        }

        if (candidates.Count == 0)
            BuildEmptyState(parent, "등록된 채용 후보가 없습니다.");

        int money = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;
        int hired = HiringService.Instance.HiredCount;
        statusText.text = $"보유 {money:N0} G  ·  고용 {hired}/{candidates.Count}명";
        RebuildLayout();
    }

    void ClearCards()
    {
        foreach (var go in _cards)
            if (go != null)
            {
                go.SetActive(false);
                Destroy(go);
            }
        _cards.Clear();
        _cardByCandidate.Clear();
    }

    // ── 카드 한 장 생성 ─────────────────────────────────────────────────────────
    GameObject BuildCard(Transform parent, NpcCandidateData data)
    {
        GameObject card;

        if (cardPrefab != null)
        {
            card = Instantiate(cardPrefab, parent);
        }
        else
        {
            // 동적 생성 카드
            card = new GameObject($"Card_{data.ResolveDisplayName()}",
                typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            var rt      = (RectTransform)card.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(0f, 148f);

            var bg   = card.GetComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        }

        LayoutElement layout = card.GetComponent<LayoutElement>() ?? card.AddComponent<LayoutElement>();
        layout.preferredHeight = 148f;

        // 이름 텍스트
        var nameTmp = GetOrAddText(card, "CardName");
        if (nameTmp != null)
        {
            nameTmp.text      = data.ResolveDisplayName();
            nameTmp.fontSize  = 18;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color     = Color.white;
            var rt = (RectTransform)nameTmp.transform;
            rt.anchorMin = new Vector2(0f, 0.72f);
            rt.anchorMax = new Vector2(0.75f, 1f);
            rt.offsetMin = new Vector2(12f, 4f);
            rt.offsetMax = new Vector2(-4f, -4f);
        }

        // 후보 소개 — 실제 역할과 고용 효과를 선택 전에 읽을 수 있게 한다.
        var bioTmp = GetOrAddText(card, "CardBio");
        if (bioTmp != null)
        {
            bioTmp.text = string.IsNullOrWhiteSpace(data.bio)
                ? $"{ResolveSpecialtyLabel(data.specialty)} 역할로 마을 일을 돕습니다."
                : data.bio;
            bioTmp.fontSize = 13;
            bioTmp.color = new Color(0.84f, 0.87f, 0.92f);
            bioTmp.textWrappingMode = TextWrappingModes.Normal;
            bioTmp.overflowMode = TextOverflowModes.Ellipsis;
            var rt = (RectTransform)bioTmp.transform;
            rt.anchorMin = new Vector2(0f, 0.31f);
            rt.anchorMax = new Vector2(0.76f, 0.72f);
            rt.offsetMin = new Vector2(12f, 2f);
            rt.offsetMax = new Vector2(-4f, -2f);
        }

        // 비용 텍스트
        var costTmp = GetOrAddText(card, "CardCost");
        if (costTmp != null)
        {
            int money = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;
            bool canAfford = money >= data.hireCost;
            bool hired = HiringService.Instance != null && HiringService.Instance.IsHired(data);
            string balance = hired
                ? "합류 완료"
                : canAfford
                    ? $"잔액 {money:N0} → {money - data.hireCost:N0} G"
                    : $"잔액 {money:N0} G · {data.hireCost - money:N0} G 부족";
            costTmp.text     = $"{ResolveSpecialtyLabel(data.specialty)} · {data.hireCost:N0} G  |  {balance}";
            costTmp.fontSize = 14;
            costTmp.color    = hired
                ? new Color(0.52f, 0.86f, 1f)
                : canAfford ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
            var rt = (RectTransform)costTmp.transform;
            rt.anchorMin = new Vector2(0f, 0.05f);
            rt.anchorMax = new Vector2(0.76f, 0.31f);
            rt.offsetMin = new Vector2(12f, 4f);
            rt.offsetMax = new Vector2(-4f, -4f);
        }

        // 고용 버튼
        var btnGO = new GameObject("HireButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var btnRT = (RectTransform)btnGO.transform;
        btnRT.SetParent(card.transform, false);
        btnRT.anchorMin = new Vector2(0.78f, 0.15f);
        btnRT.anchorMax = new Vector2(0.98f, 0.85f);
        btnRT.offsetMin = Vector2.zero;
        btnRT.offsetMax = Vector2.zero;

        var btnLabel = new GameObject("BtnLabel", typeof(TextMeshProUGUI));
        var btnLRT   = (RectTransform)btnLabel.transform;
        btnLRT.SetParent(btnGO.transform, false);
        btnLRT.anchorMin = Vector2.zero;
        btnLRT.anchorMax = Vector2.one;
        btnLRT.offsetMin = Vector2.zero;
        btnLRT.offsetMax = Vector2.zero;

        var btnTmp       = btnLabel.GetComponent<TextMeshProUGUI>();
        string unavailableReason = "채용 서비스를 준비 중입니다.";
        bool canHire = HiringService.Instance != null
                       && HiringService.Instance.CanHire(data, out unavailableReason);
        btnTmp.text      = canHire ? "고용" : ResolveUnavailableButtonLabel(unavailableReason);
        btnTmp.fontSize  = canHire ? 16 : 12;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.color     = Color.white;
        btnTmp.alignment = TextAlignmentOptions.Center;

        var captured = data;
        Button button = btnGO.GetComponent<Button>();
        button.interactable = canHire;
        btnGO.GetComponent<Image>().color = canHire
            ? new Color(0.2f, 0.6f, 1f)
            : new Color(0.28f, 0.3f, 0.34f);
        button.onClick.AddListener(() => OnHireClicked(captured));

        return card;
    }

    void OnHireClicked(NpcCandidateData data)
    {
        if (HiringService.Instance == null) return;
        if (HiringService.Instance.TryHire(data, out _, out string failureReason))
        {
            SetFeedback($"{data.ResolveDisplayName()} 고용 완료! 마을 지원 인력으로 합류했습니다.", false);
            Refresh();
        }
        else
        {
            SetFeedback(string.IsNullOrEmpty(failureReason) ? "고용에 실패했습니다." : failureReason, true);
            Refresh();
        }
        // OnHired도 갱신하지만, 이벤트 구독 시점과 무관하게 클릭 경로가 항상 최신 상태를 보장한다.
    }

    // ── 자동 ScrollView 빌드 ────────────────────────────────────────────────────
    void BuildScrollView()
    {
        // ScrollRect 를 이 GameObject 에 직접 추가
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var vpRT     = (RectTransform)viewport.transform;
        vpRT.SetParent(transform, false);
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = new Vector2(0f, 50f);
        vpRT.offsetMax = new Vector2(0f, -50f);
        // Mask는 graphic alpha를 stencil에 사용한다. 투명색이면 자식 카드도 전부 가려진다.
        viewport.GetComponent<Image>().color       = Color.white;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform));
        var cRT     = (RectTransform)content.transform;
        cRT.SetParent(viewport.transform, false);
        cRT.anchorMin = new Vector2(0f, 1f);
        cRT.anchorMax = new Vector2(1f, 1f);
        cRT.pivot     = new Vector2(0.5f, 1f);
        cRT.sizeDelta = new Vector2(0f, 0f);

        // VerticalLayoutGroup 자동 정렬
        var vlg            = content.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing        = 8f;
        vlg.padding        = new RectOffset(8, 8, 8, 8);

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect          = gameObject.GetComponent<ScrollRect>() ?? gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = vpRT;
        scrollRect.content  = cRT;
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;

        cardParent = cRT;
    }

    void EnsureStatusText()
    {
        if (statusText != null) return;

        var go = new GameObject("HiringStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(10f, -46f);
        rt.offsetMax = new Vector2(-10f, -4f);

        statusText = go.GetComponent<TextMeshProUGUI>();
        statusText.fontSize = 14;
        statusText.fontStyle = FontStyles.Bold;
        statusText.alignment = TextAlignmentOptions.MidlineLeft;
        statusText.color = new Color(0.18f, 0.2f, 0.25f);
    }

    void EnsureFeedbackText()
    {
        if (feedbackText != null) return;

        var go = new GameObject("HiringFeedback", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(10f, 4f);
        rt.offsetMax = new Vector2(-10f, 44f);

        feedbackText = go.GetComponent<TextMeshProUGUI>();
        feedbackText.fontSize = 13;
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.textWrappingMode = TextWrappingModes.Normal;
    }

    void SetFeedback(string message, bool isError)
    {
        EnsureFeedbackText();
        feedbackText.text = message ?? string.Empty;
        feedbackText.color = isError
            ? new Color(1f, 0.48f, 0.42f)
            : new Color(0.72f, 0.92f, 1f);
    }

    void BuildEmptyState(Transform parent, string message)
    {
        if (parent == null) return;
        var card = new GameObject("HiringEmptyState", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        card.transform.SetParent(parent, false);
        card.GetComponent<Image>().color = new Color(0.16f, 0.17f, 0.22f, 0.92f);
        card.GetComponent<LayoutElement>().preferredHeight = 96f;
        var text = GetOrAddText(card, "Message");
        text.text = message;
        text.fontSize = 15f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.82f, 0.85f, 0.9f);
        _cards.Add(card);
    }

    void RebuildLayout()
    {
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null && scrollRect.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
    }

    static string ResolveUnavailableButtonLabel(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return "준비 중";
        if (reason.Contains("부족")) return "잔액 부족";
        if (reason.Contains("Tier")) return "등급 잠금";
        if (reason.Contains("이미")) return "고용됨";
        return "준비 중";
    }

    static string ResolveSpecialtyLabel(NpcSpecialty specialty)
    {
        return specialty switch
        {
            NpcSpecialty.Farmer => "농부",
            NpcSpecialty.Miner => "광부",
            NpcSpecialty.Lumberjack => "벌목꾼",
            NpcSpecialty.Fisher => "어부",
            NpcSpecialty.Chef => "요리사",
            NpcSpecialty.Blacksmith => "대장장이",
            NpcSpecialty.Tailor => "재단사",
            NpcSpecialty.Carpenter => "목수",
            _ => "주민"
        };
    }

    TextMeshProUGUI GetOrAddText(GameObject parent, string childName)
    {
        var child = parent.transform.Find(childName);
        if (child != null) return child.GetComponent<TextMeshProUGUI>();

        var go = new GameObject(childName, typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);
        var rt       = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go.GetComponent<TextMeshProUGUI>();
    }
}
