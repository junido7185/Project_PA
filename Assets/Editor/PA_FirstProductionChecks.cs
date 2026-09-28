using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class PA_FirstProductionChecks
{
    const string Key="PA.FirstProduction.Checks";
    const string Output="Docs/Presentation/2026-09-09/P4";
    const string Saves="Logs/VS_PRESENT_001/P4/ValidationSave";
    static Task _task; static Keyboard _keyboard; static double _start;
    static PA_FirstProductionChecks()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=mode=>{
            if(!SessionState.GetBool(Key,false))return;
            if(mode==PlayModeStateChange.EnteredPlayMode){_task=null;_start=EditorApplication.timeSinceStartup;SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;}
            if(mode==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key+".Done",false))
            {
                bool fail=SessionState.GetBool(Key+".Error",false);SessionState.SetBool(Key,false);
                Debug.Log(fail?"[VS-P4] VALIDATION_FAIL":"[VS-P4] P4_VALIDATION_PASS");
                if(Environment.GetCommandLineArgs().Contains("-executeMethod"))EditorApplication.Exit(fail?1:0);
            }
        };
        Application.logMessageReceived+=(message,stack,type)=>{
            if(SessionState.GetBool(Key,false) && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert))SessionState.SetBool(Key+".Error",true);
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);Directory.CreateDirectory(Saves);
        SessionState.SetBool(Key,true);SessionState.SetBool(Key+".Done",false);SessionState.SetBool(Key+".Error",false);
        EditorSceneManager.OpenScene("Assets/Scenes/PA_DepartureTutorial.unity");
        PA_DepartureContinuationChecks.ConfigureGameView();
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false) || SessionState.GetBool(Key+".Done",false) || !EditorApplication.isPlaying)return;
        var p=Object.FindFirstObjectByType<FirstProductionController>();
        if(_task==null && p!=null && p.Settlement.Selection.Tutorial!=null && p.Settlement.Selection.Tutorial.IsReady)_task=Checks(p);
        if(_task!=null && _task.IsCompleted)
        {
            if(_task.IsFaulted){Debug.LogError("[VS-P4] "+_task.Exception.GetBaseException());SessionState.SetBool(Key+".Error",true);}
            Finish();
        }
        else if(EditorApplication.timeSinceStartup-_start>480){Debug.LogError("[VS-P4] bounded timeout");Finish();}
    }
    static void Finish()
    {
        if(_keyboard!=null && _keyboard.added)InputSystem.RemoveDevice(_keyboard);
        SessionState.SetBool(Key+".Done",true);Application.runInBackground=SessionState.GetBool(Key+".Background",false);EditorApplication.ExitPlaymode();
    }
    static async Task Checks(FirstProductionController p)
    {
        _keyboard=InputSystem.AddDevice<Keyboard>("P4ValidationKeyboard");_keyboard.MakeCurrent();
        var pairs=new[]{new[]{"Miner_01","Farmer_01"},new[]{"Lumberjack_01","Miner_01"},new[]{"Lumberjack_01","Farmer_01"}};
        for(int pair=0;pair<3;pair++)
        {
            if(pair>0){await FreshScene();p=Object.FindFirstObjectByType<FirstProductionController>();}
            var s=p.Settlement;var selection=s.Selection;
            selection.OpenDevelopmentSelection();
            foreach(var id in pairs[pair])Check(selection.Toggle(id),"select canonical "+id);
            selection.DepartureButton.onClick.Invoke();
            var companions=s.Voyage.Companions.ToArray();
            await Until(()=>s.IsReady,"P1 -> actual P2 voyage arrival");
            for(int i=0;i<3;i++)
            {
                s.Begin(s.BuildingIds[i]);
                var cell=i==0?new Vector2Int(6,5):i==1?new Vector2Int(9,4):new Vector2Int(8,7);
                var result=s.PreviewAt(cell,i==1?1:0);Check(result.Succeeded && s.Commit(),"P3 canonical hub/shelter placement "+i);
            }
            await Until(()=>p.IsReady,"P3 completion unlocks P4");
            Check(p.Owners.SequenceEqual(pairs[pair]) && s.Voyage.Companions.SequenceEqual(companions),"same selected IDs and original companion objects");
            var save=s.EnsureSaveManager();ConfigureRepository(save);
            int money=EconomyService.Instance.Money;
            foreach(string owner in p.Owners)
            {
                string id=FirstProductionController.WorksiteId(owner);s.Begin(id);
                Check(!s.PreviewAt(new Vector2Int(10,6),0).Succeeded,"worksite rejects natural overlap");
                var cell=FindValid(s,id,owner==p.Owners[0]?new Vector2Int(5,4):new Vector2Int(8,4));
                Check(s.PreviewAt(cell,0).Succeeded && s.Commit(),"free grid worksite "+owner);
                await Until(()=>p.Sites.ContainsKey(owner),"worksite owner binding");
            }
            Check(s.Placement.RegisteredCount==5 && p.Sites.Count==2 && EconomyService.Instance.Money==money,"3 settlement + 2 free worksites no duplicates");
            Check(p.Sites.Values.Select(site=>site.Item).Distinct().Count()==2 && p.Sites.Values.All(site=>site.Item.basePrice>0 && (int)site.Item.category==0),"two distinct canonical raw sellable products");
            if(pair==0)await Capture("01_Companion_Worksites.png");
            foreach(string owner in p.Owners)
            {
                var site=p.Sites[owner];int before=Inventory.instance.CountItems(site.Item);
                await Interact(site,s);
                if(site.Farm!=null)
                {
                    await Until(()=>site.Farm.CurrentCrop!=null,"existing FarmPlot planted existing seed");
                    // Save an actual in-flight planting and restore it using the existing crop path.
                    await save.SaveGameAsync();await save.LoadGameAsync();site=p.Sites[owner];
                    await Until(()=>site.Farm.CurrentCrop!=null && site.Farm.CurrentCrop.isFullyGrown,"existing crop growth after restore");
                    await Interact(site,s);
                }
                await Until(()=>p.Sites[owner].PlayerActivityCompleted,"actual player activity "+owner);
                Check(Inventory.instance.CountItems(p.Sites[owner].Item)>before,"activity adds canonical inventory "+owner);
            }
            Check(p.Sites.Values.All(site=>site.Producer.StarterWorking || site.Producer.StarterBatchReady),"both real ProducerNpcController sessions started");
            // No timer injection or direct ProduceItems invocation.
            await Until(()=>p.Sites.Values.All(site=>site.Producer.StarterBatchReady),"existing producer timers produced first batches",40);
            if(pair==0)await Capture("02_FirstProduction.png");
            var expected=p.Sites.Values.ToDictionary(site=>site.Owner,site=>Inventory.instance.CountItems(site.Item)+site.Producer.StarterStockCount);
            await save.SaveGameAsync();await save.LoadGameAsync();
            Check(p.Sites.Values.All(site=>site.Producer.StarterBatchReady),"unclaimed stock restored");
            foreach(string owner in p.Owners)
            {
                await Interact(p.Sites[owner],s);
                Check(p.Sites[owner].Producer.StarterClaimed && Inventory.instance.CountItems(p.Sites[owner].Item)==expected[owner],"one-time transfer of actual NPC stock "+owner);
                Check(!p.Sites[owner].Producer.TryClaimStarterBatch(Inventory.instance),"repeat claim rejected");
            }
            await Until(()=>p.Complete && p.Objective=="첫 상품을 준비하세요.","P4 complete -> P5 objective only");
            Check(EconomyService.Instance.Money==money,"support exception does not spend/grant money");
            PA_DepartureContinuationChecks.ValidateReferences(p.gameObject);
            if(pair==0)await Capture("03_P4_Complete.png");
            await save.SaveGameAsync();string json=JsonUtility.ToJson(p.CaptureState());
            await FreshScene();p=Object.FindFirstObjectByType<FirstProductionController>();ConfigureRepository(p.Settlement.EnsureSaveManager());
            await p.Settlement.ResumeAsync();
            await Until(()=>p.IsReady,"fresh reentry P4 ready");
            Check(p.Complete && JsonUtility.ToJson(p.CaptureState())==json,"fresh reentry exact worksite ownership production and completion");
            Check(p.Settlement.Placement.RegisteredCount==5 && p.Settlement.Voyage.Companions.Count==2,"reload no duplicate structures/companions");
            foreach(var site in p.Sites.Values)
                Check(Inventory.instance.CountItems(site.Item)==expected[site.Owner] && !site.Producer.TryClaimStarterBatch(Inventory.instance),"reload cannot duplicate support "+site.Owner);
            Check(!SessionState.GetBool(Key+".Error",false),"no blocking errors pair "+pair);
            File.WriteAllText(Output+"/P4-pair-"+pair+".json",JsonUtility.ToJson(p.CaptureState(),true));
            Debug.Log("[VS-P4] COMBINATION_PASS "+string.Join(",",pairs[pair]));
        }
        // v15 P3 saves migrate without inventing production or a second free grant.
        var legacy=new SaveData{version=15,firstSettlement=new FirstSettlementSaveData{settlementCompleted=true}};
        var migrated=(SaveData)typeof(SaveManager).GetMethod("MigrateSaveData",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p.Settlement.EnsureSaveManager(),new object[]{legacy});
        Check(migrated.version==16 && migrated.firstProduction==null && migrated.firstSettlement.settlementCompleted,"v15 -> v16 additive migration");
        File.WriteAllText(Output+"/P4-validation.json","{\"status\":\"PASS\",\"combinations\":3,\"runtimeErrors\":0,\"saveAndFreshReentry\":true,\"duplicateClaims\":0,\"input\":\"fixture approach pose then real SPACE -> PlayerInteraction -> original activity\",\"graphics\":\"D3D11\"}");
    }
    static Vector2Int FindValid(FirstIslandSettlementController s,string id,Vector2Int preferred)
    {
        foreach(var c in Enumerable.Range(2,12).SelectMany(z=>Enumerable.Range(2,12).Select(x=>new Vector2Int(x,z))).OrderBy(c=>(c-preferred).sqrMagnitude))
            if(s.Placement.Evaluate(id,c,0).Succeeded)return c;
        throw new InvalidOperationException("No free worksite cell");
    }
    static async Task Interact(StarterWorksiteInteraction site,FirstIslandSettlementController s)
    {
        var player=s.Selection.Tutorial.player;var traversal=player.GetComponent<WorldPlayerTraversalGuard>();
        var interaction=player.GetComponent<PlayerInteraction>();var method=typeof(PlayerInteraction).GetMethod("TryFindInteractable",BindingFlags.Instance|BindingFlags.NonPublic);
        bool reachable=false;
        for(int direction=0;direction<8;direction++)
        {
            float angle=direction*Mathf.PI/4;var offset=new Vector3(Mathf.Sin(angle),0,-Mathf.Cos(angle))*1.55f;
            if(!traversal.TryTeleportTo(site.transform.position+offset+Vector3.up*.12f))continue;
            player.LookAt(new Vector3(site.transform.position.x,player.position.y,site.transform.position.z));
            await Task.Delay(250); // Let collision/ground protection settle before checking the live target.
            Physics.SyncTransforms();object[] args={null,null};
            if((bool)method.Invoke(interaction,args) && ReferenceEquals(args[0],site)){reachable=true;break;}
        }
        Check(reachable,"actual PlayerInteraction worksite target "+site.Owner);
        InputSystem.QueueStateEvent(_keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Space));await Task.Delay(130);
        InputSystem.QueueStateEvent(_keyboard,new KeyboardState());await Task.Delay(250);
    }
    static async Task FreshScene()
    {
        var operation=EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/PA_DepartureTutorial.unity",new LoadSceneParameters(LoadSceneMode.Single));
        while(!operation.isDone)await Task.Delay(50);
        await Until(()=>{var p=Object.FindFirstObjectByType<FirstProductionController>();return p!=null && p.Settlement.Selection.Tutorial!=null && p.Settlement.Selection.Tutorial.IsReady;},"fresh departure scene initialized");
        _keyboard.MakeCurrent();
    }
    static void ConfigureRepository(SaveManager save)=>typeof(SaveManager).GetMethod("SetRepositoryForValidation",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(save,new object[]{new LocalJsonSaveRepository(Path.GetFullPath(Saves))});
    static Task Capture(string name)=>PA_SafeGameViewCapture.CaptureAsync(Path.GetFullPath(Output+"/"+name),Camera.main,null,1920,1080,600);
    static async Task Until(Func<bool> predicate,string label,float seconds=30)
    {
        double end=EditorApplication.timeSinceStartup+seconds;
        while(!predicate() && EditorApplication.timeSinceStartup<end)await Task.Delay(70);
        Check(predicate(),label);
    }
    static void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);Debug.Log("[VS-P4] CHECK_OK "+label);}
}
