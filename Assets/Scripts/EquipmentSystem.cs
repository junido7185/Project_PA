using UnityEngine;

public class EquipmentSystem : MonoBehaviour
{
    public GameObject axeModel;     
    public GameObject pickaxeModel; 

    void Start()
    {
        // ⭐ 인벤토리 이벤트 이름이 바뀌었을 수 있으니 다시 연결
        if (Inventory.instance != null)
            Inventory.instance.onItemChangedCallback += RefreshEquipment;
    }

    void OnDestroy()
    {
        if (Inventory.instance != null)
            Inventory.instance.onItemChangedCallback -= RefreshEquipment;
    }

    // Inventory.cs에서 RefreshAllUI()가 호출될 때 같이 실행됨
    void RefreshEquipment()
    {
        // 1. 모델 끄기
        if (axeModel != null) axeModel.SetActive(false);
        if (pickaxeModel != null) pickaxeModel.SetActive(false);

        BuildManager buildMgr = BuildManager.instance;

        // 2. 현재 든 아이템 확인
        if (Inventory.instance == null) return;
        Item item = Inventory.instance.GetSelectedItem();
        if (item == null)
        {
            if (buildMgr != null) buildMgr.StopBuildMode();
            return;
        }

        // --- 도구 모델 켜기 ---
        if (item.toolType == ToolType.Axe && axeModel != null) axeModel.SetActive(true);
        if (item.toolType == ToolType.Pickaxe && pickaxeModel != null) pickaxeModel.SetActive(true);

        // --- 건설 아이템이면 건설 모드 켜기 ---
        if (item.buildingToBuild != null && buildMgr != null)
        {
            if (buildMgr.currentBuilding != item.buildingToBuild)
                buildMgr.SetBuildMode(item.buildingToBuild);
        }
        else if (buildMgr != null)
        {
            buildMgr.StopBuildMode();
        }
    }
    
    // 핫바 UI에서 휠 돌릴 때마다 호출해주면 반응 속도가 더 빠름
    void Update()
    {
        // (최적화를 위해 매 프레임 체크하기보다 이벤트 방식 권장하지만, 
        // 핫바 변경 타이밍을 확실히 잡기 위해 Update에서 체크해도 됨)
        RefreshEquipment();
    }
}
