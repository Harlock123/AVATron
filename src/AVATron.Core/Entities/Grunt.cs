using AVATron.Core.Simulation;
using AVATron.Core.Sprites;

namespace AVATron.Core.Entities;

/// Grunt: steps 4 px and/or 4 lines straight at the player after a random 1..GruntSpeed passes of the
/// shared 4-frame grunt manager (World.UpdateGrunts). Dies with an electrode it walks into.
public sealed class Grunt : Entity
{
    public override EntityKind Kind => EntityKind.Grunt;
    public override Sprite Image => SpriteLibrary.Grunt[Anim % 3];
    /// Manager passes until the next step.
    public int StepTimer = 1;

    /// One step toward the player. Returns true if it moved.
    public bool Step(World w)
    {
        int px = w.Player.X, py = w.Player.Y;
        int dx = 0, dy = 0;
        // Horizontal compares at arcade byte (2 px) resolution; vertical moves unless within a line.
        int byteDiff = (px >> 9) - (X >> 9);
        if (byteDiff != 0) dx = Math.Sign(byteDiff) * 4 * Arena.One;
        if (Math.Abs(py - Y) > Arena.One) dy = Math.Sign(py - Y) * 4 * Arena.One;
        if (dx == 0 && dy == 0) return false;
        X = Arena.ClampX(X + dx, Image.Width);
        Y = Arena.ClampY(Y + dy, Image.Height);
        Anim++;
        foreach (var e in w.Electrodes)
            if (!e.Dead && Touches(e))
            {
                e.Destroy(w, 0, 0);
                Destroy(w, Math.Sign(dx), Math.Sign(dy));   // grunt dies too; player is credited 100
                break;
            }
        return true;
    }

    protected override void OnDestroyed(World w) => w.OnGruntKilled();
}

/// Electrode ("post"): stationary, lethal to the player, destroyed by shots (0 points), grunts and hulks.
public sealed class Electrode : Entity
{
    public override EntityKind Kind => EntityKind.Electrode;
    public int Shape;
    public override Sprite Image => SpriteLibrary.Electrode[Shape];

    public override void Destroy(World w, int dx, int dy)
    {
        if (Dead) return;
        Dead = true;
        w.AddEffect(new Effect(EffectKind.ElectrodeFade, Px, Py, 11) { Sprite = Image });
        w.Emit(new GameEvent(GameEventKind.ElectrodeDestroyed, CenterX, CenterY, 0, Kind));
    }

}
