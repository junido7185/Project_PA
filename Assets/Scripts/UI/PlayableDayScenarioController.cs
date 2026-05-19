using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// First playable prototype flow:
// name registration -> briefing -> map selection -> smartphone/supplies intro ->
// in-world first sale loop -> audit app -> save -> day summary.
public class PlayableDayScenarioController : MonoBehaviour
{
    public static PlayableDayScenarioController Instance { get; private set; }

    public enum Stage
    {
        TalkToNpc       = 0,
        StockShopSlot   = 1,
        SetPrice        = 2,
        WaitForPurchase = 3,
        OpenAuditApp    = 4,
        SaveProgress    = 5,
        Done            = 6,
    }

    enum StartupStep
    {
        Name,
        Briefing,
        MapSelect,
        PhoneIntro,
        Supplies,
        Arrival,
    }

    [Header("UI 연결 (자동 생성 가능)")]
    public TextMeshProUGUI objectiveText;
    public Canvas guideCanvas;

    [Header("첫날 프로토타입")]
    public bool showStartupFlow = true;
    public bool autoCreateUI = true;
    public Vector2 anchorOffset = new Vector2(0f, -90f);

    public string PlayerName { get; private set; } = "하늘";
    public string SelectedMapId { get; private set; } = "green_bay";
    public string SelectedMapName => GetMapName(SelectedMapId);
    public bool StartupFlowCompleted => _startupCompleted;
    public Stage Current { get; private set; } = Stage.TalkToNpc;
    public int CurrentStageIndex => (int)Current;

    public event Action<Stage> OnObjectiveComplete;

    Canvas _flowCanvas;
    GameObject _flowPanel;
    TextMeshProUGUI _flowTitle;
    TextMeshProUGUI _flowBody;
    TMP_InputField _nameInput;
    Button _randomNameButton;
    GameObject _mapButtonRoot;
    readonly List<Button> _mapButtons = new List<Button>();
    Button _primaryButton;
    Button _secondaryButton;
    TextMeshProUGUI _primaryText;
    TextMeshProUGUI _secondaryText;

    StartupStep _startupStep = StartupStep.Name;
    bool _startupCompleted;
    bool _startupPausedTime;
    bool _everSawDialogueOpen;
    bool _everSawSmartphoneAudit;
    bool _everSawSave;
    bool _forcedFirstBuyer;
    bool _summaryShown;
    float _timeScaleBeforeStartup = 1f;
    long _baselineRevenue;
    int _baselineMoney;
    int _baselinePriceConfirmCount;
    int _randomNameIndex;

    static readonly string[] RandomNames =
    {
        "하늘", "보리", "나루", "이든", "유나", "도현"
    };

    static readonly Dictionary<Stage, string> Labels = new Dictionary<Stage, string>
    {
        { Stage.TalkToNpc,       "1단계: 첫 이주자에게 다가가 [Space] 로 대화" },
        { Stage.StockShopSlot,   "2단계: 빈 판매대 앞에서 [Space] 로 보급품 진열" },
        { Stage.SetPrice,        "3단계: 진열한 판매대를 다시 [Space] 로 가격 확정" },
        { Stage.WaitForPurchase, "4단계: 첫 손님이 구매할 때까지 기다리기" },
        { Stage.OpenAuditApp,    "5단계: [P] 스마트폰을 열고 감사 앱 확인" },
        { Stage.SaveProgress,    "6단계: [F5] 로 첫날 진행 저장" },
        { Stage.Done,            "Day 1 목표 완료. 자유롭게 플레이하세요." },
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (autoCreateUI && objectiveText == null) BuildObjectiveUI();
        if (autoCreateUI && _flowCanvas == null) BuildFlowUI();

        CaptureBaselines();
        SubscribeInputs();

        if (showStartupFlow && !_startupCompleted)
            BeginStartupFlow();
        else
            RefreshLabel();
    }

    void OnDestroy()
    {
        UnsubscribeInputs();
        if (_startupPausedTime) Time.timeScale = _timeScaleBeforeStartup;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!_startupCompleted) return;

        if (!_everSawDialogueOpen && DialogueUI.instance != null && DialogueUI.IsOpen)
            _everSawDialogueOpen = true;

        if (!_everSawSmartphoneAudit && SmartphoneUI.instance != null)
            _everSawSmartphoneAudit = SmartphoneUI.instance.IsOpen
                && SmartphoneUI.instance.CurrentTabIndex == 0;

        switch (Current)
        {
            case Stage.TalkToNpc:
                if (_everSawDialogueOpen) Advance();
                break;
            case Stage.StockShopSlot:
                if (AnyShopSlotStocked()) Advance();
                break;
            case Stage.SetPrice:
                if (ShopPriceUI.instance != null
                    && ShopPriceUI.instance.ConfirmCount > _baselinePriceConfirmCount
                    && AnyShopSlotPriced()) Advance();
                break;
            case Stage.WaitForPurchase:
                if (!_forcedFirstBuyer) ForceFirstBuyerVisit();
                if (EconomyService.Instance != null
                    && EconomyService.Instance.CumulativeRevenue > _baselineRevenue) Advance();
                break;
            case Stage.OpenAuditApp:
                if (_everSawSmartphoneAudit) Advance();
                break;
            case Stage.SaveProgress:
                if (_everSawSave) Advance();
                break;
            case Stage.Done:
                if (!_summaryShown) ShowDaySummary();
                break;
        }
    }

    void SubscribeInputs()
    {
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnSave += OnSavePressed;
    }

    void UnsubscribeInputs()
    {
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnSave -= OnSavePressed;
    }

    void OnSavePressed()
    {
        _everSawSave = true;
    }

    void CaptureBaselines()
    {
        _baselineRevenue = EconomyService.Instance != null ? EconomyService.Instance.CumulativeRevenue : 0L;
        _baselineMoney = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;
        _baselinePriceConfirmCount = ShopPriceUI.instance != null ? ShopPriceUI.instance.ConfirmCount : 0;
    }

    void BeginStartupFlow()
    {
        _timeScaleBeforeStartup = Time.timeScale;
        Time.timeScale = 0f;
        _startupPausedTime = true;
        _startupStep = StartupStep.Name;
        SetFlowVisible(true);
        ShowStartupStep();
    }

    void CompleteStartupFlow()
    {
        _startupCompleted = true;
        EnsureRuntimeStarterSupplies();

        SetFlowVisible(false);
        if (_startupPausedTime)
        {
            Time.timeScale = _timeScaleBeforeStartup;
            _startupPausedTime = false;
        }

        CaptureBaselines();
        RefreshLabel();

        if (DialogueUI.instance != null)
        {
            DialogueUI.instance.Show("P.A. Phone",
                $"{PlayerName}님, {SelectedMapName} 현장 접속이 완료되었습니다. 첫 거래를 성사시켜 섬의 경제를 움직여 보세요.");
        }
    }

    void ShowStartupStep()
    {
        if (_flowPanel == null) return;

        SetNameInputVisible(_startupStep == StartupStep.Name);
        SetMapButtonsVisible(_startupStep == StartupStep.MapSelect);
        if (_secondaryButton != null) _secondaryButton.gameObject.SetActive(false);

        switch (_startupStep)
        {
            case StartupStep.Name:
                SetFlowText(
                    "개척자 등록",
                    "개척자 지원 본부의 현장 운영자 등록 절차입니다.\n게임 내에서 NPC와 스마트폰이 이 이름으로 당신을 부릅니다.",
                    "등록 완료");
                if (_nameInput != null && string.IsNullOrWhiteSpace(_nameInput.text))
                    _nameInput.text = PlayerName;
                break;
            case StartupStep.Briefing:
                SetFlowText(
                    "개척자 지원 본부",
                    $"{PlayerName}님, 이번 무인도 프로젝트의 실패 원인은 자원 부족이 아니라 유통망 부재였습니다.\n당신의 임무는 섬에 첫 상점을 세우고, 주민이 머물 수 있는 경제를 만드는 것입니다.",
                    "임무 확인");
                break;
            case StartupStep.MapSelect:
                SetFlowText(
                    "개척 지도 선택",
                    "프로토타입에서는 균형형 지역인 초록빛 만을 플레이합니다.\n다른 지역은 이후 생태계와 자원 특화 맵으로 확장됩니다.",
                    "초록빛 만으로 파견");
                break;
            case StartupStep.PhoneIntro:
                SetFlowText(
                    "P.A. Phone 지급",
                    "스마트폰은 현장 조력자입니다.\n감사 목표, 상점 알림, 피드, 설정을 확인할 수 있고 첫날 목표도 여기서 추적됩니다.",
                    "스마트폰 수령");
                break;
            case StartupStep.Supplies:
                SetFlowText(
                    "초기 지급 물품",
                    "보급 상자에 상점 텐트 키트, 판매대, 나무, 돌, 빵, 당근, 판자가 지급되었습니다.\n첫 판매를 시작할 준비가 끝났습니다.",
                    "보급품 확인");
                break;
            case StartupStep.Arrival:
                SetFlowText(
                    $"{SelectedMapName} 도착",
                    "첫 이주자가 상점 예정지 근처에서 기다리고 있습니다.\n대화 후 판매대에 물건을 올리고 가격을 정해 첫 거래를 성사시키세요.",
                    "첫날 시작");
                break;
        }
    }

    void SetFlowText(string title, string body, string primary)
    {
        if (_flowTitle != null) _flowTitle.text = title;
        if (_flowBody != null) _flowBody.text = body;
        if (_primaryText != null) _primaryText.text = primary;
    }

    void AdvanceStartupStep()
    {
        if (_startupStep == StartupStep.Name)
        {
            string typed = _nameInput != null ? _nameInput.text.Trim() : string.Empty;
            PlayerName = string.IsNullOrEmpty(typed) ? RandomNames[_randomNameIndex % RandomNames.Length] : typed;
        }

        if (_startupStep == StartupStep.Arrival)
        {
            CompleteStartupFlow();
            return;
        }

        _startupStep++;
        ShowStartupStep();
    }

    void PickRandomName()
    {
        _randomNameIndex = (_randomNameIndex + 1) % RandomNames.Length;
        PlayerName = RandomNames[_randomNameIndex];
        if (_nameInput != null) _nameInput.text = PlayerName;
    }

    void SelectMap(string mapId)
    {
        SelectedMapId = mapId;
        RefreshMapButtonColors();
    }

    void Advance()
    {
        var prev = Current;
        Current = (Stage)((int)Current + 1);

        if (Current == Stage.SetPrice)
            _baselinePriceConfirmCount = ShopPriceUI.instance != null ? ShopPriceUI.instance.ConfirmCount : 0;

        if (Current == Stage.WaitForPurchase)
            _forcedFirstBuyer = false;

        Debug.Log($"[PlayableDay] {prev} -> {Current}");
        OnObjectiveComplete?.Invoke(prev);
        RefreshLabel();
    }

    void RefreshLabel()
    {
        if (objectiveText == null) return;
        string label = GetStageLabel(Current);
        objectiveText.text = $"{PlayerName} · {SelectedMapName}\n{label}";
    }

    static string GetStageLabel(Stage stage)
    {
        return stage switch
        {
            Stage.TalkToNpc => "1단계: 머리 위 '첫 이주자' 표식이 있는 NPC에게 다가가 [Space] 대화",
            Stage.StockShopSlot => "2단계: 노란 '판매대 슬롯' 앞에서 [Space]를 누르면 핫바의 판매 아이템이 자동 진열됩니다",
            Stage.SetPrice => "3단계: 상품이 올라간 같은 판매대에서 [Space] → 가격 확정",
            Stage.WaitForPurchase => "4단계: NPC가 판매대까지 와서 구매할 때까지 잠시 기다리기",
            Stage.OpenAuditApp => "5단계: [P] 스마트폰 열기 → 감사 앱 확인",
            Stage.SaveProgress => "6단계: [F5]로 첫날 진행 저장",
            Stage.Done => "Day 1 목표 완료. 결산을 확인하고 자유롭게 둘러보세요.",
            _ => "(목표 데이터 없음)"
        };
    }

    bool AnyShopSlotStocked()
    {
        foreach (var s in UnityEngine.Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
            if (s != null && !s.IsEmpty) return true;
        return false;
    }

    bool AnyShopSlotPriced()
    {
        foreach (var s in UnityEngine.Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
            if (s != null && !s.IsEmpty && s.displayPrice > 0) return true;
        return false;
    }

    void ForceFirstBuyerVisit()
    {
        _forcedFirstBuyer = true;
        var npcs = UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);
        int forced = 0;
        foreach (var npc in npcs)
        {
            if (npc == null) continue;
            npc.SetShoppingPriority(true);
            npc.TryForceShop();
            forced++;
            if (forced >= 3) break;
        }
    }

    void EnsureRuntimeStarterSupplies()
    {
        if (Inventory.instance == null || HasSellableItems()) return;

        AddStarterItem("Items/Item_BreadLoaf", 3);
        AddStarterItem("Items/Item_Carrot", 5);
        AddStarterItem("Items/Item_Plank", 6);
        AddStarterItem("Items/Item_Wood", 10);
        AddStarterItem("Items/Item_Ore", 8);
    }

    bool HasSellableItems()
    {
        int count = 0;
        CountSellable(Inventory.instance.slots, ref count);
        if (Inventory.instance.hotbar != null) CountSellable(Inventory.instance.hotbar.slots, ref count);
        return count >= 3;
    }

    void CountSellable(List<InventorySlot> slots, ref int count)
    {
        if (slots == null) return;
        foreach (var slot in slots)
        {
            if (slot == null || slot.IsEmpty || slot.item == null) continue;
            if (slot.item.category != ItemCategory.Tool && slot.item.toolType == ToolType.None)
                count += slot.count;
        }
    }

    void AddStarterItem(string resourcePath, int count)
    {
        var item = Resources.Load<Item>(resourcePath);
        if (item == null || Inventory.instance == null) return;

        var inst = new ItemInstance(item, Mathf.Clamp(count, 1, item.maxStack))
        {
            quality = 1f,
            currentPrice = item.basePrice
        };

        if (Inventory.instance.hotbar != null && AddToFirstEmpty(Inventory.instance.hotbar.slots, inst))
            return;

        Inventory.instance.AddInstance(inst);
    }

    bool AddToFirstEmpty(List<InventorySlot> slots, ItemInstance inst)
    {
        if (slots == null || inst == null) return false;
        foreach (var slot in slots)
        {
            if (slot == null || !slot.IsEmpty) continue;
            slot.SetInstance(inst);
            return true;
        }
        return false;
    }

    void ShowDaySummary()
    {
        _summaryShown = true;
        SetFlowVisible(true);
        SetNameInputVisible(false);
        SetMapButtonsVisible(false);

        if (_startupPausedTime == false)
        {
            _timeScaleBeforeStartup = Time.timeScale;
            Time.timeScale = 0f;
            _startupPausedTime = true;
        }

        int money = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;
        long revenue = EconomyService.Instance != null ? EconomyService.Instance.CumulativeRevenue : 0L;
        long earnedToday = Math.Max(0L, revenue - _baselineRevenue);
        int moneyDelta = EconomyService.Instance != null ? money - _baselineMoney : 0;

        // 오늘 판매 건수 — SalesLogManager.GetRecent 에서 day == CurrentDay 만 카운트
        int salesToday = 0;
        if (SalesLogManager.Instance != null && GameClock.Instance != null)
        {
            int today = GameClock.Instance.CurrentDay;
            var records = SalesLogManager.Instance.GetRecent(50);
            if (records != null)
                foreach (var r in records)
                    if (r != null && r.gameDay == today) salesToday++;
        }

        // 보리(첫 이주민) 친밀도 변화
        int boriPoints = FriendshipService.Instance != null
            ? FriendshipService.Instance.GetPoints("bori")
            : 0;

        SetFlowText(
            "Day 1 결산",
            $"{PlayerName}님의 첫 개척일이 마무리되었습니다.\n\n" +
            $"선택 지도: {SelectedMapName}\n" +
            $"오늘 매출: {earnedToday} G\n" +
            $"현재 보유금: {money} G ({moneyDelta:+#;-#;0})\n" +
            $"판매한 상품: {salesToday} 개\n" +
            $"보리와의 친밀도: {boriPoints}\n\n" +
            "📩 피드: 농부가 '내일부터 작물을 가져갈게요' 라고 남겼습니다.\n\n" +
            "다음 감사 목표 (Tier 0 · 생존자)\n" +
            "  • 총 매출 500G\n" +
            "  • NPC 1명 이상과 대화\n" +
            "  • 상품 3개 이상 판매\n" +
            "  • 하루 종료 전 저장",
            "계속 플레이");
    }

    void CloseSummary()
    {
        SetFlowVisible(false);
        if (_startupPausedTime)
        {
            Time.timeScale = _timeScaleBeforeStartup;
            _startupPausedTime = false;
        }
    }

    void BuildObjectiveUI()
    {
        var canvasGo = new GameObject("PlayableDayGuideCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        guideCanvas = canvasGo.GetComponent<Canvas>();
        guideCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        guideCanvas.sortingOrder = 50;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var bgGo = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 1f);
        bgRt.anchorMax = new Vector2(0.5f, 1f);
        bgRt.pivot = new Vector2(0.5f, 1f);
        bgRt.anchoredPosition = anchorOffset;
        bgRt.sizeDelta = new Vector2(860f, 86f);
        bgGo.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.02f, 0.68f);

        var txtGo = new GameObject("ObjectiveText", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(bgGo.transform, false);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(20f, 8f);
        txtRt.offsetMax = new Vector2(-20f, -8f);
        objectiveText = txtGo.GetComponent<TextMeshProUGUI>();
        objectiveText.fontSize = 24f;
        objectiveText.alignment = TextAlignmentOptions.Center;
        objectiveText.color = Color.white;
        objectiveText.raycastTarget = false;
    }

    void BuildFlowUI()
    {
        var canvasGo = new GameObject("FirstDayPrototypeCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _flowCanvas = canvasGo.GetComponent<Canvas>();
        _flowCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _flowCanvas.sortingOrder = 220;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(canvasGo.transform, false);
        Stretch((RectTransform)dim.transform);
        dim.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.05f, 0.84f);

        _flowPanel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        _flowPanel.transform.SetParent(canvasGo.transform, false);
        var panelRt = (RectTransform)_flowPanel.transform;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(920f, 620f);
        _flowPanel.GetComponent<Image>().color = new Color(0.97f, 0.94f, 0.86f, 0.98f);

        _flowTitle = CreateText(_flowPanel.transform, "Title", new Vector2(0f, 235f), new Vector2(820f, 70f), 38f, new Color(0.08f, 0.42f, 0.27f), FontStyles.Bold);
        _flowBody = CreateText(_flowPanel.transform, "Body", new Vector2(0f, 70f), new Vector2(780f, 210f), 24f, new Color(0.08f, 0.10f, 0.12f), FontStyles.Normal);
        _flowBody.alignment = TextAlignmentOptions.TopLeft;

        _nameInput = CreateInput(_flowPanel.transform);
        _randomNameButton = CreateButton(_flowPanel.transform, "RandomNameButton", "랜덤 이름", new Vector2(245f, -78f), new Vector2(190f, 52f), PickRandomName);

        _mapButtonRoot = new GameObject("MapButtons", typeof(RectTransform));
        _mapButtonRoot.transform.SetParent(_flowPanel.transform, false);
        var mapRt = (RectTransform)_mapButtonRoot.transform;
        mapRt.anchorMin = new Vector2(0.5f, 0.5f);
        mapRt.anchorMax = new Vector2(0.5f, 0.5f);
        mapRt.pivot = new Vector2(0.5f, 0.5f);
        mapRt.anchoredPosition = new Vector2(0f, -95f);
        mapRt.sizeDelta = new Vector2(810f, 150f);

        _mapButtons.Add(CreateMapButton("green_bay", "초록빛 만\n균형형 · 프로토타입", -280f, true));
        _mapButtons.Add(CreateMapButton("wind_hill", "바람 언덕\n농업 특화 · 준비 중", 0f, false));
        _mapButtons.Add(CreateMapButton("shell_port", "조개 항구\n무역 특화 · 준비 중", 280f, false));

        _primaryButton = CreateButton(_flowPanel.transform, "PrimaryButton", "다음", new Vector2(150f, -250f), new Vector2(240f, 58f), OnPrimaryPressed);
        _secondaryButton = CreateButton(_flowPanel.transform, "SecondaryButton", "닫기", new Vector2(-150f, -250f), new Vector2(200f, 58f), CloseSummary);
        _primaryText = _primaryButton.GetComponentInChildren<TextMeshProUGUI>();
        _secondaryText = _secondaryButton.GetComponentInChildren<TextMeshProUGUI>();

        SetFlowVisible(false);
    }

    void OnPrimaryPressed()
    {
        if (Current == Stage.Done && _summaryShown) CloseSummary();
        else AdvanceStartupStep();
    }

    Button CreateMapButton(string mapId, string label, float x, bool enabled)
    {
        var button = CreateButton(_mapButtonRoot.transform, $"Map_{mapId}", label, new Vector2(x, 0f), new Vector2(250f, 130f), () => SelectMap(mapId));
        button.interactable = enabled;
        return button;
    }

    TMP_InputField CreateInput(Transform parent)
    {
        var go = new GameObject("NameInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-80f, -78f);
        rt.sizeDelta = new Vector2(430f, 56f);
        go.GetComponent<Image>().color = Color.white;

        var text = CreateText(go.transform, "Text", Vector2.zero, new Vector2(390f, 44f), 24f, Color.black, FontStyles.Normal);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        var placeholder = CreateText(go.transform, "Placeholder", Vector2.zero, new Vector2(390f, 44f), 22f, new Color(0f, 0f, 0f, 0.35f), FontStyles.Normal);
        placeholder.text = "이름을 입력하세요";
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;

        var input = go.GetComponent<TMP_InputField>();
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = 12;
        input.text = PlayerName;
        return input;
    }

    Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.08f, 0.42f, 0.27f, 1f);

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        var txt = CreateText(go.transform, "Label", Vector2.zero, size - new Vector2(18f, 12f), 20f, Color.white, FontStyles.Bold);
        txt.text = label;
        txt.alignment = TextAlignmentOptions.Center;
        return btn;
    }

    TextMeshProUGUI CreateText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, Color color, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    void SetFlowVisible(bool visible)
    {
        if (_flowCanvas != null) _flowCanvas.gameObject.SetActive(visible);
    }

    void SetNameInputVisible(bool visible)
    {
        if (_nameInput != null) _nameInput.gameObject.SetActive(visible);
        if (_randomNameButton != null) _randomNameButton.gameObject.SetActive(visible);
    }

    void SetMapButtonsVisible(bool visible)
    {
        if (_mapButtonRoot != null) _mapButtonRoot.SetActive(visible);
        if (visible) RefreshMapButtonColors();
    }

    void RefreshMapButtonColors()
    {
        foreach (var button in _mapButtons)
        {
            if (button == null) continue;
            var img = button.GetComponent<Image>();
            bool selected = button.gameObject.name.EndsWith(SelectedMapId, StringComparison.Ordinal);
            img.color = selected ? new Color(0.94f, 0.50f, 0.44f, 1f) : new Color(0.22f, 0.34f, 0.31f, 1f);
        }
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static string GetMapName(string id)
    {
        return id switch
        {
            "wind_hill" => "바람 언덕",
            "shell_port" => "조개 항구",
            _ => "초록빛 만",
        };
    }

    public void ResetScenario()
    {
        Current = Stage.TalkToNpc;
        _everSawDialogueOpen = false;
        _everSawSmartphoneAudit = false;
        _everSawSave = false;
        _forcedFirstBuyer = false;
        _summaryShown = false;
        CaptureBaselines();
        RefreshLabel();
    }

    public void RestoreSavedSession(string playerName, string selectedMapId, int stageIndex)
    {
        if (!string.IsNullOrWhiteSpace(playerName)) PlayerName = playerName.Trim();
        if (!string.IsNullOrWhiteSpace(selectedMapId)) SelectedMapId = selectedMapId.Trim();

        int max = (int)Stage.Done;
        Current = (Stage)Mathf.Clamp(stageIndex, 0, max);
        _startupCompleted = true;
        _summaryShown = Current == Stage.Done;
        SetFlowVisible(false);

        if (_startupPausedTime)
        {
            Time.timeScale = _timeScaleBeforeStartup;
            _startupPausedTime = false;
        }

        CaptureBaselines();
        RefreshLabel();
    }
}
