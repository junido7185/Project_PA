using UnityEngine;
using UnityEngine.UI;

public class QuickSlotUI : MonoBehaviour
{
    public Transform slotsParent;    // QuickSlotPanel
    public GameObject borderPrefab;  // Selected_Border 프리팹
    
    private GameObject currentBorder; // 현재 생성된 테두리

    void Start()
    {
        Inventory.instance.onSlotChangedCallback += UpdateSelection;
        
        // 시작하자마자 1번 슬롯 선택 표시
        UpdateSelection(0);
    }

    void UpdateSelection(int index)
    {
        // 1. 기존 테두리 삭제
        if (currentBorder != null) Destroy(currentBorder);

        // 2. 해당 슬롯 위치 찾기
        // (주의: 슬롯 개수보다 큰 번호를 누르면 에러 날 수 있으니 체크)
        if (index < slotsParent.childCount)
        {
            Transform targetSlot = slotsParent.GetChild(index);

            // 3. 테두리 생성 및 부착
            currentBorder = Instantiate(borderPrefab, targetSlot);
            
            // 위치 정중앙으로 맞추기
            currentBorder.transform.localPosition = Vector3.zero;
            // 크기 맞추기 (꽉 채우기)
            RectTransform rect = currentBorder.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}