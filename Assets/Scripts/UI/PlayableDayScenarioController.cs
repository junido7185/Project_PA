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
    // Visual Demo Integration Pass v2 — 좌측 퀘스트 패널 (상단 중앙은 현재 단계 한 줄만 유지).
    public TextMeshProUGUI questListText;

    [Header("첫날 프로토타입")]
    public bool showStartupFlow = true;
    public bool autoCreateUI = true;
    public Vector2 anchorOffset = new Vector2(0f, -26f);

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
    readonly List<string> _managementFeedback = new List<string>();

    static readonly string[] RandomNames =
    {
        "하늘", "보리", "나루", "이든", "유나", "도현"
    };

    static readonly Dictionary<Stage, string> Labels = new Dictionary<Stage, string>
    {
        { Stage.TalkToNpc,       "1단계: 첫 이주자에게 공급망 운영 브리핑 듣기" },
        { Stage.StockShopSlot,   "2단계: NPC 생산물/보급품을 상점 경제에 진열" },
        { Stage.SetPrice,        "3단계: 가격을 정해 고객 반응을 예측" },
        { Stage.WaitForPurchase, "4단계: NPC 구매/거절 이유와 매출 변화 확인" },
        { Stage.OpenAuditApp,    "5단계: [P] 스마트폰에서 감사/티어 목표 확인" },
        { Stage.SaveProgress,    "6단계: [F5] 로 운영 기록 저장" },
        { Stage.Done,            "Day 1 목표 완료. 결산을 보고 다음 운영 전략을 정하세요." },
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

    public void RecordManagementFeedback(string feedback)
    {
        if (string.IsNullOrWhiteSpace(feedback)) return;
        _managementFeedback.Add(feedback.Trim());
        while (_managementFeedback.Count > 8)
            _managementFeedback.RemoveAt(0);
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

        SetFlowBodyPresentation(summary: false);
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
                    $"{PlayerName}님, 이번 정착지의 문제는 자원 부족이 아니라 생산물의 흐름이 끊긴 것입니다.\n당신의 임무는 직접 모든 일을 하는 것이 아니라, NPC 생산물의 매입·진열·가격·판매를 관리해 경제 순환을 여는 것입니다.",
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
                    "보급 상자에 첫 판매용 상품과 상점 운영 키트가 지급되었습니다.\n이 물품은 단순 소모품이 아니라 첫 가격 실험과 고객 반응을 확인할 운영 자산입니다.",
                    "보급품 확인");
                break;
            case StartupStep.Arrival:
                SetFlowText(
                    $"{SelectedMapName} 도착",
                    "첫 이주자가 상점 예정지 근처에서 기다리고 있습니다.\n대화 후 상품을 진열하고 가격을 정해, NPC가 왜 구매하거나 거절하는지 확인하세요.",
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

    void SetFlowBodyPresentation(bool summary)
    {
        if (_flowBody == null) return;

        var rt = (RectTransform)_flowBody.transform;
        if (summary)
        {
            // Visual Demo Integration Pass — 요약 본문이 마을 변화/피드백 섹션 추가로 길어져
            // 제목 아래~버튼 위 사이 여백을 전부 사용하도록 확장 (410px 본문 수용).
            rt.anchoredPosition = new Vector2(0f, -9f);
            rt.sizeDelta = new Vector2(810f, 418f);
            _flowBody.fontSize = 18f;
            _flowBody.lineSpacing = 0f;
            _flowBody.alignment = TextAlignmentOptions.TopLeft;
            _flowBody.textWrappingMode = TextWrappingModes.Normal;
            _flowBody.overflowMode = TextOverflowModes.Ellipsis;
            return;
        }

        rt.anchoredPosition = new Vector2(0f, 70f);
        rt.sizeDelta = new Vector2(780f, 210f);
        _flowBody.fontSize = 24f;
        _flowBody.lineSpacing = 0f;
        _flowBody.alignment = TextAlignmentOptions.TopLeft;
        _flowBody.textWrappingMode = TextWrappingModes.Normal;
        _flowBody.overflowMode = TextOverflowModes.Ellipsis;
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
        // v2 — 상단 중앙은 현재 단계 한 줄만. 이름/지도와 단계 목록은 좌측 퀘스트 패널이 담당.
        if (objectiveText != null)
            objectiveText.text = GetStageLabel(Current);

        RefreshQuestList();
    }

    // v2 — 좌측 퀘스트 패널: 완료(✓)/진행(▶)/대기(○) 체크리스트.
    void RefreshQuestList()
    {
        if (questListText == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<color=#FFE9B8><b>{PlayerName} · {SelectedMapName}</b></color>");

        Stage[] order =
        {
            Stage.TalkToNpc, Stage.StockShopSlot, Stage.SetPrice,
            Stage.WaitForPurchase, Stage.OpenAuditApp, Stage.SaveProgress
        };

        foreach (var stage in order)
        {
            bool done = (int)Current > (int)stage;
            bool current = Current == stage;
            string label = Labels.TryGetValue(stage, out var text) ? text : stage.ToString();

            if (done) sb.AppendLine($"<color=#8FCF9A>✓ {label}</color>");
            else if (current) sb.AppendLine($"<color=#FFFFFF>▶ {label}</color>");
            else sb.AppendLine($"<color=#9A968C>○ {label}</color>");
        }

        questListText.text = sb.ToString().TrimEnd();
    }

    static string GetStageLabel(Stage stage)
    {
        return stage switch
        {
            Stage.TalkToNpc => "1단계: 첫 이주자에게 [Space] 대화 - NPC 생산물과 상점 운영 흐름을 확인하세요",
            Stage.StockShopSlot => "2단계: 판매대 슬롯 앞 [Space] - 상품을 상점 경제에 진열하세요",
            Stage.SetPrice => "3단계: 진열한 슬롯에서 [Space] - 가격을 정하고 예상 구매 반응을 확인하세요",
            Stage.WaitForPurchase => "4단계: NPC 고객의 구매/거절 이유와 매출 변화를 관찰하세요",
            Stage.OpenAuditApp => "5단계: [P] 스마트폰 - 감사 앱에서 Tier 0 운영 목표를 확인하세요",
            Stage.SaveProgress => "6단계: [F5] 저장 - 오늘의 운영 기록을 보존하세요",
            Stage.Done => "Day 1 목표 완료. 결산을 보고 다음 가격/재고 전략을 정하세요.",
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
        NpcBubbleUI.HideAll();
        _summaryShown = true;
        SetFlowVisible(true);
        SetNameInputVisible(false);
        SetMapButtonsVisible(false);
        SetFlowBodyPresentation(summary: true);

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

        int today = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        SalesLogManager.DailyDecisionStats decisionStats = SalesLogManager.Instance != null
            ? SalesLogManager.Instance.GetDailyDecisionStats(today)
            : default;
        string decisionSummary = SalesLogManager.Instance != null
            ? SalesLogManager.Instance.BuildDailyDecisionSummary(today)
            : "손님 판단 통계를 확인할 수 없습니다.";

        // 보리(첫 이주민) 친밀도 변화
        int boriPoints = FriendshipService.Instance != null
            ? FriendshipService.Instance.GetPoints("bori")
            : 0;

        string feedbackSummary = BuildFeedbackSummary();
        string nextAction = SalesLogManager.Instance != null && decisionStats.evaluations > 0
            ? SalesLogManager.Instance.BuildNextDayAdvice(today)
            : earnedToday > 0
                ? "다음 준비: 잘 팔린 가격대를 기준으로 재고를 보충하고, 더 높은 가치의 가공품을 준비하세요."
                : "다음 준비: 가격을 낮추거나 NPC 선호에 맞는 상품을 다시 진열하세요.";
        string tierGoalSummary = BuildTierGoalSummary();
        string villageSignalSummary = BuildVillageSignalSummary();

        SetFlowText(
            "Day 1 결산",
            $"{PlayerName}님의 첫 운영일이 마무리되었습니다.\n\n" +
            $"선택 지도: {SelectedMapName}\n" +
            $"오늘 매출: {earnedToday} G\n" +
            $"현재 보유금: {money} G ({moneyDelta:+#;-#;0})\n" +
            $"손님 판단: {decisionSummary}\n" +
            $"보리와의 친밀도: {boriPoints}\n\n" +
            $"구매/거절 피드백:\n{feedbackSummary}\n\n" +
            $"{nextAction}\n\n" +
            $"Village direction:\n{villageSignalSummary}\n\n" +
            $"{tierGoalSummary}\n" +
            "운영 체크: 재고 보충, 가격 재조정, 저장 기록 확인",
            "계속 플레이");
    }

    string BuildFeedbackSummary()
    {
        if (_managementFeedback.Count == 0)
            return "  • 아직 고객 판단 기록이 없습니다. 다음 손님 반응을 보고 가격을 조정하세요.";

        int start = Math.Max(0, _managementFeedback.Count - 3);
        var lines = new List<string>();
        for (int i = start; i < _managementFeedback.Count; i++)
            lines.Add($"  • {_managementFeedback[i]}");

        return string.Join("\n", lines);
    }

    string BuildTierGoalSummary()
    {
        if (TierService.Instance == null)
            return "다음 성장 목표\n  • 티어 서비스를 확인할 수 없습니다. 감사 앱에서 목표를 다시 확인하세요.";

        int currentTier = TierService.Instance.CurrentTier;
        var current = TierService.Instance.GetDefinition(currentTier);
        var next = TierService.Instance.GetDefinition(currentTier + 1);

        string currentName = current != null && !string.IsNullOrEmpty(current.tierName)
            ? current.tierName
            : $"Tier {currentTier}";

        if (next == null)
            return $"다음 성장 목표\n  • 현재 Tier {currentTier} · {currentName}: 최고 티어 운영 안정화 단계입니다.";

        string nextName = string.IsNullOrEmpty(next.tierName) ? $"Tier {next.tier}" : next.tierName;
        var lines = new List<string>
        {
            $"다음 성장 목표 (현재 Tier {currentTier} · {currentName} → Tier {next.tier} · {nextName})"
        };

        long revenue = EconomyService.Instance != null ? EconomyService.Instance.CumulativeRevenue : 0L;
        if (next.requiredCumulativeRevenue > 0)
        {
            long remaining = next.requiredCumulativeRevenue - revenue;
            if (remaining < 0) remaining = 0;
            lines.Add($"  • 누적 매출 {revenue:N0}G / {next.requiredCumulativeRevenue:N0}G ({remaining:N0}G 남음)");
        }

        if (next.requiredReputation > 0)
        {
            int reputation = TierService.Instance.Reputation;
            int remaining = next.requiredReputation - reputation;
            if (remaining < 0) remaining = 0;
            lines.Add($"  • 평판 {reputation} / {next.requiredReputation} ({remaining} 남음)");
        }

        if (next.requiresManualApproval)
            lines.Add("  • 본사 감사 승인 필요");

        if (lines.Count == 1)
            lines.Add("  • 감사 앱에서 다음 운영 조건을 확인하세요.");

        return string.Join("\n", lines);
    }

    string BuildVillageSignalSummary()
    {
        if (VillageChangeSignalController.Instance == null)
            return "  Village signal is not available yet.";

        VillageChangeSignalController.Instance.RefreshNow();
        string summary = VillageChangeSignalController.Instance.GetLeadingSignalSummary();
        if (string.IsNullOrWhiteSpace(summary) || summary.Contains("No village"))
            return "  Sell products to reveal the first village-change signal.";

        return $"  {summary}";
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
        Vector2 objectiveOffset = anchorOffset;
        if (objectiveOffset.y < -48f)
            objectiveOffset.y = -26f;
        bgRt.anchoredPosition = objectiveOffset;
        // v2 — 상단 중앙은 현재 단계 한 줄만 (기존 2줄 72px → 1줄 44px).
        bgRt.sizeDelta = new Vector2(780f, 44f);
        bgGo.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.02f, 0.68f);

        var txtGo = new GameObject("ObjectiveText", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(bgGo.transform, false);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(20f, 6f);
        txtRt.offsetMax = new Vector2(-20f, -6f);
        objectiveText = txtGo.GetComponent<TextMeshProUGUI>();
        objectiveText.fontSize = 19f;
        objectiveText.alignment = TextAlignmentOptions.Center;
        objectiveText.color = Color.white;
        objectiveText.raycastTarget = false;
        objectiveText.textWrappingMode = TextWrappingModes.NoWrap;
        objectiveText.overflowMode = TextOverflowModes.Ellipsis;

        BuildQuestPanel(canvasGo.transform);
    }

    // v2 — 좌측 퀘스트 패널 (ClockHUD/페이즈 스트립 아래, 좌측 컬럼 정렬).
    void BuildQuestPanel(Transform parent)
    {
        var panelGo = new GameObject("DemoQuestPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(parent, false);
        var rt = panelGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -184f);
        rt.sizeDelta = new Vector2(340f, 172f);

        var bg = panelGo.GetComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.02f, 0.55f);
        bg.raycastTarget = false;

        var txtGo = new GameObject("QuestListText", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(panelGo.transform, false);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(12f, 8f);
        txtRt.offsetMax = new Vector2(-12f, -8f);

        questListText = txtGo.GetComponent<TextMeshProUGUI>();
        questListText.fontSize = 12.5f;
        questListText.alignment = TextAlignmentOptions.TopLeft;
        questListText.color = Color.white;
        questListText.raycastTarget = false;
        questListText.textWrappingMode = TextWrappingModes.NoWrap;
        questListText.overflowMode = TextOverflowModes.Ellipsis;
        questListText.lineSpacing = 6f;
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
