using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 해변 줍기(Docs/01_GAME_DESIGN/Content/BEACHCOMBING.md). 레퍼런스 해안 장면(References/Dinkum 03)에서 가져온 '모래사장에 놓인 작은 발견물' 고리.
// 새 아이템: 가리비 껍데기(2034)·소라 껍데기(2035)·불가사리(2036)·조개 목걸이(2037, 작업대). 모델은 소유 로우폴리 메시로 만든다(외부 자산 없음).
// 멱등: 아이템·레시피·메시·프리팹·아이콘을 매번 정의대로 다시 쓴다(메시·프리팹 GUID는 유지).
// 배치: Unity -batchmode -executeMethod PA_BeachFindsSetup.BatchApply
public static class PA_BeachFindsSetup
{
    const string Items = "Assets/Resources/Items";
    const string Recipes = "Assets/Resources/DemoStructure";
    const string Art = "Assets/Art/ProjectPA/Derived/Beach";

    public const int Scallop = 2034, Conch = 2035, Starfish = 2036, Necklace = 2037, ShellDecor = 2043;
    const string Structure = "Assets/Resources/DemoStructure";

    sealed class Def
    {
        public string Asset, Name, Description, Model;
        public int Id, Price, MaxStack; public ItemCategory Category;
    }

    static readonly Def[] Defs =
    {
        new Def { Asset = "Item_ScallopShell", Id = Scallop, Name = "가리비 껍데기", Price = 8, MaxStack = 99, Category = ItemCategory.Raw, Model = "ScallopShell",
            Description = "모래사장에서 주운 부채꼴 조개껍데기. 바다가 매일 새로 밀어 올려요. 작업대에서 목걸이로 엮을 수 있어요." },
        new Def { Asset = "Item_ConchShell", Id = Conch, Name = "소라 껍데기", Price = 15, MaxStack = 99, Category = ItemCategory.Raw, Model = "ConchShell",
            Description = "돌기가 난 소라 껍데기. 귀에 대면 바닷소리가 나요. 가리비보다 드물어요." },
        new Def { Asset = "Item_Starfish", Id = Starfish, Name = "불가사리", Price = 24, MaxStack = 99, Category = ItemCategory.Raw, Model = "Starfish",
            Description = "물가에 가끔 올라오는 주황 불가사리. 해변에서 가장 드문 발견물이라 값이 좋아요." },
        new Def { Asset = "Item_ShellNecklace", Id = Necklace, Name = "조개 목걸이", Price = 72, MaxStack = 8, Category = ItemCategory.Luxury, Model = "ShellNecklace",
            Description = "가리비 둘과 소라 하나를 끈에 엮은 목걸이. 껍데기를 따로 파는 것보다 비싸고, 관광객이 기념품으로 좋아해요." },
    };

    [MenuItem("Project PA/Content/Beach Finds (Shells · Starfish · Necklace)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit mode.");
        var log = new List<string>();
        Preflight();
        Directory.CreateDirectory(Art);
        var models = new Dictionary<string, GameObject>
        {
            ["ScallopShell"] = Prefab("ScallopShell", go => Mesh(go, "Shell", ScallopMesh(), new Color(.99f, .86f, .80f))),
            ["ConchShell"] = Prefab("ConchShell", go =>
            {
                Mesh(go, "Shell", ConchMesh(), new Color(.94f, .86f, .72f));
                Mesh(go, "Lip", ConchLipMesh(), new Color(.96f, .62f, .60f));
            }),
            ["Starfish"] = Prefab("Starfish", go => Mesh(go, "Body", StarMesh(), new Color(.93f, .45f, .25f))),
        };
        models["ShellNecklace"] = Prefab("ShellNecklace", go =>
        {
            Mesh(go, "Cord", CordMesh(), new Color(.55f, .38f, .22f));
            PA_DemoIconBake.AddModel(go, models["ScallopShell"], new Vector3(0, .02f, -.25f), .3f, 180f);
            PA_DemoIconBake.AddModel(go, models["ScallopShell"], new Vector3(-.2f, .02f, -.14f), .2f, 130f);
            PA_DemoIconBake.AddModel(go, models["ConchShell"], new Vector3(.21f, .02f, -.13f), .24f, 60f);
        });

        var made = Defs.ToDictionary(d => d.Id, d => BuildItem(d, models[d.Model], log));
        Recipe("Recipe_ShellNecklace", "조개 목걸이", made[Necklace], (made[Scallop], 2), (made[Conch], 1));
        log.Add("recipe Recipe_ShellNecklace (workbench)");

        // 2026-11-03 조개 장식: 해변 발견물 → 가게 꾸밈(의자와 같은 효과, DemoSettlementController.ShellDecorKey).
        var decorModel = Prefab("ShellDecor", go =>
        {
            PA_DigSetup.Round(go, "Tub", Vector3.zero, new[] { PA_DigSetup.V(.24f, 0), PA_DigSetup.V(.3f, .2f), PA_DigSetup.V(.27f, .22f), PA_DigSetup.V(.25f, .17f), PA_DigSetup.V(0, .17f) },
                new Color(.58f, .38f, .2f), 12);
            PA_DigSetup.Round(go, "Sand", new Vector3(0, .17f, 0), new[] { PA_DigSetup.V(.26f, 0), PA_DigSetup.V(.2f, .04f), PA_DigSetup.V(0, .05f) }, new Color(.93f, .82f, .6f), 12);
            PA_DemoIconBake.AddModel(go, models["Starfish"], new Vector3(-.08f, .25f, .06f), .36f, 20f);
            PA_DemoIconBake.AddModel(go, models["ConchShell"], new Vector3(.12f, .25f, -.08f), .3f, -30f);
            PA_DemoIconBake.AddModel(go, models["ScallopShell"], new Vector3(.08f, .25f, .14f), .26f, 160f);
            PA_DemoIconBake.AddModel(go, models["ScallopShell"], new Vector3(-.15f, .25f, -.12f), .22f, 70f);
        });
        var decorItem = BuildItem(new Def { Asset = "Item_ShellDecor", Id = ShellDecor, Name = "조개 장식", Price = 0, MaxStack = 3, Category = ItemCategory.Tool, Model = "ShellDecor",
            Description = "모래를 채운 나무 통에 조개와 불가사리를 꽂은 장식. 가게 안에 놓으면 그날 밤 손님이 1명 더 와요(의자와 합쳐 최대 3개)." }, decorModel, log);
        Recipe("Recipe_ShellDecor", "조개 장식", decorItem, (made[Scallop], 2), (made[Starfish], 1), (Resources.Load<Item>("Items/Item_Plank"), 1));
        BuildDecorPlaceable(decorModel, log);
        var catalog = AssetDatabase.LoadAssetAtPath<DemoPlaceableCatalog>(Structure + "/Catalog.asset");
        if (catalog != null)
        {
            var entries = (catalog.entries ?? Array.Empty<DemoPlaceableEntry>()).Where(e => e != null && e.key != "decor-shells").ToList();
            entries.Add(DecorEntry(decorItem));
            catalog.entries = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            log.Add("catalog entry decor-shells");
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[BEACH-FINDS] " + string.Join(" | ", log));
    }

    static DemoPlaceableEntry DecorEntry(Item item) => new DemoPlaceableEntry
    {
        item = item, key = "decor-shells", kind = DemoPlaceableKind.Furniture, buildingResource = "DemoStructure/ShellDecor",
        size = Vector2Int.one, surfaces = WorldPlaceableSurface.Living | WorldPlaceableSurface.Utility | WorldPlaceableSurface.ShopFloor, canMove = true,
    };

    // PA_DemoStructureSetup가 catalog.entries를 다시 쓸 때 조개 장식 항목을 잃지 않게 같은 정의를 돌려준다.
    public static IEnumerable<DemoPlaceableEntry> ExistingEntries()
    {
        var item = AssetDatabase.LoadAssetAtPath<Item>(Items + "/Item_ShellDecor.asset");
        if (item != null && AssetDatabase.LoadAssetAtPath<GameObject>(Structure + "/ShellDecor.prefab") != null) yield return DecorEntry(item);
    }

    // 배치용 건물 프리팹 + BuildingData(의자 SimpleFurniture와 같은 구성: 모델 자식 + 상자 충돌체).
    static void BuildDecorPlaceable(GameObject model, List<string> log)
    {
        var root = new GameObject("ShellDecor");
        try
        {
            FirstDayStudioAssets.Place(model, root.transform, Vector3.zero, .55f);
            var box = root.AddComponent<BoxCollider>(); box.center = new Vector3(0, .3f, 0); box.size = new Vector3(.9f, .6f, .9f);
            root.SetActive(true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Structure + "/ShellDecor.prefab");
            string dataPath = Structure + "/ShellDecor.asset";
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(dataPath);
            if (data == null) { data = ScriptableObject.CreateInstance<BuildingData>(); AssetDatabase.CreateAsset(data, dataPath); }
            data.prefab = prefab; data.buildingName = "ShellDecor"; data.price = 0;
            EditorUtility.SetDirty(data);
            log.Add("placeable DemoStructure/ShellDecor");
        }
        finally { Object.DestroyImmediate(root); }
    }

    public static void BatchApply()
    {
        int code = 0;
        try { Apply(); }
        catch (Exception e) { Debug.LogError("[BEACH-FINDS] " + e); code = 1; }
        EditorApplication.Exit(code);
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
        item.maxStack = d.MaxStack; item.category = d.Category; item.toolType = ToolType.None;
        item.model = model;
        item.icon = PA_DemoIconBake.BakeIcon(item, model); log.Add("baked icon " + d.Id);
        EditorUtility.SetDirty(item);
        return item;
    }

    static void Recipe(string asset, string name, Item output, params (Item item, int count)[] ingredients)
    {
        string path = Recipes + "/" + asset + ".asset";
        var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
        if (recipe == null)
        {
            if (File.Exists(path)) throw new InvalidOperationException(path + " exists with an unexpected type.");
            recipe = ScriptableObject.CreateInstance<RecipeData>();
            AssetDatabase.CreateAsset(recipe, path);
        }
        recipe.recipeName = name; recipe.requiredWorkbench = WorkbenchType.BasicWorkbench; recipe.requiredTier = 0;
        if (output == null || ingredients.Any(i => i.item == null)) throw new InvalidOperationException("Missing item for recipe " + asset);
        recipe.outputItem = output; recipe.outputCount = 1; recipe.icon = output.icon;
        recipe.ingredients = ingredients.Select(i => new RecipeIngredient { item = i.item, count = i.count }).ToList();
        EditorUtility.SetDirty(recipe);
    }

    // ---------- 소유 로우폴리 모델 ----------
    static GameObject Prefab(string name, Action<GameObject> build)
    {
        string path = Art + "/" + name + ".prefab";
        var root = new GameObject(name);
        try { build(root); return PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { Object.DestroyImmediate(root); }
    }

    static void Mesh(GameObject root, string part, Mesh mesh, Color color)
    {
        string meshPath = Art + "/" + root.name + "_" + part + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        mesh.name = root.name + "_" + part;
        if (saved == null) { AssetDatabase.CreateAsset(mesh, meshPath); saved = mesh; }
        else { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); }
        var go = new GameObject(part, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(root.transform, false);
        go.GetComponent<MeshFilter>().sharedMesh = saved;
        go.GetComponent<MeshRenderer>().sharedMaterial = Material(color);
    }

    static Material Material(Color color)
    {
        string path = Art + "/Mat_" + ColorUtility.ToHtmlStringRGB(color) + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        material.SetFloat("_Smoothness", .32f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // 면마다 정점을 따로 둬 로우폴리 각진 음영을 만든다(섬의 다른 소유 모델과 같은 표현).
    // 감김 순서는 직접 맞추지 않는다: 면 법선이 모양 안쪽 기준점(inside)에서 바깥을 향하도록 뒤집는다.
    sealed class Flat
    {
        readonly List<Vector3> _v = new List<Vector3>();
        readonly Func<Vector3, Vector3> _inside;
        public Flat(Func<Vector3, Vector3> inside) { _inside = inside; }
        public void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 centroid = (a + b + c) / 3f;
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), centroid - _inside(centroid)) < 0f) { var t = b; b = c; c = t; }
            _v.Add(a); _v.Add(b); _v.Add(c);
        }
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { Tri(a, b, c); Tri(a, c, d); }
        public Mesh Build()
        {
            var mesh = new Mesh();
            mesh.SetVertices(_v);
            mesh.SetTriangles(Enumerable.Range(0, _v.Count).ToArray(), 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }

    // 가리비: 경첩(−Z)에서 펼쳐지는 부채꼴 돔 + 방사형 골 9줄 + 경첩 귀 2개.
    static Mesh ScallopMesh()
    {
        var f = new Flat(c => c.y > .02f && c.z < -.25f ? new Vector3(c.x, -1f, c.z) : new Vector3(0, .035f, -.05f));
        const int rings = 5, spokes = 18; const float radius = .5f, spread = 62f * Mathf.Deg2Rad;
        Vector3 Point(int ring, int spoke)
        {
            float t = ring / (float)rings, a = Mathf.Lerp(-spread, spread, spoke / (float)spokes);
            float r = radius * t;
            float rib = spoke % 2 == 0 ? .028f : 0f;
            float y = .2f * Mathf.Sin(t * Mathf.PI * .5f + .35f) * (1f - .4f * t) + rib * t;
            return new Vector3(Mathf.Sin(a) * r, y, Mathf.Cos(a) * r - .3f);
        }
        for (int ring = 0; ring < rings; ring++)
            for (int spoke = 0; spoke < spokes; spoke++)
            {
                Vector3 a = Point(ring, spoke), b = Point(ring, spoke + 1), c = Point(ring + 1, spoke + 1), d = Point(ring + 1, spoke);
                if (ring == 0) f.Tri(a, c, d); else f.Quad(a, b, c, d);
            }
        // 바깥 가장자리 → 바닥면
        Vector3 hinge = new Vector3(0, 0, -.3f);
        for (int spoke = 0; spoke < spokes; spoke++)
        {
            Vector3 a = Point(rings, spoke), b = Point(rings, spoke + 1);
            Vector3 a0 = new Vector3(a.x, 0, a.z), b0 = new Vector3(b.x, 0, b.z);
            f.Quad(a, b, b0, a0);
            f.Tri(hinge, b0, a0);
        }
        // 경첩 귀
        foreach (float side in new[] { -1f, 1f })
        {
            Vector3 p0 = new Vector3(0, .05f, -.31f), p1 = new Vector3(side * .16f, .03f, -.3f), p2 = new Vector3(side * .13f, .03f, -.2f);
            f.Tri(p0, p1, p2);
        }
        return f.Build();
    }

    // 소라: 옆으로 누운 나선 원뿔(축 +X). 고리마다 조금씩 돌려 나선 결을 만들고, 앞쪽 세 고리에 돌기를 세운다.
    const int ConchSides = 7, ConchRings = 7;
    static Vector3 ConchPoint(int ring, int side)
    {
        float t = ring / (float)(ConchRings - 1);
        float r = Mathf.Lerp(.2f, .015f, Mathf.Pow(t, .8f));
        float a = side / (float)ConchSides * Mathf.PI * 2f + ring * .55f;
        bool knob = ring >= 1 && ring <= 3 && side % 2 == 0;
        float rr = r * (knob ? 1.28f : 1f);
        return new Vector3(Mathf.Lerp(-.3f, .34f, t), .2f + Mathf.Sin(a) * rr, Mathf.Cos(a) * rr);
    }

    static Mesh ConchMesh()
    {
        var f = new Flat(c => new Vector3(Mathf.Min(c.x, .3f), .2f, 0));
        for (int ring = 0; ring < ConchRings - 1; ring++)
            for (int side = 0; side < ConchSides; side++)
                f.Quad(ConchPoint(ring, side), ConchPoint(ring + 1, side), ConchPoint(ring + 1, side + 1), ConchPoint(ring, side + 1));
        Vector3 tip = new Vector3(.4f, .2f, 0);
        for (int side = 0; side < ConchSides; side++) f.Tri(ConchPoint(ConchRings - 1, side), tip, ConchPoint(ConchRings - 1, side + 1));
        return f.Build();
    }

    // 소라 입구: 넓은 끝을 덮는 분홍 나팔(입이 −X로 벌어짐).
    static Mesh ConchLipMesh()
    {
        var f = new Flat(c => new Vector3(-.1f, .2f, 0));
        Vector3 center = new Vector3(-.25f, .2f, 0);
        for (int side = 0; side < ConchSides; side++)
        {
            Vector3 a = ConchPoint(0, side), b = ConchPoint(0, side + 1);
            Vector3 fa = a + new Vector3(-.06f, (a.y - .2f) * .25f, a.z * .25f), fb = b + new Vector3(-.06f, (b.y - .2f) * .25f, b.z * .25f);
            f.Quad(a, fa, fb, b);
            f.Tri(center, fb, fa);
        }
        return f.Build();
    }

    // 불가사리: 다섯 팔 별, 가운데가 도톰하게 솟고 팔끝은 얇다.
    static Mesh StarMesh()
    {
        var f = new Flat(c => new Vector3(0, .03f, 0));
        const float outer = .5f, inner = .2f;
        Vector3 top = new Vector3(0, .1f, 0), bottom = Vector3.zero;
        var rim = new List<Vector3>();
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * Mathf.PI * 2f + Mathf.PI * .5f;
            bool tip = i % 2 == 0;
            float r = tip ? outer : inner;
            rim.Add(new Vector3(Mathf.Cos(a) * r, tip ? .02f : .06f, Mathf.Sin(a) * r));
        }
        for (int i = 0; i < 10; i++)
        {
            Vector3 a = rim[i], b = rim[(i + 1) % 10];
            // 팔 등줄: 팔끝과 중심 사이에 능선을 세워 면을 둘로 나눈다.
            Vector3 ridge = (i % 2 == 0 ? a : b) * .5f + new Vector3(0, .085f, 0);
            f.Tri(top, b, a);
            Vector3 a0 = new Vector3(a.x, 0, a.z), b0 = new Vector3(b.x, 0, b.z);
            f.Quad(a, b, b0, a0);
            f.Tri(bottom, a0, b0);
            if (i % 2 == 0) f.Tri(top, ridge, a); else f.Tri(top, b, ridge);
        }
        return f.Build();
    }

    // 목걸이 끈: 바닥에 놓인 가는 8각 고리.
    static Mesh CordMesh()
    {
        var f = new Flat(c => { var flat = new Vector3(c.x, 0, c.z); return (flat.sqrMagnitude > 1e-6f ? flat.normalized : Vector3.forward) * .24f + Vector3.up * .02f; });
        const int segments = 20, sides = 5; const float ring = .24f, thick = .02f;
        Vector3 P(int s, int k)
        {
            float a = s / (float)segments * Mathf.PI * 2f, b = k / (float)sides * Mathf.PI * 2f;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            return dir * (ring + Mathf.Cos(b) * thick) + Vector3.up * (thick + Mathf.Sin(b) * thick);
        }
        for (int s = 0; s < segments; s++)
            for (int k = 0; k < sides; k++)
                f.Quad(P(s, k), P(s, k + 1), P(s + 1, k + 1), P(s + 1, k));
        return f.Build();
    }
}
