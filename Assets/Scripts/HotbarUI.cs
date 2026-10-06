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
    Inventory _boundInventory;
    PlayerInputHandler _boundInput;

    public RectTransform dragLayer;
    public Canvas rootCanvas;

    void OnEnable() => RefreshUI();

    void LateUpdate()
    {
        // bootstrap가 UI/입력 권위를 나중에 연결하거나 교체해도 현재 인스턴스로 구독한다.
        bool rebound = BindRuntimeReferences();
        // D4(데모): X로 손을 비우면 선택 칸 강조도 바뀌어야 HUD와 손이 같은 상태를 보여 준다.
        if (rebound || FirstDayHolstered() != _shownHolstered) RefreshUI();
    }

    EquipmentSystem _equipment;
    bool _shownHolstered;

    bool FirstDayHolstered()
    {
        if (inventory == null) return false;
        if (_equipment == null || _equipment.gameObject != inventory.gameObject) _equipment = inventory.GetComponent<EquipmentSystem>();
        return _equipment != null && _equipment.FirstDayPresentation && _equipment.IsHolstered;
    }

    void Start()
    {
        BindRuntimeReferences();
        if (hotbar == null || slotParent == null || slotPrefab == null)
        {
            Debug.LogWarning("HotbarUI: missing hotbar, slotParent, or slotPrefab.");
            return;
        }

        RefreshUI();
    }

    void OnDisable() => Unbind();
    void OnDestroy() => Unbind();

    void Unbind()
    {
        if (_boundInventory != null) _boundInventory.onItemChangedCallback -= RefreshUI;
        if (_boundInput != null)
        {
            _boundInput.OnHotbarDirectSelect -= SelectSlot;
            _boundInput.OnHotbarScroll -= HandleScroll;
        }
        _boundInventory = null;
        _boundInput = null;
    }

    // -------- 입력 핸들러 --------

    private void SelectSlot(int index)
    {
        BindRuntimeReferences();
        if (hotbar == null) return;
        if (index < 0 || index >= hotbar.size) return;
        // 입력 권위가 이미 선택했다. UI 콜백은 재선택 이벤트를 중복 발행하지 않는다.
        RefreshUI();
    }

    private void HandleScroll(float scrollY)
    {
        BindRuntimeReferences();
        if (hotbar == null || hotbar.size <= 0) return;
        // scrollY 양수 = 아래 방향 휠 → 다음(Next) 슬롯
        selectedIndex = inventory != null ? inventory.selectedHotbarIndex : selectedIndex;
        if (scrollY > 0f)
            selectedIndex = (selectedIndex + 1) % hotbar.size;
        else
            selectedIndex = (selectedIndex - 1 + hotbar.size) % hotbar.size;

        if (inventory != null) inventory.SelectHotbarSlot(selectedIndex);
    }

    // -------- UI 갱신 --------

    public void RefreshUI()
    {
        BindRuntimeReferences();

        if (inventory != null)
            selectedIndex = inventory.selectedHotbarIndex;
        _shownHolstered = FirstDayHolstered();

        if (slotUIs == null) slotUIs = new List<InventorySlotUI>();
        if (slotUIs.Count == 0 && hotbar != null && hotbar.slots != null && slotParent != null && slotPrefab != null)
        {
            foreach (Transform child in slotParent)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            for (int i = 0; i < hotbar.size; i++)
            {
                var go = Instantiate(slotPrefab, slotParent);
                var ui = go.GetComponent<InventorySlotUI>();
                ui.tooltip = tooltip;
                ui.SetupHotbar(hotbar, inventory, i, this);
                slotUIs.Add(ui);
            }
        }

        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] == null) continue;
            var slot = hotbar != null && hotbar.slots != null && i < hotbar.size && i < hotbar.slots.Count
                ? hotbar.slots[i] : null;
            if (PAUiTheme.Active && tooltip == null) tooltip = PAUiTheme.EnsureTooltip(rootCanvas);
            slotUIs[i].tooltip = tooltip;
            slotUIs[i].SetupHotbar(hotbar, inventory, i, this);
            // 없는/빈 권위 슬롯도 반드시 전달해 이전 sprite/count를 지운다.
            slotUIs[i].SetSlot(slot);
            if ((slot == null || slot.IsEmpty) && slotUIs[i].fallbackText != null)
                slotUIs[i].fallbackText.enabled = false;
            if (PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.FirstDayControls && slotUIs[i].transform.Find("SlotNumber") == null)
            {
                var label=new GameObject("SlotNumber",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));
                label.transform.SetParent(slotUIs[i].transform,false);
                var rect=(RectTransform)label.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
                rect.anchoredPosition=new Vector2(3,-2);rect.sizeDelta=new Vector2(22,22);
                var text=label.GetComponent<TMPro.TextMeshProUGUI>();text.text=(i+1).ToString();text.fontSize=16;text.color=new Color(.12f,.2f,.2f);text.raycastTarget=false;
            }

            Image bg = null;
            bg = slotUIs[i].GetComponent<Image>();
            bool firstDayHud = PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.FirstDayControls;
            bool empty = slot == null || slot.IsEmpty;
            // D4(데모): 빈 칸이어도 선택 칸을 강조하고, 빈 칸은 반투명으로 낮춰 화면에서 가장 밝은 요소가 되지 않게 한다.
            // 빈손(X) 상태의 선택 칸은 옅은 금색 — 실제로 들고 있을 때만 진한 금색이다.
            if (bg != null) bg.color = firstDayHud
                ? (i == selectedIndex ? (_shownHolstered ? new Color(1f, .86f, .56f, .78f) : new Color(1f, .80f, .36f, .96f))
                    : empty ? new Color(.98f, .95f, .87f, .42f) : new Color(.98f, .95f, .87f, .90f))
                : (i == selectedIndex && !empty) ? new Color(1,.8f,.36f) : new Color(.91f,.91f,.80f);
            if (firstDayHud) PAUiTheme.Slot(slotUIs[i], slot, i == selectedIndex, _shownHolstered);
        }

        UpdateCharacterModel();
    }

    void UpdateCharacterModel()
    {
        if (inventory != null && inventory.GetComponent<EquipmentSystem>()?.FirstDayPresentation == true) return;
        if (toolsParent == null) return;
        if (slotUIs == null || selectedIndex < 0 || selectedIndex >= slotUIs.Count) return;

        InventorySlot slot = slotUIs[selectedIndex].GetComponent<InventorySlotUI>().GetSlot();

        for (int x = 0; x < toolsParent.childCount; x++) Destroy(toolsParent.GetChild(x).gameObject);

        if (slot == null || slot.IsEmpty || slot.item.model == null) return;
        Instantiate(slot.item.model, toolsParent);
    }

    bool BindRuntimeReferences()
    {
        var previousHotbar = hotbar;
        GameObject player = null;
        try { player = GameObject.FindGameObjectWithTag("Player"); } catch { }
        var playerHotbar = player != null ? player.GetComponent<Hotbar>() : null;
        var playerInventory = player != null ? player.GetComponent<Inventory>() : null;

        if (Inventory.instance != null)
            inventory = Inventory.instance;
        else if (playerInventory != null)
            inventory = playerInventory;
        else if (inventory == null)
            inventory = Inventory.instance != null ? Inventory.instance : FindAnyObjectByType<Inventory>();
        // UI는 별도 GetComponent 결과보다 Inventory가 실제로 소유한 Hotbar를 따른다.
        if (inventory != null && inventory.hotbar != null)
            hotbar = inventory.hotbar;
        else if (playerHotbar != null)
            hotbar = playerHotbar;
        else if (hotbar == null)
            hotbar = FindAnyObjectByType<Hotbar>();
        if (inventory != null && inventory.hotbar == null && hotbar != null)
            inventory.hotbar = hotbar;
        if (selectedIndex < 0) selectedIndex = 0;
        if (hotbar != null && hotbar.size > 0)
            selectedIndex = Mathf.Clamp(selectedIndex, 0, hotbar.size - 1);

        bool changed = previousHotbar != hotbar;
        if (!isActiveAndEnabled) return changed;
        if (_boundInventory != inventory)
        {
            if (_boundInventory != null) _boundInventory.onItemChangedCallback -= RefreshUI;
            _boundInventory = inventory;
            if (_boundInventory != null) _boundInventory.onItemChangedCallback += RefreshUI;
            changed = true;
        }
        if (_boundInput != PlayerInputHandler.Instance)
        {
            if (_boundInput != null)
            {
                _boundInput.OnHotbarDirectSelect -= SelectSlot;
                _boundInput.OnHotbarScroll -= HandleScroll;
            }
            _boundInput = PlayerInputHandler.Instance;
            if (_boundInput != null)
            {
                _boundInput.OnHotbarDirectSelect += SelectSlot;
                _boundInput.OnHotbarScroll += HandleScroll;
            }
            changed = true;
        }
        return changed;
    }
}
