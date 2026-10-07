using AVATron.Core.Entities;
using AVATron.Core.Input;
using AVATron.Core.Waves;

namespace AVATron.Core.Simulation;

public enum GamePhase { WaveIntro, Playing, PlayerDying, WaveCleared, GameOver, Finished }

/// Where a wave (re)start begins. Restoring a checkpoint and replaying the recorded inputs reproduces a
/// game exactly — this is how Modern suspend/resume works.
public sealed record WaveCheckpoint(
    int Wave, long Score, int Reserve, long NextBonus, ulong RngState,
    int RobSpd, int RobMax, WaveCounts? Remaining, bool IsRestart);

/// One game from first wave to game over. Owns the score, lives and wave flow; delegates the playfield to World.
public sealed class GameSession
{
    readonly WaveTable _table;
    readonly IGameRandom _rng;
    readonly List<GameEvent> _events = [];
    readonly List<TickInput> _inputsSinceCheckpoint = [];

    public GameSession(GameRules rules, WaveTable table, IGameRandom rng)
    {
        Rules = rules; _table = table; _rng = rng;
        Score = 0;
        Reserve = rules.LivesPerGame - 1;
        NextBonus = rules.ExtraLifeEvery > 0 ? rules.ExtraLifeEvery : long.MaxValue;
        BeginWave(new WaveCheckpoint(Math.Max(1, rules.StartWave), 0, Reserve, NextBonus, _rng.State, -1, -1, null, false));
    }

    GameSession(GameRules rules, WaveTable table, IGameRandom rng, WaveCheckpoint cp)
    {
        Rules = rules; _table = table; _rng = rng;
        BeginWave(cp);
    }

    /// Bump whenever simulation behaviour changes, so stale suspend saves are rejected rather than replayed wrongly.
    public const int EngineVersion = 1;

    public GameRules Rules { get; }
    public long Score { get; private set; }
    /// Men in reserve (not counting the one in play).
    public int Reserve { get; private set; }
    public long NextBonus { get; private set; }
    public int Wave { get; private set; }
    public GamePhase Phase { get; private set; }
    /// Frames spent in the current phase.
    public int PhaseFrames { get; private set; }
    public int PhaseLength { get; private set; }
    public World World { get; private set; } = null!;
    /// Counts placed at the latest (re)start of this wave (survivors only after a death).
    public WaveCounts WaveStartCounts { get; private set; } = null!;
    /// The wave's full counts from the table, unaffected by deaths (used for progress display).
    public WaveCounts WaveFullCounts { get; private set; } = null!;
    public bool IsBrainWave { get; private set; }
    public long TickCount { get; private set; }
    public IReadOnlyList<GameEvent> Events => _events;
    public WaveCheckpoint Checkpoint { get; private set; } = null!;
    public IReadOnlyList<TickInput> InputsSinceCheckpoint => _inputsSinceCheckpoint;

    // Durations in frames (RESEARCH.md §Death/§Wave start; marquee and appear lengths are reconstructions).
    public const int DeathFlashFrames = 80, DeathFadeFrames = 28, GameOverFrames = 120;
    public const int AppearFrames = 16, PlayerAppearFrames = 10, BrainWaveIntroFrames = 150, WaveClearFrames = 72;
    public static int DeathFrames => DeathFlashFrames + DeathFadeFrames;

    public void Tick(TickInput input)
    {
        _events.Clear();
        _inputsSinceCheckpoint.Add(input);
        TickCount++;
        PhaseFrames++;
        switch (Phase)
        {
            case GamePhase.WaveIntro:
                World.AgeEffects();
                if (PhaseFrames >= PhaseLength)
                {
                    World.Activate();
                    SetPhase(GamePhase.Playing, 0);
                    _events.Add(new GameEvent(GameEventKind.WaveActive, Value: Wave));
                }
                break;

            case GamePhase.Playing:
                World.Step(input);
                _events.AddRange(World.Events);
                AddPoints(World.PointsThisTick);
                if (World.PlayerHit)
                {
                    SetPhase(GamePhase.PlayerDying, DeathFrames);
                    _events.Add(new GameEvent(GameEventKind.PlayerDied, World.Player.CenterX, World.Player.CenterY));
                }
                else if (World.WaveComplete)
                {
                    SetPhase(GamePhase.WaveCleared, WaveClearFrames);
                    _events.Add(new GameEvent(GameEventKind.WaveCleared, Value: Wave));
                }
                break;

            case GamePhase.PlayerDying:
                World.AgeEffects();
                if (PhaseFrames >= PhaseLength)
                {
                    if (Reserve <= 0)
                    {
                        SetPhase(GamePhase.GameOver, GameOverFrames);
                        _events.Add(new GameEvent(GameEventKind.GameOver, Value: Wave));
                    }
                    else
                    {
                        Reserve--;
                        BeginWave(new WaveCheckpoint(Wave, Score, Reserve, NextBonus, _rng.State, World.RobSpd, World.RobMax, RemainingAfterDeath(), true));
                    }
                }
                break;

            case GamePhase.WaveCleared:
                if (PhaseFrames >= PhaseLength)
                    BeginWave(new WaveCheckpoint(NextWave(Wave), Score, Reserve, NextBonus, _rng.State, -1, -1, null, false));
                break;

            case GamePhase.GameOver:
                if (PhaseFrames >= PhaseLength) SetPhase(GamePhase.Finished, 0);
                break;
        }
    }

    /// Wave counter is a byte in the arcade: after 255 it skips 0 and continues at 1.
    static int NextWave(int w) => w >= 255 ? 1 : w + 1;

    void SetPhase(GamePhase p, int length) { Phase = p; PhaseFrames = 0; PhaseLength = length; }

    void AddPoints(int points)
    {
        if (points <= 0) return;
        Score += points;
        while (Score >= NextBonus)
        {
            Reserve++;
            NextBonus += Rules.ExtraLifeEvery;
            _events.Add(new GameEvent(GameEventKind.ExtraLife, Value: Reserve));
        }
    }

    void BeginWave(WaveCheckpoint cp)
    {
        Checkpoint = cp;
        _inputsSinceCheckpoint.Clear();
        _rng.State = cp.RngState;
        Wave = cp.Wave; Score = cp.Score; Reserve = cp.Reserve; NextBonus = cp.NextBonus;

        var def = _table.Raw(Wave);
        var p = _table.Effective(Wave, Rules.Difficulty, Reserve, Rules.LivesPerGame);
        World = new World(_rng, p, Wave, Rules);
        // After a death the grunts keep the speed they had reached, unless the last-life assist applies.
        if (cp.IsRestart && cp.RobMax > 0 && !_table.IsBozo(Wave, Reserve, Rules.LivesPerGame))
        {
            World.RobMax = cp.RobMax;
            World.RobSpd = Math.Max(cp.RobSpd, cp.RobMax);
        }
        WaveStartCounts = cp.Remaining ?? def.Counts;
        WaveFullCounts = def.Counts;
        WaveBuilder.Populate(World, WaveStartCounts);

        IsBrainWave = def.Counts.Brains > 0;
        int robots = World.Enemies.Count(e => e is Grunt or Hulk or Brain);
        SetPhase(GamePhase.WaveIntro, IsBrainWave ? BrainWaveIntroFrames : robots + AppearFrames + PlayerAppearFrames);
        _events.Add(new GameEvent(GameEventKind.WaveStarting, Value: Wave));
    }

    /// Counts carried into the restarted wave. Enforcers fold back into Sphereoids (one per four, at least one,
    /// capped at the wave's original count); Tanks fold back into Quarks the same way (reconstruction).
    /// Progs and all projectiles are lost.
    WaveCounts RemainingAfterDeath()
    {
        var w = World;
        int Count(EntityKind k) => w.Enemies.Count(e => !e.Dead && e.Kind == k);
        int Fam(EntityKind k) => w.Family.Count(h => !h.Dead && h.Kind == k);
        var orig = _table.Raw(Wave).Counts;
        int Fold(int spawners, int children, int cap)
        {
            int extra = children / 4;
            if (spawners == 0 && children > 0) extra = Math.Max(1, extra);
            return Math.Min(spawners + extra, Math.Max(cap, spawners));
        }
        return new WaveCounts(
            Count(EntityKind.Grunt), w.Electrodes.Count(e => !e.Dead),
            Fam(EntityKind.Mommy), Fam(EntityKind.Daddy), Fam(EntityKind.Mikey),
            Count(EntityKind.Hulk), Count(EntityKind.Brain),
            Fold(Count(EntityKind.Sphereoid), Count(EntityKind.Enforcer), orig.Sphereoids),
            Fold(Count(EntityKind.Quark), Count(EntityKind.Tank), orig.Quarks));
    }

    /// Cheap fingerprint of the observable state, used to verify that a resumed replay matches.
    public long StateHash()
    {
        long h = Score * 31 + Wave * 7 + Reserve * 131 + (long)Phase * 17 + PhaseFrames;
        foreach (var e in World.Enemies) h = h * 1_000_003 + e.X * 7 + e.Y + (long)e.Kind;
        foreach (var f in World.Family) h = h * 1_000_003 + f.X * 5 + f.Y;
        return h ^ World.Player.X * 13 ^ (long)World.Player.Y << 20;
    }

    // ---------------- suspend / resume ----------------
    public SuspendState CreateSuspendState() => new(Rules, Checkpoint, RunLength.Encode(_inputsSinceCheckpoint), TickCount - _inputsSinceCheckpoint.Count);

    /// Rebuilds a session by restoring the checkpoint and replaying the recorded inputs.
    public static GameSession Resume(SuspendState s, WaveTable table, IGameRandom rng)
    {
        var g = new GameSession(s.Rules, table, rng, s.Checkpoint) { TickCount = s.TicksBeforeCheckpoint };
        foreach (var input in RunLength.Decode(s.Inputs)) g.Tick(input);
        return g;
    }
}

public sealed record SuspendState(GameRules Rules, WaveCheckpoint Checkpoint, IReadOnlyList<InputRun> Inputs, long TicksBeforeCheckpoint);

public readonly record struct InputRun(TickInput Input, int Count);

public static class RunLength
{
    public static List<InputRun> Encode(IReadOnlyList<TickInput> inputs)
    {
        var runs = new List<InputRun>();
        foreach (var i in inputs)
        {
            if (runs.Count > 0 && runs[^1].Input == i) runs[^1] = runs[^1] with { Count = runs[^1].Count + 1 };
            else runs.Add(new InputRun(i, 1));
        }
        return runs;
    }

    public static IEnumerable<TickInput> Decode(IEnumerable<InputRun> runs)
    {
        foreach (var r in runs)
            for (int k = 0; k < r.Count; k++) yield return r.Input;
    }
}
