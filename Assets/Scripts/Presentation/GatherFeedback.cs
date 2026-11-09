using UnityEngine;

// P3 — 도구가 대상에 닿는 순간의 소리와 파편. 프로젝트에 음원 자산이 없고 외부 다운로드를 하지 않으므로
// 짧은 효과음을 런타임에 합성한다. 파편은 기존 저폴리 나무/돌 모델의 작은 조각을 쓴다(새 재질 자산 없음).
// D4: 기본 추적 카메라에서도 타격이 읽히도록 카메라를 향한 섬광(GatherFlash, 절차 메시·런타임 재질)을 함께 띄운다.
public static class GatherFeedback
{
    // 준비 자세와 짧은 홀드 뒤 내려치는 순간. 채집·채집 실패·그물/물고기도 이 접촉 시점을 공유한다.
    public const float ContactDelay = .28f;
    // 도끼·곡괭이만 복귀 동작을 끝낸 뒤 다시 사용한다. 그물/낚시의 기존 주기는 유지한다.
    public const float HeavyToolReturnDelay = .62f;
    public const float HeavyToolReuseDelay = HeavyToolReturnDelay + .06f;

    static AudioClip _chop, _stone, _pickup;
    static AudioSource _source;

    public static AudioClip Chop => _chop != null ? _chop : _chop = Make("PA_Chop", .32f, ChopWave);
    public static AudioClip Stone => _stone != null ? _stone : _stone = Make("PA_Stone", .38f, StoneWave);
    public static AudioClip PickupClip => _pickup != null ? _pickup : _pickup = Make("PA_Pickup", .14f, PickupWave);
    public static AudioClip LastPlayed { get; private set; }
    public static float LastPlayedAt { get; private set; } = -10f;

    // 2026-11-03 삽 파기: 흙 '푹' 소리 + 갈색 흙덩이(점토 모델) 튐. 돌 '딱' 소리는 삽과 어울리지 않았다.
    static AudioClip _dirt;
    public static AudioClip DirtClip => _dirt != null ? _dirt : _dirt = Make("PA_Dirt", .3f, DirtWave);
    public static void Dig(Vector3 contact)
    {
        Play(DirtClip, .8f);
        GatherFlash.Spawn(contact, .6f);
        var clay = Resources.Load<Item>("Items/Item_Clay");
        GameObject model = clay != null && clay.model != null ? clay.model : FirstDayStudioAssets.Load()?.wood;
        if (model == null) return;
        Vector3 toCamera = TowardCamera();
        for (int i = 0; i < 5; i++)
        {
            var chip = FirstDayStudioAssets.Place(model, null, contact, .07f, Random.Range(0f, 360f));
            chip.name = "DigChip";
            foreach (var collider in chip.GetComponentsInChildren<Collider>()) collider.enabled = false;
            var arc = chip.AddComponent<GatherChip>();
            arc.velocity = new Vector3(Random.Range(-1.3f, 1.3f), Random.Range(1.8f, 2.8f), Random.Range(-1.3f, 1.3f)) + toCamera * 1f;
        }
    }

    public static void Hit(Vector3 contact, bool stone, float volumeScale = 1f)
    {
        Play(stone ? Stone : Chop, (stone ? .75f : .85f) * volumeScale);
        // 섬광은 자원 모델이 없어도 뜬다. 멀리서 작게 치는 동행 타격(volumeScale<1)은 섬광도 작게.
        // 기본 카메라 실측(d4-impact-native-pickaxe-sheet-10fps): .72m는 캐릭터 키의 2/3 정도라 웹 참고(캐릭터 키만큼)보다 작았다.
        GatherFlash.Spawn(contact, (stone ? 1.0f : .9f) * Mathf.Clamp(volumeScale, .5f, 1f));
        var assets = FirstDayStudioAssets.Load();
        GameObject model = assets == null ? null : stone
            ? assets.rocks != null && assets.rocks.Length > 0 ? assets.rocks[0] : null
            : assets.wood;
        if (model == null) return;
        Vector3 toCamera = TowardCamera();
        for (int i = 0; i < 6; i++)
        {
            var chip = FirstDayStudioAssets.Place(model, null, contact, .085f, Random.Range(0f, 360f));
            chip.name = "GatherChip";
            foreach (var collider in chip.GetComponentsInChildren<Collider>()) collider.enabled = false;
            Lighten(chip, stone ? .55f : .45f);
            var arc = chip.AddComponent<GatherChip>();
            // 조각이 대상 뒤로 숨지 않게 카메라 쪽으로 조금 더 튄다.
            arc.velocity = new Vector3(Random.Range(-1.6f, 1.6f), Random.Range(2.2f, 3.4f), Random.Range(-1.6f, 1.6f)) + toCamera * 1.2f;
        }
    }

    // 카메라 쪽 수평 방향. 카메라가 없으면 기본 배치(뒤쪽)를 가정한다.
    public static Vector3 TowardCamera()
    {
        var camera = Camera.main;
        Vector3 flat = camera != null ? Vector3.ProjectOnPlane(-camera.transform.forward, Vector3.up) : Vector3.zero;
        return flat.sqrMagnitude > .0001f ? flat.normalized : Vector3.back;
    }

    // 플레이어 몸에 가리지 않는 대상의 옆면: 카메라 쪽이면서 화면 가로로 플레이어 반대편인 수평 방향.
    public static Vector3 OpenSide(Vector3 center, Vector3 player)
    {
        Vector3 toCamera = TowardCamera();
        Vector3 side = Vector3.Cross(Vector3.up, toCamera);
        if (Vector3.Dot(player - center, side) > 0f) side = -side;
        return (side + toCamera * .45f).normalized;
    }

    // 타격 표시점: 대상 중심축의 height 높이(바위 윗면·나무 줄기)에서 열린 옆면으로 반지름의 절반만큼 비킨다.
    public static Vector3 VisibleContact(Vector3 center, float height, float radius, Vector3 player) =>
        new Vector3(center.x, height, center.z) + OpenSide(center, player) * radius * .5f;

    static MaterialPropertyBlock _chipBlock;

    // 바닥색에 묻히던 조각을 밝은 색으로 띄운다(MaterialPropertyBlock, 공유 재질은 그대로).
    static void Lighten(GameObject chip, float amount)
    {
        _chipBlock ??= new MaterialPropertyBlock();
        foreach (var renderer in chip.GetComponentsInChildren<Renderer>())
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                string property = material == null ? null
                    : material.HasProperty("_BaseColor") ? "_BaseColor" : material.HasProperty("_Color") ? "_Color" : null;
                if (property == null) continue;
                Color color = material.GetColor(property);
                _chipBlock.Clear();
                _chipBlock.SetColor(property, Color.Lerp(color, new Color(1f, .96f, .86f, color.a), amount));
                renderer.SetPropertyBlock(_chipBlock, i);
            }
        }
    }

    public static void Pickup() => Play(PickupClip, .55f);
    // P7 동행이 들판을 살필 때의 작은 소리(플레이어 근처에서만 호출).
    public static void Rustle(float volume) => Play(PickupClip, .55f * volume);

    // P4 낚시: 찌가 물에 닿는 '첨벙', 입질의 '퐁'.
    static AudioClip _splash, _bite;
    public static AudioClip SplashClip => _splash != null ? _splash : _splash = Make("PA_Splash", .45f, SplashWave);
    public static AudioClip BiteClip => _bite != null ? _bite : _bite = Make("PA_Bite", .2f, BiteWave);
    public static void Splash() => Play(SplashClip, .7f);
    public static void Bite() => Play(BiteClip, .8f);

    // P5 곤충: 잠자리채가 공기를 가르는 '휙', 잡았을 때의 가벼운 두 음 '톡톡'.
    static AudioClip _swish, _catch;
    public static AudioClip SwishClip => _swish != null ? _swish : _swish = Make("PA_NetSwish", .26f, SwishWave);
    public static AudioClip CatchClip => _catch != null ? _catch : _catch = Make("PA_NetCatch", .22f, CatchWave);
    public static void Swish() => Play(SwishClip, .6f);
    public static void Catch() => Play(CatchClip, .7f);
    static AudioClip _blocked;
    public static AudioClip BlockedClip => _blocked != null ? _blocked : _blocked = Make("PA_ToolBlocked", .16f, BlockedWave);
    public static void ToolBlocked() => Play(BlockedClip, .4f);
    static float BlockedWave(float t, System.Random r) =>
        Mathf.Sin(2 * Mathf.PI * 170f * t) * Mathf.Exp(-t * 32f) * .4f + Noise(r) * Mathf.Exp(-t * 65f) * .12f;

    static float SwishWave(float t, System.Random r) =>
        Noise(r) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / .26f)) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / .26f)) * .38f;

    static float CatchWave(float t, System.Random r)
    {
        float frequency = t < .09f ? 880f : 1320f;
        float local = t < .09f ? t : t - .09f;
        return Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-local * 30f) * .45f;
    }

    static float SplashWave(float t, System.Random r) =>
        Noise(r) * Mathf.Exp(-t * 9f) * .42f * Mathf.Clamp01(t / .015f) + Mathf.Sin(2f * Mathf.PI * 210f * t) * Mathf.Exp(-t * 18f) * .25f;

    static float BiteWave(float t, System.Random r)
    {
        float frequency = Mathf.Lerp(420f, 180f, Mathf.Clamp01(t / .12f));
        return Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * 20f) * .6f;
    }

    // 다 캔 자원은 바닥 드롭으로 남는다(Canon v2 §4 빈손 + 바닥 아이템 → E 줍기). 가방이 가득 차도 드롭은 그대로 남는다.
    public static InventoryFramework.PickupItem SpawnDrop(Item item, Vector3 from, Vector3 toward, System.Action claimed, int amount = 1)
    {
        if (item == null || amount <= 0) return null;
        var assets = FirstDayStudioAssets.Load();
        GameObject model = assets != null ? assets.ModelFor(item) ?? assets.wood : null;
        if (item == null || model == null) return null;
        Vector3 flat = Vector3.ProjectOnPlane(toward - from, Vector3.up);
        Vector3 dir = flat.sqrMagnitude > .01f ? flat.normalized : Vector3.forward;
        // 플레이어 쪽 정면에 떨어지면 기본 카메라에서 몸에 가린다. 화면의 열린 쪽으로 50° 돌려 떨군다.
        // 서 있던 자리(1.6m)에서 약 1.2m·정면 내적 0.8이라 돌지 않고 [E]로 줍는 범위(2.05m·0.75)는 그대로다.
        Vector3 open = OpenSide(from, toward);
        Vector3 left = Quaternion.AngleAxis(-50f, Vector3.up) * dir, right = Quaternion.AngleAxis(50f, Vector3.up) * dir;
        Vector3 target = from + (Vector3.Dot(left, open) > Vector3.Dot(right, open) ? left : right) * .95f;
        if (Physics.Raycast(target + Vector3.up * 3f, Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore))
            target.y = hit.point.y;
        var drop = FirstDayStudioAssets.Place(model, null, target, .3f, Random.Range(0f, 360f));
        drop.name = "GatherDrop_" + item.itemName;
        var collider = drop.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = .3f / Mathf.Max(.001f, drop.transform.lossyScale.x);
        var pickup = drop.AddComponent<InventoryFramework.PickupItem>();
        // Keep the reward in one contextual pickup: capacity failure leaves the entire reward on the ground,
        // and the existing Claimed callback commits depletion once, after the full quantity is received.
        pickup.item = item; pickup.amount = amount; pickup.contextual = true;
        pickup.Claimed = claimed;
        var pop = drop.AddComponent<GatherDropPop>();
        pop.rest = drop.transform.position;
        drop.transform.position = from + Vector3.up * .9f;
        return pickup;
    }

    static void Play(AudioClip clip, float volume)
    {
        if (clip == null) return;
        // The demo's existing AudioManager owns volume and playback. Keep the legacy fallback
        // for scenes without that composition root, while all normal Day 1 effects respect SFX.
        bool managed = AudioManager.TryPlaySFX(clip, volume);
        if (!managed && _source == null)
        {
            var go = new GameObject("PA_GatherSfx");
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // 3인칭 16m 카메라에서도 손 앞의 타격으로 들리게 2D로 낸다.
        }
        if (!managed) _source.PlayOneShot(clip, volume);
        LastPlayed = clip;
        LastPlayedAt = Time.unscaledTime;
    }

    static AudioClip Make(string name, float seconds, System.Func<float, System.Random, float> wave)
    {
        const int rate = 44100;
        int length = Mathf.CeilToInt(rate * seconds);
        var data = new float[length];
        var random = new System.Random(name.GetHashCode());
        float smooth = 0f;
        for (int i = 0; i < length; i++)
        {
            float sample = wave(i / (float)rate, random);
            smooth += (sample - smooth) * .55f; // 거친 잡음을 조금 눌러 귀에 덜 날카롭게.
            data[i] = Mathf.Clamp(smooth, -1f, 1f);
        }
        var clip = AudioClip.Create(name, length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float Noise(System.Random random) => (float)(random.NextDouble() * 2.0 - 1.0);

    // 나무: 낮은 '퍽' + 짧은 나무결 잡음.
    static float ChopWave(float t, System.Random r) =>
        Mathf.Sin(2f * Mathf.PI * 118f * t) * Mathf.Exp(-t * 26f) * .75f + Noise(r) * Mathf.Exp(-t * 55f) * .45f;

    // 돌: 딱 하는 클릭 + 높은 울림.
    static float StoneWave(float t, System.Random r) =>
        Noise(r) * Mathf.Exp(-t * 110f) * .55f + Mathf.Sin(2f * Mathf.PI * 1850f * t) * Mathf.Exp(-t * 20f) * .28f +
        Mathf.Sin(2f * Mathf.PI * 2730f * t) * Mathf.Exp(-t * 28f) * .16f;

    // 흙: 낮은 '푹'(70Hz) + 거친 흙 잡음이 길게 꺼진다.
    static float DirtWave(float t, System.Random r) =>
        Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 24f) * .7f + Noise(r) * Mathf.Exp(-t * 16f) * .32f * (1f - Mathf.Exp(-t * 200f));

    // 줍기: 짧게 올라가는 '뽁'.
    static float PickupWave(float t, System.Random r)
    {
        float frequency = Mathf.Lerp(660f, 990f, Mathf.Clamp01(t / .09f));
        return Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * 22f) * .5f;
    }
}

// 드롭이 대상 위에서 바닥으로 짧게 튀어 나와 자리 잡는다(0.35초).
public sealed class GatherDropPop : MonoBehaviour
{
    public Vector3 rest;
    float _age;
    Vector3 _start;

    void Start() => _start = transform.position;

    void Update()
    {
        _age += Time.deltaTime;
        float k = Mathf.Clamp01(_age / .35f);
        transform.position = Vector3.Lerp(_start, rest, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * .45f);
        if (k >= 1f) { transform.position = rest; Destroy(this); }
    }
}

// 파편 한 조각: 튀어 오른 뒤 떨어지며 작아지고 사라진다(0.45초).
public sealed class GatherChip : MonoBehaviour
{
    public Vector3 velocity;
    float _age;
    Vector3 _scale;

    void Start() => _scale = transform.localScale;

    void Update()
    {
        _age += Time.deltaTime;
        velocity += Physics.gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        transform.Rotate(420f * Time.deltaTime, 260f * Time.deltaTime, 0f, Space.Self);
        transform.localScale = _scale * Mathf.Clamp01(1f - _age / .45f);
        if (_age >= .45f) Destroy(gameObject);
    }
}

// D4 타격 섬광: 카메라를 향한 흰·노란 별이 0.18초 동안 튀어 커졌다 사라진다.
// 절차 메시 + 런타임 재질 1개(자산·.meta 없음) + MaterialPropertyBlock. 셰이더는 빌드에 항상 포함되는
// Sprites/Default(없으면 UI/Default)만 쓰고, 둘 다 없으면 섬광 없이 소리·조각만 남긴다. 끝난 섬광은 꺼 두었다가 다시 쓴다.
public sealed class GatherFlash : MonoBehaviour
{
    const float Duration = .18f, PullTowardCamera = .6f;
    const int PoolLimit = 4;
    static readonly System.Collections.Generic.Stack<GatherFlash> _pool = new System.Collections.Generic.Stack<GatherFlash>();
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static Mesh _mesh;
    static Material _material;
    static MaterialPropertyBlock _block;
    static bool _shaderMissing;

    MeshRenderer _renderer;
    Vector3 _anchor;
    float _size, _age, _roll;

    public static void Spawn(Vector3 anchor, float size)
    {
        if (Camera.main == null || !EnsureResources()) return;
        GatherFlash flash = null;
        while (flash == null && _pool.Count > 0) flash = _pool.Pop(); // 씬 전환으로 파괴된 항목은 버린다.
        if (flash == null)
        {
            var go = new GameObject("GatherFlash");
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            flash = go.AddComponent<GatherFlash>();
            flash._renderer = renderer;
        }
        flash._anchor = anchor;
        flash._size = size;
        flash._age = 0f;
        flash._roll = Random.Range(0f, 45f);
        flash.gameObject.SetActive(true);
        flash.Pose();
    }

    void Update()
    {
        _age += Time.deltaTime;
        if (_age >= Duration) { Release(); return; }
        Pose();
    }

    void Pose()
    {
        var camera = Camera.main;
        if (camera == null) { Release(); return; }
        float k = Mathf.Clamp01(_age / Duration);
        // 처음 30%에 크게 튀어 오르고(살짝 넘침) 남은 동안 조금 줄며 흐려진다.
        float pop = k < .3f ? Mathf.Lerp(.35f, 1.15f, Mathf.Sin(k / .3f * Mathf.PI * .5f)) : Mathf.Lerp(1.15f, .85f, (k - .3f) / .7f);
        float alpha = k < .4f ? 1f : 1f - (k - .4f) / .6f;
        Transform view = camera.transform;
        // 시선 방향으로 카메라 쪽에 당기면 화면 위치는 그대로이고 바위·줄기 면에 묻히지 않는다.
        transform.SetPositionAndRotation(_anchor - view.forward * PullTowardCamera,
            view.rotation * Quaternion.Euler(0f, 0f, _roll + k * 25f));
        transform.localScale = Vector3.one * (_size * .5f * pop); // 메시 반지름 1 → 지름 _size
        _block.SetColor(ColorId, new Color(1f, 1f, 1f, alpha));
        _renderer.SetPropertyBlock(_block);
    }

    void Release()
    {
        gameObject.SetActive(false);
        if (_pool.Count < PoolLimit) _pool.Push(this);
        else Destroy(gameObject);
    }

    static bool EnsureResources()
    {
        if (_material != null && _mesh != null) return true;
        if (_shaderMissing) return false;
        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
        if (shader == null)
        {
            _shaderMissing = true;
            Debug.LogWarning("[GatherFlash] Sprites/Default and UI/Default are missing from this build; hit flash disabled.");
            return false;
        }
        if (_material == null)
            _material = new Material(shader) { name = "PA_GatherFlash (runtime)", hideFlags = HideFlags.HideAndDontSave, renderQueue = 3100 };
        if (_mesh == null) _mesh = BuildStar();
        _block ??= new MaterialPropertyBlock();
        return true;
    }

    // 긴 빛줄기 4개와 짧은 빛줄기 4개가 번갈아 나는 별. 가운데는 흰색, 끝은 따뜻한 노란색(정점 색).
    static Mesh BuildStar()
    {
        const int rays = 8, points = rays * 2;
        var vertices = new Vector3[points + 1];
        var colors = new Color[points + 1];
        var triangles = new int[points * 3];
        colors[0] = Color.white;
        for (int i = 0; i < points; i++)
        {
            bool tip = i % 2 == 0;
            float radius = tip ? i % 4 == 0 ? 1f : .62f : .3f;
            float angle = i * Mathf.PI / rays;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            colors[i + 1] = tip ? new Color(1f, .86f, .4f, .9f) : new Color(1f, .98f, .86f, 1f);
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % points + 1;
        }
        var mesh = new Mesh { name = "PA_GatherFlashStar", hideFlags = HideFlags.HideAndDontSave };
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = new Vector2[points + 1];
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }
}
