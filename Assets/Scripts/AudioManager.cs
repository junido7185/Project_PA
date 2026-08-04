using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// §5 AudioManager — BGM 크로스페이드 + SFX 풀링.
// 음원 없어도 오류 없이 동작 (null 클립 방어 처리).
// 정적 API: AudioManager.PlayBGM("ClipName"), AudioManager.PlaySFX("ClipName").
// Inspector NamedClip 배열에 AudioClip 을 드래그하면 이름으로 재생 가능.
[DefaultExecutionOrder(-50)]
public class AudioManager : MonoBehaviour
{
    public const string SaleConfirmedSfxName = "sale.confirm";

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
            SalesLogManager.OnSaleRecorded += HandleSaleRecorded;
    }

    void OnDisable()
    {
        SalesLogManager.OnSaleRecorded -= HandleSaleRecorded;
    }

    void OnDestroy()
    {
        if (Instance != this) return;

        Instance = null;
        if (_generatedSaleConfirmedClip != null)
            Destroy(_generatedSaleConfirmedClip);
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
        // clip == null 이면 조용히 BGM 정지 (음원 미연결 상태에서도 오류 없음)

        AudioSource next = _bgmUseA ? _bgmB : _bgmA;
        AudioSource curr = _bgmUseA ? _bgmA : _bgmB;

        if (clip != null && curr.clip == clip && curr.isPlaying) return; // 이미 재생 중

        next.clip   = clip;
        next.volume = 0f;
        if (clip != null) next.Play();

        if (_crossfadeCoroutine != null) StopCoroutine(_crossfadeCoroutine);
        _crossfadeCoroutine = StartCoroutine(Crossfade(curr, next));
        _bgmUseA = !_bgmUseA;
    }

    IEnumerator Crossfade(AudioSource from, AudioSource to)
    {
        float t = 0f;
        float startVol = from.volume;
        while (t < crossfadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float ratio = Mathf.Clamp01(t / crossfadeDuration);
            from.volume = Mathf.Lerp(startVol,   0f,         ratio);
            to.volume   = Mathf.Lerp(0f,          bgmVolume,  ratio);
            yield return null;
        }
        from.Stop();
        from.clip = null;
    }

    void ApplyBGMVolume()
    {
        AudioSource active = _bgmUseA ? _bgmA : _bgmB;
        if (active.isPlaying) active.volume = bgmVolume;
    }

    // ── SFX ─────────────────────────────────────────────────────────────────────
    void PlaySFXInternal(string clipName)
    {
        AudioClip clip = FindClip(sfxClips, clipName);
        if (clip == null && clipName == SaleConfirmedSfxName)
            clip = _generatedSaleConfirmedClip;
        if (clip == null) return;

        AudioSource src = _sfxPool[_sfxIdx % _sfxPool.Length];
        _sfxIdx = (_sfxIdx + 1) % _sfxPool.Length;

        src.volume = sfxVolume;
        src.PlayOneShot(clip);
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
