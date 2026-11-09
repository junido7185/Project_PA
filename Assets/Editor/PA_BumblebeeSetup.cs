using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 호박벌(아이템 생태 연구 B05 "꽃 군락 가장자리, 08–18, 통통한 몸, 꽃 사이 머무는 위치를 기다림", owner 채택 2026-11-09).
// 실제 종 이름(2026-11-07 사용자 지시): 호박벌 Bombus ignitus — 우리나라 토박이 뒤영벌. 일벌은 검은 털옷에 배 끝이 주황색.
// Item_Bug_Bumblebee id 3105, 원재료 15G, 쌓기 20. 공격·꿀·벌통은 이 후보에 넣지 않는다(연구 B05).
// 모델은 소유 로우폴리: 통통한 검은 가슴·배 + 주황 배 끝 + 연한 날개 두 장(BeeWingL/R — BumblebeeSpawner가 날개 축 아래로 옮겨 날갯짓).
// 배치: Unity -batchmode -executeMethod PA_BumblebeeSetup.BatchApply
public static class PA_BumblebeeSetup
{
    const string Items = "Assets/Resources/Items";
    const string Art = "Assets/Art/ProjectPA/Derived/Insects";
    public const int Bumblebee = 3105;

    static readonly Color Fur = new Color(.16f, .14f, .14f), FurLight = new Color(.24f, .21f, .2f), Tail = new Color(.95f, .48f, .16f);
    static readonly Color Wing = new Color(.84f, .86f, .88f), Eye = new Color(.07f, .06f, .06f);

    [MenuItem("Project PA/Content/Bumblebee (Flower Insect)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        string path = Items + "/Item_Bug_Bumblebee.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != path && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == Bumblebee);
        if (clash != null) throw new InvalidOperationException($"id {Bumblebee} already used by {clash}");
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Art/ProjectPA/Derived", "Insects");
        var log = new List<string>();
        var root = new GameObject("PA_Insect_Bumblebee");
        GameObject model;
        try
        {
            // +Z가 머리. 가슴(첫 렌더러 — BugCritter 몸 기준)부터 만든다.
            PA_DemoIconBake.Part(root, "BeeThorax", new Vector3(0, .09f, .02f), new Vector3(.13f, .11f, .11f), FurLight, .8f);
            PA_DemoIconBake.Part(root, "BeeAbdomenA", new Vector3(0, .085f, -.075f), new Vector3(.15f, .125f, .11f), Fur, .82f);
            PA_DemoIconBake.Part(root, "BeeAbdomenB", new Vector3(0, .08f, -.155f), new Vector3(.135f, .11f, .07f), Fur, .8f);
            PA_DemoIconBake.Part(root, "BeeTail", new Vector3(0, .075f, -.21f), new Vector3(.105f, .09f, .06f), Tail, .75f);
            PA_DemoIconBake.Part(root, "BeeHead", new Vector3(0, .08f, .1f), new Vector3(.085f, .075f, .06f), Fur, .8f);
            PA_DemoIconBake.Part(root, "BeeEyeL", new Vector3(-.035f, .088f, .115f), new Vector3(.02f, .04f, .03f), Eye);
            PA_DemoIconBake.Part(root, "BeeEyeR", new Vector3(.035f, .088f, .115f), new Vector3(.02f, .04f, .03f), Eye);
            PA_DemoIconBake.Part(root, "BeeAntennaL", new Vector3(-.02f, .125f, .13f), new Vector3(.012f, .045f, .012f), Fur);
            PA_DemoIconBake.Part(root, "BeeAntennaR", new Vector3(.02f, .125f, .13f), new Vector3(.012f, .045f, .012f), Fur);
            for (int side = -1; side <= 1; side += 2)
            {
                string s = side < 0 ? "L" : "R";
                PA_DemoIconBake.Part(root, "BeeLegFront" + s, new Vector3(side * .05f, .03f, .04f), new Vector3(.016f, .05f, .016f), Fur);
                PA_DemoIconBake.Part(root, "BeeLegBack" + s, new Vector3(side * .055f, .03f, -.03f), new Vector3(.018f, .05f, .018f), Fur);
                // 날개: 가슴 위에서 뒤로 비스듬히. 날개 뿌리는 x = ±.03 (BumblebeeSpawner가 여기를 축으로 삼는다).
                PA_DemoIconBake.Part(root, "BeeWing" + s, new Vector3(side * .09f, .15f, -.04f), new Vector3(.12f, .008f, .075f), Wing, .9f);
            }
            model = PrefabUtility.SaveAsPrefabAsset(root, Art + "/PA_Insect_Bumblebee.prefab");
        }
        finally { Object.DestroyImmediate(root); }
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = Bumblebee; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
        item.itemName = "호박벌"; item.basePrice = 15; item.maxStack = 20; item.category = ItemCategory.Raw; item.toolType = ToolType.None;
        item.description = "검은 털옷에 배 끝만 주황색인 통통한 벌. 낮에 꽃 사이를 옮겨 다녀요. 꽃에 앉았을 때 잠자리채를 휘둘러요.";
        item.model = model; item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("baked icon " + Bumblebee);
        AssetDatabase.SaveAssets();
        Debug.Log("[BUMBLEBEE] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[BUMBLEBEE] " + e); code = 1; }
        EditorApplication.Exit(code);
    }
}
