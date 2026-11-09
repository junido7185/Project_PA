using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-11-09 자리 물고기(아이템 생태 연구 F07·F08, owner 채택): 섬에 이미 있는 항구·물가 바위를 '구역'으로 쓴다.
// 숭어(3006, 20G) — 실제 종 Mugil cephalus, 항구 앞 물가에서만(연구 F07 "항구/하구 후보"). 기존 생선구이(10)의 대체 재료(조리대, 숭어1).
// 복어(3007, 30G) — 실제 종 졸복류(Takifugu), 물가 바위 곁에서만(연구 F08 "바위 사이 해안"). 독이 있어 요리에 넣지 않는다(연구: 범용 요리·필수 납품 제외) — 팔거나 도감.
// 출현 구역·그림자 모양은 FishingSpot(종 6·7)·ShoreFishSchool이 맡는다.
// 배치: Unity -batchmode -executeMethod PA_ZoneFishSetup.BatchApply
public static class PA_ZoneFishSetup
{
    const string Items = "Assets/Resources/Items";
    const string Recipes = "Assets/Resources/DemoStructure";
    const string Art = "Assets/Art/ProjectPA/Derived/Fish";
    public const int Mullet = 3006, Puffer = 3007, GrilledFish = 10;

    static readonly Color Silver = new Color(.78f, .8f, .82f), MulletBack = new Color(.42f, .48f, .52f), MulletLine = new Color(.55f, .6f, .64f);
    static readonly Color PufferBack = new Color(.62f, .55f, .32f), PufferBelly = new Color(.95f, .93f, .86f), PufferSpot = new Color(.24f, .22f, .16f);
    static readonly Color Fin = new Color(.6f, .62f, .6f), Eye = new Color(.08f, .09f, .1f), EyeRing = new Color(.92f, .8f, .3f);

    [MenuItem("Project PA/Content/Zone Fish (Mullet · Puffer)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        foreach (var (id, asset) in new[] { (Mullet, "Item_Fish_Mullet"), (Puffer, "Item_Fish_Puffer") })
        {
            string own = Items + "/" + asset + ".asset";
            var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => p != own && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == id);
            if (clash != null) throw new InvalidOperationException($"id {id} already used by {clash}");
        }
        var mulletModel = Prefab("PA_Fish_Mullet", go =>
        {
            // 길쭉한 은빛 몸(+Z 머리), 등은 짙은 회청, 옆구리에 가는 세로 줄 몇 개, 머리는 납작하고 둥글다.
            var body = new GameObject("PA_Fish_Mullet_Pivot");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0, 0, -.45f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(.7f, 1f, 1f);
            PA_DigSetup.Round(body, "Body", Vector3.zero,
                new[] { PA_DigSetup.V(.014f, 0), PA_DigSetup.V(.036f, .1f), PA_DigSetup.V(.07f, .3f), PA_DigSetup.V(.078f, .52f), PA_DigSetup.V(.07f, .72f), PA_DigSetup.V(.05f, .84f), PA_DigSetup.V(0, .9f) },
                Silver, 10);
            PA_DemoIconBake.Part(go, "BackBand", new Vector3(0, .058f, .02f), new Vector3(.05f, .025f, .7f), MulletBack, .7f);
            for (int i = -1; i <= 1; i += 2)
                PA_DemoIconBake.Part(go, "SideLine" + (i < 0 ? "L" : "R"), new Vector3(i * .052f, .025f, 0), new Vector3(.006f, .01f, .58f), MulletLine);
            PA_DemoIconBake.Part(go, "DorsalA", new Vector3(0, .085f, .08f), new Vector3(.01f, .04f, .07f), Fin, .5f);
            PA_DemoIconBake.Part(go, "DorsalB", new Vector3(0, .078f, -.14f), new Vector3(.01f, .035f, .06f), Fin, .5f);
            PA_DemoIconBake.Part(go, "Tail", new Vector3(0, .01f, -.5f), new Vector3(.02f, .12f, .09f), Fin, .5f);
            PA_DemoIconBake.Part(go, "EyeL", new Vector3(-.045f, .03f, .38f), new Vector3(.012f, .026f, .026f), Eye);
            PA_DemoIconBake.Part(go, "EyeR", new Vector3(.045f, .03f, .38f), new Vector3(.012f, .026f, .026f), Eye);
        });
        var pufferModel = Prefab("PA_Fish_Puffer", go =>
        {
            // 동글동글한 몸(돌림판을 앞뒤로 눕힘), 등은 누런 갈색에 검은 점, 배는 흰색, 큰 눈(노란 테), 작은 지느러미·꼬리.
            var body = new GameObject("PA_Fish_Puffer_Pivot");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0, .02f, -.2f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(1f, 1f, .92f);
            PA_DigSetup.Round(body, "Body", Vector3.zero,
                new[] { PA_DigSetup.V(.04f, 0), PA_DigSetup.V(.12f, .08f), PA_DigSetup.V(.16f, .2f), PA_DigSetup.V(.15f, .3f), PA_DigSetup.V(.1f, .38f), PA_DigSetup.V(0, .42f) },
                PufferBelly, 12);
            PA_DemoIconBake.Part(go, "Back", new Vector3(0, .12f, 0), new Vector3(.22f, .06f, .3f), PufferBack, .7f);
            var spots = new[] { new Vector3(.05f, 0, .06f), new Vector3(-.06f, 0, -.02f), new Vector3(.04f, 0, -.09f), new Vector3(-.03f, 0, .1f), new Vector3(.08f, 0, -.03f) };
            for (int i = 0; i < spots.Length; i++)
                PA_DemoIconBake.Part(go, "Spot" + i, spots[i] + new Vector3(0, .152f, 0), new Vector3(.03f, .008f, .03f), PufferSpot, .8f);
            for (int i = -1; i <= 1; i += 2)
            {
                string s = i < 0 ? "L" : "R";
                PA_DemoIconBake.Part(go, "EyeRing" + s, new Vector3(i * .1f, .1f, .12f), new Vector3(.02f, .046f, .046f), EyeRing);
                PA_DemoIconBake.Part(go, "Eye" + s, new Vector3(i * .108f, .1f, .12f), new Vector3(.012f, .03f, .03f), Eye);
                PA_DemoIconBake.Part(go, "Pectoral" + s, new Vector3(i * .15f, .05f, .02f), new Vector3(.03f, .05f, .04f), Fin, .6f);
            }
            PA_DemoIconBake.Part(go, "Tail", new Vector3(0, .05f, -.24f), new Vector3(.02f, .08f, .07f), Fin, .5f);
        });
        var mullet = BuildItem(Mullet, "Item_Fish_Mullet", "숭어", 20, 99, mulletModel,
            "항구 앞 물가에서 길쭉한 은빛 그림자로 줄지어 다니는 물고기. 조리대에서 생선구이로 구울 수 있어요.", log);
        BuildItem(Puffer, "Item_Fish_Puffer", "복어", 30, 99, pufferModel,
            "물가 바위 곁을 동글동글 느리게 다니는 물고기. 몸에 독이 있어 요리에는 쓰지 않아요 — 팔거나 도감에 남겨요.", log);
        var grilled = AssetDatabase.LoadAssetAtPath<Item>(Items + "/Item_10_GrilledFish.asset");
        if (grilled == null || grilled.id != GrilledFish) throw new InvalidOperationException("생선구이(10) 아이템이 없어 대체 레시피를 만들 수 없습니다.");
        Recipe("Recipe_GrilledMullet", "생선구이 · 숭어", WorkbenchType.Kitchen, grilled, (mullet, 1));
        log.Add("recipe Recipe_GrilledMullet (kitchen → 10)");
        AssetDatabase.SaveAssets();
        Debug.Log("[ZONE-FISH] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[ZONE-FISH] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static GameObject Prefab(string name, Action<GameObject> build)
    {
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder(Path.GetDirectoryName(Art).Replace('\\', '/'), Path.GetFileName(Art));
        var root = new GameObject(name);
        try { build(root); return PrefabUtility.SaveAsPrefabAsset(root, Art + "/" + name + ".prefab"); }
        finally { Object.DestroyImmediate(root); }
    }

    static Item BuildItem(int id, string asset, string name, int price, int maxStack, GameObject model, string description, List<string> log)
    {
        string path = Items + "/" + asset + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = id; AssetDatabase.CreateAsset(item, path); log.Add($"created {path}"); }
        item.itemName = name; item.description = description; item.basePrice = price;
        item.maxStack = maxStack; item.category = ItemCategory.Raw; item.toolType = ToolType.None;
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
