#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// P1_CURRENT_CONTINUE (2026-10-03) — 격리 Editor Play 검사. 플레이어 persistentDataPath는 쓰지 않는다.
// FIXTURE(result.txt에 표시): Demo256 직행 + Miner_01/Farmer_01, 보급 상자 Interact, 제품과 같은 ID 형식·표지·키트 소모를
//   따르는 배치, Root 선택·일몰/영업 시계 가속·진열 재고 지급·화폐 지정.
// 저장은 일시정지 메뉴 버튼, 복원은 일시정지 [저장본 불러오기](섬 재로드) 또는 새 Play 세션의 타이틀 [이어하기]다.
// 같은 live 객체를 유지한 assertion으로 재로드를 대신하지 않는다. v16은 변환하지 않고 사유만 보이는지 확인한다.
public sealed class PA_ContinueCompatibilityTests
{
    const string TitleScene = "Assets/Scenes/Prototype_FirstDay.unity";
    const string LegacyBackup = "Logs/VisualQA/TitleScreen-20260929/SaveBackups/savegame-original-20260930-194059.json";
    const string LegacyHash = "71467AA5A0F5C81B5B2A63A2CFEA72B40509C4BF13A65BF9CF0AEE8CC2449A11";
    const string CaseKey = "PA.P1.CaseDir";
    const string FailuresKey = "PA.P1.Failures";
    const string TentOwnersKey = "PA.P1.TentOwners";
    const string InsideKey = "PA.P1.ExpectInside";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    // 먼저 세운 텐트에 더 큰 ID를 준다. 제품 ID(Guid N)가 우연히 배치 순서와 같을 때만 통과하는 검사를 막는다.
    const string FirstTentId = "demo-resident-tent-ffffffffffffffffffffffffffffffff";
    const string SecondTentId = "demo-resident-tent-00000000000000000000000000000000";

    static string CaseDir => SessionState.GetString(CaseKey, string.Empty);
    static string SaveDir => Path.Combine(CaseDir, "saves");
    static string CurrentSaveFile => Path.Combine(SaveDir, "savegame-v17.json");

    [TearDown]
    public void ClearSessionKeys()
    {
        SessionState.EraseString(SaveManager.ValidationRootSessionKey);
        SessionState.EraseString(CaseKey);
        SessionState.EraseInt(FailuresKey);
        SessionState.EraseString(TentOwnersKey);
        SessionState.EraseBool(InsideKey);
    }

    [UnityTest]
    public IEnumerator CurrentFormat_SaveContinueChain_RestoresNewObjects()
    {
        StartCase("CurrentFormatChain");
        yield return new EnterPlayMode();
        Note("== SESSION 1: new game -> day settlement -> pause save -> pause load (island reload)");
        EnterFirstDayDirect();
        yield return WaitPlayable("session1 new game", false);
        BuildDaySettlement();
        yield return VisitForestAndReturn();
        yield return Shot("S1_day_before_save");
        RecordExpectations();
        yield return SaveViaPause("save#1 day outdoors", false);
        yield return LoadViaPause("pause load#1");
        yield return WaitPlayable("pause load#1", true);
        yield return Settle();
        yield return Shot("S1_day_after_pause_load");
        VerifyAgainstSave("pause load#1 (same Play session, island reloaded)");
        yield return new ExitPlayMode();

        yield return new EnterPlayMode();
        Note("== SESSION 2: new Play -> title Continue -> verify day -> root/sunset/night -> stands -> enter shop -> save");
        yield return ContinueFromTitle("continue#1");
        yield return WaitPlayable("continue#1", true);
        yield return Settle();
        yield return Shot("S2_day_after_title_continue");
        VerifyAgainstSave("title continue#1 (new Play session)");
        yield return ProgressToNightInsideShop();
        yield return Shot("S2_night_inside_before_save");
        RecordExpectations();
        yield return SaveViaPause("save#2 night inside shop", false);
        yield return new ExitPlayMode();

        yield return new EnterPlayMode();
        Note("== SESSION 3: new Play -> title Continue -> verify night inside -> OPEN -> save refused -> close -> report -> save");
        yield return ContinueFromTitle("continue#2");
        yield return WaitPlayable("continue#2", true);
        yield return Settle();
        yield return Shot("S3_night_inside_after_title_continue");
        VerifyAgainstSave("title continue#2 (new Play session)");
        yield return BusinessSaveRefusedThenClose();
        RecordExpectations();
        yield return SaveViaPause("save#3 after report", false);
        yield return new ExitPlayMode();

        yield return new EnterPlayMode();
        Note("== SESSION 4: new Play -> title Continue -> verify report and closed business");
        yield return ContinueFromTitle("continue#3");
        yield return WaitPlayable("continue#3", true);
        yield return Settle();
        yield return Shot("S4_after_report_title_continue");
        VerifyAgainstSave("title continue#3 (new Play session)");
        VerifyReportRestored();
        yield return new ExitPlayMode();

        int failures = SessionState.GetInt(FailuresKey, 0);
        Note(failures == 0 ? "RESULT PASS current-format chain" : "RESULT FAIL current-format chain failures=" + failures);
        Assert.AreEqual(0, failures, "see " + Path.Combine(CaseDir, "result.txt"));
    }

    [UnityTest]
    public IEnumerator LegacyV16_ShowsUnsupportedNoticeAndPreservesOriginal()
    {
        StartCase("LegacyV16Notice");
        string backup = Path.GetFullPath(LegacyBackup);
        Assert.IsTrue(File.Exists(backup), "v16 backup missing");
        string source = Path.Combine(SaveDir, "savegame.json");
        File.Copy(backup, source);
        Assert.AreEqual(LegacyHash, Sha256(source));
        yield return new EnterPlayMode();
        Note("== v16 isolated copy at title (no conversion expected)");
        Button continueButton = null;
        float started = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - started < 20f)
        {
            continueButton = TitleField<Button>("_secondaryButton");
            var label = continueButton != null ? continueButton.GetComponentInChildren<TMPro.TextMeshProUGUI>() : null;
            if (label != null && label.text == "호환 불가") break;
            yield return null;
        }
        var buttonLabel = continueButton != null ? continueButton.GetComponentInChildren<TMPro.TextMeshProUGUI>() : null;
        var body = TitleField<TMPro.TextMeshProUGUI>("_flowBody");
        var primary = TitleField<Button>("_primaryButton");
        Check(buttonLabel != null && buttonLabel.text == "호환 불가", "title secondary shows 호환 불가 (" + (buttonLabel != null ? buttonLabel.text : "missing") + ")");
        Check(continueButton != null && !continueButton.interactable, "Continue disabled for v16");
        Check(body != null && body.text.Contains("이전 버전(v16)") && body.text.Contains("기존 저장은 유지"),
            "reason and preservation shown: " + (body != null ? body.text.Replace("\n", " / ") : "missing"));
        if (body != null) body.ForceMeshUpdate();
        Check(body != null && !body.isTextTruncated, "reason text fully visible (not truncated)");
        Check(primary != null && primary.interactable, "새 게임 remains available");
        yield return Shot("V16_title_notice");
        if (primary != null && primary.interactable)
        {
            Check(PointerClick(primary.gameObject, out string hit), "INPUT uGUI pointer click 새 게임 (top hit=" + hit + ")");
            yield return null; yield return null;
            body = TitleField<TMPro.TextMeshProUGUI>("_flowBody");
            Check(body != null && body.text.Contains("이전 버전 저장은 이어할 수 없지만 파일은 그대로 보존"),
                "new-game confirm explains preserved legacy save: " + (body != null ? body.text.Replace("\n", " / ") : "missing"));
            yield return Shot("V16_new_game_confirm");
            var back = TitleField<Button>("_secondaryButton");
            if (back != null) PointerClick(back.gameObject, out _);
            yield return null;
        }
        yield return new ExitPlayMode();
        // Play 전환의 domain reload 뒤 지역 변수는 남지 않는다. 경로는 SessionState/상수에서 다시 만든다.
        Check(Sha256(Path.Combine(SaveDir, "savegame.json")) == LegacyHash, "isolated v16 copy unchanged");
        Check(!File.Exists(CurrentSaveFile), "no converted savegame-v17 written");
        Check(Sha256(Path.GetFullPath(LegacyBackup)) == LegacyHash, "backup unchanged");
        int failures = SessionState.GetInt(FailuresKey, 0);
        Note(failures == 0 ? "RESULT PASS legacy notice" : "RESULT FAIL legacy notice failures=" + failures);
        Assert.AreEqual(0, failures, "see " + Path.Combine(CaseDir, "result.txt"));
    }

    // ---------- session steps ----------

    static void StartCase(string label)
    {
        // 같은 실행의 두 case는 한 폴더에 모으되, 이미 쓰인 case 폴더는 다시 쓰지 않는다(이전 증거 보존).
        string run = SessionState.GetString("PA.P1.RunDir", string.Empty);
        if (string.IsNullOrEmpty(run) || !Directory.Exists(run) || Directory.Exists(Path.Combine(run, label)))
        {
            run = Path.GetFullPath(Path.Combine("Logs", "VisualQA",
                DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-P1_CURRENT_CONTINUE"));
            SessionState.SetString("PA.P1.RunDir", run);
        }
        string dir = Path.Combine(run, label);
        Directory.CreateDirectory(Path.Combine(dir, "saves"));
        SessionState.SetString(CaseKey, dir);
        SessionState.SetInt(FailuresKey, 0);
        SessionState.SetString(SaveManager.ValidationRootSessionKey, Path.Combine(dir, "saves"));
        File.WriteAllText(Path.Combine(dir, "result.txt"),
            "P1_CURRENT_CONTINUE " + label + " " + DateTime.Now.ToString("s") + "\n" +
            "isolated save root=" + Path.Combine(dir, "saves") + " (player persistentDataPath untouched)\n" +
            "FIXTURE = QA setup; INPUT = injected uGUI pointer event or product handler call; other lines = observed state.\n");
        PA_DepartureContinuationChecks.ConfigureGameView();
        EditorSceneManager.OpenScene(TitleScene);
    }

    static void EnterFirstDayDirect()
    {
        var prefab = Resources.Load<GameObject>("DepartureTutorial/DepartureContinuation");
        var selection = prefab != null ? prefab.GetComponentInChildren<DepartureCompanionSelection>(true) : null;
        Require(selection != null && selection.candidates != null, "companion candidates");
        typeof(DemoRouteController).GetField("<SelectedCompanions>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, selection.candidates.Where(c => c.id == "Miner_01" || c.id == "Farmer_01").ToArray());
        typeof(DemoRouteController).GetField("_arrivalPending", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, true);
        Note("FIXTURE direct Demo256 entry with Miner_01/Farmer_01 (tutorial/voyage are not part of P1)");
        SceneManager.LoadScene(DemoRouteController.WorldScene, LoadSceneMode.Single);
    }

    static void BuildDaySettlement()
    {
        var player = Inventory.instance.gameObject;
        FirstDayWorldPresentation.Instance.Supply.Interact(player);
        Note("FIXTURE supply box Interact(player) (real E route is P3)");
        Require(FirstDayWorldPresentation.Instance.Supply.Collected, "supplies received");
        PlaceLikeProduct(DemoPlaceableKind.ShopBase, null, "shop");
        PlaceLikeProduct(DemoPlaceableKind.ResidentTent, FirstTentId, "tent placed first");
        PlaceLikeProduct(DemoPlaceableKind.ResidentTent, SecondTentId, "tent placed second");
        var demo = DemoSettlementController.Instance;
        Require(demo.Established && demo.LicensePoints == 1, "settlement established with one License Point");
        EconomyService.Instance.ForceSet(137, "P1 fixture");
        Note("FIXTURE money set to 137G so restore cannot pass by a default value");
        var inventory = Inventory.instance;
        var plank = Resources.Load<Item>("Items/Item_Plank");
        Require(plank != null && inventory.TryReceiveToHotbar(plank, 2), "two planks in the bag");
        inventory.SelectHotbarSlot(inventory.hotbar.slots.FindIndex(s => !s.IsEmpty && s.item == plank));
        Note("FIXTURE 2 Plank granted and selected (hotbar index " + inventory.selectedHotbarIndex + ")");
    }

    static IEnumerator VisitForestAndReturn()
    {
        var world = WorldPersistenceService.Instance.ActiveGeneratedWorld;
        var grid = UnityEngine.Object.FindFirstObjectByType<WorldGridService>();
        var player = Inventory.instance.gameObject;
        Vector3 home = player.transform.position; Quaternion homeRot = player.transform.rotation;
        Require(world.TryGetAnchor(WorldGenerationAnchorKind.ForestActivity, out var forest), "forest anchor");
        Require(grid.CellToWorld(forest.Coordinate, out Vector3 forestPos), "forest anchor position");
        Teleport(player, forestPos + Vector3.up * .05f, homeRot);
        Note("FIXTURE teleport to the forest anchor for 2 s (Report exploration sample)");
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 2f) yield return null;
        Teleport(player, home, homeRot);
        start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 1f) yield return null;
        var progress = DemoSettlementController.Instance.GetComponent<DemoPioneerReport>().CaptureProgress();
        Require(progress.visitedBiomes.Contains("Forest"), "forest visit observed");
    }

    static IEnumerator ProgressToNightInsideShop()
    {
        var demo = DemoSettlementController.Instance;
        Require(demo.TryChooseRoot(DemoSpecialization.Mining), "Mining root");
        Note("FIXTURE TryChooseRoot(Mining) (phone root button verified in D3; P2 re-verifies)");
        float start = Time.realtimeSinceStartup;
        while (!(GameClock.Instance.enabled && GameClock.Instance.CurrentHour >= 16f) && Time.realtimeSinceStartup - start < 10f) yield return null;
        GameClock.Instance.secondsPerGameHour = 1f;
        Note("FIXTURE sunset clock accelerated to 1 s per game hour");
        start = Time.realtimeSinceStartup;
        while (!demo.NightReady && Time.realtimeSinceStartup - start < 20f) yield return null;
        Require(demo.NightReady, "night shop ready at 20:00");
        PlaceLikeProduct(DemoPlaceableKind.Workbench, null, "workbench");
        for (int i = 0; i < 3; i++) PlaceLikeProduct(DemoPlaceableKind.DisplayStand, null, "stand" + i);
        var slots = demo.OperatingShop.Slots.Where(s => s != null).OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
        Require(slots.Length == 3, "three stand slots");
        Stock(slots[0], Resources.Load<Item>("Items/Item_Wood"), 5, 12);
        Stock(slots[1], Resources.Load<Item>("Items/Item_Fish"), 3, 10);
        slots[2].displayPrice = 15; slots[2].RefreshDisplay();
        Note("FIXTURE stands: Wood x5 @12G, Fish x3 @10G, empty @15G (granted items + ShopSlot.Interact)");
        var inventory = Inventory.instance;
        inventory.SelectHotbarSlot(0);
        inventory.GetComponent<EquipmentSystem>()?.Holster();
        Note("INPUT X equivalent (EquipmentSystem.Holster) to enter with empty hands");
        var interior = demo.OperatingShop.GetComponentInParent<DemoShopInterior>(true);
        Require(interior != null, "shop interior");
        Vector3 look = interior.entrance.transform.position - interior.outsideSpawn.position; look.y = 0f;
        Teleport(inventory.gameObject, interior.outsideSpawn.position, Quaternion.LookRotation(look.normalized));
        yield return null;
        interior.entrance.Interact(inventory.gameObject);
        Note("INPUT door E handler (BuildingEntrance.Interact) at the outside spawn");
        start = Time.realtimeSinceStartup;
        while (!interior.IsInside && Time.realtimeSinceStartup - start < 8f) yield return null;
        Require(interior.IsInside, "player entered the shop");
        start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 1.5f) yield return null;
    }

    static IEnumerator BusinessSaveRefusedThenClose()
    {
        var demo = DemoSettlementController.Instance;
        var loop = DayNightShopLoopController.Instance;
        Require(loop.TryOpenShop(), "OPEN via the phone OPEN handler (DayNightShopLoopController.TryOpenShop)");
        Note("INPUT phone OPEN handler");
        float start = Time.realtimeSinceStartup;
        while (!demo.BusinessInProgress && Time.realtimeSinceStartup - start < 5f) yield return null;
        Check(demo.BusinessInProgress, "business in progress after OPEN");
        string before = Sha256(CurrentSaveFile);
        yield return SaveViaPause("save during business", true);
        Check(Sha256(CurrentSaveFile) == before, "save file unchanged by the refused business save");
        yield return Shot("S3_business_save_refused");
        GameClock.Instance.secondsPerGameHour = 8f;
        Note("FIXTURE business clock accelerated to 8 s per game hour");
        start = Time.realtimeSinceStartup;
        while (demo.PioneerReport == null && Time.realtimeSinceStartup - start < 40f) yield return null;
        Require(demo.PioneerReport != null && loop.OpeningSessionCompleted, "22:00 auto close and Pioneer Report");
        Note($"Report total={demo.PioneerReport.total} rank={demo.PioneerReport.rankKey} sales={demo.PioneerReport.sales} revenue={demo.PioneerReport.revenue}");
        start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 1f) yield return null;
        demo.CloseManagement();
        yield return null;
        if (SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen) SmartphoneUI.instance.Close();
        start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 1f) yield return null;
    }

    static void RecordExpectations()
    {
        var demo = DemoSettlementController.Instance;
        SessionState.SetString(TentOwnersKey, string.Join(";", demo.Residents
            .OrderBy(r => r.CompanionId, StringComparer.Ordinal).Select(r => r.CompanionId + "=" + r.TentId)));
        var interior = demo.OperatingShop != null ? demo.OperatingShop.GetComponentInParent<DemoShopInterior>(true) : null;
        SessionState.SetBool(InsideKey, interior != null && interior.IsInside);
        Note("EXPECT tent owners " + SessionState.GetString(TentOwnersKey, "") + " inside=" + SessionState.GetBool(InsideKey, false));
    }

    // ---------- save / load through product UI ----------

    static IEnumerator SaveViaPause(string label, bool expectRefusal)
    {
        var pause = PauseManager.Instance;
        Require(pause != null, label + ": pause menu");
        pause.Pause();
        yield return null; yield return null;
        var save = PauseField<Button>("_saveButton");
        float start = Time.realtimeSinceStartup;
        while ((save == null || !save.interactable) && Time.realtimeSinceStartup - start < 5f)
        { yield return null; save = PauseField<Button>("_saveButton"); }
        Require(save != null && save.interactable, label + ": save button");
        Check(PointerClick(save.gameObject, out string hit), label + ": INPUT uGUI pointer click 게임 저장 (top hit=" + hit + ")");
        string status = string.Empty;
        start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 10f)
        {
            status = PauseStatus();
            if (status.StartsWith("저장했습니다") || status == SaveManager.BusinessSaveBlockedMessage) break;
            yield return null;
        }
        if (expectRefusal)
            Check(status == SaveManager.BusinessSaveBlockedMessage, label + ": refused with reason \"" + status + "\"");
        else
            Check(status.StartsWith("저장했습니다") && File.Exists(CurrentSaveFile), label + ": saved (\"" + status + "\")");
        if (expectRefusal) yield return Shot("pause_save_refused");
        pause.Resume();
        yield return null;
    }

    static IEnumerator LoadViaPause(string label)
    {
        var pause = PauseManager.Instance;
        pause.Pause();
        var load = PauseField<Button>("_loadButton");
        float start = Time.realtimeSinceStartup;
        while ((load == null || !load.interactable) && Time.realtimeSinceStartup - start < 5f)
        { yield return null; load = PauseField<Button>("_loadButton"); }
        Require(load != null && load.interactable, label + ": load button");
        Check(PointerClick(load.gameObject, out string hit), label + ": INPUT uGUI pointer click 저장본 불러오기 (top hit=" + hit + ")");
        yield return null;
    }

    static IEnumerator ContinueFromTitle(string label)
    {
        Button button = null;
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 25f)
        {
            button = TitleField<Button>("_secondaryButton");
            if (button != null && button.interactable) break;
            yield return null;
        }
        var text = button != null ? button.GetComponentInChildren<TMPro.TextMeshProUGUI>() : null;
        Require(button != null && button.interactable && text != null && text.text == "이어하기",
            label + ": title 이어하기 enabled (" + (text != null ? text.text : "missing") + ")");
        yield return Shot(label.Replace("#", "") + "_title");
        Check(PointerClick(button.gameObject, out string hit), label + ": INPUT uGUI pointer click 이어하기 (top hit=" + hit + ")");
    }

    static IEnumerator WaitPlayable(string label, bool fromSave)
    {
        bool sawWorld = false;
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 90f)
        {
            string scene = SceneManager.GetActiveScene().name;
            sawWorld |= scene == DemoRouteController.WorldScene;
            var route = UnityEngine.Object.FindFirstObjectByType<DemoRouteController>();
            if (route != null && route.IsPlayable && DemoSettlementController.Instance != null) yield break;
            if (fromSave && sawWorld && scene == "Prototype_FirstDay")
                throw new AssertionException(label + ": returned to title: " + DemoRouteController.ConsumeContinueError());
            yield return null;
        }
        throw new AssertionException(label + ": FirstDay world did not become playable");
    }

    static IEnumerator Settle()
    {
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 2.5f) yield return null;
    }

    // ---------- verification ----------

    static void VerifyAgainstSave(string label)
    {
        var saved = JsonUtility.FromJson<SaveData>(File.ReadAllText(CurrentSaveFile));
        var demo = DemoSettlementController.Instance;
        var clock = GameClock.Instance;
        var economy = EconomyService.Instance;
        var inventory = Inventory.instance;
        var player = GameObject.FindGameObjectWithTag("Player");
        Note("-- verify " + label + " against " + CurrentSaveFile);
        Check(saved.version == SaveManager.CurrentSaveVersion && saved.demoSession != null, label + ": saved file is v" + saved.version + " with demoSession");
        Check(economy.Money == saved.money && economy.CumulativeRevenue == saved.cumulativeRevenue,
            $"{label}: money {economy.Money}/{saved.money}, revenue {economy.CumulativeRevenue}/{saved.cumulativeRevenue}");
        Check(clock.CurrentDay == saved.gameDay && Mathf.Abs(clock.CurrentHour - saved.gameHour) < 0.05f,
            $"{label}: clock day {clock.CurrentDay}/{saved.gameDay} hour {clock.CurrentHour:0.000}/{saved.gameHour:0.000}");
        if (saved.demoSession.nightReady) Check(!clock.enabled, label + ": night shop clock held at the saved time");
        var hud = ClockHUD.instance;
        string shown = hud != null && hud.clockText != null ? hud.clockText.text : "missing";
        int clockMinutes = Mathf.FloorToInt(clock.CurrentHour) * 60 +
                           Mathf.FloorToInt((clock.CurrentHour - Mathf.FloorToInt(clock.CurrentHour)) * 60f);
        var parts = shown.Split(':');
        bool parsed = parts.Length == 2 && int.TryParse(parts[0], out int hudHour) && int.TryParse(parts[1], out int hudMinute) &&
                      Mathf.Abs(hudHour * 60 + hudMinute - clockMinutes) <= 1;
        Check(parsed, $"{label}: HUD clock text \"{shown}\" matches the restored clock");
        Check(Vector3.Distance(player.transform.position, saved.playerPosition) < .05f &&
              Quaternion.Angle(player.transform.rotation, saved.playerRotation) < 1f,
            $"{label}: player pose {player.transform.position:F2}/{saved.playerPosition:F2}");
        var captured = demo.CaptureSaveState();
        Check(captured.shopId == saved.demoSession.shopId && captured.placedBuildings.Count == saved.demoSession.placedBuildings.Count &&
              saved.demoSession.placedBuildings.All(s => captured.placedBuildings.Any(c => c.instanceId == s.instanceId &&
                  c.buildingId == s.buildingId && c.anchorX == s.anchorX && c.anchorZ == s.anchorZ &&
                  c.rotationQuarterTurns == s.rotationQuarterTurns)),
            $"{label}: placements {string.Join(",", captured.placedBuildings.Select(p => p.buildingId + "@" + p.anchorX + "," + p.anchorZ + "r" + p.rotationQuarterTurns))}");
        Check((int)demo.SelectedRoot == saved.demoSession.selectedRoot && demo.Established == saved.demoSession.established &&
              demo.GrantedStands == saved.demoSession.grantedStands && demo.NightReady == saved.demoSession.nightReady &&
              demo.LicensePoints == (demo.Established && demo.SelectedRoot == DemoSpecialization.None ? 1 : 0),
            $"{label}: root {demo.SelectedRoot} LP {demo.LicensePoints} granted {demo.GrantedStands} established {demo.Established} night {demo.NightReady}");
        string owners = string.Join(";", demo.Residents.OrderBy(r => r.CompanionId, StringComparer.Ordinal).Select(r => r.CompanionId + "=" + r.TentId));
        Check(owners == SessionState.GetString(TentOwnersKey, "?"), $"{label}: companion tents {owners}");
        var shopSlots = demo.OperatingShop != null ? demo.OperatingShop.Slots.Where(s => s != null).ToList() : new List<ShopSlot>();
        Check(saved.shopSlots.Count == shopSlots.Count && saved.shopSlots.All(s => shopSlots.Any(slot =>
                slot.displayPrice == s.displayPrice && (s.occupied
                    ? !slot.IsEmpty && slot.currentItem.data.id == s.itemId && slot.currentItem.count == s.count &&
                      Mathf.Abs(slot.currentItem.quality - s.quality) < .001f
                    : slot.IsEmpty))),
            $"{label}: stands {string.Join(",", shopSlots.Select(s => (s.IsEmpty ? "empty" : s.currentItem.data.itemName + "x" + s.currentItem.count) + "@" + s.displayPrice))}");
        Check(SameSlots(saved.inventorySlots, inventory.slots) && SameSlots(saved.hotbarSlots, inventory.hotbar.slots) &&
              inventory.selectedHotbarIndex == saved.selectedHotbarIndex,
            $"{label}: bag/hotbar and selected index {inventory.selectedHotbarIndex}/{saved.selectedHotbarIndex}");
        var supply = FirstDayWorldPresentation.Instance.Supply;
        Check(supply.Collected && !supply.gameObject.activeInHierarchy, label + ": supply box stays received (no second grant)");
        var interior = demo.OperatingShop != null ? demo.OperatingShop.GetComponentInParent<DemoShopInterior>(true) : null;
        bool expectInside = SessionState.GetBool(InsideKey, false);
        Check(interior == null ? !expectInside : interior.IsInside == expectInside && interior.interior.activeSelf == expectInside &&
              interior.exterior.activeSelf == !expectInside, $"{label}: shop interior presentation inside={expectInside}");
        var progress = demo.GetComponent<DemoPioneerReport>().CaptureProgress();
        var savedBiomes = saved.demoSession.pioneerReport?.visitedBiomes ?? Array.Empty<string>();
        Check(savedBiomes.All(b => progress.visitedBiomes.Contains(b)) && savedBiomes.Contains("Forest"),
            $"{label}: report exploration kept ({string.Join(",", progress.visitedBiomes)})");
    }

    static void VerifyReportRestored()
    {
        var saved = JsonUtility.FromJson<SaveData>(File.ReadAllText(CurrentSaveFile));
        var demo = DemoSettlementController.Instance;
        var loop = DayNightShopLoopController.Instance;
        var report = demo.PioneerReport;
        Check(report != null && report.rankKey == saved.demoSession.pioneerReport.rankKey &&
              report.total == saved.demoSession.pioneerReport.total,
            "report restored " + (report != null ? report.rankKey + "/" + report.total : "missing"));
        Check(loop.OpeningSessionCompleted && !loop.CanPlayerOpenShop && !loop.TryOpenShop() && !demo.BusinessInProgress,
            "first business stays closed after continue (OPEN rejected)");
    }

    static bool SameSlots(List<SlotSaveData> saved, List<InventorySlot> actual)
    {
        if (saved == null || actual == null || saved.Count != actual.Count) return false;
        for (int i = 0; i < saved.Count; i++)
        {
            var s = saved[i]; var a = actual[i];
            int count = a == null || a.IsEmpty ? 0 : a.count;
            if (s.count != count || count > 0 && (a.item.id != s.itemId || Mathf.Abs(a.instance.quality - s.quality) > .001f)) return false;
        }
        return true;
    }

    // ---------- fixtures that follow product semantics ----------

    static void PlaceLikeProduct(DemoPlaceableKind kind, string fixedId, string label)
    {
        var demo = DemoSettlementController.Instance;
        var entry = DemoPlaceableCatalog.Load().Find(kind);
        var inventory = Inventory.instance;
        Require(entry != null && demo.CanPlace(entry) && inventory.CountItems(entry.item) > 0, label + ": kit available and allowed");
        var placement = WorldAlphaPlayableController.Instance.Buildings;
        var grid = placement.GetComponent<WorldGridService>();
        string id = fixedId ?? "demo-" + entry.key + "-" + Guid.NewGuid().ToString("N");
        placement.RegisterDefinition(id, entry.Definition);
        Require(grid.WorldToCell(FirstDayWorldPresentation.Instance.Harbor, out var harbor), "harbor cell");
        Vector2Int chosen = default; bool found = false;
        for (int radius = 4; radius <= 60 && !found; radius++)
            for (int dz = -radius; dz <= radius && !found; dz++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != radius) continue;
                    var candidate = harbor + new Vector2Int(dx, dz);
                    var evaluated = placement.Evaluate(id, candidate, 0);
                    if (evaluated.Succeeded && DemoSettlementController.IsCustomerReachable(grid, evaluated, kind))
                    { chosen = candidate; found = true; break; }
                }
        Require(found, label + ": valid footprint near the harbor");
        var result = placement.TryPlace(id, chosen, 0);
        Require(result.Succeeded && placement.TryGetPlacement(id, out WorldPlacedBuildingRuntime placed), label + ": placed");
        placement.TryGetPlacement(id, out placed);
        // WorldHotbarPlacementController.Confirm과 같은 순서: 키트 1개 소모 → 표지 연결 → 정착 진행.
        inventory.RemoveItems(entry.item, 1);
        var marker = placed.GameObject.GetComponent<DemoPlacedObject>() ?? placed.GameObject.AddComponent<DemoPlacedObject>();
        marker.Bind(id, entry, placement);
        typeof(DemoSettlementController).GetMethod("OnPlaced", Private).Invoke(demo, new object[] { placed, entry });
        Note($"FIXTURE place {label} id={id} cell={chosen} (product id/marker/kit consumption)");
    }

    static void Stock(ShopSlot slot, Item product, int count, int price)
    {
        var inventory = Inventory.instance;
        Require(product != null && inventory.TryReceiveToHotbar(product, count), "receive " + (product != null ? product.itemName : "null"));
        inventory.SelectHotbarSlot(inventory.hotbar.slots.FindIndex(s => !s.IsEmpty && s.item == product));
        slot.Interact(inventory.gameObject);
        Require(!slot.IsEmpty && slot.currentItem.count == count, "stocked " + product.itemName);
        slot.displayPrice = price;
        slot.RefreshDisplay();
    }

    static void Teleport(GameObject player, Vector3 position, Quaternion rotation)
    {
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.SetPositionAndRotation(position, rotation);
        if (cc != null) cc.enabled = true;
        player.GetComponent<PlayerController>()?.ResetMotionAfterTeleport();
    }

    // ---------- utilities ----------

    static bool PointerClick(GameObject target, out string topHit)
    {
        topHit = "none";
        var eventSystem = EventSystem.current;
        var rect = target.GetComponent<RectTransform>();
        if (eventSystem == null || rect == null) return false;
        var canvas = target.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var data = new PointerEventData(eventSystem)
        {
            position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)),
            button = PointerEventData.InputButton.Left
        };
        var hits = new List<RaycastResult>();
        eventSystem.RaycastAll(data, hits);
        if (hits.Count == 0) return false;
        topHit = hits[0].gameObject.name;
        if (hits[0].gameObject != target && !hits[0].gameObject.transform.IsChildOf(target.transform)) return false;
        data.pointerPress = target;
        ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
        return true;
    }

    static T TitleField<T>(string field) where T : class
    {
        var title = UnityEngine.Object.FindFirstObjectByType<PlayableDayScenarioController>();
        return title == null ? null : typeof(PlayableDayScenarioController).GetField(field, Private)?.GetValue(title) as T;
    }

    static T PauseField<T>(string field) where T : class =>
        PauseManager.Instance == null ? null : typeof(PauseManager).GetField(field, Private)?.GetValue(PauseManager.Instance) as T;

    static string PauseStatus()
    {
        var text = typeof(PauseManager).GetField("_statusText", Private)?.GetValue(PauseManager.Instance) as TMPro.TextMeshProUGUI;
        return text != null ? text.text : string.Empty;
    }

    static IEnumerator Shot(string name)
    {
        string path = Path.Combine(CaseDir, name + ".png");
        ScreenCapture.CaptureScreenshot(path);
        float start = Time.realtimeSinceStartup;
        while (!File.Exists(path) && Time.realtimeSinceStartup - start < 6f) yield return null;
        Note("SHOT " + name + (File.Exists(path) ? "" : " NOT WRITTEN") +
             (GameClock.Instance != null ? $" at {GameClock.Instance.CurrentHour:0.00}h" : ""));
    }

    static void Check(bool okay, string label)
    {
        if (!okay) SessionState.SetInt(FailuresKey, SessionState.GetInt(FailuresKey, 0) + 1);
        Note((okay ? "PASS " : "FAIL ") + label);
    }

    static void Require(bool okay, string label)
    {
        if (okay) return;
        Note("FAIL(required) " + label);
        throw new AssertionException(label);
    }

    static void Note(string line)
    {
        if (string.IsNullOrEmpty(CaseDir)) return;
        File.AppendAllText(Path.Combine(CaseDir, "result.txt"), line + "\n");
    }

    static string Sha256(string path)
    {
        if (!File.Exists(path)) return "missing";
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var file = File.OpenRead(path);
        return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", string.Empty);
    }
}
#endif
