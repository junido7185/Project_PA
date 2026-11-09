using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 피드백 4(2026-10-31): 동숲·딩컴처럼 해안을 따라 물고기 그림자가 스스로 돌아다니고, 플레이어는 그림자를 보고 찌를 던진다.
// 플레이어 주변(≈25m)에서 땅과 2칸 이내인 얕은 물 칸에만 최대 8마리를 유지한다. 땅 칸과 해안 턱 근처로는 들어가지 않는다.
// 종마다 크기(작음·중간·큼)와 난이도가 정해지고, 찌가 떨어지면 근처 물고기가 다가와 1~3번 톡톡 건드린 뒤 문다.
// FishingSpot이 단계·보상·내구도를 소유하고, 이 컴포넌트는 물고기 위치·종·크기·입질 타이밍만 정한다.
// 표현 전용 세션 개체군: 저장·콜라이더·NavMesh·월드 데이터를 바꾸지 않는다.
public sealed class ShoreFishSchool : MonoBehaviour
{
    public static ShoreFishSchool Instance { get; private set; }
    public static bool Enabled = true;
    public const float SpawnRadius = 25f, DespawnRadius = 31f, NoticeRadius = 5f;
    public const int MaxFish = 8;
    const int ShoreBand = 2;                       // 땅에서 2칸(=4m) 이내의 물만 '얕은 물가'로 본다.
    const float LandMargin = .42f;                 // 해안 턱/모래 경사 아래로 파고들지 않는 여유(m).
    const float SurfaceDepth = .05f;               // 수면 바로 아래.
    const float NibblePulse = .32f;
    const float TailPivot = -.25f;                 // 단위 길이 메시에서 꼬리가 붙는 위치.
    const int EelSegments = 5;
    const float EelSpacing = .42f, EelSegmentScale = .36f;  // 장어: 마디 5개를 겹치게 이어(eel-r215 3마디·.6 간격은 끊긴 사슬로 보였다) 전체 약 L

    public enum Stage { Roam, Approach, Nibble, Bite, Hooked, Flee }

    public sealed class ShoreFish
    {
        public int Species { get; internal set; }
        public float Length { get; internal set; }       // 꼬리 포함 몸길이(m)
        public float Difficulty { get; internal set; }   // 0 쉬움 ~ 1 어려움(종 + 개체 크기)
        public Stage State { get; internal set; }
        public int Nibbles { get; internal set; }        // 지금까지 톡톡 건드린 횟수
        public float BobberDip { get; internal set; }    // 0..1 찌가 눌린 정도(1 = 물속으로 끌려감)
        public object Owner => owner;
        public Vector3 Position => pos;
        public bool Alive => root != null && !gone;
        internal Transform root, tail;
        internal MeshRenderer body, tailRenderer, halo;
        internal Vector3 pos, target, lure;
        internal float heading, speed, cruise, fade, fadeTarget = 1f, lunge, wiggle, surface;
        internal float retargetAt, nextDartAt, stateUntil, nextNibbleAt, nibbleStart;
        internal int nibblesPlanned;
        internal bool gone, darting, nibbling;
        internal object owner;
        internal ShoreFish leader;   // 정어리 무리: 우두머리를 따라 붙어 다닌다(우두머리가 잡히거나 사라지면 따로 헤엄침).
        internal Vector3 slot;       // 우두머리 기준 자리(우두머리 몸 방향 좌표)
        internal Transform[] segs;   // 장어 몸 마디(머리 뒤로 이어짐)
        internal MeshRenderer[] segRenderers;
        public float SegmentSway { get { if (segs == null) return 0f; float m = 0f; foreach (var s in segs) m = Mathf.Max(m, Mathf.Abs(s.localPosition.x)); return m; } }
    }

    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly Color DayBody = new Color(.04f, .13f, .17f), NightBody = new Color(.01f, .04f, .07f);
    static readonly Color HaloColor = new Color(.62f, .90f, .96f);

    readonly List<ShoreFish> _fish = new List<ShoreFish>();
    readonly List<Vector2Int> _candidates = new List<Vector2Int>();
    readonly Dictionary<Vector2Int, bool> _shore = new Dictionary<Vector2Int, bool>();
    WorldGridService _grid;
    Transform _player;
    Vector3 _scannedAt;
    float _nextScan, _nextSpawnAt, _nextGridLookup;
    Mesh _bodyMesh, _tailMesh, _haloMesh;
    Material _bodyMat, _haloMat;
    MaterialPropertyBlock _block;
    bool _resourcesReady, _warned;
    int _serial;

    public IReadOnlyList<ShoreFish> Fish => _fish;
    public int CandidateCells => _candidates.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterBootstrap()
    {
        // 도메인 재로드가 꺼져 있어도 구독은 하나만 유지한다.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Enabled || scene.name != DemoRouteController.WorldScene) return;
        if (Instance != null && Instance.gameObject.scene == scene) return;
        var host = new GameObject("PA_ShoreFishSchool");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<ShoreFishSchool>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this && Instance.gameObject.scene == gameObject.scene) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        ClearAll();
        DestroyAsset(_bodyMesh); DestroyAsset(_tailMesh); DestroyAsset(_haloMesh);
        DestroyAsset(_bodyMat); DestroyAsset(_haloMat);
        if (Instance == this) Instance = null;
    }

    static void DestroyAsset(Object asset) { if (asset != null) Destroy(asset); }

    void Update()
    {
        if (!ResolveWorld()) { ClearAll(); return; }
        if (!EnsureResources()) return;
        float now = Time.time, dt = Time.deltaTime;
        Vector3 player = _player.position;
        if (Time.unscaledTime >= _nextScan || Flat(player - _scannedAt) > 3f) Scan(player);
        Populate(player, now);
        float night = GameClock.Instance != null ? NightGlowPresentation.GlowWeightAt(GameClock.Instance.CurrentHour) : 0f;
        for (int i = _fish.Count - 1; i >= 0; i--)
        {
            var f = _fish[i];
            if (f.root == null) { _fish.RemoveAt(i); continue; }
            if (!f.gone && dt > 0f) Step(f, dt, now, player);
            if (f.gone) { Destroy(f.root.gameObject); f.root = null; _fish.RemoveAt(i); continue; }
            Pose(f, dt, night);
        }
    }

    bool ResolveWorld()
    {
        if (_grid == null && Time.unscaledTime >= _nextGridLookup)
        {
            _nextGridLookup = Time.unscaledTime + 1f;
            _grid = FindFirstObjectByType<WorldGridService>();
        }
        var inventory = Inventory.instance;
        _player = inventory != null && inventory.isActiveAndEnabled ? inventory.transform : null;
        return _grid != null && _player != null;
    }

    void ClearAll()
    {
        foreach (var f in _fish)
        {
            f.gone = true; f.owner = null;
            if (f.root != null) Destroy(f.root.gameObject);
            f.root = null;
        }
        _fish.Clear();
        _candidates.Clear();
    }

    // ---- 물가 판정 -------------------------------------------------------------------------------

    void Scan(Vector3 player)
    {
        _nextScan = Time.unscaledTime + 1.5f; _scannedAt = player;
        _shore.Clear(); _candidates.Clear();
        _grid.WorldToCell(player, out var origin); // 섬 밖 좌표여도 주변 유효 칸은 그대로 본다.
        float size = _grid.Definition.CellSize;
        int r = Mathf.CeilToInt(SpawnRadius / size);
        for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            {
                if ((dx * dx + dz * dz) * size * size > SpawnRadius * SpawnRadius) continue;
                var cell = origin + new Vector2Int(dx, dz);
                if (IsShoreWater(cell)) _candidates.Add(cell);
            }
    }

    bool IsShoreWater(Vector2Int c)
    {
        if (_shore.TryGetValue(c, out bool known)) return known;
        bool result = false;
        if (_grid.TryGetCell(c, out var cell) && cell.HasWater)
            for (int dx = -ShoreBand; dx <= ShoreBand && !result; dx++)
                for (int dz = -ShoreBand; dz <= ShoreBand; dz++)
                    if (_grid.TryGetCell(c + new Vector2Int(dx, dz), out var near) && !near.HasWater) { result = true; break; }
        _shore[c] = result;
        return result;
    }

    // 물 칸 안에서도 이웃 땅 칸 경계(해안 턱)에서 margin 이상 떨어진 점만 헤엄칠 수 있다. FishingSpot도 찌 위치에 쓴다.
    public static bool ClearOfLand(WorldGridService grid, Vector3 p, float margin)
    {
        if (grid == null || !grid.WorldToCell(p, out var c) || !grid.TryGetCell(c, out var cell) || !cell.HasWater) return false;
        var definition = grid.Definition;
        float half = definition.CellSize * .5f;
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;
                var n = c + new Vector2Int(dx, dz);
                if (!grid.TryGetCell(n, out var near) || near.HasWater) continue;
                float ox = Mathf.Max(0f, Mathf.Abs(p.x - (definition.WorldOrigin.x + n.x * definition.CellSize)) - half);
                float oz = Mathf.Max(0f, Mathf.Abs(p.z - (definition.WorldOrigin.z + n.y * definition.CellSize)) - half);
                if (ox * ox + oz * oz < margin * margin) return false;
            }
        return true;
    }

    // 평소 돌아다니는 범위: 얕은 물가 칸 + 해안 턱 여유.
    bool Roamable(Vector3 p) => _grid.WorldToCell(p, out var c) && IsShoreWater(c) && ClearOfLand(_grid, p, LandMargin);

    bool PathClear(Vector3 a, Vector3 b, bool shoreOnly, float margin = LandMargin)
    {
        Vector3 d = b - a; d.y = 0f;
        int steps = Mathf.Max(1, Mathf.CeilToInt(d.magnitude / .4f));
        for (int i = 1; i <= steps; i++)
        {
            Vector3 p = a + d * (i / (float)steps);
            if (shoreOnly ? !Roamable(p) : !ClearOfLand(_grid, p, margin)) return false;
        }
        return true;
    }

    // ---- 개체군 ----------------------------------------------------------------------------------

    void Populate(Vector3 player, float now)
    {
        int count = _candidates.Count;
        int target = count == 0 ? 0 : Mathf.Clamp(count / 3, 2, MaxFish);
        int alive = 0;
        foreach (var f in _fish) if (f.fadeTarget > 0f) alive++;
        if (now < _nextSpawnAt) return;
        // reg216: 자리 물고기(숭어·복어·장어)는 드물어서, 무리가 이미 차 있으면 그 구역 물가에 와도 한 마리도 없을 때가 있었다(복어·장어 못 찾음).
        // 그 구역 물가 안에 있는데 그 종이 하나도 없으면(나올 시간일 때) 한 마리를 더(무리 한도 +1까지) 그 구역에 둔다.
        if (count > 0 && alive < target + 1 && TrySpawnResident(player)) { _nextSpawnAt = now + Random.Range(1.5f, 3f); return; }
        if (alive >= target) return;
        // 새 물가에 도착하면 빠르게 채우고, 그다음은 천천히 보충한다(잡은 물고기 자리는 Catch가 더 늦춘다).
        _nextSpawnAt = now + (alive < target / 2 ? .3f : Random.Range(1.5f, 3f));
        float half = _grid.Definition.CellSize * .5f;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            var cell = _candidates[Random.Range(0, count)];
            if (!_grid.CellToWorld(cell, out var centre)) continue;
            var p = centre + new Vector3(Random.Range(-half, half), 0f, Random.Range(-half, half));
            if (Flat(p - player) < 5f && attempt < 6) continue; // 플레이어 코앞에서 갑자기 나타나지 않게
            if (!Roamable(p)) continue;
            // 그 자리에 나올 종을 먼저 정하고, 나올 시간이 아니거나(송어 06–18·장어 14–24) 구역 밖이면 다른 자리를 본다.
            int species = FishingSpot.PickSpecies(p);
            if (!FishingSpot.ActiveNow(species) || !FishingSpot.InZone(species, p)) continue;
            float yaw = Random.Range(0f, 360f);
            var leader = Spawn(p, yaw, species);
            if (leader.Species == FishingSpot.SardineSpecies) SpawnSchoolmates(leader, p, yaw);
            return;
        }
    }

    // reg237: 하구 가까운 강가는 무리가 바다 물고기로 차 송어가 하나도 없을 때가 있었다 → 강·연못에만 사는 송어·붕어도 보장.
    static readonly int[] Residents = { FishingSpot.MulletSpecies, FishingSpot.PufferSpecies, FishingSpot.EelSpecies, FishingSpot.TroutSpecies, FishingSpot.CrucianSpecies };
    bool TrySpawnResident(Vector3 player)
    {
        foreach (int species in Residents)
        {
            if (!FishingSpot.ActiveNow(species)) continue;
            bool present = false;
            foreach (var f in _fish) if (f.Alive && f.fadeTarget > 0f && f.Species == species) { present = true; break; }
            if (present) continue;
            float half = _grid.Definition.CellSize * .5f;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                var cell = _candidates[Random.Range(0, _candidates.Count)];
                if (!_grid.CellToWorld(cell, out var centre)) continue;
                var p = centre + new Vector3(Random.Range(-half, half), 0f, Random.Range(-half, half));
                if (Flat(p - player) < 5f || !FishingSpot.InZone(species, p) || !Roamable(p)) continue;
                Spawn(p, Random.Range(0f, 360f), species);
                ResidentSpawns++;
                return true;
            }
        }
        return false;
    }
    public int ResidentSpawns { get; private set; }
    public bool IsCandidate(Vector2Int c) => _candidates.Contains(c);
    public bool RoamableAt(Vector3 p) => _grid != null && Roamable(p);

    // 정어리는 두 마리가 우두머리 뒤 좌우에 붙어 세 마리 무리가 된다(놓을 자리가 물이 아니면 그만큼 적게).
    void SpawnSchoolmates(ShoreFish leader, Vector3 p, float yaw)
    {
        // reg255: 물가에 붙어 생긴 우두머리는 두 자리가 다 물 밖이라 혼자 다닐 때가 있었다(54회 중 1) →
        // 기본 두 자리가 막히면 반대쪽·바로 뒤 자리를 차례로 보고 두 마리까지 붙인다.
        var slots = new[] { new Vector3(.42f, 0f, -.4f), new Vector3(-.38f, 0f, -.55f),
            new Vector3(-.42f, 0f, -.4f), new Vector3(.38f, 0f, -.55f), new Vector3(0f, 0f, -.6f), new Vector3(.3f, 0f, -.9f), new Vector3(-.3f, 0f, -.9f) };
        int mates = 0;
        foreach (var slot in slots)
        {
            if (mates >= 2 || _fish.Count >= MaxFish + 2) return;
            Vector3 at = p + Quaternion.Euler(0f, yaw, 0f) * slot;
            if (!Roamable(at)) continue;
            var mate = Spawn(at, yaw, FishingSpot.SardineSpecies);
            mate.leader = leader; mate.slot = slot;
            mates++;
        }
    }

    public int SchoolFollowers { get { int n = 0; foreach (var f in _fish) if (f.Alive && f.leader != null && f.leader.Alive) n++; return n; } }

    // 종 → 크기 등급: 3 정어리 가장 작음, 0 기본 물고기 작음, 2 노랑쥐치 중간, 1 붉은 도미 큼(드물고 어렵다).
    // 4 고등어: 참치보다 길고(.85~1.0) 폭은 좁게(Spawn에서 x .62) — 길쭉한 그림자로 구분된다.
    static float LengthFor(int species, float t) =>
        species == FishingSpot.SardineSpecies ? Mathf.Lerp(.42f, .5f, t) :
        species == FishingSpot.MackerelSpecies ? Mathf.Lerp(.85f, 1f, t) :
        species == FishingSpot.FlounderSpecies ? Mathf.Lerp(.68f, .82f, t) :
        species == FishingSpot.MulletSpecies ? Mathf.Lerp(.95f, 1.1f, t) :
        species == FishingSpot.PufferSpecies ? Mathf.Lerp(.5f, .6f, t) :
        species == FishingSpot.TroutSpecies ? Mathf.Lerp(.75f, .9f, t) :
        species == FishingSpot.EelSpecies ? Mathf.Lerp(1.1f, 1.3f, t) :
        species == FishingSpot.CrucianSpecies ? Mathf.Lerp(.48f, .58f, t) :
        species == 1 ? Mathf.Lerp(1.15f, 1.4f, t) : species == 2 ? Mathf.Lerp(.8f, 1.0f, t) : Mathf.Lerp(.6f, .76f, t); // 12.5m 고정 카메라에서 읽히게 약 1.3배(2026-10-31 화면 검토)

    ShoreFish Spawn(Vector3 p, float yaw, int species = -1)
    {
        if (species < 0 || species >= FishingSpot.SpeciesCount) species = FishingSpot.PickSpecies(p);
        float t = Random.value;
        var f = new ShoreFish
        {
            Species = species, Length = LengthFor(species, t), State = Stage.Roam,
            Difficulty = Mathf.Clamp01(FishingSpot.SpeciesDifficulty(species) + (t - .5f) * .12f),
            pos = p, target = p, heading = yaw, fade = 0f, fadeTarget = 1f,
            nextDartAt = Time.time + Random.Range(4f, 14f), wiggle = Random.Range(0f, 6.3f)
        };
        f.surface = SurfaceAt(p, p.y);
        var root = new GameObject("ShoreFish_" + (++_serial) + "_" + species).transform;
        root.SetParent(transform, false);
        // 넙치(5)는 넓고 짧은 원반형 그림자(폭 ×1.35, 길이 ×.8).
        // 숭어(6)는 고등어보다 조금 굵은 길쭉한 그림자(폭 ×.7), 복어(7)는 동글동글(폭 ×1.15, 길이 ×.9).
        root.localScale = species == FishingSpot.MackerelSpecies ? new Vector3(f.Length * .62f, f.Length, f.Length)
            : species == FishingSpot.FlounderSpecies ? new Vector3(f.Length * 1.35f, f.Length, f.Length * .8f)
            : species == FishingSpot.MulletSpecies ? new Vector3(f.Length * .7f, f.Length, f.Length)
            : species == FishingSpot.PufferSpecies ? new Vector3(f.Length * 1.15f, f.Length, f.Length * .9f)
            // 송어(8)는 날렵한 중간 그림자(폭 ×.6). 장어(9)는 아래에서 마디를 이어 길게 굽이친다(머리 마디는 짧고 가늘게).
            : species == FishingSpot.TroutSpecies ? new Vector3(f.Length * .6f, f.Length, f.Length)
            // 붕어(10)는 작고 몸높이가 있는 둥근 그림자(폭 ×.85), 잔잔하게(내달리지 않음).
            : species == FishingSpot.CrucianSpecies ? new Vector3(f.Length * .85f, f.Length, f.Length * .95f)
            : species == FishingSpot.EelSpecies ? new Vector3(f.Length * .34f, f.Length, f.Length * EelSegmentScale) : Vector3.one * f.Length;
        if (species == FishingSpot.EelSpecies || species == FishingSpot.CrucianSpecies) f.nextDartAt = float.MaxValue; // 장어·붕어는 내달리지 않는다
        // 복어는 내달리지 않고 느릿느릿(넙치처럼 다만 덜 쉼).
        if (species == FishingSpot.PufferSpecies) f.nextDartAt = float.MaxValue;
        // 넙치는 내달리지 않는다(잠깐 움직이고 오래 쉬기).
        if (species == FishingSpot.FlounderSpecies) f.nextDartAt = float.MaxValue;
        // 고등어는 짧게 달리고 쉬는 회유(연구 F05): 첫 달리기도 일찍.
        if (species == FishingSpot.MackerelSpecies) f.nextDartAt = Time.time + Random.Range(1.5f, 4f);
        f.root = root;
        f.halo = MakeRenderer(root, "NightHalo", _haloMesh, _haloMat, Vector3.zero);
        f.body = MakeRenderer(root, "Silhouette", _bodyMesh, _bodyMat, Vector3.zero);
        f.tailRenderer = MakeRenderer(root, "Tail", _tailMesh, _bodyMat, new Vector3(0f, 0f, TailPivot));
        f.tail = f.tailRenderer.transform;
        if (species == FishingSpot.EelSpecies)
        {
            // 연구 F09 "길고 굽이치는 몸 — 공통 몸 비율 조정만으로 완료 처리하지 않음": 몸 마디 3개를 뒤로 잇고 Pose에서 위상차로 좌우로 굽힌다.
            f.segs = new Transform[EelSegments]; f.segRenderers = new MeshRenderer[EelSegments];
            for (int i = 0; i < EelSegments; i++)
            {
                var seg = MakeRenderer(root, "EelSeg" + i, _bodyMesh, _bodyMat, new Vector3(0f, 0f, -EelSpacing * (i + 1)));
                seg.transform.localScale = new Vector3(1f - .09f * i, 1f, 1f);
                f.segs[i] = seg.transform; f.segRenderers[i] = seg;
            }
            f.tail.localPosition = new Vector3(0f, 0f, -EelSpacing * EelSegments + TailPivot);
        }
        _fish.Add(f);
        return f;
    }

    static MeshRenderer MakeRenderer(Transform parent, string name, Mesh mesh, Material material, Vector3 local)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        return renderer;
    }

    // ---- 낚시 연결(FishingSpot이 부른다) -----------------------------------------------------------

    // 찌 근처(NoticeRadius)에서 돌아다니던 물고기 하나가 찌를 알아채고 다가온다. 없으면 null.
    public ShoreFish Attract(object owner, Vector3 lure)
    {
        if (_grid == null) return null;
        ShoreFish best = null;
        float bestScore = float.MaxValue;
        foreach (var f in _fish)
        {
            if (!f.Alive || f.State != Stage.Roam || f.owner != null || f.fade < .6f || f.fadeTarget <= 0f || f.darting) continue;
            float d = Flat(f.pos - lure);
            if (d > NoticeRadius) continue;
            float score = d * Random.Range(.7f, 1.3f);
            if (score >= bestScore || !PathClear(f.pos, StopPoint(f, lure), false, LandMargin * .6f)) continue;
            best = f; bestScore = score;
        }
        if (best != null) Engage(best, owner, lure);
        return best;
    }

    // 오래 기다려도 근처에 물고기가 없으면 조금 먼 물에서 한 마리가 찌를 보고 헤엄쳐 온다.
    public ShoreFish SpawnVisitor(object owner, Vector3 lure)
    {
        if (_grid == null || _fish.Count >= MaxFish + 2 || !EnsureResources()) return null;
        for (int attempt = 0; attempt < 16; attempt++)
        {
            Vector3 p = lure + Forward(Random.Range(0f, 360f)) * Random.Range(3.5f, 7f);
            if (!ClearOfLand(_grid, p, LandMargin) || !PathClear(p, lure, false, LandMargin * .6f)) continue;
            Vector3 toward = lure - p;
            var f = Spawn(p, Mathf.Atan2(toward.x, toward.z) * Mathf.Rad2Deg);
            Engage(f, owner, lure);
            return f;
        }
        return null;
    }

    public void Hook(ShoreFish f)
    {
        if (f == null || !f.Alive) return;
        f.State = Stage.Hooked; f.BobberDip = 1f; f.stateUntil = Time.time + 40f;
    }

    // 끌어올린 물고기의 그림자는 바로 사라진다(뭍의 물고기가 이어받는다). 그 자리는 잠시 뒤에 보충된다.
    public void Catch(ShoreFish f)
    {
        if (f == null) return;
        f.owner = null; f.gone = true;
        if (f.root != null) f.root.gameObject.SetActive(false);
        _nextSpawnAt = Mathf.Max(_nextSpawnAt, Time.time + Random.Range(5f, 8f));
    }

    // spooked: 놓친 입질·너무 이른 당김·실패한 릴 → 휙 달아나 사라진다. 아니면 다시 돌아다닌다.
    public void Release(ShoreFish f, bool spooked)
    {
        if (f == null || !f.Alive) return;
        f.owner = null; f.BobberDip = 0f; f.nibbling = false;
        if (!spooked) { f.State = Stage.Roam; f.darting = false; f.retargetAt = 0f; return; }
        f.State = Stage.Flee; f.darting = true;
        Vector3 away = f.pos - f.lure; away.y = 0f;
        if (away.sqrMagnitude < .01f) away = -Forward(f.heading);
        float baseYaw = Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg;
        f.target = f.pos;
        for (int i = 0; i < 7; i++)
        {
            float yaw = baseYaw + (i == 0 ? 0f : (i % 2 == 1 ? 1f : -1f) * 35f * ((i + 1) / 2));
            Vector3 p = f.pos + Forward(yaw) * 3.2f;
            if (PathClear(f.pos, p, false, LandMargin * .6f)) { f.target = p; break; }
        }
        f.speed = Mathf.Max(f.speed, 1.8f); f.cruise = 3.2f;
        f.stateUntil = Time.time + .8f;
    }

    // 뭍에서 놓친 물고기가 물로 돌아가면 그 자리에서 그림자가 보였다가 달아난다.
    public void ReleaseEscapee(Vector3 at, int species)
    {
        if (_grid == null || !EnsureResources() || !ClearOfLand(_grid, at, .2f)) return;
        var f = Spawn(at, Random.Range(0f, 360f), species);
        f.fade = 1f; f.lure = at;
        Release(f, true);
    }

    void Engage(ShoreFish f, object owner, Vector3 lure)
    {
        f.owner = owner; f.lure = lure; f.State = Stage.Approach;
        f.target = StopPoint(f, lure);
        f.cruise = Random.Range(.45f, .65f); f.darting = false;
        f.stateUntil = Time.time + 14f;
        // 크고 어려운 물고기일수록 더 오래 간을 본다(1~3번).
        f.nibblesPlanned = Mathf.Clamp(Random.Range(1, 3) + (f.Difficulty > .6f ? 1 : 0), 1, 3);
        f.Nibbles = 0; f.BobberDip = 0f; f.nibbling = false;
    }

    // 입이 찌에 닿도록 몸 중심을 찌에서 반 몸길이 뒤에 둔다. 그 자리가 해안 턱이면 옆으로 돌아 들어온다.
    Vector3 StopPoint(ShoreFish f, Vector3 lure)
    {
        Vector3 dir = f.pos - lure; dir.y = 0f;
        if (dir.sqrMagnitude < .0001f) dir = -Forward(f.heading);
        dir.Normalize();
        float back = f.Length * .5f + .02f;
        for (int i = 0; i < 5; i++)
        {
            float yaw = i == 0 ? 0f : (i % 2 == 1 ? 1f : -1f) * 40f * ((i + 1) / 2);
            Vector3 p = lure + Quaternion.Euler(0f, yaw, 0f) * dir * back;
            if (ClearOfLand(_grid, p, LandMargin * .6f)) return p;
        }
        return lure;
    }

    // ---- 움직임 ----------------------------------------------------------------------------------

    void Step(ShoreFish f, float dt, float now, Vector3 player)
    {
        f.fade = Mathf.MoveTowards(f.fade, f.fadeTarget, dt / (f.fadeTarget > f.fade ? 1.1f : .7f));
        if (f.fadeTarget <= 0f && f.fade <= 0f) { f.gone = true; return; }
        f.lunge = Mathf.MoveTowards(f.lunge, 0f, dt * .6f);
        switch (f.State)
        {
            case Stage.Roam: Roam(f, dt, now, player); break;
            case Stage.Approach:
                if (now > f.stateUntil || !Steer(f, f.target, f.cruise, 170f, dt, false, LandMargin * .6f)) { Release(f, false); break; }
                if (Flat(f.target - f.pos) < .08f)
                { f.State = Stage.Nibble; f.Nibbles = 0; f.nibbling = false; f.speed = 0f; f.nextNibbleAt = now + Random.Range(.5f, 1.1f); }
                break;
            case Stage.Nibble: Nibble(f, dt, now); break;
            case Stage.Bite:
                // FishingSpot이 입질 창(1초 안팎) 안에 처리한다. 주인이 사라졌을 때만 스스로 달아난다.
                f.BobberDip = 1f; f.lunge = .1f; Face(f, f.lure, dt, 360f);
                if (now > f.stateUntil) Release(f, true);
                break;
            case Stage.Hooked:
                // 줄에 걸려 찌 주변에서 몸부림친다.
                f.BobberDip = 1f; f.speed = 0f;
                f.heading += Mathf.Sin(now * 9f) * 260f * dt;
                f.pos = Vector3.MoveTowards(f.pos, f.lure + Forward(now * 150f) * .22f, dt * 1.6f);
                if (now > f.stateUntil) Release(f, true);
                break;
            case Stage.Flee:
                Steer(f, f.target, f.cruise, 600f, dt, false, LandMargin * .6f);
                if (now >= f.stateUntil) f.fadeTarget = 0f;
                break;
        }
    }

    void Roam(ShoreFish f, float dt, float now, Vector3 player)
    {
        if (Flat(f.pos - player) > DespawnRadius) f.fadeTarget = 0f;
        // 찌를 따라 얕은 물가 밖(깊은 물·해안 턱 가까이)까지 왔다가 풀려난 물고기는 깊은 물로 사라진다.
        if (!Roamable(f.pos))
        {
            f.fadeTarget = 0f;
            Steer(f, f.pos + Forward(f.heading) * 2f, .4f, 90f, dt, false, LandMargin * .6f);
            return;
        }
        if (f.leader != null)
        {
            var lead = f.leader;
            if (lead.Alive && lead.State == Stage.Roam && lead.fadeTarget > 0f)
            {
                // 우두머리 몸 방향 기준 자리로: 멀어지면 조금 더 빨리 따라붙는다.
                Vector3 slot = lead.pos + Quaternion.Euler(0f, lead.heading, 0f) * f.slot;
                float gap = Flat(slot - f.pos);
                f.cruise = Mathf.Max(lead.cruise, lead.speed) + Mathf.Clamp(gap - .15f, 0f, 1f) * 1.2f;
                Steer(f, slot, f.cruise, 220f, dt, true, LandMargin);
                return;
            }
            if (!lead.Alive) f.leader = null;
        }
        if (f.darting && now >= f.stateUntil) { f.darting = false; f.cruise = Random.Range(.3f, .55f); f.retargetAt = now + Random.Range(1f, 3f); }
        // 가끔 휙 방향을 틀며 짧게 내달린다.
        if (!f.darting && now >= f.nextDartAt)
        {
            f.nextDartAt = f.Species == FishingSpot.FlounderSpecies ? float.MaxValue
                : now + (f.Species == FishingSpot.MackerelSpecies ? Random.Range(3f, 6f) : Random.Range(8f, 18f));
            if (TryPickTarget(f, 1.6f, 3f, 75f, out var dash))
            { f.target = dash; f.darting = true; f.stateUntil = now + .7f; f.speed = Mathf.Max(f.speed, 1.4f); f.cruise = Random.Range(2.4f, 3.1f); }
        }
        // 2026-11-04 flounder-r145: 멈춰 쉬기는 target=pos라 '목표 도착' 조건이 다음 프레임에 바로 참이 되어 쉬기가 한 프레임 만에 끝났다.
        // 쉬는 중(cruise 0)에는 정해 둔 휴식 시간(retargetAt)만 따른다.
        if (!f.darting && (now >= f.retargetAt || f.cruise > 0f && Flat(f.target - f.pos) < .25f))
        {
            f.retargetAt = now + Random.Range(3.5f, 8f);
            float size = Mathf.InverseLerp(.4f, 1.05f, f.Length);
            bool flat = f.Species == FishingSpot.FlounderSpecies;
            if (Random.value < (flat ? .65f : .3f)) { f.target = f.pos; f.cruise = 0f; f.retargetAt = now + (flat ? Random.Range(3f, 6f) : Random.Range(1f, 2.6f)); } // 잠시 멈춰 떠 있기(넙치는 오래 바닥에 붙어 쉼)
            else if (TryPickTarget(f, 1.2f, 4.5f, 110f, out var next)) { f.target = next; f.cruise = Random.Range(.3f, .6f) * Mathf.Lerp(1.15f, .8f, size); }
            else { f.target = f.pos; f.cruise = 0f; f.retargetAt = now + .5f; }
        }
        Steer(f, f.target + Separation(f), f.cruise, f.darting ? 420f : 110f, dt, true, LandMargin);
    }

    void Nibble(ShoreFish f, float dt, float now)
    {
        Face(f, f.lure, dt, 200f);
        f.speed = 0f;
        if (f.nibbling)
        {
            float k = Mathf.Clamp01((now - f.nibbleStart) / NibblePulse);
            float s = Mathf.Sin(k * Mathf.PI);
            f.lunge = s * .1f; f.BobberDip = s * .45f;
            if (k >= 1f) { f.nibbling = false; f.BobberDip = 0f; f.Nibbles++; f.nextNibbleAt = now + Random.Range(.7f, 1.5f); }
            return;
        }
        if (now < f.nextNibbleAt) return;
        if (f.Nibbles >= f.nibblesPlanned) { f.State = Stage.Bite; f.BobberDip = 1f; f.lunge = .1f; f.stateUntil = now + 6f; return; }
        f.nibbling = true; f.nibbleStart = now;
    }

    bool TryPickTarget(ShoreFish f, float minDistance, float maxDistance, float spread, out Vector3 target)
    {
        for (int i = 0; i < 8; i++)
        {
            float yaw = f.heading + Random.Range(-spread, spread) + (i >= 5 ? 180f : 0f); // 막히면 돌아선다
            Vector3 p = f.pos + Forward(yaw) * Random.Range(minDistance, maxDistance);
            // r204 자리 물고기(숭어·복어)는 제 구역(항구 18m·바위 4m) 안의 목표만 고른다.
            if (Roamable(p) && FishingSpot.InZone(f.Species, p) && PathClear(f.pos, p, true)) { target = p; return true; }
        }
        target = f.pos;
        return false;
    }

    Vector3 Separation(ShoreFish f)
    {
        Vector3 push = Vector3.zero;
        foreach (var other in _fish)
        {
            if (other == f || !other.Alive) continue;
            Vector3 d = f.pos - other.pos; d.y = 0f;
            float m = d.magnitude, reach = (f.Length + other.Length) * .6f;
            if (m > .001f && m < reach) push += d / m * (reach - m) * 2f;
        }
        return push;
    }

    // 목표 쪽으로 제한된 회전 속도로 돌며 나아간다. 다음 위치가 헤엄칠 수 없는 곳이면 멈추고 false.
    bool Steer(ShoreFish f, Vector3 goal, float cruise, float turnRate, float dt, bool shoreOnly, float margin)
    {
        Vector3 to = goal - f.pos; to.y = 0f;
        float dist = to.magnitude;
        if (dist > .05f)
        {
            float want = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            f.heading = Mathf.MoveTowardsAngle(f.heading, want, turnRate * dt);
            float align = Mathf.Clamp01(Mathf.Cos(Mathf.DeltaAngle(f.heading, want) * Mathf.Deg2Rad));
            cruise *= Mathf.Lerp(.25f, 1f, align) * Mathf.Lerp(.35f, 1f, Mathf.Clamp01(dist / .8f));
        }
        else cruise = 0f;
        f.speed = Mathf.MoveTowards(f.speed, cruise, dt * (f.darting ? 6f : 1.2f));
        if (f.speed <= .001f) return true;
        Vector3 next = f.pos + Forward(f.heading) * f.speed * dt;
        // 구역 안에 있던 자리 물고기는 물가를 헤엄칠 때 구역 밖으로 한 걸음도 나가지 않는다(멈추고 다시 고른다).
        bool leashed = shoreOnly && FishingSpot.InZone(f.Species, f.pos) && !FishingSpot.InZone(f.Species, next);
        if (!leashed && (shoreOnly ? Roamable(next) : ClearOfLand(_grid, next, margin))) { f.pos = next; return true; }
        f.speed = 0f; f.retargetAt = 0f;
        return false;
    }

    static void Face(ShoreFish f, Vector3 point, float dt, float turnRate)
    {
        Vector3 to = point - f.pos; to.y = 0f;
        if (to.sqrMagnitude > .0004f) f.heading = Mathf.MoveTowardsAngle(f.heading, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, turnRate * dt);
    }

    // ---- 표현 ------------------------------------------------------------------------------------

    void Pose(ShoreFish f, float dt, float night)
    {
        f.surface = SurfaceAt(f.pos, f.surface);
        float swim = Mathf.Clamp01(f.speed / 1.2f);
        bool thrash = f.State == Stage.Hooked || f.State == Stage.Bite;
        f.wiggle += dt * (4f + swim * 10f + (thrash ? 14f : 0f));
        float tail = Mathf.Sin(f.wiggle) * (10f + 26f * swim + (thrash ? 22f : 0f));
        Vector3 forward = Forward(f.heading);
        f.root.SetPositionAndRotation(new Vector3(f.pos.x, f.surface - SurfaceDepth, f.pos.z) + forward * f.lunge,
            Quaternion.Euler(0f, f.heading - tail * .18f, 0f));
        f.tail.localRotation = Quaternion.Euler(0f, tail, 0f);
        if (f.segs != null)
        {
            // 장어: 머리에서 꼬리로 갈수록 크게, 위상이 늦게 좌우로 굽이친다(뱀처럼).
            float lastX = 0f, lastYaw = 0f;
            for (int i = 0; i < f.segs.Length; i++)
            {
                float phase = f.wiggle * .55f - (i + 1) * 1.15f;
                lastX = Mathf.Sin(phase) * .5f * (i + 1) / f.segs.Length;
                lastYaw = Mathf.Cos(phase) * 38f;
                f.segs[i].localPosition = new Vector3(lastX, 0f, -EelSpacing * (i + 1));
                f.segs[i].localRotation = Quaternion.Euler(0f, lastYaw, 0f);
            }
            f.tail.localPosition = new Vector3(lastX * 1.2f, 0f, -EelSpacing * f.segs.Length + TailPivot);
            f.tail.localRotation = Quaternion.Euler(0f, lastYaw + tail * .5f, 0f);
        }
        // 수면 바로 아래의 어두운 그림자. 큰 물고기가 조금 더 진하다. 밤에는 엷은 물빛 테두리로 읽힌다.
        float alpha = f.fade * Mathf.Lerp(.66f, .78f, night) * (.9f + .2f * Mathf.InverseLerp(.55f, 1.35f, f.Length));
        Color body = Color.Lerp(DayBody, NightBody, night); body.a = alpha;
        _block.SetColor(ColorId, body);
        f.body.SetPropertyBlock(_block);
        f.tailRenderer.SetPropertyBlock(_block);
        if (f.segRenderers != null) foreach (var seg in f.segRenderers) seg.SetPropertyBlock(_block);
        float halo = f.fade * night * .2f;
        f.halo.enabled = halo > .01f;
        if (f.halo.enabled)
        {
            _block.SetColor(ColorId, new Color(HaloColor.r, HaloColor.g, HaloColor.b, halo));
            f.halo.SetPropertyBlock(_block);
        }
    }

    float SurfaceAt(Vector3 p, float fallback)
    {
        if (_grid != null && _grid.WorldToCell(p, out var c) && _grid.TryGetCell(c, out var cell) && cell.HasWater)
            return WorldChunkMeshBuilder.OpeningWaterHeight(_grid.Definition, cell);
        return fallback;
    }

    static Vector3 Forward(float yaw)
    {
        float r = yaw * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r));
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

    // ---- 런타임 메시/재질 ------------------------------------------------------------------------

    bool EnsureResources()
    {
        if (_resourcesReady) return true;
        if (_warned) return false;
        // Sprites/Default는 빌드에 항상 포함된다. 물(Transparent-10) 뒤에 그려 수면 아래 그림자처럼 보인다.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            _warned = true;
            Debug.LogWarning("[ShoreFishSchool] Sprites/Default is missing from this build; shore fish silhouettes disabled.");
            return false;
        }
        _haloMat = new Material(shader) { name = "PA_ShoreFish_NightHalo (runtime)", hideFlags = HideFlags.DontSave, renderQueue = 3001 };
        _bodyMat = new Material(shader) { name = "PA_ShoreFish_Silhouette (runtime)", hideFlags = HideFlags.DontSave, renderQueue = 3002 };
        var outline = BodyOutline();
        _bodyMesh = BuildFan(outline, new Vector2(0f, .08f), 1f, .045f, 1f, .9f, "PA_ShoreFish_Body");
        _haloMesh = BuildFan(outline, new Vector2(0f, .08f), 1.22f, .14f, 1f, .55f, "PA_ShoreFish_Halo");
        _tailMesh = BuildTail();
        _block = new MaterialPropertyBlock();
        _resourcesReady = true;
        return true;
    }

    // 단위 길이(머리 +0.5 ~ 꼬리 끝 -0.5, XZ 평면) 물고기 몸통 외곽. 위에서 본 통통한 타원형.
    static readonly Vector2[] RightSide =
    {
        new Vector2(.065f, .45f), new Vector2(.12f, .36f), new Vector2(.155f, .24f), new Vector2(.172f, .10f),
        new Vector2(.165f, -.04f), new Vector2(.13f, -.15f), new Vector2(.085f, -.22f), new Vector2(.05f, -.27f)
    };

    static Vector2[] BodyOutline()
    {
        var list = new List<Vector2> { new Vector2(0f, .5f) };
        list.AddRange(RightSide);
        for (int i = RightSide.Length - 1; i >= 0; i--) list.Add(new Vector2(-RightSide[i].x, RightSide[i].y));
        return list.ToArray();
    }

    // 중심 부채꼴 + 바깥 깃털 띠(알파 0)로 가장자리를 부드럽게 한다. 색은 정점 알파 × 블록 _Color.
    static Mesh BuildFan(Vector2[] outline, Vector2 centre, float scale, float feather, float centreAlpha, float edgeAlpha, string name)
    {
        int n = outline.Length;
        var vertices = new Vector3[1 + n * 2];
        var colors = new Color[vertices.Length];
        var triangles = new List<int>(n * 9);
        vertices[0] = new Vector3(centre.x, 0f, centre.y);
        colors[0] = new Color(1f, 1f, 1f, centreAlpha);
        for (int i = 0; i < n; i++)
        {
            Vector2 p = centre + (outline[i] - centre) * scale;
            Vector2 edge = outline[(i + 1) % n] - outline[(i + n - 1) % n];
            Vector2 normal = new Vector2(-edge.y, edge.x).normalized;
            if (Vector2.Dot(normal, outline[i] - centre) < 0f) normal = -normal;
            Vector2 outer = p + normal * feather;
            vertices[1 + i] = new Vector3(p.x, 0f, p.y);
            vertices[1 + n + i] = new Vector3(outer.x, 0f, outer.y);
            colors[1 + i] = new Color(1f, 1f, 1f, edgeAlpha);
            colors[1 + n + i] = new Color(1f, 1f, 1f, 0f);
            int next = (i + 1) % n;
            triangles.Add(0); triangles.Add(1 + i); triangles.Add(1 + next);
            triangles.Add(1 + i); triangles.Add(1 + n + i); triangles.Add(1 + n + next);
            triangles.Add(1 + i); triangles.Add(1 + n + next); triangles.Add(1 + next);
        }
        var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // 꼬리지느러미(피벗 = 꼬리 뿌리). 몸통 쪽으로 살짝 겹쳐 흔들려도 틈이 없다.
    static Mesh BuildTail()
    {
        var vertices = new[]
        {
            new Vector3(.055f, 0f, .03f), new Vector3(.17f, 0f, -.25f), new Vector3(0f, 0f, -.15f),
            new Vector3(-.17f, 0f, -.25f), new Vector3(-.055f, 0f, .03f)
        };
        var colors = new[]
        {
            new Color(1f, 1f, 1f, .85f), new Color(1f, 1f, 1f, .55f), new Color(1f, 1f, 1f, .7f),
            new Color(1f, 1f, 1f, .55f), new Color(1f, 1f, 1f, .85f)
        };
        var mesh = new Mesh { name = "PA_ShoreFish_Tail", hideFlags = HideFlags.DontSave };
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = new[] { 0, 1, 2, 4, 2, 3, 0, 2, 4 };
        mesh.RecalculateBounds();
        return mesh;
    }
}
