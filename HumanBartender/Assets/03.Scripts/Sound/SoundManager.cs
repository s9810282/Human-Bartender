using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}


public class SoundManager : MonoBehaviour, ISoundManager
{
    [Header("Audio Mixer (그룹: Master/BGM/SE/SpecialSE)")]
    [SerializeField] private AudioMixer mixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;        // BG 전용 (Loop)
    [SerializeField] private AudioSource specialSeSource;  // Special SE 전용 (단독 재생)

    [Header("SE Pool")]
    [SerializeField] private int sePoolSize = 12;
    private readonly List<AudioSource> sePool = new();
    private int seCursor;

    [Header("Sound Lists")]
    [SerializeField] private List<Sound> bgmList = new();
    [SerializeField] private List<Sound> seList = new();
    [SerializeField] private List<Sound> specialSeList = new();

    private Dictionary<string, Sound> _bgm;
    private Dictionary<string, Sound> _se;
    private Dictionary<string, Sound> _special;

    private const string MIXER_BGM = "BGMVolume";
    private const string MIXER_SE = "SEVolume";
    private const string MIXER_SPECIAL = "SpecialSEVolume";

    private Coroutine _bgmFadeRoutine;

    private void Awake()
    {
        BuildDictionaries();
        BuildSePool();
    }

    private void BuildDictionaries()
    {
        _bgm = new Dictionary<string, Sound>();
        _se = new Dictionary<string, Sound>();
        _special = new Dictionary<string, Sound>();

        foreach (var s in bgmList) if (s != null && !string.IsNullOrEmpty(s.name)) _bgm[s.name] = s;
        foreach (var s in seList) if (s != null && !string.IsNullOrEmpty(s.name)) _se[s.name] = s;
        foreach (var s in specialSeList) if (s != null && !string.IsNullOrEmpty(s.name)) _special[s.name] = s;
    }

    private void BuildSePool()
    {
        var seGroup = mixer != null ? mixer.FindMatchingGroups("SE") : null;
        for (int i = 0; i < sePoolSize; i++)
        {
            var go = new GameObject($"SE_Source_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            if (seGroup != null && seGroup.Length > 0) src.outputAudioMixerGroup = seGroup[0];
            sePool.Add(src);
        }
    }

    // ─── BG ─────────────────────────────────────────
    public void PlayBGM(string name, float fadeDuration = 1f, bool loop = true)
    {
        if (!_bgm.TryGetValue(name, out var sound))
        {
            Debug.LogWarning($"[SoundManager] BGM '{name}' 없음");
            return;
        }

        if (_bgmFadeRoutine != null) StopCoroutine(_bgmFadeRoutine);

        if (fadeDuration <= 0f)
        {
            bgmSource.clip = sound.clip;
            bgmSource.volume = sound.volume;
            bgmSource.pitch = sound.pitch;
            bgmSource.loop = loop;
            bgmSource.Play();
        }
        else
        {
            _bgmFadeRoutine = StartCoroutine(CrossfadeBGM(sound, fadeDuration, loop));
        }
    }

    private IEnumerator CrossfadeBGM(Sound next, float duration, bool loop)
    {
        float half = duration * 0.5f;

        if (bgmSource.isPlaying)
        {
            float startVol = bgmSource.volume;
            for (float t = 0; t < half; t += Time.unscaledDeltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVol, 0f, t / half);
                yield return null;
            }
        }

        bgmSource.clip = next.clip;
        bgmSource.pitch = next.pitch;
        bgmSource.loop = loop;
        bgmSource.volume = 0f;
        bgmSource.Play();

        for (float t = 0; t < half; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0f, next.volume, t / half);
            yield return null;
        }
        bgmSource.volume = next.volume;
        _bgmFadeRoutine = null;
    }

    public void StopBGM(float fadeDuration = 1f)
    {
        if (_bgmFadeRoutine != null) StopCoroutine(_bgmFadeRoutine);
        if (fadeDuration <= 0f) { bgmSource.Stop(); return; }
        _bgmFadeRoutine = StartCoroutine(FadeOutAndStop(fadeDuration));
    }

    private IEnumerator FadeOutAndStop(float duration)
    {
        float startVol = bgmSource.volume;
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        bgmSource.Stop();
        _bgmFadeRoutine = null;
    }

    public void PauseBGM() => bgmSource.Pause();
    public void ResumeBGM() => bgmSource.UnPause();

    // ─── SE (동시 재생) ─────────────────────────────
    public void PlaySE(string name)
    {
        if (!_se.TryGetValue(name, out var sound))
        {
            Debug.LogWarning($"[SoundManager] SE '{name}' 없음");
            return;
        }

        AudioSource src = GetFreeSeSource();
        src.pitch = sound.pitch;
        src.PlayOneShot(sound.clip, sound.volume);
    }

    private AudioSource GetFreeSeSource()
    {
        foreach (var src in sePool)
            if (!src.isPlaying) return src;

        var reuse = sePool[seCursor];
        seCursor = (seCursor + 1) % sePool.Count;
        return reuse;
    }

    // ─── Special SE (단독 채널) ─────────────────────
    public void PlaySpecialSE(string name, bool interrupt = true)
    {
        if (!_special.TryGetValue(name, out var sound))
        {
            Debug.LogWarning($"[SoundManager] Special SE '{name}' 없음");
            return;
        }

        if (specialSeSource.isPlaying)
        {
            if (interrupt) specialSeSource.Stop();
            else return;
        }

        specialSeSource.clip = sound.clip;
        specialSeSource.volume = sound.volume;
        specialSeSource.pitch = sound.pitch;
        specialSeSource.loop = false;
        specialSeSource.Play();
    }

    public void StopSpecialSE() => specialSeSource.Stop();
    public bool IsSpecialSEPlaying => specialSeSource.isPlaying;

    // ─── Volume ─────────────────────────────────────
    public void SetBGMVolume(float v) => SetMixerVolume(MIXER_BGM, v);
    public void SetSEVolume(float v) => SetMixerVolume(MIXER_SE, v);
    public void SetSpecialSEVolume(float v) => SetMixerVolume(MIXER_SPECIAL, v);

    private void SetMixerVolume(string param, float linear01)
    {
        if (mixer == null) return;
        float dB = linear01 <= 0.0001f ? -80f : Mathf.Log10(linear01) * 20f;
        mixer.SetFloat(param, dB);
    }
}
