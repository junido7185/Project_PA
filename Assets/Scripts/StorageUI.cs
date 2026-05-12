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
    // §02 ItemInstance 보존: 원형(Item) 으로 다시 생성하지 않고, 보관함에 들어있던
    // ItemInstance 를 통째로 인벤토리에 넘겨 quality / currentPrice 를 잃지 않는다.
    void OnClickTakeItem(int index)
    {
        if (currentBox == null) return;
        if (index < 0 || index >= currentBox.items.Count) return;

        ItemInstance inst = currentBox.items[index];
        if (inst == null || inst.data == null) return;

        bool added = Inventory.instance.AddInstance(inst);
        if (added || inst.count <= 0)
        {
            currentBox.RemoveItem(index);
        }
        // 부분 적재 (가방 일부만 비어있어 일부 수량만 들어간 경우) 는 inst.count 가 0 이상으로 남는다.
        // 현재 정책: 메모리 내 inst.count 가 줄어들었다면 보관함에는 남은 수량으로 유지된다.

        UpdateUI();
        Inventory.instance.RefreshAllUI();
    }

    // 상자에 아이템 넣기
    // §02 ItemInstance 보존: 핫바에서 선택된 인스턴스를 직접 가져와 quality/currentPrice 를 살린다.
    // 한 번에 1개만 분리한다 (정밀 split UI 가 없는 MVP 단계 — TODO: 13주차 stack split UI).
    public void OnClickStoreItem()
    {
        if (currentBox == null) return;

        ItemInstance heldInst = Inventory.instance.GetSelectedInstance();
        if (heldInst == null || heldInst.data == null) return;

        // count=1 짜리 새 인스턴스로 분할 — 원본 메타(quality/currentPrice) 복사
        var splitInst = new ItemInstance(heldInst.data, 1)
        {
            quality      = heldInst.quality,
            currentPrice = heldInst.currentPrice,
        };

        if (currentBox.AddInstance(splitInst))
        {
            Inventory.instance.RemoveItems(heldInst.data, 1);
            UpdateUI();
            Inventory.instance.RefreshAllUI();
        }
    }
}