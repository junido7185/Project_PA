using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Player-facing fishing action layered on the existing daily stock-prep state.
public class FishingSpot : MonoBehaviour, IInteractable
{
    [SerializeField, Min(0.2f)] float castDuration = 1.25f;

    DaytimeStockPrepPoint _stockPoint;
    Coroutine _castRoutine;
    bool _isFishing;
    string _lastFeedback = "낚싯대를 드리울 수 있어요.";

    public bool IsFishing => _isFishing;
    public string LastFeedback => _lastFeedback;

    bool _directPlayerDemo, _biteReady, _claiming, _roaming;
    GameObject _castingPlayer;
    // 고정 물가의 직접 조작 낚시터. 해안 어디서나 던지게 따라다니는 보조 낚시터는 IsRoamingCast로 따로 구분한다.
    public bool IsDirectPlayerDemo => _directPlayerDemo && !_roaming;
    public bool IsRoamingCast => _roaming;
    public bool BiteReady => _biteReady;

    // User direction 2026-10-05: bite -> reel control -> dry-land flop -> capture.
    // 피드백 4(2026-10-31): 하루 낚시 횟수 제한이 없다. 그날 첫 포획만 기존 일일 낚시 활동
    // (DayNightShopLoopController.TryCollectDayPrepStock)으로 지급·기록해 튜토리얼/진행을 그대로 두고, 이후 포획은 같은 지급 형태로
    // 가방에만 넣는다(포획마다 저장 기록을 늘리지 않는다). 낮·밤 모두 낚을 수 있다. 입질은 ShoreFishSchool의 물고기 그림자가 일으킨다.
    public enum DemoPhase { Idle, Waiting, Bite, Hooked, Reeling, Landed }
    public DemoPhase Phase { get; private set; }
    // P9 플레이어 동작 표현용(읽기 전용): 지금 줄을 던져 둔 플레이어와 마지막으로 당긴 시각.
    public static GameObject ActiveDirectCaster { get; private set; }
    public static float LastDirectPullAt { get; private set; } = -10f;
    public GameObject Bobber => _bobber;
    public Vector3 WaterTarget => _waterTarget;
    // 지금 찌에 붙은 해안 물고기 그림자(없으면 null: 보이지 않는 물고기의 예비 입질).
    public ShoreFishSchool.ShoreFish HookFish => _fish;
    const float CastFlight = .45f;
    GameObject _bobber;
    Vector3 _waterTarget, _bobberRest, _castFrom;
    float _biteEndsAt, _phaseStartedAt, _castLandsAt, _nextAttractAt, _visitorAt, _unseenBiteAt, _nextGridLookup;
    bool _bobberLanded;
    WorldGridService _grid;
    FirstDayFishingMinigame _reel;
    ShoreFishSchool.ShoreFish _fish;
    FishingSpot _primary, _roamingTwin;
    Collider _roamTrigger;
    // 고정·이동형 낚시터가 겹쳐도 줄은 한 번에 하나만 던진다(E 입력은 줄을 던진 쪽으로 넘긴다).
    static FishingSpot s_activeCast;
    // 카메라가 찌·뭍에 오른 물고기를 따라 구도를 옮길 때 읽는다(읽기 전용).
    public static FishingSpot ActiveCast => s_activeCast != null && s_activeCast.Phase != DemoPhase.Idle ? s_activeCast : null;
    public FirstDayLandedFish LandedFish { get; private set; }
    public Item SelectedFish { get; private set; }
    string _selectedResource;
    float _selectedDifficulty;
    static readonly string[] Species = { "Items/Item_Fish", "Items/Item_Fish_RedSnapper", "Items/Item_Fish_YellowTang", "Items/Item_Fish_Sardine", "Items/Item_Fish_Mackerel", "Items/Item_Fish_Flounder", "Items/Item_Fish_Mullet", "Items/Item_Fish_Puffer", "Items/Item_Fish_Trout", "Items/Item_Fish_Eel", "Items/Item_Fish_Crucian" };
    // 종마다 출현 비율과 릴 난이도가 다르다(비쌀수록 어렵다). 그림자 크기: 정어리 가장 작음 · 기본 작음 · 노랑쥐치 중간 · 붉은 도미 큼.
    // 2026-11-03 정어리(아이템 생태 연구 F04): 한 번 뽑히면 세 마리 무리로 나타나 화면의 개체 비율은 약 35%가 된다.
    // 낮 활동·조리 목표가 참치(Items/Item_Fish)를 직접 세므로 참치 몫(.48)은 거의 그대로 두고 도미·옐로탱에서 덜었다.
    // 2026-11-04 고등어(연구 F05): 길쭉한 그림자, 중간보다 조금 쉬운 릴. 참치 몫은 그대로 두고 도미·옐로탱에서 덜었다.
    // 2026-11-04 넙치(연구 F06): 넓적한 그림자, 오래 멈춰 있어 찾기 쉬운 쪽. 릴은 쉬운 편(.22). 참치 몫 유지.
    // flounder-r143: .06은 화면 9마리 안팎에서 한 번도 안 보이는 날이 많아 '찾는 재미'가 아니라 '없는 종'이 됐다 → 고등어 .09·넙치 .08.
    // 2026-11-09 자리 물고기(연구 F07·F08): 숭어(6)는 항구 앞, 복어(7)는 물가 바위 곁에서만 — 기본 비율 0, 그 구역에서만 몫을 더한다.
    // 2026-11-09 담수·하구(연구 F09·F12): 송어(8)는 강(하구 제외) 06–18시, 장어(9)는 하구 14–24시. 바다 물고기는 강·연못 안쪽으로 들어가지 않는다.
    // 2026-11-09 붕어(10, 연구 F10): 생성 섬의 연못(고지대 구덩이 3×3)에서만 06–20시, 쉬운 입문종. 잉어(F11)는 연못이 좁아 연구 조건("차별성이 약하면 둘 중 하나") 따라 넣지 않음.
    static readonly float[] SpeciesWeight = { .48f, .11f, .13f, .11f, .09f, .08f, 0f, 0f, 0f, 0f, 0f };
    static readonly float[] SpeciesDifficultyTable = { .15f, .8f, .45f, .08f, .32f, .22f, .38f, .18f, .4f, .5f, .12f };
    public const int SardineSpecies = 3, MackerelSpecies = 4, FlounderSpecies = 5, MulletSpecies = 6, PufferSpecies = 7, TroutSpecies = 8, EelSpecies = 9, CrucianSpecies = 10;
    const float EstuaryRadius = 12f, EelShare = .3f, EstuaryMulletShare = .2f;
    static readonly HashSet<Vector2Int> FreshCells = new HashSet<Vector2Int>();
    static readonly List<Vector3> Mouths = new List<Vector3>();
    static WorldGridService s_zoneGrid;
    public static int FreshCellCount => FreshCells.Count;
    static readonly List<Vector2Int> River = new List<Vector2Int>();
    static readonly HashSet<Vector2Int> PondCells = new HashSet<Vector2Int>();
    public static int PondCellCount => PondCells.Count;
    public static IReadOnlyCollection<Vector2Int> PondCellSet => PondCells;
    // qa232: 초원의 강은 둑이 2단 넘게 높은 도랑이라 기본 카메라(남쪽 위)에서 가까운 둑에 물이 가려 송어 그림자가 안 보였다.
    // 둑(맞닿은 땅 칸)이 수면보다 1단 이하로만 높은 '트인 강' 칸에만 송어를 둔다.
    static readonly HashSet<Vector2Int> OpenRiver = new HashSet<Vector2Int>();
    public static int OpenRiverCount => OpenRiver.Count;
    public static bool IsOpenRiver(Vector3 at) => s_zoneGrid != null && s_zoneGrid.WorldToCell(at, out var c) && OpenRiver.Contains(c);
    public static Vector3? PondCentre;
    public static IReadOnlyList<Vector2Int> RiverCells => River; // 강 칸만(연못 제외) — 반딧불이가 강가 자리를 찾는다
    public static WorldGridService ZoneGrid => s_zoneGrid;
    public static IReadOnlyList<Vector3> RiverMouths => Mouths;

    // 생성 섬의 강·연못 칸과 하구(강 칸이 바다 칸과 맞닿는 곳)를 기억한다. WorldGameplayAdapterService가 부른다.
    public static void SetFreshwater(WorldGenerationResult world, WorldGridService grid)
    {
        FreshCells.Clear(); Mouths.Clear(); River.Clear(); PondCells.Clear(); PondCentre = null; s_zoneGrid = grid;
        if (world == null || grid == null) return;
        foreach (var c in world.Cells) if (c.WaterType == WorldWaterType.River) River.Add(c.Terrain.Coordinate);
        OpenRiver.Clear();
        foreach (var rc in River)
        {
            if (!grid.TryGetCell(rc, out var water)) continue;
            int bank = int.MinValue;
            foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                if (grid.TryGetCell(rc + d, out var land) && !land.HasWater) bank = Mathf.Max(bank, land.ElevationLevel);
            if (bank == int.MinValue || bank - water.WaterSurfaceLevel <= 1) OpenRiver.Add(rc);
        }
        Vector3 sum = Vector3.zero;
        foreach (var c in world.Cells)
            if (c.WaterType == WorldWaterType.Pond && grid.CellToWorld(c.Terrain.Coordinate, out var pw)) { PondCells.Add(c.Terrain.Coordinate); sum += pw; }
        PondCentre = PondCells.Count > 0 ? sum / PondCells.Count : (Vector3?)null;
        foreach (var c in world.Cells)
            if (c.WaterType == WorldWaterType.River || c.WaterType == WorldWaterType.Pond) FreshCells.Add(c.Terrain.Coordinate);
        Vector2Int[] around = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var c in world.Cells)
        {
            if (c.WaterType != WorldWaterType.River) continue;
            var at = c.Terrain.Coordinate;
            foreach (var d in around)
                if (world.TryGetCell(at + d, out var next) && next.IsOcean && grid.CellToWorld(at, out var w)) { Mouths.Add(w); break; }
        }
    }
    public static bool IsEstuary(Vector3 at) { foreach (var m in Mouths) if (FlatSq(at - m) < EstuaryRadius * EstuaryRadius) return true; return false; }
    public static bool IsFresh(Vector3 at) => s_zoneGrid != null && s_zoneGrid.WorldToCell(at, out var c) && FreshCells.Contains(c);
    public static bool IsPond(Vector3 at) => s_zoneGrid != null && s_zoneGrid.WorldToCell(at, out var c) && PondCells.Contains(c);
    static bool HourIn(float from, float until) { float h = GameClock.Instance != null ? GameClock.Instance.CurrentHour : 12f; return h >= from && h < until; }
    // 지금 나오는 시간인가(송어 06–18, 장어 14–24). 다른 종은 언제나.
    public static bool ActiveNow(int species) => species == TroutSpecies ? HourIn(6f, 18f) : species == EelSpecies ? HourIn(14f, 24f) : species == CrucianSpecies ? HourIn(6f, 20f) : true;
    const float MulletShare = .16f, PufferShare = .14f, HarborRadius = 18f, StoneRadius = 4f;
    public static Vector3? HarborZone; // WorldGameplayAdapterService가 항구 위치를 넣는다

    // 그 자리가 그 종의 구역인가. 숭어: 항구 18m 안 또는 하구, 복어: 물가 바위 4m 안(담수 제외), 송어: 강·연못(하구 제외), 장어: 하구.
    // 바다 물고기(0~5)는 강·연못 안쪽(하구 제외)이 아닌 곳.
    public static bool InZone(int species, Vector3 at)
    {
        if (species == MulletSpecies) return HarborZone.HasValue && FlatSq(at - HarborZone.Value) < HarborRadius * HarborRadius || IsEstuary(at);
        if (species == TroutSpecies) return IsFresh(at) && !IsEstuary(at) && !IsPond(at) && (OpenRiver.Count == 0 || IsOpenRiver(at));
        if (species == CrucianSpecies) return IsPond(at);
        if (species == EelSpecies) return IsEstuary(at);
        if (IsFresh(at) && !IsEstuary(at)) return false;
        if (species == PufferSpecies)
        {
            foreach (var s in CoastalVegetationField.WaterlineStones) if (FlatSq(at - s.Stone) < StoneRadius * StoneRadius) return true;
            return false;
        }
        return true;
    }
    static float FlatSq(Vector3 v) { v.y = 0f; return v.sqrMagnitude; }
    public static int SpeciesCount => Species.Length;
    public static string SpeciesResource(int species) => Species[Mathf.Clamp(species, 0, Species.Length - 1)];
    public static float SpeciesDifficulty(int species) => SpeciesDifficultyTable[Mathf.Clamp(species, 0, Species.Length - 1)];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { QaPreferSpecies = null; s_activeCast = null; ActiveDirectCaster = null; LastDirectPullAt = -10f; HarborZone = null; FreshCells.Clear(); Mouths.Clear(); River.Clear(); PondCells.Clear(); PondCentre = null; OpenRiver.Clear(); s_zoneGrid = null; }

    public void ConfigureDirectPlayerDemo(WorldGridService grid = null)
    {
        if (_directPlayerDemo) return;
        CancelCast();
        _directPlayerDemo = true;
        _grid = grid != null ? grid : FindFirstObjectByType<WorldGridService>();
        SnapToShore();
        EnsureRoamingTwin();
    }

    DaytimeStockPrepPoint Stock => _stockPoint != null ? _stockPoint : _primary != null ? _primary._stockPoint : null;

    // 낚시터를 실제 물과 맞닿은 가장 가까운 땅 칸으로 옮긴다(언제나 쓸 수 있는 기준 물가). 물고기 그림자는 ShoreFishSchool이 맡는다.
    void SnapToShore()
    {
        if (_grid == null || !_grid.WorldToCell(transform.position, out var origin)) return;
        var definition = _grid.Definition;
        Vector2Int[] around = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        // 고정 3인칭 카메라에서 찌·입질이 화면 아래(프롬프트/핫바 뒤)로 가지 않게,
        // 물이 카메라 반대쪽이나 옆에 있는 물가를 가까운 순서보다 우선한다(거리 페널티로 제한).
        Vector3 away = Camera.main != null ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up) : Vector3.forward;
        away = away.sqrMagnitude > .01f ? away.normalized : Vector3.forward;
        float bestScore = float.MaxValue;
        Vector2Int bestLand = default, bestStep = default;
        for (int dx = -24; dx <= 24; dx++)
            for (int dz = -24; dz <= 24; dz++)
            {
                var land = origin + new Vector2Int(dx, dz);
                if (!_grid.TryGetCell(land, out var landCell) || !landCell.IsWalkable) continue;
                foreach (var step in around)
                {
                    if (!_grid.TryGetCell(land + step, out var water) || !water.HasWater) continue;
                    float facing = Vector3.Dot(new Vector3(step.x, 0f, step.y), away); // 1=멀어지는 쪽, -1=카메라 쪽
                    float score = Mathf.Sqrt(dx * dx + dz * dz) + (facing > .5f ? 0f : facing > -.5f ? 4f : 18f);
                    if (score < bestScore) { bestScore = score; bestLand = land; bestStep = step; }
                }
            }
        if (bestScore == float.MaxValue) return;
        if (!_grid.CellToWorld(bestLand, out var landPosition) || !_grid.CellToWorld(bestLand + bestStep, out var waterPosition) ||
            !_grid.TryGetCell(bestLand + bestStep, out var waterCell)) return;
        // 물가 바로 앞에 낚시터 표식을 두고, 물 쪽 0.6칸 앞에 찌가 떨어진다.
        Vector3 edge = Vector3.Lerp(landPosition, waterPosition, .45f);
        transform.position = new Vector3(edge.x, landPosition.y, edge.z);
        _waterTarget = waterPosition + new Vector3(bestStep.x, 0f, bestStep.y) * definition.CellSize * .6f;
        _waterTarget.y = WorldChunkMeshBuilder.OpeningWaterHeight(definition,waterCell) + .02f;
    }

    // 피드백 4: 낚싯대를 들고 물가를 바라보면 해안 어디서나 던질 수 있게, 플레이어 앞 해안 턱으로 따라오는 보조 낚시터를 둔다.
    // PlayerInteraction은 FishingSpot 콜라이더만 고르므로 같은 클래스·같은 일일 활동을 공유하는 별도 오브젝트로 만든다.
    void EnsureRoamingTwin()
    {
        if (_roaming || _roamingTwin != null) return;
        var go = new GameObject("ShoreFishing_RoamingCast") { layer = 2 }; // Ignore Raycast: 일반 레이캐스트에 걸리지 않는다.
        go.SetActive(false);
        var stock = Stock;
        go.transform.SetParent(stock != null ? stock.transform.parent : transform.parent, false);
        var trigger = go.AddComponent<SphereCollider>();
        trigger.isTrigger = true; trigger.radius = .35f; trigger.center = Vector3.up * .75f; trigger.enabled = false;
        var twin = go.AddComponent<FishingSpot>();
        twin._roaming = true; twin._directPlayerDemo = true; twin._primary = this; twin._grid = _grid; twin._roamTrigger = trigger;
        _roamingTwin = twin;
        go.SetActive(true);
    }

    // 낚싯대를 든 플레이어가 물가를 바라볼 때만 콜라이더를 켜고 플레이어 바로 앞(물가 쪽)에 둔다.
    void UpdateRoamingAnchor()
    {
        bool show = false;
        var player = Inventory.instance != null ? Inventory.instance.gameObject : null;
        if (_primary.isActiveAndEnabled && player != null && player.activeInHierarchy && !PlayerInputHandler.ModalOpen &&
            EquipmentSystem.CurrentHeld(player)?.toolType == ToolType.FishingRod &&
            TryFindShoreAhead(player.transform, out var anchor, out var water))
        {
            transform.position = anchor;
            _waterTarget = water;
            show = true;
        }
        if (_roamTrigger != null && _roamTrigger.enabled != show) _roamTrigger.enabled = show;
    }

    static readonly float[] AheadYaw = { 0f, -16f, 16f };

    // 플레이어가 서 있는 땅 칸에서 앞(±16°)으로 2.6m 안에 물이 시작되면, 해안 턱에서 1~2m 떨어진 물 위를 찌 자리로 고른다.
    bool TryFindShoreAhead(Transform player, out Vector3 anchor, out Vector3 water)
    {
        anchor = water = default;
        if (_grid == null) _grid = _primary != null ? _primary._grid : null;
        if (_grid == null && Time.unscaledTime >= _nextGridLookup)
        { _nextGridLookup = Time.unscaledTime + 1f; _grid = FindFirstObjectByType<WorldGridService>(); }
        if (_grid == null) return false;
        Vector3 origin = player.position;
        if (!_grid.WorldToCell(origin, out var standCell) || !_grid.TryGetCell(standCell, out var stand) || stand.HasWater) return false;
        Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up);
        if (forward.sqrMagnitude < .01f) return false;
        forward.Normalize();
        var definition = _grid.Definition;
        foreach (float yaw in AheadYaw)
        {
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * forward;
            float edge = -1f;
            for (float d = .3f; d <= 2.6f; d += .15f)
            {
                if (!_grid.WorldToCell(origin + dir * d, out var c) || !_grid.TryGetCell(c, out var cell)) break;
                if (cell.HasWater) { edge = d; break; }
            }
            if (edge < 0f) continue;
            // 해안 턱에서 바깥쪽으로 물이 이어지는 동안 가장 먼(최대 2.2m) 헤엄칠 수 있는 점에 찌가 떨어진다.
            bool found = false;
            Vector3 best = default;
            WorldCellData bestCell = default;
            for (float extra = .7f; extra <= 2.2f; extra += .25f)
            {
                Vector3 p = origin + dir * (edge + extra);
                if (!_grid.WorldToCell(p, out var wc) || !_grid.TryGetCell(wc, out var wcell) || !wcell.HasWater) break;
                if (!ShoreFishSchool.ClearOfLand(_grid, p, .4f)) continue;
                found = true; best = p; bestCell = wcell;
            }
            if (!found) continue;
            float surface = WorldChunkMeshBuilder.OpeningWaterHeight(definition, bestCell);
            // 높은 절벽 위에서는 던지지 않는다. 다만 연못(고지대 바위 구덩이, 둘레 3단 위)은 위에서 드리우는 것이 유일한 길이라 4.6m까지.
            if (origin.y - surface > (IsPond(best) ? 4.6f : 3.5f)) continue;
            water = new Vector3(best.x, surface + .02f, best.z);
            anchor = origin + dir * Mathf.Clamp(edge, .6f, 1.45f);
            return true;
        }
        return false;
    }

    bool IsPlayerInReach(GameObject player)
    {
        if (player == null || player.GetComponent<Inventory>() != Inventory.instance || Inventory.instance == null)
            return false;
        Vector3 delta = Vector3.ProjectOnPlane(transform.position - player.transform.position, Vector3.up);
        return Mathf.Abs(transform.position.y - player.transform.position.y) < 2f &&
            delta.sqrMagnitude <= 2.05f * 2.05f && delta.sqrMagnitude > .0025f &&
            Vector3.Dot(player.transform.forward, delta.normalized) >= .75f;
    }

    void InteractDirect(GameObject player)
    {
        if (s_activeCast != null && s_activeCast != this && s_activeCast._castingPlayer == player && s_activeCast.Phase != DemoPhase.Idle)
        { s_activeCast.InteractDirect(player); return; }
        if (_claiming || !isActiveAndEnabled || !IsPlayerInReach(player)) return;
        if (((PlayerInputHandler.Instance?.FirstDayControls ?? false) || _roaming) &&
            EquipmentSystem.CurrentHeld(player)?.toolType != ToolType.FishingRod) return;
        switch (Phase)
        {
            case DemoPhase.Idle:
                StartDirectCast(player);
                return;
            case DemoPhase.Waiting:
                ReleaseFish(true); // 너무 이른 당김은 다가오던 물고기를 쫓는다. 곧바로 다시 던질 수 있다.
                CancelCast();
                _lastFeedback = "너무 빨리 당겼어요. 다시 던져 보세요.";
                FirstDayWorldPresentation.Toast(_lastFeedback, false);
                return;
            case DemoPhase.Bite:
                if (player != _castingPlayer) return;
                LastDirectPullAt = Time.time;
                BeginReeling();
                return;
            default:
                return;
        }
    }

    void StartDirectCast(GameObject player)
    {
        if (_waterTarget == Vector3.zero || Inventory.instance == null) { _lastFeedback = "물가를 바라보고 던져 주세요."; return; }
        _castingPlayer = player;
        ActiveDirectCaster = player;
        s_activeCast = this;
        _isFishing = true;
        _biteReady = false;
        _fish = null;
        SelectedFish = null;
        Phase = DemoPhase.Waiting;
        float now = Time.time;
        _phaseStartedAt = now;
        _castLandsAt = now + CastFlight;
        _bobberLanded = false;
        bool school = ShoreFishSchool.Instance != null;
        _nextAttractAt = _castLandsAt + Random.Range(.4f, 1.1f);
        _visitorAt = _castLandsAt + Random.Range(7f, 11f);
        // 해안 물고기 시스템이 없는 장면(검증 등)은 예전처럼 1.6~3.2초 뒤 입질한다. 있으면 물고기가 끝내 안 올 때만 쓰는 긴 예비 시간이다.
        _unseenBiteAt = _castLandsAt + (school ? Random.Range(16f, 21f) : Random.Range(1.6f, 3.2f));
        _lastFeedback = "찌를 던졌어요… 물고기 그림자가 다가오길 기다려요.";
        // Display-only float; existing fishing phases, timings and rewards remain authoritative.
        if (_bobber == null) _bobber = FirstDayFishingFloat.Create(this);
        _bobberRest = _waterTarget;
        _castFrom = player.transform.position + Vector3.up * 1.5f + player.transform.forward * .5f;
        if (_bobber != null) { _bobber.SetActive(true); _bobber.transform.position = _castFrom; }
        GatherFeedback.Swish();
    }

    // QA/캡처 전용(FIXTURE로 기록): 설정되면 새 그림자의 절반을 그 종으로 뽑는다. 실제 플레이에서는 항상 null.
    public static int? QaPreferSpecies;

    public static int PickSpecies() => PickSpecies(null);

    // at: 그림자가 생기는 자리(또는 찌 자리). 자리 물고기는 그 구역일 때만 몫이 붙는다(QA 편향도 구역 안에서만).
    public static int PickSpecies(Vector3? at)
    {
        if (QaPreferSpecies.HasValue && Random.value < .5f)
        {
            int prefer = Mathf.Clamp(QaPreferSpecies.Value, 0, Species.Length - 1);
            if (at.HasValue ? InZone(prefer, at.Value) : prefer < MulletSpecies) return prefer;
        }
        // 연못은 붕어만, 강 안쪽은 송어만(바다 추첨표에 섞지 않는다 — 연구 F10·F12).
        if (at.HasValue && IsPond(at.Value)) return CrucianSpecies;
        if (at.HasValue && IsFresh(at.Value) && !IsEstuary(at.Value)) return TroutSpecies;
        bool estuary = at.HasValue && IsEstuary(at.Value);
        float mullet = at.HasValue && InZone(MulletSpecies, at.Value) ? (estuary ? EstuaryMulletShare : MulletShare) : 0f;
        float puffer = at.HasValue && InZone(PufferSpecies, at.Value) ? PufferShare : 0f;
        float eel = estuary && ActiveNow(EelSpecies) ? EelShare : 0f;
        float roll = Random.value * (1f + mullet + puffer + eel), sum = 0f;
        for (int i = 0; i < MulletSpecies; i++) { sum += SpeciesWeight[i]; if (roll < sum) return i; }
        if (roll < sum + mullet) return MulletSpecies;
        if (roll < sum + mullet + puffer) return PufferSpecies;
        if (roll < sum + mullet + puffer + eel) return EelSpecies;
        return 0;
    }

    // 찌가 물에 닿은 뒤: 근처 그림자가 알아채고 다가와 톡톡 건드리다가(찌가 살짝 눌림) 물면(찌가 쑥 잠김) 입질 단계가 된다.
    void UpdateWaiting()
    {
        float now = Time.time;
        if (!_bobberLanded)
        {
            if (now < _castLandsAt) return;
            _bobberLanded = true;
            GatherFeedback.Splash();
            _lastFeedback = "입질을 기다리는 중… 찌가 쑥 들어가면 당기세요.";
        }
        var school = ShoreFishSchool.Instance;
        if (_fish != null && (!_fish.Alive || _fish.Owner != (object)this ||
            _fish.State == ShoreFishSchool.Stage.Roam || _fish.State == ShoreFishSchool.Stage.Flee))
        {
            // 다가오던 물고기가 길이 막혀 돌아섰다. 다른 물고기를 기다린다.
            _fish = null;
            _unseenBiteAt = Mathf.Max(_unseenBiteAt, now + 4f);
            _lastFeedback = "입질을 기다리는 중… 찌가 쑥 들어가면 당기세요.";
        }
        if (_fish == null && school != null && now >= _nextAttractAt)
        {
            _nextAttractAt = now + Random.Range(.6f, 1.3f);
            if (Random.value < .7f) _fish = school.Attract(this, _waterTarget);
            if (_fish == null && now >= _visitorAt)
            {
                _visitorAt = now + Random.Range(6f, 10f);
                _fish = school.SpawnVisitor(this, _waterTarget);
            }
            if (_fish != null) _lastFeedback = "물고기 그림자가 찌로 다가와요… 찌가 쑥 잠기면 당기세요.";
        }
        if (_fish != null)
        {
            if (_fish.State == ShoreFishSchool.Stage.Bite) EnterBite(_fish.Species, _fish.Difficulty);
            else if (_fish.State == ShoreFishSchool.Stage.Nibble && _fish.Nibbles > 0) _lastFeedback = "톡톡… 아직이에요! 찌가 쑥 잠기면 당기세요.";
            return;
        }
        if (now >= _unseenBiteAt)
        {
            int species = PickSpecies(_waterTarget);
            EnterBite(species, SpeciesDifficulty(species));
        }
    }

    void EnterBite(int species, float difficulty)
    {
        _selectedResource = SpeciesResource(species);
        SelectedFish = Resources.Load<Item>(_selectedResource);
        if (SelectedFish == null) { _selectedResource = Species[0]; SelectedFish = FishItem; }
        _selectedDifficulty = Mathf.Clamp01(difficulty);
        Phase = DemoPhase.Bite;
        _biteReady = true;
        // 어려운 물고기일수록 입질 창이 조금 짧다.
        _biteEndsAt = Time.time + Mathf.Lerp(1.15f, .8f, _selectedDifficulty);
        _lastFeedback = "지금 당기기!";
        GatherFeedback.Bite();
    }

    void PoseBobber()
    {
        float now = Time.time;
        if (Phase == DemoPhase.Waiting && !_bobberLanded)
        {
            float k = Mathf.InverseLerp(_phaseStartedAt, _castLandsAt, now);
            _bobber.transform.position = Vector3.Lerp(_castFrom, _bobberRest, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 1.1f);
            return;
        }
        float dip = _fish != null ? _fish.BobberDip : 0f;
        float bob = Phase == DemoPhase.Waiting
            ? Mathf.Sin((now - _castLandsAt) * 3f) * .03f * (1f - dip) - dip * .08f
            : -.12f + Mathf.Sin(now * 30f) * .012f;
        _bobber.transform.position = _bobberRest + Vector3.up * bob;
    }

    void BeginReeling()
    {
        Phase = DemoPhase.Reeling; _biteReady = false;
        if (_fish != null) ShoreFishSchool.Instance?.Hook(_fish);
        if (_reel == null) _reel = gameObject.AddComponent<FirstDayFishingMinigame>();
        _reel.Open(SelectedFish, CompleteReeling, _selectedDifficulty);
    }

    void CompleteReeling(bool caught)
    {
        if (Phase != DemoPhase.Reeling) return;
        if (!caught || _castingPlayer == null || Inventory.instance == null || !TryFindDryLanding(out var dry))
        {
            ReleaseFish(true);
            CancelCast(); _lastFeedback = "물고기가 달아났어요. 다시 던져 보세요.";
            FirstDayWorldPresentation.Toast(_lastFeedback, false);
            return;
        }
        var assets = FirstDayStudioAssets.Load();
        GameObject model = SelectedFish != null && SelectedFish.model != null ? SelectedFish.model : assets != null ? assets.fish : null;
        LandedFish = FirstDayLandedFish.Create(this, SelectedFish, model, _waterTarget, dry, _selectedDifficulty);
        if (LandedFish == null) { ReleaseFish(true); CancelCast(); _lastFeedback = "물고기 모델을 표시할 수 없어요."; return; }
        // 끌어올린 물고기의 그림자는 사라지고 뭍의 물고기가 이어받는다.
        if (_fish != null) { ShoreFishSchool.Instance?.Catch(_fish); _fish = null; }
        Phase = DemoPhase.Landed; _isFishing = false;
        if (_bobber != null) _bobber.SetActive(false);
        if (ActiveDirectCaster == _castingPlayer) ActiveDirectCaster = null;
        if (s_activeCast == this) s_activeCast = null;
        // 낚싯대 내구도는 실제로 끌어올린 물고기 1마리당 1만 쓴다(빈 입질·놓침·실패·뭍에서의 포획 타격은 무료).
        var inventory = _castingPlayer.GetComponent<Inventory>();
        if (inventory != null) ToolDurability.ConsumeSuccess(inventory, inventory.GetSelectedInstance());
        _lastFeedback = ItemDisplayName.For(SelectedFish) + "를 끌어올렸어요! 물로 도망가기 전에 때려서 잡으세요(맨손도 돼요).";
        FirstDayWorldPresentation.Toast(_lastFeedback, FirstCatchPending);
    }

    // 오늘 일일 낚시 활동(첫 포획)이 아직 남았는가. 남았으면 다음 포획이 기존 활동 기록·튜토리얼 경로로 간다.
    bool FirstCatchPending
    {
        get
        {
            var loop = DayNightShopLoopController.Instance; var stock = Stock;
            return loop != null && stock != null && loop.IsDayPrepPointAvailable(stock);
        }
    }

    bool TryFindDryLanding(out Vector3 position)
    {
        position = Vector3.zero;
        if (_grid == null || _castingPlayer == null || !_grid.WorldToCell(_castingPlayer.transform.position, out var origin)) return false;
        Vector3 desired = _castingPlayer.transform.position + _castingPlayer.transform.forward * .9f + _castingPlayer.transform.right * .65f;
        float best = float.MaxValue;
        for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
        {
            var cell = origin + new Vector2Int(x, z);
            if (!_grid.TryGetCell(cell, out var data) || data.HasWater || !data.IsWalkable ||
                !_grid.CellToWorld(cell, out var centre) || Mathf.Abs(centre.y - _castingPlayer.transform.position.y) > 1.2f) continue;
            float inset = _grid.Definition.CellSize * .5f - .45f;
            var candidate = new Vector3(Mathf.Clamp(desired.x, centre.x - inset, centre.x + inset), centre.y,
                Mathf.Clamp(desired.z, centre.z - inset, centre.z + inset));
            float score = (candidate - desired).sqrMagnitude;
            if (score >= best) continue;
            best = score; position = candidate;
        }
        return best < float.MaxValue;
    }

    public bool TryClaimLandedFish(FirstDayLandedFish fish, GameObject player)
    {
        if (_claiming || Phase != DemoPhase.Landed || fish == null || fish != LandedFish || !fish.ReadyToClaim ||
            player == null || Inventory.instance == null || player.GetComponent<Inventory>() != Inventory.instance) return false;
        // 그날 첫 포획은 기존 일일 낚시 활동이 Inventory 지급과 오늘 완료 기록을 소유한다(튜토리얼/진행 그대로).
        // 이후 포획(밤 포함)은 같은 지급 형태(품질·가격)로 가방에만 넣는다. 저장 기록은 늘지 않는다. 가방이 차면 아무것도 바꾸지 않는다.
        var loop = DayNightShopLoopController.Instance; var stock = Stock;
        bool daily = FirstCatchPending;
        _claiming = true;
        bool caught;
        int before = Inventory.instance.CountItems(SelectedFish);
        try
        {
            if (daily)
            {
                // Activity id stays unchanged: old saves cannot collect the same shore activity again.
                stock.itemResourcePath = _selectedResource; stock.grantCount = 1;
                caught = loop.TryCollectDayPrepStock(stock, player);
            }
            else
            {
                var instance = new ItemInstance(SelectedFish, 1) { quality = 1.03f, currentPrice = SelectedFish.basePrice };
                caught = Inventory.instance.AddInstance(instance);
            }
        }
        finally { _claiming = false; }
        if (!caught)
        {
            _lastFeedback = "가방이 가득 찼어요. 포획한 물고기는 여기서 기다려요.";
            FirstDayWorldPresentation.Toast(_lastFeedback, false);
            return false;
        }
        DemoHotbarPreference.Prefer(SelectedFish);
        FirstDayWorldPresentation.Acquired(SelectedFish, Inventory.instance.CountItems(SelectedFish) - before, fish.transform.position);
        GatherFeedback.Catch();
        CancelCast();
        _lastFeedback = "물고기를 포획했어요! 물가의 그림자를 찾아 또 던져 보세요.";
        return true;
    }

    // 뭍의 물고기가 제때 잡히지 않아 물로 돌아갔다. 보상·기록·내구도 변화 없이 곧바로 다시 던질 수 있다.
    public void OnLandedFishEscaped(FirstDayLandedFish fish)
    {
        if (fish == null || fish != LandedFish || Phase != DemoPhase.Landed) return;
        ShoreFishSchool.Instance?.ReleaseEscapee(_waterTarget, System.Array.IndexOf(Species, _selectedResource));
        CancelCast();
        _lastFeedback = "물고기가 퍼덕이다 물로 도망쳤어요. 다시 던져 보세요.";
        FirstDayWorldPresentation.Toast(_lastFeedback, false);
    }

    static Item FishItem => Resources.Load<Item>("Items/Item_Fish");

    void Update()
    {
        if (!_directPlayerDemo) return;
        if (_roaming)
        {
            if (_primary == null) { Destroy(gameObject); return; }
            if (Phase == DemoPhase.Idle) UpdateRoamingAnchor();
        }
        if (Phase == DemoPhase.Idle || Phase == DemoPhase.Landed) return;
        // 낚싯대를 내려놓거나(X·도구 변경) 자리를 떠나면 줄을 거둔다.
        if (_castingPlayer == null || !IsPlayerInReach(_castingPlayer) ||
            EquipmentSystem.CurrentHeld(_castingPlayer)?.toolType != ToolType.FishingRod)
        {
            CancelCast();
            _lastFeedback = "낚싯대를 거뒀어요.";
            return;
        }
        if (Phase == DemoPhase.Waiting) UpdateWaiting();
        if (_bobber != null && _bobber.activeSelf) PoseBobber();
        if (Phase == DemoPhase.Bite && Time.time > _biteEndsAt)
        {
            ReleaseFish(true);
            CancelCast();
            _lastFeedback = "물고기가 달아났어요. 다시 던져 보세요.";
            FirstDayWorldPresentation.Toast(_lastFeedback, false);
        }
    }

    public void Configure(DaytimeStockPrepPoint stockPoint)
    {
        _stockPoint = stockPoint;
    }

    void Awake()
    {
        if (_stockPoint == null && !_roaming)
            _stockPoint = GetComponentInParent<DaytimeStockPrepPoint>();
    }

    void OnDisable()
    {
        CancelCast();
        if (_roamTrigger != null) _roamTrigger.enabled = false;
    }

    void OnDestroy()
    {
        if (_roamingTwin != null) Destroy(_roamingTwin.gameObject);
        if (s_activeCast == this) s_activeCast = null;
    }

    public void Interact(GameObject interactor)
    {
        if (_directPlayerDemo) { InteractDirect(interactor); return; }
        if (_isFishing)
            return;

        var loop = DayNightShopLoopController.Instance;
        if (loop == null || _stockPoint == null)
        {
            _lastFeedback = "지금은 낚시할 수 없어요.";
            Debug.LogWarning("[FishingSpot] Fishing requires a day-prep stock point and loop controller.");
            return;
        }

        if (!loop.IsDayPrepPointAvailable(_stockPoint))
        {
            // Reuse the loop HUD explanation for the wrong phase or a completed catch.
            loop.TryCollectDayPrepStock(_stockPoint, interactor);
            _lastFeedback = loop.CurrentPhase == PADayNightPhase.DayPreparation
                ? "오늘 낚시는 마쳤어요."
                : "낚시는 낮 준비 시간에 할 수 있어요.";
            return;
        }

        _isFishing = true;
        _lastFeedback = "낚싯대를 드리웠어요. 찌를 기다리는 중...";
        _castRoutine = StartCoroutine(CastAndCatch(interactor));
    }

    public string GetInteractPrompt()
    {
        if (_directPlayerDemo)
        {
            // 다른 낚시터에 줄을 던져 둔 동안에는 그쪽 진행 문구를 보인다(E도 그쪽으로 간다).
            if (s_activeCast != null && s_activeCast != this && s_activeCast.Phase != DemoPhase.Idle) return s_activeCast._lastFeedback;
            if (Phase != DemoPhase.Idle) return _lastFeedback;
            return "낚싯대 던지기";
        }
        if (_isFishing)
            return _lastFeedback;

        var loop = DayNightShopLoopController.Instance;
        if (loop == null || _stockPoint == null)
            return "해변 낚시터: 이용 불가";

        if (!loop.IsDayPrepPointAvailable(_stockPoint))
            return loop.CurrentPhase == PADayNightPhase.DayPreparation
                ? "해변 낚시터: 오늘 낚시 완료"
                : "해변 낚시터: 낮에 다시 오기";

        return "해변 낚시터: 낚싯대 드리우기";
    }

    IEnumerator CastAndCatch(GameObject interactor)
    {
        float firstWait = Mathf.Min(0.35f, castDuration * 0.35f);
        yield return new WaitForSeconds(firstWait);

        _lastFeedback = "찌가 흔들려요...";
        yield return new WaitForSeconds(Mathf.Max(0.05f, castDuration - firstWait));

        _castRoutine = null;
        CompleteCatch(interactor);
    }

    bool CompleteCatch(GameObject interactor)
    {
        _isFishing = false;

        var loop = DayNightShopLoopController.Instance;
        bool caught = loop != null
            && _stockPoint != null
            && loop.TryCollectDayPrepStock(_stockPoint, interactor);

        _lastFeedback = caught
            ? "물고기를 낚아 재고 가방에 담았어요!"
            : "이번에는 낚시를 마치지 못했어요.";
        return caught;
    }

    // 찌에 붙은 물고기를 놓아준다. 톡톡 건드리던 중 이후(또는 flee)면 휙 달아나고, 다가오던 중이면 다시 돌아다닌다.
    void ReleaseFish(bool flee)
    {
        var fish = _fish;
        _fish = null;
        var school = ShoreFishSchool.Instance;
        if (fish == null || school == null) return;
        school.Release(fish, flee || fish.State == ShoreFishSchool.Stage.Nibble || fish.State == ShoreFishSchool.Stage.Bite ||
            fish.State == ShoreFishSchool.Stage.Hooked);
    }

    void CancelCast()
    {
        if (_reel != null) _reel.CloseSilently();
        if (LandedFish != null) { Destroy(LandedFish.gameObject); LandedFish = null; }
        ReleaseFish(false);
        _biteReady = false;
        if (ActiveDirectCaster == _castingPlayer) ActiveDirectCaster = null;
        if (s_activeCast == this) s_activeCast = null;
        _castingPlayer = null;
        Phase = DemoPhase.Idle;
        if (_bobber != null) _bobber.SetActive(false);
        if (_castRoutine != null)
        {
            StopCoroutine(_castRoutine);
            _castRoutine = null;
        }

        _isFishing = false;
    }

    public void PrepareForStateRestore()
    {
        CancelCast();
    }

#if UNITY_EDITOR
    // Editor smoke tests use the same completion path without waiting on wall-clock time.
    public bool CompleteCatchForValidation(GameObject interactor)
    {
        if (_directPlayerDemo) return false;
        if (!_isFishing)
            return false;

        CancelCast();
        return CompleteCatch(interactor);
    }
#endif
}
