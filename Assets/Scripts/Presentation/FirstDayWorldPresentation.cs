using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Canon v2 §6: seeded art projection over the existing Demo256 cells and resource identities.
// No replacement world, inventory, placement, clock or persistence authority.
public sealed class FirstDayWorldPresentation : MonoBehaviour
{
    public static FirstDayWorldPresentation Instance { get; private set; }
    public Vector3 Harbor { get; private set; }
    public FirstDaySupplyBox Supply { get; private set; }
    public IReadOnlyList<string> CompanionIds => DemoRouteController.SelectedCompanionIds;
    readonly List<Transform> _chunks = new List<Transform>();
    readonly List<GameObject> _approachPlants = new List<GameObject>();
    readonly HashSet<GameObject> _coveredPlants = new HashSet<GameObject>();
    Transform _player;
    WorldGridService _grid;
    FirstDayStudioAssets _assets;
    TextMeshProUGUI _toast;
    float _toastUntil, _refreshAt;
    string _lastBiome;
    static readonly Color Ink = new Color(.14f,.24f,.25f);
    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void Compose(WorldGridService grid, GameObject player)
    {
        _grid = grid; _player = player.transform; _assets = FirstDayStudioAssets.Load();
        var generated = WorldPersistenceService.Instance.ActiveGeneratedWorld;
        generated.TryGetAnchor(WorldGenerationAnchorKind.Start, out var start);
        int x = start.Coordinate.x;
        Vector2Int harborCell = start.Coordinate;
        for (int z = 4; z < start.Coordinate.y; z++)
        {
            var c = new Vector2Int(x,z);
            if (grid.TryGetCell(c, out var cell) && cell.IsWalkable && !cell.HasWater &&
                grid.TryGetCell(c + Vector2Int.up * 3, out var inland) && inland.IsWalkable && !inland.HasWater)
            { harborCell = c + Vector2Int.up * 2; break; }
        }
        grid.CellToWorld(harborCell, out var harbor);
        Harbor = harbor;
        _player.position = harbor + new Vector3(0,.05f,2);
        var dock = FirstDayStudioAssets.Place(_assets.dock, transform, harbor + new Vector3(0,-.3f,-5), .65f, 90);
        var boat = FirstDayStudioAssets.Place(_assets.boat, transform, harbor + new Vector3(-6,-.35f,-8), 4.5f, 90);
        boat.name = "PA_HarborBoat"; dock.name = "PA_HarborDock";
        var supply = new GameObject("PA_PioneerSupply", typeof(BoxCollider), typeof(FirstDaySupplyBox));
        supply.transform.SetParent(transform); supply.transform.position = harbor + new Vector3(2.8f,0,3);
        supply.GetComponent<BoxCollider>().size = new Vector3(1.2f,1,1.2f);
        supply.GetComponent<BoxCollider>().center = Vector3.up * .5f;
        Supply = supply.GetComponent<FirstDaySupplyBox>();
        ComposeCompanions();
        ComposeDressing(generated);
        ComposeApproachDressing(generated);
        ComposeGroundPickups();
        var canvas = new GameObject("FirstDay_Dispatch",typeof(Canvas),typeof(CanvasScaler));
        canvas.transform.SetParent(transform); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder = 30;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);
        var label = new GameObject("DispatchToast",typeof(RectTransform),typeof(TextMeshProUGUI));
        label.transform.SetParent(canvas.transform,false);
        var rect=(RectTransform)label.transform; rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);
        rect.anchoredPosition=new Vector2(0,-70);rect.sizeDelta=new Vector2(1000,85);
        _toast=label.GetComponent<TextMeshProUGUI>();_toast.font=TMP_Settings.defaultFontAsset;
        _toast.fontSize=26;_toast.color=Ink;_toast.alignment=TextAlignmentOptions.Center;_toast.raycastTarget=false;
        Toast("DAY 1 · 새로운 섬\n항구의 P.A. 보급 상자를 열어보세요.");
        RenderSettings.ambientLight=new Color(.74f,.8f,.81f);
        RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogStartDistance=90; RenderSettings.fogEndDistance=180;
        RenderSettings.fogColor=new Color(.69f,.83f,.86f);
        Camera.main.backgroundColor=RenderSettings.fogColor;
        Camera.main.farClipPlane=220;
        if(Camera.main.GetComponent<AudioListener>()==null)Camera.main.gameObject.AddComponent<AudioListener>();
        // Demo 구성도 기존 시간별 조명 권위를 명시 연결한다. 고정 낮 조명으로 덮어쓰지 않는다.
        var sun = FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
        if (sun != null)
        {
            var daylight = sun.GetComponent<DayNightVisual>() ?? sun.gameObject.AddComponent<DayNightVisual>();
            daylight.directionalLight = sun;
            daylight.ApplyTime(GameClock.Instance != null ? GameClock.Instance.CurrentHour : 9f);
        }
    }
    void ComposeCompanions()
    {
        var selected = DemoRouteController.SelectedCompanions;
        for (int i=0;i<selected.Length;i++)
        {
            var candidate=selected[i];
            var npc=new GameObject("Companion_"+candidate.id);
            npc.transform.SetParent(transform);npc.transform.position=Harbor+new Vector3(i==0?-2:4,0,5);
            var visual=FirstDayStudioAssets.Place(candidate.model,npc.transform,npc.transform.position,1.75f,180);
            npc.AddComponent<NpcHumanoidProceduralAnimator>();
            var col=npc.AddComponent<CapsuleCollider>();col.center=Vector3.up*.85f;col.height=1.7f;col.radius=.3f;
            var dialogue=npc.AddComponent<NpcDialogue>();dialogue.overrideProfile=candidate.profile;
            dialogue.interactPrompt=candidate.displayName+"와 대화";
            dialogue.dialogueData=Resources.Load<DialogueData>("Dialogues/Dialogue_"+candidate.id.Split('_')[0]);
            dialogue.GreetingInteracted+=(d,p)=>Toast(d.LastLine);
        }
    }
    void ComposeDressing(WorldGenerationResult generated)
    {
        var random=new System.Random(9009);
        var chunks=new Dictionary<Vector2Int,Transform>();
        var occupied=new HashSet<Vector2Int>(generated.ResourceSpawns.Select(s=>s.Coordinate));
        for(int z=4;z<_grid.Definition.Height-4;z+=3)
        for(int x=4;x<_grid.Definition.Width-4;x+=3)
        {
            var c=new Vector2Int(x,z);
            if(!_grid.TryGetCell(c,out var cell)||!cell.IsWalkable||cell.HasWater||cell.HasPath||occupied.Contains(c)) continue;
            if(!generated.TryGetCell(c,out var biomeCell))continue;
            float density=biomeCell.Biome==WorldBiomeType.Forest?.92f:biomeCell.Biome==WorldBiomeType.Highland?.8f:biomeCell.Biome==WorldBiomeType.Coast?.35f:.75f;
            if(random.NextDouble()>density)continue;
            bool steepOrCoastal=false;
            foreach(var offset in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})
                if(!_grid.TryGetCell(c+offset*2,out var neighbor)||neighbor.HasWater||Mathf.Abs(neighbor.ElevationLevel-cell.ElevationLevel)>1){steepOrCoastal=true;break;}
            if(steepOrCoastal)continue;
            _grid.CellToWorld(c,out var p);
            if(Vector3.Distance(p,Harbor)<13 || Mathf.Abs(p.x-Harbor.x)<3.5f && p.z>Harbor.z && p.z<Harbor.z+85) continue;
            if(generated.Anchors.Any(a=>Vector2Int.Distance(a.Coordinate,c)<9)) continue;
            var key=new Vector2Int(x/16,z/16);
            if(!chunks.TryGetValue(key,out var parent))
            { parent=new GameObject("Nature_"+key).transform;parent.SetParent(transform);parent.position=new Vector3(key.x*32+16,0,key.y*32+16);chunks.Add(key,parent);_chunks.Add(parent); }
            float roll=(float)random.NextDouble();
            float height; GameObject prefab;
            // Directional composition complements the stable terrain profile; resource records are untouched.
            bool forest=p.x<Harbor.x-18 && p.z>Harbor.z+25;
            bool highland=p.x>Harbor.x+30 && p.z>Harbor.z+90;
            if(forest && roll<.72f || !highland && roll<.14f)
            { prefab=_assets.trees[random.Next(_assets.trees.Length)];height=3.8f+(float)random.NextDouble()*2.5f; }
            else if(highland && roll<.58f)
            { prefab=_assets.rocks[random.Next(_assets.rocks.Length)];height=.6f+(float)random.NextDouble()*1.5f; }
            else if(roll<.4f) {prefab=_assets.bushes[random.Next(_assets.bushes.Length)];height=.45f+(float)random.NextDouble()*.4f;}
            else {var family=p.x>Harbor.x+12 ? _assets.flowers:_assets.grasses;prefab=family[random.Next(family.Length)];height=.2f+(float)random.NextDouble()*.25f;}
            p+=new Vector3((float)random.NextDouble()*2-1,0,(float)random.NextDouble()*2-1);
            var go=FirstDayStudioAssets.Place(prefab,parent,p,height,random.Next(360));
            if(height>2.8f)
            {var col=go.AddComponent<CapsuleCollider>();float scale=go.transform.lossyScale.x;col.height=2/scale;col.radius=.25f/scale;col.center=Vector3.up/scale;}
            // Small clusters break the repeated sample spacing without blocking paths.
            if(roll<.65f)
                for(int j=0;j<3;j++) FirstDayStudioAssets.Place(_assets.grasses[j%_assets.grasses.Length],parent,p+new Vector3((float)random.NextDouble()*3-1.5f,0,(float)random.NextDouble()*3-1.5f),.25f,random.Next(360));
        }
    }
    // Canon v2 §6: 항구 양옆과 정착 초지 가장자리만 낮은 군락으로 연결한다.
    // 길/자원/충돌/점유를 추가하지 않으며 플레이어가 배치하면 해당 군락은 숨긴다.
    void ComposeApproachDressing(WorldGenerationResult generated)
    {
        var resources = new HashSet<Vector2Int>(generated.ResourceSpawns.Select(s => s.Coordinate));
        var random = new System.Random(9010);
        for (int side = -1; side <= 1; side += 2)
        for (int band = 0; band < 7; band++)
        {
            float z = 3f + band * 5f;
            float x = side * (band < 3 ? 7f + band : 15f + (band % 2) * 2f);
            Vector3 center = Harbor + new Vector3(x, 0f, z);
            for (int i = 0; i < 6; i++)
            {
                Vector3 point = center + new Vector3((float)random.NextDouble() * 3f - 1.5f,
                    0f, (float)random.NextDouble() * 2.4f - 1.2f);
                if (!_grid.WorldToCell(point, out var coordinate) || resources.Contains(coordinate) ||
                    !_grid.TryGetCell(coordinate, out var cell) || !cell.IsWalkable || cell.HasPath ||
                    !_grid.CellToWorld(coordinate, out var ground)) continue;
                point.y = ground.y;
                GameObject prefab = i == 0 ? _assets.bushes[band % _assets.bushes.Length] :
                    i == 1 && band > 2 ? _assets.flowers[0] : _assets.grasses[i % _assets.grasses.Length];
                var plant = FirstDayStudioAssets.Place(prefab, transform, point,
                    i == 0 ? .65f : .26f + (float)random.NextDouble() * .12f, random.Next(360));
                plant.name = "HarborMeadow_" + prefab.name;
                foreach (var collider in plant.GetComponentsInChildren<Collider>()) collider.enabled = false;
                _approachPlants.Add(plant);
            }
        }
    }

    void RefreshApproachDressing()
    {
        foreach (var plant in _approachPlants)
        {
            if (_coveredPlants.Contains(plant)) continue;
            if (plant == null || !_grid.WorldToCell(plant.transform.position, out var coordinate)) continue;
            bool clear = true;
            for (int z = -1; z <= 1 && clear; z++)
            for (int x = -1; x <= 1; x++)
                if (!_grid.TryGetCell(coordinate + new Vector2Int(x, z), out var cell) ||
                    !cell.IsWalkable || cell.HasPath) { clear = false; break; }
            if (plant.activeSelf != clear) plant.SetActive(clear);
        }
    }

    // Canon v2 §7: presentation 소유 식생만 숨긴다. 자원 상태/월드 점유는 변경하지 않는다.
    public void ClearShopSite(DemoShopInterior shop)
    {
        foreach (var chunk in _chunks)
            foreach (Transform plant in chunk)
                if (shop.CoversGround(plant.position)) plant.gameObject.SetActive(false);
        foreach (var plant in _approachPlants)
            if (plant != null && shop.CoversGround(plant.transform.position))
            {
                _coveredPlants.Add(plant);
                plant.SetActive(false);
            }

        // 느슨한 줍기 아이템은 같은 인스턴스/수량으로 가장 가까운 빈 바닥에 보존한다.
        var reserved = new HashSet<Vector2Int>();
        foreach (var pickup in GetComponentsInChildren<InventoryFramework.PickupItem>())
        {
            if (!shop.CoversGround(pickup.transform.position) ||
                !_grid.WorldToCell(pickup.transform.position, out var origin)) continue;
            bool moved = false;
            for (int radius = 1; radius <= 12 && !moved; radius++)
            for (int z = -radius; z <= radius && !moved; z++)
            for (int x = -radius; x <= radius && !moved; x++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(z) != radius) continue;
                var coordinate = origin + new Vector2Int(x, z);
                if (reserved.Contains(coordinate) || !_grid.TryGetCell(coordinate, out var cell) ||
                    !cell.IsWalkable || cell.HasWater || cell.HasPath ||
                    !_grid.CellToWorld(coordinate, out var ground) || shop.CoversGround(ground)) continue;
                pickup.transform.position = ground;
                reserved.Add(coordinate);
                moved = true;
            }
            if (!moved) Debug.LogWarning("[DemoShopInterior] No nearby clear ground for pickup; original item preserved.");
        }
    }

    void ComposeGroundPickups()
    {
        for(int i=0;i<9;i++)
        {
            var p=Harbor+new Vector3((i%2==0?-1:1)*(4+i*.4f),0,10+i*5);
            if(!_grid.WorldToCell(p,out var cell)||!_grid.CellToWorld(cell,out p))continue;
            var go=FirstDayStudioAssets.Place(i%3==0?_assets.rocks[0]:_assets.wood,transform,p,.18f,i*47);
            var col=go.AddComponent<SphereCollider>();col.radius=.22f/go.transform.lossyScale.x;col.isTrigger=true;
            var pickup=go.AddComponent<InventoryFramework.PickupItem>();pickup.contextual=true;pickup.amount=1;
            pickup.item=Resources.Load<Item>(i%3==0?"Items/Item_Ore":"Items/Item_Wood");
        }
    }
    public static void Toast(string message)
    { if(Instance==null||Instance._toast==null)return;Instance._toast.text=message;Instance._toastUntil=Time.unscaledTime+4; }

    // 성공한 기존 Inventory 지급을 표현한다. 표시 모델에는 보상/줍기 컴포넌트를 추가하지 않는다.
    public static void Acquired(Item item, int amount, Vector3 source)
    {
        if (Instance == null || item == null) return;
        Toast($"{item.itemName} +{amount} · 보유 {Inventory.instance.CountItems(item)}");
        Instance.StartCoroutine(Instance.ShowAcquisition(item, source));
    }

    System.Collections.IEnumerator ShowAcquisition(Item item, Vector3 source)
    {
        var model = _assets.ModelFor(item);
        if (model == null || _player == null) yield break;
        var visual = FirstDayStudioAssets.Place(model, transform, source + Vector3.up * .6f, .24f);
        visual.name = "AcquisitionVisual";
        foreach (var collider in visual.GetComponentsInChildren<Collider>()) collider.enabled = false;
        Vector3 start = visual.transform.position;
        float elapsed = 0;
        while (elapsed < .65f && _player != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / .65f);
            visual.transform.position = Vector3.Lerp(start, _player.position + Vector3.up, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * .75f;
            yield return null;
        }
        Destroy(visual);
    }
    void Update()
    {
        if(_player==null)return;
        var settlement = DemoSettlementController.Instance;
        bool reportOpen = settlement != null && settlement.IsPanelOpen && settlement.PioneerReport != null;
        if(_toast!=null)_toast.gameObject.SetActive(!reportOpen && Time.unscaledTime<_toastUntil);
        if(Time.unscaledTime<_refreshAt)return;_refreshAt=Time.unscaledTime+.4f;
        RefreshApproachDressing();
        foreach(var chunk in _chunks)chunk.gameObject.SetActive(Vector3.SqrMagnitude(chunk.position-_player.position)<155*155);
        if(DemoSettlementController.Instance==null && GameClock.Instance!=null && GameClock.Instance.CurrentHour>=16)GameClock.Instance.enabled=false;
        foreach(var label in FindObjectsByType<PrototypeWorldLabel>(FindObjectsSortMode.None))
            if(label.GetComponentInParent<ShopSlot>() == null) label.gameObject.SetActive(false);
        var testAgent=_grid.GetComponent<WorldNavigationService>()?.TestAgent;
        if(testAgent!=null)
        {
            foreach(var renderer in testAgent.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            foreach(var collider in testAgent.GetComponentsInChildren<Collider>())collider.enabled=false;
        }
        var loop=DayNightShopLoopController.Instance;
        if(loop!=null && loop.phaseText!=null)
        {
            var phaseCanvas=loop.phaseText.GetComponentInParent<Canvas>();
            if(phaseCanvas!=null && phaseCanvas.name=="DayNightShopLoopCanvas")
                phaseCanvas.enabled=false;
            else
            {
                loop.phaseText.gameObject.SetActive(false);
                if(loop.activityText!=null)loop.activityText.gameObject.SetActive(false);
            }
        }
        if(MoneyHUD.instance!=null)
        {
            if(MoneyHUD.instance.tierText!=null)MoneyHUD.instance.tierText.gameObject.SetActive(false);
            if(MoneyHUD.instance.tierGoalText!=null)MoneyHUD.instance.tierGoalText.gameObject.SetActive(false);
        }
        string biome="초원";
        if(_grid.WorldToCell(_player.position,out var coordinate)&&WorldPersistenceService.Instance.ActiveGeneratedWorld.TryGetCell(coordinate,out var cell))
            biome=cell.Biome==WorldBiomeType.Coast?"해안":cell.Biome==WorldBiomeType.Forest?"숲":cell.Biome==WorldBiomeType.Highland?"고지대":"초원";
        if(biome!=_lastBiome){_lastBiome=biome;if(Time.unscaledTime>_toastUntil)Toast(biome+" · 자유롭게 둘러보세요");}
    }
}

