using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Opening Canon §23 / D1: 기존 Item/RecipeData의 표현 참조만 변경한다.
// PreviewRenderUtility의 격리 Edit preview 사용. Play 중 Camera.Render 호출 금지.
public static class PA_DemoIconBake
{
    const string Root = "Assets/Art/ProjectPA/Derived/ItemPresentation";
    const string Intake = "Assets/Art/ProjectPA/Prefabs/Intake/PA_REF_";
    static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
    public static string LastEvidence { get; private set; }

    [MenuItem("Project PA/Art/Bake Unified Demo Icons")]
    public static void Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Icon bake requires Edit mode.");
        LastEvidence = "Logs/CodexDemoPolish/D1-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-Bake";
        Directory.CreateDirectory(LastEvidence);
        Directory.CreateDirectory(Root + "/Icons");
        Directory.CreateDirectory(Root + "/Models");
        Directory.CreateDirectory(Root + "/Materials");
        AssetDatabase.Refresh();
        var items = AssetDatabase.FindAssets("t:Item", new[] { "Assets/Resources" })
            .Select(g => AssetDatabase.GUIDToAssetPath(g)).Where(p => !p.Contains("/Blueprints/"))
            .Select(p => AssetDatabase.LoadAssetAtPath<Item>(p)).Where(i => i != null).OrderBy(i => i.id).ToArray();
        var contracts = items.ToDictionary(i => i, Contract);
        var art = FirstDayStudioAssets.Load();
        var rows = new List<string> { "id\titem\tmodel\ticon\tvisiblePixels" };
        foreach (var item in items)
        {
            string path = AssetDatabase.GetAssetPath(item);
            File.Copy(path, LastEvidence + "/" + item.name + ".before", false);
            GameObject model = Resolve(item, art);
            if (model == null) throw new InvalidOperationException("No local model for " + item.name);
            if (AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(model)).Any(p => p.Contains("animal-crossing-froggy-chair")))
                throw new InvalidOperationException("Unlicensed model cannot be baked: " + item.name);
            Texture2D icon = Render(model, item.id);
            var pixels = icon.GetPixels32();
            int visible = pixels.Count(c => c.a > 16);
            if (visible < 100 || visible > 63000 || pixels[0].a != 0)
                throw new InvalidOperationException("Invalid transparent icon for " + item.name + ": " + visible);
            string iconPath = Root + "/Icons/Item_" + item.id + ".png";
            File.WriteAllBytes(iconPath, icon.EncodeToPNG());
            Object.DestroyImmediate(icon);
            AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(iconPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();
            item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            item.model = model;
            if (Contract(item) != contracts[item]) throw new InvalidOperationException("Item data changed: " + item.name);
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssetIfDirty(item);
            rows.Add(item.id + "\t" + item.itemName + "\t" + AssetDatabase.GetAssetPath(model) + "\t" + iconPath + "\t" + visible);
        }
        int recipes = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:RecipeData", new[] { "Assets/Resources" }))
        {
            var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(AssetDatabase.GUIDToAssetPath(guid));
            if (recipe == null || recipe.outputItem == null || !contracts.ContainsKey(recipe.outputItem)) continue;
            File.Copy(AssetDatabase.GetAssetPath(recipe), LastEvidence + "/" + recipe.name + ".before", false);
            recipe.icon = recipe.outputItem.icon;
            EditorUtility.SetDirty(recipe);
            AssetDatabase.SaveAssetIfDirty(recipe);
            recipes++;
        }
        File.WriteAllLines(LastEvidence + "/icons.tsv", rows);
        File.WriteAllText(LastEvidence + "/result.txt", "PASS icons=" + items.Length + " recipes=" + recipes +
            "\n256px transparent orthographic, common 3/4 angle, warm key/fill/rim, projected bounds fit.\nItem non-presentation fields unchanged. GameView verification pending.\n");
        Debug.Log("[D1 Icon Bake] PASS " + LastEvidence);
    }

    static string Contract(Item i) => i.id + "|" + i.itemName + "|" + i.description + "|" + i.basePrice + "|" +
        i.maxStack + "|" + i.toolType + "|" + i.category + "|" + i.requiredTier + "|" +
        AssetDatabase.GetAssetPath(i.buildingToBuild) + "|" + AssetDatabase.GetAssetPath(i.cropPrefab);

    static GameObject Load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Intake + name + ".prefab");

    static GameObject Resolve(Item item, FirstDayStudioAssets art)
    {
        switch (item.id)
        {
            case 1: return Load("ULTIMATENATURE_WHEAT");
            case 2: return Load("FOODKIT_CARROT");
            case 5: return Load("FOODKIT_LOAF");
            case 6: return OwnedModel("IronIngots", BuildIngots);
            case 9: return OwnedModel("BakedPotato", BuildPotato);
            case 10: return OwnedModel("GrilledFish", go => {
                AddModel(go, Load("FOODKIT_PLATE"), Vector3.zero, .9f);
                AddModel(go, Load("FOODKIT_FISH"), new Vector3(0,.12f,0), .65f);
            });
            case 11: case 2021: return OwnedModel("WoodenChair", BuildChair);
            case 12: return OwnedModel("ToolBundle", go => {
                AddModel(go, art.axe, new Vector3(-.16f,0,0), 1f, -20);
                AddModel(go, art.pickaxe, new Vector3(.16f,0,.12f), 1f, 25);
            });
            case 13: return OwnedModel("FoldedClothes", BuildClothes);
            case 14: return OwnedModel("GardenHoe", go => {
                Part(go,"Handle",new Vector3(0,.55f,0),new Vector3(.07f,1.1f,.07f),Wood);
                Part(go,"Blade",new Vector3(0,1.06f,.12f),new Vector3(.28f,.07f,.34f),Metal);
            });
            case 15: return Load("FOODKIT_BAG");
            case 2005: return art.blueprint;
            default: return art.ModelFor(item);
        }
    }

    static Color Wood => new Color(.55f,.29f,.12f);
    static Color Metal => new Color(.42f,.54f,.58f);
    static GameObject OwnedModel(string name, Action<GameObject> build)
    {
        string path = Root + "/Models/" + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;
        var root = new GameObject(name);
        try { build(root); return PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { Object.DestroyImmediate(root); }
    }

    static void AddModel(GameObject root, GameObject source, Vector3 position, float height, float yaw = 0)
    {
        if (source == null) throw new InvalidOperationException("Missing local model in " + root.name);
        var child = Object.Instantiate(source, root.transform);
        child.transform.localRotation = Quaternion.Euler(0,yaw,0) * source.transform.localRotation;
        var rs = child.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds; foreach(var r in rs) b.Encapsulate(r.bounds);
        child.transform.localScale *= height / Mathf.Max(.01f, Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z)));
        child.transform.localPosition = position;
    }

    static Material Material(Color color)
    {
        string name = "Mat_" + ColorUtility.ToHtmlStringRGB(color);
        if (Materials.TryGetValue(name,out var found)) return found;
        string path = Root + "/Materials/" + name + ".mat";
        found = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (found == null)
        {
            found = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name=name, color=color };
            found.SetFloat("_Smoothness",.22f);
            AssetDatabase.CreateAsset(found,path);
        }
        Materials[name]=found; return found;
    }

    // 모서리를 깎은 8각 단면의 소유 메시. 외부 모델/primitive placeholder에 의존하지 않는다.
    static void Part(GameObject root,string name,Vector3 position,Vector3 size,Color color,float topScale=1f)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
        go.transform.SetParent(root.transform,false);go.transform.localPosition=position;
        string meshPath=Root+"/Models/"+root.name+"_"+name+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(mesh==null)
        {
            var ring=new[]{new Vector2(-.4f,-.5f),new Vector2(.4f,-.5f),new Vector2(.5f,-.4f),new Vector2(.5f,.4f),new Vector2(.4f,.5f),new Vector2(-.4f,.5f),new Vector2(-.5f,.4f),new Vector2(-.5f,-.4f)};
            var verts=new List<Vector3>();var tris=new List<int>();
            Action<Vector3,Vector3,Vector3> tri=(a,b,c)=>{int n=verts.Count;verts.Add(a);verts.Add(b);verts.Add(c);tris.Add(n);tris.Add(n+1);tris.Add(n+2);};
            Func<int,bool,Vector3> v=(i,top)=>new Vector3(ring[i%8].x*size.x*(top?topScale:1f),(top?.5f:-.5f)*size.y,ring[i%8].y*size.z*(top?topScale:1f));
            for(int i=0;i<8;i++)
            {
                tri(v(i,false),v(i,true),v(i+1,true));tri(v(i,false),v(i+1,true),v(i+1,false));
                tri(new Vector3(0,size.y*.5f,0),v(i+1,true),v(i,true));
                tri(new Vector3(0,-size.y*.5f,0),v(i,false),v(i+1,false));
            }
            mesh=new Mesh{name=root.name+"_"+name};mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,meshPath);
        }
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=Material(color);
    }

    static void BuildChair(GameObject go)
    {
        Part(go,"Seat",new Vector3(0,.49f,0),new Vector3(.62f,.11f,.57f),new Color(.70f,.43f,.20f));
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            Part(go,"Leg"+x+"_"+z,new Vector3(x*.23f,.24f,z*.21f),new Vector3(.075f,.49f,.075f),Wood);
        for(int x=-1;x<=1;x+=2)Part(go,"Upright"+x,new Vector3(x*.24f,.81f,.22f),new Vector3(.07f,.59f,.07f),Wood);
        Part(go,"BackTop",new Vector3(0,1.04f,.22f),new Vector3(.61f,.12f,.075f),new Color(.70f,.43f,.20f));
        Part(go,"BackRail",new Vector3(0,.84f,.22f),new Vector3(.52f,.10f,.06f),Wood);
    }
    static void BuildIngots(GameObject go)
    {
        Part(go,"BarA",new Vector3(-.17f,.11f,0),new Vector3(.28f,.22f,.72f),Metal,.78f);
        Part(go,"BarB",new Vector3(.17f,.11f,0),new Vector3(.28f,.22f,.72f),Metal,.78f);
        Part(go,"BarC",new Vector3(0,.33f,0),new Vector3(.28f,.22f,.72f),new Color(.55f,.66f,.69f),.78f);
    }
    static void BuildPotato(GameObject go)
    {
        Part(go,"Skin",new Vector3(0,.14f,0),new Vector3(.48f,.28f,.75f),new Color(.55f,.30f,.13f),.70f);
        Part(go,"OpenFlesh",new Vector3(0,.28f,0),new Vector3(.20f,.08f,.5f),new Color(.98f,.83f,.42f),.8f);
        Part(go,"Butter",new Vector3(0,.35f,0),new Vector3(.16f,.06f,.16f),new Color(1f,.94f,.61f));
    }
    static void BuildClothes(GameObject go)
    {
        Part(go,"FoldedTrousers",new Vector3(0,.06f,0),new Vector3(.68f,.12f,.54f),new Color(.24f,.32f,.42f));
        Part(go,"Shirt",new Vector3(0,.19f,0),new Vector3(.52f,.15f,.55f),new Color(.29f,.63f,.60f));
        Part(go,"Collar",new Vector3(0,.28f,.15f),new Vector3(.20f,.045f,.17f),new Color(.89f,.84f,.64f));
        Part(go,"Button",new Vector3(0,.28f,-.02f),new Vector3(.04f,.04f,.04f),new Color(.98f,.91f,.72f));
    }

    static Texture2D Render(GameObject model, int itemId)
    {
        var preview=new PreviewRenderUtility();
        var previewMeshes = new List<Mesh>();
        try
        {
            var clone=Object.Instantiate(model);
            // 카메라/조명은 공통. 길쭉한 모델의 대표 실루엣만 정규화한다.
            if (itemId == 8 || itemId == 3001 || itemId == 3002)
                clone.transform.rotation = Quaternion.Euler(0,90,0) * clone.transform.rotation;
            if (itemId == 2011)
            {
                clone.transform.rotation = Quaternion.Euler(0,0,-35) * clone.transform.rotation;
                clone.transform.localScale = Vector3.Scale(clone.transform.localScale,new Vector3(1.7f,1,1.7f));
            }
            // 앞뒷면이 정점을 공유해 법선이 상쇄된 로컬 종이 모델은 preview에서만 분리한다.
            foreach (var filter in clone.GetComponentsInChildren<MeshFilter>(true))
            {
                var source = filter.sharedMesh;
                if (source == null || !source.isReadable || !source.normals.Any(n => n.sqrMagnitude < .01f)) continue;
                var vertices = source.vertices; var uv = source.uv;
                var splitVertices = new List<Vector3>(); var splitUv = new List<Vector2>();
                var submeshes = new List<int[]>();
                for (int s = 0; s < source.subMeshCount; s++)
                {
                    var indices = source.GetTriangles(s); var split = new int[indices.Length];
                    for (int i = 0; i < indices.Length; i++)
                    {
                        split[i] = splitVertices.Count; splitVertices.Add(vertices[indices[i]]);
                        splitUv.Add(uv.Length == vertices.Length ? uv[indices[i]] : Vector2.zero);
                    }
                    submeshes.Add(split);
                }
                var mesh = new Mesh { name = source.name + "_IconPreview", subMeshCount = source.subMeshCount };
                mesh.SetVertices(splitVertices); mesh.SetUVs(0,splitUv);
                for (int s = 0; s < submeshes.Count; s++) mesh.SetTriangles(submeshes[s],s);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                filter.sharedMesh = mesh; previewMeshes.Add(mesh);
            }
            // 격리 preview에서는 모델 렌더러만 보인다. 프리팹의 gameplay component는 실행하지 않는다.
            foreach(var behaviour in clone.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled=false;
            foreach(var r in clone.GetComponentsInChildren<Renderer>(true))
                if(r is TrailRenderer || r is LineRenderer || r is ParticleSystemRenderer) r.enabled=false;
            preview.AddSingleGO(clone);
            var renderers=clone.GetComponentsInChildren<Renderer>().Where(r=>r.enabled && !(r is TrailRenderer) && !(r is LineRenderer)).ToArray();
            if(renderers.Length==0)throw new InvalidOperationException("No mesh in "+model.name);
            Bounds b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            var camera=preview.camera;camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
            camera.allowHDR=false;camera.allowMSAA=true;
            Quaternion angle=Quaternion.Euler(22f,35f,0);
            camera.transform.rotation=angle;
            float extent=0;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {var corner=Quaternion.Inverse(angle)*Vector3.Scale(b.extents,new Vector3(x,y,z));extent=Mathf.Max(extent,Mathf.Abs(corner.x),Mathf.Abs(corner.y));}
            float distance=Mathf.Max(2,b.size.magnitude*2);
            camera.transform.position=b.center-camera.transform.forward*distance;
            camera.orthographicSize=Mathf.Max(.01f,extent)*1.16f;camera.nearClipPlane=.01f;camera.farClipPlane=distance*3;
            preview.ambientColor=new Color(.32f,.34f,.36f);
            preview.lights[0].type=LightType.Directional;preview.lights[0].intensity=1.6f;preview.lights[0].color=new Color(1f,.86f,.69f);preview.lights[0].transform.rotation=Quaternion.Euler(35,-35,0);
            preview.lights[1].type=LightType.Directional;preview.lights[1].intensity=.85f;preview.lights[1].color=new Color(.75f,.88f,1f);preview.lights[1].transform.rotation=Quaternion.Euler(20,125,0);
            var rim=new GameObject("WarmRim").AddComponent<Light>();rim.type=LightType.Directional;rim.color=new Color(1f,.91f,.71f);rim.intensity=1.25f;rim.transform.rotation=Quaternion.Euler(25,210,0);preview.AddSingleGO(rim.gameObject);
            // EndStaticPreview는 RGB24로 복사해 알파를 버린다. 실제 preview RT를 RGBA로 읽는다.
            float points = 256f / EditorGUIUtility.pixelsPerPoint;
            preview.BeginPreview(new Rect(0,0,points,points), GUIStyle.none);
            preview.Render(true,true);
            var rendered = (RenderTexture)preview.EndPreview();
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = rendered;
                var image = new Texture2D(256,256,TextureFormat.RGBA32,false,false);
                image.ReadPixels(new Rect(0,0,256,256),0,0);
                // Preview RT는 UNorm 선형. PNG의 sRGB 색상으로 변환하되 알파는 보존한다.
                if (QualitySettings.activeColorSpace == ColorSpace.Linear && !rendered.sRGB)
                {
                    var colors = image.GetPixels();
                    for (int i = 0; i < colors.Length; i++) colors[i] = colors[i].gamma;
                    image.SetPixels(colors);
                }
                image.Apply();
                return image;
            }
            finally { RenderTexture.active = previous; }
        }
        finally { preview.Cleanup(); foreach (var mesh in previewMeshes) Object.DestroyImmediate(mesh); }
    }
}
