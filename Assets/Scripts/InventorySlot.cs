using UnityEngine;

// 인벤토리/핫바의 한 칸.
// 내부 저장은 ItemInstance 하나이며, 기존 코드와의 호환을 위해
// slot.item / slot.count 읽기는 그대로 동작한다. 쓰기는 Set/Clear/AddCount로 일원화한다.
[System.Serializable]
public class InventorySlot
{
    // 현재 이 칸이 보유한 스택. null = 빈 칸.
    public ItemInstance instance;

    public bool IsEmpty => instance == null || instance.count <= 0 || instance.data == null;

    // -------- 읽기 호환 accessor (기존 코드가 slot.item / slot.count 를 참조) --------
    public Item item => instance?.data;
    public int count => instance != null ? instance.count : 0;

    // -------- 쓰기는 이 헬퍼들로 일원화 --------

    // 새로운 스택을 넣는다. count <= 0 또는 data == null 이면 Clear 와 동일.
    public void Set(Item data, int count)
    {
        if (data == null || count <= 0) { instance = null; return; }
        instance = new ItemInstance(data, count);
    }

    // 기존 ItemInstance 객체를 통째로 주입. 다른 슬롯에서 이동해 올 때 사용.
    public void SetInstance(ItemInstance inst)
    {
        instance = (inst == null || inst.count <= 0 || inst.data == null) ? null : inst;
    }

    public void Clear()
    {
        instance = null;
    }

    // 수량을 delta만큼 증감. 0 이하가 되면 자동으로 Clear.
    public void AddCount(int delta)
    {
        if (instance == null) return;
        int next = instance.count + delta;
        if (next <= 0) instance = null;
        else instance.count = next;
    }

    // 수량을 절대값으로 지정.
    public void SetCount(int value)
    {
        if (instance == null) return;
        if (value <= 0) instance = null;
        else instance.count = value;
    }
}
