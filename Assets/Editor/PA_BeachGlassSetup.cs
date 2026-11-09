using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 해변 유리 조각·유리 모빌(Docs/01_GAME_DESIGN/Content/BEACH_GLASS.md, 아이템 생태 연구 H05·R06 owner 채택 2026-11-03).
// 새 아이템: 해변 유리 조각(2047, 해변 발견물 18G) · 유리 모빌(2048, 현장 작업대: 유리 2 + 판재 1 + 나무 1, 배치 장식).
// 모빌은 꾸밈 전용(연구 권고: 손님 증가 효과 자동 부여 없음). 받침대 포함 외형이라 천장 부착 체계가 필요 없다.
// 배치 항목 키 "decor-glass"는 PA_DemoStructureSetup가 catalog를 다시 쓸 때 ExistingEntries()로 보존된다.
// 배치: Unity -batchmode -executeMethod PA_BeachGlassSetup.BatchApply
public static class PA_BeachGlassSetup
{
    const string Items = "Assets/Resources/Items";
    const string Recipes = "Assets/Resources/DemoStructure";
    const string Structure = "Assets/Resources/DemoStructure";
    const string Art = "Assets/Art/ProjectPA/Derived/Beach";
    public const string Key = "decor-glass";

    public const int Glass = 2047, Mobile = 2048;

    static readonly Color Aqua = new Color(.6f, .86f, .82f), Mint = new Color(.72f, .9f, .66f), Frost = new Color(.88f, .93f, .92f), Amber = new Color(.86f, .7f, .42f);
    static readonly Color Wood = new Color(.58f, .38f, .2f), DarkWood = new Color(.38f, .24f, .12f), Cord = new Color(.82f, .76f, .62f);

    [MenuItem("Project PA/Content/Beach Glass (Find · Mobile Decor)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        Preflight();
        Folder(Art);
        var shard = Prefab(Art, "PA_Beach_Glass", go =>
        {
            // 파도에 닳은 각진 조각 셋(청록·연두·서리빛). 윗면을 좁혀(.55) 모서리가 둥글게 닳은 느낌.
            Chip(go, "GlassA", new Vector3(-.07f, 0, .02f), new Vector3(.2f, .07f, .15f), Aqua, 18f);
            Chip(go, "GlassB", new Vector3(.1f, 0, -.04f), new Vector3(.15f, .06f, .12f), Mint, -35f);
            Chip(go, "GlassC", new Vector3(.02f, 0, .13f), new Vector3(.11f, .05f, .09f), Frost, 60f);
        });
        var glass = BuildItem(Glass, "Item_BeachGlass", "해변 유리 조각", 18, 99, ItemCategory.Raw, shard,
            "파도에 오래 닳아 모서리가 둥근 유리 조각. 햇빛에 비추면 바다색이 나요. 모아 두었다가 작업대에서 모빌로 엮을 수 있어요.", log);
        var mobileModel = Prefab(Art, "PA_Decor_GlassMobile", go =>
        {
            // 받침대가 있는 세움 모빌: 기둥 + 가로대 + 줄 세 가닥에 유리 조각(높이가 서로 다름).
            PA_DemoIconBake.Part(go, "MobileBase", new Vector3(0, .03f, 0), new Vector3(.36f, .06f, .36f), DarkWood, .85f);
            PA_DemoIconBake.Part(go, "MobilePost", new Vector3(0, .55f, 0), new Vector3(.05f, 1f, .05f), Wood);
            PA_DemoIconBake.Part(go, "MobileArm", new Vector3(0, 1.05f, 0), new Vector3(.7f, .04f, .04f), Wood);
            // 2026-11-03 아이콘 검토: 가운데 줄이 기둥에 가려 셋 다 기둥 밖으로(왼쪽 하나 · 오른쪽 둘), 조각도 조금 크게.
            var drops = new[] { (-.3f, .32f, Aqua), (.13f, .5f, Mint), (.31f, .24f, Frost) };
            for (int i = 0; i < drops.Length; i++)
            {
                var (x, len, color) = drops[i];
                PA_DemoIconBake.Part(go, "MobileString" + i, new Vector3(x, 1.05f - len * .5f, 0), new Vector3(.012f, len, .012f), Cord);
                PA_DemoIconBake.Part(go, "MobileGem" + i, new Vector3(x, 1.05f - len - .07f, 0), new Vector3(.13f, .15f, .035f), color, .6f);
            }
            PA_DemoIconBake.Part(go, "MobileCap", new Vector3(0, 1.09f, 0), new Vector3(.08f, .05f, .08f), Amber);
        });
        var mobile = BuildItem(Mobile, "Item_GlassMobile", "유리 모빌", 0, 3, ItemCategory.Tool, mobileModel,
            "해변 유리 조각을 판재 가로대에 매단 세움 모빌. 집이나 가게에 놓으면 바람에 흔들리는 바다색이 보여요(꾸밈 전용).", log);
        Recipe("Recipe_GlassMobile", "유리 모빌", WorkbenchType.BasicWorkbench, mobile, (glass, 2), (Require("Items/Item_Plank", 7), 1), (Require("Items/Item_Wood", 3), 1));
        log.Add("recipe Recipe_GlassMobile (workbench)");
        BuildPlaceable(mobileModel, log);
        var catalog = AssetDatabase.LoadAssetAtPath<DemoPlaceableCatalog>(Structure + "/Catalog.asset");
        if (catalog == null) throw new InvalidOperationException("Missing DemoStructure/Catalog.asset");
        var entries = (catalog.entries ?? Array.Empty<DemoPlaceableEntry>()).Where(e => e != null && e.key != Key).ToList();
        entries.Add(Entry(mobile));
        catalog.entries = entries.ToArray();
        EditorUtility.SetDirty(catalog);
        log.Add("catalog entry " + Key);
        AssetDatabase.SaveAssets();
        Debug.Log("[BEACH-GLASS] " + string.Join(" | ", log));
    }

    static void Chip(GameObject root, string name, Vector3 at, Vector3 size, Color color, float yaw)
    {
        PA_DemoIconBake.Part(root, name, at + Vector3.up * size.y * .5f, size, color, .55f);
        root.transform.Find(name).localRotation = Quaternion.Euler(0, yaw, 0);
    }

    static DemoPlaceableEntry Entry(Item item) => new DemoPlaceableEntry
    {
        item = item, key = Key, kind = DemoPlaceableKind.Furniture, buildingResource = "DemoStructure/GlassMobile",
        size = Vector2Int.one, surfaces = WorldPlaceableSurface.Living | WorldPlaceableSurface.Utility | WorldPlaceableSurface.ShopFloor, canMove = true,
    };

    public static IEnumerable<DemoPlaceableEntry> ExistingEntries()
    {
        var item = AssetDatabase.LoadAssetAtPath<Item>(Items + "/Item_GlassMobile.asset");
        if (item != null && AssetDatabase.LoadAssetAtPath<GameObject>(Structure + "/GlassMobile.prefab") != null) yield return Entry(item);
    }

    static void BuildPlaceable(GameObject model, List<string> log)
    {
        var root = new GameObject("GlassMobile");
        try
        {
            FirstDayStudioAssets.Place(model, root.transform, Vector3.zero, 1.15f);
            var box = root.AddComponent<BoxCollider>(); box.center = new Vector3(0, .55f, 0); box.size = new Vector3(.8f, 1.1f, .5f);
            root.SetActive(true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Structure + "/GlassMobile.prefab");
            string dataPath = Structure + "/GlassMobile.asset";
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(dataPath);
            if (data == null) { data = ScriptableObject.CreateInstance<BuildingData>(); AssetDatabase.CreateAsset(data, dataPath); }
            data.prefab = prefab; data.buildingName = "GlassMobile"; data.price = 0;
            EditorUtility.SetDirty(data);
            log.Add("placeable DemoStructure/GlassMobile");
        }
        finally { Object.DestroyImmediate(root); }
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[BEACH-GLASS] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static void Preflight()
    {
        var all = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" })
            .Select(g => (path: AssetDatabase.GUIDToAssetPath(g), item: AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(g))))
            .Where(x => x.item != null).ToList();
        foreach (var (id, asset) in new[] { (Glass, "Item_BeachGlass"), (Mobile, "Item_GlassMobile") })
        {
            string own = Items + "/" + asset + ".asset";
            var clash = all.FirstOrDefault(x => x.path != own && x.item.id == id);
            if (clash.item != null) throw new InvalidOperationException($"id {id} already used by {clash.path}");
        }
    }

    static Item Require(string resource, int id)
    {
        var item = Resources.Load<Item>(resource);
        if (item == null || item.id != id) throw new InvalidOperationException($"Expected existing item id {id} at Resources/{resource}.");
        return item;
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
