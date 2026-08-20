#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PA_Beta008SevenDayProgressionValidator
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.BETA008.Active";
    const string FailedKey = "PA.BETA008.Failed";
    const string ConsoleErrorKey = "PA.BETA008.ConsoleErrors";
    const string FrameKey = "PA.BETA008.Frames";
    const string StageKey = "PA.BETA008.Stage";
    const string DayKey = "PA.BETA008.Day";
    const float FastClockSecondsPerHour = 0.02f;
    const float HeldClockSecondsPerHour = 99999f;

    static WorldAlphaPlayableController _alpha;
    static WorldGameplayAdapterService _adapter;
    static LongPlayProgressionController _progression;
    static DayNightShopLoopController _dayLoop;
    static GameClock _clock;
    static float _stageStarted;
    static int _salesBeforeTourist;

    static PA_Beta008SevenDayProgressionValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        Subscribe();
        if (EditorApplication.isPlaying)
        {
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
    }

    [MenuItem("Project PA/Beta/BETA-008/Validate Seven Day Progression")]
    public static void RunBeta008Validation() => RunInternal();

    public static void RunBeta008ValidationBatch() => RunInternal();

    static void RunInternal()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetInt(ConsoleErrorKey, 0);
            SessionState.SetInt(DayKey, 1);
            SetStage(0);
            Subscribe();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && !scene.isDirty,
                "WorldSandbox opens saved and clean");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }

    static void Subscribe()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        Application.logMessageReceived += OnLogMessage;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetInt(DayKey, 1);
            SetStage(0);
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            Finish();
        }
    }

    static void ValidateRuntime()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
        int frames = SessionState.GetInt(FrameKey, 0) + 1;
        SessionState.SetInt(FrameKey, frames);
        int stage = SessionState.GetInt(StageKey, 0);
        int day = SessionState.GetInt(DayKey, 1);

        try
        {
            ResolveRuntime();
            bool ready = _alpha != null && _alpha.IsReady && _adapter != null &&
                         _adapter.IsReady && _adapter.ProductionFacilitiesBound &&
                         _progression != null && _dayLoop != null && _clock != null &&
                         EconomyService.Instance != null && SalesLogManager.Instance != null &&
                         HiringService.Instance != null && CustomerArrivalController.Instance != null;
            if (!ready && frames < 1200) return;
            if (!ready)
                throw new TimeoutException("WorldSandbox week-one authorities did not initialize.");

            if (stage == 0)
            {
                Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
                    $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
                Require(_clock.CurrentDay == 1 && _clock.CurrentHour >= 8.9f &&
                        _clock.CurrentHour < 9.2f && EconomyService.Instance.Money == 0 &&
                        EconomyService.Instance.CumulativeRevenue == 0 &&
                        HiringService.Instance.HiredCount == 0 &&
                        SalesLogManager.Instance.GetRecent(100).Count == 0,
                    "fresh WorldSandbox starts at Day 1 09:00 with zero money, revenue, hires and sales");
                Require(_alpha.BeginNewGame() && _alpha.PlayerFacingHudVisible &&
                        Mathf.Approximately(_clock.secondsPerGameHour,
                            WorldGameplayAdapterService.PlayableSecondsPerGameHour),
                    "New Game starts the real player HUD and playable week clock");
                Rect hud = _alpha.PlayerFacingHudScreenRect;
                Require(hud.width > 0f && hud.height > 0f && hud.xMin >= 0f && hud.yMin >= 0f &&
                        hud.xMax <= Screen.width && hud.yMax <= Screen.height &&
                        _progression.CurrentWeekOnePlayerSummary.Contains("Day 1"),
                    "the on-screen HUD exposes the current week-one plan inside Game View bounds");
                Require(_adapter.ShopSignTarget != null && _adapter.RuntimeShop != null &&
                        FlatDistance(_adapter.ShopSignTarget.position,
                            _adapter.RuntimeShop.transform.position) <= 8f,
                    "the existing interactive shop sign is bound beside generated B01");
                SetStage(1);
                return;
            }

            if (stage == 1)
            {
                Require(_clock.CurrentDay == day &&
                        _dayLoop.CurrentPhase == PADayNightPhase.DayPreparation,
                    $"Day {day} begins through the actual preparation phase");

                if (day >= 2)
                {
                    int moneyBefore = EconomyService.Instance.Money;
                    Require(_progression.TryPurchaseCurrentDaySupply(out string supplyResult) &&
                            _progression.CurrentDaySupplyPurchased &&
                            EconomyService.Instance.Money < moneyBefore,
                        $"Day {day} player-accepted producer delivery spends real money ({supplyResult})");
                }

                int gathered = GatherAllAvailableResources();
                int crafted = CraftHighestValueProducts();
                Require(gathered >= 20 && crafted >= 1,
                    $"Day {day} uses generated gathering and CraftingService ({gathered} gathered, {crafted} crafts)");
                Require(StockFourHighestValueItems() == 4,
                    $"Day {day} stocks four real B01 ShopSlots from Inventory");

                if (day == 5 && HiringService.Instance.HiredCount == 0)
                {
                    NpcCandidateData candidate = Resources.Load<NpcCandidateData>(
                        "Candidates/Candidate_Farmer");
                    Require(candidate != null && EconomyService.Instance.Money >= candidate.hireCost,
                        "earned week-one cash can afford the first Farmer hire");
                    Require(HiringService.Instance.TryHire(candidate, out GameObject hired,
                                out string hireReason) && hired != null &&
                            HiringService.Instance.HiredCount == 1,
                        $"Day 5 spends earned money through the actual HiringService ({hireReason})");
                }

                _clock.secondsPerGameHour = FastClockSecondsPerHour;
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(2);
                return;
            }

            if (stage == 2)
            {
                if (_dayLoop.CurrentPhase != PADayNightPhase.ShopOpen)
                {
                    if (Time.realtimeSinceStartup - _stageStarted < 8f) return;
                    throw new TimeoutException($"Day {day} did not reach ShopOpen through GameClock.Update.");
                }

                _clock.secondsPerGameHour = HeldClockSecondsPerHour;
                Require(_dayLoop.TryOpenShop() && _dayLoop.IsShopOpenForCustomers,
                    $"Day {day} opens through the real shop-sign authority");
                if (day == 1)
                {
                    foreach (ShopSlot slot in _adapter.RuntimeShopSlots.Where(slot => slot != null && !slot.IsEmpty))
                    {
                        slot.displayPrice = 1;
                        slot.RefreshDisplay();
                    }
                    _salesBeforeTourist = SalesLogManager.Instance.GetRecent(100).Count;
                    _stageStarted = Time.realtimeSinceStartup;
                    SetStage(3);
                    return;
                }

                SellAllStockThroughAuthority(day);
                Require(_progression.CurrentDayGoalComplete,
                    $"Day {day} reaches its authoritative revenue/supply/growth goal");
                _clock.secondsPerGameHour = FastClockSecondsPerHour;
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(4);
                return;
            }

            if (stage == 3)
            {
                int sales = SalesLogManager.Instance.GetRecent(100).Count;
                if (sales <= _salesBeforeTourist &&
                    Time.realtimeSinceStartup - _stageStarted < 24f)
                    return;
                Require(sales > _salesBeforeTourist,
                    "the ordinary Day 1 tourist completes a real ShopSlot/PurchaseEvaluator sale");
                Require(CustomerArrivalController.Instance.TouristsSpawnedThisOpening > 0,
                    "the normal arrival timer sourced an external tourist without a hired resident");

                foreach (ShopSlot slot in _adapter.RuntimeShopSlots.Where(slot => slot != null && !slot.IsEmpty))
                {
                    slot.displayPrice = slot.currentItem.data.basePrice;
                    slot.RefreshDisplay();
                }
                SellAllStockThroughAuthority(day);
                Require(EconomyService.Instance.Money > 0 &&
                        EconomyService.Instance.CumulativeRevenue >= 150 &&
                        _progression.CurrentDayGoalComplete,
                    "Day 1 earns its first money and completes the 150G operation goal");
                _clock.secondsPerGameHour = FastClockSecondsPerHour;
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(4);
                return;
            }

            if (stage == 4)
            {
                if (_dayLoop.CurrentPhase != PADayNightPhase.Settlement)
                {
                    if (Time.realtimeSinceStartup - _stageStarted < 8f) return;
                    throw new TimeoutException($"Day {day} did not reach Settlement through GameClock.Update.");
                }

                _clock.secondsPerGameHour = HeldClockSecondsPerHour;
                Require(_progression.CurrentDayGoalComplete,
                    $"Day {day} settlement preserves the completed daily objective");
                if (day < 7)
                {
                    Require(_dayLoop.TryStartNextDay() && _clock.CurrentDay == day + 1,
                        $"Day {day} settlement starts exactly Day {day + 1} through the real authority");
                    SessionState.SetInt(DayKey, day + 1);
                    SetStage(1);
                    return;
                }

                Require(_progression.WeekOneCompletionRequirementsMet &&
                        EconomyService.Instance.CumulativeRevenue >= 1700 &&
                        HiringService.Instance.HiredCount >= 1 &&
                        VillageCultureVisualController.Instance != null &&
                        (VillageCultureVisualController.Instance.HasActiveCategory ||
                         VillageCultureVisualController.Instance.HasPendingChange),
                    "Day 7 completion requires 1,700G, a paid hire and an earned village response");
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(5);
                return;
            }

            if (stage == 5)
            {
                if (!_progression.IsWeekCompletionOpen &&
                    Time.realtimeSinceStartup - _stageStarted < 4f)
                    return;
                Require(_progression.IsWeekCompletionOpen,
                    "the earned Day 7 Settlement opens the Week 1 completion choice");
                Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                    "blocking runtime Console Error/Exception/Assert count is 0");
                Require(!SceneManager.GetActiveScene().isDirty,
                    "BETA-008 remains runtime-only and leaves WorldSandbox scene clean");
                Debug.Log("[BETA-008] PLAY_MODE_PASS days=7 clock=true tourist=true " +
                          "supply=true crafting=true revenue=1700 hire=true village=true " +
                          "completion=true console=0");
                EditorApplication.update -= ValidateRuntime;
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception exception)
        {
            EditorApplication.update -= ValidateRuntime;
            Fail(exception);
        }
    }

    static int GatherAllAvailableResources()
    {
        int gathered = 0;
        foreach (WorldResourceKind kind in Enum.GetValues(typeof(WorldResourceKind)))
        {
            int guard = 0;
            while (guard++ < 32 && _adapter.TryGatherNext(kind, out _, out _))
                gathered++;
        }
        return gathered;
    }

    static int CraftHighestValueProducts()
    {
        int crafted = 0;
        crafted += CraftWhile("Recipes/Recipe_Plank", _adapter.RuntimeWorkbench, 8);
        crafted += CraftWhile("Recipes/Recipe_Furniture", _adapter.RuntimeWorkbench, 2);
        crafted += CraftWhile("Recipes/Recipe_IronBar", _adapter.RuntimeForge, 8);
        crafted += CraftWhile("Recipes/Recipe_ToolSet", _adapter.RuntimeForge, 2);
        crafted += CraftWhile("Recipes/Recipe_GrilledFish", _adapter.RuntimeKitchen, 8);
        crafted += CraftWhile("Recipes/Recipe_BakedPotato", _adapter.RuntimeKitchen, 8);
        return crafted;
    }

    static int CraftWhile(string resourcePath, Workbench workbench, int limit)
    {
        RecipeData recipe = Resources.Load<RecipeData>(resourcePath);
        if (recipe == null || workbench == null) return 0;
        int crafted = 0;
        while (crafted < limit && CraftingService.TryCraft(recipe, workbench))
            crafted++;
        return crafted;
    }

    static int StockFourHighestValueItems()
    {
        Inventory inventory = _adapter.PlayerInventory;
        InventorySlot hand = inventory.hotbar.GetSlot(0);
        hand.Clear();
        inventory.selectedHotbarIndex = 0;
        int stocked = 0;
        var selectedItems = new HashSet<Item>();

        foreach (ShopSlot display in _adapter.RuntimeShopSlots.Where(slot => slot != null).Take(4))
        {
            Require(display.IsEmpty, $"{display.name} is empty before Day {SessionState.GetInt(DayKey, 1)} stocking");
            InventorySlot source = inventory.slots
                .Where(slot => slot != null && !slot.IsEmpty && IsSellable(slot.item) &&
                               !selectedItems.Contains(slot.item))
                .OrderByDescending(slot => slot.item.basePrice)
                .ThenBy(slot => slot.item.itemName, StringComparer.Ordinal)
                .FirstOrDefault();
            if (source == null) break;

            var selected = new ItemInstance(source.item, 1)
            {
                quality = source.instance.quality,
                currentPrice = source.instance.currentPrice
            };
            source.AddCount(-1);
            hand.SetInstance(selected);
            display.Interact(_adapter.PlayerRoot);
            if (display.IsEmpty) break;
            display.displayPrice = display.currentItem.data.basePrice;
            display.RefreshDisplay();
            selectedItems.Add(display.currentItem.data);
            stocked++;
        }
        inventory.RefreshAllUI();
        return stocked;
    }

    static bool IsSellable(Item item)
    {
        return item != null && item.category != ItemCategory.Tool && item.toolType == ToolType.None &&
               (TierService.Instance == null || TierService.Instance.IsUnlocked(item.requiredTier));
    }

    static void SellAllStockThroughAuthority(int day)
    {
        int before = SalesLogManager.Instance.GetRecent(100).Count;
        int expectedSales = 0;
        foreach (ShopSlot slot in _adapter.RuntimeShopSlots.Where(slot => slot != null && !slot.IsEmpty))
        {
            slot.displayPrice = slot.currentItem.data.basePrice;
            slot.RefreshDisplay();
            Require(slot.TryPurchaseByNpc($"BETA008_DAY_{day}_CUSTOMER_{expectedSales}",
                        out int paid) && paid > 0,
                $"Day {day} ShopSlot commits sale {expectedSales + 1} through economy and sales-log authorities");
            expectedSales++;
        }
        Require(expectedSales > 0 &&
                SalesLogManager.Instance.GetRecent(100).Count == before + expectedSales,
            $"Day {day} records every committed sale in SalesLog");
    }

    static void ResolveRuntime()
    {
        _alpha = WorldAlphaPlayableController.Instance ??
                 UnityEngine.Object.FindFirstObjectByType<WorldAlphaPlayableController>();
        _adapter = WorldGameplayAdapterService.Instance ??
                   UnityEngine.Object.FindFirstObjectByType<WorldGameplayAdapterService>();
        _progression = LongPlayProgressionController.Instance ??
                       UnityEngine.Object.FindFirstObjectByType<LongPlayProgressionController>();
        _dayLoop = DayNightShopLoopController.Instance ??
                   UnityEngine.Object.FindFirstObjectByType<DayNightShopLoopController>();
        _clock = GameClock.Instance ?? UnityEngine.Object.FindFirstObjectByType<GameClock>();
    }

    static float FlatDistance(Vector3 left, Vector3 right)
    {
        left.y = 0f;
        right.y = 0f;
        return Vector3.Distance(left, right);
    }

    static void SetStage(int stage)
    {
        SessionState.SetInt(StageKey, stage);
        SessionState.SetInt(FrameKey, 0);
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(ActiveKey, false) ||
            (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
        SessionState.SetInt(ConsoleErrorKey, SessionState.GetInt(ConsoleErrorKey, 0) + 1);
    }

    static void Fail(Exception exception)
    {
        SessionState.SetBool(FailedKey, true);
        Debug.LogError($"[BETA-008] FAIL {exception.Message}\n{exception}");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else Finish();
    }

    static void Finish()
    {
        EditorApplication.update -= ValidateRuntime;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        bool failed = SessionState.GetBool(FailedKey, false) ||
                      SessionState.GetInt(ConsoleErrorKey, 0) != 0;
        int errors = SessionState.GetInt(ConsoleErrorKey, 0);
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(FailedKey);
        SessionState.EraseInt(ConsoleErrorKey);
        SessionState.EraseInt(FrameKey);
        SessionState.EraseInt(StageKey);
        SessionState.EraseInt(DayKey);
        Debug.Log(failed
            ? $"[BETA-008] FINISHED_WITH_ERRORS consoleErrors={errors}"
            : "[BETA-008] FINISHED_PASS days=7 clock=true tourist=true supply=true " +
              "crafting=true revenue=1700 hire=true village=true completion=true console=0");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[BETA-008] PASS {message}");
    }
}
#endif
