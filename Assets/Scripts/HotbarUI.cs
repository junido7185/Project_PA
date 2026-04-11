using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    public Hotbar    hotbar;
    public Inventory inventory;
    public Transform slotParent;
    public GameObject slotPrefab;
    public ItemTooltip tooltip;
    public Transform toolsParent;

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

        // Input System 이벤트 구독
        if (PlayerInputHandler.Instance != null)
        {
            PlayerInputHandler.Instance.OnHotbarDirectSelect += SelectSlot;
            PlayerInputHandler.Instance.OnHotbarScroll       += HandleScroll;
        }
    }

    void OnDestroy()
    {
        if (PlayerInputHandler.Instance != null)
        {
            PlayerInputHandler.Instance.OnHotbarDirectSelect -= SelectSlot;
            PlayerInputHandler.Instance.OnHotbarScroll       -= HandleScroll;
        }
    }

    // -------- 입력 핸들러 --------

    private void SelectSlot(int index)
    {
        if (index < 0 || index >= hotbar.size) return;
        selectedIndex = index;
        RefreshUI();
    }

    private void HandleScroll(float scrollY)
    {
        // scrollY 양수 = 아래 방향 휠 → 다음(Next) 슬롯
        if (scrollY > 0f)
            selectedIndex = (selectedIndex + 1) % hotbar.size;
        else
            selectedIndex = (selectedIndex - 1 + hotbar.size) % hotbar.size;

        RefreshUI();
    }

    // -------- UI 갱신 --------

    public void RefreshUI()
    {
        if (inventory != null)
            inventory.selectedHotbarIndex = selectedIndex;

        for (int i = 0; i < hotbar.size; i++)
        {
            slotUIs[i].SetSlot(hotbar.slots[i]);

            var bg = slotUIs[i].transform.GetChild(0).GetComponent<Image>();
            bg.color = (i == selectedIndex) ? Color.yellow : Color.white;
        }

        UpdateCharacterModel();
    }

    void UpdateCharacterModel()
    {
        if (toolsParent == null) return;

        InventorySlot slot = slotUIs[selectedIndex].GetComponent<InventorySlotUI>().GetSlot();

        for (int x = 0; x < toolsParent.childCount; x++) Destroy(toolsParent.GetChild(x).gameObject);

        if (slot == null || slot.IsEmpty || slot.item.model == null) return;
        Instantiate(slot.item.model, toolsParent);
    }
}
