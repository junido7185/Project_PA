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

        string json = JsonUtility.ToJson(data, true);
        await _repository.SaveAsync(SaveKey, json);
        Debug.Log($"💾 저장 완료 ({data.buildings.Count}개 건물)");
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

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = data.playerPosition;
            if (cc != null) cc.enabled = true;
        }

        // 3. 기존 건물 제거 — 레지스트리가 보유한 목록만 정확히 파괴한다.
        if (BuildingRegistry.Instance != null)
        {
            BuildingRegistry.Instance.ClearAll();
        }

        // 4. 건물 다시 짓기
        int count = 0;
        foreach (BuildingSaveData bData in data.buildings)
        {
            BuildingData bd = allBuildingTypes.Find(x => x.prefab != null && x.prefab.name == bData.buildingName);
            if (bd != null)
            {
                var go = Instantiate(bd.prefab, bData.position, bData.rotation);
                if (BuildingRegistry.Instance != null)
                {
                    BuildingRegistry.Instance.Register(bd, go);
                }
                count++;
            }
            else
            {
                Debug.LogWarning($"❓ 도감에서 찾을 수 없는 건물: {bData.buildingName}");
            }
        }

        Debug.Log($"📂 로드 완료! (복구된 건물: {count}개)");
    }

    // 기존 동기 API 호환 — 핫키(F5/F9) 외에 외부에서 호출하는 코드가 있을 수 있어 유지.
    public void SaveGame() => _ = SaveGameAsync();
    public void LoadGame() => _ = LoadGameAsync();
}
