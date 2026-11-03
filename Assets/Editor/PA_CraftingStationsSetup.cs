using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-11-02 사용자 요청 "크래프팅 요소 세분화·콘텐츠 설계 및 제작" (PA_CONTENT_DEPTH_20261026_V1 후속).
// 딩컴처럼 용도별 작업대를 나눈다: 현장 작업대(목공·설비 키트) → 조리대(요리) · 화로(제련·도구).
// 모든 산출물은 판매·설치·다른 레시피 재료·도구 교체 중 하나 이상의 쓰임을 가진다(Docs/01_GAME_DESIGN/Content/CRAFTING_STATIONS.md).
// 10/26 Content-kitchen-builder 부분 결과(PA_FieldKitchenSetup, 세션 한도로 중단)의 검증 규칙을 인수해 일반화했다:
// 기존 자산·id는 지우거나 이름을 바꾸지 않고, 충돌은 쓰기 전에 모두 검사해 실패하며, 없는 자산만 만든다.
// 레시피는 Resources/Recipes 밖(DemoStructure)에 둔다(Basic 레시피 수를 세는 기존 검증기 보존). 씬·저장 스키마 변경 없음.
// 배치: Unity -batchmode -executeMethod PA_CraftingStationsSetup.BatchApply
public static class PA_CraftingStationsSetup
{
    const string Root = "Assets/Resources/DemoStructure";
    const string Items = "Assets/Resources/Items";

    sealed class Station
    {
        public string Key, Building, KitAsset, RecipeAsset, KitName, DisplayName, ModelPath, Description;
        public int KitId; public float Height; public WorkbenchType Type;
        public (string resource, int id, int count)[] Ingredients;
    }

    static readonly Station[] Stations =
    {
        new Station { Key = "field-kitchen", Building = "FieldKitchen", KitAsset = "Item_FieldKitchenKit", RecipeAsset = "Recipe_FieldKitchenKit",
            KitId = 2023, KitName = "조리대 키트", DisplayName = "조리대", Type = WorkbenchType.Kitchen, Height = 0f,
            ModelPath = PA_StationArt.KitchenPath,
            Description = "판재와 광석으로 짠 조리대. 정착지에 놓고 [E]로 잡은 생선을 구워요.",
            Ingredients = new[] { ("Items/Item_Plank", 7, 3), ("Items/Item_Ore", 4, 2) } },
        new Station { Key = "field-forge", Building = "FieldForge", KitAsset = "Item_FieldForgeKit", RecipeAsset = "Recipe_FieldForgeKit",
            KitId = 2024, KitName = "화로 키트", DisplayName = "화로", Type = WorkbenchType.Forge, Height = 0f,
            ModelPath = PA_StationArt.ForgePath,
            Description = "광석을 녹이는 작은 화로. 정착지에 놓고 [E]로 철을 다루고 도구를 만들어요.",
            Ingredients = new[] { ("Items/Item_Plank", 7, 2), ("Items/Item_Ore", 4, 4) } },
    };

    sealed class Product
    {
        public string Asset, Recipe, Name, Description, ModelKey;
        public int Id, Price, MaxStack; public ItemCategory Category; public ToolType Tool;
        public WorkbenchType Bench;
        public (string resource, int id, int count)[] Ingredients;
    }

    // 화로의 철 주괴는 기존 Recipe_IronBar(Ore2, Forge, tier 0)를 그대로 쓴다. 철제 도구 세트(tier 1)는 성장 신호로 잠금 표시된다.
    static readonly Product[] Products =
    {
        new Product { Asset = "Item_ButterflyFrame", Recipe = "Recipe_ButterflyFrame", Id = 2025, Name = "나비 표본 액자",
            Description = "잡은 나비를 판재 액자에 담았어요. 관광객이 좋아하는 공예품이에요.",
            Price = 48, MaxStack = 8, Category = ItemCategory.Luxury, Tool = ToolType.None, Bench = WorkbenchType.BasicWorkbench, ModelKey = "frame",
            Ingredients = new[] { ("Items/Item_Plank", 7, 1), ("Items/Item_Butterfly", 2004, 1) } },
        new Product { Asset = "Item_IronAxe", Recipe = "Recipe_IronAxe", Id = 2026, Name = "철 도끼",
            Description = "화로에서 벼린 도끼. 스타터 도끼보다 세 배 오래 써요.",
            Price = 0, MaxStack = 1, Category = ItemCategory.Tool, Tool = ToolType.Axe, Bench = WorkbenchType.Forge, ModelKey = "axe",
            Ingredients = new[] { ("Items/Item_IronBar", 6, 1), ("Items/Item_Wood", 3, 2) } },
        new Product { Asset = "Item_SturdyRod", Recipe = "Recipe_SturdyRod", Id = 2027, Name = "튼튼한 낚싯대",
            Description = "쇠 고리를 단 낚싯대. 물고기를 훨씬 많이 끌어올릴 수 있어요.",
            Price = 0, MaxStack = 1, Category = ItemCategory.Tool, Tool = ToolType.FishingRod, Bench = WorkbenchType.Forge, ModelKey = "rod",
            Ingredients = new[] { ("Items/Item_IronBar", 6, 1), ("Items/Item_Plank", 7, 1) } },
        // 2026-11-03 조리대 메뉴 확장(데모에서 얻는 재료만): 물고기 종류를 모을 이유(꼬치)와 나무를 땔감으로 쓰는 가공(훈제).
        new Product { Asset = "Item_FishSkewer", Recipe = "Recipe_FishSkewer", Id = 2032, Name = "모둠 생선 꼬치",
            Description = "참치·도미·옐로탱을 한 꼬치에 구웠어요. 구이 세 개보다 비싸고, 관광객이 특히 좋아해요.",
            Price = 175, MaxStack = 8, Category = ItemCategory.Luxury, Tool = ToolType.None, Bench = WorkbenchType.Kitchen, ModelKey = "skewer",
            Ingredients = new[] { ("Items/Item_Fish", 8, 1), ("Items/Item_Fish_RedSnapper", 3001, 1), ("Items/Item_Fish_YellowTang", 3002, 1), ("Items/Item_Wood", 3, 1) } },
        new Product { Asset = "Item_SmokedFish", Recipe = "Recipe_SmokedFish", Id = 2033, Name = "훈제 생선",
            Description = "나무 연기로 천천히 훈제한 참치. 생선구이보다 조금 더 받고, 나무가 남을 때 좋아요.",
            Price = 68, MaxStack = 12, Category = ItemCategory.Processed, Tool = ToolType.None, Bench = WorkbenchType.Kitchen, ModelKey = "smoked",
            Ingredients = new[] { ("Items/Item_Fish", 8, 1), ("Items/Item_Wood", 3, 2) } },
    };

    [MenuItem("Project PA/Content/Crafting Stations (Kitchen · Forge · Products)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PA_StationArt.KitchenPath) == null) PA_StationArt.BuildAll();
        var catalog = AssetDatabase.LoadAssetAtPath<DemoPlaceableCatalog>(Root + "/Catalog.asset");
        if (catalog == null) throw new InvalidOperationException("Missing " + Root + "/Catalog.asset; run PA_DemoStructureSetup.Prepare first.");
        var log = new List<string>();
        Preflight(catalog);

        var entries = (catalog.entries ?? Array.Empty<DemoPlaceableEntry>()).ToList();
        foreach (var station in Stations)
        {
            var entry = BuildStation(station, log);
            if (entries.Any(e => e != null && e.key == station.Key)) log.Add($"kept catalog entry '{station.Key}'");
            else { entries.Add(entry); log.Add($"appended catalog entry '{station.Key}'"); }
        }
        catalog.entries = entries.ToArray();
        EditorUtility.SetDirty(catalog);

        foreach (var product in Products) BuildProduct(product, log);
        ApplyPlayerCopy(log);
        AssetDatabase.SaveAssets();
        Debug.Log("[CRAFTING-STATIONS] " + string.Join(" | ", log) + ". No scene, save or Resources/Recipes change.");
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[CRAFTING-STATIONS] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    // PA_DemoStructureSetup.Prepare가 entries를 통째로 다시 쓸 때 이미 만든 작업대 항목을 잃지 않도록 같은 정의를 돌려준다.
    public static IEnumerable<DemoPlaceableEntry> ExistingEntries()
    {
        foreach (var station in Stations)
        {
            var kit = AssetDatabase.LoadAssetAtPath<Item>(Root + "/" + station.KitAsset + ".asset");
            if (kit != null && AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + station.Building + ".prefab") != null)
                yield return Entry(station, kit);
        }
    }

    static DemoPlaceableEntry Entry(Station station, Item kit) => new DemoPlaceableEntry
    {
        item = kit, key = station.Key, kind = DemoPlaceableKind.Furniture, buildingResource = "DemoStructure/" + station.Building,
        // 로우폴리 조리대·화로(약 2.6m×1m)는 2×1칸. Furniture 종류는 OnPlaced 분기·1개 제한이 없고, DemoPlacedObject가 E를 자식 Workbench로 넘긴다.
        size = new Vector2Int(2, 1), surfaces = WorldPlaceableSurface.Settlement | WorldPlaceableSurface.Utility | WorldPlaceableSurface.Living,
    };

    // ---------- 쓰기 전 검사 ----------
    static void Preflight(DemoPlaceableCatalog catalog)
    {
        var owned = new Dictionary<int, string>();
        foreach (var s in Stations) owned[s.KitId] = Root + "/" + s.KitAsset + ".asset";
        foreach (var p in Products) owned[p.Id] = Items + "/" + p.Asset + ".asset";
        var names = Stations.Select(s => s.KitName).Concat(Products.Select(p => p.Name)).ToHashSet();
        foreach (string guid in AssetDatabase.FindAssets("t:Item"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var other in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Item>())
            {
                if (owned.TryGetValue(other.id, out string mine) && path != mine)
                    throw new InvalidOperationException($"Item id {other.id} already used by {path}.");
                if (names.Contains(other.itemName) && !owned.Values.Contains(path))
                    throw new InvalidOperationException($"Item name '{other.itemName}' already used by {path}.");
            }
        }
        foreach (var s in Stations)
        {
            var keyed = catalog.entries?.FirstOrDefault(e => e != null && e.key == s.Key);
            if (keyed != null && (keyed.item == null || AssetDatabase.GetAssetPath(keyed.item) != Root + "/" + s.KitAsset + ".asset"))
                throw new InvalidOperationException($"Catalog key '{s.Key}' already points to {AssetDatabase.GetAssetPath(keyed?.item)}.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(s.ModelPath) == null) throw new InvalidOperationException("Missing local model: " + s.ModelPath);
            var prefab = Existing<GameObject>(Root + "/" + s.Building + ".prefab");
            if (prefab != null && (prefab.GetComponent<Workbench>()?.workbenchType != s.Type ||
                prefab.transform.Find("StationModel") == null && prefab.transform.Find("Visual") == null))
                throw new InvalidOperationException(s.Building + ".prefab exists but is not a " + s.Type + " Workbench with a station model child.");
            Existing<Item>(Root + "/" + s.KitAsset + ".asset"); Existing<RecipeData>(Root + "/" + s.RecipeAsset + ".asset");
            Existing<BuildingData>(Root + "/" + s.Building + ".asset");
        }
        foreach (var p in Products) { Existing<Item>(Items + "/" + p.Asset + ".asset"); Existing<RecipeData>(Root + "/" + p.Recipe + ".asset"); }
        var ironBar = Resources.LoadAll<RecipeData>("Recipes").FirstOrDefault(r => r.outputItem != null && r.outputItem.id == 6);
        if (ironBar == null || ironBar.requiredWorkbench != WorkbenchType.Forge || ironBar.requiredTier != 0)
            throw new InvalidOperationException("Expected the existing tier-0 Forge Recipe_IronBar (Ore → IronBar).");
    }

    // ---------- 작업대 ----------
    static DemoPlaceableEntry BuildStation(Station s, List<string> log)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(s.ModelPath);
        string prefabPath = Root + "/" + s.Building + ".prefab", dataPath = Root + "/" + s.Building + ".asset";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            var root = new GameObject(s.Building);
            try
            {
                // 로우폴리 작업대 모델은 실제 치수로 만든다(높이 맞춤 없음). Workbench는 "StationModel" 자식을 찾는다.
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                visual.name = "StationModel";
                var renderers = visual.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) throw new InvalidOperationException("Station model has no renderers: " + s.ModelPath);
                Bounds b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                var box = root.AddComponent<BoxCollider>(); box.center = b.center - root.transform.position; box.size = b.size;
                var bench = root.AddComponent<Workbench>();
                bench.workbenchType = s.Type; bench.displayName = s.DisplayName;
                root.SetActive(true);
                prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            if (prefab == null) throw new InvalidOperationException("Could not save " + prefabPath);
            log.Add("created " + prefabPath);
        }
        var data = AssetDatabase.LoadAssetAtPath<BuildingData>(dataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<BuildingData>();
            data.prefab = prefab; data.buildingName = s.Building; data.price = 0;
            AssetDatabase.CreateAsset(data, dataPath); log.Add("created " + dataPath);
        }

        string kitPath = Root + "/" + s.KitAsset + ".asset";
        var kit = AssetDatabase.LoadAssetAtPath<Item>(kitPath);
        if (kit == null)
        {
            kit = ScriptableObject.CreateInstance<Item>();
            kit.id = s.KitId; kit.itemName = s.KitName; kit.maxStack = 1; kit.basePrice = 0;
            kit.category = ItemCategory.Tool; kit.model = model; kit.description = s.Description;
            AssetDatabase.CreateAsset(kit, kitPath); log.Add($"created {kitPath} (id {s.KitId})");
        }
        if (kit.icon == null) { kit.icon = PA_DemoIconBake.BakeIcon(kit, model); EditorUtility.SetDirty(kit); log.Add("baked icon " + s.KitId); }

        Recipe(s.RecipeAsset, s.KitName, WorkbenchType.BasicWorkbench, kit, s.Ingredients, log);
        return Entry(s, kit);
    }

    // ---------- 산출물 ----------
    static void BuildProduct(Product p, List<string> log)
    {
        string path = Items + "/" + p.Asset + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<Item>();
            item.id = p.Id; item.itemName = p.Name; item.description = p.Description; item.basePrice = p.Price;
            item.maxStack = p.MaxStack; item.category = p.Category; item.toolType = p.Tool;
            AssetDatabase.CreateAsset(item, path); log.Add($"created {path} (id {p.Id})");
        }
        // 도구가 아닌 산출물은 모델 정의가 바뀌면 다시 연결하고 아이콘을 굽는다(도구 외형은 PA_ToolProgressionSetup 몫).
        if (item.model == null || p.Tool == ToolType.None && item.model != Model(p.ModelKey))
        { item.model = Model(p.ModelKey); item.icon = null; EditorUtility.SetDirty(item); }
        if (item.icon == null) { item.icon = PA_DemoIconBake.BakeIcon(item, item.model); EditorUtility.SetDirty(item); log.Add("baked icon " + p.Id); }
        Recipe(p.Recipe, p.Name, p.Bench, item, p.Ingredients, log);
    }

    // 제작 창 상세에 보이는 산출물 설명. 영어 데이터 문구·설계 메모("단일 재료 고부가가치 조리 상품")를 플레이어 문장으로 바꾼다.
    static readonly Dictionary<int, string> PlayerCopy = new Dictionary<int, string>
    {
        [6] = "철 광석을 화로에 녹인 주괴. 철 곡괭이·철 도끼·튼튼한 낚싯대의 재료예요.",
        [7] = "나무를 반듯하게 켠 판재. 가구·액자·설비 키트의 재료예요.",
        [10] = "잡은 생선을 노릇하게 구웠어요. 날생선보다 훨씬 비싸게 팔려요.",
        [11] = "판재로 공들여 짠 가구. 본사 승인 등급이 오르면 만들 수 있어요.",
        [12] = "철괴와 판재로 만든 공구 세트. 실용품을 찾는 손님이 좋아해요.",
        [2021] = "나무로 짠 소박한 의자. 집이나 정착지에 놓아 꾸며요.",
        [2022] = "구리 날을 덧댄 곡괭이(힘 2). 고지대 철 광맥도 캘 수 있고, 광부 동료에게 주면 일이 빨라져요.",
    };

    static void ApplyPlayerCopy(List<string> log)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" }))
        {
            var item = AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null || !PlayerCopy.TryGetValue(item.id, out string copy) || item.description == copy) continue;
            item.description = copy; EditorUtility.SetDirty(item); log.Add("copy " + item.id);
        }
    }

    static GameObject Model(string key)
    {
        var art = FirstDayStudioAssets.Load();
        switch (key)
        {
            case "axe": return Resources.Load<Item>("FirstDayStudio/Item_2001")?.model ?? art.axe;
            case "rod": return Resources.Load<Item>("FirstDayStudio/Item_2011")?.model ?? throw new InvalidOperationException("Missing starter rod model");
            case "frame":
                var butterfly = Resources.Load<Item>("Items/Item_Butterfly")?.model ?? throw new InvalidOperationException("Missing butterfly model");
                // 세워 두는 작은 표본 액자: 판재 테두리 + 리넨 배경 + 핀으로 고정한 나비(정면 −Z).
                return PA_DemoIconBake.OwnedModel("ButterflyFrame", go =>
                {
                    var wood = new Color(.55f, .33f, .15f); var dark = new Color(.36f, .21f, .10f);
                    PA_DemoIconBake.Part(go, "Backing", new Vector3(0, .38f, .015f), new Vector3(.56f, .5f, .03f), new Color(.93f, .89f, .76f));
                    PA_DemoIconBake.Part(go, "Top", new Vector3(0, .655f, 0), new Vector3(.66f, .06f, .07f), wood);
                    PA_DemoIconBake.Part(go, "Bottom", new Vector3(0, .105f, 0), new Vector3(.66f, .06f, .07f), wood);
                    PA_DemoIconBake.Part(go, "Left", new Vector3(-.3f, .38f, 0), new Vector3(.06f, .5f, .07f), wood);
                    PA_DemoIconBake.Part(go, "Right", new Vector3(.3f, .38f, 0), new Vector3(.06f, .5f, .07f), wood);
                    PA_DemoIconBake.Part(go, "Stand", new Vector3(0, .04f, .06f), new Vector3(.42f, .08f, .22f), dark);
                    PA_DemoIconBake.AddModel(go, butterfly, new Vector3(0, .4f, -.03f), .36f);
                    // 날개를 정면으로 세운다(비행 모델은 날개가 수평).
                    var pinned = go.transform.GetChild(go.transform.childCount - 1);
                    pinned.localRotation = Quaternion.Euler(-90f, 0f, 0f) * pinned.localRotation;
                    foreach (var behaviour in pinned.GetComponentsInChildren<Behaviour>(true)) Object.DestroyImmediate(behaviour);
                    foreach (var collider in pinned.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                });
            case "skewer":
                // 나무 꼬챙이에 물고기 세 마리(같은 생선 모델을 크기만 줄여 나란히).
                return PA_DemoIconBake.OwnedModel("FishSkewer", go =>
                {
                    PA_DemoIconBake.Part(go, "Stick", new Vector3(0, .5f, 0), new Vector3(.035f, 1f, .035f), new Color(.6f, .42f, .22f));
                    for (int i = 0; i < 3; i++) PA_DemoIconBake.AddModel(go, art.fish, new Vector3(0, .28f + i * .22f, 0), .3f, 90f);
                });
            case "smoked":
                // 판재 쟁반 위 훈제 참치 두 마리.
                return PA_DemoIconBake.OwnedModel("SmokedFishTrayV2", go =>
                {
                    PA_DemoIconBake.Part(go, "Tray", new Vector3(0, .03f, 0), new Vector3(.62f, .06f, .4f), new Color(.5f, .32f, .16f));
                    PA_DemoIconBake.Part(go, "Rack", new Vector3(0, .075f, 0), new Vector3(.5f, .03f, .06f), new Color(.3f, .2f, .12f));
                    PA_DemoIconBake.AddModel(go, art.fish, new Vector3(0, .13f, 0), .5f, 90f);
                });
            default: throw new InvalidOperationException("Unknown model key " + key);
        }
    }

    static void Recipe(string asset, string name, WorkbenchType bench, Item output, (string resource, int id, int count)[] ingredients, List<string> log)
    {
        string path = Root + "/" + asset + ".asset";
        var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
        if (recipe != null)
        {
            if (recipe.outputItem != output) throw new InvalidOperationException(path + " exists but does not output " + output.name);
            if (recipe.icon == null && output.icon != null) { recipe.icon = output.icon; EditorUtility.SetDirty(recipe); }
            log.Add("kept " + path); return;
        }
        recipe = ScriptableObject.CreateInstance<RecipeData>();
        recipe.recipeName = name; recipe.requiredWorkbench = bench; recipe.requiredTier = 0;
        recipe.outputItem = output; recipe.outputCount = 1; recipe.icon = output.icon;
        recipe.ingredients = ingredients.Select(i => new RecipeIngredient { item = Require(i.resource, i.id), count = i.count }).ToList();
        AssetDatabase.CreateAsset(recipe, path); log.Add("created " + path);
    }

    static Item Require(string resource, int id)
    {
        var item = Resources.Load<Item>(resource);
        if (item == null || item.id != id) throw new InvalidOperationException($"Expected existing item id {id} at Resources/{resource}.");
        return item;
    }

    // 같은 경로에 다른 종류의 자산이 있으면 CreateAsset이 그것을 지우므로 먼저 멈춘다.
    static T Existing<T>(string path) where T : Object
    {
        var value = AssetDatabase.LoadAssetAtPath<T>(path);
        if (value == null && File.Exists(path)) throw new InvalidOperationException(path + " exists with an unexpected type.");
        return value;
    }
}
