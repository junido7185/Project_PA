using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 왕귀뚜라미(아이템 생태 연구 B07 "가까운 풀밭 그늘, 16–24 + 06–08, 우는 방향과 풀 흔들림을 함께 읽고 정지 때 접근. 소리만 정답 단서로 두지 않음",
// owner 채택 2026-11-09). 실제 종 이름(2026-11-07 사용자 지시): 왕귀뚜라미 Teleogryllus emma — 우리나라 가을밤의 대표 귀뚜라미, 수컷이 날개를 비벼 운다.
// Item_Bug_Cricket id 3109, 원재료 18G, 쌓기 20.
// 모델은 소유 로우폴리: 진갈색 몸 + 둥근 검은 머리 + 등에 겹친 날개(CricketWings 묶음 — 울 때 들썩) + 굵은 뒷다리 + 긴 더듬이 + 꼬리털 둘.
// 배치: Unity -batchmode -executeMethod PA_CricketSetup.BatchApply
public static class PA_CricketSetup
{
    const string Items = "Assets/Resources/Items";
    const string Art = "Assets/Art/ProjectPA/Derived/Insects";
    public const int Cricket = 3109;

    static readonly Color Body = new Color(.22f, .15f, .1f), Head = new Color(.1f, .08f, .07f), Wing = new Color(.38f, .27f, .17f), Vein = new Color(.5f, .38f, .25f), Leg = new Color(.18f, .12f, .08f);

    [MenuItem("Project PA/Content/Cricket (Evening Grass Insect)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        string path = Items + "/Item_Bug_Cricket.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != path && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == Cricket);
        if (clash != null) throw new InvalidOperationException($"id {Cricket} already used by {clash}");
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Art/ProjectPA/Derived", "Insects");
        var log = new List<string>();
        var root = new GameObject("PA_Insect_Cricket");
        GameObject model;
        try
        {
            // +Z가 머리. 몸통 먼저(첫 렌더러가 BugCritter 몸 기준).
            PA_DemoIconBake.Part(root, "CricketAbdomen", new Vector3(0, .045f, -.06f), new Vector3(.075f, .06f, .14f), Body, .82f);
            PA_DemoIconBake.Part(root, "CricketThorax", new Vector3(0, .05f, .04f), new Vector3(.07f, .055f, .06f), Body, .85f);
            PA_DemoIconBake.Part(root, "CricketHead", new Vector3(0, .05f, .09f), new Vector3(.065f, .058f, .05f), Head, .8f);
            // 등의 날개: 묶음 축(앞 끝)에서 들썩이도록 날개 둘을 자식으로.
            var wings = new GameObject("CricketWings");
            wings.transform.SetParent(root.transform, false);
            wings.transform.localPosition = new Vector3(0, .078f, .03f);
            PA_DemoIconBake.Part(wings, "CricketWingL", new Vector3(-.019f, 0, -.07f), new Vector3(.04f, .008f, .13f), Wing, .9f);
            PA_DemoIconBake.Part(wings, "CricketWingR", new Vector3(.019f, 0, -.07f), new Vector3(.04f, .008f, .13f), Wing, .9f);
            PA_DemoIconBake.Part(wings, "CricketVein", new Vector3(0, .005f, -.06f), new Vector3(.006f, .004f, .11f), Vein);
            for (int side = -1; side <= 1; side += 2)
            {
                string s = side < 0 ? "L" : "R";
                // 긴 더듬이: 머리에서 뒤쪽 바깥으로 길게 휜다(마디 셋).
                for (int k = 0; k < 3; k++)
                {
                    string n = $"CricketAntenna{s}{k}";
                    float t = k / 2f;
                    PA_DemoIconBake.Part(root, n, new Vector3(side * (.02f + .05f * t), .07f + .02f * t, .14f + .03f - .04f * t * t), new Vector3(.006f, .006f, .07f), Head);
                    root.transform.Find(n).localRotation = Quaternion.Euler(-12f + 20f * t, side * (20f + 45f * t), 0f);
                }
                // 굵은 뒷다리(넓적다리 + 종아리), 짧은 앞·가운데 다리, 꼬리털.
                PA_DemoIconBake.Part(root, "CricketThigh" + s, new Vector3(side * .045f, .05f, -.05f), new Vector3(.026f, .038f, .11f), Body, .75f);
                PA_DemoIconBake.Part(root, "CricketShin" + s, new Vector3(side * .05f, .02f, -.1f), new Vector3(.012f, .012f, .1f), Leg);
                PA_DemoIconBake.Part(root, "CricketLegFront" + s, new Vector3(side * .035f, .015f, .07f), new Vector3(.01f, .035f, .01f), Leg);
                PA_DemoIconBake.Part(root, "CricketLegMid" + s, new Vector3(side * .04f, .015f, .02f), new Vector3(.01f, .035f, .01f), Leg);
                PA_DemoIconBake.Part(root, "CricketCercus" + s, new Vector3(side * .014f, .045f, -.15f), new Vector3(.006f, .006f, .05f), Leg);
                root.transform.Find("CricketCercus" + s).localRotation = Quaternion.Euler(0f, side * 15f, 0f);
            }
            model = PrefabUtility.SaveAsPrefabAsset(root, Art + "/PA_Insect_Cricket.prefab");
        }
        finally { Object.DestroyImmediate(root); }
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = Cricket; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
        item.itemName = "왕귀뚜라미"; item.basePrice = 18; item.maxStack = 20; item.category = ItemCategory.Raw; item.toolType = ToolType.None;
        item.description = "해 질 녘과 이른 아침, 나무 그늘 풀숲에서 '또르르르' 우는 귀뚜라미. 소리 나는 쪽 흔들리는 풀을 찾아 살금살금 다가가요.";
        item.model = model; item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("baked icon " + Cricket);
        AssetDatabase.SaveAssets();
        Debug.Log("[CRICKET] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[CRICKET] " + e); code = 1; }
        EditorApplication.Exit(code);
    }
}
