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
    CoastalVegetationField _vegetation;
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
        ComposeLandingCargo();
        ComposeCompanions();
        ComposeDressing(generated);
        ComposeApproachDressing(generated);
        ComposeSettlementEdgeDressing(generated);
        ComposeCoastTreeline(generated);
        // Dinkum ref: instanced coastal grass clumps and shallow-water rocks/seaweed (presentation only).
        _vegetation = GetComponent<CoastalVegetationField>();
        if (_vegetation == null) _vegetation = gameObject.AddComponent<CoastalVegetationField>();
        _vegetation.Configure(_grid, generated, _player, Harbor, _assets);
        ApplyFoliageLook();
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
        // 2026-11-03 레퍼런스 06(알림): 금색 테두리(바깥 금색 면 + 안쪽 크림 면) · 가운데 정렬 · 굵은 본문 + 작은 보조 줄.
        var cardImage=card.GetComponent<Image>(); cardImage.sprite=SmartphoneUI.RoundedSprite; cardImage.type=Image.Type.Sliced;
        cardImage.color=PAUiTheme.Gold; cardImage.raycastTarget=false;
        card.AddComponent<Shadow>().effectDistance=new Vector2(0,-6);
        var inner = new GameObject("Inner",typeof(RectTransform),typeof(Image));
        inner.transform.SetParent(card.transform,false);
        var innerRect=(RectTransform)inner.transform; innerRect.anchorMin=Vector2.zero; innerRect.anchorMax=Vector2.one;
        innerRect.offsetMin=new Vector2(7,7); innerRect.offsetMax=new Vector2(-7,-7);
        var innerImage=inner.GetComponent<Image>(); innerImage.sprite=SmartphoneUI.RoundedSprite; innerImage.type=Image.Type.Sliced;
        innerImage.color=new Color(.99f,.96f,.88f,1f); innerImage.raycastTarget=false;
        var label = new GameObject("DispatchToast",typeof(RectTransform),typeof(TextMeshProUGUI));
        label.transform.SetParent(card.transform,false);
        var rect=(RectTransform)label.transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one;
        rect.offsetMin=new Vector2(22,10); rect.offsetMax=new Vector2(-22,-10);
        _toast=label.GetComponent<TextMeshProUGUI>();_toast.font=TMP_Settings.defaultFontAsset;
        _toast.fontSize=21;_toast.color=Ink;_toast.alignment=TextAlignmentOptions.Center;_toast.raycastTarget=false;
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
            // 항구에서 바다 쪽(-Z)을 보며 시작한다. 표시 모델만 돌리면 NavMeshAgent·FaceTowards가 돌리는 루트 정면과
            // 몸 정면이 반대가 되어 뒷걸음으로 걷는다(2026-11-02 사용자 "NPC가 거꾸로 다닌다"). 루트를 돌리고 표시 모델은 루트와 같은 방향에 둔다.
            npc.transform.rotation=Quaternion.Euler(0,180,0);
            var visual=FirstDayStudioAssets.Place(candidate.model,npc.transform,npc.transform.position,1.75f,180);
            npc.AddComponent<NpcHumanoidProceduralAnimator>();
            var col=npc.AddComponent<CapsuleCollider>();col.center=Vector3.up*.85f;col.height=1.7f;col.radius=.3f;
            var dialogue=npc.AddComponent<NpcDialogue>();dialogue.overrideProfile=candidate.profile;
            dialogue.interactPrompt=candidate.displayName+"와 대화";
            dialogue.dialogueData=Resources.Load<DialogueData>("Dialogues/Dialogue_"+candidate.id.Split('_')[0]);
            // 친밀도 키(대화 하루 1회·도구 선물·가게 구매가 쌓여 대화창 이름표 하트로 보인다). 같은 대사를 알림으로 한 번 더 띄우지 않는다
            // (2026-11-03: 대화창과 오른쪽 위 알림에 같은 문장이 겹쳐 보였다).
            dialogue.friendshipId="companion:"+candidate.id;
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
    // 소품 조사(PropsSurvey-20261025): 첫 화면이 빈 모래 위 보급 상자 하나뿐이었다. 상자 옆에 하선한 화물(세운 통 둘·누운 통 하나)을
    // 둬 항구에 막 내린 개척대로 읽히게 한다. 표시 전용이며, 보급 상자처럼 한 덩어리로 길을 막고 동행이 비켜 간다.
    // 독립 비평(Arrival-props-critic-20261025): 통이 상자보다 눈에 띄고 붙어 한 더미로 읽혔으며 가는 통나무는 막대로 보였다 →
    // 통을 상자 나무색 쪽으로 어둡게, 상자에서 약 .9m 떨어뜨리고(동행 통로), 통나무는 뺐다.
    static readonly Color CargoBarrelTint = new Color(.66f, .54f, .46f, 1f);
    static MaterialPropertyBlock _cargoBlock;

    void ComposeLandingCargo()
    {
        if (_assets.barrel == null) return;
        var cargo = new GameObject("PA_LandingCargo", typeof(BoxCollider));
        cargo.transform.SetParent(transform);
        cargo.transform.position = Harbor + new Vector3(5.8f, 0f, 3.4f);
        PlaceBarrel(cargo.transform, new Vector3(-.6f, 0f, .3f), .85f, 0f, true);
        PlaceBarrel(cargo.transform, new Vector3(.1f, 0f, -.2f), .78f, 30f, true);
        PlaceBarrel(cargo.transform, new Vector3(-.3f, 0f, -1f), .62f, 70f, false);
        var box = cargo.GetComponent<BoxCollider>();
        box.center = new Vector3(-.2f, .45f, -.3f); box.size = new Vector3(1.6f, .9f, 1.9f);
        var obstacle = cargo.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
        obstacle.center = box.center; obstacle.size = box.size + new Vector3(.2f, 0f, .2f);
        obstacle.carving = true;
    }

    // FoodKit 통은 옆으로 누운 채 만들어져 있어 세울 때는 Z로 90° 굴린다. 바닥 중심을 지정 위치에 맞추고,
    // 공유 재질은 그대로 둔 채 블록으로 색만 어둡게 한다(URP Lit _BaseColor).
    void PlaceBarrel(Transform parent, Vector3 offset, float height, float yaw, bool upright)
    {
        var barrel = Instantiate(_assets.barrel, parent);
        barrel.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * (upright ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity);
        var renderer = barrel.GetComponentInChildren<Renderer>();
        if (renderer == null) return;
        barrel.transform.localScale *= height / Mathf.Max(.001f, renderer.bounds.size.y);
        Bounds bounds = renderer.bounds;
        Vector3 target = parent.position + offset;
        barrel.transform.position += target - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        foreach (var collider in barrel.GetComponentsInChildren<Collider>()) collider.enabled = false;
        if (_cargoBlock == null) _cargoBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(_cargoBlock);
        _cargoBlock.SetColor("_BaseColor", CargoBarrelTint);
        renderer.SetPropertyBlock(_cargoBlock);
    }

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

    // Dinkum world pass(ref 03·05): 항구 해변 바로 위 초원 단의 앞줄에 나무 수관을 세워 바다·모래·풀·나무·캐릭터가
    // 기본 카메라 한 화면에 함께 읽히게 한다. 기존 장식 나무와 같은 충돌·도구 반응만 쓰고 보상/자원/저장은 없다.
    // 동행 경로는 NavMeshObstacle carving으로 피한다(보급 상자와 같은 방식). 항구 통로·길·자원 cell은 비운다.
    void ComposeCoastTreeline(WorldGenerationResult generated)
    {
        var resources = new HashSet<Vector2Int>(generated.ResourceSpawns.Select(s => s.Coordinate));
        var parent = new GameObject("CoastTreeline").transform;
        parent.SetParent(transform); parent.position = Harbor; _chunks.Add(parent);
        var random = new System.Random(9012);
        var placed = new List<Vector3>();
        if (!_grid.WorldToCell(Harbor, out var harborCell)) return;
        // v7 verifier: cone pines read unlike the reference's white-trunk broadleaf trees (02/13); the coast
        // row uses the catalog's birches only (falls back to any tree if none).
        var broadleaf = _assets.trees.Where(t => t != null && t.name.Contains("Birch")).ToArray();
        var family = broadleaf.Length > 0 ? broadleaf : _assets.trees;
        void PlaceTree(Vector3 point, float height, string label)
        {
            var prefab = family[random.Next(family.Length)];
            var go = FirstDayStudioAssets.Place(prefab, parent, point, height, random.Next(360));
            go.name = label + prefab.name;
            // FBX 루트의 축 변환(-90 X) 때문에 루트에 붙인 capsule은 눕는다. 월드 정렬 자식에 줄기 충돌을 둔다.
            var trunk = new GameObject("TrunkCollider");
            trunk.transform.SetPositionAndRotation(point, Quaternion.identity);
            trunk.transform.SetParent(go.transform, true);
            trunk.transform.localScale = Vector3.one / Mathf.Max(.001f, go.transform.lossyScale.x);
            var col = trunk.AddComponent<CapsuleCollider>(); col.height = 2f; col.radius = .3f; col.center = Vector3.up;
            var obstacle = trunk.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Capsule; obstacle.carving = true;
            obstacle.radius = .4f; obstacle.height = 2f; obstacle.center = Vector3.up;
            FirstDayToolSurface.Attach(go, false);
            placed.Add(point);
        }
        bool FreeCell(Vector2Int cell, out WorldCellData data, out Vector3 ground)
        {
            ground = default;
            return _grid.TryGetCell(cell, out data) && data.IsWalkable && !data.HasWater && !data.HasPath &&
                   !resources.Contains(cell) && _grid.CellToWorld(cell, out ground);
        }
        for (int attempt = 0; attempt < 80 && placed.Count < 8; attempt++)
        {
            int side = attempt % 2 == 0 ? -1 : 1;
            // 단 앞줄(항구 기준 +5~+6 cell, 약 10~12m 북쪽)과 기본 화면 폭(±6~18m) 안.
            var cell = harborCell + new Vector2Int(side * random.Next(3, 10), random.Next(5, 7));
            if (!FreeCell(cell, out var data, out var ground) || data.ElevationLevel < 1) continue;
            if (Mathf.Abs(ground.x - Harbor.x) < 4.5f) continue;
            var point = ground + new Vector3((float)random.NextDouble() - .5f, 0, (float)random.NextDouble() - .5f);
            if (placed.Any(p => Vector3.Distance(p, point) < 3.6f)) continue;
            PlaceTree(point, 3.8f + (float)random.NextDouble() * 1.4f, "CoastTree_");
        }
        // v8 verifier: no tree was fully in frame. Two beach accents on the sand side (east/west of the landing),
        // whose whole canopy sits inside the default arrival/coast camera; spawn/supply/harbour lane stay open.
        var supplyPoint = Harbor + new Vector3(2.8f, 0, 3);
        foreach (var offset in new[] { new Vector2Int(5, 3), new Vector2Int(-5, 2), new Vector2Int(6, 3), new Vector2Int(-6, 2) })
        {
            if (placed.Count(p => p.y < Harbor.y + .5f) >= 2) break;
            if (!FreeCell(harborCell + offset, out var data, out var ground) || data.ElevationLevel > 0) continue;
            if (Mathf.Abs(ground.x - Harbor.x) < 4.5f || Vector3.Distance(ground, supplyPoint) < 6f ||
                Vector3.Distance(ground, Harbor + new Vector3(0, 0, 2)) < 6f) continue;
            if (placed.Any(p => Mathf.Abs(p.x - ground.x) < 6f && p.y < Harbor.y + .5f)) continue;
            PlaceTree(ground, 4.2f, "BeachTree_");
        }
    }

    // Dinkum world pass: 기존 장식 풀·덤불(나무/바위 제외, 1.2m 미만)의 단면·무텍스처 'Green' 재질이
    // 플레이 카메라에서 검게 읽혀, 인스턴스 군락과 같은 PA/StylizedFoliage 그라데이션으로 바꾼다. 표현만 바꾼다.
    void ApplyFoliageLook()
    {
        var source = Resources.Load<Material>("WorldArt/PA_Foliage");
        if (source == null || source.shader == null || !source.shader.isSupported) return;
        Material grass = null, bush = null;
        // v6 verifier: dark slate pine/birch canopies clashed with the yellow-green ground (refs 02/03/13).
        // Presentation trees share brighter canopy copies; trunks/bark keep their source materials.
        var canopy = new Dictionary<Material, Material>();
        foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!renderer.name.Contains("Tree")) continue;
            var materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                var m = materials[i];
                if (m == null || !(m.name.StartsWith("Green") || m.name.StartsWith("DarkGreen"))) continue;
                if (!canopy.TryGetValue(m, out var bright))
                {
                    bright = new Material(m) { name = m.name + "_PACanopy", hideFlags = HideFlags.DontSave };
                    var tint = m.name.StartsWith("Dark") ? new Color(.40f, .58f, .22f) : new Color(.55f, .72f, .28f);
                    if (bright.HasProperty("_BaseColor")) bright.SetColor("_BaseColor", tint);
                    if (bright.HasProperty("_Color")) bright.SetColor("_Color", tint);
                    canopy.Add(m, bright);
                }
                materials[i] = bright; changed = true;
            }
            if (changed) renderer.sharedMaterials = materials;
        }
        foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer.name.Contains("Rock") || renderer.name.Contains("Tree") || renderer.bounds.size.y > 1.2f) continue;
            var materials = renderer.sharedMaterials;
            bool changed = false;
            bool isBush = renderer.name.Contains("Bush");
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null || !(materials[i].name.StartsWith("Green") || materials[i].name.StartsWith("DarkGreen"))) continue;
                if (isBush && bush == null) bush = FoliageVariant(source, .8f, .02f);
                if (!isBush && grass == null) grass = FoliageVariant(source, .55f, .06f);
                materials[i] = isBush ? bush : grass;
                changed = true;
            }
            if (changed) renderer.sharedMaterials = materials;
        }
    }

    static Material FoliageVariant(Material source, float gradientHeight, float wind)
    {
        var material = new Material(source) { name = "PA_Foliage_Dressing", hideFlags = HideFlags.DontSave };
        material.SetColor("_RootColor", new Color(.36f, .52f, .18f));
        material.SetColor("_TipColor", new Color(.80f, .90f, .40f));
        material.SetFloat("_GradientHeight", gradientHeight);
        material.SetFloat("_Wind", wind);
        return material;
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
        if (_vegetation != null) _vegetation.HideWithin(shop.CoversGround);

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
        // 제작 창(가운데 1220px, 오른쪽 끝 x=1570)이 열려 있으면 카드를 창 오른쪽 빈 곳 폭(316)으로 줄여 [닫기]를 가리지 않는다(DESIGN.md §6).
        bool modal=CraftingUI.instance!=null&&CraftingUI.instance.IsOpen;
        float cardWidth=modal?316f:440f;
        Instance._toastRaw=message;
        string shown=ToastText(Instance._toast,message,cardWidth-52);
        Instance._toast.text=shown;Instance._toastUntil=Time.unscaledTime+3.5f;
        // 카드 높이를 문장 길이에 맞춘다(1~4줄).
        float height=Instance._toast.GetPreferredValues(shown,cardWidth-44,0).y+26;
        Instance._toastCard.sizeDelta=new Vector2(cardWidth,Mathf.Clamp(height,64,modal?190:150));
        if(!record)return;
        string clock=GameClock.Instance!=null?$"{GameClock.Instance.CurrentHourInt:D2}:{Mathf.FloorToInt((GameClock.Instance.CurrentHour-GameClock.Instance.CurrentHourInt)*60):D2}  ":"";
        DispatchLog.Insert(0,clock+message.Replace("\n"," "));DispatchSerial++;
        if(DispatchLog.Count>8)DispatchLog.RemoveAt(DispatchLog.Count-1);
    }

    string _toastRaw;
    static readonly System.Text.RegularExpressions.Regex ToastSpeaker = new System.Text.RegularExpressions.Regex(@"^([^:\n]{1,10}): (.+)$", System.Text.RegularExpressions.RegexOptions.Singleline);

    // 본문(굵게) + 보조 줄(작게·Muted)로 나눈다: "이름: 말" → 말 / 이름, "제목\n설명" → 제목 / 설명, "A · B" → A / B.
    internal static string ToastText(TextMeshProUGUI label,string message,float width)
    {
        string main=message, sub=null;
        var speaker=ToastSpeaker.Match(message);
        int newline=message.IndexOf('\n'), dot=message.IndexOf(" · ",System.StringComparison.Ordinal);
        if(speaker.Success){main=speaker.Groups[2].Value.Replace('\n',' ');sub=speaker.Groups[1].Value;}
        else if(newline>0){main=message.Substring(0,newline);sub=message.Substring(newline+1).Replace('\n',' ');}
        else if(dot>0){main=message.Substring(0,dot);sub=message.Substring(dot+3);}
        string top=WrapWords(label,main,width);
        if(string.IsNullOrEmpty(sub))return $"<b>{top}</b>";
        float size=label.fontSize;label.fontSize=17f;
        string bottom=WrapWords(label,sub,width);
        label.fontSize=size;
        return $"<b>{top}</b>\n<size=17><color=#5E6C66>{bottom}</color></size>";
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
    bool _toastModal;
    void Update()
    {
        // 알림이 떠 있는 동안 제작 창이 열리거나 닫히면 카드 폭을 다시 맞춘다(남은 표시 시간은 유지).
        bool modal=CraftingUI.instance!=null&&CraftingUI.instance.IsOpen;
        if(modal!=_toastModal)
        {
            _toastModal=modal;
            if(_toast!=null&&Time.unscaledTime<_toastUntil)
            { float until=_toastUntil; Toast(_toastRaw ?? _toast.text,false); _toastUntil=until; }
        }
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

