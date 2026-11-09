using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 고추잠자리(아이템 생태 연구 B06 "기존 물가의 마른 둔덕, 08–19, 가는 배·네 날개, 정지 비행 때 접근", owner 채택 2026-11-09).
// 실제 종 이름(2026-11-07 사용자 지시). Item_Bug_Dragonfly id 3104, 원재료 16G, 쌓기 20.
// 모델은 소유 로우폴리: 가늘고 긴 빨간 배 + 가슴 + 큰 겹눈 + 좌우로 펼친 투명한 날개 네 장(날갯짓은 BugCritter가 모델 X 폭으로).
// 배치: Unity -batchmode -executeMethod PA_DragonflySetup.BatchApply
public static class PA_DragonflySetup
{
    const string Items = "Assets/Resources/Items";
    const string Art = "Assets/Art/ProjectPA/Derived/Insects";
    public const int Dragonfly = 3104;

    static readonly Color Red = new Color(.9f, .22f, .14f), Thorax = new Color(.62f, .24f, .16f), Eye = new Color(.55f, .14f, .1f);
    static readonly Color Wing = new Color(.9f, .95f, 1f), Vein = new Color(.62f, .5f, .4f);

    [MenuItem("Project PA/Content/Red Dragonfly (Waterside Insect)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        string path = Items + "/Item_Bug_Dragonfly.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != path && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == Dragonfly);
        if (clash != null) throw new InvalidOperationException($"id {Dragonfly} already used by {clash}");
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Art/ProjectPA/Derived", "Insects");
        var log = new List<string>();
        var root = new GameObject("PA_Insect_Dragonfly");
        GameObject model;
        try
        {
            // +Z가 머리. 배는 뒤(-Z)로 길고 가늘게 마디 셋.
            PA_DemoIconBake.Part(root, "DragonAbdomenA", new Vector3(0, .05f, -.1f), new Vector3(.035f, .035f, .14f), Red, .85f);
            PA_DemoIconBake.Part(root, "DragonAbdomenB", new Vector3(0, .05f, -.23f), new Vector3(.028f, .028f, .13f), Red, .85f);
            PA_DemoIconBake.Part(root, "DragonAbdomenC", new Vector3(0, .05f, -.34f), new Vector3(.022f, .022f, .1f), Red, .8f);
            PA_DemoIconBake.Part(root, "DragonThorax", new Vector3(0, .055f, .02f), new Vector3(.06f, .06f, .1f), Thorax, .85f);
            PA_DemoIconBake.Part(root, "DragonEyeL", new Vector3(.03f, .065f, .1f), new Vector3(.045f, .05f, .05f), Eye, .8f);
            PA_DemoIconBake.Part(root, "DragonEyeR", new Vector3(-.03f, .065f, .1f), new Vector3(.045f, .05f, .05f), Eye, .8f);
            for (int side = -1; side <= 1; side += 2)
            {
                string s = side < 0 ? "L" : "R";
                PA_DemoIconBake.Part(root, "DragonForeWing" + s, new Vector3(side * .17f, .075f, .05f), new Vector3(.28f, .006f, .06f), Wing, .7f);
                PA_DemoIconBake.Part(root, "DragonHindWing" + s, new Vector3(side * .16f, .072f, -.03f), new Vector3(.26f, .006f, .07f), Wing, .7f);
                PA_DemoIconBake.Part(root, "DragonWingTip" + s, new Vector3(side * .28f, .078f, .05f), new Vector3(.025f, .007f, .03f), Vein);
            }
            model = PrefabUtility.SaveAsPrefabAsset(root, Art + "/PA_Insect_Dragonfly.prefab");
        }
        finally { Object.DestroyImmediate(root); }
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = Dragonfly; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
        item.itemName = "고추잠자리"; item.basePrice = 16; item.maxStack = 20; item.category = ItemCategory.Raw; item.toolType = ToolType.None;
        item.description = "빨간 배가 가늘고 긴 잠자리. 낮에 물가 바위 위를 맴돌다 잠깐 멈춰 떠 있어요. 그때 잠자리채를 휘둘러요.";
        item.model = model; item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("baked icon " + Dragonfly);
        AssetDatabase.SaveAssets();
        Debug.Log("[DRAGONFLY] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[DRAGONFLY] " + e); code = 1; }
        EditorApplication.Exit(code);
    }
}
