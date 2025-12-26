using UnityEngine;

public class Gatherable : MonoBehaviour
{
    public ItemData dropItem; // 이 자원을 캐면 나올 아이템 데이터
    public ToolType requiredTool = ToolType.None;     // 채집에 필요한 도구

    // 채집 당했을 때 호출될 함수
    public void Harvest()
    {
        if (dropItem != null)
        {
            // 인벤토리(싱글톤)에 아이템 추가
            Inventory.instance.AddItem(dropItem);
        }
        
        // 오브젝트 삭제
        Destroy(gameObject);
    }
}