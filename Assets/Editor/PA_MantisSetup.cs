using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 왕사마귀(아이템 생태 연구 B10 "낮은 꽃/덤불 잎 옆, 06–18, 접은 앞다리·삼각 머리·짧은 좌우 흔들림. 색위장만으로 숨기지 않음", owner 채택 2026-11-09).
// 실제 종 이름(2026-11-07 사용자 지시): 왕사마귀 Tenodera sinensis(녹색형) — 우리나라에서 흔한 큰 사마귀. Item_Bug_Mantis id 3110, 원재료 20G, 쌓기 20.
// 모델은 소유 로우폴리: 연두 긴 배 + 가는 앞가슴 + 삼각 머리·큰 겹눈 + 모아 든 앞다리(낫) + 가는 다리 넷 + 등의 날개(갈색 가장자리 — 풀색 속에서도 윤곽이 보이게).
// 배치: Unity -batchmode -executeMethod PA_MantisSetup.BatchApply
public static class PA_MantisSetup
{
    const string Items = "Assets/Resources/Items";
    const string Art = "Assets/Art/ProjectPA/Derived/Insects";
    public const int Mantis = 3110;

    static readonly Color Body = new Color(.58f, .82f, .3f), Light = new Color(.74f, .9f, .46f), Edge = new Color(.5f, .36f, .2f), Eye = new Color(.86f, .9f, .55f), Pupil = new Color(.15f, .14f, .1f);

    [MenuItem("Project PA/Content/Mantis (Flower Insect)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        string path = Items + "/Item_Bug_Mantis.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != path && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == Mantis);
        if (clash != null) throw new InvalidOperationException($"id {Mantis} already used by {clash}");
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Art/ProjectPA/Derived", "Insects");
        var log = new List<string>();
        var root = new GameObject("PA_Insect_Mantis");
        GameObject model;
        try
        {
            // +Z가 머리. 배는 길고, 앞가슴은 가늘고 길게 위로 들려 있다.
            PA_DemoIconBake.Part(root, "MantisAbdomen", new Vector3(0, .05f, -.1f), new Vector3(.055f, .05f, .2f), Body, .8f);
            PA_DemoIconBake.Part(root, "MantisWings", new Vector3(0, .078f, -.08f), new Vector3(.05f, .01f, .19f), Light, .9f);
            PA_DemoIconBake.Part(root, "MantisWingEdgeL", new Vector3(-.026f, .08f, -.08f), new Vector3(.006f, .011f, .18f), Edge);
            PA_DemoIconBake.Part(root, "MantisWingEdgeR", new Vector3(.026f, .08f, -.08f), new Vector3(.006f, .011f, .18f), Edge);
            PA_DemoIconBake.Part(root, "MantisPronotum", new Vector3(0, .085f, .055f), new Vector3(.022f, .02f, .12f), Body, .85f);
            root.transform.Find("MantisPronotum").localRotation = Quaternion.Euler(-28f, 0f, 0f);
            // 삼각 머리: 위가 넓고 아래로 좁아지는 판 + 양옆 큰 겹눈.
            PA_DemoIconBake.Part(root, "MantisHead", new Vector3(0, .125f, .12f), new Vector3(.05f, .04f, .02f), Body, .5f);
            root.transform.Find("MantisHead").localRotation = Quaternion.Euler(180f, 0f, 0f);
            PA_DemoIconBake.Part(root, "MantisEyeL", new Vector3(-.027f, .135f, .12f), new Vector3(.016f, .02f, .018f), Eye);
            PA_DemoIconBake.Part(root, "MantisEyeR", new Vector3(.027f, .135f, .12f), new Vector3(.016f, .02f, .018f), Eye);
            PA_DemoIconBake.Part(root, "MantisPupilL", new Vector3(-.031f, .136f, .128f), new Vector3(.006f, .008f, .006f), Pupil);
            PA_DemoIconBake.Part(root, "MantisPupilR", new Vector3(.031f, .136f, .128f), new Vector3(.006f, .008f, .006f), Pupil);
            for (int side = -1; side <= 1; side += 2)
            {
                string s = side < 0 ? "L" : "R";
                // 모아 든 앞다리(낫): 위팔은 앞 아래로, 낫은 다시 위로 접힌다.
                PA_DemoIconBake.Part(root, "MantisRaptorUpper" + s, new Vector3(side * .018f, .085f, .1f), new Vector3(.014f, .016f, .07f), Light, .8f);
                root.transform.Find("MantisRaptorUpper" + s).localRotation = Quaternion.Euler(35f, 0f, 0f);
                PA_DemoIconBake.Part(root, "MantisRaptorBlade" + s, new Vector3(side * .018f, .08f, .135f), new Vector3(.011f, .012f, .055f), Light, .7f);
                root.transform.Find("MantisRaptorBlade" + s).localRotation = Quaternion.Euler(-40f, 0f, 0f);
                // 가는 다리 넷.
                for (int k = 0; k < 2; k++)
                {
                    string n = $"MantisLeg{s}{k}";
                    PA_DemoIconBake.Part(root, n, new Vector3(side * .05f, .025f, .01f - k * .07f), new Vector3(.08f, .008f, .008f), Body);
                    root.transform.Find(n).localRotation = Quaternion.Euler(0f, side * (20f - k * 40f), side * -30f);
                }
            }
            model = PrefabUtility.SaveAsPrefabAsset(root, Art + "/PA_Insect_Mantis.prefab");
        }
        finally { Object.DestroyImmediate(root); }
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = Mantis; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
        item.itemName = "왕사마귀"; item.basePrice = 20; item.maxStack = 20; item.category = ItemCategory.Raw; item.toolType = ToolType.None;
        item.description = "앞다리를 모으고 꽃 위에서 바람처럼 몸을 살랑 흔드는 연두색 사마귀. 낮에 꽃 위를 잘 살펴봐요.";
        item.model = model; item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("baked icon " + Mantis);
        AssetDatabase.SaveAssets();
        Debug.Log("[MANTIS] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[MANTIS] " + e); code = 1; }
        EditorApplication.Exit(code);
    }
}
