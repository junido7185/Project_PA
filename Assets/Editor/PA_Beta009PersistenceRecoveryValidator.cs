#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PA_Beta009PersistenceRecoveryValidator
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.BETA009.Active";
    const string FailedKey = "PA.BETA009.Failed";
    const string ConsoleErrorKey = "PA.BETA009.ConsoleErrors";
    const string FrameKey = "PA.BETA009.Frames";
    const string StageKey = "PA.BETA009.Stage";
    const string SaveDirectoryKey = "PA.BETA009.SaveDirectory";
    const string SaveDigestKey = "PA.BETA009.SaveDigest";

    static WorldAlphaPlayableController _alpha;
    static WorldGameplayAdapterService _adapter;
    static WorldPersistenceService _persistence;
    static LongPlayProgressionController _progression;
    static VillageCultureVisualController _culture;
    static Task _ioTask;
    static float _stageStarted;
    static int _settleFrames;

    static PA_Beta009PersistenceRecoveryValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        Subscribe();
        if (EditorApplication.isPlaying)
        {
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
    }

    [MenuItem("Project PA/Beta/BETA-009/Validate Persistence Recovery")]
    public static void RunBeta009Validation() => RunInternal();

    public static void RunBeta009ValidationBatch() => RunInternal();

    static void RunInternal()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetInt(ConsoleErrorKey, 0);
            SetStage(0);
            string directory = Path.Combine(Application.dataPath, "..", "Logs",
                "BETA009Persistence", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(directory);
            SessionState.SetString(SaveDirectoryKey, directory);
            Subscribe();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && !scene.isDirty,
                "WorldSandbox opens saved and clean");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }

    static void Subscribe()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        Application.logMessageReceived += OnLogMessage;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetInt(FrameKey, 0);
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            if (SessionState.GetInt(StageKey, 0) == 2 &&
                !SessionState.GetBool(FailedKey, false))
                EditorApplication.delayCall += RestartPlayMode;
            else
                Finish();
        }
    }

    static void RestartPlayMode()
    {
        if (!SessionState.GetBool(ActiveKey, false) ||
            SessionState.GetBool(FailedKey, false))
        {
            Finish();
            return;
        }

        try
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && !scene.isDirty,
                "restart reopens the saved WorldSandbox scene cleanly");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }

    static void ValidateRuntime()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
        int frames = SessionState.GetInt(FrameKey, 0) + 1;
        SessionState.SetInt(FrameKey, frames);
        int stage = SessionState.GetInt(StageKey, 0);

        try
        {
            ResolveRuntime();
            bool ready = _alpha != null && _alpha.IsReady && _adapter != null &&
                         _adapter.IsReady && _adapter.ResidentSpawnAnchorsReady &&
                         _adapter.DaytimeActivitiesBound && _persistence != null &&
                         _progression != null && _culture != null &&
                         SaveManager.instance != null && EconomyService.Instance != null &&
                         GameClock.Instance != null && HiringService.Instance != null &&
                         SalesLogManager.Instance != null && Inventory.instance != null;
            if (!ready && frames < 1200) return;
            if (!ready)
                throw new TimeoutException("WorldSandbox persistence authorities did not initialize.");

            if (stage == 0)
            {
                Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
                    $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
                SetIsolatedRepository();
                BuildPersistentFixture();
                _ioTask = SaveManager.instance.SaveGameAsync();
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(1);
                return;
            }

            if (stage == 1)
            {
                if (!AwaitTask(8f, "initial isolated save")) return;
                string path = SavePath();
                Require(File.Exists(path), "SaveManager writes the isolated savegame.json");
                SaveData saved = ReadSavedData();
                ValidateSerializedSnapshot(saved);
                SessionState.SetString(SaveDigestKey, ComputeDigest(File.ReadAllText(path)));
                SetStage(2);
                EditorApplication.update -= ValidateRuntime;
                EditorApplication.ExitPlaymode();
                return;
            }

            if (stage == 2)
            {
                SetIsolatedRepository();
                _ioTask = SaveManager.instance.LoadGameAsync();
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(3);
                return;
            }

            if (stage == 3)
            {
                if (!AwaitTask(15f, "restart load")) return;
                SaveData saved = ReadSavedData();
                ValidateRestoredRuntime(saved, "restart");
                ProveContinuedPlay(saved);
                _ioTask = SaveManager.instance.LoadGameAsync();
                _stageStarted = Time.realtimeSinceStartup;
                _settleFrames = 0;
                SetStage(4);
                return;
            }

            if (stage == 4)
            {
                if (!AwaitTask(15f, "same-save second load")) return;
                if (_settleFrames++ < 3) return;
                SaveData saved = ReadSavedData();
                ValidateRestoredRuntime(saved, "second load");
                Require(ComputeDigest(File.ReadAllText(SavePath())) ==
                        SessionState.GetString(SaveDigestKey, string.Empty),
                    "load operations do not rewrite the isolated save payload");
                Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                    "blocking runtime Console Error/Exception/Assert count is 0");
                Require(!SceneManager.GetActiveScene().isDirty,
                    "runtime recovery leaves WorldSandbox scene clean");
                Debug.Log("[BETA-009] PLAY_MODE_PASS schema=12 restart=true continue=true " +
                          "repeatLoad=true duplicate=false world=true player=true inventory=true " +
                          "shop=true farm=true hiring=true feed=true village=true console=0");
                SetStage(5);
                EditorApplication.update -= ValidateRuntime;
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception exception)
        {
            EditorApplication.update -= ValidateRuntime;
            Fail(exception);
        }
    }

    static void BuildPersistentFixture()
    {
        Require(_alpha.BeginNewGame() && _alpha.PlayerFacingHudVisible,
            "the fresh player-facing week starts before state capture");
        Require(_adapter.BeginDayForValidation(9f, 2),
            "the fixture enters Day 2 through the existing day authority");

        EconomyService.Instance.ForceSet(1600, "BETA-009 persistence fixture");
        EconomyService.Instance.ForceSetCumulativeRevenue(325,
            "BETA-009 persistence fixture");
        Require(_progression.TryPurchaseCurrentDaySupply(out string supplyReason),
            $"Day 2 supply sidecar is committed through the existing economy ({supplyReason})");

        Vector2Int movementCell = FindWalkableMovementCell();
        Require(_alpha.MovePlayerToCellForValidation(movementCell),
            "player moves to a stable generated save cell");
        _adapter.PlayerRoot.transform.rotation = Quaternion.Euler(0f, 137f, 0f);

        Vector2Int terraformCell = FindTerraformCell();
        Require(_alpha.RaiseCell(terraformCell).Succeeded &&
                _persistence.CurrentSparseDeltaCount == 1,
            "one sparse terrain edit is committed");
        Vector2Int buildingCell = FindBuildingCell();
        Require(_alpha.PlaceOrMoveShed(buildingCell, 1).Succeeded,
            "one B09 storage shed is placed transactionally");
        WorldPlacedBuildingRuntime building = GetPlacedBuilding();

        Item plank = RequireItem("Items/Item_Plank");
        StorageBox storage = building.GameObject.GetComponent<StorageBox>();
        Require(storage != null && storage.AddInstance(new ItemInstance(plank, 2)
        {
            quality = 1.45f,
            currentPrice = 31
        }), "B09 receives an ItemInstance with quality and price metadata");

        Require(_alpha.TryMoveSalesDisplay(1, 0, 1, out string displayReason),
            $"the functional B01 display is moved and rotated ({displayReason})");

        Item seed = RequireItem("Items/Item_15_Seed");
        Require(Inventory.instance.AddItem(seed, 1), "one real seed enters Inventory");
        FarmPlotInteraction plot = _adapter.FarmPlots.OrderBy(value => value.plotId).First();
        plot.Interact(_adapter.PlayerRoot);
        Require(plot.CurrentCrop != null, "the seed is consumed through FarmPlotInteraction");
        plot.CurrentCrop.RestoreGrowthState(1, 5.5f);

        Item ore = RequireItem("Items/Item_Ore");
        InventorySlot inventorySlot = Inventory.instance.slots.Last(slot => slot != null && slot.IsEmpty);
        inventorySlot.SetInstance(new ItemInstance(ore, 3)
        {
            quality = 1.75f,
            currentPrice = 33
        });
        Item fish = RequireItem("Items/Item_Fish");
        InventorySlot hotbarSlot = Inventory.instance.hotbar.GetSlot(1);
        hotbarSlot.SetInstance(new ItemInstance(fish, 2)
        {
            quality = 1.25f,
            currentPrice = 29
        });
        Inventory.instance.selectedHotbarIndex = 1;
        Inventory.instance.RefreshAllUI();

        NpcCandidateData farmer = Resources.Load<NpcCandidateData>(
            "Candidates/Candidate_Farmer");
        GameObject hired = null;
        string hireReason = "candidate unavailable";
        bool hiredSuccessfully = farmer != null && HiringService.Instance.TryHire(farmer,
            out hired, out hireReason);
        Require(hiredSuccessfully && hired != null,
            $"a paid Farmer is created through HiringService ({hireReason})");

        List<ShopSlot> slots = _adapter.RuntimeShopSlots.Where(slot => slot != null).Take(2).ToList();
        Require(slots.Count == 2, "two stable B01 ShopSlots are available");
        slots[0].currentItem = new ItemInstance(plank, 1)
        {
            quality = 1.3f,
            currentPrice = 25
        };
        slots[0].displayPrice = plank.basePrice;
        slots[0].RefreshDisplay();
        slots[1].currentItem = new ItemInstance(fish, 1)
        {
            quality = 1.6f,
            currentPrice = 41
        };
        slots[1].displayPrice = fish.basePrice + 3;
        slots[1].RefreshDisplay();

        Require(_adapter.TryOpenShopForNight(2, out string openReason) &&
                DayNightShopLoopController.Instance.IsShopOpenForCustomers,
            $"the saved shop-open gate is committed ({openReason})");
        Require(slots[0].TryPurchaseByNpc("BETA009_BUYER", out int paid) && paid > 0,
            "an actual ShopSlot sale records economy and Feed history");
        SalesLogManager.Instance.RecordRejection(plank.itemName,
            plank.category.ToString(), "BETA009_DECLINER", 2);
        Require(_culture.HasPendingChange && _culture.PendingItemName == plank.itemName,
            "the exact sold item schedules the next village response");
    }

    static void ValidateSerializedSnapshot(SaveData saved)
    {
        Require(saved != null && saved.version == SaveManager.CurrentSaveVersion &&
                saved.m85RecoveryRevision == 1,
            "new saves use the additive v12 gameplay envelope");
        Require(saved.worldState != null &&
                saved.worldState.worldMode == WorldPersistenceMigration.ProceduralMode &&
                saved.worldState.modifiedCells.Count == 1 &&
                saved.worldState.placedBuildings.Count == 1 &&
                saved.worldState.shopFurniture.Count == 1,
            "World v11 remains embedded with terrain, B09 and B01 furniture state");
        Require(saved.worldState.placedBuildings[0].storedItems.Count == 1 &&
                saved.worldState.placedBuildings[0].storedItems[0].count == 2,
            "B09 storage contents are serialized inside the stable building record");
        Require(saved.hasPlayerRotation && saved.worldAlphaStarted &&
                saved.selectedHotbarIndex == 1 && saved.shopOpenedDay == 2,
            "player pose, onboarding, hotbar and shop-open recovery fields are serialized");
        Require(saved.salesLogRecords.Count == 1 &&
                saved.salesLogRecords[0].buyerName == "BETA009_BUYER" &&
                saved.salesDecisionDays.Any(day => day != null && day.gameDay == 2 &&
                    day.purchases == 1 && day.rejections == 1),
            "sale history and daily purchase/rejection stats are serialized exactly once");
        Require(saved.farmPlots.Count >= FarmPlotInteraction.RuntimePlotCount &&
                saved.farmPlots.Any(plot => plot != null && plot.planted &&
                    plot.currentStageIndex == 1),
            "planted crop stage is serialized by stable plot identity");
        Require(saved.hiredNpcs.Count == 1 &&
                saved.hiredNpcs[0].candidateAssetName == "Candidate_Farmer",
            "the paid hired resident is serialized once");
        Require(saved.villageCultureHasPendingChange &&
                !string.IsNullOrWhiteSpace(saved.villageCulturePendingItemName) &&
                saved.villageCulturePendingBuyerName == "BETA009_BUYER",
            "exact pending village item and buyer context are serialized");
        Require(saved.longPlayLastSupplyDay == 2,
            "the Day 2 progression supply sidecar is serialized");
        ValidateLegacyMigrationContract();
    }

    static void ValidateRestoredRuntime(SaveData saved, string label)
    {
        Require(saved != null, $"{label} can read the isolated snapshot");
        Require(_persistence.ActiveSeed == saved.worldState.worldSeed &&
                _persistence.CurrentSparseDeltaCount == saved.worldState.modifiedCells.Count &&
                WorldPersistenceService.ComputePayloadChecksum(_adapter.CaptureWorldState()) ==
                WorldPersistenceService.ComputePayloadChecksum(saved.worldState),
            $"{label} restores the exact procedural-world checksum");

        WorldPlacedBuildingRuntime building = GetPlacedBuilding();
        StorageBox storage = building.GameObject.GetComponent<StorageBox>();
        Require(storage != null && storage.items.Count == 1 &&
                storage.items[0].count == 2 &&
                Mathf.Abs(storage.items[0].quality - 1.45f) < 0.001f &&
                storage.items[0].currentPrice == 31,
            $"{label} restores B09 storage ItemInstance metadata without duplication");

        Require(_alpha.Grid.WorldToCell(_adapter.PlayerRoot.transform.position,
                    out Vector2Int playerCell) &&
                _alpha.Grid.WorldToCell(saved.playerPosition, out Vector2Int savedCell) &&
                playerCell == savedCell &&
                Quaternion.Angle(_adapter.PlayerRoot.transform.rotation,
                    saved.playerRotation) < 0.5f,
            $"{label} restores the player cell and facing");

        InventorySlot oreSlot = Inventory.instance.slots.FirstOrDefault(slot =>
            slot != null && !slot.IsEmpty && slot.item.itemName == "Ore" &&
            Mathf.Abs(slot.instance.quality - 1.75f) < 0.001f);
        InventorySlot hotbar = Inventory.instance.hotbar.GetSlot(1);
        Require(oreSlot != null && oreSlot.count == 3 && oreSlot.instance.currentPrice == 33 &&
                hotbar != null && !hotbar.IsEmpty && hotbar.count == 2 &&
                Mathf.Abs(hotbar.instance.quality - 1.25f) < 0.001f &&
                hotbar.instance.currentPrice == 29 &&
                Inventory.instance.selectedHotbarIndex == 1,
            $"{label} restores inventory/hotbar counts, metadata and selected index");

        ShopSlot stocked = _adapter.RuntimeShopSlots.FirstOrDefault(slot => slot != null &&
            !slot.IsEmpty && slot.currentItem.data.itemName == "Fish");
        Require(stocked != null && stocked.currentItem.count == 1 &&
                Mathf.Abs(stocked.currentItem.quality - 1.6f) < 0.001f &&
                stocked.currentItem.currentPrice == 41 &&
                stocked.displayPrice == stocked.currentItem.data.basePrice + 3,
            $"{label} restores the stocked moved B01 display");

        Require(EconomyService.Instance.Money == saved.money &&
                EconomyService.Instance.CumulativeRevenue == saved.cumulativeRevenue &&
                GameClock.Instance.CurrentDay == saved.gameDay &&
                Mathf.Abs(GameClock.Instance.CurrentHour - saved.gameHour) < 0.05f &&
                DayNightShopLoopController.Instance.PlayerHasOpenedShopToday &&
                _alpha.PlayerFacingHudVisible &&
                Mathf.Approximately(GameClock.Instance.secondsPerGameHour,
                    WorldGameplayAdapterService.PlayableSecondsPerGameHour),
            $"{label} restores economy, time, open gate and playable clock/HUD");

        List<SaleRecord> history = SalesLogManager.Instance.GetRecent(100);
        SalesLogManager.DailyDecisionStats stats =
            SalesLogManager.Instance.GetDailyDecisionStats(2);
        Require(history.Count == 1 && history[0].buyerName == "BETA009_BUYER" &&
                stats.purchases == 1 && stats.rejections == 1,
            $"{label} replace-restores Feed history and decision stats once");
        FeedUI feed = FindSceneObject<FeedUI>();
        Require(feed != null, $"{label} retains the runtime Feed application");
        feed.Refresh();
        Require(feed.VisibleSaleCardCount == 1,
            $"{label} rebuilds one Feed sale card from restored history");

        Require(_culture.HasPendingChange &&
                _culture.PendingItemName == saved.villageCulturePendingItemName,
            $"{label} restores exact pending village causality without replaying the sale");
        Require(HiringService.Instance.HiredCount == 1 &&
                HiringService.Instance.GetHiredCandidates().Single().name == "Candidate_Farmer" &&
                CountActiveHiredFarmers() == 1,
            $"{label} restores one active Farmer without duplicate runtime NPCs");

        FarmPlotSaveData planted = saved.farmPlots.First(plot => plot != null && plot.planted);
        FarmPlotInteraction runtimePlot = _adapter.FarmPlots.First(plot => plot.plotId == planted.plotId);
        Require(runtimePlot.CurrentCrop != null &&
                runtimePlot.CurrentCrop.CurrentStageIndex >= planted.currentStageIndex &&
                runtimePlot.GetComponentsInChildren<Crop>(true).Count(crop =>
                    crop != null && crop.gameObject.activeInHierarchy) == 1,
            $"{label} restores one growing crop on its stable plot");
        Require(_progression.LastSupplyDay == saved.longPlayLastSupplyDay,
            $"{label} restores LongPlay progression after hiring and village authorities");
    }

    static void ProveContinuedPlay(SaveData saved)
    {
        int moneyBefore = EconomyService.Instance.Money;
        int salesBefore = SalesLogManager.Instance.GetRecent(100).Count;
        ShopSlot empty = _adapter.RuntimeShopSlots.First(slot => slot != null && slot.IsEmpty);
        Item plank = RequireItem("Items/Item_Plank");
        empty.currentItem = new ItemInstance(plank, 1)
        {
            quality = 1.1f,
            currentPrice = plank.basePrice
        };
        empty.displayPrice = plank.basePrice;
        empty.RefreshDisplay();
        Require(empty.TryPurchaseByNpc("BETA009_CONTINUE", out int paid) && paid > 0 &&
                EconomyService.Instance.Money == moneyBefore + paid &&
                SalesLogManager.Instance.GetRecent(100).Count == salesBefore + 1,
            "loaded state continues through a new authoritative sale");
        Inventory.instance.selectedHotbarIndex = 0;
        Require(EconomyService.Instance.Money != saved.money &&
                Inventory.instance.selectedHotbarIndex != saved.selectedHotbarIndex,
            "unsaved continuation diverges before the same snapshot is loaded again");
    }

    static void ValidateLegacyMigrationContract()
    {
        var legacy = new SaveData
        {
            version = WorldPersistenceMigration.AdditiveWorldSaveVersion,
            money = 123,
            worldState = WorldPersistenceMigration.CreateLegacyFixed(),
            salesLogRecords = null,
            salesDecisionDays = null,
            farmPlots = null,
            shopOpenedDay = 0
        };
        MethodInfo migrate = typeof(SaveManager).GetMethod("MigrateSaveData",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo normalize = typeof(SaveManager).GetMethod("NormalizeSaveData",
            BindingFlags.Static | BindingFlags.NonPublic);
        Require(migrate != null && normalize != null,
            "save migration and normalization remain explicit authorities");
        var migrated = (SaveData)migrate.Invoke(SaveManager.instance, new object[] { legacy });
        normalize.Invoke(null, new object[] { migrated });
        Require(migrated.version == SaveManager.CurrentSaveVersion && migrated.money == 123 &&
                migrated.worldState.worldMode == WorldPersistenceMigration.LegacyFixedMode &&
                migrated.salesLogRecords != null && migrated.salesDecisionDays != null &&
                migrated.farmPlots != null && migrated.shopOpenedDay == -1,
            "v11 migrates additively to v12 without converting Golden LegacyFixed state");
    }

    static bool AwaitTask(float timeoutSeconds, string label)
    {
        if (_ioTask != null && !_ioTask.IsCompleted)
        {
            if (Time.realtimeSinceStartup - _stageStarted > timeoutSeconds)
                throw new TimeoutException($"{label} exceeded {timeoutSeconds:0} seconds.");
            return false;
        }
        if (_ioTask != null && _ioTask.IsFaulted) throw _ioTask.Exception;
        return true;
    }

    static void SetIsolatedRepository()
    {
        MethodInfo setter = typeof(SaveManager).GetMethod("SetRepositoryForValidation",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Require(setter != null, "SaveManager exposes its editor-only isolated repository seam");
        setter.Invoke(SaveManager.instance, new object[]
        {
            new LocalJsonSaveRepository(SessionState.GetString(SaveDirectoryKey, string.Empty))
        });
    }

    static SaveData ReadSavedData()
    {
        string json = File.ReadAllText(SavePath());
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null) throw new InvalidOperationException("isolated save JSON is unreadable");
        return data;
    }

    static string SavePath() => Path.Combine(
        SessionState.GetString(SaveDirectoryKey, string.Empty), "savegame.json");

    static string ComputeDigest(string value)
    {
        using SHA256 sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)));
    }

    static Item RequireItem(string path)
    {
        Item item = Resources.Load<Item>(path);
        if (item == null) throw new InvalidOperationException($"missing Item resource: {path}");
        return item;
    }

    static Vector2Int FindWalkableMovementCell()
    {
        Require(_alpha.Grid.WorldToCell(_adapter.PlayerRoot.transform.position,
                out Vector2Int origin), "player start resolves to the generated grid");
        Require(_alpha.Grid.TryGetCell(origin, out WorldCellData originData),
            "player start cell remains readable");
        foreach (WorldCellData cell in _alpha.Grid.Cells
                     .Where(cell => cell.IsWalkable && !cell.HasWater &&
                                    cell.ElevationLevel == originData.ElevationLevel &&
                                    cell.Coordinate != origin)
                     .OrderBy(cell => Mathf.Abs(cell.Coordinate.x - origin.x) +
                                      Mathf.Abs(cell.Coordinate.y - origin.y)))
        {
            if (Mathf.Abs(cell.Coordinate.x - origin.x) +
                Mathf.Abs(cell.Coordinate.y - origin.y) <= 8)
                return cell.Coordinate;
        }
        throw new InvalidOperationException("No nearby safe movement cell was generated.");
    }

    static Vector2Int FindTerraformCell()
    {
        foreach (WorldCellData cell in _alpha.Grid.Cells.OrderBy(cell => cell.Coordinate.y)
                     .ThenBy(cell => cell.Coordinate.x))
        {
            if (!_alpha.Grid.IsTerraformProtected(cell.Coordinate) &&
                cell.Occupancy == WorldCellOccupancy.Empty && !cell.HasWater && !cell.HasPath &&
                cell.ElevationLevel < _alpha.Grid.Definition.MaxElevationLevel)
                return cell.Coordinate;
        }
        throw new InvalidOperationException("No legal terraform cell exists.");
    }

    static Vector2Int FindBuildingCell()
    {
        string id = WorldBuildingPlacementService.PrototypeInstanceId;
        for (int z = 0; z < _alpha.Grid.Definition.Height; z++)
        for (int x = 0; x < _alpha.Grid.Definition.Width; x++)
        {
            var coordinate = new Vector2Int(x, z);
            if (_alpha.Buildings.Evaluate(id, coordinate, 1).Succeeded)
                return coordinate;
        }
        throw new InvalidOperationException("No legal B09 building anchor exists.");
    }

    static WorldPlacedBuildingRuntime GetPlacedBuilding()
    {
        if (_alpha.Buildings.TryGetPlacement(
                WorldBuildingPlacementService.PrototypeInstanceId,
                out WorldPlacedBuildingRuntime building)) return building;
        throw new InvalidOperationException("The B09 persistence fixture is missing.");
    }

    static int CountActiveHiredFarmers()
    {
        return UnityEngine.Object.FindObjectsByType<ProducerNpcController>(
                FindObjectsSortMode.None)
            .Count(producer => producer != null && producer.gameObject.activeInHierarchy &&
                               producer.specialty == NpcSpecialty.Farmer &&
                               producer.GetComponent<NpcController>() != null);
    }

    static T FindSceneObject<T>() where T : UnityEngine.Object
    {
        return Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(value =>
        {
            if (value is Component component)
                return component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded;
            return true;
        });
    }

    static void ResolveRuntime()
    {
        _alpha = WorldAlphaPlayableController.Instance ??
                 UnityEngine.Object.FindFirstObjectByType<WorldAlphaPlayableController>();
        _adapter = WorldGameplayAdapterService.Instance ??
                   UnityEngine.Object.FindFirstObjectByType<WorldGameplayAdapterService>();
        _persistence = WorldPersistenceService.Instance ??
                       UnityEngine.Object.FindFirstObjectByType<WorldPersistenceService>();
        _progression = LongPlayProgressionController.Instance ??
                       UnityEngine.Object.FindFirstObjectByType<LongPlayProgressionController>();
        _culture = VillageCultureVisualController.Instance ??
                   UnityEngine.Object.FindFirstObjectByType<VillageCultureVisualController>();
    }

    static void SetStage(int stage)
    {
        SessionState.SetInt(StageKey, stage);
        SessionState.SetInt(FrameKey, 0);
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(ActiveKey, false) ||
            (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
        SessionState.SetInt(ConsoleErrorKey,
            SessionState.GetInt(ConsoleErrorKey, 0) + 1);
    }

    static void Fail(Exception exception)
    {
        SessionState.SetBool(FailedKey, true);
        Debug.LogError($"[BETA-009] FAIL {exception.Message}\n{exception}");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else Finish();
    }

    static void Finish()
    {
        EditorApplication.update -= ValidateRuntime;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        bool failed = SessionState.GetBool(FailedKey, false) ||
                      SessionState.GetInt(ConsoleErrorKey, 0) != 0;
        int errors = SessionState.GetInt(ConsoleErrorKey, 0);
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(FailedKey);
        SessionState.EraseInt(ConsoleErrorKey);
        SessionState.EraseInt(FrameKey);
        SessionState.EraseInt(StageKey);
        SessionState.EraseString(SaveDirectoryKey);
        SessionState.EraseString(SaveDigestKey);
        Debug.Log(failed
            ? $"[BETA-009] FINISHED_WITH_ERRORS consoleErrors={errors}"
            : "[BETA-009] FINISHED_PASS BETA_009_COMPLETE schema=12 restart=true " +
              "continue=true repeatLoad=true duplicate=false console=0");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[BETA-009] PASS {message}");
    }
}
#endif
