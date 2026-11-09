using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 삽·파기 자국(Docs/01_GAME_DESIGN/Content/DIGGING.md). 레퍼런스 09(삽을 든 캐릭터와 땅 대상 표시)의 '땅속 발견물' 고리.
// 새 아이템: 삽(2038, 작업대)·점토 덩어리(2039)·옛 동전(2040)·화석 조각(2041)·구운 토기 항아리(2042, 화로).
// 월드 표시: Resources/Dig/DigMound(들썩이는 흙 자국)·DigHole(파낸 자리). 모델은 PA_DemoIconBake.Part 소유 로우폴리.
// 멱등: 아이템·레시피·프리팹·아이콘을 매번 정의대로 다시 쓴다.
// 배치: Unity -batchmode -executeMethod PA_DigSetup.BatchApply
public static class PA_DigSetup
{
    const string Items = "Assets/Resources/Items";
    const string Recipes = "Assets/Resources/DemoStructure";
    const string WorldArt = "Assets/Resources/Dig";
    const string ItemArt = "Assets/Art/ProjectPA/Derived/Dig";

    public const int Shovel = 2038, Clay = 2039, OldCoin = 2040, FossilShard = 2041, FiredJar = 2042;

    sealed class Def
    {
        public string Asset, Name, Description, Model;
        public int Id, Price, MaxStack; public ItemCategory Category; public ToolType Tool;
    }

    static readonly Def[] Defs =
    {
        new Def { Asset = "Item_Shovel", Id = Shovel, Name = "삽", Price = 0, MaxStack = 1, Category = ItemCategory.Tool, Tool = ToolType.Shovel, Model = "Shovel",
            Description = "흙이 들썩이는 자리를 파는 삽. 점토·옛 동전·화석 조각이 묻혀 있어요. 한 자리 팔 때마다 조금 닳아요." },
        new Def { Asset = "Item_Clay", Id = Clay, Name = "점토 덩어리", Price = 9, MaxStack = 99, Category = ItemCategory.Raw, Model = "Clay",
            Description = "파기 자국에서 나온 찰진 점토. 화로에서 구우면 토기 항아리가 돼요." },
        new Def { Asset = "Item_OldCoin", Id = OldCoin, Name = "옛 동전", Price = 36, MaxStack = 99, Category = ItemCategory.Raw, Model = "OldCoin",
            Description = "누가 언제 묻었는지 모를 낡은 동전. 수집하는 손님이 값을 쳐 줘요." },
        new Def { Asset = "Item_FossilShard", Id = FossilShard, Name = "화석 조각", Price = 64, MaxStack = 99, Category = ItemCategory.Raw, Model = "Fossil",
            Description = "돌 속에 소용돌이 껍데기 무늬가 남은 화석 조각. 파기 자국에서 가장 드물게 나와요." },
        new Def { Asset = "Item_FiredJar", Id = FiredJar, Name = "구운 토기 항아리", Price = 64, MaxStack = 8, Category = ItemCategory.Processed, Model = "Jar",
            Description = "점토를 빚어 화로에 구운 항아리. 점토를 그냥 파는 것보다 두 배쯤 받아요." },
    };

    static readonly Color Wood = new Color(.58f, .38f, .2f), DarkWood = new Color(.36f, .23f, .12f), Steel = new Color(.62f, .65f, .68f);
    static readonly Color Dirt = new Color(.47f, .33f, .2f), DarkDirt = new Color(.25f, .17f, .1f);

    [MenuItem("Project PA/Content/Digging (Shovel · Dig Spots)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        Preflight();
        Folder(WorldArt); Folder(ItemArt);
        var models = new Dictionary<string, GameObject>
        {
            ["Shovel"] = Prefab(ItemArt, "PA_Dig_Shovel", go =>
            {
                // 모델 +Y = 손잡이 → 날, +X = 날의 넓은 면(도구 자세 규약). 원점 근처가 손잡이 끝(오른손).
                PA_DemoIconBake.Part(go, "DGrip", new Vector3(0, .03f, 0), new Vector3(.05f, .05f, .18f), DarkWood);
                PA_DemoIconBake.Part(go, "Shaft", new Vector3(0, .52f, 0), new Vector3(.045f, .94f, .045f), Wood);
                PA_DemoIconBake.Part(go, "Collar", new Vector3(0, .98f, 0), new Vector3(.07f, .08f, .07f), Steel);
                PA_DemoIconBake.Part(go, "Blade", new Vector3(0, 1.17f, 0), new Vector3(.04f, .32f, .26f), Steel, .55f);
            }),
            ["Clay"] = Prefab(ItemArt, "PA_Dig_Clay", go =>
            {
                var clay = new Color(.66f, .4f, .27f);
                Round(go, "LumpA", new Vector3(-.06f, 0, 0), new[] { V(.17f, 0), V(.19f, .07f), V(.14f, .16f), V(0, .2f) }, clay, 9);
                Round(go, "LumpB", new Vector3(.15f, 0, .09f), new[] { V(.11f, 0), V(.12f, .05f), V(.08f, .11f), V(0, .13f) }, new Color(.58f, .34f, .22f), 8);
            }),
            ["OldCoin"] = Prefab(ItemArt, "PA_Dig_OldCoin", go =>
            {
                // 둥근 엽전: 테두리 → 살짝 낮은 면 → 가운데 네모 구멍.
                Round(go, "Coin", Vector3.zero, new[] { V(.25f, 0), V(.25f, .05f), V(.2f, .055f), V(.2f, .045f), V(0, .045f) }, new Color(.9f, .7f, .3f), 16);
                PA_DemoIconBake.Part(go, "Hole", new Vector3(0, .05f, 0), new Vector3(.08f, .012f, .08f), new Color(.38f, .27f, .1f));
            }),
            ["Fossil"] = Prefab(ItemArt, "PA_Dig_Fossil", go =>
            {
                Round(go, "Slab", Vector3.zero, new[] { V(.28f, 0), V(.29f, .07f), V(.24f, .11f), V(0, .12f) }, new Color(.8f, .74f, .62f), 9);
                // 소용돌이 껍데기: 접선 방향으로 놓은 짧은 능선 16개를 이어 안쪽으로 감기는 나선 한 줄.
                for (int i = 0; i < 16; i++)
                {
                    float a = i * .62f, r = .19f * Mathf.Exp(-.11f * i), step = r * .62f;
                    PA_DemoIconBake.Part(go, "Ridge" + i, new Vector3(Mathf.Cos(a) * r, .125f, Mathf.Sin(a) * r), new Vector3(step * 1.15f, .03f, .028f), new Color(.5f, .42f, .32f));
                    go.transform.Find("Ridge" + i).localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg + 90f, 0);
                }
            }),
            ["Jar"] = Prefab(ItemArt, "PA_Dig_FiredJar", go =>
            {
                // 배가 불룩한 항아리(돌림판 단면).
                Round(go, "Body", Vector3.zero, new[] { V(.13f, 0), V(.2f, .08f), V(.24f, .2f), V(.2f, .33f), V(.11f, .4f), V(.1f, .45f), V(.14f, .48f), V(.12f, .5f), V(0, .46f) },
                    new Color(.8f, .44f, .27f), 14);
                Round(go, "Band", new Vector3(0, .17f, 0), new[] { V(.245f, 0), V(.25f, .04f), V(.235f, .05f) }, new Color(.55f, .28f, .16f), 14);
            }),
        };

        Prefab(WorldArt, "DigMound", go =>
        {
            Round(go, "Mound", Vector3.zero, new[] { V(.38f, 0), V(.32f, .07f), V(.18f, .13f), V(0, .15f) }, Dirt, 11);
            PA_DemoIconBake.Part(go, "ClodA", new Vector3(.32f, .04f, .18f), new Vector3(.13f, .08f, .11f), Dirt, .6f);
            PA_DemoIconBake.Part(go, "ClodB", new Vector3(-.3f, .035f, -.16f), new Vector3(.11f, .07f, .1f), Dirt, .6f);
            PA_DemoIconBake.Part(go, "ClodC", new Vector3(.05f, .03f, -.33f), new Vector3(.1f, .06f, .09f), Dirt, .6f);
            // 윗면의 X자 갈라짐(멀리서 '판 적 없는 흙'과 구별되게).
            PA_DemoIconBake.Part(go, "SplitA", new Vector3(0, .142f, 0), new Vector3(.22f, .025f, .045f), DarkDirt);
            PA_DemoIconBake.Part(go, "SplitB", new Vector3(0, .143f, 0), new Vector3(.22f, .025f, .045f), DarkDirt);
            go.transform.Find("SplitA").localRotation = Quaternion.Euler(0, 35f, 0);
            go.transform.Find("SplitB").localRotation = Quaternion.Euler(0, -35f, 0);
        });
        Prefab(WorldArt, "DigHole", go =>
        {
            Round(go, "Hole", Vector3.zero, new[] { V(.32f, 0), V(.3f, .02f), V(0, .025f) }, DarkDirt, 12);
            Round(go, "Pile", new Vector3(.44f, 0, .05f), new[] { V(.16f, 0), V(.12f, .07f), V(0, .12f) }, Dirt, 9);
        });
        log.Add("world prefabs Resources/Dig/DigMound·DigHole");

        var made = Defs.ToDictionary(d => d.Id, d => BuildItem(d, models[d.Model], log));
        Item wood = Require("Items/Item_Wood", 3), ore = Require("Items/Item_Ore", 4);
        Recipe("Recipe_Shovel", "삽", WorkbenchType.BasicWorkbench, made[Shovel], (wood, 2), (ore, 1));
        Recipe("Recipe_FiredJar", "구운 토기 항아리", WorkbenchType.Forge, made[FiredJar], (made[Clay], 3), (wood, 1));
        log.Add("recipes Recipe_Shovel(workbench)·Recipe_FiredJar(forge)");
        AssetDatabase.SaveAssets();
        Debug.Log("[DIG-SETUP] " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[DIG-SETUP] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        Folder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void Preflight()
    {
        var all = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" })
            .Select(g => (path: AssetDatabase.GUIDToAssetPath(g), item: AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(g))))
            .Where(x => x.item != null).ToList();
        foreach (var def in Defs)
        {
            string own = Items + "/" + def.Asset + ".asset";
            var clash = all.FirstOrDefault(x => x.path != own && x.item.id == def.Id);
            if (clash.item != null) throw new InvalidOperationException($"id {def.Id} already used by {clash.path}");
            if (AssetDatabase.LoadAssetAtPath<Item>(own) == null && File.Exists(own)) throw new InvalidOperationException(own + " exists with an unexpected type.");
        }
    }

    internal static Vector2 V(float radius, float y) => new Vector2(radius, y);

    // 돌림판 메시: (반지름, 높이) 단면을 sides 조각으로 돌린다. 면마다 정점을 따로 둬 로우폴리 각진 음영, 바닥은 막는다.
    internal static void Round(GameObject root, string name, Vector3 position, Vector2[] profile, Color color, int sides = 12)
    {
        var v = new List<Vector3>();
        Vector3 P(int k, Vector2 p) { float a = k / (float)sides * Mathf.PI * 2f; return new Vector3(Mathf.Cos(a) * p.x, p.y, Mathf.Sin(a) * p.x); }
        void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 inside)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3f - inside) < 0f) { var t = b; b = c; c = t; }
            v.Add(a); v.Add(b); v.Add(c);
        }
        float midY = (profile[0].y + profile[profile.Length - 1].y) * .5f;
        for (int k = 0; k < sides; k++)
        {
            for (int i = 0; i < profile.Length - 1; i++)
            {
                Vector3 a = P(k, profile[i]), b = P(k + 1, profile[i]), c = P(k + 1, profile[i + 1]), d = P(k, profile[i + 1]);
                Vector3 inside = new Vector3(0, (profile[i].y + profile[i + 1].y) * .5f + (profile[i + 1].x < .001f ? -1f : 0f), 0);
                if (profile[i].x > .001f) Tri(a, b, c, inside);
                if (profile[i + 1].x > .001f || profile[i].x > .001f) Tri(a, c, d, inside);
            }
            Vector3 bottom = new Vector3(0, profile[0].y, 0);
            if (profile[0].x > .001f) Tri(bottom, P(k, profile[0]), P(k + 1, profile[0]), bottom + Vector3.up);
        }
        var mesh = new Mesh { name = root.name + "_" + name };
        mesh.SetVertices(v); mesh.SetTriangles(Enumerable.Range(0, v.Count).ToArray(), 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string meshPath = ItemArt + "/" + root.name + "_" + name + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (saved == null) { AssetDatabase.CreateAsset(mesh, meshPath); saved = mesh; }
        else { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); }
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = position;
        go.GetComponent<MeshFilter>().sharedMesh = saved;
        go.GetComponent<MeshRenderer>().sharedMaterial = RoundMaterial(color);
    }

    static Material RoundMaterial(Color color)
    {
        string path = ItemArt + "/Mat_" + ColorUtility.ToHtmlStringRGB(color) + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        material.SetFloat("_Smoothness", .28f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static GameObject Prefab(string folder, string name, Action<GameObject> build)
    {
        var root = new GameObject(name);
        try { build(root); return PrefabUtility.SaveAsPrefabAsset(root, folder + "/" + name + ".prefab"); }
        finally { Object.DestroyImmediate(root); }
    }

    static Item BuildItem(Def d, GameObject model, List<string> log)
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
        item.model = model;
        item.icon = PA_DemoIconBake.BakeIcon(item, model); log.Add("baked icon " + d.Id);
        EditorUtility.SetDirty(item);
        return item;
    }

    static void Recipe(string asset, string name, WorkbenchType bench, Item output, params (Item item, int count)[] ingredients)
    {
        string path = Recipes + "/" + asset + ".asset";
        var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
        if (recipe == null)
        {
            if (File.Exists(path)) throw new InvalidOperationException(path + " exists with an unexpected type.");
            recipe = ScriptableObject.CreateInstance<RecipeData>();
            AssetDatabase.CreateAsset(recipe, path);
        }
        else if (recipe.outputItem != null && recipe.outputItem != output)
            throw new InvalidOperationException(path + " outputs " + recipe.outputItem.name + ", expected " + output.name);
        recipe.recipeName = name; recipe.requiredWorkbench = bench; recipe.requiredTier = 0;
        recipe.outputItem = output; recipe.outputCount = 1; recipe.icon = output.icon;
        recipe.ingredients = ingredients.Select(i => new RecipeIngredient { item = i.item, count = i.count }).ToList();
        EditorUtility.SetDirty(recipe);
    }

    static Item Require(string resource, int id)
    {
        var item = Resources.Load<Item>(resource);
        if (item == null || item.id != id) throw new InvalidOperationException($"Expected existing item id {id} at Resources/{resource}.");
        return item;
    }
}
