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
    }

    private AudioClip[] clips;
    private AudioSource[] pool;
    private int poolIndex = 0;

    // ─────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
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
