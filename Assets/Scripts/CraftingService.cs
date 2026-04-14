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
// 그 후:
//   4) 재료 차감
//   5) 결과 quality 계산: baseOutputQuality + ingredientQualityWeight × (재료평균 - 1)
//   6) ItemInstance 생성 → Inventory.AddInstance 로 메타 보존 추가
//
// 주의:
// - 재료 환원 트랜잭션 없음. 가방 풀 시 경고만 띄우고 false 반환 (재료는 이미 차감된 상태).
public static class CraftingService
{
    public static bool TryCraft(RecipeData recipe, Workbench workbench)
    {
        if (recipe == null || recipe.outputItem == null)
        {
            Debug.LogWarning("🛠 CraftingService: 잘못된 RecipeData (null 또는 outputItem 없음)");
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

        // 5. 재료 차감
        foreach (var ing in recipe.ingredients)
        {
            if (ing == null || ing.item == null || ing.count <= 0) continue;
            Inventory.instance.RemoveItems(ing.item, ing.count);
        }

        // 6. 결과 quality 계산
        float resultQuality = recipe.baseOutputQuality
            + recipe.ingredientQualityWeight * (ingredientAvgQuality - 1f);
        resultQuality = Mathf.Clamp(resultQuality, 0.5f, 3f);

        // 7. 결과 ItemInstance 생성 + 인벤토리 푸시 (메타 보존)
        var result = new ItemInstance(recipe.outputItem, recipe.outputCount)
        {
            quality = resultQuality
        };

        if (!Inventory.instance.AddInstance(result))
        {
            Debug.LogWarning($"⚠️ 가방이 가득 차 결과물 [{recipe.outputItem.itemName}] 수령 실패. " +
                             "재료는 이미 차감되었습니다.");
            return false;
        }

        Debug.Log($"✅ 가공 완료: {recipe.outputItem.itemName} ×{recipe.outputCount} " +
                  $"(품질 {resultQuality:F2})");
        return true;
    }
}
