using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Long-play layer for Project_PA 1.0 development.
//
// This controller deliberately sits beside the Day 1 demo controller instead of
// replacing it. Day 1 remains the onboarding route; Day 2-7 stabilize the
// operation loop, and Day 8-14 turn the already implemented storage,
// processing, hiring, category demand, tier, and village-change systems into
// a second-week campaign. Day 15-30 deepen those same playable systems into a
// first-month campaign with an explicit completion record. Day 31-45 then turn
// the same authored systems into a second-month opening campaign instead of
// dropping the player into an unstructured endless loop. Day 46-76 carry those
// operations through the existing 100,000G Tier 2 threshold. Day 77-90 then
// turns the existing Tier 2 kitchen, recipes, hiring, sales, and village-change
// systems into a complete kitchen-led value chain. Day 91-105 connects resident
// requests to the existing reputation gate, Tier 3, B08, and the authored
// furniture/clothes Luxury lines as a community-atelier campaign. Day 106+
// then exposes the existing headquarters audit as the final partner campaign
// and keeps a readable post-campaign operation loop after Tier 4.
public class LongPlayProgressionController : MonoBehaviour
{
    const int MonthOneFinalDay = 30;
    public const int SecondMonthOpeningFinalDay = 45;
    public const int TierTwoCampaignStartDay = 46;
    public const int TierTwoCampaignFinalDay = 76;
    public const int TierTwoKitchenCampaignStartDay = 77;
    public const int TierTwoKitchenCampaignFinalDay = 90;
    public const int TierThreeCampaignStartDay = 91;
    public const int TierThreeCampaignFinalDay = 105;
    public const int PartnerCampaignStartDay = 106;

    [Serializable]
    class SupplyEntry
    {
        public string resourcePath;
        public int count;

        public SupplyEntry(string resourcePath, int count)
        {
            this.resourcePath = resourcePath;
            this.count = count;
        }
    }

    [Serializable]
    class DayPlan
    {
        public int day;
        public string title;
        public string objective;
        public string managementFocus;
        public long revenueTarget;
        public float buyPriceMultiplier;
        public List<SupplyEntry> supplies = new List<SupplyEntry>();
    }

    public static LongPlayProgressionController Instance { get; private set; }

    [Header("Long Play Runtime")]
    public bool autoCreateUI = true;
    public bool enableDailyNpcSupply = true;
    [Range(2, 30)] public int finalPlannedDay = 7;

    [Header("UI")]
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI supplyText;

    [Header("Week One Completion")]
    public bool showWeekOneCompletion = true;

    readonly List<DayPlan> _plans = new List<DayPlan>();

    Canvas _canvas;
    Canvas _completionCanvas;
    TextMeshProUGUI _completionTitle;
    TextMeshProUGUI _completionSubtitle;
    TextMeshProUGUI _completionBody;
    TextMeshProUGUI _completionStatus;
    TextMeshProUGUI _continueButtonLabel;
    TextMeshProUGUI _quitButtonLabel;
    Button _continueButton;
    Button _quitButton;
    long _dayStartRevenue;
    int _dayStartMoney;
    int _lastSupplyDay;
    int _lastDisplayedDay = -1;
    string _lastSupplyResult = "NPC producer delivery pending.";
    bool _restoredFromSave;
    bool _weekCompletionOpen;
    bool _weekCompletionAcknowledged;
    bool _monthCompletionAcknowledged;
    bool _fullCampaignCompletionAcknowledged;
    bool _showingMonthOneCompletion;
    bool _showingFullCampaignCompletion;
    bool _completionBusy;
    float _nextCompletionCheckAt;
    float _nextTierThreeReputationCheckAt;
    float _timeScaleBeforeCompletion = 1f;
    CursorLockMode _cursorLockBeforeCompletion = CursorLockMode.Locked;
    bool _cursorVisibleBeforeCompletion;

    public int LastSupplyDay => _lastSupplyDay;
    public long DayStartRevenue => _dayStartRevenue;
    public int DayStartMoney => _dayStartMoney;
    public string CurrentGoalText => objectiveText != null ? objectiveText.text : string.Empty;
    public bool IsWeekCompletionOpen => _weekCompletionOpen;
    public bool IsMilestoneCompletionOpen => _weekCompletionOpen;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsurePlans();

        if (autoCreateUI)
        {
            if (objectiveText == null)
                BuildUI();
            BuildWeekCompletionUI();
        }
    }

    void Start()
    {
        if (!_restoredFromSave)
            CaptureDayBaselines();

        if (GameClock.Instance != null)
            GameClock.Instance.OnNewDay += OnNewDay;

        if (EconomyService.Instance != null)
        {
            EconomyService.Instance.OnMoneyChanged += OnMoneyChanged;
            EconomyService.Instance.OnCumulativeRevenueChanged += OnRevenueChanged;
        }

        if (HiringService.Instance != null)
            HiringService.Instance.OnHired += OnHired;

        RefreshUI(force: true);
    }

    void Update()
    {
        TryGrantCampaignReputation();
        RefreshRestoredFullCampaignAcknowledgement();

        if (!showWeekOneCompletion || _weekCompletionOpen
            || (_weekCompletionAcknowledged && _monthCompletionAcknowledged
                && _fullCampaignCompletionAcknowledged))
            return;

        if (Time.unscaledTime < _nextCompletionCheckAt)
            return;

        _nextCompletionCheckAt = Time.unscaledTime + 0.25f;
        if (!_fullCampaignCompletionAcknowledged && ShouldShowFullCampaignCompletion())
            ShowFullCampaignCompletion();
        else if (!_monthCompletionAcknowledged && ShouldShowMonthOneCompletion())
            ShowMonthOneCompletion();
        else if (!_weekCompletionAcknowledged && ShouldShowWeekOneCompletion())
            ShowWeekOneCompletion();
    }

    void TryGrantCampaignReputation()
    {
        if (Time.unscaledTime < _nextTierThreeReputationCheckAt)
            return;

        _nextTierThreeReputationCheckAt = Time.unscaledTime + 0.5f;

        GameClock clock = GameClock.Instance;
        DayNightShopLoopController loop = DayNightShopLoopController.Instance;
        TierService tier = TierService.Instance;
        if (clock == null || clock.CurrentDay < TierThreeCampaignStartDay
            || loop == null || loop.CurrentPhase != PADayNightPhase.DayPreparation
            || tier == null || tier.CurrentTier < 2 || tier.CurrentTier >= 4
            || !AnyResidentRequestCompletedToday())
            return;

        int reputationTarget;
        if (tier.CurrentTier == 2)
        {
            reputationTarget = 3;
        }
        else
        {
            if (clock.CurrentDay < PartnerCampaignStartDay)
                return;

            reputationTarget = AuditService.Instance != null
                ? Mathf.Max(3, AuditService.Instance.requiredReputationForAudit)
                : 5;
        }

        if (tier.Reputation >= reputationTarget)
            return;

        string activityId = $"tier3-reputation:{clock.CurrentDay}";
        if (loop.IsDailyActivityCompleted(activityId)
            || !loop.TryCompleteDailyActivity(activityId))
            return;

        tier.AddReputation(1);
        RefreshUI(force: true);
        Debug.Log($"[LongPlay] Day {clock.CurrentDay} specialist request: village reputation "
            + $"{tier.Reputation}/{reputationTarget}, Tier {tier.CurrentTier}.");
    }

    void RefreshRestoredFullCampaignAcknowledgement()
    {
        if (!_restoredFromSave || _fullCampaignCompletionAcknowledged
            || GameClock.Instance == null || TierService.Instance == null
            || AuditService.Instance == null || TierService.Instance.CurrentTier < 4)
            return;

        // 별도 저장 필드 없이 기존 lastAuditDay를 이용한다. 감사 당일 저장은
        // 완주 화면을 다시 보여 주고, "저장하고 계속"으로 다음 날 넘어간 저장은
        // 이미 확인한 완주로 복원한다.
        if (GameClock.Instance.CurrentDay > AuditService.Instance.LastAuditDay)
            _fullCampaignCompletionAcknowledged = true;
    }

    static bool AnyResidentRequestCompletedToday()
    {
        foreach (NpcDialogue dialogue in
            UnityEngine.Object.FindObjectsByType<NpcDialogue>(FindObjectsSortMode.None))
        {
            if (dialogue != null
                && dialogue.TryGetResidentRequest(out NpcDialogue.ResidentRequestState request)
                && request.CompletedToday)
                return true;
        }

        return false;
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.OnNewDay -= OnNewDay;

        if (EconomyService.Instance != null)
        {
            EconomyService.Instance.OnMoneyChanged -= OnMoneyChanged;
            EconomyService.Instance.OnCumulativeRevenueChanged -= OnRevenueChanged;
        }

        if (HiringService.Instance != null)
            HiringService.Instance.OnHired -= OnHired;

        if (_weekCompletionOpen)
            RestoreCompletionState();

        if (Instance == this)
            Instance = null;
    }

    void OnNewDay(int day)
    {
        HandleNewDay(day, forceSupply: false);
    }

    void OnMoneyChanged(int _) => RefreshUI(force: false);
    void OnRevenueChanged(long _) => RefreshUI(force: false);
    void OnHired(NpcCandidateData _, GameObject __) => RefreshUI(force: true);

    void CaptureDayBaselines()
    {
        _dayStartRevenue = EconomyService.Instance != null ? EconomyService.Instance.CumulativeRevenue : 0L;
        _dayStartMoney = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;
    }

    void HandleNewDay(int day, bool forceSupply)
    {
        CaptureDayBaselines();

        if (day >= 2 && day <= finalPlannedDay)
            TryGrantDailySupply(day, forceSupply);

        RefreshUI(force: true);
    }

    public bool SimulateNewDayForValidation(int day)
    {
        if (GameClock.Instance != null)
            GameClock.Instance.ForceSet(8f, day, "LongPlayProgressionValidator");

        int before = CountSellableItems();
        HandleNewDay(day, forceSupply: true);
        int after = CountSellableItems();

        return day < 2 || after > before;
    }

    public void RestoreSavedSession(int lastSupplyDay, long dayStartRevenue, int dayStartMoney)
    {
        _lastSupplyDay = Mathf.Max(0, lastSupplyDay);
        _dayStartRevenue = Math.Max(0L, dayStartRevenue);
        _dayStartMoney = Mathf.Max(0, dayStartMoney);
        _restoredFromSave = true;
        _weekCompletionAcknowledged = GameClock.Instance != null
            && GameClock.Instance.CurrentDay > finalPlannedDay;
        _monthCompletionAcknowledged = GameClock.Instance != null
            && GameClock.Instance.CurrentDay > MonthOneFinalDay;
        _fullCampaignCompletionAcknowledged = false;
        _lastSupplyResult = _lastSupplyDay > 0
            ? $"Restored NPC producer delivery state through Day {_lastSupplyDay}."
            : "NPC producer delivery pending.";
        RefreshUI(force: true);
    }

    bool ShouldShowWeekOneCompletion()
    {
        return GameClock.Instance != null
            && GameClock.Instance.CurrentDay == finalPlannedDay
            && DayNightShopLoopController.Instance != null
            && DayNightShopLoopController.Instance.CurrentPhase == PADayNightPhase.Settlement;
    }

    bool ShouldShowMonthOneCompletion()
    {
        return GameClock.Instance != null
            && GameClock.Instance.CurrentDay == MonthOneFinalDay
            && DayNightShopLoopController.Instance != null
            && DayNightShopLoopController.Instance.CurrentPhase == PADayNightPhase.Settlement;
    }

    bool ShouldShowFullCampaignCompletion()
    {
        return GameClock.Instance != null
            && GameClock.Instance.CurrentDay >= PartnerCampaignStartDay
            && TierService.Instance != null
            && TierService.Instance.CurrentTier >= 4
            && DayNightShopLoopController.Instance != null
            && DayNightShopLoopController.Instance.CurrentPhase == PADayNightPhase.Settlement;
    }

    void ShowWeekOneCompletion()
    {
        ShowCompletion(monthOne: false);
    }

    void ShowMonthOneCompletion()
    {
        ShowCompletion(monthOne: true);
    }

    void ShowFullCampaignCompletion()
    {
        ShowCompletion(monthOne: false, fullCampaign: true);
    }

    void ShowCompletion(bool monthOne, bool fullCampaign = false)
    {
        if (_completionCanvas == null || _weekCompletionOpen)
            return;

        _timeScaleBeforeCompletion = Time.timeScale;
        _cursorLockBeforeCompletion = Cursor.lockState;
        _cursorVisibleBeforeCompletion = Cursor.visible;

        _weekCompletionOpen = true;
        _showingFullCampaignCompletion = fullCampaign;
        _showingMonthOneCompletion = monthOne && !fullCampaign;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _completionCanvas.gameObject.SetActive(true);
        _completionCanvas.transform.SetAsLastSibling();
        ConfigureCompletionPresentation();
        RefreshCompletionSummary();
        SetCompletionBusy(false);
        SetCompletionStatus(fullCampaign
            ? "최종 감사와 마을 운영 기록을 저장하고 자유 운영을 계속하거나 완주를 마치세요."
            : monthOne
                ? "첫 달의 운영 기록을 저장하고 계속 운영하거나 완주를 마치세요."
                : "첫 주의 운영 기록을 저장하고 다음 여정을 선택하세요.");
    }

    void HideWeekOneCompletion()
    {
        if (!_weekCompletionOpen)
            return;

        RestoreCompletionState();
        if (_completionCanvas != null)
            _completionCanvas.gameObject.SetActive(false);
    }

    void RestoreCompletionState()
    {
        _weekCompletionOpen = false;
        _completionBusy = false;
        Time.timeScale = _timeScaleBeforeCompletion;
        Cursor.lockState = _cursorLockBeforeCompletion;
        Cursor.visible = _cursorVisibleBeforeCompletion;
    }

    void ConfigureCompletionPresentation()
    {
        if (_completionTitle != null)
            _completionTitle.text = _showingFullCampaignCompletion
                ? "마을 파트너 캠페인 완주"
                : _showingMonthOneCompletion ? "첫 달 운영 완주" : "첫 주 운영 완료";
        if (_completionSubtitle != null)
        {
            _completionSubtitle.text = _showingFullCampaignCompletion
                ? "주민의 생활, 잡화점, 마을 변화와 본사 감사를 잇는 전체 성장 경로를 완주했습니다."
                : _showingMonthOneCompletion
                    ? "낮의 생활과 밤의 잡화점이 이어진 30일의 마을 경제를 완성했습니다."
                    : "내가 판 물건이 마을의 다음 풍경을 만들기 시작했습니다.";
        }
        if (_continueButtonLabel != null)
        {
            int nextDay = GameClock.Instance != null ? GameClock.Instance.CurrentDay + 1 : PartnerCampaignStartDay + 1;
            _continueButtonLabel.text = _showingFullCampaignCompletion
                ? $"저장하고 Day {nextDay} 자유 운영"
                : _showingMonthOneCompletion
                    ? "저장하고 Day 31 계속"
                    : "저장하고 2주차 계속";
        }
        if (_quitButtonLabel != null)
            _quitButtonLabel.text = _showingFullCampaignCompletion || _showingMonthOneCompletion
                ? "완주 저장 후 종료"
                : "저장 후 종료";
    }

    void RefreshCompletionSummary()
    {
        if (_completionBody == null)
            return;

        string playerName = "점주";
        var playableDay = FindFirstObjectByType<PlayableDayScenarioController>();
        if (playableDay != null && !string.IsNullOrWhiteSpace(playableDay.PlayerName))
            playerName = playableDay.PlayerName.Trim();

        long revenue = EconomyService.Instance != null
            ? EconomyService.Instance.CumulativeRevenue
            : 0L;
        int money = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;
        int tier = TierService.Instance != null ? TierService.Instance.CurrentTier : 0;
        int reputation = TierService.Instance != null ? TierService.Instance.Reputation : 0;
        int hiredCount = HiringService.Instance != null ? HiringService.Instance.HiredCount : 0;
        string hiredRoster = BuildHiredRosterSummary(3);
        int closingDay = _showingFullCampaignCompletion && GameClock.Instance != null
            ? GameClock.Instance.CurrentDay
            : _showingMonthOneCompletion ? MonthOneFinalDay : finalPlannedDay;
        string closingSummary = SalesLogManager.Instance != null
            ? SalesLogManager.Instance.BuildDailyDecisionSummary(closingDay)
            : "마지막 날 판매 기록을 확인할 수 없습니다.";

        if (_showingFullCampaignCompletion)
        {
            VillageCultureVisualController culture = VillageCultureVisualController.Instance;
            string villageChange = culture != null && culture.HasActiveCategory
                ? ResolveVillageCategoryLabel(culture.ActiveCategory)
                : "자유 운영 중";
            AuditService audit = AuditService.Instance;
            long requiredRevenue = audit != null ? audit.requiredRevenueForAudit : 500000L;
            int requiredReputation = audit != null ? audit.requiredReputationForAudit : 5;
            int requiredHired = audit != null ? audit.requiredHiredNpcs : 3;
            _completionBody.text =
                $"{playerName} 님, 낮의 마을 생활과 밤의 잡화점을 성장시켜 최종 본사 감사를 통과했습니다.\n\n" +
                $"누적 매출  {revenue:N0}/{requiredRevenue:N0} G     보유금  {money:N0} G\n" +
                $"상점 Tier  {tier}/4     마을 평판  {reputation}/{requiredReputation}\n" +
                $"고용 인력  {hiredCount}/{requiredHired}명 · {hiredRoster}\n" +
                $"마을의 주된 변화  {villageChange}\n\n" +
                $"Day {closingDay} 정산\n{closingSummary}\n\n" +
                "직접 모은 재료와 주민의 도움, 가공·진열·판매가 마을의 풍경과 최종 성장으로 이어졌습니다. 이후에도 같은 저장에서 자유 운영을 계속할 수 있습니다.";
            return;
        }

        if (_showingMonthOneCompletion)
        {
            VillageCultureVisualController culture = VillageCultureVisualController.Instance;
            string villageChange = culture != null && culture.HasActiveCategory
                ? ResolveVillageCategoryLabel(culture.ActiveCategory)
                : "아직 정착 전";
            _completionBody.text =
                $"{playerName} 님, 30일 동안 낮의 생활과 밤의 상점 운영을 연결해 첫 달을 완주했습니다.\n\n" +
                $"누적 매출  {revenue:N0} G     보유금  {money:N0} G\n" +
                $"상점 Tier  {tier}     마을 평판  {reputation}\n" +
                $"고용 인력  {hiredCount}명 · {hiredRoster}\n" +
                $"마을의 주된 변화  {villageChange}\n\n" +
                $"Day {MonthOneFinalDay} 정산\n{closingSummary}\n\n" +
                "작은 좌판에서 시작한 상품 흐름이 주민의 일과 마을 풍경을 바꾸는 한 달의 운영 기록이 완성되었습니다.";
            return;
        }

        _completionBody.text =
            $"{playerName} 님, 낮에는 마을을 돕고 밤에는 잡화점을 운영하며 첫 {finalPlannedDay}일을 완주했습니다.\n\n" +
            $"누적 매출  {revenue:N0} G     보유금  {money:N0} G\n" +
            $"상점 Tier  {tier}     마을 평판  {reputation}\n\n" +
            $"고용 인력  {hiredCount}명 · {hiredRoster}\n\n" +
            $"마지막 정산\n{closingSummary}\n\n" +
            "2주차에는 지금까지 만든 마을 변화와 상품 흐름을 이어서 운영할 수 있습니다.";
    }

    async void SaveAndContinueWeekTwo()
    {
        if (_completionBusy)
            return;

        if (SaveManager.instance == null)
        {
            SetCompletionStatus("저장 서비스를 찾지 못해 진행을 보류했습니다.", true);
            return;
        }

        bool fullCampaignCompletion = _showingFullCampaignCompletion;
        bool monthOneCompletion = _showingMonthOneCompletion;
        string milestoneName = fullCampaignCompletion ? "마을 파트너 캠페인"
            : monthOneCompletion ? "첫 달" : "첫 주";
        int nextDay = fullCampaignCompletion && GameClock.Instance != null
            ? GameClock.Instance.CurrentDay + 1
            : monthOneCompletion ? MonthOneFinalDay + 1 : finalPlannedDay + 1;

        SetCompletionBusy(true);
        SetCompletionStatus($"{milestoneName} 기록을 저장하는 중...");

        try
        {
            await SaveManager.instance.SaveGameAsync();
        }
        catch (Exception ex)
        {
            if (this == null) return;
            Debug.LogError($"[LongPlay] {milestoneName} 저장 실패: {ex.Message}");
            SetCompletionBusy(false);
            SetCompletionStatus($"저장하지 못해 Day {nextDay} 시작을 보류했습니다.", true);
            return;
        }

        if (this == null)
            return;

        if (fullCampaignCompletion)
            _fullCampaignCompletionAcknowledged = true;
        else if (monthOneCompletion)
            _monthCompletionAcknowledged = true;
        else
            _weekCompletionAcknowledged = true;
        HideWeekOneCompletion();

        bool advanced = DayNightShopLoopController.Instance != null
            && DayNightShopLoopController.Instance.TryStartNextDay();
        if (!advanced)
        {
            if (fullCampaignCompletion)
            {
                _fullCampaignCompletionAcknowledged = false;
                ShowFullCampaignCompletion();
            }
            else if (monthOneCompletion)
            {
                _monthCompletionAcknowledged = false;
                ShowMonthOneCompletion();
            }
            else
            {
                _weekCompletionAcknowledged = false;
                ShowWeekOneCompletion();
            }
            SetCompletionStatus("다음 날을 시작하지 못했습니다. 정산 상태를 확인하세요.", true);
            return;
        }

        try
        {
            await SaveManager.instance.SaveGameAsync();
            Debug.Log($"[LongPlay] {milestoneName} 완주 저장 및 Day {nextDay} 시작 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LongPlay] Day {nextDay} 자동 저장 실패: {ex.Message}");
            _lastSupplyResult = $"Day {nextDay}은 시작했지만 자동 저장에 실패했습니다. Pause 메뉴에서 다시 저장하세요.";
            RefreshUI(force: true);
        }
    }

    async void SaveAndQuitAfterWeekOne()
    {
        if (_completionBusy)
            return;

        if (SaveManager.instance == null)
        {
            SetCompletionStatus("저장할 수 없어 종료를 취소했습니다.", true);
            return;
        }

        string milestoneName = _showingFullCampaignCompletion ? "마을 파트너 캠페인"
            : _showingMonthOneCompletion ? "첫 달" : "첫 주";
        SetCompletionBusy(true);
        SetCompletionStatus($"{milestoneName} 기록을 저장한 뒤 종료하는 중...");

        try
        {
            await SaveManager.instance.SaveGameAsync();
            if (this == null) return;

            SetCompletionStatus("저장 완료 · 게임을 종료합니다.");
            Application.Quit();
            if (Application.isEditor)
            {
                SetCompletionBusy(false);
                SetCompletionStatus("저장 완료 · 실제 빌드에서는 여기서 종료됩니다.");
            }
        }
        catch (Exception ex)
        {
            if (this == null) return;
            Debug.LogError($"[LongPlay] {milestoneName} 저장 후 종료 실패: {ex.Message}");
            SetCompletionBusy(false);
            SetCompletionStatus("저장에 실패해 종료를 취소했습니다.", true);
        }
    }

    void SetCompletionBusy(bool busy)
    {
        _completionBusy = busy;
        if (_continueButton != null) _continueButton.interactable = !busy;
        if (_quitButton != null) _quitButton.interactable = !busy;
    }

    void SetCompletionStatus(string message, bool isError = false)
    {
        if (_completionStatus == null)
            return;

        _completionStatus.text = message;
        _completionStatus.color = isError
            ? new Color(0.78f, 0.18f, 0.12f, 1f)
            : new Color(0.18f, 0.38f, 0.25f, 1f);
    }

    public void WriteSaveFields(SaveData data)
    {
        if (data == null) return;

        data.longPlayLastSupplyDay = _lastSupplyDay;
        data.longPlayDayStartRevenue = _dayStartRevenue;
        data.longPlayDayStartMoney = _dayStartMoney;
    }

    void TryGrantDailySupply(int day, bool force)
    {
        if (!enableDailyNpcSupply) return;
        if (!force && _lastSupplyDay >= day) return;

        DayPlan plan = GetPlan(day);
        if (plan == null || plan.supplies == null || plan.supplies.Count == 0)
        {
            _lastSupplyResult = $"Day {day}: no producer delivery plan registered.";
            _lastSupplyDay = day;
            return;
        }

        int deliveredUnits = 0;
        int spent = 0;
        int skipped = 0;
        var summary = new List<string>();

        foreach (var supply in plan.supplies)
        {
            if (supply == null || string.IsNullOrEmpty(supply.resourcePath) || supply.count <= 0)
                continue;

            Item item = Resources.Load<Item>(supply.resourcePath);
            if (item == null)
            {
                skipped++;
                summary.Add($"missing:{supply.resourcePath}");
                continue;
            }

            int count = Mathf.Max(1, supply.count);
            int unitPrice = Mathf.Max(1, Mathf.RoundToInt(item.basePrice * Mathf.Max(0.1f, plan.buyPriceMultiplier)));
            int totalCost = unitPrice * count;

            if (EconomyService.Instance != null
                && !EconomyService.Instance.TrySpend(totalCost, $"LongPlay NPC buy-in Day {day}: {item.itemName} x{count}"))
            {
                skipped++;
                summary.Add($"{item.itemName} held: low cash");
                continue;
            }

            var instance = new ItemInstance(item, count)
            {
                quality = Mathf.Clamp(1f + 0.01f * day, 1f, 1.15f),
                currentPrice = item.basePrice
            };

            bool added = Inventory.instance != null && Inventory.instance.AddInstance(instance);
            if (!added)
            {
                if (EconomyService.Instance != null)
                    EconomyService.Instance.TryModifyMoney(totalCost, $"LongPlay buy-in refund Day {day}: inventory full");

                skipped++;
                summary.Add($"{item.itemName} held: inventory full");
                continue;
            }

            deliveredUnits += count;
            spent += totalCost;
            summary.Add($"{item.itemName} x{count}");
        }

        _lastSupplyDay = day;
        if (deliveredUnits > 0)
        {
            _lastSupplyResult =
                $"Day {day}: producer delivery {deliveredUnits} units / buy-in {spent}G / {string.Join(", ", summary)}";
        }
        else
        {
            _lastSupplyResult =
                $"Day {day}: delivery blocked or held ({skipped}) / {string.Join(", ", summary)}";
        }

        Debug.Log($"[LongPlay] {_lastSupplyResult}");
    }

    void RefreshUI(bool force)
    {
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        if (!force && day == _lastDisplayedDay && objectiveText == null && supplyText == null)
            return;

        _lastDisplayedDay = day;
        DayPlan plan = GetPlan(day);

        if (objectiveText != null)
        {
            bool partnerComplete = false;
            string partnerCompleteLabel = string.Empty;
            string partnerActionLabel = string.Empty;
            bool hasPartnerStatus = plan == null
                && TryGetPartnerCampaignStatus(day, out partnerComplete,
                    out partnerCompleteLabel, out partnerActionLabel);
            string title = plan != null ? plan.title : ResolveLongTermTitle(day);
            string objective = plan != null
                ? plan.objective
                : hasPartnerStatus
                    ? partnerComplete ? partnerCompleteLabel : partnerActionLabel
                    : "Repeat stock planning, price review, customer response, processing, and growth decisions.";
            string focus = plan != null
                ? plan.managementFocus
                : hasPartnerStatus
                    ? partnerComplete
                        ? "Focus: keep the completed village economy alive through free operation."
                        : "Focus: satisfy the existing headquarters audit without bypassing village work, revenue, or hiring."
                    : "Manager focus: balance inventory turnover, customer demand, and tier goals.";
            long target = plan != null ? plan.revenueTarget : GetCampaignRevenueTarget(day);
            long revenue = EconomyService.Instance != null ? EconomyService.Instance.CumulativeRevenue : 0L;
            long todayRevenue = Math.Max(0L, revenue - _dayStartRevenue);
            long remaining = Math.Max(0L, target - revenue);
            string workforceLine = day >= 5
                ? HiringService.Instance != null && HiringService.Instance.HiredCount > 0
                    ? $"Workforce: {HiringService.Instance.HiredCount} hired · {BuildHiredRosterSummary(2)}"
                    : "Workforce: open P.A. Phone > Hiring and recruit your first producer or specialist."
                : string.Empty;

            objectiveText.text =
                $"Long Play Day {day} - {title}\n" +
                $"{objective}\n" +
                $"Today revenue {todayRevenue:N0}G / total {revenue:N0}G / next target {remaining:N0}G left\n" +
                (string.IsNullOrEmpty(workforceLine) ? string.Empty : $"{workforceLine}\n") +
                focus;
        }

        if (supplyText != null)
        {
            supplyText.text = day >= 8
                ? "Campaign: automatic onboarding supply is over. Gather, buy from producers, process, and organize stock directly."
                : _lastSupplyResult;
        }
    }

    string ResolveLongTermTitle(int day)
    {
        if (day <= 1) return "Operations onboarding";
        if (day <= 7) return "Week 1 operations stabilization";
        if (day <= 14) return "Week 2 expansion preparation";
        if (day <= 30) return "Month 1 town growth";
        if (day <= SecondMonthOpeningFinalDay) return "Month 2 local economy";
        if (day <= TierTwoCampaignFinalDay) return "Tier 2 local economy expansion";
        if (day <= TierTwoKitchenCampaignFinalDay) return "Tier 2 kitchen value chain";
        if (day <= TierThreeCampaignFinalDay) return "Tier 3 community atelier";
        if (TierService.Instance != null && TierService.Instance.CurrentTier >= 4)
            return "Village partner free operation";
        return "Headquarters partner audit";
    }

    public static long GetCampaignRevenueTarget(int day)
    {
        if (day <= 7) return 300 + 120L * day;
        if (day <= 14) return 1500 + 250L * (day - 7);
        if (day <= 30) return 15000 + 1000L * (day - 14);
        if (day >= PartnerCampaignStartDay)
            return AuditService.Instance != null
                ? Math.Max(0L, AuditService.Instance.requiredRevenueForAudit)
                : 500000L;
        return 31000 + 1500L * (day - 30);
    }

    public static bool TryGetPartnerCampaignStatus(int day, out bool complete,
        out string completeLabel, out string actionLabel)
    {
        complete = false;
        completeLabel = string.Empty;
        actionLabel = string.Empty;
        if (day < PartnerCampaignStartDay)
            return false;

        TierService tier = TierService.Instance;
        AuditService audit = AuditService.Instance;
        if (tier == null || audit == null)
        {
            actionLabel = "본사 감사 상태를 불러오는 중입니다. P.A. Phone 감사 앱을 다시 확인하세요.";
            return true;
        }

        long revenue = EconomyService.Instance != null
            ? EconomyService.Instance.CumulativeRevenue
            : 0L;
        int reputation = tier.Reputation;
        int hiredCount = HiringService.Instance != null ? HiringService.Instance.HiredCount : 0;
        long requiredRevenue = Math.Max(0L, audit.requiredRevenueForAudit);
        int requiredReputation = Mathf.Max(0, audit.requiredReputationForAudit);
        int requiredHired = Mathf.Max(0, audit.requiredHiredNpcs);

        if (tier.CurrentTier >= 4)
        {
            complete = true;
            completeLabel = $"최종 본사 감사 통과 · Tier {tier.CurrentTier} 마을 파트너 자유 운영";
            actionLabel = completeLabel;
            return true;
        }

        if (reputation < requiredReputation)
        {
            actionLabel = "전문 주민의 낮 재료 요청을 완료해 본사 감사 평판을 준비하세요 · "
                + $"{reputation}/{requiredReputation} (하루 1회)";
            return true;
        }

        if (hiredCount < requiredHired)
        {
            actionLabel = $"P.A. Phone 채용 앱에서 감사 운영 인력을 확보하세요 · "
                + $"{hiredCount}/{requiredHired}명";
            return true;
        }

        if (revenue < requiredRevenue)
        {
            actionLabel = $"낮 준비→밤 판매를 반복해 본사 감사 누적 매출을 달성하세요 · "
                + $"{revenue:N0}/{requiredRevenue:N0}G";
            return true;
        }

        int interval = Mathf.Max(1, audit.auditIntervalDays);
        int nextAuditDay = audit.LastAuditDay + interval;
        while (nextAuditDay < day)
            nextAuditDay += interval;
        actionLabel = $"감사 조건 완료 · Day {nextAuditDay} 정기 감사까지 상품 4종과 마을 운영을 유지하세요.";
        return true;
    }

    static string ResolveVillageCategoryLabel(ItemCategory category)
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

    public static string BuildHiredRosterSummary(int maxEntries)
    {
        if (HiringService.Instance == null || HiringService.Instance.HiredCount <= 0)
            return "아직 없음";

        var candidates = new List<NpcCandidateData>(HiringService.Instance.GetHiredCandidates());
        candidates.Sort((a, b) => string.CompareOrdinal(
            a != null ? a.ResolveDisplayName() : string.Empty,
            b != null ? b.ResolveDisplayName() : string.Empty));

        int visibleCount = Mathf.Clamp(maxEntries, 1, candidates.Count);
        var labels = new List<string>(visibleCount + 1);
        for (int i = 0; i < visibleCount; i++)
        {
            NpcCandidateData candidate = candidates[i];
            if (candidate == null) continue;
            labels.Add($"{candidate.ResolveDisplayName()}({ResolveSpecialtyLabel(candidate.specialty)})");
        }

        int hiddenCount = candidates.Count - visibleCount;
        if (hiddenCount > 0)
            labels.Add($"외 {hiddenCount}명");

        return labels.Count > 0 ? string.Join(" · ", labels) : "아직 없음";
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

    DayPlan GetPlan(int day)
    {
        EnsurePlans();
        foreach (var plan in _plans)
            if (plan.day == day) return plan;

        return null;
    }

    void EnsurePlans()
    {
        if (_plans.Count > 0) return;

        _plans.Add(new DayPlan
        {
            day = 1,
            title = "First operation day",
            objective = "Complete the first route: talk, stock, price, watch customer response, audit, and save.",
            managementFocus = "Focus: understand the first visible reverse supply-chain loop.",
            revenueTarget = 150,
            buyPriceMultiplier = 0.55f
        });

        _plans.Add(new DayPlan
        {
            day = 2,
            title = "Producer intake",
            objective = "Buy a small producer delivery and compare at least two stocked products.",
            managementFocus = "Focus: move NPC-produced resources into the shop economy.",
            revenueTarget = 300,
            buyPriceMultiplier = 0.50f,
            supplies = new List<SupplyEntry>
            {
                new SupplyEntry("Items/Item_Wheat", 4),
                new SupplyEntry("Items/Item_Fish", 2)
            }
        });

        _plans.Add(new DayPlan
        {
            day = 3,
            title = "Price experiment",
            objective = "Buy wood and ore, then compare fair, high, and low price reactions.",
            managementFocus = "Focus: find the balance between conversion chance and margin.",
            revenueTarget = 520,
            buyPriceMultiplier = 0.52f,
            supplies = new List<SupplyEntry>
            {
                new SupplyEntry("Items/Item_Wood", 5),
                new SupplyEntry("Items/Item_Ore", 3)
            }
        });

        _plans.Add(new DayPlan
        {
            day = 4,
            title = "Processing value check",
            objective = "Compare raw resource sales with processed goods and decide which chain deserves investment.",
            managementFocus = "Focus: move from raw sales toward a processing chain.",
            revenueTarget = 780,
            buyPriceMultiplier = 0.55f,
            supplies = new List<SupplyEntry>
            {
                new SupplyEntry("Items/Item_Carrot", 4),
                new SupplyEntry("Items/Item_Plank", 2)
            }
        });

        _plans.Add(new DayPlan
        {
            day = 5,
            title = "First workforce support",
            objective = "Open P.A. Phone > Hiring and recruit one producer or specialist for the village economy.",
            managementFocus = "Focus: turn repeated manual preparation into a deliberate NPC support choice.",
            revenueTarget = 1050,
            buyPriceMultiplier = 0.57f,
            supplies = new List<SupplyEntry>
            {
                new SupplyEntry("Items/Item_Fish", 3),
                new SupplyEntry("Items/Item_Wheat", 3)
            }
        });

        _plans.Add(new DayPlan
        {
            day = 6,
            title = "Operations pressure",
            objective = "Compare missing inventory, slow movers, and high-value products to choose next supply priority.",
            managementFocus = "Focus: manage inventory turnover and tier goals together.",
            revenueTarget = 1350,
            buyPriceMultiplier = 0.60f,
            supplies = new List<SupplyEntry>
            {
                new SupplyEntry("Items/Item_IronBar", 2),
                new SupplyEntry("Items/Item_BreadLoaf", 2)
            }
        });

        _plans.Add(new DayPlan
        {
            day = 7,
            title = "Weekly audit preparation",
            objective = "Review cumulative revenue, reputation, stock state, and what Week 2 expansion should unlock.",
            managementFocus = "Focus: close Week 1 and prepare the next tier of management choices.",
            revenueTarget = 1700,
            buyPriceMultiplier = 0.62f,
            supplies = new List<SupplyEntry>
            {
                new SupplyEntry("Items/Item_Plank", 3),
                new SupplyEntry("Items/Item_Ore", 3),
                new SupplyEntry("Items/Item_Fish", 2)
            }
        });

        // Week 2 intentionally grants no automatic onboarding stock. Each day
        // points at one already playable system so the Day 8 continuation is a
        // connected campaign instead of a generic endless-loop sentence.
        _plans.Add(new DayPlan
        {
            day = 8,
            title = "Storage and logistics",
            objective = "Move at least one sellable item into the village storage box, then prepare tonight's stock.",
            managementFocus = "Focus: separate reserve stock from goods that will be displayed tonight.",
            revenueTarget = 2100
        });

        _plans.Add(new DayPlan
        {
            day = 9,
            title = "Processed product test",
            objective = "Use an existing workbench chain and complete one processed-product sale.",
            managementFocus = "Focus: prove that daytime processing creates a different night-shop choice.",
            revenueTarget = 2600
        });

        _plans.Add(new DayPlan
        {
            day = 10,
            title = "Workforce operation",
            objective = "Recruit the first producer or specialist if needed, then operate with village support.",
            managementFocus = "Focus: turn repeated manual preparation into a visible workforce decision.",
            revenueTarget = 3300
        });

        _plans.Add(new DayPlan
        {
            day = 11,
            title = "Customer mix experiment",
            objective = "Sell products from at least two categories and compare customer responses.",
            managementFocus = "Focus: use different resident preferences instead of repeating one safe product.",
            revenueTarget = 4200
        });

        _plans.Add(new DayPlan
        {
            day = 12,
            title = "Shop expansion threshold",
            objective = "Review the next tier requirement and reach the indoor-shop tier when the economy is ready.",
            managementFocus = "Focus: convert cumulative revenue into a real shop-space upgrade.",
            revenueTarget = 10000
        });

        _plans.Add(new DayPlan
        {
            day = 13,
            title = "Village response review",
            objective = "Inspect the next-day village change created by a sold product category.",
            managementFocus = "Focus: read how yesterday's shelf choice changes today's village.",
            revenueTarget = 12000
        });

        _plans.Add(new DayPlan
        {
            day = 14,
            title = "Second-week assortment review",
            objective = "Complete sales of at least two different products and close the week with a broader assortment.",
            managementFocus = "Focus: finish Week 2 with a repeatable supply-to-village decision loop.",
            revenueTarget = 15000
        });

        // Month 1 keeps using the systems already visible to the player. The
        // plans alternate logistics, processing, workforce, assortment, shop
        // growth, and village feedback without adding another quest framework.
        AddMonthOnePlan(15, "Reserve stock foundation",
            "Organize at least three reserve items in storage before choosing tonight's display.",
            "Focus: keep a buffer so one sold-out shelf does not break the next day's plan.");
        AddMonthOnePlan(16, "Processing rhythm",
            "Prepare and sell a processed product while keeping one raw input in reserve.",
            "Focus: make processing a repeatable margin decision, not a one-time tutorial.");
        AddMonthOnePlan(17, "Two-role workforce",
            "Recruit a second producer or specialist and compare how the two roles support preparation.",
            "Focus: build a small local production team with distinct jobs.");
        AddMonthOnePlan(18, "Broader resident demand",
            "Sell from at least two product categories and review which residents responded.",
            "Focus: balance reliable staples with one different customer need.");
        AddMonthOnePlan(19, "Shop growth review",
            "Reach or maintain Tier 1 and use the larger shop space to improve the assortment.",
            "Focus: turn cumulative sales into a visible operating upgrade.");
        AddMonthOnePlan(20, "Village change reading",
            "Inspect the active village change and stock a product that supports or redirects it.",
            "Focus: make yesterday's sales part of today's town-planning choice.");
        AddMonthOnePlan(21, "Three-product close",
            "Complete sales of three different products before settlement.",
            "Focus: prove the shop can serve more than one repeated best seller.");
        AddMonthOnePlan(22, "Expanded storage route",
            "Build a five-item reserve across storage and the shop, then replenish only what is needed.",
            "Focus: separate logistics planning from last-minute shelf filling.");
        AddMonthOnePlan(23, "Forge launch night",
            "Install the Tier 1 forge, then sell one iron tool set alongside an everyday staple.",
            "Focus: turn wood and ore into the shop's first authored higher-value utility chain.");
        AddMonthOnePlan(24, "Forge value-chain night",
            "Sell an iron tool set and one processed staple such as an iron bar in the same night.",
            "Focus: repeat the forge route as a sustainable assortment instead of a one-time unlock.");
        AddMonthOnePlan(25, "Specialist village",
            "Operate with three hired residents or review which missing specialty should be recruited next.",
            "Focus: give production, processing, and shop support visibly different owners.");
        AddMonthOnePlan(26, "Category balance",
            "Complete sales across three product categories and compare purchase decisions.",
            "Focus: read the town's mixed needs instead of maximizing one category.");
        AddMonthOnePlan(27, "Processed batch",
            "Sell two processed goods and preserve enough raw stock for tomorrow.",
            "Focus: sustain a higher-value chain across multiple days.");
        AddMonthOnePlan(28, "Town identity check",
            "Review the current village visual change and choose tonight's stock direction deliberately.",
            "Focus: decide what kind of village the shop is helping to create.");
        AddMonthOnePlan(29, "Final reserve",
            "Organize eight reserve units and prepare a three-product final-day assortment.",
            "Focus: enter the last day with a stable local supply network.");
        AddMonthOnePlan(30, "First-month finale",
            "Sell three different products, settle the shop, and complete the first-month operating record.",
            "Focus: close the full day-life to night-shop to village-change campaign.");

        // The first half of Month 2 deliberately asks for stronger versions of
        // already learned decisions. It bridges the Day 30 completion record
        // toward Tier 2 without changing tier thresholds or inventing another
        // progression system.
        AddSecondMonthOpeningPlan(31, "Second-month stocktake",
            "Rebuild a ten-unit reserve before deciding which goods belong on tonight's shelves.",
            "Focus: begin the new month from a stable reserve instead of emergency restocking.");
        AddSecondMonthOpeningPlan(32, "Processed staple night",
            "Sell two processed staples and compare their margin with raw inputs.",
            "Focus: make processing carry a meaningful share of the nightly assortment.");
        AddSecondMonthOpeningPlan(33, "Three-role roster review",
            "Operate with three hired residents and review which production role is still missing.",
            "Focus: connect village residents to distinct production and preparation jobs.");
        AddSecondMonthOpeningPlan(34, "Assortment expansion",
            "Complete sales of three different products without relying on one repeated best seller.",
            "Focus: make a broader catalog resilient to changing resident demand.");
        AddSecondMonthOpeningPlan(35, "Growth checkpoint",
            "Reach today's cumulative-revenue checkpoint and review the remaining path to Tier 2.",
            "Focus: turn nightly sales into a visible long-term shop-growth plan.");
        AddSecondMonthOpeningPlan(36, "Forge maintenance",
            "Keep the Tier 1 forge in operation and sell one iron tool set.",
            "Focus: preserve the authored wood-to-metal utility chain beyond its tutorial days.");
        AddSecondMonthOpeningPlan(37, "Demand balance",
            "Complete sales across three product categories and compare resident responses.",
            "Focus: serve several village needs while choosing what identity to reinforce.");
        AddSecondMonthOpeningPlan(38, "Village feedback day",
            "Inspect the active village change and choose whether tonight's stock supports or redirects it.",
            "Focus: keep the shop-to-village consequence loop visible in the second month.");
        AddSecondMonthOpeningPlan(39, "Reserve expansion",
            "Organize twelve units of reserve stock before preparing the night assortment.",
            "Focus: support a larger catalog with deliberate storage logistics.");
        AddSecondMonthOpeningPlan(40, "Value-chain night",
            "Sell one iron tool set and two processed goods in the same night.",
            "Focus: combine a specialist utility product with reliable processed staples.");
        AddSecondMonthOpeningPlan(41, "Four-product shelf",
            "Complete sales of four different products before settlement.",
            "Focus: prove the expanded shop can support a varied nightly catalog.");
        AddSecondMonthOpeningPlan(42, "Night catalog preparation",
            "Prepare four different sellable product types before the shop opens.",
            "Focus: make the daytime plan visible on the shelves before customers arrive.");
        AddSecondMonthOpeningPlan(43, "Processed batch",
            "Complete three processed-product sales and retain tomorrow's raw inputs.",
            "Focus: sustain batch processing without exhausting the local supply chain.");
        AddSecondMonthOpeningPlan(44, "Town direction night",
            "Read the active village identity and sell three different products deliberately.",
            "Focus: use assortment choice to reinforce or redirect the town's visible direction.");
        AddSecondMonthOpeningPlan(45, "Second-month opening checkpoint",
            "Reach today's revenue checkpoint and sell four different products before settlement.",
            "Focus: close the authored Month 2 opening route ready for the Tier 2 campaign.");

        AddTierTwoCampaignPlans();
        AddTierTwoKitchenCampaignPlans();
        AddTierThreeCampaignPlans();
    }

    void AddMonthOnePlan(int day, string title, string objective, string managementFocus)
    {
        _plans.Add(new DayPlan
        {
            day = day,
            title = title,
            objective = objective,
            managementFocus = managementFocus,
            revenueTarget = GetCampaignRevenueTarget(day)
        });
    }

    void AddSecondMonthOpeningPlan(int day, string title, string objective, string managementFocus)
    {
        _plans.Add(new DayPlan
        {
            day = day,
            title = title,
            objective = objective,
            managementFocus = managementFocus,
            revenueTarget = GetCampaignRevenueTarget(day)
        });
    }

    void AddTierTwoCampaignPlans()
    {
        for (int day = TierTwoCampaignStartDay; day < TierTwoCampaignFinalDay; day++)
        {
            int cycle = GetTierTwoCampaignCycle(day);
            int cycleNumber = cycle + 1;

            switch (GetTierTwoCampaignPhase(day))
            {
                case 0:
                {
                    int target = GetTierTwoReserveTarget(day);
                    AddTierTwoCampaignPlan(day, $"Regional reserve cycle {cycleNumber}",
                        $"Organize {target} reserve units before choosing tonight's assortment.",
                        "Focus: make the larger shop resilient to stockouts and delayed producer supply.");
                    break;
                }
                case 1:
                {
                    int target = GetTierTwoProcessedSalesTarget(day);
                    AddTierTwoCampaignPlan(day, $"Processing capacity cycle {cycleNumber}",
                        $"Complete {target} processed-product sales while preserving tomorrow's raw inputs.",
                        "Focus: scale value-added production without exhausting the local supply chain.");
                    break;
                }
                case 2:
                    AddTierTwoCampaignPlan(day, $"Supported preparation cycle {cycleNumber}",
                        "Operate with three hired residents and prepare four different sellable product types.",
                        "Focus: let village support broaden the catalog without removing hands-on preparation.");
                    break;
                case 3:
                    AddTierTwoCampaignPlan(day, $"Demand balance cycle {cycleNumber}",
                        "Complete sales across three product categories and compare resident responses.",
                        "Focus: balance reliable staples, processed goods, and utility demand.");
                    break;
                case 4:
                    AddTierTwoCampaignPlan(day, $"Forge value-chain cycle {cycleNumber}",
                        "Keep the Tier 1 forge active and sell an iron tool set with a processed product.",
                        "Focus: sustain the authored wood-and-ore utility chain as a regular business line.");
                    break;
                case 5:
                    AddTierTwoCampaignPlan(day, $"Village direction cycle {cycleNumber}",
                        "Read the active village identity and complete sales of four different products.",
                        "Focus: use a broad assortment to reinforce or redirect the town's visible direction.");
                    break;
                default:
                    AddTierTwoCampaignPlan(day, $"Tier 2 revenue checkpoint {cycleNumber}",
                        "Reach today's cumulative-revenue checkpoint and review the remaining Tier 2 gap.",
                        "Focus: convert each weekly operating cycle into measurable shop growth.");
                    break;
            }
        }

        AddTierTwoCampaignPlan(TierTwoCampaignFinalDay, "Tier 2 breakthrough",
            "Reach the existing 100,000G threshold and confirm the automatic Tier 2 shop upgrade.",
            "Focus: complete the local-economy growth arc and open the next kitchen-led value chain.");
    }

    void AddTierTwoCampaignPlan(int day, string title, string objective, string managementFocus)
    {
        _plans.Add(new DayPlan
        {
            day = day,
            title = title,
            objective = objective,
            managementFocus = managementFocus,
            revenueTarget = GetCampaignRevenueTarget(day)
        });
    }

    void AddTierTwoKitchenCampaignPlans()
    {
        AddTierTwoKitchenCampaignPlan(77, "Kitchen installation",
            "Install the unlocked Tier 2 kitchen station in a usable shop preparation area.",
            "Focus: turn the Tier 2 breakthrough into a visible new production capability.");
        AddTierTwoKitchenCampaignPlan(78, "Bread line",
            "Use three wheat at the kitchen station, then sell one bread loaf.",
            "Focus: establish the kitchen with a clear farm-to-shelf staple.");
        AddTierTwoKitchenCampaignPlan(79, "Baked produce line",
            "Use two carrots at the kitchen station, then sell one baked potato.",
            "Focus: add a second authored kitchen output without inventing new ingredients.");
        AddTierTwoKitchenCampaignPlan(80, "Grilled fish line",
            "Use one fish at the kitchen station, then sell one grilled fish.",
            "Focus: connect fishing activity to the shop's highest-value existing kitchen recipe.");
        AddTierTwoKitchenCampaignPlan(81, "Two-course night",
            "Sell two different kitchen products in the same night.",
            "Focus: operate the kitchen as an assortment rather than a one-recipe unlock.");
        AddTierTwoKitchenCampaignPlan(82, "Processed village response",
            "Confirm that processed-product sales have become the active village direction.",
            "Focus: make the kitchen's effect on the town visible on the following day.");
        AddTierTwoKitchenCampaignPlan(83, "Full kitchen preparation",
            "Prepare bread, baked potato, and grilled fish before the shop opens.",
            "Focus: coordinate wheat, carrot, and fish supply into one complete daytime plan.");
        AddTierTwoKitchenCampaignPlan(84, "Full menu night",
            "Sell bread, baked potato, and grilled fish in the same night.",
            "Focus: prove that all three existing kitchen recipes reach customers.");
        AddTierTwoKitchenCampaignPlan(85, "Chef-supported kitchen",
            "Operate the placed kitchen with a hired chef in the village roster.",
            "Focus: give the kitchen value chain a visible specialist owner.");
        AddTierTwoKitchenCampaignPlan(86, "Kitchen assortment balance",
            "Sell a kitchen product while also completing sales across three product categories.",
            "Focus: keep the new processed line inside a balanced general-store catalog.");
        AddTierTwoKitchenCampaignPlan(87, "Kitchen batch night",
            "Complete four kitchen-product sales while preserving tomorrow's ingredients.",
            "Focus: scale the authored recipes into repeatable batch production.");
        AddTierTwoKitchenCampaignPlan(88, "Village kitchen identity",
            "Sell a kitchen product while the processed village direction is active.",
            "Focus: let shop stock and the visible town identity reinforce one another.");
        AddTierTwoKitchenCampaignPlan(89, "Kitchen growth checkpoint",
            "Reach today's cumulative-revenue checkpoint and review kitchen-line contribution.",
            "Focus: measure the new value chain as part of long-term shop growth.");
        AddTierTwoKitchenCampaignPlan(90, "Kitchen value-chain finale",
            "Keep the kitchen active, sell all three kitchen products, and finish with the processed village direction active.",
            "Focus: close the ingredient-to-kitchen-to-shop-to-village consequence loop.");
    }

    void AddTierTwoKitchenCampaignPlan(int day, string title, string objective, string managementFocus)
    {
        _plans.Add(new DayPlan
        {
            day = day,
            title = title,
            objective = objective,
            managementFocus = managementFocus,
            revenueTarget = GetCampaignRevenueTarget(day)
        });
    }

    void AddTierThreeCampaignPlans()
    {
        AddTierThreeCampaignPlan(91, "A trusted specialist",
            "Complete one specialist resident's daytime material request and earn the first village-reputation point.",
            "Focus: make useful daytime cooperation the explicit route into regional leadership.");
        AddTierThreeCampaignPlan(92, "Shared reliability",
            "Complete another specialist request on a new day and reach two village-reputation points.",
            "Focus: prove that resident support is a repeated relationship rather than a one-off delivery.");
        AddTierThreeCampaignPlan(93, "Regional trust",
            "Complete a third daily specialist request and confirm the existing automatic Tier 3 advancement.",
            "Focus: turn three days of practical help into the authored regional-leader shop tier.");
        AddTierThreeCampaignPlan(94, "Atelier installation",
            "Open the placement ledger and install the newly available B08 sewing table.",
            "Focus: make the Tier 3 breakthrough visible as a new player-operated production space.");
        AddTierThreeCampaignPlan(95, "Clothing line",
            "Use wheat and planks at B08 to make clothes, then sell at least one.",
            "Focus: connect farm and lumber supply to the existing sewing recipe and Luxury demand.");
        AddTierThreeCampaignPlan(96, "Two-craft assortment",
            "Sell clothes and furniture in the same night.",
            "Focus: operate B08 and the existing basic workbench as one coherent artisan assortment.");
        AddTierThreeCampaignPlan(97, "Artisan village response",
            "Confirm that Luxury sales have become the active village direction.",
            "Focus: make the shop's crafted assortment visible in the village's next-day identity.");
        AddTierThreeCampaignPlan(98, "Tailor-supported atelier",
            "Operate the placed B08 sewing table with a hired tailor in the village roster.",
            "Focus: give the clothing line a visible specialist owner without automating away preparation.");
        AddTierThreeCampaignPlan(99, "Luxury catalog balance",
            "Sell clothes while also completing sales across three product categories.",
            "Focus: keep the artisan line inside a balanced general-store catalog.");
        AddTierThreeCampaignPlan(100, "Atelier preparation",
            "Prepare both clothes and furniture before the shop opens.",
            "Focus: coordinate wheat and plank reserves into a complete daytime craft plan.");
        AddTierThreeCampaignPlan(101, "Atelier market night",
            "Sell clothes and furniture in the same operating day.",
            "Focus: prove that both authored Luxury recipes consistently reach customers.");
        AddTierThreeCampaignPlan(102, "Community workforce",
            "Keep B08 active and grow the hired village roster to four residents.",
            "Focus: support the larger regional shop with a broad, visible community workforce.");
        AddTierThreeCampaignPlan(103, "Artisan town identity",
            "With the Luxury village direction active, sell four different products.",
            "Focus: reinforce the visible town identity without abandoning general-store variety.");
        AddTierThreeCampaignPlan(104, "Regional growth checkpoint",
            "Reach today's cumulative-revenue checkpoint and review the artisan lines' contribution.",
            "Focus: measure community trust and crafted goods as part of long-term shop growth.");
        AddTierThreeCampaignPlan(105, "Community atelier finale",
            "Keep Tier 3 and B08 active, sell clothes and furniture, and finish with the Luxury village direction active.",
            "Focus: close the resident-help-to-reputation-to-atelier-to-shop-to-village consequence loop.");
    }

    void AddTierThreeCampaignPlan(int day, string title, string objective, string managementFocus)
    {
        _plans.Add(new DayPlan
        {
            day = day,
            title = title,
            objective = objective,
            managementFocus = managementFocus,
            revenueTarget = GetCampaignRevenueTarget(day)
        });
    }

    public static int GetTierTwoCampaignPhase(int day)
    {
        if (day < TierTwoCampaignStartDay || day >= TierTwoCampaignFinalDay)
            return -1;

        return (day - TierTwoCampaignStartDay) % 7;
    }

    public static int GetTierTwoCampaignCycle(int day)
    {
        return Mathf.Max(0, (day - TierTwoCampaignStartDay) / 7);
    }

    public static int GetTierTwoReserveTarget(int day)
    {
        return Mathf.Min(20, 12 + GetTierTwoCampaignCycle(day) * 2);
    }

    public static int GetTierTwoProcessedSalesTarget(int day)
    {
        return Mathf.Min(4, 2 + GetTierTwoCampaignCycle(day) / 2);
    }

    int CountSellableItems()
    {
        int count = 0;
        if (Inventory.instance == null) return count;

        CountSellable(Inventory.instance.slots, ref count);
        if (Inventory.instance.hotbar != null)
            CountSellable(Inventory.instance.hotbar.slots, ref count);
        return count;
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

    void BuildUI()
    {
        var canvasGo = new GameObject("LongPlayProgressionCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 54;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject("LongPlayPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)panel.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -112f);
        rt.sizeDelta = new Vector2(590f, 128f);

        var bg = panel.GetComponent<Image>();
        bg.color = new Color(0.03f, 0.04f, 0.035f, 0.58f);
        bg.raycastTarget = false;

        objectiveText = CreateText(panel.transform, "LongPlayObjectiveText",
            new Vector2(14f, -10f), new Vector2(562f, 82f), 15f, FontStyles.Bold);
        objectiveText.alignment = TextAlignmentOptions.TopLeft;
        objectiveText.color = new Color(0.95f, 0.98f, 0.90f, 1f);

        supplyText = CreateText(panel.transform, "LongPlaySupplyText",
            new Vector2(14f, -92f), new Vector2(562f, 28f), 12f, FontStyles.Normal);
        supplyText.alignment = TextAlignmentOptions.TopLeft;
        supplyText.color = new Color(0.72f, 0.92f, 0.78f, 1f);
    }

    void BuildWeekCompletionUI()
    {
        if (_completionCanvas != null)
            return;

        var canvasGo = new GameObject("WeekOneCompletionCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _completionCanvas = canvasGo.GetComponent<Canvas>();
        _completionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _completionCanvas.sortingOrder = 240;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(canvasGo.transform, false);
        var dimRt = (RectTransform)dim.transform;
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        dim.GetComponent<Image>().color = new Color(0.025f, 0.055f, 0.04f, 0.88f);

        var panel = new GameObject("CompletionPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(dim.transform, false);
        var panelRt = (RectTransform)panel.transform;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(920f, 720f);
        panel.GetComponent<Image>().color = new Color(0.96f, 0.92f, 0.80f, 0.99f);

        _completionTitle = CreateCompletionText(panel.transform, "CompletionTitle", "첫 주 운영 완료",
            new Vector2(0f, 280f), new Vector2(820f, 70f), 42f,
            new Color(0.08f, 0.38f, 0.24f, 1f), FontStyles.Bold);
        _completionTitle.alignment = TextAlignmentOptions.Center;

        _completionSubtitle = CreateCompletionText(panel.transform, "CompletionSubtitle",
            "내가 판 물건이 마을의 다음 풍경을 만들기 시작했습니다.",
            new Vector2(0f, 230f), new Vector2(800f, 42f), 20f,
            new Color(0.32f, 0.28f, 0.20f, 1f), FontStyles.Normal);
        _completionSubtitle.alignment = TextAlignmentOptions.Center;

        _completionBody = CreateCompletionText(panel.transform, "CompletionBody", "",
            new Vector2(0f, 30f), new Vector2(780f, 340f), 21f,
            new Color(0.10f, 0.12f, 0.10f, 1f), FontStyles.Normal);
        _completionBody.alignment = TextAlignmentOptions.TopLeft;
        _completionBody.textWrappingMode = TextWrappingModes.Normal;
        _completionBody.overflowMode = TextOverflowModes.Ellipsis;

        _continueButton = CreateCompletionButton(panel.transform, "ContinueWeekTwoButton",
            "저장하고 2주차 계속", new Vector2(-205f, -225f),
            new Color(0.10f, 0.43f, 0.27f, 1f), SaveAndContinueWeekTwo);
        _continueButtonLabel = _continueButton.GetComponentInChildren<TextMeshProUGUI>();
        _quitButton = CreateCompletionButton(panel.transform, "SaveQuitWeekOneButton",
            "저장 후 종료", new Vector2(205f, -225f),
            new Color(0.48f, 0.27f, 0.20f, 1f), SaveAndQuitAfterWeekOne);
        _quitButtonLabel = _quitButton.GetComponentInChildren<TextMeshProUGUI>();

        _completionStatus = CreateCompletionText(panel.transform, "CompletionStatus", "",
            new Vector2(0f, -300f), new Vector2(790f, 50f), 17f,
            new Color(0.18f, 0.38f, 0.25f, 1f), FontStyles.Normal);
        _completionStatus.alignment = TextAlignmentOptions.Center;

        canvasGo.SetActive(false);
    }

    Button CreateCompletionButton(Transform parent, string name, string label,
        Vector2 position, Color background, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(350f, 62f);
        go.GetComponent<Image>().color = background;

        var button = go.GetComponent<Button>();
        button.onClick.AddListener(onClick);
        var text = CreateCompletionText(go.transform, "Label", label, Vector2.zero,
            new Vector2(330f, 50f), 20f, Color.white, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    TextMeshProUGUI CreateCompletionText(Transform parent, string name, string value,
        Vector2 position, Vector2 size, float fontSize, Color color, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    TextMeshProUGUI CreateText(Transform parent, string name, Vector2 topLeftOffset, Vector2 size, float fontSize, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = topLeftOffset;
        rt.sizeDelta = size;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }
}
