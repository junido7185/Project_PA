using System.Collections.Generic; // 리스트(List)를 쓰기 위해 필요
using UnityEngine;

public class Inventory : MonoBehaviour
{
    // 싱글톤(Singleton): 어디서든 Inventory.instance로 접근 가능하게 만듦
    public static Inventory instance;

    // 🔔 UI 갱신을 위한 '초인종' (델리게이트 이벤트)
    // "아이템이 변경되면 실행할 함수들을 등록하세요" 라는 뜻
    public delegate void OnItemChanged();
    public OnItemChanged onItemChangedCallback;

    // 아이템을 담을 리스트 (가방)
    public List<ItemData> items = new List<ItemData>();

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("인벤토리가 2개 이상입니다!");
            return;
        }
        instance = this;
    }

    // 아이템 추가 함수
    public void AddItem(ItemData item)
    {
        items.Add(item);
        Debug.Log("🎒 인벤토리에 추가됨: " + item.itemName + " (현재 " + items.Count + "개)");
        
        // 초인종 누르기! (등록된 UI가 있다면 갱신하라고 신호 보냄)
        if (onItemChangedCallback != null)
        {
            onItemChangedCallback.Invoke();
        }
    }
    
    // (나중에 아이템 제거, 정렬 기능 등 추가 예정)
}