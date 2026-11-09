using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 도감 기념패(Docs/01_GAME_DESIGN/Content/COLLECTION.md §완성 보상, 아이템 생태 연구 §8.2 "현금·자원·생산 배율 없는 표지" 채택 2026-11-04).
// 새 아이템: 도감 기념패(2051, 배치 장식). 도감 섹션(바다·곤충·해변·땅속)을 처음 다 채울 때마다 하나씩 받는다(DiscoveryLog).
// 배치 항목 키 "decor-plaque"는 PA_DemoStructureSetup가 catalog를 다시 쓸 때 ExistingEntries()로 보존된다.
// 배치: Unity -batchmode -executeMethod PA_CollectionPlaqueSetup.BatchApply
public static class PA_CollectionPlaqueSetup
{
    const string Items = "Assets/Resources/Items";
    const string Structure = "Assets/Resources/DemoStructure";
    const string Art = "Assets/Art/ProjectPA/Derived/ItemPresentation/Models";
    public const string Key = "decor-plaque";
    public const int Plaque = 2051;

    static readonly Color Wood = new Color(.58f, .38f, .2f), DarkWood = new Color(.38f, .24f, .12f);
    static readonly Color Board = new Color(.2f, .48f, .46f), Gold = new Color(.98f, .8f, .3f), Cream = new Color(.95f, .9f, .78f);

    [MenuItem("Project PA/Content/Collection Plaque (Completion Decor)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != Items + "/Item_CollectionPlaque.asset" && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == Plaque);
        if (clash != null) throw new InvalidOperationException($"id {Plaque} already used by {clash}");
        var model = Prefab("PA_Decor_CollectionPlaque", go =>
        {
            // 받침 + 기둥 둘 + 청록 판(크림 테두리) + 금빛 별: 도감 앱의 청록·크림 색과 같은 결.
            PA_DemoIconBake.Part(go, "PlaqueFoot", new Vector3(0, .03f, 0), new Vector3(.5f, .06f, .22f), DarkWood, .85f);
            PA_DemoIconBake.Part(go, "PlaqueLegL", new Vector3(-.18f, .3f, 0), new Vector3(.05f, .5f, .05f), Wood);
            PA_DemoIconBake.Part(go, "PlaqueLegR", new Vector3(.18f, .3f, 0), new Vector3(.05f, .5f, .05f), Wood);
            PA_DemoIconBake.Part(go, "PlaqueFrame", new Vector3(0, .62f, 0), new Vector3(.56f, .4f, .05f), Cream);
            PA_DemoIconBake.Part(go, "PlaqueBoard", new Vector3(0, .62f, -.012f), new Vector3(.48f, .32f, .04f), Board);
            for (int i = 0; i < 5; i++)
            {
                string n = "PlaqueStar" + i;
                PA_DemoIconBake.Part(go, n, new Vector3(0, .64f, -.04f), new Vector3(.05f, .16f, .02f), Gold, .4f);
                go.transform.Find(n).localRotation = Quaternion.Euler(0, 0, i * 72f);
            }
            PA_DemoIconBake.Part(go, "PlaqueRibbon", new Vector3(0, .49f, -.04f), new Vector3(.3f, .04f, .015f), Gold);
        });
        string path = Items + "/Item_CollectionPlaque.asset";
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = Plaque; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
        item.itemName = "도감 기념패"; item.basePrice = 0; item.maxStack = 4; item.category = ItemCategory.Tool; item.toolType = ToolType.None;
        item.description = "도감의 한 섹션을 다 채우면 받는 기념패. 집이나 가게에 세워 두면 지금까지 찾은 것들이 떠올라요(꾸밈 전용).";
        item.model = model; item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("baked icon " + Plaque);
        BuildPlaceable(model, log);
        var catalog = AssetDatabase.LoadAssetAtPath<DemoPlaceableCatalog>(Structure + "/Catalog.asset") ?? throw new InvalidOperationException("Missing Catalog.asset");
        var entries = (catalog.entries ?? Array.Empty<DemoPlaceableEntry>()).Where(e => e != null && e.key != Key).ToList();
        entries.Add(Entry(item));
        catalog.entries = entries.ToArray();
        EditorUtility.SetDirty(catalog);
        log.Add("catalog entry " + Key);
        AssetDatabase.SaveAssets();
        Debug.Log("[PLAQUE] " + string.Join(" | ", log));
    }

    static DemoPlaceableEntry Entry(Item item) => new DemoPlaceableEntry
    {
        item = item, key = Key, kind = DemoPlaceableKind.Furniture, buildingResource = "DemoStructure/CollectionPlaque",
        size = Vector2Int.one, surfaces = WorldPlaceableSurface.Living | WorldPlaceableSurface.Utility | WorldPlaceableSurface.ShopFloor, canMove = true,
    };

    public static IEnumerable<DemoPlaceableEntry> ExistingEntries()
    {
        var item = AssetDatabase.LoadAssetAtPath<Item>(Items + "/Item_CollectionPlaque.asset");
        if (item != null && AssetDatabase.LoadAssetAtPath<GameObject>(Structure + "/CollectionPlaque.prefab") != null) yield return Entry(item);
    }

    static void BuildPlaceable(GameObject model, List<string> log)
    {
        var root = new GameObject("CollectionPlaque");
        try
        {
            FirstDayStudioAssets.Place(model, root.transform, Vector3.zero, .85f);
            var box = root.AddComponent<BoxCollider>(); box.center = new Vector3(0, .42f, 0); box.size = new Vector3(.7f, .85f, .35f);
            root.SetActive(true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Structure + "/CollectionPlaque.prefab");
            string dataPath = Structure + "/CollectionPlaque.asset";
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(dataPath);
            if (data == null) { data = ScriptableObject.CreateInstance<BuildingData>(); AssetDatabase.CreateAsset(data, dataPath); }
            data.prefab = prefab; data.buildingName = "CollectionPlaque"; data.price = 0;
            EditorUtility.SetDirty(data);
            log.Add("placeable DemoStructure/CollectionPlaque");
        }
        finally { Object.DestroyImmediate(root); }
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[PLAQUE] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static GameObject Prefab(string name, Action<GameObject> build)
    {
        var root = new GameObject(name);
        try { build(root); return PrefabUtility.SaveAsPrefabAsset(root, Art + "/" + name + ".prefab"); }
        finally { Object.DestroyImmediate(root); }
    }
}
