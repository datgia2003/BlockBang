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
        => Synth("fire_explode", duration: 0.40f, startFreq: 280f, endFreq: 40f,
                 waveform: Waveform.Noise, attack: 0.005f, decay: 0.395f,
                 volume: 1.0f, noiseAmount: 0.95f);

    /// <summary>Ice shatter — heavy crunch with bright high end.</summary>
    public static AudioClip IceShatter()
        => Synth("ice_shatter", duration: 0.35f, startFreq: 1600f, endFreq: 400f,
                 waveform: Waveform.Triangle, attack: 0.005f, decay: 0.345f,
                 volume: 0.9f, harmonics: true, noiseAmount: 0.15f);

    /// <summary>Lightning strike — fast electrical zap.</summary>
    public static AudioClip LightningStrike()
        => Synth("lightning_strike", duration: 0.25f, startFreq: 1200f, endFreq: 150f,
                 waveform: Waveform.Square, attack: 0.003f, decay: 0.247f,
                 volume: 0.9f, noiseAmount: 0.6f);

    /// <summary>Level complete — triumphant rising melody.</summary>
    public static AudioClip LevelComplete()
        => SynthChord("lvl_complete", duration: 0.8f,
                      freqs: new[] { 523.25f, 659.25f, 783.99f }, endFreqMult: 1.2f, // C major chord rising
                      waveform: Waveform.Sine, attack: 0.05f, decay: 0.75f,
                      volume: 0.8f);

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

public static class MusicGenerator
{
    private const int SampleRate = 44100;

    /// <summary>
    /// Generates a looping Chiptune-style music track procedurally.
    /// Uses the given seed to ensure the song remains consistent for that track ID.
    /// </summary>
    public static AudioClip GenerateLoop(int seed)
    {
        UnityEngine.Random.InitState(seed);

        // Musical parameters
        float bpm = UnityEngine.Random.Range(100f, 150f);
        float stepDur = 60f / bpm / 2f; // 8th notes
        int steps = 32; // 4 bars of 4/4
        float loopDur = steps * stepDur;

        int samples = Mathf.CeilToInt(SampleRate * loopDur);
        float[] mix = new float[samples];

        // Scales (C Minor Pentatonic variations)
        float[] minorScale = { 130.81f, 155.56f, 174.61f, 196.00f, 233.08f, 261.63f, 311.13f, 349.23f, 392.00f, 466.16f, 523.25f };
        int root = UnityEngine.Random.Range(0, 5); // shift root

        // --- Track 1: Kick Drum (4 on the floor or syncopated) ---
        bool[] kickPattern = new bool[steps];
        for (int i = 0; i < steps; i += 4) kickPattern[i] = true; 
        if (UnityEngine.Random.value > 0.5f) kickPattern[10] = true;

        for (int i = 0; i < steps; i++)
        {
            if (kickPattern[i])
            {
                AddSynth(mix, i * stepDur, 0.15f, 150f, 40f, 0f, 0.01f, 0.14f, 0.6f);
            }
        }

        // --- Track 2: Bassline (16th note arps or bouncy) ---
        int bassOffset = root;
        bool[] bassActive = new bool[steps];
        int[] bassNotes = new int[steps];
        for (int i = 0; i < steps; i++)
        {
            if (UnityEngine.Random.value > 0.3f) 
            {
                bassActive[i] = true;
                bassNotes[i] = bassOffset + UnityEngine.Random.Range(0, 4);
                if (UnityEngine.Random.value > 0.8f) bassNotes[i] += 5; // jump octave
            }
        }

        for (int i = 0; i < steps; i++)
        {
            if (bassActive[i])
            {
                float freq = minorScale[bassNotes[i] % minorScale.Length] * 0.5f; // pitch down
                AddSynth(mix, i * stepDur, 0.15f, freq, freq, 1f, 0.02f, 0.12f, 0.4f); // Square wave
            }
        }

        // --- Track 3: Lead/Arp ---
        int leadOffset = root + 3;
        int melodySpeed = UnityEngine.Random.Range(1, 3); // 1 = every 8th, 2 = every quarter
        for (int i = 0; i < steps; i += melodySpeed)
        {
            if (UnityEngine.Random.value > 0.2f)
            {
                int noteIdx = leadOffset + UnityEngine.Random.Range(0, minorScale.Length - leadOffset);
                float freq = minorScale[noteIdx % minorScale.Length] * 2f; // pitch up
                float len = stepDur * UnityEngine.Random.Range(0.8f, 2.5f);
                
                // 30% chance for Triangle, 70% for Sine
                float waveType = UnityEngine.Random.value > 0.3f ? 3f : 2f; 
                
                AddSynth(mix, i * stepDur, len, freq, freq, waveType, 0.05f, len * 0.8f, 0.2f);
            }
        }
        
        // --- Track 4: Hi-hats ---
        for (int i = 0; i < steps; i++)
        {
            if (i % 2 != 0 || UnityEngine.Random.value > 0.7f) // Upbeats + random
            {
                AddSynth(mix, i * stepDur, 0.05f, 800f, 800f, 4f, 0.01f, 0.04f, 0.1f); // Noise
            }
        }

        // Clamp & normalize
        float maxVal = 0.01f;
        for (int i = 0; i < mix.Length; i++) maxVal = Mathf.Max(maxVal, Mathf.Abs(mix[i]));
        for (int i = 0; i < mix.Length; i++) mix[i] = Mathf.Clamp(mix[i] / maxVal * 0.8f, -1f, 1f); // leaves headroom

        // Restore random context
        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
        
        var clip = AudioClip.Create("OST_" + seed, mix.Length, 1, SampleRate, false);
        clip.SetData(mix, 0);
        return clip;
    }

    /// <summary>
    /// waveType: 0=Noise (Drum), 1=Square(Bass), 2=Sine, 3=Triangle, 4=Noise
    /// </summary>
    private static void AddSynth(float[] mix, float startTime, float duration, 
                                 float startFreq, float endFreq, float waveType, 
                                 float attack, float decay, float volume)
    {
        int startSample = Mathf.FloorToInt(startTime * SampleRate);
        int samples = Mathf.CeilToInt(duration * SampleRate);
        
        for (int i = 0; i < samples; i++)
        {
            int mixIdx = startSample + i;
            if (mixIdx >= mix.Length) break; // wrap around could be done, but keeping it simple

            float t = (float)i / SampleRate;
            float prog = (float)i / samples;
            float freq = Mathf.Lerp(startFreq, endFreq, prog);
            float phase = t * freq;

            float sample = 0f;
            float frac = phase - Mathf.Floor(phase);

            if (waveType == 0f || waveType == 4f) sample = UnityEngine.Random.Range(-1f, 1f); // Noise
            else if (waveType == 1f) sample = frac < 0.5f ? 1f : -1f; // Square
            else if (waveType == 2f) sample = Mathf.Sin(phase * 2f * Mathf.PI); // Sine
            else if (waveType == 3f) sample = frac < 0.5f ? (4f * frac - 1f) : (3f - 4f * frac); // Tri

            // Envelope
            float amp = 1f;
            float attProg = t / attack;
            if (attProg < 1f) amp = attProg;
            else 
            {
                float decProg = (t - attack) / decay;
                if (decProg < 1f) amp = 1f - decProg;
                else amp = 0f;
            }

            mix[mixIdx] += sample * amp * volume;
        }
    }
}
