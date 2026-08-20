using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    // 스키마 버전 — SaveManager 가 로드 시 마이그레이션에 사용한다.
    // 새 필드가 추가되면 CurrentSaveVersion(SaveManager) 을 올리고 마이그레이션 함수를 추가한다.
    public int version = 0;

    // 플레이어 정보
    public int money;
    public Vector3 playerPosition;
    public Quaternion playerRotation = Quaternion.identity;
    public bool hasPlayerRotation = false;
    public string playerName = "하늘";
    public string selectedMapId = "green_bay";
    public int firstDayPrototypeStage = 0;

    // BETA-009 recovery fields are additive in the v12 gameplay envelope. The
    // procedural-world payload remains v11 and retains its established meaning.
    public int m85RecoveryRevision = 0;
    public bool worldAlphaStarted = false;
    public bool worldAlphaMoved = false;
    public bool worldAlphaReachedShop = false;
    public bool worldAlphaReachedWorkbench = false;

    // 티어 시스템
    public int currentTier = 0;
    public long cumulativeRevenue = 0;
    public int reputation = 0;

    // 인게임 시간 (GameClock)
    public float gameHour = 7f;   // 0~23.99
    // Long-play progression sidecar state. Added in v7.
    public int longPlayLastSupplyDay = 0;
    public long longPlayDayStartRevenue = 0;
    public int longPlayDayStartMoney = 0;
    public int   gameDay  = 1;    // 1 이상

    // 감사 시스템 (AuditService) — v2 추가
    public int lastAuditDay = 0;

    // FriendshipService save data. Added in v3, expanded in v4 with lastDialogueDay.
    public List<FriendshipRecord> friendshipData = new List<FriendshipRecord>();

    // HiringService save data. Added in v3, expanded in v4 with transform and FSM state.
    public List<HiredNpcRecord> hiredNpcs = new List<HiredNpcRecord>();

    // 건물 정보 리스트
    public List<BuildingSaveData> buildings = new List<BuildingSaveData>();

    // 인벤토리 · 핫바 (SlotSaveData.count == 0 이면 빈 칸)
    public List<SlotSaveData> inventorySlots = new List<SlotSaveData>();
    public List<SlotSaveData> hotbarSlots    = new List<SlotSaveData>();
    public int selectedHotbarIndex = 0;

    // ShopSlot display state. Added in v5 so a saved playable-day demo restores stocked shelves.
    public List<ShopSlotSaveData> shopSlots = new List<ShopSlotSaveData>();

    // Daytime gathering / stock-prep completion. Added in v8 so a same-day gather does not
    // reset on save/load (DayNightShopLoopController._prepCollectionDays).
    public int dayPrepCollectedDay = 0;
    public List<string> dayPrepCollectedActivities = new List<string>();
    public int shopOpenedDay = -1;

    // Feed and daily purchase/rejection summaries. Restoring this history never
    // replays a transaction or sale event; it only restores the read model.
    public List<SaleRecord> salesLogRecords = new List<SaleRecord>();
    public List<SalesDecisionDaySaveData> salesDecisionDays = new List<SalesDecisionDaySaveData>();
    public List<FarmPlotSaveData> farmPlots = new List<FarmPlotSaveData>();

    // Village culture visual state. Added in v9 (Task 057) so the core differentiator —
    // "오늘 판 물건이 다음날 마을을 바꾼다" — survives save/load.
    // pending: 오늘 판매로 예약된 다음날 변화 / active: 이미 나타난 변화.
    public bool villageCultureHasPendingChange = false;
    public int villageCulturePendingSaleDay = 0;
    public string villageCulturePendingCategory = "";
    public bool villageCultureHasActiveChange = false;
    public string villageCultureActiveCategory = "";
    public bool villageCultureHintShown = false;
    public string villageCulturePendingItemName = "";
    public string villageCulturePendingBuyerName = "";
    public int villageCultureActiveSaleDay = 0;
    public int villageCultureActiveResponseDay = 0;
    public string villageCultureActiveItemName = "";
    public string villageCultureActiveBuyerName = "";

    // Zone-aware shop furniture customization. Added in v10.
    public bool placementStarterGranted = false;
    public List<PlaceableSaveData> placeables = new List<PlaceableSaveData>();

    // Procedural world state. Added additively in v11; v10 and older saves migrate
    // to LegacyFixed and keep their absolute buildings/placeables unchanged.
    public WorldStateSaveData worldState = new WorldStateSaveData();
}

[System.Serializable]
public class WorldStateSaveData
{
    public string worldMode = "LegacyFixed";
    public long worldSeed = 0L;
    public int generationVersion = 0;
    public int widthCells = 0;
    public int heightCells = 0;
    public float cellSizeMeters = 0f;
    public int chunkSizeCells = 0;
    public List<WorldModifiedCellSaveData> modifiedCells = new List<WorldModifiedCellSaveData>();
    public List<WorldPlacedBuildingSaveData> placedBuildings = new List<WorldPlacedBuildingSaveData>();
    public List<WorldShopFurnitureSaveData> shopFurniture = new List<WorldShopFurnitureSaveData>();
    public List<WorldResourceStateSaveData> resourceStates = new List<WorldResourceStateSaveData>();
    public bool hasSafePlayerPosition = false;
    public Vector3 safePlayerPosition;
    public int safePlayerCellX = 0;
    public int safePlayerCellZ = 0;
}

[System.Serializable]
public class WorldModifiedCellSaveData
{
    public int x;
    public int z;
    public int elevationLevel;
    public int groundType;
    public int pathType;
    public int waterSurfaceLevel;
    public int waterDepthLevels;
}

[System.Serializable]
public class WorldPlacedBuildingSaveData
{
    public string instanceId;
    public string buildingId;
    public int anchorX;
    public int anchorZ;
    public int rotationQuarterTurns;
    public List<PlaceableStoredItemSaveData> storedItems = new List<PlaceableStoredItemSaveData>();
}

[System.Serializable]
public class WorldShopFurnitureSaveData
{
    public string zoneId;
    public string definitionId;
    public string instanceId;
    public int gridX;
    public int gridY;
    public int rotationQuarterTurns;
    public bool isFixed;
    public bool recovered;
    public string functionalState;
    public List<PlaceableStoredItemSaveData> storedItems = new List<PlaceableStoredItemSaveData>();
}

[System.Serializable]
public class WorldResourceStateSaveData
{
    public string spawnKey;
    public bool consumed;
    public int respawnDay;
}

// §4 FriendshipService 저장 DTO — FriendshipService.ForceSetPoints 로 복원한다.
[System.Serializable]
public class FriendshipRecord
{
    public string friendshipId;
    public int    points;
    public int    lastDialogueDay;
}

// v4 HiringService DTO. candidateAssetName is loaded from Resources/Candidates.
[System.Serializable]
public class HiredNpcRecord
{
    public string hiredNpcId;
    public string candidateAssetName;
    public string npcObjectName;

    public bool hasTransform;
    public Vector3 position;
    public Quaternion rotation;

    // Legacy v3 names kept so old JSON can migrate without data loss.
    public Vector3 spawnPosition;
    public Quaternion spawnRotation;

    public string activeFsm;
    public string consumerFsmState;
    public string producerFsmState;
    public string specialistFsmState;
}

[System.Serializable]
public class SalesDecisionDaySaveData
{
    public int gameDay;
    public int purchases;
    public int rejections;
}

[System.Serializable]
public class FarmPlotSaveData
{
    public string plotId;
    public bool planted;
    public int currentStageIndex;
    public float secondsUntilNextStage;
}

// 인벤토리/핫바 한 칸을 직렬화한 DTO.
// ItemRegistry.Find(itemId, itemName) 로 Item 원형을 복원한다.
[System.Serializable]
public class SlotSaveData
{
    public int    itemId;       // Item.id (복원 1차 키)
    public string itemName;    // Item.itemName (폴백 키)
    public int    count;       // 0 = 빈 칸
    public float  quality;
    public int    currentPrice;
}

[System.Serializable]
public class ShopSlotSaveData
{
    public int slotIndex;
    public string slotKey;
    public bool occupied;
    public int displayPrice;
    public int itemId;
    public string itemName;
    public int count;
    public float quality;
    public int currentPrice;
}

[System.Serializable]
public class PlaceableSaveData
{
    public string zoneId;
    public string definitionId;
    public string instanceId;
    public int gridX;
    public int gridY;
    public int rotationQuarterTurns;
    public bool isFixed;
    public bool recovered;
    public string functionalState;
    public List<PlaceableStoredItemSaveData> storedItems = new List<PlaceableStoredItemSaveData>();
}

[System.Serializable]
public class PlaceableStoredItemSaveData
{
    public int itemId;
    public string itemName;
    public int count;
    public float quality;
    public int currentPrice;
}

[System.Serializable]
public class BuildingSaveData
{
    public string buildingName; // 무슨 건물인지 (이름으로 식별)
    public Vector3 position;    // 어디에 있는지
    public Quaternion rotation; // 어느 방향인지

    public BuildingSaveData(string name, Vector3 pos, Quaternion rot)
    {
        buildingName = name;
        position = pos;
        rotation = rot;
    }
}
