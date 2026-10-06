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
    RectTransform _toastCard;
    // Canon v2 §22: HUD = 순간 알림, Smartphone = 기록. 최근 알림을 휴대폰 피드에서 다시 볼 수 있게 남긴다.
    public static readonly List<string> DispatchLog = new List<string>();
    // 기록이 남은 횟수(최근 8줄 보관과 별개로 계속 증가). 휴대폰 홈의 '새 기록' 표시가 비교한다.
    public static int DispatchSerial { get; private set; }
    float _toastUntil, _refreshAt;
    string _lastBiome;
    string _ambience;
    static readonly Color Ink = new Color(.14f,.24f,.25f);
    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void Compose(WorldGridService grid, GameObject player)
    {
        _grid = grid; _player = player.transform; _assets = FirstDayStudioAssets.Load();
        RefreshAmbience();
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
        // A waiting companion must walk around the visible chest, not stand inside it and intercept E.
        var supplyObstacle = supply.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        supplyObstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
        supplyObstacle.center = Vector3.up * .5f;
        supplyObstacle.size = new Vector3(1.4f, 1, 1.4f);
        supplyObstacle.carving = true;
        Supply = supply.GetComponent<FirstDaySupplyBox>();
        ComposeCompanions();
        ComposeDressing(generated);
        ComposeApproachDressing(generated);
        ComposeSettlementEdgeDressing(generated);
        ComposeGroundPickups();
        // P3: 채집 중 캐릭터·도구가 큰 나무 수관에 가리지 않게(장식 나무 + 직접 채집 나무).
        var foliage = GetComponent<FoliageOcclusion>();
        if (foliage == null) foliage = gameObject.AddComponent<FoliageOcclusion>();
        foliage.Configure(_player, _chunks);
        var canvas = new GameObject("FirstDay_Dispatch",typeof(Canvas),typeof(CanvasScaler));
        canvas.transform.SetParent(transform); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder = 30;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);
        // D4: 화면 중앙 글자 대신 우상단(돈 칩 아래) 크림색 알림 카드. 배경이 있어 어떤 지형 위에서도 읽힌다.
        var card = new GameObject("DispatchToastCard",typeof(RectTransform),typeof(Image));
        card.transform.SetParent(canvas.transform,false);
        _toastCard=(RectTransform)card.transform; _toastCard.anchorMin=_toastCard.anchorMax=_toastCard.pivot=new Vector2(1,1);
        _toastCard.anchoredPosition=new Vector2(-24,-96); _toastCard.sizeDelta=new Vector2(440,72);
        var cardImage=card.GetComponent<Image>(); cardImage.sprite=SmartphoneUI.RoundedSprite; cardImage.type=Image.Type.Sliced;
        cardImage.color=new Color(.98f,.95f,.87f,.96f); cardImage.raycastTarget=false;
        var accent = new GameObject("Accent",typeof(RectTransform),typeof(Image));
        accent.transform.SetParent(card.transform,false);
        var accentRect=(RectTransform)accent.transform; accentRect.anchorMin=new Vector2(0,0); accentRect.anchorMax=new Vector2(0,1);
        accentRect.offsetMin=new Vector2(10,12); accentRect.offsetMax=new Vector2(16,-12);
        var accentImage=accent.GetComponent<Image>(); accentImage.color=SmartphoneUI.Teal; accentImage.raycastTarget=false;
        var label = new GameObject("DispatchToast",typeof(RectTransform),typeof(TextMeshProUGUI));
        label.transform.SetParent(card.transform,false);
        var rect=(RectTransform)label.transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one;
        rect.offsetMin=new Vector2(28,10); rect.offsetMax=new Vector2(-16,-10);
        _toast=label.GetComponent<TextMeshProUGUI>();_toast.font=TMP_Settings.defaultFontAsset;
        _toast.fontSize=21;_toast.color=Ink;_toast.alignment=TextAlignmentOptions.MidlineLeft;_toast.raycastTarget=false;
        gameObject.AddComponent<FirstDayHudStyle>();
        DispatchLog.Clear(); DispatchSerial = 0; // 새 Opening 세션의 기록만 휴대폰에 남긴다.
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
            dialogue.GreetingInteracted+=(d,p)=>Toast(d.LastLine,false);
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
            // P10: 정착 자리(시작·상점)만 넓게 비우고 활동 랜드마크는 가까이까지 채운다(빈 원이 '테스트 맵'처럼 보였다).
            if(generated.Anchors.Any(a=>Vector2Int.Distance(a.Coordinate,c)<(a.Kind==WorldGenerationAnchorKind.Start||a.Kind==WorldGenerationAnchorKind.Shop?9:4))) continue;
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
            {var col=go.AddComponent<CapsuleCollider>();float scale=go.transform.lossyScale.x;col.height=2/scale;col.radius=.25f/scale;col.center=Vector3.up/scale;FirstDayToolSurface.Attach(go,false);}
            else if(System.Array.IndexOf(_assets.rocks,prefab)>=0) FirstDayToolSurface.Attach(go,true);
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

    void ComposeSettlementEdgeDressing(WorldGenerationResult generated)
    {
        if (!generated.TryGetAnchor(WorldGenerationAnchorKind.Start,out var start)) return;
        _grid.CellToWorld(start.Coordinate,out var centre);
        var parent=new GameObject("SettlementEdgeNature").transform;
        parent.SetParent(transform); parent.position=centre; _chunks.Add(parent);
        var random=new System.Random(9011);
        var resources=new HashSet<Vector2Int>(generated.ResourceSpawns.Select(r=>r.Coordinate));
        // Low, nonblocking clusters suggest the edges of the initial clearing.
        // Existing placement occupancy hides them; they own no resource state.
        for(int sector=0;sector<10;sector++)
        for(int plant=0;plant<7;plant++)
        {
            float angle=(sector*.6283f)+(float)random.NextDouble()*.18f;
            float radius=8f+(float)random.NextDouble()*6f;
            var point=centre+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
            if (!_grid.WorldToCell(point,out var cell) || resources.Contains(cell) ||
                !_grid.TryGetCell(cell,out var data) || !data.IsWalkable || data.HasWater || data.HasPath ||
                !_grid.CellToWorld(cell,out var ground)) continue;
            point.y=ground.y;
            bool edgeTree=plant==6 && sector%2==1;
            var prefab=edgeTree?_assets.trees[sector%_assets.trees.Length]:plant==0?_assets.bushes[sector%_assets.bushes.Length]:
                plant==1?_assets.flowers[sector%_assets.flowers.Length]:_assets.grasses[plant%_assets.grasses.Length];
            var go=FirstDayStudioAssets.Place(prefab,parent,point,edgeTree?3.7f:plant==0?.62f:.25f+(float)random.NextDouble()*.12f,random.Next(360));
            go.name="SettlementEdge_"+prefab.name;
            foreach(var collider in go.GetComponentsInChildren<Collider>()) collider.enabled=false;
            if(edgeTree) FirstDayToolSurface.Attach(go,false);
            _approachPlants.Add(go);
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
    // P7: 동행 대사는 진행 안내 토스트를 덮지 않도록 카드가 비었을 때만 말한다.
    public static bool ToastBusy => Instance != null && Time.unscaledTime < Instance._toastUntil;

    // record=false: 획득·지형·대화처럼 자주 뜨는 순간 피드백은 휴대폰 기록(놓친 목표 확인용)을 밀어내지 않게 남기지 않는다.
    public static void Toast(string message, bool record = true)
    {
        if(Instance==null||Instance._toast==null||string.IsNullOrEmpty(message))return;
        string shown=WrapWords(Instance._toast,message,392);
        Instance._toast.text=shown;Instance._toastUntil=Time.unscaledTime+3.5f;
        // 카드 높이를 문장 길이에 맞춘다(1~3줄).
        float height=Instance._toast.GetPreferredValues(shown,396,0).y+20;
        Instance._toastCard.sizeDelta=new Vector2(440,Mathf.Clamp(height,60,140));
        if(!record)return;
        string clock=GameClock.Instance!=null?$"{GameClock.Instance.CurrentHourInt:D2}:{Mathf.FloorToInt((GameClock.Instance.CurrentHour-GameClock.Instance.CurrentHourInt)*60):D2}  ":"";
        DispatchLog.Insert(0,clock+message.Replace("\n"," "));DispatchSerial++;
        if(DispatchLog.Count>8)DispatchLog.RemoveAt(DispatchLog.Count-1);
    }

    // TMP는 한글을 글자 단위로 줄바꿈한다("고/르세요"). 토스트는 띄어쓰기 단위로 미리 줄을 나눈다.
    internal static string WrapWords(TextMeshProUGUI label,string message,float width)
    {
        var result=new System.Text.StringBuilder();
        foreach(var paragraph in message.Split('\n'))
        {
            string line="";
            foreach(var word in paragraph.Split(' '))
            {
                string next=line.Length==0?word:line+" "+word;
                if(line.Length>0&&label.GetPreferredValues(next).x>width){result.Append(line).Append('\n');line=word;}
                else line=next;
            }
            result.Append(line).Append('\n');
        }
        return result.ToString().TrimEnd('\n');
    }

    // 성공한 기존 Inventory 지급을 표현한다. 표시 모델에는 보상/줍기 컴포넌트를 추가하지 않는다.
    public static void Acquired(Item item, int amount, Vector3 source)
    {
        if (Instance == null || item == null) return;
        Toast($"{ItemDisplayName.For(item)} +{amount} · 보유 {Inventory.instance.CountItems(item)}", false);
        Instance.StartCoroutine(Instance.ShowAcquisition(item, source));
    }

    System.Collections.IEnumerator ShowAcquisition(Item item, Vector3 source)
    {
        var model = _assets.ModelFor(item);
        if (model == null || _player == null) yield break;
        var visual = FirstDayStudioAssets.Place(model, transform, source + Vector3.up * .6f, .24f);
        visual.name = "AcquisitionVisual";
        foreach (var collider in visual.GetComponentsInChildren<Collider>()) collider.enabled = false;
        // 얇은 모델(나비 등)은 높이 기준으로 키우면 폭이 수 m가 된다. 가장 긴 변을 0.5m로 제한한다.
        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (longest > .5f) visual.transform.localScale *= .5f / longest;
        }
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
        if(_toastCard!=null)_toastCard.gameObject.SetActive(!reportOpen && Time.unscaledTime<_toastUntil);
        if(Time.unscaledTime<_refreshAt)return;_refreshAt=Time.unscaledTime+.4f;
        RefreshAmbience();
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
        if(biome!=_lastBiome){_lastBiome=biome;if(Time.unscaledTime>_toastUntil)Toast(biome+" · 자유롭게 둘러보세요",false);}
    }

    void RefreshAmbience()
    {
        if (AudioManager.Instance == null) return;
        float hour = GameClock.Instance != null ? GameClock.Instance.CurrentHour : 9f;
        string next = hour >= 18f || hour < 6f ? AudioManager.OpeningNightAmbience : AudioManager.OpeningDayAmbience;
        if (_ambience == next) return;
        _ambience = next;
        AudioManager.PlayBGM(next);
    }
}

