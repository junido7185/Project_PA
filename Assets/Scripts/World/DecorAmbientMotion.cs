using System.Collections.Generic;
using UnityEngine;

// 2026-11-04 ACNH 소품 연구 H3(작은 움직임): 놓인 유리 모빌의 줄·유리 조각이 줄 윗끝을 축으로 바람에 살짝 흔들린다.
// 표시 전용: 자산·충돌·배치·저장은 그대로. 새로 놓인 모빌은 2초마다 가게 배치물에서 찾아 붙이고, 사라진 모빌은 버린다.
public sealed class DecorAmbientMotion : MonoBehaviour
{
    public static int Swinging { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Swinging = 0; Twinkles = 0; }

    const float ScanInterval = 2f, SwingDegrees = 7f, SwingSpeed = 1.5f;

    sealed class Pendulum
    {
        public Transform String, Gem;
        public Vector3 Pivot, StringRest, GemRest;
        public Quaternion StringRot, GemRot;
        public float Phase;
    }

    readonly List<Pendulum> _pendulums = new List<Pendulum>();

    // 2026-11-08 H3: 놓인 광물 받침(decor-crystal) 위에 가끔 작은 별이 반짝인다(발견물 반짝임과 같은 별 그림).
    public static int Twinkles { get; private set; }
    sealed class Twinkle { public Transform Crystal; public SpriteRenderer Star; public float NextAt, StartAt; public Vector3 Offset; public float Top; }
    readonly List<Twinkle> _twinkles = new List<Twinkle>();
    const float TwinkleLife = .7f;
    readonly HashSet<int> _known = new HashSet<int>();
    float _scanAt;

    void Update()
    {
        if (Time.time >= _scanAt) { _scanAt = Time.time + ScanInterval; Scan(); }
        float t = Time.time * SwingSpeed;
        for (int i = _pendulums.Count - 1; i >= 0; i--)
        {
            var p = _pendulums[i];
            if (p.String == null || p.Gem == null) { _pendulums.RemoveAt(i); continue; }
            // 두 사인을 섞어 규칙적인 시계추가 아니라 바람에 흔들리는 느낌.
            float a = (Mathf.Sin(t + p.Phase) * .7f + Mathf.Sin(t * 1.9f + p.Phase * 2.3f) * .3f) * SwingDegrees;
            var q = Quaternion.Euler(a * .35f, 0f, a);
            p.String.localPosition = p.Pivot + q * (p.StringRest - p.Pivot); p.String.localRotation = q * p.StringRot;
            p.Gem.localPosition = p.Pivot + q * (p.GemRest - p.Pivot); p.Gem.localRotation = q * p.GemRot;
        }
        Swinging = _pendulums.Count;
        UpdateTwinkles();
    }

    void UpdateTwinkles()
    {
        var cam = Camera.main;
        for (int i = _twinkles.Count - 1; i >= 0; i--)
        {
            var w = _twinkles[i];
            if (w.Crystal == null) { if (w.Star != null) Destroy(w.Star.gameObject); _twinkles.RemoveAt(i); continue; }
            float now = Time.time;
            if (!w.Star.enabled && now >= w.NextAt)
            {
                w.StartAt = now; w.Star.enabled = true; Twinkles++;
                w.Offset = new Vector3(Random.Range(-.12f, .12f), Random.Range(-.05f, .08f), Random.Range(-.12f, .12f));
            }
            if (!w.Star.enabled) continue;
            float t = (now - w.StartAt) / TwinkleLife;
            if (t >= 1f) { w.Star.enabled = false; w.NextAt = now + Random.Range(1.8f, 3.6f); continue; }
            float k = Mathf.Sin(t * Mathf.PI);
            w.Star.transform.position = w.Crystal.position + Vector3.up * w.Top + w.Offset;
            w.Star.transform.localScale = Vector3.one * (.06f + .14f * k);
            var c = w.Star.color; c.a = k; w.Star.color = c;
            if (cam != null) w.Star.transform.rotation = Quaternion.LookRotation(w.Star.transform.position - cam.transform.position, cam.transform.up) * Quaternion.Euler(0, 0, t * 90f);
        }
    }

    void ScanCrystals(DemoSettlementController settlement)
    {
        foreach (var placed in settlement.ShopDecorWith("decor-crystal"))
        {
            if (!_known.Add(placed.GetInstanceID())) continue;
            float top = .4f;
            var renderers = placed.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                float max = float.MinValue;
                foreach (var r in renderers) max = Mathf.Max(max, r.bounds.max.y);
                top = Mathf.Max(.2f, max - placed.transform.position.y - .05f);
            }
            var star = new GameObject("CrystalTwinkle").AddComponent<SpriteRenderer>();
            star.transform.SetParent(transform, false);
            star.sprite = FindSparkle.StarSprite;
            if (FindSparkle.StarMaterial != null) star.sharedMaterial = FindSparkle.StarMaterial;
            star.color = new Color(1f, .97f, .85f, 0f);
            star.enabled = false;
            _twinkles.Add(new Twinkle { Crystal = placed.transform, Star = star, Top = top, NextAt = Time.time + Random.Range(.3f, 1.5f) });
        }
    }

    static float MeshTop(Transform t)
    {
        var filter = t.GetComponent<MeshFilter>();
        return filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds.max.y : .5f;
    }

    void Scan()
    {
        // 유리 모빌은 가게 안에만 놓인다(decor-glass). 씬 전체를 훑지 않고 그 배치물의 자식만 본다.
        var settlement = DemoSettlementController.Instance;
        if (settlement == null) return;
        ScanCrystals(settlement);
        foreach (var placed in settlement.ShopDecorWith("decor-glass"))
        foreach (var t in placed.GetComponentsInChildren<Transform>())
        {
            if (!t.name.StartsWith("MobileString")) continue;
            if (!_known.Add(t.GetInstanceID())) continue;
            var gem = t.parent != null ? t.parent.Find("MobileGem" + t.name.Substring("MobileString".Length)) : null;
            if (gem == null) continue;
            _pendulums.Add(new Pendulum
            {
                String = t, Gem = gem,
                // 부품 크기는 메시에 구워져 있다(PA_DemoIconBake.Part, 축척 1) → 메시 윗면이 매단 점.
                Pivot = t.localPosition + t.localRotation * Vector3.Scale(t.localScale, Vector3.up * MeshTop(t)),
                StringRest = t.localPosition, GemRest = gem.localPosition, StringRot = t.localRotation, GemRot = gem.localRotation,
                Phase = _pendulums.Count * 1.3f + Random.value * .6f
            });
        }
    }
}
