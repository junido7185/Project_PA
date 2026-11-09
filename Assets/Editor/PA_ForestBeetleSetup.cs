using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 장수풍뎅이(Content-insects-plan-20261026 §B, owner 인수 2026-11-04). Item_Bug_Beetle id 3101, 원재료 24G(나비 12G의 두 배), 쌓기 20.
// 모델은 소유 로우폴리(등껍질 돌림판 + 머리·뿔·다리 8각 부품). 기존 아이템·id는 지우거나 바꾸지 않고, id가 겹치면 멈춘다.
// 배치: Unity -batchmode -executeMethod PA_ForestBeetleSetup.BatchApply
public static class PA_ForestBeetleSetup
{
    const string Items = "Assets/Resources/Items";
    const string Art = "Assets/Art/ProjectPA/Derived/Insects";
    public const int Beetle = 3101;

    static readonly Color Shell = new Color(.36f, .2f, .11f), ShellLight = new Color(.5f, .3f, .16f), Head = new Color(.18f, .11f, .07f), Leg = new Color(.14f, .09f, .06f);

    [MenuItem("Project PA/Content/Forest Beetle (Insect)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        string path = Items + "/Item_Bug_Beetle.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != path && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == Beetle);
        if (clash != null) throw new InvalidOperationException($"id {Beetle} already used by {clash}");
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Art/ProjectPA/Derived", "Insects");
        var log = new List<string>();
        var root = new GameObject("PA_Insect_ForestBeetle");
        GameObject model;
        try
        {
            // 몸 +Z가 머리. 등껍질은 Y축 돌림판을 눕혀(+Z) 옆으로 넓게, 위아래로 납작하게.
            var pivot = new GameObject("PA_Insect_ForestBeetle_Pivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0, .09f, -.17f);
            pivot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pivot.transform.localScale = new Vector3(1.05f, 1f, .62f);
            PA_DigSetup.Round(pivot, "Shell", Vector3.zero,
                new[] { PA_DigSetup.V(.04f, 0), PA_DigSetup.V(.11f, .05f), PA_DigSetup.V(.14f, .14f), PA_DigSetup.V(.13f, .24f), PA_DigSetup.V(.09f, .3f), PA_DigSetup.V(0, .32f) },
                Shell, 12);
            PA_DemoIconBake.Part(root, "BeetleSeam", new Vector3(0, .175f, -.02f), new Vector3(.012f, .01f, .28f), Head);
            PA_DemoIconBake.Part(root, "BeetleShine", new Vector3(-.04f, .17f, -.04f), new Vector3(.05f, .012f, .1f), ShellLight, .6f);
            PA_DemoIconBake.Part(root, "BeetleThorax", new Vector3(0, .09f, .17f), new Vector3(.16f, .09f, .09f), Head, .8f);
            PA_DemoIconBake.Part(root, "BeetleHead", new Vector3(0, .07f, .24f), new Vector3(.09f, .06f, .06f), Head, .8f);
            // 장수풍뎅이다운 큰 뿔(위로 휜 앞뿔 + 가슴 뿔).
            PA_DemoIconBake.Part(root, "BeetleHorn", new Vector3(0, .12f, .32f), new Vector3(.025f, .025f, .14f), Head, .7f);
            root.transform.Find("BeetleHorn").localRotation = Quaternion.Euler(-35f, 0f, 0f);
            PA_DemoIconBake.Part(root, "BeetleHornTip", new Vector3(0, .17f, .37f), new Vector3(.04f, .02f, .02f), Head);
            PA_DemoIconBake.Part(root, "BeetleChestHorn", new Vector3(0, .15f, .2f), new Vector3(.02f, .02f, .1f), Head, .7f);
            root.transform.Find("BeetleChestHorn").localRotation = Quaternion.Euler(-20f, 0f, 0f);
            for (int side = -1; side <= 1; side += 2)
                for (int k = 0; k < 3; k++)
                {
                    string n = $"BeetleLeg{(side < 0 ? "L" : "R")}{k}";
                    PA_DemoIconBake.Part(root, n, new Vector3(side * .14f, .03f, .12f - k * .12f), new Vector3(.12f, .018f, .018f), Leg);
                    root.transform.Find(n).localRotation = Quaternion.Euler(0f, side * (20f - k * 20f), side * -25f);
                }
            model = PrefabUtility.SaveAsPrefabAsset(root, Art + "/PA_Insect_ForestBeetle.prefab");
        }
        finally { Object.DestroyImmediate(root); }
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = Beetle; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
        item.itemName = "장수풍뎅이"; item.basePrice = 24; item.maxStack = 20; item.category = ItemCategory.Raw; item.toolType = ToolType.None;
        item.description = "숲 가장자리 나무 밑동에 붙어 있는 큰 뿔 딱정벌레. 달려가면 먼저 날아가니 천천히 다가가 잠자리채로 잡아요. 나비보다 두 배 받아요.";
        item.model = model; item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("baked icon " + Beetle);
        BuildSpecimenBox(item, model, log);
        AssetDatabase.SaveAssets();
        Debug.Log("[FOREST-BEETLE] " + string.Join(" | ", log));
    }

    // 2026-11-04 장수풍뎅이 표본 상자(계획서 §C.2 곤충의 공예 쓰임): 현장 작업대 장수풍뎅이 1 + 판재 1 → 공예품 70G(재료 49G).
    // 나비 표본 액자(2025, 48G)와 같은 결 — 판재 테두리 + 리넨 배경 + 핀으로 고정한 표본, 받침에 세움(정면 −Z).
    public const int SpecimenBox = 2056;
    static void BuildSpecimenBox(Item beetle, GameObject beetleModel, List<string> log)
    {
        string path = Items + "/Item_BeetleSpecimenBox.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != path && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == SpecimenBox);
        if (clash != null) throw new InvalidOperationException($"id {SpecimenBox} already used by {clash}");
        var plank = Resources.Load<Item>("Items/Item_Plank");
        if (plank == null || plank.id != 7) throw new InvalidOperationException("Expected Items/Item_Plank id 7");
        var root = new GameObject("PA_Insect_BeetleBox");
        GameObject box;
        try
        {
            var wood = new Color(.42f, .26f, .13f); var dark = new Color(.28f, .17f, .09f);
            PA_DemoIconBake.Part(root, "BoxBacking", new Vector3(0, .38f, .02f), new Vector3(.56f, .5f, .03f), new Color(.9f, .86f, .74f));
            PA_DemoIconBake.Part(root, "BoxTop", new Vector3(0, .655f, 0), new Vector3(.66f, .06f, .09f), wood);
            PA_DemoIconBake.Part(root, "BoxBottom", new Vector3(0, .105f, 0), new Vector3(.66f, .06f, .09f), wood);
            PA_DemoIconBake.Part(root, "BoxLeft", new Vector3(-.3f, .38f, 0), new Vector3(.06f, .5f, .09f), wood);
            PA_DemoIconBake.Part(root, "BoxRight", new Vector3(.3f, .38f, 0), new Vector3(.06f, .5f, .09f), wood);
            PA_DemoIconBake.Part(root, "BoxStand", new Vector3(0, .04f, .06f), new Vector3(.42f, .08f, .22f), dark);
            PA_DemoIconBake.Part(root, "BoxLabel", new Vector3(0, .17f, -.005f), new Vector3(.2f, .04f, .01f), new Color(.97f, .95f, .88f));
            PA_DemoIconBake.AddModel(root, beetleModel, new Vector3(0, .42f, -.02f), .32f);
            // 등을 정면으로 세운다(뿔이 위).
            var pinned = root.transform.GetChild(root.transform.childCount - 1);
            pinned.localRotation = Quaternion.Euler(-90f, 0f, 0f) * pinned.localRotation;
            box = PrefabUtility.SaveAsPrefabAsset(root, Art + "/PA_Insect_BeetleBox.prefab");
        }
        finally { Object.DestroyImmediate(root); }
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = SpecimenBox; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
        item.itemName = "장수풍뎅이 표본 상자"; item.basePrice = 70; item.maxStack = 8; item.category = ItemCategory.Luxury; item.toolType = ToolType.None;
        item.description = "판재 상자에 장수풍뎅이를 핀으로 고정한 표본. 그냥 파는 것보다 비싸고, 관광객이 기념품으로 좋아해요.";
        item.model = box; item.icon = PA_DemoIconBake.BakeIcon(item, box);
        EditorUtility.SetDirty(item);
        string recipePath = "Assets/Resources/DemoStructure/Recipe_BeetleSpecimenBox.asset";
        var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(recipePath);
        if (recipe == null) { recipe = ScriptableObject.CreateInstance<RecipeData>(); AssetDatabase.CreateAsset(recipe, recipePath); }
        recipe.recipeName = "장수풍뎅이 표본 상자"; recipe.requiredWorkbench = WorkbenchType.BasicWorkbench; recipe.requiredTier = 0;
        recipe.outputItem = item; recipe.outputCount = 1; recipe.icon = item.icon;
        recipe.ingredients = new List<RecipeIngredient> { new RecipeIngredient { item = beetle, count = 1 }, new RecipeIngredient { item = plank, count = 1 } };
        EditorUtility.SetDirty(recipe);
        log.Add("recipe Recipe_BeetleSpecimenBox (workbench) · item " + SpecimenBox);
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[FOREST-BEETLE] " + e); code = 1; }
        EditorApplication.Exit(code);
    }
}
