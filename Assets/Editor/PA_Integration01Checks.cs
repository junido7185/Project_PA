using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PA_Integration01Checks
{
    const string Active = "PA.Integration01.Active";
    const string Output = "Logs/INTEGRATION-01/Play";
    const string BuildEntryPath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string DeparturePath = "Assets/Scenes/PA_DepartureTutorial.unity";
    const string WorldPath = "Assets/Scenes/WorldSandbox.unity";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static IEnumerator routine;
    static int checks, lastFrame = -1;
    static double started;
    static WorldAlphaPlayableController alpha;
    static WorldGameplayAdapterService adapter;
    static Inventory inventory;
    static WorldHotbarPlacementController direct;
    static WorldBuildingPlacementService placement;

    [MenuItem("Tools/Project PA/Integration 01/Run Product Entry Route")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/checks.txt", "Real build entry, scene transition, WorldSandbox input event subscribers, placement and inventory authorities.\n");
        ValidateBuildScenes();
        SessionState.SetBool(Active, true);
        SessionState.SetBool(Active + ".Failed", false);
        SessionState.SetInt(Active + ".Errors", 0);
        Subscribe();
        EditorSceneManager.OpenScene(BuildEntryPath, OpenSceneMode.Single);
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
            if (EditorApplication.timeSinceStartup - started > 300) throw new Exception("Bounded Play timeout.");
            if (!routine.MoveNext()) Finish(false, "All targeted acceptance checks passed.");
        }
        catch (Exception ex) { Finish(true, ex.ToString()); }
    }


    // Build Settings만 추가하며 씬 직렬화는 변경하지 않는다.
    [MenuItem("Tools/Project PA/Integration 01/Register Demo Scenes")]
    public static void RegisterDemoScenes()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        foreach (string path in new[] { DeparturePath, WorldPath })
        {
            var existing = scenes.FirstOrDefault(s => s.path == path);
            if (existing == null) scenes.Add(new EditorBuildSettingsScene(path, true));
            else existing.enabled = true;
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void ValidateBuildScenes()
    {
        var enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
        Check(enabled.Length > 0 && enabled[0].path == BuildEntryPath,
            "Prototype_FirstDay is the enabled build entry");
        Check(enabled.Any(scene => scene.path == DeparturePath),
            "PA_DepartureTutorial is enabled in Build Settings");
        Check(enabled.Any(scene => scene.path == WorldPath),
            "WorldSandbox is enabled in Build Settings");
    }

    static IEnumerator Exercise()
    {
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Prototype_FirstDay",
            "Play starts from Prototype_FirstDay");
        PlayableDayScenarioController entry;
        while ((entry = Object.FindFirstObjectByType<PlayableDayScenarioController>()) == null) yield return null;
        typeof(PlayableDayScenarioController).GetMethod("OnPrimaryPressed", Private).Invoke(entry, null);
        yield return null;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Prototype_FirstDay")
        {
            entry = Object.FindFirstObjectByType<PlayableDayScenarioController>();
            typeof(PlayableDayScenarioController).GetMethod("OnPrimaryPressed", Private).Invoke(entry, null);
        }
        while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != DepartureTutorialController.SceneName) yield return null;
        Check(Time.timeScale > 0f, "NEW GAME restores gameplay time before departure load");
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == DepartureTutorialController.SceneName,
            "Prototype_FirstDay NEW GAME enters PA_DepartureTutorial");

        DepartureTutorialController tutorial;
        while ((tutorial = Object.FindFirstObjectByType<DepartureTutorialController>()) == null || !tutorial.IsReady) yield return null;
        Check(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11, "D3D11");
        IsolateSave(SaveManager.instance);
        OpeningPose(tutorial.player, tutorial.movementCheckpoint.position + Vector3.up * .08f, tutorial.fruitTree.transform.position);
        while (tutorial.Stage != 2) yield return null;
        OpeningPose(tutorial.player, new Vector3(0,.1f,-2.65f), tutorial.fruitTree.transform.position);
        Input("OnInteractPressed");
        while (tutorial.Stage != 3) yield return null;
        OpeningPose(tutorial.player, new Vector3(7,.1f,-2.85f), tutorial.trainingSlot.transform.position);
        Input("OnInteractPressed");
        while (tutorial.Stage != 4) yield return null;
        Input("OnInteractPressed");
        Check(ShopPriceUI.instance.IsOpen, "Opening real stocking / price UI");
        SetPrice(1, true);
        while (!tutorial.Complete) yield return null;
        Check(tutorial.SaleAmount == 1 && tutorial.trainingSlot.IsEmpty, "Opening existing NPC certification sale");
        var selection = AssertCompanionSelectionBeforeCta(tutorial);
        tutorial.presentation.CompanionButton.onClick.Invoke();
        Check(selection.IsOpen, "Existing certification CTA opens companion selection");
        selection.CandidateButtons[1].onClick.Invoke(); selection.CandidateButtons[2].onClick.Invoke();
        selection.DepartureButton.onClick.Invoke();
        Check(selection.IsConfirmed, "Existing departure confirmation");
        var voyage = selection.GetComponent<DepartureVoyagePresentation>();
        while (!voyage.Sailing) yield return null;
        Check(!selection.GetComponent<FirstIslandSettlementController>().enabled, "Small-island settlement deferred by bridge");
        while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != DemoRouteController.WorldScene) yield return null;
        while ((alpha = WorldAlphaPlayableController.Instance) == null || !alpha.IsReady || alpha.DemoRoute == null || !alpha.DemoRoute.IsPlayable) yield return null;
        adapter = alpha.Adapter; inventory = adapter.PlayerInventory;
        direct = adapter.PlayerRoot.GetComponent<WorldHotbarPlacementController>();
        placement = alpha.Buildings;
        IsolateSave(adapter.RuntimeSaveManager);
        Check(alpha.Grid.Definition.Width == 256 && alpha.Grid.Definition.Height == 256, "Opening transitions to actual Demo256 runtime");
        Check(Object.FindFirstObjectByType<DepartureVoyagePresentation>() == null, "16x16 presentation island unloaded");
        Check(alpha.HasStartedBeta && !alpha.StartPromptVisible && !alpha.DevelopmentOverlayVisible && alpha.Opening == null,
            "Playable session starts automatically without campaign grants / development UI");
        while (alpha.GetComponent<WorldNavigationService>().IsRebuilding) yield return null;
        GameClock.Instance.secondsPerGameHour = 99999;
        var loop = DayNightShopLoopController.Instance;
        var required = new[] { "Items/Item_Axe", "Items/Item_Pickaxe", "Items/Item_Net", WorldPlaceableKitCatalog.HubItemPath, WorldPlaceableKitCatalog.ShopItemPath };
        foreach (var path in required)
        {
            var item = Resources.Load<Item>(path);
            Check(inventory.CountItems(item) == 1, path + " bootstrap exactly one");
            Select(item);
        }
        Check(inventory.slots.Concat(adapter.PlayerHotbar.slots).Where(s => !s.IsEmpty).All(s => required.Any(path => Resources.Load<Item>(path) == s.item)),
            "Initial inventory contains only required tools/kits; no sellable resources");
        int successCount = 0;
        alpha.DemoRoute.DemoSucceeded += () => successCount++;
        Check(!alpha.DemoRoute.FirstDemoSaleCompleted, "Opening sale does not complete Demo256");
        Select(Resources.Load<Item>("Items/Item_Axe"));
        var moving = CheckMovement(); while (moving.MoveNext()) yield return moving.Current;

        var timber = FindResource<Gatherable>(WorldGenerationAnchorKind.ForestActivity, g => g.IsDirectWorld);
        var wood = Resources.Load<Item>("Items/Item_Wood");
        Select(Resources.Load<Item>("Items/Item_Axe"));
        Approach(timber);
        for (int i=0;i<3;i++) { Input("OnInteractPressed"); float until = Time.time + .35f; while (Time.time < until) yield return null; }
        Check(timber.DirectDepleted && inventory.CountItems(wood) > 0, "Forest Timber -> actual Wood " + inventory.CountItems(wood));
        int gatheredWood = inventory.CountItems(wood);
        var stone = FindResource<MiningSpot>(WorldGenerationAnchorKind.HighlandActivity, g => g.IsDirectWorld);
        var ore = Resources.Load<Item>("Items/Item_Ore");
        Select(Resources.Load<Item>("Items/Item_Pickaxe")); Approach(stone);
        for (int i=0;i<3;i++) { Input("OnInteractPressed"); float until = Time.time + .35f; while (Time.time < until) yield return null; }
        Check(stone.DirectDepleted && inventory.CountItems(ore) > 0, "Highland Stone -> actual Ore " + inventory.CountItems(ore));
        var shore = adapter.FindDaytimeActivity("shore-forage");
        var fishing = shore.GetComponentInChildren<FishingSpot>();
        var fish = Resources.Load<Item>("Items/Item_Fish");
        Check(fishing != null && fishing.IsDirectPlayerDemo, "Existing direct fishing requires proximity and two inputs, no rod grant");
        Reachable(fishing, WorldGenerationAnchorKind.PondActivity); Approach(fishing);
        Input("OnInteractPressed"); Check(fishing.IsFishing, "Fish cast through Space");
        while (!fishing.BiteReady) yield return null;
        Input("OnInteractPressed");
        Check(inventory.CountItems(fish) == shore.grantCount && !fishing.IsFishing, "Coast Fish real bite -> catch " + inventory.CountItems(fish));
        var bug = adapter.RuntimeRoot.GetComponentsInChildren<BugCritter>().First();
        Reachable(bug, WorldGenerationAnchorKind.MeadowActivity);
        Select(Resources.Load<Item>("Items/Item_Net")); Approach(bug); Input("OnInteractPressed");
        Check(bug.Captured && inventory.CountItems(Resources.Load<Item>("Items/Item_Butterfly")) == 1, "Meadow Net capture -> Butterfly 1");

        foreach (string path in new[] { WorldPlaceableKitCatalog.HubItemPath, WorldPlaceableKitCatalog.ShopItemPath })
        {
            var kit = Resources.Load<Item>(path);
            Check(WorldPlaceableKitCatalog.TryKit(kit, out var id, out var definition), "Canonical kit binding");
            placement.RegisterDefinition(id, definition);
            Select(kit); FindClearPlayerPose(id, definition);
            Input("OnInteractPressed"); Input("OnBuildPlace");
            Check(placement.TryGetPlacement(id, out _) && inventory.CountItems(kit) == 0, id + " placed; one kit consumed");
            yield return null;
            while (alpha.GetComponent<WorldNavigationService>().IsRebuilding) yield return null;
        }
        yield return null;
        Check(placement.TryGetPlacement(WorldPlaceableKitCatalog.ShopId, out var placed) && adapter.RuntimeShop.gameObject == placed.GameObject,
            "Actual placed B01 bound to shop operation");
        MoveGatheredWoodToHotbar(wood); Select(wood);
        var slot = adapter.RuntimeShopSlots.First(s => s.isActiveAndEnabled);
        Approach(slot); Input("OnInteractPressed");
        Check(!slot.IsEmpty && slot.currentItem.data == wood && slot.currentItem.count == 1 && inventory.CountItems(wood) == gatheredWood - 1,
            "Actually gathered Wood stocked once through Hotbar / Space");
        Input("OnInteractPressed"); Check(ShopPriceUI.instance.IsOpen, "Existing ShopPriceUI opens"); SetPrice(1, true);
        Check(slot.displayPrice == 1 && !ShopPriceUI.instance.IsOpen, "Valid 1G price confirmed by existing UI");
        Check(!alpha.DemoRoute.FirstDemoSaleCompleted && successCount == 0, "Gather/place/stock do not complete sale");
        loop.SimulatePhaseForValidation(19, 1);
        loop.SetShopOpenedForValidation(false);
        int money = EconomyService.Instance.Money, sales = SalesLogManager.Instance.GetRecent(100).Count;
        int decisions = 0; bool realBuyer = false;
        void OnDecision(NpcProfile profile, PurchaseEvaluator.Result result, Item item, int price, string customer)
        { if (item == wood) { decisions++; realBuyer |= result.willBuy; } }
        PurchaseFeedbackPresentationController.OnDecisionRecorded += OnDecision;
        Approach(adapter.ShopSignTarget.GetComponent<ShopOpenSign>()); Input("OnInteractPressed");
        Check(loop.IsShopOpenForCustomers, "Real sign Space -> OPEN");
        double deadline = EditorApplication.timeSinceStartup + 100;
        bool sawTarget = false;
        while (SalesLogManager.Instance.GetRecent(100).Count == sales)
        {
            sawTarget |= Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None).Any(n => n.CurrentShopSlotTarget == slot);
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Real NPC purchase timeout");
            yield return null;
        }
        PurchaseFeedbackPresentationController.OnDecisionRecorded -= OnDecision;
        Check(sawTarget && decisions > 0 && realBuyer, "Automatic real NPC targeted / evaluated / purchased gathered item");
        Check(slot.IsEmpty && inventory.CountItems(wood) == gatheredWood - 1, "Product consumed exactly once");
        Check(EconomyService.Instance.Money == money + 1, "Balance " + money + " -> " + EconomyService.Instance.Money);
        Check(SalesLogManager.Instance.GetRecent(100).Count == sales + 1, "SalesLog " + sales + " -> " + SalesLogManager.Instance.GetRecent(100).Count);
        Check(alpha.DemoRoute.FirstDemoSaleCompleted && successCount == 1, "DEMO_SUCCESS true / event exactly once");
        Check(!slot.TryPurchaseByNpc("repeat", out _), "Sold slot rejects repeat transaction");
        float end = Time.time + 2f; while (Time.time < end) yield return null;
        Check(EconomyService.Instance.Money == money+1 && SalesLogManager.Instance.GetRecent(100).Count == sales+1 && successCount == 1,
            "No duplicate money / log / success after repeated callback and live frames");
        // 이미 판매된 칸에서 벗어나 실제 이동 입력을 확인한다.
        WorldPersistenceService.Instance.ActiveGeneratedWorld.TryGetAnchor(WorldGenerationAnchorKind.Start, out var startAnchor);
        alpha.MovePlayerToCellForValidation(startAnchor.Coordinate);
        CheckControl("complete demo loop");
        moving = CheckMovement(); while (moving.MoveNext()) yield return moving.Current;
        File.WriteAllText(Output + "/metrics.txt", $"Startup=Prototype_FirstDay -> NEW GAME -> PA_DepartureTutorial -> voyage -> WorldSandbox Demo256\nWood={gatheredWood}\nOre={inventory.CountItems(ore)}\nFish={inventory.CountItems(fish)}\nBug=1\nBalance={money}->{EconomyService.Instance.Money}\nSalesLog={sales}->{SalesLogManager.Instance.GetRecent(100).Count}\nSuccessEvents={successCount}\nNPCDecisions={decisions}\n");
    }

    // Shared with focused visual QA: observe these same lifecycle assertions before
    // invoking the CTA, without running the unrelated remainder of the full route.
    public static DepartureCompanionSelection AssertCompanionSelectionBeforeCta(DepartureTutorialController tutorial)
    {
        Directory.CreateDirectory(Output);
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var loaded = Object.FindObjectsByType<DepartureCompanionSelection>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(candidate => candidate != null && candidate.gameObject.scene.IsValid()
                && candidate.gameObject.scene.isLoaded).ToArray();
        var selections = loaded.Where(candidate => candidate.gameObject.scene == scene).ToArray();
        string state = $"activeScene={scene.path}; loadedCount={loaded.Length}; sceneCount={selections.Length}\n"
            + string.Join("\n", loaded.Select(candidate =>
                $"id={candidate.GetInstanceID()}; name={candidate.name}; scene={candidate.gameObject.scene.path}; "
                + $"activeSelf={candidate.gameObject.activeSelf}; activeInHierarchy={candidate.gameObject.activeInHierarchy}; "
                + $"enabled={candidate.enabled}; isActiveAndEnabled={candidate.isActiveAndEnabled}; tutorialMatches={candidate.Tutorial == tutorial}"));
        File.AppendAllText(Output + "/checks.txt", "LIFECYCLE " + state + "\n");
        try
        {
            Check(selections.Length == 1, "Departure scene owns exactly one companion selection");
            var selection = selections[0];
            Check(selection.gameObject.activeInHierarchy && selection.enabled,
                "Departure companion selection is active and enabled before CTA");
            Check(tutorial != null && selection.Tutorial == tutorial && tutorial.presentation != null
                && tutorial.presentation.CompanionButton != null,
                "Departure companion selection references the active tutorial CTA");
            return selection;
        }
        catch (Exception exception)
        {
            File.AppendAllText(Output + "/checks.txt", "LIFECYCLE FAIL " + exception + "\n");
            throw;
        }
    }

    static void IsolateSave(SaveManager save)
    {
        // 출항 씬에서는 정착 단계 이전에 SaveManager가 존재하지 않는다.
        if (save == null) return;
        typeof(SaveManager).GetMethod("SetRepositoryForValidation", Private).Invoke(save,
            new object[] { new LocalJsonSaveRepository(Output + "/isolated-save") });
    }
    static void OpeningPose(Transform player, Vector3 position, Vector3 target)
    {
        var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
        player.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.ProjectOnPlane(target-position, Vector3.up)));
        cc.enabled = true; player.GetComponent<PlayerController>().ResetMotionAfterTeleport(); Physics.SyncTransforms();
    }
    static T FindResource<T>(WorldGenerationAnchorKind kind, Func<T,bool> predicate) where T : Component
    {
        var generated = WorldPersistenceService.Instance.ActiveGeneratedWorld;
        generated.TryGetAnchor(kind, out var anchor); alpha.Grid.CellToWorld(anchor.Coordinate, out var point);
        var target = adapter.RuntimeRoot.GetComponentsInChildren<T>().Where(predicate).OrderBy(x => (x.transform.position-point).sqrMagnitude).First();
        Reachable(target, kind); return target;
    }
    static void Reachable(Component target, WorldGenerationAnchorKind kind)
    {
        var generated = WorldPersistenceService.Instance.ActiveGeneratedWorld;
        Check(generated.TryGetAnchor(kind, out var anchor) && alpha.Grid.WorldToCell(target.transform.position, out var cell) &&
            WorldCellReachability.CanReachAll(alpha.Grid, anchor.Coordinate, new[] { cell }, null, null, out _), "Reachable activity: " + kind);
    }
    static void MoveGatheredWoodToHotbar(Item wood)
    {
        InventoryUI.instance.Toggle();
        var fromIndex = inventory.slots.FindIndex(s => s.item == wood);
        var toIndex = adapter.PlayerHotbar.slots.FindIndex(s => s.IsEmpty);
        var uis = Object.FindObjectsByType<InventorySlotUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var from = uis.First(s => s.owner == SlotOwner.Inventory && s.index == fromIndex && s.GetSlot() == inventory.slots[fromIndex]);
        var to = uis.First(s => s.owner == SlotOwner.Hotbar && s.index == toIndex && s.GetSlot() == adapter.PlayerHotbar.slots[toIndex]);
        var data = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        from.OnBeginDrag(data); to.OnDrop(data); from.OnEndDrag(data);
        InventoryUI.instance.Toggle();
        Check(adapter.PlayerHotbar.slots[toIndex].item == wood, "Gathered inventory -> Hotbar via existing drag/drop");
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
        File.WriteAllText(Output + "/result.txt", $"INTEGRATION-01 {(failed ? "FAIL" : "PASS")}\nChecks={checks}\nPlay sessions=1\nRuntime errors/exceptions/asserts={SessionState.GetInt(Active + ".Errors", 0)}\n{reason}\n");
        EditorApplication.update -= Tick; EditorApplication.ExitPlaymode();
    }
}
