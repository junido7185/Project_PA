using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum SlotOwner { Inventory, Hotbar }

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image icon;
    public TextMeshProUGUI countText;

    Inventory inventory;
    InventoryUI inventoryUI;
    Hotbar hotbar;
    HotbarUI hotbarUI;

    public int index;
    public SlotOwner owner;
    GameObject dragIcon;
    RectTransform dragRT;
    public ItemTooltip tooltip;

    void Update()
    {
        if (tooltip != null && tooltip.gameObject.activeSelf)
        {
            Vector2 mousePos = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : Vector2.zero;
            tooltip.UpdatePosition(mousePos);
        }
    }

    public void Setup(Inventory inv, Hotbar hb, int idx, InventoryUI ui)
    {
        inventory = inv; hotbar = hb; index = idx; inventoryUI = ui;
        owner = SlotOwner.Inventory; hotbarUI = null;
    }

    public void SetupHotbar(Hotbar hb, Inventory inv, int idx, HotbarUI ui)
    {
        hotbar = hb; inventory = inv; hotbarUI = ui; index = idx;
        owner = SlotOwner.Hotbar; inventoryUI = null;
    }

    public InventorySlot GetSlot()
    {
        return owner == SlotOwner.Inventory ? inventory.slots[index] : hotbar.slots[index];
    }

    public void SetSlot(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty)
        {
            icon.enabled = false; countText.text = "";
        }
        else
        {
            icon.enabled = true; icon.sprite = slot.item.icon;
            countText.text = slot.count > 1 ? slot.count.ToString() : "";
        }
    }

    // --- 드래그 로직 ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        var s = GetSlot();
        if (s.IsEmpty) return;

        bool shiftHeld = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
        int amount = (eventData.button == PointerEventData.InputButton.Right)
            ? Mathf.CeilToInt(s.count / 2f)
            : (shiftHeld ? 1 : s.count);

        DragContext.draggedItem = s.item;
        DragContext.draggedCount = amount;
        DragContext.fromSlotIndex = index;
        DragContext.fromOwner = owner;

        s.AddCount(-amount);

        Transform parentLayer = owner == SlotOwner.Inventory ? inventoryUI.dragLayer : hotbarUI.dragLayer;
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(parentLayer, false);
        dragRT = dragIcon.AddComponent<RectTransform>();
        var img = dragIcon.AddComponent<Image>();
        img.sprite = icon.sprite; img.raycastTarget = false;
        dragRT.sizeDelta = icon.rectTransform.sizeDelta;

        RefreshAllUIs();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null) dragRT.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null) Destroy(dragIcon);

        if (DragContext.draggedItem != null && DragContext.draggedCount > 0)
        {
            ReturnToOriginalSlot();
        }
        RefreshAllUIs();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (DragContext.draggedItem == null) return;
        var targetSlot = GetSlot();

        if (targetSlot.IsEmpty)
        {
            targetSlot.Set(DragContext.draggedItem, DragContext.draggedCount);
        }
        else if (targetSlot.item == DragContext.draggedItem)
        {
            int space = targetSlot.item.maxStack - targetSlot.count;
            int add = Mathf.Min(space, DragContext.draggedCount);
            targetSlot.AddCount(add);
            DragContext.draggedCount -= add;
            if (DragContext.draggedCount > 0) ReturnToOriginalSlot();
        }
        else
        {
            // Swap — ItemInstance 참조 자체를 교환해 동적 상태(quality 등)를 보존한다.
            var orig = GetOriginalSlot();
            var targetInstance = targetSlot.instance;
            targetSlot.Set(DragContext.draggedItem, DragContext.draggedCount);
            orig.SetInstance(targetInstance);
        }

        DragContext.draggedItem = null; DragContext.draggedCount = 0;
        RefreshAllUIs();
    }

    InventorySlot GetOriginalSlot()
    {
        return DragContext.fromOwner == SlotOwner.Inventory ? inventory.slots[DragContext.fromSlotIndex] : hotbar.slots[DragContext.fromSlotIndex];
    }
    void ReturnToOriginalSlot()
    {
        var s = GetOriginalSlot();
        if (s.IsEmpty) s.Set(DragContext.draggedItem, DragContext.draggedCount);
        else if (s.item == DragContext.draggedItem) s.AddCount(DragContext.draggedCount);
        DragContext.draggedItem = null;
    }
    void RefreshAllUIs()
    {
        if (inventoryUI != null) inventoryUI.RefreshUI();
        if (hotbarUI != null) hotbarUI.RefreshUI();
    }
    public void OnPointerEnter(PointerEventData eventData) { if (GetSlot()?.item != null) tooltip.Show(GetSlot().item, eventData.position); }
    public void OnPointerExit(PointerEventData eventData) { tooltip.Hide(); }
}