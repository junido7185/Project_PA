using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Bounded acceptance: real WorldSandbox, existing input event subscribers and live inventory.
public static class PA_DirectGathering01BChecks
{
    const string Active = "PA.DirectGathering01B.Active";
    const string Output = "Logs/DIRECT-GATHERING-01B/Play";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static IEnumerator routine;
    static int checks, lastFrame = -1;
    static double started;
    static WorldAlphaPlayableController alpha;
    static WorldGameplayAdapterService adapter;
    static Inventory inventory;

    [InitializeOnLoadMethod]
    static void Resume() { if (SessionState.GetBool(Active, false)) Subscribe(); }

    public static void Prepare()
    {
        Directory.CreateDirectory("Logs/DIRECT-GATHERING-01B");
        CreateItem("Net", 2003, ToolType.Net, ItemCategory.Tool, 1, 0);
        CreateItem("Butterfly", 2004, ToolType.None, ItemCategory.Raw, 20, 12);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        var items = Resources.LoadAll<Item>("Items");
        foreach (var name in new[] { "Net", "Butterfly" })
        {
            var item = Resources.Load<Item>("Items/Item_" + name);
            if (item == null || items.Count(i => i.id == item.id) != 1)
                throw new Exception("Missing or duplicate canonical item " + name);
        }
        File.WriteAllText("Logs/DIRECT-GATHERING-01B/compile.txt",
            "Unity Runtime + Editor compile PASS; dedicated validator loaded. Net/Butterfly assets imported; IDs unique.\n");
    }

    static void CreateItem(string name, int id, ToolType tool, ItemCategory category, int stack, int price)
    {
        string path = "Assets/Resources/Items/Item_" + name + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item != null)
        {
            if (item.id != id || item.toolType != tool || item.category != category)
                throw new Exception("Existing item conflicts with approved asset " + path);
            return;
        }
        item = ScriptableObject.CreateInstance<Item>();
        item.id = id; item.itemName = name; item.toolType = tool;
        item.category = category; item.maxStack = stack; item.basePrice = price;
        item.description = name == "Net" ? "Select in the hotbar, face a butterfly and press Space." : "A butterfly caught in the meadow.";
        AssetDatabase.CreateAsset(item, path);
    }

    public static void Run()
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/checks.txt", "Input events invoke existing subscribers; no direct hit mutation.\n");
        SessionState.SetBool(Active, true);
        SessionState.SetBool(Active + ".Failed", false);
        SessionState.SetInt(Active + ".Errors", 0);
        Subscribe();
        EditorSceneManager.OpenScene("Assets/Scenes/WorldSandbox.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    static void Subscribe()
    {
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Application.logMessageReceived -= Log; Application.logMessageReceived += Log;
        EditorApplication.playModeStateChanged -= Mode; EditorApplication.playModeStateChanged += Mode;
    }

    static void Mode(PlayModeStateChange mode)
    {
        if (mode != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Active, false)) return;
        bool failed = SessionState.GetBool(Active + ".Failed", false) || SessionState.GetInt(Active + ".Errors", 0) != 0;
        File.AppendAllText(Output + "/result.txt", "Post-Play runtime errors/exceptions/asserts=" + SessionState.GetInt(Active + ".Errors", 0) + "\n");
        SessionState.SetBool(Active, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Log;
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
            if (EditorApplication.timeSinceStartup - started > 180) throw new Exception("Bounded Play timed out.");
            if (!routine.MoveNext()) Finish(false, "All targeted acceptance checks passed.");
        }
        catch (Exception ex) { Finish(true, ex.ToString()); }
    }

    static IEnumerator Exercise()
    {
        while ((alpha = WorldAlphaPlayableController.Instance) == null || !alpha.IsReady) yield return null;
        adapter = alpha.Adapter; inventory = adapter.PlayerInventory;
        typeof(SaveManager).GetMethod("SetRepositoryForValidation", Private).Invoke(adapter.RuntimeSaveManager,
            new object[] { new LocalJsonSaveRepository(Output + "/isolated-save") });
        Check(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11, "D3D11 baseline");
        Check(alpha.BeginNewGame(), "Existing WorldSandbox new-game entry succeeds");
        yield return null;
        while (alpha.GetComponent<WorldNavigationService>().IsRebuilding) yield return null;
        Check(adapter.BeginDayForValidation(), "Day preparation available");
        var fish = Resources.Load<Item>("Items/Item_Fish");
        var net = Resources.Load<Item>("Items/Item_Net");
        var butterfly = Resources.Load<Item>("Items/Item_Butterfly");
        foreach (var item in new[] { net, butterfly })
        {
            Check(item != null && ItemRegistry.Instance.Find(item.id, item.itemName) == item, item.name + " resolves through ItemRegistry");
            Check(ItemRegistry.Instance.allItems.Count(i => i.id == item.id) == 1, item.name + " canonical ID unique");
        }
        Select(net);
        Check(inventory.GetSelectedItem() == net && net.toolType == ToolType.Net, "Net selected through real Hotbar input");
        Check(Object.FindFirstObjectByType<HotbarUI>() != null, "Existing HotbarUI present");
        var shore = adapter.FindDaytimeActivity("shore-forage");
        var fishing = shore.GetComponentInChildren<FishingSpot>();
        Check(fishing != null && fishing.IsDirectPlayerDemo, "Generated shore uses explicit direct FishingSpot");
        PlaceReachable(fishing, WorldGenerationAnchorKind.PondActivity);
        int before = inventory.CountItems(fish);
        var near = adapter.PlayerRoot.transform.position;
        Teleport(near + Vector3.right * 8, fishing.transform.position);
        fishing.Interact(adapter.PlayerRoot);
        Check(!fishing.IsFishing, "Remote cast rejected");
        Teleport(near, fishing.transform.position);
        Interact();
        Check(fishing.IsFishing && !fishing.BiteReady && inventory.CountItems(fish) == before, "Cast starts wait without reward");
        Interact();
        Check(inventory.CountItems(fish) == before && !fishing.BiteReady, "Early input cannot grant Fish");
        while (!fishing.BiteReady) yield return null;
        Check(inventory.CountItems(fish) == before && fishing.GetInteractPrompt().Contains("BITE"), "Real timer shows BITE and waits for player input");
        float until = Time.time + .2f;
        while (Time.time < until) yield return null;
        Check(InteractPromptUI.instance != null, "Existing interaction prompt UI available");
        var saved = FillInventory();
        int fullBefore = inventory.CountItems(fish);
        Interact();
        Check(fishing.BiteReady && fishing.IsFishing && inventory.CountItems(fish) == fullBefore &&
            DayNightShopLoopController.Instance.IsDayPrepPointAvailable(shore) && fishing.GetInteractPrompt().Contains("Bag full"),
            "Full inventory retains bite, reward and daily opportunity with clear feedback");
        RestoreInventory(saved);
        Interact();
        Check(!fishing.IsFishing && !fishing.BiteReady && inventory.CountItems(fish) == before + shore.grantCount,
            "One input after making room grants exact Fish reward through Inventory");
        fishing.Interact(adapter.PlayerRoot); Interact();
        Check(inventory.CountItems(fish) == before + shore.grantCount && !fishing.IsFishing,
            "Duplicate catch rejected by existing daily completion");
        Check(adapter.BeginDayForValidation(9, 3), "Next day available for cancellation check");
        Interact();
        Check(fishing.IsFishing, "Direct fishing becomes available next day");
        fishing.PrepareForStateRestore();
        until = Time.time + 1.5f; while (Time.time < until) yield return null;
        Check(!fishing.IsFishing && !fishing.BiteReady && inventory.CountItems(fish) == before + shore.grantCount,
            "State restore cancels pending bite without reward");

        var bugs = adapter.RuntimeRoot.GetComponentsInChildren<BugCritter>();
        Check(bugs.Length == 3, "Three visible Meadow demo critters composed");
        var bug = bugs[0];
        Check(bugs.All(b => b.GetComponentsInChildren<Renderer>().Any(r => r.enabled)), "Bug wing visuals present");
        var initial = bug.transform.position;
        until = Time.time + .8f;
        bool stayedBounded = true;
        while (Time.time < until)
        {
            stayedBounded &= Vector3.ProjectOnPlane(bug.transform.position - bug.Home, Vector3.up).magnitude <= BugCritter.RoamRadius * 1.415f;
            yield return null;
        }
        Check(stayedBounded, "Critter remains inside bounded idle area");
        Check((bug.transform.position-initial).sqrMagnitude > .00001f, "Critter moves during actual Play");
        PlaceReachable(bug, WorldGenerationAnchorKind.MeadowActivity);
        int bugsBefore = inventory.CountItems(butterfly);
        Select(null); Interact();
        Check(!bug.Captured && inventory.CountItems(butterfly) == bugsBefore, "No tool rejects bug capture");
        Select(Resources.Load<Item>("Items/Item_Axe")); Interact();
        Check(!bug.Captured && inventory.CountItems(butterfly) == bugsBefore, "Wrong tool rejects bug capture");
        Select(net);
        near = adapter.PlayerRoot.transform.position;
        Teleport(near + Vector3.right * 8, bug.transform.position); bug.Interact(adapter.PlayerRoot);
        Check(!bug.Captured, "Remote capture rejected");
        Teleport(near, near - (bug.transform.position - near)); bug.Interact(adapter.PlayerRoot);
        Check(!bug.Captured, "Behind-player capture rejected");
        Teleport(near, bug.transform.position);
        saved = FillInventory();
        Interact();
        Check(!bug.Captured && inventory.CountItems(butterfly) == 0 && bug.GetInteractPrompt().Contains("Bag full") &&
            bug.GetComponentsInChildren<Renderer>().Any(r => r.enabled), "Full inventory preserves visible critter and capture opportunity");
        RestoreInventory(saved);
        // Exercise synchronous Inventory callback reentry as well as later repeat input.
        Inventory.OnItemChanged reentry = () => bug.Interact(adapter.PlayerRoot);
        inventory.onItemChangedCallback += reentry;
        Interact();
        inventory.onItemChangedCallback -= reentry;
        Check(bug.Captured && inventory.CountItems(butterfly) == bugsBefore + 1, "Valid Net capture grants exactly one Butterfly, including callback reentry");
        bug.Interact(adapter.PlayerRoot); Interact();
        Check(inventory.CountItems(butterfly) == bugsBefore + 1, "Duplicate bug capture rejected");
        Check(bug.GetComponentsInChildren<Renderer>().All(r => !r.enabled) &&
            bug.GetComponentsInChildren<Collider>().All(c => !c.enabled), "Successful capture depletes visuals and colliders");

        // An unconfigured FishingSpot must retain its original automatic daily completion.
        Check(adapter.BeginDayForValidation(9, 4), "Day preparation available for legacy fishing");
        var legacyRoot = new GameObject("LegacyFishingCompatibility");
        var point = legacyRoot.AddComponent<DaytimeStockPrepPoint>();
        point.Configure("legacy-fishing-01b", "Items/Item_Fish", 2, "Legacy fishing");
        var legacy = legacyRoot.AddComponent<FishingSpot>(); legacy.Configure(point);
        before = inventory.CountItems(fish);
        Check(!legacy.IsDirectPlayerDemo, "Legacy FishingSpot default mode preserved");
        legacy.Interact(adapter.PlayerRoot);
        Check(legacy.IsFishing, "Legacy cast starts");
        while (legacy.IsFishing) yield return null;
        Check(inventory.CountItems(fish) == before + 2, "Legacy timer auto-grants Fish x2 without second input");
        legacy.Interact(adapter.PlayerRoot);
        Check(!legacy.IsFishing && inventory.CountItems(fish) == before + 2, "Legacy same-day duplicate rejected");
        Object.Destroy(legacyRoot);
        yield return null;
        Check(SessionState.GetInt(Active + ".Errors", 0) == 0, "Runtime errors/exceptions/asserts = 0");
    }

    static ItemInstance[] FillInventory()
    {
        var saved = inventory.slots.Select(s => s.instance).ToArray();
        var filler = Resources.Load<Item>("Items/Item_Carrot");
        foreach (var slot in inventory.slots) slot.Set(filler, filler.maxStack);
        return saved;
    }
    static void RestoreInventory(ItemInstance[] saved)
    {
        for (int i = 0; i < saved.Length; i++) inventory.slots[i].SetInstance(saved[i]);
        inventory.RefreshAllUI();
    }

    static void PlaceReachable(Component target, WorldGenerationAnchorKind kind)
    {
        var generated = WorldPersistenceService.Instance.ActiveGeneratedWorld;
        Check(generated.TryGetAnchor(kind, out var anchor), kind + " exists");
        Check(alpha.Grid.WorldToCell(target.transform.position, out var targetCell) &&
            WorldCellReachability.CanReachAll(alpha.Grid, anchor.Coordinate, new[] { targetCell }, null, null, out _),
            target.GetType().Name + " generated activity reachable from " + kind);
        for (int i = 0; i < 24; i++)
        {
            Vector3 offset = Quaternion.Euler(0, 45 * (i % 8), 0) * Vector3.forward * (.8f + .5f * (i / 8));
            Vector3 near = target.transform.position + offset;
            if (!alpha.Grid.WorldToCell(near, out var cell) || !alpha.Grid.TryGetCell(cell, out var data) || !data.IsWalkable || data.HasWater) continue;
            alpha.Grid.CellToWorld(cell, out var ground); near.y = ground.y + .05f;
            Teleport(near, target.transform.position);
            object[] args = { null, default(RaycastHit) };
            bool found = (bool)typeof(PlayerInteraction).GetMethod("TryFindInteractable", Private).Invoke(adapter.PlayerInteraction, args);
            if (found && ReferenceEquals(args[0], target))
            { Check(true, target.GetType().Name + " selected through existing PlayerInteraction from walkable ground"); return; }
        }
        throw new Exception("No selectable nearby position for " + target.name);
    }

    static void Teleport(Vector3 position, Vector3 target)
    {
        var player = adapter.PlayerRoot;
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false; player.transform.position = position; controller.enabled = true;
        player.GetComponent<PlayerController>().ResetMotionAfterTeleport();
        player.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(target - position, Vector3.up));
        Physics.SyncTransforms();
    }
    static void Select(Item item)
    {
        int index = adapter.PlayerHotbar.slots.FindIndex(slot => item == null ? slot.IsEmpty : slot.item == item);
        Check(index >= 0, (item == null ? "Empty slot" : item.itemName) + " is available in actual Hotbar");
        var callback = (Action<int>)typeof(PlayerInputHandler).GetField("OnHotbarDirectSelect", Private).GetValue(PlayerInputHandler.Instance);
        Check(callback != null, "Hotbar selection input has live subscribers"); callback(index);
    }
    static void Interact()
    {
        var callback = (Action)typeof(PlayerInputHandler).GetField("OnInteractPressed", Private).GetValue(PlayerInputHandler.Instance);
        Check(callback != null, "Interaction input has live subscribers"); callback();
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
        File.WriteAllText(Output + "/result.txt", $"DIRECT-GATHERING-01B {(failed ? "FAIL" : "PASS")}\nChecks={checks}\nPlay sessions=1\nRuntime errors/exceptions/asserts={SessionState.GetInt(Active + ".Errors", 0)}\n{reason}\n");
        EditorApplication.update -= Tick;
        EditorApplication.ExitPlaymode();
    }
}
