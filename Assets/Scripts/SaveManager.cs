using UnityEngine;
using System.Collections.Generic;

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

    private const string SaveKey = "savegame";

    // 현재 스키마 버전. 새 필드 추가 시 올리고 MigrateSaveData() 에 마이그레이션 추가.
    private const int CurrentSaveVersion = 2;

    void Awake()
    {
        instance = this;
        _repository = new LocalJsonSaveRepository();
    }

    void Start()
    {
        if (PlayerInputHandler.Instance != null)
        {
            PlayerInputHandler.Instance.OnSave += () => _ = SaveGameAsync();
            PlayerInputHandler.Instance.OnLoad += () => _ = LoadGameAsync();
        }
    }

    public async System.Threading.Tasks.Task SaveGameAsync()
    {
        SaveData data = new SaveData();

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
        if (playerGo != null) data.playerPosition = playerGo.transform.position;

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
                data.hotbarSlots = SerializeSlots(Inventory.instance.hotbar.slots);
        }

        // 5. 감사 시스템
        data.lastAuditDay = AuditService.Instance != null ? AuditService.Instance.LastAuditDay : 0;

        // 버전 스탬프
        data.version = CurrentSaveVersion;

        string json = JsonUtility.ToJson(data, true);
        await _repository.SaveAsync(SaveKey, json);
        Debug.Log($"💾 저장 완료 (건물 {data.buildings.Count}개, 인벤토리 {data.inventorySlots.Count}칸, 핫바 {data.hotbarSlots.Count}칸)");
    }

    public async System.Threading.Tasks.Task LoadGameAsync()
    {
        string json = await _repository.LoadAsync(SaveKey);
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("📂 저장된 파일이 없습니다.");
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // 버전 마이그레이션
        if (data.version < CurrentSaveVersion)
        {
            data = MigrateSaveData(data);
            Debug.Log($"💾 세이브 마이그레이션 완료: v{data.version}");
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
        if (GameClock.Instance != null)
        {
            GameClock.Instance.ForceSet(data.gameHour, data.gameDay, "SaveManager.LoadGame");
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = data.playerPosition;
            if (cc != null) cc.enabled = true;
        }

        // 3-a. 감사 시스템 복구
        if (AuditService.Instance != null)
            AuditService.Instance.ForceSetLastAuditDay(data.lastAuditDay);

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
            if (data.inventorySlots != null && data.inventorySlots.Count > 0)
                DeserializeSlots(data.inventorySlots, Inventory.instance.slots);

            if (Inventory.instance.hotbar != null
                && data.hotbarSlots != null && data.hotbarSlots.Count > 0)
                DeserializeSlots(data.hotbarSlots, Inventory.instance.hotbar.slots);

            Inventory.instance.RefreshAllUI();
        }

        Debug.Log($"📂 로드 완료! (건물 {count}개, 인벤토리/핫바 복구)");
    }

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

        return data;
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
}
