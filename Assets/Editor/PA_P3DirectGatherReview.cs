using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// P3_DIRECT_GATHER — 보급·도구·직접 채집 Play 검수 (WorldSandbox Demo256).
// INPUT = 가상 InputSystem 키보드/마우스(E·X·1·2·휠). FIXTURE = 직행 진입·대상 앞 이동(teleport)·가방 채우기/비우기.
// 채집 권위(Gatherable/MiningSpot/PickupItem/Inventory/WorldPersistence)는 실제 입력 경로로만 호출한다.
// 증거는 실행마다 Logs/VisualQA/<stamp>-P3_DIRECT_GATHER/ 에만 쓴다.
public static class PA_P3DirectGatherReview
{
    const string Active = "PA.P3Gather.Active";
    const string OutKey = "PA.P3Gather.Out";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static bool running, finished;
    static int checks, failures, errors;
    static string problem;
    static Keyboard keyboard;
    static Mouse mouse;
    static InputDevice[] audioNativeDevices;

    static void RestoreAudioNativeInput()
    {
        if (audioNativeDevices == null) return;
        foreach (var device in audioNativeDevices)
            if (device != null && device.added && !device.enabled) InputSystem.EnableDevice(device);
        audioNativeDevices = null;
    }

    static void AudioReviewPlayState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode) RestoreAudioNativeInput();
    }

    static string OutDir => SessionState.GetString(OutKey, "Logs/VisualQA/unknown-P3_DIRECT_GATHER");

    const string ModeKey = "PA.P3Gather.Mode";
    const string AmbienceReviewKey = "PA.P11.AmbienceReview";
    const string AmbienceExpectedKey = "PA.P11.AmbienceExpected";
    static bool AmbienceReview => SessionState.GetBool(AmbienceReviewKey, false);
    static string Mode => SessionState.GetString(ModeKey, "gather");

    [MenuItem("Project PA/Validation/Demo P3 Direct Gather Review")]
    public static void Run() => Start("gather", "P3_DIRECT_GATHER");

    // P4 — 같은 직행·보급 준비 뒤 기본 낚시만 검수한다(공용 입력/증거 도우미 재사용).
    [MenuItem("Project PA/Validation/Demo P4 Fishing Review")]
    public static void RunFishing() => Start("fishing", "P4_FISHING");

    // P5 — 같은 보급 뒤 초원 나비 포획만 검수한다.
    [MenuItem("Project PA/Validation/Demo P5 Bug Catch Review")]
    public static void RunBugs() => Start("bugs", "P5_BUG_CATCH");

    // P6 — 같은 보급 뒤 배치 문법·작업대 제작·해금 업그레이드·판재 진열·실내 보관함을 검수한다.
    [MenuItem("Project PA/Validation/Demo P6 Craft Place Storage Review")]
    public static void RunCraft() => Start("craft", "P6_CRAFT_PLACE_STORAGE");

    [MenuItem("Project PA/Validation/Demo P6 Applied UI Interactions")]
    public static void RunUiInteractions() => Start("ui", "P6_APPLIED_UI");

    // P7 — 같은 보급 뒤 동행의 이동·집·일과·반응, Root 선택 미리보기, 도구 지급 전후 생산을 검수한다.
    [MenuItem("Project PA/Validation/Demo P7 Companion Growth Review")]
    public static void RunCompanions() => Start("companion", "P7_COMPANION_GROWTH");

    // P8 — 같은 보급 뒤 직접 얻은 상품으로 첫 밤 영업과 Report까지 검수한다.
    [MenuItem("Project PA/Validation/Demo P8 Business Report Review")]
    public static void RunBusiness() => Start("business", "P8_BUSINESS_REPORT");

    // P9 — 모션 전/후 원속 연속 촬영(프레임은 프로젝트 밖 임시 폴더, mp4는 Logs/VisualQA).
    // P10 — 섬 조사: 위에서 본 모습·랜드마크 화면·걷기 경로·디버그 잔재.
    [MenuItem("Project PA/Validation/Demo P10 Island Survey")]
    public static void RunTour() => Start("tour", "P10_ISLAND_SURVEY");

    // P11 — 타이틀 NEW GAME부터 출항 교육·동행 선택·항해·섬 도착·보급·정착·진열·가격·보관·Root·일몰·영업·Report·저장까지 한 Play에서 잇는다.
    //       저장은 Logs/VisualQA/<stamp>-P11_.../save 격리 폴더(사용자 save 미접촉). 이 save가 standalone Continue 검수의 시작점이다.
    [MenuItem("Project PA/Validation/Demo P11 Continuous Route Review")]
    public static void RunRoute() => Start("route", "P11_CONTINUOUS_ROUTE");

    [MenuItem("Project PA/Validation/Demo P11 Intro UI Review")]
    public static void RunIntro() { SessionState.SetBool(AmbienceReviewKey, false); Start("intro", "P11_INTRO_UI"); }

    [MenuItem("Project PA/Validation/Demo P11 Ambience Before")]
    public static void RunAmbienceBefore() { SessionState.SetBool(AmbienceReviewKey, true); SessionState.SetBool(AmbienceExpectedKey, false); Start("intro", "P11_AMBIENCE-before"); }
    [MenuItem("Project PA/Validation/Demo P11 Ambience After")]
    public static void RunAmbienceAfter() { SessionState.SetBool(AmbienceReviewKey, true); SessionState.SetBool(AmbienceExpectedKey, true); Start("intro", "P11_AMBIENCE-after"); }
    [MenuItem("Project PA/Validation/Demo P11 Ambience Night Targeted")]
    public static void RunAmbienceNight() { SessionState.SetBool(AmbienceReviewKey, true); SessionState.SetBool(AmbienceExpectedKey, true); Start("ambience", "P11_AMBIENCE-night"); }

    [MenuItem("Project PA/Validation/Demo P11 Audio Output Review")]
    public static void RunAudio() => Start("audio", "P11_AUDIO");

    [MenuItem("Project PA/Validation/Demo P9 Foot Cadence Before")]
    public static void RunFootBefore() { SessionState.SetString(FeelLabelKey, "before"); Start("foot", "P9_FOOT-before"); }
    [MenuItem("Project PA/Validation/Demo P9 Foot Cadence After")]
    public static void RunFootAfter() { SessionState.SetString(FeelLabelKey, "after"); Start("foot", "P9_FOOT-after"); }

    [MenuItem("Project PA/Validation/Demo P9 Character Feel Capture (before)")]
    public static void RunFeelBefore() { SessionState.SetBool(GripReviewKey, false); SessionState.SetString(FeelLabelKey, "before"); Start("feel", "P9_CHARACTER_FEEL-before"); }
    [MenuItem("Project PA/Validation/Demo P9 Character Feel Capture (after)")]
    public static void RunFeelAfter() { SessionState.SetBool(GripReviewKey, false); SessionState.SetString(FeelLabelKey, "after"); Start("feel", "P9_CHARACTER_FEEL-after"); }
    [MenuItem("Project PA/Validation/Demo P9 Support Grip Capture")]
    public static void RunSupportGrip() { SessionState.SetBool(GripReviewKey, true); SessionState.SetString(FeelLabelKey, "after"); Start("feel", "P9_CHARACTER_FEEL-support-grip"); }

    static void Start(string mode, string ticket)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run from Edit mode.");
        SessionState.SetString(ModeKey, mode);
        string outDir = "Logs/VisualQA/" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + ticket;
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, "result.txt"),
            "P3 direct gather review (supply, hotbar, axe/pickaxe contact, drop + E pickup, durability, full bag, rapid input).\n" +
            "FIXTURE = QA setup, INPUT = injected InputSystem key/wheel, other lines = observed product behavior.\n");
        SessionState.SetString(OutKey, outDir);
        SessionState.SetBool(Active, true);
        bool route = mode == "route" || mode == "intro";
        if (route || mode == "ui" || mode == "audio" || mode == "foot" || AmbienceReview)
        {
            string saveRoot = Path.GetFullPath(Path.Combine(outDir, "save"));
            Directory.CreateDirectory(saveRoot);
            SessionState.SetString(SaveManager.ValidationRootSessionKey, saveRoot);
        }
        EditorSceneManager.OpenScene(mode == "intro" ? PA_DepartureTutorialBuilder.ScenePath
            : route ? "Assets/Scenes/Prototype_FirstDay.unity" : "Assets/Scenes/WorldSandbox.unity");
        PA_DepartureContinuationChecks.ConfigureGameView();
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Subscribe()
    {
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
        EditorApplication.playModeStateChanged -= AudioReviewPlayState; EditorApplication.playModeStateChanged += AudioReviewPlayState;
        AssemblyReloadEvents.beforeAssemblyReload -= RestoreAudioNativeInput; AssemblyReloadEvents.beforeAssemblyReload += RestoreAudioNativeInput;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BindDirectDemo()
    {
        if (!SessionState.GetBool(Active, false)) return;
        Application.runInBackground = true;
        if (Mode == "route" || Mode == "intro") return;
        var prefab = Resources.Load<GameObject>("DepartureTutorial/DepartureContinuation");
        var selection = prefab != null ? prefab.GetComponentInChildren<DepartureCompanionSelection>(true) : null;
        typeof(DemoRouteController).GetField("<SelectedCompanions>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, selection.candidates.Where(c => c.id == "Miner_01" || c.id == "Farmer_01").ToArray());
        var grid = UnityEngine.Object.FindFirstObjectByType<WorldGridService>();
        if (grid != null && grid.GetComponent<DemoRouteController>() == null) grid.gameObject.AddComponent<DemoRouteController>();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || finished || running) return;
        if (Mode == "route" || Mode == "intro")
        {
            if (Mode == "intro")
            {
                var tutorial = UnityEngine.Object.FindFirstObjectByType<DepartureTutorialController>();
                if (tutorial == null || !tutorial.IsReady) return;
            }
            else if (UnityEngine.Object.FindFirstObjectByType<PlayableDayScenarioController>() == null) return;
            running = true;
            _ = Exercise();
            return;
        }
        var route = WorldAlphaPlayableController.Instance?.DemoRoute;
        if (route == null || !route.IsPlayable) return;
        running = true;
        _ = Exercise();
    }

    static async Task Exercise()
    {
        try
        {
            if (Mode == "audio" || Mode == "foot" || Mode == "ui" || AmbienceReview || PA_DemoThemeReview.Active)
            {
                audioNativeDevices = InputSystem.devices.Where(d => d.native && d.enabled && (d is Mouse || d is Keyboard)).ToArray();
                foreach (var device in audioNativeDevices) InputSystem.DisableDevice(device, true);
                Note("FIXTURE scoped audio/foot/ui native input frontend isolation; restored at finish/Play exit/reload");
            }
            keyboard = InputSystem.AddDevice<Keyboard>("P3Keyboard"); keyboard.MakeCurrent();
            mouse = InputSystem.AddDevice<Mouse>("P3Mouse"); mouse.MakeCurrent();
            if (Mode == "route" || Mode == "intro") { await ExerciseRoute(); return; }
            var inventory = Inventory.instance;
            var player = inventory.gameObject;
            var equipment = player.GetComponent<EquipmentSystem>();
            Note("FIXTURE direct Demo256 entry with Miner_01/Farmer_01 (tutorial/voyage not part of P3)");
            await Delay(1500);
            if (Mode == "ambience") { await ObserveIslandAmbience(); Finish("PASS"); return; }

            // 1. 보급 상자: 실제 E. 두 번째 E는 아무것도 더 주지 않는다.
            var supply = FirstDayWorldPresentation.Instance.Supply;
            Face(player, supply.transform, 1.4f, "supply box");
            await Delay(400);
            Check(Prompt().Contains("보급 상자"), "P3 supply prompt visible (" + Prompt() + ")");
            await Press(Key.E); Note("INPUT E at supply box");
            await Until(() => supply.Collected, "supply collected by E", 4);
            var axe = Item(2001); var pickaxe = Item(2002);
            int[] kitIds = { 2001, 2002, 2003, 2011, 2012, 2013, 2014 };
            string counts = string.Join(",", kitIds.Select(id => id + "x" + inventory.CountItems(Item(id))));
            Note("SUPPLY counts " + counts);
            Check(kitIds.All(id => inventory.CountItems(Item(id)) == (id == 2013 ? 2 : 1)), "P3 supply grants axe, pickaxe, net, rod, shop kit, 2 tents, workbench once");
            await Delay(1500);
            await Shot("P3_supply_received");
            await Press(Key.E); Note("INPUT E again at the supply spot");
            await Delay(500);
            Check(kitIds.All(id => inventory.CountItems(Item(id)) == (id == 2013 ? 2 : 1)), "P3 second E grants nothing (no duplicate supply)");

            if (Mode == "fishing") { await ExerciseFishing(player, inventory); return; }
            if (Mode == "bugs") { await ExerciseBugs(player, inventory); return; }
            if (Mode == "craft") { await ExerciseCraft(player, inventory); return; }
            if (Mode == "ui") { await ExerciseUiInteractions(player, inventory); return; }
            if (Mode == "audio") { await ExerciseAudio(player, inventory); return; }
            if (Mode == "foot") { await ExerciseFoot(player); return; }
            if (Mode == "companion") { await ExerciseCompanions(player, inventory); return; }
            if (Mode == "business") { await ExerciseBusiness(player, inventory); return; }
            if (Mode == "feel") { await ExerciseFeel(player, inventory); return; }
            if (Mode == "tour") { await ExerciseTour(player, inventory); return; }

            // 2. 핫바 1~9·휠·손 표시·X.
            int axeSlot = HotbarIndex(inventory, axe), pickSlot = HotbarIndex(inventory, pickaxe);
            Require(axeSlot >= 0 && pickSlot >= 0, "axe/pickaxe on hotbar");
            await Press(DigitKey(axeSlot)); Note($"INPUT {axeSlot + 1}");
            await Delay(300);
            Check(EquipmentSystem.CurrentHeld(player) == axe && equipment.HeldVisual != null && equipment.HeldVisual.activeInHierarchy,
                "P3 number key selects the axe and shows it in hand");
            await Shot("P3_axe_in_hand");
            await Scroll(axeSlot < pickSlot ? -120f : 120f); Note("INPUT mouse wheel");
            await Delay(300);
            Check(inventory.selectedHotbarIndex != axeSlot, $"P3 wheel moves the hotbar selection ({axeSlot}->{inventory.selectedHotbarIndex})");
            await Press(DigitKey(pickSlot)); await Delay(250);
            Check(EquipmentSystem.CurrentHeld(player) == pickaxe, "P3 number key selects the pickaxe");
            await Press(Key.X); await Delay(250); Note("INPUT X");
            Check(EquipmentSystem.CurrentHeld(player) == null, "P3 X empties the hands");

            // 3. 허공 휘두르기·틀린 대상은 내구도 0.
            await Press(DigitKey(axeSlot)); await Delay(250);
            var axeInstance = inventory.GetSelectedInstance();
            Require(axeInstance != null && axeInstance.data == axe, "axe instance selected");
            player.transform.rotation = Quaternion.LookRotation(-OpenDirection(player));
            await Delay(200);
            await Press(Key.E); Note("INPUT E with the axe at empty air"); await Delay(500);
            Check(axeInstance.durabilityUsed == 0, "P3 axe swing at air costs no durability");
            var rock = Nearest<MiningSpot>(player.transform.position, m => m.IsDirectWorld && !m.DirectDepleted);
            Require(rock != null, "direct rock");
            int rockHits = rock.DirectRemainingHits;
            Face(player, rock.transform, 1.4f, "rock with the axe");
            await Delay(400);
            await Press(Key.E); Note("INPUT E with the axe at a rock"); await Delay(500);
            Check(rock.DirectRemainingHits == rockHits && axeInstance.durabilityUsed == 0, "P3 axe on a rock: no hit, no durability");

            // 4. 나무: 접촉 순간 타격·소리·파편, 연타 무시, 베면 바닥 드롭(아직 저장 확정 아님).
            var tree = Nearest<Gatherable>(player.transform.position, g => g.IsDirectWorld && !g.DirectDepleted);
            Require(tree != null, "direct tree");
            Face(player, tree.transform, 1.5f, "tree");
            await Delay(400);
            Check(Prompt().Contains("남은 타격"), "P3 tree prompt shows remaining hits (" + Prompt() + ")");
            int hits = tree.DirectRemainingHits;
            float soundBefore = GatherFeedback.LastPlayedAt;
            await Press(Key.E, 60); Note("INPUT E at the tree");
            Check(tree.DirectRemainingHits == hits, "P3 hit is not counted before the swing reaches the tree");
            await Delay(120);
            await Shot("P3_tree_contact");
            Note("FOLIAGE hidden occluders at contact = " + (FirstDayWorldPresentation.Instance.GetComponent<FoliageOcclusion>()?.HiddenCount ?? -1));
            await Delay(250);
            Check(tree.DirectRemainingHits == hits - 1, $"P3 hit counted at contact ({hits}->{tree.DirectRemainingHits})");
            Check(GatherFeedback.LastPlayedAt > soundBefore && GatherFeedback.LastPlayed == GatherFeedback.Chop, "P3 chop SFX at contact");
            Note($"CHIPS active={UnityEngine.Object.FindObjectsByType<GatherChip>(FindObjectsSortMode.None).Length} (may already be gone)");
            await Delay(300);
            int before = tree.DirectRemainingHits;
            for (int i = 0; i < 6; i++) await Tap(Key.E);
            Note("INPUT E x6 rapid taps within 0.3 s");
            await Delay(450);
            Check(before - tree.DirectRemainingHits <= 1, $"P3 rapid E gives at most one hit per swing ({before}->{tree.DirectRemainingHits})");
            await FellTree(tree);
            var woodItem = Item(3);
            var drop = DropFor(woodItem);
            Check(tree.DirectDepleted && drop != null, "P3 felled tree leaves a Wood drop on the ground");
            Check(!Consumed(tree.DirectSpawnKey), "P3 depletion is not saved before the drop is picked up");
            Check(axeInstance.durabilityUsed == 1, "P3 one felled tree costs exactly one axe durability");
            await Delay(500);
            Note("FOLIAGE hidden occluders after the fell = " + (FirstDayWorldPresentation.Instance.GetComponent<FoliageOcclusion>()?.HiddenCount ?? -1));
            await Shot("P3_tree_felled_drop");

            // 5. 도구를 든 채 드롭 앞: [X] 안내 → X → E 줍기.
            Face(player, drop.transform, 1.0f, "wood drop");
            await Delay(400);
            Check(Prompt().Contains("[X] 손 비우기") && Prompt().Contains("나무 줍기"), "P3 drop with a tool in hand shows [X] 손 비우기 → [E] 나무 줍기 (" + Prompt() + ")");
            await Shot("P3_drop_x_hint");
            int woodBefore = inventory.CountItems(woodItem);
            await Press(Key.X); await Delay(250);
            await Press(Key.E); Note("INPUT X then E at the drop"); await Delay(500);
            Check(inventory.CountItems(woodItem) == woodBefore + 1 && drop == null, "P3 E picks up exactly one Wood");
            Check(Consumed(tree.DirectSpawnKey), "P3 depletion saved after pickup");
            Check(GatherFeedback.LastPlayed == GatherFeedback.PickupClip, "P3 pickup SFX");
            Check(woodItem.category != ItemCategory.Tool && woodItem.basePrice > 0, "P3 Wood is a displayable product (not a tool, has a price)");
            await Shot("P3_wood_picked");

            // 6. 가득 찬 가방: 드롭이 남고 유실 없음. 비우면 다시 줍기.
            await Press(DigitKey(axeSlot)); await Delay(250);
            var tree2 = Nearest<Gatherable>(player.transform.position, g => g.IsDirectWorld && !g.DirectDepleted);
            Require(tree2 != null, "second tree");
            Face(player, tree2.transform, 1.5f, "second tree");
            await Delay(400);
            await FellTree(tree2);
            var drop2 = DropFor(woodItem);
            Require(drop2 != null, "second drop");
            var plank = Item(7);
            int filled = FillBag(inventory, plank);
            int woodTopUp = FillBag(inventory, woodItem);
            Note($"FIXTURE bag filled with {filled} Plank and the Wood stack topped up by {woodTopUp}");
            Face(player, drop2.transform, 1.0f, "second drop");
            await Press(Key.X); await Delay(250);
            int woodBeforeFull = inventory.CountItems(woodItem);
            await Press(Key.E); Note("INPUT X then E with a full bag"); await Delay(500);
            Check(drop2 != null && inventory.CountItems(woodItem) == woodBeforeFull && !Consumed(tree2.DirectSpawnKey),
                "P3 full bag: drop stays, nothing lost, depletion not saved");
            await Shot("P3_full_bag_drop_kept");
            inventory.RemoveItems(plank, filled); inventory.RemoveItems(woodItem, woodTopUp); inventory.RefreshAllUI();
            Note("FIXTURE removed the filler Plank and Wood top-up");
            await Delay(300);
            await Press(Key.E); Note("INPUT E after freeing space"); await Delay(500);
            // 기대 수량: 채운 뒤 수량 − 회수한 top-up + 이번 줍기 1.
            int expectedWood = woodBeforeFull - woodTopUp + 1;
            Check(drop2 == null && inventory.CountItems(woodItem) == expectedWood && Consumed(tree2.DirectSpawnKey),
                $"P3 after freeing space the same drop is picked up once (Wood {inventory.CountItems(woodItem)}/{expectedWood})");

            // 7. 고갈 자원은 대상이 아니다.
            await Press(DigitKey(axeSlot)); await Delay(250);
            Face(player, tree2.transform, 1.5f, "felled tree");
            await Delay(300);
            await Press(Key.E); await Delay(500);
            Check(DropFor(woodItem) == null && axeInstance.durabilityUsed == 2, "P3 E at a felled tree does nothing (no drop, no durability)");

            // 8. 내구도 여유: 필수 벌목 4회 뒤 1회(25%) 남고, 5번째 성공에 부서진다.
            for (int n = axeInstance.durabilityUsed; n < 4; n++) await FellAndPick(player, inventory, woodItem, axeSlot);
            Check(ToolDurability.Remaining(axeInstance) == 1, $"P3 after 4 required fells the axe keeps 1 of 5 (25% margin) [used {axeInstance.durabilityUsed}]");
            await Shot("P3_axe_durability_low");
            await FellAndPick(player, inventory, woodItem, axeSlot);
            Check(inventory.CountItems(axe) == 0, "P3 the 5th successful fell breaks the axe (removed from the bag)");
            await Delay(600);
            await Shot("P3_axe_broken");

            // 9. 곡괭이: 바위 3개(필수) 뒤 1회(33%) 남음.
            pickSlot = HotbarIndex(inventory, pickaxe);
            await Press(DigitKey(pickSlot)); await Delay(250);
            var pickInstance = inventory.GetSelectedInstance();
            var oreItem = Item(4);
            for (int n = 0; n < 3; n++)
            {
                var stone = Nearest<MiningSpot>(player.transform.position, m => m.IsDirectWorld && !m.DirectDepleted);
                Require(stone != null, "rock " + n);
                Face(player, stone.transform, 1.4f, "rock " + n);
                await Delay(400);
                for (int guard = 0; guard < 6 && !stone.DirectDepleted; guard++) { await Press(Key.E); await Delay(420); }
                Require(stone.DirectDepleted, "rock mined " + n);
                if (n == 0) { Check(GatherFeedback.LastPlayed == GatherFeedback.Stone, "P3 stone SFX at contact"); await Shot("P3_rock_mined_drop"); }
                var ore = DropFor(oreItem);
                Require(ore != null, "ore drop " + n);
                int oreBefore = inventory.CountItems(oreItem);
                Face(player, ore.transform, 1.0f, "ore drop");
                await Press(Key.X); await Delay(250); await Press(Key.E); await Delay(450);
                Check(inventory.CountItems(oreItem) == oreBefore + 1 && Consumed(stone.DirectSpawnKey), $"P3 ore {n + 1} picked up once and saved");
                await Press(DigitKey(pickSlot)); await Delay(250);
            }
            Check(ToolDurability.Remaining(pickInstance) == 1, $"P3 after 3 required rocks the pickaxe keeps 1 of 4 (33% margin) [used {pickInstance.durabilityUsed}]");

            // 10. 바닥 줍기(직접 채집과 함께 경험).
            await Press(Key.X); await Delay(250);
            var ground = UnityEngine.Object.FindObjectsByType<InventoryFramework.PickupItem>(FindObjectsSortMode.None)
                .Where(p => p.contextual && !p.name.StartsWith("GatherDrop_") && p.item != null)
                .OrderBy(p => Vector3.Distance(p.transform.position, player.transform.position)).FirstOrDefault();
            Require(ground != null, "ground pickup");
            var groundItem = ground.item;
            int groundBefore = inventory.CountItems(groundItem);
            Face(player, ground.transform, 1.0f, "ground pickup");
            await Delay(300);
            await Press(Key.E); Note("INPUT E at a ground pickup"); await Delay(500);
            Check(inventory.CountItems(groundItem) == groundBefore + 1, "P3 ground pickup by E adds one item");
            await Shot("P3_hotbar_after_gathering");
            Finish("PASS");
        }
        catch (Exception ex)
        {
            problem = ex.Message;
            try { await Shot("failure-context"); } catch { }
            Finish("FAIL");
        }
    }

    // P4 user override: real reel keys, dry-land fish and real tool contact before reward.
    static async Task ExerciseFishing(GameObject player, Inventory inventory)
    {
        var fishing = UnityEngine.Object.FindObjectsByType<FishingSpot>(FindObjectsSortMode.None).FirstOrDefault(f => f.IsDirectPlayerDemo);
        Require(fishing != null, "demo fishing spot");
        var grid = UnityEngine.Object.FindFirstObjectByType<WorldGridService>();
        var loop = DayNightShopLoopController.Instance;
        var point = fishing.GetComponent<DaytimeStockPrepPoint>();
        var rod = Item(2011); int rodSlot = HotbarIndex(inventory, rod);
        await Press(DigitKey(rodSlot)); await Delay(250);
        var rodInstance = inventory.GetSelectedInstance();
        Require(rodInstance != null && rodInstance.data == rod, "rod held");
        Vector3 landSide = Vector3.ProjectOnPlane(fishing.transform.position - fishing.WaterTarget, Vector3.up);
        Face(player, fishing.transform, 1.2f, "fishing spot (dry land)", fishing.transform.position + landSide.normalized * 1.2f);
        await Delay(500);
        var shadow = GameObject.Find("FishingSpot_FishShadow");
        Check(shadow != null && shadow.activeInHierarchy, "P4 local fish shadow visible");
        Check(Prompt().Contains("낚싯대 던지기"), "P4 cast prompt visible");
        Check(fishing.WaterTarget.y < fishing.transform.position.y, "P4 water is below the beach (water " + fishing.WaterTarget.y.ToString("F2") + " / land " + fishing.transform.position.y.ToString("F2") + ")");
        // INPUT against the water edge; no teleport during this attempted crossing.
        await WalkTo(player.transform, fishing.WaterTarget, .1f, 2.5f, "attempt to enter the sea");
        grid.WorldToCell(player.transform.position, out var safeCell); grid.TryGetCell(safeCell, out var safeData);
        Check(!safeData.HasWater && player.transform.position.y >= fishing.transform.position.y - .5f, "P4 actual movement/jump cannot enter or swim in the sea");
        Face(player, fishing.transform, 1.2f, "reset dry fishing approach", fishing.transform.position + landSide.normalized * 1.2f);
        await Delay(500); await Shot("P4_low_water_shore");
        var species = new[] { Item(8), Item(3001), Item(3002) };
        Check(species.All(i => i != null && i.icon != null && i.model != null && i.basePrice > 0 && i.category == ItemCategory.Raw), "P4 three local species have distinct ids, model icons and saleable existing Item data");
        Check(species.All(i => i.model.GetComponentsInChildren<Renderer>().All(r => r.sharedMaterials.All(m => m != null && m.shader.name.Contains("Universal Render Pipeline")))), "P4 species use existing URP rendering");
        int fishStart = species.Sum(i => inventory.CountItems(i));

        await Press(Key.E); await Delay(250); await Press(Key.E); await Delay(200);
        Check(fishing.Phase == FishingSpot.DemoPhase.Idle && fishing.LastFeedback.Contains("너무 빨리") && rodInstance.durabilityUsed == 0, "P4 early pull cancels with no reward or durability");
        await Press(Key.E); await Until(() => fishing.Phase == FishingSpot.DemoPhase.Bite, "bite", 5);
        await Until(() => fishing.Phase == FishingSpot.DemoPhase.Idle, "missed bite", 3);
        Check(fishing.LastFeedback.Contains("달아났") && rodInstance.durabilityUsed == 0, "P4 missed bite loses no inventory or durability");
        await Press(Key.E); await Delay(250); await Press(Key.X); await Delay(250);
        Check(fishing.Phase == FishingSpot.DemoPhase.Idle && (fishing.Bobber == null || !fishing.Bobber.activeSelf), "P4 holstering cancels the wait");
        await Press(DigitKey(rodSlot)); await Delay(250);

        await StartReel(fishing);
        Check(FirstDayFishingMinigame.IsOpen && PlayerInputHandler.ModalOpen, "P4 bite opens reel control instead of granting a fish");
        await Shot("P4_reel_panel");
        Vector3 stopped = player.transform.position; int jumps = player.GetComponent<PlayerController>().JumpCount;
        await Press(Key.W, 350); await Press(Key.I); await Press(Key.P);
        Check(Vector3.Distance(stopped, player.transform.position) < .1f && !(InventoryUI.instance != null && InventoryUI.instance.gameObject.activeSelf) && !(SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen), "P4 reel panel locks movement, bag and phone input");
        await Press(Key.Escape); await Delay(200);
        Check(!FirstDayFishingMinigame.IsOpen && fishing.Phase == FishingSpot.DemoPhase.Idle && Time.timeScale == 1 && fishing.LandedFish == null, "P4 Esc cancels reel without pausing or granting fish");
        await StartReel(fishing);
        await Until(() => !FirstDayFishingMinigame.IsOpen, "uncontrolled reel failure", 15);
        Check(fishing.Phase == FishingSpot.DemoPhase.Idle && fishing.LandedFish == null && rodInstance.durabilityUsed == 0, "P4 failed reel leaves no fish or durability cost");

        int filled = FillBag(inventory, Item(7)); Note("FIXTURE full bag with Plank " + filled);
        await StartReel(fishing);
        await ControlReel();
        await Until(() => fishing.LandedFish != null && fishing.LandedFish.OnLand, "actual fish lands", 3);
        var landed = fishing.LandedFish; var caughtItem = fishing.SelectedFish;
        grid.WorldToCell(landed.DryRest, out var dryCell); grid.TryGetCell(dryCell, out var dryData);
        Check(!dryData.HasWater && dryData.IsWalkable && landed.Hits == 0 && loop.IsDayPrepPointAvailable(point) && species.Sum(i => inventory.CountItems(i)) == fishStart, "P4 successful reel creates a flopping fish on dry land without any reward");
        Check(player.GetComponent<PlayerController>().JumpCount == jumps, "P4 Space reel input never jumps the player");
        await Shot("P4_fish_flop_a"); await Delay(180); await Shot("P4_fish_flop_b");
        await ApproachFront(player.transform, landed, "landed fish"); await Delay(400);
        await Press(Key.E); await Delay(400);
        Check(landed.Hits == 0 && rodInstance.durabilityUsed == 1, "P4 rod cannot strike; successful reel costs one rod use");
        int axeSlot = HotbarIndex(inventory, Item(2001)); await Press(DigitKey(axeSlot)); await Delay(250);
        var axeInstance = inventory.GetSelectedInstance(); int used = axeInstance.durabilityUsed;
        for (int i = 0; i < 3; i++) await Tap(Key.E);
        await Delay(400);
        Check(landed.Hits == 1 && axeInstance.durabilityUsed == used + 1, "P4 rapid E produces one timed tool contact");
        await Shot("P4_first_tool_contact");
        await Delay(250); await Press(Key.E); await Delay(450);
        Check(landed.ReadyToClaim && fishing.Phase == FishingSpot.DemoPhase.Landed && loop.IsDayPrepPointAvailable(point) && species.Sum(i => inventory.CountItems(i)) == fishStart, "P4 full bag retains captured fish without completing activity or granting inventory");
        await Shot("P4_captured_full_bag");
        inventory.RemoveItems(Item(7), filled); inventory.RefreshAllUI(); Note("FIXTURE remove only filler Plank");
        await Delay(500); int durabilityBeforeRetry = axeInstance.durabilityUsed;
        for (int i = 0; i < 3; i++) await Tap(Key.E);
        await Delay(500);
        Check(species.Sum(i => inventory.CountItems(i)) == fishStart + 1 && inventory.CountItems(caughtItem) == 1 && !loop.IsDayPrepPointAvailable(point), "P4 tool capture grants exactly one selected species through existing daily stock authority");
        Check(axeInstance.durabilityUsed == durabilityBeforeRetry && durabilityBeforeRetry == used + 2, "P4 only two valid contacts cost tool durability; full-bag retry is free");
        Check(fishing.LandedFish == null && fishing.Phase == FishingSpot.DemoPhase.Idle && !shadow.activeSelf, "P4 claimed fish and shadow removed with no duplicate catch");
        await Shot("P4_species_in_bag");
        await Press(DigitKey(rodSlot)); await Delay(300);
        Face(player, fishing.transform, 1.2f, "completed fishing spot", fishing.transform.position + landSide.normalized * 1.2f);
        await Delay(400); await Press(Key.E); await Delay(250);
        Check(Prompt().Contains("오늘 낚시 완료") && species.Sum(i => inventory.CountItems(i)) == fishStart + 1, "P4 completed daily activity cannot grant another species");
        await Shot("P4_after_completion"); Finish("PASS");
    }

    static async Task StartReel(FishingSpot fishing)
    {
        await Press(Key.E); Note("INPUT E cast");
        await Until(() => fishing.Phase == FishingSpot.DemoPhase.Bite, "bite arrives", 5);
        await Press(Key.E); Note("INPUT E bite -> reel");
        Require(FirstDayFishingMinigame.IsOpen, "reel panel opened");
    }

    static async Task ControlReel()
    {
        Note("INPUT Space hold/release follows observed fish; no catch/progress injection");
        double deadline = EditorApplication.timeSinceStartup + 25;
        while (FirstDayFishingMinigame.IsOpen && EditorApplication.timeSinceStartup < deadline)
        {
            var reel = FirstDayFishingMinigame.Instance;
            bool hold = reel.BandPosition < reel.FishPosition;
            InputSystem.QueueStateEvent(keyboard, hold ? new KeyboardState(Key.Space) : new KeyboardState());
            await Task.Delay(80);
        }
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); await Delay(150);
        Require(!FirstDayFishingMinigame.IsOpen, "reel ended through input");
    }

    // P5: 빈손/틀린 도구 안내 → 가득 찬 가방(놓아줌) → 멀리서 빗나감(이탈) → 가까이 연타 포획 1회 → 중복 없음.
    static async Task ExerciseBugs(GameObject player, Inventory inventory)
    {
        var bugs = UnityEngine.Object.FindObjectsByType<BugCritter>(FindObjectsSortMode.None);
        Note("BUGS " + string.Join(" ", bugs.Select(b => b.name + "@" + b.transform.position.ToString("F1"))) + " clock " +
             (GameClock.Instance != null ? GameClock.Instance.CurrentHour.ToString("F2") + "h" : "?"));
        Require(bugs.Length > 0, "meadow butterflies exist");
        var bug = bugs.OrderBy(b => b.name).First();
        Check(bug.GetComponentsInChildren<Renderer>().Any(r => r.enabled && r.name.Contains("Butterfly")), "P5 butterfly uses the local butterfly model");
        var butterfly = Item(2004); var net = Item(2003); var axe = Item(2001);
        int netSlot = HotbarIndex(inventory, net), axeSlot = HotbarIndex(inventory, axe);
        Require(netSlot >= 0 && axeSlot >= 0, "net/axe on hotbar");

        // 1) 빈손·틀린 도구: 안내만, 포획/내구도 없음.
        await Press(Key.X); await Delay(200);
        FaceBug(player, bug, 1.1f, "butterfly (empty hands)");
        await Delay(500);
        Check(Prompt().Contains("잠자리채를 들면"), "P5 empty hands near a butterfly shows the net hint (" + Prompt() + ")");
        await Shot("P5_hint_no_net");
        await Press(Key.E); Note("INPUT E with empty hands"); await Delay(500);
        await Press(DigitKey(axeSlot)); await Delay(250);
        var axeInstance = inventory.GetSelectedInstance();
        FaceBug(player, bug, 1.1f, "butterfly (axe)");
        await Delay(400);
        Note("PROMPT with axe: " + Prompt());
        await Press(Key.E); Note("INPUT E with the axe"); await Delay(500);
        Check(!bug.Captured && inventory.CountItems(butterfly) == 0 && axeInstance.durabilityUsed == 0, "P5 empty hands / wrong tool catch nothing and cost nothing");

        // 2) 가득 찬 가방: 잡은 나비를 놓아준다(유실·내구도 없음).
        await Press(DigitKey(netSlot)); await Delay(250);
        Check(EquipmentSystem.CurrentHeld(player) == net, "P5 number key holds the net");
        var netInstance = inventory.GetSelectedInstance();
        FaceBug(player, bug, .9f, "butterfly (net, close)");
        await Delay(400);
        Check(Prompt().Contains("잠자리채 휘두르기"), "P5 net prompt invites a swing (" + Prompt() + ")");
        await Shot("P5_ready");
        var plank = Item(7);
        int filled = FillBag(inventory, plank);
        Note($"FIXTURE bag filled with {filled} Plank");
        await Press(Key.E); Note("INPUT E with a full bag"); await Delay(550);
        Check(!bug.Captured && bug.Fleeing && bug.LastFeedback.Contains("놓아줬") && inventory.CountItems(butterfly) == 0 && netInstance.durabilityUsed == 0,
            "P5 full bag releases the butterfly (no reward, no durability, it flies off)");
        await Shot("P5_full_bag_release");
        inventory.RemoveItems(plank, filled); inventory.RefreshAllUI();
        Note("FIXTURE removed the filler Plank");
        await Until(() => !bug.Fleeing, "butterfly settles", 3);

        // 3) 멀리서 휘두르기: 빗나가고 놀라 날아간다. 나는 동안은 잡을 수 없다.
        FaceBug(player, bug, 1.85f, "butterfly (net, edge of reach)");
        await Delay(300);
        Check(Prompt().Contains("잠자리채 휘두르기"), "P5 prompt still offers the swing at the edge of reach (" + Prompt() + ")");
        float swishBefore = GatherFeedback.LastPlayedAt;
        await Press(Key.E); Note("INPUT E from 1.85 m"); await Delay(450);
        Check(bug.LastFeedback.Contains("빗나갔") && bug.Fleeing && !bug.Captured && inventory.CountItems(butterfly) == 0 && netInstance.durabilityUsed == 0 &&
              GatherFeedback.LastPlayedAt > swishBefore, "P5 far swing whooshes, misses and the butterfly escapes (no durability)");
        await Shot("P5_miss_flee");
        await Press(Key.E); Note("INPUT E while it flees"); await Delay(300);
        Check(!bug.Captured && inventory.CountItems(butterfly) == 0, "P5 a fleeing butterfly cannot be caught");
        await Until(() => !bug.Fleeing, "butterfly settles after escape", 3);

        // 4) 가까이에서 연타: 한 번만 잡고 내구도 1.
        FaceBug(player, bug, .9f, "butterfly (net, after escape)");
        await Delay(300);
        for (int i = 0; i < 3; i++) await Tap(Key.E);
        Note("INPUT E x3 rapid taps");
        await Delay(700);
        Check(bug.Captured && inventory.CountItems(butterfly) == 1 && netInstance.durabilityUsed == 1 && GatherFeedback.LastPlayed == GatherFeedback.CatchClip,
            $"P5 close swing catches once: Butterfly {inventory.CountItems(butterfly)}, net used {netInstance.durabilityUsed}, catch sound");
        Check(!bug.GetComponentsInChildren<Renderer>().Any(r => r.enabled), "P5 caught butterfly leaves the meadow");
        await Shot("P5_caught");
        await Press(Key.E); await Delay(400);
        Check(inventory.CountItems(butterfly) == 1 && netInstance.durabilityUsed == 1, "P5 E after the catch gives nothing more");
        Check(butterfly.category != ItemCategory.Tool && butterfly.basePrice > 0, "P5 Butterfly is a displayable product");
        Finish("PASS");
    }

    // P6: 배치 문법(마우스·E·R·Esc/RMB·범위·불가·Hold E 이동) → 작업대 제작 UI(잠김·재료 부족·제작·닫기/이동)
    //     → 실제 정착(상점·텐트, 큰 건물 build view) → 광업 해금 → 개선 곡괭이 제작·직접 사용 → 판재 진열 → 실내 보관함.
    static async Task ExerciseFoot(GameObject player)
    {
        var motion = player.GetComponent<PlayerLocomotionAnimator>();
        Require(motion != null && motion.Animator != null, "foot humanoid animator");
        var left = motion.Animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var right = motion.Animator.GetBoneTransform(HumanBodyBones.RightFoot);
        Require(left != null && right != null, "foot bone markers");
        string label = SessionState.GetString(FeelLabelKey, "before");
        var capture = new GameObject("P9_FootFrames").AddComponent<FrameSequenceCapture>();
        var camera = Camera.main; var follow = camera.GetComponent<CameraController>();
        var skin = motion.Animator.GetComponentInChildren<SkinnedMeshRenderer>();
        Require(skin != null, "foot skinned shoe mesh");
        var baked = new Mesh();
        var walkField = typeof(PlayerLocomotionAnimator).GetField("walkStrideSpeed", Private);
        var jogField = typeof(PlayerLocomotionAnimator).GetField("jogStrideSpeed", Private);
        foreach (string phase in label == "before" ? new[] { "before" } : new[] { "baseline6", "candidate4p6" })
        {
        float stride = phase == "candidate4p6" ? 4.6f : 6f;
        walkField.SetValue(motion, stride); jogField.SetValue(motion, stride);
        Note("FIXTURE transient stride " + stride + " for actual sole comparison; product/source baseline6 retained, no scene save");
        foreach (string view in label == "before" ? new[] { "game", "close" } : new[] { "game", "close", "foot-close" })
        {
            var rows = new System.Collections.Generic.List<FootSample>();
            Vector3 lastLeft = Vector3.zero, lastRight = Vector3.zero, lastRoot = Vector3.zero; bool previous = false;
            Vector3[] lastVertices = null;
            int[] soleIndices = null;
            float groundY = 0f;
            capture.OnCapturedFrame = () =>
            {
                Vector3 l = left.position, r = right.position, root = player.transform.position;
                skin.BakeMesh(baked);
                var localVertices = baked.vertices;
                var vertices = localVertices.Select(v => skin.transform.TransformPoint(v)).ToArray();
                if (soleIndices == null)
                {
                    var leftIndices = Enumerable.Range(0,vertices.Length).Where(i => Vector3.Distance(vertices[i],l)<.32f)
                        .OrderBy(i=>vertices[i].y).Take(10);
                    var rightIndices = Enumerable.Range(0,vertices.Length).Where(i => Vector3.Distance(vertices[i],r)<.32f)
                        .OrderBy(i=>vertices[i].y).Take(10);
                    soleIndices=leftIndices.Concat(rightIndices).Distinct().ToArray();
                    Require(soleIndices.Length>=10,"tracked shoe sole vertices");
                }
                if (previous && motion.Grounded && motion.MoveSpeed > 3.9f && Mathf.Abs(root.y - lastRoot.y) < .02f)
                {
                    Vector3 direction = Vector3.ProjectOnPlane(root-lastRoot,Vector3.up).normalized;
                    float signed=0f, absolute=0f; int contacts=0;
                    foreach(int i in soleIndices)
                    {
                        float height=vertices[i].y-groundY;
                        if(height<-.025f||height>.025f) continue;
                        float speed=Vector3.Dot((vertices[i]-lastVertices[i])/Time.deltaTime,direction);
                        signed+=speed; absolute+=Mathf.Abs(speed);contacts++;
                    }
                    rows.Add(new FootSample { frame = capture.Frames, leftY = l.y-root.y, rightY = r.y-root.y,
                        leftSpeed = (l.x-lastLeft.x)/Time.deltaTime, rightSpeed = (r.x-lastRight.x)/Time.deltaTime,
                        rootSpeed = (root.x-lastRoot.x)/Time.deltaTime,soleContacts=contacts,soleSigned=signed,soleAbsolute=absolute });
                }
                lastLeft=l; lastRight=r; lastRoot=root; lastVertices=vertices; previous=true;
            };
            Vector3 start = FirstDayWorldPresentation.Instance.Harbor + new Vector3(-6f, 0f, 9f);
            if (Physics.Raycast(start+Vector3.up*6f, Vector3.down, out var ground, 12f, ~0, QueryTriggerInteraction.Ignore))
            { groundY=ground.point.y; start.y=groundY+.05f; }
            Teleport(player,start,Quaternion.LookRotation(Vector3.right));
            if (EquipmentSystem.CurrentHeld(player) != null) await Press(Key.X);
            await Delay(500);
            QaCloseFollowCamera close = null;
            if(view!="game")
            {
                follow.enabled=false;close=camera.gameObject.AddComponent<QaCloseFollowCamera>();close.target=player.transform;
                if(view=="foot-close") { close.localOffset=new Vector3(3.2f,1.3f,2.4f);close.lookHeight=.8f; }
            }
            capture.folder=Path.GetFullPath(Path.Combine(OutDir,"frames-"+phase,view)); Directory.CreateDirectory(capture.folder);
            Time.captureFramerate=30;capture.capturing=true;
            await WaitGame(1f);await HoldGame(3f,Key.D);await WaitGame(.8f);capture.capturing=false;
            capture.OnCapturedFrame=null;
            Check(rows.Count>=45, "P9 foot "+view+" has at least45 actual moving/grounded rendered samples ("+rows.Count+")");
            Require(rows.Count>=10,"foot moving samples");
            float leftLow=rows.Select(s=>s.leftY).OrderBy(y=>y).ElementAt(Mathf.FloorToInt(rows.Count*.1f));
            float rightLow=rows.Select(s=>s.rightY).OrderBy(y=>y).ElementAt(Mathf.FloorToInt(rows.Count*.1f));
            var stance=rows.SelectMany(s=>new[]{ s.leftY<leftLow+.018f ? Mathf.Abs(s.leftSpeed):float.NaN,
                s.rightY<rightLow+.018f ? Mathf.Abs(s.rightSpeed):float.NaN }).Where(v=>!float.IsNaN(v)).ToArray();
            int contactCount=rows.Sum(s=>s.soleContacts);
            Check(contactCount>=30,"P9 tracked mesh-sole contact samples "+phase+"/"+view+" ("+contactCount+")");
            float soleMean=contactCount>0?rows.Sum(s=>s.soleAbsolute)/contactCount:float.NaN;
            float soleSigned=contactCount>0?rows.Sum(s=>s.soleSigned)/contactCount:float.NaN;
            Note($"FOOT {phase}/{view} bone-marker mean={stance.Average():F4}; mesh-sole meanAbs={soleMean:F4} signed={soleSigned:F4}m/s rootX={rows.Average(s=>s.rootSpeed):F4}; numerical sampling not human acceptance");
            File.WriteAllText(Path.Combine(OutDir,"foot-"+phase+"-"+view+".json"),JsonUtility.ToJson(new FootSamples { rows=rows.ToArray(),stanceMean=stance.Average(),soleMean=soleMean,soleSigned=soleSigned,contacts=contactCount },true));
            await Shot("P9_foot_"+phase+"_"+view);
            if(close!=null) { UnityEngine.Object.Destroy(close);follow.enabled=true; }
        }
        }
        walkField.SetValue(motion,6f);jogField.SetValue(motion,6f);
        UnityEngine.Object.Destroy(baked);
        Finish("PASS");
    }

    [Serializable] sealed class FootSample { public int frame,soleContacts; public float leftY,rightY,leftSpeed,rightSpeed,rootSpeed,soleSigned,soleAbsolute; }
    [Serializable] sealed class FootSamples { public FootSample[] rows; public float stanceMean,soleMean,soleSigned;public int contacts; }

    static async Task ExerciseAudio(GameObject player, Inventory inventory)
    {
        Require(AudioManager.Instance != null, "existing AudioManager");
        Note("OUTPUT start=" + DateTimeOffset.Now.ToString("o") + " sampleRate=" + AudioSettings.outputSampleRate
            + " listenerVolume=" + AudioListener.volume + " pause=" + AudioListener.pause);
        float idle = await MixedAudioPeak(1.2f);
        var tree = Nearest<Gatherable>(player.transform.position, g => g.IsDirectWorld && !g.DirectDepleted);
        Require(tree != null, "audio contact tree");
        Face(player, tree.transform, 1.5f, "audio contact tree");
        await Press(DigitKey(HotbarIndex(inventory, Item(2001)))); await Delay(700);
        Note("FIXTURE existing SFX setting1 (not a claim of phone-slider input)");
        AudioManager.SetSFXVolume(1f);
        await Press(Key.E); float full = await MixedAudioPeak(.9f);
        Check(full > .002f && GatherFeedback.LastPlayed != null && GatherFeedback.LastPlayed.name == "PA_Chop",
            "P11 real E tree contact reaches the mixed output at full SFX volume");
        await Shot("P11_audio_contact");
        await Delay(700);
        Note("FIXTURE existing SFX setting0; same real E contact path");
        AudioManager.SetSFXVolume(0f);
        await Press(Key.E); float muted = await MixedAudioPeak(.9f);
        Check(muted < .002f, "P11 SFX volume0 silences actual tool output");
        await Delay(700);
        AudioManager.SetSFXVolume(1f);
        await Press(DigitKey(HotbarIndex(inventory, Item(2003)))); await Delay(300);
        await Press(Key.E); float net = await MixedAudioPeak(.9f);
        Check(net > .002f, "P11 the actual net swing returns after restoring SFX volume1");
        Note("FIXTURE sale.confirm clip output only; natural sale still requires final candidate route");
        AudioManager.PlaySFX(AudioManager.SaleConfirmedSfxName); float sale = await MixedAudioPeak(.6f);
        Check(sale > .002f, "P11 existing sale-confirm clip reaches the mixed output");
        File.WriteAllText(Path.Combine(OutDir, "mixed-output.json"), JsonUtility.ToJson(new AudioPeakRecord { idle = idle, full = full, muted = muted, net = net, sale = sale }, true));
        Note("OUTPUT finish=" + DateTimeOffset.Now.ToString("o") + $" idle={idle:F6} full={full:F6} muted={muted:F6} net={net:F6} sale={sale:F6}");
        Finish("PASS");
    }

    [Serializable] sealed class AudioPeakRecord { public float idle, full, muted, net, sale; }

    static async Task<float> MixedAudioPeak(float seconds)
    {
        var samples = new float[2048]; float peak = 0f;
        double end = EditorApplication.timeSinceStartup + seconds;
        while (EditorApplication.timeSinceStartup < end)
        {
            AudioListener.GetOutputData(samples, 0);
            foreach (float value in samples) peak = Mathf.Max(peak, Mathf.Abs(value));
            await Delay(40);
        }
        return peak;
    }

    static async Task ObserveAmbience(string stage)
    {
        Require(AudioManager.Instance != null, "existing ambience AudioManager");
        string clips = string.Join(",", AudioManager.Instance.GetComponents<AudioSource>()
            .Where(s => s.loop && s.isPlaying && s.clip != null).Select(s => s.clip.name));
        float peak = await MixedAudioPeak(1.2f);
        Note("AMBIENCE " + stage + " at=" + DateTimeOffset.Now.ToString("o")
            + " clips=" + clips + " peak=" + peak.ToString("F6") + " bgm=" + AudioManager.Instance.bgmVolume);
        if (SessionState.GetBool(AmbienceExpectedKey, false))
        {
            Check(peak > .001f && clips.Length > 0, "P11 " + stage + " ambience reaches mixed output");
            string expected = stage == "island-day" ? "day" : stage == "island-night" ? "night" : stage;
            Check(clips.Contains("PA_Ambience_" + expected), "P11 " + stage + " uses its matching sound bed");
        }
        await Shot("P11_ambience_" + stage);
    }

    static async Task ObserveIslandAmbience()
    {
        await ObserveAmbience("island-day");
        var clock = GameClock.Instance;
        Require(clock != null, "ambience clock");
        var settlement = DemoSettlementController.Instance;
        bool settlementEnabled = settlement != null && settlement.enabled;
        try
        {
            Note("FIXTURE suspend unsettled daylight-hold component only for this isolated time/audio observation; restore on completion. No placement/reward/progress injection");
            if (settlement != null) settlement.enabled = false;
            Note("FIXTURE clock20 for isolated night/Continue-time presentation, not natural sunset or business proof");
            clock.ForceSet(20f, 1, "QA_AMBIENCE_NIGHT");
            await Delay(250);
            Note("FIXTURE BGM setting0 during scene/time fade, not phone slider input");
            AudioManager.SetBGMVolume(0f);
            await Delay(1500);
            float muted = await MixedAudioPeak(1f);
            Check(muted < .001f, "P11 BGM setting0 keeps time-transition output silent");
            Note("AMBIENCE muted peak=" + muted.ToString("F6") + " at=" + DateTimeOffset.Now.ToString("o"));
            AudioManager.SetBGMVolume(.6f);
            await Delay(1600);
            await ObserveAmbience("island-night");
            await Delay(13200);
            Check(await MixedAudioPeak(1f) > .001f, "P11 night sound continues across a twelve-second loop");
        }
        finally { if (settlement != null) settlement.enabled = settlementEnabled; }
    }

    // Applied Q1/Q2/Q6 follow-up: existing placement/input helpers, no repeated Root/upgrade/night checks.
    static async Task ExerciseUiInteractions(GameObject player, Inventory inventory)
    {
        var wood = Item(3); var plank = Item(7); var axe = Item(2001); var pick = Item(2002);
        var benchRuntime = await PlaceWithMouse(player, inventory, Item(2014), "UI workbench", false);
        Require(benchRuntime != null, "UI workbench placed");
        Face(player, benchRuntime.GameObject.transform, 1.3f, "UI workbench");
        await Press(Key.X); await Delay(300); await Press(Key.E); await Delay(500);
        var craft = CraftingUI.instance;
        Require(craft != null && craft.IsOpen, "Q1 crafting open through E");
        var card = Card(craft, "판재");
        Check(card.Key != null && !card.Key.GetComponent<UnityEngine.UI.Button>().interactable && Plain(card.Value).Contains("재료 부족"),
            "Q1 material shortage is visible and disables crafting");
        await Shot("Q1_materials_missing");
        var scroll = craft.slotParent.GetComponentInParent<UnityEngine.UI.ScrollRect>();
        Require(scroll != null, "Q1 recipe scroll");
        await ScrollAt(ScreenCenter(scroll.viewport), -1200f); await Delay(300);
        Check(scroll.verticalNormalizedPosition < .95f, "Q1 mouse wheel reaches lower recipes");
        await Shot("Q1_lower_recipes");
        await ScrollAt(ScreenCenter(scroll.viewport), 1800f); await Delay(250);
        await Press(Key.Escape); await Delay(250);
        Require(inventory.TryReceiveToHotbar(wood, 4), "Q1 fixture Wood+4");
        Note("FIXTURE Wood+4; direct resource collection already proven");
        await Press(Key.E); await Delay(400);
        card = Card(craft, "판재");
        Require(card.Key != null && TopHit(card.Key.gameObject), "Q1 visible clickable plank card");
        await ClickUI(card.Key.gameObject, "Q1 make plank"); await Delay(350);
        Check(inventory.CountItems(plank) == 1 && inventory.CountItems(wood) == 2 && Status(craft).Contains("제작 완료"),
            "Q1 one click consumes two Wood and produces one Plank with success status");
        await Shot("Q1_crafted");
        await Press(Key.Escape); await Delay(300);

        // Make a genuinely used axe before testing the live mouse drag handlers.
        int axeIndex = HotbarIndex(inventory, axe), pickIndex = HotbarIndex(inventory, pick);
        await Press(DigitKey(axeIndex)); await Delay(250);
        var tree = Nearest<Gatherable>(player.transform.position, g => g.IsDirectWorld && !g.DirectDepleted);
        Require(tree != null, "tree for used-tool UI");
        Face(player, tree.transform, 1.5f, "used axe UI"); await Delay(300); await FellTree(tree);
        var used = inventory.hotbar.slots[axeIndex].instance;
        Require(used != null && used.durabilityUsed == 1, "axe has one real successful use");
        used.quality = 1.25f; used.currentPrice = 31;
        Note("FIXTURE non-default axe quality1.25/price31 to observe metadata conservation; wear1 came from real E tree gathering");
        inventory.RefreshAllUI(); await Press(Key.Tab); await Delay(600);
        var bag = InventoryUI.instance;
        Require(bag != null && bag.gameObject.activeInHierarchy, "Q6 Tab opens inventory");
        var slots = UnityEngine.Object.FindObjectsByType<InventorySlotUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var bagSlot = slots.First(s => s.owner == SlotOwner.Inventory && s.index == 0);
        var axeSlot = slots.First(s => s.owner == SlotOwner.Hotbar && s.index == axeIndex);
        var pickSlot = slots.First(s => s.owner == SlotOwner.Hotbar && s.index == pickIndex);
        Require(bagSlot.GetSlot().IsEmpty && TopHit(axeSlot.gameObject) && TopHit(bagSlot.gameObject), "Q6 reachable source and empty bag slot");
        await DragUi(axeSlot, ScreenCenter((RectTransform)bagSlot.transform), "used axe hotbar to bag");
        Check(axeSlot.GetSlot().IsEmpty && bagSlot.GetSlot().item == axe && ToolMetadata(bagSlot.GetSlot().instance),
            "Q6 dragging the used axe preserves wear/quality/price/count");
        await Shot("Q6_tool_in_bag");
        if (PA_DemoThemeReview.Active)
        {
            await MoveMouse(new Vector2(50,400)); await Delay(150);
            await MoveMouse(ScreenCenter((RectTransform)bagSlot.transform)); await Delay(350);
            Require(bagSlot.tooltip != null && bagSlot.tooltip.gameObject.activeInHierarchy, "D2 slot hover opens tooltip");
            Check(bagSlot.tooltip.descriptionText.text.Contains("기본 가격"), "D2 tooltip includes price");
            await Shot("D2_tooltip");
            await MoveMouse(new Vector2(50,400)); await Delay(100);
        }
        await DragUi(bagSlot, new Vector2(160f, 850f), "cancel axe drag outside slots");
        Check(bagSlot.GetSlot().item == axe && ToolMetadata(bagSlot.GetSlot().instance) && DragContext.draggedInstance == null,
            "Q6 dropping outside slots returns the same used tool without duplication");
        await DragUi(pickSlot, ScreenCenter((RectTransform)bagSlot.transform), "pickaxe swaps with used axe");
        Check(pickSlot.GetSlot().item == axe && ToolMetadata(pickSlot.GetSlot().instance) && bagSlot.GetSlot().item == pick,
            "Q6 cross-panel swap preserves the used axe and the pickaxe");
        Check(pickSlot.icon.enabled && pickSlot.icon.sprite == axe.icon && bagSlot.icon.enabled && bagSlot.icon.sprite == pick.icon,
            "Q6 both panels show the swapped icons immediately");
        await Shot("Q6_cross_panel_swap");
        await DragUi(bagSlot, ScreenCenter((RectTransform)axeSlot.transform), "pickaxe back to empty hotbar slot");
        Check(inventory.CountItems(axe) == 1 && inventory.CountItems(pick) == 1 && inventory.hotbar.slots[pickIndex].instance.durabilityUsed == 1,
            "Q6 complete drag/cancel/swap sequence conserves both tools and wear");
        await ClickUI(bag.transform.Find("BagHeader/BagClose").gameObject, "Q6 close bag button"); await Delay(300);
        Check(!bag.gameObject.activeSelf, "Q6 close button returns to gameplay");

        var shop = await PlaceWithMouse(player, inventory, Item(2012), "UI shop", true);
        await PlaceWithMouse(player, inventory, Item(2013), "UI tent 1", false);
        await PlaceWithMouse(player, inventory, Item(2013), "UI tent 2", false);
        Require(DemoSettlementController.Instance.Established, "Q2 shop and two tents unlock the existing door");
        var interior = shop?.GameObject.GetComponent<DemoShopInterior>();
        Require(interior != null && interior.Storage != null, "Q2 existing shop storage");
        Vector3 front = interior.outsideSpawn.position;
        // 문은 작성된 바깥 스폰에서 접근한다. 일반 자원용 Face의 방사형 지면 탐색은
        // 건물 벽/지붕을 바닥으로 고른 뒤 문 뒤편으로 돌 수 있어 여기서는 사용하지 않는다.
        var doorController = player.GetComponent<CharacterController>();
        if (doorController != null) doorController.enabled = false;
        Vector3 doorLook = Vector3.ProjectOnPlane(interior.entrance.transform.position - front, Vector3.up);
        player.transform.SetPositionAndRotation(front, Quaternion.LookRotation(doorLook.normalized));
        if (doorController != null) doorController.enabled = true;
        player.GetComponent<PlayerController>()?.ResetMotionAfterTeleport();
        Note("FIXTURE authored outside door spawn " + front + " facing " + doorLook.normalized);
        await Press(Key.X); await Delay(500);
        Require(Prompt().Contains("상점 들어가기"), "Q2 authored outside spawn reaches door prompt: " + Prompt());
        await Press(Key.E); await Until(() => interior.IsInside, "Q2 normal door fade", 3);
        Require(interior.IsInside, "Q2 product door enters shop");
        Face(player, interior.Storage.transform, 1.3f, "UI chest");
        await Press(DigitKey(pickIndex)); await Delay(300); await Press(Key.E); await Delay(450);
        var storage = StorageUI.instance;
        Require(storage != null && storage.IsOpen, "Q2 E opens chest");
        var store = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).First(b => b.name == "StoreSelectedButton");
        await ClickUI(store.gameObject, "Q2 store used axe"); await Delay(300);
        Check(inventory.CountItems(axe) == 0 && interior.Storage.items.Count == 1 && ToolMetadata(interior.Storage.items[0]),
            "Q2 store transfers the used axe with all metadata");
        await Shot("Q2_used_tool_stored");
        var filler = Item(1);
        int filled = FillBag(inventory, filler); Note("FIXTURE full bag with " + filled + " filler units");
        await Delay(300);
        var take = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).First(b => b.name == "StorageSlot_01");
        await ClickUI(take.gameObject, "Q2 take with full bag"); await Delay(300);
        Check(interior.Storage.items.Count == 1 && ToolMetadata(interior.Storage.items[0]) && inventory.CountItems(axe) == 0,
            "Q2 a full bag retains the complete used tool in the chest");
        await Shot("Q2_bag_full");
        inventory.RemoveItems(filler, filled); Note("FIXTURE remove only filler units"); await Delay(200);
        take = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).First(b => b.name == "StorageSlot_01");
        await ClickUI(take.gameObject, "Q2 take used axe after making room"); await Delay(300);
        int restored = HotbarIndex(inventory, axe);
        Check(restored >= 0 && ToolMetadata(inventory.hotbar.slots[restored].instance) && interior.Storage.items.Count == 0,
            "Q2 take restores the complete used tool to the hotbar once");
        for (int i = 0; i < interior.Storage.maxSlotCount; i++) interior.Storage.AddInstance(new ItemInstance(filler, 1) { currentPrice = i + 1 });
        Note("FIXTURE chest full with distinct filler entries");
        await Press(DigitKey(HotbarIndex(inventory, wood))); await Delay(500);
        int woodBefore = inventory.CountItems(wood);
        await ClickUI(store.gameObject, "Q2 store with full chest"); await Delay(200);
        Check(!store.interactable && inventory.CountItems(wood) == woodBefore && interior.Storage.items.Count == interior.Storage.maxSlotCount,
            "Q2 a full chest disables store and conserves held materials");
        await Shot("Q2_chest_full");
        interior.Storage.items.Clear(); Note("FIXTURE clear only filler entries");
        await Press(Key.S, 1400); await Delay(300);
        Check(!storage.IsOpen, "Q2 leaving the chest closes the panel");
        // D1: 기존 배치/진열 입력 경로로 가격 창의 새 아이콘까지 확인한다.
        var iconStand = await PlaceWithMouse(player, inventory, Item(2020), "D1 icon display stand", false, shop.GameObject.transform.position, 0);
        var iconSlot = iconStand != null ? iconStand.GameObject.GetComponentInChildren<ShopSlot>() : null;
        Require(iconSlot != null, "D1 display stand");
        Face(player, iconSlot.transform, 1.2f, "D1 plank display");
        await Press(DigitKey(HotbarIndex(inventory, plank))); await Delay(300);
        await Press(Key.E); await Delay(400);
        Require(!iconSlot.IsEmpty && iconSlot.currentItem.data == plank, "D1 plank stocked by E");
        await Press(Key.X); await Delay(250); await Press(Key.E); await Delay(400);
        Require(ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen, "D1 price panel opened by E");
        var priceIcon = (UnityEngine.UI.Image)typeof(ShopPriceUI).GetField("_itemIcon", Private).GetValue(ShopPriceUI.instance);
        Check(priceIcon != null && priceIcon.enabled && priceIcon.sprite == plank.icon, "D1 price panel uses the unified item icon");
        await Shot("D1_price_icon");
        await Press(Key.Escape); await Delay(200);
        if (PA_DemoThemeReview.Active) await ThemeSupplementalPanels();
        Finish("PASS");
    }

    static async Task ThemeSupplementalPanels()
    {
        await Press(Key.Escape); await Delay(500);
        Require(PauseManager.Instance != null && PauseManager.Instance.IsPaused, "D2 Esc opens pause");
        await Shot("D2_pause"); await Press(Key.Escape); await Delay(300);
        await Press(Key.P); await Delay(700);
        Require(SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen, "D2 P opens phone");
        await Shot("D2_phone_home");
        var record = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)
            .FirstOrDefault(b=>b.name == "RecordPanel_Tile");
        Require(record != null, "D2 record tile");
        await ClickUI(record.gameObject,"D2 phone record"); await Delay(300); await Shot("D2_feed");
        await Press(Key.Escape); await Delay(300);
        Note("FIXTURE settings panel via existing phone API; visual coverage, not new home navigation");
        int settings = Array.FindIndex(SmartphoneUI.instance.tabPanels,p=>p != null && p.GetComponentInChildren<SettingsUI>(true) != null);
        Require(settings >= 0,"D2 existing settings panel");
        SmartphoneUI.instance.SelectTab(settings); await Delay(300); await Shot("D2_settings");
        SmartphoneUI.instance.Close(); await Delay(500);
        Note("FIXTURE dialogue and report data only for presentation coverage; no sale/score granted");
        DialogueUI.instance.Show("로건","가게를 채우고 나면 오늘의 첫 손님을 맞이해 봐요.");
        await Delay(1800); await Shot("D2_dialogue"); DialogueUI.instance.Hide();
        var bubble = UnityEngine.Object.FindObjectsByType<NpcBubbleUI>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault();
        if (bubble != null) { bubble.Show("오늘은 어떤 물건이 들어왔나요?",8); await Delay(250); await Shot("D2_bubble"); bubble.HideBubble(); }
        var report = PioneerReportCardUI.Show(new DemoPioneerReportData { settlement=25, commerce=22, development=18, exploration=16,total=81,sales=4,revenue=120,rejections=1,rankKey="A",commentKeys=new[]{"settlement.established","commerce.first_customers"},activities=new[]{"craft"},visitedBiomes=new[]{"Grassland"}},null);
        await Delay(900); await Shot("D2_report"); report.Hide();
        Note("FIXTURE reel panel with no reward callback for visual coverage only");
        var fishing = new GameObject("D2ReelPresentation").AddComponent<FirstDayFishingMinigame>();
        fishing.Open(Item(8),null); await Delay(200); await Shot("D2_fishing"); fishing.CloseSilently();
    }

    static bool ToolMetadata(ItemInstance item) => item != null && item.count == 1 && item.durabilityUsed == 1
        && Mathf.Approximately(item.quality, 1.25f) && item.currentPrice == 31;

    static async Task DragUi(InventorySlotUI source, Vector2 destination, string label)
    {
        var origin = ScreenCenter((RectTransform)source.transform);
        await MoveMouse(origin); await Delay(150);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = origin }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, true));
        await Delay(150);
        for (int i = 1; i <= 6; i++)
        {
            mousePosition = Vector2.Lerp(origin, destination, i / 6f);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = mousePosition }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, true));
            await Delay(100);
        }
        InputSystem.QueueStateEvent(mouse, new MouseState { position = destination });
        await Delay(250);
        Note("INPUT real UI pointer drag " + label + " " + origin.ToString("F0") + " -> " + destination.ToString("F0"));
    }

    static async Task ExerciseCraft(GameObject player, Inventory inventory)
    {
        var controller = player.GetComponent<WorldHotbarPlacementController>();
        var placement = UnityEngine.Object.FindFirstObjectByType<WorldBuildingPlacementService>();
        var settlement = DemoSettlementController.Instance;
        Require(controller != null && placement != null && settlement != null, "placement controller/service/settlement");
        var grid = placement.GetComponent<WorldGridService>();
        float cell = grid.Definition.CellSize;
        var module = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentInputModule : null;
        Note("UI input module " + (module != null ? module.GetType().Name : "none"));
        var wbKit = Item(2014); var shopKit = Item(2012); var tentKit = Item(2013); var standKit = Item(2020);
        var wood = Item(3); var ore = Item(4); var plank = Item(7); var pickaxe = Item(2002); var improved = Item(2022);

        // 1) 배치 문법 — 작업대 키트.
        int wbSlot = HotbarIndex(inventory, wbKit);
        Require(wbSlot >= 0, "workbench kit on hotbar");
        await Press(DigitKey(wbSlot)); await Delay(500);
        Check(controller.IsPlacing, "P6 holding the workbench kit starts a placement preview");
        Check(Prompt().Contains("마우스 위치") && Prompt().Contains("[E] 확정") && Prompt().Contains("[R] 회전") && Prompt().Contains("취소"),
            "P6 placement prompt shows mouse/E/R/Esc grammar in Korean (" + Prompt() + ")");
        var ring = GameObject.Find("PlacementRange");
        Check(ring != null && ring.activeInHierarchy, "P6 placement range ring is visible");
        Vector3 feet = player.transform.position;
        // 불가 위치: 도착 항구(모래)의 보급 상자 쪽 → 근처 지점 순서로 마우스를 옮긴다.
        var aims = new System.Collections.Generic.List<Vector3> { FirstDayWorldPresentation.Instance.Supply.transform.position };
        for (int a = 0; a < 12; a++) aims.Add(feet + Quaternion.Euler(0f, a * 30f, 0f) * Vector3.forward * 3f);
        string invalidStatus = null;
        foreach (var aim in aims)
        {
            await MoveMouse(Camera.main.WorldToScreenPoint(new Vector3(aim.x, feet.y, aim.z))); await Delay(90);
            if (!controller.MouseAiming || controller.Status == "설치 가능") continue;
            invalidStatus = controller.Status;
            await Shot("P6_place_invalid");
            await Press(Key.E); await Delay(250);
            Check(controller.IsPlacing && inventory.CountItems(wbKit) == 1, "P6 E on a blocked spot keeps the preview and the kit (" + invalidStatus + ")");
            break;
        }
        Check(controller.MouseAiming, "P6 moving the mouse aims the preview");
        Check(invalidStatus != null, "P6 a blocked spot reads as not placeable in Korean (" + (invalidStatus ?? "none found") + ")");
        await MoveMouse(new Vector2(12f, 12f)); await Delay(150);
        grid.CellToWorld(controller.PreviewResult.Anchor, out var clamped);
        float reach = Vector3.ProjectOnPlane(clamped - player.transform.position, Vector3.up).magnitude;
        Check(reach <= (WorldHotbarPlacementController.MouseRadiusCells + 1f) * cell, $"P6 a far cursor is clamped to the range around the player ({reach:F1} m)");
        int turnsBefore = controller.PreviewResult.QuarterTurns;
        await Press(Key.R); await Delay(200);
        Check(controller.PreviewResult.QuarterTurns == (turnsBefore + 1) % 4, "P6 R rotates the preview");
        await MouseButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right); await Delay(200);
        Check(!controller.IsPlacing && inventory.CountItems(wbKit) == 1, "P6 right click cancels and keeps the kit");
        var benchRuntimeFirst = await PlaceWithMouse(player, inventory, wbKit, "workbench", false);
        var bench = benchRuntimeFirst != null ? benchRuntimeFirst.GameObject.GetComponent<DemoPlacedObject>() : null;
        Require(bench != null, "placed workbench");

        // Hold E 이동 → Esc 취소 복구.
        Require(placement.TryGetPlacement(bench.InstanceId, out var benchRuntime), "workbench runtime");
        var benchAnchor = benchRuntime.Anchor;
        Face(player, bench.transform, 1.3f, "workbench");
        await Press(Key.X); await Delay(300);
        await Press(Key.E, 800); Note("INPUT hold E 0.8 s on the workbench");
        await Delay(200);
        Check(controller.IsPlacing, "P6 holding E on a placed workbench starts a move preview");
        await Press(Key.Escape); await Delay(300);
        placement.TryGetPlacement(bench.InstanceId, out benchRuntime);
        Check(!controller.IsPlacing && benchRuntime.Anchor == benchAnchor, "P6 Esc cancels the move and the workbench stays");

        // 2) 작업대 제작(Codex UI): 잠김·재료 부족·닫기/이동.
        Face(player, bench.transform, 1.3f, "workbench");
        await Delay(300);
        Check(Prompt().Contains("작업대 · 제작"), "P6 workbench prompt (" + Prompt() + ")");
        await Press(Key.E); await Delay(500);
        var ui = CraftingUI.instance;
        Check(ui != null && ui.IsOpen, "P6 E at the workbench opens crafting");
        Require(ui != null && ui.IsOpen, "crafting open");
        Note("CARDS " + string.Join(" | ", Cards(ui).Select(c => Plain(c.Value))));
        var plankCard = Card(ui, "판재");
        Check(plankCard.Key != null && Plain(plankCard.Value).Contains("재료 부족") && !plankCard.Key.GetComponent<UnityEngine.UI.Button>().interactable,
            "P6 판재 card shows 재료 부족 and is disabled with no Wood");
        var lockedCard = Card(ui, "개선 곡괭이");
        Check(lockedCard.Key != null && Plain(lockedCard.Value).Contains("광업 전문 분야를 선택해야"), "P6 개선 곡괭이 is locked until 광업 is chosen");
        var tierCard = Card(ui, "목제 가구");
        Check(tierCard.Key != null && Plain(tierCard.Value).Contains("등급 2"), "P6 목제 가구 shows its approval-tier lock");
        await Shot("P6_craft_locked");
        await ClickUI(plankCard.Key.gameObject, "disabled 판재 card");
        Check(inventory.CountItems(plank) == 0, "P6 clicking a card without materials crafts nothing");
        Vector3 standing = player.transform.position;
        await Press(Key.W, 400);
        Check(Vector3.Distance(standing, player.transform.position) < .05f, "P6 the player does not walk while crafting is open");
        await Press(Key.Escape); await Delay(300);
        Check(!ui.IsOpen, "P6 Esc closes crafting");
        inventory.TryReceiveToHotbar(wood, 8); inventory.TryReceiveToHotbar(ore, 2); inventory.RefreshAllUI();
        Note("FIXTURE Wood +8, Ore +2 (direct gathering verified in P3)");
        await Press(Key.E); await Delay(500);
        plankCard = Card(ui, "판재");
        Check(ui.IsOpen && Plain(plankCard.Value).Contains("제작 가능") && plankCard.Key.GetComponent<UnityEngine.UI.Button>().interactable, "P6 판재 card is craftable with Wood");
        await ClickUI(plankCard.Key.gameObject, "판재 card");
        await Delay(250);
        Check(inventory.CountItems(plank) == 1 && inventory.CountItems(wood) == 6 && Status(ui).Contains("판재 ×1 제작 완료") && HotbarIndex(inventory, plank) >= 0,
            $"P6 click crafts one 판재 from 2 Wood ({inventory.CountItems(plank)} 판재, {inventory.CountItems(wood)} Wood, '{Status(ui)}')");
        await ClickUI(Card(ui, "판재").Key.gameObject, "판재 card again");
        await Delay(250);
        Check(inventory.CountItems(plank) == 2 && inventory.CountItems(wood) == 4, "P6 a second click crafts a second 판재");
        await Shot("P6_craft_plank");
        await Press(Key.Escape); await Delay(300);
        standing = player.transform.position;
        await Press(Key.S, 350); await Delay(200);
        Check(!ui.IsOpen && Vector3.Distance(standing, player.transform.position) > .2f, "P6 after closing, the player walks again");

        // 3) 실제 정착: 상점(큰 건물 build view) + 텐트 2.
        var shop = await PlaceWithMouse(player, inventory, shopKit, "shop", true);
        await PlaceWithMouse(player, inventory, tentKit, "tent 1", false);
        await PlaceWithMouse(player, inventory, tentKit, "tent 2", false);
        Require(settlement.Established, "settlement established by placed shop + 2 tents");
        Check(inventory.CountItems(standKit) == 3, "P6 establishing grants 3 display stands");

        // 4) 광업 해금 → 개선 곡괭이 제작.
        Require(settlement.TryChooseRoot(DemoSpecialization.Mining), "광업 root");
        Note("FIXTURE 광업 via TryChooseRoot (phone 상점 앱 input verified in P2)");
        Face(player, bench.transform, 1.3f, "workbench (upgrade)");
        await Press(Key.X); await Delay(300);
        await Press(Key.E); await Delay(500);
        var upgradeCard = Card(ui, "개선 곡괭이");
        Check(ui.IsOpen && Plain(upgradeCard.Value).Contains("제작 가능"), "P6 광업 unlocks the 개선 곡괭이 recipe");
        await Shot("P6_craft_upgrade_ready");
        await ClickUI(upgradeCard.Key.gameObject, "개선 곡괭이 card");
        await Delay(250);
        Check(inventory.CountItems(improved) == 1 && inventory.CountItems(pickaxe) == 0 && inventory.CountItems(wood) == 2 && inventory.CountItems(ore) == 0,
            "P6 개선 곡괭이 consumes the pickaxe, 2 Wood and 2 Ore");
        await Press(Key.Escape); await Delay(300);

        // 5) 개선 곡괭이 직접 사용: 같은 바위를 더 적은 스윙으로 캔다.
        int upSlot = HotbarIndex(inventory, improved);
        Require(upSlot >= 0, "improved pickaxe on hotbar");
        await Press(DigitKey(upSlot)); await Delay(250);
        var rock = Nearest<MiningSpot>(player.transform.position, m => !m.DirectDepleted && m.isActiveAndEnabled);
        Require(rock != null, "an unmined rock");
        int baseHits = (int)typeof(MiningSpot).GetField("_directHits", Private).GetValue(rock);
        Face(player, rock.transform, 1.2f, "rock (improved pickaxe)");
        await Delay(300);
        var upInstance = inventory.GetSelectedInstance();
        int swings = 0;
        while (swings < 6 && DropFor(ore) == null) { await Press(Key.E); swings++; await Delay(650); }
        Check(DropFor(ore) != null && swings < baseHits && upInstance.durabilityUsed == 1,
            $"P6 개선 곡괭이 breaks a {baseHits}-hit rock in {swings} swings and uses 1 durability");
        await Shot("P6_upgrade_mining");
        await Press(Key.X); await Delay(200); await Press(Key.E); await Delay(400);

        // 6) 상점 안: 문 E로 들어가 마우스로 가판대를 놓고 판재를 진열한다(한국어 이름·도마 모델·가격이 모델 위에).
        var interior = shop != null ? shop.GameObject.GetComponent<DemoShopInterior>() : null;
        Require(interior != null && interior.Storage != null, "shop interior storage chest");
        // 문 바깥 스폰에 서서 문을 바라본다(P2와 같은 접근 fixture).
        Vector3 doorFront = interior.outsideSpawn.position;
        Face(player, interior.entrance.transform, Vector3.ProjectOnPlane(interior.entrance.transform.position - doorFront, Vector3.up).magnitude, "shop door", doorFront);
        await Press(Key.X); await Delay(500);
        Check(Prompt().Contains("상점 들어가기"), "P6 door prompt (" + Prompt() + ")");
        await Press(Key.E); await Delay(900);
        Check(interior.IsInside, "P6 E at the door enters the shop");
        var standRuntime = await PlaceWithMouse(player, inventory, standKit, "display stand", false, shop.GameObject.transform.position, 0);
        var slot = standRuntime != null ? standRuntime.GameObject.GetComponentInChildren<ShopSlot>() : null;
        Require(slot != null, "stand slot");
        int plankSlot = HotbarIndex(inventory, plank);
        Require(plankSlot >= 0, "판재 on hotbar");
        Face(player, slot.transform, 1.2f, "display stand");
        await Press(DigitKey(plankSlot)); await Delay(300);
        await Press(Key.E); await Delay(500);
        Check(!slot.IsEmpty && slot.currentItem.data == plank, "P6 판재 is displayed on the stand");
        var label = slot.GetComponentsInChildren<TMPro.TextMeshPro>(true).FirstOrDefault(t => t.name == "ItemLabel");
        Note("DISPLAY label='" + (label != null ? label.text.Replace("\n", " / ") : "none") + "' meshes=" + string.Join(",", slot.GetComponentsInChildren<MeshFilter>(true).Where(m => m.sharedMesh != null).Select(m => m.sharedMesh.name).Distinct()));
        Check(label != null && label.text.Contains("판재") && label.text.Contains(slot.EffectiveDisplayPrice + "G"), "P6 stand label reads 판재 and its price");
        Check(slot.GetComponentsInChildren<MeshFilter>(true).Any(m => m.sharedMesh != null && m.sharedMesh.name.ToLowerInvariant().Contains("cutting")),
            "P6 판재 shows the local wooden board model (not the fruit)");
        var shownModel = slot.transform.Find("ShopSlot_Display/ItemModel");
        if (label != null && shownModel != null)
        {
            float top = shownModel.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds.max.y).DefaultIfEmpty(float.MinValue).Max();
            Check(label.transform.position.y > top + .1f, $"P6 price label sits above the displayed model ({label.transform.position.y:F2} > {top:F2})");
        }
        Check(Prompt().Contains("판재"), "P6 stand prompt uses the Korean name (" + Prompt() + ")");
        await Shot("P6_plank_display");

        // 7) 실내 보관함: 상자 E → 넣기(칸 전체)·꺼내기·가득 참·걸어 나가면 닫힘·저장 기록.
        Face(player, interior.Storage.transform, 1.3f, "storage chest");
        await Delay(300);
        Check(Prompt().Contains("보관함"), "P6 chest prompt (" + Prompt() + ")");
        int woodSlot = HotbarIndex(inventory, wood);
        Require(woodSlot >= 0, "Wood on hotbar");
        await Press(DigitKey(woodSlot)); await Delay(250);
        int woodHeld = inventory.CountItems(wood);
        await Press(Key.E); await Delay(500);
        var storageUi = StorageUI.instance;
        Check(storageUi != null && storageUi.IsOpen && storageUi.CurrentBox == interior.Storage, "P6 E at the chest opens the shop storage");
        Require(storageUi != null && storageUi.IsOpen, "storage open");
        var store = storageUi.GetComponentsInChildren<UnityEngine.UI.Button>(true).FirstOrDefault(b => b.name == "StoreSelectedButton");
        if (store == null) store = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(b => b.name == "StoreSelectedButton");
        Require(store != null, "store button");
        Note("STORE button '" + store.GetComponentInChildren<TMPro.TMP_Text>().text + "'");
        await ClickUI(store.gameObject, "선택 핫바 보관");
        await Delay(250);
        int boxed = interior.Storage.items.Where(i => i != null && i.data == wood).Sum(i => i.count);
        Check(boxed == woodHeld && inventory.CountItems(wood) == 0, $"P6 storing moves the whole Wood stack into the chest ({boxed}/{woodHeld})");
        Check(settlement.CaptureSaveState().placedBuildings.Any(b => b.storedItems.Any(s => s.itemId == wood.id && s.count == woodHeld)),
            "P6 the chest contents are in the existing demo save record");
        await Shot("P6_storage_stored");
        var firstSlot = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(b => b.name == "StorageSlot_01");
        Require(firstSlot != null, "storage slot 1");
        await ClickUI(firstSlot.gameObject, "storage slot 1");
        await Delay(250);
        Check(inventory.CountItems(wood) == woodHeld && interior.Storage.items.Count == 0 && HotbarIndex(inventory, wood) >= 0,
            "P6 clicking the slot takes the same quantity back onto the hotbar");
        var filler = Item(1);
        for (int i = 0; i < interior.Storage.maxSlotCount; i++) interior.Storage.AddInstance(new ItemInstance(filler, 1) { currentPrice = i + 1 });
        Note("FIXTURE chest filled with " + interior.Storage.maxSlotCount + " distinct single entries");
        await Press(DigitKey(HotbarIndex(inventory, wood))); await Delay(400);
        string fullText = store.GetComponentInChildren<TMPro.TMP_Text>().text;
        await ClickUI(store.gameObject, "store with a full chest");
        await Delay(250);
        Check(!store.interactable && fullText.Contains("가득") && inventory.CountItems(wood) == woodHeld, "P6 a full chest refuses and keeps the bag stack (" + fullText + ")");
        await Shot("P6_storage_full");
        interior.Storage.items.Clear();
        Note("FIXTURE cleared the filler entries");
        await Press(Key.S, 1400); await Delay(300);
        Check(!storageUi.IsOpen, "P6 walking away closes the storage");
        Finish("PASS");
    }

    // P7: 항구에서 거닐며 다가오면 바라봄 → 텐트를 받으면 자기 집으로 → 정착 뒤 광부는 기본 곡괭이로 실제 채굴, 농부는 들판 일과
    //     → 전화 상점 앱의 Root4·동행 궁합·다음 단계 → 개선 곡괭이 지급(이전 도구 반환·손 도구 교체·생산 간격 단축) → 생산물 받기.
    static async Task ExerciseCompanions(GameObject player, Inventory inventory)
    {
        var settlement = DemoSettlementController.Instance;
        Require(settlement != null && settlement.Residents.Count == 2, "two companions");
        var residents = settlement.Residents.ToArray();
        var routines = residents.Select(r => r.GetComponent<DemoCompanionRoutine>()).ToArray();
        Require(routines.All(r => r != null), "companion routines");
        await Until(() => routines.All(r => r.Agent != null && r.Agent.isOnNavMesh), "companions stand on the NavMesh", 6);
        var miner = residents.First(r => r.CompanionId.StartsWith("Miner"));
        var farmer = residents.First(r => r != miner);
        var minerRoutine = miner.GetComponent<DemoCompanionRoutine>();
        var farmerRoutine = farmer.GetComponent<DemoCompanionRoutine>();
        Note($"COMPANIONS {minerRoutine.DisplayName}({miner.CompanionId}) {farmerRoutine.DisplayName}({farmer.CompanionId})");
        foreach (var r in residents)
        {
            var agent = r.GetComponent<UnityEngine.AI.NavMeshAgent>();
            bool sampled = UnityEngine.AI.NavMesh.SamplePosition(r.transform.position, out var near, 6f, UnityEngine.AI.NavMesh.AllAreas);
            Note($"AGENT {r.CompanionId}: agent={(agent != null)} enabled={(agent != null && agent.enabled)} onMesh={(agent != null && agent.isOnNavMesh)} " +
                 $"pos={r.transform.position:F1} navmesh6m={sampled} gap={(sampled ? Vector3.Distance(near.position, r.transform.position) : -1f):F2} " +
                 $"components={string.Join(",", r.GetComponents<Component>().Select(c => c.GetType().Name))}");
        }

        // 1) 항구: 가만히 선 장식이 아니라 거닐고, 다가가면 멈춰 바라본다.
        var start = residents.Select(r => r.transform.position).ToArray();
        await Delay(4500);
        float moved = residents.Select((r, i) => Vector3.Distance(r.transform.position, start[i])).Max();
        Check(moved > .3f, $"P7 companions walk around the harbour instead of standing still (moved {moved:F1} m)");
        Face(player, miner.transform, 1.6f, "miner at the harbour");
        await Press(Key.X); await Delay(1500);
        Vector3 toPlayer = Vector3.ProjectOnPlane(player.transform.position - miner.transform.position, Vector3.up).normalized;
        Check(Vector3.Dot(miner.transform.forward, toPlayer) > .7f, "P7 a companion stops and turns to the approaching player");
        Face(player, miner.transform, 1.5f, "miner (prompt)");
        await Delay(400);
        Check(Prompt().Contains(minerRoutine.DisplayName) && Prompt().Contains("기다리는 중"), "P7 companion prompt shows the name and current activity (" + Prompt() + ")");
        await Shot("P7_harbour_greet");

        // 2) 정착: 상점 + 텐트 2를 마우스로 놓으면 각자 자기 텐트를 집으로 삼고 걸어간다.
        await PlaceWithMouse(player, inventory, Item(2012), "shop", true);
        await PlaceWithMouse(player, inventory, Item(2013), "tent 1", false);
        await PlaceWithMouse(player, inventory, Item(2013), "tent 2", false);
        Require(settlement.Established, "settlement established");
        Check(residents.All(r => r.Home != null && !string.IsNullOrEmpty(r.TentId)) && residents[0].TentId != residents[1].TentId,
            "P7 each companion owns a different tent as home");
        // 3) 광부는 기본 곡괭이로 실제 채굴을 시작한다(생산자 FSM의 이동·작업).
        var producer = miner.Worksite != null ? miner.Worksite.Producer : null;
        Require(producer != null, "miner producer");
        Check(producer.StarterWorking && producer.DemoEquippedTool != null && producer.DemoEquippedTool.data == Item(2002),
            "P7 after settling, the miner starts real work with the starter pickaxe");
        float before = producer.EffectiveInterval;
        Note($"MINER at settlement: state={producer.CurrentState} working={producer.StarterWorking} interval={before:F1}s " +
             $"workSpot={(producer.workSpot != null ? producer.workSpot.name : "none")} distance={(producer.workSpot != null ? Vector3.Distance(miner.transform.position, producer.workSpot.position) : -1f):F1}m");
        await Until(() => producer.CurrentState == ProducerNpcController.State.Working || producer.StarterBatchReady, "the miner reaches the rock", 90);
        {
            var tri = UnityEngine.AI.NavMesh.CalculateTriangulation();
            var b = new Bounds(tri.vertices.Length > 0 ? tri.vertices[0] : Vector3.zero, Vector3.zero);
            foreach (var v in tri.vertices) b.Encapsulate(v);
            Note($"NAVMESH bounds min={b.min:F0} max={b.max:F0} verts={tri.vertices.Length}");
            UnityEngine.AI.NavMesh.SamplePosition(miner.Home.position, out var homeHit, 4f, UnityEngine.AI.NavMesh.AllAreas);
            foreach (var rock in UnityEngine.Object.FindObjectsByType<MiningSpot>(FindObjectsSortMode.None).Where(m => m.IsDirectWorld)
                         .OrderBy(m => Vector3.Distance(m.transform.position, miner.Home.position)).Take(5))
            {
                bool s3 = UnityEngine.AI.NavMesh.SamplePosition(rock.transform.position, out var r3, 3f, UnityEngine.AI.NavMesh.AllAreas);
                bool s10 = UnityEngine.AI.NavMesh.SamplePosition(rock.transform.position, out var r10, 10f, UnityEngine.AI.NavMesh.AllAreas);
                var path = new UnityEngine.AI.NavMeshPath();
                bool ok = s10 && UnityEngine.AI.NavMesh.CalculatePath(homeHit.position, r10.position, UnityEngine.AI.NavMesh.AllAreas, path);
                Note($"ROCK {rock.name} at {rock.transform.position:F0} d={Vector3.Distance(rock.transform.position, miner.Home.position):F0} s3={s3} s10={s10}({(s10 ? Vector3.Distance(r10.position, rock.transform.position) : -1):F1}) path={(ok ? path.status.ToString() : "none")}");
            }
        }
        {
            var agent = miner.GetComponent<UnityEngine.AI.NavMeshAgent>();
            var fromMiner = new UnityEngine.AI.NavMeshPath();
            var fromHome = new UnityEngine.AI.NavMeshPath();
            UnityEngine.AI.NavMesh.SamplePosition(producer.workSpot.position, out var spotHit, 4f, UnityEngine.AI.NavMesh.AllAreas);
            UnityEngine.AI.NavMesh.SamplePosition(miner.Home.position, out var homeHit2, 4f, UnityEngine.AI.NavMesh.AllAreas);
            UnityEngine.AI.NavMesh.CalculatePath(miner.transform.position, spotHit.position, UnityEngine.AI.NavMesh.AllAreas, fromMiner);
            UnityEngine.AI.NavMesh.CalculatePath(homeHit2.position, spotHit.position, UnityEngine.AI.NavMesh.AllAreas, fromHome);
            Note($"MINER path: agent hasPath={agent.hasPath} status={agent.pathStatus} remaining={agent.remainingDistance:F1} dest={agent.destination:F0} stopped={agent.isStopped} " +
                 $"| miner->spot {fromMiner.status} | home->spot {fromHome.status} | miner at {miner.transform.position:F0} home {miner.Home.position:F0} spot {producer.workSpot.position:F0}");
        }
        float workAt = Time.time;
        Require(!producer.StarterBatchReady, "batch not finished before the upgrade");
        Check(miner.GetComponentsInChildren<Transform>(true).Any(t => t.name == "CompanionTool_2002"), "P7 the miner holds the starter pickaxe while working");
        Check(Vector3.Distance(miner.transform.position, producer.workSpot.position) < 4f, $"P7 the miner walked to its work spot {producer.workSpot.name} ({Vector3.Distance(miner.transform.position, producer.workSpot.position):F1} m)");
        Face(player, miner.transform, 1.5f, "miner at the rock");
        await Delay(800);
        Check(Prompt().Contains("광석 캐는 중"), "P7 the miner reads as mining (" + Prompt() + ")");
        await Shot("P7_miner_work_before");

        // 5) 개선 곡괭이 지급: 이전 도구가 돌아오고, 손 도구가 바뀌고, 같은 일이 빨라진다.
        var improved = Item(2022); var starter = Item(2002);
        inventory.TryReceiveToHotbar(improved, 1); inventory.RefreshAllUI();
        Note("FIXTURE 개선 곡괭이 +1 (crafting verified in P6)");
        int starterBefore = inventory.CountItems(starter);
        await Press(DigitKey(HotbarIndex(inventory, improved))); await Delay(300);
        Face(player, miner.transform, 1.4f, "miner (gift)");
        await Delay(400);
        Check(Prompt().Contains("도구 전달"), "P7 holding the upgrade at the miner offers the gift (" + Prompt() + ")");
        await Press(Key.E); Note("INPUT E gift"); await Delay(500);
        float after = producer.EffectiveInterval;
        Check(producer.DemoEquippedTool != null && producer.DemoEquippedTool.data == improved && inventory.CountItems(improved) == 0 &&
              inventory.CountItems(starter) == starterBefore + 1, "P7 the miner equips the 개선 곡괭이 and the old pickaxe returns to the hotbar");
        Check(Mathf.Abs(after - before / 1.5f) < .05f, $"P7 mining interval before/after the gift: {before:F1}s -> {after:F1}s");
        await Delay(600);
        Check(miner.GetComponentsInChildren<Transform>(true).Any(t => t.name == "CompanionTool_2022"), "P7 the miner's hand shows the new tool");
        await Shot("P7_gift_after");

        // 6) 생산 효과: 첫 지원 묶음이 준비되면 빈손 E로 받는다.
        await Until(() => producer.StarterBatchReady, "the miner finishes the batch", after + 6f);
        Note($"MINER batch ready {Time.time - workAt:F1}s after starting at the rock (gift at once; base {before:F1}s, upgraded {after:F1}s)");
        int ore = inventory.CountItems(Item(4));
        int batch = producer.StarterStockCount;
        Face(player, miner.transform, 1.4f, "miner (batch)");
        await Press(Key.X); await Delay(500);
        Check(Prompt().Contains("생산물 받기"), "P7 a ready batch reads 생산물 받기 (" + Prompt() + ")");
        await Press(Key.E); Note("INPUT E claim"); await Delay(500);
        Check(batch > 0 && inventory.CountItems(Item(4)) == ore + batch && !producer.StarterBatchReady, $"P7 claiming gives the miner's {batch} Ore");
        await Shot("P7_batch_claimed");

        await Until(() => farmerRoutine.ReachedHomeOnce, "the farmer walks home", 60);
        Check(farmerRoutine.ReachedHomeOnce, "P7 the farmer walked to their own tent (" + farmerRoutine.Status + ")");
        Face(player, farmer.transform, 2.2f, "farmer");
        await Delay(500);
        await Shot("P7_farmer_day");

        // 4) 전화 상점 앱: 네 분야·동행 궁합 → 광업 선택 → 다음 1단계 미리보기.
        await Press(Key.P); Note("INPUT P");
        await Until(() => SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen, "phone open", 5);
        await Delay(600);
        var tile = SmartphoneUI.instance.homeScreen != null ? SmartphoneUI.instance.homeScreen.transform.Find("ShopPanel_Tile") : null;
        Require(tile != null, "상점 tile");
        await ClickUI(tile.gameObject, "상점 tile");
        await Delay(600);
        var app = UnityEngine.Object.FindFirstObjectByType<ShopManagementPhoneUI>();
        Require(app != null, "shop app");
        var growth = app.GetComponentsInChildren<TMPro.TMP_Text>(true).FirstOrDefault(t => t.name == "Growth");
        var rootButtons = Enumerable.Range(0, 4).Select(i => app.GetComponentsInChildren<UnityEngine.UI.Button>(true).FirstOrDefault(b => b.name == "Root" + i)).ToArray();
        Require(growth != null && rootButtons.All(b => b != null), "growth text and 4 root buttons");
        string labels = string.Join("/", rootButtons.Select(b => b.GetComponentInChildren<TMPro.TMP_Text>().text));
        Note("ROOTS " + labels + " | " + growth.text.Replace("\n", " / "));
        Check(rootButtons.All(b => b.gameObject.activeInHierarchy) && labels.Contains("광업 ★") && labels.Contains("농업 ★") && growth.text.Contains("동행 궁합"),
            "P7 four roots are readable with the companion synergy marks");
        // 실제 휠로 상점 앱을 내려 전문 분야 버튼이 화면에 보이게 한다.
        var appRect = (RectTransform)app.transform;
        var appScroll = app.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
        {
            var data = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = ScreenCenter(appRect) };
            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(data, hits);
            var module = UnityEngine.EventSystems.EventSystem.current.currentInputModule as UnityEngine.InputSystem.UI.InputSystemUIInputModule;
            Note($"SCROLL under wheel: {string.Join(" > ", hits.Take(4).Select(h => h.gameObject.name))} | scrollRect={(appScroll != null ? appScroll.name + " pos=" + appScroll.verticalNormalizedPosition.ToString("F2") + " sens=" + appScroll.scrollSensitivity : "none")} " +
                 $"| module={(module != null ? "InputSystemUI scroll=" + (module.scrollWheel != null && module.scrollWheel.action != null ? module.scrollWheel.action.enabled + ":" + string.Join(",", module.scrollWheel.action.bindings.Select(x => x.effectivePath)) : "null") : (UnityEngine.EventSystems.EventSystem.current.currentInputModule != null ? UnityEngine.EventSystems.EventSystem.current.currentInputModule.GetType().Name : "none"))}");
        }
        for (int i = 0; i < 10 && !TopHit(rootButtons[1].gameObject); i++)
            await ScrollAt(ScreenCenter(appRect), -120f);
        Note("SCROLL after wheel pos=" + (appScroll != null ? appScroll.verticalNormalizedPosition.ToString("F2") : "-"));
        Check(TopHit(rootButtons[1].gameObject), "P7 wheel-scrolling the shop app reveals the root buttons");
        await Shot("P7_phone_roots");
        await ClickUI(rootButtons[1].gameObject, "광업");
        await Delay(400);
        Check(settlement.SelectedRoot == DemoSpecialization.Mining && growth.text.Contains("다음 단계") && growth.text.Contains("금속 가공"),
            "P7 choosing 광업 with the license point shows its next step (" + growth.text.Replace("\n", " / ") + ")");
        await Shot("P7_phone_root_preview");
        await Press(Key.Escape); await Press(Key.Escape);
        await Until(() => !SmartphoneUI.instance.IsOpen, "phone closed", 4);

        // 7) 농부의 낮 일과와 사건 대사.
        await Until(() => farmerRoutine.Current == DemoCompanionRoutine.Activity.Working || farmerRoutine.Current == DemoCompanionRoutine.Activity.GoingToWork, "the farmer heads out to work", 30);
        Check(farmerRoutine.WorkTarget != null, "P7 the farmer has a field target for the day (" + farmerRoutine.Status + ")");
        double linesDeadline = EditorApplication.timeSinceStartup + 14;
        while ((minerRoutine.LinesSpoken < 2 || farmerRoutine.LinesSpoken < 2) && EditorApplication.timeSinceStartup < linesDeadline) await Delay(250);
        Check(minerRoutine.LinesSpoken >= 2 && farmerRoutine.LinesSpoken >= 2, $"P7 companions react to home/work events in lines ({minerRoutine.LinesSpoken}/{farmerRoutine.LinesSpoken})");
        Finish("PASS");
    }

    // P8: 낮에 직접 얻고 만든 상품(낚시·곤충·벌목→판재)만 진열 → 가격 UI → 밤 P폰 개점 → 관광객·동행 손님의 방문/구매/거절,
    //     동시 평가·평가 중 가격 변경·품절·영업 중 이동 금지·결제/말풍선 → 마감 1회·퇴장 → 매출=판매기록=Report, 행동 댓글.
    static async Task ExerciseBusiness(GameObject player, Inventory inventory)
    {
        var settlement = DemoSettlementController.Instance;
        var loop = DayNightShopLoopController.Instance;
        var arrivals = CustomerArrivalController.Instance;
        Require(settlement != null && loop != null && arrivals != null, "settlement/loop/arrivals");
        var fish = Item(8); var butterfly = Item(2004); var wood = Item(3); var plank = Item(7);
        var decisions = new System.Collections.Generic.List<string>();
        var deciders = new System.Collections.Generic.Dictionary<string, int[]>(); // name -> [buy, reject]
        System.Action<NpcProfile, PurchaseEvaluator.Result, Item, int, string> onDecision = (profile, result, item, price, who) =>
        {
            decisions.Add($"{who}:{(item != null ? item.itemName : "-")}@{price}:{(result.willBuy ? "BUY" : "PASS")}");
            if (!deciders.TryGetValue(who ?? "?", out var counts)) deciders[who ?? "?"] = counts = new int[2];
            counts[result.willBuy ? 0 : 1]++;
        };
        PurchaseFeedbackPresentationController.OnDecisionRecorded += onDecision;
        int closes = 0; int closeRevenue = -1;
        System.Action<int, int, int> onClose = (s, r, j) => { closes++; closeRevenue = r; };
        loop.OpeningShopClosed += onClose;
        try
        {
            // 1) 직접 얻은 상품: 낚시 2, 나비 1, 벌목 2 → 판재 1.
            int fishBefore = inventory.CountItems(fish);
            await CatchFishOnce(player, inventory);
            Check(inventory.CountItems(fish) == fishBefore + 2, "P8 real fishing gives 2 Fish for the shop");
            var bug = UnityEngine.Object.FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Where(b => !b.Captured).OrderBy(b => b.name).First();
            await Press(DigitKey(HotbarIndex(inventory, Item(2003)))); await Delay(250);
            for (int attempt = 0; attempt < 4 && !bug.Captured; attempt++)
            {
                await Until(() => !bug.Fleeing, "butterfly settles", 4);
                FaceBug(player, bug, .9f, "butterfly");
                await Delay(250);
                await Press(Key.E); await Delay(800);
            }
            Check(bug.Captured && inventory.CountItems(butterfly) == 1, "P8 a real net catch gives 1 Butterfly");
            int axeSlot = HotbarIndex(inventory, Item(2001));
            await FellAndPick(player, inventory, wood, axeSlot);
            await FellAndPick(player, inventory, wood, axeSlot);
            Check(inventory.CountItems(wood) == 2, "P8 two felled trees give 2 Wood");
            var bench = await PlaceWithMouse(player, inventory, Item(2014), "workbench", false);
            var benchObject = bench != null ? bench.GameObject.GetComponent<DemoPlacedObject>() : null;
            Require(benchObject != null, "workbench placed");
            Face(player, benchObject.transform, 1.3f, "workbench");
            await Press(Key.X); await Delay(300);
            await Press(Key.E); await Delay(500);
            Require(CraftingUI.instance != null && CraftingUI.instance.IsOpen, "crafting open");
            await ClickUI(Card(CraftingUI.instance, "판재").Key.gameObject, "판재 card");
            await Delay(300);
            await Press(Key.Escape); await Delay(300);
            Check(inventory.CountItems(plank) == 1 && inventory.CountItems(wood) == 0, "P8 the workbench turns the 2 Wood into 1 판재");
            Note("PRODUCTS fish=" + inventory.CountItems(fish) + " butterfly=" + inventory.CountItems(butterfly) + " plank=" + inventory.CountItems(plank) + " (no QA stock)");

            // 2) 정착·가판대 3: 상점 안에 마우스로 놓는다.
            var shop = await PlaceWithMouse(player, inventory, Item(2012), "shop", true);
            await PlaceWithMouse(player, inventory, Item(2013), "tent 1", false);
            await PlaceWithMouse(player, inventory, Item(2013), "tent 2", false);
            Require(settlement.Established, "settlement established");
            var interior = shop.GameObject.GetComponent<DemoShopInterior>();
            Vector3 doorFront = interior.outsideSpawn.position;
            Face(player, interior.entrance.transform, Vector3.ProjectOnPlane(interior.entrance.transform.position - doorFront, Vector3.up).magnitude, "shop door", doorFront);
            await Press(Key.X); await Delay(400);
            await Press(Key.E); await Delay(900);
            Require(interior.IsInside, "inside the shop");
            var stands = new System.Collections.Generic.List<ShopSlot>();
            for (int i = 0; i < 3; i++)
            {
                var stand = await PlaceWithMouse(player, inventory, Item(2020), "stand " + (i + 1), false, shop.GameObject.transform.position, 0);
                var slot = stand != null ? stand.GameObject.GetComponentInChildren<ShopSlot>() : null;
                Require(slot != null, "stand slot " + (i + 1));
                stands.Add(slot);
            }

            // 3) 진열(손에 든 상품 E) → 가격 UI(+10 → 확정).
            var products = new[] { fish, butterfly, plank };
            for (int i = 0; i < 3; i++)
            {
                var empty = stands.First(s => s.IsEmpty);
                Face(player, empty.transform, 1.2f, "empty stand");
                int productSlot = HotbarIndex(inventory, products[i]);
                Check(productSlot >= 0, $"P8 {ItemDisplayName.For(products[i])} waits on the hotbar so it can be held");
                await Press(DigitKey(Mathf.Max(0, productSlot))); await Delay(300);
                Note("PROMPT stock " + ItemDisplayName.For(products[i]) + ": " + Prompt());
                await Press(Key.E); await Delay(500);
                Check(stands.Any(s => !s.IsEmpty && s.currentItem.data == products[i]), $"P8 {ItemDisplayName.For(products[i])} is displayed from the bag");
            }
            var plankStand = stands.First(s => !s.IsEmpty && s.currentItem.data == plank);
            int plankBase = plankStand.EffectiveDisplayPrice;
            await SetPriceWithUI(player, plankStand, 10);
            Check(plankStand.EffectiveDisplayPrice == plankBase + 10, $"P8 price UI sets 판재 {plankBase} -> {plankStand.EffectiveDisplayPrice}G");
            Check(inventory.CountItems(fish) == 0 && inventory.CountItems(butterfly) == 0 && inventory.CountItems(plank) == 0, "P8 all displayed goods came from the bag (nothing granted)");
            await Shot("P8_stocked");

            // 4) 낮: 점수·랭크 상시 노출 없음 → 광업 → 밤.
            Check(!UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Any(t => t.isActiveAndEnabled && (t.text.Contains("/ 100") || t.text.Contains("랭크") || t.text.Contains("Rank "))),
                "P8 no score or rank is shown during the day");
            Require(settlement.TryChooseRoot(DemoSpecialization.Mining), "광업");
            Note("FIXTURE 광업 via TryChooseRoot (phone input verified in P2/P7)");
            await Until(() => GameClock.Instance.enabled && GameClock.Instance.CurrentHour >= 16f, "sunset starts", 10);
            GameClock.Instance.secondsPerGameHour = 1f;
            Note("FIXTURE sunset clock accelerated to 1 s per game hour");
            await Until(() => settlement.NightReady, "night ready", 20);

            // 5) P폰 개점.
            int moneyAtOpen = EconomyService.Instance.Money;
            int dayAtOpen = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
            await Press(Key.P); await Until(() => SmartphoneUI.instance.IsOpen, "phone", 4); await Delay(500);
            await ClickUI(SmartphoneUI.instance.homeScreen.transform.Find("ShopPanel_Tile").gameObject, "상점 tile");
            await Delay(500);
            var app = UnityEngine.Object.FindFirstObjectByType<ShopManagementPhoneUI>();
            var open = app.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "Open");
            await ClickUI(open.gameObject, "영업 시작");
            await Delay(400);
            Check(loop.IsShopOpenForCustomers, "P8 the phone opens the shop at night");
            await Press(Key.Escape); await Press(Key.Escape);
            await Until(() => !SmartphoneUI.instance.IsOpen, "phone closed", 4);
            await Shot("P8_open");

            // 6) 영업 관찰.
            double started = EditorApplication.timeSinceStartup;
            int maxTourists = 0, maxShoppers = 0, stuckFlags = 0;
            bool priceChanged = false, moveTried = false, soldOutSeen = false, bubbleSeen = false, residentShot = false, customersShot = false;
            var lastPos = new System.Collections.Generic.Dictionary<NpcController, Vector3>();
            var lastMove = new System.Collections.Generic.Dictionary<NpcController, double>();
            var flagged = new System.Collections.Generic.HashSet<NpcController>();
            var routines = settlement.Residents.Select(r => r.GetComponent<DemoCompanionRoutine>()).Where(r => r != null).ToArray();
            var displayedPrices = new System.Collections.Generic.HashSet<int>(stands.Select(s => s.EffectiveDisplayPrice));
            System.Action<string> noteCompanions = label =>
            {
                foreach (var r in routines)
                {
                    var agent = r.Agent;
                    var path = new UnityEngine.AI.NavMeshPath();
                    bool ok = agent != null && agent.isOnNavMesh && UnityEngine.AI.NavMesh.CalculatePath(agent.nextPosition, doorFront, UnityEngine.AI.NavMesh.AllAreas, path);
                    Note($"COMPANION {label} {r.DisplayName}: {r.Current} '{r.Status}' at {r.transform.position:F1} door {Vector3.Distance(r.transform.position, doorFront):F1}m " +
                         $"onMesh={(agent != null && agent.isOnNavMesh)} hasPath={(agent != null && agent.hasPath)} dest={(agent != null ? agent.destination.ToString("F0") : "-")} pathToDoor={(ok ? path.status.ToString() : "none")} shopper={(r.Shopper != null ? r.Shopper.currentState.ToString() : "none")} hops={r.OffscreenHops}");
                }
            };
            noteCompanions("open");
            bool companionNote20 = false;
            while (!loop.OpeningSessionCompleted && EditorApplication.timeSinceStartup - started < 170)
            {
                double now = EditorApplication.timeSinceStartup;
                maxTourists = Mathf.Max(maxTourists, arrivals.ActiveTouristCount);
                maxShoppers = Mathf.Max(maxShoppers, arrivals.ActiveShopperCount);
                foreach (var npc in UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None))
                {
                    if (npc.currentState == NpcController.State.Idle) { lastPos.Remove(npc); continue; }
                    if (!lastPos.TryGetValue(npc, out var p) || Vector3.Distance(p, npc.transform.position) > .25f) { lastPos[npc] = npc.transform.position; lastMove[npc] = now; }
                    else if (now - lastMove[npc] > 25 && flagged.Add(npc)) { stuckFlags++; Note($"STUCK {npc.name} state={npc.currentState} at {npc.transform.position:F1}"); }
                }
                bubbleSeen |= UnityEngine.Object.FindObjectsByType<NpcBubbleUI>(FindObjectsSortMode.None).Any(b => b.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.isActiveAndEnabled && t.text.Length > 0));
                if (!soldOutSeen && stands.Any(s => s.IsSoldOutToday)) { soldOutSeen = true; await Shot("P8_sold_out"); }
                if (!residentShot && routines.Any(r => r.Current == DemoCompanionRoutine.Activity.Shopping))
                {
                    var shopper = routines.First(r => r.Current == DemoCompanionRoutine.Activity.Shopping);
                    if (Vector3.Distance(shopper.transform.position, interior.Storage.transform.position) < 12f) { residentShot = true; await Shot("P8_resident_visit"); }
                }
                // 평가 중 가격 변경: 손님이 고르고 있는 가판대의 값을 실제 UI로 바꾼다.
                if (!priceChanged && now - started > 15)
                {
                    var browsing = UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None)
                        .Select(n => typeof(NpcController).GetField("_currentSlotTarget", Private).GetValue(n) as ShopSlot)
                        .FirstOrDefault(s => s != null && !s.IsEmpty && stands.Contains(s));
                    if (browsing != null)
                    {
                        priceChanged = true;
                        int before = browsing.EffectiveDisplayPrice;
                        await SetPriceWithUI(player, browsing, -1);
                        displayedPrices.Add(browsing.EffectiveDisplayPrice);
                        Note($"PRICE changed while a customer evaluates {ItemDisplayName.For(browsing.currentItem != null ? browsing.currentItem.data : null)}: {before} -> {browsing.EffectiveDisplayPrice}G");
                    }
                }
                if (!moveTried && now - started > 45)
                {
                    moveTried = true;
                    var target = stands.First(s => s != null).GetComponentInParent<DemoPlacedObject>();
                    Face(player, target.transform, 1.2f, "stand (move during business)");
                    await Press(Key.X); await Delay(300);
                    await Press(Key.E, 800); await Delay(300);
                    var controller = player.GetComponent<WorldHotbarPlacementController>();
                    Check(!controller.IsPlacing, "P8 a stand cannot be moved while the shop is open");
                    if (ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen) { await Press(Key.Escape); await Delay(200); }
                }
                if (!customersShot && now - started > 25) { customersShot = true; await Shot("P8_business_customers"); }
                if (!companionNote20 && now - started > 20) { companionNote20 = true; noteCompanions("+20s"); }
                await Delay(400);
            }
            Require(loop.OpeningSessionCompleted, "business closed by 22:00");
            Note($"BUSINESS tourists spawned={arrivals.TouristsSpawnedThisOpening} maxConcurrentTourists={maxTourists} maxShoppers={maxShoppers} stuck={stuckFlags}");
            Note("DECISIONS " + string.Join(" | ", decisions));
            Check(arrivals.TouristsSpawnedThisOpening <= 7 && maxTourists <= 3 && arrivals.TouristsSpawnedThisOpening > 0, "P8 tourists respect 7 per opening and 3 at once");
            string[] residentNames = routines.Select(r => r.DisplayName).ToArray();
            var residentDecisions = deciders.Where(kv => residentNames.Contains(kv.Key)).ToArray();
            var touristDecisions = deciders.Where(kv => kv.Key.StartsWith("여행 손님")).ToArray();
            Note("DECIDERS residents=" + string.Join(",", residentDecisions.Select(kv => $"{kv.Key} buy{kv.Value[0]}/pass{kv.Value[1]}")) +
                 " tourists=" + string.Join(",", touristDecisions.Select(kv => $"{kv.Key} buy{kv.Value[0]}/pass{kv.Value[1]}")));
            Check(routines.All(r => r.ShopVisits > 0) || residentDecisions.Length > 0, $"P8 companions visit the shop as resident customers (visits {string.Join("/", routines.Select(r => r.ShopVisits))})");
            Check(residentDecisions.Length > 0 && touristDecisions.Length > 0, "P8 both residents and tourists evaluated goods with their own profiles");
            Check(stuckFlags == 0, "P8 no customer stayed stuck on the way or at a stand for 25 s");
            Check(priceChanged, "P8 a price was changed while a customer was evaluating");
            Check(soldOutSeen, "P8 a stand sold out during business");
            Check(bubbleSeen, "P8 customers show speech feedback");

            // 7) 마감 1회·퇴장·매출 일치·Report.
            await Delay(1500);
            Check(closes == 1, $"P8 the business closes exactly once ({closes})");
            await Until(() => arrivals.ActiveShopperCount == 0 && arrivals.ActiveTouristCount == 0, "customers leave after closing", 30);
            Check(true, "P8 customers leave after closing");
            var sales = SalesLogManager.Instance.GetRecent(100).Where(r => r != null && r.gameDay == dayAtOpen).ToList();
            int moneyDelta = EconomyService.Instance.Money - moneyAtOpen;
            int logged = sales.Sum(r => r.price);
            var report = settlement.PioneerReport;
            Require(report != null, "pioneer report");
            Note($"REVENUE money+{moneyDelta} log={logged} ({sales.Count} sales) closeEvent={closeRevenue} report={report.revenue}/{report.sales} total={report.total} rank={report.rankKey} activities={string.Join(",", report.activities ?? new string[0])} comments={string.Join(",", report.commentKeys ?? new string[0])}");
            Check(moneyDelta == logged && logged == closeRevenue && closeRevenue == report.revenue && sales.Count == report.sales && sales.Count > 0,
                "P8 money gained = sales log = close event = Report revenue");
            Check(sales.All(r => displayedPrices.Contains(r.price)), "P8 every sale used a price that was on display");
            int stocked = 4;
            Check(sales.Count <= stocked, $"P8 simultaneous evaluations never sell more than was displayed ({sales.Count}/{stocked})");
            var acts = report.activities ?? new string[0];
            var comments = report.commentKeys ?? new string[0];
            Check(acts.Contains("fish") && acts.Contains("bug") && acts.Contains("forestry") && acts.Contains("craft"), "P8 the Report records fishing, bug catching, felling and crafting");
            Check(comments.Contains("settlement.established") && comments.Contains("commerce.first_customers") && comments.Contains("development.crafted_goods"),
                "P8 behaviour comments reflect settling, the first customers and crafted goods");
            await Delay(800);
            await Shot("P8_report");
            Finish("PASS");
        }
        finally
        {
            PurchaseFeedbackPresentationController.OnDecisionRecorded -= onDecision;
            loop.OpeningShopClosed -= onClose;
        }
    }

    // P11 연속 경로: 타이틀 → 출항 교육 → 동행 → 항해 → 섬 → 보급 → 정착 → 진열·가격·보관 → Root → 일몰 → 영업 → Report → 저장.
    // INPUT = 가상 키보드/마우스(실제 InputSystem·uGUI 경로). 출항 교육·보급은 실제 걷기(teleport 없음).
    // FIXTURE = 섬의 먼 대상(나무·부지·상점 문·가판대·보관함) 접근 teleport만. 시계 가속·상품 지급·API 판매 없음.
    static async Task ExerciseRoute()
    {
        double routeStart = EditorApplication.timeSinceStartup;
        Func<string> at = () => $"t+{EditorApplication.timeSinceStartup - routeStart:F0}s";
        string saveRoot = SessionState.GetString(SaveManager.ValidationRootSessionKey, string.Empty);
        Require(!string.IsNullOrEmpty(saveRoot) && Directory.Exists(saveRoot), "isolated save root");
        Note("SAVE ROOT (isolated, user save untouched) " + saveRoot);

        // 1) 타이틀: 실제 '새 게임' 버튼 클릭(격리 폴더가 비어 있어 확인 단계 없이 출항 교육으로 간다).
        if (Mode != "intro")
        {
            UnityEngine.UI.Button primary = null;
            await Until(() => (primary = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "PrimaryButton" && b.interactable)) != null, "title primary button", 20);
            await Delay(1500);
            Note("TITLE primary='" + primary.GetComponentInChildren<TMPro.TMP_Text>()?.text + "'");
            await Shot("P11_01_title");
            await ClickUI(primary.gameObject, "새 게임");
        }
        else Note("FIXTURE direct tutorial scene entry; native candidate4 NEWGAME evidence is preserved separately. No inventory/progress/clock injection.");
        DepartureTutorialController t = null;
        await Until(() => (t = UnityEngine.Object.FindFirstObjectByType<DepartureTutorialController>()) != null && t.IsReady, "departure tutorial ready", 30);
        Note($"SCENE departure tutorial {at()} objective='{t.ObjectiveText}'");
        await Delay(1500);
        await Shot("P11_02_departure_start");
        if (AmbienceReview) await ObserveAmbience("harbor");
        if (Mode == "intro")
        {
            await Delay(5200);
            Check(t.Stage == 1 && t.presentation.ObjectiveLabel.gameObject.activeInHierarchy,
                "P11 intro lesson stays visible beyond the former four-second timeout");
            Check(t.fruit.icon != null, "P11 tutorial fruit has a local-model sprite");
            await Shot("P11_intro_objective_persistent");
        }

        // 2) 출항 교육: 실제 걷기·점프 → 빈손 E 나무 → 사과 3개 E → 1–9/X/재선택 → E 진열 → 가격 UI → 손님 구매.
        var player = t.player;
        Vector3 spawn = player.position;
        Check(await WalkTo(player, t.movementCheckpoint.position, t.checkpointRadius * .6f, 20, "tutorial checkpoint"), "P11 WASD walk reaches the first marker");
        await Press(Key.Space); await Delay(900);
        await Until(() => t.Stage >= 2, "stage 2 after walk + jump", 6);
        Check(Vector3.Distance(spawn, player.position) > 1f, $"P11 tutorial stage 1 by real walking and jumping ({at()})");
        await Press(Key.X); await Delay(250);
        Vector3 treeFront = t.fruitTree.transform.position + Vector3.ProjectOnPlane(player.position - t.fruitTree.transform.position, Vector3.up).normalized * 1.5f;
        await WalkTo(player, treeFront, .35f, 20, "fruit tree");
        await WalkTo(player, t.fruitTree.transform.position, 1.35f, 4, "face tree");
        Note("PROMPT tree: " + Prompt());
        await Press(Key.E); Note("INPUT E at the tree (empty hand)");
        await Until(() => t.fruitTree.FruitDropped, "fruit dropped", 6);
        await Delay(1600);
        Note("APPLES " + string.Join(" ", UnityEngine.Object.FindObjectsByType<InventoryFramework.PickupItem>(FindObjectsSortMode.None)
            .Where(p => p != null && p.item == t.fruit).Select(p => $"{p.transform.position:F2}{(p.contextual ? "" : "(not contextual)")}")) + $" player {player.position:F2}");
        for (int i = 0; i < 9 && t.FruitCount < 3; i++)
        {
            await Press(Key.X); await Delay(180);
            var apple = UnityEngine.Object.FindObjectsByType<InventoryFramework.PickupItem>(FindObjectsSortMode.None)
                .Where(p => p != null && p.isActiveAndEnabled && p.item == t.fruit).OrderBy(p => Vector3.Distance(p.transform.position, player.position)).FirstOrDefault();
            if (apple == null) break;
            // 사과 1m 앞에 선 뒤 사과 쪽으로 걸어 들어가며 바라본다(바로 위에 서면 E 대상이 되지 않는다).
            Vector3 away = Vector3.ProjectOnPlane(player.position - apple.transform.position, Vector3.up);
            await WalkTo(player, apple.transform.position + (away.sqrMagnitude > .01f ? away.normalized : Vector3.back) * 1.1f, .2f, 8, "near apple " + (t.FruitCount + 1));
            await WalkTo(player, apple.transform.position, .65f, 3, "face apple " + (t.FruitCount + 1));
            Note("PROMPT apple: " + Prompt());
            await Press(Key.E); await Delay(450);
        }
        await Until(() => t.Stage >= 3, "three apples picked", 6);
        Check(t.FruitCount == 3 && Inventory.instance.CountItems(t.fruit) == 3, $"P11 three apples picked by E from the ground ({at()})");
        int appleSlot = HotbarIndex(Inventory.instance, t.fruit);
        Require(appleSlot >= 0, "apple on hotbar");
        await Press(DigitKey(appleSlot)); await Delay(350); Note("OBJECTIVE " + t.ObjectiveText);
        await Press(Key.X); await Delay(350); Note("OBJECTIVE " + t.ObjectiveText);
        await Press(DigitKey(appleSlot)); await Delay(350); Note("OBJECTIVE " + t.ObjectiveText);
        await ApproachFront(player, t.trainingSlot, "training shelf");
        Note("PROMPT shelf: " + Prompt());
        await Press(Key.E); await Delay(600);
        await Until(() => t.Stage >= 4, "apple stocked", 5);
        Check(!t.trainingSlot.IsEmpty && Inventory.instance.CountItems(t.fruit) == 2, "P11 the held apple is stocked with E (hand lesson complete)");
        int target = Mathf.Max(1, Mathf.FloorToInt(t.fruit.basePrice * .7f));
        for (int attempt = 0; attempt < 3 && !t.Complete; attempt++)
        {
            await Press(Key.X); await Delay(250);
            await ApproachFront(player, t.trainingSlot, "training shelf (price)");
            Note("PROMPT shelf (empty hand): " + Prompt());
            await Press(Key.E); await Delay(500);
            Require(ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen, "tutorial price UI open");
            if (attempt == 0) await Shot("P11_03_tutorial_price");
            await SetPriceTo(Mathf.Max(1, target - attempt));
            await Until(() => t.Complete || t.Stage <= 4, "customer decision", 40);
            Note($"TUTORIAL attempt {attempt + 1}: price {Mathf.Max(1, target - attempt)}G stage={t.Stage} decisions={t.DecisionCount} reaction='{t.LastReaction}'");
        }
        Check(t.Complete && t.SaleAmount > 0, $"P11 departure certification completes with a real customer purchase ({t.SaleAmount}G, {at()})");
        await Delay(1200);
        await Shot("P11_04_certified");
        if (Mode == "intro")
        {
            var controls = t.presentation.transform.Find("PA_DepartureHUD/Controls");
            Check(controls != null && !controls.gameObject.activeInHierarchy,
                "P11 customer reaction remains readable without the controls overlay");
        }

        // 3) 동행 선택 2명 → 출항 → 항해 → 섬 도착(Demo256).
        var selection = UnityEngine.Object.FindFirstObjectByType<DepartureCompanionSelection>();
        Require(selection != null && t.presentation != null && t.presentation.CompanionButton != null, "companion selection");
        await Until(() => t.presentation.CompanionButton.interactable, "companion button enabled", 5);
        await ClickUI(t.presentation.CompanionButton.gameObject, "동행 선택");
        await Until(() => selection.IsOpen && selection.CandidateButtons != null, "selection open", 5);
        await Delay(800);
        foreach (string id in new[] { "Miner_01", "Farmer_01" })
        {
            int index = Array.FindIndex(selection.candidates, c => c.id == id);
            Require(index >= 0, "candidate " + id);
            await ClickUI(selection.CandidateButtons[index].gameObject, "candidate " + id);
            await Delay(300);
        }
        await Shot("P11_05_companions");
        Check(selection.DepartureButton.interactable, "P11 two chosen companions enable departure");
        if (Mode == "intro") { await Press(Key.Enter); Note("INPUT Enter confirms the two selected companions"); }
        else await ClickUI(selection.DepartureButton.gameObject, "출항");
        double sailStart = EditorApplication.timeSinceStartup;
        await Delay(5000);
        if (AmbienceReview) await ObserveAmbience("voyage");
        await Shot("P11_06_voyage");
        DemoRouteController route = null;
        await Until(() => (route = WorldAlphaPlayableController.Instance?.DemoRoute) != null && route.IsPlayable, "island playable", 90);
        Note($"SCENE island playable {at()} (voyage+load {EditorApplication.timeSinceStartup - sailStart:F0}s) companions={string.Join(",", DemoRouteController.SelectedCompanionIds)}");
        Check(DemoRouteController.SelectedCompanionIds.SequenceEqual(new[] { "Miner_01", "Farmer_01" }), "P11 the chosen companions arrive on the island");
        await Delay(2000);
        await Shot("P11_07_island_arrival");
        if (Mode == "intro") { if (AmbienceReview) await ObserveIslandAmbience(); Finish("PASS"); return; }

        // 4) 섬: 보급(실제 걷기) → 나무 2그루 → 상점·텐트 → 실내 가판대 → 진열·가격 → 보관함 → 폰 Root.
        var inventory = Inventory.instance;
        var islandPlayer = inventory.gameObject;
        var settlement = DemoSettlementController.Instance;
        var loop = DayNightShopLoopController.Instance;
        var supply = FirstDayWorldPresentation.Instance.Supply;
        Require(settlement != null && loop != null && supply != null, "island authorities");
        await ApproachFront(islandPlayer.transform, supply, "supply box");
        Note("PROMPT supply: " + Prompt());
        await Press(Key.E);
        await Until(() => supply.Collected, "supply collected", 5);
        Check(new[] { 2001, 2002, 2003, 2011, 2012, 2013, 2014 }.All(id => inventory.CountItems(Item(id)) == (id == 2013 ? 2 : 1)), $"P11 supply box gives the starting tools and kits once ({at()})");
        var wood = Item(3);
        int axeSlot = HotbarIndex(inventory, Item(2001));
        await FellAndPick(islandPlayer, inventory, wood, axeSlot);
        await FellAndPick(islandPlayer, inventory, wood, axeSlot);
        int woodCount = inventory.CountItems(wood);
        Check(woodCount >= 2, $"P11 two trees felled with the axe give Wood x{woodCount}");
        var shop = await PlaceWithMouse(islandPlayer, inventory, Item(2012), "shop", true);
        await PlaceWithMouse(islandPlayer, inventory, Item(2013), "tent 1", false);
        await PlaceWithMouse(islandPlayer, inventory, Item(2013), "tent 2", false);
        Require(settlement.Established, "settlement established");
        Check(settlement.Established, $"P11 shop + 2 tents establish the settlement ({at()})");
        var interior = shop.GameObject.GetComponent<DemoShopInterior>();
        Vector3 doorFront = interior.outsideSpawn.position;
        Face(islandPlayer, interior.entrance.transform, Vector3.ProjectOnPlane(interior.entrance.transform.position - doorFront, Vector3.up).magnitude, "shop door", doorFront);
        await Press(Key.X); await Delay(400);
        await Press(Key.E); await Delay(900);
        Require(interior.IsInside, "inside the shop");
        var stand = await PlaceWithMouse(islandPlayer, inventory, Item(2020), "stand", false, shop.GameObject.transform.position, 0);
        var slot = stand != null ? stand.GameObject.GetComponentInChildren<ShopSlot>() : null;
        Require(slot != null, "stand slot");
        Face(islandPlayer, slot.transform, 1.2f, "stand");
        await Press(DigitKey(HotbarIndex(inventory, wood))); await Delay(300);
        await Press(Key.E); await Delay(500);
        int shelved = !slot.IsEmpty && slot.currentItem.data == wood ? slot.currentItem.count : 0;
        Check(shelved > 0, $"P11 Wood x{shelved} displayed from the hand with E");
        int basePrice = slot.EffectiveDisplayPrice;
        Face(islandPlayer, slot.transform, 1.2f, "stand (price)");
        await Press(Key.X); await Delay(300);
        await Press(Key.E); await Delay(500);
        Require(ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen, "island price UI open");
        await SetPriceTo(basePrice + 1);
        Check(slot.EffectiveDisplayPrice == basePrice + 1, $"P11 price UI confirms Wood {basePrice} -> {slot.EffectiveDisplayPrice}G");
        await Shot("P11_08_stocked");
        Face(islandPlayer, interior.Storage.transform, 1.3f, "storage chest");
        var pickaxe = Item(2002);
        await Press(DigitKey(HotbarIndex(inventory, pickaxe))); await Delay(250);
        await Press(Key.E); await Delay(500);
        Require(StorageUI.instance != null && StorageUI.instance.IsOpen, "storage open");
        var store = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(b => b.name == "StoreSelectedButton");
        Require(store != null, "store button");
        await ClickUI(store.gameObject, "선택 핫바 보관");
        await Delay(300);
        Check(interior.Storage.items.Any(i => i != null && i.data == pickaxe) && inventory.CountItems(pickaxe) == 0, "P11 the pickaxe is stored in the shop chest through the storage UI");
        await Shot("P11_09_storage");
        await Press(Key.Escape); await Delay(300);
        await Press(Key.P); await Until(() => SmartphoneUI.instance.IsOpen, "phone", 4); await Delay(500);
        await ClickUI(SmartphoneUI.instance.homeScreen.transform.Find("ShopPanel_Tile").gameObject, "상점 tile");
        await Delay(500);
        var app = UnityEngine.Object.FindFirstObjectByType<ShopManagementPhoneUI>();
        var mining = app.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "Root1");
        for (int i = 0; i < 10 && !TopHit(mining.gameObject); i++) await ScrollAt(ScreenCenter((RectTransform)app.transform), -120f);
        Check(TopHit(mining.gameObject), "P11 wheel-scrolling the shop app reveals the root buttons");
        await ClickUI(mining.gameObject, "광업");
        await Delay(400);
        Check(settlement.SelectedRoot == DemoSpecialization.Mining, $"P11 the phone shop app chooses 광업 ({at()})");
        await Shot("P11_10_root");
        await Press(Key.Escape); await Press(Key.Escape);
        await Until(() => !SmartphoneUI.instance.IsOpen, "phone closed", 4);
        // 낮 저장: 진열 상품·가격·보관함·정착·Root가 담긴 상태. standalone 이어하기 왕복 검수의 시작점(save-day/ 사본).
        await PauseSave(saveRoot, "day");

        // 5) 일몰(제품 속도 25s/시) → 밤 → 폰 영업 시작 → 실제 손님 → 22:00 마감 → Report.
        double sunsetStart = EditorApplication.timeSinceStartup;
        await Until(() => GameClock.Instance.enabled && GameClock.Instance.CurrentHour >= 18f, "18:00", 120);
        await Shot("P11_11_sunset");
        await Until(() => settlement.NightReady, "night ready", 150);
        Note($"SUNSET 16:00 -> night ready in {EditorApplication.timeSinceStartup - sunsetStart:F0}s at product speed (no clock fixture)");
        int moneyAtOpen = EconomyService.Instance.Money;
        int dayAtOpen = GameClock.Instance.CurrentDay;
        // 밤 전환 프레임에서 짧은 입력이 한 프레임 안에 눌렸다 떼질 수 있어 길게 누르고 한 번 더 시도한다.
        for (int tries = 0; tries < 3 && !SmartphoneUI.instance.IsOpen; tries++)
        {
            await Press(Key.P, 260);
            double wait = EditorApplication.timeSinceStartup + 2;
            while (!SmartphoneUI.instance.IsOpen && EditorApplication.timeSinceStartup < wait) await Delay(100);
            if (!SmartphoneUI.instance.IsOpen) Note($"INPUT P at night not received (try {tries + 1}), fps~{1f / Mathf.Max(.0001f, Time.smoothDeltaTime):F0}");
        }
        await Until(() => SmartphoneUI.instance.IsOpen, "phone", 2); await Delay(500);
        await ClickUI(SmartphoneUI.instance.homeScreen.transform.Find("ShopPanel_Tile").gameObject, "상점 tile");
        await Delay(500);
        var openButton = app.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "Open");
        for (int i = 0; i < 10 && !TopHit(openButton.gameObject); i++) await ScrollAt(ScreenCenter((RectTransform)app.transform), 120f);
        await ClickUI(openButton.gameObject, "영업 시작");
        await Delay(400);
        Check(loop.IsShopOpenForCustomers, $"P11 the phone opens the shop at night ({at()})");
        await Press(Key.Escape); await Press(Key.Escape);
        await Until(() => !SmartphoneUI.instance.IsOpen, "phone closed", 4);
        double businessStart = EditorApplication.timeSinceStartup;
        bool customersShot = false;
        while (!loop.OpeningSessionCompleted && EditorApplication.timeSinceStartup - businessStart < 200)
        {
            if (!customersShot && EditorApplication.timeSinceStartup - businessStart > 30) { customersShot = true; await Shot("P11_12_business"); }
            await Delay(500);
        }
        Require(loop.OpeningSessionCompleted, "business closed by 22:00");
        Note($"BUSINESS {EditorApplication.timeSinceStartup - businessStart:F0}s real time at product speed");
        await Until(() => PioneerReportCardUI.Instance != null && PioneerReportCardUI.Instance.IsOpen, "report card", 15);
        var sales = SalesLogManager.Instance.GetRecent(100).Where(r => r != null && r.gameDay == dayAtOpen).ToList();
        var report = settlement.PioneerReport;
        int moneyDelta = EconomyService.Instance.Money - moneyAtOpen;
        Note($"REPORT money+{moneyDelta} sales={sales.Count}/{sales.Sum(r => r.price)}G report={report.sales}/{report.revenue}G total={report.total} rank={report.rankKey} remainingWood={(slot.IsEmpty ? 0 : slot.currentItem.count)}");
        Check(sales.Count > 0 && moneyDelta == sales.Sum(r => r.price) && report.revenue == moneyDelta && report.sales == sales.Count,
            $"P11 real customers bought Wood and money = sales log = Report ({at()})");
        await Delay(800);
        await Shot("P11_13_report");
        await Press(Key.E); await Delay(600);
        Check(!PioneerReportCardUI.Instance.IsOpen, "P11 E closes the Report card");

        // 6) 일시정지 메뉴의 실제 '게임 저장' → 격리 폴더에 현재 형식(v17) 저장(save-final/ 사본).
        Note("AFTER REPORT phone open=" + (SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen));
        await PauseSave(saveRoot, "final");
        Note($"ROUTE total {at()}");
        Finish("PASS");
    }

    // Report를 닫으면 상점 앱이 열린다. Esc는 앱 → 홈 → 폰 닫기 순서로 처리하고, 그다음 Esc가 일시정지다.
    // 일시정지 '게임 저장'을 실제로 누르고, 저장 파일을 save-<label>/ 에 복사한 뒤 Esc로 계속한다.
    static async Task PauseSave(string saveRoot, string label)
    {
        int escapes = 0;
        for (; escapes < 5 && !(PauseManager.Instance != null && PauseManager.Instance.IsPaused); escapes++) { await Press(Key.Escape); await Delay(350); }
        Note($"INPUT Esc x{escapes} until the pause menu ({label})");
        await Until(() => PauseManager.Instance != null && PauseManager.Instance.IsPaused, "pause menu " + label, 4);
        await Delay(400);
        var saveButton = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(b => b.name == "SaveButton");
        Require(saveButton != null, "pause save button");
        string savePath = Path.Combine(saveRoot, "savegame-v17.json");
        DateTime before = File.Exists(savePath) ? File.GetLastWriteTimeUtc(savePath) : DateTime.MinValue;
        await ClickUI(saveButton.gameObject, "게임 저장 (" + label + ")");
        await Until(() => File.Exists(savePath) && File.GetLastWriteTimeUtc(savePath) > before, "save written " + label, 10);
        await Delay(400);
        await Shot("P11_saved_" + label);
        string copyDir = Path.Combine(Path.GetDirectoryName(saveRoot), "save-" + label);
        Directory.CreateDirectory(copyDir);
        File.Copy(savePath, Path.Combine(copyDir, "savegame-v17.json"), true);
        Note($"SAVED {label} {new FileInfo(savePath).Length}B (copy in save-{label}/)");
        await Press(Key.Escape); await Delay(300);
        Check(PauseManager.Instance != null && !PauseManager.Instance.IsPaused, $"P11 pause menu saves the current-format island ({label}) and Esc resumes");
    }

    // INPUT: 카메라 기준 WASD(대각선 포함)로 실제로 걷는다. 0.6초 이상 막히면 Space 점프. teleport 없음.
    static async Task<bool> WalkTo(Transform player, Vector3 destination, float tolerance, float seconds, string label)
    {
        double deadline = EditorApplication.timeSinceStartup + seconds;
        Vector3 start = player.position, last = start;
        double lastMoved = EditorApplication.timeSinceStartup;
        int jumps = 0;
        keyboard.MakeCurrent();
        while (EditorApplication.timeSinceStartup < deadline)
        {
            Vector3 delta = destination - player.position; delta.y = 0f;
            if (delta.magnitude < tolerance) break;
            var view = Camera.main.transform;
            Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized, right = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;
            Vector3 dir = delta.normalized;
            float fw = Vector3.Dot(dir, forward), rt = Vector3.Dot(dir, right);
            var keys = new System.Collections.Generic.List<Key>();
            if (fw > .38f) keys.Add(Key.W); else if (fw < -.38f) keys.Add(Key.S);
            if (rt > .38f) keys.Add(Key.D); else if (rt < -.38f) keys.Add(Key.A);
            double now = EditorApplication.timeSinceStartup;
            if (Vector3.Distance(player.position, last) > .15f) { last = player.position; lastMoved = now; }
            else if (now - lastMoved > .6f) { keys.Add(Key.Space); lastMoved = now; jumps++; }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
            await Task.Delay(50);
        }
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        await Task.Delay(250);
        Vector3 error = destination - player.position; error.y = 0f;
        Note($"INPUT walk {label}: moved {Vector3.Distance(start, player.position):F1} m, {error.magnitude:F2} m left, jumps {jumps}");
        return error.magnitude < tolerance + .3f;
    }

    // 상호작용 기준점(InteractionAnchor)이 있으면 그 바깥쪽 앞에서, 없으면 지금 방향에서 걸어가 대상을 바라본다.
    static async Task ApproachFront(Transform player, Component target, string label)
    {
        Transform anchor = target.transform.Find("InteractionAnchor");
        Vector3 point = anchor != null ? anchor.position : target.transform.position;
        Vector3 outward = anchor != null ? Vector3.ProjectOnPlane(anchor.position - target.transform.position, Vector3.up) : Vector3.ProjectOnPlane(player.position - point, Vector3.up);
        if (anchor != null && outward.sqrMagnitude < .01f) outward = Vector3.ProjectOnPlane(anchor.forward, Vector3.up);
        outward = outward.sqrMagnitude > .0001f ? outward.normalized : Vector3.back;
        await WalkTo(player, point + outward * 1.7f, .3f, 20, label);
        await WalkTo(player, point, anchor != null ? .9f : 1.25f, 3, "face " + label);
    }

    // 열린 가격 창에서 실제 ±버튼을 눌러 목표 가격을 맞추고 '가격 확정'을 누른다.
    static async Task SetPriceTo(int target)
    {
        var ui = ShopPriceUI.instance;
        var pending = typeof(ShopPriceUI).GetField("_pendingPrice", Private);
        UnityEngine.UI.Button Find(string text) => UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .FirstOrDefault(b => b.GetComponentInChildren<TMPro.TMP_Text>() != null && b.GetComponentInChildren<TMPro.TMP_Text>().text == text);
        for (int guard = 0; guard < 30; guard++)
        {
            int diff = target - (int)pending.GetValue(ui);
            if (diff == 0) break;
            string label = Mathf.Abs(diff) >= 10 ? (diff > 0 ? "+10" : "-10") : (diff > 0 ? "+1" : "-1");
            var step = Find(label);
            Require(step != null, "price button " + label);
            await ClickUI(step.gameObject, "price " + label);
        }
        Note($"PRICE set {(int)pending.GetValue(ui)}G (target {target}G)");
        var confirm = Find("가격 확정") ?? Find("가격 적용");
        Require(confirm != null, "price confirm");
        await ClickUI(confirm.gameObject, confirm.GetComponentInChildren<TMPro.TMP_Text>().text);
        await Delay(300);
    }

    static async Task CatchFishOnce(GameObject player, Inventory inventory)
    {
        var fishing = UnityEngine.Object.FindObjectsByType<FishingSpot>(FindObjectsSortMode.None).FirstOrDefault(f => f.IsDirectPlayerDemo);
        Require(fishing != null, "demo fishing spot");
        await Press(DigitKey(HotbarIndex(inventory, Item(2011)))); await Delay(250);
        Vector3 landSide = Vector3.ProjectOnPlane(fishing.transform.position - fishing.WaterTarget, Vector3.up);
        Face(player, fishing.transform, 1.2f, "fishing spot (land side)", fishing.transform.position + landSide.normalized * 1.2f);
        await Delay(500);
        await Press(Key.E); Note("INPUT E cast");
        await Until(() => fishing.Phase == FishingSpot.DemoPhase.Bite, "bite", 6);
        await Press(Key.E); Note("INPUT E at the bite");
        await Delay(500);
    }

    static async Task SetPriceWithUI(GameObject player, ShopSlot slot, int delta)
    {
        Face(player, slot.transform, 1.2f, "stand (price)");
        await Press(Key.X); await Delay(300);
        await Press(Key.E); await Delay(500);
        var ui = ShopPriceUI.instance;
        Require(ui != null && ui.IsOpen, "price UI open");
        string label = delta > 0 ? "+" + delta : delta.ToString();
        // 가격 창은 별도 캔버스에 만들어진다. 화면에 보이는 버튼에서 글자로 찾는다.
        var buttons = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var step = buttons.FirstOrDefault(b => b.GetComponentInChildren<TMPro.TMP_Text>() != null && b.GetComponentInChildren<TMPro.TMP_Text>().text == label);
        var confirm = buttons.FirstOrDefault(b => b.GetComponentInChildren<TMPro.TMP_Text>() != null && b.GetComponentInChildren<TMPro.TMP_Text>().text == "가격 확정");
        Require(step != null && confirm != null, "price buttons");
        await ClickUI(step.gameObject, "price " + label);
        await ClickUI(confirm.gameObject, "가격 확정");
        await Delay(300);
        if (ui.IsOpen) { await Press(Key.Escape); await Delay(200); }
    }

    // P9: 같은 실제 입력 연속 동작을 정상 제품 카메라와 근접 3/4에서 30fps 원속으로 저장한다(전/후 비교 영상 재료).
    //     대기 → 달리기 → 정지 → 좌우 회전 → 질주 → 제자리 점프 → 달리며 점프·착지 → 도끼 들고 이동 → 도끼 타격 → 곡괭이 → 낚싯대 던지기 → 잠자리채.
    static async Task ExerciseFeel(GameObject player, Inventory inventory)
    {
        string label = SessionState.GetString(FeelLabelKey, "before");
        string framesRoot = Path.GetFullPath(Path.Combine(OutDir, "frames-" + label));
        var capture = new GameObject("P9_FrameCapture").AddComponent<FrameSequenceCapture>();
        var view = Camera.main;
        var follow = view.GetComponent<CameraController>();
        Note($"FRAMES {framesRoot}; fixed simulation step 30fps, not wall-clock performance evidence");
        foreach (string shot in new[] { "game", "close" })
        {
            string dir = Path.Combine(framesRoot, shot);
            Directory.CreateDirectory(dir);
            QaCloseFollowCamera close = null;
            if (shot == "close")
            {
                follow.enabled = false;
                close = view.gameObject.AddComponent<QaCloseFollowCamera>();
                close.target = player.transform;
                Note("FIXTURE close 3/4 view: product CameraController paused, main camera follows at (2.2, 1.45, 2.6) m");
            }
            await FeelSequence(player, inventory, capture, dir, shot);
            if (close != null) { UnityEngine.Object.Destroy(close); follow.enabled = true; }
        }
        Finish("PASS");
    }

    const string FeelLabelKey = "PA.P9.Label";
    const string GripReviewKey = "PA.P9.SupportGrip";
    static readonly System.Collections.Generic.List<string> feelMarks = new System.Collections.Generic.List<string>();

    static async Task FeelSequence(GameObject player, Inventory inventory, FrameSequenceCapture capture, string dir, string shot)
    {
        feelMarks.Clear();
        // 고정된 평지(항구 모래)에서 +x 방향을 보고 시작한다. 전/후 실행이 같은 자리·같은 입력이 되게 한다.
        Vector3 start = FirstDayWorldPresentation.Instance.Harbor + new Vector3(-6f, 0f, 9f);
        if (Physics.Raycast(start + Vector3.up * 6f, Vector3.down, out var ground, 12f, ~0, QueryTriggerInteraction.Ignore)) start.y = ground.point.y + .05f;
        Teleport(player, start, Quaternion.LookRotation(Vector3.right));
        await Press(Key.X); await Delay(600);
        CheckSupportGrip(player, false, shot + " empty hands");
        capture.folder = dir;
        Time.captureFramerate = 30;
        capture.capturing = true;
        Mark(capture, "idle"); await WaitGame(1.5f);
        Mark(capture, "run D"); await HoldGame(1.6f, Key.D);
        Mark(capture, "stop"); await WaitGame(1.0f);
        Mark(capture, "turn A"); await HoldGame(.6f, Key.A);
        Mark(capture, "turn D"); await HoldGame(.6f, Key.D);
        Mark(capture, "stop"); await WaitGame(.8f);
        Mark(capture, "sprint Shift+D"); await HoldGame(1.2f, Key.LeftShift, Key.D);
        Mark(capture, "stop"); await WaitGame(.9f);
        Mark(capture, "jump in place"); await HoldGame(.1f, Key.Space); await WaitGame(1.3f);
        Mark(capture, "running jump"); await HoldGame(.3f, Key.A); await HoldGame(.1f, Key.A, Key.Space); await HoldGame(.8f, Key.A);
        Mark(capture, "land/stop"); await WaitGame(1.2f);
        int axeSlot = HotbarIndex(inventory, Item(2001));
        Mark(capture, "axe out"); await HoldGame(.1f, DigitKey(axeSlot)); await WaitGame(.8f);
        Mark(capture, "run with axe"); await HoldGame(1.0f, Key.D);
        Mark(capture, "stop with axe"); await WaitGame(.8f);
        capture.capturing = false;

        var tree = Nearest<Gatherable>(player.transform.position, g => g.IsDirectWorld && !g.DirectDepleted);
        if (tree != null)
        {
            Face(player, tree.transform, 1.5f, "tree (" + shot + ")");
            await WaitGame(.4f);
            capture.capturing = true;
            Mark(capture, "axe swing x2"); await HoldGame(.1f, Key.E); await WaitGame(.2f);
            CheckSupportGrip(player, true, shot + " axe contact");
            await WaitGame(.7f); await HoldGame(.1f, Key.E); await WaitGame(1.1f);
            capture.capturing = false;
        }
        int pickSlot = HotbarIndex(inventory, Item(2002));
        var rock = Nearest<MiningSpot>(player.transform.position, m => m.IsDirectWorld && !m.DirectDepleted);
        if (rock != null && pickSlot >= 0)
        {
            await HoldGame(.1f, DigitKey(pickSlot));
            Face(player, rock.transform, 1.2f, "rock (" + shot + ")");
            await WaitGame(.4f);
            capture.capturing = true;
            Mark(capture, "pickaxe swing"); await HoldGame(.1f, Key.E); await WaitGame(.2f);
            CheckSupportGrip(player, true, shot + " pickaxe contact"); await WaitGame(1f);
            capture.capturing = false;
        }
        var fishing = UnityEngine.Object.FindObjectsByType<FishingSpot>(FindObjectsSortMode.None).FirstOrDefault(f => f.IsDirectPlayerDemo);
        int rodSlot = HotbarIndex(inventory, Item(2011));
        if (fishing != null && rodSlot >= 0)
        {
            await HoldGame(.1f, DigitKey(rodSlot));
            Vector3 landSide = Vector3.ProjectOnPlane(fishing.transform.position - fishing.WaterTarget, Vector3.up);
            Face(player, fishing.transform, 1.2f, "fishing spot (" + shot + ")", fishing.transform.position + landSide.normalized * 1.2f);
            await WaitGame(.4f);
            capture.capturing = true;
            // 던지기만 찍고 X로 거둔다(오늘 낚시를 끝내지 않아 다음 시점에서도 같은 동작을 찍는다).
            Mark(capture, "rod cast"); await HoldGame(.1f, Key.E); await WaitGame(.2f);
            CheckSupportGrip(player, true, shot + " rod cast");
            await WaitGame(.9f); await HoldGame(.1f, Key.X); await WaitGame(.6f);
            capture.capturing = false;
        }
        var bug = UnityEngine.Object.FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Where(b => !b.Captured).OrderBy(b => b.name).FirstOrDefault();
        int netSlot = HotbarIndex(inventory, Item(2003));
        if (bug != null && netSlot >= 0)
        {
            await HoldGame(.1f, DigitKey(netSlot));
            FaceBug(player, bug, 1.0f, "butterfly (" + shot + ")");
            await WaitGame(.3f);
            capture.capturing = true;
            Mark(capture, "net swing"); await HoldGame(.1f, Key.E); await WaitGame(.2f);
            CheckSupportGrip(player, false, shot + " one-handed net"); await WaitGame(1f);
            capture.capturing = false;
        }
        await FreeToolUseSequence(player, inventory, capture, start, shot);
        Time.captureFramerate = 0;
        File.WriteAllLines(Path.Combine(dir, "marks.txt"), feelMarks);
        Note($"CAPTURE {shot}: {capture.Frames} frames total, last encode {capture.LastEncodeMs:F0} ms, marks {feelMarks.Count}");
    }

    static async Task FreeToolUseSequence(GameObject player, Inventory inventory, FrameSequenceCapture capture, Vector3 start, string view)
    {
        bool after = SessionState.GetString(FeelLabelKey, "before") == "after";
        var equipment = player.GetComponent<EquipmentSystem>();
        foreach (int id in new[] { 2001, 2002, 2003 })
        {
            Teleport(player, start, Quaternion.LookRotation(Vector3.right));
            await HoldGame(.1f, DigitKey(HotbarIndex(inventory, Item(id)))); await WaitGame(.7f);
            var tool = inventory.GetSelectedInstance(); int used = tool.durabilityUsed;
            float began = equipment.ActionStartedAt;
            capture.capturing = true;
            Mark(capture, "air swing " + id); await HoldGame(.1f, Key.E); await WaitGame(.2f);
            await Shot("P9_" + view + "_air_" + id); await WaitGame(.5f); capture.capturing = false;
            bool moved = equipment.ActionStartedAt > began;
            Note("OBSERVED " + view + " air tool " + id + " action=" + moved + " durability " + used + "->" + tool.durabilityUsed);
            if (after) Check(moved && tool.durabilityUsed == used, "P9 air swing " + id + " starts animation without durability");
        }
        var tree = Nearest<Gatherable>(player.transform.position, g => g.IsDirectWorld && !g.DirectDepleted);
        var rock = Nearest<MiningSpot>(player.transform.position, m => m.IsDirectWorld && !m.DirectDepleted);
        foreach (var pair in new[] { new { id = 2002, target = tree as Component }, new { id = 2003, target = tree as Component }, new { id = 2001, target = rock as Component } })
        {
            Require(pair.target != null, "wrong-target fixture available");
            await HoldGame(.1f, DigitKey(HotbarIndex(inventory, Item(pair.id))));
            Face(player, pair.target.transform, 1.5f, "wrong tool " + pair.id + " (" + view + ")"); await WaitGame(.5f);
            var tool = inventory.GetSelectedInstance(); int used = tool.durabilityUsed;
            int remaining = pair.target is Gatherable gather ? gather.DirectRemainingHits : ((MiningSpot)pair.target).DirectRemainingHits;
            float began = equipment.ActionStartedAt;
            capture.capturing = true;
            Mark(capture, "wrong-target " + pair.id); await HoldGame(.1f, Key.E); await WaitGame(.2f);
            await Shot("P9_" + view + "_wrong_" + pair.id); await WaitGame(.5f); capture.capturing = false;
            int end = pair.target is Gatherable g ? g.DirectRemainingHits : ((MiningSpot)pair.target).DirectRemainingHits;
            bool moved = equipment.ActionStartedAt > began;
            Note("OBSERVED " + view + " wrong-target " + pair.id + " action=" + moved + " target hits " + remaining + "->" + end + " durability " + used + "->" + tool.durabilityUsed);
            if (after) Check(moved && remaining == end && used == tool.durabilityUsed, "P9 wrong tool " + pair.id + " moves but cannot gather or consume durability");
        }
    }

    static void CheckSupportGrip(GameObject player, bool expected, string label)
    {
        if (!SessionState.GetBool(GripReviewKey, false)) return;
        var pose = player.GetComponent<PlayerLocomotionAnimator>();
        bool supporting = pose != null && pose.SupportingTool;
        float error = pose != null ? pose.SupportGripError : float.PositiveInfinity;
        Note($"GRIP {label}: supporting={supporting} hand-to-shaft={error:F3}m");
        Check(expected ? supporting && error < .065f : !supporting, "P9 support grip " + label);
    }

    static void Mark(FrameSequenceCapture capture, string label)
    {
        feelMarks.Add($"{capture.Frames}\t{Time.time:F2}\t{label}");
        Note("INPUT " + label);
    }

    static async Task WaitGame(float seconds)
    {
        float until = Time.time + seconds;
        while (Time.time < until) await Task.Delay(5);
    }

    // 게임 시간 기준으로 키를 누르고 있다가 뗀다(captureFramerate 동안 실시간과 게임 시간이 다르다).
    static async Task HoldGame(float seconds, params Key[] keys)
    {
        keyboard.MakeCurrent();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        await WaitGame(seconds);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        await WaitGame(1f / 30f);
    }

    // P10 조사: 섬 위에서 본 모습, 랜드마크별 정상 카메라 화면, 출발점에서 랜드마크까지 걷기 경로(NavMesh)·시간, 디버그 잔재.
    static async Task ExerciseTour(GameObject player, Inventory inventory)
    {
        var world = WorldPersistenceService.Instance?.ActiveGeneratedWorld;
        var grid = UnityEngine.Object.FindFirstObjectByType<WorldGridService>();
        Require(world != null && grid != null, "generated world + grid");
        var cc = player.GetComponent<CharacterController>();
        var pc = player.GetComponent<PlayerController>();
        Note($"PLAYER stepOffset={cc.stepOffset:F2} slope={cc.slopeLimit:F0} height={cc.height:F2} moveSpeed={(pc != null ? pc.moveSpeed : -1f):F1} elevationStep={grid.Definition.ElevationStep:F2} cell={grid.Definition.CellSize:F1}");
        var points = new System.Collections.Generic.List<(string name, Vector3 pos)>();
        points.Add(("Harbor", FirstDayWorldPresentation.Instance.Harbor));
        foreach (var anchor in world.Anchors)
            if (grid.CellToWorld(anchor.Coordinate, out var p)) points.Add((anchor.Kind.ToString(), p));
        foreach (var pt in points)
        {
            string biome = grid.WorldToCell(pt.pos, out var c) && world.TryGetCell(c, out var cell) && grid.TryGetCell(c, out var gridCell) ? cell.Biome + " lvl" + gridCell.ElevationLevel : "?";
            Note($"ANCHOR {pt.name} at {pt.pos:F0} {biome}");
        }
        // 걷기 경로: 항구에서 각 랜드마크로(NavMesh) + 다음 랜드마크로 순서대로.
        UnityEngine.AI.NavMesh.SamplePosition(points[0].pos, out var harborHit, 6f, UnityEngine.AI.NavMesh.AllAreas);
        for (int i = 1; i < points.Count; i++)
        {
            var path = new UnityEngine.AI.NavMeshPath();
            bool sampled = UnityEngine.AI.NavMesh.SamplePosition(points[i].pos, out var hit, 6f, UnityEngine.AI.NavMesh.AllAreas);
            bool ok = sampled && UnityEngine.AI.NavMesh.CalculatePath(harborHit.position, hit.position, UnityEngine.AI.NavMesh.AllAreas, path);
            float length = 0f; for (int k = 1; ok && k < path.corners.Length; k++) length += Vector3.Distance(path.corners[k - 1], path.corners[k]);
            Note($"PATH Harbor -> {points[i].name}: {(ok ? path.status.ToString() : "none")} {length:F0} m (~{length / 4.2f:F0} s at 4.2 m/s) straight {Vector3.Distance(points[0].pos, points[i].pos):F0} m");
            Check(ok && path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete,"P10 complete harbour path to "+points[i].name);
        }
        // Product keyboard input over the retained block terrain. No teleport
        // between the harbour and settlement; the existing walker uses Space
        // when blocked, retaining the actual player jump authority and speed.
        Teleport(player,points[0].pos+new Vector3(0,.05f,4f),Quaternion.LookRotation(Vector3.forward));
        Note("FIXTURE initial harbour pose only; following harbour-to-Start leg uses WASD/Space");
        var startPoint=points.First(p=>p.name=="Start");
        double walkStarted=EditorApplication.timeSinceStartup;
        Check(await WalkTo(player.transform,startPoint.pos,.7f,85f,"harbour to settlement over block terraces"),"P10 player reaches settlement by actual WASD/Space");
        Note("OBSERVED harbour-to-settlement wall seconds "+(EditorApplication.timeSinceStartup-walkStarted).ToString("F1"));
        await Shot("P10_walk_arrival");

        var step=grid.GetComponentsInChildren<Unity.AI.Navigation.NavMeshLink>()
            .Where(l=>l.name.StartsWith("DemoTerraceStepLink_",StringComparison.Ordinal))
            .OrderBy(l=>Vector3.Distance(l.startPoint,player.transform.position)).FirstOrDefault();
        var companion=FirstDayWorldPresentation.Instance.GetComponentsInChildren<DemoCompanionRoutine>().FirstOrDefault();
        if(step!=null && companion!=null && companion.Agent!=null)
        {
            companion.enabled=false;
            var agent=companion.Agent;
            Note("FIXTURE one companion placed at an existing step link start for visible crossing (not a natural whole route)");
            agent.Warp(step.startPoint); agent.isStopped=false;
            var traversal=agent.GetComponent<FirstDayStepTraversal>();
            Require(traversal!=null,"NPC step traversal adapter");
            int beforeSteps=traversal.CompletedSteps;
            Teleport(player,step.startPoint+new Vector3(-3f,.1f,-4f),Quaternion.LookRotation(Vector3.forward));
            await Delay(400); await Shot("P10_companion_step_before");
            agent.SetDestination(step.endPoint);
            await Until(()=>traversal.IsJumping,"companion begins visible terrace crossing",8);
            await Shot("P10_companion_step_air");
            await Until(()=>traversal.CompletedSteps>beforeSteps,"companion lands across block step",5);
            Check(traversal.CompletedSteps>beforeSteps,"P10 NPC completes visible one-level step without offscreen hop");
            Note($"OBSERVED NPC steps={traversal.CompletedSteps-beforeSteps} routine offscreenHops={companion.OffscreenHops} endpoint gap={Vector3.Distance(agent.transform.position,step.endPoint):F2}");
            await Shot("P10_companion_step_after");
            companion.enabled=true;
        }
        else Check(false,"P10 actual companion and terrace step available");
        // 디버그 잔재: 활성 라벨·기본 도형·OnGUI 표시 컴포넌트.
        var labels = UnityEngine.Object.FindObjectsByType<PrototypeWorldLabel>(FindObjectsSortMode.None).Where(l => l.isActiveAndEnabled && l.GetComponentInParent<ShopSlot>() == null).ToArray();
        var primitives = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
            .Where(m => m.sharedMesh != null && (m.sharedMesh.name == "Cube" || m.sharedMesh.name == "Sphere" || m.sharedMesh.name == "Capsule" || m.sharedMesh.name == "Cylinder") && m.GetComponent<Renderer>() != null && m.GetComponent<Renderer>().enabled && m.gameObject.activeInHierarchy)
            .Select(m => m.name + "@" + m.transform.position.ToString("F0")).Take(25).ToArray();
        Note($"DEBUG labels={labels.Length} [{string.Join(", ", labels.Take(8).Select(l => l.name))}] primitives={primitives.Length} [{string.Join(", ", primitives)}]");

        // 갈색 선 찾기: P3와 같은 첫 나무 앞에서 화면에 가늘고 길게 투영되는 렌더러를 모두 적는다.
        {
            var firstTree = Nearest<Gatherable>(player.transform.position, g => g.IsDirectWorld && !g.DirectDepleted);
            if (firstTree != null)
            {
                Face(player, firstTree.transform, 1.5f, "first tree (line check)");
                await Delay(1500);
                await Shot("P10_line_check");
                if (grid.WorldToCell(player.transform.position, out var pc0))
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        var row = new System.Text.StringBuilder();
                        for (int dx = -5; dx <= 2; dx++)
                        {
                            var cc0 = pc0 + new Vector2Int(dx, dz);
                            if (grid.TryGetCell(cc0, out var gc)) row.Append($"[{dx}:L{gc.ElevationLevel}{(gc.HasPath ? "P" : "")}{(gc.HasWater ? "W" : "")}{(gc.IsWalkable ? "" : "X")}]");
                        }
                        Note($"ROW dz={dz} player cell {pc0} {row}");
                    }
            }
        }

        // 항구에서 주요 발견 지점까지(직선 거리·4.2m/s 걷기 시간 추정, 단은 점프로 오른다).
        {
            Vector3 harbor = points[0].pos;
            System.Func<Vector3, string> eta = v => { float d = Vector3.Distance(new Vector3(v.x, 0, v.z), new Vector3(harbor.x, 0, harbor.z)); return $"{d:F0} m ~{d / 4.2f:F0} s"; };
            var finds = new System.Collections.Generic.List<string>();
            finds.Add("supply " + eta(FirstDayWorldPresentation.Instance.Supply.transform.position));
            var fishSpot = UnityEngine.Object.FindObjectsByType<FishingSpot>(FindObjectsSortMode.None).FirstOrDefault(f => f.IsDirectPlayerDemo);
            if (fishSpot != null) finds.Add("fishing " + eta(fishSpot.transform.position));
            var bugNear = Nearest<BugCritter>(harbor, b => true); if (bugNear != null) finds.Add("butterfly " + eta(bugNear.transform.position));
            var treeNear = Nearest<Gatherable>(harbor, g => g.IsDirectWorld); if (treeNear != null) finds.Add("tree " + eta(treeNear.transform.position));
            var rockNear = Nearest<MiningSpot>(harbor, m => m.IsDirectWorld); if (rockNear != null) finds.Add("rock " + eta(rockNear.transform.position));
            foreach (var pt in points.Skip(1)) finds.Add(pt.name + " " + eta(pt.pos));
            Note("DISCOVERY from harbor: " + string.Join(" | ", finds));
        }

        // 일몰·밤 조명(시계만 옮김): 항구 쪽 정상 카메라.
        {
            Teleport(player, points[0].pos + new Vector3(0f, .05f, 4f), Quaternion.LookRotation(Vector3.forward));
            foreach (var hour in new[] { 18.5f, 20.5f })
            {
                GameClock.Instance.ForceSet(hour, 1, "P10 lighting survey");
                GameClock.Instance.secondsPerGameHour = 3600f;
                await Delay(1800);
                await Shot("P10_light_" + hour.ToString("00.0").Replace(".", "h"));
            }
            GameClock.Instance.ForceSet(9, 1, "P10 lighting survey reset");
            Note("FIXTURE clock moved to 18:30 and 20:30 for lighting shots (no business state)");
        }

        // 랜드마크마다 정상 카메라(플레이어를 그 자리에 두고).
        foreach (var pt in points)
        {
            Vector3 at = pt.pos;
            if (Physics.Raycast(at + Vector3.up * 30f, Vector3.down, out var g, 80f, ~0, QueryTriggerInteraction.Ignore)) at.y = g.point.y + .05f;
            Teleport(player, at + new Vector3(0f, 0f, -2f), Quaternion.LookRotation(Vector3.forward));
            await Delay(1300);
            await Shot("P10_" + pt.name);
        }
        Finish("PASS");
    }

    static void Teleport(GameObject player, Vector3 position, Quaternion rotation)
    {
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.SetPositionAndRotation(position, rotation);
        if (cc != null) cc.enabled = true;
        player.GetComponent<PlayerController>()?.ResetMotionAfterTeleport();
        Note("FIXTURE start position " + position.ToString("F1"));
    }

    static async Task<WorldPlacedBuildingRuntime> PlaceWithMouse(GameObject player, Inventory inventory, Item kit, string label, bool large,
        Vector3? searchFrom = null, int firstRing = -1)
    {
        var controller = player.GetComponent<WorldHotbarPlacementController>();
        var placement = UnityEngine.Object.FindFirstObjectByType<WorldBuildingPlacementService>();
        var grid = placement.GetComponent<WorldGridService>();
        var entry = DemoPlaceableCatalog.Load().Find(kit);
        Require(entry != null, "catalog entry " + label);
        float cell = grid.Definition.CellSize;
        var offsets = entry.Definition.ResolveFootprint(Vector2Int.zero, 0);
        Vector2 mean = Vector2.zero; foreach (var c in offsets) mean += (Vector2)c; mean /= offsets.Length;
        float radius = 0f; foreach (var c in offsets) radius = Mathf.Max(radius, Vector2.Distance(c, mean));
        // FIXTURE: 제품 배치 평가로 근처의 빈 부지를 찾아 그 남쪽에 선다(도보 탐색 대신). 확정은 마우스+E 입력.
        string probe = "p6-probe-" + label.Replace(' ', '-');
        placement.RegisterDefinition(probe, entry.Definition);
        grid.WorldToCell(searchFrom ?? player.transform.position, out var origin);
        Vector2Int site = default; bool found = false;
        for (int r = firstRing >= 0 ? firstRing : Mathf.CeilToInt(radius) + 2; r <= 40 && !found; r++)
            for (int dz = -r; dz <= r && !found; dz++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue;
                    var candidate = origin + new Vector2Int(dx, dz);
                    var evaluated = placement.Evaluate(probe, candidate, 0);
                    if (!evaluated.Succeeded || !DemoSettlementController.IsCustomerReachable(grid, evaluated, entry.kind)) continue;
                    site = candidate; found = true; break;
                }
        Require(found, "free site for " + label);
        grid.CellToWorld(site, out var siteWorld);
        Vector3 center = siteWorld + new Vector3(mean.x, 0f, mean.y) * cell;
        if (searchFrom == null || Vector3.ProjectOnPlane(center - player.transform.position, Vector3.up).magnitude > (WorldHotbarPlacementController.MouseRadiusCells - .5f) * cell)
        {
            var marker = new GameObject("P6_Site").transform; marker.position = center;
            Face(player, marker, (radius + 1.6f) * cell, label + " site", center + Vector3.back * 10f);
            UnityEngine.Object.Destroy(marker.gameObject);
        }
        else Note($"{label}: site {Vector3.Distance(center, player.transform.position):F1} m away, no approach needed");
        await Delay(500);
        int slot = HotbarIndex(inventory, kit);
        Require(slot >= 0, label + " on hotbar");
        int before = inventory.CountItems(kit);
        await Press(Key.X); await Delay(150);
        await Press(DigitKey(slot)); await Delay(1300);
        Require(controller.IsPlacing, label + " preview");
        var follow = Camera.main.GetComponent<CameraController>();
        if (large) Check(follow != null && follow.OpeningBuildMode, "P6 a large building uses the camera build view (" + label + ")");
        Vector3 aim = new Vector3(center.x, player.transform.position.y, center.z);
        Vector2 screen = Camera.main.WorldToScreenPoint(aim);
        await MoveMouse(screen + new Vector2(20f, 0f)); await Delay(100);
        await MoveMouse(screen); await Delay(300);
        Vector2[] nudges = { Vector2.zero, new Vector2(0f, 40f), new Vector2(0f, -40f), new Vector2(40f, 0f), new Vector2(-40f, 0f) };
        foreach (var nudge in nudges)
        {
            await MoveMouse(screen + nudge); await Delay(200);
            if (controller.PreviewResult.Succeeded && DemoSettlementController.IsCustomerReachable(grid, controller.PreviewResult, entry.kind)) break;
        }
        Note($"INPUT mouse aim {label}: site={site} preview={controller.PreviewResult.Anchor} status={controller.Status}");
        Check(controller.Status == "설치 가능", $"P6 the aimed {label} preview reads 설치 가능 ({controller.Status})");
        await Shot("P6_place_" + label.Replace(' ', '_'));
        if (Mode == "ui")
        {
            // A capture can wait several slow Editor frames after the teleport/build-view camera moves.
            // Reproject the same world point using the current product camera before the real E input.
            for (int i = 0; i < 4; i++)
            {
                await MoveMouse(Camera.main.WorldToScreenPoint(aim)); await Delay(250);
            }
            Note("UI place confirm pointer=" + Mouse.current.position.ReadValue() + " device=" + Mouse.current.name
                + " preview=" + controller.PreviewResult.Anchor + " status=" + controller.Status);
        }
        await Press(Key.E); await Delay(500);
        var runtime = placement.Placements.Where(p => p.Definition.StableId == entry.Definition.StableId).OrderBy(p => Vector3.Distance(p.GameObject.transform.position, center)).FirstOrDefault();
        Check(!controller.IsPlacing && inventory.CountItems(kit) == before - 1 && runtime != null,
            $"P6 mouse + E places the {label} and consumes one kit (placing={controller.IsPlacing}, kit {before}->{inventory.CountItems(kit)}, runtime={(runtime != null)})");
        if (large) Check(follow == null || !follow.OpeningBuildMode, "P6 build view ends after placing the " + label);
        return runtime;
    }

    static System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<Transform, string>> Cards(CraftingUI ui) =>
        ui.slotParent.Cast<Transform>().Where(t => t.gameObject.activeSelf)
            .Select(t => new System.Collections.Generic.KeyValuePair<Transform, string>(t, t.GetComponentInChildren<TMPro.TMP_Text>(true)?.text ?? ""));

    static System.Collections.Generic.KeyValuePair<Transform, string> Card(CraftingUI ui, string name) =>
        Cards(ui).FirstOrDefault(c => Plain(c.Value).StartsWith(name));

    static string Plain(string rich) => System.Text.RegularExpressions.Regex.Replace(rich ?? "", "<[^>]+>", "").Replace("\n", " ");

    static string Status(CraftingUI ui)
    {
        var text = typeof(CraftingUI).GetField("_statusText", Private)?.GetValue(ui) as TMPro.TMP_Text;
        return text != null ? text.text : "";
    }

    static Vector2 mousePosition = new Vector2(960f, 540f);

    static async Task MoveMouse(Vector2 screen)
    {
        mouse.MakeCurrent();
        mousePosition = screen;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen });
        await Task.Delay(60);
    }

    static Vector2 ScreenCenter(RectTransform rt)
    {
        var canvas = rt.GetComponentInParent<Canvas>().rootCanvas;
        Camera view = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return RectTransformUtility.WorldToScreenPoint(view, (corners[0] + corners[2]) * .5f);
    }

    static bool TopHit(GameObject target)
    {
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events == null || target == null) return false;
        var data = new UnityEngine.EventSystems.PointerEventData(events) { position = ScreenCenter((RectTransform)target.transform) };
        var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        events.RaycastAll(data, hits);
        return hits.Count > 0 && (hits[0].gameObject == target || hits[0].gameObject.transform.IsChildOf(target.transform));
    }

    static async Task ScrollAt(Vector2 screen, float amount)
    {
        await MoveMouse(screen);
        // 휠은 다음 업데이트에 저절로 0이 되는 delta다. 0 상태를 따로 보내면 느린 Editor 프레임에서 한 업데이트에 겹쳐 사라진다.
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen, scroll = new Vector2(0f, amount) });
        await Task.Delay(350);
        Note("INPUT wheel " + amount + " at " + screen.ToString("F0"));
    }

    static async Task MouseButton(UnityEngine.InputSystem.LowLevel.MouseButton button)
    {
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = mousePosition }.WithButton(button, true));
        await Task.Delay(100);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = mousePosition });
        await Task.Delay(90);
        Note("INPUT mouse " + button);
    }

    // 실제 가상 마우스 포인터로 uGUI를 누른다(InputSystemUIInputModule).
    static async Task ClickUI(GameObject target, string label)
    {
        var rt = (RectTransform)target.transform;
        var canvas = target.GetComponentInParent<Canvas>().rootCanvas;
        Camera view = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector2 center = RectTransformUtility.WorldToScreenPoint(view, (corners[0] + corners[2]) * .5f);
        await MoveMouse(center); await Delay(60);
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = center }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, true));
        await Task.Delay(90);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = center });
        await Task.Delay(120);
        Note("INPUT mouse click " + label + " at " + center.ToString("F0"));
    }

    static void FaceBug(GameObject player, BugCritter bug, float distance, string label)
    {
        var ground = new GameObject("P5_BugGround").transform;
        ground.position = bug.transform.position - Vector3.up * .85f;
        Face(player, ground, distance, label);
        UnityEngine.Object.Destroy(ground.gameObject);
    }

    static async Task FellTree(Gatherable tree)
    {
        for (int guard = 0; guard < 6 && !tree.DirectDepleted; guard++) { await Press(Key.E); await Delay(420); }
        Require(tree.DirectDepleted, "tree felled");
    }

    static async Task FellAndPick(GameObject player, Inventory inventory, Item wood, int axeSlot)
    {
        await Press(DigitKey(axeSlot)); await Delay(250);
        var tree = Nearest<Gatherable>(player.transform.position, g => g.IsDirectWorld && !g.DirectDepleted);
        Require(tree != null, "tree for durability route");
        Face(player, tree.transform, 1.5f, "tree");
        await Delay(350);
        await FellTree(tree);
        var drop = DropFor(wood);
        Require(drop != null, "drop for durability route");
        Face(player, drop.transform, 1.0f, "drop");
        await Press(Key.X); await Delay(250); await Press(Key.E); await Delay(450);
        Require(drop == null, "drop picked in durability route");
    }

    static int FillBag(Inventory inventory, Item filler)
    {
        int added = 0;
        while (added < 5000 && inventory.TryReceiveToHotbar(filler, 1)) added++;
        return added;
    }

    static bool Consumed(string spawnKey)
    {
        var persistence = WorldPersistenceService.Instance;
        var state = persistence.CaptureState(Vector3.zero, null).resourceStates?.Find(s => s != null && s.spawnKey == spawnKey);
        return state != null && state.consumed;
    }

    static InventoryFramework.PickupItem DropFor(Item item) =>
        UnityEngine.Object.FindObjectsByType<InventoryFramework.PickupItem>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p != null && p.isActiveAndEnabled && p.name.StartsWith("GatherDrop_") && p.item == item);

    static T Nearest<T>(Vector3 from, Func<T, bool> filter) where T : Component =>
        UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None).Where(c => c != null && c.gameObject.activeInHierarchy && filter(c))
            .OrderBy(c => Vector3.Distance(c.transform.position, from)).FirstOrDefault();

    static Item Item(int id) => ItemRegistry.Instance.allItems.FirstOrDefault(i => i != null && i.id == id) ??
                                FirstDayStudioAssets.Load().supplies.FirstOrDefault(i => i != null && i.id == id);

    static int HotbarIndex(Inventory inventory, Item item) => inventory.hotbar.slots.FindIndex(s => s != null && !s.IsEmpty && s.item == item);

    static Key DigitKey(int index) => (Key)((int)Key.Digit1 + index);

    // FIXTURE: 대상 앞 distance m 지점(대상 쪽을 바라봄, 지면 높이)으로 옮긴다. 이동 감각은 P9/P10/P11에서 실제 입력으로 본다.
    static void Face(GameObject player, Transform target, float distance, string label, Vector3? from = null)
    {
        Vector3 flat = Vector3.ProjectOnPlane((from ?? player.transform.position) - target.position, Vector3.up);
        if (flat.sqrMagnitude < .01f) flat = Vector3.back;
        // 대상과 같은 단(±0.75m)의 바닥이 있는 쪽을 고른다(계단식 지형의 아래 단에서 접근하지 않게).
        Vector3 spot = target.position + flat.normalized * distance;
        float bestGap = float.MaxValue;
        for (int i = 0; i < 8; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, i * 45f, 0f) * flat.normalized;
            Vector3 candidate = target.position + dir * distance;
            if (!Physics.Raycast(candidate + Vector3.up * 4f, Vector3.down, out var probe, 10f, ~0, QueryTriggerInteraction.Ignore)) continue;
            float gap = Mathf.Abs(probe.point.y - target.position.y);
            if (gap < bestGap - .01f) { bestGap = gap; spot = candidate; spot.y = probe.point.y; }
            if (gap < .75f) break;
        }
        spot.y += .05f;
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        Vector3 look = target.position - spot; look.y = 0f;
        player.transform.SetPositionAndRotation(spot, Quaternion.LookRotation(look.sqrMagnitude > .001f ? look.normalized : Vector3.forward));
        if (cc != null) cc.enabled = true;
        player.GetComponent<PlayerController>()?.ResetMotionAfterTeleport();
        Note("FIXTURE approach " + label);
    }

    static Vector3 OpenDirection(GameObject player) => player.transform.forward;

    static string Prompt()
    {
        var prompt = InteractPromptUI.instance;
        return prompt != null && prompt.promptText != null && prompt.promptText.gameObject.activeInHierarchy ? prompt.promptText.text.Replace("\n", " ") : string.Empty;
    }

    static async Task Press(Key key, int holdMs = 110)
    {
        keyboard.MakeCurrent();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        await Task.Delay(holdMs);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        await Task.Delay(90);
    }

    static async Task Tap(Key key)
    {
        keyboard.MakeCurrent();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        await Task.Delay(25);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        await Task.Delay(25);
    }

    static async Task Scroll(float amount)
    {
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0f, amount) });
        await Task.Delay(80);
        InputSystem.QueueStateEvent(mouse, new MouseState());
        await Task.Delay(80);
    }

    static Task Delay(int ms) => Task.Delay(ms);

    static async Task Until(Func<bool> condition, string label, float seconds)
    {
        double deadline = EditorApplication.timeSinceStartup + seconds;
        while (!condition() && EditorApplication.timeSinceStartup < deadline) await Task.Delay(100);
        if (!condition()) throw new InvalidOperationException(label);
    }

    static async Task Shot(string name)
    {
        string path = Path.Combine(OutDir, name + ".png");
        ScreenCapture.CaptureScreenshot(path);
        double deadline = EditorApplication.timeSinceStartup + 6;
        while (!File.Exists(path) && EditorApplication.timeSinceStartup < deadline) await Task.Delay(100);
        Note("SHOT " + name + (File.Exists(path) ? "" : " NOT WRITTEN"));
        if (PA_DemoThemeReview.Active) await PA_DemoThemeReview.Capture720(OutDir, name);
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (!SessionState.GetBool(Active, false) || type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (stack.Contains("UnityEditor.Connect.")) return;
        errors++; Note("CONSOLE " + message + "\n" + stack);
    }

    static void Check(bool okay, string label)
    {
        checks++;
        if (!okay) failures++;
        Note((okay ? "PASS " : "FAIL ") + label);
    }

    static void Require(bool okay, string label) { if (!okay) throw new InvalidOperationException(label); }

    static void Note(string line)
    {
        if (finished) return;
        File.AppendAllText(Path.Combine(OutDir, "result.txt"), line + "\n");
    }

    static void Finish(string status)
    {
        if (finished) return;
        Time.captureFramerate = 0;
        if (status == "PASS" && (errors != 0 || failures != 0)) status = "FAIL";
        Note($"{status} checks={checks} failed={failures} consoleErrors={errors}" + (problem == null ? "" : " problem=" + problem));
        finished = true;
        RestoreAudioNativeInput();
        foreach (var device in new InputDevice[] { keyboard, mouse })
            if (device != null) InputSystem.RemoveDevice(device);
        keyboard = null; mouse = null;
        SessionState.SetBool(Active, false);
        if (Mode == "route" || Mode == "intro" || Mode == "ui" || Mode == "audio" || Mode == "foot" || AmbienceReview) SessionState.EraseString(SaveManager.ValidationRootSessionKey);
        SessionState.EraseString(ModeKey);
        SessionState.EraseBool(AmbienceReviewKey);
        PA_DemoThemeReview.Clear();
        EditorApplication.isPlaying = false;
    }
}
