using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 고등어(Docs/01_GAME_DESIGN/Content/MACKEREL.md, 아이템 생태 연구 F05 owner 채택 2026-11-04).
// 새 아이템: 고등어(3004, 원재료 14G). 새 산출물은 만들지 않는다 — 기존 훈제 생선(2033)의 대체 재료(고등어1+나무2, 조리대).
// 월드 그림자(길쭉한 몸·짧게 달리고 쉬기)·출현 비율은 FishingSpot/ShoreFishSchool이 맡는다(종 번호 4).
// 멱등: 아이템·레시피·모델·아이콘을 매번 정의대로 다시 쓴다.
// 배치: Unity -batchmode -executeMethod PA_MackerelSetup.BatchApply
public static class PA_MackerelSetup
{
    const string Items = "Assets/Resources/Items";
    const string Recipes = "Assets/Resources/DemoStructure";
    const string Art = "Assets/Art/ProjectPA/Derived/Fish";

    public const int Mackerel = 3004, SmokedFish = 2033;

    static readonly Color Belly = new Color(.84f, .87f, .86f), Back = new Color(.18f, .46f, .5f), Bar = new Color(.08f, .2f, .24f);
    static readonly Color Fin = new Color(.3f, .44f, .48f), Eye = new Color(.08f, .09f, .1f), Gold = new Color(.86f, .78f, .46f);

    [MenuItem("Project PA/Content/Mackerel (Fish · Smoked alt)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        Preflight();
        Folder(Art);
        var fish = Prefab("PA_Fish_Mackerel", go =>
        {
            // 몸: 정어리보다 길고(0.9) 가는 돌림판, +Z가 머리. 등은 청록, 배는 은백, 등 위에 짙은 물결 줄무늬.
            var body = new GameObject("PA_Fish_Mackerel_Pivot");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0, 0, -.43f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(.62f, 1f, 1f);
            PA_DigSetup.Round(body, "Body", Vector3.zero,
                new[] { PA_DigSetup.V(.012f, 0), PA_DigSetup.V(.03f, .1f), PA_DigSetup.V(.064f, .34f), PA_DigSetup.V(.072f, .52f), PA_DigSetup.V(.06f, .7f), PA_DigSetup.V(.032f, .82f), PA_DigSetup.V(0, .88f) },
                Belly, 10);
            PA_DemoIconBake.Part(go, "BackBand", new Vector3(0, .05f, .04f), new Vector3(.04f, .03f, .66f), Back, .7f);
            // 고등어 등의 물결 무늬: 짧은 사선 막대를 엇갈려 얹는다.
            for (int i = 0; i < 6; i++)
            {
                float z = -.24f + i * .09f;
                PA_DemoIconBake.Part(go, "Wave" + i, new Vector3(0, .063f, z), new Vector3(.046f, .012f, .028f), Bar, .4f);
                go.transform.Find("Wave" + i).localRotation = Quaternion.Euler(0f, i % 2 == 0 ? 32f : -32f, 0f);
            }
            PA_DemoIconBake.Part(go, "SideLine", new Vector3(0, .012f, .02f), new Vector3(.05f, .008f, .56f), Gold, .4f);
            PA_DemoIconBake.Part(go, "TailUp", new Vector3(0, .05f, -.49f), new Vector3(.012f, .13f, .05f), Fin, .6f);
            PA_DemoIconBake.Part(go, "TailDown", new Vector3(0, -.05f, -.49f), new Vector3(.012f, .13f, .05f), Fin, .6f);
            go.transform.Find("TailUp").localRotation = Quaternion.Euler(-40f, 0f, 0f);
            go.transform.Find("TailDown").localRotation = Quaternion.Euler(40f, 0f, 0f);
            PA_DemoIconBake.Part(go, "Dorsal", new Vector3(0, .085f, .1f), new Vector3(.01f, .05f, .09f), Fin, .4f);
            // 꼬리 앞의 작은 토막 지느러미(고등어 실루엣의 특징).
            for (int i = 0; i < 3; i++)
                PA_DemoIconBake.Part(go, "Finlet" + i, new Vector3(0, .05f, -.3f - i * .045f), new Vector3(.008f, .022f, .02f), Fin, .3f);
            PA_DemoIconBake.Part(go, "EyeL", new Vector3(.03f, .016f, .33f), new Vector3(.01f, .026f, .026f), Eye);
            PA_DemoIconBake.Part(go, "EyeR", new Vector3(-.03f, .016f, .33f), new Vector3(.01f, .026f, .026f), Eye);
        });
        var mackerel = BuildItem(Mackerel, "Item_Fish_Mackerel", "고등어", 14, 99, ItemCategory.Raw, fish,
            "등에 물결 무늬가 있는 길쭉한 물고기. 짧게 휙 달렸다가 멈춰 쉬어요. 조리대에서 나무와 함께 훈제 생선으로 만들 수 있어요.", log);
        var smoked = AssetDatabase.LoadAssetAtPath<Item>(Items + "/Item_SmokedFish.asset");
        var wood = AssetDatabase.LoadAssetAtPath<Item>(Items + "/Item_Wood.asset");
        if (smoked == null || smoked.id != SmokedFish || wood == null) throw new InvalidOperationException("훈제 생선(2033)·나무 아이템이 없어 대체 레시피를 만들 수 없습니다(PA_CraftingStationsSetup 먼저).");
        Recipe("Recipe_SmokedMackerel", "훈제 생선 · 고등어", WorkbenchType.Kitchen, smoked, (mackerel, 1), (wood, 2));
        log.Add("recipe Recipe_SmokedMackerel (kitchen → 2033)");
        AssetDatabase.SaveAssets();
        Debug.Log("[MACKEREL-SETUP] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[MACKEREL-SETUP] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static void Preflight()
    {
        string own = Items + "/Item_Fish_Mackerel.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != own && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == Mackerel);
        if (clash != null) throw new InvalidOperationException($"id {Mackerel} already used by {clash}");
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
