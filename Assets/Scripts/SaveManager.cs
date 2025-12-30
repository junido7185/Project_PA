using UnityEngine;
using System.IO; 
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;
    string savePath;

    // 도감 (여기에 Data_Floor 등이 들어있어야 함)
    public List<BuildingData> allBuildingTypes; 

    void Awake()
    {
        instance = this;
        savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) SaveGame();
        if (Input.GetKeyDown(KeyCode.F9)) LoadGame();
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();

        // 1. 플레이어 정보
        data.money = GameManager.instance.money;
        data.playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;

        // 2. 건물 정보 (태그로 찾기)
        GameObject[] buildings = GameObject.FindGameObjectsWithTag("Building");
        
        foreach (GameObject b in buildings)
        {
            // 이름 뒤에 붙는 "(Clone)" 제거 -> "Building_Floor"만 남음
            string realName = b.name.Replace("(Clone)", "").Trim();
            data.buildings.Add(new BuildingSaveData(realName, b.transform.position, b.transform.rotation));
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);

        Debug.Log($"💾 저장 완료 ({buildings.Length}개 건물): {savePath}");
    }

    public void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("📂 저장된 파일이 없습니다.");
            return;
        }

        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // 1. 플레이어 복구
        GameManager.instance.money = 0; 
        GameManager.instance.AddMoney(data.money);
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) 
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if(cc != null) cc.enabled = false; // 강제 이동을 위해 잠시 끄기
            player.transform.position = data.playerPosition;
            if(cc != null) cc.enabled = true;
        }

        // 2. 기존 건물 삭제 (중복 방지)
        GameObject[] existings = GameObject.FindGameObjectsWithTag("Building");
        foreach (GameObject b in existings) Destroy(b);

        // 3. 건물 다시 짓기
        int count = 0;
        foreach (BuildingSaveData bData in data.buildings)
        {
            // ⭐ [핵심 수정] 한글 이름(buildingName)이 아니라 '프리팹 이름(prefab.name)'으로 찾습니다!
            // 저장된 이름("Building_Floor") == 프리팹 파일 이름("Building_Floor")
            BuildingData bd = allBuildingTypes.Find(x => x.prefab.name == bData.buildingName);
            
            if (bd != null)
            {
                Instantiate(bd.prefab, bData.position, bData.rotation);
                count++;
            }
            else
            {
                Debug.LogWarning($"❓ 도감에서 찾을 수 없는 건물: {bData.buildingName}");
            }
        }

        Debug.Log($"📂 로드 완료! (복구된 건물: {count}개)");
    }
}