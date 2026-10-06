using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 역할:
// - 게임 상태의 직렬화/역직렬화를 담당.
// - 저장 백엔드는 ISaveRepository 로 추상화되어 있어, 나중에 UGS Cloud Save 구현체로
//   필드 하나만 교체하면 클라우드 저장으로 이관된다.
// - 무엇을 저장할지는 FindGameObjectsWithTag 같은 씬 스캔 대신 BuildingRegistry 가 보유한
//   명시적 목록을 사용한다.
public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    // 도감 — 로드 시 prefabName 으로 프리팹을 조회하는 데 사용한다.
    public List<BuildingData> allBuildingTypes;

    // 저장소 백엔드. MVP 에서는 로컬 JSON 고정.
    // 멀티 전환 시 이 필드 하나만 UGSCloudSaveRepository 로 교체된다.
    private ISaveRepository _repository;

    private string SaveKey => gameObject.scene.name == DepartureTutorialController.SceneName ? "departure_settlement" : "savegame-v17";
    private const string LegacySaveKey = "savegame";

    // 현재 스키마 버전. 새 필드 추가 시 올리고 MigrateSaveData() 에 마이그레이션 추가.
    public const int CurrentSaveVersion = 17;
    public string LastLoadError { get; private set; }

    public sealed class ContinueInspection
    {
        public bool HasSave;
        public string Issue;
        public string[] CompanionIds;
        // 이전 형식(v16 이하) 저장: 이어하기는 막고 원본 파일은 읽기만 한다. 새 진행은 별도 키에 저장된다.
        public bool LegacyFormat;
        public bool CanContinue => HasSave && string.IsNullOrEmpty(Issue);
    }

    // FirstDay 영업(20:00 OPEN → 22:00 자동 마감)은 고객·영업 시계·매출 집계가 세션 상태라 중간 저장을 받지 않는다.
    public const string BusinessSaveBlockedMessage = "영업 중에는 저장할 수 없어요. 22:00 마감 뒤 저장해 주세요.";

    static string LegacyFormatIssue(int version) =>
        $"이전 버전(v{version}) 저장은 현재 섬의 배치 형식과 달라 이어할 수 없습니다.";

    bool _loadInProgress;
    PlayerInputHandler _input;

    void Awake()
    {
        instance = this;
#if UNITY_EDITOR
        string validationRoot = UnityEditor.SessionState.GetString(ValidationRootSessionKey, string.Empty);
        _repository = string.IsNullOrEmpty(validationRoot)
            ? new LocalJsonSaveRepository() : new LocalJsonSaveRepository(validationRoot);
#else
        string isolatedRoot = CommandLineSaveRoot();
        _repository = string.IsNullOrEmpty(isolatedRoot)
            ? new LocalJsonSaveRepository() : new LocalJsonSaveRepository(isolatedRoot);
#endif
    }

    // 배포 후보 검수용: `-pa-save-root <폴더>`로 실행하면 사용자 save 대신 그 폴더에 저장·이어하기 한다.
    // 인자가 없으면 기존 persistentDataPath 그대로(스키마·저장 권위 불변).
    static string CommandLineSaveRoot()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
        {
            if (args[i] != "-pa-save-root") continue;
            string root = args[i + 1];
            System.IO.Directory.CreateDirectory(root);
            Debug.Log($"💾 [SaveManager] 격리 저장 경로 사용: {root}");
            return root;
        }
        return null;
    }

#if UNITY_EDITOR
    public const string ValidationRootSessionKey = "PA.Continue.ValidationSaveRoot";
    // WORLD-009 validator must exercise the real SaveManager without touching a player's
    // persistent save. Kept editor-only so production save authority and schema stay unchanged.
    internal void SetRepositoryForValidation(ISaveRepository repository)
    {
        _repository = repository ?? new LocalJsonSaveRepository();
    }
#endif

    void Start()
    {
        _input = PlayerInputHandler.Instance;
        if (_input != null)
        {
            _input.OnSave += HandleSaveInput;
            _input.OnLoad += HandleLoadInput;
        }
    }

    void OnDestroy()
    {
        if (_input != null)
        {
            _input.OnSave -= HandleSaveInput;
            _input.OnLoad -= HandleLoadInput;
        }
        if (instance == this) instance = null;
    }

    void HandleSaveInput() => _ = SaveGameAsync();
    void HandleLoadInput() => _ = LoadGameAsync();

    public async System.Threading.Tasks.Task SaveGameAsync()
    {
        var openingDemo = DemoSettlementController.Instance;
        if (openingDemo != null && gameObject.scene.name == DemoRouteController.WorldScene &&
            openingDemo.BusinessInProgress)
            throw new System.InvalidOperationException(BusinessSaveBlockedMessage);

        var settlement = FindFirstObjectByType<FirstIslandSettlementController>();
        if (gameObject.scene.name == DepartureTutorialController.SceneName && (settlement == null || !settlement.IsReady))
        {
            Debug.LogWarning("[SaveManager] Settlement is not ready; existing save preserved.");
            return;
        }
        SaveData data = new SaveData();
        data.firstSettlement = settlement != null && settlement.IsReady ? settlement.CaptureState() : null;
        var production = FindFirstObjectByType<FirstProductionController>();
        data.firstProduction = production != null ? production.CaptureState() : new FirstProductionSaveData();
        data.m85RecoveryRevision = 1;

        // 1. 플레이어 정보
        data.money = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;
        data.cumulativeRevenue = EconomyService.Instance != null ? EconomyService.Instance.CumulativeRevenue : 0;

        // 2. 티어 정보
        if (TierService.Instance != null)
        {
            data.currentTier = TierService.Instance.CurrentTier;
            data.reputation = TierService.Instance.Reputation;
        }

        // 3. 인게임 시간
        if (GameClock.Instance != null)
        {
            data.gameHour = GameClock.Instance.CurrentHour;
            data.gameDay  = GameClock.Instance.CurrentDay;
        }

        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            data.playerPosition = playerGo.transform.position;
            data.playerRotation = playerGo.transform.rotation;
            data.hasPlayerRotation = true;
        }

        var firstDay = FindFirstObjectByType<PlayableDayScenarioController>();
        if (firstDay != null)
        {
            data.playerName = firstDay.PlayerName;
            data.selectedMapId = firstDay.SelectedMapId;
            data.firstDayPrototypeStage = firstDay.CurrentStageIndex;
        }

        var worldAlpha = FindFirstObjectByType<WorldAlphaPlayableController>();
        if (worldAlpha != null)
            worldAlpha.WriteSaveFields(data);
        var demo = DemoSettlementController.Instance;
        if (demo != null && gameObject.scene.name == DemoRouteController.WorldScene)
            data.demoSession = demo.CaptureSaveState();

        var longPlay = FindFirstObjectByType<LongPlayProgressionController>();
        if (longPlay != null)
            longPlay.WriteSaveFields(data);

        // CDN/IL — 당일 채집(낮 재고 준비) 완료 상태 직렬화.
        var dayLoop = DayNightShopLoopController.Instance ?? FindFirstObjectByType<DayNightShopLoopController>();
        if (dayLoop != null)
            dayLoop.WriteSaveFields(data);

        // Task 057 — 마을 변화(대기/활성) 상태 직렬화. v9.
        var villageCulture = VillageCultureVisualController.Instance
            ?? FindFirstObjectByType<VillageCultureVisualController>();
        if (villageCulture != null)
            villageCulture.WriteSaveFields(data);

        if (SalesLogManager.Instance != null)
            SalesLogManager.Instance.WriteSaveFields(data);

        FarmPlotInteraction.WriteAllSaveFields(data);

        // 3. 건물 정보 — 레지스트리가 가진 명시 목록을 직렬화한다.
        if (BuildingRegistry.Instance != null)
        {
            foreach (var b in BuildingRegistry.Instance.Buildings)
            {
                if (b.gameObject == null) continue;
                data.buildings.Add(new BuildingSaveData(
                    b.prefabName,
                    b.gameObject.transform.position,
                    b.gameObject.transform.rotation));
            }
        }

        // 4. 인벤토리 직렬화
        if (Inventory.instance != null)
        {
            data.inventorySlots = SerializeSlots(Inventory.instance.slots);

            if (Inventory.instance.hotbar != null)
            {
                data.hotbarSlots = SerializeSlots(Inventory.instance.hotbar.slots);
                data.selectedHotbarIndex = Mathf.Clamp(Inventory.instance.selectedHotbarIndex,
                    0, Mathf.Max(0, Inventory.instance.hotbar.slots.Count - 1));
            }
        }

        // 4-a. ShopSlot 진열 상태 — v5
        data.shopSlots = SerializeShopSlots();

        // 4-b. Zone-aware shop furniture placement — v10.
        var customization = ShopCustomizationController.Instance
            ?? FindFirstObjectByType<ShopCustomizationController>();
        if (customization != null && customization.IsReady)
            customization.WriteSaveFields(data);
        var outdoorPlacement = OutdoorPlacementController.Instance
            ?? FindFirstObjectByType<OutdoorPlacementController>();
        if (outdoorPlacement != null && outdoorPlacement.IsReady)
            outdoorPlacement.WriteSaveFields(data);
        var worldAdapter = WorldGameplayAdapterService.Instance
            ?? FindFirstObjectByType<WorldGameplayAdapterService>();
        if (worldAdapter != null && worldAdapter.IsReady)
            worldAdapter.WriteSaveFields(data);

        // WORLD-007 — procedural world state is additive. Legacy scenes emit an
        // explicit LegacyFixed marker and retain their existing absolute records.
        var worldPersistence = WorldPersistenceService.Instance
            ?? FindFirstObjectByType<WorldPersistenceService>();
        data.worldState = worldPersistence != null && worldPersistence.IsProceduralActive
            ? worldPersistence.CaptureState(data.playerPosition, data.placeables)
            : WorldPersistenceMigration.CreateLegacyFixed();

        // 5. 감사 시스템
        data.lastAuditDay = AuditService.Instance != null ? AuditService.Instance.LastAuditDay : 0;

        // 6. 친밀도 — v4
        if (FriendshipService.Instance != null)
        {
            data.friendshipData = SerializeFriendship();
        }

        // 7. 채용 NPC — v4 (id, transform, FSM state)
        data.hiredNpcs = new List<HiredNpcRecord>();
        if (HiringService.Instance != null)
        {
            foreach (var runtime in HiringService.Instance.GetHiredRuntimeRecords())
            {
                var record = SerializeHiredNpc(runtime);
                if (record != null) data.hiredNpcs.Add(record);
            }
        }

        // 버전 스탬프
        data.version = CurrentSaveVersion;

        string json = JsonUtility.ToJson(data, true);
        await _repository.SaveAsync(SaveKey, json);
        Debug.Log($"💾 저장 완료 (건물 {data.buildings.Count}개, 인벤토리 {data.inventorySlots.Count}칸, 핫바 {data.hotbarSlots.Count}칸, 진열대 {data.shopSlots.Count}칸)");
    }

    public async System.Threading.Tasks.Task LoadGameAsync()
    {
        await TryLoadGameAsync();
    }

    public async System.Threading.Tasks.Task<bool> TryLoadGameAsync()
    {
        if (_loadInProgress)
        {
            LastLoadError = "이미 다른 불러오기가 진행 중입니다.";
            Debug.LogWarning("[SaveManager] " + LastLoadError);
            return false;
        }

        _loadInProgress = true;
        LastLoadError = null;
        try
        {
            return await LoadGameInternalAsync();
        }
        finally
        {
            _loadInProgress = false;
        }
    }

    bool RejectLoad(string reason)
    {
        LastLoadError = reason;
        Debug.LogWarning("[SaveManager] " + reason);
        return false;
    }

    async System.Threading.Tasks.Task<string> ReadCurrentSaveJsonAsync()
    {
        if (gameObject.scene.name == DepartureTutorialController.SceneName)
            return await _repository.LoadAsync(SaveKey);
        if (await _repository.ExistsAsync(SaveKey))
            return await _repository.LoadAsync(SaveKey);
        return await _repository.LoadAsync(LegacySaveKey);
    }

    public async System.Threading.Tasks.Task<ContinueInspection> InspectContinueAsync()
    {
        var result = new ContinueInspection();
        string json = await ReadCurrentSaveJsonAsync();
        result.HasSave = !string.IsNullOrEmpty(json);
        if (!result.HasSave) return result;
        SaveData data;
        try { data = JsonUtility.FromJson<SaveData>(json); }
        catch (System.Exception) { result.Issue = "저장 JSON을 읽을 수 없습니다."; return result; }
        if (data == null) { result.Issue = "저장 JSON을 읽을 수 없습니다."; return result; }
        if (data.version > CurrentSaveVersion)
        { result.Issue = "더 새로운 버전에서 만든 저장이라 열 수 없습니다."; return result; }
        // 2026-09-30 사용자 결정: v16 이하는 재배치/변환하지 않는다. 사유만 보이고 원본은 그대로 둔다.
        if (data.version < CurrentSaveVersion)
        { result.LegacyFormat = true; result.Issue = LegacyFormatIssue(data.version); return result; }
        if (data.worldState == null || data.worldState.worldMode != WorldPersistenceMigration.ProceduralMode)
        { result.Issue = "현재 FirstDay 섬에서 지원하지 않는 저장 형식입니다."; return result; }
        if (data.firstSettlement != null && !IsEmptyUnstartedSettlement(data.firstSettlement))
        { result.Issue = "출항 정착 저장은 현재 섬 이어하기와 연결되지 않습니다."; return result; }
        if (!FirstProductionController.IsValidSave(data.firstProduction, data.firstSettlement))
        { result.Issue = "첫 생산 진행 기록이 유효하지 않습니다."; return result; }
        if (data.demoSession == null || data.demoSession.version != 1)
        { result.Issue = "FirstDay 배치 기록이 없습니다."; return result; }
        result.CompanionIds = data.demoSession.companionIds ?? System.Array.Empty<string>();
        return result;
    }

    static bool IsEmptyUnstartedSettlement(FirstSettlementSaveData state) =>
        state != null && state.version == 1 && !state.settlementCompleted &&
        (state.companionIds == null || state.companionIds.Length == 0) &&
        (state.buildings == null || state.buildings.Count == 0);

    async System.Threading.Tasks.Task<bool> LoadGameInternalAsync()
    {
        string json = await ReadCurrentSaveJsonAsync();
        if (string.IsNullOrEmpty(json))
            return RejectLoad("저장된 파일이 없습니다.");

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (System.Exception ex)
        {
            return RejectLoad($"손상된 JSON을 적용하지 않았습니다: {ex.Message}");
        }
        if (data == null || data.version > CurrentSaveVersion)
        {
            return RejectLoad("지원하지 않거나 비어 있는 저장 데이터입니다.");
        }
        bool firstDayWorld = gameObject.scene.name == DemoRouteController.WorldScene &&
            FindFirstObjectByType<DemoRouteController>() != null;
        if (firstDayWorld)
        {
            // 어떤 런타임 상태도 바꾸기 전에 거절한다(부분 적용 방지). v16 이하는 변환하지 않는다.
            if (data.version < CurrentSaveVersion)
                return RejectLoad(LegacyFormatIssue(data.version));
            var freshDemo = DemoSettlementController.Instance;
            if (freshDemo == null)
                return RejectLoad("FirstDay 섬이 준비되지 않았습니다.");
            if (!freshDemo.CanRestoreSession(out string freshReason))
                return RejectLoad(freshReason);
        }

        // 버전 마이그레이션
        if (data.version < CurrentSaveVersion)
        {
            data = MigrateSaveData(data);
            Debug.Log($"💾 세이브 마이그레이션 완료: v{data.version}");
        }

        NormalizeSaveData(data);
        if (!FirstProductionController.IsValidSave(data.firstProduction, data.firstSettlement))
        {
            return RejectLoad("첫 생산 진행 기록이 유효하지 않습니다.");
        }
        var settlement = FindFirstObjectByType<FirstIslandSettlementController>();
        if (gameObject.scene.name == DepartureTutorialController.SceneName)
        {
            if (settlement == null || !await settlement.PrepareRestoreAsync(data.firstSettlement))
            {
                return RejectLoad("출항 정착 저장이 유효하지 않습니다.");
            }
        }
        else if (data.firstSettlement != null &&
                 !(data.worldState?.worldMode == WorldPersistenceMigration.ProceduralMode &&
                   IsEmptyUnstartedSettlement(data.firstSettlement)))
        {
            return RejectLoad("출항 정착 저장은 출항 씬에서만 복원할 수 있습니다.");
        }
        PrepareRuntimeForStateRestore();

        Vector3 restoredPlayerPosition = data.playerPosition;
        if (data.worldState != null &&
            data.worldState.worldMode == WorldPersistenceMigration.ProceduralMode)
        {
            var worldPersistence = WorldPersistenceService.Instance
                ?? FindFirstObjectByType<WorldPersistenceService>();
            string worldReason = "WorldPersistenceService unavailable.";
            List<PlaceableSaveData> restoredFurniture = null;
            if (worldPersistence == null ||
                !worldPersistence.TryRestore(data.worldState, out restoredPlayerPosition,
                    out restoredFurniture, out worldReason))
            {
                return RejectLoad("절차 월드 저장을 적용하지 않았습니다: " + worldReason);
            }
            data.placeables = MergeProceduralFurnitureWithPlaceables(
                data.placeables, restoredFurniture);
            if (firstDayWorld)
            {
                var grid = FindFirstObjectByType<WorldGridService>();
                if (grid == null || !grid.WorldToCell(data.playerPosition, out Vector2Int savedCell) ||
                    !grid.TryGetCell(savedCell, out WorldCellData savedCellData) || !savedCellData.IsWalkable ||
                    savedCell.x != data.worldState.safePlayerCellX ||
                    savedCell.y != data.worldState.safePlayerCellZ)
                    return RejectLoad("저장된 플레이어 위치가 안전 위치 기록과 일치하지 않습니다.");
                restoredPlayerPosition = data.playerPosition;
            }
        }

        // FirstDay 순서: 월드 → 기존 placement로 상점/텐트/가구 → 가판대 슬롯 대상 확인 → 아래의 슬롯·진행 바인딩.
        var demo = DemoSettlementController.Instance;
        if (demo != null && gameObject.scene.name == DemoRouteController.WorldScene)
        {
            if (!demo.RestoreSaveState(data.demoSession, out string demoReason))
                return RejectLoad("FirstDay 배치 복원 실패: " + demoReason);
            if (!ValidateShopSlotTargets(data.shopSlots, out string slotReason))
                return RejectLoad("진열 슬롯 복원 실패: " + slotReason);
        }

        // 1. 플레이어 복구 — 돈은 EconomyService 의 단일 경로로만 세팅한다.
        if (EconomyService.Instance != null)
        {
            EconomyService.Instance.ForceSet(data.money, "SaveManager.LoadGame");
            EconomyService.Instance.ForceSetCumulativeRevenue(data.cumulativeRevenue, "SaveManager.LoadGame");
        }

        // 2. 티어 복구
        if (TierService.Instance != null)
        {
            TierService.Instance.ForceSetTier(data.currentTier, data.reputation, "SaveManager.LoadGame");
        }

        // 3. 인게임 시간 복구
        // ⚠ Week11 검증: 마이그레이션 분기 바깥에서 무조건 호출되어야 v4 최신 세이브도 시간/일차가
        //    복구된다. 만약 이 블록을 if (data.version < CurrentSaveVersion) 안으로 옮기면
        //    저장 직후 로드한 사용자의 시간이 1일 7시로 리셋되는 회귀가 발생한다. 절대 옮기지 말 것.
        if (GameClock.Instance != null)
        {
            GameClock.Instance.ForceSet(data.gameHour, data.gameDay, "SaveManager.LoadGame");
        }

        var firstDay = FindFirstObjectByType<PlayableDayScenarioController>();
        if (firstDay != null)
            firstDay.RestoreSavedSession(data.playerName, data.selectedMapId, data.firstDayPrototypeStage);

        // CDN/IL — 당일 채집 완료 상태 복원(저장된 날과 현재 날이 같을 때만 유지).
        if (SalesLogManager.Instance != null)
            SalesLogManager.Instance.RestoreSavedState(
                data.salesLogRecords, data.salesDecisionDays);

        var dayLoop = DayNightShopLoopController.Instance ?? FindFirstObjectByType<DayNightShopLoopController>();
        if (dayLoop != null)
            dayLoop.RestoreSavedState(data.dayPrepCollectedDay,
                data.dayPrepCollectedActivities, data.shopOpenedDay);

        // Task 057 — 마을 변화(대기/활성) 상태 복원. 핵심 차별점의 다음날 지속성.
        var villageCulture = VillageCultureVisualController.Instance
            ?? FindFirstObjectByType<VillageCultureVisualController>();
        if (villageCulture != null)
            villageCulture.RestoreSavedState(
                data.villageCultureHasPendingChange,
                data.villageCulturePendingSaleDay,
                data.villageCulturePendingCategory,
                data.villageCultureHasActiveChange,
                data.villageCultureActiveCategory,
                data.villageCultureHintShown,
                data.villageCulturePendingItemName,
                data.villageCulturePendingBuyerName,
                data.villageCultureActiveSaleDay,
                data.villageCultureActiveResponseDay,
                data.villageCultureActiveItemName,
                data.villageCultureActiveBuyerName);

        ApplyPlayerPose(restoredPlayerPosition, data);

        // 3-a. 감사 시스템 복구
        if (AuditService.Instance != null)
            AuditService.Instance.ForceSetLastAuditDay(data.lastAuditDay);

        // 3-b. 친밀도 복구 — v4
        if (FriendshipService.Instance != null && data.friendshipData != null)
        {
            FriendshipService.Instance.Clear();
            foreach (var fr in data.friendshipData)
            {
                FriendshipService.Instance.ForceSetPoints(fr.friendshipId, fr.points);
                FriendshipService.Instance.ForceSetLastDialogueDay(fr.friendshipId, fr.lastDialogueDay);
            }
        }

        // Hired NPCs are restored after buildings and inventory so FSM targets can be resolved.

        // 3-b. 기존 건물 제거 — 레지스트리가 보유한 목록만 정확히 파괴한다.
        if (BuildingRegistry.Instance != null)
            BuildingRegistry.Instance.ClearAll();

        // 그리드 점유맵 초기화 (건물 재배치 전)
        if (GridService.Instance != null)
            GridService.Instance.Clear();

        // 4. 건물 다시 짓기
        int count = 0;
        foreach (BuildingSaveData bData in data.buildings)
        {
            BuildingData bd = allBuildingTypes.Find(x => x.prefab != null && x.prefab.name == bData.buildingName);
            if (bd != null)
            {
                var go = Instantiate(bd.prefab, bData.position, bData.rotation);
                if (BuildingRegistry.Instance != null)
                    BuildingRegistry.Instance.Register(bd, go);

                // 그리드 점유 재등록
                if (GridService.Instance != null)
                    GridService.Instance.TryOccupyWorld(bData.position);

                count++;
            }
            else
            {
                Debug.LogWarning($"❓ 도감에서 찾을 수 없는 건물: {bData.buildingName}");
            }
        }

        // 5. 인벤토리 복구
        if (Inventory.instance != null)
        {
            DeserializeSlots(data.inventorySlots, Inventory.instance.slots);

            if (Inventory.instance.hotbar != null)
            {
                DeserializeSlots(data.hotbarSlots, Inventory.instance.hotbar.slots);
                Inventory.instance.selectedHotbarIndex = Mathf.Clamp(data.selectedHotbarIndex,
                    0, Mathf.Max(0, Inventory.instance.hotbar.slots.Count - 1));
            }

            Inventory.instance.RefreshAllUI();
        }

        // Furniture transforms/active state must be restored before ShopSlot contents,
        // so moved shelves keep both their functional target and their stocked item.
        var customization = ShopCustomizationController.Instance
            ?? FindFirstObjectByType<ShopCustomizationController>();
        if (customization != null && customization.IsReady)
            customization.RestoreSavedState(data.placeables, data.placementStarterGranted);

        var outdoorPlacement = OutdoorPlacementController.Instance
            ?? FindFirstObjectByType<OutdoorPlacementController>();
        if (outdoorPlacement != null && outdoorPlacement.IsReady)
            outdoorPlacement.RestoreSavedState(data.placeables);

        var worldAdapter = WorldGameplayAdapterService.Instance
            ?? FindFirstObjectByType<WorldGameplayAdapterService>();
        if (worldAdapter != null && worldAdapter.IsReady)
            worldAdapter.RestoreRuntimeWorldState(restoredPlayerPosition, data.placeables);

        DeserializeShopSlots(data.shopSlots);

        RestoreHiredNpcs(data.hiredNpcs);
        FarmPlotInteraction.RestoreAllSavedState(data.farmPlots);

        // Completion and player-facing recovery depend on restored hiring,
        // village and world state, so restore these presentation sidecars last.
        var longPlay = FindFirstObjectByType<LongPlayProgressionController>();
        if (longPlay != null)
            longPlay.RestoreSavedSession(data.longPlayLastSupplyDay,
                data.longPlayDayStartRevenue, data.longPlayDayStartMoney);

        var worldAlpha = FindFirstObjectByType<WorldAlphaPlayableController>();
        if (worldAlpha != null)
        {
            bool legacyWorldAlpha = data.m85RecoveryRevision <= 0 &&
                                    HasLegacyWorldAlphaProgress(data);
            bool started = data.worldAlphaStarted || legacyWorldAlpha;
            if (!worldAlpha.RestoreSavedSession(started,
                    data.worldAlphaMoved || legacyWorldAlpha,
                    data.worldAlphaReachedShop || legacyWorldAlpha,
                    data.worldAlphaReachedWorkbench || legacyWorldAlpha, data.campaign))
            {
                if (demo != null)
                    return RejectLoad("FirstDay 플레이 상태를 복원하지 못했습니다.");
                Debug.LogWarning("[SaveManager] WorldSandbox player-facing session state could not be restored.");
            }
        }

        if (data.firstSettlement != null && settlement != null &&
            gameObject.scene.name == DepartureTutorialController.SceneName)
        {
            var production = FindFirstObjectByType<FirstProductionController>();
            if (production != null) production.ClearWorksitesForRestore();
            settlement.RestoreState(data.firstSettlement);
            if (production != null ? !production.RestoreState(data.firstProduction) : data.firstProduction.started)
            {
                // Global rollback is separate SaveManager hardening debt. Never report this load as successful.
                return RejectLoad("첫 생산 복원 실패: 이전 상태가 일부 적용됐을 수 있습니다.");
            }
        }

        // World/furniture/hiring/presentation restore can synchronously rebind runtime roots.
        // The saved player pose is the final authority, so apply it after every restore consumer.
        ApplyPlayerPose(restoredPlayerPosition, data);
        if (GameClock.Instance != null)
        {
            GameClock.Instance.ForceSet(data.gameHour, data.gameDay, "SaveManager.LoadGame.FinalClock");
            if (demo != null && demo.NightReady) GameClock.Instance.enabled = false;
        }
        // 보급 상자 수령·상점 실내 표시는 저장된 배치/가방/플레이어 위치에서 결정한다(별도 필드 없음).
        if (demo != null) demo.CompleteRestore(data);

        if (demo != null && !ValidateRestoredFirstDay(data, out string restoredReason))
            return RejectLoad("FirstDay 진행 복원 불일치: " + restoredReason);

        VillageChangeSignalController.Instance?.RefreshNow();
        villageCulture?.RefreshNow();

        Debug.Log($"📂 로드 완료! (건물 {count}개, 인벤토리/핫바 복구)");
        return true;
    }

    static void ApplyPlayerPose(Vector3 restoredPlayerPosition, SaveData data)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = restoredPlayerPosition;
        if (data != null && data.hasPlayerRotation)
            player.transform.rotation = data.playerRotation;
        if (controller != null) controller.enabled = true;
        Physics.SyncTransforms();
    }

    static bool HasLegacyWorldAlphaProgress(SaveData data)
    {
        if (data?.worldState == null ||
            data.worldState.worldMode != WorldPersistenceMigration.ProceduralMode)
            return false;

        return data.gameDay > 1 || data.cumulativeRevenue > 0 ||
               (data.inventorySlots != null && data.inventorySlots.Any(slot =>
                   slot != null && slot.count > 0)) ||
               (data.hotbarSlots != null && data.hotbarSlots.Any(slot =>
                   slot != null && slot.count > 0)) ||
               (data.shopSlots != null && data.shopSlots.Any(slot =>
                   slot != null && slot.occupied)) ||
               (data.hiredNpcs != null && data.hiredNpcs.Count > 0);
    }

    static void PrepareRuntimeForStateRestore()
    {
        (WorldGameplayAdapterService.Instance ??
            FindFirstObjectByType<WorldGameplayAdapterService>())?.PrepareForStateRestore();
        CustomerArrivalController.Instance?.PrepareForStateRestore();
        foreach (FishingSpot spot in FindObjectsByType<FishingSpot>(FindObjectsSortMode.None))
            spot?.PrepareForStateRestore();
        foreach (MiningSpot spot in FindObjectsByType<MiningSpot>(FindObjectsSortMode.None))
            spot?.PrepareForStateRestore();
        FindFirstObjectByType<LongPlayProgressionController>()?.PrepareForStateRestore();
        FindFirstObjectByType<SmartphoneUI>()?.Close();
    }

    // Product-entry UI reads only save presence before offering Continue.
    // Loading and migration still go through LoadGameAsync as the single authority.
    public System.Threading.Tasks.Task<bool> HasSaveAsync()
    {
        return HasAnySaveAsync();
    }

    async System.Threading.Tasks.Task<bool> HasAnySaveAsync() => _repository != null &&
        (await _repository.ExistsAsync(SaveKey) ||
         gameObject.scene.name != DepartureTutorialController.SceneName &&
         await _repository.ExistsAsync(LegacySaveKey));

    // 기존 동기 API 호환 — 핫키(F5/F9) 외에 외부에서 호출하는 코드가 있을 수 있어 유지.
    public void SaveGame() => _ = SaveGameAsync();
    public void LoadGame() => _ = LoadGameAsync();

    // -------- 버전 마이그레이션 --------

    // 저장 데이터의 스키마가 바뀔 때마다 한 단계씩 올리는 체인.
    // 각 단계는 해당 버전에서 추가된 필드에 안전한 기본값을 채운다.
    SaveData MigrateSaveData(SaveData data)
    {
        // v0 → v1: inventorySlots / hotbarSlots 가 없던 시절
        if (data.version < 1)
        {
            if (data.inventorySlots == null) data.inventorySlots = new List<SlotSaveData>();
            if (data.hotbarSlots == null) data.hotbarSlots = new List<SlotSaveData>();
            data.version = 1;
            Debug.Log("💾 마이그레이션 v0→v1: 인벤토리 슬롯 초기화");
        }

        // v1 → v2: lastAuditDay 추가
        if (data.version < 2)
        {
            data.lastAuditDay = 0;
            data.version = 2;
            Debug.Log("💾 마이그레이션 v1→v2: 감사 시스템 필드 추가");
        }

        // v2 → v3: friendshipData, hiredNpcs 추가
        if (data.version < 3)
        {
            if (data.friendshipData == null) data.friendshipData = new List<FriendshipRecord>();
            if (data.hiredNpcs == null)      data.hiredNpcs      = new List<HiredNpcRecord>();
            data.version = 3;
            Debug.Log("💾 마이그레이션 v2→v3: 친밀도 + 채용 필드 추가");
        }

        // v3 → v4: friendship daily cooldown, hired NPC transform/FSM state.
        if (data.version < 4)
        {
            if (data.friendshipData == null) data.friendshipData = new List<FriendshipRecord>();
            foreach (var friendship in data.friendshipData)
            {
                if (friendship == null) continue;
                if (friendship.lastDialogueDay < 0) friendship.lastDialogueDay = 0;
            }

            if (data.hiredNpcs == null) data.hiredNpcs = new List<HiredNpcRecord>();
            foreach (var hired in data.hiredNpcs)
            {
                if (hired == null) continue;
                if (string.IsNullOrEmpty(hired.hiredNpcId)) hired.hiredNpcId = hired.candidateAssetName;
                if (string.IsNullOrEmpty(hired.activeFsm)) hired.activeFsm = "None";
                if (string.IsNullOrEmpty(hired.consumerFsmState)) hired.consumerFsmState = "Idle";
                if (string.IsNullOrEmpty(hired.producerFsmState)) hired.producerFsmState = "Idle";
                if (string.IsNullOrEmpty(hired.specialistFsmState)) hired.specialistFsmState = "Idle";
                hired.hasTransform = false;
            }

            data.version = 4;
            Debug.Log("💾 마이그레이션 v3→v4: 친밀도 일일 제한 + 고용 NPC 상태 필드 추가");
        }

        // v4 → v5: ShopSlot stocked item/display price persistence.
        if (data.version < 5)
        {
            if (data.shopSlots == null) data.shopSlots = new List<ShopSlotSaveData>();
            data.version = 5;
            Debug.Log("💾 마이그레이션 v4→v5: 상점 진열대 저장 필드 추가");
        }

        // v5 → v6: first-day prototype profile and selected map.
        if (data.version < 6)
        {
            if (string.IsNullOrWhiteSpace(data.playerName)) data.playerName = "하늘";
            if (string.IsNullOrWhiteSpace(data.selectedMapId)) data.selectedMapId = "green_bay";
            data.firstDayPrototypeStage = Mathf.Clamp(data.firstDayPrototypeStage, 0, 6);
            data.version = 6;
            Debug.Log("💾 마이그레이션 v5→v6: 플레이어 이름/선택 맵/첫날 단계 필드 추가");
        }

        // v6 -> v7: long-play progression sidecar state.
        if (data.version < 7)
        {
            data.longPlayLastSupplyDay = 0;
            data.longPlayDayStartRevenue = 0L;
            data.longPlayDayStartMoney = Mathf.Max(0, data.money);
            data.version = 7;
            Debug.Log("[SaveManager] Migration v6->v7: long-play progression fields added.");
        }

        // v7 -> v8: daytime gathering / stock-prep completion state.
        if (data.version < 8)
        {
            data.dayPrepCollectedDay = 0;
            if (data.dayPrepCollectedActivities == null)
                data.dayPrepCollectedActivities = new List<string>();
            data.version = 8;
            Debug.Log("[SaveManager] Migration v7->v8: day-prep gathering fields added.");
        }

        // v8 -> v9: village culture pending/active change state (Task 057).
        if (data.version < 9)
        {
            data.villageCultureHasPendingChange = false;
            data.villageCulturePendingSaleDay = 0;
            if (data.villageCulturePendingCategory == null) data.villageCulturePendingCategory = "";
            data.villageCultureHasActiveChange = false;
            if (data.villageCultureActiveCategory == null) data.villageCultureActiveCategory = "";
            data.villageCultureHintShown = false;
            data.version = 9;
            Debug.Log("[SaveManager] Migration v8->v9: village culture change fields added.");
        }

        // v9 -> v10: zone/cell/rotation-based shop furniture placement state.
        // Empty placeables intentionally means "adopt the authored interior layout".
        if (data.version < 10)
        {
            data.placementStarterGranted = false;
            if (data.placeables == null) data.placeables = new List<PlaceableSaveData>();
            data.version = 10;
            Debug.Log("[SaveManager] Migration v9->v10: shop customization placement fields added.");
        }

        // v10 -> v11: additive procedural world payload. Existing absolute
        // buildings/placeables remain untouched and are explicitly LegacyFixed.
        if (data.version < WorldPersistenceMigration.AdditiveWorldSaveVersion)
        {
            WorldPersistenceMigration.UpgradeV10ToV11(data);
            Debug.Log("[SaveManager] Migration v10->v11: additive world state added as LegacyFixed.");
        }

        // v11 -> v12: additive gameplay recovery envelope. The embedded
        // procedural-world payload remains v11 and no existing field changes meaning.
        if (data.version < 12)
        {
            data.m85RecoveryRevision = 0;
            data.hasPlayerRotation = false;
            data.selectedHotbarIndex = 0;
            data.shopOpenedDay = -1;
            data.salesLogRecords ??= new List<SaleRecord>();
            data.salesDecisionDays ??= new List<SalesDecisionDaySaveData>();
            data.farmPlots ??= new List<FarmPlotSaveData>();
            data.version = 12;
            Debug.Log("[SaveManager] Migration v11->v12: additive gameplay recovery fields added.");
        }

        if (data.version < 13)
        {
            data.campaign = null;
            data.version = 13;
            Debug.Log("[SaveManager] Migration v12->v13: optional authored campaign; legacy progress preserved.");
        }

        if (data.version < 14)
        {
            if (data.campaign != null) data.campaign.firstNight = null;
            data.version = 14;
            Debug.Log("[SaveManager] Migration v13->v14: optional first-shop-night evidence; opening preserved.");
        }

        if (data.version < 15)
        {
            data.firstSettlement = null;
            data.version = 15;
        }
        if (data.version < 16)
        {
            data.firstProduction = new FirstProductionSaveData();
            data.version = 16;
        }
        if (data.version < 17)
        {
            data.version = 17; // Demo placements are reconstructed only for the exact approved v16 shape.
        }
        return data;
    }

    static void NormalizeSaveData(SaveData data)
    {
        if (data == null) return;

        data.playerName ??= string.Empty;
        data.selectedMapId ??= string.Empty;
        data.friendshipData ??= new List<FriendshipRecord>();
        data.hiredNpcs ??= new List<HiredNpcRecord>();
        data.buildings ??= new List<BuildingSaveData>();
        data.inventorySlots ??= new List<SlotSaveData>();
        data.hotbarSlots ??= new List<SlotSaveData>();
        data.shopSlots ??= new List<ShopSlotSaveData>();
        data.dayPrepCollectedActivities ??= new List<string>();
        data.salesLogRecords ??= new List<SaleRecord>();
        data.salesDecisionDays ??= new List<SalesDecisionDaySaveData>();
        data.farmPlots ??= new List<FarmPlotSaveData>();
        data.placeables ??= new List<PlaceableSaveData>();
        if (data.demoSession != null)
        {
            data.demoSession.placedBuildings ??= new List<WorldPlacedBuildingSaveData>();
            data.demoSession.companionIds ??= System.Array.Empty<string>();
        }

        data.villageCulturePendingCategory ??= string.Empty;
        data.villageCultureActiveCategory ??= string.Empty;
        data.villageCulturePendingItemName ??= string.Empty;
        data.villageCulturePendingBuyerName ??= string.Empty;
        data.villageCultureActiveItemName ??= string.Empty;
        data.villageCultureActiveBuyerName ??= string.Empty;

        data.worldState ??= WorldPersistenceMigration.CreateLegacyFixed();
        data.worldState.worldMode ??= WorldPersistenceMigration.LegacyFixedMode;
        data.worldState.modifiedCells ??= new List<WorldModifiedCellSaveData>();
        data.worldState.placedBuildings ??= new List<WorldPlacedBuildingSaveData>();
        data.worldState.shopFurniture ??= new List<WorldShopFurnitureSaveData>();
        data.worldState.resourceStates ??= new List<WorldResourceStateSaveData>();

        foreach (WorldPlacedBuildingSaveData building in data.worldState.placedBuildings)
            if (building != null) building.storedItems ??= new List<PlaceableStoredItemSaveData>();
        foreach (WorldShopFurnitureSaveData furniture in data.worldState.shopFurniture)
            if (furniture != null) furniture.storedItems ??= new List<PlaceableStoredItemSaveData>();
        foreach (PlaceableSaveData placeable in data.placeables)
            if (placeable != null) placeable.storedItems ??= new List<PlaceableStoredItemSaveData>();
    }

    static List<PlaceableSaveData> MergeProceduralFurnitureWithPlaceables(
        IReadOnlyList<PlaceableSaveData> original,
        IReadOnlyList<PlaceableSaveData> restoredFurniture)
    {
        var merged = new List<PlaceableSaveData>();
        if (original != null)
        {
            foreach (PlaceableSaveData record in original)
            {
                if (record == null ||
                    record.zoneId == ShopCustomizationController.ShopInteriorZoneId)
                    continue;
                merged.Add(record);
            }
        }

        if (restoredFurniture != null)
            merged.AddRange(restoredFurniture.Where(record => record != null));
        return merged;
    }

    void RestoreHiredNpcs(List<HiredNpcRecord> hiredNpcs)
    {
        if (HiringService.Instance == null || hiredNpcs == null) return;

        HiringService.Instance.ClearHired(true);
        foreach (var hr in hiredNpcs)
        {
            if (hr == null || string.IsNullOrEmpty(hr.candidateAssetName)) continue;

            var candidate = Resources.Load<NpcCandidateData>($"Candidates/{hr.candidateAssetName}");
            if (candidate == null)
            {
                Debug.LogWarning($"⚠️ 고용 NPC 복구 실패: Candidates/{hr.candidateAssetName} 없음");
                continue;
            }

            GameObject npc;
            bool restored = hr.hasTransform
                ? HiringService.Instance.RestoreHiredNpc(candidate, hr.hiredNpcId, hr.position, hr.rotation, hr.npcObjectName, out npc)
                : HiringService.Instance.RestoreHiredNpc(candidate, hr.hiredNpcId, hr.npcObjectName, out npc);

            if (restored) RestoreHiredNpcFsm(npc, hr);
        }
    }

    List<FriendshipRecord> SerializeFriendship()
    {
        var records = new List<FriendshipRecord>();
        var indexById = new Dictionary<string, FriendshipRecord>();

        foreach (var kv in FriendshipService.Instance.GetAllPoints())
        {
            if (string.IsNullOrEmpty(kv.Key)) continue;
            var record = new FriendshipRecord
            {
                friendshipId = kv.Key,
                points = kv.Value,
                lastDialogueDay = 0
            };
            records.Add(record);
            indexById[kv.Key] = record;
        }

        foreach (var kv in FriendshipService.Instance.GetAllLastDialogueDays())
        {
            if (string.IsNullOrEmpty(kv.Key)) continue;
            if (!indexById.TryGetValue(kv.Key, out FriendshipRecord record))
            {
                record = new FriendshipRecord
                {
                    friendshipId = kv.Key,
                    points = FriendshipService.Instance.GetPoints(kv.Key)
                };
                records.Add(record);
                indexById[kv.Key] = record;
            }
            record.lastDialogueDay = kv.Value;
        }

        return records;
    }

    HiredNpcRecord SerializeHiredNpc(HiringService.HiredNpcRuntimeRecord runtime)
    {
        if (runtime.Candidate == null) return null;

        var record = new HiredNpcRecord
        {
            hiredNpcId = runtime.HiredNpcId,
            candidateAssetName = runtime.Candidate.name,
            npcObjectName = runtime.Instance != null ? runtime.Instance.name : runtime.Candidate.ResolveDisplayName(),
            hasTransform = runtime.Instance != null,
            position = runtime.Instance != null ? runtime.Instance.transform.position : Vector3.zero,
            rotation = runtime.Instance != null ? runtime.Instance.transform.rotation : Quaternion.identity,
            spawnPosition = runtime.Instance != null ? runtime.Instance.transform.position : Vector3.zero,
            spawnRotation = runtime.Instance != null ? runtime.Instance.transform.rotation : Quaternion.identity,
            activeFsm = "None",
            consumerFsmState = "Idle",
            producerFsmState = "Idle",
            specialistFsmState = "Idle"
        };

        if (runtime.Instance != null)
            CaptureHiredNpcFsm(runtime.Instance, record);

        return record;
    }

    void CaptureHiredNpcFsm(GameObject npc, HiredNpcRecord record)
    {
        var consumer = npc.GetComponent<NpcController>();
        if (consumer != null)
        {
            record.consumerFsmState = consumer.GetFsmState();
            if (record.consumerFsmState != "Idle") record.activeFsm = "Consumer";
        }

        var producer = npc.GetComponent<ProducerNpcController>();
        if (producer != null)
        {
            record.producerFsmState = producer.GetFsmState();
            if (record.producerFsmState != "Idle") record.activeFsm = "Producer";
        }

        var specialist = npc.GetComponent<SpecialistNpcController>();
        if (specialist != null)
        {
            record.specialistFsmState = specialist.GetFsmState();
            if (record.specialistFsmState != "Idle") record.activeFsm = "Specialist";
        }
    }

    void RestoreHiredNpcFsm(GameObject npc, HiredNpcRecord record)
    {
        if (npc == null || record == null) return;

        var consumer = npc.GetComponent<NpcController>();
        var producer = npc.GetComponent<ProducerNpcController>();
        var specialist = npc.GetComponent<SpecialistNpcController>();

        bool consumerActive = record.activeFsm == "Consumer";
        bool producerActive = record.activeFsm == "Producer";
        bool specialistActive = record.activeFsm == "Specialist";

        if (consumer != null && !consumerActive)
            consumer.RestoreFsmState(string.IsNullOrEmpty(record.consumerFsmState) ? "Idle" : record.consumerFsmState);
        if (producer != null && !producerActive)
            producer.RestoreFsmState(string.IsNullOrEmpty(record.producerFsmState) ? "Idle" : record.producerFsmState);
        if (specialist != null && !specialistActive)
            specialist.RestoreFsmState(string.IsNullOrEmpty(record.specialistFsmState) ? "Idle" : record.specialistFsmState);

        if (consumer != null && consumerActive)
            consumer.RestoreFsmState(string.IsNullOrEmpty(record.consumerFsmState) ? "Idle" : record.consumerFsmState);
        if (producer != null && producerActive)
            producer.RestoreFsmState(string.IsNullOrEmpty(record.producerFsmState) ? "Idle" : record.producerFsmState);
        if (specialist != null && specialistActive)
            specialist.RestoreFsmState(string.IsNullOrEmpty(record.specialistFsmState) ? "Idle" : record.specialistFsmState);
    }

    // -------- 슬롯 직렬화 헬퍼 --------

    // InventorySlot 리스트 → SlotSaveData 리스트.
    // 빈 칸은 count=0 인 빈 DTO 로 직렬화한다 (JsonUtility 가 null 리스트 원소를 지원하지 않음).
    List<SlotSaveData> SerializeSlots(List<InventorySlot> slots)
    {
        var result = new List<SlotSaveData>(slots.Count);
        foreach (var slot in slots)
        {
            if (slot.IsEmpty)
            {
                result.Add(new SlotSaveData()); // count=0 → 빈 칸
            }
            else
            {
                result.Add(new SlotSaveData
                {
                    itemId       = slot.item.id,
                    itemName     = slot.item.itemName,
                    count        = slot.count,
                    quality      = slot.instance.quality,
                    currentPrice = slot.instance.currentPrice
                });
            }
        }
        return result;
    }

    // SlotSaveData 리스트 → InventorySlot 리스트 복원.
    // 기존 슬롯을 먼저 Clear 한 뒤, ItemRegistry.Find 로 원형을 찾아 채운다.
    void DeserializeSlots(List<SlotSaveData> saved, List<InventorySlot> slots)
    {
        // 기존 슬롯 초기화
        foreach (var slot in slots) slot.Clear();

        int len = Mathf.Min(saved.Count, slots.Count);
        for (int i = 0; i < len; i++)
        {
            var sd = saved[i];
            if (sd == null || sd.count <= 0) continue;

            Item item = ItemRegistry.Instance != null
                ? ItemRegistry.Instance.Find(sd.itemId, sd.itemName)
                : null;

            if (item == null)
            {
                Debug.LogWarning($"❓ 슬롯[{i}] 복원 실패: id={sd.itemId} name=\"{sd.itemName}\" — ItemRegistry 에 미등록");
                continue;
            }

            var inst = new ItemInstance(item, sd.count)
            {
                quality      = sd.quality,
                currentPrice = sd.currentPrice
            };
            slots[i].SetInstance(inst);
        }
    }

    List<ShopSlotSaveData> SerializeShopSlots()
    {
        var slots = GetOrderedShopSlots();
        var result = new List<ShopSlotSaveData>(slots.Count);

        for (int i = 0; i < slots.Count; i++)
        {
            ShopSlot slot = slots[i];
            var record = new ShopSlotSaveData
            {
                slotIndex = i,
                slotKey = BuildShopSlotKey(slot != null ? slot.transform : null),
                occupied = slot != null && !slot.IsEmpty,
                displayPrice = slot != null ? slot.displayPrice : 0
            };

            if (record.occupied && slot.currentItem != null && slot.currentItem.data != null)
            {
                record.itemId = slot.currentItem.data.id;
                record.itemName = slot.currentItem.data.itemName;
                record.count = slot.currentItem.count;
                record.quality = slot.currentItem.quality;
                record.currentPrice = slot.currentItem.currentPrice;
            }

            result.Add(record);
        }

        return result;
    }

    static bool ValidateSavedShopItems(List<ShopSlotSaveData> saved, out string reason)
    {
        reason = string.Empty;
        if (saved == null) { reason = "진열 기록이 없습니다."; return false; }
        foreach (var record in saved)
        {
            if (record == null || record.displayPrice < 0 || record.count < 0 ||
                record.occupied && (record.count <= 0 ||
                    ItemRegistry.Instance?.Find(record.itemId, record.itemName) == null))
            { reason = "진열 상품 ID, 재고 또는 가격을 복원할 수 없습니다."; return false; }
        }
        return true;
    }

    bool ValidateShopSlotTargets(List<ShopSlotSaveData> saved, out string reason)
    {
        if (!ValidateSavedShopItems(saved, out reason)) return false;
        var keys = GetOrderedShopSlots().Select(slot => BuildShopSlotKey(slot.transform)).ToList();
        if (keys.Count != saved.Count || keys.Distinct(System.StringComparer.Ordinal).Count() != keys.Count ||
            saved.Select(record => record.slotKey).Distinct(System.StringComparer.Ordinal).Count() != saved.Count ||
            saved.Any(record => string.IsNullOrEmpty(record.slotKey) || !keys.Contains(record.slotKey)))
        { reason = "저장된 가판대 ID와 현재 씬의 가판대가 일치하지 않습니다."; return false; }
        return true;
    }

    bool ValidateRestoredFirstDay(SaveData data, out string reason)
    {
        reason = string.Empty;
        if (EconomyService.Instance == null || EconomyService.Instance.Money != data.money ||
            EconomyService.Instance.CumulativeRevenue != data.cumulativeRevenue)
        { reason = "화폐/누적 매출"; return false; }
        if (GameClock.Instance == null || GameClock.Instance.CurrentDay != data.gameDay ||
            Mathf.Abs(GameClock.Instance.CurrentHour - data.gameHour) > 0.02f)
        { reason = "날짜/시간"; return false; }
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || Vector3.Distance(player.transform.position, data.playerPosition) > 0.05f)
        { reason = "플레이어 위치"; return false; }
        var demo = DemoSettlementController.Instance;
        if (demo == null || data.demoSession == null ||
            !string.IsNullOrEmpty(data.demoSession.shopId) && demo.OperatingShop == null)
        { reason = "FirstDay 상점"; return false; }
        var captured = demo.CaptureSaveState();
        // P11: 상점을 놓기 전 저장은 shopId가 ""(JsonUtility)이고 실행 중 값은 null이다. 같은 '상점 없음'으로 비교한다.
        if ((captured.shopId ?? string.Empty) != (data.demoSession.shopId ?? string.Empty) ||
            captured.placedBuildings.Count != data.demoSession.placedBuildings.Count ||
            data.demoSession.placedBuildings.Any(record => !captured.placedBuildings.Any(current =>
                current.instanceId == record.instanceId && current.buildingId == record.buildingId &&
                current.anchorX == record.anchorX && current.anchorZ == record.anchorZ &&
                current.rotationQuarterTurns == record.rotationQuarterTurns)))
        { reason = "상점/가판대 배치"; return false; }
        var actual = SerializeShopSlots();
        if (actual.Count != data.shopSlots.Count || data.shopSlots.Any(record =>
            !actual.Any(current => current.slotKey == record.slotKey &&
                current.occupied == record.occupied && current.displayPrice == record.displayPrice &&
                current.itemId == record.itemId && current.count == record.count &&
                Mathf.Abs(current.quality - record.quality) < 0.001f &&
                current.currentPrice == record.currentPrice)))
        { reason = "진열 상품/가격/재고"; return false; }
        if (Inventory.instance == null || !SameInventorySlots(data.inventorySlots, SerializeSlots(Inventory.instance.slots)) ||
            Inventory.instance.hotbar == null ||
            !SameInventorySlots(data.hotbarSlots, SerializeSlots(Inventory.instance.hotbar.slots)))
        { reason = "인벤토리/핫바"; return false; }
        return true;
    }

    static bool SameInventorySlots(List<SlotSaveData> saved, List<SlotSaveData> actual)
    {
        if (saved == null || actual == null || saved.Count != actual.Count) return false;
        for (int i = 0; i < saved.Count; i++)
        {
            var a = saved[i]; var b = actual[i];
            if (a == null || b == null || a.itemId != b.itemId || a.count != b.count ||
                a.currentPrice != b.currentPrice || Mathf.Abs(a.quality - b.quality) > 0.001f)
                return false;
        }
        return true;
    }

    void DeserializeShopSlots(List<ShopSlotSaveData> saved)
    {
        var slots = GetOrderedShopSlots();
        foreach (ShopSlot slot in slots)
        {
            if (slot == null) continue;
            slot.currentItem = null;
            slot.displayPrice = 0;
            slot.RefreshDisplay();
        }
        if (saved == null || saved.Count == 0) return;

        var byKey = new Dictionary<string, ShopSlot>();
        foreach (var slot in slots)
        {
            string key = BuildShopSlotKey(slot != null ? slot.transform : null);
            if (!string.IsNullOrEmpty(key) && !byKey.ContainsKey(key)) byKey.Add(key, slot);
        }

        foreach (var record in saved)
        {
            if (record == null) continue;

            ShopSlot slot = null;
            if (!string.IsNullOrEmpty(record.slotKey))
                byKey.TryGetValue(record.slotKey, out slot);

            if (slot == null && record.slotIndex >= 0 && record.slotIndex < slots.Count)
                slot = slots[record.slotIndex];

            if (slot == null)
            {
                Debug.LogWarning($"❓ ShopSlot 복원 실패: index={record.slotIndex} key=\"{record.slotKey}\"");
                continue;
            }

            slot.displayPrice = record.displayPrice;
            if (!record.occupied || record.count <= 0)
            {
                slot.currentItem = null;
                slot.RefreshDisplay();
                continue;
            }

            Item item = ItemRegistry.Instance != null
                ? ItemRegistry.Instance.Find(record.itemId, record.itemName)
                : null;

            if (item == null)
            {
                Debug.LogWarning($"❓ ShopSlot[{record.slotIndex}] 복원 실패: id={record.itemId} name=\"{record.itemName}\"");
                slot.currentItem = null;
                slot.RefreshDisplay();
                continue;
            }

            slot.currentItem = new ItemInstance(item, record.count)
            {
                quality = record.quality,
                currentPrice = record.currentPrice
            };
            slot.RefreshDisplay();
        }
    }

    List<ShopSlot> GetOrderedShopSlots()
    {
        var slots = FindObjectsByType<ShopSlot>(FindObjectsSortMode.None).ToList();
        slots.Sort((a, b) => string.CompareOrdinal(
            BuildShopSlotKey(a != null ? a.transform : null),
            BuildShopSlotKey(b != null ? b.transform : null)));
        return slots;
    }

    string BuildShopSlotKey(Transform transform)
    {
        if (transform == null) return string.Empty;

        var names = new List<string>();
        Transform current = transform;
        while (current != null)
        {
            names.Add(current.name.Replace("(Clone)", string.Empty).Trim());
            current = current.parent;
        }
        names.Reverse();
        return string.Join("/", names);
    }
}
