using System.Collections.Generic;
using UnityEngine;

// 상점 건물. 역할 2가지:
// 1. 자식 ShopSlot 들의 컨테이너 — NPC 가 GetAvailableSlots() 로 진열된 물건 목록을 조회한다.
// 2. 플레이어가 직접 상호작용하면 기존의 "모두 판매" 디버그 기능을 실행한다. (초기 테스트용)
//
// NPC 경제 루프의 진짜 주체는 ShopSlot 이며, Shop 은 그 집합을 관리한다.
public class Shop : MonoBehaviour, IInteractable
{
    // 자식 ShopSlot 들이 Awake 시점에 자기 자신을 여기에 등록한다.
    private readonly List<ShopSlot> _slots = new List<ShopSlot>();
    public IReadOnlyList<ShopSlot> Slots => _slots;

    public void RegisterSlot(ShopSlot slot)
    {
        if (slot != null && !_slots.Contains(slot)) _slots.Add(slot);
    }

    public void UnregisterSlot(ShopSlot slot)
    {
        _slots.Remove(slot);
    }

    // NPC 가 둘러볼 수 있는 "물건이 진열된" 슬롯만 반환한다.
    public List<ShopSlot> GetAvailableSlots()
    {
        var result = new List<ShopSlot>();
        foreach (var s in _slots)
        {
            if (s != null && !s.IsEmpty) result.Add(s);
        }
        return result;
    }

    // -------- 플레이어 직접 상호작용 (디버그 일괄 판매) --------

    public void Interact(GameObject interactor)
    {
        Debug.Log("🏪 상점 주인: 어서오세요!");
        SellAllItems();
    }

    public string GetInteractPrompt()
    {
        return "모두 판매하기 (디버그)";
    }

    public void SellAllItems()
    {
        Inventory inventory = Inventory.instance;
        int totalEarnings = 0;
        int soldCount = 0;

        soldCount += SellFromSlots(inventory.slots, ref totalEarnings);
        if (inventory.hotbar != null)
        {
            soldCount += SellFromSlots(inventory.hotbar.slots, ref totalEarnings);
        }

        if (soldCount > 0)
        {
            if (EconomyService.Instance != null)
            {
                EconomyService.Instance.Deposit(totalEarnings, "Shop.SellAllItems (디버그)");
            }
            Debug.Log($"💵 정산 완료! {soldCount}개 판매, 수익: {totalEarnings}G");
            inventory.RefreshAllUI();
        }
        else
        {
            Debug.Log("🚫 판매할 수 있는 아이템(자원)이 없습니다.");
        }
    }

    private int SellFromSlots(List<InventorySlot> slots, ref int earnings)
    {
        int count = 0;
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty && slot.item.toolType == ToolType.None && slot.item.category != ItemCategory.Tool)
            {
                // ItemInstance 의 EffectivePrice 를 사용해 향후 가격/품질 보정을 자동 반영한다.
                earnings += slot.instance.EffectivePrice * slot.count;
                count += slot.count;
                slot.Clear();
            }
        }
        return count;
    }
}
