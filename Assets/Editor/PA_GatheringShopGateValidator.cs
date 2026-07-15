#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// IL-001 + CDN-002 검증 — 실제 낮 채집과 밤 영업 게이트의 엔드 투 엔드 루프.
//
// 점검:
// - 채집 포인트 >= 3, DayPreparation 에서만 채집, 유효 sellable 아이템 추가, 같은 날 중복 불가, 다음날 재활성화.
// - 채집 아이템을 ShopSlot 에 진열 + 가격 설정 가능.
// - 일반 DayPreparation 에서는 손님 구매 게이트가 닫혀 있고, ShopOpen + 영업 시작 시 열린다.
// - Day 1 튜토리얼은 항상 열림으로 첫 판매 루트 보존.
// - Village Direction 유지, 당일 채집 상태 save/load 라운드트립.
//
// 기존 경제/구매/NPC/상점 로직은 변경하지 않고, 컨트롤러의 공개 API 만 호출한다.
[InitializeOnLoad]
public static class PA_GatheringShopGateValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.GatheringShopGate.Active";
    const string EnteredKey = "PA.GatheringShopGate.Entered";
    const string RanKey = "PA.GatheringShopGate.Ran";
    const string HadErrorKey = "PA.GatheringShopGate.HadError";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;

    static PA_GatheringShopGateValidator()
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

    [MenuItem("Project PA/Validation/Run Gathering + Shop Gate Validation")]
    public static void RunGatheringShopGateValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA GatheringShopGate: failed to open scene at {ScenePath}");
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

        Debug.Log("PA GatheringShopGate: entering Play Mode.");
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
                Debug.LogError("PA GatheringShopGate: timed out before entering Play Mode.");
                MarkFailedAndExit();
            }
            return;
        }

        if (!_ran && EditorApplication.isPlaying && elapsed > 3.0)
        {
            _ran = true;
            SessionState.SetBool(RanKey, true);
            RunRuntimeChecks();
            EditorApplication.ExitPlaymode();
            return;
        }

        if (_entered && !_ran && elapsed > 100.0)
        {
            Debug.LogError("PA GatheringShopGate: timed out before runtime checks.");
            MarkFailedAndExit();
        }
    }

    static void RunRuntimeChecks()
    {
        try
        {
            Time.timeScale = 1f;

            var loop = RequireOne<DayNightShopLoopController>("DayNightShopLoopController");
            Require(GameClock.Instance != null, "GameClock exists");
            Require(Inventory.instance != null, "Inventory exists");

            // 1) 채집 포인트 >= 3.
            var points = Object.FindObjectsByType<DaytimeStockPrepPoint>(FindObjectsSortMode.None);
            Require(points.Length >= 3, $"at least 3 daytime gather points exist ({points.Length})");

            var shore = FindPoint(points, "shore-forage");
            var forest = FindPoint(points, "forest-forage");
            Require(shore != null && forest != null, "spread forage points (shore/forest) exist");
            var fishing = shore.GetComponentInChildren<FishingSpot>(true);
            Require(fishing != null, "shore point exposes a dedicated FishingSpot interaction");
            Require(fishing.GetInteractPrompt().Contains("낚싯대"), "shore prompt offers casting a fishing rod");

            // 2) Day 2 DayPreparation: 채집 가능 상태 + 손님 게이트 닫힘.
            loop.SimulatePhaseForValidation(8f, 2);
            loop.ResetDayPrepForValidation();
            Require(loop.CurrentPhase == PADayNightPhase.DayPreparation, "Day 2 is in DayPreparation phase");
            Require(!loop.IsShopOpenForCustomers, "Day 2 daytime: customer purchases are gated off");
            Require(loop.IsDayPrepPointAvailable(shore), "shore forage point is available in DayPreparation");

            // 3) 채집 시 유효 sellable ItemInstance 가 인벤토리에 추가된다.
            ClearRuntimeInventory();
            int before = CountSellableItems();
            var fishingPlayer = PlayerObject();
            Require(fishingPlayer != null, "player exists for fishing interaction");
            fishing.Interact(fishingPlayer);
            Require(fishing.IsFishing, "fishing interaction enters the cast-and-wait state");
            Require(fishing.LastFeedback.Contains("기다리는 중"), "casting gives immediate wait feedback");
            bool collected = fishing.CompleteCatchForValidation(fishingPlayer);
            int after = CountSellableItems();
            Require(collected, "finishing the fishing action catches stock");
            Require(fishing.LastFeedback.Contains("물고기"), "successful catch gives player feedback");
            Require(after > before, $"fishing adds sellable items to inventory ({before} -> {after})");

            var shoreItem = Resources.Load<Item>("Items/Item_Fish");
            Require(shoreItem != null && IsSellable(shoreItem), "forage item (Fish) is a valid sellable item");
            Require(Inventory.instance.HasItems(shoreItem, 1), "gathered item is present in inventory");
            int caughtFishCount = CountInventoryItem(shoreItem);
            Require(caughtFishCount == shore.grantCount,
                $"fishing grants the configured Fish count ({caughtFishCount})");

            // 4) 같은 날 중복 채집 불가.
            fishing.Interact(fishingPlayer);
            Require(!fishing.IsFishing, "same-day repeat fishing is blocked before casting");

            // 5) DayPreparation 외(ShopOpen)에서는 채집 불가.
            loop.SimulatePhaseForValidation(20f, 2);
            bool gatherWhileOpen = loop.TryCollectDayPrepStock(forest, PlayerObject());
            Require(!gatherWhileOpen, "gathering is blocked outside DayPreparation (ShopOpen)");

            // 6) 다음날 채집 포인트 재활성화.
            loop.SimulatePhaseForValidation(8f, 3);
            Require(loop.IsDayPrepPointAvailable(shore), "forage point reactivates on the next day");

            // 7) 채집 아이템을 ShopSlot 에 진열 + 가격 설정 가능.
            var slot = FindEmptyShopSlot();
            var player = PlayerObject();
            slot.Interact(player);
            Require(!slot.IsEmpty, "gathered item can be stocked into a ShopSlot");
            Require(slot.currentItem != null && slot.currentItem.data == shoreItem, "stocked slot holds the gathered item");
            Require(CountInventoryItem(shoreItem) == caughtFishCount - 1,
                "stocking consumes one Fish from the actual fishing inventory");
            Require(slot.EffectiveDisplayPrice == shoreItem.basePrice, "stocked slot starts at the item base price");

            var priceUi = RequireOne<ShopPriceUI>("ShopPriceUI");
            int confirmBefore = priceUi.ConfirmCount;
            slot.Interact(player); // 진열된 슬롯 재상호작용 -> 가격 UI
            Require(priceUi.IsOpen, "interacting with the stocked slot opens ShopPriceUI");
            InvokePrivate(priceUi, "OnConfirm");
            Require(priceUi.ConfirmCount == confirmBefore + 1, "price can be confirmed for the gathered item");

            // 8) 밤 영업 게이트: ShopOpen + 플레이어 영업 시작 시에만 손님 구매 허용.
            loop.SimulatePhaseForValidation(8f, 2);
            Require(!loop.IsShopOpenForCustomers, "Day 2 DayPreparation keeps customer purchases gated");
            loop.SimulatePhaseForValidation(20f, 2);
            loop.SetShopOpenedForValidation(false);
            Require(loop.CurrentPhase == PADayNightPhase.ShopOpen, "Day 2 evening reaches ShopOpen phase");
            Require(loop.CanPlayerOpenShop, "player can open the shop during ShopOpen");
            Require(!loop.IsShopOpenForCustomers, "customers wait until the player opens the shop");
            Require(loop.TryOpenShop(), "player opens the shop via the sign");
            Require(loop.IsShopOpenForCustomers, "after opening, customer purchases are allowed");

            // 9) 같은 낚시 재고를 실제 NPC 구매 진입점으로 판매해 돈/매출/판매 기록까지 닫는다.
            var customer = RequireOne<NpcController>("fishing flow customer NPC");
            string buyerName = customer.profile != null && !string.IsNullOrWhiteSpace(customer.profile.npcName)
                ? customer.profile.npcName
                : customer.gameObject.name;
            Require(!string.IsNullOrWhiteSpace(buyerName), "fishing sale uses a real customer NPC name");

            var economy = RequireOne<EconomyService>("EconomyService");
            var sales = RequireOne<SalesLogManager>("SalesLogManager");
            int moneyBeforeSale = economy.Money;
            long revenueBeforeSale = economy.CumulativeRevenue;
            int saleRecordsBefore = sales.GetRecent(100).Count;
            var decisionStatsBefore = sales.GetDailyDecisionStats(2);
            int expectedFishPayment = slot.EffectiveDisplayPrice * slot.currentItem.count;

            Require(slot.TryPurchaseByNpc(buyerName, out int fishPaid),
                "customer NPC purchase entry point buys the caught Fish");
            Require(fishPaid == expectedFishPayment && fishPaid == shoreItem.basePrice,
                $"caught Fish sale pays the confirmed price ({fishPaid}G)");
            Require(slot.IsEmpty, "Fish ShopSlot is empty after the customer purchase");
            Require(economy.Money == moneyBeforeSale + fishPaid,
                $"Fish sale increases player money ({moneyBeforeSale} -> {economy.Money}G)");
            Require(economy.CumulativeRevenue == revenueBeforeSale + fishPaid,
                "Fish sale increases cumulative revenue");

            var recentSales = sales.GetRecent(100);
            Require(recentSales.Count == saleRecordsBefore + 1,
                "Fish sale appends one SalesLog record");
            var fishSale = recentSales[0];
            Require(fishSale.itemName == shoreItem.itemName
                && fishSale.category == shoreItem.category.ToString()
                && fishSale.buyerName == buyerName,
                "SalesLog records the caught Fish, Raw category, and customer NPC");
            var decisionStatsAfter = sales.GetDailyDecisionStats(2);
            Require(decisionStatsAfter.purchases == decisionStatsBefore.purchases + 1,
                "successful Fish sale increments the daily purchase count");

            var moneyHud = RequireOne<MoneyHUD>("MoneyHUD");
            Require(moneyHud.moneyText != null && moneyHud.moneyText.text.Contains(economy.Money.ToString()),
                "MoneyHUD reflects the Fish sale balance");

            // 10) Day 1 튜토리얼 override: DayPreparation 단계에서도 항상 열림(첫 판매 루트 보존).
            loop.SimulatePhaseForValidation(8f, 1);
            Require(loop.IsTutorialAlwaysOpen, "Day 1 keeps the tutorial-always-open override");
            Require(loop.IsShopOpenForCustomers, "Day 1 tutorial customers can buy regardless of phase");

            // 11) Village Direction / 정산 신호 유지.
            Require(VillageChangeSignalController.Instance != null, "Village Direction signal controller is alive");

            // 12) 당일 채집 상태 save/load 라운드트립(v8).
            loop.SimulatePhaseForValidation(8f, 2);
            loop.ResetDayPrepForValidation();
            fishing.Interact(fishingPlayer);
            Require(fishing.IsFishing && fishing.CompleteCatchForValidation(fishingPlayer),
                "fish on Day 2 for save test");

            var data = new SaveData { gameDay = 2 };
            loop.WriteSaveFields(data);
            Require(data.dayPrepCollectedDay == 2, "save writes the day-prep collected day");
            Require(data.dayPrepCollectedActivities != null && data.dayPrepCollectedActivities.Contains("shore-forage"),
                "save writes the collected forage activity id");

            loop.ResetDayPrepForValidation();
            Require(loop.IsDayPrepPointAvailable(shore), "reset clears the collected state before load");

            loop.RestoreSavedState(data.dayPrepCollectedDay, data.dayPrepCollectedActivities);
            Require(!loop.IsDayPrepPointAvailable(shore), "load restores the same-day collected state");

            Debug.Log($"PA Gathering ShopGate Validation passed. gatherPoints={points.Length}, fishing=OK, FishSale={fishPaid}G, buyer={buyerName}, shopGate=OK");
        }
        catch (Exception ex)
        {
            _hadError = true;
            SessionState.SetBool(HadErrorKey, true);
            Debug.LogError($"PA Gathering ShopGate Validation failed: {ex.Message}\n{ex}");
        }
    }

    static DaytimeStockPrepPoint FindPoint(DaytimeStockPrepPoint[] points, string activityId)
    {
        foreach (var p in points)
            if (p != null && p.activityId == activityId) return p;
        return null;
    }

    static GameObject PlayerObject()
    {
        return GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
    }

    static void ClearRuntimeInventory()
    {
        foreach (var slot in Inventory.instance.slots)
            slot?.Clear();

        if (Inventory.instance.hotbar != null)
            foreach (var slot in Inventory.instance.hotbar.slots)
                slot?.Clear();

        Inventory.instance.RefreshAllUI();
    }

    static int CountSellableItems()
    {
        int count = 0;
        CountSellable(Inventory.instance.slots, ref count);
        if (Inventory.instance.hotbar != null)
            CountSellable(Inventory.instance.hotbar.slots, ref count);
        return count;
    }

    static void CountSellable(List<InventorySlot> slots, ref int count)
    {
        if (slots == null) return;
        foreach (var slot in slots)
        {
            if (slot == null || slot.IsEmpty || slot.item == null) continue;
            if (slot.item.category != ItemCategory.Tool && slot.item.toolType == ToolType.None)
                count += slot.count;
        }
    }

    static bool IsSellable(Item item)
    {
        return item != null && item.category != ItemCategory.Tool && item.toolType == ToolType.None;
    }

    static int CountInventoryItem(Item item)
    {
        if (item == null || Inventory.instance == null) return 0;

        int count = 0;
        CountItemInSlots(Inventory.instance.slots, item, ref count);
        if (Inventory.instance.hotbar != null)
            CountItemInSlots(Inventory.instance.hotbar.slots, item, ref count);
        return count;
    }

    static void CountItemInSlots(List<InventorySlot> slots, Item item, ref int count)
    {
        if (slots == null) return;
        foreach (var slot in slots)
            if (slot != null && !slot.IsEmpty && slot.item == item)
                count += slot.count;
    }

    static ShopSlot FindEmptyShopSlot()
    {
        foreach (var slot in Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
            if (slot != null && slot.gameObject.activeInHierarchy && slot.IsEmpty)
                return slot;

        throw new InvalidOperationException("No empty active ShopSlot found.");
    }

    static object InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
            throw new MissingMethodException(target.GetType().Name, methodName);
        return method.Invoke(target, args);
    }

    static T RequireOne<T>(string label) where T : Object
    {
        foreach (var obj in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            if (obj != null) return obj;

        throw new InvalidOperationException($"{label} not found.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);

        Debug.Log($"PA GatheringShopGate Check OK: {message}");
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
            Debug.LogError("PA Gathering ShopGate Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA Gathering ShopGate Validation finished successfully.");
        EditorApplication.Exit(0);
    }

    static void Cleanup()
    {
        Application.logMessageReceived -= OnLogMessage;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
    }
}
#endif
