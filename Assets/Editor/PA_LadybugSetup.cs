using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 칠성무당벌레(아이템 생태 연구 B03 "꽃 군락의 낮은 잎/꽃 옆, 06–18, 반구형 몸", owner 채택 2026-11-09).
// 실제 종 이름(2026-11-07 사용자 지시). Item_Bug_Ladybug id 3103, 원재료 14G, 쌓기 20. 모델은 소유 로우폴리(빨간 반구 + 검은 점 7 + 검은 머리).
// 배치: Unity -batchmode -executeMethod PA_LadybugSetup.BatchApply
public static class PA_LadybugSetup
{
    const string Items = "Assets/Resources/Items";
    const string Art = "Assets/Art/ProjectPA/Derived/Insects";
    public const int Ladybug = 3103;

    static readonly Color Shell = new Color(.88f, .16f, .12f), Spot = new Color(.08f, .07f, .07f), Head = new Color(.1f, .09f, .09f), Cheek = new Color(.96f, .94f, .88f);

    [MenuItem("Project PA/Content/Seven-spot Ladybug (Flower Insect)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        string path = Items + "/Item_Bug_Ladybug.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != path && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == Ladybug);
        if (clash != null) throw new InvalidOperationException($"id {Ladybug} already used by {clash}");
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Art/ProjectPA/Derived", "Insects");
        var log = new List<string>();
        var root = new GameObject("PA_Insect_Ladybug");
        GameObject model;
        try
        {
            // 반구 등껍질(+Z가 머리). 가운데 갈라진 줄, 양쪽 점 3개씩 + 앞쪽 가운데 1개 = 7개.
            PA_DigSetup.Round(root, "LadybugShell", Vector3.zero,
                new[] { PA_DigSetup.V(.12f, 0), PA_DigSetup.V(.118f, .03f), PA_DigSetup.V(.1f, .07f), PA_DigSetup.V(.06f, .1f), PA_DigSetup.V(0, .112f) }, Shell, 12);
            PA_DemoIconBake.Part(root, "LadybugSeam", new Vector3(0, .108f, -.01f), new Vector3(.008f, .01f, .2f), Spot);
            var spots = new[] { new Vector3(.055f, 0, .05f), new Vector3(.075f, 0, -.02f), new Vector3(.045f, 0, -.075f),
                                new Vector3(-.055f, 0, .05f), new Vector3(-.075f, 0, -.02f), new Vector3(-.045f, 0, -.075f), new Vector3(0, 0, .085f) };
            for (int i = 0; i < spots.Length; i++)
            {
                float y = .1f - spots[i].magnitude * .35f;
                PA_DemoIconBake.Part(root, "LadybugSpot" + i, spots[i] + new Vector3(0, y, 0), new Vector3(.035f, .012f, .035f), Spot, .7f);
            }
            PA_DemoIconBake.Part(root, "LadybugHead", new Vector3(0, .035f, .125f), new Vector3(.09f, .06f, .05f), Head, .8f);
            PA_DemoIconBake.Part(root, "LadybugCheekL", new Vector3(.03f, .045f, .14f), new Vector3(.02f, .02f, .015f), Cheek);
            PA_DemoIconBake.Part(root, "LadybugCheekR", new Vector3(-.03f, .045f, .14f), new Vector3(.02f, .02f, .015f), Cheek);
            model = PrefabUtility.SaveAsPrefabAsset(root, Art + "/PA_Insect_Ladybug.prefab");
        }
        finally { Object.DestroyImmediate(root); }
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = Ladybug; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
        item.itemName = "칠성무당벌레"; item.basePrice = 14; item.maxStack = 20; item.category = ItemCategory.Raw; item.toolType = ToolType.None;
        item.description = "빨간 등에 검은 점 일곱 개. 낮에 풀밭 가장자리 꽃 위에 앉아 있어요. 살금살금 다가가 잠자리채로 잡아요.";
        item.model = model; item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("baked icon " + Ladybug);
        AssetDatabase.SaveAssets();
        Debug.Log("[LADYBUG] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[LADYBUG] " + e); code = 1; }
        EditorApplication.Exit(code);
    }
}
