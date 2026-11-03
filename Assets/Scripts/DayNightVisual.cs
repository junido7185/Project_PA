using UnityEngine;

// §8 DayNightVisual — GameClock 시각으로 기존 Directional Light와 주변광을 구동한다.
[RequireComponent(typeof(Light))]
public class DayNightVisual : MonoBehaviour
{
    [Header("광원 (null 이면 이 GameObject 의 Light 사용)")]
    public Light directionalLight;

    [Header("시간대별 색상 커브")]
    [Tooltip("X=시각(0~24h), 샘플로 그라디언트 적용")]
    public Gradient lightColorGradient;

    [Header("시간대별 강도 커브")]
    [Tooltip("X=0~1 (0h→1 = 24h), Y=강도")]
    public AnimationCurve intensityCurve = BuildDefaultIntensityCurve();

    [Header("최대 강도")]
    public float maxIntensity = 1.2f;

    [Header("태양 경로")]
    [Tooltip("X=0~1 (0h→24h), Y=Directional Light의 X 회전각. 시간 사이도 연속 보간")]
    public AnimationCurve sunPitchCurve = AnimationCurve.Linear(0f, -90f, 1f, 270f);
    [Range(-180f, 180f)] public float sunAzimuthDegrees = -30f;

    // v3 Final Presentation Lock — 청회색 skybox 앰비언트가 코지 톤을 죽이는 문제 보정.
    // Trilight 앰비언트를 시간대에 맞춰 구동한다 (낮=따뜻한 크림, 밤=어두운 남색 유지).
    [Header("앰비언트(주변광) 온도 보정 — v3")]
    public bool driveAmbient = true;
    public Gradient ambientSkyGradient;
    public Gradient ambientEquatorGradient;
    public Gradient ambientGroundGradient;

    // 전시 키아트 보정(밤에는 잡화점) — 직렬화된 그라디언트/커브 값과 무관하게 ApplyTime 마지막에 덮는 표현 바닥값.
    // 낮(08~16시)은 손대지 않는다. 골든아워(16:20~19:10)는 따뜻한 저각 햇빛, 해가 진 뒤(19:20~)는 달빛 방향·청보라 주변광 바닥.
    // 16시 대기·20시 영업 대기(시계 정지)에서 각각 낮/완전한 밤으로 보이도록 시각을 잡았다.
    public static bool PresentationGradeEnabled = true;
    public static readonly Color MoonColor = new Color(0.64f, 0.70f, 1.00f);
    public const float MoonIntensity = 0.48f;
    public static readonly Vector2 MoonAngles = new Vector2(48f, -50f); // (pitch, yaw): 카메라 오른쪽 앞에서 비춰 정면·윗면이 읽히고 그림자는 좌상단
    static readonly Color GoldenLight = new Color(1.00f, 0.68f, 0.40f);
    const float GoldenIntensity = 1.05f, SunMinElevation = 16f;
    static readonly Color NightSky = new Color(0.33f, 0.37f, 0.62f);
    static readonly Color NightEquator = new Color(0.28f, 0.28f, 0.47f);
    static readonly Color NightGround = new Color(0.15f, 0.14f, 0.25f);
    static readonly Color GoldenSky = new Color(0.66f, 0.54f, 0.62f);
    static readonly Color GoldenEquator = new Color(0.92f, 0.66f, 0.50f);
    static readonly Color GoldenGround = new Color(0.45f, 0.32f, 0.27f);
    static readonly Color NightFog = new Color(0.16f, 0.19f, 0.32f);
    static readonly Color GoldenFog = new Color(0.88f, 0.72f, 0.64f);

    // 안개색은 다른 연출(FirstDayWorldPresentation)이 정한 값을 기준으로 밤/노을에만 섞고, 낮에는 원래 값으로 돌려준다.
    static DayNightVisual s_fogOwner;
    Color _fogBase, _fogWritten;
    bool _fogTracked;

    void Awake()
    {
        if (directionalLight == null)
            directionalLight = GetComponent<Light>();
        EnsureDefaults();
    }

    // 비활성 상태에서 AddComponent된 뒤 수동 ApplyHour가 먼저 불려도 기본값이 있도록 Awake와 공유한다.
    void EnsureDefaults()
    {
        // 기본 그라디언트가 없으면 코드로 생성
        if (lightColorGradient == null || lightColorGradient.colorKeys.Length == 0)
            lightColorGradient = BuildDefaultGradient();

        // 기본 강도 커브 — 낮은 환하고 밤은 어두움
        if (intensityCurve == null || intensityCurve.length == 0)
            intensityCurve = BuildDefaultIntensityCurve();
        if (sunPitchCurve == null || sunPitchCurve.length == 0)
            sunPitchCurve = AnimationCurve.Linear(0f, -90f, 1f, 270f);

        // v3 — 앰비언트 그라디언트 기본값
        if (ambientSkyGradient == null || ambientSkyGradient.colorKeys.Length == 0)
            ambientSkyGradient = BuildGradient(
                new Color(0.10f, 0.12f, 0.20f),   // 자정
                new Color(0.50f, 0.44f, 0.42f),   // 06h
                new Color(0.64f, 0.68f, 0.76f),   // 12h
                new Color(0.58f, 0.44f, 0.40f),   // 18h
                new Color(0.10f, 0.12f, 0.20f));  // 24h
        if (ambientEquatorGradient == null || ambientEquatorGradient.colorKeys.Length == 0)
            ambientEquatorGradient = BuildGradient(
                new Color(0.08f, 0.09f, 0.14f),
                new Color(0.52f, 0.42f, 0.36f),
                new Color(0.82f, 0.76f, 0.66f),   // 낮 — 따뜻한 크림 (코지 핵심)
                new Color(0.78f, 0.54f, 0.38f),
                new Color(0.08f, 0.09f, 0.14f));
        if (ambientGroundGradient == null || ambientGroundGradient.colorKeys.Length == 0)
            ambientGroundGradient = BuildGradient(
                new Color(0.05f, 0.05f, 0.08f),
                new Color(0.30f, 0.24f, 0.20f),
                new Color(0.48f, 0.41f, 0.33f),
                new Color(0.38f, 0.28f, 0.22f),
                new Color(0.05f, 0.05f, 0.08f));
    }

    static Gradient BuildGradient(Color midnight, Color dawn, Color noon, Color dusk, Color midnightEnd)
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(midnight, 0.00f),
                new GradientColorKey(dawn, 0.25f),
                new GradientColorKey(noon, 0.50f),
                new GradientColorKey(dusk, 0.75f),
                new GradientColorKey(midnightEnd, 1.00f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }

    void Start()
    {
        if (GameClock.Instance != null)
            ApplyTime(GameClock.Instance.CurrentHour);
    }

    void LateUpdate()
    {
        if (GameClock.Instance != null)
            ApplyTime(GameClock.Instance.CurrentHour);
    }

    // 기존 캡처 툴/검증기의 수동 호출 계약을 유지한다.
    public void ApplyHour(int hour) => ApplyTime(hour);

    public void ApplyTime(float hour)
    {
        if (directionalLight == null) directionalLight = GetComponent<Light>();
        if (directionalLight == null) return;
        EnsureDefaults();

        float t = Mathf.Clamp(hour / 24f, 0f, 1f);

        Color color = lightColorGradient.Evaluate(t);
        float intensity = intensityCurve.Evaluate(t) * maxIntensity;
        float pitch = sunPitchCurve.Evaluate(t), yaw = sunAzimuthDegrees;

        bool grade = PresentationGradeEnabled;
        float h = Mathf.Repeat(hour, 24f);
        float night = grade ? PresentationNightWeight(h) : 0f;
        float golden = grade ? PresentationGoldenWeight(h) : 0f;
        if (grade)
        {
            // 골든아워: 직렬화 색/강도와 무관하게 따뜻하고 충분히 밝은 저각 햇빛으로 끌어온다.
            color = Color.Lerp(color, GoldenLight, 0.6f * golden); // 0.75는 넓은 풀밭이 황록색으로 떠서 낮춤
            intensity = Mathf.Lerp(intensity, Mathf.Max(intensity, GoldenIntensity), golden);
            float moon = MoonVisibility(h);
            if (moon > 0f)
            {
                // 해가 지평선 아래(빛이 위로 향해 지면이 꺼지던 구간) 대신 달 각도의 차가운 푸른빛으로 형태와 그림자를 살린다.
                color = MoonColor;
                intensity = MoonIntensity * moon;
                pitch = MoonAngles.x;
                yaw = MoonAngles.y;
            }
            else
            {
                // 해→달 교대는 두 빛이 모두 0인 순간에 일어나 그림자 방향이 튀지 않는다.
                intensity *= SunVisibility(h);
                if (h < 8f || h > 16f) pitch = FloorSunPitch(pitch, SunMinElevation);
            }
        }

        directionalLight.color     = color;
        directionalLight.intensity = intensity;

        // 시각 점프와 일반 시간 진행이 같은 경로를 거쳐 실제 그림자 방향을 바꾼다.
        directionalLight.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // v3 — 따뜻한 Trilight 앰비언트. 전시 보정이 켜져 있으면 노을은 장밋빛·복숭아, 밤은 청보라 바닥값을 보장한다.
        if (driveAmbient)
        {
            Color sky = ambientSkyGradient.Evaluate(t);
            Color equator = ambientEquatorGradient.Evaluate(t);
            Color ground = ambientGroundGradient.Evaluate(t);
            if (grade)
            {
                sky = GradeAmbient(sky, GoldenSky, NightSky, golden, night);
                equator = GradeAmbient(equator, GoldenEquator, NightEquator, golden, night);
                ground = GradeAmbient(ground, GoldenGround, NightGround, golden, night);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground;
        }

        GradeFog(grade, night, golden);
    }

    // 0 = 낮, 1 = 완전한 밤(18:30~19:48에 들어가고 04:42~05:54에 풀린다). 후처리·밤 장식이 같은 곡선을 공유한다.
    public static float PresentationNightWeight(float hour)
    {
        float h = Mathf.Repeat(hour, 24f);
        return h >= 12f ? Smooth(18.5f, 19.8f, h) : 1f - Smooth(4.7f, 5.9f, h);
    }

    // 0 = 보정 없음, 1 = 저녁 골든아워 최대(17:12~18:24). 새벽은 절반 세기만 쓴다.
    public static float PresentationGoldenWeight(float hour)
    {
        float h = Mathf.Repeat(hour, 24f);
        if (h >= 12f) return Smooth(16.3f, 17.2f, h) * (1f - Smooth(18.4f, 19.2f, h));
        return 0.5f * Smooth(5.0f, 5.7f, h) * (1f - Smooth(6.4f, 7.3f, h));
    }

    static float SunVisibility(float h) => h >= 12f ? 1f - Smooth(18.6f, 19.3f, h) : Smooth(5.2f, 5.9f, h);
    static float MoonVisibility(float h) => h >= 12f ? Smooth(19.3f, 19.9f, h) : 1f - Smooth(4.5f, 5.2f, h);

    static float Smooth(float a, float b, float x)
    {
        float k = Mathf.Clamp01((x - a) / (b - a));
        return k * k * (3f - 2f * k);
    }

    // 해가 지평선 근처로 내려가도 최소 고도를 유지해 노을빛이 지면에 닿게 한다(같은 쪽 하늘에서 연속).
    static float FloorSunPitch(float pitch, float minElevation)
    {
        float rad = pitch * Mathf.Deg2Rad;
        float elevation = Mathf.Asin(Mathf.Clamp(Mathf.Sin(rad), -1f, 1f)) * Mathf.Rad2Deg;
        if (elevation >= minElevation) return pitch;
        return Mathf.Cos(rad) >= 0f ? minElevation : 180f - minElevation;
    }

    static Color GradeAmbient(Color authored, Color golden, Color nightFloor, float goldenWeight, float nightWeight)
    {
        Color c = Color.Lerp(authored, golden, 0.55f * goldenWeight);
        var floor = new Color(Mathf.Max(c.r, nightFloor.r), Mathf.Max(c.g, nightFloor.g), Mathf.Max(c.b, nightFloor.b), c.a);
        return Color.Lerp(c, floor, nightWeight);
    }

    void GradeFog(bool grade, float night, float golden)
    {
        if (s_fogOwner == null || !s_fogOwner.isActiveAndEnabled) s_fogOwner = this;
        if (s_fogOwner != this) return;
        if (!grade || !RenderSettings.fog)
        {
            // 보정을 끄면 마지막으로 섞은 밤 안개색을 원래 값으로 되돌린다.
            if (_fogTracked && RenderSettings.fog && SameColor(RenderSettings.fogColor, _fogWritten))
                RenderSettings.fogColor = _fogBase;
            _fogTracked = false;
            return;
        }
        Color current = RenderSettings.fogColor;
        if (!_fogTracked || !SameColor(current, _fogWritten)) { _fogBase = current; _fogTracked = true; }
        Color c = Color.Lerp(_fogBase, GoldenFog, 0.5f * golden);
        c = Color.Lerp(c, NightFog, night);
        RenderSettings.fogColor = c;
        _fogWritten = c;
    }

    static bool SameColor(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) < 0.002f && Mathf.Abs(a.g - b.g) < 0.002f && Mathf.Abs(a.b - b.b) < 0.002f;

    static Gradient BuildDefaultGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 0.00f),   // 자정 — 짙은 남색
                new GradientColorKey(new Color(0.4f,  0.25f, 0.15f), 0.25f),   // 06h — 새벽 주황
                new GradientColorKey(new Color(1.0f,  0.95f, 0.85f), 0.50f),   // 12h — 정오 밝은 흰
                new GradientColorKey(new Color(0.9f,  0.5f,  0.2f),  0.75f),   // 18h — 저녁 노을
                new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 1.00f),   // 24h = 자정
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            }
        );
        return g;
    }

    static AnimationCurve BuildDefaultIntensityCurve()
    {
        // 낮(08h~18h) 강도 1.0, 자정·새벽 강도 0.05
        var curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0f,         0.05f));
        curve.AddKey(new Keyframe(6f  / 24f,  0.3f));
        curve.AddKey(new Keyframe(8f  / 24f,  0.9f));
        curve.AddKey(new Keyframe(12f / 24f,  1.0f));
        curve.AddKey(new Keyframe(17f / 24f,  0.9f));
        curve.AddKey(new Keyframe(20f / 24f,  0.2f));
        curve.AddKey(new Keyframe(1f,         0.05f));
        return curve;
    }
}
