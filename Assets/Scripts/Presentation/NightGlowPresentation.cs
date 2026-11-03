using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// 전시 키아트 "밤에는 잡화점" — 플레이어가 세운 P.A. SHOP(DemoPlaceableKind.ShopBase)과 주민 텐트에
// 밤 조명 장식(따뜻한 창 발광, 문 앞 조명, 저폴리 랜턴, 바닥 빛 웅덩이)을 런타임으로 붙인다.
// 표현 전용: 콜라이더·NavMesh·배치·저장·상점 권위를 건드리지 않는다. 새 배치와 Continue 복원 모두
// DemoPlacedObject.Bind 이후 0.5초 안에 같은 경로로 장식된다. 실제 Light는 상점 5개 + 텐트당 1개(총 7개 이하).
public sealed class NightGlowPresentation : MonoBehaviour
{
    public static NightGlowPresentation Instance { get; private set; }
    public static bool Enabled = true;
    public const float FadeInStartHour = 18.3f, FadeInEndHour = 19.3f, FadeOutStartHour = 5.2f, FadeOutEndHour = 6.0f;
    public const string RigName = "PA_NightGlow";

    // 0 = 낮(장식만 보임), 1 = 완전히 켜진 밤 조명. 캡처/검증 도구가 읽을 수 있다.
    public float GlowWeight { get; private set; }
    public int RigCount => _rigs.Count;
    public int LightCount
    {
        get { int n = 0; foreach (var rig in _rigs.Values) n += rig.lights.Count; return n; }
    }

    public static float GlowWeightAt(float hour)
    {
        float h = Mathf.Repeat(hour, 24f);
        return h >= 12f ? Smooth(FadeInStartHour, FadeInEndHour, h) : 1f - Smooth(FadeOutStartHour, FadeOutEndHour, h);
    }

    // 캡처 도구가 배치 직후 바로 장식을 원할 때 0.5초 주기를 기다리지 않게 한다.
    public void RescanNow() => _nextScan = 0f;

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
        var host = new GameObject("PA_NightGlowPresentation");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<NightGlowPresentation>();
    }

    // 선형 색공간 목표값(HDR). 톤매핑 없이도 핵심이 노랗게 하얘지지 않도록 G를 1 아래로 둔다.
    static readonly Color PaneOff = new Color(0.045f, 0.085f, 0.095f, 1f);
    static readonly Color PaneOn = new Color(1.35f, 0.80f, 0.30f, 1f);
    static readonly Color LanternDusk = new Color(0.30f, 0.26f, 0.17f, 1f);
    static readonly Color LanternOn = new Color(1.65f, 1.00f, 0.38f, 1f);
    static readonly Color PoolColor = new Color(2.0f, 1.15f, 0.42f, 1f);
    const float PoolAlpha = 0.17f;
    static readonly Color DoorLightColor = new Color(1.00f, 0.72f, 0.42f);
    static readonly Color LanternLightColor = new Color(1.00f, 0.76f, 0.46f);

    sealed class Rig
    {
        public GameObject root;
        public DemoPlaceableKind kind;
        public GameObject exterior;                // 상점 외관: 실내 진입 시 숨겨지므로 창 발광·문 조명도 함께 숨는다
        public Transform yard, facade;
        public Vector3 position;
        public Quaternion rotation;
        public bool glassGlowing;
        public readonly List<Renderer> panes = new List<Renderer>();
        public readonly List<Renderer> facadePools = new List<Renderer>();
        public readonly List<Renderer> pools = new List<Renderer>();
        public readonly List<Renderer> lanternGlass = new List<Renderer>();
        public readonly List<Light> lights = new List<Light>();
        public readonly List<float> lightBase = new List<float>();
        public readonly List<float> lightSeed = new List<float>();
        public readonly List<bool> lightOutside = new List<bool>(); // 마당 랜턴: 실내 진입 시 벽 너머 새는 빛을 줄인다

        public void Destroy()
        {
            if (yard != null) Object.Destroy(yard.gameObject);
            if (facade != null) Object.Destroy(facade.gameObject);
        }
    }

    sealed class Shape
    {
        public bool valid, hasDoor, hasDoorGlass;
        public Bounds all, door, doorGlass;
        public readonly List<Bounds> windows = new List<Bounds>();
    }

    readonly Dictionary<DemoPlacedObject, Rig> _rigs = new Dictionary<DemoPlacedObject, Rig>();
    readonly List<DemoPlacedObject> _stale = new List<DemoPlacedObject>();
    Material _paneMat, _glowMat, _poolMat, _glassMat, _ironMat, _woodMat;
    Mesh _box, _quad;
    Texture2D _poolTex;
    WorldChunkTerrain _terrain;
    float _nextScan, _appliedWeight = -1f;
    bool _resourcesReady, _warned;

    void Awake()
    {
        if (Instance != null && Instance != this && Instance.gameObject.scene == gameObject.scene) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        foreach (var rig in _rigs.Values) rig.Destroy();
        _rigs.Clear();
        DestroyAsset(_paneMat); DestroyAsset(_glowMat); DestroyAsset(_poolMat);
        DestroyAsset(_glassMat); DestroyAsset(_ironMat); DestroyAsset(_woodMat);
        DestroyAsset(_box); DestroyAsset(_quad); DestroyAsset(_poolTex);
        if (Instance == this) Instance = null;
    }

    static void DestroyAsset(Object asset) { if (asset != null) Destroy(asset); }

    void Update()
    {
        if (Time.unscaledTime >= _nextScan)
        {
            _nextScan = Time.unscaledTime + 0.5f;
            Scan();
        }
        float hour = GameClock.Instance != null ? GameClock.Instance.CurrentHour : 12f;
        float w = Enabled ? GlowWeightAt(hour) : 0f;
        GlowWeight = w;
        if (!_resourcesReady) return;
        if (Mathf.Abs(w - _appliedWeight) > 0.002f)
        {
            _appliedWeight = w;
            SetLinear(_paneMat, Color.Lerp(PaneOff, PaneOn, w));
            SetLinear(_glowMat, Color.Lerp(LanternDusk, LanternOn, w));
            var pool = PoolColor; pool.a = PoolAlpha * w;
            SetLinear(_poolMat, pool);
        }
        bool on = w > 0.01f;
        foreach (var rig in _rigs.Values)
        {
            if (rig.root == null) continue;
            bool facadeShown = rig.exterior == null || rig.exterior.activeInHierarchy;
            SetEnabled(rig.panes, on);
            SetEnabled(rig.pools, on);
            SetEnabled(rig.facadePools, on && facadeShown);
            if (rig.glassGlowing != on)
            {
                rig.glassGlowing = on;
                foreach (var glass in rig.lanternGlass)
                    if (glass != null) glass.sharedMaterial = on ? _glowMat : _glassMat;
            }
            for (int i = 0; i < rig.lights.Count; i++)
            {
                var light = rig.lights[i];
                if (light == null) continue;
                light.enabled = on;
                if (!on) continue;
                float flicker = rig.lightSeed[i] < 0f ? 1f : 0.95f + 0.07f * Mathf.PerlinNoise(rig.lightSeed[i], Time.time * 1.3f);
                float inside = rig.lightOutside[i] && !facadeShown ? 0.35f : 1f;
                light.intensity = rig.lightBase[i] * w * flicker * inside;
            }
        }
    }

    static void SetEnabled(List<Renderer> renderers, bool enabled)
    {
        foreach (var r in renderers) if (r != null && r.enabled != enabled) r.enabled = enabled;
    }

    static void SetLinear(Material material, Color linear)
    {
        // Material 색은 sRGB로 저장되므로 선형 목표값을 감마로 넘긴다(HDR 1 초과도 왕복 보존).
        Color c = QualitySettings.activeColorSpace == ColorSpace.Linear ? linear.gamma : linear;
        c.a = linear.a;
        material.color = c;
    }

    void Scan()
    {
        // 사라진 건물 정리, 이동/회전한 건물은 다음 주기에 새 자리에서 다시 꾸민다.
        _stale.Clear();
        foreach (var pair in _rigs)
        {
            var rig = pair.Value;
            if (pair.Key == null || rig.root == null) { rig.Destroy(); _stale.Add(pair.Key); continue; }
            Transform t = rig.root.transform;
            if (rig.root.activeInHierarchy &&
                ((t.position - rig.position).sqrMagnitude > 0.0025f || Quaternion.Angle(t.rotation, rig.rotation) > 0.5f))
            { rig.Destroy(); _stale.Add(pair.Key); }
        }
        foreach (var key in _stale) _rigs.Remove(key);
        if (!Enabled || !EnsureResources()) return;

        foreach (var placed in FindObjectsByType<DemoPlacedObject>(FindObjectsSortMode.None))
        {
            if (placed == null || placed.Entry == null || _rigs.ContainsKey(placed)) continue;
            var kind = placed.Entry.kind;
            if (kind != DemoPlaceableKind.ShopBase && kind != DemoPlaceableKind.ResidentTent) continue;
            _rigs.Add(placed, Build(placed, kind));
        }
    }

    Rig Build(DemoPlacedObject placed, DemoPlaceableKind kind)
    {
        Transform root = placed.transform;
        var interior = placed.GetComponent<DemoShopInterior>();
        var rig = new Rig
        {
            root = placed.gameObject, kind = kind,
            exterior = interior != null ? interior.exterior : null,
            position = root.position, rotation = root.rotation,
            glassGlowing = false
        };
        _appliedWeight = -1f; // 새 장식이 현재 밝기를 바로 받는다

        // rig 공간: 건물 루트 기준, 로컬 -z가 건물 정면(입구 방향)이 되게 돌린다.
        Vector3 front = ResolveFrontLocal(placed, interior, out bool hasEntrance, out Vector3 entranceLocal);
        var yard = new GameObject(RigName + "_Yard").transform;
        yard.SetParent(root, false);
        yard.localRotation = Quaternion.LookRotation(-front, Vector3.up);
        rig.yard = yard;
        if (hasEntrance) entranceLocal = yard.InverseTransformPoint(root.TransformPoint(entranceLocal));

        Rect foot = FootprintRect(placed, yard);
        Transform visualRoot = rig.exterior != null ? rig.exterior.transform : root;
        Shape shape = Analyse(visualRoot, yard);
        if (!shape.valid) return rig;

        if (kind == DemoPlaceableKind.ShopBase) BuildShop(rig, shape, foot, hasEntrance, entranceLocal);
        else BuildTent(rig, shape, foot);
        return rig;
    }

    void BuildShop(Rig rig, Shape s, Rect foot, bool hasEntrance, Vector3 entrance)
    {
        Transform facade = rig.yard;
        if (rig.exterior != null)
        {
            facade = new GameObject(RigName + "_Facade").transform;
            facade.SetParent(rig.exterior.transform, false);
            facade.SetPositionAndRotation(rig.yard.position, rig.yard.rotation);
            rig.facade = facade;
        }

        float frontZ = s.all.min.z;
        if (s.windows.Count > 0) { frontZ = float.MaxValue; foreach (var w in s.windows) frontZ = Mathf.Min(frontZ, w.min.z); }
        else if (s.hasDoor) frontZ = s.door.min.z;

        // 1) 창 발광: 실제 유리 상자의 바깥면 바로 앞(4mm)에 붙여 떠 보이지 않게 한다. 창살·창턱은 그대로 앞에 남는다.
        if (s.windows.Count > 0)
            foreach (var w in s.windows) rig.panes.Add(Pane(facade, w.center.x, w.center.y, w.min.z, w.size.x * 0.92f, w.size.y * 0.88f));
        else
            foreach (int side in new[] { -1, 1 })
                rig.panes.Add(Pane(facade, s.all.center.x + side * s.all.size.x * 0.27f, s.all.min.y + s.all.size.y * 0.36f,
                    frontZ - 0.006f, s.all.size.x * 0.2f, s.all.size.y * 0.26f));
        if (s.hasDoorGlass)
            rig.panes.Add(Pane(facade, s.doorGlass.center.x, s.doorGlass.center.y, s.doorGlass.min.z,
                s.doorGlass.size.x * 0.9f, s.doorGlass.size.y * 0.88f));

        // 2) 문·간판 조명: 차양 바로 앞 아래에서 문과 간판, 창 두 개를 함께 비춘다.
        float doorX = s.hasDoor ? s.door.center.x : s.hasDoorGlass ? s.doorGlass.center.x : s.all.center.x;
        float doorTop = s.hasDoor ? s.door.max.y : Mathf.Min(2.1f, s.all.max.y * 0.5f);
        AddLight(rig, facade, new Vector3(doorX, doorTop + 0.4f, frontZ - 1.4f), DoorLightColor, 2.6f, 7.5f, false, false);

        // 3) 지면 셰이더는 점광원을 받지 않으므로 문·창 앞 바닥 빛은 부드러운 빛 웅덩이로 그린다(외관이 보일 때만).
        AddPool(rig, rig.facadePools, new Vector3(doorX, 0f, frontZ - 1.5f), new Vector2(4.6f, 3.4f));
        foreach (var w in s.windows)
            AddPool(rig, rig.facadePools, new Vector3(w.center.x, 0f, frontZ - 1.0f), new Vector2(w.size.x * 1.6f, 2.2f));

        // 4) 정면 모서리 랜턴 2개: 상점 자기 발자국 칸 안(옆벽 바깥)이라 다른 배치와 겹치지 않는다.
        float cx = s.all.center.x, half = s.all.extents.x;
        foreach (int side in new[] { -1, 1 })
        {
            Vector2 p = ClampInto(foot, new Vector2(cx + side * (half + 0.45f), frontZ + 0.2f), 0.3f);
            AddLantern(rig, new Vector3(p.x, 0f, p.y), 1f, 1.5f, 5f, true);
        }

        // 5) 문 양옆 작은 랜턴 2개: 문 앞 1칸은 다른 배치 금지 구역(Door 앞 No-Placement)이고, 상점 부지 정리 범위
        //    (DemoShopInterior.CoversGround) 안이라 식생과 겹치지 않는다. 문틀 바깥이라 손님 통로를 가리지 않는다.
        float pathX = !s.hasDoor && hasEntrance ? entrance.x : doorX;
        float pathZ = frontZ - 0.5f;
        foreach (int side in new[] { -1, 1 })
            AddLantern(rig, new Vector3(pathX + side * 1.35f, 0f, pathZ), 0.62f, 0.9f, 3.2f, false);
    }

    void BuildTent(Rig rig, Shape s, Rect foot)
    {
        // 텐트 입구 왼쪽 앞의 작은 랜턴 하나(입구 플랩·직업 표식과 겹치지 않게 중앙에서 비켜 둔다).
        float frontZ = s.all.min.z;
        Vector2 p = ClampInto(foot, new Vector2(s.all.center.x - Mathf.Min(0.95f, s.all.extents.x * 0.6f), frontZ - 0.3f), 0.15f);
        AddLantern(rig, new Vector3(p.x, 0f, p.y), 0.78f, 1.1f, 4.2f, true);
    }

    Renderer Pane(Transform parent, float x, float y, float faceZ, float width, float height)
    {
        var go = Part(parent, "WindowGlow", _quad, _paneMat, new Vector3(x, y, faceZ - 0.004f), new Vector3(width, height, 1f));
        var r = go.GetComponent<Renderer>();
        r.enabled = false;
        return r;
    }

    void AddLantern(Rig rig, Vector3 basePos, float scale, float intensity, float range, bool withPool)
    {
        var lantern = new GameObject("Lantern").transform;
        lantern.SetParent(rig.yard, false);
        float y = GroundY(rig.yard, basePos, out bool hit);
        if (!hit || Mathf.Abs(y) > 1.2f) y = 0f;
        lantern.localPosition = new Vector3(basePos.x, y, basePos.z);
        lantern.localRotation = Quaternion.identity;
        lantern.localScale = Vector3.one * scale;

        const float post = 1.45f, glowY = post + 0.21f;
        Part(lantern, "Base", _box, _woodMat, new Vector3(0f, 0.06f, 0f), new Vector3(0.30f, 0.12f, 0.30f));
        Part(lantern, "Post", _box, _woodMat, new Vector3(0f, post * 0.5f, 0f), new Vector3(0.11f, post, 0.11f));
        Part(lantern, "Plate", _box, _ironMat, new Vector3(0f, post + 0.03f, 0f), new Vector3(0.34f, 0.05f, 0.34f));
        var glass = Part(lantern, "Glow", _box, _glassMat, new Vector3(0f, glowY, 0f), new Vector3(0.24f, 0.30f, 0.24f));
        var glassRenderer = glass.GetComponent<Renderer>();
        glassRenderer.shadowCastingMode = ShadowCastingMode.Off;
        rig.lanternGlass.Add(glassRenderer);
        foreach (int x in new[] { -1, 1 })
            foreach (int z in new[] { -1, 1 })
                Part(lantern, "Bar", _box, _ironMat, new Vector3(x * 0.125f, glowY, z * 0.125f), new Vector3(0.04f, 0.32f, 0.04f));
        Part(lantern, "Cap", _box, _ironMat, new Vector3(0f, post + 0.40f, 0f), new Vector3(0.40f, 0.07f, 0.40f));
        Part(lantern, "Knob", _box, _ironMat, new Vector3(0f, post + 0.47f, 0f), new Vector3(0.12f, 0.08f, 0.12f));

        AddLight(rig, lantern, new Vector3(0f, glowY, 0f), LanternLightColor, intensity, range, true, true);
        if (withPool) AddPool(rig, rig.pools, basePos + new Vector3(0f, 0f, -0.5f), new Vector2(3.4f, 3.4f) * scale);
    }

    void AddLight(Rig rig, Transform parent, Vector3 localPos, Color color, float intensity, float range, bool flicker, bool outside)
    {
        var go = new GameObject("WarmLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = 0f;
        light.shadows = LightShadows.None;
        light.enabled = false;
        rig.lights.Add(light);
        rig.lightBase.Add(intensity);
        rig.lightSeed.Add(flicker ? Random.Range(0f, 100f) : -1f);
        rig.lightOutside.Add(outside);
    }

    void AddPool(Rig rig, List<Renderer> list, Vector3 local, Vector2 size)
    {
        float y = GroundY(rig.yard, local, out bool hit);
        if (hit && Mathf.Abs(y) > 0.6f) return; // 계단식 지형 아래/위에 떠 보이는 웅덩이는 만들지 않는다
        if (!hit) y = 0f;
        var go = Part(rig.yard, "GroundGlow", _quad, _poolMat, new Vector3(local.x, y + 0.045f, local.z), new Vector3(size.x, size.y, 1f));
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var r = go.GetComponent<Renderer>();
        r.enabled = false;
        list.Add(r);
    }

    GameObject Part(Transform parent, string partName, Mesh mesh, Material material, Vector3 localPos, Vector3 localScale)
    {
        // 렌더러 전용 조각: 콜라이더가 없어 이동·배치·상호작용·카메라 가림 판정에 영향이 없다.
        var go = new GameObject(partName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        bool unlit = material == _paneMat || material == _poolMat || material == _glowMat;
        r.shadowCastingMode = unlit ? ShadowCastingMode.Off : ShadowCastingMode.On;
        r.receiveShadows = !unlit;
        return go;
    }

    float GroundY(Transform space, Vector3 local, out bool hit)
    {
        hit = false;
        if (_terrain == null) _terrain = FindFirstObjectByType<WorldChunkTerrain>();
        MeshCollider terrain = _terrain != null ? _terrain.TerrainCollider : null;
        if (terrain == null) return 0f;
        Vector3 world = space.TransformPoint(new Vector3(local.x, 0f, local.z));
        if (!terrain.Raycast(new Ray(world + Vector3.up * 6f, Vector3.down), out RaycastHit info, 14f)) return 0f;
        hit = true;
        return space.InverseTransformPoint(info.point).y;
    }

    static Vector3 ResolveFrontLocal(DemoPlacedObject placed, DemoShopInterior interior, out bool hasEntrance, out Vector3 entranceLocal)
    {
        // 1) 배치 서비스의 실제 입구 칸, 2) 상점 입구 오브젝트, 3) 기본 -z(프리팹 정면).
        Transform root = placed.transform;
        hasEntrance = false;
        entranceLocal = Vector3.zero;
        var service = placed.Placement;
        if (service != null && service.TryGetPlacement(placed.InstanceId, out var runtime))
        {
            var grid = service.GetComponent<WorldGridService>();
            if (grid != null && grid.CellToWorld(runtime.Entrance, out Vector3 world))
            { entranceLocal = root.InverseTransformPoint(world); hasEntrance = true; }
        }
        Vector3 probe = entranceLocal;
        if (!hasEntrance && interior != null && interior.entrance != null)
            probe = root.InverseTransformPoint(interior.entrance.transform.position);
        probe.y = 0f;
        if (probe.sqrMagnitude < 0.01f) return Vector3.back;
        return Mathf.Abs(probe.z) >= Mathf.Abs(probe.x)
            ? new Vector3(0f, 0f, Mathf.Sign(probe.z))
            : new Vector3(Mathf.Sign(probe.x), 0f, 0f);
    }

    static Rect FootprintRect(DemoPlacedObject placed, Transform space)
    {
        var service = placed.Placement;
        if (service == null || !service.TryGetPlacement(placed.InstanceId, out var runtime) ||
            runtime.Footprint == null || runtime.Footprint.Count == 0) return Rect.zero;
        var grid = service.GetComponent<WorldGridService>();
        if (grid == null) return Rect.zero;
        float half = grid.Definition.CellSize * 0.5f;
        bool any = false;
        float x0 = 0f, x1 = 0f, z0 = 0f, z1 = 0f;
        foreach (var cell in runtime.Footprint)
        {
            if (!grid.CellToWorld(cell, out Vector3 world)) continue;
            Vector3 l = space.InverseTransformPoint(world);
            if (!any) { x0 = x1 = l.x; z0 = z1 = l.z; any = true; continue; }
            x0 = Mathf.Min(x0, l.x); x1 = Mathf.Max(x1, l.x);
            z0 = Mathf.Min(z0, l.z); z1 = Mathf.Max(z1, l.z);
        }
        return any ? Rect.MinMaxRect(x0 - half, z0 - half, x1 + half, z1 + half) : Rect.zero;
    }

    static Vector2 ClampInto(Rect rect, Vector2 p, float inset)
    {
        if (rect.width <= 0f || rect.height <= 0f) return p;
        float x = rect.width > inset * 2f ? Mathf.Clamp(p.x, rect.xMin + inset, rect.xMax - inset) : rect.center.x;
        float y = rect.height > inset * 2f ? Mathf.Clamp(p.y, rect.yMin + inset, rect.yMax - inset) : rect.center.y;
        return new Vector2(x, y);
    }

    // 보이는(외관 기준 활성) 메시 경계를 rig 공간으로 모은다. 비활성 옛 모델·TMP 글자·기존 장식은 제외한다.
    static Shape Analyse(Transform visualRoot, Transform space)
    {
        var shape = new Shape();
        foreach (var filter in visualRoot.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter == null || filter.sharedMesh == null || !ActiveUnder(filter.transform, visualRoot)) continue;
            if (filter.GetComponent<TMP_Text>() != null || UnderRig(filter.transform, visualRoot)) continue;
            Bounds b = MeshBounds(filter, space);
            if (b.size.magnitude > 40f) continue;
            if (!shape.valid) { shape.all = b; shape.valid = true; }
            else shape.all.Encapsulate(b);
            string n = filter.name;
            if (n == "DoorGlass") { shape.doorGlass = b; shape.hasDoorGlass = true; }
            else if (n == "Door") { shape.door = b; shape.hasDoor = true; }
            else if (n.StartsWith("Window") && !n.Contains("Frame") && !n.Contains("Bar") && !n.Contains("Sill"))
                shape.windows.Add(b);
        }
        // 정면(-z) 쪽 창만 쓴다. 뒷면·옆면 창은 카메라에 보이지 않는다.
        if (shape.valid)
            shape.windows.RemoveAll(w => w.min.z > shape.all.min.z + 1.5f);
        return shape;
    }

    static bool ActiveUnder(Transform t, Transform top)
    {
        for (var c = t; c != null; c = c.parent)
        {
            if (c == top) return true;
            if (!c.gameObject.activeSelf) return false;
        }
        return true;
    }

    static bool UnderRig(Transform t, Transform top)
    {
        for (var c = t; c != null && c != top; c = c.parent)
            if (c.name.StartsWith(RigName)) return true;
        return false;
    }

    static Bounds MeshBounds(MeshFilter filter, Transform space)
    {
        Bounds m = filter.sharedMesh.bounds;
        Transform t = filter.transform;
        Vector3 c = m.center, e = m.extents;
        var b = new Bounds(space.InverseTransformPoint(t.TransformPoint(c - e)), Vector3.zero);
        for (int i = 1; i < 8; i++)
        {
            var corner = new Vector3((i & 1) != 0 ? e.x : -e.x, (i & 2) != 0 ? e.y : -e.y, (i & 4) != 0 ? e.z : -e.z);
            b.Encapsulate(space.InverseTransformPoint(t.TransformPoint(c + corner)));
        }
        return b;
    }

    bool EnsureResources()
    {
        if (_resourcesReady) return true;
        // Sprites/Default는 빌드에 항상 포함된다(GraphicsSettings Always Included). Cull Off·HDR 색을 그대로 낸다.
        Shader unlit = Shader.Find("Sprites/Default");
        Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (unlit == null || lit == null)
        {
            if (!_warned) { _warned = true; Debug.LogWarning("[NightGlow] Sprites/Default or URP Lit is missing from this build; night glow disabled."); }
            return false;
        }
        _box = BuildBox();
        _quad = BuildQuad();
        _poolTex = BuildPoolTexture();
        _paneMat = new Material(unlit) { name = "PA_NightGlow_Window (runtime)", hideFlags = HideFlags.DontSave };
        _glowMat = new Material(unlit) { name = "PA_NightGlow_Lantern (runtime)", hideFlags = HideFlags.DontSave };
        _poolMat = new Material(unlit) { name = "PA_NightGlow_Pool (runtime)", hideFlags = HideFlags.DontSave, mainTexture = _poolTex };
        _glassMat = LitMaterial(lit, "PA_NightGlow_LanternGlass", new Color(0.93f, 0.88f, 0.72f), 0.55f);
        _ironMat = LitMaterial(lit, "PA_NightGlow_Iron", new Color(0.20f, 0.16f, 0.13f), 0.25f);
        _woodMat = LitMaterial(lit, "PA_NightGlow_Wood", new Color(0.50f, 0.34f, 0.20f), 0.1f);
        _appliedWeight = -1f;
        _resourcesReady = true;
        return true;
    }

    static Material LitMaterial(Shader shader, string materialName, Color color, float smoothness)
    {
        var m = new Material(shader) { name = materialName + " (runtime)", hideFlags = HideFlags.DontSave, color = color };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        return m;
    }

    // 면별 법선을 가진 단위 상자(흰 정점색: Sprites/Default 발광과 URP Lit 둘 다 쓴다).
    static Mesh BuildBox()
    {
        var vertices = new List<Vector3>(24);
        var normals = new List<Vector3>(24);
        var uvs = new List<Vector2>(24);
        var colors = new List<Color>(24);
        var triangles = new List<int>(36);
        Vector3[] faces = { Vector3.forward, Vector3.back, Vector3.up, Vector3.down, Vector3.right, Vector3.left };
        foreach (var n in faces)
        {
            Vector3 forward = -n;
            Vector3 up = Mathf.Abs(n.y) > 0.5f ? Vector3.forward : Vector3.up;
            Vector3 right = Vector3.Cross(up, forward);
            Vector3 c = n * 0.5f, u = up * 0.5f, r = right * 0.5f;
            int start = vertices.Count;
            vertices.Add(c - r - u); vertices.Add(c - r + u); vertices.Add(c + r + u); vertices.Add(c + r - u);
            uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(0f, 1f)); uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(1f, 0f));
            for (int i = 0; i < 4; i++) { normals.Add(n); colors.Add(Color.white); }
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
        }
        var mesh = new Mesh { name = "PA_NightGlow_Box", hideFlags = HideFlags.DontSave };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // XY 평면, -z를 향한 단위 사각형(창 발광·바닥 빛). Sprites/Default가 Cull Off라 뒤집혀도 보인다.
    static Mesh BuildQuad()
    {
        var mesh = new Mesh { name = "PA_NightGlow_Quad", hideFlags = HideFlags.DontSave };
        mesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) };
        mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
        mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
        mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        return mesh;
    }

    // 가장자리에서 완전히 사라지는 둥근 빛 웅덩이 알파(흰색 RGB).
    static Texture2D BuildPoolTexture()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "PA_NightGlow_Pool", hideFlags = HideFlags.DontSave,
            wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
        };
        var pixels = new Color32[size * size];
        float centre = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - centre) / centre, dy = (y - centre) / centre;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Pow(Mathf.Clamp01(1f - r), 1.6f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }

    static float Smooth(float a, float b, float x)
    {
        float k = Mathf.Clamp01((x - a) / (b - a));
        return k * k * (3f - 2f * k);
    }
}
