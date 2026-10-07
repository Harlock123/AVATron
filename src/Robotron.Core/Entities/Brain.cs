using Robotron.Core.Simulation;
using Robotron.Core.Sprites;

namespace Robotron.Core.Entities;

/// Brain: walks diagonally toward the nearest family member (Manhattan distance), or the player when no
/// family is left, one step every BrainSleep frames. Reprograms a caught human into a Prog over ~80 frames
/// and fires cruise missiles (at most 8 alive).
public sealed class Brain : Entity
{
    public override EntityKind Kind => EntityKind.Brain;
    public override Sprite Image => SpriteLibrary.Brain[Anim % 2];

    public Human? Victim;
    int _convertSteps;
    int _missileTimer;
    public const int ConvertIterations = 20, ConvertIterationFrames = 4;
    public const int MaxMissiles = 8;

    public void Init(World w) => _missileTimer = w.RMax(w.P.BrainMissileTimer);

    public override void Think(World w)
    {
        if (Victim is not null) { Convert(w); return; }
        Sleep = w.P.BrainSleep;

        Human? target = Nearest(w);
        int tx = target?.X ?? w.Player.X, ty = target?.Y ?? w.Player.Y;
        int dx = tx - X, dy = ty - Y;
        if (Math.Abs(dx) > 4 * Arena.One) X = Arena.ClampX(X + Math.Sign(dx) * 2 * Arena.One, Image.Width);
        if (dy != 0) Y = Arena.ClampY(Y + Math.Sign(dy) * Arena.One, Image.Height);
        Anim++;

        if (target is not null && Math.Abs(target.X - X) <= 6 * Arena.One && Math.Abs(target.Y - Y) <= 3 * Arena.One)
        {
            Victim = target;
            target.CapturedBy = this;
            // Pull the human beside the brain.
            int side = X + (Image.Width + 1) * Arena.One;
            target.X = Arena.ClampX(side <= Arena.MaxX(target.Image.Width) ? side : X - (target.Image.Width + 1) * Arena.One, target.Image.Width);
            target.Y = Arena.ClampY(Y, target.Image.Height);
            _convertSteps = ConvertIterations;
            Sleep = ConvertIterationFrames;
            w.Emit(new GameEvent(GameEventKind.BrainCapturing, CenterX, CenterY, 0, Kind));
            return;
        }

        if (--_missileTimer <= 0)
        {
            _missileTimer = w.RMax(w.P.BrainMissileTimer);
            if (w.Count(EntityKind.CruiseMissile) < MaxMissiles)
            {
                w.Spawn(new CruiseMissile { X = X + 4 * Arena.One, Y = Y + 6 * Arena.One, Sleep = 2 });
                w.Emit(new GameEvent(GameEventKind.MissileFired, CenterX, CenterY, 0, Kind));
            }
        }
    }

    void Convert(World w)
    {
        Sleep = ConvertIterationFrames;
        var h = Victim!;
        if (h.Dead) { Victim = null; return; }
        if (--_convertSteps > 0) return;
        h.Dead = true;
        Victim = null;
        var prog = new Prog(h.Kind) { X = h.X, Y = h.Y, Sleep = Prog.StepFrames };
        prog.X = Arena.ClampX(prog.X, prog.Image.Width);
        prog.Y = Arena.ClampY(prog.Y, prog.Image.Height);
        w.Spawn(prog);
        w.Emit(new GameEvent(GameEventKind.ProgCreated, h.CenterX, h.CenterY, 0, h.Kind));
    }

    Human? Nearest(World w)
    {
        Human? best = null; int bestD = int.MaxValue;
        foreach (var h in w.Family)
        {
            if (h.Dead || h.CapturedBy is not null) continue;
            int d = Math.Abs(h.X - X) + Math.Abs(h.Y - Y);
            if (d < bestD) { bestD = d; best = h; }
        }
        return best;
    }

    public bool IsConverting => Victim is not null;

    protected override void OnDestroyed(World w)
    {
        // Killing a Brain mid-conversion leaves the human dead (skull), not rescued.
        if (Victim is { Dead: false } v) { v.CapturedBy = null; v.Kill(w); }
        Victim = null;
    }
}

/// Prog: a reprogrammed human. Every 3 frames lunges 4 px horizontally or 4 lines vertically toward the
/// player plus a random offset; re-picks axis/offset at random (~11%) or when blocked.
public sealed class Prog : Entity
{
    public Prog(EntityKind from) { From = from; }
    public EntityKind From { get; }
    public override EntityKind Kind => EntityKind.Prog;
    public override Sprite Image => (From switch
    {
        EntityKind.Mommy => SpriteLibrary.ProgFromMommy,
        EntityKind.Daddy => SpriteLibrary.ProgFromDaddy,
        _ => SpriteLibrary.ProgFromMikey,
    })[Anim % 3];

    public const int StepFrames = 3;
    public const int TrailLength = 4;
    /// Recent positions (pixels), newest first, for the trailing-shadow effect.
    public readonly List<(int X, int Y)> Trail = [];
    bool _axisX;
    int _offX, _offY;

    public override void Think(World w)
    {
        Sleep = StepFrames;
        if (w.Rng.Next(256) < 28) _axisX = !_axisX;
        if (w.Rng.Next(256) < 28) Retarget(w);
        int tx = w.Player.X + _offX, ty = w.Player.Y + _offY;
        int d = _axisX ? tx - X : ty - Y;
        if (Math.Abs(d) < 4 * Arena.One) { _axisX = !_axisX; d = _axisX ? tx - X : ty - Y; }
        int step = Math.Sign(d) * 4 * Arena.One;
        int nx = _axisX ? X + step : X, ny = _axisX ? Y : Y + step;
        if (!InsideX(nx) || !InsideY(ny)) { _axisX = !_axisX; Retarget(w); return; }
        Trail.Insert(0, (Px, Py));
        if (Trail.Count > TrailLength) Trail.RemoveAt(Trail.Count - 1);
        X = nx; Y = ny;
        Anim++;
    }

    void Retarget(World w)
    {
        _offX = w.Rng.Next(-32, 33) * Arena.One;
        _offY = w.Rng.Next(-32, 33) * Arena.One;
    }
}

/// Brain cruise missile: two 2 px/1 line moves per 2-frame wake, re-aiming at the player (with jitter)
/// every RMAX(7) wakes, reflecting off walls, never expiring. Shootable for 25.
public sealed class CruiseMissile : Entity
{
    public override EntityKind Kind => EntityKind.CruiseMissile;
    public override Sprite Image => SpriteLibrary.CruiseMissile;
    public override Sprite ShotMask => SpriteLibrary.CruiseMissileShotMask;
    public const int TrailLength = 8;
    public readonly List<(int X, int Y)> Trail = [];
    int _dx, _dy, _aimTimer;

    public override void Think(World w)
    {
        Sleep = 2;
        if (--_aimTimer <= 0)
        {
            _aimTimer = w.RMax(7);
            int tx = w.Player.X + w.Rng.Next(-6, 7) * Arena.One, ty = w.Player.Y + w.Rng.Next(-6, 7) * Arena.One;
            _dx = Math.Sign(tx - X); _dy = Math.Sign(ty - Y);
            if (_dx == 0 && _dy == 0) _dx = 1;
        }
        for (int i = 0; i < 2; i++)
        {
            Trail.Insert(0, (CenterX, CenterY));
            if (Trail.Count > TrailLength) Trail.RemoveAt(Trail.Count - 1);
            int nx = X + _dx * 2 * Arena.One, ny = Y + _dy * Arena.One;
            if (!InsideX(nx)) { _dx = -_dx; nx = X + _dx * 2 * Arena.One; }
            if (!InsideY(ny)) { _dy = -_dy; ny = Y + _dy * Arena.One; }
            X = Arena.ClampX(nx, Image.Width); Y = Arena.ClampY(ny, Image.Height);
        }
        Anim++;
    }
}
