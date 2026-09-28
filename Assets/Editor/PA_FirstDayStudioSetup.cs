using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Canon v2 PASS 1. Creates owned reference assets; never rewrites imported art or authored scenes.
public static class PA_FirstDayStudioSetup
{
    const string Root="Assets/Resources/FirstDayStudio";
    // Canon v2 §4: 실제 로컬 모델의 Editor preview를 기존 Item.icon에 연결한다.
    public static async System.Threading.Tasks.Task PrepareItemIcons()
    {
        var art = FirstDayStudioAssets.Load();
        var items = art.supplies.Concat(DemoPlaceableCatalog.Load().entries.Select(e => e.item))
            .Concat(new[] { Resources.Load<Item>("Items/Item_Wood"), Resources.Load<Item>("Items/Item_Ore"),
                Resources.Load<Item>("Items/Item_Fish"), Resources.Load<Item>("Items/Item_Butterfly") })
            .Where(i => i != null).Distinct().ToArray();
        string folder = Root + "/Icons"; Directory.CreateDirectory(folder);
        string evidence = "Logs/VisualQA/Continuation-20260926/T4";
        Directory.CreateDirectory(evidence);
        foreach (var item in items)
        {
            if (item.icon != null) continue;
            string assetPath = AssetDatabase.GetAssetPath(item);
            File.Copy(assetPath, evidence + "/" + Path.GetFileName(assetPath) + ".before", false);
            var model = art.ModelFor(item);
            Texture2D preview = null;
            for (int attempt = 0; attempt < 30 && preview == null; attempt++)
            {
                preview = AssetPreview.GetAssetPreview(model);
                if (preview == null) await System.Threading.Tasks.Task.Delay(200);
            }
            if (preview == null) throw new InvalidOperationException("No model preview for " + item.name);
            string path = folder + "/Item_" + item.id + ".png";
            File.WriteAllBytes(path, preview.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
            item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(path); EditorUtility.SetDirty(item);
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText(evidence + "/icons.txt", "Prepared missing local-model icons: " + items.Count(i => i.icon != null) + "/" + items.Length);
    }
    [MenuItem("Tools/Project PA/First Day Studio/Prepare Local Assets")]
    public static void Prepare()
    {
        Directory.CreateDirectory(Root); Directory.CreateDirectory("Logs/FirstDayStudio");
        AssetDatabase.Refresh();
        var catalog=AssetDatabase.LoadAssetAtPath<FirstDayStudioAssets>(Root+"/Assets.asset");
        if(catalog==null){catalog=ScriptableObject.CreateInstance<FirstDayStudioAssets>();AssetDatabase.CreateAsset(catalog,Root+"/Assets.asset");}
        GameObject Load(string path)=>AssetDatabase.LoadAssetAtPath<GameObject>(path)??throw new Exception("Missing local asset: "+path);
        string intake="Assets/Art/ProjectPA/Prefabs/Intake/PA_REF_";
        catalog.player=Load("Assets/Art/Character/C-01.fbx");
        catalog.boat=Load("Assets/Resources/DepartureTutorial/Visuals/PA_DepartureBoat.prefab");
        catalog.fruit=Load("Assets/Resources/DepartureTutorial/Visuals/PA_DepartureFruit.prefab");
        catalog.dock=Load(intake+"CUTEFISH_DOCK_LONG_NOROPE.prefab");
        catalog.axe=Load(intake+"CUBEWORLDKIT_AXE_STONE.prefab");
        catalog.pickaxe=Load(intake+"CUBEWORLDKIT_PICKAXE_STONE.prefab");
        catalog.rod=Load(intake+"CUTEFISH_FISHINGROD_LVL1.prefab");
        catalog.chestClosed=Load(intake+"CUBEWORLDKIT_CHEST_CLOSED.prefab");
        catalog.chestOpen=Load(intake+"CUBEWORLDKIT_CHEST_OPEN.prefab");
        string nature="Assets/Art/Ultimate Nature Pack - Jun 2019/FBX/";
        catalog.trees=new[]{"BirchTree_1","BirchTree_2","BirchTree_3","PineTree_1","PineTree_2"}.Select(n=>Load(nature+n+".fbx")).ToArray();
        catalog.rocks=new[]{"Rock_1","Rock_2","Rock_3","Rock_Moss_1"}.Select(n=>Load(nature+n+".fbx")).ToArray();
        catalog.grasses=new[]{"Grass","Grass_2","Grass_Short"}.Select(n=>Load(nature+n+".fbx")).ToArray();
        catalog.flowers=new[]{Load(nature+"Flowers.fbx")};
        catalog.bushes=new[]{Load(nature+"Bush_1.fbx"),Load(nature+"Bush_2.fbx")};
        catalog.workbench=Load("Assets/Models/Buildings/B05_Workbench.fbx");
        catalog.wood=Load(intake+"ULTIMATENATURE_WOODLOG.prefab");
        catalog.blueprint=MakeBlueprint();
        catalog.phone=MakePhone();

        // Full local AssetDatabase pass includes ignored/uncommitted imports and subasset names.
        var matcher=new Regex(@"(^|[_\s\-])(net|bugnet|insectnet|butterflynet)([_\s\-]|$)|fishingnet|butterfly.?net|bug.?net|insect.?net|곤충망|채집망|잠자리채",RegexOptions.IgnoreCase);
        var report=new List<string>();GameObject net=null;
        string[] paths=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!p.StartsWith(Root+"/")).ToArray();
        int inspected=0;
        foreach(string path in paths)
        {
            string ext=Path.GetExtension(path).ToLowerInvariant();
            if(ext!=".prefab"&&!(AssetImporter.GetAtPath(path) is ModelImporter))continue;
            inspected++;
            var objects=AssetDatabase.LoadAllAssetsAtPath(path);
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            bool namedChild=model!=null&&model.GetComponentsInChildren<Transform>(true).Any(t=>matcher.IsMatch(t.name));
            bool namedMesh=model!=null&&model.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh!=null&&matcher.IsMatch(f.sharedMesh.name));
            if(!matcher.IsMatch(Path.GetFileNameWithoutExtension(path))&&!objects.Any(o=>o!=null&&matcher.IsMatch(o.name))&&!namedChild&&!namedMesh)continue;
            report.Add(path+" | "+string.Join(",",objects.Where(o=>o!=null&&matcher.IsMatch(o.name)).Select(o=>o.name)));
            if(model!=null && model.GetComponentsInChildren<Renderer>(true).Length>0 && net==null)net=model;
        }
        report.Insert(0,"AssetDatabase model/prefab paths inspected="+inspected);
        report.Add(net!=null?"Selected local net="+AssetDatabase.GetAssetPath(net):"No matching local net model/subasset. Minimal mesh substitute authored with existing palette.");
        File.WriteAllLines("Logs/FirstDayStudio/net-asset-search.txt",report);
        catalog.net=net!=null?net:MakeNet();
        catalog.butterfly=MakeButterfly();
        catalog.fish=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.FindAssets("Tuna t:GameObject").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault()??"");
        catalog.supplies=new[]{
            ItemAsset(2010,"스마트폰",ToolType.None,catalog.phone,1),
            Resources.Load<Item>("Items/Item_Axe"),Resources.Load<Item>("Items/Item_Pickaxe"),
            ItemAsset(2011,"낚싯대",ToolType.FishingRod,catalog.rod,1),Resources.Load<Item>("Items/Item_Net"),
            ItemAsset(2012,"상점 설계도",ToolType.None,catalog.blueprint,1),
            ItemAsset(2013,"텐트 설계도",ToolType.None,catalog.blueprint,2),
            ItemAsset(2014,"작업대 키트",ToolType.None,catalog.workbench,1)};
        if(catalog.supplies.Any(i=>i==null))throw new Exception("Existing starter item missing");
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        File.WriteAllText("Logs/FirstDayStudio/setup.txt","PASS Local assets prepared; no scene/SaveData/persistence edits.\n");
        Debug.Log("[FIRST-DAY] Local asset setup complete.");
    }
    static Item ItemAsset(int id,string name,ToolType tool,GameObject model,int stack)
    {
        string path=Root+"/Item_"+id+".asset";
        var item=AssetDatabase.LoadAssetAtPath<Item>(path);
        if(item==null){item=ScriptableObject.CreateInstance<Item>();AssetDatabase.CreateAsset(item,path);}
        item.id=id;item.itemName=name;item.toolType=tool;item.model=model;item.maxStack=stack;item.basePrice=0;
        item.category=ItemCategory.Tool;item.description="P.A.에서 준비한 새 섬 생활을 위한 보급품.";
        EditorUtility.SetDirty(item);return item;
    }
    static Material Mat(string name,Color color)
    {
        string path=Root+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Smoothness",.15f);EditorUtility.SetDirty(m);return m;
    }
    static GameObject MeshPrefab(string name,List<Vector3> vertices,List<int> triangles,Material material)
    {
        string path=Root+"/"+name;
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path+".asset");
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path+".asset");}
        mesh.Clear();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var root=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));root.GetComponent<MeshFilter>().sharedMesh=mesh;root.GetComponent<MeshRenderer>().sharedMaterial=material;
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,path+".prefab");Object.DestroyImmediate(root);return prefab;
    }
    static void Rod(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,float radius)
    {
        Vector3 axis=(b-a).normalized;Vector3 right=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.forward:Vector3.up).normalized;
        Vector3 up=Vector3.Cross(axis,right);int start=v.Count;
        for(int end=0;end<2;end++)for(int i=0;i<6;i++)v.Add((end==0?a:b)+(right*Mathf.Cos(i*Mathf.PI/3)+up*Mathf.Sin(i*Mathf.PI/3))*radius);
        for(int i=0;i<6;i++){int n=(i+1)%6;t.AddRange(new[]{start+i,start+n,start+6+i,start+n,start+6+n,start+6+i});}
    }
    static GameObject MakeNet()
    {
        var v=new List<Vector3>();var t=new List<int>();Rod(v,t,Vector3.zero,new Vector3(0,.75f,0),.022f);
        for(int i=0;i<24;i++)
        {float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Rod(v,t,new Vector3(Mathf.Cos(a)*.24f,.92f+Mathf.Sin(a)*.24f,0),new Vector3(Mathf.Cos(b)*.24f,.92f+Mathf.Sin(b)*.24f,0),.012f);}
        for(int i=-3;i<=3;i++)
        {float x=i*.055f,h=Mathf.Sqrt(.22f*.22f-x*x);Rod(v,t,new Vector3(x,.92f-h,0),new Vector3(x,.92f+h,0),.004f);Rod(v,t,new Vector3(-h,.92f+x,0),new Vector3(h,.92f+x,0),.004f);}
        return MeshPrefab("PA_StarterNet",v,t,Mat("PA_SupplyWood",new Color(.76f,.59f,.35f)));
    }
    static GameObject MakeButterfly()
    {
        var v = new List<Vector3>(); var t = new List<int>();
        // 앞뒷면 정점을 분리해야 RecalculateNormals에서 반대 법선이 상쇄되지 않는다.
        var outline = new[] { new Vector3(0,0,.08f), new Vector3(.22f,.04f,.43f),
            new Vector3(.52f,.09f,.4f), new Vector3(.6f,.1f,.18f),
            new Vector3(.34f,.06f,-.04f), new Vector3(.42f,.07f,-.24f),
            new Vector3(.2f,.03f,-.34f), new Vector3(0,0,-.12f) };
        foreach (int side in new[] { -1, 1 })
        foreach (bool back in new[] { false, true })
        {
            int start = v.Count;
            foreach (var point in outline) v.Add(new Vector3(point.x * side, point.y, point.z));
            for (int i = 1; i < outline.Length - 1; i++)
                if (back ^ (side < 0)) t.AddRange(new[] { start, start + i + 1, start + i });
                else t.AddRange(new[] { start, start + i, start + i + 1 });
        }
        Rod(v, t, new Vector3(0,0,-.18f), new Vector3(0,0,.24f), .025f);
        return MeshPrefab("PA_MeadowButterfly",v,t,Mat("PA_WingGold",new Color(1,.65f,.2f)));
    }

    public static void RepairButterflyVisual()
    {
        MakeButterfly();
        AssetDatabase.SaveAssets();
    }
    static GameObject MakePhone()
    {
        var v=new List<Vector3>{new Vector3(-.2f,0,0),new Vector3(.2f,0,0),new Vector3(.2f,.7f,0),new Vector3(-.2f,.7f,0)};
        var prefab=MeshPrefab("PA_Smartphone",v,new List<int>{0,2,1,0,3,2,1,2,0,2,3,0},Mat("PA_PhoneCoral",new Color(.94f,.4f,.32f)));
        return prefab;
    }
    static GameObject MakeBlueprint()
    {
        var v=new List<Vector3>{new Vector3(-.23f,0,0),new Vector3(.23f,0,0),new Vector3(.23f,.6f,0),new Vector3(-.23f,.6f,0)};
        var t=new List<int>{0,2,1,0,3,2,1,2,0,2,3,0};
        Rod(v,t,new Vector3(-.25f,0,0),new Vector3(.25f,0,0),.045f);
        Rod(v,t,new Vector3(-.25f,.6f,0),new Vector3(.25f,.6f,0),.045f);
        return MeshPrefab("PA_RolledBlueprint",v,t,Mat("PA_BlueprintPaper",new Color(.53f,.78f,.8f)));
    }
}
