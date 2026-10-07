using AVATron.Core.Simulation;
using AVATron.Core.Sprites;

namespace AVATron.Core.Entities;

/// Hulk: indestructible. Moves only up/down/left/right, one step every HulkSleep frames, hunting a family
/// member (about 3 in 4 hulks) or the player. Shots push it back. Crushes family and electrodes.
public sealed class Hulk : Entity
{
    public override EntityKind Kind => EntityKind.Hulk;
    public override Sprite Image => _left ? MirrorCache[Anim % 3] : SpriteLibrary.Hulk[Anim % 3];
    static readonly Sprite[] MirrorCache = SpriteLibrary.Hulk.Select(SpriteLibrary.Mirror).ToArray();

    public Human? Target;
    int _dir;            // 0 right, 1 down, 2 left, 3 up
    int _stepsLeft;
    bool _seekX;
    bool _wideStep;
    bool _left;

    public override void Think(World w)
    {
        Sleep = w.P.HulkSleep;
        if (Target is { Dead: true }) Target = null;
        if (--_stepsLeft <= 0) ChooseDirection(w);

        _wideStep = !_wideStep;
        int dx = _dir switch { 0 => (_wideStep ? 4 : 3), 2 => -(_wideStep ? 4 : 3), _ => 0 } * Arena.One;
        int dy = _dir switch { 1 => 2, 3 => -2, _ => 0 } * Arena.One;
        int nx = X + dx, ny = Y + dy;
        if (!InsideX(nx) || !InsideY(ny)) { ChooseDirection(w); return; }
        X = nx; Y = ny;
        if (dx != 0) _left = dx < 0;
        Anim++;
        w.Emit(new GameEvent(GameEventKind.HulkStep, CenterX, CenterY, 0, Kind));

        foreach (var h in w.Family)
            if (!h.Dead && h.CapturedBy is null && Touches(h)) h.Kill(w);
        foreach (var e in w.Electrodes)
            if (!e.Dead && Touches(e)) e.Destroy(w, 0, 0);
    }

    void ChooseDirection(World w)
    {
        _stepsLeft = w.Rng.Next(1, 33);
        _seekX = !_seekX;
        int tx, ty;
        if (Target is { Dead: false } t) { tx = t.X; ty = t.Y; } else { tx = w.Player.X; ty = w.Player.Y; }
        tx += w.Rng.Next(-32, 33) * Arena.One;   // +/-16 arcade bytes of jitter
        ty += w.Rng.Next(-16, 17) * Arena.One;   // +/-16 lines
        _dir = _seekX ? (tx >= X ? 0 : 2) : (ty >= Y ? 1 : 3);
    }

    public override void OnShot(World w, int dx, int dy)
    {
        // Pushed in the shot's direction by 1-2 arcade bytes horizontally and 1-2 lines vertically.
        X = Arena.ClampX(X + dx * w.Rng.Next(1, 3) * 2 * Arena.One, Image.Width);
        Y = Arena.ClampY(Y + dy * w.Rng.Next(1, 3) * Arena.One, Image.Height);
        w.Emit(new GameEvent(GameEventKind.HulkHit, CenterX, CenterY, 0, Kind));
    }
}
