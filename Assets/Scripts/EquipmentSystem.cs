using UnityEngine;

public class EquipmentSystem : MonoBehaviour
{
    public GameObject axeModel;     
    public GameObject pickaxeModel; 

    void Start()
    {
        Inventory.instance.onSlotChangedCallback += UpdateEquipment;
        UpdateEquipment(Inventory.instance.selectedSlotIndex);
    }

    void UpdateEquipment(int slotIndex)
    {
        // 1. 모델 초기화 (도끼/곡괭이 끄기)
        if (axeModel != null) axeModel.SetActive(false);
        if (pickaxeModel != null) pickaxeModel.SetActive(false);

        // 2. 일단 건설 모드 끄기 (기본값)
        // (BuildManager가 내 몸에 붙어있으니 GetComponent로 바로 찾음)
        BuildManager buildMgr = GetComponent<BuildManager>();
        if (buildMgr != null) buildMgr.StopBuildMode();

        // 3. 아이템 데이터 확인
        ItemData item = Inventory.instance.GetSelectedItem();
        if (item == null) return; // 빈손

        // --- 도구 모델 켜기 ---
        if (item.toolType == ToolType.Axe && axeModel != null) axeModel.SetActive(true);
        if (item.toolType == ToolType.Pickaxe && pickaxeModel != null) pickaxeModel.SetActive(true);

        // --- ⭐ 건설 아이템이면 건설 모드 켜기 ---
        if (item.buildingToBuild != null && buildMgr != null)
        {
            buildMgr.SetBuildMode(item.buildingToBuild);
        }
    }
}