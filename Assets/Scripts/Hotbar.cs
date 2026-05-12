using System.Collections.Generic;
using UnityEngine;

public class Hotbar : MonoBehaviour
{
    public int size = 9;
    public List<InventorySlot> slots;

    void Awake()
    {
        // ⚠ Demo Seed/Editor 주입 보존: Edit 모드에서 미리 채워둔 slots 가
        // Play 진입 시 새 List 로 덮어써져 사라지는 사고를 방지한다.
        // size 와 일치하고 각 칸이 null 이 아니면 그대로 둔다.
        EnsureSlots();
    }

    void EnsureSlots()
    {
        if (slots == null || slots.Count != size)
        {
            slots = new List<InventorySlot>(new InventorySlot[size]);
            for (int i = 0; i < size; i++) slots[i] = new InventorySlot();
            return;
        }
        // size 가 맞지만 내부 null 칸이 섞여 있으면 그 칸만 복원
        for (int i = 0; i < size; i++)
            if (slots[i] == null) slots[i] = new InventorySlot();
    }

    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= size) return null;
        return slots[index];
    }

    public bool AddItem(Item newItem, int amount = 1)
    {
        // 1. 겹치기 시도
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
        // 2. 빈 슬롯 시도
        foreach (var slot in slots)
        {
            if (slot.IsEmpty)
            {
                slot.Set(newItem, amount);
                return true;
            }
        }
        return false;
    }
}