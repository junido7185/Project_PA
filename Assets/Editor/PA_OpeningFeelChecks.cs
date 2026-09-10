using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class PA_OpeningFeelChecks
{
    const string Key="PA.OpeningFeel", Output="Docs/Presentation/2026-09-10/GameFeel";
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static Task _task; static Keyboard _keyboard; static Mouse _mouse; static double _start;
    static readonly List<string> Results=new List<string>();
    static PA_OpeningFeelChecks()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool(Key,false)&&(t==LogType.Error||t==LogType.Exception||t==LogType.Assert))SessionState.SetBool(Key+".Error",true);};
        EditorApplication.playModeStateChanged+=m=>{
            if(!SessionState.GetBool(Key,false))return;
            if(m==PlayModeStateChange.EnteredPlayMode){_task=null;_start=EditorApplication.timeSinceStartup;SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;}
            if(m==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key+".Done",false))
            {bool fail=SessionState.GetBool(Key+".Error",false);SessionState.SetBool(Key,false);Debug.Log(fail?"[OPENING-FEEL] FAIL":"[OPENING-FEEL] CHECKS_PASS");if(Environment.GetCommandLineArgs().Contains("-executeMethod"))EditorApplication.Exit(fail?1:0);}
        };
    }
    [MenuItem("Project PA/Validation/Opening Feel Cameras %#F8")]
    public static void Cameras(){Run(true);}
    [MenuItem("Project PA/Validation/Opening Feel Flow %#F7")]
    public static void All(){Run(false);SessionState.SetBool(Key+".Final",true);}
    [MenuItem("Project PA/Validation/Opening Feel P1 Input Probe %#F6")]
    public static void InputProbe(){Run(false);SessionState.SetBool(Key+".Probe",true);}
    [MenuItem("Project PA/Validation/Opening Feel Remaining %#F5")]
    public static void Remaining(){Run(false);SessionState.SetBool(Key+".Remaining",true);}
    [MenuItem("Project PA/Validation/Opening Feel Save Recovery %#F4")]
    public static void SaveRecovery(){Run(false);SessionState.SetBool(Key+".Save",true);}
    [MenuItem("Project PA/Validation/Opening Feel Fresh Restore %#F3")]
    public static void FreshRestore(){Run(false);SessionState.SetBool(Key+".Restore",true);}
    static void Run(bool cameras)
    {
        SessionState.SetBool(Key+".Final",false);Directory.CreateDirectory(Output);Directory.CreateDirectory("Logs/OpeningFeel001/ValidationSave");Results.Clear();SessionState.SetBool(Key+".Save",false);SessionState.SetBool(Key+".Restore",false);SessionState.SetBool(Key+".Remaining",false);SessionState.SetBool(Key+".Probe",false);
        SessionState.SetBool(Key,true);SessionState.SetBool(Key+".Done",false);SessionState.SetBool(Key+".Error",false);SessionState.SetBool(Key+".Cameras",cameras);
        EditorSceneManager.OpenScene("Assets/Scenes/PA_DepartureTutorial.unity");
        PA_DepartureContinuationChecks.ConfigureGameView();
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||SessionState.GetBool(Key+".Done",false))return;
        var t=Object.FindFirstObjectByType<DepartureTutorialController>();
        if(_task==null&&t!=null&&t.IsReady&&Camera.main.GetComponent<CameraController>().OpeningFraming)_task=Checks(t);
        if(_task!=null&&_task.IsCompleted || EditorApplication.timeSinceStartup-_start>420)
        {
            if(_task==null||!_task.IsCompleted||_task.IsFaulted){SessionState.SetBool(Key+".Error",true);Debug.LogError("[OPENING-FEEL] "+(_task?.Exception?.GetBaseException().ToString()??"timeout"));}
            File.WriteAllLines(Output+"/"+(SessionState.GetBool(Key+".Final",false)?"FinalRoute":SessionState.GetBool(Key+".Restore",false)?"FreshRestore":SessionState.GetBool(Key+".Save",false)?"SaveRecovery":SessionState.GetBool(Key+".Remaining",false)?"Remaining":SessionState.GetBool(Key+".Probe",false)?"P1Probe":SessionState.GetBool(Key+".Cameras",false)?"Camera":"Flow")+"-checks.txt",Results);
            if(_keyboard!=null&&_keyboard.added)InputSystem.RemoveDevice(_keyboard);
            if(_mouse!=null&&_mouse.added)InputSystem.RemoveDevice(_mouse);
            SessionState.SetBool(Key+".Done",true);Application.runInBackground=SessionState.GetBool(Key+".Background",false);EditorApplication.ExitPlaymode();
        }
    }
    static async Task Checks(DepartureTutorialController t)
    {
        _keyboard=InputSystem.AddDevice<Keyboard>("OpeningFeelKeyboard");_keyboard.MakeCurrent();
        _mouse=InputSystem.AddDevice<Mouse>("OpeningFeelMouse");_mouse.MakeCurrent();
        await Task.Delay(500);
        var follow=Camera.main.GetComponent<CameraController>();
        if(SessionState.GetBool(Key+".Final",false)){await FinalRoute(t);return;}
        if(SessionState.GetBool(Key+".Save",false)||SessionState.GetBool(Key+".Restore",false))
        {
            var savedSelection=Object.FindFirstObjectByType<DepartureCompanionSelection>();
            var savedSettlement=savedSelection.GetComponent<FirstIslandSettlementController>();
            var manager=ConfigureSave(savedSettlement);
            if(SessionState.GetBool(Key+".Restore",false))
            {
                string expected=File.ReadAllText("Logs/OpeningFeel001/ExpectedSettlement.json");
                await manager.LoadGameAsync();
                Check(JsonUtility.ToJson(savedSettlement.CaptureState())==expected,"fresh reentry exact P3 state");
                Check(Object.FindObjectsByType<SaveManager>(FindObjectsSortMode.None).Length==1,"one SaveManager after reentry");
                Check(savedSelection.GetComponent<DepartureVoyagePresentation>().Companions.Count==2,"fresh reentry same two companions");
                Check(!SessionState.GetBool(Key+".Error",false),"fresh runtime errors0");
                return;
            }
            // Rebuild only prerequisites for the unfinished save check; passed route checks/captures are not repeated.
            savedSelection.RestoreConfirmedSelection(new[]{"Miner_01","Farmer_01"});
            savedSelection.GetComponent<DepartureVoyagePresentation>().SkipTravelForSavedArrival();
            await Until(()=>savedSettlement.IsReady,"save fixture ready");
            for(int i=0;i<3;i++){savedSettlement.Begin(savedSettlement.BuildingIds[i]);savedSettlement.PreviewAt(i==0?new Vector2Int(6,5):i==1?new Vector2Int(9,4):new Vector2Int(8,7),i==1?1:0);savedSettlement.Commit();}
            await Until(()=>savedSelection.GetComponent<DepartureVoyagePresentation>().Companions.All(n=>!n.GetComponent<NavMeshAgent>().pathPending&&n.GetComponent<NavMeshAgent>().remainingDistance<.8f),"save fixture companions settled",30);
            await Walk(t.player,new Vector3(114,0,108));await SaveAndRecovery(t,savedSelection);return;
        }
        if(SessionState.GetBool(Key+".Probe",false))
        {
            var probeSelection=Object.FindFirstObjectByType<DepartureCompanionSelection>();probeSelection.OpenDevelopmentSelection();
            await Click(probeSelection.CandidateButtons[1]);await Click(probeSelection.CandidateButtons[2]);
            Results.Add("before Enter selected="+string.Join(",",probeSelection.SelectedIds)+" active="+probeSelection.isActiveAndEnabled);
            _keyboard.MakeCurrent();InputSystem.QueueStateEvent(_keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Enter));
            for(int n=0;n<8;n++){await Task.Delay(35);Results.Add("sample="+n+" current="+Keyboard.current?.name+" held="+Keyboard.current?.enterKey.isPressed+" frame="+Time.frameCount+" confirmed="+probeSelection.IsConfirmed+" selected="+string.Join(",",probeSelection.SelectedIds));}
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState());
            return;
        }
        if(SessionState.GetBool(Key+".Cameras",false))
        {
            float[] distances={12.5f,14f,15.5f};
            for(int i=0;i<3;i++)
            {
                follow.ConfigureOpening(t.player,distances[i],40,34);follow.SnapToTarget();
                await Capture("CameraCandidate_"+(char)('A'+i)+".png");MeasureCamera(t.player,"candidate"+i);
            }
            foreach(var animator in t.player.GetComponentsInChildren<Animator>())Check(animator.avatar!=null&&animator.avatar.isHuman&&animator.avatar.isValid&&!animator.applyRootMotion,"valid humanoid / code-authoritative root / controller="+(animator.runtimeAnimatorController==null?"existing procedural":animator.runtimeAnimatorController.name));
            return;
        }
        if(SessionState.GetBool(Key+".Remaining",false))
        {
            var pending=Object.FindFirstObjectByType<DepartureCompanionSelection>();pending.OpenDevelopmentSelection();
            await FinishOpening(t,pending);return;
        }
        await Capture("01_GameplayCamera_Final.png");MeasureCamera(t.player,"P0");
        await MovementMetrics(t);
        await Motion(t);
        await Walk(t.player,new Vector3(t.movementCheckpoint.position.x,0,-3));await Until(()=>t.Stage==2,"checkpoint");
        await Walk(t.player,new Vector3(0,0,-3.3f));await Face(t.player,t.fruitTree.transform.position);
        Pose(t.player,new Vector3(0,.1f,-5));Check(!Target(t.player,t.fruitTree),"two cells rejected");
        Pose(t.player,new Vector3(0,.1f,-2.65f));t.player.rotation=Quaternion.Euler(0,180,0);Check(!Target(t.player,t.fruitTree),"behind rejected");
        await Face(t.player,t.fruitTree.transform.position);Check(Target(t.player,t.fruitTree),"adjacent front tree accepted");
        var occluder=GameObject.CreatePrimitive(PrimitiveType.Cube);occluder.name="ValidationOccluder";
        occluder.transform.position=t.player.position+Vector3.forward*.8f+Vector3.up;occluder.transform.localScale=new Vector3(1,2,.15f);
        Check(!Target(t.player,t.fruitTree),"solid obstacle blocks interaction");Object.Destroy(occluder);await Task.Delay(100);
        await Capture("02_P0_AdjacentInteraction.png");await Press(UnityEngine.InputSystem.Key.Space);await Until(()=>t.Stage==3,"real tree inventory3");
        await Walk(t.player,new Vector3(0,0,-3.7f));await Walk(t.player,new Vector3(7,0,-3.7f));await Walk(t.player,new Vector3(7,0,-2.85f));await Face(t.player,t.trainingSlot.transform.position);
        Check(Target(t.player,t.trainingSlot),"shelf front anchor accepted");await Press(UnityEngine.InputSystem.Key.Space);await Until(()=>t.Stage==4,"real shelf stock1");
        Pose(t.player,new Vector3(7,.1f,.6f));await Face(t.player,t.trainingSlot.transform.position);Check(!Target(t.player,t.trainingSlot),"back of shelf rejected");
        Pose(t.player,new Vector3(7,.1f,-2.85f));await Face(t.player,t.trainingSlot.transform.position);
        for(int i=0;i<10;i++)
        {
            await Press(UnityEngine.InputSystem.Key.Space);Check(ShopPriceUI.instance.IsOpen,"UI opens cycle"+i);
            await Click((Button)typeof(ShopPriceUI).GetField("_closeBtn",Hidden).GetValue(ShopPriceUI.instance));
            Check(!ShopPriceUI.instance.IsOpen&&Cursor.visible&&Cursor.lockState==CursorLockMode.None,"mouse close without Escape "+i);
            await Press(UnityEngine.InputSystem.Key.S,70);await Face(t.player,t.trainingSlot.transform.position);
            Pose(t.player,new Vector3(7,.1f,-2.85f));
        }
        await Press(UnityEngine.InputSystem.Key.Space);Check(ShopPriceUI.instance.IsOpen,"sale price UI open");
        int money=EconomyService.Instance.Money;
        Check(!t.trainingSlot.IsEmpty&&t.trainingSlot.currentItem.count==1&&Display(t.trainingSlot),"stock1 visual present before sale");
        await Click((Button)typeof(ShopPriceUI).GetField("_confirmBtn",Hidden).GetValue(ShopPriceUI.instance));
        await Until(()=>t.Complete||t.Stage==4,"real purchase evaluator decision",40);
        if(!t.Complete){await Press(UnityEngine.InputSystem.Key.Space);await Click((Button)typeof(ShopPriceUI).GetField("_confirmBtn",Hidden).GetValue(ShopPriceUI.instance));await Until(()=>t.Complete,"real repricing retry",40);}
        Check(t.trainingSlot.IsEmpty&&!Display(t.trainingSlot)&&EconomyService.Instance.Money==money+t.SaleAmount,"stock0 visual absent exact sale money");
        t.trainingSlot.RefreshDisplay();await Task.Delay(100);Check(!Display(t.trainingSlot),"refresh cannot resurrect sold fruit");
        await Capture("05_SaleClearsDisplay.png");
        var selection=Object.FindFirstObjectByType<DepartureCompanionSelection>();
        await Press(UnityEngine.InputSystem.Key.Enter);Check(selection.IsOpen,"keyboard CTA actually enters P1 (mouse passed first run)");
        await Capture("06_P0_to_P1_ActualTransition.png");
        await FinishOpening(t,selection);
    }
    static async Task FinalRoute(DepartureTutorialController t)
    {
        Check(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Direct3D11,"D3D11 final connected route");
        await Walk(t.player,new Vector3(t.movementCheckpoint.position.x,0,-3));await Until(()=>t.Stage==2,"checkpoint");
        await Walk(t.player,new Vector3(0,0,-2.65f));await Face(t.player,t.fruitTree.transform.position);
        await Press(UnityEngine.InputSystem.Key.Space);await Until(()=>t.Stage==3,"tree interaction inventory3");
        await Walk(t.player,new Vector3(0,0,-3.7f));await Walk(t.player,new Vector3(7,0,-3.7f));await Walk(t.player,new Vector3(7,0,-2.85f));await Face(t.player,t.trainingSlot.transform.position);
        await Press(UnityEngine.InputSystem.Key.Space);await Until(()=>t.Stage==4,"display existing ShopSlot stock1");
        await Press(UnityEngine.InputSystem.Key.Space);Check(ShopPriceUI.instance.IsOpen,"existing price UI");
        int money=EconomyService.Instance.Money;
        await Click((Button)typeof(ShopPriceUI).GetField("_confirmBtn",Hidden).GetValue(ShopPriceUI.instance));
        await Until(()=>t.Complete||t.Stage==4,"PurchaseEvaluator decision",40);
        if(!t.Complete){await Press(UnityEngine.InputSystem.Key.Space);await Click((Button)typeof(ShopPriceUI).GetField("_confirmBtn",Hidden).GetValue(ShopPriceUI.instance));await Until(()=>t.Complete,"normal price retry",40);}
        Check(t.trainingSlot.IsEmpty&&!Display(t.trainingSlot)&&EconomyService.Instance.Money==money+t.SaleAmount,"sale removes display and deposits once");
        var selection=Object.FindFirstObjectByType<DepartureCompanionSelection>();
        await Click(t.presentation.CompanionButton);Check(selection.IsOpen,"mouse certification CTA enters P1");
        await FinishOpening(t,selection);
    }
    static async Task FinishOpening(DepartureTutorialController t,DepartureCompanionSelection selection)
    {
        var follow=Camera.main.GetComponent<CameraController>();
        await Click(selection.CandidateButtons[1]);await Click(selection.CandidateButtons[2]);Check(selection.SelectedIds.Count==2,"mouse chooses2");
        await Press(UnityEngine.InputSystem.Key.Enter);Check(selection.IsConfirmed,"keyboard confirms P1");
        var voyage=selection.GetComponent<DepartureVoyagePresentation>();await Until(()=>voyage.Sailing,"fade then deck spawn");
        Check(voyage.Companions.Count==2&&t.player.position.y>1.3f,"deck player and2 companions");
        if(!SessionState.GetBool(Key+".Final",false))await Capture("07_P2_BoatDeck.png");
        await Until(()=>voyage.ArrivalFadeFinished,"voyage arrival",25);
        var settlement=selection.GetComponent<FirstIslandSettlementController>();await Until(()=>settlement.IsReady,"P3 ready");
        for(int i=0;i<3;i++){settlement.Begin(settlement.BuildingIds[i]);Check(follow.OpeningBuildMode,"build zoom state");var cell=i==0?new Vector2Int(6,5):i==1?new Vector2Int(9,4):new Vector2Int(8,7);Check(settlement.PreviewAt(cell,i==1?1:0).Succeeded&&settlement.Commit(),"P3 place"+i);}
        Check(!follow.OpeningBuildMode&&settlement.SettlementCompleted,"normal camera returns P3complete");
        await Until(()=>voyage.Companions.All(n=>!n.GetComponent<NavMeshAgent>().pathPending&&n.GetComponent<NavMeshAgent>().remainingDistance<.8f),"companions stop at shelters",30);
        await Walk(t.player,new Vector3(114,0,108));
        await Capture("08_P3_GameplayCamera.png");MeasureCamera(t.player,"P3");
        await SaveAndRecovery(t,selection);
    }
    static SaveManager ConfigureSave(FirstIslandSettlementController settlement)
    {
        var save=settlement.EnsureSaveManager();typeof(SaveManager).GetMethod("SetRepositoryForValidation",Hidden).Invoke(save,new object[]{new LocalJsonSaveRepository(Path.GetFullPath("Logs/OpeningFeel001/ValidationSave"))});return save;
    }
    static async Task SaveAndRecovery(DepartureTutorialController t,DepartureCompanionSelection selection)
    {
        var settlement=selection.GetComponent<FirstIslandSettlementController>();var save=ConfigureSave(settlement);
        string expected=JsonUtility.ToJson(settlement.CaptureState());File.WriteAllText("Logs/OpeningFeel001/ExpectedSettlement.json",expected);await save.SaveGameAsync();int savedMoney=EconomyService.Instance.Money;EconomyService.Instance.Deposit(11,"Opening feel restore probe");await save.LoadGameAsync();Check(EconomyService.Instance.Money==savedMoney,"load actually restores changed money");Check(JsonUtility.ToJson(settlement.CaptureState())==expected,"P3 save restore exact");
        var feel=selection.GetComponent<OpeningFeelPresentation>();int recoveries=feel.Recoveries;Pose(t.player,new Vector3(t.player.position.x,-8,t.player.position.z));await Task.Delay(200);Check(t.player.position.y>-.5f,"forced fall recovery");
        await Press(UnityEngine.InputSystem.Key.A,150);await Until(()=>t.player.GetComponent<PlayerController>().ActualPlanarSpeed<.15f,"post recovery stop",2);
        PA_DepartureContinuationChecks.ValidateReferences(selection.gameObject);
        Check(!SessionState.GetBool(Key+".Error",false),"runtime errors0");
    }
    static async Task MovementMetrics(DepartureTutorialController t)
    {
        var p=t.player;var origin=p.position;var controller=p.GetComponent<PlayerController>();
        float[] speeds=new float[2];
        for(int i=0;i<2;i++)
        {
            Pose(p,origin);var keys=i==0?new[]{UnityEngine.InputSystem.Key.D}:new[]{UnityEngine.InputSystem.Key.D,UnityEngine.InputSystem.Key.W};
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(keys));await Task.Delay(250);
            var before=p.position;float began=Time.time;await Task.Delay(300);
            speeds[i]=Vector3.ProjectOnPlane(p.position-before,Vector3.up).magnitude/(Time.time-began);
            var released=p.position;InputSystem.QueueStateEvent(_keyboard,new KeyboardState());await Task.Delay(230);
            float stopped=Vector3.ProjectOnPlane(p.position-released,Vector3.up).magnitude;
            Check(stopped<.4f,"stop distance="+stopped.ToString("F3"));
        }
        Check(Mathf.Abs(speeds[1]/speeds[0]-1)<.12f,"cardinal="+speeds[0].ToString("F2")+" diagonal="+speeds[1].ToString("F2")+" cellSeconds="+(2/speeds[0]).ToString("F3"));
        Pose(p,origin);var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=origin+Vector3.right*1.2f+Vector3.up;blocker.transform.localScale=new Vector3(.2f,2,2);
        InputSystem.QueueStateEvent(_keyboard,new KeyboardState(UnityEngine.InputSystem.Key.D));await Task.Delay(600);
        Check(controller.ActualPlanarSpeed<.15f&&p.GetComponent<NpcHumanoidProceduralAnimator>().CurrentPlanarSpeed<.15f,"collision stop drives idle from actual velocity");
        InputSystem.QueueStateEvent(_keyboard,new KeyboardState());Object.Destroy(blocker);await Task.Delay(200);
        var safety=Object.FindFirstObjectByType<OpeningFeelPresentation>();int beforeRecovery=safety.Recoveries;Pose(p,new Vector3(origin.x,-8,origin.z));await Task.Delay(200);
        Check(p.position.y>-.5f&&safety.Recoveries==beforeRecovery+1,"P0 forced fall recovers exactly once");Pose(p,origin);
    }
    static async Task Motion(DepartureTutorialController t)
    {
        var player=t.player;var origin=player.position;
        for(int i=0;i<12;i++)
        {
            var keys=i>=2&&i<5?new[]{UnityEngine.InputSystem.Key.D}:i>=5&&i<8?new[]{UnityEngine.InputSystem.Key.W}:i==8?new[]{UnityEngine.InputSystem.Key.A}:Array.Empty<UnityEngine.InputSystem.Key>();
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(keys));
            await Capture("PlayerMotion_"+i.ToString("00")+".png",100);
        }
        InputSystem.QueueStateEvent(_keyboard,new KeyboardState());await Task.Delay(200);Pose(player,origin);
        var npc=t.buyer;var agent=npc.GetComponent<NavMeshAgent>();
        Pose(player,npc.transform.position+new Vector3(-2,0,-2));Camera.main.GetComponent<CameraController>().SnapToTarget();
        for(int i=0;i<6;i++)await Capture("NPCIdle_"+i.ToString("00")+".png",250);
        agent.SetDestination(npc.transform.position+Vector3.left*3);
        for(int i=0;i<8;i++)await Capture("NPCWalkStop_"+i.ToString("00")+".png",200);
        Pose(player,origin);
    }
    static bool Display(ShopSlot slot)=>slot.GetComponentsInChildren<Transform>().Any(t=>t.name=="ItemModel"&&t.gameObject.activeInHierarchy);
    static bool Target(Transform player,IInteractable expected){Physics.SyncTransforms();object[] args={null,null};bool found=(bool)typeof(PlayerInteraction).GetMethod("TryFindInteractable",Hidden).Invoke(player.GetComponent<PlayerInteraction>(),args);return found&&ReferenceEquals(args[0],expected);}
    static void Pose(Transform p,Vector3 v){var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.position=v;p.GetComponent<PlayerController>().ResetMotionAfterTeleport();cc.enabled=true;Physics.SyncTransforms();}
    static async Task Face(Transform p,Vector3 point){Vector3 delta=point-p.position;delta.y=0;p.rotation=Quaternion.LookRotation(delta);await Task.Delay(100);}
    static async Task Press(UnityEngine.InputSystem.Key key,int ms=100){InputSystem.QueueStateEvent(_keyboard,new KeyboardState(key));await Task.Delay(ms);InputSystem.QueueStateEvent(_keyboard,new KeyboardState());await Task.Delay(200);}
    static async Task Click(Button button)
    {
        Check(button!=null&&button.isActiveAndEnabled&&button.interactable,"clickable "+button?.name);
        Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;Vector2 point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        InputSystem.QueueStateEvent(_mouse,new MouseState{position=point});await Task.Delay(100);
        InputSystem.QueueStateEvent(_mouse,new MouseState{position=point,buttons=1});await Task.Delay(100);
        InputSystem.QueueStateEvent(_mouse,new MouseState{position=point});await Task.Delay(200);
    }
    static async Task Walk(Transform player,Vector3 target)
    {
        double end=EditorApplication.timeSinceStartup+18;
        while(EditorApplication.timeSinceStartup<end)
        {
            var delta=target-player.position;delta.y=0;if(delta.magnitude<.20f)break;
            var keys=new List<UnityEngine.InputSystem.Key>();if(Mathf.Abs(delta.x)>.15f)keys.Add(delta.x>0?UnityEngine.InputSystem.Key.D:UnityEngine.InputSystem.Key.A);if(Mathf.Abs(delta.z)>.15f)keys.Add(delta.z>0?UnityEngine.InputSystem.Key.W:UnityEngine.InputSystem.Key.S);
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(keys.ToArray()));await Task.Delay(35);
        }
        InputSystem.QueueStateEvent(_keyboard,new KeyboardState());await Task.Delay(200);Check(Vector3.ProjectOnPlane(target-player.position,Vector3.up).magnitude<.6f,"walk "+target);
    }
    static void MeasureCamera(Transform player,string label)
    {
        var renderers=player.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        var top=Camera.main.WorldToViewportPoint(bounds.center+Vector3.up*bounds.extents.y);var bottom=Camera.main.WorldToViewportPoint(bounds.center-Vector3.up*bounds.extents.y);
        Results.Add(label+" height="+(top.y-bottom.y).ToString("F4")+" centerFromTop="+(1-(top.y+bottom.y)*.5f).ToString("F4"));
    }
    static Task Capture(string name,int settle=500)=>PA_SafeGameViewCapture.CaptureAsync(Path.GetFullPath(Output+"/"+name),Camera.main,null,1920,1080,settle);
    static async Task Until(Func<bool> f,string label,float seconds=30){double end=EditorApplication.timeSinceStartup+seconds;while(!f()&&EditorApplication.timeSinceStartup<end)await Task.Delay(70);Check(f(),label);}
    static void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);Results.Add("PASS "+label);Debug.Log("[OPENING-FEEL] PASS "+label);}
}
