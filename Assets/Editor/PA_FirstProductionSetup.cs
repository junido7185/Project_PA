using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PA_FirstProductionSetup
{
    const string Root="Assets/Resources/DepartureTutorial/Worksites";
    public static void Configure()
    {
        Directory.CreateDirectory(Root);AssetDatabase.Refresh();
        foreach(string role in new[]{"Wood","Ore","Wheat"})
        {
            var root=new GameObject("PA_StarterWorksite_"+role);
            try
            {
                var source=PA_DepartureContinuationSetup.Require<GameObject>("Assets/Art/ProjectPA/Derived/Worksites/PA_StarterWorksite_"+role+".fbx");
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(source,root.transform);visual.transform.localRotation=Quaternion.Euler(0,180,0);
                foreach(var r in visual.GetComponentsInChildren<Renderer>()) r.sharedMaterials=r.sharedMaterials.Select(m=>PA_DepartureContinuationSetup.Require<Material>("Assets/Resources/DepartureTutorial/Materials/"+m.name.Split('.')[0]+".mat")).ToArray();
                var collider=root.AddComponent<BoxCollider>();collider.size=new Vector3(1.72f,.16f,1.72f);collider.center=Vector3.up*.08f;
                var target=new GameObject("InteractionTarget");target.transform.SetParent(root.transform,false);target.transform.localPosition=new Vector3(0,.7f,-.7f);
                var trigger=target.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(1.5f,1.3f,.65f);
                var entrance=new GameObject("InteractionAnchor");entrance.transform.SetParent(root.transform,false);entrance.transform.localPosition=Vector3.back*2;
                var renderers=root.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                if(bounds.size.x>2 || bounds.size.z>2 || Mathf.Abs(bounds.min.y)>.02f)throw new InvalidOperationException("Worksite bounds "+role+" "+bounds);
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/PA_StarterWorksite_"+role+".prefab");
                var data=AssetDatabase.LoadAssetAtPath<BuildingData>(Root+"/"+role+".asset");
                if(data==null){data=ScriptableObject.CreateInstance<BuildingData>();AssetDatabase.CreateAsset(data,Root+"/"+role+".asset");}
                data.buildingName=role+" 첫 작업터";data.price=0;data.requiredTier=0;data.prefab=prefab;EditorUtility.SetDirty(data);
                string tool=role=="Wood"?"AXE_STONE":role=="Ore"?"PICKAXE_STONE":"CHEST_CLOSED";
                var toolRoot=new GameObject("Tool_"+role);
                try
                {
                    var toolSource=PA_DepartureContinuationSetup.Require<GameObject>("Assets/Art/ProjectPA/Prefabs/Intake/PA_REF_CUBEWORLDKIT_"+tool+".prefab");
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(toolSource,toolRoot.transform);
                    var rr=instance.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);
                    instance.transform.localScale*= (role=="Wheat"?.32f:.8f)/Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
                    foreach(var col in instance.GetComponentsInChildren<Collider>())col.enabled=false;
                    PrefabUtility.SaveAsPrefabAsset(toolRoot,Root+"/Tool_"+role+".prefab");
                }
                finally{UnityEngine.Object.DestroyImmediate(toolRoot);}
                Debug.Log("[VS-P4] WRAPPER_PASS "+role+" "+bounds);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        var continuation=PrefabUtility.LoadPrefabContents(PA_DepartureContinuationSetup.PrefabPath);
        try
        {
            if(continuation.GetComponent<FirstProductionController>()==null)continuation.AddComponent<FirstProductionController>();
            PrefabUtility.SaveAsPrefabAsset(continuation,PA_DepartureContinuationSetup.PrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(continuation);}
        AssetDatabase.SaveAssets();Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
        ConfigureBindings();
        Debug.Log("[VS-P4] SETUP_PASS canonical Wood Ore Wheat");
    }

    // 아트·씬을 재생성하지 않고 기존 continuation의 작은 프로필 배열만 설정한다.
    [MenuItem("Project PA/Presentation/Configure P4 Worksite Bindings")]
    public static void ConfigureBindings()
    {
        var continuation = PrefabUtility.LoadPrefabContents(PA_DepartureContinuationSetup.PrefabPath);
        try
        {
            var flow = continuation.GetComponent<FirstProductionController>();
            if (flow == null) throw new InvalidOperationException("Missing P4 orchestrator.");
            // 이후 Inspector로 추가/수정한 프로필은 재실행으로 덮어쓰지 않는다.
            if (flow.worksiteProfiles == null || flow.worksiteProfiles.Length == 0)
            {
                flow.worksiteProfiles = new[]
                {
                    MakeProfile("Lumberjack_01", "Wood", NpcSpecialty.Lumberjack, WorksiteActivityKind.Gathering,
                        "p4-gather-", "정착 실습 원목", "공용 도끼로 원목 채집"),
                    MakeProfile("Miner_01", "Ore", NpcSpecialty.Miner, WorksiteActivityKind.Mining,
                        "p4-mine-", "정착 실습 광맥", "광맥 채굴 체험"),
                    MakeProfile("Farmer_01", "Wheat", NpcSpecialty.Farmer, WorksiteActivityKind.Farming,
                        "p4-farm-", "정착 실습 밭", "파종 / 수확 체험")
                };
                PrefabUtility.SaveAsPrefabAsset(continuation, PA_DepartureContinuationSetup.PrefabPath);
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(continuation); }
        AssetDatabase.SaveAssets();
        ValidateBindingProfiles();
    }

    static StarterWorksiteProfile MakeProfile(string companionId, string profileId, NpcSpecialty specialty,
        WorksiteActivityKind activity, string activityPrefix, string label, string prompt)
    {
        return new StarterWorksiteProfile
        {
            companionId = companionId, profileId = profileId, specialty = specialty, activityKind = activity,
            productionData = PA_DepartureContinuationSetup.Require<ProductionData>("Assets/Resources/Production/Production_" + profileId + ".asset"),
            buildingId = "PA_STARTER_" + profileId,
            buildingResourcePath = FirstProductionController.ResourceRoot + profileId,
            activityIdPrefix = activityPrefix, activityLabel = label, activityPrompt = prompt,
            activityItemResourcePath = activity == WorksiteActivityKind.Mining ? "Items/Item_Ore" : "",
            activityAmount = activity == WorksiteActivityKind.Mining ? 2 : 1,
            starterSeed = activity == WorksiteActivityKind.Farming ? PA_DepartureContinuationSetup.Require<Item>("Assets/Resources/Items/Item_15_Seed.asset") : null,
            toolPrefab = PA_DepartureContinuationSetup.Require<GameObject>(Root + "/Tool_" + profileId + ".prefab")
        };
    }

    [MenuItem("Project PA/Presentation/Validate P4 Worksite Profiles (No Play)")]
    public static void ValidateBindingProfiles()
    {
        var prefab = PA_DepartureContinuationSetup.Require<GameObject>(PA_DepartureContinuationSetup.PrefabPath);
        var profiles = prefab.GetComponent<FirstProductionController>().worksiteProfiles;
        var selection = prefab.GetComponent<DepartureCompanionSelection>();
        if (profiles == null || profiles.Any(p => p == null || !p.IsValid) ||
            profiles.Select(p => p.companionId).Distinct().Count() != profiles.Length)
            throw new InvalidOperationException("Missing, invalid or duplicate worksite profile.");
        var checks = new System.Collections.Generic.List<string>();
        foreach (var candidate in selection.candidates)
        {
            var profile = StarterWorksiteProfile.Find(profiles, candidate.id);
            if (profile == null || Resources.Load<BuildingData>(profile.buildingResourcePath)?.prefab == null ||
                profile.toolPrefab == null || FirstProductionController.Role(candidate.id) != profile.profileId ||
                FirstProductionController.Definition(candidate.id).StableId != profile.buildingId)
                throw new InvalidOperationException("Unresolved worksite profile: " + candidate.id);
            if (profile.activityKind == WorksiteActivityKind.Mining && Resources.Load<Item>(profile.activityItemResourcePath) == null)
                throw new InvalidOperationException("Missing mining resource: " + candidate.id);
            checks.Add("PASS serialized profile references and compatibility lookup: " + candidate.id + " -> " + profile.profileId);
        }
        for (int i = 0; i < selection.candidates.Length; i++)
            for (int j = i + 1; j < selection.candidates.Length; j++)
            {
                var pair = new[] { selection.candidates[i].id, selection.candidates[j].id };
                if (pair.Select(id => StarterWorksiteProfile.Find(profiles, id)).Distinct().Count() != 2)
                    throw new InvalidOperationException("Ambiguous selected pair.");
                checks.Add("PASS selected pair representable: " + string.Join(" + ", pair));
            }
        Directory.CreateDirectory("Logs/P4-R3");
        checks.Add("Unity Editor asset checks only; Play runs: 0. No scene/save/production lifecycle exercised.");
        File.WriteAllLines("Logs/P4-R3/asset-checks.txt", checks);
        Debug.Log("[P4-R3] PROFILE_CHECKS_PASS " + profiles.Length + " serialized profiles.");
    }
}
