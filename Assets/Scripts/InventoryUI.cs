using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    // ⭐ [추가] 외부에서 접근 가능하도록 싱글톤 설정
    public static InventoryUI instance;

    public Inventory inventory;
    public Hotbar hotbar;
    public Transform slotParent;
    public GameObject slotPrefab;
    public ItemTooltip tooltip;

    public RectTransform dragLayer;
    public Canvas rootCanvas;

    private List<InventorySlotUI> slotUIs;

    void Awake()
    {
        // ⭐ [추가] 싱글톤 초기화
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        BindRuntimeReferences();
        if (inventory == null || hotbar == null || slotParent == null || slotPrefab == null)
        {
            Debug.LogWarning("InventoryUI: missing inventory, hotbar, slotParent, or slotPrefab.");
            gameObject.SetActive(false);
            return;
        }

        slotUIs = new List<InventorySlotUI>();
        foreach (Transform child in slotParent) Destroy(child.gameObject);

        for (int i = 0; i < inventory.size; i++)
        {
            var slotGO = Instantiate(slotPrefab, slotParent);
            var slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.tooltip = tooltip;
            slotUI.Setup(inventory, hotbar, i, this);
            slotUIs.Add(slotUI);
        }

        RefreshUI();

        // ⭐ [추가] 시작하자마자 화면에서 숨기기!
        gameObject.SetActive(false);
    }

    public void RefreshUI()
    {
        BindRuntimeReferences();
        if (slotUIs == null || inventory == null) return;
        for (int i = 0; i < inventory.size; i++)
        {
            if (i < slotUIs.Count) slotUIs[i].SetSlot(inventory.slots[i]);
        }
    }

    // ⭐ [추가] 껐다 켰다 하는 함수
    public void Toggle()
    {
        bool isActive = !gameObject.activeSelf; // 현재 상태의 반대로
        gameObject.SetActive(isActive);

        if (isActive)
        {
            RefreshUI(); // 켤 때 갱신 한 번 해줌

            // 📱 상호배타: 스마트폰이 열려있으면 강제로 닫는다 (Docs §레퍼런스.html)
            if (SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen)
                SmartphoneUI.instance.Toggle();

            // 인벤토리 열리면 마우스 보이기
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // 닫으면 마우스 숨기고 게임으로 돌아가기
            PlayerInputHandler.RestoreGameplayCursor();
            
            // 툴팁도 같이 꺼주기 (혹시 켜져있을까봐)
            if(tooltip != null) tooltip.Hide();
        }
    }

    void BindRuntimeReferences()
    {
        GameObject player = null;
        try { player = GameObject.FindGameObjectWithTag("Player"); } catch { }

        var playerInventory = player != null ? player.GetComponent<Inventory>() : null;
        var playerHotbar = player != null ? player.GetComponent<Hotbar>() : null;

        if (playerInventory != null)
            inventory = playerInventory;
        else if (inventory == null)
            inventory = Inventory.instance != null ? Inventory.instance : FindAnyObjectByType<Inventory>();

        if (playerHotbar != null)
            hotbar = playerHotbar;
        else if (hotbar == null)
            hotbar = inventory != null && inventory.hotbar != null ? inventory.hotbar : FindAnyObjectByType<Hotbar>();

        if (inventory != null && inventory.hotbar == null && hotbar != null)
            inventory.hotbar = hotbar;
    }
}
