using System.Linq;
using UnityEngine;

// P6 데모: 제작 결과·보관함에서 꺼낸 물건도 줍기(Inventory.TryReceiveToHotbar)처럼 핫바를 우선한다.
// 새 권위가 아니라 기존 Inventory 안에서 가방 칸을 핫바 칸으로 옮기기만 한다.
public static class DemoHotbarPreference
{
    // 옮긴 뒤 그 물품이 있는 핫바 칸 번호(0부터), 없으면 -1.
    public static int Prefer(Item item)
    {
        var inventory = Inventory.instance;
        if (item == null || inventory == null || inventory.hotbar == null || inventory.slots == null ||
            PlayerInputHandler.Instance == null || !PlayerInputHandler.Instance.FirstDayControls) return -1;
        int stack = Mathf.Max(1, item.maxStack);
        foreach (var source in inventory.slots.Where(s => s != null && !s.IsEmpty && s.item == item).ToList())
        {
            var target = inventory.hotbar.slots.Find(s => s != null && !s.IsEmpty && s.instance.CanStackWith(source.instance) && s.count + source.count <= stack)
                ?? inventory.hotbar.slots.Find(s => s != null && s.IsEmpty);
            if (target == null) break;
            if (target.IsEmpty) target.SetInstance(source.instance);
            else target.AddCount(source.count);
            source.Clear();
        }
        inventory.RefreshAllUI();
        return inventory.hotbar.slots.FindIndex(s => s != null && !s.IsEmpty && s.item == item);
    }
}
