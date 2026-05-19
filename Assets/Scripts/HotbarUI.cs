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
        BindRuntimeReferences();
        if (hotbar == null || slotParent == null || slotPrefab == null)
        {
            Debug.LogWarning("HotbarUI: missing hotbar, slotParent, or slotPrefab.");
            return;
        }

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
        if (hotbar == null) return;
        if (index < 0 || index >= hotbar.size) return;
        selectedIndex = index;
        RefreshUI();
    }

    private void HandleScroll(float scrollY)
    {
        if (hotbar == null || hotbar.size <= 0) return;
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
        BindRuntimeReferences();
        if (hotbar == null || hotbar.slots == null) return;

        if (inventory != null)
            inventory.selectedHotbarIndex = selectedIndex;

        if (slotUIs == null) slotUIs = new List<InventorySlotUI>();
        if (slotUIs.Count == 0 && slotParent != null && slotPrefab != null)
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
        }

        for (int i = 0; i < hotbar.size; i++)
        {
            if (i >= slotUIs.Count || i >= hotbar.slots.Count) continue;
            slotUIs[i].SetSlot(hotbar.slots[i]);

            Image bg = null;
            if (slotUIs[i].transform.childCount > 0)
                bg = slotUIs[i].transform.GetChild(0).GetComponent<Image>();
            if (bg == null) bg = slotUIs[i].GetComponent<Image>();
            if (bg != null) bg.color = (i == selectedIndex) ? Color.yellow : Color.white;
        }

        UpdateCharacterModel();
    }

    void UpdateCharacterModel()
    {
        if (toolsParent == null) return;
        if (slotUIs == null || selectedIndex < 0 || selectedIndex >= slotUIs.Count) return;

        InventorySlot slot = slotUIs[selectedIndex].GetComponent<InventorySlotUI>().GetSlot();

        for (int x = 0; x < toolsParent.childCount; x++) Destroy(toolsParent.GetChild(x).gameObject);

        if (slot == null || slot.IsEmpty || slot.item.model == null) return;
        Instantiate(slot.item.model, toolsParent);
    }

    void BindRuntimeReferences()
    {
        GameObject player = null;
        try { player = GameObject.FindGameObjectWithTag("Player"); } catch { }
        var playerHotbar = player != null ? player.GetComponent<Hotbar>() : null;
        var playerInventory = player != null ? player.GetComponent<Inventory>() : null;

        if (playerHotbar != null)
            hotbar = playerHotbar;
        else if (hotbar == null)
            hotbar = FindAnyObjectByType<Hotbar>();

        if (playerInventory != null)
            inventory = playerInventory;
        else if (inventory == null)
            inventory = Inventory.instance != null ? Inventory.instance : FindAnyObjectByType<Inventory>();
        if (inventory != null && inventory.hotbar == null && hotbar != null)
            inventory.hotbar = hotbar;
        if (selectedIndex < 0) selectedIndex = 0;
        if (hotbar != null && hotbar.size > 0)
            selectedIndex = Mathf.Clamp(selectedIndex, 0, hotbar.size - 1);
    }
}
