using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 넙치(Docs/01_GAME_DESIGN/Content/FLOUNDER.md, 아이템 생태 연구 F06 owner 채택 2026-11-04).
// 새 아이템: 넙치(3005, 원재료 17G). 새 산출물은 만들지 않는다 — 기존 생선구이(10)의 대체 재료(넙치1, 조리대).
// 월드 그림자(넓적한 몸·잠깐 움직이고 오래 쉬기)·출현 비율은 FishingSpot/ShoreFishSchool이 맡는다(종 번호 5).
// 멱등: 아이템·레시피·모델·아이콘을 매번 정의대로 다시 쓴다.
// 배치: Unity -batchmode -executeMethod PA_FlounderSetup.BatchApply
public static class PA_FlounderSetup
{
    const string Items = "Assets/Resources/Items";
    const string Recipes = "Assets/Resources/DemoStructure";
    const string Art = "Assets/Art/ProjectPA/Derived/Fish";

    public const int Flounder = 3005, GrilledFish = 10;

    static readonly Color Sand = new Color(.72f, .6f, .44f), Spot = new Color(.46f, .36f, .26f), SpotLight = new Color(.88f, .8f, .64f);
    static readonly Color Fin = new Color(.55f, .45f, .33f), Eye = new Color(.08f, .09f, .1f);

    [MenuItem("Project PA/Content/Flounder (Fish · Grilled alt)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        Preflight();
        Folder(Art);
        var fish = Prefab("PA_Fish_Flounder", go =>
        {
            // 몸: 납작한 타원 원반(돌림판을 세우지 않고 눕힌 채 x .78로 좁힘), +Z가 머리. 윗면은 모래색 점박이, 테두리는 짙은 지느러미 띠.
            var body = new GameObject("PA_Fish_Flounder_Pivot");
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(.78f, 1f, 1f);
            PA_DigSetup.Round(body, "Fringe", new Vector3(0, -.006f, 0),
                new[] { PA_DigSetup.V(.33f, 0), PA_DigSetup.V(.34f, .012f), PA_DigSetup.V(.3f, .02f), PA_DigSetup.V(0, .02f) }, Fin, 16);
            PA_DigSetup.Round(body, "Body", Vector3.zero,
                new[] { PA_DigSetup.V(.28f, 0), PA_DigSetup.V(.29f, .02f), PA_DigSetup.V(.24f, .05f), PA_DigSetup.V(.12f, .066f), PA_DigSetup.V(0, .07f) }, Sand, 16);
            // 윗면 점무늬(모래에 숨는 무늬).
            var spots = new[] { new Vector3(.08f, 0, .05f), new Vector3(-.09f, 0, -.04f), new Vector3(.05f, 0, -.13f), new Vector3(-.04f, 0, .14f), new Vector3(.13f, 0, -.06f), new Vector3(-.13f, 0, .06f) };
            for (int i = 0; i < spots.Length; i++)
                PA_DemoIconBake.Part(go, "Spot" + i, spots[i] + new Vector3(0, .062f, 0), new Vector3(.045f, .01f, .045f), i % 2 == 0 ? Spot : SpotLight, .8f);
            // 넙치는 두 눈이 윗면 한쪽에 모여 있다.
            PA_DemoIconBake.Part(go, "EyeA", new Vector3(.05f, .078f, .2f), new Vector3(.03f, .028f, .03f), Eye);
            PA_DemoIconBake.Part(go, "EyeB", new Vector3(.1f, .074f, .16f), new Vector3(.028f, .026f, .028f), Eye);
            PA_DemoIconBake.Part(go, "Tail", new Vector3(0, .02f, -.33f), new Vector3(.14f, .012f, .1f), Fin, .6f);
        });
        var flounder = BuildItem(Flounder, "Item_Fish_Flounder", "넙치", 17, 99, ItemCategory.Raw, fish,
            "모래 바닥에 납작하게 붙어 쉬는 물고기. 넓적한 그림자가 잠깐 움직였다가 오래 멈춰 있어요. 조리대에서 생선구이로 구울 수 있어요.", log);
        var grilled = AssetDatabase.LoadAssetAtPath<Item>(Items + "/Item_10_GrilledFish.asset");
        if (grilled == null || grilled.id != GrilledFish) throw new InvalidOperationException("생선구이(10) 아이템이 없어 대체 레시피를 만들 수 없습니다.");
        Recipe("Recipe_GrilledFlounder", "생선구이 · 넙치", WorkbenchType.Kitchen, grilled, (flounder, 1));
        log.Add("recipe Recipe_GrilledFlounder (kitchen → 10)");
        AssetDatabase.SaveAssets();
        Debug.Log("[FLOUNDER-SETUP] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[FLOUNDER-SETUP] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static void Preflight()
    {
        string own = Items + "/Item_Fish_Flounder.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != own && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == Flounder);
        if (clash != null) throw new InvalidOperationException($"id {Flounder} already used by {clash}");
    }

    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        Folder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static GameObject Prefab(string name, Action<GameObject> build)
    {
        var root = new GameObject(name);
        try { build(root); return PrefabUtility.SaveAsPrefabAsset(root, Art + "/" + name + ".prefab"); }
        finally { Object.DestroyImmediate(root); }
    }

    static Item BuildItem(int id, string asset, string name, int price, int maxStack, ItemCategory category, GameObject model, string description, List<string> log)
    {
        string path = Items + "/" + asset + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<Item>();
            item.id = id;
            AssetDatabase.CreateAsset(item, path);
            log.Add($"created {path}");
        }
        item.itemName = name; item.description = description; item.basePrice = price;
        item.maxStack = maxStack; item.category = category; item.toolType = ToolType.None;
        item.model = model;
        item.icon = PA_DemoIconBake.BakeIcon(item, model); log.Add("baked icon " + id);
        EditorUtility.SetDirty(item);
        return item;
    }

    static void Recipe(string asset, string name, WorkbenchType bench, Item output, params (Item item, int count)[] ingredients)
    {
        string path = Recipes + "/" + asset + ".asset";
        var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
        if (recipe == null)
        {
            if (File.Exists(path)) throw new InvalidOperationException(path + " exists with an unexpected type.");
            recipe = ScriptableObject.CreateInstance<RecipeData>();
            AssetDatabase.CreateAsset(recipe, path);
        }
        recipe.recipeName = name; recipe.requiredWorkbench = bench; recipe.requiredTier = 0;
        recipe.outputItem = output; recipe.outputCount = 1; recipe.icon = output.icon;
        recipe.ingredients = ingredients.Select(i => new RecipeIngredient { item = i.item, count = i.count }).ToList();
        EditorUtility.SetDirty(recipe);
    }
}
