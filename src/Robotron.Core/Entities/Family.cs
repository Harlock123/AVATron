using Robotron.Core.Simulation;
using Robotron.Core.Sprites;

namespace Robotron.Core.Entities;

/// Mommy, Daddy or Mikey. Wanders one step every 8 frames in one of eight directions, re-choosing every
/// 1..128 steps or on meeting a wall or electrode. Rescued on touching the player.
public sealed class Human : Entity
{
    public Human(EntityKind kind) { _kind = kind; }
    readonly EntityKind _kind;
    public override EntityKind Kind => _kind;
    public override bool Shootable => false;   // player shots pass through the family
    public override Sprite Image => Frames[Anim % 3];
    Sprite[] Frames => _kind switch
    {
        EntityKind.Mommy => SpriteLibrary.Mommy,
        EntityKind.Daddy => SpriteLibrary.Daddy,
        _ => SpriteLibrary.Mikey,
    };

    public int DirX, DirY;
    int _stepsLeft;
    bool _wideStep;
    /// Set while a Brain is reprogramming this human.
    public Brain? CapturedBy;
    public bool FacingLeft => DirX < 0;

    public const int StepFrames = 8;

    public override void Think(World w)
    {
        Sleep = StepFrames;
        if (CapturedBy is not null) return;
        if (--_stepsLeft <= 0) PickDirection(w);
        _wideStep = !_wideStep;
        int nx = X + DirX * (_wideStep ? 2 : 1) * Arena.One;
        int ny = Y + DirY * Arena.One;
        if (!InsideX(nx) || !InsideY(ny) || WouldHitElectrode(w, nx, ny)) { PickDirection(w); return; }
        X = nx; Y = ny;
        Anim++;
    }

    void PickDirection(World w)
    {
        int d = w.Rng.Next(8);
        DirX = Dx8[d]; DirY = Dy8[d];
        _stepsLeft = w.Rng.Next(1, 129);
    }

    bool WouldHitElectrode(World w, int nx, int ny)
    {
        foreach (var e in w.Electrodes)
            if (!e.Dead && Sprite.Overlaps(Image, nx >> 8, ny >> 8, e.Image, e.Px, e.Py)) return true;
        return false;
    }

    /// Killed by a Hulk (or by a Brain dying mid-conversion): leaves a skull, no score.
    public void Kill(World w)
    {
        if (Dead) return;
        Dead = true;
        w.AddEffect(new Effect(EffectKind.Skull, CenterX - 6, CenterY - 5, 90) { Sprite = SpriteLibrary.Skull });
        w.Emit(new GameEvent(GameEventKind.HumanKilled, CenterX, CenterY, 0, Kind));
    }

    public static readonly int[] Dx8 = [1, 1, 0, -1, -1, -1, 0, 1];
    public static readonly int[] Dy8 = [0, 1, 1, 1, 0, -1, -1, -1];
}
