using System.Collections.Generic;
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
    public TextMeshProUGUI feedbackText;

    // 후보 카드 프리팹 (null 이면 코드로 동적 생성)
    public GameObject cardPrefab;

    // 생성된 카드 인스턴스 풀
    readonly List<GameObject> _cards = new List<GameObject>();

    void Awake()
    {
        if (scrollRect == null) BuildScrollView();
        EnsureFeedbackText();
    }

    void OnEnable()
    {
        Refresh();
        SetFeedback("역할과 비용을 확인한 뒤 마을 지원 인력을 고용하세요.", false);
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

    void OnHired(NpcCandidateData _, GameObject __) => Refresh();

    // ── 카드 목록 갱신 ──────────────────────────────────────────────────────────
    public void Refresh()
    {
        ClearCards();

        if (HiringService.Instance == null) return;

        List<NpcCandidateData> candidates = HiringService.Instance.GetAvailableCandidates();
        Transform parent = cardParent != null ? cardParent
                          : scrollRect != null ? scrollRect.content
                          : transform;

        foreach (var c in candidates)
            _cards.Add(BuildCard(parent, c));
    }

    void ClearCards()
    {
        foreach (var go in _cards)
            if (go != null) Destroy(go);
        _cards.Clear();
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
            rt.sizeDelta = new Vector2(0f, 132f);

            var bg   = card.GetComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        }

        LayoutElement layout = card.GetComponent<LayoutElement>() ?? card.AddComponent<LayoutElement>();
        layout.preferredHeight = 132f;

        // 이름 텍스트
        var nameTmp = GetOrAddText(card, "CardName");
        if (nameTmp != null)
        {
            nameTmp.text      = data.ResolveDisplayName();
            nameTmp.fontSize  = 18;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color     = Color.white;
            var rt = (RectTransform)nameTmp.transform;
            rt.anchorMin = new Vector2(0f, 0.7f);
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
            rt.anchorMin = new Vector2(0f, 0.28f);
            rt.anchorMax = new Vector2(0.76f, 0.7f);
            rt.offsetMin = new Vector2(12f, 2f);
            rt.offsetMax = new Vector2(-4f, -2f);
        }

        // 비용 텍스트
        var costTmp = GetOrAddText(card, "CardCost");
        if (costTmp != null)
        {
            int money = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;
            bool canAfford = money >= data.hireCost;
            costTmp.text     = $"{ResolveSpecialtyLabel(data.specialty)} · {data.hireCost:N0} G";
            costTmp.fontSize = 14;
            costTmp.color    = canAfford ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
            var rt = (RectTransform)costTmp.transform;
            rt.anchorMin = new Vector2(0f, 0.05f);
            rt.anchorMax = new Vector2(0.75f, 0.28f);
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
        }
        else
        {
            SetFeedback(string.IsNullOrEmpty(failureReason) ? "고용에 실패했습니다." : failureReason, true);
            Refresh();
        }
        // 성공 시 OnHired 이벤트가 후보 목록을 자동 갱신한다.
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
        vpRT.offsetMin = new Vector2(0f, 48f);
        vpRT.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color       = Color.clear;
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
