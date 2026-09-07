using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// VS-PRESENT-001: 인증 목표 한 개와 기존 거래 결과만 표시한다.
// 가격 조작은 기존 ShopPriceUI가 담당하며 이 화면은 값을 변경하지 않는다.
public sealed class DepartureTutorialPresentation : MonoBehaviour
{
    public DepartureTutorialController tutorial;
    public TMP_FontAsset font;
    public Button CompanionButton { get; private set; }
    public TextMeshProUGUI ObjectiveLabel { get; private set; }

    TextMeshProUGUI _step;
    TextMeshProUGUI _money;
    TextMeshProUGUI _reaction;
    TextMeshProUGUI _controls;
    GameObject _reactionPanel;
    GameObject _completionPanel;
    RectTransform _progress;
    Canvas _canvas;

    static readonly Color Ink = new Color(0.17f, 0.23f, 0.23f);
    static readonly Color Paper = new Color(0.97f, 0.94f, 0.85f, 0.98f);
    static readonly Color Teal = new Color(0.22f, 0.45f, 0.44f);
    static readonly Color Amber = new Color(0.88f, 0.57f, 0.22f);

    void Start()
    {
        if (gameObject.scene.name != DepartureTutorialController.SceneName) { enabled = false; return; }
        if (tutorial == null) tutorial = GetComponent<DepartureTutorialController>();
        if (font == null) font = TMP_Settings.defaultFontAsset;
        BuildUI();
        var priceCanvas = Resources.FindObjectsOfTypeAll<Canvas>().FirstOrDefault(c => c.gameObject.scene.IsValid() && c.name == "ShopPriceUI_Canvas");
        if (priceCanvas != null)
        {
            var panel = priceCanvas.transform.Find("ShopPricePanel") as RectTransform;
            if (panel != null) panel.anchoredPosition = new Vector2(580f, 0f);
        }
    }

    void Update()
    {
        if (tutorial == null || _canvas == null) return;
        ObjectiveLabel.text = tutorial.ObjectiveText;
        _step.text = tutorial.Complete ? "P.A. COMPANY  /  DEPARTURE CERTIFIED"
            : $"P.A. COMPANY  /  STEP {tutorial.Stage:00}";
        _money.text = $"{(EconomyService.Instance != null ? EconomyService.Instance.Money : 0):N0} G";
        _progress.anchorMax = new Vector2(tutorial.Stage / 6f, 1f);
        bool reactionVisible = tutorial.DecisionCount > 0 && Time.unscaledTime < tutorial.ReactionUntil;
        _reactionPanel.SetActive(reactionVisible);
        if (reactionVisible) _reaction.text = tutorial.LastReaction;
        _completionPanel.SetActive(tutorial.Complete && !(ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen));
        _controls.text = tutorial.Complete ? "출항 준비를 마쳤습니다."
            : "W A S D  이동     SPACE  상호작용";
    }

    void BuildUI()
    {
        GameObject root = new GameObject("PA_DepartureHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        _canvas = root.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 70;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform objective = Panel(root.transform, "CurrentObjective", new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(34f, -30f), new Vector2(520f, 140f), Paper);
        Panel(objective, "CompanyStripe", new Vector2(0f, 1f), new Vector2(0f, 1f),
            Vector2.zero, new Vector2(6f, 140f), Teal);
        _step = Label(objective, "Step", "", new Vector2(26f, -18f), new Vector2(470f, 26f), 18f, Teal);
        ObjectiveLabel = Label(objective, "Objective", "", new Vector2(26f, -53f), new Vector2(470f, 66f), 29f, Ink);
        ObjectiveLabel.enableAutoSizing = true;
        ObjectiveLabel.fontSizeMin = 23f;
        ObjectiveLabel.fontSizeMax = 29f;
        RectTransform track = Panel(objective, "ProgressTrack", Vector2.zero, Vector2.zero,
            new Vector2(26f, 11f), new Vector2(468f, 4f), new Color(0.82f, 0.81f, 0.72f));
        _progress = Panel(track, "Progress", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Amber);
        _progress.anchorMin = Vector2.zero;
        _progress.anchorMax = new Vector2(1f / 6f, 1f);
        _progress.offsetMin = _progress.offsetMax = Vector2.zero;

        RectTransform moneyPanel = Panel(root.transform, "Money", Vector2.one, Vector2.one,
            new Vector2(-34f, -30f), new Vector2(180f, 68f), Teal);
        _money = Label(moneyPanel, "Amount", "0 G", new Vector2(16f, -8f), new Vector2(148f, 52f), 30f, Paper);
        _money.alignment = TextAlignmentOptions.MidlineRight;

        RectTransform controlsPanel = Panel(root.transform, "Controls", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 26f), new Vector2(580f, 44f), new Color(0.17f, 0.23f, 0.23f, 0.86f));
        _controls = Label(controlsPanel, "Keys", "", new Vector2(14f, -3f), new Vector2(552f, 38f), 19f, Paper);
        _controls.alignment = TextAlignmentOptions.Center;

        RectTransform reaction = Panel(root.transform, "ActualCustomerReaction", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 206f), new Vector2(720f, 86f), Paper);
        _reactionPanel = reaction.gameObject;
        Label(reaction, "Caption", "손님의 반응", new Vector2(22f, -10f), new Vector2(670f, 22f), 16f, Teal);
        _reaction = Label(reaction, "Reaction", "", new Vector2(22f, -36f), new Vector2(676f, 40f), 23f, Ink);
        _reaction.enableAutoSizing = true;
        _reaction.fontSizeMin = 18f;
        _reaction.fontSizeMax = 23f;
        _reactionPanel.SetActive(false);

        RectTransform complete = Panel(root.transform, "CompanionUnlock", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -40f), new Vector2(570f, 190f), Paper);
        _completionPanel = complete.gameObject;
        Label(complete, "Unlocked", "동행 생산자 선정 단계가 열렸습니다.", new Vector2(28f, -24f), new Vector2(514f, 74f), 27f, Ink);
        RectTransform button = Panel(complete, "CompanionSelectionButton", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 24f), new Vector2(410f, 58f), Teal);
        CompanionButton = button.gameObject.AddComponent<Button>();
        CompanionButton.targetGraphic = button.GetComponent<Image>();
        CompanionButton.interactable = false;
        button.GetComponent<Image>().raycastTarget = true;
        TextMeshProUGUI buttonLabel = Label(button, "Label", "다음 단계  ·  동행자 선정", new Vector2(12f, -4f), new Vector2(386f, 50f), 23f, Paper);
        buttonLabel.alignment = TextAlignmentOptions.Center;
        _completionPanel.SetActive(false);
    }

    static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    TextMeshProUGUI Label(Transform parent, string name, string value, Vector2 position, Vector2 size, float sizePoints, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = value;
        label.fontSize = sizePoints;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        return label;
    }
}
