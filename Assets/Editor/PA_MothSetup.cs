using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 등불 나방(Content-insects-plan §C.3 저녁 시간대 곤충, 아이템 생태 연구 B11 owner 채택 2026-11-04).
// Item_Bug_Moth id 3102, 원재료 18G, 쌓기 20. 모델은 소유 로우폴리(굵은 몸 + 접힌 삼각 날개 + 더듬이).
// 배치: Unity -batchmode -executeMethod PA_MothSetup.BatchApply
public static class PA_MothSetup
{
    const string Items = "Assets/Resources/Items";
    const string Art = "Assets/Art/ProjectPA/Derived/Insects";
    public const int Moth = 3102;

    // 2026-11-04 moth-r1: 밤 풀밭 위 갈색은 보이지 않아 등불을 받는 연한 크림 날개로(실물 등불 나방처럼 밝게).
    static readonly Color Body = new Color(.62f, .54f, .44f), Wing = new Color(.93f, .88f, .74f), WingEdge = new Color(.82f, .75f, .6f), Eye = new Color(.2f, .16f, .12f);

    [MenuItem("Project PA/Content/Lamp Moth (Evening Insect)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        string path = Items + "/Item_Bug_Moth.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != path && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == Moth);
        if (clash != null) throw new InvalidOperationException($"id {Moth} already used by {clash}");
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Art/ProjectPA/Derived", "Insects");
        var log = new List<string>();
        var root = new GameObject("PA_Insect_LampMoth");
        GameObject model;
        try
        {
            // 몸 +Z가 머리. 날개는 좌우로 펼친 삼각형(X 방향) — BugCritter의 날갯짓은 모델 X 폭을 접었다 편다.
            var pivot = new GameObject("PA_Insect_LampMoth_Pivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0, .05f, -.12f);
            pivot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            PA_DigSetup.Round(pivot, "MothBody", Vector3.zero,
                new[] { PA_DigSetup.V(.015f, 0), PA_DigSetup.V(.045f, .06f), PA_DigSetup.V(.05f, .14f), PA_DigSetup.V(.04f, .2f), PA_DigSetup.V(0, .24f) }, Body, 10);
            // 2026-11-04 아이콘 검토: 직사각 날개 한 장은 널빤지처럼 보여 앞날개(뒤로 젖힘)·뒷날개 두 장 + 가장자리 무늬로.
            for (int side = -1; side <= 1; side += 2)
            {
                string s1 = side < 0 ? "L" : "R";
                PA_DemoIconBake.Part(root, "MothFore" + s1, new Vector3(side * .13f, .07f, .03f), new Vector3(.24f, .012f, .15f), Wing, .55f);
                root.transform.Find("MothFore" + s1).localRotation = Quaternion.Euler(0f, side * -32f, side * -6f);
                PA_DemoIconBake.Part(root, "MothHind" + s1, new Vector3(side * .1f, .066f, -.08f), new Vector3(.16f, .012f, .13f), WingEdge, .55f);
                root.transform.Find("MothHind" + s1).localRotation = Quaternion.Euler(0f, side * -58f, side * -6f);
                PA_DemoIconBake.Part(root, "MothSpot" + s1, new Vector3(side * .16f, .078f, .02f), new Vector3(.05f, .006f, .05f), Eye, .7f);
                string a = "MothFeeler" + s1;
                PA_DemoIconBake.Part(root, a, new Vector3(side * .03f, .09f, .15f), new Vector3(.008f, .008f, .08f), Eye);
                root.transform.Find(a).localRotation = Quaternion.Euler(-25f, side * 25f, 0f);
            }
            model = PrefabUtility.SaveAsPrefabAsset(root, Art + "/PA_Insect_LampMoth.prefab");
        }
        finally { Object.DestroyImmediate(root); }
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = Moth; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
        item.itemName = "등불 나방"; item.basePrice = 18; item.maxStack = 20; item.category = ItemCategory.Raw; item.toolType = ToolType.None;
        item.description = "해 질 녘부터 가게 앞 등불 가까이 모여드는 갈색 나방. 저녁에만 보여요. 영업하러 가는 길에 잠자리채로 잡을 수 있어요.";
        item.model = model; item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("baked icon " + Moth);
        AssetDatabase.SaveAssets();
        Debug.Log("[LAMP-MOTH] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[LAMP-MOTH] " + e); code = 1; }
        EditorApplication.Exit(code);
    }
}
