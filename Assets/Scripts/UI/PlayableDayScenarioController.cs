using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// First playable prototype flow:
// name registration -> briefing -> map selection -> smartphone/controls/supplies intro ->
// in-world first sale loop -> audit app -> save -> day summary.
public class PlayableDayScenarioController : MonoBehaviour
{
    const string BlacksmithForgeDefinitionId = "Blueprint_B07_BlacksmithForge";
    const string KitchenStationDefinitionId = "Blueprint_B06_KitchenStation";
    const string SewingTableDefinitionId = "Blueprint_B08_SewingTable";
    const string ToolSetResourcePath = "Items/Item_12_ToolSet";
    const string BreadLoafResourcePath = "Items/Item_BreadLoaf";
    const string BakedPotatoResourcePath = "Items/Item_09_BakedPotato";
    const string GrilledFishResourcePath = "Items/Item_10_GrilledFish";
    const string FurnitureResourcePath = "Items/Item_11_Furniture";
    const string ClothesResourcePath = "Items/Item_13_Clothes";

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
        Title,
        NewGameConfirm,
        Name,
        Briefing,
        MapSelect,
        PhoneIntro,
        Controls,
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
    Button _quitButton;
    TextMeshProUGUI _primaryText;
    TextMeshProUGUI _secondaryText;

    StartupStep _startupStep = StartupStep.Title;
    bool _startupCompleted;
    bool _startupPausedTime;
    bool _continueSaveAvailable;
    bool _continueLoading;
    int _continueCheckVersion;
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
    float _nextContinuationRefreshAt;
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

        if (Current == Stage.Done && CurrentGameDay > 1
            && Time.unscaledTime >= _nextContinuationRefreshAt)
        {
            _nextContinuationRefreshAt = Time.unscaledTime + 0.5f;
            RefreshLabel();
        }

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
        if (GameClock.Instance != null)
            GameClock.Instance.OnNewDay += OnNewDay;
    }

    void UnsubscribeInputs()
    {
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnSave -= OnSavePressed;
        if (GameClock.Instance != null)
            GameClock.Instance.OnNewDay -= OnNewDay;
    }

    void OnNewDay(int _)
    {
        if (Current == Stage.Done)
        {
            _nextContinuationRefreshAt = 0f;
            RefreshLabel();
        }
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
        _startupStep = StartupStep.Title;
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
        bool showTitleControls = _startupStep == StartupStep.Title;
        SetTitleButtonLayout(showTitleControls);
        if (_primaryButton != null) _primaryButton.interactable = true;
        if (_secondaryButton != null)
        {
            _secondaryButton.gameObject.SetActive(false);
            _secondaryButton.interactable = true;
        }
        if (_quitButton != null)
        {
            _quitButton.gameObject.SetActive(showTitleControls);
            _quitButton.interactable = true;
        }

        switch (_startupStep)
        {
            case StartupStep.Title:
                SetFlowText(
                    "PROJECT P.A.",
                    "낮에는 동물 마을에서 재료와 상품을 준비하고, 밤에는 마을의 유일한 잡화점을 운영하세요.\n당신이 판매한 물건은 다음 날 마을의 풍경과 생활을 바꿉니다.",
                    "새 게임");
                BeginContinueAvailabilityCheck();
                break;
            case StartupStep.NewGameConfirm:
                SetFlowText(
                    "기존 저장 기록 확인",
                    "현재 저장 기록이 있습니다.\n새 게임을 시작해도 지금 바로 삭제되지는 않지만, 이후 저장하면 기존 기록을 덮어씁니다.\n계속하시겠습니까?",
                    "새 게임 시작");
                if (_secondaryButton != null)
                {
                    _secondaryButton.gameObject.SetActive(true);
                    _secondaryButton.interactable = true;
                }
                if (_secondaryText != null) _secondaryText.text = "타이틀로 돌아가기";
                break;
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
            case StartupStep.Controls:
                SetFlowText(
                    "현장 조작 안내",
                    "이동 [WASD/방향키] · 상호작용 [Space] · 인벤토리 [I]\n" +
                    "스마트폰 [P] · 제작 [C] · 핫바 [1~9/마우스 휠]\n" +
                    "건설 배치 [좌클릭] · 회전 [R] · 이동 [M] · 회수 [X]\n" +
                    "저장 [F5] · 불러오기 [F9] · 일시정지 [ESC]\n\n" +
                    "화면 상단 목표와 가까운 오브젝트의 상호작용 안내를 따라 첫날을 시작하세요.",
                    "조작 확인");
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

    void BeginContinueAvailabilityCheck()
    {
        if (_secondaryButton == null) return;

        int requestVersion = ++_continueCheckVersion;
        _continueSaveAvailable = false;
        _continueLoading = false;
        if (_primaryButton != null) _primaryButton.interactable = false;
        _secondaryButton.gameObject.SetActive(true);
        _secondaryButton.interactable = false;
        if (_secondaryText != null) _secondaryText.text = "저장 확인 중";
        _ = RefreshContinueAvailabilityAsync(requestVersion);
    }

    async System.Threading.Tasks.Task RefreshContinueAvailabilityAsync(int requestVersion)
    {
        bool exists = false;
        try
        {
            exists = SaveManager.instance != null && await SaveManager.instance.HasSaveAsync();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PlayableDay] 저장 확인 실패: {ex.Message}");
        }

        if (this == null || requestVersion != _continueCheckVersion
            || _startupCompleted || _startupStep != StartupStep.Title)
            return;

        _continueSaveAvailable = exists;
        if (_primaryButton != null) _primaryButton.interactable = true;
        _secondaryButton.interactable = exists;
        if (_secondaryText != null) _secondaryText.text = exists ? "이어하기" : "저장 없음";
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
            objectiveText.text = Current == Stage.Done && CurrentGameDay > 1
                ? GetContinuationObjective(CurrentGameDay)
                : GetStageLabel(Current);

        RefreshQuestList();
    }

    // v2 — 좌측 퀘스트 패널: 완료(✓)/진행(▶)/대기(○) 체크리스트.
    void RefreshQuestList()
    {
        if (questListText == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<color=#FFE9B8><b>{PlayerName} · {SelectedMapName}</b></color>");

        if (Current == Stage.Done && CurrentGameDay > 1)
        {
            AppendContinuationChecklist(sb, CurrentGameDay);
            questListText.text = sb.ToString().TrimEnd();
            return;
        }

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

    int CurrentGameDay => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    string GetContinuationObjective(int day)
    {
        DayNightShopLoopController loop = DayNightShopLoopController.Instance;
        if (loop == null)
            return $"Day {day}: 낮 준비 → 밤 영업 → 정산 루프를 이어가세요.";

        if (loop.CurrentPhase == PADayNightPhase.Settlement)
            return $"Day {day} 정산: 가게 간판에서 하루를 마무리하고 다음 날을 시작하세요.";

        if (loop.CurrentPhase == PADayNightPhase.ShopOpen)
            return loop.IsShopOpenForCustomers
                ? $"Day {day} 영업 중: 손님 구매·보류 이유를 보고 재고와 가격을 조정하세요."
                : $"Day {day} 밤: 상품을 진열하고 가격을 확인한 뒤 간판에서 영업을 시작하세요.";

        if (day == 3)
            return "Day 3 낮: 어제 손님 반응을 참고해 상품 2종과 가격 전략을 바꿔보세요.";

        if (TryResolveWeekTwoMilestone(day, out bool weekTwoComplete,
            out string weekTwoCompleteLabel, out string weekTwoActionLabel))
        {
            return weekTwoComplete
                ? $"Day {day} 낮: 2주차 목표 완료 · {weekTwoCompleteLabel}. 오늘 밤 진열과 가격 전략을 이어가세요."
                : $"Day {day} 낮: {weekTwoActionLabel}";
        }

        if (TryResolveMonthOneMilestone(day, out bool monthOneComplete,
            out string monthOneCompleteLabel, out string monthOneActionLabel))
        {
            return monthOneComplete
                ? $"Day {day} 낮: 첫 달 목표 완료 · {monthOneCompleteLabel}. 오늘 밤 운영과 정산을 이어가세요."
                : $"Day {day} 낮: {monthOneActionLabel}";
        }

        if (TryResolveSecondMonthOpeningMilestone(day, out bool secondMonthComplete,
            out string secondMonthCompleteLabel, out string secondMonthActionLabel))
        {
            return secondMonthComplete
                ? $"Day {day} 낮: 두 번째 달 진입 목표 완료 · {secondMonthCompleteLabel}. 오늘 밤 운영과 정산을 이어가세요."
                : $"Day {day} 낮: {secondMonthActionLabel}";
        }

        if (TryResolveTierTwoCampaignMilestone(day, out bool tierTwoCampaignComplete,
            out string tierTwoCampaignCompleteLabel, out string tierTwoCampaignActionLabel))
        {
            return tierTwoCampaignComplete
                ? $"Day {day} 낮: Tier 2 성장 목표 완료 · {tierTwoCampaignCompleteLabel}. 오늘 밤 운영과 정산을 이어가세요."
                : $"Day {day} 낮: {tierTwoCampaignActionLabel}";
        }

        if (TryResolveTierTwoKitchenMilestone(day, out bool kitchenCampaignComplete,
            out string kitchenCampaignCompleteLabel, out string kitchenCampaignActionLabel))
        {
            return kitchenCampaignComplete
                ? $"Day {day} 낮: Tier 2 주방 목표 완료 · {kitchenCampaignCompleteLabel}. 오늘 밤 운영과 정산을 이어가세요."
                : $"Day {day} 낮: {kitchenCampaignActionLabel}";
        }

        if (TryResolveTierThreeCampaignMilestone(day, out bool tierThreeCampaignComplete,
            out string tierThreeCampaignCompleteLabel, out string tierThreeCampaignActionLabel))
        {
            return tierThreeCampaignComplete
                ? $"Day {day} 낮: Tier 3 공방 목표 완료 · {tierThreeCampaignCompleteLabel}. 오늘 밤 운영과 정산을 이어가세요."
                : $"Day {day} 낮: {tierThreeCampaignActionLabel}";
        }

        if (LongPlayProgressionController.TryGetPartnerCampaignStatus(day,
            out bool partnerComplete, out string partnerCompleteLabel, out string partnerActionLabel))
        {
            return partnerComplete
                ? $"Day {day} 자유 운영: {partnerCompleteLabel}. 오늘의 상품과 마을 방향을 직접 선택하세요."
                : $"Day {day} 최종 감사 준비: {partnerActionLabel}";
        }

        if (day >= 5)
        {
            int hiredCount = HiringService.Instance != null ? HiringService.Instance.HiredCount : 0;
            if (hiredCount <= 0)
                return $"Day {day} 낮: [P] P.A. Phone 채용 앱에서 첫 생산자 또는 전문가를 고용해 반복 준비를 줄이세요.";

            return $"Day {day} 낮: 지원 인력 {hiredCount}명과 함께 재고를 준비하고 오늘 밤 판매 전략을 정하세요.";
        }

        return $"Day {day} 낮: 생활 활동으로 오늘 밤 판매할 상품 2종을 준비하세요.";
    }

    void AppendContinuationChecklist(System.Text.StringBuilder sb, int day)
    {
        DayNightShopLoopController loop = DayNightShopLoopController.Instance;
        List<string> completedActivities = GetCompletedDayActivityLabels(loop);
        int preparedTypes = CountPreparedSellableTypes();
        bool stocked = AnyShopSlotStocked();
        bool priced = AnyShopSlotPriced();
        SalesLogManager.DailyDecisionStats sales = SalesLogManager.Instance != null
            ? SalesLogManager.Instance.GetDailyDecisionStats(day)
            : default;

        sb.AppendLine($"<color=#FFFFFF>▶ Day {day} 생활–상점 운영</color>");
        AppendChecklistLine(sb, completedActivities.Count > 0,
            completedActivities.Count > 0
                ? $"낮 활동: {string.Join("·", completedActivities)}"
                : "낮 활동: 낚시·채광·농사·주민 부탁");
        AppendChecklistLine(sb, preparedTypes >= 2,
            $"판매 상품 {Mathf.Min(preparedTypes, 2)}/2종 준비");

        if (day >= 5 && day < 8)
        {
            int hiredCount = HiringService.Instance != null ? HiringService.Instance.HiredCount : 0;
            string roster = LongPlayProgressionController.BuildHiredRosterSummary(1);
            AppendChecklistLine(sb, hiredCount > 0,
                hiredCount > 0
                    ? $"성장 지원 {hiredCount}명 · {roster}"
                    : "[P] P.A. Phone 채용 앱에서 첫 지원 인력 고용",
                hiredCount > 0 ? null : "성장");
        }

        if (TryResolveWeekTwoMilestone(day, out bool weekTwoComplete,
            out string weekTwoCompleteLabel, out string weekTwoActionLabel))
        {
            AppendChecklistLine(sb, weekTwoComplete,
                weekTwoComplete ? weekTwoCompleteLabel : weekTwoActionLabel,
                weekTwoComplete ? null : "2주차");
        }

        if (TryResolveMonthOneMilestone(day, out bool monthOneComplete,
            out string monthOneCompleteLabel, out string monthOneActionLabel))
        {
            AppendChecklistLine(sb, monthOneComplete,
                monthOneComplete ? monthOneCompleteLabel : monthOneActionLabel,
                monthOneComplete ? null : "첫 달");
        }

        if (TryResolveSecondMonthOpeningMilestone(day, out bool secondMonthComplete,
            out string secondMonthCompleteLabel, out string secondMonthActionLabel))
        {
            AppendChecklistLine(sb, secondMonthComplete,
                secondMonthComplete ? secondMonthCompleteLabel : secondMonthActionLabel,
                secondMonthComplete ? null : "두 번째 달");
        }

        if (TryResolveTierTwoCampaignMilestone(day, out bool tierTwoCampaignComplete,
            out string tierTwoCampaignCompleteLabel, out string tierTwoCampaignActionLabel))
        {
            AppendChecklistLine(sb, tierTwoCampaignComplete,
                tierTwoCampaignComplete ? tierTwoCampaignCompleteLabel : tierTwoCampaignActionLabel,
                tierTwoCampaignComplete ? null : "Tier 2 성장");
        }

        if (TryResolveTierTwoKitchenMilestone(day, out bool kitchenCampaignComplete,
            out string kitchenCampaignCompleteLabel, out string kitchenCampaignActionLabel))
        {
            AppendChecklistLine(sb, kitchenCampaignComplete,
                kitchenCampaignComplete ? kitchenCampaignCompleteLabel : kitchenCampaignActionLabel,
                kitchenCampaignComplete ? null : "Tier 2 주방");
        }

        if (TryResolveTierThreeCampaignMilestone(day, out bool tierThreeCampaignComplete,
            out string tierThreeCampaignCompleteLabel, out string tierThreeCampaignActionLabel))
        {
            AppendChecklistLine(sb, tierThreeCampaignComplete,
                tierThreeCampaignComplete ? tierThreeCampaignCompleteLabel : tierThreeCampaignActionLabel,
                tierThreeCampaignComplete ? null : "Tier 3 공방");
        }

        if (LongPlayProgressionController.TryGetPartnerCampaignStatus(day,
            out bool partnerComplete, out string partnerCompleteLabel, out string partnerActionLabel))
        {
            AppendChecklistLine(sb, partnerComplete,
                partnerComplete ? partnerCompleteLabel : partnerActionLabel,
                partnerComplete ? null : "최종 감사");
        }

        if (ProcessingOpportunityController.Instance != null
            && ProcessingOpportunityController.Instance.TryGetFurnitureLoopGuide(day, out var furnitureGuide))
        {
            AppendChecklistLine(sb, furnitureGuide.complete, furnitureGuide.label, furnitureGuide.pendingLabel);
        }

        if (stocked && priced)
            AppendChecklistLine(sb, true, "진열·가격 확인");
        else if (stocked)
            AppendChecklistLine(sb, false, "진열 완료 · 가격 확인 필요", "진행");
        else
            AppendChecklistLine(sb, false, "진열대에 상품 놓기");

        if (loop == null || loop.CurrentPhase == PADayNightPhase.DayPreparation)
            AppendChecklistLine(sb, false, "18시 이후 간판으로 개점");
        else if (loop.CurrentPhase == PADayNightPhase.ShopOpen && !loop.IsShopOpenForCustomers)
            AppendChecklistLine(sb, false, "간판에서 영업 시작", "지금");
        else
            AppendChecklistLine(sb, true, "밤 영업 시작");

        if (loop != null && loop.CurrentPhase == PADayNightPhase.Settlement)
            AppendChecklistLine(sb, false, "간판에서 정산·다음 날", "지금");
        else if (sales.purchases > 0)
            AppendChecklistLine(sb, true, $"오늘 판매 {sales.purchases}건 · 23시 정산");
        else
            AppendChecklistLine(sb, false, "손님 구매 확인 · 23시 정산");
    }

    static bool TryResolveWeekTwoMilestone(int day, out bool complete,
        out string completeLabel, out string actionLabel)
    {
        complete = false;
        completeLabel = string.Empty;
        actionLabel = string.Empty;

        switch (day)
        {
            case 8:
            {
                int storedUnits = CountStoredUnits();
                complete = storedUnits > 0;
                completeLabel = $"보관함 예비 재고 {storedUnits}개 정리";
                actionLabel = "B09 공동 창고에 예비 상품 1개 보관";
                return true;
            }
            case 9:
            {
                int processedSales = CountDailyCategorySales(day, ItemCategory.Processed);
                complete = processedSales > 0;
                completeLabel = $"가공품 판매 {processedSales}건 완료";
                actionLabel = "작업대에서 가공품을 준비해 오늘 밤 1건 판매";
                return true;
            }
            case 10:
            {
                int hiredCount = HiringService.Instance != null ? HiringService.Instance.HiredCount : 0;
                complete = hiredCount > 0;
                completeLabel = $"마을 지원 인력 {hiredCount}명 · {LongPlayProgressionController.BuildHiredRosterSummary(1)}";
                actionLabel = "[P] P.A. Phone 채용 앱에서 생산자 또는 전문가 1명 고용";
                return true;
            }
            case 11:
            {
                int categoryCount = CountDailySoldCategories(day);
                complete = categoryCount >= 2;
                completeLabel = $"서로 다른 상품 카테고리 {categoryCount}종 판매";
                actionLabel = $"오늘 서로 다른 카테고리 상품 판매 {Mathf.Min(categoryCount, 2)}/2종";
                return true;
            }
            case 12:
            {
                int currentTier = TierService.Instance != null ? TierService.Instance.CurrentTier : 0;
                complete = currentTier >= 1;
                completeLabel = $"상점 Tier {currentTier} · 실내 잡화점 성장";
                actionLabel = BuildTierOneActionLabel();
                return true;
            }
            case 13:
            {
                VillageCultureVisualController culture = VillageCultureVisualController.Instance;
                complete = culture != null && culture.HasActiveCategory;
                completeLabel = complete
                    ? $"마을 변화 확인 · {ResolveCategoryLabel(culture.ActiveCategory)}"
                    : string.Empty;
                actionLabel = "어제 판매가 만든 광장 변화를 확인하고 오늘 상품 방향 결정";
                return true;
            }
            case 14:
            {
                int productCount = CountDailySoldProducts(day);
                complete = productCount >= 2;
                completeLabel = $"서로 다른 상품 {productCount}종 판매로 2주차 마감";
                actionLabel = $"오늘 서로 다른 상품 판매 {Mathf.Min(productCount, 2)}/2종";
                return true;
            }
            default:
                return false;
        }
    }

    static bool TryResolveMonthOneMilestone(int day, out bool complete,
        out string completeLabel, out string actionLabel)
    {
        complete = false;
        completeLabel = string.Empty;
        actionLabel = string.Empty;

        switch (day)
        {
            case 15:
            {
                int storedUnits = CountStoredUnits();
                complete = storedUnits >= 3;
                completeLabel = $"보관함 예비 재고 {storedUnits}개 확보";
                actionLabel = $"보관함 예비 재고 {Mathf.Min(storedUnits, 3)}/3개 정리";
                return true;
            }
            case 16:
            {
                int processedSales = CountDailyCategorySales(day, ItemCategory.Processed);
                complete = processedSales > 0;
                completeLabel = $"가공품 판매 {processedSales}건으로 반복 생산 연결";
                actionLabel = "기존 작업대에서 가공품을 준비해 오늘 밤 1건 판매";
                return true;
            }
            case 17:
            {
                int hiredCount = HiringService.Instance != null ? HiringService.Instance.HiredCount : 0;
                complete = hiredCount >= 2;
                completeLabel = $"서로 다른 역할의 지원 인력 {hiredCount}명 운영";
                actionLabel = $"[P] 채용 앱에서 두 번째 생산자 또는 전문가 고용 {Mathf.Min(hiredCount, 2)}/2명";
                return true;
            }
            case 18:
            {
                int categoryCount = CountDailySoldCategories(day);
                complete = categoryCount >= 2;
                completeLabel = $"상품 카테고리 {categoryCount}종 판매";
                actionLabel = $"서로 다른 카테고리 상품 판매 {Mathf.Min(categoryCount, 2)}/2종";
                return true;
            }
            case 19:
            {
                int currentTier = TierService.Instance != null ? TierService.Instance.CurrentTier : 0;
                complete = currentTier >= 1;
                completeLabel = $"상점 Tier {currentTier} 공간으로 성장";
                actionLabel = BuildTierOneActionLabel();
                return true;
            }
            case 20:
            case 28:
            {
                VillageCultureVisualController culture = VillageCultureVisualController.Instance;
                complete = culture != null && culture.HasActiveCategory;
                completeLabel = complete
                    ? $"마을 정체성 확인 · {ResolveCategoryLabel(culture.ActiveCategory)}"
                    : string.Empty;
                actionLabel = "광장의 상품 카테고리 변화를 확인하고 오늘 진열 방향 결정";
                return true;
            }
            case 21:
            {
                int productCount = CountDailySoldProducts(day);
                complete = productCount >= 3;
                completeLabel = $"서로 다른 상품 {productCount}종 판매";
                actionLabel = $"서로 다른 상품 판매 {Mathf.Min(productCount, 3)}/3종";
                return true;
            }
            case 22:
            {
                int storedUnits = CountStoredUnits();
                complete = storedUnits >= 5;
                completeLabel = $"확장 예비 재고 {storedUnits}개 확보";
                actionLabel = $"보관함 예비 재고 {Mathf.Min(storedUnits, 5)}/5개 정리";
                return true;
            }
            case 23:
            {
                bool forgePlaced = ShopCustomizationController.Instance != null
                    && ShopCustomizationController.Instance.HasActiveDefinitionPlacement(BlacksmithForgeDefinitionId);
                int toolSetSales = CountDailyItemSales(day, ToolSetResourcePath);
                int productCount = CountDailySoldProducts(day);
                complete = forgePlaced && toolSetSales > 0 && productCount >= 2;
                completeLabel = $"대장간 배치 · 철제 도구 {toolSetSales}건 · 상품 {productCount}종 판매";
                if (TierService.Instance != null && TierService.Instance.CurrentTier < 1)
                    actionLabel = BuildTierOneActionLabel();
                else if (!forgePlaced)
                    actionLabel = "빈 진열대 두 칸을 회수한 뒤 배치 장부에서 Tier 1 대장간 설계도를 받아 배치";
                else if (toolSetSales <= 0)
                    actionLabel = "목재 가공대에서 Plank 1개, 대장간에서 Ore 4개→IronBar 2개→철제 도구 1개 제작·판매";
                else
                    actionLabel = $"철제 도구와 다른 일상 상품 판매 {Mathf.Min(productCount, 2)}/2종";
                return true;
            }
            case 24:
            {
                bool forgePlaced = ShopCustomizationController.Instance != null
                    && ShopCustomizationController.Instance.HasActiveDefinitionPlacement(BlacksmithForgeDefinitionId);
                int toolSetSales = CountDailyItemSales(day, ToolSetResourcePath);
                int processedSales = CountDailyCategorySales(day, ItemCategory.Processed);
                complete = forgePlaced && toolSetSales > 0 && processedSales > 0;
                completeLabel = $"철제 도구 {toolSetSales}건 · 가공품 {processedSales}건 판매";
                if (TierService.Instance != null && TierService.Instance.CurrentTier < 1)
                    actionLabel = BuildTierOneActionLabel();
                else if (!forgePlaced)
                    actionLabel = "Tier 1 대장간을 배치해 철제 가치사슬을 다시 준비";
                else if (toolSetSales <= 0)
                    actionLabel = "철제 도구 1개를 제작해 오늘 밤 판매";
                else
                    actionLabel = $"IronBar 같은 가공품 동반 판매 {Mathf.Min(processedSales, 1)}/1건";
                return true;
            }
            case 25:
            {
                int hiredCount = HiringService.Instance != null ? HiringService.Instance.HiredCount : 0;
                complete = hiredCount >= 3;
                completeLabel = $"생산·가공 지원 인력 {hiredCount}명 운영";
                actionLabel = $"세 번째 역할 채용 또는 다음 전문 역할 검토 {Mathf.Min(hiredCount, 3)}/3명";
                return true;
            }
            case 26:
            {
                int categoryCount = CountDailySoldCategories(day);
                complete = categoryCount >= 3;
                completeLabel = $"서로 다른 카테고리 {categoryCount}종 판매";
                actionLabel = $"서로 다른 카테고리 상품 판매 {Mathf.Min(categoryCount, 3)}/3종";
                return true;
            }
            case 27:
            {
                int processedSales = CountDailyCategorySales(day, ItemCategory.Processed);
                complete = processedSales >= 2;
                completeLabel = $"가공품 묶음 판매 {processedSales}건 완료";
                actionLabel = $"가공품 판매 {Mathf.Min(processedSales, 2)}/2건";
                return true;
            }
            case 29:
            {
                int storedUnits = CountStoredUnits();
                complete = storedUnits >= 8;
                completeLabel = $"마지막 날 예비 재고 {storedUnits}개 확보";
                actionLabel = $"보관함 예비 재고 {Mathf.Min(storedUnits, 8)}/8개 정리";
                return true;
            }
            case 30:
            {
                int productCount = CountDailySoldProducts(day);
                complete = productCount >= 3;
                completeLabel = $"서로 다른 상품 {productCount}종 판매로 첫 달 마감 준비";
                actionLabel = $"첫 달 마지막 서로 다른 상품 판매 {Mathf.Min(productCount, 3)}/3종";
                return true;
            }
            default:
                return false;
        }
    }

    static bool TryResolveSecondMonthOpeningMilestone(int day, out bool complete,
        out string completeLabel, out string actionLabel)
    {
        complete = false;
        completeLabel = string.Empty;
        actionLabel = string.Empty;

        switch (day)
        {
            case 31:
            {
                int storedUnits = CountStoredUnits();
                complete = storedUnits >= 10;
                completeLabel = $"두 번째 달 예비 재고 {storedUnits}개 확보";
                actionLabel = $"보관함 예비 재고 {Mathf.Min(storedUnits, 10)}/10개 정리";
                return true;
            }
            case 32:
            {
                int processedSales = CountDailyCategorySales(day, ItemCategory.Processed);
                complete = processedSales >= 2;
                completeLabel = $"가공 주력 상품 {processedSales}건 판매";
                actionLabel = $"가공품 판매 {Mathf.Min(processedSales, 2)}/2건";
                return true;
            }
            case 33:
            {
                int hiredCount = HiringService.Instance != null ? HiringService.Instance.HiredCount : 0;
                complete = hiredCount >= 3;
                completeLabel = $"생산·가공 지원 인력 {hiredCount}명 · {LongPlayProgressionController.BuildHiredRosterSummary(3)}";
                actionLabel = $"생산·가공 역할을 맡을 지원 인력 {Mathf.Min(hiredCount, 3)}/3명 운영";
                return true;
            }
            case 34:
            {
                int productCount = CountDailySoldProducts(day);
                complete = productCount >= 3;
                completeLabel = $"서로 다른 상품 {productCount}종 판매";
                actionLabel = $"서로 다른 상품 판매 {Mathf.Min(productCount, 3)}/3종";
                return true;
            }
            case 35:
            {
                long revenue = EconomyService.Instance != null
                    ? EconomyService.Instance.CumulativeRevenue
                    : 0L;
                long target = LongPlayProgressionController.GetCampaignRevenueTarget(day);
                complete = revenue >= target;
                completeLabel = $"누적 매출 {revenue:N0}G로 성장 점검선 달성";
                actionLabel = $"두 번째 달 성장 점검선 {revenue:N0}/{target:N0}G";
                return true;
            }
            case 36:
            {
                bool forgePlaced = ShopCustomizationController.Instance != null
                    && ShopCustomizationController.Instance.HasActiveDefinitionPlacement(BlacksmithForgeDefinitionId);
                int toolSetSales = CountDailyItemSales(day, ToolSetResourcePath);
                complete = forgePlaced && toolSetSales > 0;
                completeLabel = $"Tier 1 대장간 유지 · 철제 도구 {toolSetSales}건 판매";
                if (TierService.Instance != null && TierService.Instance.CurrentTier < 1)
                    actionLabel = BuildTierOneActionLabel();
                else if (!forgePlaced)
                    actionLabel = "배치 장부에서 Tier 1 대장간을 다시 배치";
                else
                    actionLabel = "Plank 1개와 Ore 4개로 철제 도구 1개 제작·판매";
                return true;
            }
            case 37:
            {
                int categoryCount = CountDailySoldCategories(day);
                complete = categoryCount >= 3;
                completeLabel = $"서로 다른 상품 카테고리 {categoryCount}종 판매";
                actionLabel = $"서로 다른 카테고리 상품 판매 {Mathf.Min(categoryCount, 3)}/3종";
                return true;
            }
            case 38:
            {
                VillageCultureVisualController culture = VillageCultureVisualController.Instance;
                complete = culture != null && culture.HasActiveCategory;
                completeLabel = complete
                    ? $"마을 변화 확인 · {ResolveCategoryLabel(culture.ActiveCategory)}"
                    : string.Empty;
                actionLabel = "광장의 활성 마을 변화를 확인하고 오늘 상품 방향 결정";
                return true;
            }
            case 39:
            {
                int storedUnits = CountStoredUnits();
                complete = storedUnits >= 12;
                completeLabel = $"확장 예비 재고 {storedUnits}개 확보";
                actionLabel = $"보관함 예비 재고 {Mathf.Min(storedUnits, 12)}/12개 정리";
                return true;
            }
            case 40:
            {
                int toolSetSales = CountDailyItemSales(day, ToolSetResourcePath);
                int processedSales = CountDailyCategorySales(day, ItemCategory.Processed);
                complete = toolSetSales > 0 && processedSales >= 2;
                completeLabel = $"철제 도구 {toolSetSales}건 · 가공품 {processedSales}건 판매";
                if (toolSetSales <= 0)
                    actionLabel = "철제 도구 1개를 제작해 오늘 밤 판매";
                else
                    actionLabel = $"가공품 동반 판매 {Mathf.Min(processedSales, 2)}/2건";
                return true;
            }
            case 41:
            {
                int productCount = CountDailySoldProducts(day);
                complete = productCount >= 4;
                completeLabel = $"서로 다른 상품 {productCount}종 판매";
                actionLabel = $"서로 다른 상품 판매 {Mathf.Min(productCount, 4)}/4종";
                return true;
            }
            case 42:
            {
                int preparedTypes = CountPreparedSellableTypes();
                complete = preparedTypes >= 4;
                completeLabel = $"밤 영업 상품 {preparedTypes}종 준비";
                actionLabel = $"인벤토리·핫바에 판매 상품 {Mathf.Min(preparedTypes, 4)}/4종 준비";
                return true;
            }
            case 43:
            {
                int processedSales = CountDailyCategorySales(day, ItemCategory.Processed);
                complete = processedSales >= 3;
                completeLabel = $"가공품 묶음 판매 {processedSales}건 완료";
                actionLabel = $"가공품 판매 {Mathf.Min(processedSales, 3)}/3건";
                return true;
            }
            case 44:
            {
                VillageCultureVisualController culture = VillageCultureVisualController.Instance;
                int productCount = CountDailySoldProducts(day);
                complete = culture != null && culture.HasActiveCategory && productCount >= 3;
                completeLabel = complete
                    ? $"{ResolveCategoryLabel(culture.ActiveCategory)} 방향 확인 · 상품 {productCount}종 판매"
                    : string.Empty;
                if (culture == null || !culture.HasActiveCategory)
                    actionLabel = "광장의 활성 마을 변화를 확인";
                else
                    actionLabel = $"{ResolveCategoryLabel(culture.ActiveCategory)} 방향을 고려한 상품 판매 {Mathf.Min(productCount, 3)}/3종";
                return true;
            }
            case 45:
            {
                long revenue = EconomyService.Instance != null
                    ? EconomyService.Instance.CumulativeRevenue
                    : 0L;
                long target = LongPlayProgressionController.GetCampaignRevenueTarget(day);
                int productCount = CountDailySoldProducts(day);
                complete = revenue >= target && productCount >= 4;
                completeLabel = $"누적 매출 {revenue:N0}G · 상품 {productCount}종으로 두 번째 달 진입 구간 마감";
                if (revenue < target)
                    actionLabel = $"두 번째 달 매출 점검선 {revenue:N0}/{target:N0}G";
                else
                    actionLabel = $"마감 상품 판매 {Mathf.Min(productCount, 4)}/4종";
                return true;
            }
            default:
                return false;
        }
    }

    static bool TryResolveTierTwoCampaignMilestone(int day, out bool complete,
        out string completeLabel, out string actionLabel)
    {
        complete = false;
        completeLabel = string.Empty;
        actionLabel = string.Empty;

        if (day < LongPlayProgressionController.TierTwoCampaignStartDay
            || day > LongPlayProgressionController.TierTwoCampaignFinalDay)
            return false;

        if (day == LongPlayProgressionController.TierTwoCampaignFinalDay)
        {
            int currentTier = TierService.Instance != null ? TierService.Instance.CurrentTier : 0;
            complete = currentTier >= 2;
            completeLabel = complete
                ? $"누적 매출 100,000G 성장 · 상점 Tier {currentTier} 달성"
                : string.Empty;
            actionLabel = BuildTierTwoActionLabel();
            return true;
        }

        switch (LongPlayProgressionController.GetTierTwoCampaignPhase(day))
        {
            case 0:
            {
                int storedUnits = CountStoredUnits();
                int target = LongPlayProgressionController.GetTierTwoReserveTarget(day);
                complete = storedUnits >= target;
                completeLabel = $"지역 경제 예비 재고 {storedUnits}개 확보";
                actionLabel = $"보관함 예비 재고 {Mathf.Min(storedUnits, target)}/{target}개 정리";
                return true;
            }
            case 1:
            {
                int processedSales = CountDailyCategorySales(day, ItemCategory.Processed);
                int target = LongPlayProgressionController.GetTierTwoProcessedSalesTarget(day);
                complete = processedSales >= target;
                completeLabel = $"가공품 판매 {processedSales}건으로 생산 규모 확장";
                actionLabel = $"가공품 판매 {Mathf.Min(processedSales, target)}/{target}건";
                return true;
            }
            case 2:
            {
                int hiredCount = HiringService.Instance != null ? HiringService.Instance.HiredCount : 0;
                int preparedTypes = CountPreparedSellableTypes();
                complete = hiredCount >= 3 && preparedTypes >= 4;
                completeLabel = $"지원 인력 {hiredCount}명 · 판매 상품 {preparedTypes}종 준비";
                if (hiredCount < 3)
                    actionLabel = $"생산·가공 지원 인력 {Mathf.Min(hiredCount, 3)}/3명 운영";
                else
                    actionLabel = $"인벤토리·핫바에 판매 상품 {Mathf.Min(preparedTypes, 4)}/4종 준비";
                return true;
            }
            case 3:
            {
                int categoryCount = CountDailySoldCategories(day);
                complete = categoryCount >= 3;
                completeLabel = $"서로 다른 상품 카테고리 {categoryCount}종 판매";
                actionLabel = $"서로 다른 카테고리 상품 판매 {Mathf.Min(categoryCount, 3)}/3종";
                return true;
            }
            case 4:
            {
                bool forgePlaced = ShopCustomizationController.Instance != null
                    && ShopCustomizationController.Instance.HasActiveDefinitionPlacement(BlacksmithForgeDefinitionId);
                int toolSetSales = CountDailyItemSales(day, ToolSetResourcePath);
                int processedSales = CountDailyCategorySales(day, ItemCategory.Processed);
                complete = forgePlaced && toolSetSales > 0 && processedSales > 0;
                completeLabel = $"철제 도구 {toolSetSales}건 · 가공품 {processedSales}건 판매";
                if (TierService.Instance != null && TierService.Instance.CurrentTier < 1)
                    actionLabel = BuildTierOneActionLabel();
                else if (!forgePlaced)
                    actionLabel = "배치 장부에서 Tier 1 대장간을 배치";
                else if (toolSetSales <= 0)
                    actionLabel = "Plank 1개와 Ore 4개로 철제 도구 1개 제작·판매";
                else
                    actionLabel = "철제 도구와 함께 가공품 1건 판매";
                return true;
            }
            case 5:
            {
                VillageCultureVisualController culture = VillageCultureVisualController.Instance;
                int productCount = CountDailySoldProducts(day);
                complete = culture != null && culture.HasActiveCategory && productCount >= 4;
                completeLabel = complete
                    ? $"{ResolveCategoryLabel(culture.ActiveCategory)} 방향 확인 · 상품 {productCount}종 판매"
                    : string.Empty;
                if (culture == null || !culture.HasActiveCategory)
                    actionLabel = "광장의 활성 마을 변화를 확인";
                else
                    actionLabel = $"{ResolveCategoryLabel(culture.ActiveCategory)} 방향을 고려한 상품 판매 {Mathf.Min(productCount, 4)}/4종";
                return true;
            }
            default:
            {
                long revenue = EconomyService.Instance != null
                    ? EconomyService.Instance.CumulativeRevenue
                    : 0L;
                long target = LongPlayProgressionController.GetCampaignRevenueTarget(day);
                complete = revenue >= target;
                completeLabel = $"누적 매출 {revenue:N0}G로 Tier 2 주간 점검선 달성";
                actionLabel = $"Tier 2 주간 매출 점검선 {revenue:N0}/{target:N0}G";
                return true;
            }
        }
    }

    static bool TryResolveTierTwoKitchenMilestone(int day, out bool complete,
        out string completeLabel, out string actionLabel)
    {
        complete = false;
        completeLabel = string.Empty;
        actionLabel = string.Empty;

        if (day < LongPlayProgressionController.TierTwoKitchenCampaignStartDay
            || day > LongPlayProgressionController.TierTwoKitchenCampaignFinalDay)
            return false;

        bool kitchenPlaced = ShopCustomizationController.Instance != null
            && ShopCustomizationController.Instance.HasActiveDefinitionPlacement(KitchenStationDefinitionId);

        switch (day)
        {
            case 77:
            {
                int currentTier = TierService.Instance != null ? TierService.Instance.CurrentTier : 0;
                complete = currentTier >= 2 && kitchenPlaced;
                completeLabel = "Tier 2 주방 스테이션 배치 완료";
                if (currentTier < 2)
                    actionLabel = BuildTierTwoActionLabel();
                else
                    actionLabel = "배치 장부를 열어 지급된 Tier 2 주방 설계도를 선택하고 작업 공간에 배치";
                return true;
            }
            case 78:
            {
                int sales = CountDailyItemSales(day, BreadLoafResourcePath);
                complete = kitchenPlaced && sales > 0;
                completeLabel = $"BreadLoaf {sales}개 판매로 밀 조리 라인 가동";
                actionLabel = BuildKitchenProductActionLabel(kitchenPlaced,
                    "Wheat 3개로 BreadLoaf 1개를 조리해 진열·판매");
                return true;
            }
            case 79:
            {
                int sales = CountDailyItemSales(day, BakedPotatoResourcePath);
                complete = kitchenPlaced && sales > 0;
                completeLabel = $"구운 감자 {sales}개 판매로 채소 조리 라인 가동";
                actionLabel = BuildKitchenProductActionLabel(kitchenPlaced,
                    "Carrot 2개로 구운 감자 2개를 조리해 1개 이상 판매");
                return true;
            }
            case 80:
            {
                int sales = CountDailyItemSales(day, GrilledFishResourcePath);
                complete = kitchenPlaced && sales > 0;
                completeLabel = $"생선구이 {sales}개 판매로 어획 조리 라인 가동";
                actionLabel = BuildKitchenProductActionLabel(kitchenPlaced,
                    "Fish 1개로 생선구이 1개를 조리해 진열·판매");
                return true;
            }
            case 81:
            {
                int productKinds = CountDailyKitchenProductKinds(day);
                complete = kitchenPlaced && productKinds >= 2;
                completeLabel = $"주방 상품 {productKinds}종을 한 영업일에 판매";
                actionLabel = BuildKitchenProductActionLabel(kitchenPlaced,
                    $"서로 다른 주방 상품 판매 {Mathf.Min(productKinds, 2)}/2종");
                return true;
            }
            case 82:
            {
                VillageCultureVisualController culture = VillageCultureVisualController.Instance;
                complete = kitchenPlaced && culture != null && culture.HasActiveCategory
                    && culture.ActiveCategory == ItemCategory.Processed;
                completeLabel = "가공품 중심 마을 변화 활성화 확인";
                if (!kitchenPlaced)
                    actionLabel = BuildKitchenProductActionLabel(false, string.Empty);
                else
                    actionLabel = "전날 가공품 판매 비중을 높이고 광장의 가공품 중심 마을 변화를 확인";
                return true;
            }
            case 83:
            {
                int preparedKinds = CountPreparedKitchenProductKinds();
                complete = kitchenPlaced && preparedKinds >= 3;
                completeLabel = "BreadLoaf·구운 감자·생선구이 3종 준비 완료";
                actionLabel = BuildKitchenProductActionLabel(kitchenPlaced,
                    $"인벤토리·핫바·진열대에 주방 상품 준비 {Mathf.Min(preparedKinds, 3)}/3종");
                return true;
            }
            case 84:
            {
                int productKinds = CountDailyKitchenProductKinds(day);
                complete = kitchenPlaced && productKinds >= 3;
                completeLabel = "주방 전체 메뉴 3종 판매 완료";
                actionLabel = BuildKitchenProductActionLabel(kitchenPlaced,
                    $"BreadLoaf·구운 감자·생선구이 판매 {Mathf.Min(productKinds, 3)}/3종");
                return true;
            }
            case 85:
            {
                bool chefHired = HasHiredSpecialty(NpcSpecialty.Chef);
                complete = kitchenPlaced && chefHired;
                completeLabel = "배치된 주방과 고용된 요리사 연결 완료";
                if (!kitchenPlaced)
                    actionLabel = BuildKitchenProductActionLabel(false, string.Empty);
                else
                    actionLabel = "P.A. Phone 채용 탭에서 요리사 전문 주민을 고용";
                return true;
            }
            case 86:
            {
                int kitchenSales = CountDailyKitchenProductSales(day);
                int categoryCount = CountDailySoldCategories(day);
                complete = kitchenPlaced && kitchenSales > 0 && categoryCount >= 3;
                completeLabel = $"주방 상품 {kitchenSales}개와 상품 분류 {categoryCount}종 판매";
                if (!kitchenPlaced)
                    actionLabel = BuildKitchenProductActionLabel(false, string.Empty);
                else if (kitchenSales <= 0)
                    actionLabel = "주방 상품 1개 이상 판매";
                else
                    actionLabel = $"서로 다른 상품 분류 판매 {Mathf.Min(categoryCount, 3)}/3종";
                return true;
            }
            case 87:
            {
                int kitchenSales = CountDailyKitchenProductSales(day);
                complete = kitchenPlaced && kitchenSales >= 4;
                completeLabel = $"주방 상품 {kitchenSales}개 배치 판매 완료";
                actionLabel = BuildKitchenProductActionLabel(kitchenPlaced,
                    $"주방 상품 판매 {Mathf.Min(kitchenSales, 4)}/4개");
                return true;
            }
            case 88:
            {
                VillageCultureVisualController culture = VillageCultureVisualController.Instance;
                int kitchenSales = CountDailyKitchenProductSales(day);
                bool processedCulture = culture != null && culture.HasActiveCategory
                    && culture.ActiveCategory == ItemCategory.Processed;
                complete = kitchenPlaced && processedCulture && kitchenSales > 0;
                completeLabel = $"가공품 마을 방향과 주방 상품 {kitchenSales}개 판매 연결";
                if (!kitchenPlaced)
                    actionLabel = BuildKitchenProductActionLabel(false, string.Empty);
                else if (!processedCulture)
                    actionLabel = "가공품 판매 우세를 유지해 광장의 가공품 중심 마을 변화를 활성화";
                else
                    actionLabel = "활성화된 가공품 마을 방향에 맞춰 주방 상품 1개 이상 판매";
                return true;
            }
            case 89:
            {
                long revenue = EconomyService.Instance != null
                    ? EconomyService.Instance.CumulativeRevenue
                    : 0L;
                long target = LongPlayProgressionController.GetCampaignRevenueTarget(day);
                complete = revenue >= target;
                completeLabel = $"누적 매출 {revenue:N0}G로 주방 성장 점검 완료";
                actionLabel = $"주방 성장 매출 점검 {revenue:N0}/{target:N0}G";
                return true;
            }
            case 90:
            {
                VillageCultureVisualController culture = VillageCultureVisualController.Instance;
                int productKinds = CountDailyKitchenProductKinds(day);
                bool processedCulture = culture != null && culture.HasActiveCategory
                    && culture.ActiveCategory == ItemCategory.Processed;
                complete = kitchenPlaced && productKinds >= 3 && processedCulture;
                completeLabel = "재료→주방→판매→가공품 마을 변화 가치사슬 완주";
                if (!kitchenPlaced)
                    actionLabel = BuildKitchenProductActionLabel(false, string.Empty);
                else if (productKinds < 3)
                    actionLabel = $"주방 전체 메뉴 판매 {Mathf.Min(productKinds, 3)}/3종";
                else
                    actionLabel = "가공품 판매 비중을 유지해 가공품 중심 마을 변화를 활성화";
                return true;
            }
            default:
                return false;
        }
    }

    static string BuildKitchenProductActionLabel(bool kitchenPlaced, string readyAction)
    {
        return kitchenPlaced
            ? readyAction
            : "배치 장부를 열어 Tier 2 주방 설계도를 받고 주방 스테이션을 배치";
    }

    static bool TryResolveTierThreeCampaignMilestone(int day, out bool complete,
        out string completeLabel, out string actionLabel)
    {
        complete = false;
        completeLabel = string.Empty;
        actionLabel = string.Empty;

        if (day < LongPlayProgressionController.TierThreeCampaignStartDay
            || day > LongPlayProgressionController.TierThreeCampaignFinalDay)
            return false;

        TierService tierService = TierService.Instance;
        int currentTier = tierService != null ? tierService.CurrentTier : 0;
        int reputation = tierService != null ? tierService.Reputation : 0;
        bool sewingTablePlaced = ShopCustomizationController.Instance != null
            && ShopCustomizationController.Instance.HasActiveDefinitionPlacement(SewingTableDefinitionId);

        switch (day)
        {
            case 91:
            case 92:
            {
                int target = day - 90;
                complete = reputation >= target || currentTier >= 3;
                completeLabel = $"전문 주민 요청으로 마을 평판 {Mathf.Max(reputation, target)}/3 확보";
                actionLabel = BuildTierThreeReputationActionLabel(target);
                return true;
            }
            case 93:
                complete = currentTier >= 3;
                completeLabel = $"마을 평판 {reputation}/3 · Tier {currentTier} 지역장 상점 진입";
                actionLabel = BuildTierThreeReputationActionLabel(3);
                return true;
            case 94:
                complete = currentTier >= 3 && sewingTablePlaced;
                completeLabel = "Tier 3 B08 재봉 작업대 배치 완료";
                actionLabel = BuildAtelierActionLabel(currentTier, sewingTablePlaced,
                    "B08 재봉 작업대 배치 완료");
                return true;
            case 95:
            {
                int clothesSales = CountDailyItemSales(day, ClothesResourcePath);
                complete = sewingTablePlaced && clothesSales > 0;
                completeLabel = $"의류 {clothesSales}개 판매로 재봉 상품 라인 가동";
                actionLabel = BuildAtelierActionLabel(currentTier, sewingTablePlaced,
                    "Wheat 2개와 Plank 1개로 의류를 제작해 1개 이상 판매");
                return true;
            }
            case 96:
            {
                int clothesSales = CountDailyItemSales(day, ClothesResourcePath);
                int furnitureSales = CountDailyItemSales(day, FurnitureResourcePath);
                complete = sewingTablePlaced && clothesSales > 0 && furnitureSales > 0;
                completeLabel = $"의류 {clothesSales}개 · 가구 {furnitureSales}개 Luxury 상품 판매";
                actionLabel = BuildAtelierActionLabel(currentTier, sewingTablePlaced,
                    $"의류 판매 {Mathf.Min(clothesSales, 1)}/1 · 가구 판매 {Mathf.Min(furnitureSales, 1)}/1");
                return true;
            }
            case 97:
            {
                VillageCultureVisualController culture = VillageCultureVisualController.Instance;
                bool luxuryCulture = culture != null && culture.HasActiveCategory
                    && culture.ActiveCategory == ItemCategory.Luxury;
                complete = sewingTablePlaced && luxuryCulture;
                completeLabel = "Luxury 중심 마을 변화 활성화 확인";
                actionLabel = BuildAtelierActionLabel(currentTier, sewingTablePlaced,
                    "전날 Luxury 판매 비중을 높이고 광장의 공예·전시 중심 마을 변화를 확인");
                return true;
            }
            case 98:
            {
                bool tailorHired = HasHiredSpecialty(NpcSpecialty.Tailor);
                complete = sewingTablePlaced && tailorHired;
                completeLabel = "배치된 B08과 고용된 재단사 연결 완료";
                actionLabel = BuildAtelierActionLabel(currentTier, sewingTablePlaced,
                    "P.A. Phone 채용 탭에서 재단사 전문 주민을 고용");
                return true;
            }
            case 99:
            {
                int clothesSales = CountDailyItemSales(day, ClothesResourcePath);
                int categoryCount = CountDailySoldCategories(day);
                complete = sewingTablePlaced && clothesSales > 0 && categoryCount >= 3;
                completeLabel = $"의류 {clothesSales}개와 상품 분류 {categoryCount}종 판매";
                actionLabel = BuildAtelierActionLabel(currentTier, sewingTablePlaced,
                    clothesSales <= 0
                        ? "의류 1개 이상 판매"
                        : $"서로 다른 상품 분류 판매 {Mathf.Min(categoryCount, 3)}/3종");
                return true;
            }
            case 100:
            {
                int preparedKinds = CountPreparedAtelierProductKinds();
                complete = sewingTablePlaced && preparedKinds >= 2;
                completeLabel = "의류·가구 2종 공방 상품 준비 완료";
                actionLabel = BuildAtelierActionLabel(currentTier, sewingTablePlaced,
                    $"인벤토리·핫바·진열대에 공방 상품 준비 {Mathf.Min(preparedKinds, 2)}/2종");
                return true;
            }
            case 101:
            {
                int clothesSales = CountDailyItemSales(day, ClothesResourcePath);
                int furnitureSales = CountDailyItemSales(day, FurnitureResourcePath);
                complete = sewingTablePlaced && clothesSales > 0 && furnitureSales > 0;
                completeLabel = "의류·가구 2종 공방 상품 판매 완료";
                actionLabel = BuildAtelierActionLabel(currentTier, sewingTablePlaced,
                    $"의류 판매 {Mathf.Min(clothesSales, 1)}/1 · 가구 판매 {Mathf.Min(furnitureSales, 1)}/1");
                return true;
            }
            case 102:
            {
                int hiredCount = HiringService.Instance != null ? HiringService.Instance.HiredCount : 0;
                complete = sewingTablePlaced && hiredCount >= 4;
                completeLabel = $"B08 공방과 마을 지원 인력 {hiredCount}명 운영";
                actionLabel = BuildAtelierActionLabel(currentTier, sewingTablePlaced,
                    $"P.A. Phone 채용 앱에서 지원 인력 고용 {Mathf.Min(hiredCount, 4)}/4명");
                return true;
            }
            case 103:
            {
                VillageCultureVisualController culture = VillageCultureVisualController.Instance;
                bool luxuryCulture = culture != null && culture.HasActiveCategory
                    && culture.ActiveCategory == ItemCategory.Luxury;
                int productCount = CountDailySoldProducts(day);
                complete = sewingTablePlaced && luxuryCulture && productCount >= 4;
                completeLabel = $"Luxury 마을 방향과 서로 다른 상품 {productCount}종 판매 연결";
                actionLabel = BuildAtelierActionLabel(currentTier, sewingTablePlaced,
                    !luxuryCulture
                        ? "Luxury 판매 우세를 유지해 공예·전시 중심 마을 변화를 활성화"
                        : $"서로 다른 상품 판매 {Mathf.Min(productCount, 4)}/4종");
                return true;
            }
            case 104:
            {
                long revenue = EconomyService.Instance != null
                    ? EconomyService.Instance.CumulativeRevenue
                    : 0L;
                long target = LongPlayProgressionController.GetCampaignRevenueTarget(day);
                complete = revenue >= target;
                completeLabel = $"누적 매출 {revenue:N0}G로 Tier 3 성장 점검선 달성";
                actionLabel = $"Tier 3 공방 매출 점검선 {revenue:N0}/{target:N0}G";
                return true;
            }
            case 105:
            {
                VillageCultureVisualController culture = VillageCultureVisualController.Instance;
                bool luxuryCulture = culture != null && culture.HasActiveCategory
                    && culture.ActiveCategory == ItemCategory.Luxury;
                int clothesSales = CountDailyItemSales(day, ClothesResourcePath);
                int furnitureSales = CountDailyItemSales(day, FurnitureResourcePath);
                complete = currentTier >= 3 && sewingTablePlaced && clothesSales > 0
                    && furnitureSales > 0 && luxuryCulture;
                completeLabel = "주민 도움→평판→Tier 3→공방→판매→Luxury 마을 변화 가치사슬 완주";
                if (currentTier < 3)
                    actionLabel = BuildTierThreeReputationActionLabel(3);
                else if (!sewingTablePlaced)
                    actionLabel = BuildAtelierActionLabel(currentTier, false, string.Empty);
                else if (clothesSales <= 0 || furnitureSales <= 0)
                    actionLabel = $"의류 판매 {Mathf.Min(clothesSales, 1)}/1 · 가구 판매 {Mathf.Min(furnitureSales, 1)}/1";
                else
                    actionLabel = "Luxury 판매 비중을 유지해 공예·전시 중심 마을 변화를 활성화";
                return true;
            }
            default:
                return false;
        }
    }

    static string BuildTierThreeReputationActionLabel(int target)
    {
        if (TierService.Instance == null || TierService.Instance.CurrentTier < 2)
            return BuildTierTwoActionLabel();

        int reputation = TierService.Instance.Reputation;
        return $"전문 주민에게 필요한 재료를 건네 낮 요청 완료 · 마을 평판 "
            + $"{Mathf.Min(reputation, target)}/{target} (하루 1회)";
    }

    static string BuildAtelierActionLabel(int currentTier, bool sewingTablePlaced, string readyAction)
    {
        if (currentTier < 3)
            return BuildTierThreeReputationActionLabel(3);

        return sewingTablePlaced
            ? readyAction
            : "배치 장부를 열어 지급된 Tier 3 B08 재봉 작업대를 작업 공간에 배치";
    }

    static bool HasHiredSpecialty(NpcSpecialty specialty)
    {
        if (HiringService.Instance == null) return false;

        foreach (NpcCandidateData candidate in HiringService.Instance.GetHiredCandidates())
            if (candidate != null && candidate.specialty == specialty)
                return true;

        return false;
    }

    static int CountDailyKitchenProductKinds(int day)
    {
        int count = 0;
        if (CountDailyItemSales(day, BreadLoafResourcePath) > 0) count++;
        if (CountDailyItemSales(day, BakedPotatoResourcePath) > 0) count++;
        if (CountDailyItemSales(day, GrilledFishResourcePath) > 0) count++;
        return count;
    }

    static int CountDailyKitchenProductSales(int day)
    {
        return CountDailyItemSales(day, BreadLoafResourcePath)
            + CountDailyItemSales(day, BakedPotatoResourcePath)
            + CountDailyItemSales(day, GrilledFishResourcePath);
    }

    static int CountPreparedKitchenProductKinds()
    {
        var expectedItems = new HashSet<Item>();
        AddResourceItem(expectedItems, BreadLoafResourcePath);
        AddResourceItem(expectedItems, BakedPotatoResourcePath);
        AddResourceItem(expectedItems, GrilledFishResourcePath);

        var preparedItems = new HashSet<Item>();
        Inventory inventory = Inventory.instance;
        if (inventory != null)
        {
            AddPreparedMatches(preparedItems, expectedItems, inventory.slots);
            if (inventory.hotbar != null)
                AddPreparedMatches(preparedItems, expectedItems, inventory.hotbar.slots);
        }

        foreach (ShopSlot slot in UnityEngine.Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
            if (slot != null && !slot.IsEmpty && slot.currentItem?.data != null
                && expectedItems.Contains(slot.currentItem.data))
                preparedItems.Add(slot.currentItem.data);

        return preparedItems.Count;
    }

    static int CountPreparedAtelierProductKinds()
    {
        var expectedItems = new HashSet<Item>();
        AddResourceItem(expectedItems, FurnitureResourcePath);
        AddResourceItem(expectedItems, ClothesResourcePath);

        var preparedItems = new HashSet<Item>();
        Inventory inventory = Inventory.instance;
        if (inventory != null)
        {
            AddPreparedMatches(preparedItems, expectedItems, inventory.slots);
            if (inventory.hotbar != null)
                AddPreparedMatches(preparedItems, expectedItems, inventory.hotbar.slots);
        }

        foreach (ShopSlot slot in UnityEngine.Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
            if (slot != null && !slot.IsEmpty && slot.currentItem?.data != null
                && expectedItems.Contains(slot.currentItem.data))
                preparedItems.Add(slot.currentItem.data);

        return preparedItems.Count;
    }

    static void AddResourceItem(HashSet<Item> items, string resourcePath)
    {
        Item item = Resources.Load<Item>(resourcePath);
        if (item != null) items.Add(item);
    }

    static void AddPreparedMatches(HashSet<Item> preparedItems, HashSet<Item> expectedItems,
        List<InventorySlot> slots)
    {
        if (slots == null) return;

        foreach (InventorySlot slot in slots)
            if (slot != null && !slot.IsEmpty && slot.item != null && expectedItems.Contains(slot.item))
                preparedItems.Add(slot.item);
    }

    static int CountStoredUnits()
    {
        int total = 0;
        foreach (StorageBox box in UnityEngine.Object.FindObjectsByType<StorageBox>(FindObjectsSortMode.None))
        {
            if (box == null || box.items == null) continue;
            foreach (ItemInstance instance in box.items)
                if (instance != null && instance.data != null && instance.count > 0)
                    total += instance.count;
        }

        return total;
    }

    static int CountDailyCategorySales(int day, ItemCategory category)
    {
        int count = 0;
        foreach (SaleRecord record in GetRecentSales())
        {
            if (record == null || record.gameDay != day) continue;
            if (Enum.TryParse(record.category, true, out ItemCategory soldCategory)
                && soldCategory == category)
                count++;
        }

        return count;
    }

    static int CountDailyItemSales(int day, string resourcePath)
    {
        Item expectedItem = Resources.Load<Item>(resourcePath);
        if (expectedItem == null || string.IsNullOrWhiteSpace(expectedItem.itemName))
            return 0;

        string expectedName = expectedItem.itemName.Trim();
        int count = 0;
        foreach (SaleRecord record in GetRecentSales())
        {
            if (record == null || record.gameDay != day || string.IsNullOrWhiteSpace(record.itemName))
                continue;
            if (string.Equals(record.itemName.Trim(), expectedName, StringComparison.Ordinal))
                count++;
        }

        return count;
    }

    static int CountDailySoldCategories(int day)
    {
        var categories = new HashSet<ItemCategory>();
        foreach (SaleRecord record in GetRecentSales())
        {
            if (record == null || record.gameDay != day) continue;
            if (Enum.TryParse(record.category, true, out ItemCategory category)
                && category != ItemCategory.Tool)
                categories.Add(category);
        }

        return categories.Count;
    }

    static int CountDailySoldProducts(int day)
    {
        var products = new HashSet<string>(StringComparer.Ordinal);
        foreach (SaleRecord record in GetRecentSales())
        {
            if (record == null || record.gameDay != day || string.IsNullOrWhiteSpace(record.itemName))
                continue;
            products.Add(record.itemName.Trim());
        }

        return products.Count;
    }

    static List<SaleRecord> GetRecentSales()
    {
        if (SalesLogManager.Instance == null)
            return new List<SaleRecord>();

        return SalesLogManager.Instance.GetRecent(
            Mathf.Max(1, SalesLogManager.Instance.maxRecords));
    }

    static string BuildTierOneActionLabel()
    {
        if (TierService.Instance == null)
            return "감사 앱에서 Tier 1 실내 잡화점 성장 조건 확인";

        TierDefinition next = TierService.Instance.GetDefinition(1);
        long revenue = EconomyService.Instance != null
            ? EconomyService.Instance.CumulativeRevenue
            : 0L;
        if (next != null && next.requiredCumulativeRevenue > 0)
        {
            long remaining = Math.Max(0L, next.requiredCumulativeRevenue - revenue);
            return $"Tier 1 실내 잡화점까지 누적 매출 {remaining:N0}G 남음";
        }

        return "감사 앱에서 Tier 1 실내 잡화점 성장 조건 달성";
    }

    static string BuildTierTwoActionLabel()
    {
        if (TierService.Instance == null)
            return "감사 앱에서 Tier 2 성장 조건 확인";

        int currentTier = TierService.Instance.CurrentTier;
        if (currentTier >= 2)
            return $"상점 Tier {currentTier} 달성 · 배치 장부에서 다음 설계도 확인";

        TierDefinition tierTwo = TierService.Instance.GetDefinition(2);
        long revenue = EconomyService.Instance != null
            ? EconomyService.Instance.CumulativeRevenue
            : 0L;
        if (tierTwo != null && tierTwo.requiredCumulativeRevenue > 0)
        {
            long remaining = Math.Max(0L, tierTwo.requiredCumulativeRevenue - revenue);
            return $"Tier 2까지 누적 매출 {remaining:N0}G 남음 · 현재 {revenue:N0}/{tierTwo.requiredCumulativeRevenue:N0}G";
        }

        return "감사 앱에서 Tier 2 성장 조건 달성";
    }

    static string ResolveCategoryLabel(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Raw => "원재료 수거처",
            ItemCategory.Processed => "가공 준비대",
            ItemCategory.Utility => "공구 수리대",
            ItemCategory.Luxury => "공예 전시대",
            _ => category.ToString()
        };
    }

    static void AppendChecklistLine(System.Text.StringBuilder sb, bool complete, string label,
        string pendingLabel = null)
    {
        string state = complete ? "완료" : string.IsNullOrWhiteSpace(pendingLabel) ? " " : pendingLabel;
        string color = complete ? "#8FCF9A" : pendingLabel == "지금" ? "#FFE9B8" : "#D8D3C8";
        sb.AppendLine($"<color={color}>[{state}] {label}</color>");
    }

    static List<string> GetCompletedDayActivityLabels(DayNightShopLoopController loop)
    {
        var labels = new List<string>();
        if (loop != null)
        {
            if (loop.IsDailyActivityCompleted("shore-forage")) labels.Add("낚시");
            if (loop.IsDailyActivityCompleted("quarry-mining")) labels.Add("채광");
            if (AnyFarmPlotStarted()) labels.Add("농사");
            else if (loop.IsDailyActivityCompleted("farm-seed-pouch")) labels.Add("농사 준비");
            if (loop.IsDailyActivityCompleted("forest-forage") || loop.IsDailyActivityCompleted("meadow-forage"))
                labels.Add("채집");
        }

        if (AnyResidentRequestCompletedToday()) labels.Add("주민 도움");
        return labels;
    }

    static bool AnyFarmPlotStarted()
    {
        foreach (FarmPlotInteraction plot in UnityEngine.Object.FindObjectsByType<FarmPlotInteraction>(FindObjectsSortMode.None))
            if (plot != null && plot.CurrentCrop != null) return true;
        return false;
    }

    static bool AnyResidentRequestCompletedToday()
    {
        foreach (NpcDialogue dialogue in UnityEngine.Object.FindObjectsByType<NpcDialogue>(FindObjectsSortMode.None))
            if (dialogue != null && dialogue.TryGetResidentRequest(out NpcDialogue.ResidentRequestState request)
                && request.CompletedToday) return true;
        return false;
    }

    static int CountPreparedSellableTypes()
    {
        var items = new HashSet<Item>();
        Inventory inventory = Inventory.instance;
        if (inventory != null)
        {
            AddSellableTypes(items, inventory.slots);
            if (inventory.hotbar != null) AddSellableTypes(items, inventory.hotbar.slots);
        }

        foreach (ShopSlot slot in UnityEngine.Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
            if (slot != null && !slot.IsEmpty && IsSellableNow(slot.currentItem.data))
                items.Add(slot.currentItem.data);

        return items.Count;
    }

    static void AddSellableTypes(HashSet<Item> items, List<InventorySlot> slots)
    {
        if (slots == null) return;
        foreach (InventorySlot slot in slots)
            if (slot != null && !slot.IsEmpty && IsSellableNow(slot.item))
                items.Add(slot.item);
    }

    static bool IsSellableNow(Item item)
    {
        return item != null
            && item.category != ItemCategory.Tool
            && item.toolType == ToolType.None
            && (TierService.Instance == null || TierService.Instance.IsUnlocked(item.requiredTier));
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
        SetTitleButtonLayout(false);
        if (_quitButton != null) _quitButton.gameObject.SetActive(false);
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
            "다음 날 시작");
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
        _secondaryButton = CreateButton(_flowPanel.transform, "SecondaryButton", "닫기", new Vector2(-150f, -250f), new Vector2(200f, 58f), OnSecondaryPressed);
        _quitButton = CreateButton(_flowPanel.transform, "QuitButton", "게임 종료", new Vector2(-260f, -250f), new Vector2(200f, 58f), OnQuitPressed);
        _quitButton.GetComponent<Image>().color = new Color(0.45f, 0.27f, 0.20f, 1f);
        _quitButton.gameObject.SetActive(false);
        _primaryText = _primaryButton.GetComponentInChildren<TextMeshProUGUI>();
        _secondaryText = _secondaryButton.GetComponentInChildren<TextMeshProUGUI>();

        SetFlowVisible(false);
    }

    void OnPrimaryPressed()
    {
        if (!_startupCompleted && _startupStep == StartupStep.Title)
        {
            _startupStep = _continueSaveAvailable
                ? StartupStep.NewGameConfirm
                : StartupStep.Name;
            ShowStartupStep();
            return;
        }

        if (!_startupCompleted && _startupStep == StartupStep.NewGameConfirm)
        {
            _startupStep = StartupStep.Name;
            ShowStartupStep();
            return;
        }

        if (Current == Stage.Done && _summaryShown)
        {
            bool advanced = DayNightShopLoopController.Instance != null
                && DayNightShopLoopController.Instance.TryStartNextDayAfterTutorial();
            CloseSummary();
            if (!advanced)
                Debug.LogWarning("[PlayableDay] Day 1 결산 후 다음 날 전환에 실패했습니다.");
        }
        else AdvanceStartupStep();
    }

    async void OnSecondaryPressed()
    {
        if (!_startupCompleted && _startupStep == StartupStep.NewGameConfirm)
        {
            _startupStep = StartupStep.Title;
            ShowStartupStep();
            return;
        }

        if (Current == Stage.Done && _summaryShown)
        {
            CloseSummary();
            return;
        }

        if (_startupCompleted || _startupStep != StartupStep.Title
            || !_continueSaveAvailable || _continueLoading || SaveManager.instance == null)
            return;

        _continueLoading = true;
        _primaryButton.interactable = false;
        _secondaryButton.interactable = false;
        if (_quitButton != null) _quitButton.interactable = false;
        if (_secondaryText != null) _secondaryText.text = "불러오는 중";

        try
        {
            await SaveManager.instance.LoadGameAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayableDay] 이어하기 실패: {ex.Message}");
        }

        if (this == null || _startupCompleted) return;

        _continueLoading = false;
        _primaryButton.interactable = true;
        if (_quitButton != null) _quitButton.interactable = true;
        if (_flowBody != null)
            _flowBody.text = "저장 데이터를 불러오지 못했습니다. 새 게임을 시작하거나 저장 파일을 확인해 주세요.";
        BeginContinueAvailabilityCheck();
    }

    void OnQuitPressed()
    {
        if (_startupCompleted || _startupStep != StartupStep.Title || _continueLoading)
            return;

        Application.Quit();
        if (Application.isEditor && _flowBody != null)
            _flowBody.text = "실제 빌드에서는 게임이 종료됩니다. Editor에서는 Play Mode를 직접 중지해 주세요.";
    }

    void SetTitleButtonLayout(bool title)
    {
        if (_primaryButton != null)
            ((RectTransform)_primaryButton.transform).anchoredPosition = title
                ? new Vector2(260f, -250f)
                : new Vector2(150f, -250f);

        if (_secondaryButton != null)
            ((RectTransform)_secondaryButton.transform).anchoredPosition = title
                ? new Vector2(0f, -250f)
                : new Vector2(-150f, -250f);
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
        _nextContinuationRefreshAt = 0f;
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
        _nextContinuationRefreshAt = 0f;
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
