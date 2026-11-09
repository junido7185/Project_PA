using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 방아깨비(아이템 생태 연구 B04 "짧은 풀·흙 경계, 06–18, 긴 뒷다리, 도약 뒤 정지 때 다가감", owner 채택 2026-11-09).
// 실제 종 이름(2026-11-07 사용자 지시): 방아깨비 Acrida cinerea — 우리나라에서 가장 큰 메뚜기 무리. 녹색형·갈색형이 있는데,
// 밝은 풀밭에서 녹색은 보이지 않아 갈색형으로 만든다. Item_Bug_Grasshopper id 3106, 원재료 12G, 쌓기 20.
// 모델은 소유 로우폴리: 가늘고 긴 몸 + 앞으로 뾰족한 머리 + 납작한 더듬이 + 접은 긴 뒷다리(넓적다리·종아리) + 짧은 앞·가운데 다리.
// 배치: Unity -batchmode -executeMethod PA_GrasshopperSetup.BatchApply
public static class PA_GrasshopperSetup
{
    const string Items = "Assets/Resources/Items";
    const string Art = "Assets/Art/ProjectPA/Derived/Insects";
    public const int Grasshopper = 3106;

    static readonly Color Body = new Color(.66f, .5f, .3f), Stripe = new Color(.86f, .76f, .55f), Dark = new Color(.42f, .31f, .19f), Eye = new Color(.25f, .18f, .12f);

    [MenuItem("Project PA/Content/Grasshopper (Grass Edge Insect)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        string path = Items + "/Item_Bug_Grasshopper.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != path && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == Grasshopper);
        if (clash != null) throw new InvalidOperationException($"id {Grasshopper} already used by {clash}");
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Art/ProjectPA/Derived", "Insects");
        var log = new List<string>();
        // grasshopper-r180: 처음 만든 몸이 기본 카메라에서 가는 막대였다 → 몸을 굵게 다시 만든다. Part는 같은 이름의 메시를 재사용하므로 이 모델이 만든 메시만 지우고 새로 만든다.
        foreach (var guid in AssetDatabase.FindAssets("PA_Insect_Grasshopper_ t:Mesh"))
        {
            string meshPath = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileName(meshPath).StartsWith("PA_Insect_Grasshopper_") && AssetDatabase.DeleteAsset(meshPath)) log.Add("rebuilt " + System.IO.Path.GetFileNameWithoutExtension(meshPath));
        }
        var root = new GameObject("PA_Insect_Grasshopper");
        GameObject model;
        try
        {
            // +Z가 머리. 몸통(가슴+배)은 길고 가늘게, 등에 밝은 줄.
            PA_DemoIconBake.Part(root, "HopperAbdomen", new Vector3(0, .055f, -.1f), new Vector3(.07f, .065f, .2f), Body, .8f);
            PA_DemoIconBake.Part(root, "HopperThorax", new Vector3(0, .06f, .05f), new Vector3(.08f, .075f, .1f), Body, .8f);
            PA_DemoIconBake.Part(root, "HopperStripe", new Vector3(0, .094f, -.03f), new Vector3(.026f, .006f, .3f), Stripe);
            // 방아깨비 머리는 앞으로 길게 뾰족하다(원뿔 대신 끝을 줄인 상자 두 개).
            PA_DemoIconBake.Part(root, "HopperHead", new Vector3(0, .062f, .13f), new Vector3(.06f, .06f, .07f), Body, .8f);
            PA_DemoIconBake.Part(root, "HopperSnout", new Vector3(0, .066f, .185f), new Vector3(.038f, .04f, .05f), Body, .6f);
            PA_DemoIconBake.Part(root, "HopperEyeL", new Vector3(-.03f, .074f, .14f), new Vector3(.014f, .024f, .028f), Eye);
            PA_DemoIconBake.Part(root, "HopperEyeR", new Vector3(.03f, .074f, .14f), new Vector3(.014f, .024f, .028f), Eye);
            for (int side = -1; side <= 1; side += 2)
            {
                string s = side < 0 ? "L" : "R";
                // 납작한 칼 모양 더듬이: 머리 끝에서 앞·위로.
                PA_DemoIconBake.Part(root, "HopperAntenna" + s, new Vector3(side * .012f, .085f, .225f), new Vector3(.014f, .006f, .07f), Dark);
                // 뒷다리: 넓적다리는 몸 옆에서 위로 접혀 무릎이 등 높이, 종아리는 다시 뒤로 내려온다.
                PA_DemoIconBake.Part(root, "HopperThigh" + s, new Vector3(side * .05f, .08f, -.04f), new Vector3(.03f, .04f, .17f), Body, .75f);
                PA_DemoIconBake.Part(root, "HopperKnee" + s, new Vector3(side * .052f, .105f, -.125f), new Vector3(.026f, .026f, .026f), Dark);
                PA_DemoIconBake.Part(root, "HopperShin" + s, new Vector3(side * .056f, .05f, -.1f), new Vector3(.016f, .016f, .14f), Dark);
                PA_DemoIconBake.Part(root, "HopperLegFront" + s, new Vector3(side * .03f, .02f, .09f), new Vector3(.01f, .04f, .01f), Dark);
                PA_DemoIconBake.Part(root, "HopperLegMid" + s, new Vector3(side * .035f, .02f, .03f), new Vector3(.01f, .04f, .01f), Dark);
            }
            model = PrefabUtility.SaveAsPrefabAsset(root, Art + "/PA_Insect_Grasshopper.prefab");
        }
        finally { Object.DestroyImmediate(root); }
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = Grasshopper; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
        item.itemName = "방아깨비"; item.basePrice = 12; item.maxStack = 20; item.category = ItemCategory.Raw; item.toolType = ToolType.None;
        item.description = "뒷다리가 긴 갈색 메뚜기. 낮에 풀밭과 모래가 만나는 가장자리에서 폴짝 뛰어요. 뛴 다음 멈췄을 때 잠자리채를 휘둘러요.";
        item.model = model; item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("baked icon " + Grasshopper);
        AssetDatabase.SaveAssets();
        Debug.Log("[GRASSHOPPER] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[GRASSHOPPER] " + e); code = 1; }
        EditorApplication.Exit(code);
    }
}
