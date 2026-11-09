using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-11-09 소유 의자로 교체: 07 큐 결정("라이선스 문서가 없는 Froggy Chair — 플레이어 실행 경로 참조 0, 원본은 삭제하지 않음")이
// 의자 배치물(SimpleFurniture)과 가게 실내(PioneerShopBase 책상 의자)에 다시 들어와 있었다. 원작 소품 복제 금지(너굴 리스트 연구 경계)와도 어긋난다.
// 소유 로우폴리 나무 의자(쿠션·등받이 살·둥근 윗막대)를 만들고, 두 자리의 Froggy 표현을 떼어 같은 자리·같은 높이에 놓는다. 원본 FBX·프리팹은 그대로 둔다.
// 배치: Unity -batchmode -executeMethod PA_OwnedChairSetup.BatchApply
public static class PA_OwnedChairSetup
{
    const string Art = "Assets/Art/ProjectPA/Derived/ItemPresentation/Models";
    const string Structure = "Assets/Resources/DemoStructure";
    public const string ModelPath = Art + "/PA_Decor_CozyChair.prefab";

    static readonly Color Wood = new Color(.58f, .38f, .2f), DarkWood = new Color(.42f, .27f, .14f), LightWood = new Color(.7f, .5f, .3f);
    static readonly Color Cushion = new Color(.36f, .55f, .45f), Pillow = new Color(.93f, .87f, .72f);

    // 있으면 불러오고, 없으면 만든다(PA_DemoStructureSetup 전체 재생성도 이 모델을 쓴다).
    public static GameObject Model()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        return existing != null ? existing : BuildModel();
    }

    static GameObject BuildModel() => BuildModel("PA_Decor_CozyChair", ModelPath, Cushion, Pillow);

    // 색 변형(PA_ChairVariantsSetup)도 같은 모양에 쿠션 색만 바꿔 만든다(부품 메시는 루트 이름별로 따로 저장).
    internal static GameObject BuildModel(string rootName, string path, Color cushion, Color pillow)
    {
        var root = new GameObject(rootName);
        try
        {
            PA_DemoIconBake.Part(root, "ChairSeat", new Vector3(0, .45f, 0), new Vector3(.56f, .08f, .52f), Wood, .95f);
            PA_DemoIconBake.Part(root, "ChairCushion", new Vector3(0, .51f, -.01f), new Vector3(.5f, .06f, .46f), cushion, .85f);
            foreach (int x in new[] { -1, 1 })
            foreach (int z in new[] { -1, 1 })
                PA_DemoIconBake.Part(root, $"ChairLeg{(x < 0 ? "L" : "R")}{(z < 0 ? "F" : "B")}", new Vector3(x * .23f, .21f, z * .2f), new Vector3(.06f, .42f, .06f), DarkWood);
            foreach (int x in new[] { -1, 1 })
                PA_DemoIconBake.Part(root, "ChairUpright" + (x < 0 ? "L" : "R"), new Vector3(x * .23f, .78f, .22f), new Vector3(.06f, .62f, .06f), Wood);
            PA_DemoIconBake.Part(root, "ChairTopRail", new Vector3(0, 1.06f, .22f), new Vector3(.56f, .12f, .07f), Wood, .85f);
            for (int i = -1; i <= 1; i++)
                PA_DemoIconBake.Part(root, "ChairSlat" + (i + 1), new Vector3(i * .11f, .78f, .225f), new Vector3(.05f, .42f, .04f), LightWood);
            PA_DemoIconBake.Part(root, "ChairBackPillow", new Vector3(0, .82f, .18f), new Vector3(.38f, .2f, .05f), pillow, .9f);
            // 손님 앉기(CustomerArrivalController.SeatOf)가 읽는 앉는 점: 좌석 가운데, 정면(-Z)을 본다. Froggy 원본 프리팹의 Sitpoint와 같은 역할.
            var sit = new GameObject("Sitpoint");
            sit.transform.SetParent(root.transform, false);
            sit.transform.localPosition = new Vector3(0, .54f, -.02f);
            sit.transform.localRotation = Quaternion.LookRotation(Vector3.back);
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { Object.DestroyImmediate(root); }
    }

    // 이름에 Froggy가 든 가장 바깥 표현(프리팹 복제 루트)을 떼고, 그 자리·방향·높이에 소유 의자를 놓는다.
    static int Replace(GameObject prefabRoot, GameObject model, float height)
    {
        // Froggy 표현 또는 이전에 놓은 소유 의자(복제본이라 모델 변경이 따라오지 않음 — 다시 놓는다).
        var roots = prefabRoot.GetComponentsInChildren<Transform>(true)
            .Where(t => (t.name.Contains("Froggy") && (t.parent == null || !t.parent.name.Contains("Froggy"))) || t.name == "CozyChair").ToList();
        // 원래 놓을 때 쓴 축 변환(소품 프리팹 루트 회전)을 빼야 놓을 때의 yaw가 나온다.
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(FroggyKeptPath) ?? AssetDatabase.LoadAssetAtPath<GameObject>(FroggyResourcesPath);
        Quaternion authored = source != null ? source.transform.localRotation : Quaternion.identity;
        foreach (var froggy in roots)
        {
            Transform parent = froggy.parent;
            var renderers = froggy.GetComponentsInChildren<Renderer>(true);
            Vector3 at = froggy.position;
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                at = new Vector3(b.center.x, b.min.y, b.center.z); // 바닥 높이·가운데 그대로
            }
            float yaw = (froggy.rotation * Quaternion.Inverse(froggy.name == "CozyChair" ? Quaternion.identity : authored)).eulerAngles.y;
            Object.DestroyImmediate(froggy.gameObject);
            var chair = FirstDayStudioAssets.Place(model, parent, at, height, yaw);
            chair.name = "CozyChair";
        }
        return roots.Count;
    }

    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        var model = BuildModel();
        log.Add("model " + ModelPath);
        foreach (var name in new[] { "SimpleFurniture", "PioneerShopBase" })
        {
            string path = Structure + "/" + name + ".prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int n = Replace(prefab, model, 1.1f);
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                log.Add($"{name}: Froggy {n} → CozyChair");
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        var item = AssetDatabase.LoadAssetAtPath<Item>(Structure + "/Item_SimpleFurniture.asset") ?? throw new InvalidOperationException("Missing Item_SimpleFurniture");
        item.model = model;
        item.icon = PA_DemoIconBake.BakeIcon(item, model);
        EditorUtility.SetDirty(item);
        log.Add("chair item model/icon " + item.id);
        AssetDatabase.SaveAssets();
        Debug.Log("[OWNED-CHAIR] " + string.Join(" | ", log));
    }

    // Froggy 소품 프리팹을 Resources 밖(원본 폴더)으로 옮긴다: 지우지 않고(07 큐 결정 "원본 보존") 빌드 데이터에서만 빠지게. GUID 그대로.
    const string FroggyResourcesPath = "Assets/Resources/PA_DemoProps/Prop_FroggyChair.prefab";
    const string FroggyKeptPath = "Assets/Art/animal-crossing-froggy-chair/Prop_FroggyChair.prefab";
    public static void MoveFroggyOutOfResourcesBatch()
    {
        int code = 0;
        try
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(FroggyResourcesPath) != null)
            {
                string error = AssetDatabase.MoveAsset(FroggyResourcesPath, FroggyKeptPath);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[OWNED-CHAIR] Froggy prop kept at " + FroggyKeptPath + " (out of Resources): " + (AssetDatabase.LoadAssetAtPath<GameObject>(FroggyKeptPath) != null));
        }
        catch (Exception e) { Debug.LogError("[OWNED-CHAIR] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[OWNED-CHAIR] " + e); code = 1; }
        EditorApplication.Exit(code);
    }
}
