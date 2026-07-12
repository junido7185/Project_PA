using UnityEngine;

// §8 DayNightVisual — GameClock.OnHourTick 구독 → Directional Light 색·강도를 시각에 따라 보간.
// Light 를 Inspector 에서 연결하거나 Awake 에서 씬의 첫 번째 Directional Light 를 자동 탐색.
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
    public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("최대 강도")]
    public float maxIntensity = 1.2f;

    // v3 Final Presentation Lock — 청회색 skybox 앰비언트가 코지 톤을 죽이는 문제 보정.
    // Trilight 앰비언트를 시간대에 맞춰 구동한다 (낮=따뜻한 크림, 밤=어두운 남색 유지).
    [Header("앰비언트(주변광) 온도 보정 — v3")]
    public bool driveAmbient = true;
    public Gradient ambientSkyGradient;
    public Gradient ambientEquatorGradient;
    public Gradient ambientGroundGradient;

    void Awake()
    {
        if (directionalLight == null)
            directionalLight = GetComponent<Light>();

        // 기본 그라디언트가 없으면 코드로 생성
        if (lightColorGradient == null || lightColorGradient.colorKeys.Length == 0)
            lightColorGradient = BuildDefaultGradient();

        // 기본 강도 커브 — 낮은 환하고 밤은 어두움
        if (intensityCurve == null || intensityCurve.length == 0)
            intensityCurve = BuildDefaultIntensityCurve();

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
        {
            GameClock.Instance.OnHourTick += ApplyHour;
            ApplyHour(GameClock.Instance.CurrentHourInt);
        }
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.OnHourTick -= ApplyHour;
    }

    // v3 — 캡처 툴/검증기가 GameClock.ForceSet 후 즉시 재적용할 수 있도록 public.
    public void ApplyHour(int hour)
    {
        if (directionalLight == null) return;

        float t = hour / 24f; // 0~1

        directionalLight.color     = lightColorGradient.Evaluate(t);
        directionalLight.intensity = intensityCurve.Evaluate(t) * maxIntensity;

        // 태양 고도: 정오(12h)에 최고, 자정(0h/24h)에 최저
        float sunAngle   = (hour / 24f) * 360f - 90f; // -90° 오프셋 → 정오에 90°(천정)
        transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);

        // v3 — 따뜻한 Trilight 앰비언트 (밤은 기존처럼 어둡게 유지).
        if (driveAmbient)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSkyGradient.Evaluate(t);
            RenderSettings.ambientEquatorColor = ambientEquatorGradient.Evaluate(t);
            RenderSettings.ambientGroundColor = ambientGroundGradient.Evaluate(t);
        }
    }

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
