using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 알락하늘소(아이템 생태 연구 B09 "있는 그루터기 또는 낮은 나무 옆, 08–20, 긴 더듬이의 움직임. 잡기 위해 새로 벌목하도록 요구하지 않음", owner 채택 2026-11-09).
// 실제 종 이름(2026-11-07 사용자 지시): 알락하늘소 Anoplophora malasiaca — 광택 있는 검은 몸에 흰 점, 몸보다 훨씬 긴 더듬이에 흰 고리.
// Item_Bug_Longhorn id 3108, 원재료 26G, 쌓기 20.
// 모델은 소유 로우폴리: 길쭉한 검은 몸(딱지날개 둘·앞가슴·머리) + 흰 점 + 마디마다 흰 고리를 둔 긴 더듬이 두 개(LonghornAntennaL/R 묶음 —
// ForestBeetleSpawner가 뿌리 축 아래로 옮겨 흔든다) + 다리 여섯.
// 배치: Unity -batchmode -executeMethod PA_LonghornSetup.BatchApply
public static class PA_LonghornSetup
{
    const string Items = "Assets/Resources/Items";
    const string Art = "Assets/Art/ProjectPA/Derived/Insects";
    public const int Longhorn = 3108;

    static readonly Color Body = new Color(.1f, .1f, .12f), Spot = new Color(.93f, .94f, .96f), Ring = new Color(.78f, .84f, .9f), Leg = new Color(.09f, .09f, .1f);

    [MenuItem("Project PA/Content/Longhorn Beetle (Forest Insect)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        string path = Items + "/Item_Bug_Longhorn.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != path && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == Longhorn);
        if (clash != null) throw new InvalidOperationException($"id {Longhorn} already used by {clash}");
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Art/ProjectPA/Derived", "Insects");
        var log = new List<string>();
        var root = new GameObject("PA_Insect_Longhorn");
        GameObject model;
        try
        {
            // +Z가 머리. 몸통 먼저(첫 렌더러가 BugCritter 몸 기준).
            PA_DemoIconBake.Part(root, "LonghornElytraL", new Vector3(-.032f, .05f, -.09f), new Vector3(.062f, .055f, .2f), Body, .82f);
            PA_DemoIconBake.Part(root, "LonghornElytraR", new Vector3(.032f, .05f, -.09f), new Vector3(.062f, .055f, .2f), Body, .82f);
            PA_DemoIconBake.Part(root, "LonghornPronotum", new Vector3(0, .05f, .04f), new Vector3(.07f, .05f, .06f), Body, .85f);
            PA_DemoIconBake.Part(root, "LonghornHead", new Vector3(0, .045f, .085f), new Vector3(.055f, .045f, .04f), Body, .85f);
            var spots = new[] { new Vector3(-.035f, 0, -.03f), new Vector3(.03f, 0, -.05f), new Vector3(-.028f, 0, -.11f), new Vector3(.034f, 0, -.13f), new Vector3(-.03f, 0, -.17f), new Vector3(.028f, 0, -.18f) };
            for (int i = 0; i < spots.Length; i++)
                PA_DemoIconBake.Part(root, "LonghornSpot" + i, spots[i] + new Vector3(0, .079f, 0), new Vector3(.016f, .004f, .016f), Spot);
            for (int side = -1; side <= 1; side += 2)
            {
                string s = side < 0 ? "L" : "R";
                // 긴 더듬이: 머리 앞에서 뒤쪽 바깥으로 휘어 몸보다 길게. 마디 다섯, 마디 뿌리마다 흰 고리.
                var antenna = new GameObject("LonghornAntenna" + s);
                antenna.transform.SetParent(root.transform, false);
                antenna.transform.localPosition = new Vector3(side * .018f, .07f, .1f);
                for (int k = 0; k < 5; k++)
                {
                    float t = k / 4f;
                    var p = new Vector3(side * (.02f + .11f * t), .02f + .05f * Mathf.Sin(t * 2.4f), .03f - .12f * t * t);
                    var seg = $"LonghornAntenna{s}Seg{k}";
                    PA_DemoIconBake.Part(antenna, seg, p, new Vector3(.009f, .009f, .06f), Body);
                    antenna.transform.Find(seg).localRotation = Quaternion.Euler(-20f + 35f * t, side * (35f + 70f * t), 0f);
                    PA_DemoIconBake.Part(antenna, seg + "Ring", p + new Vector3(0, .001f, 0), new Vector3(.012f, .012f, .012f), Ring);
                }
                for (int k = 0; k < 3; k++)
                {
                    string n = $"LonghornLeg{s}{k}";
                    PA_DemoIconBake.Part(root, n, new Vector3(side * .07f, .022f, .05f - k * .07f), new Vector3(.09f, .012f, .012f), Leg);
                    root.transform.Find(n).localRotation = Quaternion.Euler(0f, side * (25f - k * 25f), side * -22f);
                }
            }
            model = PrefabUtility.SaveAsPrefabAsset(root, Art + "/PA_Insect_Longhorn.prefab");
        }
        finally { Object.DestroyImmediate(root); }
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = Longhorn; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
        item.itemName = "알락하늘소"; item.basePrice = 26; item.maxStack = 20; item.category = ItemCategory.Raw; item.toolType = ToolType.None;
        item.description = "검은 몸에 흰 점, 몸보다 훨씬 긴 더듬이를 천천히 흔드는 하늘소. 낮에 숲 나무 밑동 옆에 앉아 있어요. 천천히 다가가 잠자리채로 잡아요.";
        item.model = model; item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("baked icon " + Longhorn);
        AssetDatabase.SaveAssets();
        Debug.Log("[LONGHORN] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[LONGHORN] " + e); code = 1; }
        EditorApplication.Exit(code);
    }
}
