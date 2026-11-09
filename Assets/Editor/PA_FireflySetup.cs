using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 애반딧불이(아이템 생태 연구 B12 "습한 물가 풀의 마른 쪽, 17–24, 꺼졌다 켜지는 작은 빛, 정지 지점을 보고 접근").
// 실제 종: 애반딧불이 Luciola lateralis — 애벌레가 맑은 민물에서 자라는 우리나라 반딧불이. Item_Bug_Firefly id 3111, 원재료 18G, 쌓기 20.
// 생성 섬의 강(FishingSpot.FreshCellCount)이 확인돼 담수 전제가 풀렸다(2026-11-09, Content/FRESH_FISH.md).
// 모델은 소유 로우폴리: 검은 딱지날개 + 주황 앞가슴(가운데 검은 점) + 연노랑 꽁무니 발광 마디(모델 이름 FireflyLantern — 실행 중 깜빡임이 이 마디를 찾는다).
// 배치: Unity -batchmode -executeMethod PA_FireflySetup.BatchApply
public static class PA_FireflySetup
{
    const string Items = "Assets/Resources/Items";
    const string Art = "Assets/Art/ProjectPA/Derived/Insects";
    public const int Firefly = 3111;

    static readonly Color Elytra = new Color(.12f, .12f, .13f), Pronotum = new Color(.95f, .55f, .3f), Spot = new Color(.15f, .1f, .08f);
    static readonly Color Lantern = new Color(.93f, .97f, .55f), Leg = new Color(.18f, .17f, .16f);

    [MenuItem("Project PA/Content/Firefly (River Insect)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        string path = Items + "/Item_Bug_Firefly.asset";
        var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p != path && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == Firefly);
        if (clash != null) throw new InvalidOperationException($"id {Firefly} already used by {clash}");
        if (!AssetDatabase.IsValidFolder(Art)) AssetDatabase.CreateFolder("Assets/Art/ProjectPA/Derived", "Insects");
        var log = new List<string>();
        var root = new GameObject("PA_Insect_Firefly");
        GameObject model;
        try
        {
            // +Z가 머리. 길쭉한 딱지날개(검정) 앞에 주황 앞가슴, 뒤 끝에 연노랑 발광 마디 두 개.
            PA_DemoIconBake.Part(root, "FireflyElytra", new Vector3(0, .04f, -.01f), new Vector3(.06f, .03f, .13f), Elytra, .8f);
            PA_DemoIconBake.Part(root, "FireflySeam", new Vector3(0, .056f, -.01f), new Vector3(.004f, .004f, .12f), new Color(.3f, .3f, .32f));
            PA_DemoIconBake.Part(root, "FireflyPronotum", new Vector3(0, .042f, .075f), new Vector3(.058f, .022f, .04f), Pronotum, .8f);
            PA_DemoIconBake.Part(root, "FireflyPronotumSpot", new Vector3(0, .054f, .078f), new Vector3(.016f, .003f, .022f), Spot);
            PA_DemoIconBake.Part(root, "FireflyHead", new Vector3(0, .035f, .1f), new Vector3(.026f, .018f, .02f), Elytra, .7f);
            PA_DemoIconBake.Part(root, "FireflyLantern", new Vector3(0, .03f, -.085f), new Vector3(.048f, .024f, .04f), Lantern, .8f);
            PA_DemoIconBake.Part(root, "FireflyLanternTip", new Vector3(0, .028f, -.11f), new Vector3(.034f, .02f, .02f), Lantern, .8f);
            for (int side = -1; side <= 1; side += 2)
            {
                string s = side < 0 ? "L" : "R";
                PA_DemoIconBake.Part(root, "FireflyAntenna" + s, new Vector3(side * .012f, .042f, .13f), new Vector3(.004f, .004f, .045f), Leg);
                root.transform.Find("FireflyAntenna" + s).localRotation = Quaternion.Euler(-20f, side * 25f, 0f);
                for (int k = 0; k < 3; k++)
                {
                    string n = $"FireflyLeg{s}{k}";
                    PA_DemoIconBake.Part(root, n, new Vector3(side * .038f, .016f, .05f - k * .045f), new Vector3(.03f, .005f, .005f), Leg);
                    root.transform.Find(n).localRotation = Quaternion.Euler(0f, side * (25f - k * 25f), side * -30f);
                }
            }
            model = PrefabUtility.SaveAsPrefabAsset(root, Art + "/PA_Insect_Firefly.prefab");
        }
        finally { Object.DestroyImmediate(root); }
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = Firefly; AssetDatabase.CreateAsset(item, path); log.Add("created " + path); }
        item.itemName = "애반딧불이"; item.basePrice = 18; item.maxStack = 20; item.category = ItemCategory.Raw; item.toolType = ToolType.None;
        item.description = "밤에 강가 풀숲 위에서 꽁무니 불빛을 깜빡이는 작은 반딧불이. 빛이 멈춘 자리를 보고 살금살금 다가가요.";
        item.model = model; item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("baked icon " + Firefly);
        AssetDatabase.SaveAssets();
        Debug.Log("[FIREFLY] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[FIREFLY] " + e); code = 1; }
        EditorApplication.Exit(code);
    }
}
