using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 해초·해초 생선말이(Docs/01_GAME_DESIGN/Content/SEAWEED.md, 아이템 생태 연구 H04·R02 owner 채택 2026-11-03).
// 새 아이템: 해초(2045, 해변 발견물 6G) · 해초 생선말이(2046, 조리대: 참치 1 + 해초 1 → 62G). 낚시와 해변 두 동선을 한 요리로 잇는다.
// 월드 배치는 BeachFinds(Kinds 4번째), 도감은 DiscoveryLog 해변 섹션. 모델은 소유 로우폴리(8각 부품·돌림판).
// 멱등: 아이템·레시피·모델·아이콘을 매번 정의대로 다시 쓴다.
// 배치: Unity -batchmode -executeMethod PA_SeaweedSetup.BatchApply
public static class PA_SeaweedSetup
{
    const string Items = "Assets/Resources/Items";
    const string Recipes = "Assets/Resources/DemoStructure";
    const string Art = "Assets/Art/ProjectPA/Derived/Beach";

    public const int Seaweed = 2045, SeaweedRoll = 2046;

    static readonly Color Kelp = new Color(.3f, .48f, .2f), KelpLight = new Color(.46f, .63f, .28f), KelpDark = new Color(.2f, .34f, .15f);
    static readonly Color Fish = new Color(.93f, .58f, .52f), Board = new Color(.6f, .4f, .22f);

    [MenuItem("Project PA/Content/Seaweed (Beach Find · Fish Roll)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        Preflight();
        Folder(Art);
        var weed = Prefab("PA_Beach_Seaweed", go =>
        {
            // 모래 위에 늘어진 해초 세 가닥: 마디마다 방향을 조금씩 틀어 물결 모양(납작, 높이 약 .03).
            // 2026-11-03 아이콘 검토: 첫 세 가닥은 겹쳐 돌·막대처럼 보여, 한 밑동에서 부채꼴로 벌어지는 세 가닥으로(밝은 초록 섞음).
            Strand(go, "FrondA", new Vector3(0, 0, -.24f), -24f, Kelp, .07f);
            Strand(go, "FrondB", new Vector3(0, 0, -.24f), 2f, KelpLight, .075f);
            Strand(go, "FrondC", new Vector3(0, 0, -.24f), 28f, KelpDark, .06f);
            PA_DemoIconBake.Part(go, "FrondHold", new Vector3(0, .014f, -.25f), new Vector3(.06f, .028f, .05f), KelpDark, .8f);
        });
        var seaweed = BuildItem(Seaweed, "Item_Seaweed", "해초", 6, 99, ItemCategory.Raw, weed,
            "물가 모래에 밀려 올라온 긴 해초. 그대로는 싸지만, 조리대에서 참치를 말면 생선구이보다 비싼 요리가 돼요.", log);
        var rollModel = Prefab("PA_Food_SeaweedRoll", go =>
        {
            PA_DemoIconBake.Part(go, "RollBoard", new Vector3(0, .025f, 0), new Vector3(.62f, .05f, .3f), Board);
            // 말이 세 조각: 겉은 해초(짙은 초록), 자른 면은 참치 살(분홍).
            for (int i = 0; i < 3; i++)
            {
                Vector3 at = new Vector3((i - 1) * .18f, .05f, 0);
                PA_DigSetup.Round(go, "RollSlice" + i, at, new[] { PA_DigSetup.V(.075f, 0), PA_DigSetup.V(.08f, .02f), PA_DigSetup.V(.08f, .1f), PA_DigSetup.V(.075f, .11f), PA_DigSetup.V(0, .11f) }, KelpDark, 12);
                PA_DigSetup.Round(go, "RollCore" + i, at + Vector3.up * .108f, new[] { PA_DigSetup.V(.05f, 0), PA_DigSetup.V(.05f, .006f), PA_DigSetup.V(0, .008f) }, Fish, 12);
            }
        });
        var roll = BuildItem(SeaweedRoll, "Item_SeaweedRoll", "해초 생선말이", 62, 12, ItemCategory.Processed, rollModel,
            "참치를 해초로 말아 구워 썰었어요. 낚시와 해변 줍기를 함께 한 날 만들 수 있고, 생선구이보다 조금 더 받아요.", log);
        var tuna = Resources.Load<Item>("Items/Item_Fish");
        if (tuna == null || tuna.id != 8) throw new InvalidOperationException("Expected Items/Item_Fish id 8");
        Recipe("Recipe_SeaweedRoll", "해초 생선말이", WorkbenchType.Kitchen, roll, (tuna, 1), (seaweed, 1));
        log.Add("recipe Recipe_SeaweedRoll (kitchen)");
        AssetDatabase.SaveAssets();
        Debug.Log("[SEAWEED-SETUP] " + string.Join(" | ", log));
    }

    // 한 가닥 = 마디 5개(끝으로 갈수록 가늘게), 마디마다 ±18° 꺾음.
    static void Strand(GameObject root, string name, Vector3 start, float yaw, Color color, float width)
    {
        Vector3 at = start; float heading = yaw;
        for (int i = 0; i < 5; i++)
        {
            float len = .11f, w = width * Mathf.Lerp(1f, .55f, i / 4f);
            Vector3 dir = Quaternion.Euler(0, heading, 0) * Vector3.forward;
            string part = name + i;
            PA_DemoIconBake.Part(root, part, at + dir * len * .5f + Vector3.up * .012f, new Vector3(w, .022f, len * 1.08f), color, .8f);
            root.transform.Find(part).localRotation = Quaternion.Euler(0, heading, 0);
            at += dir * len;
            heading += (i % 2 == 0 ? 16f : -16f);
        }
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[SEAWEED-SETUP] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static void Preflight()
    {
        var all = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" })
            .Select(g => (path: AssetDatabase.GUIDToAssetPath(g), item: AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(g))))
            .Where(x => x.item != null).ToList();
        foreach (var (id, asset) in new[] { (Seaweed, "Item_Seaweed"), (SeaweedRoll, "Item_SeaweedRoll") })
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
