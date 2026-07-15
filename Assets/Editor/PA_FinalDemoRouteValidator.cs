using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PA_FinalDemoRouteValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.FinalDemoRoute.Active";
    const string EnteredKey = "PA.FinalDemoRoute.Entered";
    const string RanKey = "PA.FinalDemoRoute.Ran";
    const string HadErrorKey = "PA.FinalDemoRoute.HadError";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;

    static PA_FinalDemoRouteValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;

        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _hadError = SessionState.GetBool(HadErrorKey, false);
        RegisterCallbacks();

        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Run Final Demo Route Validation")]
    public static void RunFinalDemoRouteValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA Final Route: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        _entered = false;
        _ran = false;
        _hadError = false;
        _startedAt = EditorApplication.timeSinceStartup;

        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(HadErrorKey, false);

        RegisterCallbacks();

        Debug.Log("PA Final Route: entering Play Mode.");
        EditorApplication.EnterPlaymode();
    }

    static void RegisterCallbacks()
    {
        Application.logMessageReceived -= OnLogMessage;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;

        Application.logMessageReceived += OnLogMessage;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            _hadError = true;
            SessionState.SetBool(HadErrorKey, true);
        }
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _entered = true;
            _startedAt = EditorApplication.timeSinceStartup;
            SessionState.SetBool(EnteredKey, true);
        }

        if (state == PlayModeStateChange.EnteredEditMode
            && SessionState.GetBool(ActiveKey, false)
            && SessionState.GetBool(RanKey, false))
        {
            Finish();
        }
    }

    static void OnEditorUpdate()
    {
        double elapsed = EditorApplication.timeSinceStartup - _startedAt;

        if (!_entered)
        {
            if (elapsed > 60.0)
            {
                Debug.LogError("PA Final Route: timed out before entering Play Mode.");
                MarkFailedAndExit();
            }
            return;
        }

        if (!_ran && EditorApplication.isPlaying && elapsed > 2.5)
        {
            _ran = true;
            SessionState.SetBool(RanKey, true);
            RunRuntimeChecks();
            EditorApplication.ExitPlaymode();
            return;
        }

        if (_entered && !_ran && elapsed > 90.0)
        {
            Debug.LogError("PA Final Route: timed out before runtime checks.");
            MarkFailedAndExit();
        }
    }

    static void RunRuntimeChecks()
    {
        try
        {
            Time.timeScale = 1f;

            var player = RequireOne<PlayerController>("player movement controller");
            Require(PlayerInputHandler.Instance != null, "PlayerInputHandler exists");
            Require(player.GetComponent<CharacterController>() != null, "player has CharacterController");

            var scenario = RequireOne<PlayableDayScenarioController>("PlayableDayScenarioController");
            scenario.RestoreSavedSession("하늘", "green_bay", 0);

            var dialogue = FindObject<NpcDialogue>("first NPC dialogue");
            dialogue.Interact(player.gameObject);
            Require(DialogueUI.instance != null && DialogueUI.IsOpen, "first NPC dialogue opens DialogueUI");

            var slot = FindEmptyShopSlot();
            var stockedItem = FindSellableItem();
            SeedHotbarWith(stockedItem);
            slot.Interact(player.gameObject);
            Require(!slot.IsEmpty, "shop slot can stock a sellable item from hotbar");

            int basePrice = Mathf.Max(1, stockedItem.basePrice);
            Require(slot.EffectiveDisplayPrice == basePrice, "stocked slot starts at base price");

            var priceUi = RequireOne<ShopPriceUI>("ShopPriceUI");
            slot.Interact(player.gameObject);
            Require(priceUi.IsOpen, "interacting with stocked slot opens ShopPriceUI");
            int confirmBefore = priceUi.ConfirmCount;
            InvokePrivate(priceUi, "OnConfirm");
            Require(priceUi.ConfirmCount == confirmBefore + 1, "ShopPriceUI price confirmation increments ConfirmCount");
            Require(!priceUi.IsOpen, "ShopPriceUI closes after price confirmation");

            var npc = FindObject<NpcController>("customer NPC");
            var purchaseResult = PurchaseEvaluator.Evaluate(npc.profile, slot, new System.Random(7));
            string feedback = (string)InvokePrivate(npc, "BuildPurchaseFeedback", purchaseResult, slot);
            Require(!string.IsNullOrWhiteSpace(feedback), "NPC purchase/rejection feedback text generated");
            Require(feedback.Length <= 34, $"NPC feedback is compact enough for bubble ({feedback.Length} chars)");
            InvokePrivate(npc, "ShowBubbleMessage", feedback);
            InvokePrivate(npc, "RecordScenarioFeedback", feedback);
            var bubble = npc.GetComponentInChildren<NpcBubbleUI>(true);
            Require(bubble != null && bubble.gameObject.activeInHierarchy, "NPC feedback bubble becomes visible");
            Require(bubble.bubbleText != null && bubble.bubbleText.text == feedback, "NPC feedback bubble shows generated text");

            int moneyBefore = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;
            long revenueBefore = EconomyService.Instance != null ? EconomyService.Instance.CumulativeRevenue : 0L;
            int expectedPaid = slot.EffectiveDisplayPrice * slot.currentItem.count;
            Require(slot.TryPurchaseByNpc("RouteValidator", out int paid), "ShopSlot.TryPurchaseByNpc succeeds");
            Require(paid == expectedPaid, "purchase paid amount matches display price");
            Require(slot.IsEmpty, "shop slot is empty after NPC purchase");
            Require(EconomyService.Instance != null && EconomyService.Instance.Money == moneyBefore + paid, "money HUD source changes after sale");
            Require(EconomyService.Instance.CumulativeRevenue == revenueBefore + paid, "cumulative revenue changes after sale");

            ValidateMoneyHud();
            ValidateAuditPanel();
            ValidateOverlayLayout();
            ValidateScenarioSummary(scenario);
            ValidateSceneMarkers();

            Debug.Log($"PA Final Route Validation passed. stocked={stockedItem.itemName}, paid={paid}G, feedback='{feedback}'");
        }
        catch (Exception ex)
        {
            _hadError = true;
            SessionState.SetBool(HadErrorKey, true);
            Debug.LogError($"PA Final Route Validation failed: {ex.Message}\n{ex}");
        }
    }

    static void ValidateMoneyHud()
    {
        var hud = RequireOne<MoneyHUD>("MoneyHUD");
        Require(hud.moneyText != null && hud.moneyText.text.Contains("G"), "MoneyHUD shows money text");
        Require(hud.tierText != null && hud.tierText.text.Contains("Tier"), "MoneyHUD shows tier text");
        Require(hud.tierGoalText != null && hud.tierGoalText.text.Contains("다음:"), "MoneyHUD shows next-tier goal text");
    }

    static void ValidateAuditPanel()
    {
        var audit = FindObject<AuditResultUI>("AuditResultUI", includeInactive: true);
        var onEnable = typeof(AuditResultUI).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
        onEnable?.Invoke(audit, null);
        audit.Refresh();
        Require(audit.nextTierText != null && audit.nextTierText.text.Contains("다음:"), "audit app shows next-tier goal text");
    }

    static void ValidateScenarioSummary(PlayableDayScenarioController scenario)
    {
        scenario.RestoreSavedSession("하늘", "green_bay", (int)PlayableDayScenarioController.Stage.Done);
        InvokePrivate(scenario, "ShowDaySummary");

        var title = GetPrivateField<TextMeshProUGUI>(scenario, "_flowTitle");
        var body = GetPrivateField<TextMeshProUGUI>(scenario, "_flowBody");
        Require(title != null && title.text.Contains("Day 1"), "Day 1 summary title is visible");
        Require(body != null && body.text.Contains("구매/거절 피드백"), "Day 1 summary includes purchase feedback section");
        Require(body.text.Contains("Village direction"), "Day 1 summary includes village direction section");
        Require(body.text.Contains("다음 성장 목표"), "Day 1 summary includes next growth goal");
        Require(body.text.Contains("누적 매출"), "Day 1 summary includes tier revenue progress");

        int closingDay = GameClock.Instance.CurrentDay;
        InvokePrivate(scenario, "OnPrimaryPressed");
        Require(GameClock.Instance.CurrentDay == closingDay + 1, "Day 1 summary button starts Day 2");
        Require(DayNightShopLoopController.Instance.CurrentPhase == PADayNightPhase.DayPreparation,
            "Day 2 begins in day preparation phase");
        Require(scenario.objectiveText != null && scenario.objectiveText.text.Contains("Day 2"),
            "player-facing objective advances from Day 1 to Day 2");
    }

    static void ValidateSceneMarkers()
    {
        Require(CountTransforms("PA_MarketStall_Hub_Visual") > 0, "market hub visual exists");
        Require(CountTransforms("PA_DemoRoute_VisualMarkers") > 0, "demo route markers exist");
        Require(CountTransforms("PA_EconomicRoleBadge") > 0, "NPC role badges exist");
        Require(CountTransforms("PA_ScreenshotCameraMarker_MarketHub") > 0, "screenshot marker exists");
    }

    static void ValidateOverlayLayout()
    {
        var moneyPanel = GameObject.Find("MoneyHudPanel")?.GetComponent<RectTransform>();
        var objectiveBg = GameObject.Find("PlayableDayGuideCanvas")?.transform.Find("Bg")?.GetComponent<RectTransform>();

        if (moneyPanel != null && objectiveBg != null)
            Require(!ScreenRectsOverlap(moneyPanel, objectiveBg), "MoneyHUD does not overlap objective panel");
    }

    static bool ScreenRectsOverlap(RectTransform a, RectTransform b)
    {
        Rect ra = GetScreenRect(a);
        Rect rb = GetScreenRect(b);
        return ra.Overlaps(rb);
    }

    static Rect GetScreenRect(RectTransform rt)
    {
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float minX = corners[0].x;
        float maxX = corners[0].x;
        float minY = corners[0].y;
        float maxY = corners[0].y;

        for (int i = 1; i < corners.Length; i++)
        {
            minX = Mathf.Min(minX, corners[i].x);
            maxX = Mathf.Max(maxX, corners[i].x);
            minY = Mathf.Min(minY, corners[i].y);
            maxY = Mathf.Max(maxY, corners[i].y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    static ShopSlot FindEmptyShopSlot()
    {
        foreach (var slot in Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
        {
            if (slot != null && slot.gameObject.activeInHierarchy && slot.IsEmpty)
                return slot;
        }

        throw new InvalidOperationException("No empty active ShopSlot found.");
    }

    static Item FindSellableItem()
    {
        Item fallback = Resources.Load<Item>("Items/Item_BreadLoaf");
        if (IsSellable(fallback)) return fallback;

        foreach (var item in Resources.LoadAll<Item>("Items"))
            if (IsSellable(item)) return item;

        throw new InvalidOperationException("No sellable item found under Resources/Items.");
    }

    static bool IsSellable(Item item)
    {
        return item != null
            && item.category != ItemCategory.Tool
            && item.toolType == ToolType.None
            && (TierService.Instance == null || TierService.Instance.IsUnlocked(item.requiredTier));
    }

    static void SeedHotbarWith(Item item)
    {
        if (Inventory.instance == null)
            throw new InvalidOperationException("Inventory.instance missing.");

        if (Inventory.instance.hotbar == null)
            throw new InvalidOperationException("Inventory.hotbar missing.");

        Inventory.instance.selectedHotbarIndex = 0;
        var slot = Inventory.instance.hotbar.GetSlot(0);
        if (slot == null)
            throw new InvalidOperationException("Hotbar slot 1 missing.");

        slot.SetInstance(new ItemInstance(item, 1)
        {
            quality = 1f,
            currentPrice = item.basePrice
        });
        Inventory.instance.RefreshAllUI();
    }

    static T RequireOne<T>(string label) where T : Object
    {
        return FindObject<T>(label);
    }

    static T FindObject<T>(string label, bool includeInactive = false) where T : Object
    {
        T[] objects = includeInactive
            ? Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            : Object.FindObjectsByType<T>(FindObjectsSortMode.None);

        foreach (var obj in objects)
            if (obj != null) return obj;

        throw new InvalidOperationException($"{label} not found.");
    }

    static int CountTransforms(string name)
    {
        int count = 0;
        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (transform != null && transform.name == name)
                count++;
        return count;
    }

    static object InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
            throw new MissingMethodException(target.GetType().Name, methodName);
        return method.Invoke(target, args);
    }

    static T GetPrivateField<T>(object target, string fieldName) where T : class
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(target) as T;
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        Debug.Log($"PA Final Route Check OK: {message}");
    }

    static void MarkFailedAndExit()
    {
        _hadError = true;
        SessionState.SetBool(HadErrorKey, true);
        Cleanup();
        EditorApplication.Exit(1);
    }

    static void Finish()
    {
        bool hadError = _hadError || SessionState.GetBool(HadErrorKey, false);
        bool entered = _entered || SessionState.GetBool(EnteredKey, false);
        bool ran = _ran || SessionState.GetBool(RanKey, false);

        Cleanup();
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(EnteredKey);
        SessionState.EraseBool(RanKey);
        SessionState.EraseBool(HadErrorKey);

        if (!entered || !ran || hadError)
        {
            Debug.LogError("PA Final Route Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA Final Route Validation finished successfully.");
        EditorApplication.Exit(0);
    }

    static void Cleanup()
    {
        Application.logMessageReceived -= OnLogMessage;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
    }
}
