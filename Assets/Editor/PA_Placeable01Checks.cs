using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PA_Placeable01Checks
{
    const string Active = "PA.Placeable01.Active";
    const string Output = "Logs/PLACEABLE-01/Play";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static IEnumerator routine;
    static int checks, lastFrame = -1;
    static double started;
    static WorldAlphaPlayableController alpha;
    static WorldGameplayAdapterService adapter;
    static Inventory inventory;
    static WorldHotbarPlacementController direct;
    static WorldBuildingPlacementService placement;

    public static void Prepare()
    {
        var hubData = Resources.Load<BuildingData>(WorldPlaceableKitCatalog.Hub.BuildingResourcePath);
        var shop = Resources.Load<Item>(WorldPlaceableKitCatalog.ShopItemPath);
        if (hubData == null || hubData.prefab == null || shop == null || shop.id != 1001)
            throw new Exception("Existing Hub/B01 assets unavailable.");
        string path = "Assets/Resources/Items/Item_ManagementHubKit.asset";
        var hub = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (hub == null)
        {
            if (Resources.LoadAll<Item>("Items").Any(i => i.id == 2005 || i.buildingToBuild == hubData))
                throw new Exception("Equivalent Hub item or ID already exists.");
            hub = ScriptableObject.CreateInstance<Item>();
            hub.id = 2005; hub.itemName = "Management Hub Kit";
            hub.description = "Select in the hotbar and press Space to place the management hub.";
            hub.toolType = ToolType.Building; hub.category = ItemCategory.Tool;
            hub.maxStack = 99; hub.basePrice = 0; hub.buildingToBuild = hubData;
            AssetDatabase.CreateAsset(hub, path);
        }
        shop.itemName = "Shop Kit";
        shop.description = "Select in the hotbar and press Space to place the market stall.";
        EditorUtility.SetDirty(shop);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        foreach (var item in new[] { hub, shop })
            if (!WorldPlaceableKitCatalog.TryKit(item, out _, out _) || Resources.LoadAll<Item>("Items").Count(i => i.id == item.id) != 1)
                throw new Exception("Canonical kit mapping or ID invalid.");
        Directory.CreateDirectory("Logs/PLACEABLE-01");
        File.WriteAllText("Logs/PLACEABLE-01/compile.txt", "Unity Runtime + Editor compile PASS; dedicated validator loaded; Hub/Shop assets imported.\n");
    }

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
        Check(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11, "D3D11 baseline");
        Check(alpha.BeginNewGame(), "WorldSandbox BeginNewGame");
        yield return null;
        while (alpha.GetComponent<WorldNavigationService>().IsRebuilding) yield return null;
        Check(direct != null && adapter.PlayerInteraction.enabled, "Direct placement composed on existing player");
        Check(adapter.BeginDayForValidation(), "Existing day preparation available");
        var hub = Resources.Load<Item>(WorldPlaceableKitCatalog.HubItemPath);
        var shop = Resources.Load<Item>(WorldPlaceableKitCatalog.ShopItemPath);
        foreach (var kit in new[] { hub, shop })
        {
            Check(kit != null && ItemRegistry.Instance.Find(kit.id, kit.itemName) == kit, kit.name + " resolves via ItemRegistry");
            Check(inventory.CountItems(kit) == 1, kit.name + " bootstrap grants exactly one");
            Check(WorldPlaceableKitCatalog.TryKit(kit, out var id, out var def), "Explicit canonical building reference");
            placement.RegisterDefinition(id, def);
            Select(kit);
            Check(inventory.GetSelectedItem() == kit, "Existing Hotbar selects " + kit.itemName);
            FindClearPlayerPose(id, def);
            Input("OnInteractPressed");
            Check(direct.IsPlacing && placement.HasPreview && placement.PreviewGhost != null, kit.itemName + " Space enters real preview");
            Check(placement.LastResult.Succeeded, "Near-player preview uses placement validity");
            Check(Vector3.Distance(placement.PreviewGhost.transform.position, adapter.PlayerRoot.transform.position) < 10f, "Preview within 10m of player");
            Check(inventory.CountItems(kit) == 1, "Preview consumes zero");
            var initial = placement.PreviewGhost.transform.position;
            Teleport(adapter.PlayerRoot.transform.position + Vector3.right * alpha.Grid.Definition.CellSize);
            direct.RefreshPreview();
            Check(Vector3.Distance(initial, placement.PreviewGhost.transform.position) > 1f, "Preview follows player movement");
            Input("OnBuildRotate");
            Check(placement.LastResult.QuarterTurns == 1, "Existing R input rotates footprint");
            typeof(PlayerInputHandler).GetMethod("CancelPlacementOrPause", Private).Invoke(PlayerInputHandler.Instance, null);
            Check(!direct.IsPlacing && !placement.HasPreview && inventory.CountItems(kit) == 1, "Esc cancels with zero consumption");
            Check(PauseManager.Instance == null || !PauseManager.Instance.IsPaused, "Esc cancellation does not pause");
            CheckControl("cancel");
            var movement = CheckMovement();
            while (movement.MoveNext()) yield return movement.Current;

            // Invalid terrain and out-of-bounds are evaluated by the real authority.
            Teleport(new Vector3(-1000, 0, -1000));
            Input("OnInteractPressed");
            Check(direct.IsPlacing && !placement.LastResult.Succeeded, "Invalid preview retained");
            Input("OnBuildPlace");
            Check(direct.IsPlacing && inventory.CountItems(kit) == 1 && !placement.TryGetPlacement(id, out _), "Invalid confirm consumes zero and places nothing");
            direct.Cancel();

            FindClearPlayerPose(id, def);
            Input("OnInteractPressed");
            var data = kit.buildingToBuild;
            var prefab = data.prefab;
            try
            {
                data.prefab = null;
                Input("OnBuildPlace");
                Check(direct.IsPlacing && placement.LastResult.Failure == WorldBuildingPlacementFailure.MissingPrefab &&
                    inventory.CountItems(kit) == 1 && !placement.TryGetPlacement(id, out _), "Service failure consumes zero and allows retry");
            }
            finally { data.prefab = prefab; }
            Input("OnBuildPlace");
            Check(!direct.IsPlacing && !placement.HasPreview && inventory.CountItems(kit) == 0, "Successful confirm consumes exactly one selected kit");
            Check(placement.TryGetPlacement(id, out var built) && built.Definition.StableId == def.StableId && built.GameObject.activeInHierarchy,
                "Existing building definition instantiated and registered");
            Input("OnBuildPlace");
            Check(inventory.CountItems(kit) == 0, "Repeated confirm cannot consume again");
            yield return null;
            CheckControl("confirm");
            movement = CheckMovement();
            while (movement.MoveNext()) yield return movement.Current;
            if (kit == shop)
            {
                var actualShop = built.GameObject.GetComponentInChildren<Shop>();
                Check(actualShop != null && actualShop.Slots.Count > 0 && built.GameObject.GetComponentsInChildren<ShopSlot>().Length > 0,
                    "Placed B01 retains Shop and registered ShopSlots for existing customers");
            }
            // Duplicate identity follows authority rules and cannot consume a second kit.
            Check(adapter.PlayerHotbar.AddItem(kit), "Second kit test fixture fits existing Hotbar");
            Select(kit); Input("OnInteractPressed"); Input("OnBuildPlace");
            Check(placement.LastResult.Failure == WorldBuildingPlacementFailure.DuplicateInstance && inventory.CountItems(kit) == 1,
                "Duplicate building rejected without consumption");
            direct.Cancel();
            inventory.RemoveItems(kit, 1);
            while (alpha.GetComponent<WorldNavigationService>().IsRebuilding) yield return null;
        }
        Select(Resources.Load<Item>("Items/Item_Axe"));
        Check(!direct.TryUseSelected() && !direct.IsPlacing && inventory.GetSelectedItem().toolType == ToolType.Axe,
            "Placement glue passes non-kit tools to unchanged PlayerInteraction gathering path");
        Check(Object.FindObjectsByType<WorldBuildingPlacementService>(FindObjectsSortMode.None).Length == 1, "One placement authority");

        var persistence = WorldPersistenceService.Instance;
        var saved = persistence.CaptureState(adapter.PlayerRoot.transform.position, null);
        Check(saved.placedBuildings.Count == 2, "Existing world persistence captures both buildings");
        string json = JsonUtility.ToJson(saved);
        File.WriteAllText(Output + "/world-state.json", json);
        var roundtrip = JsonUtility.FromJson<WorldStateSaveData>(json);
        Check(persistence.TryRestore(roundtrip, out _, out _, out var reason), "Existing world restore accepts both kit definitions: " + reason);
        yield return null;
        while (alpha.GetComponent<WorldNavigationService>().IsRebuilding) yield return null;
        Check(placement.RegisteredCount == 2 && placement.TryGetPlacement(WorldPlaceableKitCatalog.HubId, out _) &&
            placement.TryGetPlacement(WorldPlaceableKitCatalog.ShopId, out _), "Restore reuses both registered identities");
        Check(inventory.CountItems(hub) == 0 && inventory.CountItems(shop) == 0, "World rebind does not regrant consumed kits");
        var malformed = JsonUtility.FromJson<WorldStateSaveData>(json);
        malformed.placedBuildings[1].instanceId = malformed.placedBuildings[0].instanceId;
        Check(!persistence.TryRestore(malformed, out _, out _, out _) && placement.RegisteredCount == 2,
            "Duplicate persisted identity rejected before world mutation");
        Select(Resources.Load<Item>("Items/Item_Net"));
        Check(inventory.GetSelectedItem().toolType == ToolType.Net && !direct.TryUseSelected(), "Hotbar/Net remain usable after placement and restore");
        yield return null;
        Check(SessionState.GetInt(Active + ".Errors", 0) == 0, "Runtime errors/exceptions/asserts zero");
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
        File.WriteAllText(Output + "/result.txt", $"PLACEABLE-01 {(failed ? "FAIL" : "PASS")}\nChecks={checks}\nPlay sessions=1\nRuntime errors/exceptions/asserts={SessionState.GetInt(Active + ".Errors", 0)}\n{reason}\n");
        EditorApplication.update -= Tick; EditorApplication.ExitPlaymode();
    }
}
