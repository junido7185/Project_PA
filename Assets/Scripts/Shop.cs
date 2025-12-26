using UnityEngine;

public class Shop : MonoBehaviour
{
    // 판매 함수 (외부에서 호출)
    public void SellAllItems()
    {
        Inventory inventory = Inventory.instance;

        if (inventory.items.Count == 0)
        {
            Debug.Log("🚫 팔 물건이 없습니다!");
            return;
        }

        int totalEarnings = 0;
        int soldCount = 0; // 몇 개 팔았는지 세기

        // ⭐ 리스트를 수정(삭제)할 때는 '뒤에서부터' 반복하긔
        // (앞에서부터 지우면 인덱스가 당겨져서 건너뛰는 아이템이 생기거든요)
        for (int i = inventory.items.Count - 1; i >= 0; i--)
        {
            ItemData item = inventory.items[i];

            // 🛑 중요: 도구(ToolType이 None이 아닌 것)는 팔지 않고 건너뜀!
            if (item.toolType != ToolType.None)
            {
                continue; 
            }

            // 판매 로직
            totalEarnings += item.basePrice;
            inventory.items.RemoveAt(i); // 리스트에서 해당 아이템만 쏙 뺌
            soldCount++;
        }

        if (soldCount > 0)
        {
            GameManager.instance.AddMoney(totalEarnings);
            Debug.Log("💵 정산 완료! " + soldCount + "개 판매, 수익: " + totalEarnings + "G");

            // UI 갱신 요청
            if (inventory.onItemChangedCallback != null)
            {
                inventory.onItemChangedCallback.Invoke();
            }
        }
        else
        {
            Debug.Log("🚫 판매할 수 있는 아이템(자원)이 없습니다.");
        }
    }
}