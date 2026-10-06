using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// §5 AudioManager — BGM 크로스페이드 + SFX 풀링.
// 음원 없어도 오류 없이 동작 (null 클립 방어 처리).
// 정적 API: AudioManager.PlayBGM("ClipName"), AudioManager.PlaySFX("ClipName").
// Inspector NamedClip 배열에 AudioClip 을 드래그하면 이름으로 재생 가능.
[DefaultExecutionOrder(-50)]
public class AudioManager : MonoBehaviour
{
    public const string SaleConfirmedSfxName = "sale.confirm";
    public const string OpeningHarborAmbience = "opening.harbor";
    public const string OpeningVoyageAmbience = "opening.voyage";
    public const string OpeningDayAmbience = "opening.day";
    public const string OpeningNightAmbience = "opening.night";

    public static AudioManager Instance { get; private set; }

    [Serializable]
    public class NamedClip
    {
        public string    clipName;
        public AudioClip clip;
    }

    [Header("클립 라이브러리")]
    public NamedClip[] bgmClips = Array.Empty<NamedClip>();
    public NamedClip[] sfxClips = Array.Empty<NamedClip>();

    [Header("볼륨 (0~1)")]
    [Range(0f, 1f)] public float bgmVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;

    [Header("크로스페이드")]
    public float crossfadeDuration = 1.2f;

    // BGM 크로스페이드용 AudioSource ×2
    AudioSource _bgmA;
    AudioSource _bgmB;
    bool        _bgmUseA = true;   // 현재 재생 중인 소스
    float _bgmGainA, _bgmGainB;
    string _bgmName;
    readonly Dictionary<string, AudioClip> _openingAmbience = new Dictionary<string, AudioClip>();

    // SFX 풀
    [SerializeField] int sfxPoolSize = 8;
    AudioSource[] _sfxPool;
    int           _sfxIdx;
    AudioClip     _generatedSaleConfirmedClip;

    Coroutine _crossfadeCoroutine;

    // ── 정적 진입점 ─────────────────────────────────────────────────────────────
    public static void PlayBGM(string clipName)
        => Instance?.PlayBGMInternal(clipName);

    public static void PlaySFX(string clipName)
        => Instance?.PlaySFXInternal(clipName);

    // Synthesized gameplay feedback uses the same pool and SFX setting as named clips.
    public static bool TryPlaySFX(AudioClip clip, float gain = 1f)
    {
        if (Instance == null || clip == null) return false;
        Instance.PlaySFXClip(clip, gain);
        return true;
    }

    public static void SetBGMVolume(float vol)
    {
        if (Instance == null) return;
        Instance.bgmVolume = Mathf.Clamp01(vol);
        Instance.ApplyBGMVolume();
    }

    public static void SetSFXVolume(float vol)
    {
        if (Instance == null) return;
        Instance.sfxVolume = Mathf.Clamp01(vol);
        if (Instance._sfxPool != null)
            foreach (var source in Instance._sfxPool)
                if (source != null) source.volume = Instance.sfxVolume;
    }

    // ── Unity 생명주기 ──────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildAudioSources();
        BuildGeneratedSaleConfirmedClip();
    }

    void OnEnable()
    {
        if (Instance == null || Instance == this)
        {
            SalesLogManager.OnSaleRecorded += HandleSaleRecorded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }
    }

    void OnDisable()
    {
        SalesLogManager.OnSaleRecorded -= HandleSaleRecorded;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance != this) return;

        Instance = null;
        if (_generatedSaleConfirmedClip != null)
            Destroy(_generatedSaleConfirmedClip);
        foreach (var clip in _openingAmbience.Values)
            if (clip != null) Destroy(clip);
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // A return to the title must not carry the island's night bed into the menu.
        if (_bgmName != null && _bgmName.StartsWith("opening.", StringComparison.Ordinal)
            && scene.name != DepartureTutorialController.SceneName && scene.name != "WorldSandbox")
            PlayBGMInternal(null);
    }

    void BuildAudioSources()
    {
        _bgmA = gameObject.AddComponent<AudioSource>();
        _bgmB = gameObject.AddComponent<AudioSource>();
        foreach (var src in new[] { _bgmA, _bgmB })
        {
            src.loop        = true;
            src.playOnAwake = false;
            src.volume      = 0f;
        }

        int safePoolSize = Mathf.Max(1, sfxPoolSize);
        _sfxPool = new AudioSource[safePoolSize];
        for (int i = 0; i < safePoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.loop        = false;
            src.playOnAwake = false;
            src.volume      = sfxVolume;
            _sfxPool[i]     = src;
        }
    }

    // ── BGM ─────────────────────────────────────────────────────────────────────
    void PlayBGMInternal(string clipName)
    {
        AudioClip clip = FindClip(bgmClips, clipName);
        if (clip == null) clip = FindOpeningAmbience(clipName);
        // clip == null 이면 조용히 BGM 정지 (음원 미연결 상태에서도 오류 없음)

        AudioSource next = _bgmUseA ? _bgmB : _bgmA;
        AudioSource curr = _bgmUseA ? _bgmA : _bgmB;

        if (clip != null && curr.clip == clip && curr.isPlaying) return; // 이미 재생 중

        if (_crossfadeCoroutine != null) StopCoroutine(_crossfadeCoroutine);
        SetBGMGain(next, 0f);
        next.Stop();
        next.clip = clip;
        if (clip != null) next.Play();

        _bgmName = clipName;
        _crossfadeCoroutine = StartCoroutine(Crossfade(curr, next));
        _bgmUseA = !_bgmUseA;
    }

    IEnumerator Crossfade(AudioSource from, AudioSource to)
    {
        float t = 0f;
        float startGain = from == _bgmA ? _bgmGainA : _bgmGainB;
        while (t < crossfadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float ratio = Mathf.Clamp01(t / crossfadeDuration);
            SetBGMGain(from, Mathf.Lerp(startGain, 0f, ratio));
            SetBGMGain(to, ratio);
            yield return null;
        }
        from.Stop();
        from.clip = null;
        SetBGMGain(from, 0f);
        SetBGMGain(to, 1f);
        _crossfadeCoroutine = null;
    }

    void ApplyBGMVolume()
    {
        _bgmA.volume = _bgmGainA * bgmVolume;
        _bgmB.volume = _bgmGainB * bgmVolume;
    }

    void SetBGMGain(AudioSource source, float gain)
    {
        if (source == _bgmA) _bgmGainA = gain; else _bgmGainB = gain;
        source.volume = gain * bgmVolume;
    }

    // Local, quiet fallback sound beds. Inspector clips take priority; no outside
    // recordings/music or extra AudioSource authority. Cyclic noise and envelopes
    // keep the loop seam continuous and leave headroom for tool/sale feedback.
    AudioClip FindOpeningAmbience(string name)
    {
        int kind = name == OpeningHarborAmbience ? 0 : name == OpeningVoyageAmbience ? 1
            : name == OpeningDayAmbience ? 2 : name == OpeningNightAmbience ? 3 : -1;
        if (kind < 0) return null;
        if (_openingAmbience.TryGetValue(name, out var cached)) return cached;
        const int rate = 24000;
        const float duration = 12f;
        var random = new System.Random(31007 + kind * 157);
        float[] low = NoiseTrack(random, 2048), high = NoiseTrack(random, 32768);
        var samples = new float[(int)(rate * duration)];
        bool sea = kind < 2, night = kind == 3;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate, phase = t / duration;
            float swell = .5f + .5f * Mathf.Sin(phase * Mathf.PI * 4f);
            float breeze = .7f + .3f * Mathf.Cos(phase * Mathf.PI * 2f);
            float noise = CyclicNoise(low, phase) * .78f + CyclicNoise(high, phase) * .22f;
            float sample = noise * (sea ? .045f + swell * .027f : .025f * breeze);
            if (kind == 1) sample *= 1.18f;
            if (kind == 2)
            {
                sample += BirdPhrase(t - 2.1f, 2200f) * .009f;
                sample += BirdPhrase(t - 8.4f, 2550f) * .006f;
            }
            if (night)
            {
                float pulse = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * Mathf.PI * 10f)), 8f);
                float chorus = .5f + .5f * Mathf.Cos(phase * Mathf.PI * 4f);
                sample += Mathf.Sin(t * Mathf.PI * 2f * 2800f) * pulse * chorus * .0045f;
            }
            samples[i] = sample;
        }
        var clip = AudioClip.Create("PA_Ambience_" + name.Substring(8), samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        _openingAmbience.Add(name, clip);
        return clip;
    }

    static float[] NoiseTrack(System.Random random, int count)
    {
        var values = new float[count];
        for (int i = 0; i < count; i++) values[i] = (float)(random.NextDouble() * 2d - 1d);
        return values;
    }

    static float CyclicNoise(float[] values, float phase)
    {
        float index = phase * values.Length;
        int lo = (int)index; float blend = index - lo;
        blend = blend * blend * (3f - 2f * blend);
        return Mathf.Lerp(values[lo % values.Length], values[(lo + 1) % values.Length], blend);
    }

    static float BirdPhrase(float time, float frequency)
    {
        if (time < 0f || time > .6f) return 0f;
        float pulse = time % .2f;
        if (pulse > .12f) return 0f;
        float envelope = Mathf.Sin(pulse / .12f * Mathf.PI);
        float phase = Mathf.PI * 2f * (frequency * pulse + 650f * pulse * pulse);
        return Mathf.Sin(phase) * envelope * envelope;
    }

    // ── SFX ─────────────────────────────────────────────────────────────────────
    void PlaySFXInternal(string clipName)
    {
        AudioClip clip = FindClip(sfxClips, clipName);
        if (clip == null && clipName == SaleConfirmedSfxName)
            clip = _generatedSaleConfirmedClip;
        if (clip == null) return;
        PlaySFXClip(clip, 1f);
    }

    void PlaySFXClip(AudioClip clip, float gain)
    {
        AudioSource src = _sfxPool[_sfxIdx % _sfxPool.Length];
        _sfxIdx = (_sfxIdx + 1) % _sfxPool.Length;

        src.volume = sfxVolume;
        src.PlayOneShot(clip, Mathf.Clamp01(gain));
    }

    void HandleSaleRecorded(SaleRecord record)
    {
        if (record == null) return;
        PlaySFXInternal(SaleConfirmedSfxName);
    }

    // 프로젝트 내부에서 결정적으로 생성하는 짧은 판매 확인음이다.
    // 외부 샘플이나 라이선스가 필요한 음원을 사용하지 않으며, Inspector 클립이 있으면 그쪽이 우선한다.
    void BuildGeneratedSaleConfirmedClip()
    {
        const float duration = 0.30f;
        int sampleRate = Mathf.Clamp(AudioSettings.outputSampleRate, 22050, 48000);
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        var samples = new float[sampleCount];
        float peak = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float first = BuildMalletTone(time, 659.25f, 13f);
            float second = BuildMalletTone(time - 0.075f, 830.61f, 14f);
            float woodenBody = BuildMalletTone(time, 329.63f, 18f);
            float attack = Mathf.Clamp01(time / 0.006f);
            float release = Mathf.Clamp01((duration - time) / 0.05f);

            float sample = (first * 0.18f + second * 0.20f + woodenBody * 0.045f)
                         * attack * release;
            samples[i] = sample;
            peak = Mathf.Max(peak, Mathf.Abs(sample));
        }

        const float maximumPeak = 0.42f;
        if (peak > maximumPeak)
        {
            float gain = maximumPeak / peak;
            for (int i = 0; i < samples.Length; i++)
                samples[i] *= gain;
        }

        _generatedSaleConfirmedClip = AudioClip.Create(
            "PA_Generated_SaleConfirmed", sampleCount, 1, sampleRate, false);
        _generatedSaleConfirmedClip.SetData(samples, 0);
    }

    static float BuildMalletTone(float time, float frequency, float decay)
    {
        if (time < 0f) return 0f;

        float phase = Mathf.PI * 2f * frequency * time;
        float harmonics = Mathf.Sin(phase)
                        + Mathf.Sin(phase * 2f) * 0.22f
                        + Mathf.Sin(phase * 3f) * 0.07f;
        return harmonics * Mathf.Exp(-decay * time);
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────────────────
    AudioClip FindClip(NamedClip[] library, string name)
    {
        if (library == null) return null;
        foreach (var nc in library)
            if (nc != null && nc.clipName == name && nc.clip != null)
                return nc.clip;
        return null;
    }
}
