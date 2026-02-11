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
            
            // 인벤토리 열리면 마우스 보이기
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // 닫으면 마우스 숨기고 게임으로 돌아가기
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // 툴팁도 같이 꺼주기 (혹시 켜져있을까봐)
            if(tooltip != null) tooltip.Hide();
        }
    }
}