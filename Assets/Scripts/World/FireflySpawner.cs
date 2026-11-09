using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 애반딧불이(아이템 생태 연구 B12 "습한 물가 풀의 마른 쪽, 17–24, 꺼졌다 켜지는 작은 빛, 정지 지점을 보고 접근", G4 "정지와 짧은 직선 비행").
// 생성 섬의 강(FishingSpot.RiverCells) 가장자리 마른 땅 .9m 위에 세 마리. 한 자리에 3~5초 멈춰 꽁무니 빛을 깜빡이다가
// 가까운 강가 자리로 짧게 곧장 날아간다. 나는 동안은 잡을 수 없다(BugCritter.Fleeing → "내려앉을 때까지 기다려요").
// 빛: 꽁무니 마디(FireflyLantern·FireflyLanternTip) 색과 작은 점광원이 함께 깜빡인다 — 멈췄을 때는 또렷하게 켜졌다 꺼지고, 날 때는 희미하게.
// 잡기·보상·하루 반복·저장은 기존 BugCritter 그대로(떠다님, 1.6m 안에서 달려오면 날아감, 그물 1.3m).
public sealed class FireflySpawner : MonoBehaviour
{
    public const string NamePrefix = "FirstDay_Insect_Firefly_";
    const float From = 17f, Until = 24f, PauseMin = 3f, PauseMax = 5f, Speed = 1.6f, Height = .9f;
    const float MouthMin = 6f, MouthMax = 70f, Apart = 4f;
    static readonly Color Lit = new Color(.95f, 1f, .45f), Dim = new Color(.32f, .34f, .2f);

    sealed class Fly
    {
        public BugCritter Critter;
        public Vector3[] Route;
        public int At;
        public float LeaveAt, Phase, Glow;
        public Renderer[] Lantern;
        public Light Light;
        public Renderer Halo;
    }

    Fly[] _flies;
    float _nextCheck;
    public static int Hops { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Hops = 0;

    public static bool Night(float hour) => hour >= From && hour < Until;
    public int Spawned => _flies != null ? _flies.Length : 0;
    public float GlowOf(int i) => _flies != null && i >= 0 && i < _flies.Length ? _flies[i].Glow : 0f;
    public int RouteLength(int i) => _flies != null && i >= 0 && i < _flies.Length ? _flies[i].Route.Length : 0;
    public Vector3 NextOf(int i) => _flies[i].Route[(_flies[i].At + 1) % _flies[i].Route.Length];

    void Update()
    {
        if (_flies != null) { Wander(); Blink(); }
        if (Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + .5f;
        if (_flies == null) TrySpawn();
        if (_flies == null) return;
        bool on = Night(GameClock.Instance != null ? GameClock.Instance.CurrentHour : 12f);
        foreach (var fly in _flies) if (fly.Critter != null && fly.Critter.gameObject.activeSelf != on) fly.Critter.gameObject.SetActive(on);
    }

    void Wander()
    {
        foreach (var fly in _flies)
        {
            var c = fly.Critter;
            if (c == null || !c.isActiveAndEnabled) continue;
            if (c.Captured) { fly.At = 0; fly.LeaveAt = Time.time + PauseMax; continue; }
            if (c.Fleeing) { fly.LeaveAt = Mathf.Max(fly.LeaveAt, Time.time + PauseMin); continue; }
            if (fly.Route.Length < 2 || Time.time < fly.LeaveAt) continue;
            int next = (fly.At + 1) % fly.Route.Length;
            Vector3 to = fly.Route[next];
            float seconds = Mathf.Clamp(Mathf.Sqrt(Flat(to - c.transform.position)) / Speed, .8f, 2.2f);
            if (!c.FlyTo(to, seconds, .06f, .08f)) continue; // 짧은 직선 비행(연구 G4)
            fly.At = next;
            fly.LeaveAt = Time.time + seconds + Random.Range(PauseMin, PauseMax);
            Hops++;
        }
    }

    // 멈춘 동안: .55초 켜짐 → .75초 꺼짐을 되풀이(또렷한 깜빡임). 나는 동안: 희미하게 가끔.
    void Blink()
    {
        foreach (var fly in _flies)
        {
            if (fly.Critter == null || !fly.Critter.isActiveAndEnabled) continue;
            float t = Time.time + fly.Phase;
            float target = fly.Critter.Fleeing ? (Mathf.Repeat(t, 1.1f) < .15f ? .45f : .08f) : (Mathf.Repeat(t, 1.3f) < .55f ? 1f : .05f);
            fly.Glow = Mathf.MoveTowards(fly.Glow, target, Time.deltaTime * 6f);
            Color color = Color.Lerp(Dim, Lit, fly.Glow);
            foreach (var r in fly.Lantern)
            {
                if (r == null) continue;
                var m = r.material;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
                if (m.HasProperty("_Color")) m.SetColor("_Color", color);
                if (m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Lit * (fly.Glow * 2.2f)); }
            }
            if (fly.Light != null) fly.Light.intensity = fly.Glow * 1.6f;
            if (fly.Halo != null)
            {
                // 꽁무니 둘레 빛 번짐(카메라를 향함): 점광원만으로는 밤 화면에서 빛이 거의 안 보였다(firefly-r222).
                var cam = Camera.main;
                if (cam != null) fly.Halo.transform.rotation = cam.transform.rotation;
                fly.Halo.transform.localScale = Vector3.one * Mathf.Lerp(.25f, .75f, fly.Glow);
                fly.Halo.enabled = fly.Glow > .03f;
                s_block ??= new MaterialPropertyBlock();
                s_block.SetColor(ColorId, new Color(Lit.r, Lit.g, Lit.b, fly.Glow * .95f));
                fly.Halo.SetPropertyBlock(s_block);
            }
        }
    }

    void TrySpawn()
    {
        var item = Resources.Load<Item>("Items/Item_Bug_Firefly");
        var player = Inventory.instance != null ? Inventory.instance.transform : null;
        var grid = FishingSpot.ZoneGrid;
        var river = FishingSpot.RiverCells;
        var mouths = FishingSpot.RiverMouths;
        if (item == null || item.model == null || player == null || grid == null || river.Count == 0) return;
        var banks = BankPoints(grid, river, mouths);
        if (banks.Count == 0) { _flies = new Fly[0]; return; }
        // 서로 Apart(4m) 넘게 떨어진 강가 세 자리(하구에서 가까운 쪽부터 — 모래밭을 지나는 얕은 구간이 잘 보인다).
        var starts = new List<Vector3>();
        foreach (var b in banks)
        {
            if (starts.Any(s => Flat(s - b) < Apart * Apart)) continue;
            starts.Add(b);
            if (starts.Count == 3) break;
        }
        _flies = new Fly[starts.Count];
        for (int i = 0; i < starts.Count; i++)
        {
            var route = new List<Vector3> { starts[i] };
            foreach (var b in banks.OrderBy(b => Flat(b - starts[i])))
            {
                float d = Flat(b - starts[i]);
                if (d < 1.5f * 1.5f || d > 3.5f * 3.5f || route.Any(r => Flat(r - b) < 1.2f * 1.2f)) continue; // firefly-r218: 제곱 거리를 그대로 비교해 경로가 1칸이었다
                route.Add(b);
                if (route.Count == 3) break;
            }
            var go = new GameObject(NamePrefix + i);
            go.transform.SetParent(transform);
            go.transform.position = starts[i];
            var model = FirstDayStudioAssets.Place(item.model, go.transform, starts[i], .075f); // 전시 다듬기: .1은 딱정벌레처럼 커 보였다
            go.transform.rotation = Quaternion.Euler(0f, 30f + i * 121f, 0f);
            go.AddComponent<SphereCollider>().isTrigger = true;
            var lightGo = new GameObject("FireflyGlow");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, .02f, -.05f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point; light.range = 1.8f; light.color = Lit; light.intensity = 0f; light.shadows = LightShadows.None;
            var halo = MakeHalo(lightGo.transform);
            var critter = go.AddComponent<BugCritter>();
            critter.Configure(item, i * 1.3f + .2f);
            critter.ConfigureSpecies(false, 1.6f, 1.3f);
            _flies[i] = new Fly
            {
                Critter = critter, Route = route.ToArray(), At = 0, LeaveAt = Time.time + PauseMin + i * 1.2f, Phase = i * .43f,
                Lantern = model.GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith("FireflyLantern")).ToArray(), Light = light, Halo = halo,
            };
        }
    }

    // 강 칸과 맞닿은 마른 땅 칸 쪽으로 물가에서 .5m 들어간 점(땅 높이 + .9m). 하구에서 6~70m 사이 강만.
    static List<Vector3> BankPoints(WorldGridService grid, IReadOnlyList<Vector2Int> river, IReadOnlyList<Vector3> mouths)
    {
        var points = new List<(Vector3 At, float Mouth)>();
        float half = grid.Definition.CellSize * .5f;
        Vector2Int[] around = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var c in river)
        {
            if (!grid.CellToWorld(c, out var w)) continue;
            float mouth = mouths.Count > 0 ? mouths.Min(m => Mathf.Sqrt(Flat(m - w))) : 30f;
            if (mouth < MouthMin || mouth > MouthMax) continue;
            foreach (var d in around)
            {
                if (!grid.TryGetCell(c + d, out var land) || land.HasWater || land.Occupancy != WorldCellOccupancy.Empty) continue;
                Vector3 p = w + new Vector3(d.x, 0f, d.y) * (half + .5f);
                if (!Ground(p, out float y)) continue;
                points.Add((new Vector3(p.x, y + Height, p.z), mouth));
            }
        }
        return points.OrderBy(p => p.Mouth).Select(p => p.At).ToList();
    }

    // 땅(가장 낮은 단단한 면) 높이. 나무 갓·바위 위를 피하려고 가장 낮은 면을 쓴다.
    static bool Ground(Vector3 p, out float y)
    {
        y = 0f;
        var hits = Physics.RaycastAll(new Vector3(p.x, 80f, p.z), Vector3.down, 160f, ~0, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0) return false;
        y = hits.Min(h => h.point.y);
        return true;
    }

    static readonly int ColorId = Shader.PropertyToID("_Color");
    static MaterialPropertyBlock s_block;
    static Mesh s_quad;
    static Material s_haloMat;

    // 가운데가 밝고 가장자리로 갈수록 투명한 둥근 빛 판(런타임 텍스처, Sprites/Default — 빌드에 항상 있음).
    static Renderer MakeHalo(Transform parent)
    {
        if (s_haloMat == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16f, 16f)) / 16f;
                    float a = Mathf.Clamp01(1f - d); a *= a;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            s_haloMat = new Material(shader) { name = "PA_FireflyHalo (runtime)", mainTexture = tex, hideFlags = HideFlags.DontSave, renderQueue = 3100 };
            s_quad = new Mesh { name = "PA_FireflyHaloQuad", hideFlags = HideFlags.DontSave };
            s_quad.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(-.5f, .5f, 0), new Vector3(.5f, .5f, 0) };
            s_quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            s_quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        }
        var go = new GameObject("FireflyHalo");
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = s_quad;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = s_haloMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        return r;
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude; }
}
