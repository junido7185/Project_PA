using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 넓적사슴벌레(아이템 생태 연구 B08 "숲의 낮은 나무줄기, 14–24, 큰 턱. 드물게 만나도 충분히 관찰할 틈 제공", owner 채택 2026-11-09).
// 실제 종 이름(2026-11-07 사용자 지시): 넓적사슴벌레 Dorcus titanus castanicolor — 우리나라에서 가장 흔한 사슴벌레, 밤에 참나무 진을 먹는다.
// Item_Bug_StagBeetle id 3107, 원재료 30G, 쌓기 20. 필수 납품에 넣지 않는다(연구 B08).
// 모델은 소유 로우폴리: 납작하고 넓은 검은 몸(딱지날개 둘 + 앞가슴) + 넓은 머리 + 안쪽으로 휜 큰 턱 두 개(안쪽 돌기) + 다리 여섯.
// 배치: Unity -batchmode -executeMethod PA_StagBeetleSetup.BatchApply
public static class PA_StagBeetleSetup
{
    const string Items = "Assets/Resources/Items";
    const string Art = "Assets/Art/ProjectPA/Derived/Insects";
    public const int StagBeetle = 3107;

    static readonly Color Body = new Color(.13f, .1f, .08f), Shine = new Color(.32f, .24f, .17f), Jaw = new Color(.2f, .13f, .08f), Leg = new Color(.1f, .08f, .06f);

    [MenuItem("Project PA/Content/Stag Beetle (Forest Insect)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        string path = Items + "/Item_Bug_StagBeetle.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != path && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == StagBeetle);
        if (clash != null) throw new InvalidOperationException($"id {StagBeetle} already used by {clash}");
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Art/ProjectPA/Derived", "Insects");
        var log = new List<string>();
        var root = new GameObject("PA_Insect_StagBeetle");
        GameObject model;
        try
        {
            // +Z가 머리. 딱지날개(뒤) → 앞가슴 → 머리 → 큰 턱 순으로 납작하게.
            PA_DemoIconBake.Part(root, "StagElytraL", new Vector3(-.045f, .05f, -.1f), new Vector3(.09f, .06f, .2f), Body, .85f);
            PA_DemoIconBake.Part(root, "StagElytraR", new Vector3(.045f, .05f, -.1f), new Vector3(.09f, .06f, .2f), Body, .85f);
            PA_DemoIconBake.Part(root, "StagShine", new Vector3(-.05f, .082f, -.12f), new Vector3(.03f, .006f, .11f), Shine, .6f);
            PA_DemoIconBake.Part(root, "StagPronotum", new Vector3(0, .05f, .04f), new Vector3(.17f, .055f, .08f), Body, .85f);
            PA_DemoIconBake.Part(root, "StagHead", new Vector3(0, .045f, .11f), new Vector3(.15f, .045f, .06f), Body, .85f);
            for (int side = -1; side <= 1; side += 2)
            {
                string s = side < 0 ? "L" : "R";
                // 큰 턱: 머리 앞 모서리에서 앞·바깥으로 뻗었다가(뿌리) 끝이 안쪽으로 휜다(끝). 안쪽에 작은 돌기 하나.
                Tilted(root, "StagJawBase" + s, new Vector3(side * .05f, .05f, .175f), new Vector3(.03f, .025f, .09f), Jaw, side * 14f);
                Tilted(root, "StagJawTip" + s, new Vector3(side * .042f, .05f, .235f), new Vector3(.025f, .022f, .06f), Jaw, side * -30f);
                PA_DemoIconBake.Part(root, "StagJawTooth" + s, new Vector3(side * .035f, .05f, .2f), new Vector3(.02f, .018f, .015f), Jaw);
                for (int k = 0; k < 3; k++)
                {
                    string n = $"StagLeg{s}{k}";
                    PA_DemoIconBake.Part(root, n, new Vector3(side * .12f, .025f, .06f - k * .09f), new Vector3(.11f, .016f, .016f), Leg);
                    root.transform.Find(n).localRotation = Quaternion.Euler(0f, side * (25f - k * 25f), side * -22f);
                }
            }
            model = PrefabUtility.SaveAsPrefabAsset(root, Art + "/PA_Insect_StagBeetle.prefab");
        }
        finally { Object.DestroyImmediate(root); }
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = StagBeetle; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
        item.itemName = "넓적사슴벌레"; item.basePrice = 30; item.maxStack = 20; item.category = ItemCategory.Raw; item.toolType = ToolType.None;
        item.description = "납작하고 까만 몸에 큰 턱이 멋진 사슴벌레. 오후부터 밤까지 숲 나무 밑동에 나와 있어요. 천천히 다가가 잠자리채로 잡아요.";
        item.model = model; item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("baked icon " + StagBeetle);
        AssetDatabase.SaveAssets();
        Debug.Log("[STAGBEETLE] " + string.Join(" | ", log));
    }

    static void Tilted(GameObject root, string name, Vector3 at, Vector3 size, Color color, float yaw)
    {
        PA_DemoIconBake.Part(root, name, at, size, color, .8f);
        root.transform.Find(name).localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[STAGBEETLE] " + e); code = 1; }
        EditorApplication.Exit(code);
    }
}
