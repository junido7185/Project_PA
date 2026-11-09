using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-11-09 담수·하구 물고기(아이템 생태 연구 F09·F12): 생성 섬에 이미 있는 강(River 80칸)·하구(강이 바다와 닿는 곳)를 구역으로 쓴다.
// 송어(3008, 25G) — 실제 종 송어(Oncorhynchus masou, 강에 남은 것은 산천어). 강(하구 제외)에서 06–18시. 기존 훈제 생선(2033)의 대체 재료(조리대, 송어1 + 나무2).
// 장어(3009, 35G) — 실제 종 뱀장어(Anguilla japonica). 하구에서 14–24시. 기존 생선구이(10)의 대체 재료(조리대, 장어1).
// 출현 구역·시간·그림자(장어는 마디가 굽이침)는 FishingSpot(종 8·9)·ShoreFishSchool이 맡는다.
// 배치: Unity -batchmode -executeMethod PA_FreshFishSetup.BatchApply
public static class PA_FreshFishSetup
{
    const string Items = "Assets/Resources/Items";
    const string Recipes = "Assets/Resources/DemoStructure";
    const string Art = "Assets/Art/ProjectPA/Derived/Fish";
    public const int Trout = 3008, Eel = 3009, Crucian = 3010, GrilledFish = 10, SmokedFish = 2033;
    // 붕어(3010, 16G) — 실제 종 붕어(Carassius auratus). 연못(고지대 구덩이 3×3)에서만 06–20시, 쉬운 입문종. 생선구이(10)의 대체 재료(조리대, 붕어1).
    static readonly Color CrucianBody = new Color(.7f, .6f, .36f), CrucianBack = new Color(.4f, .38f, .24f), CrucianScale = new Color(.82f, .72f, .46f);

    static readonly Color TroutSide = new Color(.8f, .8f, .74f), TroutBack = new Color(.36f, .42f, .3f), TroutBand = new Color(.86f, .6f, .58f), TroutSpot = new Color(.16f, .18f, .14f);
    static readonly Color EelBack = new Color(.24f, .26f, .16f), EelBelly = new Color(.78f, .74f, .5f);
    static readonly Color Fin = new Color(.6f, .62f, .55f), Eye = new Color(.08f, .09f, .1f);

    [MenuItem("Project PA/Content/Fresh Fish (Trout · Eel)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        foreach (var (id, asset) in new[] { (Trout, "Item_Fish_Trout"), (Eel, "Item_Fish_Eel"), (Crucian, "Item_Fish_Crucian") })
        {
            string own = Items + "/" + asset + ".asset";
            var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => p != own && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == id);
            if (clash != null) throw new InvalidOperationException($"id {id} already used by {clash}");
        }
        var troutModel = Prefab("PA_Fish_Trout", go =>
        {
            // 날렵한 은빛 몸(+Z 머리), 올리브빛 등에 검은 점, 옆구리 분홍빛 띠, 작은 기름지느러미.
            var body = new GameObject("PA_Fish_Trout_Pivot");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0, 0, -.4f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(.62f, 1f, 1f);
            PA_DigSetup.Round(body, "Body", Vector3.zero,
                new[] { PA_DigSetup.V(.012f, 0), PA_DigSetup.V(.035f, .1f), PA_DigSetup.V(.075f, .3f), PA_DigSetup.V(.085f, .48f), PA_DigSetup.V(.072f, .66f), PA_DigSetup.V(.045f, .76f), PA_DigSetup.V(0, .82f) },
                TroutSide, 10);
            PA_DemoIconBake.Part(go, "BackBand", new Vector3(0, .065f, .02f), new Vector3(.05f, .028f, .6f), TroutBack, .7f);
            for (int i = -1; i <= 1; i += 2)
                PA_DemoIconBake.Part(go, "PinkBand" + (i < 0 ? "L" : "R"), new Vector3(i * .05f, .015f, .02f), new Vector3(.008f, .03f, .5f), TroutBand, .8f);
            var spots = new[] { new Vector3(.02f, 0, .16f), new Vector3(-.025f, 0, .06f), new Vector3(.03f, 0, -.05f), new Vector3(-.02f, 0, -.15f), new Vector3(.015f, 0, -.24f) };
            for (int i = 0; i < spots.Length; i++)
                PA_DemoIconBake.Part(go, "Spot" + i, spots[i] + new Vector3(0, .082f, 0), new Vector3(.018f, .006f, .018f), TroutSpot, .8f);
            PA_DemoIconBake.Part(go, "Dorsal", new Vector3(0, .1f, .02f), new Vector3(.01f, .045f, .08f), Fin, .5f);
            PA_DemoIconBake.Part(go, "Adipose", new Vector3(0, .08f, -.24f), new Vector3(.008f, .022f, .03f), Fin, .6f);
            PA_DemoIconBake.Part(go, "Tail", new Vector3(0, .01f, -.45f), new Vector3(.02f, .13f, .09f), Fin, .5f);
            PA_DemoIconBake.Part(go, "EyeL", new Vector3(-.045f, .03f, .32f), new Vector3(.012f, .024f, .024f), Eye);
            PA_DemoIconBake.Part(go, "EyeR", new Vector3(.045f, .03f, .32f), new Vector3(.012f, .024f, .024f), Eye);
        });
        var eelModel = Prefab("PA_Fish_Eel", go =>
        {
            // 길고 가는 원통 몸을 네 마디로 S자로 굽혀 둔다(굽이치는 몸). 등은 짙은 올리브, 배는 누런빛, 등·배를 따라 긴 지느러미 띠.
            var path = new[] { new Vector3(0, 0, .42f), new Vector3(.05f, 0, .2f), new Vector3(-.02f, 0, -.02f), new Vector3(-.06f, 0, -.24f), new Vector3(.02f, 0, -.46f) };
            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector3 a = path[i], b = path[i + 1], mid = (a + b) * .5f, dir = (b - a);
                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                var seg = new GameObject("PA_Fish_Eel_Seg" + i);
                seg.transform.SetParent(go.transform, false);
                seg.transform.localPosition = a + Vector3.up * .035f;
                float r0 = i == 0 ? .028f : .04f - i * .004f, r1 = .04f - (i + 1) * .006f;
                float len = dir.magnitude + .02f;
                // 돌림판은 +Y로 뻗는다 → X 90°로 눕히면 -Z쪽이 아니라 +Z로 간다. 머리(a)에서 꼬리(b) 방향으로 yaw를 맞췄다.
                seg.transform.localRotation = Quaternion.Euler(0f, yaw + 180f, 0f) * Quaternion.Euler(-90f, 0f, 0f);
                PA_DigSetup.Round(seg, "Body", Vector3.zero,
                    new[] { PA_DigSetup.V(r0, 0), PA_DigSetup.V((r0 + r1) * .5f + .004f, len * .5f), PA_DigSetup.V(r1, len) },
                    EelBack, 10);
                PA_DemoIconBake.Part(go, "Belly" + i, mid + new Vector3(0, .012f, 0), new Vector3(.045f - i * .006f, .012f, len * .9f), EelBelly, .6f);
                go.transform.Find("Belly" + i).localRotation = Quaternion.Euler(0f, yaw, 0f);
                if (i > 0)
                {
                    PA_DemoIconBake.Part(go, "Fin" + i, mid + new Vector3(0, .075f, 0), new Vector3(.006f, .02f, len * .95f), Fin, .4f);
                    go.transform.Find("Fin" + i).localRotation = Quaternion.Euler(0f, yaw, 0f);
                }
            }
            PA_DemoIconBake.Part(go, "Snout", new Vector3(0, .035f, .45f), new Vector3(.035f, .03f, .05f), EelBack, .7f);
            PA_DemoIconBake.Part(go, "EyeL", new Vector3(-.022f, .05f, .4f), new Vector3(.008f, .012f, .012f), Eye);
            PA_DemoIconBake.Part(go, "EyeR", new Vector3(.022f, .05f, .4f), new Vector3(.008f, .012f, .012f), Eye);
            PA_DemoIconBake.Part(go, "PectoralL", new Vector3(-.04f, .03f, .33f), new Vector3(.02f, .006f, .03f), Fin, .6f);
            PA_DemoIconBake.Part(go, "PectoralR", new Vector3(.04f, .03f, .33f), new Vector3(.02f, .006f, .03f), Fin, .6f);
        });
        var crucianModel = Prefab("PA_Fish_Crucian", go =>
        {
            // 몸높이가 있는 작고 통통한 몸(옆으로 납작), 누런 청동빛, 등은 짙은 올리브, 옆구리에 비늘 줄 몇 개, 큰 등지느러미.
            var body = new GameObject("PA_Fish_Crucian_Pivot");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0, .02f, -.3f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(.55f, 1f, 1.25f);
            PA_DigSetup.Round(body, "Body", Vector3.zero,
                new[] { PA_DigSetup.V(.015f, 0), PA_DigSetup.V(.05f, .08f), PA_DigSetup.V(.095f, .22f), PA_DigSetup.V(.1f, .36f), PA_DigSetup.V(.08f, .5f), PA_DigSetup.V(.045f, .58f), PA_DigSetup.V(0, .62f) },
                CrucianBody, 10);
            PA_DemoIconBake.Part(go, "BackBand", new Vector3(0, .12f, .02f), new Vector3(.04f, .03f, .4f), CrucianBack, .7f);
            for (int i = 0; i < 3; i++)
                for (int k = -1; k <= 1; k += 2)
                    PA_DemoIconBake.Part(go, $"Scale{i}{(k < 0 ? "L" : "R")}", new Vector3(k * .053f, .06f - i * .035f, .0f), new Vector3(.006f, .008f, .34f), CrucianScale);
            PA_DemoIconBake.Part(go, "Dorsal", new Vector3(0, .16f, -.02f), new Vector3(.01f, .06f, .2f), CrucianBack, .5f);
            PA_DemoIconBake.Part(go, "Tail", new Vector3(0, .03f, -.34f), new Vector3(.02f, .14f, .08f), CrucianBack, .5f);
            PA_DemoIconBake.Part(go, "EyeL", new Vector3(-.045f, .06f, .22f), new Vector3(.012f, .024f, .024f), Eye);
            PA_DemoIconBake.Part(go, "EyeR", new Vector3(.045f, .06f, .22f), new Vector3(.012f, .024f, .024f), Eye);
        });
        var crucian = BuildItem(Crucian, "Item_Fish_Crucian", "붕어", 16, 99, crucianModel, // 12G였을 때 쉬운 종이 구이 증분 +40으로 가장 이득 → 16G(+36, 연구 §9.1 반복 식재 16~20G)
            "고지대 바위 사이 작은 연못에 사는 몸높이가 있는 민물고기. 잔잔하고 잘 물어요. 조리대에서 생선구이로 구울 수 있어요.", log);
        var trout = BuildItem(Trout, "Item_Fish_Trout", "송어", 25, 99, troutModel,
            "섬을 흐르는 강에서 낮에 물길을 따라 오르내리는 물고기. 조리대에서 나무와 함께 훈제 생선으로 만들 수 있어요.", log);
        var eel = BuildItem(Eel, "Item_Fish_Eel", "장어", 35, 99, eelModel,
            "강이 바다와 만나는 하구에서 오후부터 밤까지 길게 굽이치며 다니는 물고기. 조리대에서 생선구이로 구울 수 있어요.", log);
        var grilled = AssetDatabase.LoadAssetAtPath<Item>(Items + "/Item_10_GrilledFish.asset");
        var smoked = AssetDatabase.LoadAssetAtPath<Item>(Items + "/Item_SmokedFish.asset");
        var wood = AssetDatabase.LoadAssetAtPath<Item>(Items + "/Item_Wood.asset");
        if (grilled == null || grilled.id != GrilledFish || smoked == null || smoked.id != SmokedFish || wood == null)
            throw new InvalidOperationException("생선구이(10)·훈제 생선(2033)·나무 아이템이 없어 대체 레시피를 만들 수 없습니다.");
        Recipe("Recipe_SmokedTrout", "훈제 생선 · 송어", WorkbenchType.Kitchen, smoked, (trout, 1), (wood, 2));
        Recipe("Recipe_GrilledEel", "생선구이 · 장어", WorkbenchType.Kitchen, grilled, (eel, 1));
        Recipe("Recipe_GrilledCrucian", "생선구이 · 붕어", WorkbenchType.Kitchen, grilled, (crucian, 1));
        log.Add("recipes Recipe_SmokedTrout (→2033), Recipe_GrilledEel (→10)");
        AssetDatabase.SaveAssets();
        Debug.Log("[FRESH-FISH] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[FRESH-FISH] " + e); code = 1; }
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
