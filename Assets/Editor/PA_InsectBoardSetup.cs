using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 곤충 관찰 안내판(아이템 생태 연구 R09 "현장 작업대·판재2+나무1, 새 장식, 곤충 비소모, 정적인 형태 그림", owner 채택 2026-11-09).
// 새 아이템: 곤충 관찰 안내판(2057, 배치 장식). 산책로 안내판처럼 작은 지붕 + 기둥 둘 + 크림 종이판에 이 섬 곤충 아홉 가지 그림(2026-11-09 3×3: 넓적사슴벌레·알락하늘소 추가)
// (네발나비·칠성무당벌레·고추잠자리·호박벌 / 방아깨비·장수풍뎅이·흰불나방). 곤충을 쓰지 않고, 발견에 따라 그림이 바뀌지 않는다(연구 경계).
// 배치 항목 키 "decor-insectboard"는 PA_DemoStructureSetup가 catalog를 다시 쓸 때 ExistingEntries()로 보존된다.
// 배치: Unity -batchmode -executeMethod PA_InsectBoardSetup.BatchApply
public static class PA_InsectBoardSetup
{
    const string Items = "Assets/Resources/Items";
    const string Structure = "Assets/Resources/DemoStructure";
    const string Art = "Assets/Art/ProjectPA/Derived/ItemPresentation/Models";
    public const string Key = "decor-insectboard";
    public const int Board = 2057;

    static readonly Color Wood = new Color(.58f, .38f, .2f), DarkWood = new Color(.38f, .24f, .12f), Paper = new Color(.96f, .92f, .8f), Leaf = new Color(.36f, .6f, .36f);
    static readonly Color Orange = new Color(.95f, .55f, .18f), Red = new Color(.88f, .18f, .13f), Ink = new Color(.12f, .1f, .1f), Pale = new Color(.82f, .9f, .97f);
    static readonly Color BeeTail = new Color(.95f, .48f, .16f), Hopper = new Color(.66f, .5f, .3f), Beetle = new Color(.32f, .19f, .1f), Moth = new Color(.93f, .88f, .72f), White = new Color(.97f, .97f, .97f), Stag = new Color(.16f, .12f, .09f);

    [MenuItem("Project PA/Content/Insect Observation Board (Decor)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        string path = Items + "/Item_InsectBoard.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != path && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == Board);
        if (clash != null) throw new InvalidOperationException($"id {Board} already used by {clash}");
        var model = Prefab("PA_Decor_InsectBoard", BuildModel);
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = Board; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
        item.itemName = "곤충 관찰 안내판"; item.basePrice = 0; item.maxStack = 3; item.category = ItemCategory.Tool; item.toolType = ToolType.None;
        item.description = "판재에 이 섬의 곤충 아홉 가지를 그려 넣은 안내판. 집이나 가게에 세워 두면 곤충 이야기를 나누기 좋아요(꾸밈 전용).";
        item.model = model; item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("baked icon " + Board);
        var plank = Resources.Load<Item>("Items/Item_Plank");
        var wood = Resources.Load<Item>("Items/Item_Wood");
        if (plank == null || plank.id != 7) throw new InvalidOperationException("Expected Items/Item_Plank id 7");
        if (wood == null) throw new InvalidOperationException("Missing Items/Item_Wood");
        Recipe("Recipe_InsectBoard", "곤충 관찰 안내판", WorkbenchType.BasicWorkbench, item, (plank, 2), (wood, 1));
        log.Add("recipe Recipe_InsectBoard (workbench)");
        BuildPlaceable(model, log);
        var catalog = AssetDatabase.LoadAssetAtPath<DemoPlaceableCatalog>(Structure + "/Catalog.asset") ?? throw new InvalidOperationException("Missing Catalog.asset");
        var entries = (catalog.entries ?? Array.Empty<DemoPlaceableEntry>()).Where(e => e != null && e.key != Key).ToList();
        entries.Add(Entry(item));
        catalog.entries = entries.ToArray();
        EditorUtility.SetDirty(catalog);
        log.Add("catalog entry " + Key);
        AssetDatabase.SaveAssets();
        Debug.Log("[INSECTBOARD] " + string.Join(" | ", log));
    }

    // 앞(-Z)이 그림 면. 기둥 둘 + 나무 판 + 크림 종이 + 초록 제목 띠 + 작은 지붕.
    static void BuildModel(GameObject go)
    {
        PA_DemoIconBake.Part(go, "BoardPostL", new Vector3(-.3f, .42f, 0), new Vector3(.05f, .84f, .05f), DarkWood);
        PA_DemoIconBake.Part(go, "BoardPostR", new Vector3(.3f, .42f, 0), new Vector3(.05f, .84f, .05f), DarkWood);
        PA_DemoIconBake.Part(go, "BoardBack", new Vector3(0, .62f, 0), new Vector3(.7f, .44f, .04f), Wood);
        PA_DemoIconBake.Part(go, "BoardPaper", new Vector3(0, .6f, -.022f), new Vector3(.62f, .34f, .012f), Paper);
        PA_DemoIconBake.Part(go, "BoardTitle", new Vector3(0, .79f, -.026f), new Vector3(.44f, .045f, .012f), Leaf);
        Tilted(go, "BoardRoofL", new Vector3(-.19f, .905f, 0), new Vector3(.44f, .035f, .16f), DarkWood, 16f);
        Tilted(go, "BoardRoofR", new Vector3(.19f, .905f, 0), new Vector3(.44f, .035f, .16f), DarkWood, -16f);
        // 2026-11-09 곤충 9종(넓적사슴벌레·알락하늘소 추가) → 3줄 × 3칸. 각 그림은 칸 가운데 기준 상대 위치(크기·이름은 그대로라 메시 재사용).
        var cells = new[] { new Vector2(-.2f, .7f), new Vector2(0f, .7f), new Vector2(.2f, .7f), new Vector2(-.2f, .6f), new Vector2(0f, .6f), new Vector2(.2f, .6f), new Vector2(-.2f, .5f), new Vector2(0f, .5f), new Vector2(.2f, .5f) };
        void P(int cell, string name, float x, float y, bool front, Vector3 size, Color color, float top = 1f, float roll = 0f)
        {
            var at = new Vector3(cells[cell].x + x, cells[cell].y + y, front ? -.038f : -.032f);
            PA_DemoIconBake.Part(go, name, at, size, color, top);
            if (roll != 0f) go.transform.Find(name).localRotation = Quaternion.Euler(0, 0, roll);
        }
        // 첫 줄: 네발나비 · 칠성무당벌레 · 고추잠자리
        P(0, "PicButterflyWingL", -.025f, 0f, false, new Vector3(.045f, .05f, .008f), Orange, 1f, 22f);
        P(0, "PicButterflyWingR", .025f, 0f, false, new Vector3(.045f, .05f, .008f), Orange, 1f, -22f);
        P(0, "PicButterflyBody", 0f, -.005f, true, new Vector3(.012f, .06f, .008f), Ink);
        P(1, "PicLadybugShell", 0f, 0f, false, new Vector3(.052f, .046f, .01f), Red, .7f);
        P(1, "PicLadybugHead", 0f, .028f, false, new Vector3(.026f, .014f, .008f), Ink);
        P(1, "PicLadybugDot", 0f, -.003f, true, new Vector3(.013f, .013f, .006f), Ink);
        P(2, "PicDragonflyBody", 0f, -.01f, true, new Vector3(.011f, .08f, .008f), Red);
        P(2, "PicDragonflyWingA", 0f, .02f, false, new Vector3(.095f, .014f, .006f), Pale);
        P(2, "PicDragonflyWingB", 0f, .005f, false, new Vector3(.085f, .012f, .006f), Pale);
        // 가운데 줄: 호박벌 · 방아깨비 · 장수풍뎅이
        P(3, "PicBeeBody", -.01f, 0f, false, new Vector3(.05f, .036f, .01f), Ink, .8f);
        P(3, "PicBeeTail", .023f, 0f, false, new Vector3(.018f, .03f, .01f), BeeTail);
        P(3, "PicBeeWing", -.012f, .026f, true, new Vector3(.032f, .018f, .006f), White);
        P(4, "PicHopperBody", 0f, 0f, false, new Vector3(.085f, .016f, .008f), Hopper);
        P(4, "PicHopperLeg", -.015f, .013f, true, new Vector3(.05f, .01f, .006f), DarkWood, 1f, 24f);
        P(5, "PicBeetleBody", 0f, -.005f, false, new Vector3(.046f, .056f, .01f), Beetle, .75f);
        P(5, "PicBeetleHorn", 0f, .035f, true, new Vector3(.01f, .03f, .008f), Beetle);
        // 아랫줄: 흰불나방 · 넓적사슴벌레 · 알락하늘소
        P(6, "PicMothWings", 0f, 0f, false, new Vector3(.085f, .036f, .008f), Moth, .45f);
        P(6, "PicMothBody", 0f, .004f, true, new Vector3(.01f, .04f, .006f), Ink);
        P(7, "PicStagBody", 0f, -.008f, false, new Vector3(.04f, .05f, .01f), Stag, .8f);
        P(7, "PicStagJawL", -.012f, .028f, true, new Vector3(.008f, .026f, .006f), Stag, 1f, 25f);
        P(7, "PicStagJawR", .012f, .028f, true, new Vector3(.008f, .026f, .006f), Stag, 1f, -25f);
        P(8, "PicLonghornBody", 0f, -.01f, false, new Vector3(.03f, .06f, .01f), Ink, .8f);
        P(8, "PicLonghornAntL", -.022f, .03f, true, new Vector3(.006f, .06f, .006f), Ink, 1f, 35f);
        P(8, "PicLonghornAntR", .022f, .03f, true, new Vector3(.006f, .06f, .006f), Ink, 1f, -35f);
        P(8, "PicLonghornSpot", 0f, -.012f, true, new Vector3(.009f, .009f, .006f), White);
    }

    static void Tilted(GameObject go, string name, Vector3 at, Vector3 size, Color color, float roll)
    {
        PA_DemoIconBake.Part(go, name, at, size, color);
        go.transform.Find(name).localRotation = Quaternion.Euler(0, 0, roll);
    }

    static DemoPlaceableEntry Entry(Item item) => new DemoPlaceableEntry
    {
        item = item, key = Key, kind = DemoPlaceableKind.Furniture, buildingResource = "DemoStructure/InsectBoard",
        size = Vector2Int.one, surfaces = WorldPlaceableSurface.Living | WorldPlaceableSurface.Utility | WorldPlaceableSurface.ShopFloor, canMove = true,
    };

    public static IEnumerable<DemoPlaceableEntry> ExistingEntries()
    {
        var item = AssetDatabase.LoadAssetAtPath<Item>(Items + "/Item_InsectBoard.asset");
        if (item != null && AssetDatabase.LoadAssetAtPath<GameObject>(Structure + "/InsectBoard.prefab") != null) yield return Entry(item);
    }

    static void BuildPlaceable(GameObject model, List<string> log)
    {
        var root = new GameObject("InsectBoard");
        try
        {
            FirstDayStudioAssets.Place(model, root.transform, Vector3.zero, .95f);
            var box = root.AddComponent<BoxCollider>(); box.center = new Vector3(0, .47f, 0); box.size = new Vector3(.8f, .95f, .3f);
            root.SetActive(true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Structure + "/InsectBoard.prefab");
            string dataPath = Structure + "/InsectBoard.asset";
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(dataPath);
            if (data == null) { data = ScriptableObject.CreateInstance<BuildingData>(); AssetDatabase.CreateAsset(data, dataPath); }
            data.prefab = prefab; data.buildingName = "InsectBoard"; data.price = 0;
            EditorUtility.SetDirty(data);
            log.Add("placeable DemoStructure/InsectBoard");
        }
        finally { Object.DestroyImmediate(root); }
    }

    static void Recipe(string asset, string name, WorkbenchType bench, Item output, params (Item item, int count)[] ingredients)
    {
        string path = Structure + "/" + asset + ".asset";
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

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[INSECTBOARD] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static GameObject Prefab(string name, Action<GameObject> build)
    {
        var root = new GameObject(name);
        try { build(root); return PrefabUtility.SaveAsPrefabAsset(root, Art + "/" + name + ".prefab"); }
        finally { Object.DestroyImmediate(root); }
    }
}
