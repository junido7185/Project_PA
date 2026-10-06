using UnityEngine;

// P3 — 도구가 대상에 닿는 순간의 소리와 파편. 프로젝트에 음원 자산이 없고 외부 다운로드를 하지 않으므로
// 짧은 효과음을 런타임에 합성한다. 파편은 기존 저폴리 나무/돌 모델의 작은 조각을 쓴다(새 재질·프리미티브 없음).
public static class GatherFeedback
{
    // 절차 스윙(0.5초 사인)의 내려치는 구간. 입력 즉시가 아니라 이 순간에 타격이 성립한다.
    public const float ContactDelay = .28f;

    static AudioClip _chop, _stone, _pickup;
    static AudioSource _source;

    public static AudioClip Chop => _chop != null ? _chop : _chop = Make("PA_Chop", .32f, ChopWave);
    public static AudioClip Stone => _stone != null ? _stone : _stone = Make("PA_Stone", .38f, StoneWave);
    public static AudioClip PickupClip => _pickup != null ? _pickup : _pickup = Make("PA_Pickup", .14f, PickupWave);
    public static AudioClip LastPlayed { get; private set; }
    public static float LastPlayedAt { get; private set; } = -10f;

    public static void Hit(Vector3 contact, bool stone, float volumeScale = 1f)
    {
        Play(stone ? Stone : Chop, (stone ? .75f : .85f) * volumeScale);
        var assets = FirstDayStudioAssets.Load();
        GameObject model = assets == null ? null : stone
            ? assets.rocks != null && assets.rocks.Length > 0 ? assets.rocks[0] : null
            : assets.wood;
        if (model == null) return;
        for (int i = 0; i < 6; i++)
        {
            var chip = FirstDayStudioAssets.Place(model, null, contact, .07f, Random.Range(0f, 360f));
            chip.name = "GatherChip";
            foreach (var collider in chip.GetComponentsInChildren<Collider>()) collider.enabled = false;
            var arc = chip.AddComponent<GatherChip>();
            arc.velocity = new Vector3(Random.Range(-1.6f, 1.6f), Random.Range(2.2f, 3.4f), Random.Range(-1.6f, 1.6f));
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
    public static InventoryFramework.PickupItem SpawnDrop(Item item, Vector3 from, Vector3 toward, System.Action claimed)
    {
        var assets = FirstDayStudioAssets.Load();
        GameObject model = assets != null ? assets.ModelFor(item) ?? assets.wood : null;
        if (item == null || model == null) return null;
        Vector3 flat = Vector3.ProjectOnPlane(toward - from, Vector3.up);
        Vector3 target = from + (flat.sqrMagnitude > .01f ? flat.normalized : Vector3.forward) * .95f;
        if (Physics.Raycast(target + Vector3.up * 3f, Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore))
            target.y = hit.point.y;
        var drop = FirstDayStudioAssets.Place(model, null, target, .2f, Random.Range(0f, 360f));
        drop.name = "GatherDrop_" + item.itemName;
        var collider = drop.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = .3f / Mathf.Max(.001f, drop.transform.lossyScale.x);
        var pickup = drop.AddComponent<InventoryFramework.PickupItem>();
        pickup.item = item; pickup.amount = 1; pickup.contextual = true;
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
