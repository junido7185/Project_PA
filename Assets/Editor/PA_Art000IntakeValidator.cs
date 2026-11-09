using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Docs/08 §8 — ART-000 격리 입고 검사. 기존 씬/게임플레이/원본 importer는 수정하지 않는다.
public static class PA_Art000IntakeValidator
{
    [Serializable] public sealed class Manifest { public Entry[] assets; }
    [Serializable] public sealed class Entry
    {
        public string id, pack, unityPath, wrapperPath, sourceSha256, classification;
        public bool importToUnity, reuseExisting;
        public float targetHeight;
    }
    [Serializable] public sealed class Result
    {
        public string id, unityPath, wrapperPath, classification;
        public Vector3 sourceBoundsMin, sourceSize, wrapperSize, bottomCenter;
        public int vertices, triangles, renderers, materials, skinnedRenderers;
        public string[] animationClips, materialPaths;
        public bool reusedSource, referencesValid;
        public string orientation = "Source orientation retained; interaction-facing visual check pending ART-001";
    }
    [Serializable] public sealed class Report
    {
        public string ticket = "ART-000", status, unityVersion, error;
        public int materialCount;
        public Result[] assets;
        public string validationScope = "Editor preview scene and prefab round-trip; no gameplay or final art approval";
    }

    const string MaterialFolder = "Assets/Art/ProjectPA/Materials/Intake";
    static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

    [MenuItem("Project PA/Art/Validate ART-000 Intake")]
    public static void Run()
    {
        var report = new Report { unityVersion = Application.unityVersion };
        var results = new List<Result>();
        Scene preview = default;
        try
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Exit Play Mode before intake validation.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText("Docs/AssetProvenance/selected-assets.json"));
            Require(manifest != null && manifest.assets != null, "Missing selection manifest.");
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/Art/ProjectPA/Prefabs/Intake");
            Materials.Clear();
            preview = EditorSceneManager.NewPreviewScene();
            foreach (var entry in manifest.assets.Where(e => e.importToUnity))
            {
                Require(entry.wrapperPath == "Assets/Art/ProjectPA/Prefabs/Intake/" + entry.id + ".prefab", "Wrapper path escaped intake.");
                Require(entry.unityPath.StartsWith("Assets/Art/External/", StringComparison.Ordinal) ||
                    (entry.reuseExisting && entry.unityPath.StartsWith("Assets/Art/Ultimate Nature Pack - Jun 2019/", StringComparison.Ordinal)), "Unexpected model path.");
                Require(HashFile(entry.unityPath) == entry.sourceSha256, "FBX differs from audited source: " + entry.id);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(entry.unityPath);
                Require(model != null, "Model failed import: " + entry.unityPath);
                var root = new GameObject(entry.id);
                SceneManager.MoveGameObjectToScene(root, preview);
                try
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, preview);
                    visual.transform.SetParent(root.transform, false);
                    var sourceBounds = BoundsOf(root);
                    Require(sourceBounds.size.y > 0.00001f && entry.targetHeight > 0, "Invalid source height: " + entry.id);
                    float scale = entry.targetHeight / sourceBounds.size.y;
                    visual.transform.localScale *= scale;
                    var bounds = BoundsOf(root);
                    visual.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                    foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        var sourceMaterials = renderer.sharedMaterials;
                        Require(sourceMaterials.Length > 0 && sourceMaterials.All(m => m != null), "Missing imported material: " + entry.id);
                        renderer.sharedMaterials = sourceMaterials.Select(m => GetMaterial(m, entry)).ToArray();
                    }
                    if (File.Exists(entry.wrapperPath))
                    {
                        // A repeat run is read-only for existing wrapper assets.
                        Require(AssetDatabase.LoadAssetAtPath<GameObject>(entry.wrapperPath) != null, "Existing wrapper is not a prefab.");
                    }
                    else
                        Require(PrefabUtility.SaveAsPrefabAsset(root, entry.wrapperPath) != null, "Prefab save failed: " + entry.id);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }

                var loaded = PrefabUtility.LoadPrefabContents(entry.wrapperPath);
                try
                {
                    var renderers = loaded.GetComponentsInChildren<Renderer>(true);
                    var bounds = BoundsOf(loaded);
                    var meshes = loaded.GetComponentsInChildren<MeshFilter>(true).Select(m => m.sharedMesh)
                        .Concat(loaded.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(m => m.sharedMesh)).ToArray();
                    Require(meshes.Length > 0 && meshes.All(m => m != null && m.vertexCount > 0), "Empty mesh: " + entry.id);
                    Require(Mathf.Abs(bounds.min.y) < 0.005f && Mathf.Abs(bounds.center.x) < 0.005f && Mathf.Abs(bounds.center.z) < 0.005f,
                        "Wrapper pivot is not bottom centered: " + entry.id);
                    Require(Mathf.Abs(bounds.size.y - entry.targetHeight) < 0.005f, "Wrapper scale mismatch: " + entry.id);
                    foreach (var tr in loaded.GetComponentsInChildren<Transform>(true))
                        Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject) == 0, "Missing script: " + entry.id);
                    var materials = renderers.SelectMany(r => r.sharedMaterials).Distinct().ToArray();
                    Require(materials.All(m => m != null && m.shader != null && m.shader.name == "Universal Render Pipeline/Lit"), "Invalid wrapper shader: " + entry.id);
                    foreach (string dependency in AssetDatabase.GetDependencies(entry.wrapperPath))
                        Require(File.Exists(dependency), "Missing prefab dependency: " + dependency);
                    var original = BoundsOf(model);
                    results.Add(new Result {
                        id = entry.id, unityPath = entry.unityPath, wrapperPath = entry.wrapperPath,
                        classification = entry.classification, sourceBoundsMin = original.min, sourceSize = original.size,
                        wrapperSize = bounds.size, bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z),
                        vertices = meshes.Sum(m => m.vertexCount), triangles = meshes.Sum(TriangleCount), renderers = renderers.Length,
                        materials = materials.Length, skinnedRenderers = loaded.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length,
                        animationClips = AssetDatabase.LoadAllAssetsAtPath(entry.unityPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).Select(c => c.name).ToArray(),
                        materialPaths = materials.Select(AssetDatabase.GetAssetPath).ToArray(),
                        reusedSource = entry.reuseExisting, referencesValid = true
                    });
                }
                finally { PrefabUtility.UnloadPrefabContents(loaded); }
                Debug.Log("ART000_ASSET_PASS " + entry.id);
            }
            Require(results.Count == manifest.assets.Count(e => e.importToUnity), "Selection count mismatch.");
            report.status = "PASS";
        }
        catch (Exception exception)
        {
            report.status = "FAIL";
            report.error = exception.ToString();
            Debug.LogException(exception);
        }
        finally
        {
            if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            report.assets = results.ToArray();
            report.materialCount = Materials.Count;
            File.WriteAllText("Docs/AssetProvenance/unity-validation.json", JsonUtility.ToJson(report, true), Encoding.UTF8);
        }
        Debug.Log("ART000_INTAKE_" + report.status + " assets=" + results.Count);
        if (Application.isBatchMode) EditorApplication.Exit(report.status == "PASS" ? 0 : 1);
    }

    static Material GetMaterial(Material source, Entry entry)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        Require(shader != null, "URP/Lit not available.");
        Color color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") :
            source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
        Texture texture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : null;
        if (texture == null && source.HasProperty("_MainTex")) texture = source.GetTexture("_MainTex");
        if (entry.pack == "MiniMarket" || entry.pack == "FoodKit")
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.GetDirectoryName(entry.unityPath).Replace('\\', '/') + "/Textures/colormap.png");
        if (entry.pack == "CubeWorldKit")
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.GetDirectoryName(entry.unityPath).Replace('\\', '/') + "/Textures/Atlas.png");
        if (entry.pack == "MiniMarket" || entry.pack == "FoodKit" || entry.pack == "CubeWorldKit")
            Require(texture != null, "Missing audited palette/atlas: " + entry.id);
        string textureGuid = texture != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(texture)) : "none";
        string key = ColorUtility.ToHtmlStringRGBA(color) + "_" + textureGuid;
        if (Materials.TryGetValue(key, out var found)) return found;
        string path = MaterialFolder + "/PA_Source_" + key + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = "PA_Source_" + key };
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.35f);
            material.SetFloat("_Cull", 0f);
            AssetDatabase.CreateAsset(material, path);
        }
        Materials.Add(key, material);
        return material;
    }

    static Bounds BoundsOf(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>(true);
        Require(renderers.Length > 0, "No renderer: " + obj.name);
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        Require(float.IsFinite(bounds.size.x) && float.IsFinite(bounds.size.y) && float.IsFinite(bounds.size.z), "Non-finite bounds.");
        return bounds;
    }

    static int TriangleCount(Mesh mesh)
    {
        int count = 0;
        for (int i = 0; i < mesh.subMeshCount; i++) count += (int)mesh.GetIndexCount(i) / 3;
        return count;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static string HashFile(string path)
    {
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
