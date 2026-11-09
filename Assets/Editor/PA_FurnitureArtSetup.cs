using System;
using UnityEditor;
using UnityEngine;

// 목제 가구(id 11, 공예품 185G)가 제작 목록·가방에서 의자(2021)와 같은 의자 아이콘으로 보이던 문제(2026-11-03 crafting 회귀 캡처).
// 서랍 두 칸 나무 수납장 모델(소유 8각 부품)로 바꾸고 아이콘을 다시 굽는다. 아이템 id·가격·레시피는 그대로.
// 배치: Unity -batchmode -executeMethod PA_FurnitureArtSetup.BatchApply
public static class PA_FurnitureArtSetup
{
    [MenuItem("Project PA/Content/Wooden Furniture Art (Cabinet)")]
    public static void Apply()
    {
        var item = AssetDatabase.LoadAssetAtPath<Item>("Assets/Resources/Items/Item_11_Furniture.asset")
            ?? throw new InvalidOperationException("Missing Item_11_Furniture");
        var wood = new Color(.66f, .42f, .22f); var dark = new Color(.42f, .26f, .13f); var light = new Color(.78f, .55f, .32f);
        var model = PA_DemoIconBake.OwnedModel("WoodenCabinet", go =>
        {
            PA_DemoIconBake.Part(go, "Body", new Vector3(0, .43f, 0), new Vector3(.72f, .62f, .42f), wood);
            PA_DemoIconBake.Part(go, "Top", new Vector3(0, .76f, 0), new Vector3(.8f, .05f, .48f), dark);
            PA_DemoIconBake.Part(go, "DrawerTop", new Vector3(0, .58f, -.215f), new Vector3(.62f, .22f, .03f), light);
            PA_DemoIconBake.Part(go, "DrawerLow", new Vector3(0, .31f, -.215f), new Vector3(.62f, .22f, .03f), light);
            PA_DemoIconBake.Part(go, "KnobTop", new Vector3(0, .58f, -.24f), new Vector3(.07f, .05f, .03f), dark);
            PA_DemoIconBake.Part(go, "KnobLow", new Vector3(0, .31f, -.24f), new Vector3(.07f, .05f, .03f), dark);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    PA_DemoIconBake.Part(go, $"Leg{x}{z}", new Vector3(x * .31f, .06f, z * .16f), new Vector3(.07f, .12f, .07f), dark);
        });
        item.model = model;
        item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        // 같은 산출물을 쓰는 레시피 아이콘도 맞춘다.
        foreach (var guid in AssetDatabase.FindAssets("t:RecipeData", new[] { "Assets/Resources" }))
        {
            var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(AssetDatabase.GUIDToAssetPath(guid));
            if (recipe != null && recipe.outputItem == item && recipe.icon != item.icon) { recipe.icon = item.icon; EditorUtility.SetDirty(recipe); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[FURNITURE-ART] Item_11_Furniture → WoodenCabinet, icon rebaked");
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[FURNITURE-ART] " + e); code = 1; }
        EditorApplication.Exit(code);
    }
}
