using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// VS-PRESENT-001-P2: 짧은 출항 표현. 선택 세션과 기존 WorldGrid/이동 권위를 연결한다.
[DefaultExecutionOrder(220)]
public sealed class DepartureVoyagePresentation : MonoBehaviour
{
    public GameObject boatPrefab, treePrefab, birchPrefab, rockPrefab, dockPrefab, cratePrefab;
    public Material waterMaterial;
    public float voyageSeconds = 12f;
    public bool Sailing { get; private set; }
    public bool Arrived { get; private set; }
    public bool ArrivalFadeFinished { get; private set; }
    public Transform IslandRoot => _world;

    public void SkipTravelForSavedArrival() { Elapsed = voyageSeconds; SkippedForSavedArrival = true; }
    // 저장 도착 복원으로 항해를 건너뛰었으면 섬 상륙 연출도 다시 틀지 않는다(플레이어의 길게 눌러 건너뛰기는 해당 없음).
    public bool SkippedForSavedArrival { get; private set; }
    public void SetSettlementObjective(string text)
    {
        if (_objective != null) _objective.text = text;
    }
    public float Elapsed { get; private set; }
    public WorldGridService IslandGrid { get; private set; }
    public WorldChunkTerrain IslandTerrain { get; private set; }
    public Transform Boat { get; private set; }
    public IReadOnlyList<GameObject> Companions => _companions.AsReadOnly();
    public IReadOnlyList<string> ArrivedIds => Array.AsReadOnly(_arrivedIds);
    public Vector3 ArrivalPosition => new Vector3(114f, .55f, 106f);

    DepartureCompanionSelection _selection;
    Transform _player, _world, _cameraFraming;
    CharacterController _controller;
    CameraController _cameraFollow;
    readonly List<GameObject> _companions = new List<GameObject>(2);
    string[] _arrivedIds = Array.Empty<string>();
    bool _started;
    Image _fade;
    Camera _screenBridgeCamera;
    TextMeshProUGUI _objective, _subtitle;
    Vector3 _boatStart = new Vector3(114f, 0f, 97f);
    Vector3 _boatEnd = new Vector3(114f, 0f, 101f);

    // 전시용 항해 컷 타이밍(캡처 도구가 컷별 프레임을 고를 수 있게 공개한다). Elapsed는 이 길이까지 오른 뒤 페이드한다.
    public const float CinematicSeconds = 13f, DeckShotAt = 4.4f, RevealShotAt = 8.8f;
    const float VoyageTravel = 32f, IslandZ = 80f;
    static readonly Color MorningHorizon = new Color(1f, .84f, .67f);
    static readonly Quaternion MorningSun = Quaternion.Euler(15f, 250f, 0f);
    readonly List<UnityEngine.Object> _stageAssets = new List<UnityEngine.Object>();
    readonly Dictionary<Material, Material> _canopy = new Dictionary<Material, Material>();
    VoyageCinematicOverlay _cinema;
    Transform _sky, _flag, _seaRoot;
    Transform[] _gulls = Array.Empty<Transform>(), _gullWings = Array.Empty<Transform>();
    Transform[] _crests = Array.Empty<Transform>(), _wake = Array.Empty<Transform>();
    Vector4[] _gullOrbit = Array.Empty<Vector4>();
    Vector3[] _crestScale = Array.Empty<Vector3>(), _wakeDrift = Array.Empty<Vector3>();
    float[] _wakeAge = Array.Empty<float>();
    float _wakeTimer;
    int _wakeNext, _cue = -1;
    bool _titleShown, _atmosphereSaved, _sunCreated;
    Light _sun;
    Quaternion _sunRotation;
    Color _sunColor, _fogColor, _ambientLight, _ambientSky, _ambientEquator, _ambientGround;
    float _sunIntensity, _fogStart, _fogEnd;
    bool _fogEnabled;
    FogMode _fogMode;
    UnityEngine.Rendering.AmbientMode _ambientMode;

    void Awake()
    {
        _selection = GetComponent<DepartureCompanionSelection>();
        _selection.DepartureConfirmed += Depart;
    }

    void OnDestroy()
    {
        if (_selection != null) _selection.DepartureConfirmed -= Depart;
        // 조명/안개 복원은 항해 끝에서만 한다(씬 언로드 중에는 다음 씬 설정을 덮을 수 있다).
        foreach (var asset in _stageAssets) if (asset != null) Destroy(asset);
        _stageAssets.Clear();
    }

    void Depart(IReadOnlyList<string> ids)
    {
        if (_started || ids.Count != 2 || ids.Distinct().Count() != 2) return;
        if (boatPrefab == null || treePrefab == null || birchPrefab == null || rockPrefab == null ||
            dockPrefab == null || cratePrefab == null || waterMaterial == null)
            throw new InvalidOperationException("[VS-P2] Missing existing voyage asset reference.");
        _started = true;
        AudioManager.PlayBGM(AudioManager.OpeningVoyageAmbience);
        _player = _selection.Tutorial.player;
        _controller = _player.GetComponent<CharacterController>();
        _cameraFollow = Camera.main.GetComponent<CameraController>();
        _player.GetComponent<PlayerController>().enabled = false;
        _player.GetComponent<PlayerInteraction>().enabled = false;
        BuildHUD();
        if (FirstDayStudioAssets.Load() != null)
        { StartCoroutine(PixelVoyage(ids)); return; }
        StartCoroutine(Board(ids));
    }

    // 전시용 항해 컷(약 13초, 길게 눌러 건너뛰기). 이전 480x270 점 필터 텍스처 대신 화면 해상도로 직접 렌더한다.
    // (a) 넓은 설정 컷 → 빠른 페이드 → (b) 갑판 위 세 사람 → (c) 상승하며 섬 공개. 무대는 Voyage_ArtStage 안에만 만든다.
    IEnumerator PixelVoyage(IReadOnlyList<string> ids)
    {
        DemoRouteController.CarryCompanions(_selection);
        yield return Fade(0,1);
        _selection.HideSelection();
        var assets=FirstDayStudioAssets.Load();
        _world=new GameObject("Voyage_ArtStage").transform;_world.SetParent(transform);
        _world.position=new Vector3(1000,0,1000);
        _boatStart=_world.position;_boatEnd=_boatStart+Vector3.forward*VoyageTravel;
        BuildBoat(ids);
        _controller.enabled=false;
        _player.SetParent(Boat,true);_player.localPosition=new Vector3(0,1.38f,-.8f);
        _player.localRotation=Quaternion.identity;
        _player.GetComponent<EquipmentSystem>()?.Holster();
        _player.GetComponent<PlayerController>().ResetMotionAfterTeleport();
        // 갑판 컷에서 세 얼굴이 함께 읽히도록 동행은 뱃머리에서 섬 쪽을 바라본다.
        for(int i=0;i<_companions.Count;i++) _companions[i].transform.localRotation=Quaternion.Euler(0,i==0?-16f:16f,0);
        var camera=Camera.main;_cameraFollow.enabled=false;
        ApplyMorningAtmosphere(camera);
        BuildVoyageStage(assets);
        // 항해 카메라는 화면에 직접 그린다. 씬 전환 동안 화면 카메라가 끊기지 않게 기존 브리지 카메라는 그대로 둔다.
        _screenBridgeCamera=_fade.transform.parent.gameObject.AddComponent<Camera>();
        _screenBridgeCamera.clearFlags=CameraClearFlags.SolidColor;
        _screenBridgeCamera.backgroundColor=Color.black;
        _screenBridgeCamera.cullingMask=0;
        _screenBridgeCamera.depth=-99f;
        foreach(Transform child in _fade.transform.parent)
            if(child!=_fade.transform) child.gameObject.SetActive(false);
        // 기존 HUD 페이드(110) 아래에 둬 끝 페이드가 레터박스·자막까지 덮는다.
        _cinema=VoyageCinematicOverlay.Create(transform,"PA_VoyageCinematic",105,"P.A. COMPANY  /  개척 항로",_selection.font);
        _cinema.LetterboxTarget=1f;
        Sailing=true;voyageSeconds=CinematicSeconds;Elapsed=SkippedForSavedArrival?voyageSeconds:0f;
        float shot=0f;
        AnimateVoyage(camera,0f,0f);
        Debug.Log("[VS-P2] SAILING cinematic ids="+string.Join(",",ids)+" duration="+voyageSeconds);
        while(Elapsed<voyageSeconds)
        {
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            Elapsed=Mathf.Min(voyageSeconds,Elapsed+dt);shot+=dt;
            if(_cinema.SkipRequested) Elapsed=voyageSeconds;
            AnimateVoyage(camera,shot,dt);
            UpdateVoyageCues(shot,ids);
            // 시작 페이드인 + 설정 컷→갑판 컷 사이의 의도된 짧은 페이드.
            SetFadeAlpha(Mathf.Max(1f-VoyageCinematicOverlay.Ease(shot/.7f),Mathf.Clamp01(1f-Mathf.Abs(shot-DeckShotAt)/.2f)));
            yield return null;
        }
        _cinema.SkipEnabled=false;_cinema.HideLine();_cinema.HideTitle();
        // 끝 페이드 동안에도 배·카메라·갈매기는 계속 움직여 정지 화면이 되지 않는다.
        float from=_fade.color.a;
        for(float tail=0f;tail<.7f;)
        {
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);tail+=dt;shot+=dt;
            AnimateVoyage(camera,shot,dt);
            SetFadeAlpha(Mathf.Lerp(from,1f,VoyageCinematicOverlay.Ease(tail/.7f)));
            yield return null;
        }
        SetFadeAlpha(1f);
        Sailing=false;_arrivedIds=ids.ToArray();
        RestoreMorningAtmosphere();
        DemoRouteController.TransitionOverlay=_fade.transform.parent.gameObject;
        DemoRouteController.TransitionOverlay.transform.SetParent(null);
        // The original camera keeps its listener until unload; the destination
        // Main Camera owns the listener after unload. The bridge owns none.
        DontDestroyOnLoad(DemoRouteController.TransitionOverlay);
        ArrivalFadeFinished=true;
    }

    void SetFadeAlpha(float alpha) => _fade.color = new Color(.12f, .2f, .22f, Mathf.Clamp01(alpha));

    // ---- 항해 컷: 카메라·자막 ----
    void AnimateVoyage(Camera camera, float shot, float dt)
    {
        Vector3 path = _boatStart + Vector3.forward * (VoyageTravel * shot / CinematicSeconds);
        Boat.position = path + Vector3.up * (Mathf.Sin(shot * 1.7f) * .09f);
        Boat.rotation = Quaternion.Euler(Mathf.Sin(shot * 1.15f + .6f) * 1.1f, 0f, Mathf.Sin(shot * 1.7f) * 1.6f);
        PoseVoyageCamera(camera, shot, path);
        if (_sky != null) _sky.position = camera.transform.position;
        AnimateGulls(shot, path);
        AnimateWake(dt, path);
        for (int i = 0; i < _crests.Length; i++)
        {
            float pulse = Mathf.Max(.08f, .55f + .45f * Mathf.Sin(shot * 1.6f + i * 1.7f));
            Vector3 size = _crestScale[i];
            _crests[i].localScale = new Vector3(size.x * pulse, size.y, size.z);
        }
        if (_flag != null) _flag.localRotation = Quaternion.Euler(0f, 10f + Mathf.Sin(shot * 2.3f) * 14f, Mathf.Sin(shot * 3.1f) * 3f);
    }

    void PoseVoyageCamera(Camera camera, float shot, Vector3 path)
    {
        Vector3 position, look;
        float fov;
        if (shot < DeckShotAt)
        {
            // (a) 넓은 설정 컷: 오른쪽 뒤에서 배와 수평선 위 섬을 함께 잡고 천천히 다가간다.
            float e = VoyageCinematicOverlay.Ease(shot / DeckShotAt);
            position = path + Vector3.Lerp(new Vector3(15f, 5.8f, -19f), new Vector3(10.5f, 4.4f, -12.5f), e);
            look = path + Vector3.Lerp(new Vector3(-3f, 1.6f, 16f), new Vector3(-2.2f, 1.9f, 20f), e);
            fov = Mathf.Lerp(37f, 35f, e);
        }
        else
        {
            // (b) 뱃머리 앞에서 세 사람의 얼굴 → (c) 왼쪽으로 돌아 오르며 배 너머 섬을 공개한다.
            float d = VoyageCinematicOverlay.Ease((shot - DeckShotAt) / (RevealShotAt - DeckShotAt));
            Vector3 deck = Vector3.Lerp(new Vector3(-3.4f, 3.1f, 6.1f), new Vector3(-2.5f, 2.75f, 4.7f), d);
            float u = VoyageCinematicOverlay.Smoother((shot - RevealShotAt) / 3.6f);
            Vector3 bend = new Vector3(-4.6f, 7.4f, -3f), rise = new Vector3(1.5f, 6.2f, -14f);
            Vector3 offset = Vector3.Lerp(Vector3.Lerp(deck, bend, u), Vector3.Lerp(bend, rise, u), u);
            position = path + offset + Vector3.up * (Mathf.Max(0f, shot - RevealShotAt - 3.6f) * .25f);
            float turn = VoyageCinematicOverlay.Smoother((shot - RevealShotAt - .2f) / 3.4f);
            look = Vector3.Lerp(path + new Vector3(.1f, 2.35f, .1f), _world.position + new Vector3(0f, -.5f, IslandZ - 10f), turn);
            fov = Mathf.Lerp(43f, 40f, u);
        }
        camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(look - position, Vector3.up));
        camera.fieldOfView = fov;
    }

    void UpdateVoyageCues(float shot, IReadOnlyList<string> ids)
    {
        if (!_titleShown && shot >= .5f)
        {
            _titleShown = true;
            _cinema.ShowTitle("DAY 1", "첫 번째 섬으로", "P.A. COMPANY  /  개척 지원 프로그램");
        }
        if (shot >= 3.9f) _cinema.HideTitle();
        int cue = shot >= 4.75f && shot < 6.8f ? 0 : shot >= 6.8f && shot < 8.85f ? 1 : shot >= 9.3f && shot < 12.7f ? 2 : -1;
        if (cue == _cue) return;
        _cue = cue;
        if (cue < 0) { _cinema.HideLine(); return; }
        if (cue == 2)
        {
            _cinema.ShowLine("P.A. COMPANY  ·  개척 지원 프로그램",
                "곧 첫 번째 섬에 도착합니다. 낮에는 마을을 가꾸고, 밤에는 잡화점을 열어요.", PAUiTheme.Gold);
            return;
        }
        var candidate = cue < ids.Count ? _selection.candidates.FirstOrDefault(c => c.id == ids[cue]) : null;
        if (candidate == null) { _cinema.HideLine(); return; }
        _cinema.ShowLine(CompanionName(candidate) + "  ·  " + CleanText(candidate.profession, "동행"), VoyageLine(candidate), candidate.accent);
    }

    // 선택한 동행의 직업으로 짧고 따뜻한 첫 소감을 고른다(후보 데이터는 바꾸지 않는다).
    static string VoyageLine(DepartureCompanionSelection.Candidate c)
    {
        string id = c.id ?? string.Empty, job = c.profession ?? string.Empty;
        if (id.StartsWith("Lumberjack", StringComparison.Ordinal) || job.Contains("벌목"))
            return "섬 안쪽 숲이 울창하네요. 첫 가게 지을 목재는 넉넉하겠어요.";
        if (id.StartsWith("Miner", StringComparison.Ordinal) || job.Contains("광부"))
            return "저 바위 능선 좀 보세요. 어딘가 광맥이 숨어 있을 거예요.";
        if (id.StartsWith("Farmer", StringComparison.Ordinal) || job.Contains("농부"))
            return "흙빛이 진한 걸 보니 땅이 기름지겠어요. 첫 씨앗부터 심어 봐요.";
        if (id.StartsWith("Fisher", StringComparison.Ordinal) || job.Contains("어부"))
            return "물이 맑아서 물고기가 훤히 보여요. 저녁 찬거리는 맡겨 주세요.";
        return "드디어 우리 섬이네요. 벌써 마음이 두근거려요.";
    }

    static string CompanionName(DepartureCompanionSelection.Candidate c)
    {
        string name = c.displayName;
        if (string.IsNullOrWhiteSpace(name) && c.profile != null) name = c.profile.npcName;
        return CleanText(name, "동행");
    }

    static string CleanText(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        const string suffix = " · TEMP";
        string text = value.Trim();
        if (text.EndsWith(suffix, StringComparison.Ordinal)) text = text.Substring(0, text.Length - suffix.Length).TrimEnd();
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    // ---- 아침 대기: 낮은 해, 따뜻한 수평선 안개, 삼색 환경광. 항해 끝(페이드 완료 후)에 원래대로 돌린다. ----
    void ApplyMorningAtmosphere(Camera camera)
    {
        _fogEnabled = RenderSettings.fog; _fogMode = RenderSettings.fogMode; _fogColor = RenderSettings.fogColor;
        _fogStart = RenderSettings.fogStartDistance; _fogEnd = RenderSettings.fogEndDistance;
        _ambientMode = RenderSettings.ambientMode; _ambientLight = RenderSettings.ambientLight;
        _ambientSky = RenderSettings.ambientSkyColor; _ambientEquator = RenderSettings.ambientEquatorColor; _ambientGround = RenderSettings.ambientGroundColor;
        _atmosphereSaved = true;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = MorningHorizon;
        RenderSettings.fogStartDistance = 60f; RenderSettings.fogEndDistance = 220f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.66f, .74f, .86f);
        RenderSettings.ambientEquatorColor = new Color(.88f, .80f, .71f);
        RenderSettings.ambientGroundColor = new Color(.44f, .46f, .43f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = MorningHorizon;
        camera.farClipPlane = 260f;
        _sun = FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional && l.isActiveAndEnabled);
        if (_sun == null)
        {
            var go = new GameObject("Voyage_MorningSun");
            go.transform.SetParent(_world, false);
            _sun = go.AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.shadows = LightShadows.Soft;
            _sunCreated = true;
        }
        else
        {
            _sunRotation = _sun.transform.rotation; _sunColor = _sun.color; _sunIntensity = _sun.intensity;
        }
        _sun.transform.rotation = MorningSun;
        _sun.color = new Color(1f, .82f, .63f);
        _sun.intensity = 1.3f;
    }

    void RestoreMorningAtmosphere()
    {
        if (!_atmosphereSaved) return;
        _atmosphereSaved = false;
        RenderSettings.fog = _fogEnabled; RenderSettings.fogMode = _fogMode; RenderSettings.fogColor = _fogColor;
        RenderSettings.fogStartDistance = _fogStart; RenderSettings.fogEndDistance = _fogEnd;
        RenderSettings.ambientMode = _ambientMode; RenderSettings.ambientLight = _ambientLight;
        RenderSettings.ambientSkyColor = _ambientSky; RenderSettings.ambientEquatorColor = _ambientEquator; RenderSettings.ambientGroundColor = _ambientGround;
        if (_sun != null && !_sunCreated)
        {
            _sun.transform.rotation = _sunRotation; _sun.color = _sunColor; _sun.intensity = _sunIntensity;
        }
    }

    // ---- 항해 무대(표시 전용, 충돌 없음). 기존 waterMaterial(URP Lit)을 복제해 색만 바꾼다. ----
    // 바다·섬은 해수면 루트 아래에 둔다. 배(갑판 높이·검증 기준)는 그대로 두고 물을 흘수만큼 올려 선체가 물에 잠기게 한다.
    Transform StageRoot => _seaRoot != null ? _seaRoot : _world;

    void BuildVoyageStage(FirstDayStudioAssets assets)
    {
        _seaRoot = new GameObject("Voyage_SeaLevel").transform;
        _seaRoot.SetParent(_world, false);
        _seaRoot.localPosition = Vector3.up * .22f;
        Prim(PrimitiveType.Plane, "Voyage_Water", new Vector3(0f, -.1f, 0f), Vector3.one * 60f, Paint(new Color(.33f, .66f, .77f), .86f));
        BuildMainIsland(assets);
        BuildIslet(assets, new Vector3(-72f, 0f, 128f), 13f, 11);
        BuildIslet(assets, new Vector3(64f, 0f, 142f), 10f, 12);
        BuildIslet(assets, new Vector3(-24f, 0f, 172f), 8f, 13);
        BuildClouds();
        BuildCrests();
        BuildWake();
        BuildGulls();
        BuildSky();
    }

    Material Paint(Color color, float smoothness = .06f)
    {
        var material = new Material(waterMaterial) { name = "Voyage_Paint", hideFlags = HideFlags.DontSave };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        _stageAssets.Add(material);
        return material;
    }

    Transform Prim(PrimitiveType type, string name, Vector3 local, Vector3 scale, Material material, Transform parent = null, bool shadows = true)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        var collider = go.GetComponent<Collider>();
        if (collider != null) { collider.enabled = false; Destroy(collider); }
        go.transform.SetParent(parent != null ? parent : StageRoot, false);
        go.transform.localPosition = local;
        go.transform.localScale = scale;
        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        if (!shadows) { renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false; }
        return go.transform;
    }

    GameObject Prop(GameObject prefab, Vector3 local, float height, float yaw)
    {
        if (prefab == null) return null;
        var go = FirstDayStudioAssets.Place(prefab, StageRoot, StageRoot.position + local, height, yaw);
        foreach (var collider in go.GetComponentsInChildren<Collider>()) collider.enabled = false;
        return go;
    }

    static GameObject Pick(GameObject[] set, System.Random random) =>
        set == null || set.Length == 0 ? null : set[random.Next(set.Length)];

    // 섬 초지와 같은 밝은 수관 사본(FirstDayWorldPresentation과 같은 색 규칙). 줄기/껍질 재질은 그대로 둔다.
    void BrightenCanopy(GameObject tree)
    {
        foreach (var renderer in tree.GetComponentsInChildren<MeshRenderer>())
        {
            var materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                var m = materials[i];
                if (m == null || !(m.name.StartsWith("Green", StringComparison.Ordinal) || m.name.StartsWith("DarkGreen", StringComparison.Ordinal))) continue;
                if (!_canopy.TryGetValue(m, out var bright))
                {
                    bright = new Material(m) { name = m.name + "_Voyage", hideFlags = HideFlags.DontSave };
                    var tint = m.name.StartsWith("Dark", StringComparison.Ordinal) ? new Color(.40f, .60f, .24f) : new Color(.56f, .75f, .30f);
                    if (bright.HasProperty("_BaseColor")) bright.SetColor("_BaseColor", tint);
                    if (bright.HasProperty("_Color")) bright.SetColor("_Color", tint);
                    _canopy.Add(m, bright);
                    _stageAssets.Add(bright);
                }
                materials[i] = bright; changed = true;
            }
            if (changed) renderer.sharedMaterials = materials;
        }
    }

    // 앞쪽(남쪽) 모래 상륙지 + 흙빛 절벽과 초록 덮개의 3단 초지 + 수목/바위 군락 + P.A. 깃발.
    void BuildMainIsland(FirstDayStudioAssets assets)
    {
        var c = new Vector3(0f, 0f, IslandZ);
        Material sand = Paint(new Color(.95f, .87f, .68f)), foam = Paint(new Color(.97f, .97f, .93f), .3f),
            shallow = Paint(new Color(.52f, .85f, .83f), .75f), cliff = Paint(new Color(.74f, .62f, .48f)),
            grass = Paint(new Color(.56f, .76f, .38f)), grassHigh = Paint(new Color(.63f, .81f, .43f));
        Prim(PrimitiveType.Cylinder, "Island_Shallows", c + new Vector3(0f, -.06f, 0f), new Vector3(68f, .02f, 58f), shallow, null, false);
        Prim(PrimitiveType.Cylinder, "Island_Foam", c + new Vector3(0f, -.02f, 0f), new Vector3(54.6f, .02f, 44.6f), foam, null, false);
        Prim(PrimitiveType.Cylinder, "Island_Sand", c, new Vector3(53f, .35f, 43f), sand);
        var meadow = new Vector4(0f, IslandZ + 2.5f, 20.5f, 15.5f);
        var terrace = new Vector4(-3f, IslandZ + 7.5f, 13f, 9.5f);
        var summit = new Vector4(-6.5f, IslandZ + 10.5f, 6.25f, 4.75f);
        Terrace(meadow, -.05f, 1.45f, cliff, grass);
        Terrace(terrace, .2f, 3.0f, cliff, grass);
        Terrace(summit, 1.4f, 4.6f, cliff, grassHigh);
        float Ground(Vector2 p) => Inside(p, summit, 0f) ? 4.68f : Inside(p, terrace, 0f) ? 3.08f : Inside(p, meadow, 0f) ? 1.53f : .35f;
        bool NearEdge(Vector2 p) => Edge(p, summit) || Edge(p, terrace) || Edge(p, meadow);

        var random = new System.Random(5011);
        var broadleaf = assets.trees == null ? Array.Empty<GameObject>() : assets.trees.Where(t => t != null && t.name.Contains("Birch")).ToArray();
        int trees = 0;
        for (int attempt = 0; attempt < 220 && trees < 34; attempt++)
        {
            float x = (float)(random.NextDouble() * 2 - 1) * meadow.z, z = (float)(random.NextDouble() * 2 - 1) * meadow.w;
            var p = new Vector2(meadow.x + x, meadow.y + z);
            if (!Inside(p, meadow, -1.2f) || NearEdge(p)) continue;
            if (z < -2f && Mathf.Abs(x) < 8f) continue; // 앞쪽 공터는 상륙지로 비운다.
            if (z < 0f && random.NextDouble() > .55) continue; // 앞줄은 듬성하게, 뒤는 빽빽하게.
            var prefab = broadleaf.Length > 0 && random.NextDouble() < .6 ? broadleaf[random.Next(broadleaf.Length)] : Pick(assets.trees, random);
            var tree = Prop(prefab, new Vector3(p.x, Ground(p), p.y), 4.4f + (float)random.NextDouble() * 2.8f, random.Next(360));
            if (tree == null) continue;
            BrightenCanopy(tree);
            trees++;
        }
        for (int i = 0; i < 14; i++)
        {
            float angle = (200f + i * 10.5f) * Mathf.Deg2Rad;
            var p = new Vector2(meadow.x + Mathf.Cos(angle) * (meadow.z - 2.2f), meadow.y + Mathf.Sin(angle) * (meadow.w - 2.2f));
            Prop(Pick(assets.bushes, random), new Vector3(p.x, 1.53f, p.y), .9f + (float)random.NextDouble() * .5f, random.Next(360));
        }
        // 모래 가장자리 바위(앞쪽 상륙 호는 비움)와 단 아래 큰 바위.
        for (int i = 0; i < 13; i++)
        {
            float degrees = i * (360f / 13f) + 8f;
            if (Mathf.Abs(Mathf.DeltaAngle(degrees, 270f)) < 38f) continue;
            float angle = degrees * Mathf.Deg2Rad;
            var local = c + new Vector3(Mathf.Cos(angle) * 24.6f, .15f, Mathf.Sin(angle) * 19.8f);
            Prop(Pick(assets.rocks, random), local, 1.2f + (float)random.NextDouble() * 1.6f, random.Next(360));
        }
        foreach (var spot in new[] { new Vector2(11.5f, IslandZ + 4f), new Vector2(-17f, IslandZ + 3f), new Vector2(8f, IslandZ - 1f), new Vector2(-9f, IslandZ + 15.5f) })
            Prop(Pick(assets.rocks, random), new Vector3(spot.x, Ground(spot) - .15f, spot.y), 2.6f + (float)random.NextDouble(), random.Next(360));
        Prop(assets.dock, c + new Vector3(0f, -.3f, -23.6f), .65f, 90f);
        Prop(assets.chestClosed, c + new Vector3(1.4f, .35f, -16.6f), .8f, 20f);
        // P.A. 상륙 깃발(청록 천이 천천히 흔들린다).
        Prim(PrimitiveType.Cylinder, "PA_Flagpole", c + new Vector3(3.6f, .35f + 1.7f, -17.2f), new Vector3(.12f, 1.7f, .12f), Paint(new Color(.63f, .47f, .31f)));
        _flag = new GameObject("PA_FlagPivot").transform;
        _flag.SetParent(StageRoot, false);
        _flag.localPosition = c + new Vector3(3.6f, .35f + 3.25f, -17.2f);
        Prim(PrimitiveType.Cube, "PA_Flag", new Vector3(.64f, -.32f, 0f), new Vector3(1.24f, .68f, .05f), Paint(new Color(.20f, .55f, .56f)), _flag);
        Prim(PrimitiveType.Cube, "PA_FlagStripe", new Vector3(.64f, -.32f, 0f), new Vector3(1.26f, .14f, .06f), Paint(new Color(.98f, .95f, .87f)), _flag);
    }

    void Terrace(Vector4 ellipse, float bottom, float top, Material side, Material cap)
    {
        float half = (top - bottom) * .5f;
        Prim(PrimitiveType.Cylinder, "Terrace_Cliff", new Vector3(ellipse.x, bottom + half, ellipse.y), new Vector3(ellipse.z * 2f, half, ellipse.w * 2f), side);
        Prim(PrimitiveType.Cylinder, "Terrace_Grass", new Vector3(ellipse.x, top, ellipse.y), new Vector3(ellipse.z * 2f + 1.1f, .08f, ellipse.w * 2f + 1.1f), cap);
    }

    // ellipse = (중심 x, 중심 z, 반지름 x, 반지름 z)
    static bool Inside(Vector2 p, Vector4 e, float margin)
    {
        float x = (p.x - e.x) / Mathf.Max(.1f, e.z + margin), z = (p.y - e.y) / Mathf.Max(.1f, e.w + margin);
        return x * x + z * z < 1f;
    }

    static bool Edge(Vector2 p, Vector4 e) => Inside(p, e, .8f) && !Inside(p, e, -1.1f);

    // 수평선의 작은 섬: 모래 띠 + 낮은 언덕 + 큰 나무 몇 그루(안개에 반쯤 묻힌다).
    void BuildIslet(FirstDayStudioAssets assets, Vector3 c, float r, int seed)
    {
        var random = new System.Random(seed);
        Prim(PrimitiveType.Cylinder, "Islet_Sand", c, new Vector3(2.1f * r, .25f, 1.7f * r), Paint(new Color(.94f, .86f, .69f)));
        var hill = new Vector3(1.7f * r, .62f * r, 1.35f * r);
        Prim(PrimitiveType.Sphere, "Islet_Hill", c + new Vector3(0f, -.05f * r, .1f * r), hill, Paint(new Color(.52f, .72f, .40f)));
        for (int i = 0; i < 4; i++)
        {
            float dx = (float)(random.NextDouble() * 2 - 1) * .32f, dz = (float)(random.NextDouble() * 2 - 1) * .32f;
            float y = -.05f * r + hill.y * .5f * Mathf.Sqrt(Mathf.Max(0f, 1f - 4f * (dx * dx + dz * dz)));
            Prop(Pick(assets.trees, random), c + new Vector3(dx * hill.x, y - .3f, .1f * r + dz * hill.z), 6f + (float)random.NextDouble() * 2.5f, random.Next(360));
        }
        Prop(Pick(assets.rocks, random), c + new Vector3(.85f * r, .1f, -.55f * r), 1.6f + (float)random.NextDouble(), random.Next(360));
    }

    void BuildClouds()
    {
        var material = Paint(new Color(1f, .97f, .93f), 0f);
        var random = new System.Random(77);
        foreach (var spot in new[] { new Vector3(-55f, 27f, 135f), new Vector3(38f, 32f, 150f), new Vector3(96f, 24f, 108f),
                     new Vector3(-108f, 29f, 92f), new Vector3(4f, 39f, 176f), new Vector3(-30f, 35f, -60f) })
        {
            var cloud = new GameObject("Voyage_Cloud").transform;
            cloud.SetParent(StageRoot, false);
            cloud.localPosition = spot;
            int puffs = 4 + random.Next(2);
            for (int k = 0; k < puffs; k++)
            {
                var local = new Vector3((k - (puffs - 1) * .5f) * 4.2f + (float)random.NextDouble() * 1.5f,
                    (float)random.NextDouble() * 1.2f - (k == 0 || k == puffs - 1 ? .8f : 0f), (float)random.NextDouble() * 2f);
                var size = new Vector3(7f + (float)random.NextDouble() * 3f, 3.4f + (float)random.NextDouble() * 1.4f, 5f + (float)random.NextDouble() * 2f);
                Prim(PrimitiveType.Sphere, "Puff", local, size, material, cloud, false);
            }
        }
    }

    // 바다 결: 흩어진 흰 물결 조각이 천천히 반짝여 배가 지나가는 속도가 읽힌다.
    void BuildCrests()
    {
        var material = Paint(new Color(.94f, .98f, .98f), .3f);
        var random = new System.Random(4242);
        var crests = new List<Transform>();
        var sizes = new List<Vector3>();
        for (int i = 0; i < 180 && crests.Count < 90; i++)
        {
            float x = (float)(random.NextDouble() * 92 - 46), z = (float)(random.NextDouble() * 150 - 30);
            float ex = x / 36f, ez = (z - IslandZ) / 31f;
            if (ex * ex + ez * ez < 1f) continue;
            if (Mathf.Abs(x) < 3.2f && z > -6f && z < VoyageTravel + 6f) continue; // 뱃길 위는 비운다.
            var size = new Vector3(.7f + (float)random.NextDouble() * .8f, .02f, .1f + (float)random.NextDouble() * .08f);
            var crest = Prim(PrimitiveType.Cube, "Voyage_Crest", new Vector3(x, -.085f, z), size, material, null, false);
            crest.localRotation = Quaternion.Euler(0f, 80f + (float)random.NextDouble() * 20f, 0f);
            crests.Add(crest); sizes.Add(size);
        }
        _crests = crests.ToArray(); _crestScale = sizes.ToArray();
    }

    void BuildWake()
    {
        var material = Paint(new Color(.96f, .98f, .97f), .2f);
        _wake = new Transform[54]; _wakeAge = new float[54]; _wakeDrift = new Vector3[54];
        for (int i = 0; i < _wake.Length; i++)
        {
            _wake[i] = Prim(PrimitiveType.Cylinder, "Voyage_Wake", Vector3.zero, Vector3.one * .01f, material, null, false);
            _wake[i].gameObject.SetActive(false);
            _wakeAge[i] = 99f;
        }
    }

    // 양 뱃전과 고물에서 흰 거품이 번갈아 나와 퍼지고 줄어든다(배와 같은 월드 좌표, 회전 없음).
    void AnimateWake(float dt, Vector3 path)
    {
        if (_wake.Length == 0) return;
        const float Life = 2.2f;
        _wakeTimer += dt;
        while (_wakeTimer >= .075f)
        {
            _wakeTimer -= .075f;
            int side = _wakeNext % 3, k = _wakeNext % _wake.Length;
            _wakeNext++;
            var local = side == 2 ? new Vector3(0f, 0f, -2.3f) : new Vector3(side == 0 ? -1.05f : 1.05f, 0f, 1.2f);
            _wake[k].position = new Vector3(path.x + local.x, StageRoot.position.y - .075f, path.z + local.z);
            _wakeDrift[k] = side == 2 ? new Vector3(0f, 0f, -.2f) : new Vector3(side == 0 ? -.7f : .7f, 0f, -.35f);
            _wakeAge[k] = 0f;
            _wake[k].gameObject.SetActive(true);
        }
        for (int k = 0; k < _wake.Length; k++)
        {
            if (_wakeAge[k] >= Life) continue;
            _wakeAge[k] += dt;
            if (_wakeAge[k] >= Life) { _wake[k].gameObject.SetActive(false); continue; }
            _wake[k].position += _wakeDrift[k] * dt;
            float life = _wakeAge[k] / Life;
            float size = Mathf.Lerp(.35f, 1.25f, life) * (life > .7f ? (1f - life) / .3f : 1f);
            _wake[k].localScale = new Vector3(size, .012f, size * 1.35f);
        }
    }

    void BuildGulls()
    {
        Material white = Paint(new Color(.97f, .97f, .95f), .1f), grey = Paint(new Color(.45f, .50f, .54f)), beak = Paint(new Color(.98f, .66f, .26f));
        _gullOrbit = new[] { new Vector4(6.5f, 6.8f, .48f, 0f), new Vector4(9f, 8.4f, -.36f, 2.1f), new Vector4(7.5f, 9.6f, .41f, 4f), new Vector4(11f, 7.4f, -.3f, 5.2f) };
        _gulls = new Transform[_gullOrbit.Length];
        _gullWings = new Transform[_gullOrbit.Length * 2];
        for (int i = 0; i < _gulls.Length; i++)
        {
            var root = new GameObject("Voyage_Gull_" + i).transform;
            root.SetParent(_world, false);
            Prim(PrimitiveType.Sphere, "Body", Vector3.zero, new Vector3(.24f, .2f, .62f), white, root, false);
            Prim(PrimitiveType.Sphere, "Head", new Vector3(0f, .07f, .3f), new Vector3(.17f, .16f, .18f), white, root, false);
            Prim(PrimitiveType.Cube, "Beak", new Vector3(0f, .05f, .43f), new Vector3(.04f, .035f, .11f), beak, root, false);
            Prim(PrimitiveType.Cube, "Tail", new Vector3(0f, 0f, -.34f), new Vector3(.16f, .03f, .14f), grey, root, false);
            for (int s = 0; s < 2; s++)
            {
                float side = s == 0 ? -1f : 1f;
                var wing = new GameObject("Wing").transform;
                wing.SetParent(root, false);
                wing.localPosition = new Vector3(side * .08f, .04f, .02f);
                Prim(PrimitiveType.Cube, "Feather", new Vector3(side * .42f, 0f, 0f), new Vector3(.84f, .025f, .26f), white, wing, false);
                Prim(PrimitiveType.Cube, "Tip", new Vector3(side * .92f, 0f, -.03f), new Vector3(.26f, .03f, .2f), grey, wing, false);
                _gullWings[i * 2 + s] = wing;
            }
            _gulls[i] = root;
        }
    }

    // 배 앞쪽 하늘을 도는 갈매기: 원을 그리며 안쪽으로 기울고, 날갯짓과 활공을 번갈아 한다.
    void AnimateGulls(float t, Vector3 path)
    {
        Vector3 center = path + new Vector3(0f, 0f, 7f);
        for (int i = 0; i < _gulls.Length; i++)
        {
            var o = _gullOrbit[i];
            float a = o.w + o.z * t, turn = Mathf.Sign(o.z);
            var position = center + new Vector3(Mathf.Cos(a) * o.x, o.y + Mathf.Sin(t * .8f + o.w) * .6f, Mathf.Sin(a) * o.x);
            var tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) * turn;
            _gulls[i].SetPositionAndRotation(position, Quaternion.LookRotation(tangent, Vector3.up) * Quaternion.Euler(0f, 0f, turn * 16f));
            float glide = .5f + .5f * Mathf.Sin(t * .9f + o.w);
            float flap = 8f + Mathf.Sin(t * 8.5f + o.w * 3f) * 34f * glide;
            _gullWings[i * 2].localRotation = Quaternion.Euler(0f, 0f, -flap);
            _gullWings[i * 2 + 1].localRotation = Quaternion.Euler(0f, 0f, flap);
        }
    }

    // 정점 색 반구 하늘: 따뜻한 복숭아빛 수평선 → 크림 → 하늘색 천정, 해 쪽 수평선은 더 따뜻하다.
    // 항상 포함되는 Sprites/Default를 쓰며 없으면 단색 배경(같은 수평선 색)만 남는다.
    void BuildSky()
    {
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) return;
        var material = new Material(shader) { name = "Voyage_SkyDome", hideFlags = HideFlags.DontSave, renderQueue = 1000 };
        _stageAssets.Add(material);
        var mesh = SkyMesh(-(MorningSun * Vector3.forward));
        _stageAssets.Add(mesh);
        var go = new GameObject("Voyage_Sky", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(_world, false);
        go.transform.localScale = Vector3.one * 235f;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        _sky = go.transform;
    }

    static Mesh SkyMesh(Vector3 toSun)
    {
        float[] elevation = { -20f, 0f, 5f, 12f, 22f, 36f, 56f, 90f };
        Color[] tone = { MorningHorizon, MorningHorizon, new Color(1f, .87f, .73f), new Color(.98f, .91f, .81f),
            new Color(.82f, .89f, .91f), new Color(.64f, .81f, .91f), new Color(.51f, .73f, .89f), new Color(.43f, .66f, .87f) };
        var glow = new Color(1f, .73f, .52f);
        const int segments = 40;
        int rings = elevation.Length, stride = segments + 1;
        var vertices = new Vector3[rings * stride];
        var colors = new Color[vertices.Length];
        var sun = new Vector2(toSun.x, toSun.z).normalized;
        for (int r = 0; r < rings; r++)
        for (int s = 0; s <= segments; s++)
        {
            float az = s * Mathf.PI * 2f / segments, el = elevation[r] * Mathf.Deg2Rad;
            vertices[r * stride + s] = new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el));
            float facing = Mathf.Max(0f, Vector2.Dot(new Vector2(Mathf.Sin(az), Mathf.Cos(az)), sun));
            float warmth = Mathf.Pow(facing, 3f) * Mathf.Clamp01(1f - elevation[r] / 32f) * .7f;
            colors[r * stride + s] = Color.Lerp(tone[r], glow, warmth);
        }
        var triangles = new int[(rings - 1) * segments * 6];
        int t = 0;
        for (int r = 0; r < rings - 1; r++)
        for (int s = 0; s < segments; s++)
        {
            int a = r * stride + s, b = a + stride;
            triangles[t++] = a; triangles[t++] = b; triangles[t++] = b + 1;
            triangles[t++] = a; triangles[t++] = b + 1; triangles[t++] = a + 1;
        }
        var mesh = new Mesh { name = "Voyage_SkyDome", hideFlags = HideFlags.DontSave };
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    IEnumerator Board(IReadOnlyList<string> ids)
    {
        yield return Fade(0, 1);
        BuildIsland();
        BuildBoat(ids);
        _selection.HideSelection();
        _selection.Tutorial.buyer.Pause();
        _selection.Tutorial.buyer.gameObject.SetActive(false);
        MovePlayer(Boat.position + new Vector3(0, 1.4f, -.9f));
        _cameraFraming = new GameObject("VoyageCameraFraming").transform;
        _cameraFraming.SetParent(transform, false);
        UpdateCameraFraming();
        _cameraFollow.target = _cameraFollow.OpeningFraming ? _player : _cameraFraming;
        _cameraFollow.SnapToTarget();
        Camera.main.orthographicSize = 12f;
        yield return Fade(1, 0);
        _player.GetComponent<PlayerController>().enabled = true;
        Sailing = true;
        Debug.Log("[VS-P2] SAILING ids=" + string.Join(",", ids) + " duration=" + voyageSeconds);
        StartCoroutine(Voyage());
    }

    void Update() => UpdateCameraFraming();

    void UpdateCameraFraming()
    {
        if (_cameraFraming != null && _player != null)
            _cameraFraming.position = _player.position + new Vector3(0, .2f, 3.5f);
    }

    IEnumerator Voyage()
    {
        while (Elapsed < voyageSeconds)
        {
            Elapsed = Mathf.Min(voyageSeconds, Elapsed + Time.deltaTime);
            Vector3 next = Vector3.Lerp(_boatStart, _boatEnd, Elapsed / voyageSeconds);
            Vector3 delta = next - Boat.position;
            Boat.position = next;
            Physics.SyncTransforms();
            _controller.Move(delta);
            yield return null;
        }
        Sailing = false;
        _player.GetComponent<PlayerController>().enabled = false;
        yield return Fade(0f, 1f);
        MovePlayer(ArrivalPosition);
        var traversal = _player.GetComponent<WorldPlayerTraversalGuard>() ?? _player.gameObject.AddComponent<WorldPlayerTraversalGuard>();
        traversal.Configure(IslandGrid);
        if (!traversal.TryTeleportTo(ArrivalPosition)) throw new InvalidOperationException("[VS-P2] Arrival is not a dry walkable cell.");
        for (int i = 0; i < _companions.Count; i++)
        {
            Transform npc = _companions[i].transform;
            npc.SetParent(_world, true);
            npc.position = ArrivalPosition + new Vector3(i == 0 ? -1.7f : 1.7f, 0, .25f);
            npc.rotation = Quaternion.Euler(0, 180, 0);
        }
        _arrivedIds = _selection.ConfirmedIds.ToArray();
        _objective.text = "첫 거점을 세울 장소를 찾아보세요.";
        _subtitle.text = "P.A. COMPANY  /  FIRST ARRIVAL";
        UpdateCameraFraming();
        _cameraFollow.SnapToTarget();
        _player.GetComponent<PlayerController>().enabled = true;
        _player.GetComponent<PlayerInteraction>().enabled = true;
        Arrived = true;
        Debug.Log("[VS-P2] ARRIVED same companions=" + string.Join(",", _arrivedIds));
        yield return Fade(1f, 0f);
        ArrivalFadeFinished = true;
    }

    IEnumerator Fade(float from, float to)
    {
        for (float t = 0; t < .6f; t += Time.unscaledDeltaTime)
        {
            _fade.color = new Color(.12f, .2f, .22f, Mathf.Lerp(from, to, t / .6f));
            yield return null;
        }
        _fade.color = new Color(.12f, .2f, .22f, to);
    }

    void BuildIsland()
    {
        if (FindFirstObjectByType<WorldGridService>() != null)
            throw new InvalidOperationException("[VS-P2] Development departure requires no pre-existing world grid.");
        _world = new GameObject("PA_FirstArrival_Island").transform;
        _world.SetParent(transform, false);
        var gridObject = new GameObject("OpeningIsland_ExistingWorldGrid");
        gridObject.transform.SetParent(_world, false);
        IslandGrid = gridObject.AddComponent<WorldGridService>();
        var definition = new WorldGridDefinition(2f, 16, 16, 16, .25f, 0, 6, new Vector3(100, 0, 100));
        var cells = new WorldCellData[256];
        for (int z = 0; z < 16; z++)
        for (int x = 0; x < 16; x++)
        {
            float distance = new Vector2((x - 7.5f) / 6.5f, (z - 8f) / 6.4f).magnitude;
            bool water = distance > 1f;
            int elevation = water ? 0 : distance > .76f ? 2 : 3;
            if (!water && distance < .72f && z >= 9) elevation = Mathf.Min(6, 3 + (z - 8));
            WorldGroundType ground = water || distance > .76f ? WorldGroundType.Sand : WorldGroundType.Grass;
            cells[z * 16 + x] = new WorldCellData(new Vector2Int(x, z), elevation, ground,
                WorldPathType.None, water ? 1 : elevation, water ? 1 : 0, WorldCellOccupancy.Empty);
        }
        // WORLD-007 검증된 초기 스냅샷 경계에 작은 authored island cell 데이터를 전달한다.
        if (!IslandGrid.TryRestoreSnapshot(definition, cells)) throw new InvalidOperationException("[VS-P2] Island cell initialization rejected.");
        IslandTerrain = gridObject.AddComponent<WorldChunkTerrain>();
        if (!IslandTerrain.IsReady) throw new InvalidOperationException("[VS-P2] Existing terrain mesh/collider not ready.");
        var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
        sea.name = "Ocean_ExistingPalette";
        sea.transform.SetParent(_world, false);
        sea.transform.position = new Vector3(115, .23f, 110);
        sea.transform.localScale = Vector3.one * 16;
        sea.GetComponent<Collider>().enabled = false;
        sea.GetComponent<Renderer>().sharedMaterial = IslandTerrain.GetComponent<Renderer>().sharedMaterials[WorldSurfaceMaterialSlots.Water];

        var trees = new[] { new Vector2Int(3,6), new Vector2Int(4,8), new Vector2Int(3,10),
            new Vector2Int(5,11), new Vector2Int(7,12), new Vector2Int(9,11), new Vector2Int(11,9),
            new Vector2Int(12,7), new Vector2Int(10,6), new Vector2Int(5,7), new Vector2Int(9,9), new Vector2Int(7,10) };
        for (int i = 0; i < trees.Length; i++)
        {
            IslandGrid.CellToWorld(trees[i], out Vector3 point);
            GameObject tree = Place(i % 3 == 0 ? birchPrefab : treePrefab, "TimberResource_" + i, _world, point, 1f);
            Renderer[] renderers = tree.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            tree.transform.localScale = Vector3.one * ((4.2f + i % 3 * .4f) / Mathf.Max(.1f, bounds.size.y));
            var trunk = tree.AddComponent<CapsuleCollider>();
            trunk.radius = .10f; trunk.height = .8f; trunk.center = Vector3.up * .4f;
        }
        foreach (Vector2Int cell in new[] { new Vector2Int(4,4), new Vector2Int(10,5), new Vector2Int(11,10), new Vector2Int(6,12) })
        {
            IslandGrid.CellToWorld(cell, out Vector3 point);
            var rock = Place(rockPrefab, "StoneResource_" + cell, _world, point, 2f);
            rock.AddComponent<SphereCollider>().radius = .32f;
        }
        Place(dockPrefab, "TemporaryDock", _world, new Vector3(114, .25f, 103), 3f);
        Box("DockWalkSurface", _world, new Vector3(114, .42f, 103), new Vector3(1.95f, .16f, 4.5f));
        Place(cratePrefab, "PA_SupplyCrate", _world, new Vector3(117.4f, .5f, 105.8f), 2.2f);
    }

    void BuildBoat(IReadOnlyList<string> ids)
    {
        Boat = new GameObject("PA_DepartureBoat_Presentation").transform;
        Boat.SetParent(transform, false);
        Boat.position = _boatStart;
        Place(boatPrefab, "ExistingDepartureBoat", Boat, _boatStart, 1.8f);
        Box("DeckCollision", Boat, new Vector3(0, 1.28f, 0), new Vector3(2.7f, .16f, 4.14f), true);
        Box("PortRail", Boat, new Vector3(-1.4f, 2, 0), new Vector3(.15f, 2, 4.4f), true);
        Box("StarboardRail", Boat, new Vector3(1.4f, 2, 0), new Vector3(.15f, 2, 4.4f), true);
        Box("BowRail", Boat, new Vector3(0, 2, 2.15f), new Vector3(3, 2, .15f), true);
        Box("SternRail", Boat, new Vector3(0, 2, -2.15f), new Vector3(3, 2, .15f), true);
        for (int i = 0; i < ids.Count; i++)
        {
            var c = _selection.candidates.First(candidate => candidate.id == ids[i]);
            var npc = new GameObject("Companion_" + c.id);
            npc.transform.SetParent(Boat, false);
            npc.transform.localPosition = new Vector3(i == 0 ? -.78f : .78f, 1.38f, .9f);
            Instantiate(c.model, npc.transform).name = "CharacterVisual";
            npc.AddComponent<NpcPresentationNormalizer>();
            npc.transform.localRotation = Quaternion.Euler(0, 180, 0);
            _companions.Add(npc);
        }
    }

    static GameObject Place(GameObject prefab, string name, Transform parent, Vector3 position, float scale)
    {
        var go = Instantiate(prefab, parent);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    static void Box(string name, Transform parent, Vector3 position, Vector3 size, bool local = false)
    {
        var go = new GameObject(name, typeof(BoxCollider));
        go.transform.SetParent(parent, false);
        if (local) go.transform.localPosition = position; else go.transform.position = position;
        go.GetComponent<BoxCollider>().size = size;
    }

    void MovePlayer(Vector3 position)
    {
        _controller.enabled = false;
        _player.position = position;
        _player.rotation = Quaternion.identity;
        _controller.enabled = true;
        _player.GetComponent<PlayerController>().ResetMotionAfterTeleport();
        Physics.SyncTransforms();
    }

    void BuildHUD()
    {
        var root = new GameObject("PA_VoyageHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var paper = PAUiTheme.Cream;
        var ink = PAUiTheme.Ink;
        var header = DepartureCompanionSelection.Panel(root.transform, "Objective", 32, 28, 715, 117, paper);
        _subtitle = _selection.Label(header, "Stage", "P.A. COMPANY  /  DEPARTURE", 22, 12, 665, 26, 19, ink);
        _objective = _selection.Label(header, "Objective", "선택한 동료들과 섬으로 향하고 있습니다.", 22, 50, 670, 46, 27, ink);
        var footer = DepartureCompanionSelection.Panel(root.transform, "Companions", 32, 965, 930, 81, paper);
        string names = string.Join("  +  ", _selection.ConfirmedIds.Select(id => _selection.candidates.First(c => c.id == id).profession));
        _selection.Label(footer, "Party", "함께 온 생산자  ·  " + names + "     /     W A S D 이동", 22, 14, 890, 51, 25, ink);
        _fade = DepartureCompanionSelection.Panel(root.transform, "ArrivalFade", 0, 0, 1920, 1080, Color.clear).GetComponent<Image>();
    }
}
