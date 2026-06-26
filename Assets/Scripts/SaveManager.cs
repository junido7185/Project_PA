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

    private const string SaveKey = "savegame";

    // 현재 스키마 버전. 새 필드 추가 시 올리고 MigrateSaveData() 에 마이그레이션 추가.
    private const int CurrentSaveVersion = 8;

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

        var firstDay = FindFirstObjectByType<PlayableDayScenarioController>();
        if (firstDay != null)
        {
            data.playerName = firstDay.PlayerName;
            data.selectedMapId = firstDay.SelectedMapId;
            data.firstDayPrototypeStage = firstDay.CurrentStageIndex;
        }

        var longPlay = FindFirstObjectByType<LongPlayProgressionController>();
        if (longPlay != null)
            longPlay.WriteSaveFields(data);

        // CDN/IL — 당일 채집(낮 재고 준비) 완료 상태 직렬화.
        var dayLoop = DayNightShopLoopController.Instance ?? FindFirstObjectByType<DayNightShopLoopController>();
        if (dayLoop != null)
            dayLoop.WriteSaveFields(data);

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

        // 4-a. ShopSlot 진열 상태 — v5
        data.shopSlots = SerializeShopSlots();

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

        data.version = CurrentSaveVersion;

        string json = JsonUtility.ToJson(data, true);
        await _repository.SaveAsync(SaveKey, json);
        Debug.Log($"💾 저장 완료 (건물 {data.buildings.Count}개, 인벤토리 {data.inventorySlots.Count}칸, 핫바 {data.hotbarSlots.Count}칸, 진열대 {data.shopSlots.Count}칸)");
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

        var longPlay = FindFirstObjectByType<LongPlayProgressionController>();
        if (longPlay != null)
            longPlay.RestoreSavedSession(data.longPlayLastSupplyDay, data.longPlayDayStartRevenue, data.longPlayDayStartMoney);

        // CDN/IL — 당일 채집 완료 상태 복원(저장된 날과 현재 날이 같을 때만 유지).
        var dayLoop = DayNightShopLoopController.Instance ?? FindFirstObjectByType<DayNightShopLoopController>();
        if (dayLoop != null)
            dayLoop.RestoreSavedState(data.dayPrepCollectedDay, data.dayPrepCollectedActivities);

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
            if (data.inventorySlots != null && data.inventorySlots.Count > 0)
                DeserializeSlots(data.inventorySlots, Inventory.instance.slots);

            if (Inventory.instance.hotbar != null
                && data.hotbarSlots != null && data.hotbarSlots.Count > 0)
                DeserializeSlots(data.hotbarSlots, Inventory.instance.hotbar.slots);

            Inventory.instance.RefreshAllUI();
        }

        DeserializeShopSlots(data.shopSlots);

        RestoreHiredNpcs(data.hiredNpcs);

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

        return data;
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

    void DeserializeShopSlots(List<ShopSlotSaveData> saved)
    {
        if (saved == null || saved.Count == 0) return;

        var slots = GetOrderedShopSlots();
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
