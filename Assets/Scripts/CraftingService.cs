using System.Collections.Generic;
using UnityEngine;

// 가공(Crafting) 단일 진입점.
//
// 역할:
// - RecipeData 1개 + Workbench 컨텍스트를 받아 가공을 수행한다.
// - UI/입력에 무관한 순수 헬퍼이므로, 단위 테스트·자동 가공 NPC·디버그 콘솔 등
//   다양한 호출자에서 동일한 로직을 재사용할 수 있다.
//
// 검사 순서 (모두 통과해야 가공 성공):
//   1) 워크샵 종류 매칭 (recipe.requiredWorkbench != None 이면 wb 종류 일치 필수)
//   2) Tier 잠금 (TierService.IsUnlocked)
//   3) 재료 보유량 (Inventory.HasItems)
//   4) 결과 quality/ItemInstance 계산
//   5) 재료 차감 후의 인벤토리를 모의해 결과 스택 수용 가능성 확인
// 그 후에만:
//   6) 재료 차감 → Inventory.AddInstance 로 메타 보존 결과 추가
public static class CraftingService
{
    public static bool TryCraft(RecipeData recipe, Workbench workbench)
    {
        if (recipe == null || recipe.outputItem == null || recipe.outputCount <= 0)
        {
            Debug.LogWarning("🛠 CraftingService: 잘못된 RecipeData (null, outputItem 없음 또는 outputCount <= 0)");
            return false;
        }

        // 1. 워크샵 종류 매칭
        if (recipe.requiredWorkbench != WorkbenchType.None)
        {
            if (workbench == null || workbench.workbenchType != recipe.requiredWorkbench)
            {
                Debug.Log($"🚫 [{recipe.recipeName}] 이 작업대에서는 만들 수 없습니다 " +
                          $"(필요: {recipe.requiredWorkbench}, 현재: {(workbench != null ? workbench.workbenchType.ToString() : "없음")})");
                return false;
            }
        }

        // 2. 티어 잠금
        if (TierService.Instance != null && !TierService.Instance.IsUnlocked(recipe.requiredTier))
        {
            Debug.Log($"🔒 [{recipe.recipeName}] Tier {recipe.requiredTier} 이상 필요 " +
                      $"(현재 Tier {TierService.Instance.CurrentTier})");
            return false;
        }

        // 2-a. 히든 블루프린트 잠금 (FriendshipService 가 있을 때만 검사)
        //      히든 레시피이면 친밀도로 해금되기 전까지는 가공 자체를 차단한다.
        if (FriendshipService.Instance != null
            && !FriendshipService.Instance.IsRecipeUnlocked(recipe))
        {
            Debug.Log($"📜 [{recipe.recipeName}] 은(는) 아직 전수받지 못한 비법입니다 (친밀도 부족).");
            return false;
        }

        // 3. 재료 보유 확인
        if (Inventory.instance == null)
        {
            Debug.LogWarning("🛠 CraftingService: Inventory.instance 가 없습니다.");
            return false;
        }

        if (recipe.ingredients == null || recipe.ingredients.Count == 0)
        {
            Debug.LogWarning($"🛠 [{recipe.recipeName}] 재료 정의가 비어 있습니다.");
            return false;
        }

        foreach (var ing in recipe.ingredients)
        {
            if (ing == null || ing.item == null || ing.count <= 0) continue;
            if (!Inventory.instance.HasItems(ing.item, ing.count))
            {
                Debug.Log($"🚫 재료 부족: {ing.item.itemName} ×{ing.count}");
                return false;
            }
        }

        // 4. 재료 가중평균 quality — 차감될 스택의 quality 를 count 비례로 평균.
        //    Inventory.GetAverageQuality 가 hotbar → inventory 순으로 탐색하므로
        //    RemoveItems 의 차감 순서와 일치한다.
        float ingredientAvgQuality;
        {
            float totalQ     = 0f;
            int   totalCount = 0;
            foreach (var ing in recipe.ingredients)
            {
                if (ing == null || ing.item == null || ing.count <= 0) continue;
                float avg = Inventory.instance.GetAverageQuality(ing.item, ing.count);
                totalQ     += avg * ing.count;
                totalCount += ing.count;
            }
            ingredientAvgQuality = totalCount > 0 ? totalQ / totalCount : 1f;
        }

        // 5. 결과 quality 계산
        float resultQuality = recipe.baseOutputQuality
            + recipe.ingredientQualityWeight * (ingredientAvgQuality - 1f);
        resultQuality = Mathf.Clamp(resultQuality, 0.5f, 3f);

        // 6. 결과 ItemInstance를 먼저 만들고, 실제 차감 순서(hotbar → inventory)를
        //    모의한 뒤에도 메타 일치 스택 또는 빈 슬롯이 남는지 확인한다.
        var result = new ItemInstance(recipe.outputItem, recipe.outputCount)
        {
            quality = resultQuality
        };

        if (!CanReceiveAfterIngredientRemoval(result, recipe.ingredients))
        {
            Debug.LogWarning($"⚠️ 가방 공간 부족: 결과물 [{recipe.outputItem.itemName}] ×{recipe.outputCount}을 받을 수 없습니다. " +
                             "재료는 차감되지 않았습니다.");
            return false;
        }

        // 7. 수용 가능성이 확정된 뒤에만 재료를 차감한다.
        foreach (var ing in recipe.ingredients)
        {
            if (ing == null || ing.item == null || ing.count <= 0) continue;
            Inventory.instance.RemoveItems(ing.item, ing.count);
        }

        if (!Inventory.instance.AddInstance(result))
        {
            Debug.LogError($"❌ [{recipe.recipeName}] 결과 수용량 선검사 후 AddInstance가 실패했습니다. " +
                           "인벤토리 변경 콜백과 제작 트랜잭션을 점검해야 합니다.");
            return false;
        }

        workbench?.PlayCraftFeedback(recipe.outputItem.itemName);
        Debug.Log($"✅ 가공 완료: {recipe.outputItem.itemName} ×{recipe.outputCount} " +
                  $"(품질 {resultQuality:F2})");
        return true;
    }

    static bool CanReceiveAfterIngredientRemoval(ItemInstance result, List<RecipeIngredient> ingredients)
    {
        Inventory inventory = Inventory.instance;
        if (inventory == null || inventory.slots == null || result == null || result.data == null)
            return false;

        var remainingRemoval = new Dictionary<Item, int>();
        if (ingredients != null)
        {
            foreach (RecipeIngredient ingredient in ingredients)
            {
                if (ingredient == null || ingredient.item == null || ingredient.count <= 0) continue;
                remainingRemoval.TryGetValue(ingredient.item, out int count);
                remainingRemoval[ingredient.item] = count + ingredient.count;
            }
        }

        // RemoveItems와 같은 순서로 핫바 소모분을 먼저 계산한다.
        if (inventory.hotbar != null && inventory.hotbar.slots != null)
        {
            foreach (InventorySlot slot in inventory.hotbar.slots)
                ConsumeSimulated(slot, remainingRemoval);
        }

        int remainingOutput = result.count;
        int maxStack = Mathf.Max(1, result.data.maxStack);
        bool emptySlotAfterRemoval = false;

        foreach (InventorySlot slot in inventory.slots)
        {
            if (slot == null || slot.IsEmpty)
            {
                emptySlotAfterRemoval = true;
                continue;
            }

            int simulatedCount = slot.count;
            if (remainingRemoval.TryGetValue(slot.item, out int remove) && remove > 0)
            {
                int consumed = Mathf.Min(simulatedCount, remove);
                simulatedCount -= consumed;
                remainingRemoval[slot.item] = remove - consumed;
            }

            if (simulatedCount <= 0)
            {
                emptySlotAfterRemoval = true;
                continue;
            }

            if (slot.instance != null && slot.instance.CanStackWith(result))
            {
                remainingOutput -= Mathf.Max(0, maxStack - simulatedCount);
                if (remainingOutput <= 0) return true;
            }
        }

        // AddInstance는 메타 일치 스택을 채운 뒤 남은 결과를 첫 빈 슬롯에 넣는다.
        return remainingOutput <= 0 || emptySlotAfterRemoval;
    }

    static void ConsumeSimulated(InventorySlot slot, Dictionary<Item, int> remainingRemoval)
    {
        if (slot == null || slot.IsEmpty || slot.item == null) return;
        if (!remainingRemoval.TryGetValue(slot.item, out int remove) || remove <= 0) return;

        remainingRemoval[slot.item] = Mathf.Max(0, remove - slot.count);
    }
}
