using UnityEngine;

/// <summary>
/// Central audio manager. Attach to a persistent GameObject in the scene.
/// All game sounds are generated at Start() so there's zero IO at runtime.
///
/// Usage: SoundManager.Instance.Play(SoundManager.SFX.BlockPlace);
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    // ── Tunable in Inspector ──────────────────────────────
    [Header("Volume")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume    = 1f;
    [Range(0f, 1f)] [SerializeField] private float bgmVolume    = 0.65f;

    [Header("Pooling")]
    [Tooltip("Number of AudioSource components to pool (allows overlapping sounds).")]
    [SerializeField] private int sourcePoolSize = 8;

    // ── Sound catalogue ───────────────────────────────────
    public enum SFX
    {
        BlockPickup,
        BlockPlace,
        BlockInvalid,
        LineClear,
        MultiLineClear,
        CellClear,
        GameOver,
        ScoreTick,
        // Element-specific
        FireExplode,
        IceShatter,
        LightningStrike,
        LevelComplete,
    }

    private AudioClip[] clips;
    private AudioSource[] pool;
    private int poolIndex = 0;

    private AudioSource bgmSource;
    private int currentMusicLevel = -1;
    private int randomMusicSeed; // Randomize start seed per game session

    // ─────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        randomMusicSeed = Random.Range(0, 10000);

        // Build BGM Source
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = masterVolume * bgmVolume;

        // Build audio pool
        pool = new AudioSource[sourcePoolSize];
        for (int i = 0; i < sourcePoolSize; i++)
        {
            var go = new GameObject($"AudioSource_{i}");
            go.transform.SetParent(transform);
            pool[i] = go.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
        }

        // Pre-generate all clips (runs once, ~1ms total)
        int count = System.Enum.GetValues(typeof(SFX)).Length;
        clips = new AudioClip[count];
        clips[(int)SFX.BlockPickup]     = ProceduralAudio.BlockPickup();
        clips[(int)SFX.BlockPlace]      = ProceduralAudio.BlockPlace();
        clips[(int)SFX.BlockInvalid]    = ProceduralAudio.BlockInvalid();
        clips[(int)SFX.LineClear]       = ProceduralAudio.LineClear();
        clips[(int)SFX.MultiLineClear]  = ProceduralAudio.MultiLineClear();
        clips[(int)SFX.CellClear]       = ProceduralAudio.CellClear();
        clips[(int)SFX.GameOver]        = ProceduralAudio.GameOver();
        clips[(int)SFX.ScoreTick]       = ProceduralAudio.ScoreTick();
        clips[(int)SFX.FireExplode]     = ProceduralAudio.FireExplode();
        clips[(int)SFX.IceShatter]      = ProceduralAudio.IceShatter();
        clips[(int)SFX.LightningStrike] = ProceduralAudio.LightningStrike();
        clips[(int)SFX.LevelComplete]   = ProceduralAudio.LevelComplete();

        ChangeMusicLevel(0);
    }

    // ─────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────

    /// <summary>Play a sound effect.</summary>
    public void Play(SFX sfx, float pitchVariance = 0.05f)
    {
        var clip = clips[(int)sfx];
        if (clip == null) return;

        var source = NextSource();
        source.clip   = clip;
        source.volume = masterVolume * sfxVolume;
        source.pitch  = 1f + Random.Range(-pitchVariance, pitchVariance);
        source.Play();
    }

    /// <summary>Play a line-clear sound, choosing multi vs single automatically.</summary>
    public void PlayLineClear(int lineCount)
    {
        Play(lineCount >= 2 ? SFX.MultiLineClear : SFX.LineClear);
    }

    /// <summary>Changes BGM based on score milestones.</summary>
    public void ChangeMusicLevel(int level)
    {
        if (currentMusicLevel == level) return;
        currentMusicLevel = level;
        StopAllCoroutines();
        StartCoroutine(CrossfadeMusic(level));
    }

    private System.Collections.IEnumerator CrossfadeMusic(int level)
    {
        float fadeTime = 1.0f;

        // 1. Fade out current track
        if (bgmSource.isPlaying)
        {
            float startVol = bgmSource.volume;
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
                yield return null;
            }
        }

        // Generate track synchronously (takes ~15ms)
        AudioClip nextBGM = MusicGenerator.GenerateLoop(randomMusicSeed + level);
        
        bgmSource.clip = nextBGM;
        bgmSource.Play();

        // 2. Fade in new track
        float targetVol = masterVolume * bgmVolume;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0f, targetVol, t / fadeTime);
            yield return null;
        }
        bgmSource.volume = targetVol;
    }

    // ─────────────────────────────────────────────────────────
    //  Internals
    // ─────────────────────────────────────────────────────────

    private AudioSource NextSource()
    {
        // Round-robin through pool — allows overlapping sounds
        var src = pool[poolIndex];
        poolIndex = (poolIndex + 1) % pool.Length;
        return src;
    }
}
