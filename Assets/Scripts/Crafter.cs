using UnityEngine;

public class Crafter : MonoBehaviour
{
    public RecipeData recipe; // 이 작업대에서 사용할 레시피 (Inspector에서 연결)

    public void Craft()
    {
        Inventory inventory = Inventory.instance;

        // 1. 재료가 충분한지 검사 (직접 개수 세기)
        int currentCount = 0;
        foreach (ItemData item in inventory.items)
        {
            if (item == recipe.inputItem) // 이름 비교가 아니라 데이터 원본 비교
            {
                currentCount++;
            }
        }

        if (currentCount < recipe.inputCount)
        {
            Debug.Log("🚫 재료가 부족합니다! (필요: " + recipe.inputItem.itemName + ")");
            return;
        }

        // 2. 재료 소모 (리스트에서 제거)
        // 리스트를 앞에서부터 지우면 인덱스가 꼬이므로, 필요한 개수만큼만 찾아서 지움
        int removeCount = 0;
        // 리스트를 역순으로 순회하거나, 별도 제거 로직 필요. 여기선 간단히 구현:
        for (int i = inventory.items.Count - 1; i >= 0; i--)
        {
            if (inventory.items[i] == recipe.inputItem)
            {
                inventory.items.RemoveAt(i);
                removeCount++;
                if (removeCount >= recipe.inputCount) break; // 다 지웠으면 탈출
            }
        }

        // 3. 결과물 지급
        for (int i = 0; i < recipe.outputCount; i++)
        {
            inventory.AddItem(recipe.outputItem);
        }

        Debug.Log("🔨 제작 성공! " + recipe.outputItem.itemName + " 획득.");
    }
}