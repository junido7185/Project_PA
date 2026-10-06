using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// Bounded local-source import. No scene, registry, save-schema or original FBX material mutation.
public static class PA_FirstDayFishingSetup
{
    const string Root = "Assets/Art/ProjectPA/Derived/Fishing/Species";
    const string Evidence = "Logs/CodexOpeningDemoFinal/P4-LandFish-20261005-001";

    [MenuItem("Tools/Project PA/First Day Studio/Prepare Fishing Species")]
    public static async void Prepare()
    {
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            var tuna = Resources.Load<Item>("Items/Item_Fish");
            if (tuna == null) throw new InvalidOperationException("Existing Fish item missing.");
            var tunaModel = FirstDayStudioAssets.Load().fish;
            tuna.model = tunaModel; tuna.icon = await Icon(tunaModel, "Tuna");
            EditorUtility.SetDirty(tuna); AssetDatabase.SaveAssetIfDirty(tuna);
            await Species("RedSnapper", "도미", 3001, 20);
            await Species("YellowTang", "옐로탱", 3002, 16);
            File.WriteAllText(Evidence + "/species-setup.txt", "PASS: existing Fish id8/Tuna retained; local RedSnapper 3001, YellowTang 3002; URP materials, model icons and existing grilled-fish recipe variants. No scene/save changes.");
        }
        catch (Exception ex)
        {
            File.WriteAllText(Evidence + "/species-setup-error.txt", ex.ToString());
            Debug.LogException(ex);
        }
    }

    static async Task Species(string name, string display, int id, int price)
    {
        string path = "Assets/Art/External/Quaternius/CuteFish/Models/" + name + ".fbx";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        string prefabPath = Root + "/" + name + ".prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            var raw = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (raw == null) throw new InvalidOperationException("Missing local FBX " + path);
            var root = new GameObject("PA_Fish_" + name);
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(raw);
                model.transform.SetParent(root.transform, false);
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("Existing URP shader unavailable.");
                var converted = new Dictionary<Material, Material>();
                foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var source = materials[i];
                        if (source == null) continue;
                        if (!converted.TryGetValue(source, out var material))
                        {
                            string materialPath = Root + "/" + name + "_" + converted.Count + ".mat";
                            material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                            if (material == null)
                            {
                                var color = source.HasProperty("_Color") ? source.color : Color.white;
                                material = new Material(shader) { name = name + "_" + source.name };
                                material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .22f);
                                if (source.mainTexture != null) material.SetTexture("_BaseMap", source.mainTexture);
                                AssetDatabase.CreateAsset(material, materialPath);
                            }
                            converted.Add(source, material);
                        }
                        materials[i] = material;
                    }
                    renderer.sharedMaterials = materials;
                }
                prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        string itemPath = "Assets/Resources/Items/Item_Fish_" + name + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<Item>(itemPath);
        if (item == null)
        {
            foreach (var existing in Resources.LoadAll<Item>("Items"))
                if (existing.id == id) throw new InvalidOperationException("Item id already used: " + id);
            item = ScriptableObject.CreateInstance<Item>();
            item.id = id; item.itemName = display; item.maxStack = 99; item.basePrice = price;
            item.category = ItemCategory.Raw; item.description = "물가에서 끌어올린 " + display + ". 판매하거나 생선구이 재료로 쓸 수 있어요.";
            AssetDatabase.CreateAsset(item, itemPath);
        }
        item.model = prefab; item.icon = await Icon(prefab, name);
        EditorUtility.SetDirty(item); AssetDatabase.SaveAssetIfDirty(item);
        string recipePath = "Assets/Resources/Recipes/Recipe_GrilledFish_" + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<RecipeData>(recipePath) == null)
        {
            var recipe = UnityEngine.Object.Instantiate(Resources.Load<RecipeData>("Recipes/Recipe_GrilledFish"));
            recipe.name = "Recipe_GrilledFish_" + name; recipe.recipeName = display + " 굽기";
            recipe.ingredients = new List<RecipeIngredient> { new RecipeIngredient { item = item, count = 1 } };
            AssetDatabase.CreateAsset(recipe, recipePath);
        }
    }

    static async Task<Sprite> Icon(GameObject model, string name)
    {
        string path = Root + "/" + name + ".png";
        if (!File.Exists(path))
        {
            Texture2D preview = null;
            for (int attempt = 0; attempt < 30 && preview == null; attempt++)
            {
                preview = AssetPreview.GetAssetPreview(model);
                if (preview == null) await Task.Delay(200);
            }
            if (preview == null) throw new InvalidOperationException("Local model preview missing: " + name);
            File.WriteAllBytes(path, preview.EncodeToPNG());
        }
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
