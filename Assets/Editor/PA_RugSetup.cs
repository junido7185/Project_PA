using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-11-09 너굴 리스트 연구 H5 "러그 교체": 가게 실내 러그를 바꾸는 러그 아이템 둘(ShopRugSwap).
// 해초 러그(2060, 현장 작업대: 해초 3), 산호 러그(2061, 산호 조각 1 + 해초 1) — 섬에서 모은 것으로 짜는 러그(수집 → 꾸밈).
// 꾸밈 전용(값 없음, 쌓기 3). 모델은 소유 로우폴리: 반쯤 말린 러그(펼친 면 + 테두리 + 줄무늬 + 말린 끝).
// 배치: Unity -batchmode -executeMethod PA_RugSetup.BatchApply
public static class PA_RugSetup
{
    const string Items = "Assets/Resources/Items";
    const string Structure = "Assets/Resources/DemoStructure";
    const string Art = "Assets/Art/ProjectPA/Derived/ItemPresentation/Models";

    sealed class Rug { public int Id; public string Asset, Name, Recipe, Description; public Color Inner, Stripe; public (string res, int count)[] Inputs; }

    static readonly Rug[] Rugs =
    {
        new Rug { Id = 2060, Asset = "Item_RugSeaweed", Name = "해초 러그", Recipe = "Recipe_RugSeaweed", Inner = new Color(.45f, .63f, .34f), Stripe = new Color(.9f, .85f, .6f),
            Inputs = new[] { ("Items/Item_Seaweed", 3) }, Description = "말린 해초로 짠 초록 러그. 가게 안 생활 공간 러그 위에서 들고 [E]로 바꿔 깔아요(꾸밈 전용)." },
        new Rug { Id = 2061, Asset = "Item_RugCoral", Name = "산호 러그", Recipe = "Recipe_RugCoral", Inner = new Color(.93f, .64f, .57f), Stripe = new Color(.99f, .91f, .83f),
            Inputs = new[] { ("Items/Item_CoralPiece", 1), ("Items/Item_Seaweed", 1) }, Description = "산호 조각 빛으로 물들인 분홍 러그. 가게 안 생활 공간 러그 위에서 들고 [E]로 바꿔 깔아요(꾸밈 전용)." },
    };

    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        foreach (var r in Rugs)
        {
            string path = Items + "/" + r.Asset + ".asset";
            var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => p != path && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == r.Id);
            if (clash != null) throw new InvalidOperationException($"id {r.Id} already used by {clash}");
            var root = new GameObject("PA_Decor_" + r.Asset.Substring(5));
            GameObject model;
            try
            {
                var border = new Color(.88f, .78f, .52f);
                PA_DemoIconBake.Part(root, "RugBorder", new Vector3(0, .01f, .04f), new Vector3(.56f, .02f, .42f), border);
                PA_DemoIconBake.Part(root, "RugFace", new Vector3(0, .022f, .04f), new Vector3(.48f, .012f, .34f), r.Inner);
                PA_DemoIconBake.Part(root, "RugStripeA", new Vector3(0, .03f, .12f), new Vector3(.44f, .008f, .04f), r.Stripe);
                PA_DemoIconBake.Part(root, "RugStripeB", new Vector3(0, .03f, -.04f), new Vector3(.44f, .008f, .04f), r.Stripe);
                PA_DemoIconBake.Part(root, "RugRoll", new Vector3(0, .055f, -.2f), new Vector3(.56f, .11f, .12f), r.Inner, .75f);
                PA_DemoIconBake.Part(root, "RugRollEdge", new Vector3(0, .055f, -.2f), new Vector3(.58f, .08f, .08f), border, .8f);
                model = PrefabUtility.SaveAsPrefabAsset(root, Art + "/" + root.name + ".prefab");
            }
            finally { Object.DestroyImmediate(root); }
            var item = AssetDatabase.LoadAssetAtPath<Item>(path);
            if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = r.Id; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
            item.itemName = r.Name; item.description = r.Description; item.basePrice = 0; item.maxStack = 3;
            item.category = ItemCategory.Tool; item.toolType = ToolType.None; item.model = model;
            item.icon = PA_DemoIconBake.BakeIcon(item, model);
            EditorUtility.SetDirty(item);
            var inputs = r.Inputs.Select(i => (Resources.Load<Item>(i.res) ?? throw new InvalidOperationException("Missing " + i.res), i.count)).ToArray();
            Recipe(r.Recipe, r.Name, item, inputs);
            log.Add($"{r.Id} {r.Name} recipe {r.Recipe}");
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[RUG] " + string.Join(" | ", log));
    }

    static void Recipe(string asset, string name, Item output, params (Item item, int count)[] ingredients)
    {
        string path = Structure + "/" + asset + ".asset";
        var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
        if (recipe == null)
        {
            if (File.Exists(path)) throw new InvalidOperationException(path + " exists with an unexpected type.");
            recipe = ScriptableObject.CreateInstance<RecipeData>();
            AssetDatabase.CreateAsset(recipe, path);
        }
        recipe.recipeName = name; recipe.requiredWorkbench = WorkbenchType.BasicWorkbench; recipe.requiredTier = 0;
        recipe.outputItem = output; recipe.outputCount = 1; recipe.icon = output.icon;
        recipe.ingredients = ingredients.Select(i => new RecipeIngredient { item = i.item, count = i.count }).ToList();
        EditorUtility.SetDirty(recipe);
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[RUG] " + e); code = 1; }
        EditorApplication.Exit(code);
    }
}
