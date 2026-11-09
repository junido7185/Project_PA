using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 정어리·정어리 한 접시(Docs/01_GAME_DESIGN/Content/SARDINE.md, 아이템 생태 연구 F04·R01 owner 채택 2026-11-03).
// 새 아이템: 정어리(3003, 원재료 9G) · 정어리 한 접시(2044, 조리대: 정어리 3 → 56G). 모델은 소유 로우폴리(돌림판 몸 + 8각 부품).
// 월드 그림자·무리 행동·출현 비율은 FishingSpot/ShoreFishSchool이 맡는다(종 번호 3).
// 멱등: 아이템·레시피·모델·아이콘을 매번 정의대로 다시 쓴다.
// 배치: Unity -batchmode -executeMethod PA_SardineSetup.BatchApply
public static class PA_SardineSetup
{
    const string Items = "Assets/Resources/Items";
    const string Recipes = "Assets/Resources/DemoStructure";
    const string Art = "Assets/Art/ProjectPA/Derived/Fish";

    public const int Sardine = 3003, SardinePlate = 2044;

    static readonly Color Silver = new Color(.74f, .8f, .84f), Back = new Color(.22f, .38f, .55f), Fin = new Color(.48f, .58f, .66f);
    static readonly Color Eye = new Color(.08f, .09f, .1f), Plate = new Color(.93f, .91f, .85f), Rim = new Color(.36f, .55f, .66f);

    [MenuItem("Project PA/Content/Sardine (Fish · Plate)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        Preflight();
        Folder(Art);
        var fish = Prefab("PA_Fish_Sardine", go =>
        {
            // 몸: Y축 돌림판을 눕혀 +Z가 머리, 옆으로 납작하게(폭 0.6). 등은 짙은 청색 줄, 배는 은색.
            var body = new GameObject("PA_Fish_Sardine_Pivot");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0, 0, -.31f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            // 2026-11-03 icon 검토: 첫 몸(최대 반지름 .09)은 작은 참치처럼 통통해 보여 가늘고 길게(.068), 꼬리는 두 갈래로.
            body.transform.localScale = new Vector3(.55f, 1f, 1f);
            PA_DigSetup.Round(body, "Body", Vector3.zero,
                new[] { PA_DigSetup.V(.014f, 0), PA_DigSetup.V(.034f, .08f), PA_DigSetup.V(.062f, .25f), PA_DigSetup.V(.068f, .4f), PA_DigSetup.V(.054f, .53f), PA_DigSetup.V(.027f, .61f), PA_DigSetup.V(0, .65f) },
                Silver, 10);
            PA_DemoIconBake.Part(go, "BackSlim", new Vector3(0, .052f, .03f), new Vector3(.034f, .026f, .44f), Back, .7f);
            PA_DemoIconBake.Part(go, "TailUp", new Vector3(0, .045f, -.37f), new Vector3(.012f, .11f, .045f), Fin, .6f);
            PA_DemoIconBake.Part(go, "TailDown", new Vector3(0, -.045f, -.37f), new Vector3(.012f, .11f, .045f), Fin, .6f);
            go.transform.Find("TailUp").localRotation = Quaternion.Euler(-38f, 0f, 0f);
            go.transform.Find("TailDown").localRotation = Quaternion.Euler(38f, 0f, 0f);
            PA_DemoIconBake.Part(go, "DorsalSlim", new Vector3(0, .078f, .05f), new Vector3(.01f, .04f, .07f), Fin, .4f);
            PA_DemoIconBake.Part(go, "EyeSlimL", new Vector3(.026f, .016f, .24f), new Vector3(.01f, .024f, .024f), Eye);
            PA_DemoIconBake.Part(go, "EyeSlimR", new Vector3(-.026f, .016f, .24f), new Vector3(.01f, .024f, .024f), Eye);
        });
        var sardine = BuildItem(Sardine, "Item_Fish_Sardine", "정어리", 9, 99, ItemCategory.Raw, fish,
            "작은 그림자가 서너 마리씩 몰려다니는 은빛 물고기. 잡기 쉽고 흔해요. 세 마리를 모으면 조리대에서 한 접시로 구워 팔 수 있어요.", log);
        var plateModel = Prefab("PA_Food_SardinePlate", go =>
        {
            PA_DigSetup.Round(go, "Plate", Vector3.zero, new[] { PA_DigSetup.V(.22f, 0), PA_DigSetup.V(.34f, .035f), PA_DigSetup.V(.36f, .05f), PA_DigSetup.V(.3f, .045f), PA_DigSetup.V(0, .04f) }, Plate, 16);
            PA_DigSetup.Round(go, "Rim", new Vector3(0, .042f, 0), new[] { PA_DigSetup.V(.355f, 0), PA_DigSetup.V(.36f, .01f), PA_DigSetup.V(.335f, .012f) }, Rim, 16);
            // 구운 정어리 세 마리를 엇갈려 나란히(같은 정어리 모델).
            for (int i = 0; i < 3; i++) PA_DemoIconBake.AddModel(go, fish, new Vector3((i - 1) * .12f, .1f, (i % 2 == 0 ? .03f : -.03f)), .44f, i % 2 == 0 ? 0f : 180f);
            PA_DemoIconBake.Part(go, "Lemon", new Vector3(.24f, .07f, .12f), new Vector3(.08f, .03f, .06f), new Color(.98f, .86f, .3f));
        });
        var plate = BuildItem(SardinePlate, "Item_SardinePlate", "정어리 한 접시", 56, 12, ItemCategory.Processed, plateModel,
            "정어리 세 마리를 한 접시에 구웠어요. 한 마리씩 파는 것보다 손님 한 명에게 훨씬 많이 받아요.", log);
        Recipe("Recipe_SardinePlate", "정어리 한 접시", WorkbenchType.Kitchen, plate, (sardine, 3));
        log.Add("recipe Recipe_SardinePlate (kitchen)");
        AssetDatabase.SaveAssets();
        Debug.Log("[SARDINE-SETUP] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[SARDINE-SETUP] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static void Preflight()
    {
        var all = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" })
            .Select(g => (path: AssetDatabase.GUIDToAssetPath(g), item: AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(g))))
            .Where(x => x.item != null).ToList();
        foreach (var (id, asset) in new[] { (Sardine, "Item_Fish_Sardine"), (SardinePlate, "Item_SardinePlate") })
        {
            string own = Items + "/" + asset + ".asset";
            var clash = all.FirstOrDefault(x => x.path != own && x.item.id == id);
            if (clash.item != null) throw new InvalidOperationException($"id {id} already used by {clash.path}");
        }
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
