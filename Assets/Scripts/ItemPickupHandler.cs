using UnityEngine;

public class ItemPickupHandler : MonoBehaviour
{
    public Hotbar hotbar;
    public Inventory inventory;

    public void PickupItem(Item item, int amount = 1)
    {
        // 1. 핫바 먼저 시도
        bool addedToHotbar = hotbar.AddItem(item, amount);

        if (!addedToHotbar)
        {
            // 2. 인벤토리 시도
            bool addedToInventory = inventory.AddItem(item, amount);
            if (!addedToInventory)
            {
                Debug.Log("가방이 꽉 찼습니다!");
                return;
            }
        }
        
        // UI 갱신 (싱글톤이 아닌 Find로 찾음 - 안전장치)
        if(InventoryUI.instance) InventoryUI.instance.RefreshUI(); // 이 부분을 위해 InventoryUI에 static instance 추가 필요할 수도 있음.
        else inventory.RefreshAllUI();
    }
}