using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

// 데모 완성 D1~D4 실제 Play 검수 (WorldSandbox Demo256).
// QA fixture(결과에 FIXTURE로 표시): 동행 Miner/Farmer 주입 직행, 보급 상자 Interact, 상점·텐트·가판대 배치 API(키트 1개씩 회수),
//   20:00 ForceSet, 가판대 재고 지급(TryReceiveToHotbar)과 ShopSlot.Interact·가격 확정, 문 앞 위치 이동.
// 제품 입력(QA 주입 키/UI 이벤트): P 휴대폰 → 상점 앱 → 전문 분야·영업 시작, E 문 출입, E Report 닫기, Esc 휴대폰 닫기, W 이동 복귀.
// 판매 권위(ShopSlot/PurchaseEvaluator/Economy/SalesLog)는 호출하지 않고 실제 NPC 흐름의 결과만 관찰한다.
// 증거는 실행마다 Logs/VisualQA/DemoCompletion-yyyyMMdd-HHmmss/ 에만 쓴다(이전 증거를 덮어쓰지 않음).
public static class PA_DemoBusinessPlayReview
{
    const string Active = "PA.DemoBusinessPlayReview.Active";
    const string OutKey = "PA.DemoBusinessPlayReview.Out";
    const string BackgroundKey = "PA.DemoBusinessPlayReview.RunInBackground";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static bool running, finished;
    static double started;
    static int errors, checks, failures, sales, revenue, closeEvents, peakTourists, stuckEvents;
    static string problem;
    static Keyboard keyboard;
    static readonly List<string> buyers = new List<string>();
    static readonly List<float> toastSpans = new List<float>();
    static float toastCalledAt = -1f, lastToastUntil;
    static bool toastVisible;

    static string OutDir => SessionState.GetString(OutKey, "Logs/VisualQA/DemoCompletion-unknown");

    const string ModeKey = "PA.DemoBusinessPlayReview.Mode";
    static bool ManualClose => SessionState.GetString(ModeKey, "") == "manual";

    [MenuItem("Project PA/Validation/Demo Completion D2 Business Play Review")]
    public static void Run() => Start("auto");

    // P2: 같은 준비 뒤 OPEN → 휴대폰 [영업 마감] 버튼으로 일찍 닫는 짧은 경로(자동 마감 경로와 별도 초기 상태).
    [MenuItem("Project PA/Validation/Demo P2 Manual Close Check")]
    public static void RunManualClose() => Start("manual");

    static void Start(string mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run from Edit mode.");
        string outDir = "Logs/VisualQA/" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-P2_PHONE_GUIDE_D4" +
                        (mode == "manual" ? "-manual-close" : "");
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, "result.txt"),
            "Demo play review (P2 phone/guide + D1-D4 business; mode=" + mode + ").\n" +
            "FIXTURE = QA setup, INPUT = injected InputSystem key or uGUI pointer event, other lines = observed product behavior.\n");
        SessionState.SetString(OutKey, outDir);
        SessionState.SetString(ModeKey, mode);
        SessionState.SetBool(BackgroundKey, Application.runInBackground);
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/Scenes/WorldSandbox.unity");
        PA_DepartureContinuationChecks.ConfigureGameView();
        FocusGameView();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Subscribe()
    {
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.update -= WatchToast; EditorApplication.update += WatchToast;
        Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BindDirectDemo()
    {
        if (!SessionState.GetBool(Active, false)) return;
        Application.runInBackground = true;
        var prefab = Resources.Load<GameObject>("DepartureTutorial/DepartureContinuation");
        var selection = prefab != null ? prefab.GetComponentInChildren<DepartureCompanionSelection>(true) : null;
        Require(selection != null && selection.candidates != null && selection.candidates.Length == 3, "companion candidates");
        typeof(DemoRouteController).GetField("<SelectedCompanions>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, selection.candidates.Where(c => c.id == "Miner_01" || c.id == "Farmer_01").ToArray());
        var grid = UnityEngine.Object.FindFirstObjectByType<WorldGridService>();
        Require(grid != null, "WorldSandbox grid");
        if (grid.GetComponent<DemoRouteController>() == null) grid.gameObject.AddComponent<DemoRouteController>();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || finished) return;
        if (started == 0) started = EditorApplication.timeSinceStartup;
        if (EditorApplication.timeSinceStartup - started > 540) { problem = "watchdog timeout"; Finish("FAIL"); return; }
        if (running) return;
        var route = WorldAlphaPlayableController.Instance?.DemoRoute;
        if (route == null || !route.IsPlayable) return;
        running = true;
        _ = Exercise();
    }

    static async Task Exercise()
    {
        try
        {
            keyboard = InputSystem.AddDevice<Keyboard>("DemoReviewKeyboard");
            keyboard.MakeCurrent();
            FocusGameView();
            var alpha = WorldAlphaPlayableController.Instance;
            var adapter = alpha.Adapter;
            var player = adapter.PlayerRoot;
            var inventory = adapter.PlayerInventory;
            var progress = DemoSettlementController.Instance;
            var loop = DayNightShopLoopController.Instance;
            var presentation = FirstDayWorldPresentation.Instance;
            Require(player != null && inventory != null && progress != null && loop != null && presentation != null, "demo runtime bound");
            Note("FIXTURE direct Demo256 entry with authored Miner_01/Farmer_01 (tutorial/voyage not replayed)");

            // D4 — 도착 직후 오전 HUD: 시간·돈·우상단 알림·핫바.
            await Delay(2200);
            await Shot("D4_hud_morning");
            HudLayout("morning", false);

            presentation.Supply.Interact(player);
            Note("FIXTURE supply box Interact(player); real E pickup was validated by the 84-check route");
            await Delay(500);
            var placement = alpha.Buildings;
            var catalog = DemoPlaceableCatalog.Load();
            Place(placement, progress, inventory, catalog.Find(DemoPlaceableKind.ShopBase), "shop");
            Place(placement, progress, inventory, catalog.Find(DemoPlaceableKind.ResidentTent), "tent1");
            Place(placement, progress, inventory, catalog.Find(DemoPlaceableKind.ResidentTent), "tent2");
            Require(progress.Established, "settlement established (Shop/Base + 2 tents)");
            await Delay(700);

            // D3 — 전문 분야: P → 휴대폰 상점 앱 → 광업 버튼.
            var app = await OpenPhoneShopApp(viewRecordFirst: true);
            ScrollShopApp(app, 0f);
            await Delay(400);
            await Shot("D3_phone_specialization");
            var mining = Named<Button>(app, "Root1");
            Require(mining != null && mining.interactable, "phone 광업 button enabled after settlement");
            Check(TopHitIs(mining.gameObject), "D3 광업 button is not covered by another UI");
            Click(mining.gameObject);
            Note("INPUT pointer click 광업 (Root1)");
            await Delay(300);
            Require(progress.SelectedRoot == DemoSpecialization.Mining, "광업 chosen through the phone shop app");
            await ClosePhoneWithEscape("specialization");

            // 일몰 → 밤: 제품 자체 진행(분 단위 tick → HUD/조명/영업 단계)을 빠르게만 돌린다.
            // ForceSet은 tick을 보내지 않아 HUD 시각이 멈춘 채 캡처되므로 쓰지 않는다.
            await Until(() => GameClock.Instance != null && GameClock.Instance.enabled && GameClock.Instance.CurrentHour >= 16f, "sunset begins after the root choice", 10);
            GameClock.Instance.secondsPerGameHour = 1f;
            Note("FIXTURE sunset clock accelerated to 1 s per game hour (natural 25 s/h was verified 2026-09-26)");
            await Until(() => GameClock.Instance.CurrentHour >= 19f, "sunset reaches 19:00", 15);
            Check(HudClockMatches(), "D4 HUD clock follows the sunset clock");
            await Shot("D4_hud_sunset");
            await Until(() => GameClock.Instance.CurrentHour >= 19.8f, "sunset reaches 19:48", 15);
            GameClock.Instance.secondsPerGameHour = 25f; // 20:00 스냅은 제품 속도로 통과시킨다.
            Note("FIXTURE sunset clock restored to the product 25 s per game hour for the last minutes before 20:00");
            await Until(() => progress.NightReady, "night shop ready at 20:00", 15);

            // 가판대 3개 × 같은 상품 5개 (fixture).
            var standEntry = catalog.Find(DemoPlaceableKind.DisplayStand);
            string[] products = { "Items/Item_Wood", "Items/Item_Fish", "Items/Item_Plank" };
            var stands = new List<ShopSlot>();
            for (int i = 0; i < 3; i++)
            {
                Place(placement, progress, inventory, standEntry, "stand" + i);
                var slot = progress.OperatingShop.Slots.FirstOrDefault(s => s != null && s.isActiveAndEnabled && s.IsEmpty && !stands.Contains(s));
                Require(slot != null && slot.stockCapacity == DemoSettlementController.OpeningStandCapacity, "stand " + i + " capacity");
                var product = Resources.Load<Item>(products[i]);
                Require(product != null && inventory.TryReceiveToHotbar(product, 5), "stock " + products[i]);
                inventory.SelectHotbarSlot(inventory.hotbar.slots.FindIndex(s => !s.IsEmpty && s.item == product));
                slot.Interact(player);
                Require(!slot.IsEmpty && slot.currentItem.count == 5, "five units displayed on stand " + i);
                slot.Interact(player);
                Require(ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen, "price UI " + i);
                typeof(ShopPriceUI).GetMethod("OnConfirm", Private).Invoke(ShopPriceUI.instance, null);
                stands.Add(slot);
            }
            int stocked = stands.Sum(s => s.IsEmpty ? 0 : s.currentItem.count);
            Note($"FIXTURE 3 stands x5 stocked={stocked} prices={string.Join("/", stands.Select(s => s.EffectiveDisplayPrice))}G (granted items, ShopSlot.Interact, price confirm)");
            // 데모 조작에서 문은 빈손일 때만 대상이 된다(PlayerInteraction). 도구를 든 채 문 앞에 서면 [X] 안내가 보여야 한다.
            inventory.SelectHotbarSlot(0);
            await Delay(300);
            Check(EquipmentSystem.CurrentHeld(player) != null, "hotbar slot 1 tool is held before the door");

            // 상점 문: E로 기존 BuildingEntrance 사용.
            var interior = progress.OperatingShop.GetComponentInParent<DemoShopInterior>(true) ??
                           UnityEngine.Object.FindObjectsByType<DemoShopInterior>(FindObjectsSortMode.None).FirstOrDefault();
            Require(interior != null && interior.entrance != null && interior.outsideSpawn != null, "shop door");
            Vector3 front = interior.outsideSpawn.position;
            Vector3 look = interior.entrance.transform.position - front; look.y = 0f;
            Teleport(player, front, look.sqrMagnitude > .01f ? Quaternion.LookRotation(look.normalized) : player.transform.rotation);
            Note("FIXTURE approach: player placed on the door's outside spawn, facing the door");
            await Delay(800);
            string heldPrompt = PromptText();
            Note("PROMPT holding a tool at the door: " + heldPrompt);
            Check(!Targets(player, interior.entrance) && heldPrompt.Contains("[X] 손 비우기") && heldPrompt.Contains("상점 들어가기"),
                "P2 tool in hand at the door shows [X] 손 비우기 → [E] 상점 들어가기");
            await Shot("P2_door_x_hint");
            await Press(Key.X);
            Note("INPUT X (empty hands at the door)");
            Check(EquipmentSystem.CurrentHeld(player) == null, "X empties the hands");
            var selected = SelectedSlotColor();
            Note($"HOTBAR selected slot color after X = {selected}");
            Check(selected.a > 0f && selected.a < .9f, "D4 hotbar shows the empty-hand (pale) selection after X");
            await Delay(400);
            bool targeted = Targets(player, interior.entrance);
            Check(PromptText().StartsWith("[E] 상점 들어가기"), "P2 after X the door prompt reads [E] 상점 들어가기 (" + PromptText() + ")");
            await Shot("D4_hud_prompt_door");
            HudLayout("door prompt", true);
            Check(targeted, "E targets the shop door from its outside spawn");
            await Press(Key.E);
            Note("INPUT E at shop door");
            bool entered = await Wait(() => interior.IsInside, 6);
            Check(entered, "E enters the shop through the existing BuildingEntrance");
            if (!entered)
            {
                interior.entrance.Interact(player);
                Note("FIXTURE fallback entrance.Interact(player) after the E door check failed");
                await Until(() => interior.IsInside, "shop interior after fallback", 6);
            }
            await Delay(900);

            // D3 — OPEN: P → 휴대폰 상점 앱 → 영업 시작.
            int moneyAtOpen = EconomyService.Instance.Money;
            int day = GameClock.Instance.CurrentDay;
            var statsAtOpen = SalesLogManager.Instance.GetDailyDecisionStats(day);
            SalesLogManager.OnSaleRecorded += CountSale;
            loop.OpeningShopClosed += CountClose;
            app = await OpenPhoneShopApp();
            ScrollShopApp(app, 1f);
            await Delay(400);
            await Shot("D3_phone_open_ready");
            var open = Named<Button>(app, "Open");
            Require(open != null && open.interactable, "영업 시작 enabled at night with stocked stands");
            Check(TopHitIs(open.gameObject), "D3 영업 시작 button is not covered by another UI");
            Click(open.gameObject);
            Note("INPUT pointer click 영업 시작");
            await Until(() => loop.IsShopOpenForCustomers, "영업 시작 opens the shop", 3);
            var openFeedback = Named<TextMeshProUGUI>(app, "Feedback");
            Note("SHOP APP after OPEN: state=" + Named<TextMeshProUGUI>(app, "State")?.text + " feedback=" + openFeedback?.text);
            if (ManualClose)
            {
                await ManualCloseCheck(app, progress, loop);
                return;
            }
            await ClosePhoneWithEscape("open");

            // D2 — OPEN 동안 손을 떼고 실제 손님 흐름을 관찰한다.
            double openAt = EditorApplication.timeSinceStartup;
            int framesAtOpen = Time.frameCount;
            float realAtOpen = Time.realtimeSinceStartup;
            var lastPos = new Dictionary<NpcController, Vector3>();
            var lastMoveAt = new Dictionary<NpcController, double>();
            var flagged = new HashSet<NpcController>();
            var seenTourists = new HashSet<string>();
            var seenResidents = new HashSet<string>();
            int shots = 0; bool warned = false, diagnosed = false, closeChecked = false;
            while (!loop.OpeningSessionCompleted)
            {
                double t = EditorApplication.timeSinceStartup - openAt;
                if (t > 300) throw new TimeoutException("business session did not auto-close");
                var arrivals = CustomerArrivalController.Instance;
                peakTourists = Mathf.Max(peakTourists, arrivals != null ? arrivals.ActiveTouristCount : 0);
                SampleCustomers(arrivals, lastPos, lastMoveAt, flagged, seenTourists, seenResidents);
                if (!diagnosed && t > 12) { diagnosed = true; Diagnose(progress); }
                if (shots == 0 && t > 12) { shots++; await Shot("D2_open_12s"); }
                else if (shots == 1 && t > 45) { shots++; await Shot("D2_open_45s"); }
                else if (shots == 2 && t > 85) { shots++; await Shot("D2_open_85s"); }
                if (!closeChecked && t > 60 && GameClock.Instance.CurrentHour < 21.3f)
                {
                    // D3 — 영업 중 휴대폰의 수동 마감 버튼이 살아 있는지만 확인한다(누르지 않음: 자동 마감 검증 유지).
                    closeChecked = true;
                    var live = await OpenPhoneShopApp();
                    ScrollShopApp(live, 1f);
                    await Delay(400);
                    await Shot("D3_phone_close_ready");
                    var close = Named<Button>(live, "Close");
                    Check(close != null && close.interactable && TopHitIs(close.gameObject), "D3 영업 마감 button live and uncovered during OPEN");
                    await ClosePhoneWithEscape("during open");
                }
                if (!warned && ToastText().Contains("마감 30분 전") && ToastCard() != null && ToastCard().gameObject.activeInHierarchy)
                { warned = true; await Shot("D4_close_warning_toast"); }
                await Delay(500);
            }
            float closeHour = GameClock.Instance.CurrentHour;
            float realSeconds = Time.realtimeSinceStartup - realAtOpen;
            Note($"AUTO CLOSE at {closeHour:0.00} after {EditorApplication.timeSinceStartup - openAt:0}s real; " +
                 $"Editor Play average {(Time.frameCount - framesAtOpen) / Mathf.Max(.01f, realSeconds):0.0} fps (unfocused Editor, 1920x1080 GameView, not standalone)");
            Check(closeHour >= DemoSettlementController.OpeningCloseHour - .02f, "D2 closed by business hours (22:00), not manually");
            Check(warned, "D4 21:30 closing warning toast shown");

            // D1 — CLOSE 직후 Report 카드와 실제 판매 기록 대조.
            await Delay(2600);
            await Shot("D3_report_card");
            var report = progress.PioneerReport;
            Require(report != null, "Pioneer Report produced at CLOSE");
            var statsAfter = SalesLogManager.Instance.GetDailyDecisionStats(day);
            int remaining = stands.Sum(s => s.IsEmpty ? 0 : s.currentItem.count);
            int moneyDelta = EconomyService.Instance.Money - moneyAtOpen;
            int logPurchases = statsAfter.purchases - statsAtOpen.purchases;
            int logRejections = statsAfter.rejections - statsAtOpen.rejections;
            Note($"SALES events={sales} revenue={revenue}G report={report.sales}/{report.revenue}G moneyDelta={moneyDelta}G stock {stocked}->{remaining} " +
                 $"logPurchases={logPurchases} logRejections={logRejections} reportRejections={report.rejections} closeEvents={closeEvents}");
            Note($"REPORT total={report.total} rank={report.rankKey} settlement/commerce/development/exploration=" +
                 $"{report.settlement}/{report.commerce}/{report.development}/{report.exploration} comments={string.Join(",", report.commentKeys ?? Array.Empty<string>())}");
            Note("BUYERS " + string.Join(", ", buyers));
            Note($"CUSTOMERS touristsSeen={seenTourists.Count} residentsSeen={seenResidents.Count} ({string.Join("/", seenResidents)}) " +
                 $"peakConcurrentTourists={peakTourists} touristsSpawned={CustomerArrivalController.Instance?.TouristsSpawnedThisOpening} " +
                 $"residentInvites={CustomerArrivalController.Instance?.InvitedThisOpening} stuck={stuckEvents}");
            Check(closeEvents == 1, "D2 CLOSE fired exactly once");
            Check(report.sales == sales && report.revenue == revenue, "D1 report sales/revenue equal the SalesLog events");
            Check(moneyDelta == revenue, "D1 Economy money delta equals SalesLog revenue");
            Check(stocked - remaining == sales, "D2 displayed stock decreased by exactly the sales");
            Check(logPurchases == sales && report.rejections == logRejections, "D1 SalesLog purchase/rejection stats match the report");
            Check(sales >= 4, "D2 business density: at least 4 autonomous sales in one opening");
            Check(peakTourists >= 2, "D2 several tourists shopping at once");
            Check(stuckEvents == 0, "D2 no customer blocked on its path for 6s");

            // D3 — 카드 모달, IMGUI 없음, 클릭 가림 없음, E 닫기 → 휴대폰 상점 기록.
            var card = PioneerReportCardUI.Instance;
            Check(card != null && card.IsOpen && progress.IsPanelOpen && PlayerInputHandler.ModalOpen, "D3 report card open and modal after CLOSE");
            Check(NoDebugGui(), "D3 no debug IMGUI component active in the demo route");
            var closeButton = card != null ? Named<Button>(card.transform, "Close") : null;
            Check(closeButton != null && TopHitIs(closeButton.gameObject), "D3 report close button is not covered");
            await Press(Key.E);
            Note("INPUT E on report card");
            await Until(() => card != null && !card.IsOpen && SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen && ShopAppActive(),
                "D3 E closes the card and opens the phone shop record", 4);
            Check(!progress.IsPanelOpen && interior.IsInside, "D3 E closed only the card (no door interaction leaked)");
            Check(PauseManager.Instance == null || !PauseManager.Instance.IsPaused, "D3 closing the card did not pause");
            await Delay(800);
            var reportText = Named<TextMeshProUGUI>(ShopApp(), "Report");
            Check(reportText != null && reportText.text.Contains(report.total.ToString()), "D3 phone shop app shows the latest Report");
            CheckClosedShopApp(ShopApp(), "auto close");
            await Shot("D3_phone_shop_record");

            // D4/P2 — 휴대폰 기록: 상점 앱 → Esc 홈 → [기록] 타일(실제 클릭). 획득 피드백은 기록을 밀어내지 않는다.
            var log = FirstDayWorldPresentation.DispatchLog;
            Note("DISPATCH " + string.Join(" | ", log));
            Check(log.Count > 0 && !log.Any(l => l.Contains("· 보유")), "D4 pickup feedback does not flood the phone record");
            Check(log.Any(l => l.Contains("영업 시작")) && log.Any(l => l.Contains("마감 30분 전")), "D4 business guidance toasts are recorded");
            await Press(Key.Escape);
            Note("INPUT Esc (shop app -> home)");
            await Until(() => SmartphoneUI.instance.homeScreen.activeInHierarchy, "Esc returns from the shop app to the phone home", 3);
            await Delay(300);
            await ReadRecordFromHome("after_close", new[] { "영업 시작", "마감 30분 전" });
            await ClosePhoneWithEscape("after report");

            // D3 — 이동 복귀.
            Vector3 before = player.transform.position;
            await Hold(700, Key.W);
            float moved = Planar(player.transform.position - before);
            if (moved < .4f) { before = player.transform.position; await Hold(700, Key.S); moved = Planar(player.transform.position - before); }
            Note("INPUT W/S hold after closing report and phone");
            Check(moved > .4f, $"D3 movement returns after the report and phone ({moved:0.00} m)");
            await Shot("D3_after_report_free_move");
            HudLayout("after report", false);

            // D2 — 마감 후 관광객 퇴장.
            bool cleared = await Wait(() => CustomerArrivalController.Instance == null || CustomerArrivalController.Instance.ActiveTouristCount == 0, 30);
            Check(cleared, "D2 tourists leave after CLOSE");

            Note("TOAST visible seconds " + string.Join(", ", toastSpans.Select(s => s.ToString("0.00"))));
            Check(toastSpans.Count >= 3 && toastSpans.All(s => s >= 1.9f && s <= 4.25f), "D4 toast stays 2-4 s at top right");
            Finish("PASS");
        }
        catch (Exception ex)
        {
            problem = ex.Message;
            try { await Shot("failure-context"); } catch { }
            Finish("FAIL");
        }
    }

    static async Task<Transform> OpenPhoneShopApp(bool viewRecordFirst = false)
    {
        await Press(Key.P);
        Note("INPUT P");
        await Until(() => SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen, "P opens the phone", 5);
        await Delay(700);
        if (viewRecordFirst)
        {
            await Shot("P2_phone_home");
            await ReadRecordFromHome("morning", new[] { "보급품을 받았어요" });
            await Press(Key.Escape);
            Note("INPUT Esc (record -> home)");
            await Until(() => SmartphoneUI.instance.homeScreen.activeInHierarchy && SmartphoneUI.instance.IsOpen, "Esc returns from the record to the phone home", 3);
            await Delay(300);
        }
        var home = SmartphoneUI.instance.homeScreen;
        var tile = home != null ? home.transform.Find("ShopPanel_Tile") : null;
        Require(tile != null && tile.gameObject.activeInHierarchy, "phone home shows the 상점 tile");
        Check(TopHitIs(tile.gameObject), "D3 상점 tile is not covered by another UI");
        Click(tile.gameObject);
        Note("INPUT pointer click 상점 tile");
        await Until(ShopAppActive, "상점 tile opens the shop app", 3);
        await Delay(400);
        return ShopApp();
    }

    static async Task ClosePhoneWithEscape(string label)
    {
        await Press(Key.Escape);
        await Press(Key.Escape);
        Note("INPUT Esc x2 (" + label + ")");
        await Until(() => SmartphoneUI.instance == null || !SmartphoneUI.instance.IsOpen, "Esc twice closes the phone (" + label + ")", 4);
        await Delay(600);
        Check(!PlayerInputHandler.ModalOpen, "no modal left after closing the phone (" + label + ")");
        Check(PauseManager.Instance == null || !PauseManager.Instance.IsPaused, "Esc did not open pause (" + label + ")");
        Check(Mathf.Approximately(Time.timeScale, 1f), "time scale stays 1 (" + label + ")");
    }

    // P2: 홈의 [기록] 타일을 실제로 눌러 FeedUI 기록을 읽는다(QA SelectTab 사용 안 함).
    static async Task ReadRecordFromHome(string label, string[] expected)
    {
        var phone = SmartphoneUI.instance;
        var tile = phone.homeScreen.transform.Find("RecordPanel_Tile");
        Require(tile != null && tile.gameObject.activeInHierarchy, "phone home shows the 기록 tile");
        var badge = tile.Find("NewBadge");
        Note($"RECORD badge before viewing={(badge != null && badge.gameObject.activeSelf)} ({label})");
        Check(TopHitIs(tile.gameObject), $"P2 기록 tile is not covered ({label})");
        Click(tile.gameObject);
        Note("INPUT pointer click 기록 tile");
        int index = Array.FindIndex(phone.tabPanels, p => p != null && p.GetComponentInChildren<FeedUI>(true) != null);
        await Until(() => index >= 0 && phone.tabPanels[index].activeInHierarchy, "기록 tile opens the FeedUI record panel", 3);
        await Delay(600);
        var texts = phone.tabPanels[index].GetComponentsInChildren<TextMeshProUGUI>(false);
        foreach (var t in texts) t.ForceMeshUpdate();
        var lines = texts.Where(t => t.name == "DispatchLine").Skip(1).ToArray();
        Note("RECORD " + label + ": " + string.Join(" | ", lines.Select(l => l.text)));
        Check(lines.Length >= 2 && expected.All(e => lines.Any(l => l.text.Contains(e))), $"P2 record lists the expected guidance ({label})");
        var cut = lines.Where(l => l.isTextTruncated).Select(l => l.text).ToArray();
        Check(cut.Length == 0, $"P2 record lines readable without truncation ({label})" + (cut.Length > 0 ? ": " + string.Join(" | ", cut) : ""));
        Check(!texts.Any(t => t.text.Contains("Processed") || t.text.Contains("sale(s)") || t.text.Contains("influence")),
            $"P2 record shows no internal English summary ({label})");
        Check(badge == null || !badge.gameObject.activeSelf, $"P2 new-record badge clears after viewing ({label})");
        await Shot("P2_phone_record_" + label);
    }

    // P2: 마감(자동/수동) 뒤 상점 앱의 상태·안내·버튼이 실제 영업 상태와 같은지.
    static void CheckClosedShopApp(Transform app, string label)
    {
        string state = Named<TextMeshProUGUI>(app, "State")?.text ?? "";
        string feedback = Named<TextMeshProUGUI>(app, "Feedback")?.text ?? "";
        var open = Named<Button>(app, "Open");
        var close = Named<Button>(app, "Close");
        Note($"SHOP APP after {label}: state={state} feedback={feedback}");
        Check(state == "오늘 영업 완료" && !feedback.Contains("영업을 시작했어요") && feedback.Contains("영업을 마쳤어요"),
            $"P2 shop app state and message match the closed business ({label})");
        Check(open != null && !open.interactable && close != null && !close.interactable, $"P2 OPEN/CLOSE disabled after the business ({label})");
    }

    // P2: 별도 초기 상태에서 OPEN 직후 휴대폰 [영업 마감]으로 일찍 닫는 제품 버튼 경로.
    static async Task ManualCloseCheck(Transform app, DemoSettlementController progress, DayNightShopLoopController loop)
    {
        await Delay(4000);
        ScrollShopApp(app, 1f);
        var close = Named<Button>(app, "Close");
        Require(close != null && close.interactable, "영업 마감 enabled during OPEN");
        Check(TopHitIs(close.gameObject), "P2 영업 마감 button is not covered");
        float hour = GameClock.Instance.CurrentHour;
        await Shot("P2_manual_close_ready");
        Click(close.gameObject);
        Note($"INPUT pointer click 영업 마감 at {hour:0.00}h");
        await Until(() => progress.PioneerReport != null, "manual CLOSE produces the Pioneer Report", 5);
        Check(closeEvents == 1 && loop.OpeningSessionCompleted && hour < DemoSettlementController.OpeningCloseHour - .2f,
            $"P2 manual CLOSE before 22:00 fires exactly once ({hour:0.00}h)");
        await Delay(1500);
        var card = PioneerReportCardUI.Instance;
        Check(card != null && card.IsOpen, "P2 report card opens after manual CLOSE");
        await Shot("P2_manual_close_report");
        await Press(Key.E);
        Note("INPUT E on report card");
        await Until(() => card != null && !card.IsOpen && ShopAppActive(), "E closes the card to the shop app", 4);
        await Delay(600);
        CheckClosedShopApp(ShopApp(), "manual close");
        await Shot("P2_manual_close_shop_app");
        await ClosePhoneWithEscape("manual close");
        Check(!PlayerInputHandler.ModalOpen && !progress.IsPanelOpen, "P2 no modal left after manual close");
        Finish("PASS");
    }

    static string PromptText()
    {
        var prompt = InteractPromptUI.instance;
        return prompt != null && prompt.promptText != null && prompt.promptText.gameObject.activeInHierarchy
            ? prompt.promptText.text.Replace("\n", " ") : string.Empty;
    }

    static bool ShopAppActive()
    {
        var panels = SmartphoneUI.instance != null ? SmartphoneUI.instance.tabPanels : null;
        return panels != null && panels.Length > 4 && panels[4] != null && panels[4].activeInHierarchy;
    }

    static Transform ShopApp() => SmartphoneUI.instance.tabPanels[4].transform;

    static void ScrollShopApp(Transform app, float normalized)
    {
        var scroll = app.GetComponentInChildren<ScrollRect>(true);
        if (scroll == null) return;
        scroll.verticalNormalizedPosition = normalized;
        Note($"FIXTURE scroll shop app to {(normalized > .5f ? "top" : "bottom")}");
    }

    static void SampleCustomers(CustomerArrivalController arrivals, Dictionary<NpcController, Vector3> lastPos,
        Dictionary<NpcController, double> lastMoveAt, HashSet<NpcController> flagged, HashSet<string> tourists, HashSet<string> residents)
    {
        double now = EditorApplication.timeSinceStartup;
        foreach (var npc in UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None))
        {
            if (npc == null) continue;
            bool tourist = arrivals != null && arrivals.IsTransientTourist(npc);
            if (npc.currentState != NpcController.State.Idle) (tourist ? tourists : residents).Add(npc.name);
            var agent = npc.GetComponent<NavMeshAgent>();
            Vector3 p = npc.transform.position;
            if (!lastPos.TryGetValue(npc, out var previous) || (p - previous).sqrMagnitude > .09f)
            { lastPos[npc] = p; lastMoveAt[npc] = now; continue; }
            bool wantsToMove = agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.pathPending &&
                               !agent.isStopped && agent.hasPath && agent.remainingDistance > .8f && !float.IsInfinity(agent.remainingDistance);
            if (!wantsToMove) { lastMoveAt[npc] = now; continue; }
            if (now - lastMoveAt[npc] > 6 && flagged.Add(npc))
            {
                stuckEvents++;
                Note($"STUCK {npc.name} state={npc.currentState} pos={p} remaining={agent.remainingDistance:0.0} path={agent.pathStatus}");
            }
        }
    }

    static void Diagnose(DemoSettlementController progress)
    {
        var a = CustomerArrivalController.Instance;
        string line = $"DIAG arrivals={(a != null)} enabled={(a != null && a.isActiveAndEnabled)}";
        if (a != null)
        {
            object[] src = { null, null };
            bool source = (bool)typeof(CustomerArrivalController).GetMethod("TryResolveTouristSource", Private).Invoke(a, src);
            var shop = progress.OperatingShop != null ? progress.OperatingShop.transform : null;
            bool shopOnMesh = shop != null && NavMesh.SamplePosition(shop.position, out _, 6f, NavMesh.AllAreas);
            line += $" invite={a.ShouldActivelyInvite} source={source}({(src[0] as NpcController)?.name}) shopOnNavMesh={shopOnMesh}" +
                    $" npcControllers={UnityEngine.Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None).Length}" +
                    $" maxTourists={a.maxTouristsPerOpening} concurrent={a.maxConcurrentTourists} interval={a.touristInviteInterval}";
        }
        Note(line);
    }

    static void CountSale(SaleRecord sale)
    {
        if (sale == null) return;
        sales++; revenue += sale.price;
        buyers.Add($"{sale.buyerName}:{sale.itemName}@{sale.price}G");
    }

    static void CountClose(int purchases, int income, int rejections) => closeEvents++;

    static Color SelectedSlotColor()
    {
        var hud = UnityEngine.Object.FindFirstObjectByType<HotbarUI>();
        var list = hud != null ? typeof(HotbarUI).GetField("slotUIs", Private)?.GetValue(hud) as System.Collections.IList : null;
        int index = Inventory.instance != null ? Inventory.instance.selectedHotbarIndex : 0;
        var ui = list != null && index >= 0 && index < list.Count ? list[index] as Component : null;
        var image = ui != null ? ui.GetComponent<Image>() : null;
        return image != null ? image.color : Color.clear;
    }

    // HUD 시각(hh:mm)이 GameClock과 2게임분 이내로 같은지. 분 tick 사이의 한 프레임 지연은 허용한다.
    static bool HudClockMatches()
    {
        var text = ClockHUD.instance != null && ClockHUD.instance.clockText != null ? ClockHUD.instance.clockText.text : "";
        var parts = text.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], out int h) || !int.TryParse(parts[1], out int m)) return false;
        float clockMinutes = GameClock.Instance.CurrentHour * 60f;
        bool ok = Mathf.Abs(h * 60 + m - clockMinutes) <= 2f;
        Note($"HUD clock {text} vs GameClock {GameClock.Instance.CurrentHour:0.00}h");
        return ok;
    }

    static bool NoDebugGui() =>
        !UnityEngine.Object.FindObjectsByType<WorldBuildingPlacementDebugController>(FindObjectsSortMode.None).Any(c => c.isActiveAndEnabled) &&
        !UnityEngine.Object.FindObjectsByType<WorldGeneratedIslandDebugView>(FindObjectsSortMode.None).Any(c => c.isActiveAndEnabled);

    static void HudLayout(string label, bool requirePrompt)
    {
        var items = new List<KeyValuePair<string, Rect>>();
        AddRect(items, "clock", ClockHUD.instance != null && ClockHUD.instance.clockText != null ? ClockHUD.instance.clockText.transform.parent as RectTransform : null);
        AddRect(items, "money", MoneyHUD.instance != null && MoneyHUD.instance.moneyText != null ? MoneyHUD.instance.moneyText.transform.parent as RectTransform : null);
        AddRect(items, "toast", ToastCard());
        AddRect(items, "prompt", InteractPromptUI.instance != null && InteractPromptUI.instance.promptText != null ? InteractPromptUI.instance.promptText.transform.parent as RectTransform : null);
        var hotbar = GameObject.Find("Hotbar_1_to_9");
        AddRect(items, "hotbar", hotbar != null ? hotbar.transform as RectTransform : null);
        Note($"HUD {label}: " + string.Join("  ", items.Select(i => $"{i.Key}=({i.Value.xMin:0},{i.Value.yMin:0} {i.Value.width:0}x{i.Value.height:0})")));
        for (int a = 0; a < items.Count; a++)
            for (int b = a + 1; b < items.Count; b++)
                Check(!items[a].Value.Overlaps(items[b].Value), $"D4 HUD {label}: {items[a].Key} clear of {items[b].Key}");
        Check(new[] { "clock", "money", "hotbar" }.All(k => items.Any(i => i.Key == k)), $"D4 HUD {label}: time, money and hotbar visible");
        Check(HudClockMatches(), $"D4 HUD {label}: clock text equals GameClock");
        Check(MoneyHUD.instance != null && MoneyHUD.instance.moneyText != null && EconomyService.Instance != null &&
              new string(MoneyHUD.instance.moneyText.text.Where(char.IsDigit).ToArray()) == EconomyService.Instance.Money.ToString(),
              $"D4 HUD {label}: money text equals EconomyService");
        if (!requirePrompt) return;
        bool prompt = items.Any(i => i.Key == "prompt");
        Check(prompt, $"D4 HUD {label}: interaction prompt visible");
        if (prompt) Note("PROMPT " + InteractPromptUI.instance.promptText.text.Replace("\n", " "));
    }

    static void AddRect(List<KeyValuePair<string, Rect>> items, string key, RectTransform rt)
    {
        if (rt == null || !rt.gameObject.activeInHierarchy) return;
        var canvas = rt.GetComponentInParent<Canvas>();
        if (canvas == null || !canvas.enabled) return;
        var root = canvas.rootCanvas;
        Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        items.Add(new KeyValuePair<string, Rect>(key, Rect.MinMaxRect(min.x, min.y, max.x, max.y)));
    }

    static bool TopHitIs(GameObject target)
    {
        if (EventSystem.current == null || target == null) return false;
        var rt = (RectTransform)target.transform;
        var root = target.GetComponentInParent<Canvas>().rootCanvas;
        Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        var data = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(camera, (corners[0] + corners[2]) * .5f) };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        var top = hits.Count > 0 ? hits[0].gameObject : null;
        return top != null && (top == target || top.transform.IsChildOf(target.transform));
    }

    static void Click(GameObject target) =>
        ExecuteEvents.Execute(target, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);

    static T Named<T>(Transform root, string name) where T : Component =>
        root.GetComponentsInChildren<T>(true).FirstOrDefault(c => c.name == name);

    static bool Targets(GameObject player, Component expected)
    {
        var interaction = player.GetComponent<PlayerInteraction>();
        if (interaction == null) return false;
        object[] args = { null, null };
        bool found = (bool)typeof(PlayerInteraction).GetMethod("TryFindInteractable", Private).Invoke(interaction, args);
        return found && ReferenceEquals(args[0], expected);
    }

    static RectTransform ToastCard() => FirstDayWorldPresentation.Instance == null ? null :
        typeof(FirstDayWorldPresentation).GetField("_toastCard", Private)?.GetValue(FirstDayWorldPresentation.Instance) as RectTransform;

    static string ToastText()
    {
        var text = FirstDayWorldPresentation.Instance == null ? null :
            typeof(FirstDayWorldPresentation).GetField("_toast", Private)?.GetValue(FirstDayWorldPresentation.Instance) as TextMeshProUGUI;
        return text != null ? text.text : string.Empty;
    }

    // Toast 호출(_toastUntil 증가)부터 카드가 사라질 때까지의 실제 표시 시간을 잰다. Report가 가린 경우는 제외.
    static void WatchToast()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying || finished) return;
        var presentation = FirstDayWorldPresentation.Instance;
        var card = ToastCard();
        var untilField = typeof(FirstDayWorldPresentation).GetField("_toastUntil", Private);
        if (presentation == null || card == null || untilField == null) return;
        float until = (float)untilField.GetValue(presentation);
        float now = Time.unscaledTime;
        if (until > lastToastUntil + .01f) { toastCalledAt = now; lastToastUntil = until; }
        bool visible = card.gameObject.activeInHierarchy;
        if (toastVisible && !visible && toastCalledAt >= 0f &&
            !(DemoSettlementController.Instance != null && DemoSettlementController.Instance.IsPanelOpen))
            toastSpans.Add(now - toastCalledAt);
        toastVisible = visible;
    }

    static void Place(WorldBuildingPlacementService placement, DemoSettlementController progress, Inventory inventory, DemoPlaceableEntry entry, string suffix)
    {
        Require(entry != null && progress.CanPlace(entry), "entry allowed " + suffix);
        string id = "d2-review-" + suffix;
        placement.RegisterDefinition(id, entry.Definition);
        var grid = placement.GetComponent<WorldGridService>();
        Require(grid.WorldToCell(FirstDayWorldPresentation.Instance.Harbor, out var harbor), "harbor cell");
        Vector2Int candidate = default; bool found = false;
        for (int radius = 5; radius <= 50 && !found; radius++)
            for (int dz = -radius; dz <= radius && !found; dz++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    candidate = harbor + new Vector2Int(dx, dz);
                    var evaluated = placement.Evaluate(id, candidate, 0);
                    if (evaluated.Succeeded && DemoSettlementController.IsCustomerReachable(grid, evaluated, entry.kind)) { found = true; break; }
                }
        Require(found, "customer-reachable footprint " + suffix);
        var chosen = placement.Evaluate(id, candidate, 0);
        Note($"FIXTURE place {suffix} cell={candidate} entrance={chosen.Entrance}");
        var result = placement.TryPlace(id, candidate, 0);
        Require(result.Succeeded && placement.TryGetPlacement(id, out WorldPlacedBuildingRuntime runtime), "place " + suffix);
        placement.TryGetPlacement(id, out runtime);
        typeof(DemoSettlementController).GetMethod("OnPlaced", Private).Invoke(progress, new object[] { runtime, entry });
        // 실제 배치 입력은 선택한 키트 1개를 소모한다. HUD가 실제 상태처럼 보이도록 같은 양을 회수한다.
        if (entry.item != null && inventory.CountItems(entry.item) > 0) inventory.RemoveItems(entry.item, 1);
    }

    static void Teleport(GameObject player, Vector3 position, Quaternion rotation)
    {
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.SetPositionAndRotation(position, rotation);
        if (cc != null) cc.enabled = true;
        player.GetComponent<PlayerController>()?.ResetMotionAfterTeleport();
    }

    static float Planar(Vector3 v) { v.y = 0f; return v.magnitude; }

    static void FocusGameView() => EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();

    static async Task Press(Key key) => await Hold(110, key);

    static async Task Hold(int milliseconds, params Key[] keys)
    {
        FocusGameView();
        keyboard.MakeCurrent();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        await Task.Delay(milliseconds);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        await Task.Delay(150);
    }

    static Task Delay(int milliseconds) => Task.Delay(milliseconds);

    static async Task<bool> Wait(Func<bool> condition, float seconds)
    {
        double deadline = EditorApplication.timeSinceStartup + seconds;
        while (!condition() && EditorApplication.timeSinceStartup < deadline) await Task.Delay(100);
        return condition();
    }

    static async Task Until(Func<bool> condition, string label, float seconds)
    {
        if (!await Wait(condition, seconds)) throw new InvalidOperationException(label);
    }

    static async Task Shot(string name)
    {
        string path = Path.Combine(OutDir, name + ".png");
        ScreenCapture.CaptureScreenshot(path);
        double deadline = EditorApplication.timeSinceStartup + 6;
        while (!File.Exists(path) && EditorApplication.timeSinceStartup < deadline) await Task.Delay(100);
        await Task.Delay(200);
        Note($"SHOT {name} at {(GameClock.Instance != null ? GameClock.Instance.CurrentHour.ToString("0.00") : "?")}h" + (File.Exists(path) ? "" : " NOT WRITTEN"));
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
        if (status == "PASS" && (errors != 0 || failures != 0)) status = "FAIL";
        Note($"{status} checks={checks} failed={failures} consoleErrors={errors}" + (problem == null ? "" : " problem=" + problem));
        finished = true;
        SalesLogManager.OnSaleRecorded -= CountSale;
        if (DayNightShopLoopController.Instance != null) DayNightShopLoopController.Instance.OpeningShopClosed -= CountClose;
        if (keyboard != null)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.RemoveDevice(keyboard);
            keyboard = null;
        }
        Application.runInBackground = SessionState.GetBool(BackgroundKey, false);
        SessionState.SetBool(Active, false);
        SessionState.EraseString(ModeKey);
        EditorApplication.isPlaying = false;
    }
}
