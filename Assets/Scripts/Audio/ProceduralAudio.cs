using System;
using UnityEngine;

/// <summary>
/// Generates AudioClips procedurally at runtime — no external audio files needed.
/// Inspired by BFXR/SFXR chiptune synthesis.
/// </summary>
public static class ProceduralAudio
{
    private const int SampleRate = 44100;

    // ─────────────────────────────────────────────────────────
    // Public factory methods
    // ─────────────────────────────────────────────────────────

    /// <summary>Short pop — block placed on board.</summary>
    public static AudioClip BlockPlace()
        => Synth("blk_place", duration: 0.12f, startFreq: 480f, endFreq: 280f,
                 waveform: Waveform.Square, attack: 0.01f, decay: 0.11f,
                 volume: 0.55f);

    /// <summary>Soft click — block picked up.</summary>
    public static AudioClip BlockPickup()
        => Synth("blk_pickup", duration: 0.08f, startFreq: 600f, endFreq: 700f,
                 waveform: Waveform.Sine, attack: 0.005f, decay: 0.075f,
                 volume: 0.4f);

    /// <summary>Invalid placement — low thud.</summary>
    public static AudioClip BlockInvalid()
        => Synth("blk_invalid", duration: 0.15f, startFreq: 140f, endFreq: 80f,
                 waveform: Waveform.Noise, attack: 0.005f, decay: 0.14f,
                 volume: 0.45f, noiseAmount: 0.9f);

    /// <summary>Single line cleared — bright rising chime.</summary>
    public static AudioClip LineClear()
        => Synth("line_clear", duration: 0.25f, startFreq: 520f, endFreq: 1100f,
                 waveform: Waveform.Sine, attack: 0.01f, decay: 0.24f,
                 volume: 0.7f, harmonics: true);

    /// <summary>Multiple lines cleared at once — fuller chord sweep.</summary>
    public static AudioClip MultiLineClear()
        => SynthChord("multi_clear", duration: 0.38f,
                      freqs: new[] { 520f, 660f, 780f }, endFreqMult: 1.8f,
                      waveform: Waveform.Sine, attack: 0.01f, decay: 0.37f,
                      volume: 0.75f);

    /// <summary>Soft click when a cell is cleared individually (Fire/Lightning).</summary>
    public static AudioClip CellClear()
        => Synth("cell_clear", duration: 0.09f, startFreq: 700f, endFreq: 400f,
                 waveform: Waveform.Sine, attack: 0.005f, decay: 0.085f,
                 volume: 0.35f);

    /// <summary>Game over — descending sad tone.</summary>
    public static AudioClip GameOver()
        => Synth("game_over", duration: 0.6f, startFreq: 330f, endFreq: 110f,
                 waveform: Waveform.Square, attack: 0.02f, decay: 0.58f,
                 volume: 0.6f, harmonics: false, pitchVibrato: 0f);

    /// <summary>Score popup — tiny tick.</summary>
    public static AudioClip ScoreTick()
        => Synth("score_tick", duration: 0.06f, startFreq: 900f, endFreq: 900f,
                 waveform: Waveform.Sine, attack: 0.002f, decay: 0.058f,
                 volume: 0.25f);

    // ─── Element-specific SFX ───────────────────────────────

    /// <summary>Fire explosion — low noise boom with pitch drop.</summary>
    public static AudioClip FireExplode()
        => Synth("fire_explode", duration: 0.30f, startFreq: 220f, endFreq: 60f,
                 waveform: Waveform.Noise, attack: 0.005f, decay: 0.295f,
                 volume: 0.65f, noiseAmount: 0.75f);

    /// <summary>Ice shatter — triangle wave crackling high.</summary>
    public static AudioClip IceShatter()
        => Synth("ice_shatter", duration: 0.22f, startFreq: 1400f, endFreq: 500f,
                 waveform: Waveform.Triangle, attack: 0.005f, decay: 0.215f,
                 volume: 0.55f, harmonics: true);

    /// <summary>Lightning strike — fast electric zap.</summary>
    public static AudioClip LightningStrike()
        => Synth("lightning_strike", duration: 0.18f, startFreq: 800f, endFreq: 200f,
                 waveform: Waveform.Square, attack: 0.003f, decay: 0.177f,
                 volume: 0.60f, noiseAmount: 0.4f);

    // ─────────────────────────────────────────────────────────
    // Core synthesizer
    // ─────────────────────────────────────────────────────────

    private enum Waveform { Sine, Square, Triangle, Noise }

    private static AudioClip Synth(
        string name, float duration,
        float startFreq, float endFreq,
        Waveform waveform,
        float attack, float decay,
        float volume = 0.5f,
        float noiseAmount = 0f,
        bool harmonics = false,
        float pitchVibrato = 0f)
    {
        int samples = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t    = (float)i / SampleRate;          // time in seconds
            float prog = (float)i / samples;              // 0→1 progress

            // Pitch envelope: linear interpolation startFreq→endFreq
            float freq = Mathf.Lerp(startFreq, endFreq, prog);

            // Optional light vibrato
            if (pitchVibrato > 0f)
                freq += Mathf.Sin(t * 12f) * pitchVibrato;

            // Phase accumulation (more accurate than t*freq for varying freq)
            // We approximate with instantaneous freq here (simple & fine for sfx)
            float phase = t * freq;

            float sample = GenerateSample(waveform, phase);

            // Harmonics (adds 2nd + 3rd overtone)
            if (harmonics)
                sample = sample * 0.6f
                        + GenerateSample(waveform, phase * 2f) * 0.25f
                        + GenerateSample(waveform, phase * 3f) * 0.15f;

            // Noise blend
            if (noiseAmount > 0f)
                sample = Mathf.Lerp(sample, UnityEngine.Random.Range(-1f, 1f), noiseAmount);

            // Amplitude envelope
            float amp = AmplitudeEnvelope(prog, attack / duration, decay / duration);

            data[i] = sample * amp * volume;
        }

        return BuildClip(name, data);
    }

    private static AudioClip SynthChord(
        string name, float duration,
        float[] freqs, float endFreqMult,
        Waveform waveform,
        float attack, float decay,
        float volume = 0.5f)
    {
        int samples = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[samples];
        float layerVol = volume / freqs.Length;

        foreach (float startFreq in freqs)
        {
            for (int i = 0; i < samples; i++)
            {
                float t    = (float)i / SampleRate;
                float prog = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, startFreq * endFreqMult, prog);
                float sample = GenerateSample(waveform, t * freq);
                float amp    = AmplitudeEnvelope(prog, attack / duration, decay / duration);
                data[i] += sample * amp * layerVol;
            }
        }

        // Clamp to prevent clipping
        for (int i = 0; i < data.Length; i++)
            data[i] = Mathf.Clamp(data[i], -1f, 1f);

        return BuildClip(name, data);
    }

    // ─────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────

    private static float GenerateSample(Waveform waveform, float phase)
    {
        float frac = phase - Mathf.Floor(phase); // 0..1 within cycle
        return waveform switch
        {
            Waveform.Sine     => Mathf.Sin(phase * 2f * Mathf.PI),
            Waveform.Square   => frac < 0.5f ? 1f : -1f,
            Waveform.Triangle => frac < 0.5f ? (4f * frac - 1f) : (3f - 4f * frac),
            Waveform.Noise    => UnityEngine.Random.Range(-1f, 1f),
            _                 => 0f,
        };
    }

    /// <summary>
    /// Simple attack / sustain / decay envelope.
    /// attackNorm and decayNorm are fractions of total clip duration.
    /// </summary>
    private static float AmplitudeEnvelope(float prog, float attackNorm, float decayNorm)
    {
        if (prog < attackNorm)
            return prog / attackNorm;                         // ramp up
        float sustain = 1f - attackNorm - decayNorm;
        if (sustain > 0f && prog < attackNorm + sustain)
            return 1f;                                        // flat sustain
        float decayStart = 1f - decayNorm;
        return Mathf.Clamp01((1f - prog) / Mathf.Max(decayNorm, 0.001f));  // ramp down
    }

    private static AudioClip BuildClip(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
