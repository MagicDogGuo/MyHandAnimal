using UnityEngine;

/// <summary>
/// 音效管理單例，負責 BGM 與共用 SFX。
///
/// 職責：
///   - BGM：循環播放背景音樂；可淡入淡出切換
///   - SFX：過關 / 失敗、左手張開鵝叫、小鵝／麵包拿起等（由各遊戲腳本呼叫）
///
/// Scene 設置：
///   1. 建立空 GameObject 命名 "AudioManager"，掛上此腳本
///   2. Inspector 拖入 bgmClip、clearClip、failClip、handOpenGooseHonkClip、littleGoosePickupClip、breadPickupClip
///   3. 選填：調整 bgmVolume / sfxVolume
///
/// 接線：
///   - GameManager.OnLevelClear → AudioManager.Instance.PlayClear()
///   - GameManager.OnFail       → AudioManager.Instance.PlayFail()
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ── Inspector ──────────────────────────────────────────────────────────
    [Header("BGM")]
    [Tooltip("背景音樂 AudioClip（循環播放）")]
    public AudioClip bgmClip;

    [Range(0f, 1f)]
    public float bgmVolume = 0.5f;

    [Tooltip("BGM 淡入 / 淡出時間（秒）；設為 0 則立即切換")]
    [Range(0f, 3f)]
    public float bgmFadeDuration = 1f;

    [Header("音效 (SFX)")]
    [Tooltip("過關音效")]
    public AudioClip clearClip;

    [Tooltip("失敗音效")]
    public AudioClip failClip;

    [Tooltip("左手張開到一定程度時的鵝叫（由 GooseHeadHandController 觸發）")]
    public AudioClip handOpenGooseHonkClip;

    [Tooltip("小鵝被拿起時")]
    public AudioClip littleGoosePickupClip;

    [Tooltip("麵包被拿起（吸附嘴前）")]
    public AudioClip breadPickupClip;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    // ── 私有 AudioSource ───────────────────────────────────────────────────
    private AudioSource _bgmSource;
    private AudioSource _sfxSource;

    private Coroutine _fadeCoroutine;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _bgmSource = CreateAudioSource("BGM", loop: true,  volume: bgmVolume);
        _sfxSource = CreateAudioSource("SFX", loop: false, volume: sfxVolume);
    }

    void Start()
    {
        PlayBGM(bgmClip);
    }

    // ── BGM ────────────────────────────────────────────────────────────────

    /// <summary>播放指定 BGM（帶淡入；若已播放相同曲目則忽略）。</summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        if (bgmFadeDuration > 0f)
            _fadeCoroutine = StartCoroutine(CrossFadeBGM(clip));
        else
        {
            _bgmSource.clip   = clip;
            _bgmSource.volume = bgmVolume;
            _bgmSource.Play();
        }
    }

    /// <summary>停止 BGM（帶淡出）。</summary>
    public void StopBGM()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        if (bgmFadeDuration > 0f)
            _fadeCoroutine = StartCoroutine(FadeOut(_bgmSource, bgmFadeDuration));
        else
            _bgmSource.Stop();
    }

    // ── SFX ────────────────────────────────────────────────────────────────

    /// <summary>播放過關音效（由 GameManager.OnLevelClear 呼叫）。</summary>
    public void PlayClear()
    {
        PlaySFX(clearClip);
    }

    /// <summary>播放失敗音效（由 GameManager.OnFail 呼叫）。</summary>
    public void PlayFail()
    {
        PlaySFX(failClip);
    }

    /// <summary>左手張開觸發的鵝叫（由 GooseHeadHandController 呼叫）。</summary>
    public void PlayHandOpenGooseHonk()
    {
        PlaySFX(handOpenGooseHonkClip);
    }

    /// <summary>小鵝被拿起（由 BreadSnapToMouth / LittleGoose 呼叫）。</summary>
    public void PlayLittleGoosePickup()
    {
        PlaySFX(littleGoosePickupClip);
    }

    /// <summary>麵包被拿起（由 BreadSnapToMouth 呼叫）。</summary>
    public void PlayBreadPickup()
    {
        PlaySFX(breadPickupClip);
    }

    /// <summary>一次性播放任意 SFX。</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // ── 音量控制 ────────────────────────────────────────────────────────────

    public void SetBGMVolume(float volume)
    {
        bgmVolume            = Mathf.Clamp01(volume);
        _bgmSource.volume    = bgmVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume            = Mathf.Clamp01(volume);
        _sfxSource.volume    = sfxVolume;
    }

    // ── 內部工具 ────────────────────────────────────────────────────────────

    private AudioSource CreateAudioSource(string sourceName, bool loop, float volume)
    {
        var go = new GameObject($"AudioSource_{sourceName}");
        go.transform.SetParent(transform);

        var src        = go.AddComponent<AudioSource>();
        src.loop       = loop;
        src.volume     = volume;
        src.playOnAwake = false;
        return src;
    }

    private System.Collections.IEnumerator CrossFadeBGM(AudioClip newClip)
    {
        // 淡出舊曲
        if (_bgmSource.isPlaying)
        {
            float half = bgmFadeDuration * 0.5f;
            yield return FadeOut(_bgmSource, half);
        }

        // 切換並淡入新曲
        _bgmSource.clip   = newClip;
        _bgmSource.volume = 0f;
        _bgmSource.Play();

        float elapsed = 0f;
        float fadeIn  = bgmFadeDuration * 0.5f;
        while (elapsed < fadeIn)
        {
            elapsed          += Time.unscaledDeltaTime;
            _bgmSource.volume = Mathf.Lerp(0f, bgmVolume, elapsed / fadeIn);
            yield return null;
        }
        _bgmSource.volume = bgmVolume;
    }

    private System.Collections.IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVol = source.volume;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed       += Time.unscaledDeltaTime;
            source.volume  = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }

        source.Stop();
        source.volume = startVol;
    }
}
