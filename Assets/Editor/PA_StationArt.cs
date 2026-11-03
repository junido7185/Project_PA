using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-11-02 사용자 "조리대·작업대 모델이 엄청 조잡하다(스킨 문제?)": B05·B06·B07 Tripo 생성 모델은 구운 텍스처가 줄무늬로 뭉개져
// 깨끗한 로우폴리 세계(상점·나무·소품)와 어긋난다. 같은 문법(모서리를 깎은 단순 덩어리 + 평면 색 + Kenney CC0 소품)으로
// 현장 작업대·조리대·화로 세 작업대를 직접 짓는다. 정면(작업면)은 −Z(배치 정의의 접근 방향)다.
// 생성물: Assets/Art/ProjectPA/Derived/Stations/*.prefab, Meshes/*.asset, Materials/*.mat (다시 실행하면 같은 경로를 덮어써 GUID 유지).
public static class PA_StationArt
{
    public const string Folder = "Assets/Art/ProjectPA/Derived/Stations";
    public const string WorkbenchPath = Folder + "/PA_Station_Workbench.prefab";
    public const string KitchenPath = Folder + "/PA_Station_Kitchen.prefab";
    public const string ForgePath = Folder + "/PA_Station_Forge.prefab";
    const string Kenney = "Assets/Art/External/Kenney/FoodKit/Models/";

    // 팔레트: 상점(InteriorWood .56/.37/.20, Rug teal)과 같은 계열.
    static readonly Color WoodTop = new Color(.72f, .48f, .27f), WoodMid = new Color(.58f, .37f, .20f), WoodDark = new Color(.40f, .25f, .13f);
    static readonly Color Bark = new Color(.42f, .29f, .18f), Cut = new Color(.86f, .70f, .47f);
    static readonly Color Stone = new Color(.66f, .64f, .60f), StoneDark = new Color(.47f, .46f, .45f), Mortar = new Color(.78f, .75f, .69f);
    static readonly Color Iron = new Color(.30f, .32f, .35f), Steel = new Color(.55f, .60f, .63f), Soot = new Color(.20f, .20f, .21f);
    static readonly Color Counter = new Color(.93f, .89f, .79f), Tile = new Color(.36f, .58f, .53f), Brass = new Color(.86f, .66f, .30f);

    [MenuItem("Project PA/Art/Build Station Models (Workbench · Kitchen · Forge)")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(Folder + "/Meshes");
        Directory.CreateDirectory(Folder + "/Materials");
        AssetDatabase.Refresh();
        Save("PA_Station_Workbench", BuildWorkbench, WorkbenchPath);
        Save("PA_Station_Kitchen", BuildKitchen, KitchenPath);
        Save("PA_Station_Forge", BuildForge, ForgePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[STATION-ART] built " + WorkbenchPath + ", " + KitchenPath + ", " + ForgePath);
    }

    // 데모 작업대 프리팹(현장 작업대·조리대·화로)의 Tripo 시각 자식을 새 모델("StationModel")로 바꾸고,
    // 키트 아이템 모델·아이콘·레시피 아이콘·FirstDay 카탈로그 참조를 같은 모델로 맞춘다. 레거시 B05~B08 경로는 건드리지 않는다.
    [MenuItem("Project PA/Art/Apply Station Models To Demo Stations")]
    public static void ApplyToStations()
    {
        BuildAll();
        var targets = new (string prefab, string model, string oldVisual)[]
        {
            ("Assets/Resources/DemoStructure/FieldWorkbench.prefab", WorkbenchPath, "B05_Workbench(Clone)"),
            ("Assets/Resources/DemoStructure/FieldKitchen.prefab", KitchenPath, "Visual"),
            ("Assets/Resources/DemoStructure/FieldForge.prefab", ForgePath, "Visual"),
        };
        var log = new List<string>();
        foreach (var (prefabPath, modelPath, oldVisual) in targets)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) { log.Add("skip (missing) " + prefabPath); continue; }
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                    if (child.name == oldVisual || child.name == "StationModel") Object.DestroyImmediate(child.gameObject);
                var station = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                station.name = "StationModel";
                station.transform.localPosition = Vector3.zero; station.transform.localRotation = Quaternion.identity; station.transform.localScale = Vector3.one;
                var renderers = station.GetComponentsInChildren<Renderer>();
                Bounds b = new Bounds(root.transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
                foreach (var r in renderers) { b.Encapsulate(root.transform.InverseTransformPoint(r.bounds.min)); b.Encapsulate(root.transform.InverseTransformPoint(r.bounds.max)); }
                var boxes = root.GetComponents<BoxCollider>();
                var box = boxes.Length > 0 ? boxes[0] : root.AddComponent<BoxCollider>();
                for (int i = 1; i < boxes.Length; i++) Object.DestroyImmediate(boxes[i]);
                box.center = b.center; box.size = b.size;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                log.Add($"{Path.GetFileNameWithoutExtension(prefabPath)} → StationModel box={b.size.ToString("F2")}");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        var workbench = AssetDatabase.LoadAssetAtPath<GameObject>(WorkbenchPath);
        var kitchen = AssetDatabase.LoadAssetAtPath<GameObject>(KitchenPath);
        var forge = AssetDatabase.LoadAssetAtPath<GameObject>(ForgePath);
        var art = AssetDatabase.LoadAssetAtPath<FirstDayStudioAssets>("Assets/Resources/FirstDayStudio/Assets.asset");
        if (art != null) { art.workbench = workbench; EditorUtility.SetDirty(art); log.Add("FirstDayStudio catalog.workbench → station model"); }
        foreach (var (itemPath, model) in new[] { ("Assets/Resources/FirstDayStudio/Item_2014.asset", workbench),
            ("Assets/Resources/DemoStructure/Item_FieldKitchenKit.asset", kitchen), ("Assets/Resources/DemoStructure/Item_FieldForgeKit.asset", forge) })
        {
            var item = AssetDatabase.LoadAssetAtPath<Item>(itemPath);
            if (item == null) continue;
            item.model = model;
            item.icon = PA_DemoIconBake.BakeIcon(item, model);
            EditorUtility.SetDirty(item);
            foreach (var guid in AssetDatabase.FindAssets("t:RecipeData", new[] { "Assets/Resources" }))
            {
                var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(AssetDatabase.GUIDToAssetPath(guid));
                if (recipe != null && recipe.outputItem == item) { recipe.icon = item.icon; EditorUtility.SetDirty(recipe); }
            }
            log.Add($"item {item.id} model+icon");
        }
        var catalog = AssetDatabase.LoadAssetAtPath<DemoPlaceableCatalog>("Assets/Resources/DemoStructure/Catalog.asset");
        if (catalog?.entries != null)
            foreach (var entry in catalog.entries.Where(e => e != null && (e.key == "field-kitchen" || e.key == "field-forge")))
            { entry.size = new Vector2Int(2, 1); log.Add(entry.key + " footprint 2×1"); }
        if (catalog != null) EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log("[STATION-ART] applied: " + string.Join(" | ", log));
    }

    public static void BatchApply()
    {
        int code = 0;
        try { ApplyToStations(); }
        catch (Exception e) { Debug.LogError("[STATION-ART] " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static void Save(string name, Action<GameObject> build, string path)
    {
        var root = new GameObject(name);
        try
        {
            build(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { Object.DestroyImmediate(root); }
    }

    // ---------- 현장 작업대: 목공 ----------
    static void BuildWorkbench(GameObject go)
    {
        Part(go, "Top", new Vector3(0, .86f, 0), new Vector3(1.8f, .12f, .86f), WoodTop);
        foreach (int x in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
            Part(go, $"Leg{x}{z}", new Vector3(x * .78f, .4f, z * .33f), new Vector3(.12f, .8f, .12f), WoodDark);
        Part(go, "Shelf", new Vector3(0, .24f, 0), new Vector3(1.62f, .06f, .7f), WoodMid);
        for (int i = 0; i < 3; i++)
            Part(go, "ShelfPlank" + i, new Vector3(-.1f + i * .05f, .3f + i * .045f, -.05f + i * .06f), new Vector3(1.15f, .04f, .22f), i % 2 == 0 ? Cut : WoodTop);
        // 뒤판과 걸어 둔 도구(딩컴식 작업대의 실루엣).
        Part(go, "BackPost0", new Vector3(-.82f, 1.25f, .36f), new Vector3(.09f, .82f, .09f), WoodDark);
        Part(go, "BackPost1", new Vector3(.82f, 1.25f, .36f), new Vector3(.09f, .82f, .09f), WoodDark);
        Part(go, "Pegboard", new Vector3(0, 1.3f, .38f), new Vector3(1.6f, .62f, .05f), WoodMid);
        Part(go, "PegboardTrim", new Vector3(0, 1.64f, .37f), new Vector3(1.72f, .07f, .09f), WoodDark);
        Part(go, "HangSawBlade", new Vector3(-.42f, 1.3f, .34f), new Vector3(.5f, .14f, .02f), Steel);
        Part(go, "HangSawHandle", new Vector3(-.12f, 1.31f, .34f), new Vector3(.12f, .17f, .04f), WoodTop);
        Part(go, "HangHammerHandle", new Vector3(.28f, 1.24f, .34f), new Vector3(.05f, .36f, .05f), WoodTop);
        Part(go, "HangHammerHead", new Vector3(.28f, 1.43f, .33f), new Vector3(.2f, .08f, .08f), Iron);
        Part(go, "HangSquareA", new Vector3(.6f, 1.36f, .34f), new Vector3(.05f, .28f, .02f), Brass);
        Part(go, "HangSquareB", new Vector3(.67f, 1.24f, .34f), new Vector3(.18f, .05f, .02f), Brass);
        // 작업면: 바이스·대패·못 상자.
        Part(go, "ViseBody", new Vector3(-.66f, .99f, -.36f), new Vector3(.24f, .14f, .18f), Iron);
        Part(go, "ViseJaw", new Vector3(-.66f, .99f, -.49f), new Vector3(.24f, .12f, .05f), Steel);
        Part(go, "ViseScrew", new Vector3(-.66f, .99f, -.55f), new Vector3(.3f, .03f, .03f), Steel);
        Part(go, "PlaneBody", new Vector3(.25f, .97f, -.12f), new Vector3(.34f, .1f, .12f), WoodTop);
        Part(go, "PlaneKnob", new Vector3(.36f, 1.05f, -.12f), new Vector3(.06f, .08f, .06f), WoodDark);
        Part(go, "Board", new Vector3(-.15f, .935f, .1f), new Vector3(.8f, .03f, .26f), Cut);
        Part(go, "NailBox", new Vector3(.66f, .97f, .12f), new Vector3(.24f, .1f, .18f), WoodMid);
        Part(go, "Nails", new Vector3(.66f, 1.02f, .12f), new Vector3(.18f, .02f, .12f), Steel);
    }

    // ---------- 조리대: 요리 ----------
    static void BuildKitchen(GameObject go)
    {
        // 왼쪽 찬장 + 상판
        Part(go, "Cabinet", new Vector3(-.45f, .43f, 0), new Vector3(1.6f, .86f, .9f), WoodMid);
        Part(go, "Plinth", new Vector3(-.45f, .04f, -.02f), new Vector3(1.62f, .08f, .9f), WoodDark);
        foreach (int i in new[] { 0, 1 })
        {
            Part(go, "Door" + i, new Vector3(-.85f + i * .8f, .45f, -.46f), new Vector3(.7f, .64f, .03f), WoodTop);
            Part(go, "Knob" + i, new Vector3(-.58f + i * .26f, .55f, -.49f), new Vector3(.05f, .05f, .04f), Brass);
        }
        Part(go, "CounterTop", new Vector3(-.45f, .9f, 0), new Vector3(1.72f, .08f, .98f), Counter);
        Part(go, "Backsplash", new Vector3(-.45f, 1.16f, .47f), new Vector3(1.72f, .44f, .05f), Tile);
        Part(go, "Shelf", new Vector3(-.45f, 1.42f, .4f), new Vector3(1.5f, .05f, .2f), WoodDark);
        // 오른쪽 돌 화덕 + 석쇠 + 불 아궁이
        Part(go, "StoveBody", new Vector3(.85f, .45f, 0), new Vector3(.95f, .9f, .94f), Stone);
        Part(go, "StoveBand", new Vector3(.85f, .7f, 0), new Vector3(.98f, .05f, .97f), Mortar);
        Part(go, "StoveTop", new Vector3(.85f, .93f, 0), new Vector3(1f, .06f, .98f), Soot);
        for (int i = 0; i < 6; i++)
            Part(go, "Grate" + i, new Vector3(.62f + i * .09f, .975f, 0), new Vector3(.03f, .02f, .7f), Iron);
        Part(go, "FireArch", new Vector3(.85f, .33f, -.46f), new Vector3(.58f, .4f, .04f), StoneDark);
        Ember(Part(go, "FireMouth", new Vector3(.85f, .3f, -.475f), new Vector3(.44f, .28f, .03f), Color.white));
        Part(go, "Chimney", new Vector3(1.02f, 1.5f, .3f), new Vector3(.26f, 1.1f, .26f), Soot, .9f);
        Part(go, "ChimneyCap", new Vector3(1.02f, 2.08f, .3f), new Vector3(.36f, .06f, .36f), Iron);
        Prop(go, "pot", new Vector3(1.02f, .96f, -.16f), .36f);
        Prop(go, "fish", new Vector3(.66f, .99f, -.02f), .34f, 90f);
        Prop(go, "cutting-board", new Vector3(-.72f, .94f, -.08f), .5f, 15f);
        Prop(go, "plate", new Vector3(-.12f, .94f, .12f), .32f);
        Prop(go, "plate", new Vector3(-.12f, .965f, .12f), .32f);
        FireLight(go, new Vector3(.85f, .35f, -.62f));
    }

    // ---------- 화로: 제련·도구 ----------
    static void BuildForge(GameObject go)
    {
        Part(go, "FurnaceBase", new Vector3(-.55f, .14f, 0), new Vector3(1.32f, .28f, 1.02f), StoneDark);
        Part(go, "FurnaceBody", new Vector3(-.55f, .72f, 0), new Vector3(1.16f, .9f, .92f), Stone, .86f);
        Part(go, "FurnaceBand", new Vector3(-.55f, .9f, 0), new Vector3(1.12f, .05f, .9f), Mortar);
        Part(go, "MouthArch", new Vector3(-.55f, .58f, -.43f), new Vector3(.6f, .46f, .05f), StoneDark);
        Ember(Part(go, "Mouth", new Vector3(-.55f, .55f, -.45f), new Vector3(.44f, .3f, .03f), Color.white));
        Part(go, "Lip", new Vector3(-.55f, .36f, -.5f), new Vector3(.66f, .06f, .14f), Mortar);
        Part(go, "Hood", new Vector3(-.55f, 1.3f, .02f), new Vector3(.92f, .3f, .76f), StoneDark, .55f);
        Part(go, "Chimney", new Vector3(-.55f, 1.9f, .04f), new Vector3(.36f, .92f, .36f), Stone, .86f);
        Part(go, "ChimneyRim", new Vector3(-.55f, 2.36f, .04f), new Vector3(.4f, .06f, .4f), StoneDark);
        Part(go, "Coal0", new Vector3(-1.02f, .32f, -.42f), new Vector3(.16f, .1f, .14f), Soot);
        Part(go, "Coal1", new Vector3(-.93f, .34f, -.5f), new Vector3(.12f, .09f, .12f), Soot);
        // 모루 + 그루터기 + 망치
        Part(go, "Stump", new Vector3(.62f, .28f, -.05f), new Vector3(.5f, .56f, .5f), Bark);
        Part(go, "StumpTop", new Vector3(.62f, .565f, -.05f), new Vector3(.48f, .02f, .48f), Cut);
        Part(go, "AnvilFoot", new Vector3(.62f, .62f, -.05f), new Vector3(.3f, .1f, .24f), Iron);
        Part(go, "AnvilWaist", new Vector3(.62f, .72f, -.05f), new Vector3(.17f, .12f, .15f), Iron);
        Part(go, "AnvilFace", new Vector3(.62f, .82f, -.05f), new Vector3(.52f, .1f, .25f), Steel);
        var horn = Part(go, "AnvilHorn", new Vector3(.94f, .825f, -.05f), new Vector3(.12f, .18f, .12f), Steel, .25f);
        horn.transform.localRotation = Quaternion.Euler(0, 0, -90f);
        Part(go, "HammerHandle", new Vector3(.55f, .9f, -.16f), new Vector3(.3f, .04f, .04f), WoodTop);
        Part(go, "HammerHead", new Vector3(.69f, .91f, -.16f), new Vector3(.07f, .08f, .14f), Iron);
        // 담금질 물통·철괴 더미
        // 담금질 물통(세워 둔 나무 통 + 쇠 테 + 물).
        Part(go, "Tub", new Vector3(1.1f, .27f, .28f), new Vector3(.44f, .54f, .44f), WoodMid, 1.08f);
        Part(go, "TubHoopLow", new Vector3(1.1f, .12f, .28f), new Vector3(.46f, .04f, .46f), Iron);
        Part(go, "TubHoopHigh", new Vector3(1.1f, .44f, .28f), new Vector3(.48f, .04f, .48f), Iron);
        Part(go, "Water", new Vector3(1.1f, .52f, .28f), new Vector3(.4f, .02f, .4f), new Color(.38f, .6f, .66f));
        Part(go, "IngotA", new Vector3(.28f, .05f, .34f), new Vector3(.16f, .1f, .34f), Steel, .8f);
        Part(go, "IngotB", new Vector3(.12f, .05f, .34f), new Vector3(.16f, .1f, .34f), Steel, .8f);
        Part(go, "IngotC", new Vector3(.2f, .15f, .34f), new Vector3(.16f, .1f, .34f), new Color(.64f, .69f, .72f), .8f);
        FireLight(go, new Vector3(-.55f, .55f, -.66f));
    }

    // ---------- 부품 ----------
    // 모서리를 깎은 8각 단면 덩어리(PA_DemoIconBake 소유 메시와 같은 문법). 같은 경로의 메시는 내용만 바꿔 GUID를 유지한다.
    static GameObject Part(GameObject root, string name, Vector3 position, Vector3 size, Color color, float topScale = 1f)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = position;
        string meshPath = Folder + "/Meshes/" + root.name + "_" + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        bool created = mesh == null;
        if (created) mesh = new Mesh { name = root.name + "_" + name };
        Bevelled(mesh, size, topScale);
        if (created) AssetDatabase.CreateAsset(mesh, meshPath); else EditorUtility.SetDirty(mesh);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = Flat(color);
        return go;
    }

    // 상하 면은 8각, 옆면은 평면 법선(각진 로우폴리 음영). 위 면만 topScale로 줄여 굴뚝·후드처럼 좁힌다.
    static void Bevelled(Mesh mesh, Vector3 size, float topScale)
    {
        var ring = new[] { new Vector2(-.42f, -.5f), new Vector2(.42f, -.5f), new Vector2(.5f, -.42f), new Vector2(.5f, .42f),
            new Vector2(.42f, .5f), new Vector2(-.42f, .5f), new Vector2(-.5f, .42f), new Vector2(-.5f, -.42f) };
        var verts = new List<Vector3>(); var tris = new List<int>();
        void Tri(Vector3 a, Vector3 b, Vector3 c) { int n = verts.Count; verts.Add(a); verts.Add(b); verts.Add(c); tris.Add(n); tris.Add(n + 1); tris.Add(n + 2); }
        Vector3 V(int i, bool top) => new Vector3(ring[i % 8].x * size.x * (top ? topScale : 1f), (top ? .5f : -.5f) * size.y, ring[i % 8].y * size.z * (top ? topScale : 1f));
        for (int i = 0; i < 8; i++)
        {
            Tri(V(i, false), V(i, true), V(i + 1, true)); Tri(V(i, false), V(i + 1, true), V(i + 1, false));
            Tri(new Vector3(0, size.y * .5f, 0), V(i + 1, true), V(i, true));
            Tri(new Vector3(0, -size.y * .5f, 0), V(i, false), V(i + 1, false));
        }
        mesh.Clear();
        mesh.SetVertices(verts); mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
    }

    static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
    static Material Flat(Color color)
    {
        string name = "Station_" + ColorUtility.ToHtmlStringRGB(color);
        if (Cache.TryGetValue(name, out var found) && found != null) return found;
        string path = Folder + "/Materials/" + name + ".mat";
        found = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (found == null)
        {
            found = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            found.SetFloat("_Smoothness", .18f);
            AssetDatabase.CreateAsset(found, path);
        }
        return Cache[name] = found;
    }

    // 불 아궁이: 낮에도 보이는 주황 발광(블룸은 약하게 걸리는 정도).
    static void Ember(GameObject part)
    {
        string path = Folder + "/Materials/Station_Ember.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Station_Ember" };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = new Color(1f, .52f, .18f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(1f, .45f, .12f) * 1.6f);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(material);
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        part.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void FireLight(GameObject root, Vector3 position)
    {
        var light = new GameObject("FireLight").AddComponent<Light>();
        light.transform.SetParent(root.transform, false);
        light.transform.localPosition = position;
        light.type = LightType.Point; light.color = new Color(1f, .6f, .28f);
        light.intensity = 1.1f; light.range = 2.6f; light.shadows = LightShadows.None;
    }

    static void Prop(GameObject root, string model, Vector3 position, float height, float yaw = 0f)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Kenney + model + ".fbx");
        if (source == null) throw new InvalidOperationException("Missing Kenney CC0 model: " + model);
        var child = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
        child.name = "Prop_" + model;
        child.transform.localRotation = Quaternion.Euler(0, yaw, 0) * source.transform.localRotation;
        var renderers = child.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
        child.transform.localScale *= height / Mathf.Max(.01f, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)));
        renderers = child.GetComponentsInChildren<Renderer>();
        b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
        // 바닥이 놓일 높이에 맞춘다(원점이 모델 중심인 Kenney 모델 대응).
        child.transform.position += root.transform.TransformPoint(position) - new Vector3(b.center.x, b.min.y, b.center.z);
        foreach (var collider in child.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
    }
}
