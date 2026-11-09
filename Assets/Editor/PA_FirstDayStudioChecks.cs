using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// One bounded acceptance driver. Input uses the real Input System and existing UI event handlers.
public static class PA_FirstDayStudioChecks
{
    const string Active="PA.FirstDayStudio.Active";
    static string Output=>SessionState.GetString(Active+".Output","Logs/FirstDayStudio/Play-1");
    static Keyboard keyboard;
    static bool running;
    static int checks;
    static double began;
    static bool record;
    static Task recording;
    // Edit-mode inspection only; does not consume a Play attempt or regenerate assets.
    public static void InspectAcceptanceAssets()
    {
        var assets=FirstDayStudioAssets.Load();
        if(assets==null)throw new Exception("Missing prepared catalog");
        var rows=new System.Collections.Generic.List<string>();
        foreach(var prefab in assets.trees.Concat(assets.grasses).Concat(new[]{assets.dock,assets.boat}))
        {
            var root=new GameObject("FirstDayAssetInspection");
            try
            {
                var visual=FirstDayStudioAssets.Place(prefab,root.transform,Vector3.zero,2);
                Bounds bounds=visual.GetComponentsInChildren<Renderer>()[0].bounds;
                foreach(var renderer in visual.GetComponentsInChildren<Renderer>())bounds.Encapsulate(renderer.bounds);
                rows.Add(prefab.name+" | imported rotation="+prefab.transform.localEulerAngles+" | composed rotation="+visual.transform.eulerAngles+" | bounds="+bounds.size+" | bottom="+bounds.min.y);
                if(Mathf.Abs(bounds.size.y-2)>.02f||Mathf.Abs(bounds.min.y)>.02f)throw new Exception("Asset grounding/height failure: "+prefab.name);
            }
            finally{Object.DestroyImmediate(root);}
        }
        File.WriteAllLines("Logs/FirstDayStudio/post-capture-asset-check.txt",rows);
        Debug.Log("[FIRST-DAY] Edit-mode import rotation/height/grounding checks PASS; GameView recheck still required.");
    }
    [MenuItem("Tools/Project PA/First Day Studio/Verify and Capture")]
    public static void Run()
    {
        if(FirstDayStudioAssets.Load()==null)throw new Exception("Prepared FirstDayStudio catalog is required; setup is not repeated by acceptance.");
        string path="Logs/FirstDayStudio/Play-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(path);SessionState.SetString(Active+".Output",path);
        SessionState.SetBool(Active,true);SessionState.SetBool(Active+".Failed",false);SessionState.SetInt(Active+".Errors",0);
        File.WriteAllText(path+"/checks.txt","Real InputSystem keyboard; UI drag/click events; targeted approach poses explicitly noted.\n");
        Subscribe();
        PA_DepartureContinuationChecks.ConfigureGameView();
        EditorSceneManager.OpenScene("Assets/Scenes/PA_DepartureTutorial.unity",OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool(Active,false))Subscribe();}
    static void Subscribe()
    {
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
        EditorApplication.playModeStateChanged-=Mode;EditorApplication.playModeStateChanged+=Mode;
    }
    static void Log(string message,string stack,LogType type)
    {
        if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;
        if(stack.Contains("UnityEditor.Connect.") && message.Contains("Token Exchange"))
        {File.AppendAllText(Output+"/editor-service-errors.txt",message+"\n"+stack+"\n");return;}
        SessionState.SetInt(Active+".Errors",SessionState.GetInt(Active+".Errors",0)+1);
        File.AppendAllText(Output+"/errors.txt",message+"\n"+stack+"\n");
    }
    static void Mode(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Active,false))return;
        bool failed=SessionState.GetBool(Active+".Failed",false)||SessionState.GetInt(Active+".Errors",0)>0;
        File.AppendAllText(Output+"/result.txt","Final="+(failed?"FAIL":"PASS")+"\nRuntime errors="+SessionState.GetInt(Active+".Errors",0)+"\n");
        SessionState.SetBool(Active,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
        EditorApplication.Exit(failed?1:0);
    }
    static async void Tick()
    {
        if(!EditorApplication.isPlaying||running)return;
        running=true;began=EditorApplication.timeSinceStartup;
        try
        {
            Application.runInBackground=true;
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            await Exercise();
            if(recording!=null)await recording;
            Check(SessionState.GetInt(Active+".Errors",0)==0,"runtime errors / exceptions / asserts zero");
            File.WriteAllText(Output+"/result.txt","Targeted gameplay "+(SessionState.GetBool(Active+".Failed",false)?"FAIL":"PASS")+"; checks="+checks+"\nVisual review required for captures.\n");
        }
        catch(Exception ex)
        {SessionState.SetBool(Active+".Failed",true);File.WriteAllText(Output+"/result.txt","FAIL\n"+ex);try{await Capture("failure-context.png");}catch{}}
        finally
        {
            record=false;
            if(recording!=null)await recording;
            if(keyboard!=null){InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.RemoveDevice(keyboard);}
            EditorApplication.ExitPlaymode();
        }
    }
    static async Task Exercise()
    {
        await Until(()=>Object.FindFirstObjectByType<DepartureTutorialController>()?.IsReady==true,"tutorial ready",45);
        var tutorial=Object.FindFirstObjectByType<DepartureTutorialController>();
        var player=tutorial.player;var inventory=player.GetComponent<Inventory>();var equipment=player.GetComponent<EquipmentSystem>();
        Check(PlayerInputHandler.Instance.FirstDayControls,"opening controls configured");
        Check(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Direct3D11,"D3D11 rendered Play");
        await Task.Delay(800);
        Vector3 before=player.position;await Hold(650,Key.D);float walk=Vector3.Distance(before,player.position);
        before=player.position;await Hold(650,Key.D,Key.LeftShift);float run=Vector3.Distance(before,player.position);
        Check(walk>1 && run>walk*1.1f,"actual walk/run input speeds");
        Pose(player,tutorial.movementCheckpoint.position+Vector3.up*.05f,tutorial.fruitTree.transform.position);
        await Press(Key.Space);await Until(()=>tutorial.Stage==2,"jump unlocks movement certification");
        await Task.Delay(750);
        Check(player.GetComponent<PlayerController>().JumpCount>0,"Space jumps instead of interacting");
        Pose(player,new Vector3(0,.08f,-2.65f),tutorial.fruitTree.transform.position);
        await Press(Key.X);await Press(Key.E);
        await Until(()=>Object.FindObjectsByType<InventoryFramework.PickupItem>(FindObjectsSortMode.None).Count(p=>p.contextual)==3,"three physical fruit drops");
        await Task.Delay(1500);await Capture("01-tree-fruit.png");
        Check(inventory.CountItems(tutorial.fruit)==0,"tree shaking does not grant inventory");
        var fruits=Object.FindObjectsByType<InventoryFramework.PickupItem>(FindObjectsSortMode.None).Where(p=>p.contextual).ToArray();
        foreach(var fruit in fruits)
        {
            Approach(player,fruit.transform.position);
            await Press(Key.E);
        }
        await Until(()=>tutorial.Stage==3,"E pickups enter canonical inventory");
        Check(inventory.CountItems(tutorial.fruit)==3,"fruit awarded exactly three");
        for(int i=0;i<9;i++){await Press(Key.Digit1+i);Check(inventory.selectedHotbarIndex==i,"digit selects slot "+(i+1));}
        var mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();
        InputSystem.QueueDeltaStateEvent(mouse.scroll,new Vector2(0,120));await Task.Delay(200);
        Check(inventory.selectedHotbarIndex==0,"real mouse wheel wraps hotbar 9 to 1");InputSystem.RemoveDevice(mouse);
        int apple=inventory.hotbar.slots.FindIndex(s=>!s.IsEmpty&&s.item==tutorial.fruit);
        await Press(Key.Digit1+apple);Check(equipment.HeldVisual!=null&&equipment.HeldItem==tutorial.fruit,"apple visible in hand");
        await Press(Key.X);
        Check(equipment.IsHolstered&&equipment.HeldItem==null&&inventory.selectedHotbarIndex==apple&&inventory.CountItems(tutorial.fruit)==3,"X hides held expression only; slot and ownership unchanged");
        await Press(Key.Digit1+apple);
        Pose(player,new Vector3(7,.08f,-2.85f),tutorial.trainingSlot.transform.position);
        await Press(Key.E);await Until(()=>tutorial.Stage==4,"actual held apple stocked");
        await Press(Key.E);Check(ShopPriceUI.instance.IsOpen,"existing price UI open");
        var slider=ShopPriceUI.instance.TutorialPriceDrag;
        Check(slider!=null,"tutorial drag-price control");
        var rect=(RectTransform)slider.transform;var corners=new Vector3[4];rect.GetWorldCorners(corners);
        Vector2 left=RectTransformUtility.WorldToScreenPoint(null,(corners[0]+corners[1])*.5f);
        var drag=new PointerEventData(EventSystem.current){position=left,button=PointerEventData.InputButton.Left};
        ExecuteEvents.Execute(slider.gameObject,drag,ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(slider.gameObject,drag,ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(slider.gameObject,drag,ExecuteEvents.pointerUpHandler);
        Check(ShopPriceUI.instance.TutorialPriceDragged,"real UI drag event changes price");
        await Capture("02-price-drag.png");
        var confirm=Resources.FindObjectsOfTypeAll<Button>().First(b=>b.gameObject.scene.IsValid()&&b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="가격 확정");
        ExecuteEvents.Execute(confirm.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
        await Until(()=>tutorial.Complete,"existing NPC real purchase certifies",70);
        Check(tutorial.trainingSlot.IsEmpty&&tutorial.SaleAmount>0,"real sale clears training shelf");
        tutorial.presentation.CompanionButton.onClick.Invoke();
        var selection=Object.FindFirstObjectByType<DepartureCompanionSelection>();
        selection.CandidateButtons[1].onClick.Invoke();selection.CandidateButtons[2].onClick.Invoke();
        string[] selected=selection.SelectedIds.ToArray();selection.DepartureButton.onClick.Invoke();
        var voyage=selection.GetComponent<DepartureVoyagePresentation>();
        await Until(()=>voyage.Sailing,"voyage starts");await Task.Delay(2200);await Capture("03-pixel-voyage.png");
        await Until(()=>WorldAlphaPlayableController.Instance?.DemoRoute?.IsPlayable==true,"real Demo256 harbor playable",120);
        var alpha=WorldAlphaPlayableController.Instance;player=alpha.Adapter.PlayerRoot.transform;inventory=alpha.Adapter.PlayerInventory;equipment=player.GetComponent<EquipmentSystem>();
        record=true;recording=RecordGameView();
        await Task.Delay(1200);await Capture("04-harbor.png");
        typeof(SaveManager).GetMethod("SetRepositoryForValidation",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(SaveManager.instance,new object[]{new LocalJsonSaveRepository(Output+"/isolated-save")});
        Check(alpha.Grid.Definition.Width==256&&alpha.Grid.Definition.Height==256,"actual Demo256 grid");
        var players=Resources.FindObjectsOfTypeAll<PlayerController>().Where(p=>p.gameObject.scene.IsValid()&&p.gameObject.activeInHierarchy).ToArray();
        var inventories=Resources.FindObjectsOfTypeAll<Inventory>().Where(p=>p.gameObject.scene.IsValid()&&p.gameObject.activeInHierarchy).ToArray();
        File.AppendAllText(Output+"/checks.txt","Authorities: players="+string.Join(",",players.Select(p=>p.name))+" inventories="+string.Join(",",inventories.Select(p=>p.name))+"\n");
        Check(players.Length==1&&inventories.Length==1&&inventories[0]==inventory&&inventory==Inventory.instance,"single player and inventory authority including DontSave runtime objects");
        Check(selected.SequenceEqual(DemoRouteController.SelectedCompanionIds),"exact selected companions cross scene");
        foreach(var candidate in DemoRouteController.SelectedCompanions)
        {
            var npc=FirstDayWorldPresentation.Instance.transform.Find("Companion_"+candidate.id);
            Check(npc!=null&&npc.GetComponent<NpcDialogue>()?.overrideProfile==candidate.profile&&npc.GetComponentsInChildren<SkinnedMeshRenderer>().Length>0,"selected companion profile/model actually at harbor: "+candidate.id);
        }
        Check(!inventory.slots.Concat(inventory.hotbar.slots).Any(s=>!s.IsEmpty),"island starts empty; no training items or automatic tools");
        Check(EconomyService.Instance.Money==0,"training money does not carry");
        Check(GameClock.Instance.CurrentDay==1&&GameClock.Instance.CurrentHour<10,"Day 1 starts at harbor control");
        var supply=FirstDayWorldPresentation.Instance.Supply;
        Approach(player,supply.transform.position);await Press(Key.X);
        var filler=Resources.Load<Item>("Items/Item_Plank");
        foreach(var s in inventory.slots.Concat(inventory.hotbar.slots))s.Set(filler,99);
        inventory.slots[0].Clear();inventory.RefreshAllUI();
        await Press(Key.E);
        Check(!supply.Collected&&inventory.CountItems(FirstDayStudioAssets.Load().supplies[0])==1,"partial supply keeps one delivered entry and awaits capacity");
        await Press(Key.E);Check(inventory.CountItems(FirstDayStudioAssets.Load().supplies[0])==1,"partial supply retry does not duplicate phone");
        foreach(var s in inventory.slots.Concat(inventory.hotbar.slots))if(s.item==filler)s.Clear();
        var receivedPhone=inventory.slots.FirstOrDefault(s=>!s.IsEmpty);
        if(receivedPhone!=null){inventory.hotbar.slots[0].SetInstance(receivedPhone.instance);receivedPhone.Clear();inventory.RefreshAllUI();}
        await Press(Key.E);
        Check(supply.Collected,"E supply collected");
        foreach(var item in FirstDayStudioAssets.Load().supplies)Check(inventory.CountItems(item)==(item.id==2013?2:1),"supply exact count "+item.itemName);
        await Press(Key.E);Check(inventory.CountItems(FirstDayStudioAssets.Load().supplies[1])==1,"repeat supply input cannot duplicate");
        await Press(Key.Digit2);Check(equipment.HeldVisual!=null&&equipment.HeldItem?.toolType==ToolType.Axe,"axe visibly equipped");
        await Capture("05-supply-held-tool.png");
        int slotBefore=inventory.selectedHotbarIndex;await Press(Key.X);
        Check(inventory.selectedHotbarIndex==slotBefore&&equipment.IsHolstered,"island X preserves selected slot");
        await Press(Key.Tab);Check(InventoryUI.instance.gameObject.activeSelf,"Tab opens existing inventory");
        before=player.position;int jumps=player.GetComponent<PlayerController>().JumpCount;
        await Hold(300,Key.W,Key.Space,Key.Digit9);
        Check(Vector3.Distance(before,player.position)<.05f&&player.GetComponent<PlayerController>().JumpCount==jumps&&inventory.selectedHotbarIndex==slotBefore,"inventory UI blocks movement/jump/slot shortcuts");
        await Press(Key.Tab);
        await Press(Key.P);Check(SmartphoneUI.instance!=null&&SmartphoneUI.instance.IsOpen,"P opens existing smartphone");
        await Press(Key.P);
        var pickup=SceneObjects<InventoryFramework.PickupItem>().Where(p=>p.contextual).OrderBy(p=>Vector3.Distance(player.position,p.transform.position)).First();
        Approach(player,pickup.transform.position);var reward=pickup.item;int owned=inventory.CountItems(reward);
        var original=inventory.slots.Select(s=>s.instance).ToArray();
        foreach(var s in inventory.slots)s.Set(tutorial==null?FirstDayStudioAssets.Load().supplies[1]:reward,99);
        await Press(Key.E);Check(pickup!=null&&pickup.gameObject.activeSelf,"full-bag E preserves ground pickup");
        for(int i=0;i<original.Length;i++)inventory.slots[i].SetInstance(original[i]);inventory.RefreshAllUI();
        await Press(Key.E);Check(inventory.CountItems(reward)==owned+1,"retry pickup grants exactly once");
        await Press(Key.Digit2);
        var tree=SceneObjects<Gatherable>().Where(g=>g.IsDirectWorld).OrderBy(g=>Vector3.Distance(player.position,g.transform.position)).First();
        Approach(player,tree.transform.position);int wood=inventory.CountItems(Resources.Load<Item>("Items/Item_Wood"));
        for(int i=0;i<3;i++){await Press(Key.E);await Task.Delay(350);}
        Check(tree.DirectDepleted&&inventory.CountItems(Resources.Load<Item>("Items/Item_Wood"))==wood+1,"existing three-hit timber reward/depletion");
        await Capture("06-forest-exploration.png");
        await Hold(800,Key.W,Key.LeftShift);await Press(Key.Space);await Task.Delay(700);
        Check(player.GetComponent<PlayerController>().JumpCount>0,"world traversal permits jump");
        Check(Object.FindFirstObjectByType<WorldHotbarPlacementController>()==null,"PASS 2 placement not activated");
        Check(Object.FindFirstObjectByType<FirstIslandSettlementController>()==null,"legacy 16x16 settlement not loaded");
        await Capture("07-island-control.png");
        await Press(Key.Digit3);
        var stone=SceneObjects<MiningSpot>().Where(m=>m.IsDirectWorld).OrderBy(m=>Vector3.Distance(player.position,m.transform.position)).First();
        Approach(player,stone.transform.position);int ore=inventory.CountItems(Resources.Load<Item>("Items/Item_Ore"));
        for(int i=0;i<3;i++){await Press(Key.E);await Task.Delay(350);}
        Check(stone.DirectDepleted&&inventory.CountItems(Resources.Load<Item>("Items/Item_Ore"))==ore+1,"existing pickaxe reward/depletion through E");
        await Press(Key.Digit4);var fishing=SceneObjects<FishingSpot>().First(f=>f.IsDirectPlayerDemo);
        Approach(player,fishing.transform.position);await Press(Key.E);await Task.Delay(1600);Check(fishing.BiteReady,"rod casts through contextual E");
        await Press(Key.E);Check(inventory.CountItems(Resources.Load<Item>("Items/Item_Fish"))==2,"existing fishing authority grants two fish");
        await Press(Key.Digit5);var bug=SceneObjects<BugCritter>().First();Approach(player,bug.transform.position);await Press(Key.E);
        Check(bug.Captured&&inventory.CountItems(Resources.Load<Item>("Items/Item_Butterfly"))==1,"net catches actual roaming bug through E");
        for(int i=5;i<8;i++){await Press(Key.Digit1+i);Check(equipment.HeldVisual!=null,"supply blueprint/kit held model "+(i+1));}
        await Press(Key.X);
        for(int i=0;i<3;i++){await Hold(2300,Key.W,Key.LeftShift);await Press(Key.Space);await Task.Delay(700);}
        await Capture("08-meadow-exploration.png");
        var generated=WorldPersistenceService.Instance.ActiveGeneratedWorld;
        foreach(var kind in new[]{WorldGenerationAnchorKind.ForestActivity,WorldGenerationAnchorKind.HighlandActivity,WorldGenerationAnchorKind.MeadowActivity})
        {
            if(!generated.TryGetAnchor(kind,out var anchor)||!alpha.Grid.CellToWorld(anchor.Coordinate,out var ground))continue;
            Pose(player,ground+Vector3.up*.05f,ground+Vector3.forward*5);await Task.Delay(600);
            before=player.position;await Hold(1200,Key.W,Key.LeftShift);await Press(Key.Space);await Task.Delay(700);
            Check(Vector3.Distance(before,player.position)>1,"actual traversal at "+kind);
            await Capture("09-region-"+kind+".png");
        }
        Check(!FirstDayWorldPresentation.Instance.Supply.gameObject.activeSelf,"collected supply finished shrinking out of scene");
        var world=FirstDayWorldPresentation.Instance;
        File.WriteAllText(Output+"/world-observation.txt","Harbor="+world.Harbor+"\nPlayer="+player.position+"\nResources="+generated.ResourceSpawns.Count+"\nTreeBindings="+SceneObjects<Gatherable>().Length+"\nStoneBindings="+SceneObjects<MiningSpot>().Length+"\nVisible world labels="+string.Join(",",SceneObjects<PrototypeWorldLabel>().Select(p=>p.name))+"\nCanvases="+string.Join(",",SceneObjects<Canvas>().Where(c=>c.enabled).Select(c=>c.name))+"\n");
    }
    static T[] SceneObjects<T>() where T:Component => Resources.FindObjectsOfTypeAll<T>().Where(c=>c.gameObject.scene.IsValid()&&c.gameObject.activeInHierarchy).ToArray();
    static async Task RecordGameView()
    {
        string folder=Path.GetFullPath(Output+"/frames");Directory.CreateDirectory(folder);
        double start=EditorApplication.timeSinceStartup;int frame=0;
        try
        {
            while(record&&EditorApplication.isPlaying&&EditorApplication.timeSinceStartup-start<75)
            {
                string name=(frame++).ToString("D5")+".png";
                ScreenCapture.CaptureScreenshot(folder+"/"+name);
                File.AppendAllText(Output+"/frame-times.tsv",name+"\t"+(EditorApplication.timeSinceStartup-start).ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+"\n");
                await Task.Delay(100);
            }
        }
        catch(Exception ex){File.WriteAllText(Output+"/video-capture-note.txt",ex.ToString());}
    }
    static void Pose(Transform player,Vector3 position,Vector3 target)
    {
        var cc=player.GetComponent<CharacterController>();cc.enabled=false;
        player.position=position;Vector3 dir=Vector3.ProjectOnPlane(target-position,Vector3.up);
        if(dir.sqrMagnitude>.01f)player.rotation=Quaternion.LookRotation(dir);
        cc.enabled=true;player.GetComponent<PlayerController>().ResetMotionAfterTeleport();
        var guard=player.GetComponent<WorldPlayerTraversalGuard>();if(guard!=null)guard.TryTeleportTo(position);
        Camera.main.GetComponent<CameraController>().SnapToTarget();Physics.SyncTransforms();
    }
    static void Approach(Transform player,Vector3 target)
    {
        Vector3 position=target+Vector3.back*.95f;
        var grid=WorldAlphaPlayableController.Instance?.Grid;
        if(grid!=null&&grid.WorldToCell(position,out var cell)&&grid.CellToWorld(cell,out var ground))position.y=ground.y+.05f;
        else position.y=.05f;
        Pose(player,position,target);
    }
    static async Task Press(Key key)=>await Hold(100,key);
    static async Task Hold(int ms,params Key[] keys)
    {keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));await Task.Delay(ms);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(100);}
    static async Task Until(Func<bool> condition,string name,int seconds=20)
    {double deadline=EditorApplication.timeSinceStartup+seconds;while(!condition()&&EditorApplication.timeSinceStartup<deadline){if(SessionState.GetInt(Active+".Errors",0)>0)throw new Exception("Runtime errors: see errors.txt while "+name);await Task.Delay(80);}if(!condition())throw new Exception(name);Check(true,name);}
    static async Task Capture(string name)
    {
        var view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Focus();view.Repaint();
        await Task.Delay(500);string path=Path.GetFullPath(Output+"/"+name);ScreenCapture.CaptureScreenshot(path);
        double deadline=EditorApplication.timeSinceStartup+8;
        while(!File.Exists(path)&&EditorApplication.timeSinceStartup<deadline){view.Repaint();EditorApplication.QueuePlayerLoopUpdate();await Task.Delay(100);}
        Check(File.Exists(path),"GameView capture "+name);
    }
    static void Check(bool ok,string message)
    {if(!ok)SessionState.SetBool(Active+".Failed",true);checks++;File.AppendAllText(Output+"/checks.txt",(ok?"PASS ":"FAIL ")+message+"\n");Debug.Log("[FIRST-DAY] "+(ok?"PASS ":"FAIL ")+message);}
}
