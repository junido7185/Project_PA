using UnityEngine;
using System.Collections.Generic;

public class StorageBox : MonoBehaviour, IInteractable
{
    public string boxName = "Storage";
    
    // ⭐ ItemData -> Item
    public List<Item> items = new List<Item>();
    
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

    public bool AddItem(Item item)
    {
        if (items.Count >= maxSlotCount)
        {
            Debug.Log("📦 상자가 꽉 찼습니다!");
            return false;
        }
        items.Add(item);
        return true;
    }

    public void RemoveItem(int index)
    {
        if (index < items.Count)
        {
            items.RemoveAt(index);
        }
    }
}