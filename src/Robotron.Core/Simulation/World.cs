using Robotron.Core.Entities;
using Robotron.Core.Input;
using Robotron.Core.Waves;

namespace Robotron.Core.Simulation;

/// Everything on the playfield for one life within one wave. A new World is built at every wave start
/// and after every death (the arcade's PLINIT), which is what resets the rescue multiplier and shell counter.
///
/// Per-tick order (Step):
///   1 player move  2 motion-object velocity  3 laser process (new shot)  4 shots advance + hit tests
///   5 entity processes whose sleep expired (grunt manager every 4 frames)  6 player collision
///   7 lifecycle: remove dead, add queued spawns, age effects  8 exec every 15 frames (wave-end, grunt speed-up)
public sealed class World
{
    public World(IGameRandom rng, WaveParameters p, int wave, GameRules rules)
    {
        Rng = rng; P = p; Wave = wave; Rules = rules;
        RobSpd = p.GruntSpeed; RobMax = p.GruntSpeedFloor;
    }

    public IGameRandom Rng { get; }
    public WaveParameters P { get; }
    public int Wave { get; }
    public GameRules Rules { get; }
    public Player Player { get; } = new();
    public long Frame { get; private set; }

    public readonly List<Entity> Enemies = [];       // robots + motion objects, in creation order
    public readonly List<Electrode> Electrodes = [];
    public readonly List<Human> Family = [];
    public readonly List<PlayerShot> Shots = [];
    public readonly List<Effect> Effects = [];
    public readonly List<GameEvent> Events = [];
    readonly List<Entity> _spawned = [];

    /// Grunt step-timer ceiling and its floor; both are driven down during a wave.
    public int RobSpd, RobMax;
    /// Tank-shell counter. As in the arcade program it is lowered only when a shell is shot.
    public int ShellCounter;
    /// Rescues this life/wave (multiplier for the next rescue).
    public int Rescues;
    public int PointsThisTick { get; private set; }
    bool _scoredSinceSpeedCheck;
    int _gruntPassTimer, _execTimer, _speedUpExecs;
    public bool PlayerHit { get; private set; }
    public bool WaveComplete { get; private set; }

    public const int GruntPassFrames = 4, ExecFrames = 15, FirstSpeedUpExecs = 18, SpeedUpExecs = 15, SpeedUpGruntLimit = 30;

    // ---------------- helpers used by entities ----------------
    /// Uniform 1..n.
    public int RandU(int n) => n <= 1 ? 1 : Rng.Next(1, n + 1);
    /// The arcade's top-weighted 1..n: a random byte halved until it fits.
    public int RMax(int n)
    {
        if (n <= 1) return 1;
        int b = Rng.Next(256);
        while (b > n) b >>= 1;
        return Math.Max(1, b);
    }
    public void Spawn(Entity e) => _spawned.Add(e);
    public void Emit(GameEvent e) => Events.Add(e);
    public void AddEffect(Effect e) => Effects.Add(e);
    public void Award(int points) { PointsThisTick += points; _scoredSinceSpeedCheck = true; }
    public int Count(EntityKind k)
    {
        int n = 0;
        foreach (var e in Enemies) if (!e.Dead && e.Kind == k) n++;
        foreach (var e in _spawned) if (e.Kind == k) n++;
        return n;
    }
    public void OnGruntKilled() => RobSpd = Math.Max(RobMax, RobSpd * 0xE0 / 256);

    /// Enemies that must all be gone to finish the wave (Hulks, Electrodes, Progs and projectiles do not count).
    public int BlockingCount()
    {
        int n = 0;
        foreach (var e in Enemies)
            if (!e.Dead && e.Kind is EntityKind.Grunt or EntityKind.Sphereoid or EntityKind.Enforcer or EntityKind.Brain or EntityKind.Tank or EntityKind.Quark) n++;
        foreach (var e in _spawned)
            if (e.Kind is EntityKind.Enforcer or EntityKind.Tank) n++;
        return n;
    }

    /// Called when the wave goes live: staggered start delays as in the arcade.
    public void Activate()
    {
        _gruntPassTimer = 10;   // grunt manager naps 10 frames first
        foreach (var e in Enemies)
            e.Sleep = e.Kind switch { EntityKind.Brain => 12, EntityKind.Hulk => 8, EntityKind.Tank => 15, EntityKind.Sphereoid => 2, EntityKind.Quark => 3, _ => Math.Max(1, e.Sleep) };
        foreach (var h in Family) h.Sleep = 1 + Rng.Next(8);
    }

    // ---------------- the tick ----------------
    public void Step(TickInput input)
    {
        Frame++;
        PointsThisTick = 0;
        Events.Clear();

        // 1. Player movement (every frame).
        Player.Move(input.Move, Rules.AnalogMove);

        // 2. Motion objects integrate velocity every frame.
        foreach (var e in Enemies) if (!e.Dead) e.Integrate(this);

        // 3. Laser process.
        var shot = Player.UpdateFire(input.Fire, Rules.AnalogFire, Shots.Count);
        if (shot is not null) { Shots.Add(shot); Emit(new GameEvent(GameEventKind.PlayerShot, shot.Px, shot.Py)); }

        // 4. Shots advance and test (electrodes, then robots, then motion objects). New shots wait a frame.
        foreach (var s in Shots)
        {
            if (s == shot) { TestShot(s); continue; }
            s.X += s.VX; s.Y += s.VY;
            TestShot(s);
        }

        // 5. Processes.
        if (--_gruntPassTimer <= 0) { _gruntPassTimer = GruntPassFrames; UpdateGrunts(); }
        for (int i = 0; i < Enemies.Count; i++)
        {
            var e = Enemies[i];
            if (e.Dead || e is Grunt) continue;
            if (--e.Sleep <= 0) e.Think(this);
        }
        foreach (var h in Family)
            if (!h.Dead && --h.Sleep <= 0) h.Think(this);

        // 6. Player collision: fatal objects first, then rescues.
        CheckPlayer();

        // 7. Lifecycle.
        Enemies.RemoveAll(e => e.Dead);
        Electrodes.RemoveAll(e => e.Dead);
        Family.RemoveAll(h => h.Dead);
        Shots.RemoveAll(s => s.Dead);
        foreach (var e in _spawned)
        {
            if (e is Enforcer or Tank) e.Sleep = int.MaxValue / 2;   // woken when grown
            Enemies.Add(e);
        }
        _spawned.Clear();
        AgeEffects();

        // 8. Exec: wave-end check and grunt speed-up, every 15 frames.
        if (++_execTimer >= ExecFrames) { _execTimer = 0; Exec(); }
    }

    /// Advance visual effects only (used while the playfield is frozen).
    public void AgeEffects()
    {
        foreach (var fx in Effects) fx.Age++;
        Effects.RemoveAll(fx => fx.Done);
    }

    void UpdateGrunts()
    {
        bool moved = false;
        foreach (var e in Enemies)
        {
            if (e is not Grunt g || g.Dead) continue;
            if (--g.StepTimer > 0) continue;
            g.StepTimer = RandU(RobSpd);
            moved |= g.Step(this);
        }
        if (moved) Emit(new GameEvent(GameEventKind.GruntsMarched));
    }

    void TestShot(PlayerShot s)
    {
        var img = s.Image;
        if (s.Px < Arena.Left || s.Px + img.Width > Arena.Right || s.Py < Arena.Top || s.Py + img.Height > Arena.Bottom)
        {
            s.Dead = true;
            AddEffect(new Effect(EffectKind.WallGlow, Math.Clamp(s.Px, Arena.Left, Arena.Right), Math.Clamp(s.Py, Arena.Top, Arena.Bottom), 6) { DirX = s.DirX, DirY = s.DirY });
            Emit(new GameEvent(GameEventKind.ShotHitWall, s.Px, s.Py));
            return;
        }
        foreach (var e in Electrodes)
            if (!e.Dead && Sprites.Sprite.Overlaps(img, s.Px, s.Py, e.ShotMask, e.Px, e.Py)) { s.Dead = true; e.OnShot(this, s.DirX, s.DirY); return; }
        // Robot list before motion objects, matching the arcade's test order.
        for (int pass = 0; pass < 2; pass++)
            foreach (var e in Enemies)
            {
                if (e.Dead || !e.Shootable || (e is MotionEntity) != (pass == 1)) continue;
                if (Sprites.Sprite.Overlaps(img, s.Px, s.Py, e.ShotMask, e.Px, e.Py)) { s.Dead = true; e.OnShot(this, s.DirX, s.DirY); return; }
            }
    }

    void CheckPlayer()
    {
        var img = Player.Image; int px = Player.Px, py = Player.Py;
        for (int pass = 0; pass < 2 && !PlayerHit; pass++)
            foreach (var e in Enemies)
                if (!e.Dead && e.Harmful && (e is MotionEntity) == (pass == 1) && Sprites.Sprite.Overlaps(img, px, py, e.Image, e.Px, e.Py)) { PlayerHit = true; break; }
        if (!PlayerHit)
            foreach (var e in Electrodes)
                if (!e.Dead && Sprites.Sprite.Overlaps(img, px, py, e.Image, e.Px, e.Py)) { PlayerHit = true; break; }
        if (PlayerHit) return;
        foreach (var h in Family)
        {
            if (h.Dead || h.CapturedBy is not null || !Sprites.Sprite.Overlaps(img, px, py, h.Image, h.Px, h.Py)) continue;
            h.Dead = true;
            Rescues++;
            int value = EntityScores.RescueStep * Math.Min(Rescues, EntityScores.RescueCapMultiplier);
            Award(value);
            AddEffect(new Effect(EffectKind.ScorePopup, h.CenterX - 6, h.CenterY - 2, 60) { Value = value });
            Emit(new GameEvent(GameEventKind.HumanRescued, h.CenterX, h.CenterY, value, h.Kind));
        }
    }

    void Exec()
    {
        if (BlockingCount() == 0) { WaveComplete = true; return; }
        if (++_speedUpExecs < (_speedUpPrimed ? SpeedUpExecs : FirstSpeedUpExecs)) return;
        _speedUpExecs = 0; _speedUpPrimed = true;
        if (Count(EntityKind.Grunt) >= SpeedUpGruntLimit) { _scoredSinceSpeedCheck = false; return; }
        int k = _scoredSinceSpeedCheck ? 1 : 2;   // stalling doubles the speed-up
        RobMax = Math.Max(1, RobMax - k);
        RobSpd = Math.Max(RobMax, RobSpd - 2 * k);
        _scoredSinceSpeedCheck = false;
    }
    bool _speedUpPrimed;
}
