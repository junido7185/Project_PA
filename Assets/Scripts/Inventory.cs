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
        if (instance != null && instance != this)
        {
            bool thisIsPlayerInventory = IsPlayerObject(gameObject);
            bool currentIsPlayerInventory = instance != null && IsPlayerObject(instance.gameObject);

            if (thisIsPlayerInventory && !currentIsPlayerInventory)
            {
                Destroy(instance);
            }
            else
            {
                Destroy(this);
                return;
            }
        }

        instance = this;

        // ⚠ Demo Seed/Editor 주입 보존: Edit 모드에서 미리 채워둔 slots 가
        // Play 진입 시 새 List 로 덮어써져 사라지는 사고를 방지한다.
        // size 와 일치하고 각 칸이 null 이 아니면 그대로 둔다.
        EnsureSlots();
    }

    static bool IsPlayerObject(GameObject go)
    {
        if (go == null) return false;
        try { return go.CompareTag("Player"); }
        catch { return false; }
    }

    void EnsureSlots()
    {
        if (slots == null || slots.Count != size)
        {
            slots = new List<InventorySlot>(new InventorySlot[size]);
            for (int i = 0; i < size; i++) slots[i] = new InventorySlot();
            return;
        }
        for (int i = 0; i < size; i++)
            if (slots[i] == null) slots[i] = new InventorySlot();
    }

    // ⭐ [호환 함수 1] 현재 손에 든(핫바에서 선택된) 아이템 원형(Item) 가져오기
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

    // 현재 손에 든 ItemInstance(동적 상태 포함)를 가져온다.
    // ShopSlot 진열 등에서 quality/currentPrice 메타를 보존하려면 이 함수를 써야 한다.
    public ItemInstance GetSelectedInstance()
    {
        if (hotbar == null) return null;
        InventorySlot slot = hotbar.GetSlot(selectedHotbarIndex);
        return (slot != null && !slot.IsEmpty) ? slot.instance : null;
    }

    // ⭐ [호환 함수 2] 아이템 가지고 있는지 확인
    public int CountItems(Item item)
    {
        if (item == null) return 0;

        int currentCount = 0;
        if (slots != null)
        {
            foreach (var slot in slots)
                if (slot != null && !slot.IsEmpty && slot.item == item)
                    currentCount += slot.count;
        }

        if (hotbar != null && hotbar.slots != null)
        {
            foreach (var slot in hotbar.slots)
                if (slot != null && !slot.IsEmpty && slot.item == item)
                    currentCount += slot.count;
        }

        return currentCount;
    }

    public bool HasItems(Item item, int count)
    {
        return count <= 0 || CountItems(item) >= count;
    }

    // AddItem이 사용하는 일반 인벤토리 슬롯에 전량을 넣을 수 있는지 미리 검사한다.
    // 수확처럼 실패 시 원본 상태를 보존해야 하는 흐름에서 사용한다.
    public bool CanAddItems(Item item, int count)
    {
        if (item == null || count <= 0) return false;
        if (slots == null) return false;

        int capacity = 0;
        int maxStack = Mathf.Max(1, item.maxStack);
        foreach (InventorySlot slot in slots)
        {
            if (slot == null || slot.IsEmpty)
                capacity += maxStack;
            else if (slot.item == item)
                capacity += Mathf.Max(0, maxStack - slot.count);

            if (capacity >= count) return true;
        }

        return false;
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
                    list[i].AddCount(-amount);
                    amount = 0;
                }
                else
                {
                    amount -= list[i].count;
                    list[i].Clear();
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
                slot.AddCount(add);
                amount -= add;
                if (amount <= 0) return true;
            }
        }

        // 2. 빈 슬롯 찾기
        foreach (var slot in slots)
        {
            if (slot.IsEmpty)
            {
                slot.Set(newItem, amount);
                return true;
            }
        }
        return false; // 가방 꽉 참
    }

    // quality / currentPrice 등 동적 메타를 보존하는 인스턴스를 전량 받을 수 있는지 검사한다.
    // AddInstance와 같은 규칙(메타 일치 스택 우선, 남은 수량은 빈 슬롯 1칸)을 사용하며
    // 슬롯이나 newInst를 변경하지 않는다.
    public bool CanAddInstance(ItemInstance newInst)
    {
        if (newInst == null || newInst.data == null || newInst.count <= 0) return false;
        if (slots == null) return false;

        int remaining = newInst.count;
        int maxStack = Mathf.Max(1, newInst.data.maxStack);
        bool hasEmptySlot = false;

        foreach (InventorySlot slot in slots)
        {
            if (slot == null)
                continue;

            if (slot.IsEmpty)
            {
                hasEmptySlot = true;
                continue;
            }

            if (slot.instance == null || !slot.instance.CanStackWith(newInst))
                continue;

            remaining -= Mathf.Max(0, maxStack - slot.count);
            if (remaining <= 0)
                return true;
        }

        // AddInstance는 메타 일치 스택을 채운 뒤 남은 전량을 첫 빈 슬롯에 넣는다.
        return remaining <= 0 || hasEmptySlot;
    }

    // quality / currentPrice 등 동적 메타를 보존하는 인스턴스 추가 경로.
    // 가공·구매·드롭 등 "메타가 의미 있는 출처" 에서 호출한다.
    //
    // 동작:
    // 1) 기존 슬롯들 중 ItemInstance.CanStackWith() 가 true 인 것에 합친다 (메타 일치).
    // 2) 합쳐도 남은 수량은 빈 슬롯에 통째로 SetInstance 한다.
    // 3) 모두 실패하면 false 를 반환한다 (가방 풀).
    //
    // 주의: newInst 는 호출 후에도 호출자가 들고 있을 수 있는 객체이므로,
    // 빈 슬롯에 그대로 넘길 때 참조를 그대로 사용한다 (복사하지 않음 — 메타 보존).
    public bool AddInstance(ItemInstance newInst)
    {
        if (newInst == null || newInst.data == null || newInst.count <= 0) return false;
        // 아래 스택 병합은 newInst.count와 기존 슬롯을 변경한다.
        // 먼저 전량 수용을 확인해 false 반환 시 어떤 아이템도 부분 이동하지 않게 한다.
        if (!CanAddInstance(newInst)) return false;

        int maxStack = Mathf.Max(1, newInst.data.maxStack);

        // 1. 메타 일치 스택과 합치기 (CanStackWith 가 quality/currentPrice 도 비교)
        foreach (var slot in slots)
        {
            if (slot == null || slot.IsEmpty || slot.instance == null) continue;
            if (!slot.instance.CanStackWith(newInst)) continue;
            if (slot.count >= maxStack) continue;

            int space = maxStack - slot.count;
            int add = Mathf.Min(space, newInst.count);
            slot.AddCount(add);
            newInst.count -= add;
            if (newInst.count <= 0)
            {
                RefreshAllUI();
                return true;
            }
        }

        // 2. 빈 슬롯에 통째로 주입 (메타 보존)
        foreach (var slot in slots)
        {
            if (slot != null && slot.IsEmpty)
            {
                slot.SetInstance(newInst);
                RefreshAllUI();
                return true;
            }
        }

        Debug.LogError("Inventory.AddInstance: 수용량 선검사 후 추가에 실패했습니다. 슬롯 상태 변경 콜백을 점검하세요.");
        return false;
    }

    // 차감될 재료 count 개의 가중평균 quality 를 반환한다.
    // RemoveItems 와 동일하게 hotbar → inventory 순으로 탐색해 실제 소모될 스택과 일치시킨다.
    // HasItems 로 사전 확인을 권장한다 (수량 부족 시 보유분만으로 평균을 낸다).
    public float GetAverageQuality(Item item, int count)
    {
        float totalQuality = 0f;
        int   totalCount   = 0;
        int   remaining    = count;

        if (hotbar != null)
            AccumulateQuality(hotbar.slots, item, ref remaining, ref totalQuality, ref totalCount);

        if (remaining > 0)
            AccumulateQuality(slots, item, ref remaining, ref totalQuality, ref totalCount);

        return totalCount > 0 ? totalQuality / totalCount : 1f;
    }

    private void AccumulateQuality(List<InventorySlot> list, Item item,
        ref int remaining, ref float totalQuality, ref int totalCount)
    {
        foreach (var slot in list)
        {
            if (remaining <= 0) break;
            if (slot.IsEmpty || slot.item != item) continue;

            float q    = slot.instance != null ? slot.instance.quality : 1f;
            int   take = Mathf.Min(slot.count, remaining);
            totalQuality += q * take;
            totalCount   += take;
            remaining    -= take;
        }
    }

    public void MoveOrSwap(int from, int to)
    {
        if (from == to) return;
        var slotFrom = slots[from];
        var slotTo = slots[to];

        // ItemInstance 참조 자체를 스왑하면 quality/currentPrice 등 동적 상태가 그대로 보존된다.
        var tmp = slotFrom.instance;
        slotFrom.SetInstance(slotTo.instance);
        slotTo.SetInstance(tmp);
    }
}
