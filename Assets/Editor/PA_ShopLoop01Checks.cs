using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PA_ShopLoop01Checks
{
    const string Active = "PA.ShopLoop01.Active";
    const string Output = "Logs/SHOP-LOOP-01/Play";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static IEnumerator routine;
    static int checks, lastFrame = -1;
    static double started;
    static WorldAlphaPlayableController alpha;
    static WorldGameplayAdapterService adapter;
    static Inventory inventory;
    static WorldHotbarPlacementController direct;
    static WorldBuildingPlacementService placement;

    public static void Run()
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/checks.txt", "Real WorldSandbox input event subscribers; existing placement and inventory authorities.\n");
        SessionState.SetBool(Active, true);
        SessionState.SetBool(Active + ".Failed", false);
        SessionState.SetInt(Active + ".Errors", 0);
        Subscribe();
        EditorSceneManager.OpenScene("Assets/Scenes/WorldSandbox.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Resume() { if (SessionState.GetBool(Active, false)) Subscribe(); }
    static void Subscribe()
    {
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Application.logMessageReceived -= Log; Application.logMessageReceived += Log;
        EditorApplication.playModeStateChanged -= Mode; EditorApplication.playModeStateChanged += Mode;
    }
    static void Mode(PlayModeStateChange mode)
    {
        if (mode != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Active, false)) return;
        int errors = SessionState.GetInt(Active + ".Errors", 0);
        bool failed = SessionState.GetBool(Active + ".Failed", false) || errors != 0;
        File.AppendAllText(Output + "/result.txt", $"Final status={(failed ? "FAIL" : "PASS")}\nPost-Play runtime errors/exceptions/asserts={errors}\n");
        SessionState.SetBool(Active, false);
        EditorApplication.update -= Tick; Application.logMessageReceived -= Log;
        EditorApplication.playModeStateChanged -= Mode;
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        try
        {
            if (SessionState.GetInt(Active + ".Errors", 0) != 0) throw new Exception("Runtime error observed.");
            if (routine == null) { started = EditorApplication.timeSinceStartup; routine = Exercise(); }
            if (EditorApplication.timeSinceStartup - started > 210) throw new Exception("Bounded Play timeout.");
            if (!routine.MoveNext()) Finish(false, "All targeted acceptance checks passed.");
        }
        catch (Exception ex) { Finish(true, ex.ToString()); }
    }

    static IEnumerator Exercise()
    {
        while ((alpha = WorldAlphaPlayableController.Instance) == null || !alpha.IsReady) yield return null;
        adapter = alpha.Adapter; inventory = adapter.PlayerInventory;
        direct = adapter.PlayerRoot.GetComponent<WorldHotbarPlacementController>();
        placement = alpha.GetComponent<WorldBuildingPlacementService>();
        typeof(SaveManager).GetMethod("SetRepositoryForValidation", Private).Invoke(adapter.RuntimeSaveManager,
            new object[] { new LocalJsonSaveRepository(Output + "/isolated-save") });
        Check(alpha.BeginNewGame(), "Existing new-game entry");
        yield return null;
        while (alpha.GetComponent<WorldNavigationService>().IsRebuilding) yield return null;
        var loop = DayNightShopLoopController.Instance;
        GameClock.Instance.secondsPerGameHour = 99999;
        loop.SimulatePhaseForValidation(19, 2);
        loop.SetShopOpenedForValidation(false);
        var kit = Resources.Load<Item>(WorldPlaceableKitCatalog.ShopItemPath);
        placement.RegisterDefinition(WorldPlaceableKitCatalog.ShopId, WorldPlaceableKitCatalog.Shop);
        Select(kit); FindClearPlayerPose(WorldPlaceableKitCatalog.ShopId, WorldPlaceableKitCatalog.Shop);
        Input("OnInteractPressed"); Input("OnBuildPlace");
        Check(placement.TryGetPlacement(WorldPlaceableKitCatalog.ShopId, out var built), "Shop Kit creates the actual placed B01");
        yield return null;
        while (alpha.GetComponent<WorldNavigationService>().IsRebuilding) yield return null;
        yield return null;
        var shop = built.GameObject.GetComponent<Shop>();
        Check(adapter.RuntimeShop == shop, "Runtime operations bind placed B01");
        var slots = shop.Slots.Where(x => x.isActiveAndEnabled).OrderBy(x => x.name).ToArray();
        Check(slots.Length >= 3, "At least three usable existing slots: " + slots.Length);
        Check(ShopPriceUI.instance != null && CustomerArrivalController.Instance != null, "Existing pricing and arrival authorities");
        foreach (var slot in inventory.slots) slot.Clear();
        foreach (var slot in adapter.PlayerHotbar.slots) slot.Clear();
        inventory.RefreshAllUI();
        var first = slots[0];
        Approach(first); Input("OnInteractPressed");
        Check(first.IsEmpty, "Unowned item cannot stock");
        var wood = Resources.Load<Item>("Items/Item_Wood");
        Check(inventory.AddInstance(new ItemInstance(wood, 4)), "Owned gathered-item inventory fixture");
        first.enabled = false; first.Interact(adapter.PlayerRoot); first.enabled = true;
        Check(inventory.CountItems(wood) == 4 && first.IsEmpty, "Invalid disabled slot consumes zero");
        foreach (var slot in slots.Take(3))
        {
            Approach(slot); Input("OnInteractPressed");
            Check(!slot.IsEmpty && slot.currentItem.data == wood && slot.currentItem.count == 1, "Space stocks real owned Wood in " + slot.name);
            Input("OnInteractPressed");
            Check(ShopPriceUI.instance.IsOpen, "Space opens existing price UI");
            SetPrice(10000, false);
            Check(slot.displayPrice == wood.basePrice && !ShopPriceUI.instance.IsOpen, "Cancel pending price preserves stock and price");
            slot.Interact(adapter.PlayerRoot); SetPrice(10000, true);
            Check(slot.displayPrice >= 10000, "Existing UI confirms rejection price");
        }
        Check(inventory.CountItems(wood) == 1, "Three stock transfers deduct exactly three");
        int money = EconomyService.Instance.Money, sales = SalesLogManager.Instance.GetRecent(100).Count;
        Check(!first.TryPurchaseByNpc("closed", out _) && !first.IsEmpty && EconomyService.Instance.Money == money,
            "Closed authority blocks payment and preserves stock");
        float wait = Time.realtimeSinceStartup + 1;
        while (Time.realtimeSinceStartup < wait) yield return null;
        Check(CustomerArrivalController.Instance.TouristsSpawnedThisOpening == 0, "No automatic tourists before OPEN");
        var sign = adapter.ShopSignTarget.GetComponent<ShopOpenSign>();
        Approach(sign); Input("OnInteractPressed");
        Check(loop.IsShopOpenForCustomers, "Space on existing sign opens placed shop");
        int decisionsBefore = SalesLogManager.Instance.GetDailyDecisionStats(2).rejections;
        double deadline = EditorApplication.timeSinceStartup + 90;
        bool sawCustomer = false, sawTarget = false;
        while (SalesLogManager.Instance.GetDailyDecisionStats(2).rejections == decisionsBefore)
        {
            foreach (var npc in Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None))
            {
                sawCustomer |= npc.shopLocation == shop.transform;
                sawTarget |= npc.CurrentShopSlotTarget != null && slots.Contains(npc.CurrentShopSlotTarget);
            }
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Automatic rejection timeout; tourists=" + CustomerArrivalController.Instance.TouristsSpawnedThisOpening);
            yield return null;
        }
        Check(sawCustomer && sawTarget, "Automatically arriving real NPC targets placed ShopSlot");
        Check(EconomyService.Instance.Money == money && SalesLogManager.Instance.GetRecent(100).Count == sales && slots.Take(3).All(x => !x.IsEmpty),
            "Real overpriced rejection pays zero, logs no sale, preserves stock");
        foreach (var slot in slots.Take(3)) { slot.Interact(adapter.PlayerRoot); SetPrice(1, true); }
        Check(slots.Take(3).All(x => x.displayPrice == 1), "Existing pricing UI confirms purchase-capable price");
        bool callbackBlocked = false;
        Action<int> onMoney = amount => { var active = slots.FirstOrDefault(x => x.IsClaimed); callbackBlocked = active != null && !active.TryPurchaseByNpc("reentrant", out _); };
        EconomyService.Instance.OnMoneyChanged += onMoney;
        try
        {
            deadline = EditorApplication.timeSinceStartup + 100;
            while (SalesLogManager.Instance.GetRecent(100).Count == sales)
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Automatic purchase timeout");
                yield return null;
            }
        }
        finally { EconomyService.Instance.OnMoneyChanged -= onMoney; }
        loop.SetShopOpenedForValidation(false);
        Check(EconomyService.Instance.Money == money + 1, "Actual sale money " + money + " -> " + EconomyService.Instance.Money);
        Check(SalesLogManager.Instance.GetRecent(100).Count == sales + 1 && SalesLogManager.Instance.GetRecent(1)[0].price == 1,
            "Exactly one successful sales record");
        Check(slots.Take(3).Count(x => x.IsEmpty) == 1, "Exactly one product removed");
        var sold = slots.Take(3).Single(x => x.IsEmpty);
        Check(!sold.TryPurchaseByNpc("repeat", out _) && EconomyService.Instance.Money == money + 1 &&
            SalesLogManager.Instance.GetRecent(100).Count == sales + 1, "Repeated callback cannot duplicate money/log");
        Check(callbackBlocked, "Synchronous wallet callback cannot reenter purchase");
        Approach(sold); Input("OnInteractPressed");
        Check(!sold.IsEmpty && inventory.CountItems(wood) == 0, "Player can restock sold slot and continue");
        sold.RetrieveItem();
        Check(sold.IsEmpty && inventory.CountItems(wood) == 1, "Existing retrieval returns owned item");
        File.WriteAllText(Output + "/metrics.txt", "Slots=" + slots.Length + "\nMoney=" + money + "->" + EconomyService.Instance.Money +
            "\nSales=" + sales + "->" + SalesLogManager.Instance.GetRecent(100).Count + "\nRejections=" + SalesLogManager.Instance.GetDailyDecisionStats(2).rejections);
        yield return null;
    }

    static void SetPrice(int price, bool confirm)
    {
        var ui = ShopPriceUI.instance;
        int current = (int)typeof(ShopPriceUI).GetField("_pendingPrice", Private).GetValue(ui);
        typeof(ShopPriceUI).GetMethod("AdjustPrice", Private).Invoke(ui, new object[] { price-current });
        var button = (UnityEngine.UI.Button)typeof(ShopPriceUI).GetField(confirm ? "_confirmBtn" : "_closeBtn", Private).GetValue(ui);
        button.onClick.Invoke();
    }
    static void Approach(Component target)
    {
        Vector3 center = target.transform.position;
        for (int ring = 0; ring < 4; ring++)
        for (int i = 0; i < 16; i++)
        {
            Vector3 pos = center + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * (.7f + ring*.35f);
            if (!alpha.Grid.WorldToCell(pos, out var cell) || !alpha.Grid.TryGetCell(cell, out var data) || !data.IsWalkable) continue;
            alpha.Grid.CellToWorld(cell, out var ground); pos.y = ground.y + .08f;
            Teleport(pos);
            adapter.PlayerRoot.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(center-pos, Vector3.up));
            Physics.SyncTransforms();
            object[] args = { null, default(RaycastHit) };
            if ((bool)typeof(PlayerInteraction).GetMethod("TryFindInteractable", Private).Invoke(adapter.PlayerInteraction, args) && ReferenceEquals(args[0], target)) return;
        }
        throw new Exception("No player interaction pose: " + target.name);
    }

    static void FindClearPlayerPose(string id, WorldBuildingPlacementDefinition def)
    {
        var grid = alpha.Grid;
        var generated = WorldPersistenceService.Instance.ActiveGeneratedWorld;
        generated.TryGetAnchor(WorldGenerationAnchorKind.Start, out var start);
        // Small local search near the generated player start, not a whole-world scan.
        for (int radius = 4; radius < 30; radius++)
        for (int x = -radius; x <= radius; x++)
        foreach (int z in new[] { -radius, radius })
        {
            var anchor = start.Coordinate + new Vector2Int(x, z);
            if (!placement.Evaluate(id, anchor, 0).Succeeded) continue;
            var offsets = def.ResolveFootprint(Vector2Int.zero, 0);
            Vector2 mean = Vector2.zero;
            foreach (var cell in offsets) mean += (Vector2)cell;
            mean /= offsets.Length;
            float r = offsets.Max(cell => Vector2.Distance(cell, mean));
            grid.CellToWorld(anchor, out var world);
            var player = world + new Vector3(mean.x, 0, mean.y) * grid.Definition.CellSize -
                Vector3.forward * ((r + 1.25f) * grid.Definition.CellSize);
            if (!grid.WorldToCell(player, out var pc) || !grid.TryGetCell(pc, out var data) || !data.IsWalkable || data.HasWater) continue;
            grid.CellToWorld(pc, out var ground); player.y = ground.y + .08f;
            Teleport(player); return;
        }
        throw new Exception("No local placement pose for " + id);
    }
    static void Teleport(Vector3 position)
    {
        var player = adapter.PlayerRoot;
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false; player.transform.SetPositionAndRotation(position, Quaternion.identity); cc.enabled = true;
        player.GetComponent<PlayerController>().ResetMotionAfterTeleport(); Physics.SyncTransforms();
    }
    static void Select(Item item)
    {
        int index = adapter.PlayerHotbar.slots.FindIndex(s => s.item == item && !s.IsEmpty);
        Check(index >= 0, item.itemName + " present in actual Hotbar");
        var callback = (Action<int>)typeof(PlayerInputHandler).GetField("OnHotbarDirectSelect", Private).GetValue(PlayerInputHandler.Instance);
        Check(callback != null, "Hotbar input subscribed"); callback(index);
    }
    static void Input(string name)
    {
        var callback = (Action)typeof(PlayerInputHandler).GetField(name, Private).GetValue(PlayerInputHandler.Instance);
        Check(callback != null, name + " subscribed"); callback();
    }
    static void CheckControl(string after)
    {
        Check(adapter.PlayerRoot.GetComponent<PlayerController>().enabled && adapter.PlayerRoot.GetComponent<CharacterController>().enabled &&
            adapter.PlayerInteraction.enabled && Time.timeScale > 0 && !direct.IsPlacing, "Player control available after " + after);
    }
    static IEnumerator CheckMovement()
    {
        var input = PlayerInputHandler.Instance;
        var move = typeof(PlayerInputHandler).GetProperty("MoveInput");
        var before = adapter.PlayerRoot.transform.position;
        input.enabled = false;
        try
        {
            move.SetValue(input, Vector2.down);
            float until = Time.realtimeSinceStartup + .35f;
            while (Time.realtimeSinceStartup < until) yield return null;
        }
        finally { move.SetValue(input, Vector2.zero); input.enabled = true; }
        Check(Vector3.ProjectOnPlane(adapter.PlayerRoot.transform.position - before, Vector3.up).magnitude > .05f,
            "Existing PlayerController actually moves across live frames after placement exit");
    }
    static void Check(bool ok, string text)
    {
        if (!ok) throw new Exception(text);
        checks++; File.AppendAllText(Output + "/checks.txt", "PASS " + text + "\n");
    }
    static void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            SessionState.SetInt(Active + ".Errors", SessionState.GetInt(Active + ".Errors", 0) + 1);
    }
    static void Finish(bool failed, string reason)
    {
        SessionState.SetBool(Active + ".Failed", failed);
        File.WriteAllText(Output + "/result.txt", $"SHOP-LOOP-01 {(failed ? "FAIL" : "PASS")}\nChecks={checks}\nPlay sessions=1\nRuntime errors/exceptions/asserts={SessionState.GetInt(Active + ".Errors", 0)}\n{reason}\n");
        EditorApplication.update -= Tick; EditorApplication.ExitPlaymode();
    }
}
