using UnityEngine;
using System.Collections.Generic;

public class StorageBox : MonoBehaviour, IInteractable
{
    public string boxName = "Storage";

    // 보관함은 내부적으로 ItemInstance 리스트로 동작한다.
    // Docs/02의 "Inventory & ItemInstance 1:N 관계"를 보관함에도 그대로 적용.
    public List<ItemInstance> items = new List<ItemInstance>();

    public int maxSlotCount = 10;

    public void Interact(GameObject interactor)
    {
        Debug.Log("📦 보관함을 엽니다.");
        if (StorageUI.instance != null)
        {
            StorageUI.instance.OpenBox(this);
        }
    }

    public string GetInteractPrompt() => "보관함 열기";

    // 외부에서는 여전히 Item(원형)으로 편하게 넣을 수 있다.
    public bool AddItem(Item item)
    {
        if (item == null) return false;
        if (items.Count >= maxSlotCount)
        {
            Debug.Log("📦 상자가 꽉 찼습니다!");
            return false;
        }
        items.Add(new ItemInstance(item, 1));
        return true;
    }

    // 이미 존재하는 인스턴스를 통째로 보관 (동적 상태 보존).
    public bool AddInstance(ItemInstance inst)
    {
        if (inst == null || inst.data == null || inst.count <= 0) return false;
        if (items.Count >= maxSlotCount)
        {
            Debug.Log("📦 상자가 꽉 찼습니다!");
            return false;
        }
        items.Add(inst);
        return true;
    }

    public void RemoveItem(int index)
    {
        if (index >= 0 && index < items.Count)
        {
            items.RemoveAt(index);
        }
    }
}
