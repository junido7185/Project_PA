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

    // 선택된 슬롯 번호 (0부터 시작)
    public int selectedSlotIndex = 0; 
    
    // 슬롯 변경 이벤트 (UI 갱신용)
    public delegate void OnSlotChanged(int index);
    public OnSlotChanged onSlotChangedCallback;

    // 👇 추가: 테스트용 시작 아이템 목록
    public List<ItemData> startingItems;

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("인벤토리가 2개 이상입니다!");
            return;
        }
        instance = this;
    }

    void Start() // Awake 대신 Start에 넣거나 Awake 밑에 추가
    {
        // 시작 아이템 지급
        foreach (ItemData item in startingItems)
        {
            AddItem(item);
        }
    }

    void Update()
    {
        // 키보드 숫자키 1~8 입력 감지
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SelectSlot(5);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SelectSlot(6);
        if (Input.GetKeyDown(KeyCode.Alpha8)) SelectSlot(7);
    }

    void SelectSlot(int index)
    {
        selectedSlotIndex = index;
        Debug.Log("👉 슬롯 선택: " + (index + 1) + "번");

        // UI 갱신 요청
        if (onSlotChangedCallback != null)
            onSlotChangedCallback.Invoke(index);
    }

    // 현재 들고 있는 아이템 데이터 반환 (없으면 null)
    public ItemData GetSelectedItem()
    {
        if (selectedSlotIndex < items.Count)
        {
            return items[selectedSlotIndex];
        }
        return null;
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