using UnityEngine;

// Canon v2 §5.1: 도구 내구도는 성공한 작업 결과(벌목 1그루·채광 1개·포획 1마리·낚시 1마리)에만 줄어든다.
// 허공·틀린 대상·실패는 이 메서드를 부르지 않는다.
// 스타터 수치 = 데모 대표 경로의 도구 성공 횟수 + 약 25~35% 여유:
//   판재(Wood2)·개선 곡괭이(Wood2+Ore2)·진열(Wood2·Ore1) 중 약 1/3은 바닥 줍기 → 벌목 4회, 채광 3회.
//   도끼 4→5(25%), 곡괭이 3→4(33%). 낚시/곤충은 선택 행동이라 6회. 실제 동선 테스트 뒤 조정 가능.
// 런타임 ItemInstance 상태만 쓴다(저장 schema 없음).
public static class ToolDurability
{
    public static int MaxFor(Item item)
    {
        if (item == null) return 0;
        switch (item.id)
        {
            case 2001: return 5;   // Starter Axe
            case 2002: return 4;   // Starter Pickaxe
            case 2003: return 6;   // Starter Net
            case 2011: return 6;   // Starter Fishing Rod
            case 2022: return 12;  // Improved Pickaxe (플레이어 사용 시)
            default: return 0;
        }
    }

    public static int Remaining(ItemInstance tool) =>
        tool == null ? 0 : Mathf.Max(0, MaxFor(tool.data) - tool.durabilityUsed);

    // 성공한 작업 결과 1회. 0이 되면 그 도구 스택을 가방에서 없애고 알린다.
    public static void ConsumeSuccess(Inventory inventory, ItemInstance tool)
    {
        if (inventory == null || tool == null || MaxFor(tool.data) <= 0) return;
        tool.durabilityUsed++;
        int left = Remaining(tool);
        string name = DisplayName(tool.data);
        if (left <= 0)
        {
            RemoveInstance(inventory, tool);
            FirstDayWorldPresentation.Toast($"{name}가 닳아서 부서졌어요.");
        }
        else if (left == 1)
            FirstDayWorldPresentation.Toast($"{name}를 한 번 더 쓰면 부서져요.", false);
        inventory.RefreshAllUI();
    }

    // 모두 모음으로 끝나는 이름이라 조사 '가/를'을 쓴다.
    public static string DisplayName(Item item) => item == null ? "도구" : item.id switch
    {
        2001 => "도끼", 2002 => "곡괭이", 2003 => "잠자리채", 2011 => "낚싯대", 2022 => "개선 곡괭이",
        _ => item.itemName
    };

    static void RemoveInstance(Inventory inventory, ItemInstance tool)
    {
        if (inventory.hotbar != null)
            foreach (var slot in inventory.hotbar.slots)
                if (slot != null && ReferenceEquals(slot.instance, tool)) { slot.Clear(); return; }
        foreach (var slot in inventory.slots)
            if (slot != null && ReferenceEquals(slot.instance, tool)) { slot.Clear(); return; }
    }
}
