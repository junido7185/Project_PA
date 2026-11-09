using UnityEngine;

// 발견물 반짝임(해변 발견물·파기 자국). 몇 초마다 작은 네 갈래 별빛이 한 번 반짝여 멀리서·밤에도 '주울 것'이 보이게 한다.
// 표시 전용(충돌·상호작용·저장 없음). 셰이더는 빌드에 늘 포함되는 Sprites/Default, 텍스처는 런타임에 한 번 그린다.
public sealed class FindSparkle : MonoBehaviour
{
    const float Period = 2.8f, Flash = .6f, Size = .85f, Lift = .38f;
    static Sprite _sprite;
    static Material _material;
    SpriteRenderer _renderer;
    float _phase;

    public static void Attach(GameObject target, int seed)
    {
        if (target == null || target.GetComponentInChildren<FindSparkle>() != null) return;
        var go = new GameObject("FindSparkle");
        go.transform.SetParent(target.transform, false);
        var sparkle = go.AddComponent<FindSparkle>();
        sparkle._phase = (seed * 0.618f) % 1f * Period;
    }

    void Awake()
    {
        _renderer = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = Sprite;
        if (_material == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) _material = new Material(shader) { name = "PA_FindSparkle" };
        }
        if (_material != null) _renderer.sharedMaterial = _material;
        _renderer.color = new Color(1f, .97f, .82f, 0f);
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        var parent = transform.parent;
        if (cam == null || parent == null) return;
        float t = Mathf.Repeat(Time.time + _phase, Period);
        float a = t < Flash ? Mathf.Sin(t / Flash * Mathf.PI) : 0f;
        // 밤(19~6시)에는 조금 더 밝고 크게.
        float hour = GameClock.Instance != null ? GameClock.Instance.CurrentHour : 12f;
        bool night = hour >= 19f || hour < 6f;
        _renderer.enabled = a > .01f;
        if (!_renderer.enabled) return;
        _renderer.color = new Color(1f, .97f, .82f, a * (night ? 1f : .85f));
        float scale = Size * (night ? 1.25f : 1f) * (.6f + .4f * a);
        // 부모 크기와 무관한 월드 크기·위치, 카메라를 보는 판.
        transform.position = parent.position + Vector3.up * Lift + (cam.transform.position - parent.position).normalized * .15f;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up) *
                             Quaternion.Euler(0, 0, t * 90f);
        Vector3 lossy = parent.lossyScale;
        transform.localScale = new Vector3(scale / Mathf.Max(.0001f, lossy.x), scale / Mathf.Max(.0001f, lossy.y), scale / Mathf.Max(.0001f, lossy.z));
    }

    // 네 갈래 별빛(가운데 밝은 점 + 가는 십자 빛줄기), 64px.
    // 주민 감정 별(NpcEmote)도 같은 별 그림·재질을 쓴다.
    internal static Sprite StarSprite => Sprite;
    internal static Material StarMaterial
    {
        get
        {
            if (_material == null) { var shader = Shader.Find("Sprites/Default"); if (shader != null) _material = new Material(shader) { name = "PA_FindSparkle" }; }
            return _material;
        }
    }

    static Sprite Sprite
    {
        get
        {
            if (_sprite != null) return _sprite;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "PA_FindSparkle" };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + .5f) / n * 2f - 1f, v = (y + .5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float core = Mathf.Clamp01(1f - r / .28f);
                    float rays = Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(u) / .07f) * Mathf.Clamp01(1f - Mathf.Abs(v)),
                                           Mathf.Clamp01(1f - Mathf.Abs(v) / .07f) * Mathf.Clamp01(1f - Mathf.Abs(u)));
                    float alpha = Mathf.Clamp01(core * core + rays * rays * 1.2f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            tex.SetPixels32(px); tex.Apply(false, true);
            _sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), n);
            return _sprite;
        }
    }
}
