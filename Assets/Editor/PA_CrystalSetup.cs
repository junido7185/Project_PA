using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 수정 원석·작은 광물 받침(Docs/01_GAME_DESIGN/Content/CRYSTAL_STAND.md, 아이템 생태 연구 D05·R08 owner 채택 2026-11-04).
// 새 아이템: 수정 원석(2052, 파기 자국 발견물 32G) · 작은 광물 받침(2053, 현장 작업대: 수정 1 + 판재 1, 배치 장식).
// 연구 경계: 수정은 도구 단계의 새 병목이 아니다(어떤 도구 레시피에도 쓰지 않음). 받침은 꾸밈 전용(손님 효과 없음).
// 배치 항목 키 "decor-crystal"는 PA_DemoStructureSetup가 catalog를 다시 쓸 때 ExistingEntries()로 보존된다.
// 배치: Unity -batchmode -executeMethod PA_CrystalSetup.BatchApply
public static class PA_CrystalSetup
{
    const string Items = "Assets/Resources/Items";
    const string Recipes = "Assets/Resources/DemoStructure";
    const string Structure = "Assets/Resources/DemoStructure";
    const string Art = "Assets/Art/ProjectPA/Derived/Dig";
    public const string Key = "decor-crystal";

    public const int Crystal = 2052, Stand = 2053;

    static readonly Color Violet = new Color(.72f, .6f, .92f), Lilac = new Color(.84f, .78f, .98f), Clear = new Color(.9f, .94f, .98f), Rock = new Color(.5f, .48f, .46f);
    static readonly Color Wood = new Color(.58f, .38f, .2f), DarkWood = new Color(.38f, .24f, .12f), Felt = new Color(.24f, .36f, .4f);

    [MenuItem("Project PA/Content/Crystal (Find · Mineral Stand Decor)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        Preflight();
        Folder(Art);
        var crystalModel = Prefab(Art, "PA_Dig_Crystal", go => Cluster(go, "Gem", Vector3.zero, 1f));
        var crystal = BuildItem(Crystal, "Item_Crystal", "수정 원석", 32, 99, ItemCategory.Raw, crystalModel,
            "바위 가까운 흙에서 나온 보랏빛 수정 덩어리. 팔아도 좋고, 작업대에서 받침에 세워 집이나 가게에 둘 수도 있어요.", log);
        var standModel = Prefab(Art, "PA_Decor_CrystalStand", go =>
        {
            // 판재 받침(펠트 깔개) 위에 수정 덩어리 하나.
            PA_DemoIconBake.Part(go, "StandBase", new Vector3(0, .05f, 0), new Vector3(.46f, .1f, .34f), DarkWood, .9f);
            PA_DemoIconBake.Part(go, "StandTop", new Vector3(0, .115f, 0), new Vector3(.4f, .03f, .28f), Wood);
            PA_DemoIconBake.Part(go, "StandFelt", new Vector3(0, .135f, 0), new Vector3(.3f, .012f, .2f), Felt);
            Cluster(go, "StandGem", new Vector3(0, .14f, 0), .9f);
        });
        var stand = BuildItem(Stand, "Item_CrystalStand", "작은 광물 받침", 0, 3, ItemCategory.Tool, standModel,
            "수정 원석을 판재 받침에 세운 장식. 집이나 가게에 두면 빛을 받아 반짝여요(꾸밈 전용).", log);
        var plank = Resources.Load<Item>("Items/Item_Plank");
        if (plank == null || plank.id != 7) throw new InvalidOperationException("Expected Items/Item_Plank id 7");
        Recipe("Recipe_CrystalStand", "작은 광물 받침", WorkbenchType.BasicWorkbench, stand, (crystal, 1), (plank, 1));
        log.Add("recipe Recipe_CrystalStand (workbench)");
        BuildPlaceable(standModel, log);
        var catalog = AssetDatabase.LoadAssetAtPath<DemoPlaceableCatalog>(Structure + "/Catalog.asset");
        if (catalog == null) throw new InvalidOperationException("Missing DemoStructure/Catalog.asset");
        var entries = (catalog.entries ?? Array.Empty<DemoPlaceableEntry>()).Where(e => e != null && e.key != Key).ToList();
        entries.Add(Entry(stand));
        catalog.entries = entries.ToArray();
        EditorUtility.SetDirty(catalog);
        log.Add("catalog entry " + Key);
        AssetDatabase.SaveAssets();
        Debug.Log("[CRYSTAL] " + string.Join(" | ", log));
    }

    // 바위 밑동 + 끝이 뾰족한 결정 다섯 개(가운데 큰 것, 주변으로 기울어진 것).
    static void Cluster(GameObject root, string prefix, Vector3 at, float s)
    {
        PA_DemoIconBake.Part(root, prefix + "Rock", at + new Vector3(0, .03f, 0) * s, new Vector3(.26f, .06f, .2f) * s, Rock, .7f);
        var spikes = new[]
        {
            (new Vector3(0, .17f, 0), new Vector3(.09f, .26f, .09f), 0f, 0f, Violet),
            (new Vector3(-.08f, .12f, .03f), new Vector3(.07f, .18f, .07f), 0f, 24f, Lilac),
            (new Vector3(.08f, .12f, -.02f), new Vector3(.07f, .2f, .07f), 0f, -22f, Violet),
            (new Vector3(.02f, .1f, .08f), new Vector3(.06f, .14f, .06f), 26f, 0f, Clear),
            (new Vector3(-.03f, .1f, -.08f), new Vector3(.055f, .13f, .055f), -24f, 0f, Lilac),
        };
        for (int i = 0; i < spikes.Length; i++)
        {
            var (p, size, rx, rz, color) = spikes[i];
            string n = prefix + "Spike" + i;
            PA_DemoIconBake.Part(root, n, at + p * s, size * s, color, .15f);
            root.transform.Find(n).localRotation = Quaternion.Euler(rx, 0, rz);
        }
    }

    static DemoPlaceableEntry Entry(Item item) => new DemoPlaceableEntry
    {
        item = item, key = Key, kind = DemoPlaceableKind.Furniture, buildingResource = "DemoStructure/CrystalStand",
        size = Vector2Int.one, surfaces = WorldPlaceableSurface.Living | WorldPlaceableSurface.Utility | WorldPlaceableSurface.ShopFloor, canMove = true,
    };

    public static IEnumerable<DemoPlaceableEntry> ExistingEntries()
    {
        var item = AssetDatabase.LoadAssetAtPath<Item>(Items + "/Item_CrystalStand.asset");
        if (item != null && AssetDatabase.LoadAssetAtPath<GameObject>(Structure + "/CrystalStand.prefab") != null) yield return Entry(item);
    }

    static void BuildPlaceable(GameObject model, List<string> log)
    {
        var root = new GameObject("CrystalStand");
        try
        {
            FirstDayStudioAssets.Place(model, root.transform, Vector3.zero, .62f); // 첫 .5는 기본 카메라에서 작았다(dig-r102)
            var box = root.AddComponent<BoxCollider>(); box.center = new Vector3(0, .25f, 0); box.size = new Vector3(.6f, .5f, .45f);
            root.SetActive(true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Structure + "/CrystalStand.prefab");
            string dataPath = Structure + "/CrystalStand.asset";
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(dataPath);
            if (data == null) { data = ScriptableObject.CreateInstance<BuildingData>(); AssetDatabase.CreateAsset(data, dataPath); }
            data.prefab = prefab; data.buildingName = "CrystalStand"; data.price = 0;
            EditorUtility.SetDirty(data);
            log.Add("placeable DemoStructure/CrystalStand");
        }
        finally { Object.DestroyImmediate(root); }
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[CRYSTAL] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static void Preflight()
    {
        var all = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" })
            .Select(g => (path: AssetDatabase.GUIDToAssetPath(g), item: AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(g))))
            .Where(x => x.item != null).ToList();
        foreach (var (id, asset) in new[] { (Crystal, "Item_Crystal"), (Stand, "Item_CrystalStand") })
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

    static GameObject Prefab(string folder, string name, Action<GameObject> build)
    {
        var root = new GameObject(name);
        try { build(root); return PrefabUtility.SaveAsPrefabAsset(root, folder + "/" + name + ".prefab"); }
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
