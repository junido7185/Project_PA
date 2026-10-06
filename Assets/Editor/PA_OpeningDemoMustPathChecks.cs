using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Canon v2 §21: enter the remaining demo route directly, preserving the already verified tutorial.
public static class PA_OpeningDemoMustPathChecks
{
    const string Active = "PA.OpeningDemoMustPath.Active";
    const string Output = "Logs/OpeningDemoMustPath/result.txt";
    static double started;
    static int step;
    static int errors;
    static string problem;

    [InitializeOnLoadMethod]
    static void Subscribe()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run from Edit mode.");
        Directory.CreateDirectory("Logs/OpeningDemoMustPath");
        File.WriteAllText(Output, "Canon v2 remaining MUST PATH, direct Demo256 entry.\n");
        AssertAssets();
        SessionState.SetBool(Active, true);
        started = 0;
        step = 0;
        errors = 0;
        problem = null;
        EditorSceneManager.OpenScene("Assets/Scenes/WorldSandbox.unity");
        PA_DepartureContinuationChecks.ConfigureGameView();
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        EditorApplication.EnterPlaymode();
    }

    public static void RunEntranceReview()
    {
        Run();
        SessionState.SetBool(Active + ".EntranceReview", true);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BindDirectDemo()
    {
        if (!SessionState.GetBool(Active, false)) return;
        SessionState.SetBool(Active + ".RunInBackground", Application.runInBackground);
        Application.runInBackground = true;
        var prefab = Resources.Load<GameObject>("DepartureTutorial/DepartureContinuation");
        var selection = prefab != null ? prefab.GetComponentInChildren<DepartureCompanionSelection>(true) : null;
        Require(selection != null && selection.candidates != null && selection.candidates.Length == 3,
            "authored companion candidates present");
        typeof(DemoRouteController).GetField("<SelectedCompanions>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, selection.candidates.Where(c => c.id == "Miner_01" || c.id == "Farmer_01").ToArray());
        var grid = UnityEngine.Object.FindFirstObjectByType<WorldGridService>();
        Require(grid != null, "WorldSandbox grid present");
        if (grid.GetComponent<DemoRouteController>() == null) grid.gameObject.AddComponent<DemoRouteController>();
    }

    static void AssertAssets()
    {
        var assets = FirstDayStudioAssets.Load();
        var catalog = DemoPlaceableCatalog.Load();
        Require(assets != null && catalog != null, "first day and placeable catalogs present");
        int[] expected = { 2011, 2012, 2013, 2014 };
        foreach (int id in expected)
            Require(assets.supplies.Any(i => i != null && i.id == id), "supply item " + id);
        foreach (DemoPlaceableKind kind in new[] { DemoPlaceableKind.ShopBase, DemoPlaceableKind.ResidentTent,
                     DemoPlaceableKind.Workbench, DemoPlaceableKind.DisplayStand })
        {
            var entry = catalog.Find(kind);
            Require(entry != null && entry.item != null && Resources.Load<BuildingData>(entry.buildingResource)?.prefab != null,
                "prepared placeable " + kind);
        }
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (started == 0) started = EditorApplication.timeSinceStartup;
        try
        {
            if (EditorApplication.timeSinceStartup - started > 90) throw new TimeoutException("step " + step);
            var route = WorldAlphaPlayableController.Instance?.DemoRoute;
            if (route == null || !route.IsPlayable) return;
            var adapter = WorldAlphaPlayableController.Instance.Adapter;
            var progress = DemoSettlementController.Instance;
            Require(adapter != null && progress != null, "direct Demo256 authorities ready");
            if (step == 0)
            {
                var supply = FirstDayWorldPresentation.Instance?.Supply;
                Require(supply != null && !supply.Collected, "supply available at harbor");
                supply.Interact(adapter.PlayerRoot);
                Require(supply.Collected, "supply collected through IInteractable");
                var inv = adapter.PlayerInventory;
                var assets = FirstDayStudioAssets.Load();
                foreach (var item in assets.supplies.Where(i => i != null && i.id != 2010))
                    Require(inv.CountItems(item) == (item.id == 2013 ? 2 : 1), "inventory supply " + item.id);
                Require(inv.CountItems(assets.supplies.First(i => i.id == 2010)) == 0, "smartphone stays system UI");
                Note("SUPPLY PASS");
                step++;
            }
            if (step == 1)
            {
                var placement = WorldAlphaPlayableController.Instance.Buildings;
                var catalog = DemoPlaceableCatalog.Load();
                Require(!DayNightShopLoopController.Instance.TryOpenShop(), "uninstalled shop cannot open");
                if (SessionState.GetBool(Active + ".EntranceReview", false))
                {
                    SessionState.SetBool(Active + ".EntranceReview", false);
                    SessionState.SetBool(Active, false);
                    Note("ENTRANCE REVIEW: supplied inventory only; use actual Hotbar preview/confirm for all buildings.");
                    return;
                }
                Place(placement, progress, catalog.Find(DemoPlaceableKind.ShopBase), "shop");
                Require(progress.HasShopBase && progress.OperatingShop != null, "shop/base registered");
                Place(placement, progress, catalog.Find(DemoPlaceableKind.ResidentTent), "tent1");
                Place(placement, progress, catalog.Find(DemoPlaceableKind.ResidentTent), "tent2");
                Require(progress.Established && progress.TentCount == 2 && progress.LicensePoints == 1,
                    "shop/base and two tents establish settlement");
                Require(progress.Residents.Count == 2 && progress.Residents.All(r => !string.IsNullOrEmpty(r.TentId)),
                    "selected companions claim separate tents");
                Note("SETTLEMENT PASS");
                step++;
            }
            if (step == 2)
            {
                Require(progress.TryChooseRoot(DemoSpecialization.Mining), "first root selectable");
                Require(progress.SelectedRoot == DemoSpecialization.Mining && progress.LicensePoints == 0,
                    "root fixed and point consumed");
                Require(!progress.TryChooseRoot(DemoSpecialization.Forestry), "second root rejected");
                Note("SPECIALIZATION PASS");
                step++;
            }
            if (step == 3)
            {
                if (GameClock.Instance == null || GameClock.Instance.CurrentHour < 16) return;
                GameClock.Instance.ForceSet(20, 1, "Opening MUST PATH deterministic night");
                typeof(DayNightShopLoopController).GetMethod("RefreshState", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(DayNightShopLoopController.Instance, new object[] { true });
                step++;
            }
            if (step == 4)
            {
                if (!progress.NightReady) return;
                var loop = DayNightShopLoopController.Instance;
                Require(loop != null && loop.CanPlayerOpenShop, "night shop gate ready");
                Note("SUNSET/NIGHT PASS");
                var placement = WorldAlphaPlayableController.Instance.Buildings;
                Place(placement, progress, DemoPlaceableCatalog.Load().Find(DemoPlaceableKind.DisplayStand), "stand");
                ShopSlot slot = progress.OperatingShop.Slots.FirstOrDefault(s => s != null && s.isActiveAndEnabled);
                Require(slot != null && slot.IsEmpty, "placed display stand bound to operating shop");
                Item product = Resources.Load<Item>("Items/Item_Wood");
                Require(adapter.PlayerInventory.TryReceiveToHotbar(product, 1), "sellable product enters hotbar");
                int productIndex = adapter.PlayerInventory.hotbar.slots.FindIndex(s => !s.IsEmpty && s.item == product);
                Require(productIndex >= 0, "sellable product hotbar slot");
                adapter.PlayerInventory.SelectHotbarSlot(productIndex);
                slot.Interact(adapter.PlayerRoot);
                Require(!slot.IsEmpty && slot.currentItem.data == product, "display uses existing ShopSlot stock transfer");
                slot.Interact(adapter.PlayerRoot);
                Require(ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen, "price UI opens for stocked stand");
                typeof(ShopPriceUI).GetMethod("OnConfirm", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(ShopPriceUI.instance, null);
                Require(slot.displayPrice > 0 && !ShopPriceUI.instance.IsOpen, "price confirmed");
                Require(loop.TryOpenShop() && loop.IsShopOpenForCustomers, "OPEN activates customer purchase gate");
                int money = EconomyService.Instance.Money;
                Require(slot.TryPurchaseByNpc("OpeningMustPathCustomer", out int paid) && paid == slot.displayPrice,
                    "actual ShopSlot NPC purchase");
                Require(EconomyService.Instance.Money == money + paid && route.FirstDemoSaleCompleted,
                    "sale reaches economy, log, and demo success observer");
                Require(loop.TryCloseOpeningShop() && loop.OpeningSessionCompleted, "CLOSE settles opening session");
                Require(progress.PioneerReport != null && progress.PioneerReport.sales == 1 &&
                    progress.PioneerReport.revenue == paid && SmartphoneUI.instance != null &&
                    SmartphoneUI.instance.IsOpen && SmartphoneUI.instance.CurrentTabIndex == 4,
                    "Pioneer Report appears in shop phone with actual sale result");
                Require(!loop.TryCloseOpeningShop() && !loop.TryOpenShop(),
                    "repeat open and close cannot settle again");
                Note("DISPLAY/PRICE/OPEN/SALE/CLOSE/REPORT PASS");
                Finish("PASS");
            }
        }
        catch (Exception ex) { problem = ex.ToString(); Finish("FAIL"); }
    }

    static void Place(WorldBuildingPlacementService placement, DemoSettlementController progress,
        DemoPlaceableEntry entry, string suffix)
    {
        Require(entry != null && progress.CanPlace(entry), "entry allowed " + suffix);
        string id = "opening-check-" + suffix;
        placement.RegisterDefinition(id, entry.Definition);
        var grid = placement.GetComponent<WorldGridService>();
        Require(grid.WorldToCell(FirstDayWorldPresentation.Instance.Harbor, out var harbor), "harbor cell");
        bool found = false;
        Vector2Int candidate = default;
        for (int radius = 5; radius <= 50 && !found; radius++)
            for (int dz = -radius; dz <= radius && !found; dz++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    candidate = harbor + new Vector2Int(dx, dz);
                    if (!placement.Evaluate(id, candidate, 0).Succeeded) continue;
                    found = true; break;
                }
        Require(found, "valid footprint near harbor for " + suffix);
        var result = placement.TryPlace(id, candidate, 0);
        WorldPlacedBuildingRuntime placed = null;
        Require(result.Succeeded && placement.TryGetPlacement(id, out placed), "placement commit " + suffix);
        typeof(DemoSettlementController).GetMethod("OnPlaced", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(progress, new object[] { placed, entry });
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (!SessionState.GetBool(Active, false) ||
            type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (stack.Contains("UnityEditor.Connect.") && message.Contains("Token Exchange")) return;
        errors++;
        File.AppendAllText(Output, "CONSOLE " + message + "\n" + stack + "\n");
    }
    static void Require(bool okay, string label)
    {
        if (!okay) throw new InvalidOperationException(label);
    }
    static void Note(string line) => File.AppendAllText(Output, line + "\n");
    static void Finish(string status)
    {
        if (status == "PASS" && errors != 0) status = "FAIL";
        Note(status + " consoleErrors=" + errors + (problem == null ? "" : "\n" + problem));
        SessionState.SetBool(Active, false);
        Application.runInBackground = SessionState.GetBool(Active + ".RunInBackground", false);
        EditorApplication.isPlaying = false;
        if (Application.isBatchMode) EditorApplication.Exit(status == "PASS" ? 0 : 1);
    }
}
