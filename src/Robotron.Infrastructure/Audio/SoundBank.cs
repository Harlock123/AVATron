using Robotron.Core.Simulation;

namespace Robotron.Infrastructure.Audio;

public enum Cue
{
    Laser, GruntMarch, GruntKill, EnemyKill, BrainKill, SpawnerKill, SmallKill, ElectrodeKill, HulkHit,
    Rescue, HumanKilled, Capture, ProgCreated, PlayerDeath, ExtraLife, WaveStart, WaveEnd, GameStart, GameOver,
    EnforcerAppear, SparkFire, TankAppear, ShellFire, ShellBounce, MissileFire, Drop, MenuMove, MenuSelect, AmbientHum,
}

/// All sound effects, synthesised at start-up (no samples). Priorities follow the arcade's single-channel
/// sequencer where the original source gives them (laser $D0, death $EE, wave end $E0, extra man $EF,
/// rescue $E0, human killed $E0, grunt hit $D0, grunt move $C0, start $F0); others are assigned by analogy.
/// Timbres are approximations designed by ear — see THIRD_PARTY.md / FIDELITY.md §Audio.
public sealed class SoundBank
{
    readonly Dictionary<Cue, SoundClip> _clips = [];
    public SoundClip? this[Cue c] => _clips.GetValueOrDefault(c);
    public IReadOnlyDictionary<Cue, SoundClip> All => _clips;

    public static SoundBank Build()
    {
        var b = new SoundBank();
        void Add(Cue c, int prio, Synth s, bool loop = false, float gain = 0.6f) =>
            b._clips[c] = new SoundClip(c.ToString(), s.Build(gain: gain), prio, loop);

        Add(Cue.Laser, 0xD0, new Synth().Pulse(40, 2200, 900, 0.8, 0.5, 0.3).Pulse(60, 900, 500, 0.5, 0.0, 0.3));
        Add(Cue.GruntMarch, 0xC0, new Synth().Noise(18, 2500, 900, 0.5, 0.0).Pulse(30, 110, 80, 0.45, 0.0), gain: 0.45f);
        Add(Cue.GruntKill, 0xD0, new Synth().Noise(70, 9000, 1500, 0.9, 0.5).Saw(130, 700, 90, 0.6, 0.0));
        Add(Cue.EnemyKill, 0xD0, new Synth().Noise(90, 7000, 1200, 0.9, 0.4).Pulse(140, 500, 70, 0.6, 0.0, 0.4));
        Add(Cue.BrainKill, 0xD4, new Synth().Saw(80, 200, 1600, 0.7, 0.7).Noise(160, 6000, 600, 0.8, 0.0));
        Add(Cue.SpawnerKill, 0xD4, new Synth().Pulse(60, 1500, 300, 0.8, 0.6, 0.25).Noise(220, 5000, 300, 0.9, 0.0));
        Add(Cue.SmallKill, 0xC8, new Synth().Noise(50, 12000, 4000, 0.6, 0.0).Pulse(30, 1200, 600, 0.4, 0.0));
        Add(Cue.ElectrodeKill, 0xD0, new Synth().Noise(30, 14000, 9000, 0.7, 0.3).Noise(80, 9000, 2500, 0.5, 0.0));
        Add(Cue.HulkHit, 0xD0, new Synth().Pulse(90, 240, 130, 0.8, 0.2, 0.5).Pulse(60, 130, 110, 0.3, 0.0, 0.5));

        var rescue = new Synth();
        for (int i = 0; i < 8; i++) rescue.Pulse(40, 300 + i * 110, 700 + i * 140, 0.7, 0.5, 0.5);
        Add(Cue.Rescue, 0xE0, rescue);

        var hk = new Synth();
        for (int i = 0; i < 6; i++) hk.Tri(60, 620 - i * 70, 520 - i * 70, 0.8, 0.5);
        Add(Cue.HumanKilled, 0xE0, hk);

        var cap = new Synth();
        for (int i = 0; i < 6; i++) cap.Pulse(25, 180, 260, 0.5, 0.4, 0.2).Pulse(25, 260, 180, 0.4, 0.3, 0.2);
        Add(Cue.Capture, 0xD8, cap);
        var prog = new Synth();
        for (int i = 0; i < 10; i++) prog.Saw(30, 150 + (i % 3) * 300, 900 - (i % 2) * 400, 0.6, 0.5);
        Add(Cue.ProgCreated, 0xDC, prog);

        Add(Cue.PlayerDeath, 0xEE, new Synth().Pulse(130, 1600, 200, 0.9, 0.7, 0.5).Pulse(130, 1400, 150, 0.8, 0.6, 0.5)
            .Noise(550, 6000, 150, 1.0, 0.0).Saw(500, 300, 40, 0.5, 0.0));
        Add(Cue.ExtraLife, 0xEF, new Synth().Notes(70, 0.7, 0.5, 523, 659, 784, 1047, 784, 1047, 1319).Pulse(200, 1568, 1568, 0.7, 0.0, 0.5));
        Add(Cue.WaveStart, 0xE4, new Synth().Saw(450, 80, 1800, 0.3, 0.8).Pulse(80, 1800, 1800, 0.5, 0.0, 0.5), gain: 0.5f);
        var wend = new Synth();
        for (int i = 0; i < 12; i++) wend.Pulse(66, 200 + i * 80, 900 + i * 120, 0.7, 0.4, 0.5);
        Add(Cue.WaveEnd, 0xE0, wend);
        Add(Cue.GameStart, 0xF0, new Synth().Notes(90, 0.7, 0.5, 392, 523, 659, 784).Pulse(250, 1047, 1047, 0.7, 0.0, 0.5));
        Add(Cue.GameOver, 0xF0, new Synth().Notes(160, 0.7, 0.5, 392, 330, 262, 196).Pulse(350, 131, 98, 0.7, 0.0, 0.5));

        Add(Cue.EnforcerAppear, 0xC4, new Synth().Pulse(90, 300, 1300, 0.5, 0.3, 0.25), gain: 0.45f);
        Add(Cue.SparkFire, 0xC6, new Synth().Pulse(55, 3200, 1500, 0.5, 0.0, 0.2), gain: 0.4f);
        Add(Cue.TankAppear, 0xC4, new Synth().Pulse(120, 120, 400, 0.6, 0.3, 0.5), gain: 0.45f);
        Add(Cue.ShellFire, 0xC6, new Synth().Noise(30, 3000, 800, 0.6, 0.2).Pulse(60, 180, 90, 0.6, 0.0), gain: 0.45f);
        Add(Cue.ShellBounce, 0xC2, new Synth().Tri(50, 1400, 1100, 0.6, 0.0), gain: 0.4f);
        Add(Cue.MissileFire, 0xC6, new Synth().Saw(70, 400, 2400, 0.5, 0.2), gain: 0.4f);
        Add(Cue.Drop, 0xC4, new Synth().Pulse(60, 900, 300, 0.5, 0.0, 0.5), gain: 0.4f);
        Add(Cue.MenuMove, 0x80, new Synth().Pulse(25, 900, 900, 0.4, 0.0, 0.5), gain: 0.35f);
        Add(Cue.MenuSelect, 0x80, new Synth().Pulse(40, 700, 1400, 0.5, 0.0, 0.5), gain: 0.4f);

        // Modern addition: seamless 2 s hum (integer cycles of 55 Hz and the 0.5 Hz swell).
        var hum = new float[AudioMixer.SampleRate * 2];
        for (int i = 0; i < hum.Length; i++)
        {
            double t = i / (double)AudioMixer.SampleRate;
            double swell = 0.6 + 0.4 * Math.Sin(2 * Math.PI * 0.5 * t);
            hum[i] = (float)(0.18 * swell * (Math.Sin(2 * Math.PI * 55 * t) + 0.4 * Math.Sin(2 * Math.PI * 110 * t + 0.3)));
        }
        b._clips[Cue.AmbientHum] = new SoundClip("AmbientHum", hum, 0, Loop: true);
        return b;
    }

    /// Maps a simulation event to a sound cue (null = silent).
    public static Cue? CueFor(in GameEvent e) => e.Kind switch
    {
        GameEventKind.PlayerShot => Cue.Laser,
        GameEventKind.GruntsMarched => Cue.GruntMarch,
        GameEventKind.EnemyKilled => e.Entity switch
        {
            EntityKind.Grunt or EntityKind.Prog => Cue.GruntKill,
            EntityKind.Brain => Cue.BrainKill,
            EntityKind.Sphereoid or EntityKind.Quark => Cue.SpawnerKill,
            EntityKind.Spark or EntityKind.Shell or EntityKind.CruiseMissile => Cue.SmallKill,
            _ => Cue.EnemyKill,
        },
        GameEventKind.ElectrodeDestroyed => Cue.ElectrodeKill,
        GameEventKind.HulkHit => Cue.HulkHit,
        GameEventKind.HumanRescued => Cue.Rescue,
        GameEventKind.HumanKilled => Cue.HumanKilled,
        GameEventKind.BrainCapturing => Cue.Capture,
        GameEventKind.ProgCreated => Cue.ProgCreated,
        GameEventKind.PlayerDied => Cue.PlayerDeath,
        GameEventKind.ExtraLife => Cue.ExtraLife,
        GameEventKind.WaveStarting => Cue.WaveStart,
        GameEventKind.WaveCleared => Cue.WaveEnd,
        GameEventKind.GameOver => Cue.GameOver,
        GameEventKind.EnforcerAppears => Cue.EnforcerAppear,
        GameEventKind.SparkFired => Cue.SparkFire,
        GameEventKind.TankAppears => Cue.TankAppear,
        GameEventKind.ShellFired => Cue.ShellFire,
        GameEventKind.ShellBounce => Cue.ShellBounce,
        GameEventKind.MissileFired => Cue.MissileFire,
        GameEventKind.SphereoidDrop or GameEventKind.QuarkDrop => Cue.Drop,
        _ => null,
    };
}
