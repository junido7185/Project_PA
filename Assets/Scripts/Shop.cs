using UnityEngine;

public class Shop : MonoBehaviour, IInteractable
{
    public void Interact(GameObject interactor)
    {
        Debug.Log("🏪 상점 주인: 어서오세요!");
        SellAllItems(); 
    }

    public string GetInteractPrompt()
    {
        return "모두 판매하기"; 
    }
    
    public void SellAllItems()
    {
        Inventory inventory = Inventory.instance;
        int totalEarnings = 0;
        int soldCount = 0;

        // 1. 인벤토리 슬롯 검사
        soldCount += SellFromSlots(inventory.slots, ref totalEarnings);

        // 2. 핫바 슬롯 검사 (옵션: 핫바도 팔고 싶다면)
        if (inventory.hotbar != null)
        {
            soldCount += SellFromSlots(inventory.hotbar.slots, ref totalEarnings);
        }

        if (soldCount > 0)
        {
            GameManager.instance.AddMoney(totalEarnings);
            Debug.Log($"💵 정산 완료! {soldCount}개 판매, 수익: {totalEarnings}G");
            
            // UI 갱신
            inventory.RefreshAllUI();
        }
        else
        {
            Debug.Log("🚫 판매할 수 있는 아이템(자원)이 없습니다.");
        }
    }

    // 슬롯 리스트를 돌면서 판매하는 내부 함수
    private int SellFromSlots(System.Collections.Generic.List<InventorySlot> slots, ref int earnings)
    {
        int count = 0;
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty && slot.item.toolType == ToolType.None) // 도구는 안 팜
            {
                earnings += slot.item.basePrice * slot.count;
                count += slot.count;

                // ⭐ 슬롯 비우기 (리스트 삭제가 아니라 내용을 null로 만듦)
                slot.item = null;
                slot.count = 0;
            }
        }
        return count;
    }
}