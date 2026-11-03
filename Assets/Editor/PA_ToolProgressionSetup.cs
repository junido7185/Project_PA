using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 도구 단계·재료·가공 데이터(Docs/01_GAME_DESIGN/Content/TOOL_PROGRESSION.md).
// 새 아이템: 철 광석(2028)·구리 주괴(2029)·구리 도끼(2030)·철 곡괭이(2031). 옛 개선 곡괭이(2022)는 구리 곡괭이로 바뀐다.
// 화로 레시피: 구리 주괴, 철 주괴(철 광석), 구리 곡괭이, 구리 도끼, 철 곡괭이, 철 도끼. 레시피는 매번 정의대로 다시 쓴다(멱등).
// 배치: Unity -batchmode -executeMethod PA_ToolProgressionSetup.BatchApply
public static class PA_ToolProgressionSetup
{
    const string Root = "Assets/Resources/DemoStructure";
    const string Items = "Assets/Resources/Items";

    sealed class Def
    {
        public string Asset, Name, Description, ModelKey;
        public int Id, Price, MaxStack = 1; public ItemCategory Category; public ToolType Tool;
    }

    static readonly Def[] NewItems =
    {
        new Def { Asset = "Item_IronOre", Id = ToolPower.IronOre, Name = "철 광석", Price = 24, MaxStack = 99, Category = ItemCategory.Raw, ModelKey = "ironOre",
            Description = "고지대 철 광맥에서 캔 묵직한 광석. 화로에서 철 주괴로 녹여요. 구리 곡괭이 이상이 있어야 캘 수 있어요." },
        new Def { Asset = "Item_CopperBar", Id = ToolPower.CopperBar, Name = "구리 주괴", Price = 38, MaxStack = 99, Category = ItemCategory.Processed, ModelKey = "copperBar",
            Description = "구리 광석을 화로에 녹인 주괴. 구리 곡괭이·구리 도끼의 재료예요." },
        new Def { Asset = "Item_CopperAxe", Id = ToolPower.CopperAxe, Name = "구리 도끼", Category = ItemCategory.Tool, Tool = ToolType.Axe, ModelKey = "axe",
            Description = "구리 날을 단 도끼(힘 2). 나무를 더 깊게 찍어서 덜 휘둘러도 돼요." },
        new Def { Asset = "Item_IronPickaxe", Id = ToolPower.IronPickaxe, Name = "철 곡괭이", Category = ItemCategory.Tool, Tool = ToolType.Pickaxe, ModelKey = "pickaxe",
            Description = "단단한 철 곡괭이(힘 3). 철 광맥도 한두 번에 깨요. 광업 전문 분야에서 만들어요." },
    };

    [MenuItem("Project PA/Content/Tool Progression (Copper · Iron)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var catalog = AssetDatabase.LoadAssetAtPath<DemoPlaceableCatalog>(Root + "/Catalog.asset")
            ?? throw new InvalidOperationException("Missing DemoStructure/Catalog.asset");
        var log = new List<string>();
        Preflight();
        var made = NewItems.ToDictionary(d => d.Id, d => BuildItem(d, log));

        Item ore = Require("Items/Item_Ore", 4), wood = Require("Items/Item_Wood", 3), plank = Require("Items/Item_Plank", 7);
        // 스타터 도구는 보급 상자(FirstDayStudio supplies)의 실제 인스턴스를 재료로 쓴다(기존 개선 곡괭이 레시피와 같은 규칙).
        var art = FirstDayStudioAssets.Load();
        Item ironBar = Require("Items/Item_IronBar", 6);
        Item axe = art.supplies.First(i => i != null && i.toolType == ToolType.Axe), pickaxe = art.supplies.First(i => i != null && i.toolType == ToolType.Pickaxe);
        Item copper = AssetDatabase.LoadAssetAtPath<Item>(Root + "/Item_ImprovedPickaxe.asset") ?? throw new InvalidOperationException("Missing Item_ImprovedPickaxe");
        Item ironAxe = AssetDatabase.LoadAssetAtPath<Item>(Items + "/Item_IronAxe.asset") ?? throw new InvalidOperationException("Missing Item_IronAxe (run Crafting Stations setup first)");

        // 기존 아이템을 새 단계에 맞춘다(id 유지 = 저장 호환).
        Copy(ore, "일반 바위에서 캔 구리빛 광석. 화로에서 나무 1과 함께 녹이면 구리 주괴가 돼요.", log);
        Copy(ironBar, "철 광석을 화로에 녹인 주괴. 철 곡괭이·철 도끼·튼튼한 낚싯대의 재료예요.", log, price: 62);
        copper.itemName = "구리 곡괭이"; EditorUtility.SetDirty(copper);
        Copy(copper, "구리 날을 덧댄 곡괭이(힘 2). 고지대 철 광맥도 캘 수 있고, 광부 동료에게 주면 일이 빨라져요.", log);
        Copy(ironAxe, "철 날 도끼(힘 3). 나무를 한두 번에 넘겨요. 임업 전문 분야에서 만들어요.", log);

        Recipe("Recipe_CopperBar", "구리 주괴", made[ToolPower.CopperBar], (ore, 2), (wood, 1));
        Recipe("Recipe_IronBarSmelt", "철 주괴", ironBar, (made[ToolPower.IronOre], 2), (wood, 1));
        Recipe("Recipe_ImprovedPickaxe", "구리 곡괭이", copper, (pickaxe, 1), (made[ToolPower.CopperBar], 2), (wood, 1));
        Recipe("Recipe_CopperAxe", "구리 도끼", made[ToolPower.CopperAxe], (axe, 1), (made[ToolPower.CopperBar], 2), (wood, 1));
        Recipe("Recipe_IronPickaxe", "철 곡괭이", made[ToolPower.IronPickaxe], (copper, 1), (ironBar, 2), (plank, 1));
        Recipe("Recipe_IronAxe", "철 도끼", ironAxe, (made[ToolPower.CopperAxe], 1), (ironBar, 2), (plank, 1));
        log.Add("recipes written (forge ×6)");
        ApplyTierArt(copper, made[ToolPower.CopperAxe], made[ToolPower.IronPickaxe], ironAxe, log);
        // 레시피 아이콘도 새 아이콘으로 맞춘다.
        foreach (string asset in new[] { "Recipe_ImprovedPickaxe", "Recipe_CopperAxe", "Recipe_IronPickaxe", "Recipe_IronAxe" })
        {
            var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(Root + "/" + asset + ".asset");
            if (recipe != null && recipe.outputItem != null && recipe.icon != recipe.outputItem.icon) { recipe.icon = recipe.outputItem.icon; EditorUtility.SetDirty(recipe); }
        }

        // 구리 곡괭이는 누구나 만든다(전문 조건 없음). 광부 동행 선물·힘 2는 그대로.
        foreach (var upgrade in catalog.upgrades ?? Array.Empty<DemoToolUpgrade>())
            if (upgrade != null && upgrade.tool == copper && upgrade.root != DemoSpecialization.None)
            { upgrade.root = DemoSpecialization.None; log.Add("copper pickaxe upgrade root → None"); }
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log("[TOOL-PROGRESSION] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[TOOL-PROGRESSION] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    // id·이름이 다른 아이템과 겹치면 쓰기 전에 멈춘다.
    static void Preflight()
    {
        var all = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" })
            .Select(g => (path: AssetDatabase.GUIDToAssetPath(g), item: AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(g))))
            .Where(x => x.item != null).ToList();
        foreach (var def in NewItems)
        {
            string own = Items + "/" + def.Asset + ".asset";
            var clash = all.FirstOrDefault(x => x.path != own && x.item.id == def.Id);
            if (clash.item != null) throw new InvalidOperationException($"id {def.Id} already used by {clash.path}");
            if (AssetDatabase.LoadAssetAtPath<Item>(own) == null && File.Exists(own)) throw new InvalidOperationException(own + " exists with an unexpected type.");
        }
    }

    static Item BuildItem(Def d, List<string> log)
    {
        string path = Items + "/" + d.Asset + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<Item>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<Item>();
            item.id = d.Id;
            AssetDatabase.CreateAsset(item, path);
            log.Add($"created {path}");
        }
        item.itemName = d.Name; item.description = d.Description; item.basePrice = d.Price;
        item.maxStack = d.MaxStack; item.category = d.Category; item.toolType = d.Tool;
        if (item.model == null) item.model = Model(d.ModelKey);
        if (item.icon == null) { item.icon = PA_DemoIconBake.BakeIcon(item, item.model); log.Add("baked icon " + d.Id); }
        EditorUtility.SetDirty(item);
        return item;
    }

    static void Copy(Item item, string description, List<string> log, int price = -1)
    {
        if (item.description != description) { item.description = description; log.Add("copy " + item.id); }
        if (price >= 0 && item.basePrice != price) { item.basePrice = price; log.Add($"price {item.id} → {price}"); }
        EditorUtility.SetDirty(item);
    }

    static GameObject Model(string key)
    {
        var art = FirstDayStudioAssets.Load();
        switch (key)
        {
            case "axe": return Resources.Load<Item>("FirstDayStudio/Item_2001")?.model ?? art.axe;
            case "pickaxe": return art.pickaxe;
            case "ironOre":
                // 어두운 돌덩이에 붉은 녹빛 광맥 알갱이(월드 철 광맥 바위와 같은 색).
                return PA_DemoIconBake.OwnedModel("IronOre", go =>
                {
                    var rock = new Color(.36f, .35f, .37f); var rust = new Color(.62f, .2f, .12f);
                    PA_DemoIconBake.Part(go, "Chunk", new Vector3(0, .16f, 0), new Vector3(.42f, .3f, .36f), rock, .75f);
                    PA_DemoIconBake.Part(go, "Chip", new Vector3(.14f, .1f, .12f), new Vector3(.2f, .16f, .18f), rock, .7f);
                    PA_DemoIconBake.Part(go, "VeinA", new Vector3(-.06f, .3f, -.04f), new Vector3(.12f, .06f, .1f), rust);
                    PA_DemoIconBake.Part(go, "VeinB", new Vector3(.1f, .24f, -.15f), new Vector3(.09f, .07f, .06f), rust);
                    PA_DemoIconBake.Part(go, "VeinC", new Vector3(-.17f, .18f, .1f), new Vector3(.07f, .08f, .08f), rust);
                });
            case "copperBar":
                // 위가 좁은 주괴 두 개를 엇갈려 쌓은 구리 덩어리.
                return PA_DemoIconBake.OwnedModel("CopperBar", go =>
                {
                    var copper = new Color(.86f, .47f, .22f); var shade = new Color(.7f, .36f, .17f);
                    PA_DemoIconBake.Part(go, "BarLow", new Vector3(0, .06f, 0), new Vector3(.46f, .12f, .2f), shade, .78f);
                    PA_DemoIconBake.Part(go, "BarHigh", new Vector3(.04f, .18f, .02f), new Vector3(.44f, .12f, .19f), copper, .78f);
                });
            default: throw new InvalidOperationException("Unknown model key " + key);
        }
    }

    // ---------- 단계별 도구 외형 ----------
    // 스타터 돌 도구(Cube World Kit)는 텍스처 한 장을 쓰므로 재질 색만 바꾸면 자루도 바뀐다.
    // 텍스처 색으로 돌 머리 삼각형을 두 번째 서브메시로 떼고 금속 재질을 입힌다.
    const string ToolArt = "Assets/Art/ProjectPA/Derived/Tools";

    static GameObject MetalTool(GameObject source, string name, Color metal, List<string> log)
    {
        if (!AssetDatabase.IsValidFolder(ToolArt)) AssetDatabase.CreateFolder("Assets/Art/ProjectPA/Derived", "Tools");
        string path = ToolArt + "/" + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null && existing.GetComponentsInChildren<MeshRenderer>(true).Any(r => r.sharedMaterials.Length > 1)) return existing;
        var material = MetalMaterial(metal);
        var root = UnityEngine.Object.Instantiate(source);
        root.name = name;
        try
        {
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            int split = 0;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (filter.sharedMesh == null || renderer == null) continue;
                var mesh = SplitHead(filter, renderer, out int headTris);
                if (mesh == null) continue;
                AssetDatabase.CreateAsset(mesh, ToolArt + "/" + name + "_" + filter.name + ".asset");
                filter.sharedMesh = mesh;
                renderer.sharedMaterials = new[] { renderer.sharedMaterial, material };
                split += headTris;
            }
            log.Add($"{name}: head triangles {split}");
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    // 머리 판정: 각 삼각형 UV 중심의 텍스처 색. 돌 머리는 회색(채도 낮음), 나무 자루는 갈색(채도 높음)이다.
    // (첫 시도의 높이별 폭 판정은 정점이 몇 높이에만 있는 로우폴리라 실패: 머리 0개.)
    static Mesh SplitHead(MeshFilter filter, MeshRenderer renderer, out int headTris)
    {
        headTris = 0;
        var source = filter.sharedMesh;
        var texture = renderer.sharedMaterial != null ? renderer.sharedMaterial.mainTexture as Texture2D : null;
        if (source == null || texture == null || source.uv.Length == 0) return null;
        var pixels = Readable(texture);
        var uv = source.uv;
        var tris = source.triangles;
        var body = new List<int>(); var head = new List<int>();
        for (int t = 0; t < tris.Length; t += 3)
        {
            Vector2 c = (uv[tris[t]] + uv[tris[t + 1]] + uv[tris[t + 2]]) / 3f;
            Color color = pixels.GetPixelBilinear(c.x, c.y);
            Color.RGBToHSV(color, out _, out float saturation, out float value);
            (saturation < .2f && value > .25f ? head : body).AddRange(new[] { tris[t], tris[t + 1], tris[t + 2] });
        }
        UnityEngine.Object.DestroyImmediate(pixels);
        headTris = head.Count / 3;
        Debug.Log($"[TOOL-PROGRESSION] split {filter.name}: triangles={tris.Length / 3} head={headTris}");
        if (headTris == 0 || body.Count == 0) return null;
        var mesh = UnityEngine.Object.Instantiate(source);
        mesh.name = source.name + "_MetalHead";
        mesh.subMeshCount = 2;
        mesh.SetTriangles(body, 0);
        mesh.SetTriangles(head, 1);
        return mesh;
    }

    // 가져온 텍스처는 읽기 불가일 수 있어 RenderTexture로 복사해 읽는다(에디터 전용).
    static Texture2D Readable(Texture2D texture)
    {
        var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var previous = RenderTexture.active;
        Graphics.Blit(texture, rt);
        RenderTexture.active = rt;
        var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        copy.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return copy;
    }

    static Material MetalMaterial(Color color)
    {
        string path = ToolArt + "/Metal_" + ColorUtility.ToHtmlStringRGB(color) + ".mat";
        var found = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (found != null) return found;
        found = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        found.SetFloat("_Metallic", .55f);
        found.SetFloat("_Smoothness", .5f);
        AssetDatabase.CreateAsset(found, path);
        return found;
    }

    static readonly Color CopperMetal = new Color(.86f, .47f, .22f), IronMetal = new Color(.55f, .58f, .62f);

    // 단계 도구 4종에 금속 머리 모델을 연결하고 아이콘을 다시 굽는다.
    static void ApplyTierArt(Item copperPick, Item copperAxe, Item ironPick, Item ironAxe, List<string> log)
    {
        var art = FirstDayStudioAssets.Load();
        var axeModel = Resources.Load<Item>("FirstDayStudio/Item_2001")?.model ?? art.axe;
        foreach (var (item, source, color, name) in new[]
        {
            (copperPick, art.pickaxe, CopperMetal, "PA_Tool_CopperPickaxe"), (copperAxe, axeModel, CopperMetal, "PA_Tool_CopperAxe"),
            (ironPick, art.pickaxe, IronMetal, "PA_Tool_IronPickaxe"), (ironAxe, axeModel, IronMetal, "PA_Tool_IronAxe"),
        })
        {
            var model = MetalTool(source, name, color, log);
            item.model = model;
            item.icon = PA_DemoIconBake.BakeIcon(item, model);
            EditorUtility.SetDirty(item);
            log.Add("tier art " + item.id);
        }
        SturdyRodArt(art, log);
    }

    // 튼튼한 낚싯대(2027): 스타터 낚싯대에 철 릴(손잡이 위)과 줄 고리 세 개를 단다. 원본 모델의 회전·크기는 자식에 그대로 둔다.
    static void SturdyRodArt(FirstDayStudioAssets art, List<string> log)
    {
        var item = AssetDatabase.LoadAssetAtPath<Item>(Items + "/Item_SturdyRod.asset");
        var source = Resources.Load<Item>("FirstDayStudio/Item_2011")?.model ?? art.rod;
        if (item == null || source == null) { log.Add("sturdy rod art skipped (missing item or starter rod)"); return; }
        if (!AssetDatabase.IsValidFolder(ToolArt)) AssetDatabase.CreateFolder("Assets/Art/ProjectPA/Derived", "Tools");
        var root = new GameObject("PA_Tool_SturdyRod");
        try
        {
            var rod = (GameObject)UnityEngine.Object.Instantiate(source, root.transform);
            rod.name = "Rod";
            rod.transform.localPosition = Vector3.zero; rod.transform.localRotation = source.transform.localRotation; rod.transform.localScale = source.transform.localScale;
            var renderers = rod.GetComponentsInChildren<Renderer>();
            Bounds b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
            float h = b.size.y, w = Mathf.Max(.012f, Mathf.Min(b.size.x, b.size.z));
            Vector3 At(float t) => new Vector3(b.center.x, b.min.y + h * t, b.center.z);
            var dark = new Color(.24f, .26f, .3f);
            // 릴: 손잡이 바로 위 옆에 붙은 철 원통 + 손잡이 감개(낚싯대 길이에 비례). 휜 낚싯대라 줄 고리는 원본 것을 그대로 쓴다.
            PA_DemoIconBake.Part(root, "ReelBodyV2", At(.19f) + new Vector3(w * .5f + h * .022f, 0, 0), new Vector3(h * .045f, h * .05f, h * .05f), IronMetal, .9f);
            PA_DemoIconBake.Part(root, "ReelKnobV2", At(.19f) + new Vector3(w * .5f + h * .05f, h * .012f, 0), new Vector3(h * .014f, h * .026f, h * .014f), dark);
            PA_DemoIconBake.Part(root, "GripV2", At(.09f), new Vector3(w * 1.5f, h * .09f, w * 1.5f), dark);
            var model = PrefabUtility.SaveAsPrefabAsset(root, ToolArt + "/PA_Tool_SturdyRod.prefab");
            item.model = model;
            item.icon = PA_DemoIconBake.BakeIcon(item, model);
            EditorUtility.SetDirty(item);
            log.Add("tier art 2027 (sturdy rod: reel + grip)");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    // 레시피는 정의대로 다시 쓴다. 다른 아이템을 만드는 같은 이름의 자산이 있으면 멈춘다.
    static void Recipe(string asset, string name, Item output, params (Item item, int count)[] ingredients)
    {
        string path = Root + "/" + asset + ".asset";
        var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
        if (recipe == null)
        {
            if (File.Exists(path)) throw new InvalidOperationException(path + " exists with an unexpected type.");
            recipe = ScriptableObject.CreateInstance<RecipeData>();
            AssetDatabase.CreateAsset(recipe, path);
        }
        else if (recipe.outputItem != null && recipe.outputItem != output)
            throw new InvalidOperationException(path + " outputs " + recipe.outputItem.name + ", expected " + output.name);
        recipe.recipeName = name; recipe.requiredWorkbench = WorkbenchType.Forge; recipe.requiredTier = 0;
        recipe.outputItem = output; recipe.outputCount = 1; recipe.icon = output.icon;
        recipe.ingredients = ingredients.Select(i => new RecipeIngredient { item = i.item, count = i.count }).ToList();
        EditorUtility.SetDirty(recipe);
    }

    static Item Require(string resource, int id)
    {
        var item = Resources.Load<Item>(resource);
        if (item == null || item.id != id) throw new InvalidOperationException($"Expected item id {id} at Resources/{resource}.");
        return item;
    }
}
