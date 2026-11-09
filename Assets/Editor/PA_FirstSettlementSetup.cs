using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PA_FirstSettlementSetup
{
    public const string Root = "Assets/Resources/DepartureTutorial/Settlement";

    public static void FinalEvidence()
    {
        // The existing thin canvas must remain visible from both sides after 90-degree placement.
        string path = Root + "/PA_ShelterCanvas.mat";
        var canvas = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (canvas == null)
        {
            canvas = new Material(PA_DepartureContinuationSetup.Require<Material>("Assets/Resources/DepartureTutorial/Materials/PA_Cream.mat"));
            canvas.name = "PA_ShelterCanvas";
            AssetDatabase.CreateAsset(canvas, path);
        }
        canvas.SetFloat("_Cull", 0);
        canvas.doubleSidedGI = true;
        EditorUtility.SetDirty(canvas);
        string prefabPath = Root + "/PA_StarterShelter.prefab";
        var shelter = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            foreach (var renderer in shelter.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m.name == "PA_Cream" || m.name == "PA_ShelterCanvas" ? canvas : m).ToArray();
            PrefabUtility.SaveAsPrefabAsset(shelter, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(shelter); }
        AssetDatabase.SaveAssets();
        Debug.Log("[VS-P3] CANVAS_TWO_SIDED_PASS existing mesh and palette preserved");
        PA_FirstSettlementChecks.RunSavedEvidence();
    }
    [MenuItem("Project PA/Presentation/Configure First Settlement P3")]
    public static void Configure()
    {
        Directory.CreateDirectory(Root);
        AssetDatabase.Refresh();
        Build("PA_SettlementHub_Tier0", "Hub", new Vector3(3.6f,2.6f,3.3f));
        Build("PA_StarterShelter", "Shelter", new Vector3(3.2f,2.4f,3.1f));
        var root=PrefabUtility.LoadPrefabContents(PA_DepartureContinuationSetup.PrefabPath);
        try
        {
            if (root.GetComponent<FirstIslandSettlementController>() == null) root.AddComponent<FirstIslandSettlementController>();
            PrefabUtility.SaveAsPrefabAsset(root,PA_DepartureContinuationSetup.PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
        Debug.Log("[VS-P3] SETUP_PASS two wrappers + original continuation prefab");
    }

    static void Build(string modelName,string key,Vector3 collisionSize)
    {
        var source=PA_DepartureContinuationSetup.Require<GameObject>("Assets/Art/ProjectPA/Derived/Settlement/"+modelName+".fbx");
        var root=new GameObject(modelName);
        try
        {
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(source,root.transform);
            visual.name="Visual";visual.transform.localRotation=Quaternion.Euler(0,180,0);
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>
                    key == "Shelter" && m.name.Split('.')[0] == "PA_Cream" && AssetDatabase.LoadAssetAtPath<Material>(Root+"/PA_ShelterCanvas.mat") != null
                        ? AssetDatabase.LoadAssetAtPath<Material>(Root+"/PA_ShelterCanvas.mat")
                        : PA_DepartureContinuationSetup.Require<Material>("Assets/Resources/DepartureTutorial/Materials/"+m.name.Split('.')[0]+".mat")).ToArray();
            }
            var box=root.AddComponent<BoxCollider>();box.size=collisionSize;box.center=Vector3.up*(collisionSize.y/2);
            var entrance=new GameObject("InteractionAnchor");entrance.transform.SetParent(root.transform,false);entrance.transform.localPosition=new Vector3(-1,0,-3);
            if(key=="Shelter")
            {
                var accent=GameObject.CreatePrimitive(PrimitiveType.Cube);accent.name="ProfessionAccent";accent.transform.SetParent(root.transform,false);
                accent.transform.localPosition=new Vector3(.8f,.82f,-1.6f);accent.transform.localScale=new Vector3(.62f,.30f,.08f);
                accent.GetComponent<Collider>().enabled=false;
                accent.GetComponent<Renderer>().sharedMaterial=PA_DepartureContinuationSetup.Require<Material>("Assets/Resources/DepartureTutorial/Materials/PA_Terracotta.mat");
            }
            var renderers=root.GetComponentsInChildren<Renderer>();Bounds b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            if(b.size.x>4 || b.size.z>4 || Mathf.Abs(b.min.y)>.03f || b.size.y<2)throw new InvalidOperationException("Wrapper bounds invalid: "+modelName+" "+b);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/"+modelName+".prefab");
            var data=AssetDatabase.LoadAssetAtPath<BuildingData>(Root+"/"+key+".asset");
            if(data==null){data=ScriptableObject.CreateInstance<BuildingData>();AssetDatabase.CreateAsset(data,Root+"/"+key+".asset");}
            data.buildingName=key=="Hub"?"개척 관리 거점 Tier 0":"동행자 임시 거처";data.price=0;data.requiredTier=0;data.prefab=prefab;EditorUtility.SetDirty(data);
            Debug.Log("[VS-P3] WRAPPER_PASS "+modelName+" bounds="+b+" ground="+b.min.y+" footprint=4x4 entrance=-Z");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
