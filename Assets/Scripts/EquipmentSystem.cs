using UnityEngine;

public class EquipmentSystem : MonoBehaviour
{
    // Inspector에서 연결할 장비 모델들
    public GameObject axeModel;     // 도끼 오브젝트 (Equip_Axe)
    public GameObject pickaxeModel; // 나중에 곡괭이도 생기면 추가

    void Start()
    {
        // 인벤토리 슬롯 변경될 때마다 내 함수(UpdateEquipment) 실행해달라고 등록
        Inventory.instance.onSlotChangedCallback += UpdateEquipment;
        
        // 시작할 때 한 번 실행 (초기화)
        UpdateEquipment(Inventory.instance.selectedSlotIndex);
    }

    void UpdateEquipment(int slotIndex)
    {
        if (axeModel != null) axeModel.SetActive(false);
        if (pickaxeModel != null) pickaxeModel.SetActive(false); // 👇 주석 해제

        ItemData item = Inventory.instance.GetSelectedItem();
        if (item == null) return;

        if (item.toolType == ToolType.Axe)
        {
            if (axeModel != null) axeModel.SetActive(true);
        }
        else if (item.toolType == ToolType.Pickaxe) // 👇 주석 해제 및 로직 활성화
        {
            if (pickaxeModel != null) pickaxeModel.SetActive(true);
            Debug.Log("⛏️ 곡괭이 장착!");
        }
    }
}