using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum DemoSpecialization { None = 0, Forestry = 1, Mining = 2, Fisheries = 3, Agriculture = 4 }

// Opening session progression only. This observes existing placements and inventory;
// it never owns a second world/placement/shop/save store.
public sealed class DemoSettlementController : MonoBehaviour
{
    public static DemoSettlementController Instance { get; private set; }
    public bool Established { get; private set; }
    public bool HasShopBase => !string.IsNullOrEmpty(shopId);
    public int TentCount => tents.Count;
    public bool NightReady { get; private set; }
    bool sunsetStarted;
    bool _daylightHeld;  // GameClock robustness: daylight-hold ForceSet runs once per session
    float nextClockCheck;
    public int LicensePoints { get; private set; }
    public DemoSpecialization SelectedRoot { get; private set; }
    public Shop OperatingShop { get; private set; }
    public bool IsPanelOpen { get; private set; }
    public DemoPioneerReportData PioneerReport { get; private set; }
    public int GrantedStands { get; private set; }
    // Canon v2 §10: placed Field Workbench Kit becomes a functional Workbench for CraftingUI
    public Workbench PlacedWorkbench { get; private set; }
    public event Action SettlementEstablished;
    public event Action<DemoSpecialization> SpecializationSelected;
    public IReadOnlyList<DemoResident> Residents => residents;
    WorldBuildingPlacementService placement;
    WorldGridService grid;
    WorldHotbarPlacementController input;
    Inventory inventory;
    DemoPlaceableCatalog catalog;
    string shopId;
    readonly List<string> tents = new List<string>();
    readonly List<DemoResident> residents = new List<DemoResident>();
    bool granting;

    public void Configure(WorldGridService world, Inventory player)
    {
        Instance = this; grid = world; inventory = player;
        placement = grid.GetComponent<WorldBuildingPlacementService>();
        catalog = DemoPlaceableCatalog.Load();
        DayNightShopLoopController.Instance?.ConfigureOpeningDemo(null);
        placement.MoveAllowed = placed => !(DayNightShopLoopController.Instance?.IsShopOpenForCustomers ?? false);
        if (CustomerArrivalController.Instance == null) new GameObject("DemoCustomerArrivals").AddComponent<CustomerArrivalController>();
        input = player.GetComponent<WorldHotbarPlacementController>() ?? player.gameObject.AddComponent<WorldHotbarPlacementController>();
        input.Configure(placement, player); input.Placed += OnPlaced;
        var selected = DemoRouteController.SelectedCompanions;
        foreach (var candidate in selected)
        {
            var npc = FirstDayWorldPresentation.Instance.transform.Find("Companion_" + candidate.id);
            if (npc == null) continue;
            var resident = npc.GetComponent<DemoResident>() ?? npc.gameObject.AddComponent<DemoResident>();
            resident.Configure(candidate.id, candidate.profile); residents.Add(resident);
        }
        if (catalog != null && ItemRegistry.Instance != null)
        {
            foreach (var entry in catalog.entries ?? Array.Empty<DemoPlaceableEntry>())
                if (entry?.item != null && !ItemRegistry.Instance.allItems.Contains(entry.item)) ItemRegistry.Instance.allItems.Add(entry.item);
            foreach (var upgrade in catalog.upgrades ?? Array.Empty<DemoToolUpgrade>())
                if (upgrade?.tool != null && !ItemRegistry.Instance.allItems.Contains(upgrade.tool)) ItemRegistry.Instance.allItems.Add(upgrade.tool);
        }
        var zone = new GameObject("SettlementGround").AddComponent<WorldPlaceableZone>();
        zone.transform.SetParent(transform, false);
        // Demo256 전체를 월드 정의에서 계산한다. 항구/외곽의 첫 Blueprint도 같은 정착 구역이다.
        WorldGridDefinition definition = grid.Definition;
        float width = definition.Width * definition.CellSize;
        float depth = definition.Height * definition.CellSize;
        zone.transform.position = definition.WorldOrigin + new Vector3(
            (definition.Width - 1) * definition.CellSize * .5f, 0f,
            (definition.Height - 1) * definition.CellSize * .5f);
        zone.surface = WorldPlaceableSurface.Settlement;
        zone.localBounds = new Bounds(Vector3.zero,
            new Vector3(width + definition.CellSize, 80f, depth + definition.CellSize));
        placement.RegisterZone(zone);
        if (CraftingUI.instance == null) new GameObject("DemoCraftingUI").AddComponent<CraftingUI>();
    }

    void OnDestroy()
    {
        if (managementSkin != null) Destroy(managementSkin);
        if (input != null) input.Placed -= OnPlaced;
        // 재시작 중 이전 런타임의 Destroy가 지연되면 새 정착 인스턴스의 이동 게이트를 지우지 않는다.
        if (Instance == this)
        {
            if (IsPanelOpen) PlayerInputHandler.RestoreGameplayCursor();
            if (placement != null) placement.MoveAllowed = null;
            Instance = null;
        }
    }

    void OnPlaced(WorldPlacedBuildingRuntime placed, DemoPlaceableEntry entry)
    {
        if (entry.kind == DemoPlaceableKind.ShopBase)
        {
            Shop placedShop = placed.GameObject.GetComponentInChildren<Shop>(true);
            if (placedShop == null)
            {
                Debug.LogError("[DemoSettlement] Placed Shop/Base has no existing Shop authority.");
                return;
            }
            shopId = placed.InstanceId;
            OperatingShop = placedShop;
            placed.GameObject.GetComponent<DemoShopInterior>()?.Bind(this);
            // Guard: DayNightShopLoopController may not exist in all demo scenes.
            if (DayNightShopLoopController.Instance != null)
                DayNightShopLoopController.Instance.ConfigureOpeningDemo(OperatingShop);
            else
                Debug.LogWarning("[DemoSettlement] DayNightShopLoopController not found — OPEN/CLOSE gate unavailable.");
            CustomerArrivalController.Instance?.BindOperatingShop(OperatingShop);
            WorldGameplayAdapterService.Instance?.BindFirstDayOperatingShop(OperatingShop);
            foreach (var zone in placed.GameObject.GetComponentsInChildren<WorldPlaceableZone>()) placement.RegisterZone(zone);
            TryClaimStands();
        }
        if (entry.kind == DemoPlaceableKind.ResidentTent)
        {
            if (!tents.Contains(placed.InstanceId)) tents.Add(placed.InstanceId);
            var owner = residents.FirstOrDefault(r => r.TentId == placed.InstanceId) ?? residents.FirstOrDefault(r => string.IsNullOrEmpty(r.TentId));
            owner?.Claim(placed, grid);
        }
        if (entry.kind == DemoPlaceableKind.DisplayStand && OperatingShop != null)
        {
            placed.GameObject.GetComponentInChildren<ShopSlot>()?.BindOperatingShop(OperatingShop);
            WorldGameplayAdapterService.Instance?.BindFirstDayOperatingShop(OperatingShop);
        }
        // Canon v2 §10: placed Workbench Kit → functional Workbench component for CraftingUI
        if (entry.kind == DemoPlaceableKind.Workbench && PlacedWorkbench == null)
        {
            var wb = placed.GameObject.GetComponentInChildren<Workbench>(true);
            if (wb == null)
            {
                // The placed prefab may not have a Workbench yet — add one to its root.
                // Requires a Collider (already present as building footprint or trigger).
                if (placed.GameObject.GetComponent<Collider>() == null)
                    placed.GameObject.AddComponent<BoxCollider>();
                wb = placed.GameObject.AddComponent<Workbench>();
                wb.workbenchType = WorkbenchType.BasicWorkbench;
                wb.displayName = "P.A. Field Workbench";
                wb.ApplyFunctionalArt();
            }
            PlacedWorkbench = wb;
            // Ensure CraftingUI exists and is open-capable.
            if (CraftingUI.instance == null) new GameObject("DemoCraftingUI").AddComponent<CraftingUI>();
            FirstDayWorldPresentation.Toast("작업대를 설치했어요. E로 제작 메뉴를 열 수 있습니다.");
            Debug.Log("[DemoSettlement] Workbench placed and bound to CraftingUI.");
        }
        if (!Established && !string.IsNullOrEmpty(shopId) && tents.Count == 2 && residents.Count == 2 && residents.All(r => !string.IsNullOrEmpty(r.TentId)))
        {
            Established = true; LicensePoints = 1;
            var mine = Resources.FindObjectsOfTypeAll<MiningSpot>().FirstOrDefault(m => m.gameObject.scene.IsValid() && m.IsDirectWorld);
            foreach (var resident in residents)
                foreach (var upgrade in catalog?.upgrades ?? Array.Empty<DemoToolUpgrade>())
                {
                    try { resident.BindWork(upgrade, mine != null ? mine.transform : null); }
                    catch (System.Exception ex) { Debug.LogWarning("[DemoSettlement] BindWork skipped: " + ex.Message); }
                }
            SettlementEstablished?.Invoke();
            FirstDayWorldPresentation.Toast("정착 완료 · 개척 허가 포인트 +1\n상점에서 첫 전문 분야를 선택하세요.");
        }
    }

    public bool CanPlace(DemoPlaceableEntry entry) => entry != null &&
        !(DayNightShopLoopController.Instance?.IsShopOpenForCustomers ?? false) &&
        (entry.kind != DemoPlaceableKind.ShopBase || string.IsNullOrEmpty(shopId)) &&
        (entry.kind != DemoPlaceableKind.ResidentTent || tents.Count < 2) &&
        (entry.kind != DemoPlaceableKind.DisplayStand || OperatingShop != null) &&
        // Canon v2 §10: Workbench can only be placed once
        (entry.kind != DemoPlaceableKind.Workbench || PlacedWorkbench == null);

    public void TryClaimStands()
    {
        if (granting || OperatingShop == null || catalog == null) return;
        granting = true;
        try
        {
            var item = catalog.Find(DemoPlaceableKind.DisplayStand)?.item;
            while (GrantedStands < 3 && item != null && inventory.TryReceiveToHotbar(item, 1)) GrantedStands++;
            if (GrantedStands < 3) FirstDayWorldPresentation.Toast("가방 공간을 비운 뒤 상점에서 남은 가판대를 받으세요.");
        }
        finally { granting = false; }
    }

    public bool TryChooseRoot(DemoSpecialization root)
    {
        if (!Established || LicensePoints != 1 || SelectedRoot != DemoSpecialization.None ||
            root == DemoSpecialization.None || !Enum.IsDefined(typeof(DemoSpecialization), root)) return false;
        SelectedRoot = root; LicensePoints = 0;
        SpecializationSelected?.Invoke(root); return true;
    }

    public bool HasSynergy(DemoSpecialization root) => residents.Any(r =>
        root == DemoSpecialization.Forestry && r.CompanionId.StartsWith("Lumberjack", StringComparison.Ordinal) ||
        root == DemoSpecialization.Mining && r.CompanionId.StartsWith("Miner", StringComparison.Ordinal) ||
        root == DemoSpecialization.Fisheries && r.CompanionId.StartsWith("Fisher", StringComparison.Ordinal) ||
        root == DemoSpecialization.Agriculture && r.CompanionId.StartsWith("Farmer", StringComparison.Ordinal));

    public static string NextPreview(DemoSpecialization root) => root switch
    {
        DemoSpecialization.Forestry => "Better Axe → Carpentry (preview)",
        DemoSpecialization.Mining => "Better Pickaxe → Metalwork (preview)",
        DemoSpecialization.Fisheries => "Better Rod → Fisheries (preview)",
        DemoSpecialization.Agriculture => "Hoe / Crop Plot / Seeds (preview only)",
        _ => ""
    };

    public void OpenManagement()
    {
        input?.Cancel();
        IsPanelOpen = true;
        TryClaimStands();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowPioneerReport(DemoPioneerReportData report)
    {
        if (report == null) return;
        PioneerReport = report;
        OpenManagement();
        FirstDayWorldPresentation.Toast($"개척 보고서 · {ReportRankDisplay(report.rankKey)} · {report.total}점");
    }

    public void CloseManagement()
    {
        if (!IsPanelOpen) return;
        IsPanelOpen = false;
        PlayerInputHandler.RestoreGameplayCursor();
    }

    // Translate raw comment/rank keys to readable Korean for the OnGUI panel.
    static string ReportRankDisplay(string key) => key switch
    {
        "rank.s" => "S",
        "rank.a" => "A",
        "rank.b" => "B",
        "rank.c" => "C",
        _ => "D"
    };

    static string CommentDisplay(string key) => key switch
    {
        "settlement.established"   => "정착 완료 — 마을의 기초를 세웠습니다.",
        "settlement.in_progress"   => "아직 정착 중 — 텐트와 상점을 모두 세우세요.",
        "commerce.first_customers" => "첫 손님들이 찾아왔습니다.",
        "commerce.no_sale"         => "아직 판매 기록 없음 — 상점을 열어보세요.",
        "commerce.price_experiment"=> "가격 실험 정신이 돋보입니다.",
        "development.shared_tools" => "NPC에게 도구를 나눠줬습니다. 생산성이 오릅니다!",
        "development.chosen_path"  => "전문 분야를 선택해 성장 경로를 열었습니다.",
        "exploration.curiosity"    => "낚시와 채집 두 가지 모두 도전했습니다.",
        "exploration.traveller"    => "다양한 지형을 탐험했습니다.",
        _ => key
    };

    void Update()
    {
        if (Time.unscaledTime >= nextClockCheck)
        {
            nextClockCheck = Time.unscaledTime + .2f;
            var clock = GameClock.Instance;
            if (clock != null)
            {
                if (!sunsetStarted && Established && SelectedRoot != DemoSpecialization.None)
                {
                    sunsetStarted = true; clock.ForceSet(Mathf.Max(16, clock.CurrentHour), 1, "Demo sunset");
                    clock.secondsPerGameHour = 25; clock.enabled = true;
                    FirstDayWorldPresentation.Toast("해가 지고 있어요. 첫 영업을 준비하세요.");
                }
                if (sunsetStarted && !NightReady && clock.CurrentHour >= 20)
                { NightReady = true; clock.ForceSet(20, 1, "Demo night shop"); clock.enabled = false; }
                // Structural robustness: daylight hold only executes once (_daylightHeld) to avoid
                // repeated ForceSet calls every 0.2 s while settlement has not yet been established.
                else if (!sunsetStarted && !_daylightHeld && clock.CurrentHour >= 16)
                { _daylightHeld = true; clock.ForceSet(16, 1, "Settlement daylight hold"); clock.enabled = false; }
            }
        }
        if (IsPanelOpen && (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true ||
            InventoryUI.instance != null && InventoryUI.instance.gameObject.activeSelf || SmartphoneUI.instance?.IsOpen == true)) CloseManagement();
    }

    static string RootDisplay(DemoSpecialization root) => root switch
    {
        DemoSpecialization.Forestry => "임업", DemoSpecialization.Mining => "광업",
        DemoSpecialization.Fisheries => "수산업", DemoSpecialization.Agriculture => "농업", _ => "미선택"
    };

    void DrawReport()
    {
        var title = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
        var score = new GUIStyle(title) { fontSize = 34 };
        score.normal.textColor = new Color(1f, .88f, .53f);
        GUILayout.Label("첫 영업 · 개척 보고서", title, GUILayout.Height(42));
        GUILayout.Label($"{ReportRankDisplay(PioneerReport.rankKey)} 등급   {PioneerReport.total} / 100점", score, GUILayout.Height(54));
        GUILayout.Space(12);
        GUILayout.Label($"판매 {PioneerReport.sales}건     매출 {PioneerReport.revenue:N0} G", title, GUILayout.Height(40));
        GUILayout.Label($"구매 보류 {PioneerReport.rejections}회", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter }, GUILayout.Height(28));
        GUILayout.Space(16);
        GUILayout.Label("항목별 평가");
        ReportRow("정착", PioneerReport.settlement);
        ReportRow("상업", PioneerReport.commerce);
        ReportRow("발전", PioneerReport.development);
        ReportRow("탐험", PioneerReport.exploration);
        GUILayout.Space(14);
        GUILayout.Label("개척 코멘트");
        foreach (string key in PioneerReport.commentKeys ?? Array.Empty<string>())
            GUILayout.Label("· " + CommentDisplay(key), GUILayout.Height(30));
        GUILayout.Space(12);
    }

    static void ReportRow(string label, int value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(140));
        GUILayout.Label($"{value} / 25점", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight });
        GUILayout.EndHorizontal();
    }

    GUISkin managementSkin;
    void OnGUI()
    {
        if (!IsPanelOpen) return;
        var previousSkin = GUI.skin;
        if (managementSkin == null)
        {
            managementSkin = Instantiate(previousSkin);
            var font = TMPro.TMP_Settings.defaultFontAsset;
            if (font != null && font.sourceFontFile != null) managementSkin.font = font.sourceFontFile;
            managementSkin.label.fontSize = 18; managementSkin.label.wordWrap = true;
            managementSkin.label.normal.textColor = new Color(.96f, .93f, .82f);
            managementSkin.button.fontSize = 18; managementSkin.button.fixedHeight = 38;
            managementSkin.button.margin = new RectOffset(4, 4, 5, 5);
        }
        GUI.skin = managementSkin;
        var panel = new Rect(Screen.width / 2f - 360, Screen.height / 2f - 335, 720, 670);
        var previousColor = GUI.color; GUI.color = new Color(.10f, .16f, .15f, 1f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture); GUI.color = previousColor;
        GUILayout.BeginArea(new Rect(panel.x + 28, panel.y + 24, panel.width - 56, panel.height - 48));
        if (PioneerReport != null)
        {
            DrawReport();
            if (GUILayout.Button("닫기  ·  Esc")) CloseManagement();
            GUILayout.EndArea();
            GUI.skin = previousSkin;
            return;
        }
        GUILayout.Label("개척 상점 · 거점");
        GUILayout.Label(Established ? "정착 완료" : "상점과 주민 텐트 2개가 필요합니다");
        GUILayout.Label("개척 포인트: " + LicensePoints + "  ·  전문 분야: " + RootDisplay(SelectedRoot));
        if (PlacedWorkbench != null) GUILayout.Label("작업대 설치 완료 · E로 제작");
        GUILayout.Space(4);
        if (PioneerReport == null) foreach (DemoSpecialization root in Enum.GetValues(typeof(DemoSpecialization)))
        {
            if (root == DemoSpecialization.None) continue;
            GUI.enabled = Established && SelectedRoot == DemoSpecialization.None;
            if (GUILayout.Button(RootDisplay(root) + (HasSynergy(root) ? " · 동행 시너지" : ""))) TryChooseRoot(root);
            GUI.enabled = true;
            if (root == SelectedRoot) GUILayout.Label("  → " + NextPreview(root));
        }
        GUILayout.Space(4);
        var loop = DayNightShopLoopController.Instance;
        if (loop != null && NightReady && !loop.OpeningSessionCompleted)
        {
            if (loop.IsShopOpenForCustomers)
            { if (GUILayout.Button("영업 마감")) loop.TryCloseOpeningShop(); }
            else if (GUILayout.Button("영업 시작 (가판대 하나 이상 진열 필요)"))
            {
                input.Cancel();
                if (!loop.TryOpenShop()) FirstDayWorldPresentation.Toast("가판대 하나 이상에 상품을 진열하세요.");
            }
        }
        if (GrantedStands < 3 && GUILayout.Button("남은 가판대 받기")) TryClaimStands();
        if (GUILayout.Button("닫기")) CloseManagement();
        GUILayout.EndArea();
        GUI.skin = previousSkin;
    }
}
