using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// 전시 캡처용 URP 후처리. 섬(WorldSandbox) 메인 카메라의 후처리를 켜고, 씬에 다른 전역 Volume이 없을 때만
// 런타임 전역 Volume 하나를 만들어 시간대(낮·골든아워·밤) 값으로 은은하게 조정한다.
// 기존 설정: 씬/프리팹 Volume 없음, 카메라 후처리 꺼짐, PC/Mobile RP 에셋 기본 프로필(SampleSceneProfile:
// Bloom .25/임계 1, Neutral 톤매핑, Vignette .2). 이 Volume(priority 10)이 같은 항목만 덮고 에셋은 바꾸지 않는다.
// 톤매핑은 기본 None: 지금까지 승인된 밝은 파스텔 낮 화면(후처리 없음)을 보존한다. Neutral은 밝은 크림/모래를
// 약 15% 눌러 회색빛이 돌았다. 다른 판단이 필요하면 Tonemap만 바꾼다. URP 에셋 HDR은 켜져 있어 밤 발광이 Bloom을 받는다.
public sealed class AtmospherePresentation : MonoBehaviour
{
    public static AtmospherePresentation Instance { get; private set; }
    public static bool Enabled = true;
    public static TonemappingMode Tonemap = TonemappingMode.None;
    public static bool EnableSmaa = true;
    public static readonly List<string> SceneNames = new List<string> { DemoRouteController.WorldScene };
    public const float VolumePriority = 10f;

    public Volume RuntimeVolume { get; private set; }
    public bool DeferringToSceneVolume { get; private set; }
    public float NightWeight { get; private set; }
    public float GoldenWeight { get; private set; }

    VolumeProfile _profile;
    Bloom _bloom;
    Tonemapping _tonemap;
    ColorAdjustments _color;
    Vignette _vignette;
    Camera _camera;
    DemoShopInterior _interior;
    float _nextCheck, _inside;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterBootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Enabled || !SceneNames.Contains(scene.name)) return;
        if (Instance != null && Instance.gameObject.scene == scene) return;
        var host = new GameObject("PA_AtmospherePresentation");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<AtmospherePresentation>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this && Instance.gameObject.scene == gameObject.scene) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        EnsureCamera();
        EnsureVolume();
        Apply(true);
    }

    void OnDestroy()
    {
        if (RuntimeVolume != null) Destroy(RuntimeVolume.gameObject);
        if (_profile != null) Destroy(_profile); // Add<T>로 만든 구성요소는 프로필과 함께 정리된다
        if (Instance == this) Instance = null;
    }

    void LateUpdate()
    {
        if (Time.unscaledTime >= _nextCheck)
        {
            _nextCheck = Time.unscaledTime + 1f;
            EnsureCamera();
            EnsureVolume();
            ResolveInterior();
        }
        Apply(false);
    }

    // 카메라마다 한 번만 켠다. 이후 사진 모드 등 다른 시스템이 끄면 그 결정을 존중한다.
    void EnsureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null || cam == _camera) return;
        _camera = cam;
        UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
        if (data == null) return;
        data.renderPostProcessing = true;
        data.dithering = true; // 어두운 밤 그라데이션·비네트의 밴딩을 줄인다
        // 셰이더 NaN/Inf 한 픽셀을 Bloom이 화면 크기 빛 덩어리로 번지게 하지 않는다(2026-10-31 손에 든 설계도 법선 0 사례).
        data.stopNaN = true;
        if (EnableSmaa && data.antialiasing == UnityEngine.Rendering.Universal.AntialiasingMode.None)
        {
            // 저폴리 외곽선 계단 현상 완화(RP 에셋 MSAA가 꺼져 있음). 이미 다른 AA가 설정돼 있으면 그대로 둔다.
            data.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
        }
    }

    void EnsureVolume()
    {
        // 다른 시스템/씬이 전역 Volume을 두면 중복 보정하지 않도록 이 Volume은 물러난다.
        bool foreign = false;
        foreach (var volume in FindObjectsByType<Volume>(FindObjectsSortMode.None))
            if (volume != null && volume != RuntimeVolume && volume.isGlobal && volume.isActiveAndEnabled) { foreign = true; break; }
        DeferringToSceneVolume = foreign;
        if (RuntimeVolume != null) { RuntimeVolume.enabled = !foreign; return; }
        if (foreign) return;

        _profile = ScriptableObject.CreateInstance<VolumeProfile>();
        _profile.name = "PA_AtmosphereProfile (runtime)";
        _profile.hideFlags = HideFlags.DontSave;
        _bloom = _profile.Add<Bloom>();
        _bloom.threshold.Override(1.1f);
        _bloom.intensity.Override(0.14f);
        _bloom.scatter.Override(0.55f);
        _bloom.tint.Override(Color.white);
        _bloom.highQualityFiltering.Override(true);
        _tonemap = _profile.Add<Tonemapping>();
        _tonemap.mode.Override(Tonemap);
        _color = _profile.Add<ColorAdjustments>();
        _color.postExposure.Override(0f);
        _color.contrast.Override(6f);
        _color.saturation.Override(6f);
        _color.colorFilter.Override(Color.white);
        _vignette = _profile.Add<Vignette>();
        _vignette.color.Override(Color.black);
        _vignette.intensity.Override(0.14f);
        _vignette.smoothness.Override(0.4f);

        var go = new GameObject("PA_AtmosphereVolume");
        go.SetActive(false);
        go.transform.SetParent(transform, false);
        var runtimeVolume = go.AddComponent<Volume>();
        runtimeVolume.isGlobal = true;
        runtimeVolume.priority = VolumePriority;
        runtimeVolume.weight = 1f;
        runtimeVolume.sharedProfile = _profile;
        go.SetActive(true);
        RuntimeVolume = runtimeVolume;
    }

    void ResolveInterior()
    {
        var shop = DemoSettlementController.Instance != null ? DemoSettlementController.Instance.OperatingShop : null;
        _interior = shop != null ? shop.GetComponentInParent<DemoShopInterior>() : null;
    }

    void Apply(bool immediate)
    {
        if (_profile == null) return;
        float hour = GameClock.Instance != null ? GameClock.Instance.CurrentHour : 12f;
        float n = DayNightVisual.PresentationNightWeight(hour);
        float g = DayNightVisual.PresentationGoldenWeight(hour);
        NightWeight = n;
        GoldenWeight = g;

        // 따뜻한 상점 실내에서는 밤의 청보라 필터를 약하게 해 실내 조명 색을 살린다.
        float insideTarget = _interior != null && _interior.IsInside ? 1f : 0f;
        _inside = immediate ? insideTarget : Mathf.MoveTowards(_inside, insideTarget, Time.unscaledDeltaTime * 2.5f);
        float cool = n * (1f - 0.7f * _inside);

        if (_tonemap.mode.value != Tonemap) _tonemap.mode.value = Tonemap;
        _bloom.threshold.value = Blend(1.1f, 1.0f, 0.82f, g, n);
        _bloom.intensity.value = Blend(0.14f, 0.24f, 0.55f, g, n);
        _bloom.scatter.value = Blend(0.55f, 0.6f, 0.68f, g, n);
        _bloom.tint.value = Color.Lerp(Color.white, new Color(1f, 0.93f, 0.84f), n);
        _color.postExposure.value = Blend(0f, 0.05f, 0.12f, g, n);
        _color.contrast.value = Blend(6f, 8f, 6f, g, n);
        _color.saturation.value = Mathf.Lerp(Mathf.Lerp(6f, 12f, g), -8f, cool);
        _color.colorFilter.value = Color.Lerp(Color.Lerp(Color.white, new Color(1f, 0.95f, 0.88f), g), new Color(0.90f, 0.89f, 1f), cool);
        _vignette.intensity.value = Blend(0.14f, 0.2f, 0.3f, g, n);
        _vignette.color.value = Color.Lerp(Color.black, new Color(0.04f, 0.03f, 0.12f), cool);
    }

    static float Blend(float day, float golden, float night, float g, float n) => Mathf.Lerp(Mathf.Lerp(day, golden, g), night, n);
}
