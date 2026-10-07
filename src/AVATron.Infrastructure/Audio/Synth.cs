namespace AVATron.Infrastructure.Audio;

/// Small offline synthesis toolkit. Sounds are rendered once at start-up into float buffers.
/// Everything is generated from code; no sampled audio is used (see THIRD_PARTY.md).
public sealed class Synth
{
    public const int Rate = AudioMixer.SampleRate;
    readonly List<float> _buf = [];
    uint _lfsr = 0xACE1u;

    public static int Ms(double ms) => (int)(Rate * ms / 1000.0);

    /// Square/pulse wave with exponential frequency sweep and linear amplitude envelope.
    public Synth Pulse(double ms, double f0, double f1, double a0, double a1, double duty = 0.5)
    {
        int n = Ms(ms); double phase = 0;
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / n;
            double f = f0 * Math.Pow(f1 / f0, t);
            phase = (phase + f / Rate) % 1.0;
            _buf.Add((float)((phase < duty ? 1 : -1) * Lerp(a0, a1, t)));
        }
        return this;
    }

    /// Sawtooth sweep (the DAC "ramp" sounds).
    public Synth Saw(double ms, double f0, double f1, double a0, double a1)
    {
        int n = Ms(ms); double phase = 0;
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / n;
            phase = (phase + f0 * Math.Pow(f1 / f0, t) / Rate) % 1.0;
            _buf.Add((float)((phase * 2 - 1) * Lerp(a0, a1, t)));
        }
        return this;
    }

    /// Triangle sweep.
    public Synth Tri(double ms, double f0, double f1, double a0, double a1)
    {
        int n = Ms(ms); double phase = 0;
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / n;
            phase = (phase + f0 * Math.Pow(f1 / f0, t) / Rate) % 1.0;
            _buf.Add((float)((1 - 4 * Math.Abs(phase - 0.5)) * Lerp(a0, a1, t)));
        }
        return this;
    }

    /// LFSR noise with a sample-and-hold rate sweep (lower rate = darker rumble).
    public Synth Noise(double ms, double rate0, double rate1, double a0, double a1)
    {
        int n = Ms(ms); double acc = 0; float held = 0;
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / n;
            acc += rate0 * Math.Pow(rate1 / rate0, t) / Rate;
            while (acc >= 1) { acc -= 1; held = NextNoise(); }
            _buf.Add((float)(held * Lerp(a0, a1, t)));
        }
        return this;
    }

    /// Repeating arpeggio of pulse notes (jingles, fanfares).
    public Synth Notes(double noteMs, double amp, double duty, params double[] freqs)
    {
        foreach (var f in freqs)
            if (f <= 0) Silence(noteMs); else Pulse(noteMs, f, f, amp, amp * 0.6, duty);
        return this;
    }

    public Synth Silence(double ms) { _buf.AddRange(new float[Ms(ms)]); return this; }

    /// Quantise to 8-bit DAC steps, as the arcade sound board's DAC would.
    public float[] Build(bool dac8 = true, float gain = 0.6f)
    {
        var a = _buf.ToArray();
        for (int i = 0; i < a.Length; i++)
        {
            float s = Math.Clamp(a[i] * gain, -1f, 1f);
            a[i] = dac8 ? MathF.Round(s * 127f) / 127f : s;
        }
        // 2 ms fade-out to avoid end clicks
        int fade = Math.Min(a.Length, Ms(2));
        for (int i = 0; i < fade; i++) a[a.Length - 1 - i] *= (float)i / fade;
        return a;
    }

    float NextNoise()
    {
        uint bit = ((_lfsr >> 0) ^ (_lfsr >> 2) ^ (_lfsr >> 3) ^ (_lfsr >> 5)) & 1;
        _lfsr = (_lfsr >> 1) | (bit << 15);
        return (_lfsr & 1) == 1 ? 1f : -1f;
    }

    static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
