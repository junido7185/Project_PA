using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 토기 조각·토기 모자이크(Docs/01_GAME_DESIGN/Content/POTTERY_MOSAIC.md, 아이템 생태 연구 D04·R07 owner 채택 2026-11-03).
// 새 아이템: 토기 조각(2049, 파기 자국 발견물 14G) · 토기 모자이크(2050, 화로: 토기 조각 2 + 점토 1, 바닥형 배치 장식).
// 모자이크는 꾸밈 전용(손님 효과 없음). 새 지면 페인팅 체계 없이 1칸 배치물 하나로 놓는다.
// 배치 항목 키 "decor-mosaic"는 PA_DemoStructureSetup가 catalog를 다시 쓸 때 ExistingEntries()로 보존된다.
// 배치: Unity -batchmode -executeMethod PA_PotterySetup.BatchApply
public static class PA_PotterySetup
{
    const string Items = "Assets/Resources/Items";
    const string Recipes = "Assets/Resources/DemoStructure";
    const string Structure = "Assets/Resources/DemoStructure";
    const string Art = "Assets/Art/ProjectPA/Derived/Dig";
    public const string Key = "decor-mosaic";

    public const int Shard = 2049, Mosaic = 2050;

    static readonly Color Terracotta = new Color(.78f, .45f, .3f), TerraDark = new Color(.6f, .33f, .21f), Band = new Color(.3f, .2f, .14f);
    static readonly Color Cream = new Color(.92f, .85f, .7f), Teal = new Color(.32f, .6f, .58f), Ochre = new Color(.86f, .66f, .3f), Rim = new Color(.5f, .36f, .24f);

    [MenuItem("Project PA/Content/Pottery (Shard · Mosaic Decor)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        Preflight();
        Folder(Art);
        var shardModel = Prefab(Art, "PA_Dig_PotteryShard", go =>
        {
            // 깨진 토기 두 조각: 바깥 면에 짙은 띠 문양(옛 항아리였던 흔적).
            Piece(go, "ShardA", new Vector3(-.06f, 0, 0), new Vector3(.22f, .05f, .16f), Terracotta, 20f);
            PA_DemoIconBake.Part(go, "ShardABand", new Vector3(-.06f, .052f, 0), new Vector3(.2f, .012f, .035f), Band);
            go.transform.Find("ShardABand").localRotation = Quaternion.Euler(0, 20f, 0);
            Piece(go, "ShardB", new Vector3(.11f, 0, .07f), new Vector3(.14f, .045f, .12f), TerraDark, -40f);
            PA_DemoIconBake.Part(go, "ShardBBand", new Vector3(.11f, .047f, .07f), new Vector3(.12f, .01f, .03f), Cream);
            go.transform.Find("ShardBBand").localRotation = Quaternion.Euler(0, -40f, 0);
        });
        var shard = BuildItem(Shard, "Item_PotteryShard", "토기 조각", 14, 99, ItemCategory.Raw, shardModel,
            "띠 무늬가 남은 깨진 토기 조각. 누군가 오래전 이 섬에서 쓰던 그릇이었나 봐요. 화로에서 점토와 붙이면 모자이크 장식이 돼요.", log);
        var mosaicModel = Prefab(Art, "PA_Decor_PotteryMosaic", go =>
        {
            // 둥근 바닥 판(점토 테두리) 위에 조각을 꽃 모양으로 박은 모자이크.
            PA_DigSetup.Round(go, "MosaicBase", Vector3.zero, new[] { PA_DigSetup.V(.42f, 0), PA_DigSetup.V(.44f, .05f), PA_DigSetup.V(.4f, .07f), PA_DigSetup.V(0, .07f) }, Rim, 16);
            PA_DigSetup.Round(go, "MosaicFace", new Vector3(0, .069f, 0), new[] { PA_DigSetup.V(.38f, 0), PA_DigSetup.V(.37f, .006f), PA_DigSetup.V(0, .006f) }, Cream, 16);
            // 2026-11-03 아이콘 검토: 조각 6개는 듬성듬성 흩어져 보여 안쪽 6 + 바깥 10의 두 고리로 촘촘하게.
            var colors = new[] { Terracotta, Teal, Ochre, TerraDark };
            void Ring(string prefix, int count, float radius, Vector3 size, int colorShift)
            {
                for (int i = 0; i < count; i++)
                {
                    float deg = i * 360f / count, a = deg * Mathf.Deg2Rad;
                    string n = prefix + i;
                    PA_DemoIconBake.Part(go, n, new Vector3(Mathf.Cos(a) * radius, .082f, Mathf.Sin(a) * radius), size, colors[(i + colorShift) % colors.Length], .8f);
                    go.transform.Find(n).localRotation = Quaternion.Euler(0, -deg, 0);
                }
            }
            Ring("MosaicInner", 6, .14f, new Vector3(.1f, .02f, .1f), 0);
            Ring("MosaicOuter", 10, .29f, new Vector3(.1f, .02f, .13f), 1);
            PA_DemoIconBake.Part(go, "MosaicCentre", new Vector3(0, .083f, 0), new Vector3(.12f, .022f, .12f), Band, .8f);
        });
        var mosaic = BuildItem(Mosaic, "Item_PotteryMosaic", "토기 모자이크", 0, 3, ItemCategory.Tool, mosaicModel,
            "파낸 토기 조각을 점토 판에 꽃 모양으로 박아 구운 바닥 장식. 집 앞이나 가게 바닥에 놓아요(꾸밈 전용).", log);
        var clay = Resources.Load<Item>("Items/Item_Clay");
        if (clay == null || clay.id != 2039) throw new InvalidOperationException("Expected Items/Item_Clay id 2039");
        Recipe("Recipe_PotteryMosaic", "토기 모자이크", WorkbenchType.Forge, mosaic, (shard, 2), (clay, 1));
        log.Add("recipe Recipe_PotteryMosaic (forge)");
        BuildPlaceable(mosaicModel, log);
        var catalog = AssetDatabase.LoadAssetAtPath<DemoPlaceableCatalog>(Structure + "/Catalog.asset");
        if (catalog == null) throw new InvalidOperationException("Missing DemoStructure/Catalog.asset");
        var entries = (catalog.entries ?? Array.Empty<DemoPlaceableEntry>()).Where(e => e != null && e.key != Key).ToList();
        entries.Add(Entry(mosaic));
        catalog.entries = entries.ToArray();
        EditorUtility.SetDirty(catalog);
        log.Add("catalog entry " + Key);
        AssetDatabase.SaveAssets();
        Debug.Log("[POTTERY] " + string.Join(" | ", log));
    }

    static void Piece(GameObject root, string name, Vector3 at, Vector3 size, Color color, float yaw)
    {
        PA_DemoIconBake.Part(root, name, at + Vector3.up * size.y * .5f, size, color, .7f);
        root.transform.Find(name).localRotation = Quaternion.Euler(0, yaw, 0);
    }

    static DemoPlaceableEntry Entry(Item item) => new DemoPlaceableEntry
    {
        item = item, key = Key, kind = DemoPlaceableKind.Furniture, buildingResource = "DemoStructure/PotteryMosaic",
        size = Vector2Int.one, surfaces = WorldPlaceableSurface.Living | WorldPlaceableSurface.Utility | WorldPlaceableSurface.ShopFloor, canMove = true,
    };

    public static IEnumerable<DemoPlaceableEntry> ExistingEntries()
    {
        var item = AssetDatabase.LoadAssetAtPath<Item>(Items + "/Item_PotteryMosaic.asset");
        if (item != null && AssetDatabase.LoadAssetAtPath<GameObject>(Structure + "/PotteryMosaic.prefab") != null) yield return Entry(item);
    }

    static void BuildPlaceable(GameObject model, List<string> log)
    {
        var root = new GameObject("PotteryMosaic");
        try
        {
            FirstDayStudioAssets.Place(model, root.transform, Vector3.zero, .1f);
            var box = root.AddComponent<BoxCollider>(); box.center = new Vector3(0, .05f, 0); box.size = new Vector3(.9f, .1f, .9f);
            root.SetActive(true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Structure + "/PotteryMosaic.prefab");
            string dataPath = Structure + "/PotteryMosaic.asset";
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(dataPath);
            if (data == null) { data = ScriptableObject.CreateInstance<BuildingData>(); AssetDatabase.CreateAsset(data, dataPath); }
            data.prefab = prefab; data.buildingName = "PotteryMosaic"; data.price = 0;
            EditorUtility.SetDirty(data);
            log.Add("placeable DemoStructure/PotteryMosaic");
        }
        finally { Object.DestroyImmediate(root); }
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[POTTERY] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static void Preflight()
    {
        var all = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" })
            .Select(g => (path: AssetDatabase.GUIDToAssetPath(g), item: AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(g))))
            .Where(x => x.item != null).ToList();
        foreach (var (id, asset) in new[] { (Shard, "Item_PotteryShard"), (Mosaic, "Item_PotteryMosaic") })
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
