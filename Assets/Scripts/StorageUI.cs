using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StorageUI : MonoBehaviour
{
    public static StorageUI instance;

    [Header("UI 연결")]
    public GameObject uiPanel;      // 전체 패널
    public Transform itemsParent;   // 슬롯 부모 (Grid)
    public GameObject slotPrefab;   // 슬롯 프리팹
    public TextMeshProUGUI titleText; // 제목

    private StorageBox currentBox;  // 현재 열린 상자

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        uiPanel.SetActive(false); 
    }

    public void OpenBox(StorageBox box)
    {
        currentBox = box;
        uiPanel.SetActive(true);
        titleText.text = box.boxName;

        UpdateUI();
        
        // UI 열리면 마우스 커서 보이기
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseBox()
    {
        currentBox = null;
        uiPanel.SetActive(false);
        
        // 닫으면 마우스 커서 숨기기 (상황에 따라 다름)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UpdateUI()
    {
        // 슬롯 초기화
        foreach (Transform child in itemsParent) Destroy(child.gameObject);

        // 현재 상자 내용물 표시
        for (int i = 0; i < currentBox.items.Count; i++)
        {
            int index = i;
            ItemInstance inst = currentBox.items[i];
            if (inst == null || inst.data == null) continue;

            GameObject newSlot = Instantiate(slotPrefab, itemsParent);

            // 아이콘 설정
            Image icon = newSlot.transform.Find("Icon").GetComponent<Image>();
            icon.sprite = inst.data.icon;
            icon.enabled = true;

            // 버튼 클릭 연결
            Button btn = newSlot.GetComponent<Button>();
            btn.onClick.AddListener(() => OnClickTakeItem(index));
        }
    }

    // 상자에서 아이템 꺼내기
    void OnClickTakeItem(int index)
    {
        if (currentBox == null) return;

        ItemInstance inst = currentBox.items[index];
        if (inst == null || inst.data == null) return;

        // 인벤토리에 넣기 (원형 템플릿 기반)
        Inventory.instance.AddItem(inst.data, inst.count);

        // 상자에서 빼기
        currentBox.RemoveItem(index);

        UpdateUI();
        // 인벤토리 UI도 갱신
        Inventory.instance.RefreshAllUI();
    }

    // 상자에 아이템 넣기
    public void OnClickStoreItem()
    {
        if (currentBox == null) return;

        // ⭐ [수정] ItemData -> Item
        Item heldItem = Inventory.instance.GetSelectedItem();
        if (heldItem == null) return;

        if (currentBox.AddItem(heldItem))
        {
            Inventory.instance.RemoveItems(heldItem, 1);
            UpdateUI();
            Inventory.instance.RefreshAllUI();
        }
    }
}