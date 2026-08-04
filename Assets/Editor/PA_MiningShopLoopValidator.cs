#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// 실제 광질 Ore가 저장/로드를 거쳐 밤 판매까지 이어지는 단일 왕복 검증.
// 사용자 save 대신 Logs/MiningShopLoop/<timestamp>의 격리 저장소를 사용한다.
[InitializeOnLoad]
public static class PA_MiningShopLoopValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.MiningShopLoop.Active";
    const string EnteredKey = "PA.MiningShopLoop.Entered";
    const string RanKey = "PA.MiningShopLoop.Ran";
    const string HadErrorKey = "PA.MiningShopLoop.HadError";
    const string OutputKey = "PA.MiningShopLoop.Output";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;
    static Task _runtimeTask;

    static PA_MiningShopLoopValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;

        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _hadError = SessionState.GetBool(HadErrorKey, false);
        RegisterCallbacks();

        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Run Mining + Shop Loop Validation")]
    public static void RunMiningShopLoopValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA MiningShopLoop: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string output = Path.GetFullPath(Path.Combine(
            Application.dataPath, "..", "Logs", "MiningShopLoop", stamp));
        Directory.CreateDirectory(output);

        _entered = false;
        _ran = false;
        _hadError = false;
        _runtimeTask = null;
        _startedAt = EditorApplication.timeSinceStartup;

        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(HadErrorKey, false);
        SessionState.SetString(OutputKey, output);

        RegisterCallbacks();
        Debug.Log($"PA MiningShopLoop: entering Play Mode. Output={output}");
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
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        _hadError = true;
        SessionState.SetBool(HadErrorKey, true);
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
            Finish();
    }

    static void OnEditorUpdate()
    {
        double elapsed = EditorApplication.timeSinceStartup - _startedAt;

        if (!_entered)
        {
            if (elapsed > 60.0)
            {
                Debug.LogError("PA MiningShopLoop: timed out before entering Play Mode.");
                MarkFailedAndExit();
            }
            return;
        }

        if (!_ran && EditorApplication.isPlaying && _runtimeTask == null && elapsed > 3.0)
        {
            _runtimeTask = RunRuntimeChecksAsync();
            return;
        }

        if (!_ran && _runtimeTask != null && _runtimeTask.IsCompleted)
        {
            if (_runtimeTask.IsFaulted)
            {
                _hadError = true;
                SessionState.SetBool(HadErrorKey, true);
                Debug.LogError($"PA MiningShopLoop Validation failed: {_runtimeTask.Exception?.GetBaseException()}");
            }

            _ran = true;
            SessionState.SetBool(RanKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }

        if (!_ran && elapsed > 100.0)
        {
            Debug.LogError("PA MiningShopLoop: timed out before runtime checks completed.");
            MarkFailedAndExit();
        }
    }

    static async Task RunRuntimeChecksAsync()
    {
        Time.timeScale = 1f;

        var loop = RequireOne<DayNightShopLoopController>("DayNightShopLoopController");
        var save = RequireOne<SaveManager>("SaveManager");
        var economy = RequireOne<EconomyService>("EconomyService");
        var sales = RequireOne<SalesLogManager>("SalesLogManager");
        Require(GameClock.Instance != null, "GameClock exists");
        Require(Inventory.instance != null && Inventory.instance.hotbar != null,
            "Inventory and Hotbar exist");

        string output = SessionState.GetString(OutputKey, string.Empty);
        Require(!string.IsNullOrWhiteSpace(output), "isolated mining save output exists");
        Directory.CreateDirectory(output);

        var repositoryField = typeof(SaveManager).GetField(
            "_repository", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(repositoryField != null, "SaveManager repository can be isolated");
        repositoryField.SetValue(save, new LocalJsonSaveRepository(output));

        ClearRuntimeInventory();
        ClearShopSlots();

        var ore = Resources.Load<Item>("Items/Item_Ore");
        Require(ore != null && ore.itemName == "Ore", "Project P.A. Ore item exists");
        Require(ore.category == ItemCategory.Raw && ore.basePrice == 15,
            "Ore is a 15G Raw sellable item");

        var quarry = FindPoint("quarry-mining");
        Require(quarry != null, "quarry-mining day activity exists");
        Require(quarry.itemResourcePath == "Items/Item_Ore" && quarry.grantCount == 2,
            "quarry is configured to grant Ore x2");

        var mining = quarry.GetComponentInChildren<MiningSpot>(true);
        Require(mining != null, "quarry exposes MiningSpot IInteractable");
        Require(mining.GetInteractPrompt().Contains("광맥"), "quarry prompt offers a mining action");

        var player = PlayerObject();
        Require(player != null, "player exists for mining interaction");

        // Day 2 낮: 실제 상호작용으로 Ore 2개를 획득한다.
        economy.ForceSet(500, "PA_MiningShopLoop seed");
        economy.ForceSetCumulativeRevenue(0, "PA_MiningShopLoop seed");
        loop.SimulatePhaseForValidation(8f, 2);
        loop.ResetDayPrepForValidation();
        Require(loop.CurrentPhase == PADayNightPhase.DayPreparation,
            "Day 2 starts in DayPreparation");
        Require(loop.IsDayPrepPointAvailable(quarry), "quarry is available on Day 2");

        mining.Interact(player);
        Require(mining.IsMining, "mining enters the strike-and-wait state");
        Require(mining.LastFeedback.Contains("두드리는 중"), "mining gives immediate strike feedback");
        Require(mining.CompleteMiningForValidation(player),
            "finishing the mining action collects stock");
        Require(mining.LastFeedback.Contains("광석"), "successful mining gives Ore feedback");
        Require(CountInventoryItem(ore) == 2, "actual mining grants Ore x2");

        mining.Interact(player);
        Require(!mining.IsMining, "same-day repeat mining is blocked before striking");
        Require(!loop.IsDayPrepPointAvailable(quarry), "quarry is marked complete for Day 2");

        // 실제 SaveManager와 격리 JSON 저장소로 같은 날 완료/Ore를 왕복한다.
        await save.SaveGameAsync();
        string saveFile = Path.Combine(output, "savegame.json");
        Require(File.Exists(saveFile), "isolated mining savegame.json was written");
        var savedData = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveFile));
        Require(savedData != null && savedData.version == 10, "mining save uses schema v10");
        Require(savedData.dayPrepCollectedDay == 2
            && savedData.dayPrepCollectedActivities.Contains("quarry-mining"),
            "save JSON contains quarry-mining completion");
        Require(savedData.inventorySlots.Exists(x => x != null
            && x.itemId == ore.id && x.itemName == ore.itemName && x.count == 2),
            "save JSON contains the actually mined Ore x2");

        economy.ForceSet(1, "PA_MiningShopLoop mutate");
        economy.ForceSetCumulativeRevenue(2, "PA_MiningShopLoop mutate");
        GameClock.Instance.ForceSet(18f, 5, "PA_MiningShopLoop mutate");
        ClearRuntimeInventory();
        loop.RestoreSavedState(5, new List<string>());
        loop.SimulatePhaseForValidation(8f, 5);
        Require(loop.IsDayPrepPointAvailable(quarry), "mutated runtime state clears quarry completion");

        await save.LoadGameAsync();
        Require(GameClock.Instance.CurrentDay == 2
            && Mathf.Abs(GameClock.Instance.CurrentHour - 8f) < 0.01f,
            "load restores Day 2 morning");
        Require(economy.Money == 500 && economy.CumulativeRevenue == 0,
            "load restores pre-sale economy state");
        Require(CountInventoryItem(ore) == 2, "load restores mined Ore x2");
        Require(!loop.IsDayPrepPointAvailable(quarry),
            "load restores same-day quarry completion");

        // 저장에서 복원된 같은 Ore를 진열하고 기본가를 확정한다.
        var slot = FindEmptyShopSlot();
        slot.Interact(player);
        Require(!slot.IsEmpty && slot.currentItem.data == ore,
            "restored mined Ore can be stocked into a ShopSlot");
        Require(slot.currentItem.count == 1 && CountInventoryItem(ore) == 1,
            "stocking moves one of the two mined Ore into the shop");
        Require(slot.EffectiveDisplayPrice == ore.basePrice,
            "stocked Ore starts at its 15G base price");

        var priceUi = RequireOne<ShopPriceUI>("ShopPriceUI");
        int confirmBefore = priceUi.ConfirmCount;
        slot.Interact(player);
        Require(priceUi.IsOpen, "stocked Ore opens ShopPriceUI");
        InvokePrivate(priceUi, "OnConfirm");
        Require(priceUi.ConfirmCount == confirmBefore + 1
            && slot.EffectiveDisplayPrice == 15,
            "player confirms the Ore price at 15G");

        // Day 2 밤: 기존 영업 게이트와 NPC 구매 진입점으로 판매한다.
        loop.SimulatePhaseForValidation(20f, 2);
        loop.SetShopOpenedForValidation(false);
        Require(loop.CanPlayerOpenShop && !loop.IsShopOpenForCustomers,
            "customers wait for the Day 2 shop sign");
        Require(loop.TryOpenShop() && loop.IsShopOpenForCustomers,
            "player opens the Day 2 night shop");

        var customer = FindMinerCustomer();
        Require(customer != null, "Miner customer NPC exists");
        string buyerName = customer.profile != null && !string.IsNullOrWhiteSpace(customer.profile.npcName)
            ? customer.profile.npcName
            : customer.gameObject.name;

        int moneyBefore = economy.Money;
        long revenueBefore = economy.CumulativeRevenue;
        int saleRecordsBefore = sales.GetRecent(100).Count;
        var statsBefore = sales.GetDailyDecisionStats(2);

        Require(slot.TryPurchaseByNpc(buyerName, out int paid),
            "Miner purchase entry point buys the actually mined Ore");
        Require(paid == 15, "mined Ore sale pays 15G");
        Require(slot.IsEmpty, "Ore ShopSlot is empty after purchase");
        Require(economy.Money == moneyBefore + 15,
            $"Ore sale increases money ({moneyBefore} -> {economy.Money}G)");
        Require(economy.CumulativeRevenue == revenueBefore + 15,
            "Ore sale increases cumulative revenue by 15G");

        var recentSales = sales.GetRecent(100);
        Require(recentSales.Count == saleRecordsBefore + 1,
            "Ore sale appends one SalesLog record");
        var oreSale = recentSales[0];
        Require(oreSale.itemName == ore.itemName
            && oreSale.category == ItemCategory.Raw.ToString()
            && oreSale.buyerName == buyerName,
            "SalesLog records Ore, Raw category, and Miner buyer");
        var statsAfter = sales.GetDailyDecisionStats(2);
        Require(statsAfter.purchases == statsBefore.purchases + 1,
            "Ore sale increments the Day 2 purchase count");

        var moneyHud = RequireOne<MoneyHUD>("MoneyHUD");
        Require(moneyHud.moneyText != null
            && moneyHud.moneyText.text.Contains(economy.Money.ToString()),
            "MoneyHUD reflects the Ore sale balance");

        // 다음 날에는 같은 광맥이 다시 플레이 가능한 상태로 돌아온다.
        loop.SimulatePhaseForValidation(8f, 3);
        Require(loop.IsDayPrepPointAvailable(quarry),
            "quarry reactivates on Day 3");

        Debug.Log($"PA Mining ShopLoop Validation passed. Ore=2->1 stocked, sale={paid}G, "
            + $"money={moneyBefore}->{economy.Money}G, buyer={buyerName}, save={saveFile}");
    }

    static DaytimeStockPrepPoint FindPoint(string activityId)
    {
        foreach (var point in Object.FindObjectsByType<DaytimeStockPrepPoint>(FindObjectsSortMode.None))
            if (point != null && point.activityId == activityId) return point;
        return null;
    }

    static NpcController FindMinerCustomer()
    {
        NpcController fallback = null;
        foreach (var npc in Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            if (npc == null) continue;
            fallback ??= npc;
            string profileName = npc.profile != null ? npc.profile.npcName : string.Empty;
            if (npc.gameObject.name.Contains("Miner", StringComparison.OrdinalIgnoreCase)
                || profileName.Contains("Miner", StringComparison.OrdinalIgnoreCase)
                || profileName.Contains("광부", StringComparison.OrdinalIgnoreCase))
                return npc;
        }
        return fallback;
    }

    static GameObject PlayerObject()
    {
        return GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
    }

    static void ClearRuntimeInventory()
    {
        if (Inventory.instance == null) return;
        ClearSlots(Inventory.instance.slots);
        if (Inventory.instance.hotbar != null)
            ClearSlots(Inventory.instance.hotbar.slots);
        Inventory.instance.RefreshAllUI();
    }

    static void ClearSlots(List<InventorySlot> slots)
    {
        if (slots == null) return;
        foreach (var slot in slots) slot?.Clear();
    }

    static void ClearShopSlots()
    {
        foreach (var slot in Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
        {
            if (slot == null) continue;
            slot.currentItem = null;
            slot.displayPrice = 0;
            slot.RefreshDisplay();
        }
    }

    static int CountInventoryItem(Item item)
    {
        if (item == null || Inventory.instance == null) return 0;
        int count = CountInSlots(Inventory.instance.slots, item);
        if (Inventory.instance.hotbar != null)
            count += CountInSlots(Inventory.instance.hotbar.slots, item);
        return count;
    }

    static int CountInSlots(List<InventorySlot> slots, Item item)
    {
        if (slots == null) return 0;
        int count = 0;
        foreach (var slot in slots)
            if (slot != null && !slot.IsEmpty && slot.item == item)
                count += slot.count;
        return count;
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
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"PA MiningShopLoop Check OK: {message}");
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
        SessionState.EraseString(OutputKey);

        if (!entered || !ran || hadError)
        {
            Debug.LogError("PA Mining ShopLoop Validation failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("PA Mining ShopLoop Validation finished successfully.");
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
