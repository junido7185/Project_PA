#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Visual Demo Integration Pass v3 — 데모 소품 베이커.
//
// 목적: Nature Pack / 프로젝트 내부 실모델은 Resources 밖이라 런타임 드레싱이 로드할 수
// 없다. 이 툴이 실모델을 감싼 **신규 데모 프리팹**을 Assets/Resources/PA_DemoProps/ 에
// 구워 두면, DemoVisualDressingController 가 primitive 대신 실모델을 배치할 수 있다.
//
// 안전 규칙:
// - 원본 FBX/프리팹/meta 는 절대 수정하지 않는다 (읽기 + 인스턴스화만).
// - 머티리얼이 URP 가 아니면(빌트인 셰이더 → 마젠타 위험) 색/텍스처를 복사한
//   URP Lit 머티리얼을 새로 만들어 데모 프리팹에만 할당한다.
// - 장식 전용이므로 콜라이더는 전부 제거한다 (NavMesh/이동 무영향).
public static class PA_DemoPropBaker
{
    const string OutputDir = "Assets/Resources/PA_DemoProps";
    const string MaterialDir = "Assets/Resources/PA_DemoProps/Materials";
    const string NaturePackDir = "Assets/Art/Ultimate Nature Pack - Jun 2019/FBX";

    static readonly (string propName, string assetPath)[] Sources =
    {
        ("Prop_TreeA",       NaturePackDir + "/CommonTree_1.fbx"),
        ("Prop_TreeB",       NaturePackDir + "/CommonTree_4.fbx"),
        ("Prop_TreeBirch",   NaturePackDir + "/BirchTree_1.fbx"),
        ("Prop_Bush",        NaturePackDir + "/Bush_1.fbx"),
        ("Prop_BushBerries", NaturePackDir + "/BushBerries_1.fbx"),
        ("Prop_Flowers",     NaturePackDir + "/Flowers.fbx"),
        ("Prop_Grass",       NaturePackDir + "/Grass_2.fbx"),
        ("Prop_Rock",        NaturePackDir + "/Rock_Moss_2.fbx"),
        ("Prop_Stump",       NaturePackDir + "/TreeStump_Moss.fbx"),
        ("Prop_WoodLog",     NaturePackDir + "/WoodLog_Moss.fbx"),
        ("Prop_Wheat",       NaturePackDir + "/Wheat.fbx"),
        ("Prop_FroggyChair", "Assets/Prefabs/FroggyChair.prefab"),
    };

    [MenuItem("Project PA/Demo/Bake Demo Props")]
    public static void BakeDemoProps()
    {
        Directory.CreateDirectory(OutputDir);
        Directory.CreateDirectory(MaterialDir);

        int baked = 0;
        var failures = new List<string>();

        foreach (var (propName, assetPath) in Sources)
        {
            try
            {
                if (BakeOne(propName, assetPath)) baked++;
                else failures.Add($"{propName} ({assetPath}) — source not found");
            }
            catch (System.Exception ex)
            {
                failures.Add($"{propName} — {ex.Message}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"PA DemoPropBaker: baked={baked}, failures={failures.Count}");
        foreach (var f in failures) Debug.LogWarning($"PA DemoPropBaker failure: {f}");

        if (Application.isBatchMode)
            EditorApplication.Exit(failures.Count == 0 ? 0 : 1);
    }

    static bool BakeOne(string propName, string assetPath)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (source == null) return false;

        var root = new GameObject(propName);
        try
        {
            var child = (GameObject)PrefabUtility.InstantiatePrefab(source);
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = Vector3.zero;

            // 장식 전용 — 콜라이더 제거.
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);

            // 게임플레이 스크립트가 딸려 있으면 제거 (FroggyChair 등 프로젝트 프리팹 대비).
            foreach (var mono in root.GetComponentsInChildren<MonoBehaviour>(true))
                Object.DestroyImmediate(mono);

            RemapMaterialsToUrp(root);

            string prefabPath = $"{OutputDir}/{propName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"PA DemoPropBaker: saved {prefabPath}");
            return true;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // 빌트인 셰이더 머티리얼 → 색/텍스처를 복사한 URP Lit 사본으로 교체 (원본 불변).
    static void RemapMaterialsToUrp(GameObject root)
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        var cache = new Dictionary<Material, Material>();

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                if (mat == null || mat.shader == null) continue;
                if (mat.shader.name.Contains("Universal Render Pipeline")) continue;

                if (!cache.TryGetValue(mat, out var replacement))
                {
                    replacement = CreateUrpCopy(mat, urpLit);
                    cache[mat] = replacement;
                }

                materials[i] = replacement;
                changed = true;
            }

            if (changed) renderer.sharedMaterials = materials;
        }
    }

    static Material CreateUrpCopy(Material original, Shader urpLit)
    {
        string matPath = $"{MaterialDir}/{Sanitize(original.name)}_URP.mat";

        var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null) return existing;

        var copy = new Material(urpLit);

        Color color = Color.white;
        if (original.HasProperty("_BaseColor")) color = original.GetColor("_BaseColor");
        else if (original.HasProperty("_Color")) color = original.GetColor("_Color");
        copy.color = color;

        Texture mainTex = null;
        if (original.HasProperty("_BaseMap")) mainTex = original.GetTexture("_BaseMap");
        else if (original.HasProperty("_MainTex")) mainTex = original.GetTexture("_MainTex");
        if (mainTex != null) copy.SetTexture("_BaseMap", mainTex);

        // 로우폴리 식생 특유의 플랫한 느낌 유지 — 스무스니스 낮춤.
        if (copy.HasProperty("_Smoothness")) copy.SetFloat("_Smoothness", 0.1f);

        AssetDatabase.CreateAsset(copy, matPath);
        return copy;
    }

    static string Sanitize(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Replace(" ", "_");
    }
}
#endif
