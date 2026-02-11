using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    // ⭐ 싱글톤 추가 (기존 코드 호환용)
    public static Inventory instance;

    public int size = 24; // 인벤토리 슬롯 개수
    public List<InventorySlot> slots;

    // ⭐ 핫바 연결 (선택된 아이템을 찾기 위해 필수)
    public Hotbar hotbar;
    public int selectedHotbarIndex = 0; // 현재 핫바에서 선택된 슬롯 번호

    // UI 갱신용 이벤트 (기존 코드 호환용)
    public delegate void OnItemChanged();
    public OnItemChanged onItemChangedCallback;

    private void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;

        // 슬롯 초기화
        slots = new List<InventorySlot>(new InventorySlot[size]);
        for (int i = 0; i < size; i++) slots[i] = new InventorySlot();
    }

    // ⭐ [호환 함수 1] 현재 손에 든(핫바에서 선택된) 아이템 가져오기
    public Item GetSelectedItem()
    {
        if (hotbar == null) return null;
        
        InventorySlot slot = hotbar.GetSlot(selectedHotbarIndex);
        if (slot != null && !slot.IsEmpty)
        {
            return slot.item;
        }
        return null;
    }

    // ⭐ [호환 함수 2] 아이템 가지고 있는지 확인
    public bool HasItems(Item item, int count)
    {
        int currentCount = 0;
        
        // 인벤토리 검사
        foreach (var slot in slots)
            if (!slot.IsEmpty && slot.item == item) currentCount += slot.count;
        
        // 핫바 검사
        if (hotbar != null)
            foreach (var slot in hotbar.slots)
                if (!slot.IsEmpty && slot.item == item) currentCount += slot.count;

        return currentCount >= count;
    }

    // ⭐ [호환 함수 3] 아이템 제거 (핫바 -> 인벤토리 순으로 차감)
    public void RemoveItems(Item item, int count)
    {
        int leftToRemove = count;

        // 1. 핫바에서 제거
        if (hotbar != null) 
            leftToRemove = RemoveFromList(hotbar.slots, item, leftToRemove);

        // 2. 인벤토리에서 제거
        if (leftToRemove > 0) 
            RemoveFromList(slots, item, leftToRemove);

        // UI 갱신 알림
        RefreshAllUI();
    }

    // 리스트에서 아이템 빼는 내부 로직
    private int RemoveFromList(List<InventorySlot> list, Item item, int amount)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (amount <= 0) break;
            if (!list[i].IsEmpty && list[i].item == item)
            {
                if (list[i].count > amount)
                {
                    list[i].count -= amount;
                    amount = 0;
                }
                else
                {
                    amount -= list[i].count;
                    list[i].item = null;
                    list[i].count = 0;
                }
            }
        }
        return amount;
    }

    public void RefreshAllUI()
    {
        if (onItemChangedCallback != null) onItemChangedCallback.Invoke();
        
        // 에셋 쪽 UI들도 갱신
        InventoryUI invUI = FindAnyObjectByType<InventoryUI>();
        if(invUI) invUI.RefreshUI();
        
        HotbarUI hotUI = FindAnyObjectByType<HotbarUI>();
        if(hotUI) hotUI.RefreshUI();
    }

    // --- 아래는 프레임워크 원본 로직 (AddItem 등) ---

    public bool AddItem(Item newItem, int amount = 1)
    {
        // 1. 겹치기(Stacking) 시도
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty && slot.item == newItem && slot.count < newItem.maxStack)
            {
                int space = newItem.maxStack - slot.count;
                int add = Mathf.Min(space, amount);
                slot.count += add;
                amount -= add;
                if (amount <= 0) return true;
            }
        }

        // 2. 빈 슬롯 찾기
        foreach (var slot in slots)
        {
            if (slot.IsEmpty)
            {
                slot.item = newItem;
                slot.count = amount;
                return true;
            }
        }
        return false; // 가방 꽉 참
    }

    public void MoveOrSwap(int from, int to)
    {
        if (from == to) return;
        var slotFrom = slots[from];
        var slotTo = slots[to];

        if (slotTo.IsEmpty)
        {
            slotTo.item = slotFrom.item;
            slotTo.count = slotFrom.count;
            slotFrom.item = null;
            slotFrom.count = 0;
        }
        else
        {
            var tmpItem = slotFrom.item;
            var tmpCount = slotFrom.count;
            slotFrom.item = slotTo.item;
            slotFrom.count = slotTo.count;
            slotTo.item = tmpItem;
            slotTo.count = tmpCount;
        }
    }
}