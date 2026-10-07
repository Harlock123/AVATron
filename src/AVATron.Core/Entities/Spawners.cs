using AVATron.Core.Simulation;
using AVATron.Core.Sprites;

namespace AVATron.Core.Entities;

/// Object moved by velocity every frame (the arcade's IRQ-driven "motion objects").
public abstract class MotionEntity : Entity
{
    public int VX, VY;
    /// Reflect off the playfield walls (reconstruction for most types; documented for tank shells).
    protected virtual bool Bounces => true;

    public override void Integrate(World w)
    {
        X += VX; Y += VY;
        if (!Bounces) return;
        if (X < Arena.MinX(Image.Width)) { X = Arena.MinX(Image.Width); VX = Math.Abs(VX); }
        else if (X > Arena.MaxX(Image.Width)) { X = Arena.MaxX(Image.Width); VX = -Math.Abs(VX); }
        if (Y < Arena.MinY(Image.Height)) { Y = Arena.MinY(Image.Height); VY = Math.Abs(VY); }
        else if (Y > Arena.MaxY(Image.Height)) { Y = Arena.MaxY(Image.Height); VY = -Math.Abs(VY); }
    }

    protected bool OffPlayfield =>
        X + Image.Width * Arena.One < Arena.Left * Arena.One || X > Arena.Right * Arena.One ||
        Y + Image.Height * Arena.One < Arena.Top * Arena.One || Y > Arena.Bottom * Arena.One;
}

/// Sphereoid: drifts with random acceleration and damping, drops 1..5 (1..6 from wave 21) Enforcers,
/// then flies off the side and is removed.
public sealed class Sphereoid : MotionEntity
{
    public override EntityKind Kind => EntityKind.Sphereoid;
    public override Sprite Image => SpriteLibrary.Sphereoid[(Anim / 2) % 8];
    public override Sprite ShotMask => SpriteLibrary.SphereoidShotMask;
    protected override bool Bounces => !_leaving;

    int _ax, _ay, _accelTimer, _dropTimer, _children;
    bool _leaving;
    public const int MaxEnforcers = 8;

    public void Init(World w)
    {
        _children = (w.RMax(w.P.SpawnerChildren2x) + 1) / 2;
        _dropTimer = w.RMax(w.P.SphereoidDropTimer) * 10;
        Sleep = 2;
    }

    public override void Think(World w)
    {
        Sleep = 2;
        Anim++;
        if (_leaving) { if (OffPlayfield) Dead = true; return; }

        if (--_accelTimer <= 0)
        {
            _accelTimer = w.Rng.Next(1, 16);
            _ax = w.Rng.Next(2) == 0 ? -32 : 32;
            _ay = w.Rng.Next(2) == 0 ? -32 : 32;
        }
        VX = Math.Clamp(VX + _ax - (VX + _ax) / 64, -2 * Arena.One, 2 * Arena.One);
        VY = Math.Clamp(VY + _ay - (VY + _ay) / 128, -2 * Arena.One, 2 * Arena.One);

        _dropTimer -= 2;
        if (_dropTimer > 0) return;
        if (w.Count(EntityKind.Enforcer) >= MaxEnforcers) { _dropTimer = 2; return; }
        w.Spawn(new Enforcer { X = X + 3 * Arena.One, Y = Y + 2 * Arena.One });
        w.Emit(new GameEvent(GameEventKind.SphereoidDrop, CenterX, CenterY, 0, Kind));
        if (--_children > 0) { _dropTimer = w.RMax(Math.Max(1, w.P.SphereoidDropTimer / 4)) * 16; return; }
        // Spent: leave horizontally toward the nearer side at 1 arcade byte (2 px) per frame.
        _leaving = true;
        VX = CenterX < (Arena.Left + Arena.Right) / 2 ? -2 * Arena.One : 2 * Arena.One;
        VY = 0;
    }
}

/// Enforcer: materialises over 40 frames, then glides toward the player (with jitter) and fires sparks.
public sealed class Enforcer : MotionEntity
{
    public override EntityKind Kind => EntityKind.Enforcer;
    public override Sprite Image => SpriteLibrary.Enforcer;
    public const int GrowFrames = 40, MaxSparks = 20;
    public int Age;
    public override bool Harmful => Age >= GrowFrames;
    int _velTimer, _shotTimer = -1;

    public override void Integrate(World w)
    {
        Age++;
        if (Age == GrowFrames) { Sleep = 1; w.Emit(new GameEvent(GameEventKind.EnforcerAppears, CenterX, CenterY, 0, Kind)); }
        if (Age >= GrowFrames) base.Integrate(w);
    }

    public override void Think(World w)
    {
        if (Age < GrowFrames) { Sleep = GrowFrames - Age; return; }
        Sleep = 3;
        if (_shotTimer < 0) _shotTimer = w.RMax(w.P.EnforcerShotTimer);
        if (--_velTimer <= 0)
        {
            _velTimer = w.Rng.Next(0, 32);
            int tx = w.Player.X + w.Rng.Next(-32, 33) * Arena.One, ty = w.Player.Y + w.Rng.Next(-16, 17) * Arena.One;
            VX = (tx - X) * 2 / 256; VY = (ty - Y) * 2 / 256;
        }
        if (--_shotTimer <= 0)
        {
            _shotTimer = w.RMax(w.P.EnforcerShotTimer);
            if (w.Count(EntityKind.Spark) < MaxSparks)
            {
                var s = new Spark { X = X + Arena.One, Y = Y + 2 * Arena.One };
                s.Aim(w);
                w.Spawn(s);
                w.Emit(new GameEvent(GameEventKind.SparkFired, CenterX, CenterY, 0, Kind));
            }
        }
    }
}

/// Enforcer spark: launched at the player, curving under random acceleration; expires after 80..140 frames.
public sealed class Spark : MotionEntity
{
    public override EntityKind Kind => EntityKind.Spark;
    public override Sprite Image => SpriteLibrary.Spark[Anim % 2];
    int _life;

    public void Aim(World w)
    {
        int tx = w.Player.X + w.Rng.Next(-32, 33) * Arena.One, ty = w.Player.Y + w.Rng.Next(-16, 17) * Arena.One;
        VX = (tx - X) * 4 / 256; VY = (ty - Y) * 4 / 256;
        _life = w.Rng.Next(20, 36);
        Sleep = 4;
    }

    public override void Think(World w)
    {
        Sleep = 4;
        Anim++;
        if (--_life <= 0) { Dead = true; return; }
        VX += w.Rng.Next(-32, 33);
        VY += w.Rng.Next(-16, 17);
    }
}

/// Quark: bounces about at random speed, drops Tanks (at most 20 alive), then flees vertically.
public sealed class Quark : MotionEntity
{
    public override EntityKind Kind => EntityKind.Quark;
    public override Sprite Image => SpriteLibrary.Quark[Anim % 4];
    public override Sprite ShotMask => SpriteLibrary.QuarkShotMask;
    protected override bool Bounces => !_leaving;
    public const int MaxTanks = 20;
    int _velTimer, _dropTimer, _children;
    bool _leaving;

    public void Init(World w)
    {
        _children = (w.RMax(w.P.SpawnerChildren2x) + 1) / 2;
        _dropTimer = w.RMax(w.P.QuarkDropTimer) * 15;
        Sleep = 3;
    }

    public override void Think(World w)
    {
        Sleep = 3;
        Anim++;
        if (_leaving) { if (OffPlayfield) Dead = true; return; }
        if (--_velTimer <= 0)
        {
            _velTimer = w.Rng.Next(1, 33);
            VX = w.RMax(w.P.QuarkSpeed) * 8 * (w.Rng.Next(2) == 0 ? -1 : 1);
            VY = w.RMax(w.P.QuarkSpeed) * 8 * (w.Rng.Next(2) == 0 ? -1 : 1);
            // Head away from an edge it is near.
            if (Px < Arena.Left + 24) VX = Math.Abs(VX); else if (Px > Arena.Right - 40) VX = -Math.Abs(VX);
            if (Py < Arena.Top + 24) VY = Math.Abs(VY); else if (Py > Arena.Bottom - 40) VY = -Math.Abs(VY);
        }
        _dropTimer -= 3;
        if (_dropTimer > 0) return;
        if (w.Count(EntityKind.Tank) >= MaxTanks) { _dropTimer = 3; return; }
        w.Spawn(new Tank { X = X + Arena.One, Y = Y });
        w.Emit(new GameEvent(GameEventKind.QuarkDrop, CenterX, CenterY, 0, Kind));
        if (--_children > 0) { _dropTimer = w.RMax(w.P.QuarkDropTimer / 2 + 1) * 3; return; }
        _leaving = true;
        VX = 0;
        VY = CenterY < (Arena.Top + Arena.Bottom) / 2 ? -2 * Arena.One : 2 * Arena.One;
    }
}

/// Tank: materialises over 48 frames, crawls toward the player (~38%) or a random point, fires shells.
public sealed class Tank : Entity
{
    public override EntityKind Kind => EntityKind.Tank;
    public override Sprite Image => SpriteLibrary.Tank[Anim % 2];
    public const int GrowFrames = 48, StepFrames = 2, MaxShellCounter = 20;
    public int Age;
    public override bool Harmful => Age >= GrowFrames;
    int _tx, _ty, _stepsLeft, _shotSteps = -1;

    public override void Integrate(World w)
    {
        Age++;
        if (Age == GrowFrames) { Sleep = 1; w.Emit(new GameEvent(GameEventKind.TankAppears, CenterX, CenterY, 0, Kind)); }
    }

    public override void Think(World w)
    {
        if (Age < GrowFrames) { Sleep = GrowFrames - Age; return; }
        Sleep = StepFrames;
        if (_shotSteps < 0) _shotSteps = w.P.TankShotSteps + w.Rng.Next(0, 32);
        if (--_stepsLeft <= 0) Retarget(w);
        int dx = Math.Sign(_tx - X) * Arena.One;
        int dy = Math.Abs(_ty - Y) > 16 * Arena.One ? Math.Sign(_ty - Y) * Arena.One : 0;
        int nx = X + dx, ny = Y + dy;
        if (!InsideX(nx) || !InsideY(ny)) Retarget(w);
        else { X = nx; Y = ny; if (dx != 0 || dy != 0) Anim++; }

        if (--_shotSteps <= 0)
        {
            _shotSteps = w.P.TankShotSteps;
            if (w.ShellCounter <= MaxShellCounter) Fire(w);
        }
    }

    void Retarget(World w)
    {
        _stepsLeft = w.Rng.Next(1, 33);
        if (w.Rng.Next(256) <= 0x60) { _tx = w.Player.X; _ty = w.Player.Y; }
        else
        {
            _tx = w.Rng.Next(Arena.Left, Arena.Right - Image.Width) * Arena.One;
            _ty = w.Rng.Next(Arena.Top, Arena.Bottom - Image.Height) * Arena.One;
        }
    }

    void Fire(World w)
    {
        var s = new Shell { X = X + 3 * Arena.One, Y = Y + 2 * Arena.One };
        int tx = w.Player.X + w.Rng.Next(-16, 17) * Arena.One, ty = w.Player.Y + w.Rng.Next(-16, 17) * Arena.One;
        if (w.Rng.Next(2) == 1)
        {
            // Bank shot: aim at the player's mirror image across a wall so the rebound comes at them.
            switch (w.Rng.Next(4))
            {
                case 0: tx = 2 * Arena.Left * Arena.One - tx; break;
                case 1: tx = 2 * (Arena.Right - s.Image.Width) * Arena.One - tx; break;
                case 2: ty = 2 * Arena.Top * Arena.One - ty; break;
                default: ty = 2 * (Arena.Bottom - s.Image.Height) * Arena.One - ty; break;
            }
        }
        long f = (long)w.P.TankShellSpeed * 8;
        s.VX = (int)((tx - s.X) * f / 65536);
        s.VY = (int)((ty - s.Y) * f / 65536);
        s.Init(w);
        w.ShellCounter++;
        w.Spawn(s);
        w.Emit(new GameEvent(GameEventKind.ShellFired, CenterX, CenterY, 0, Kind));
    }
}

/// Tank shell: rebounds off walls (checked every 2 frames), expires after 96..158 frames. Shootable for 25.
public sealed class Shell : MotionEntity
{
    public override EntityKind Kind => EntityKind.Shell;
    public override Sprite Image => SpriteLibrary.Shell;
    protected override bool Bounces => false;
    int _life;

    public void Init(World w) { _life = w.Rng.Next(48, 80); Sleep = 2; }

    public override void Think(World w)
    {
        Sleep = 2;
        if (--_life <= 0) { Dead = true; return; }   // as in the arcade, expiry does not lower the shell counter
        bool bounced = false;
        if (X < Arena.MinX(Image.Width) || X > Arena.MaxX(Image.Width)) { VX = -VX; X = Arena.ClampX(X, Image.Width); bounced = true; }
        if (Y < Arena.MinY(Image.Height) || Y > Arena.MaxY(Image.Height)) { VY = -VY; Y = Arena.ClampY(Y, Image.Height); bounced = true; }
        if (bounced) w.Emit(new GameEvent(GameEventKind.ShellBounce, CenterX, CenterY, 0, Kind));
    }

    protected override void OnDestroyed(World w) => w.ShellCounter--;
}
