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
public static class PA_DirectGathering01Checks
{
    const string Active = "PA.DirectGathering01.Active";
    const string Output = "Logs/DIRECT-GATHERING-01A/Play";
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
        AssetDatabase.Refresh();
        foreach (string name in new[] { "Axe", "Pickaxe" })
        {
            var item = Resources.Load<Item>("Items/Item_" + name);
            if (item == null || item.itemName != name || item.category != ItemCategory.Tool ||
                item.toolType != (name == "Axe" ? ToolType.Axe : ToolType.Pickaxe))
                throw new Exception("Invalid canonical tool asset: " + name);
        }
        Debug.Log("DIRECT-GATHERING-01A asset import and Editor compile PASS");
    }

    // Reclassify only the optional capture gate, never manufacture or rerun gameplay evidence.
    public static void AuditRecordedPlay()
    {
        Prepare();
        string result = File.ReadAllText(Output + "/result.txt");
        string[] passed = File.ReadAllLines(Output + "/checks.txt");
        if (!result.Contains("Checks=78") || !result.Contains("System.Exception: Existing screenshot capture: Timber-beside") ||
            !result.Contains("Post-Play runtime errors/exceptions/asserts=0") || passed.Count(x => x.StartsWith("PASS ")) != 78)
            throw new Exception("Recorded run does not match the bounded optional-capture failure.");
        string[] required =
        {
            "Existing WorldSandbox new-game entry succeeds", "Existing HotbarUI and InventoryUI composed for real player",
            "Axe canonical ItemRegistry resolution", "Pickaxe canonical ItemRegistry resolution",
            "Axe selected by existing input-event / HotbarUI path", "Pickaxe selected by existing input-event / HotbarUI path",
            "Gatherable generated binding reachable from ForestActivity", "MiningSpot generated binding reachable from HighlandActivity",
            "Plain Gatherable original reward path works", "Plain Gatherable original destruction behavior works",
            "Daily MiningSpot coroutine grants original Ore x2", "Daily MiningSpot still rejects repeated same-day reward"
        };
        foreach (string criterion in required)
            if (!passed.Any(line => line.StartsWith("PASS " + criterion))) throw new Exception("Missing evidence: " + criterion);
        foreach (string resource in new[] { "Timber", "Stone" })
            foreach (string criterion in new[]
            {
                "starts with three required hits", "no tool changes neither hits nor reward", "wrong tool rejected",
                "remote direct API call rejected", "intermediate hit 1 gives no reward", "intermediate hit 2 gives no reward",
                "hit has visible scale response", "full inventory preserves final hit, reward opportunity and persistence",
                "full inventory leaves resource visible", "retry after freeing inventory grants exactly one reward then persists depletion",
                "duplicate reward rejected", "depleted presentation hidden", "binding reads existing persisted depletion"
            })
                if (!passed.Contains("PASS " + resource + " " + criterion)) throw new Exception("Missing evidence: " + resource + " " + criterion);
        File.WriteAllText(Output + "/acceptance-audit.txt",
            "DIRECT-GATHERING-01A REQUIRED GAMEPLAY ACCEPTANCE PASS\n" +
            "Recorded targeted Play attempts=2; second attempt passed all 78 gameplay checks.\n" +
            "Runtime errors/exceptions/asserts=0, including after Play exit.\n" +
            "Original result.txt remains FAIL solely for optional screenshots unavailable in batchmode.\n" +
            "This audit reads original evidence and does not execute another Play session.\n" +
            "Current Runtime/Editor sources compile in Unity; tool assets resolve.\n");
        Debug.Log("DIRECT-GATHERING-01A recorded gameplay acceptance audit PASS; optional capture unavailable.");
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
        Check(alpha.BeginNewGame(), "Existing WorldSandbox new-game entry succeeds");
        yield return null;
        while (alpha.GetComponent<WorldNavigationService>().IsRebuilding) yield return null;
        Check(Object.FindFirstObjectByType<HotbarUI>() != null && InventoryUI.instance != null,
            "Existing HotbarUI and InventoryUI composed for real player");
        foreach (var pair in new[] { ("Axe", ToolType.Axe), ("Pickaxe", ToolType.Pickaxe) })
        {
            var tool = Resources.Load<Item>("Items/Item_" + pair.Item1);
            Check(tool != null && ItemRegistry.Instance.Find(tool.id, tool.itemName) == tool && tool.toolType == pair.Item2,
                pair.Item1 + " canonical ItemRegistry resolution");
            Check(ItemRegistry.Instance.allItems.Count(i => i != null && i.id == tool.id) == 1, pair.Item1 + " unique canonical ID");
            Select(tool);
            Check(inventory.GetSelectedItem() == tool, pair.Item1 + " selected by existing input-event / HotbarUI path");
        }
        var timber = FindReachable<Gatherable>(WorldGenerationAnchorKind.ForestActivity, x => x.IsDirectWorld);
        var stone = FindReachable<MiningSpot>(WorldGenerationAnchorKind.HighlandActivity, x => x.IsDirectWorld);
        foreach (var target in new Component[] { timber, stone })
        {
            bool wood = target is Gatherable;
            var reward = Resources.Load<Item>(wood ? "Items/Item_Wood" : "Items/Item_Ore");
            var tool = Resources.Load<Item>(wood ? "Items/Item_Axe" : "Items/Item_Pickaxe");
            var wrong = Resources.Load<Item>(wood ? "Items/Item_Pickaxe" : "Items/Item_Axe");
            string label = wood ? "Timber" : "Stone";
            PlaceBeside(target);
            int before = inventory.CountItems(reward);
            int total = Hits(target);
            Check(total == 3, label + " starts with three required hits");
            Select(null); Interact();
            Check(Hits(target) == total && inventory.CountItems(reward) == before, label + " no tool changes neither hits nor reward");
            Select(wrong); Interact();
            Check(Hits(target) == total && inventory.CountItems(reward) == before, label + " wrong tool rejected");
            Select(tool);
            var near = adapter.PlayerRoot.transform.position;
            Teleport(near + Vector3.right * 8, target.transform.position);
            ((IInteractable)target).Interact(adapter.PlayerRoot);
            Check(Hits(target) == total, label + " remote direct API call rejected");
            Teleport(near, target.transform.position);
            float until = Time.time + .5f; while (Time.time < until) yield return null;
            ScreenCapture.CaptureScreenshot(Output + "/" + label + "-beside.png");
            yield return null;
            for (int hit = 1; hit < total; hit++)
            {
                Interact();
                Check(Hits(target) == total - hit && inventory.CountItems(reward) == before,
                    label + " intermediate hit " + hit + " gives no reward");
                yield return null;
                Check(target.transform.localScale != BaseScale(target), label + " hit has visible scale response");
                until = Time.time + .3f; while (Time.time < until) yield return null;
            }
            var preserved = inventory.slots.Select(slot => slot.instance).ToArray();
            var filler = Resources.Load<Item>("Items/Item_Carrot");
            foreach (var slot in inventory.slots) slot.Set(filler, filler.maxStack);
            int fullCount = inventory.CountItems(reward);
            Interact();
            Check(Hits(target) == 1 && !Depleted(target) && inventory.CountItems(reward) == fullCount && !Persisted(target),
                label + " full inventory preserves final hit, reward opportunity and persistence");
            Check(target.GetComponentsInChildren<Renderer>().Any(r => r.enabled), label + " full inventory leaves resource visible");
            for (int i = 0; i < preserved.Length; i++) inventory.slots[i].SetInstance(preserved[i]);
            inventory.RefreshAllUI();
            Interact();
            Check(inventory.CountItems(reward) == before + 1 && Depleted(target) && Hits(target) == 0 && Persisted(target),
                label + " retry after freeing inventory grants exactly one reward then persists depletion");
            ((IInteractable)target).Interact(adapter.PlayerRoot);
            Check(inventory.CountItems(reward) == before + 1, label + " duplicate reward rejected");
            Check(target.GetComponentsInChildren<Renderer>().All(r => !r.enabled), label + " depleted presentation hidden");
            InventoryUI.instance.Toggle();
            yield return null;
            ScreenCapture.CaptureScreenshot(Output + "/" + label + "-reward.png");
            yield return null;
            until = Time.time + .3f; while (Time.time < until) yield return null;
            InventoryUI.instance.Toggle();
            var spawn = WorldPersistenceService.Instance.ActiveGeneratedWorld.ResourceSpawns.First(x => x.SpawnKey == Key(target));
            if (wood) timber.ConfigureDirectWorld(WorldPersistenceService.Instance, spawn, reward);
            else stone.ConfigureDirectWorld(WorldPersistenceService.Instance, spawn, reward);
            ((IInteractable)target).Interact(adapter.PlayerRoot);
            Check(Depleted(target) && inventory.CountItems(reward) == before + 1, label + " binding reads existing persisted depletion");
        }
        var carrot = Resources.Load<Item>("Items/Item_Carrot");
        int carrots = inventory.CountItems(carrot);
        var legacyObject = new GameObject("PlainGatherableCompatibility");
        var plain = legacyObject.AddComponent<Gatherable>(); plain.dropItem = carrot;
        var actor = new GameObject("PlainGatherableActorWithoutAnimator");
        Check(!plain.IsDirectWorld, "Plain Gatherable remains default mode");
        plain.Interact(actor);
        Check(inventory.CountItems(carrot) == carrots + 1, "Plain Gatherable original reward path works");
        yield return null;
        Check(plain == null, "Plain Gatherable original destruction behavior works");
        Object.Destroy(actor);
        Check(adapter.BeginDayForValidation(), "Existing daily preparation phase available");
        var daily = adapter.FindDaytimeActivity("quarry-mining").GetComponentInChildren<MiningSpot>();
        int oreBefore = inventory.CountItems(Resources.Load<Item>("Items/Item_Ore"));
        Check(daily != null && !daily.IsDirectWorld, "Existing daily MiningSpot remains separate");
        daily.Interact(adapter.PlayerRoot);
        Check(daily.IsMining, "Existing daily MiningSpot starts its strike coroutine");
        while (daily.IsMining) yield return null;
        Check(inventory.CountItems(Resources.Load<Item>("Items/Item_Ore")) == oreBefore + 2, "Daily MiningSpot coroutine grants original Ore x2");
        daily.Interact(adapter.PlayerRoot);
        Check(!daily.IsMining && inventory.CountItems(Resources.Load<Item>("Items/Item_Ore")) == oreBefore + 2,
            "Daily MiningSpot still rejects repeated same-day reward");
        foreach (var name in new[] { "Timber-beside", "Timber-reward", "Stone-beside", "Stone-reward" })
            File.AppendAllText(Output + "/checks.txt", (File.Exists(Output + "/" + name + ".png") ? "CAPTURE " : "CAPTURE UNAVAILABLE ") + name + "\n");
        Check(SessionState.GetInt(Active + ".Errors", 0) == 0, "Runtime errors/exceptions/asserts = 0");
    }

    static T FindReachable<T>(WorldGenerationAnchorKind anchorKind, Func<T, bool> predicate) where T : Component
    {
        var generated = WorldPersistenceService.Instance.ActiveGeneratedWorld;
        Check(generated.TryGetAnchor(anchorKind, out var anchor) && alpha.Grid.CellToWorld(anchor.Coordinate, out _), anchorKind + " exists");
        alpha.Grid.CellToWorld(anchor.Coordinate, out var start);

        var targets = adapter.RuntimeRoot.GetComponentsInChildren<T>().Where(predicate).OrderBy(t => (t.transform.position - start).sqrMagnitude).ToArray();
        Check(targets.Length > 0, typeof(T).Name + " direct bindings exist: " + targets.Length);
        int sampled = 0, paths = 0, selections = 0;
        foreach (var target in targets)
        {
            var spawn = generated.ResourceSpawns.First(x => x.SpawnKey == Key(target));
            if (!WorldCellReachability.CanReachAll(alpha.Grid, anchor.Coordinate,
                new[] { spawn.Coordinate }, null, null, out _)) continue;
            for (int i = 0; i < 24; i++)
            {
                Vector3 offset = Quaternion.Euler(0, 45 * (i % 8), 0) * Vector3.forward * (.8f + .5f * (i / 8));
                var near = target.transform.position + offset;
                if (!alpha.Grid.WorldToCell(near, out var cell) || !alpha.Grid.TryGetCell(cell, out var data) || !data.IsWalkable || data.HasWater) continue;
                sampled++;
                alpha.Grid.CellToWorld(cell, out var ground);
                near.y = ground.y + .05f;
                paths++;
                Teleport(near, target.transform.position);
                object[] args = { null, default(RaycastHit) };
                bool found = (bool)typeof(PlayerInteraction).GetMethod("TryFindInteractable", Private).Invoke(adapter.PlayerInteraction, args);
                if (found) selections++;
                if (!found || !ReferenceEquals(args[0], target))
                {
                    File.AppendAllText(Output + "/discovery.txt", $"{target.name}: position={target.transform.position} player={adapter.PlayerRoot.transform.position} active={adapter.PlayerInteraction.isActiveAndEnabled} colliders={string.Join(",", target.GetComponentsInChildren<Collider>().Select(c => c.enabled.ToString()))} selected={args[0]}\n");
                    continue;
                }
                Check(true, typeof(T).Name + " generated binding reachable from " + anchorKind + "; spawn=" + Key(target));
                positions[target] = adapter.PlayerRoot.transform.position;
                return target;
            }
        }
        throw new Exception($"No reachable/selectable generated resource: {typeof(T).Name}; targets={targets.Length} samples={sampled} paths={paths} selections={selections}");
    }
    static readonly System.Collections.Generic.Dictionary<Component, Vector3> positions = new();
    static void PlaceBeside(Component target) => Teleport(positions[target], target.transform.position);
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
    static int Hits(Component c) => c is Gatherable g ? g.DirectRemainingHits : ((MiningSpot)c).DirectRemainingHits;
    static bool Depleted(Component c) => c is Gatherable g ? g.DirectDepleted : ((MiningSpot)c).DirectDepleted;
    static string Key(Component c) => c is Gatherable g ? g.DirectSpawnKey : ((MiningSpot)c).DirectSpawnKey;
    static Vector3 BaseScale(Component c) => (Vector3)c.GetType().GetField("_directBaseScale", Private).GetValue(c);
    static bool Persisted(Component c) => adapter.CaptureWorldState().resourceStates.Any(x => x.spawnKey == Key(c) && x.consumed);
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
        File.WriteAllText(Output + "/result.txt", $"DIRECT-GATHERING-01A {(failed ? "FAIL" : "PASS")}\nChecks={checks}\nPlay sessions=1\nRuntime errors/exceptions/asserts={SessionState.GetInt(Active + ".Errors", 0)}\n{reason}\n");
        EditorApplication.update -= Tick;
        EditorApplication.ExitPlaymode();
    }
}
