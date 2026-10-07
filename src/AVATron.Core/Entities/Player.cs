using AVATron.Core.Input;
using AVATron.Core.Simulation;
using AVATron.Core.Sprites;

namespace AVATron.Core.Entities;

public sealed class Player
{
    public int X = Arena.PlayerStartX * Arena.One, Y = Arena.PlayerStartY * Arena.One;
    public int Facing;           // 0 down, 1 up, 2 left, 3 right
    int _walkTick, _walkFrame;
    int _fireState, _fireHold;

    public int Px => X >> 8;
    public int Py => Y >> 8;
    public int CenterX => Px + 4;
    public int CenterY => Py + 6;

    static readonly int[] WalkSeq = [0, 1, 0, 2];
    public Sprite Image => (Facing switch
    {
        1 => SpriteLibrary.PlayerUp,
        2 => SpriteLibrary.PlayerLeft,
        3 => SpriteLibrary.PlayerRight,
        _ => SpriteLibrary.PlayerDown,
    })[WalkSeq[_walkFrame & 3]];

    /// Movement runs every frame. Digital: 1 px and/or 1 line per frame. Analog: the same per-axis maximum,
    /// scaled by stick deflection.
    public void Move(StickInput s, bool analog)
    {
        int dx, dy;
        if (analog) { dx = s.X * Arena.One / 127; dy = s.Y * Arena.One / 127; }
        else { dx = s.DirX * Arena.One; dy = s.DirY * Arena.One; }
        if (dx == 0 && dy == 0) return;
        // Per axis: a move that would leave the playfield is clamped at the wall.
        X = Arena.ClampX(X + dx, 8);
        Y = Arena.ClampY(Y + dy, 12);
        Facing = dx < 0 ? 2 : dx > 0 ? 3 : dy < 0 ? 1 : 0;
        if (++_walkTick >= 2) { _walkTick = 0; _walkFrame++; }
    }

    public const int MaxShots = 4, FirstShotDelay = 2, RepeatFrames = 8, ShotSpeed = 6;

    /// Laser process, once per frame: first shot 2 frames after the fire direction settles, then every
    /// 8 frames while held; at most 4 shots alive. Returns a new shot or null.
    public PlayerShot? UpdateFire(StickInput fire, bool analog, int shotsAlive)
    {
        int state = fire.IsNeutral ? 0 : analog ? 1 : (fire.DirX + 1) * 3 + (fire.DirY + 1) + 1;
        if (state != _fireState) { _fireState = state; _fireHold = 0; return null; }
        if (state == 0) return null;
        int next = _fireHold + 1;
        bool fireNow = next == FirstShotDelay || next % RepeatFrames == 0;
        if (fireNow && shotsAlive >= MaxShots) return null;   // hold the count so it fires as soon as a slot frees
        _fireHold = next;
        return fireNow ? PlayerShot.Create(this, fire, analog) : null;
    }
}

public sealed class PlayerShot
{
    public int X, Y, VX, VY;
    /// Octant direction (-1/0/1) used for images, spawn offsets, explosions and hulk push-back.
    public int DirX, DirY;
    public bool Dead;
    public Sprite Image => SpriteLibrary.ShotFor(DirX, DirY);
    public int Px => X >> 8;
    public int Py => Y >> 8;

    public static PlayerShot Create(Player p, StickInput fire, bool analog)
    {
        int vx, vy, dx, dy;
        if (analog)
        {
            // Any angle; scale so the larger axis moves 6 per frame (matches arcade speeds at the 8 directions).
            double fx = fire.X, fy = fire.Y, m = Math.Max(Math.Abs(fx), Math.Abs(fy));
            vx = (int)Math.Round(fx / m * Player.ShotSpeed * Arena.One);
            vy = (int)Math.Round(fy / m * Player.ShotSpeed * Arena.One);
            var oct = StickQuantizer.ToEightWay(fire);
            dx = oct.DirX; dy = oct.DirY;
        }
        else
        {
            dx = fire.DirX; dy = fire.DirY;
            vx = dx * Player.ShotSpeed * Arena.One; vy = dy * Player.ShotSpeed * Arena.One;
        }
        // Spawn offsets from the player's top-left (arcade bytes x2 = pixels, lines).
        (int ox, int oy) = (dx, dy) switch
        {
            (0, -1) => (4, -1), (0, 1) => (4, 4), (-1, 0) => (0, 4), (1, 0) => (4, 4),
            (-1, -1) => (0, 0), (-1, 1) => (0, 4), (1, -1) => (4, 0), _ => (4, 4),
        };
        return new PlayerShot { X = p.X + ox * Arena.One, Y = p.Y + oy * Arena.One, VX = vx, VY = vy, DirX = dx, DirY = dy };
    }
}
