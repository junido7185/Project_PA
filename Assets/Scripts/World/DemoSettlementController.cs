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
    bool restoring;

    public void Configure(WorldGridService world, Inventory player)
    {
        Instance = this; grid = world; inventory = player;
        placement = grid.GetComponent<WorldBuildingPlacementService>();
        catalog = DemoPlaceableCatalog.Load();
        DayNightShopLoopController.Instance?.ConfigureOpeningDemo(null);
        placement.MoveAllowed = placed => !(DayNightShopLoopController.Instance?.IsShopOpenForCustomers ?? false);
        if (CustomerArrivalController.Instance == null) new GameObject("DemoCustomerArrivals").AddComponent<CustomerArrivalController>();
        ConfigureOpeningCustomers(CustomerArrivalController.Instance);
        input = player.GetComponent<WorldHotbarPlacementController>() ?? player.gameObject.AddComponent<WorldHotbarPlacementController>();
        input.Configure(placement, player); input.Placed += OnPlaced;
        var selected = DemoRouteController.SelectedCompanions;
        foreach (var candidate in selected)
        {
            var npc = FirstDayWorldPresentation.Instance.transform.Find("Companion_" + candidate.id);
            if (npc == null) continue;
            var resident = npc.GetComponent<DemoResident>() ?? npc.gameObject.AddComponent<DemoResident>();
            resident.Configure(candidate.id, candidate.profile); residents.Add(resident);
            // P7: 동행의 이동·집·일과·반응(표현 어댑터). 생산/재고 권위는 기존 WorksiteBinding/ProducerNpcController.
            if (npc.GetComponent<DemoCompanionRoutine>() == null) npc.gameObject.AddComponent<DemoCompanionRoutine>();
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
            if (!restoring) TryClaimStands();
        }
        if (entry.kind == DemoPlaceableKind.ResidentTent)
        {
            if (!tents.Contains(placed.InstanceId)) tents.Add(placed.InstanceId);
            var owner = residents.FirstOrDefault(r => r.TentId == placed.InstanceId) ?? residents.FirstOrDefault(r => string.IsNullOrEmpty(r.TentId));
            owner?.Claim(placed, grid);
        }
        if (entry.kind == DemoPlaceableKind.DisplayStand && OperatingShop != null)
        {
            var standSlot = placed.GameObject.GetComponentInChildren<ShopSlot>();
            if (standSlot != null)
            {
                // D2: 가판대 3개 × 같은 상품 5개까지 → 한 번의 영업에서 여러 손님이 살 수 있다.
                standSlot.stockCapacity = OpeningStandCapacity;
                standSlot.BindOperatingShop(OperatingShop);
            }
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
                wb.ApplyFunctionalArt();
            }
            // P6: 제작 창 제목·안내에 쓰는 데모 표시 이름(프리팹의 영문 이름 대신).
            wb.displayName = "P.A. 현장 작업대";
            PlacedWorkbench = wb;
            // Ensure CraftingUI exists and is open-capable.
            if (CraftingUI.instance == null) new GameObject("DemoCraftingUI").AddComponent<CraftingUI>();
            if (!restoring) FirstDayWorldPresentation.Toast("작업대를 설치했어요. E로 제작 메뉴를 열 수 있습니다.");
            Debug.Log("[DemoSettlement] Workbench placed and bound to CraftingUI.");
        }
        if (!Established && !string.IsNullOrEmpty(shopId) && tents.Count == 2 && residents.Count == 2 && residents.All(r => !string.IsNullOrEmpty(r.TentId)))
        {
            Established = true; LicensePoints = 1;
            var mines = Resources.FindObjectsOfTypeAll<MiningSpot>().Where(m => m.gameObject.scene.IsValid() && m.IsDirectWorld).ToArray();
            foreach (var resident in residents)
                foreach (var upgrade in catalog?.upgrades ?? Array.Empty<DemoToolUpgrade>())
                {
                    // P7: 동행은 자기 집에서 실제로 걸어갈 수 있는 가장 가까운 바위로 일하러 간다.
                    Vector3 from = resident.Home != null ? resident.Home.position : resident.transform.position;
                    var workSpot = resident.CompanionId != null && upgrade?.companionPrefix != null &&
                        resident.CompanionId.StartsWith(upgrade.companionPrefix, StringComparison.Ordinal)
                        ? CompanionWorkSpot(mines, from, resident.CompanionId) : null;
                    try { resident.BindWork(upgrade, workSpot); }
                    catch (System.Exception ex) { Debug.LogWarning("[DemoSettlement] BindWork skipped: " + ex.Message); }
                }
            // P7: 호환 동료는 기본 도구로 바로 일을 시작한다(업그레이드 도구를 받으면 같은 작업이 빨라진다).
            if (!restoring)
                foreach (var resident in residents)
                    if (resident.Worksite != null && resident.Worksite.Assigned) resident.Worksite.Producer.StartStarterWork();
            if (!restoring)
            {
                SettlementEstablished?.Invoke();
                FirstDayWorldPresentation.Toast("정착 완료 · 개척 허가 포인트 +1\nP 휴대폰의 상점 앱에서 첫 전문 분야를 고르세요.");
            }
        }
    }

    // 20:00 OPEN 뒤 22:00 자동 마감 전까지의 첫 영업. 고객·영업 시계·매출 집계가 세션 상태라 저장하지 않는다.
    public bool BusinessInProgress
    {
        get
        {
            var loop = DayNightShopLoopController.Instance;
            return loop != null && NightReady && !loop.OpeningSessionCompleted && loop.IsShopOpenForCustomers;
        }
    }

    // 저장 복원은 새로 구성된 섬에만 적용한다. 진행 중인 섬에 덮어쓰면 월드가 먼저 바뀐 뒤 거절될 수 있다.
    public bool CanRestoreSession(out string reason)
    {
        reason = string.Empty;
        if (placement == null || catalog == null)
        { reason = "FirstDay 배치 서비스가 준비되지 않았습니다."; return false; }
        if (restoring || Established || !string.IsNullOrEmpty(shopId) || tents.Count > 0 ||
            placement.Placements.Any(p => p.GameObject != null && p.GameObject.GetComponent<DemoPlacedObject>() != null))
        { reason = "진행 중인 섬에는 저장을 덮어 불러올 수 없습니다. 섬을 새로 불러와 이어하세요."; return false; }
        return true;
    }

    public DemoSessionSaveData CaptureSaveState()
    {
        var state = new DemoSessionSaveData
        {
            shopId = shopId,
            companionIds = CompanionIdsInTentOrder(),
            selectedRoot = (int)SelectedRoot,
            grantedStands = GrantedStands,
            established = Established,
            nightReady = NightReady,
            sunsetStarted = sunsetStarted,
            // 마감 전에는 Report 관찰 진행(방문 지형·활동)만 담는다. rankKey가 비어 있으면 아직 결말이 아니다.
            pioneerReport = PioneerReport ?? CaptureReportProgress()
        };
        foreach (var placed in placement.Placements.OrderBy(p => p.InstanceId, StringComparer.Ordinal))
        {
            var marker = placed.GameObject != null ? placed.GameObject.GetComponent<DemoPlacedObject>() : null;
            if (marker?.Entry == null || marker.InstanceId != placed.InstanceId) continue;
            var record = new WorldPlacedBuildingSaveData
            {
                instanceId = placed.InstanceId,
                buildingId = placed.Definition.StableId,
                anchorX = placed.Anchor.x,
                anchorZ = placed.Anchor.y,
                rotationQuarterTurns = placed.QuarterTurns
            };
            var storage = placed.GameObject.GetComponentInChildren<StorageBox>(true);
            if (storage != null)
                foreach (var item in storage.items)
                    if (item?.data != null && item.count > 0)
                        record.storedItems.Add(new PlaceableStoredItemSaveData
                        {
                            itemId = item.data.id, itemName = item.data.itemName,
                            count = item.count, quality = item.quality,
                            currentPrice = item.currentPrice
                        });
            state.placedBuildings.Add(record);
        }
        return state;
    }

    public bool RestoreSaveState(DemoSessionSaveData state, out string reason)
    {
        reason = string.Empty;
        if (state == null || state.version != 1 || state.placedBuildings == null ||
            state.placedBuildings.Count > 128 || placement == null || catalog == null ||
            placement.Placements.Any(p => p.GameObject != null && p.GameObject.GetComponent<DemoPlacedObject>() != null))
        { reason = "FirstDay placement state is unavailable or already populated."; return false; }
        if (state.placedBuildings.Any(p => p == null || string.IsNullOrWhiteSpace(p.instanceId)) ||
            state.placedBuildings.Select(p => p.instanceId).Distinct(StringComparer.Ordinal).Count() != state.placedBuildings.Count ||
            !Enum.IsDefined(typeof(DemoSpecialization), state.selectedRoot) || state.grantedStands < 0 || state.grantedStands > 3)
        { reason = "FirstDay placement metadata is invalid."; return false; }
        var ordered = state.placedBuildings.OrderBy(p =>
            p.buildingId == catalog.Find(DemoPlaceableKind.ShopBase)?.key ? 0 :
            p.buildingId == catalog.Find(DemoPlaceableKind.ResidentTent)?.key ? 1 : 2).ToArray();
        var placedIds = new List<string>();
        restoring = true;
        try
        {
            foreach (var record in ordered)
            {
                var entry = catalog.entries?.FirstOrDefault(e => e != null && e.key == record.buildingId);
                if (entry == null || !record.instanceId.StartsWith("demo-" + entry.key + "-", StringComparison.Ordinal) ||
                    record.rotationQuarterTurns < 0 || record.rotationQuarterTurns > 3)
                { reason = "Unsupported FirstDay placement identity: " + record.instanceId; return false; }
                placement.RegisterDefinition(record.instanceId, entry.Definition);
                var result = placement.TryPlace(record.instanceId,
                    new Vector2Int(record.anchorX, record.anchorZ), record.rotationQuarterTurns);
                if (!result.Succeeded)
                { reason = $"FirstDay placement {record.instanceId}: {result.Failure}."; return false; }
                placedIds.Add(record.instanceId);
                var placed = GetPlaced(record.instanceId);
                BindRestored(placed, entry);
                var storage = placed.GameObject.GetComponentInChildren<StorageBox>(true);
                if (record.storedItems != null && record.storedItems.Count > 0)
                {
                    if (storage == null) { reason = "Saved FirstDay storage is unavailable."; return false; }
                    foreach (var itemRecord in record.storedItems)
                    {
                        var item = itemRecord != null ? ItemRegistry.Instance?.Find(itemRecord.itemId, itemRecord.itemName) : null;
                        if (item == null || itemRecord.count <= 0 || !storage.AddInstance(new ItemInstance(item, itemRecord.count)
                            { quality = itemRecord.quality, currentPrice = itemRecord.currentPrice }))
                        { reason = "Saved FirstDay storage item is unavailable."; return false; }
                    }
                }
            }
            // P11: JsonUtility는 null 문자열을 ""로 저장한다. 상점을 놓기 전 저장(빈 shopId)도 이어할 수 있게 같은 값으로 본다.
            if ((shopId ?? string.Empty) != (state.shopId ?? string.Empty) || Established != state.established)
            { reason = "FirstDay shop or settlement progress did not match the save."; return false; }
            SelectedRoot = (DemoSpecialization)state.selectedRoot;
            LicensePoints = Established && SelectedRoot == DemoSpecialization.None ? 1 : 0;
            GrantedStands = state.grantedStands;
            NightReady = state.nightReady;
            sunsetStarted = state.sunsetStarted;
            _daylightHeld = NightReady || sunsetStarted;
            // JsonUtility는 null 클래스도 기본값 객체로 되살린다. rankKey가 있어야 마감 뒤 결말 Report다.
            bool finalReport = !string.IsNullOrEmpty(state.pioneerReport?.rankKey);
            PioneerReport = finalReport ? state.pioneerReport : null;
            var observer = GetComponent<DemoPioneerReport>();
            if (observer != null) observer.RestoreProgress(state.pioneerReport, finalReport);
            var loop = DayNightShopLoopController.Instance;
            if (finalReport && loop != null) loop.RestoreOpeningSessionCompleted();
            return true;
        }
        finally
        {
            restoring = false;
            if (!string.IsNullOrEmpty(reason))
                for (int i = placedIds.Count - 1; i >= 0; i--) placement.TryRemove(placedIds[i]);
        }
    }

    DemoPioneerReportData CaptureReportProgress()
    {
        var observer = GetComponent<DemoPioneerReport>();
        return observer != null ? observer.CaptureProgress() : null;
    }

    // 텐트는 저장 순서(InstanceId)대로 복원되고 아직 집 없는 동행자가 차례로 받는다.
    // 동행자 순서를 소유 텐트 순서로 기록해 Continue 뒤에도 같은 동행자가 같은 텐트를 쓴다.
    string[] CompanionIdsInTentOrder()
    {
        var ordered = residents.Where(r => !string.IsNullOrEmpty(r.TentId))
            .OrderBy(r => r.TentId, StringComparer.Ordinal).Select(r => r.CompanionId)
            .Concat(residents.Where(r => string.IsNullOrEmpty(r.TentId)).Select(r => r.CompanionId)).ToList();
        foreach (var id in DemoRouteController.SelectedCompanionIds)
            if (!ordered.Contains(id)) ordered.Add(id);
        return ordered.ToArray();
    }

    // SaveManager가 가방·시계·플레이어 위치까지 복원한 뒤 호출한다. 저장된 사실에서 파생되는 표시만 맞춘다.
    public void CompleteRestore(SaveData data)
    {
        var supply = FirstDayWorldPresentation.Instance?.Supply;
        if (supply != null && FirstDaySupplyBox.SaveShowsReceived(data)) supply.RestoreReceived();
        var interior = OperatingShop != null ? OperatingShop.GetComponentInParent<DemoShopInterior>(true) : null;
        if (interior != null && inventory != null) interior.RestoreForPlayer(inventory.transform.position);
        // P11: 일몰 중 저장을 이어하면 남은 일몰도 새 게임과 같은 속도로 흐른다(복원 중 적용된 항구 시계 120s/시 대신).
        var clock = GameClock.Instance;
        if (clock != null && sunsetStarted && !NightReady) { clock.secondsPerGameHour = SunsetSecondsPerGameHour; clock.enabled = true; }
    }

    WorldPlacedBuildingRuntime GetPlaced(string id)
    { placement.TryGetPlacement(id, out var placed); return placed; }

    void BindRestored(WorldPlacedBuildingRuntime placed, DemoPlaceableEntry entry)
    {
        var marker = placed.GameObject.GetComponent<DemoPlacedObject>() ?? placed.GameObject.AddComponent<DemoPlacedObject>();
        marker.Bind(placed.InstanceId, entry, placement);
        OnPlaced(placed, entry);
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
            if (GrantedStands < 3) FirstDayWorldPresentation.Toast("가방 공간을 비운 뒤 P 휴대폰 상점 앱에서 남은 가판대를 받으세요.");
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

    // 동행이 실제로 걸어갈 수 있는 작업 지점. 바위까지 NavMesh 경로가 이어지면 그 바위,
    // 높은 단 위 바위처럼 끊기면(PathPartial) 바위에 가장 가까이 갈 수 있는 지점에 채굴 지점을 둔다.
    Transform CompanionWorkSpot(MiningSpot[] mines, Vector3 from, string companionId)
    {
        var ordered = mines.OrderBy(m => Vector3.Distance(m.transform.position, from)).ToArray();
        if (ordered.Length == 0) return null;
        if (!UnityEngine.AI.NavMesh.SamplePosition(from, out var start, 4f, UnityEngine.AI.NavMesh.AllAreas)) return ordered[0].transform;
        var path = new UnityEngine.AI.NavMeshPath();
        MiningSpot bestRock = null; Vector3 bestPoint = Vector3.zero; float bestScore = float.MaxValue; bool bestComplete = false;
        foreach (var mine in ordered.Take(16))
        {
            if (!UnityEngine.AI.NavMesh.SamplePosition(mine.transform.position, out var end, 10f, UnityEngine.AI.NavMesh.AllAreas) ||
                !UnityEngine.AI.NavMesh.CalculatePath(start.position, end.position, UnityEngine.AI.NavMesh.AllAreas, path) ||
                path.corners.Length == 0) continue;
            float length = 0f;
            for (int i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            bool complete = path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
            Vector3 reach = path.corners[path.corners.Length - 1];
            float gap = Vector3.Distance(reach, mine.transform.position);
            float score = (complete ? 0f : 1000f) + gap * 3f + length * .1f;
            if (score < bestScore) { bestScore = score; bestRock = mine; bestPoint = reach; bestComplete = complete; }
        }
        if (bestRock == null) return ordered[0].transform;
        if (bestComplete) return bestRock.transform;
        var spot = new GameObject("CompanionQuarry_" + companionId).transform;
        spot.SetParent(transform, false);
        spot.position = bestPoint;
        Vector3 look = Vector3.ProjectOnPlane(bestRock.transform.position - bestPoint, Vector3.up);
        if (look.sqrMagnitude > .01f) spot.rotation = Quaternion.LookRotation(look.normalized);
        return spot;
    }

    public bool HasSynergy(DemoSpecialization root) => residents.Any(r =>
        root == DemoSpecialization.Forestry && r.CompanionId.StartsWith("Lumberjack", StringComparison.Ordinal) ||
        root == DemoSpecialization.Mining && r.CompanionId.StartsWith("Miner", StringComparison.Ordinal) ||
        root == DemoSpecialization.Fisheries && r.CompanionId.StartsWith("Fisher", StringComparison.Ordinal) ||
        root == DemoSpecialization.Agriculture && r.CompanionId.StartsWith("Farmer", StringComparison.Ordinal));

    public static string NextPreview(DemoSpecialization root) => root switch
    {
        DemoSpecialization.Forestry => "다음 단계: 좋은 도끼 → 목공 (미리보기)",
        DemoSpecialization.Mining => "다음 단계: 개선 곡괭이 → 금속 가공 (미리보기)",
        DemoSpecialization.Fisheries => "다음 단계: 좋은 낚싯대 → 수산 가공 (미리보기)",
        DemoSpecialization.Agriculture => "다음 단계: 괭이 · 밭 · 씨앗 (미리보기)",
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
        // Canon v2 §20: CLOSE 직후 결말 카드(점수·Rank·댓글). 닫으면 휴대폰 상점 앱에 기록이 남는다.
        if (SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen) SmartphoneUI.instance.Close();
        input?.Cancel();
        IsPanelOpen = true;
        PioneerReportCardUI.Show(report, () =>
        {
            IsPanelOpen = false;
            PlayerInputHandler.RestoreGameplayCursor();
            SmartphoneUI.instance?.OpenShopApp();
        });
    }

    public void CloseManagement()
    {
        if (!IsPanelOpen) return;
        if (PioneerReportCardUI.Instance != null && PioneerReportCardUI.Instance.IsOpen) { PioneerReportCardUI.Instance.Hide(); return; }
        IsPanelOpen = false;
        PlayerInputHandler.RestoreGameplayCursor();
    }

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
                    clock.secondsPerGameHour = SunsetSecondsPerGameHour; clock.enabled = true;
                    FirstDayWorldPresentation.Toast("해가 지고 있어요. 첫 영업을 준비하세요.");
                }
                UpdateBusinessHours(clock);
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

    // D2 — Canon v2 §16: 첫 영업은 20:00 OPEN 뒤 시계가 흐르고 22:00에 자동 마감한다(21:30 예고).
    // 약 2분의 실제 영업 동안 관광객이 여러 명 겹쳐 방문해 밤 장사의 밀도를 만든다.
    public const float OpeningCloseHour = 22f;
    public const int OpeningStandCapacity = 5;
    const float OpeningWarnHour = 21.5f, OpeningSecondsPerGameHour = 60f, SunsetSecondsPerGameHour = 25f;
    bool _businessClockRunning, _closeWarned;

    // Canon v2 §12 "고객 통로 확보": 손님이 걸어서 상점 바닥과 가판대까지 들어올 수 있어야 한다.
    // 격자 규칙은 1단 높이차를 통행 가능으로 보지만 실제 지형은 1m 절벽이라, 입구만 아래 단에 걸치면
    // 문턱이 절벽이 되어 손님이 0명이 된다(D2 실측). 관광객은 상점 주변 12~16m에서 들어오므로 그 공간도 확인한다.
    public static bool IsCustomerReachable(WorldGridService grid, WorldBuildingPlacementResult preview, DemoPlaceableKind kind)
    {
        if (grid == null || FirstDayWorldPresentation.Instance == null) return true;
        // Canon v2 §12 "Door 앞 No-Placement": 상점 입구 바깥 1칸 안에는 텐트·작업대·가판대 모두 두지 않는다.
        if (kind != DemoPlaceableKind.ShopBase && Instance != null && Instance.TryGetShopEntrance(out var shopEntrance) &&
            preview.Footprint.Any(c => Mathf.Abs(c.x - shopEntrance.x) <= 1 && Mathf.Abs(c.y - shopEntrance.y) <= 1))
            return false;
        if (kind != DemoPlaceableKind.ShopBase && kind != DemoPlaceableKind.DisplayStand) return true;
        if (!TrySampleCell(grid, preview.Anchor, out var floor)) return false;
        if (kind == DemoPlaceableKind.DisplayStand)
        {
            // Canon v2 §12 "Door 앞 No-Placement": 가판대 충돌체는 NavMesh 구멍이 되므로
            // 문 안쪽 칸과 그 좌우에 두면 입구가 막혀 손님이 들어오지 못한다(D2 실측).
            if (Instance != null && Instance.TryGetShopDoorInside(out var doorInside) &&
                preview.Footprint.Any(c => Mathf.Abs(c.x - doorInside.x) <= 1 && Mathf.Abs(c.y - doorInside.y) <= 1))
                return false;
            var shop = Instance != null ? Instance.OperatingShop : null;
            if (shop == null || !UnityEngine.AI.NavMesh.SamplePosition(shop.transform.position, out var shopFloor, 2f, UnityEngine.AI.NavMesh.AllAreas)) return true;
            return Connected(shopFloor.position, floor);
        }
        if (!TrySampleCell(grid, preview.Entrance, out var door) || !Connected(door, floor)) return false;
        // 미리보기 시점엔 벽이 없어 건물 안을 가로지르는 길도 통과로 잡힌다. 설치 후엔 벽이 막으므로
        // 건물 영역(발자국 외곽 사각형)을 지나지 않고 입구에 닿는 길만 인정한다.
        if (!TryFootprintRect(grid, preview.Footprint, out var building)) return false;
        for (int ring = 0; ring < 2; ring++)
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f * Mathf.Deg2Rad;
                var probe = door + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (12f + ring * 4f);
                if (!UnityEngine.AI.NavMesh.SamplePosition(probe, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) continue;
                var path = new UnityEngine.AI.NavMeshPath();
                if (UnityEngine.AI.NavMesh.CalculatePath(hit.position, door, UnityEngine.AI.NavMesh.AllAreas, path) &&
                    path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete && !PathEnters(path, building))
                    return true;
            }
        return false;
    }

    public bool TryGetShopEntrance(out Vector2Int entrance)
    {
        entrance = default;
        if (placement == null || string.IsNullOrEmpty(shopId) || !placement.TryGetPlacement(shopId, out var shop)) return false;
        entrance = shop.Entrance;
        return true;
    }

    static bool TryFootprintRect(WorldGridService grid, IReadOnlyList<Vector2Int> footprint, out Rect rect)
    {
        rect = default;
        if (footprint == null || footprint.Count == 0) return false;
        Vector2Int min = footprint[0], max = footprint[0];
        foreach (var c in footprint) { min = Vector2Int.Min(min, c); max = Vector2Int.Max(max, c); }
        if (!grid.CellToWorld(min, out var a) || !grid.CellToWorld(max, out var b)) return false;
        float half = grid.Definition.CellSize * .5f;
        rect = Rect.MinMaxRect(Mathf.Min(a.x, b.x) - half, Mathf.Min(a.z, b.z) - half, Mathf.Max(a.x, b.x) + half, Mathf.Max(a.z, b.z) + half);
        return true;
    }

    static bool PathEnters(UnityEngine.AI.NavMeshPath path, Rect building)
    {
        var corners = path.corners;
        for (int i = 1; i < corners.Length; i++)
        {
            float length = Vector3.Distance(corners[i - 1], corners[i]);
            for (float t = 0f; t <= length; t += .5f)
            {
                var p = Vector3.Lerp(corners[i - 1], corners[i], length > 0f ? t / length : 0f);
                if (building.Contains(new Vector2(p.x, p.z))) return true;
            }
        }
        return false;
    }

    // 상점 입구 바로 안쪽 칸. 상점 발자국은 벽(ㄷ자)만 포함하므로 입구에서 발자국 중심 방향으로 한 칸 들어간다.
    public bool TryGetShopDoorInside(out Vector2Int doorInside)
    {
        doorInside = default;
        if (placement == null || string.IsNullOrEmpty(shopId) || !placement.TryGetPlacement(shopId, out var shop) ||
            shop.Footprint == null || shop.Footprint.Count == 0)
            return false;
        Vector2 center = Vector2.zero;
        foreach (var cell in shop.Footprint) center += (Vector2)cell;
        center /= shop.Footprint.Count;
        Vector2 inward = center - (Vector2)shop.Entrance;
        doorInside = shop.Entrance + (Mathf.Abs(inward.x) >= Mathf.Abs(inward.y)
            ? new Vector2Int((int)Mathf.Sign(inward.x), 0) : new Vector2Int(0, (int)Mathf.Sign(inward.y)));
        return true;
    }

    static bool TrySampleCell(WorldGridService grid, Vector2Int cell, out Vector3 position)
    {
        position = default;
        if (!grid.CellToWorld(cell, out var world)) return false;
        // 반경을 크게 잡으면 절벽 아래 지면 NavMesh에 붙어 거짓 통과가 난다. 같은 높이의 NavMesh만 인정한다.
        if (!UnityEngine.AI.NavMesh.SamplePosition(world, out var hit, 1.2f, UnityEngine.AI.NavMesh.AllAreas) ||
            Mathf.Abs(hit.position.y - world.y) > .35f) return false;
        position = hit.position;
        return true;
    }

    static bool Connected(Vector3 from, Vector3 to)
    {
        var path = new UnityEngine.AI.NavMeshPath();
        return UnityEngine.AI.NavMesh.CalculatePath(from, to, UnityEngine.AI.NavMesh.AllAreas, path) &&
               path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
    }

    static void ConfigureOpeningCustomers(CustomerArrivalController arrivals)
    {
        if (arrivals == null) return;
        arrivals.maxTouristsPerOpening = 7;
        arrivals.maxConcurrentTourists = 3;
        arrivals.touristInviteInterval = 7f;
        arrivals.firstTouristDelay = 2f;
        // 관광객은 기념품처럼 사치품에 관대하고 가격에 덜 민감하다. 주민은 기존 프로필 그대로.
        arrivals.touristPriceSensitivityScale = .75f;
        arrivals.touristLuxuryScale = 1.35f;
        arrivals.touristUtilityScale = .8f;
    }

    void UpdateBusinessHours(GameClock clock)
    {
        var loop = DayNightShopLoopController.Instance;
        bool operating = loop != null && NightReady && !loop.OpeningSessionCompleted && loop.IsShopOpenForCustomers;
        if (!operating)
        {
            if (_businessClockRunning) { _businessClockRunning = false; clock.enabled = false; }
            return;
        }
        if (!_businessClockRunning)
        {
            _businessClockRunning = true; _closeWarned = false;
            clock.secondsPerGameHour = OpeningSecondsPerGameHour; clock.enabled = true;
            FirstDayWorldPresentation.Toast("영업 시작 · 오늘은 22:00에 문을 닫아요.");
        }
        if (!_closeWarned && clock.CurrentHour >= OpeningWarnHour)
        {
            _closeWarned = true;
            FirstDayWorldPresentation.Toast("마감 30분 전 · 마지막 손님을 맞이하세요.");
        }
        if (clock.CurrentHour >= OpeningCloseHour && loop.TryCloseOpeningShop())
        {
            _businessClockRunning = false; clock.enabled = false;
        }
    }

    static string RootDisplay(DemoSpecialization root) => root switch
    {
        DemoSpecialization.Forestry => "임업", DemoSpecialization.Mining => "광업",
        DemoSpecialization.Fisheries => "수산업", DemoSpecialization.Agriculture => "농업", _ => "미선택"
    };
}
