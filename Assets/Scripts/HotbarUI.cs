using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    public Hotbar hotbar;
    public Inventory inventory;
    public Transform slotParent;
    public GameObject slotPrefab;
    public ItemTooltip tooltip;
    public Transform toolsParent; // 캐릭터 손 위치 (모델 보여주기용)

    private List<InventorySlotUI> slotUIs = new();
    private int selectedIndex = 0;

    public RectTransform dragLayer;
    public Canvas rootCanvas;

    void Start()
    {
        foreach (Transform child in slotParent) Destroy(child.gameObject);

        for (int i = 0; i < hotbar.size; i++)
        {
            var go = Instantiate(slotPrefab, slotParent);
            var ui = go.GetComponent<InventorySlotUI>();
            ui.tooltip = tooltip;
            ui.SetupHotbar(hotbar, inventory, i, this);
            slotUIs.Add(ui);
        }

        RefreshUI();
    }

    void Update()
    {
        // 숫자키 입력 (1~9)
        for (int i = 0; i < hotbar.size; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                selectedIndex = i;
                RefreshUI();
            }
        }

        // 마우스 휠 입력
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            selectedIndex = (selectedIndex + 1) % hotbar.size;
            RefreshUI();
        }
        else if (scroll < 0f)
        {
            selectedIndex = (selectedIndex - 1 + hotbar.size) % hotbar.size;
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        // ⭐ [중요] 인벤토리 매니저에게 현재 선택된 핫바 번호를 알림
        if (inventory != null)
        {
            inventory.selectedHotbarIndex = selectedIndex;
        }

        for (int i = 0; i < hotbar.size; i++)
        {
            slotUIs[i].SetSlot(hotbar.slots[i]);

            // 선택된 슬롯 강조 (노란색 배경)
            var bg = slotUIs[i].transform.GetChild(0).GetComponent<Image>();
            bg.color = (i == selectedIndex) ? Color.yellow : Color.white;
        }
        
        UpdateCharacterModel();
    }
    
    // 캐릭터 손에 모델 들려주기
    void UpdateCharacterModel()
    {
        if (toolsParent == null) return;
        
        InventorySlot slot = slotUIs[selectedIndex].GetComponent<InventorySlotUI>().GetSlot();
        
        // 기존 모델 삭제
        for (int x = 0; x < toolsParent.childCount; x++) Destroy(toolsParent.GetChild(x).gameObject);

        if (slot == null || slot.IsEmpty) return;
        if (slot.item.model == null) return;

        // 새 모델 생성
        Instantiate(slot.item.model, toolsParent);
    }
}