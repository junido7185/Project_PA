using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-11-09 해변 유리 펜던트(아이템 생태 연구 R05 "현장 작업대·유리1+구리 주괴1, 새 공예품 — 주괴를 도구에 쓸지 공예에 쓸지 선택"):
// 연구 표 대조에서 빠져 있던 승인 항목. 새 아이템 2062(Luxury 92G — 재료 18+38=56G, 장부 증분 +36, 조개 목걸이 +41보다 조금 낮게).
// 주민 특별 선호는 연구가 '별도 검토'로 남겨 두어 넣지 않는다(Luxury라 손님 선호는 기존 계산 그대로).
// 소유 모델: 구리 고리·짧은 구리 줄 + 물방울 모양 바다빛 유리 + 가는 끈.
// 배치: Unity -batchmode -executeMethod PA_GlassPendantSetup.BatchApply
public static class PA_GlassPendantSetup
{
    const string Items = "Assets/Resources/Items";
    const string Recipes = "Assets/Resources/DemoStructure";
    const string Art = "Assets/Art/ProjectPA/Derived/ItemPresentation/Models";
    public const int Pendant = 2062, Glass = 2047, CopperBar = 2029;

    static readonly Color Copper = new Color(.78f, .45f, .25f), GlassTeal = new Color(.45f, .82f, .78f), GlassLight = new Color(.75f, .95f, .9f), Cord = new Color(.35f, .26f, .18f);

    [MenuItem("Project PA/Content/Beach Glass Pendant (R05)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        string own = Items + "/Item_GlassPendant.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != own && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == Pendant);
        if (clash != null) throw new InvalidOperationException($"id {Pendant} already used by {clash}");
        var glass = Require("Items/Item_BeachGlass", Glass);
        var copper = Require("Items/Item_CopperBar", CopperBar);
        var model = Prefab("PA_Craft_GlassPendant", go =>
        {
            // 위에서 아래로: 끈 고리 → 구리 고리 → 구리 받침 → 물방울 유리(가운데 밝은 빛).
            PA_DemoIconBake.Part(go, "PendantCordL", new Vector3(-.05f, .3f, 0), new Vector3(.012f, .2f, .012f), Cord);
            go.transform.Find("PendantCordL").localRotation = Quaternion.Euler(0, 0, -20f);
            PA_DemoIconBake.Part(go, "PendantCordR", new Vector3(.05f, .3f, 0), new Vector3(.012f, .2f, .012f), Cord);
            go.transform.Find("PendantCordR").localRotation = Quaternion.Euler(0, 0, 20f);
            PA_DemoIconBake.Part(go, "PendantBail", new Vector3(0, .2f, 0), new Vector3(.05f, .04f, .03f), Copper, .7f);
            PA_DemoIconBake.Part(go, "PendantCap", new Vector3(0, .165f, 0), new Vector3(.08f, .025f, .05f), Copper, .8f);
            var drop = new GameObject("PA_Craft_GlassPendant_Drop");
            drop.transform.SetParent(go.transform, false);
            drop.transform.localPosition = new Vector3(0, .155f, 0);
            drop.transform.localRotation = Quaternion.Euler(180f, 0f, 0f); // 아래로 뻗는 물방울
            drop.transform.localScale = new Vector3(1f, 1f, .55f);
            PA_DigSetup.Round(drop, "Glass", Vector3.zero,
                new[] { PA_DigSetup.V(.035f, 0), PA_DigSetup.V(.055f, .04f), PA_DigSetup.V(.06f, .08f), PA_DigSetup.V(.045f, .12f), PA_DigSetup.V(0, .15f) },
                GlassTeal, 10);
            PA_DemoIconBake.Part(go, "PendantGlint", new Vector3(-.018f, .09f, -.03f), new Vector3(.018f, .03f, .008f), GlassLight);
        });
        var item = AssetDatabase.LoadAssetAtPath<Item>(own);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = Pendant; AssetDatabase.CreateAsset(item, own); log.Add("created " + own); }
        item.itemName = "해변 유리 펜던트"; item.basePrice = 92; item.maxStack = 10; item.category = ItemCategory.Luxury; item.toolType = ToolType.None;
        item.description = "바다에 닳은 유리 조각을 구리 받침에 감싼 펜던트. 구리 주괴를 도구 대신 공예에 쓴 선택이에요. 관광객이 좋아해요.";
        item.model = model; item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("baked icon " + Pendant);
        string recipePath = Recipes + "/Recipe_GlassPendant.asset";
        var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(recipePath);
        if (recipe == null)
        {
            if (File.Exists(recipePath)) throw new InvalidOperationException(recipePath + " exists with an unexpected type.");
            recipe = ScriptableObject.CreateInstance<RecipeData>();
            AssetDatabase.CreateAsset(recipe, recipePath);
        }
        recipe.recipeName = "해변 유리 펜던트"; recipe.requiredWorkbench = WorkbenchType.BasicWorkbench; recipe.requiredTier = 0;
        recipe.outputItem = item; recipe.outputCount = 1; recipe.icon = item.icon;
        recipe.ingredients = new List<RecipeIngredient> { new RecipeIngredient { item = glass, count = 1 }, new RecipeIngredient { item = copper, count = 1 } };
        EditorUtility.SetDirty(recipe);
        log.Add("recipe Recipe_GlassPendant (workbench, 유리1+구리 주괴1 → 2062)");
        AssetDatabase.SaveAssets();
        Debug.Log("[PENDANT] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[PENDANT] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static Item Require(string resource, int id)
    {
        var item = Resources.Load<Item>(resource);
        if (item == null || item.id != id) throw new InvalidOperationException($"Expected existing item id {id} at Resources/{resource}.");
        return item;
    }

    static GameObject Prefab(string name, Action<GameObject> build)
    {
        var root = new GameObject(name);
        try { build(root); return PrefabUtility.SaveAsPrefabAsset(root, Art + "/" + name + ".prefab"); }
        finally { Object.DestroyImmediate(root); }
    }
}
