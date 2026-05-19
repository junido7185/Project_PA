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
    public TextMeshProUGUI fallbackText;

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
        if (owner == SlotOwner.Inventory)
        {
            if (inventory == null || inventory.slots == null || index < 0 || index >= inventory.slots.Count) return null;
            return inventory.slots[index];
        }

        if (hotbar == null || hotbar.slots == null || index < 0 || index >= hotbar.slots.Count) return null;
        return hotbar.slots[index];
    }

    public void SetSlot(InventorySlot slot)
    {
        EnsureFallbackText();

        if (slot == null || slot.IsEmpty)
        {
            if (icon != null)
            {
                icon.enabled = false;
                icon.sprite = null;
                icon.color = Color.white;
            }
            if (countText != null) countText.text = "";
            if (fallbackText != null) fallbackText.text = "";
        }
        else
        {
            bool hasIcon = slot.item.icon != null;
            if (icon != null)
            {
                icon.enabled = true;
                icon.sprite = slot.item.icon;
                icon.color = hasIcon ? Color.white : new Color(0.92f, 0.82f, 0.58f, 0.95f);
            }

            if (fallbackText != null)
            {
                fallbackText.text = hasIcon ? "" : BuildFallbackLabel(slot);
                fallbackText.enabled = !hasIcon;
            }

            if (countText != null)
                countText.text = slot.count > 1 ? slot.count.ToString() : "";
        }
    }

    void EnsureFallbackText()
    {
        if (fallbackText != null) return;

        var go = new GameObject("FallbackItemText", typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(4f, 4f);
        rt.offsetMax = new Vector2(-4f, -4f);

        fallbackText = go.GetComponent<TextMeshProUGUI>();
        fallbackText.alignment = TextAlignmentOptions.Center;
        fallbackText.fontSize = 14f;
        fallbackText.fontStyle = FontStyles.Bold;
        fallbackText.color = new Color(0.18f, 0.12f, 0.08f, 1f);
        fallbackText.raycastTarget = false;
        fallbackText.textWrappingMode = TextWrappingModes.Normal;
        fallbackText.text = "";
    }

    static string BuildFallbackLabel(InventorySlot slot)
    {
        string name = slot?.item != null && !string.IsNullOrEmpty(slot.item.itemName)
            ? slot.item.itemName
            : "ITEM";
        if (name.Length > 6) name = name.Substring(0, 6);
        return name;
    }

    // --- 드래그 로직 ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        var s = GetSlot();
        if (s == null || s.IsEmpty) return;

        bool shiftHeld = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
        int amount = (eventData.button == PointerEventData.InputButton.Right)
            ? Mathf.CeilToInt(s.count / 2f)
            : (shiftHeld ? 1 : s.count);

        DragContext.draggedItem = s.item;
        DragContext.draggedInstance = new ItemInstance(s.item, amount)
        {
            quality = s.instance != null ? s.instance.quality : 1f,
            currentPrice = s.instance != null ? s.instance.currentPrice : 0
        };
        DragContext.draggedCount = amount;
        DragContext.fromSlotIndex = index;
        DragContext.fromOwner = owner;

        s.AddCount(-amount);

        Transform parentLayer = owner == SlotOwner.Inventory ? inventoryUI?.dragLayer : hotbarUI?.dragLayer;
        if (parentLayer == null) parentLayer = transform.root;
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

        if (DragContext.draggedInstance != null && DragContext.draggedInstance.count > 0)
        {
            ReturnToOriginalSlot();
        }
        RefreshAllUIs();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (DragContext.draggedInstance == null || DragContext.draggedInstance.data == null) return;
        var targetSlot = GetSlot();

        if (targetSlot.IsEmpty)
        {
            targetSlot.SetInstance(DragContext.draggedInstance);
            DragContext.draggedInstance = null;
        }
        else if (targetSlot.instance != null && targetSlot.instance.CanStackWith(DragContext.draggedInstance))
        {
            int space = targetSlot.item.maxStack - targetSlot.count;
            int add = Mathf.Min(space, DragContext.draggedInstance.count);
            targetSlot.AddCount(add);
            DragContext.draggedInstance.count -= add;
            if (DragContext.draggedInstance.count > 0) ReturnToOriginalSlot();
        }
        else
        {
            // Swap — ItemInstance 참조 자체를 교환해 동적 상태(quality 등)를 보존한다.
            var orig = GetOriginalSlot();
            var targetInstance = targetSlot.instance;
            targetSlot.SetInstance(DragContext.draggedInstance);
            orig.SetInstance(targetInstance);
            DragContext.draggedInstance = null;
        }

        DragContext.Clear();
        RefreshAllUIs();
    }

    InventorySlot GetOriginalSlot()
    {
        return DragContext.fromOwner == SlotOwner.Inventory ? inventory.slots[DragContext.fromSlotIndex] : hotbar.slots[DragContext.fromSlotIndex];
    }
    void ReturnToOriginalSlot()
    {
        var s = GetOriginalSlot();
        if (DragContext.draggedInstance == null) return;

        if (s.IsEmpty)
        {
            s.SetInstance(DragContext.draggedInstance);
        }
        else if (s.instance != null && s.instance.CanStackWith(DragContext.draggedInstance))
        {
            s.AddCount(DragContext.draggedInstance.count);
        }
        DragContext.Clear();
    }
    void RefreshAllUIs()
    {
        if (inventoryUI != null) inventoryUI.RefreshUI();
        if (hotbarUI != null) hotbarUI.RefreshUI();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        var slot = GetSlot();
        if (slot?.item != null && tooltip != null)
            tooltip.Show(slot.item, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null) tooltip.Hide();
    }
}
