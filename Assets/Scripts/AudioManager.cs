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

        _sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
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
        if (clip == null) return;

        AudioSource src = _sfxPool[_sfxIdx % _sfxPool.Length];
        _sfxIdx = (_sfxIdx + 1) % _sfxPool.Length;

        src.volume = sfxVolume;
        src.PlayOneShot(clip);
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
