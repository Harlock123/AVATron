using System.Collections.Concurrent;

namespace Robotron.Infrastructure.Audio;

public enum MixMode
{
    /// One effect at a time; a new effect plays only if its priority is >= the one sounding.
    /// Approximates a single sound CPU driving one DAC (Classic).
    SingleChannelPriority,
    /// Several voices; under load the lowest-priority, oldest voice is stolen (Modern).
    Polyphonic,
}

public sealed record SoundClip(string Name, float[] Samples, int Priority, bool Loop = false);

/// Mixes effect voices and one ambient loop into mono float PCM. Thread-safe: game code calls Play/Stop
/// from the UI thread; the output thread calls Render. Never throws on unknown or missing clips.
public sealed class AudioMixer
{
    public const int SampleRate = 44100;
    const int MaxVoices = 12;

    readonly ConcurrentQueue<Action> _commands = new();
    /// Commands waiting for the output thread. Bounded so a missing or stalled output can't grow memory.
    int _pending;
    public const int MaxPendingCommands = 512;
    readonly Voice[] _voices = new Voice[MaxVoices];
    Voice _ambient;
    long _stamp;

    public volatile float MasterVolume = 0.8f;
    public volatile float EffectsVolume = 0.9f;
    public volatile float AmbientVolume = 0.6f;
    public volatile bool Muted;
    MixMode _mode = MixMode.Polyphonic;

    public MixMode Mode { get => _mode; set => Enqueue(() => { _mode = value; }); }

    /// Commands queued but not yet applied by Render (diagnostics/tests).
    public int PendingCommands => Volatile.Read(ref _pending);

    void Enqueue(Action a) { Interlocked.Increment(ref _pending); _commands.Enqueue(a); }

    /// Number of effect voices sounding after the last Render (diagnostics/tests).
    public int ActiveVoices { get; private set; }

    public void Play(SoundClip? clip, float gain = 1f)
    {
        if (clip is null || clip.Samples.Length == 0) return;
        if (Volatile.Read(ref _pending) >= MaxPendingCommands) return;   // no consumer: drop effects
        Enqueue(() => Start(clip, gain));
    }

    public void SetAmbient(SoundClip? clip) =>
        Enqueue(() => _ambient = clip is null ? default : new Voice { Clip = clip, Gain = 1f });

    public void StopAll() => Enqueue(() => { Array.Clear(_voices); _ambient = default; });
    public void StopEffects() => Enqueue(() => Array.Clear(_voices));

    void Start(SoundClip clip, float gain)
    {
        var v = new Voice { Clip = clip, Gain = gain, Stamp = ++_stamp };
        if (_mode == MixMode.SingleChannelPriority)
        {
            ref var only = ref _voices[0];
            if (only.Clip is null || clip.Priority >= only.Clip.Priority) only = v;
            return;
        }
        // Restart the same clip rather than stacking copies (prevents machine-gun clipping).
        for (int i = 0; i < _voices.Length; i++)
            if (ReferenceEquals(_voices[i].Clip, clip)) { _voices[i] = v; return; }
        int slot = -1;
        for (int i = 0; i < _voices.Length; i++)
        {
            if (_voices[i].Clip is null) { slot = i; break; }
            if (slot < 0 || Worse(_voices[i], _voices[slot])) slot = i;
        }
        if (_voices[slot].Clip is null || _voices[slot].Clip!.Priority <= clip.Priority) _voices[slot] = v;
    }

    static bool Worse(in Voice a, in Voice b) =>
        a.Clip!.Priority < b.Clip!.Priority || (a.Clip.Priority == b.Clip.Priority && a.Stamp < b.Stamp);

    public void Render(Span<float> output)
    {
        while (_commands.TryDequeue(out var cmd)) { Interlocked.Decrement(ref _pending); cmd(); }
        output.Clear();
        if (Muted) return;
        float fx = MasterVolume * EffectsVolume, amb = MasterVolume * AmbientVolume;
        int active = 0;
        for (int i = 0; i < _voices.Length; i++)
        {
            if (_voices[i].Clip is null) continue;
            Mix(ref _voices[i], output, fx);
            if (_voices[i].Clip is not null) active++;
        }
        if (_ambient.Clip is not null) Mix(ref _ambient, output, amb, forceLoop: true);
        ActiveVoices = active;
        // Soft clip: keeps heavy load from wrapping or hard-clipping.
        for (int i = 0; i < output.Length; i++)
        {
            float s = output[i];
            output[i] = s / (1f + MathF.Abs(s) * 0.5f) * 1.25f;
            if (output[i] > 1f) output[i] = 1f; else if (output[i] < -1f) output[i] = -1f;
        }
    }

    static void Mix(ref Voice v, Span<float> output, float volume, bool forceLoop = false)
    {
        var src = v.Clip!.Samples;
        float g = v.Gain * volume;
        bool loop = forceLoop || v.Clip.Loop;
        for (int i = 0; i < output.Length; i++)
        {
            if (v.Pos >= src.Length)
            {
                if (!loop) { v = default; return; }
                v.Pos = 0;
            }
            output[i] += src[v.Pos++] * g;
        }
    }

    struct Voice
    {
        public SoundClip? Clip;
        public int Pos;
        public float Gain;
        public long Stamp;
    }
}
