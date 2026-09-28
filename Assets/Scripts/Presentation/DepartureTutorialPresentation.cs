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
    Mesh _keyCoverMesh;
    float _nextBoardCheck;

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
        foreach(var label in FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None))
            if(label.text.Contains("실습 가격"))label.text="가격은 직접 정해보세요";
        CorrectGatherBoards();
        var priceCanvas = Resources.FindObjectsOfTypeAll<Canvas>().FirstOrDefault(c => c.gameObject.scene.IsValid() && c.name == "ShopPriceUI_Canvas");
        if (priceCanvas != null)
        {
            var panel = priceCanvas.transform.Find("ShopPricePanel") as RectTransform;
            if (panel != null) panel.anchoredPosition = new Vector2(580f, 0f);
        }
    }

    int _lastStage;
    float _noticeUntil;
    void Update()
    {
        // 생성 시점이 다른 실제 보드 인스턴스도 교정한다. MOVE 메시에는 적용하지 않는다.
        if (Time.unscaledTime >= _nextBoardCheck)
        {
            _nextBoardCheck = Time.unscaledTime + 1f;
            CorrectGatherBoards();
        }
        if (tutorial == null || _canvas == null) return;
        if (_lastStage != tutorial.Stage) { _lastStage = tutorial.Stage; _noticeUntil = Time.unscaledTime + 4; }
        ObjectiveLabel.transform.parent.gameObject.SetActive(Time.unscaledTime < _noticeUntil || tutorial.Complete);
        ObjectiveLabel.text = tutorial.ObjectiveText;
        _step.text = tutorial.Complete ? "P.A. COMPANY  /  DEPARTURE CERTIFIED"
            : $"P.A. COMPANY  /  STEP {tutorial.Stage:00}";
        _money.text = $"{(EconomyService.Instance != null ? EconomyService.Instance.Money : 0):N0} G";
        _progress.anchorMax = new Vector2(tutorial.Stage / 6f, 1f);
        bool reactionVisible = tutorial.DecisionCount > 0 && Time.unscaledTime < tutorial.ReactionUntil;
        _reactionPanel.SetActive(reactionVisible);
        if (reactionVisible) _reaction.text = tutorial.LastReaction;
        _completionPanel.SetActive(tutorial.Complete && !(ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen));
        bool modal=PlayerInputHandler.ModalOpen;
        _controls.transform.parent.gameObject.SetActive(!modal);
        _controls.text = tutorial.Complete ? "출항 준비를 마쳤습니다." : tutorial.Stage==3
            ? "1–9 / 휠 선택 · X 빈손 · E 진열" : "WASD 이동 · Shift 달리기 · Space 점프 · E 행동";
    }

    void CorrectGatherBoards()
    {
        foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (renderer.gameObject.scene.name != DepartureTutorialController.SceneName) continue;
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || filter.sharedMesh.name != "Board_Harvest"
                || renderer.transform.Find("InteractionKey_E") != null) continue;

            // 실제 FBX는 SPACE까지 하나의 Board_Harvest 메시로 합쳐져 있다.
            // 확인한 글자 bounds: x ±.19, y 1.35–1.45, z .19. 키 바탕 안쪽만 덮는다.
            if (_keyCoverMesh == null)
            {
                _keyCoverMesh = new Mesh { name = "GatherKeyCover" };
                _keyCoverMesh.vertices = new[] { new Vector3(-.3f, -.09f, 0), new Vector3(.3f, -.09f, 0),
                    new Vector3(.3f, .09f, 0), new Vector3(-.3f, .09f, 0) };
                _keyCoverMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                _keyCoverMesh.RecalculateNormals();
                _keyCoverMesh.RecalculateBounds();
            }
            var cover = new GameObject("InteractionKeyCover", typeof(MeshFilter), typeof(MeshRenderer));
            cover.transform.SetParent(renderer.transform, false);
            cover.transform.localPosition = new Vector3(0, 1.39f, .202f);
            cover.GetComponent<MeshFilter>().sharedMesh = _keyCoverMesh;
            var coverRenderer = cover.GetComponent<MeshRenderer>();
            coverRenderer.sharedMaterial = Resources.Load<Material>("DepartureTutorial/Materials/PA_Ink");
            coverRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            coverRenderer.receiveShadows = false;

            var legend = new GameObject("InteractionKey_E", typeof(TextMeshPro));
            legend.transform.SetParent(renderer.transform, false);
            legend.transform.localPosition = new Vector3(0, 1.40f, .208f);
            legend.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var text = legend.GetComponent<TextMeshPro>();
            text.font = font;
            text.text = "E";
            text.fontSize = 1.6f;
            text.fontStyle = FontStyles.Bold;
            text.color = Paper;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(.6f, .18f);
        }
    }

    void OnDestroy()
    {
        if (_keyCoverMesh != null) Destroy(_keyCoverMesh);
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
            new Vector2(0f, 182f), new Vector2(720f, 44f), new Color(0.17f, 0.23f, 0.23f, 0.86f));
        _controls = Label(controlsPanel, "Keys", "", new Vector2(14f, -3f), new Vector2(692f, 38f), 19f, Paper);
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
