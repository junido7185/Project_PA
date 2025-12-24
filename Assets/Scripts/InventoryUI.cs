using UnityEngine;
using UnityEngine.UI; // UI를 건드리기 위해 필수!

public class InventoryUI : MonoBehaviour
{
    public Transform itemsParent;   // 슬롯들이 들어갈 부모 객체 (InventoryPanel)
    public GameObject slotPrefab;   // 슬롯 디자인 (Slot_Prefab)

    Inventory inventory;

    void Start()
    {
        inventory = Inventory.instance;
        
        // 인벤토리의 초인종(이벤트)에 내 함수(UpdateUI)를 등록한다.
        // 즉, "아이템이 들어오면 UpdateUI를 실행해줘!"라고 예약하는 것.
        inventory.onItemChangedCallback += UpdateUI;
    }

    // 화면 갱신 함수
    void UpdateUI()
    {
        Debug.Log("🖥️ UI 갱신 시작!");

        // 1. 기존에 그려진 슬롯 싹 지우기 (초기화)
        foreach (Transform child in itemsParent)
        {
            Destroy(child.gameObject);
        }

        // 2. 현재 인벤토리 리스트만큼 슬롯 새로 만들기
        for (int i = 0; i < inventory.items.Count; i++)
        {
            // 슬롯 생성 (Prefab 복제)
            GameObject newSlot = Instantiate(slotPrefab, itemsParent);
            
            // 슬롯 안의 Icon 이미지 찾아서 바꾸기
            ItemData item = inventory.items[i];
            Image iconImage = newSlot.transform.Find("Icon").GetComponent<Image>();
            
            if (item.icon != null)
            {
                iconImage.sprite = item.icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false; // 아이콘 없으면 숨김
            }
        }
    }
}