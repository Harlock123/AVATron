using Robotron.Core.Simulation;
using Robotron.Core.Sprites;

namespace Robotron.Core.Entities;

/// Base for every non-player object. Positions are 8.8 fixed point in raw arcade pixels/lines.
/// Lifetime: created by World (wave placement) or by another entity via World.Spawn (queued, added
/// at end of tick); destroyed by setting Dead (removed at end of tick). Nothing is removed mid-iteration.
public abstract class Entity
{
    public abstract EntityKind Kind { get; }
    public int X, Y;
    /// Frames until the next Think(). Entities that only integrate velocity leave this at 0.
    public int Sleep;
    public bool Dead;
    public int Anim;
    /// Order in which the entity materialises during the wave-start effect.
    public int AppearOrder;

    public abstract Sprite Image { get; }
    /// Image used when a player shot tests this entity (some objects use a solid "fat" stand-in).
    public virtual Sprite ShotMask => Image;
    /// False while an entity is still materialising (cannot hurt the player).
    public virtual bool Harmful => true;
    public virtual bool Shootable => true;

    public int Px => X >> 8;
    public int Py => Y >> 8;
    public int CenterX => Px + Image.Width / 2;
    public int CenterY => Py + Image.Height / 2;

    /// Runs when Sleep counts down to zero. Implementations must set Sleep again.
    public virtual void Think(World w) { Sleep = int.MaxValue; }
    /// Runs every frame (velocity integration for "motion objects").
    public virtual void Integrate(World w) { }

    /// A player shot travelling (dx, dy) hit this entity.
    public virtual void OnShot(World w, int dx, int dy) => Destroy(w, dx, dy);

    public virtual void Destroy(World w, int dx, int dy)
    {
        if (Dead) return;
        Dead = true;
        int pts = EntityScores.For(Kind);
        if (pts > 0) w.Award(pts);
        w.AddEffect(new Effect(EffectKind.Explosion, Px, Py, 24) { Sprite = Image, DirX = dx, DirY = dy });
        w.Emit(new GameEvent(GameEventKind.EnemyKilled, CenterX, CenterY, pts, Kind));
        OnDestroyed(w);
    }

    protected virtual void OnDestroyed(World w) { }

    public bool Touches(Entity other) => Sprite.Overlaps(Image, Px, Py, other.Image, other.Px, other.Py);

    protected bool InsideX(int x) => x >= Arena.MinX(Image.Width) && x <= Arena.MaxX(Image.Width);
    protected bool InsideY(int y) => y >= Arena.MinY(Image.Height) && y <= Arena.MaxY(Image.Height);
}

public enum EffectKind : byte { Explosion, ScorePopup, Skull, ElectrodeFade, WallGlow }

/// Short-lived, non-interacting visual. Kept in the simulation so timing is deterministic.
public sealed class Effect(EffectKind kind, int x, int y, int duration)
{
    public EffectKind Kind { get; } = kind;
    public int X { get; } = x;
    public int Y { get; } = y;
    public int Duration { get; } = duration;
    public int Age;
    public Sprite? Sprite { get; init; }
    public int DirX { get; init; }
    public int DirY { get; init; }
    public int Value { get; init; }
    public bool Done => Age >= Duration;
}
