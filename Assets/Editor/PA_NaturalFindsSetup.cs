using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 산호 조각·호박 조각(Docs/01_GAME_DESIGN/Content/NATURAL_FINDS.md, 아이템 생태 연구 H06·D06 owner 채택 2026-11-04).
// 연구 경계: 레시피 없이 소장·판매만 해도 되는 '자연사 관심' 발견물. 고급 장비의 필수재로 만들지 않는다.
// 호박 속 작은 점(갇힌 벌레)은 표현일 뿐 곤충 도감 한 종으로 세지 않는다.
// 배치: Unity -batchmode -executeMethod PA_NaturalFindsSetup.BatchApply
public static class PA_NaturalFindsSetup
{
    const string Items = "Assets/Resources/Items";
    const string BeachArt = "Assets/Art/ProjectPA/Derived/Beach";
    const string DigArt = "Assets/Art/ProjectPA/Derived/Dig";
    public const int Coral = 2054, Amber = 2055;

    static readonly Color Pink = new Color(.96f, .55f, .55f), PinkLight = new Color(.99f, .72f, .68f), Base = new Color(.9f, .84f, .74f);
    static readonly Color Honey = new Color(.95f, .62f, .18f), HoneyLight = new Color(1f, .78f, .36f), Speck = new Color(.24f, .16f, .1f);

    [MenuItem("Project PA/Content/Natural Finds (Coral · Amber)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        foreach (var (id, asset) in new[] { (Coral, "Item_CoralPiece"), (Amber, "Item_AmberPiece") })
        {
            string own = Items + "/" + asset + ".asset";
            var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(path => path != own && AssetDatabase.LoadAssetAtPath<Item>(path)?.id == id);
            if (clash != null) throw new InvalidOperationException($"id {id} already used by {clash}");
        }
        var coral = Prefab(BeachArt, "PA_Beach_Coral", go =>
        {
            // 떠밀려 온 갈라진 가지 산호: 밑동에서 세 갈래, 끝은 가늘게.
            PA_DemoIconBake.Part(go, "CoralFoot", new Vector3(0, .03f, 0), new Vector3(.14f, .06f, .12f), Base, .8f);
            Branch(go, "CoralA", new Vector3(0, .06f, 0), 0f, 0f, .26f, Pink);
            Branch(go, "CoralB", new Vector3(-.03f, .1f, 0), 0f, 34f, .2f, PinkLight);
            Branch(go, "CoralC", new Vector3(.03f, .09f, .01f), 12f, -30f, .18f, Pink);
            Branch(go, "CoralD", new Vector3(-.08f, .2f, 0), 0f, 60f, .1f, PinkLight);
            Branch(go, "CoralE", new Vector3(.07f, .19f, .01f), 0f, -55f, .09f, PinkLight);
        });
        Build(Coral, "Item_CoralPiece", "산호 조각", 22, coral,
            "파도에 떠밀려 온 분홍 가지 산호. 따로 만들 것은 없지만, 모아 두거나 파는 손님이 반겨요.", log);
        var amber = Prefab(DigArt, "PA_Dig_Amber", go =>
        {
            // 둥글게 닳은 꿀빛 덩어리 + 속의 작은 점.
            // 2026-11-04 아이콘 검토: 8각 상자 둘은 치즈 덩어리처럼 보여 돌림판 둥근 덩어리(옆으로 살짝 납작)로.
            var pivot = new GameObject("PA_Dig_Amber_Pivot");
            pivot.transform.SetParent(go.transform, false);
            pivot.transform.localScale = new Vector3(1.15f, 1f, .85f);
            PA_DigSetup.Round(pivot, "Lump", Vector3.zero, new[] { PA_DigSetup.V(.09f, 0), PA_DigSetup.V(.13f, .04f), PA_DigSetup.V(.135f, .09f), PA_DigSetup.V(.1f, .14f), PA_DigSetup.V(.04f, .165f), PA_DigSetup.V(0, .17f) }, Honey, 10);
            PA_DigSetup.Round(pivot, "Shine", new Vector3(-.03f, .13f, -.02f), new[] { PA_DigSetup.V(.05f, 0), PA_DigSetup.V(.04f, .03f), PA_DigSetup.V(0, .04f) }, HoneyLight, 8);
            PA_DemoIconBake.Part(go, "AmberSpeckDot", new Vector3(.04f, .08f, -.108f), new Vector3(.035f, .025f, .01f), Speck);
        });
        Build(Amber, "Item_AmberPiece", "호박 조각", 40, amber,
            "숲 흙에서 나온 꿀빛 나무 진 덩어리. 속에 아주 작은 점이 갇혀 있어요. 귀한 자연물이라 값이 좋아요.", log);
        AssetDatabase.SaveAssets();
        Debug.Log("[NATURAL-FINDS] " + string.Join(" | ", log));
    }

    static void Branch(GameObject root, string name, Vector3 at, float rx, float rz, float length, Color color)
    {
        var dir = Quaternion.Euler(rx, 0, rz) * Vector3.up;
        PA_DemoIconBake.Part(root, name, at + dir * length * .5f, new Vector3(.045f, length, .045f), color, .55f);
        root.transform.Find(name).localRotation = Quaternion.Euler(rx, 0, rz);
    }

    static void Build(int id, string asset, string name, int price, GameObject model, string description, List<string> log)
    {
        string path = Items + "/" + asset + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = id; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
        item.itemName = name; item.description = description; item.basePrice = price;
        item.maxStack = 99; item.category = ItemCategory.Raw; item.toolType = ToolType.None;
        item.model = model; item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("baked icon " + id);
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[NATURAL-FINDS] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static GameObject Prefab(string folder, string name, Action<GameObject> build)
    {
        var root = new GameObject(name);
        try { build(root); return PrefabUtility.SaveAsPrefabAsset(root, folder + "/" + name + ".prefab"); }
        finally { Object.DestroyImmediate(root); }
    }
}
