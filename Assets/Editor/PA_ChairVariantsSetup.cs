using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-11-09 꾸밈 색 변형(너굴 리스트 연구 H4 "같은 가구 2~3색을 작업대 '칠하기'로"): 저장 형식을 바꾸지 않도록 색마다 다른 아이템·배치 키로 둔다
// (아이템 인스턴스 메타 없음 — 기존 저장은 그대로, 새 아이템은 추가만).
// 칠하는 재료는 섬에서 모은 것: 산호 조각 → 산호빛 의자, 해변 유리 조각 → 바다빛 의자(모은 것 → 꾸밈). 손님 쉼터 의자로도 센다(DemoSettlementController.ChairKeys).
// 배치물은 기존 의자 배치물(SimpleFurniture)을 복사해 의자 모델만 바꾼다(충돌체·구성 그대로). 카탈로그는 PA_DemoStructureSetup 재생성 때 ExistingEntries()로 보존.
// 배치: Unity -batchmode -executeMethod PA_ChairVariantsSetup.BatchApply
public static class PA_ChairVariantsSetup
{
    const string Items = "Assets/Resources/Items";
    const string Structure = "Assets/Resources/DemoStructure";
    const string Art = "Assets/Art/ProjectPA/Derived/ItemPresentation/Models";

    sealed class Variant
    {
        public int Id; public string Key, Asset, Name, Resource, Recipe, Dye, Description; public Color Cushion, Pillow;
    }

    static readonly Variant[] Variants =
    {
        new Variant { Id = 2058, Key = "furniture-chair-coral", Asset = "Item_ChairCoral", Name = "산호빛 의자", Resource = "SimpleFurniture_Coral",
            Recipe = "Recipe_ChairCoral", Dye = "Items/Item_CoralPiece", Cushion = new Color(.93f, .56f, .52f), Pillow = new Color(.99f, .88f, .82f),
            Description = "나무 의자 쿠션을 산호 조각 빛으로 물들였어요. 가게에 두면 손님이 앉아 쉬어요." },
        new Variant { Id = 2059, Key = "furniture-chair-sea", Asset = "Item_ChairSea", Name = "바다빛 의자", Resource = "SimpleFurniture_Sea",
            Recipe = "Recipe_ChairSea", Dye = "Items/Item_BeachGlass", Cushion = new Color(.36f, .62f, .8f), Pillow = new Color(.87f, .94f, .97f),
            Description = "나무 의자 쿠션을 해변 유리 조각처럼 맑은 바다빛으로 물들였어요. 가게에 두면 손님이 앉아 쉬어요." },
    };

    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        var catalog = AssetDatabase.LoadAssetAtPath<DemoPlaceableCatalog>(Structure + "/Catalog.asset") ?? throw new InvalidOperationException("Missing Catalog.asset");
        var baseEntry = catalog.entries.FirstOrDefault(e => e != null && e.key == "furniture-chair") ?? throw new InvalidOperationException("Missing furniture-chair entry");
        var baseChair = AssetDatabase.LoadAssetAtPath<Item>(Structure + "/Item_SimpleFurniture.asset") ?? throw new InvalidOperationException("Missing chair item");
        foreach (var v in Variants)
        {
            string itemPath = Items + "/" + v.Asset + ".asset";
            var clash = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => p != itemPath && AssetDatabase.LoadAssetAtPath<Item>(p)?.id == v.Id);
            if (clash != null) throw new InvalidOperationException($"id {v.Id} already used by {clash}");
            var dye = Resources.Load<Item>(v.Dye) ?? throw new InvalidOperationException("Missing dye " + v.Dye);
            var model = PA_OwnedChairSetup.BuildModel("PA_Decor_CozyChair_" + v.Resource.Split('_')[1], Art + "/PA_Decor_CozyChair_" + v.Resource.Split('_')[1] + ".prefab", v.Cushion, v.Pillow);

            // 배치물: 기존 의자 배치물을 복사해 의자 모델만 바꾼다.
            string prefabPath = Structure + "/" + v.Resource + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null && !AssetDatabase.CopyAsset(Structure + "/SimpleFurniture.prefab", prefabPath))
                throw new InvalidOperationException("Copy failed " + prefabPath);
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                prefab.name = v.Resource;
                var old = prefab.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "CozyChair");
                Vector3 at = Vector3.zero; float yaw = 0f; Transform parent = prefab.transform;
                if (old != null) { parent = old.parent; at = old.position; yaw = old.eulerAngles.y; Object.DestroyImmediate(old.gameObject); }
                var chair = FirstDayStudioAssets.Place(model, parent, at, 1.1f, yaw);
                chair.name = "CozyChair";
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            string dataPath = Structure + "/" + v.Resource + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(dataPath);
            if (data == null) { data = ScriptableObject.CreateInstance<BuildingData>(); AssetDatabase.CreateAsset(data, dataPath); }
            data.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath); data.buildingName = v.Resource; data.price = 0;
            EditorUtility.SetDirty(data);

            var item = AssetDatabase.LoadAssetAtPath<Item>(itemPath);
            if (item == null) { item = ScriptableObject.CreateInstance<Item>(); item.id = v.Id; AssetDatabase.CreateAsset(item, itemPath); log.Add("created " + itemPath); }
            item.itemName = v.Name; item.description = v.Description; item.basePrice = baseChair.basePrice; item.maxStack = baseChair.maxStack;
            item.category = baseChair.category; item.toolType = ToolType.None; item.model = model;
            item.icon = PA_DemoIconBake.BakeIcon(item, model);
            EditorUtility.SetDirty(item);

            Recipe(v.Recipe, v.Name, item, (baseChair, 1), (dye, 1));
            log.Add($"{v.Id} {v.Key} recipe {v.Recipe}");
        }
        var entries = catalog.entries.Where(e => e != null && Variants.All(v => v.Key != e.key)).ToList();
        entries.AddRange(ExistingEntries());
        catalog.entries = entries.ToArray();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log("[CHAIR-VARIANTS] " + string.Join(" | ", log));
    }

    public static IEnumerable<DemoPlaceableEntry> ExistingEntries()
    {
        foreach (var v in Variants)
        {
            var item = AssetDatabase.LoadAssetAtPath<Item>(Items + "/" + v.Asset + ".asset");
            if (item == null || AssetDatabase.LoadAssetAtPath<GameObject>(Structure + "/" + v.Resource + ".prefab") == null) continue;
            yield return new DemoPlaceableEntry
            {
                item = item, key = v.Key, kind = DemoPlaceableKind.Furniture, buildingResource = "DemoStructure/" + v.Resource,
                size = Vector2Int.one, entrance = Vector2Int.down,
                surfaces = WorldPlaceableSurface.Living | WorldPlaceableSurface.Utility | WorldPlaceableSurface.ShopFloor, canMove = true,
            };
        }
    }

    static void Recipe(string asset, string name, Item output, params (Item item, int count)[] ingredients)
    {
        string path = Structure + "/" + asset + ".asset";
        var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
        if (recipe == null)
        {
            if (File.Exists(path)) throw new InvalidOperationException(path + " exists with an unexpected type.");
            recipe = ScriptableObject.CreateInstance<RecipeData>();
            AssetDatabase.CreateAsset(recipe, path);
        }
        recipe.recipeName = name; recipe.requiredWorkbench = WorkbenchType.BasicWorkbench; recipe.requiredTier = 0;
        recipe.outputItem = output; recipe.outputCount = 1; recipe.icon = output.icon;
        recipe.ingredients = ingredients.Select(i => new RecipeIngredient { item = i.item, count = i.count }).ToList();
        EditorUtility.SetDirty(recipe);
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[CHAIR-VARIANTS] " + e); code = 1; }
        EditorApplication.Exit(code);
    }
}
