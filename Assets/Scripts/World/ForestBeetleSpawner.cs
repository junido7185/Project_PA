using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 숲 가장자리 장수풍뎅이(Logs/ParallelProduction/Content-insects-plan-20261026/worktree/HANDOFF.md §B, owner 인수 2026-11-04).
// 항구에서 가장 가까운 실제 나무(Timber) 둘의 밑동 옆(항구 쪽 한 칸) 땅 위에 한 마리씩. 잡기·보상·저장은 기존 BugCritter/Inventory 그대로.
// 나비와 다른 점: 떠다니지 않고 붙어 있음(perched), 3.5m 안에서 달려오면 먼저 날아감(wary), 그물이 닿는 거리 1.3m.
// 2026-11-09 넓적사슴벌레(연구 B08 "숲의 낮은 나무줄기, 14–24, 드물게 만나도 충분히 관찰할 틈"): 세 번째로 가까운 나무 밑동에 한 마리, 14~24시에만.
// 2026-11-09 알락하늘소(연구 B09 "있는 그루터기 또는 낮은 나무 옆, 08–20, 긴 더듬이의 움직임"): 사슴벌레 다음 나무 밑동에 한 마리, 8~20시, 더듬이를 천천히 흔든다.
public static class ForestBeetleSpawner
{
    public const string NamePrefix = "FirstDay_Insect_Beetle_";
    public const string StagName = "FirstDay_Insect_StagBeetle_0";
    public const string LonghornName = "FirstDay_Insect_Longhorn_0";
    public const string CricketName = "FirstDay_Insect_Cricket_0";
    public const float StagFrom = 14f, StagUntil = 24f, LonghornFrom = 8f, LonghornUntil = 20f;

    public static int Spawn(Transform parent, Vector3 harbor, IEnumerable<WorldResourceSpawnRecord> spawns, WorldGridService grid)
    {
        var item = Resources.Load<Item>("Items/Item_Bug_Beetle");
        if (item == null || item.model == null || grid == null || spawns == null) return 0;
        int placed = 0;
        foreach (var tree in NearestTrees(harbor, spawns, grid).Take(2))
        {
            if (!BesideTrunk(tree.Coordinate, tree.Position, harbor, grid, out Vector3 at)) continue;
            var beetle = new GameObject(NamePrefix + placed);
            beetle.transform.SetParent(parent);
            beetle.transform.position = at;
            beetle.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(harbor - at, Vector3.up).normalized + Vector3.right * .3f);
            FirstDayStudioAssets.Place(item.model, beetle.transform, at, .16f);
            beetle.AddComponent<SphereCollider>().isTrigger = true;
            var critter = beetle.AddComponent<BugCritter>();
            critter.Configure(item, placed * 1.3f);
            critter.ConfigureSpecies(true, 3.5f, 1.3f);
            placed++;
        }
        return placed;
    }

    // 장수풍뎅이 두 나무 다음부터 밑동 자리가 있는 나무를 차례로: 첫째 = 넓적사슴벌레, 둘째 = 알락하늘소. 시간 창은 CritterHours가 켜고 끈다.
    static List<Vector3> LaterTrunkSpots(Vector3 harbor, IEnumerable<WorldResourceSpawnRecord> spawns, WorldGridService grid)
    {
        var spots = new List<Vector3>();
        foreach (var tree in NearestTrees(harbor, spawns, grid).Skip(2).Take(8))
            if (BesideTrunk(tree.Coordinate, tree.Position, harbor, grid, out Vector3 at)) spots.Add(at);
        return spots;
    }

    public static bool SpawnStag(Transform parent, Vector3 harbor, IEnumerable<WorldResourceSpawnRecord> spawns, WorldGridService grid)
    {
        var item = Resources.Load<Item>("Items/Item_Bug_StagBeetle");
        if (item == null || item.model == null || grid == null || spawns == null) return false;
        var spots = LaterTrunkSpots(harbor, spawns, grid);
        if (spots.Count == 0) return false;
        // 장수풍뎅이(3.5m)보다 순함 — 드물게 만나도 관찰할 틈
        PlaceOne(parent, "FirstDay_StagBeetle", StagName, item, spots[0], harbor, .13f, 2.5f, StagFrom, StagUntil, Vector3.left);
        return true;
    }

    public static bool SpawnLonghorn(Transform parent, Vector3 harbor, IEnumerable<WorldResourceSpawnRecord> spawns, WorldGridService grid)
    {
        var item = Resources.Load<Item>("Items/Item_Bug_Longhorn");
        if (item == null || item.model == null || grid == null || spawns == null) return false;
        var spots = LaterTrunkSpots(harbor, spawns, grid);
        if (spots.Count < 2) return false;
        var critter = PlaceOne(parent, "FirstDay_Longhorn", LonghornName, item, spots[1], harbor, .22f, 2.5f, LonghornFrom, LonghornUntil, Vector3.right);
        // longhorn-r188: .12는 더듬이 높이 때문에 몸이 기본 카메라에서 15px 남짓이었다 → .22
        critter.UseOwnWings(); // 날개 축은 없다 — 놀라 날 때 몸 폭을 접지 않게
        var sway = critter.gameObject.AddComponent<AntennaSway>();
        sway.Antennae = critter.GetComponentsInChildren<Transform>(true).Where(t => t.name == "LonghornAntennaL" || t.name == "LonghornAntennaR").ToArray();
        return true;
    }

    static BugCritter PlaceOne(Transform parent, string holderName, string name, Item item, Vector3 at, Vector3 harbor, float height, float wary, float from, float until, Vector3 lean)
    {
        var holder = new GameObject(holderName);
        holder.transform.SetParent(parent);
        var bug = new GameObject(name);
        bug.transform.SetParent(holder.transform);
        bug.transform.position = at;
        bug.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(harbor - at, Vector3.up).normalized + lean * .3f);
        FirstDayStudioAssets.Place(item.model, bug.transform, at, height);
        bug.AddComponent<SphereCollider>().isTrigger = true;
        var critter = bug.AddComponent<BugCritter>();
        critter.Configure(item, .7f);
        critter.ConfigureSpecies(true, wary, 1.3f);
        var hours = holder.AddComponent<CritterHours>();
        hours.Critter = critter; hours.From = from; hours.Until = until;
        return critter;
    }

    // 2026-11-09 왕귀뚜라미(연구 B07 "가까운 풀밭 그늘, 16–24 + 06–08"): 하늘소 다음 나무 그늘 밑동, 해 질 녘~자정·이른 아침.
    // 소리만 단서로 두지 않는다 — 울 때 옆 풀포기가 떨리고 날개가 들썩인다(CricketSong).
    public static bool SpawnCricket(Transform parent, Vector3 harbor, IEnumerable<WorldResourceSpawnRecord> spawns, WorldGridService grid)
    {
        var item = Resources.Load<Item>("Items/Item_Bug_Cricket");
        if (item == null || item.model == null || grid == null || spawns == null) return false;
        var spots = LaterTrunkSpots(harbor, spawns, grid);
        if (spots.Count < 3) return false;
        var critter = PlaceOne(parent, "FirstDay_Cricket", CricketName, item, spots[2], harbor, .13f, 2.2f, 16f, 24f, Vector3.left);
        var hours = critter.transform.parent.GetComponent<CritterHours>();
        hours.From2 = 6f; hours.Until2 = 8f;
        critter.UseOwnWings(); // 날개 축은 없다 — 놀라 뛸 때 몸 폭을 접지 않게
        var song = critter.gameObject.AddComponent<CricketSong>();
        song.Wings = critter.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "CricketWings");
        // 옆 풀포기(그늘 풀숲): 울 때 떨리는 시각 단서. 곤충과 따로 두어 잡혀도 남는다.
        var assets = FirstDayStudioAssets.Load();
        if (assets != null && assets.grasses != null && assets.grasses.Length > 0)
        {
            Vector3 at = critter.transform.position + critter.transform.right * .28f;
            var tuft = FirstDayStudioAssets.Place(assets.grasses[0], critter.transform.parent, at, .42f, 30f);
            tuft.name = "CricketGrass";
            foreach (var c in tuft.GetComponentsInChildren<Collider>()) c.enabled = false;
            song.Grass = tuft.transform;
        }
        return true;
    }

    public static bool StagTime(float hour) => hour >= StagFrom && hour < StagUntil;
    public static bool LonghornTime(float hour) => hour >= LonghornFrom && hour < LonghornUntil;

    static IEnumerable<(Vector2Int Coordinate, Vector3 Position)> NearestTrees(Vector3 harbor, IEnumerable<WorldResourceSpawnRecord> spawns, WorldGridService grid) =>
        spawns.Where(s => s.Kind == WorldResourceKind.Timber)
            .Select(s => (spawn: s, ok: grid.CellToWorld(s.Coordinate, out var p), position: p))
            .Where(t => t.ok).OrderBy(t => Vector3.ProjectOnPlane(t.position - harbor, Vector3.up).sqrMagnitude)
            .Select(t => (t.spawn.Coordinate, t.position));

    // 이웃 칸 가운데 걸을 수 있고 길이 아닌 칸(나무 칸 자체는 다시 지어질 수 있어 피한다). 기본 카메라는 남쪽 위에서
    // 내려다봐 나무 북쪽 밑동은 수관에 가린다(beetle-r2) → 가장 남쪽(-Z), 같으면 항구에 가까운 칸. 나무 쪽으로 조금 붙여 '밑동 옆'으로 읽히게.
    static bool BesideTrunk(Vector2Int treeCell, Vector3 treePosition, Vector3 harbor, WorldGridService grid, out Vector3 at)
    {
        Vector3 ground = Vector3.zero; bool found = false; float bestZ = float.MaxValue, bestDistance = float.MaxValue;
        foreach (var step in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            var cell = treeCell + step;
            if (!grid.TryGetCell(cell, out var data) || !data.IsWalkable || data.HasPath || !grid.CellToWorld(cell, out var p)) continue;
            float z = p.z - treePosition.z, d = Vector3.ProjectOnPlane(p - harbor, Vector3.up).sqrMagnitude;
            if (z < bestZ - .1f || Mathf.Abs(z - bestZ) <= .1f && d < bestDistance) { bestZ = z; bestDistance = d; ground = p; found = true; }
        }
        at = Vector3.Lerp(ground, treePosition, .28f); at.y = ground.y + .04f;
        return found;
    }
}

// 정해진 시간에만 보이는 곤충(넓적사슴벌레 14–24, 알락하늘소 8–20). 곤충 오브젝트의 부모에 붙어 켜고 끈다.
public sealed class CritterHours : MonoBehaviour
{
    public BugCritter Critter;
    public float From, Until = 24f, From2, Until2; // 두 번째 시간 창(없으면 0~0)
    float _nextCheck;

    void Update()
    {
        if (Critter == null || Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + .5f;
        float hour = GameClock.Instance != null ? GameClock.Instance.CurrentHour : 12f;
        bool on = hour >= From && hour < Until || hour >= From2 && hour < Until2;
        if (Critter.gameObject.activeSelf != on) Critter.gameObject.SetActive(on);
    }
}

// 왕귀뚜라미 울음: 런타임 합성 '또르르르'(약 4.6kHz 짧은 펄스 넷)를 곤충 자리에서 낸다(방향이 들림). 울 때마다 옆 풀포기가 떨리고 날개가 들썩인다.
// 플레이어가 2.2m 안에 오면 조용해진다(실제 귀뚜라미처럼). 잡히면 멈춤. 음량은 효과음 설정을 따른다.
public sealed class CricketSong : MonoBehaviour
{
    public Transform Wings, Grass;
    public static int Chirps { get; private set; }
    public static float LastGrassAngle { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Chirps = 0; LastGrassAngle = 0f; }

    static AudioClip _chirp;
    AudioSource _source;
    BugCritter _critter;
    Quaternion _wingRest, _grassRest;
    float _nextChirpAt, _chirpAt = -10f;
    const float ChirpSeconds = .3f, QuietDistance = 2.2f;

    static AudioClip ChirpClip()
    {
        if (_chirp != null) return _chirp;
        const int rate = 44100;
        int length = Mathf.CeilToInt(rate * ChirpSeconds);
        var data = new float[length];
        for (int i = 0; i < length; i++)
        {
            float t = i / (float)rate, v = 0f;
            for (int k = 0; k < 4; k++)
            {
                float u = (t - k * .06f) / .034f;
                if (u <= 0f || u >= 1f) continue;
                float env = Mathf.Sin(u * Mathf.PI);
                v += env * (Mathf.Sin(2f * Mathf.PI * 4600f * t) + .25f * Mathf.Sin(2f * Mathf.PI * 9200f * t));
            }
            data[i] = v * .32f;
        }
        _chirp = AudioClip.Create("PA_CricketChirp", length, 1, rate, false);
        _chirp.SetData(data, 0);
        return _chirp;
    }

    void Start()
    {
        _critter = GetComponent<BugCritter>();
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false; _source.loop = false;
        _source.spatialBlend = .7f; _source.rolloffMode = AudioRolloffMode.Linear;
        _source.minDistance = 12f; _source.maxDistance = 38f; // 듣는 귀는 16m 뒤 카메라 — 방향은 들리고 너무 멀면 사라지게
        if (Wings != null) _wingRest = Wings.localRotation;
        if (Grass != null) _grassRest = Grass.localRotation;
        _nextChirpAt = Time.time + Random.Range(.4f, 1.2f);
    }

    void Update()
    {
        if (_critter == null || _critter.Captured || _critter.Fleeing) { Settle(); return; }
        if (Time.time >= _nextChirpAt)
        {
            _nextChirpAt = Time.time + Random.Range(.9f, 1.5f);
            var player = Inventory.instance != null ? Inventory.instance.transform : null;
            bool near = player != null && Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up).sqrMagnitude < QuietDistance * QuietDistance;
            if (!near)
            {
                float sfx = AudioManager.Instance != null ? AudioManager.Instance.sfxVolume : 1f;
                _source.PlayOneShot(ChirpClip(), .55f * sfx);
                _chirpAt = Time.time; Chirps++;
            }
        }
        float k = (Time.time - _chirpAt) / ChirpSeconds;
        if (k >= 0f && k <= 1f)
        {
            float shake = Mathf.Sin(Time.time * 70f) * (1f - k);
            if (Wings != null) Wings.localRotation = _wingRest * Quaternion.Euler(14f + 6f * shake, 0f, 0f);
            if (Grass != null) { Grass.localRotation = _grassRest * Quaternion.Euler(0f, 0f, 9f * shake); LastGrassAngle = Mathf.Max(LastGrassAngle, Mathf.Abs(9f * shake)); }
        }
        else Settle();
    }

    void Settle()
    {
        if (Wings != null) Wings.localRotation = _wingRest;
        if (Grass != null) Grass.localRotation = _grassRest;
    }
}

// 알락하늘소 더듬이: 뿌리에서 천천히 위아래·좌우로 흔든다(연구 B09 관찰 포인트 "긴 더듬이의 움직임"). 잡히면 멈춘다.
public sealed class AntennaSway : MonoBehaviour
{
    public Transform[] Antennae;
    Quaternion[] _rest;
    BugCritter _critter;
    public static float LastSwing { get; private set; }

    void Start()
    {
        _critter = GetComponent<BugCritter>();
        _rest = Antennae != null ? Antennae.Select(a => a.localRotation).ToArray() : new Quaternion[0];
    }

    void Update()
    {
        if (Antennae == null || _rest == null || _critter == null || _critter.Captured) return;
        float t = Time.time;
        for (int i = 0; i < Antennae.Length && i < _rest.Length; i++)
        {
            if (Antennae[i] == null) continue;
            float k = i == 0 ? 0f : 1.7f;
            float pitch = Mathf.Sin(t * 1.3f + k) * 9f, yaw = Mathf.Sin(t * .8f + k * 1.3f) * 7f;
            Antennae[i].localRotation = _rest[i] * Quaternion.Euler(pitch, yaw, 0f);
            LastSwing = Mathf.Abs(pitch) + Mathf.Abs(yaw);
        }
    }
}
