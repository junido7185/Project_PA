using UnityEngine;

public class Shop : MonoBehaviour
{
    // 판매 함수 (외부에서 호출)
    public void SellAllItems()
    {
        Inventory inventory = Inventory.instance;

        // 1. 인벤토리가 비었는지 확인
        if (inventory.items.Count == 0)
        {
            Debug.Log("🚫 팔 물건이 없습니다!");
            return;
        }

        int totalEarnings = 0; // 총 판매 수익금

        // 2. 인벤토리의 모든 아이템 가격 합산
        foreach (ItemData item in inventory.items)
        {
            totalEarnings += item.basePrice;
        }

        // 3. 돈 지급
        GameManager.instance.AddMoney(totalEarnings);
        Debug.Log("💵 정산 완료! 수익: " + totalEarnings + "G");

        // 4. 인벤토리 비우기
        inventory.items.Clear();
        
        // 5. UI 갱신 요청 (초인종 누르기)
        if (inventory.onItemChangedCallback != null)
        {
            inventory.onItemChangedCallback.Invoke();
        }
    }
}