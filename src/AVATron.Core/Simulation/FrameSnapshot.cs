using AVATron.Core.Entities;
using AVATron.Core.Sprites;

namespace AVATron.Core.Simulation;

public enum DrawKind : byte
{
    Sprite,        // ordinary image
    Appear,        // materialising image (Progress 0..1); Value 0 = zoom, 1 = horizontal, 2 = transporter
    Shot,          // player laser: X,Y start; VX,VY velocity (for analog angle)
    Explosion,     // image streaking apart along (VX,VY) direction, Progress 0..1
    ElectrodeFade, // shrinking electrode, Progress 0..1
    Popup,         // rescue bonus value text
    WallGlow,      // laser hitting the wall
    Trail,         // dim afterimage (prog shadow / missile trail point)
}

[Flags]
public enum DrawFlags : byte { None = 0, Silhouette = 1, Mirror = 2, Dim = 4, Flicker = 8 }

public struct DrawItem
{
    public DrawKind Kind;
    public Sprite? Sprite;
    public int X, Y;          // raw arcade pixels/lines (renderer subtracts the view origin)
    public int VX, VY;
    public float Progress;
    public int Value;
    public DrawFlags Flags;
    public EntityKind Entity;
}

/// Immutable-by-convention view of one simulated frame. The renderer reads only this, never the World.
public sealed class FrameSnapshot
{
    public readonly List<DrawItem> Items = new(512);
    public long Score, HighScore;
    public int Reserve, Wave, PhaseFrames, PhaseLength;
    public GamePhase Phase;
    public bool BrainWave;
    public int BlockingRemaining, BlockingAtStart;
    public long Tick;
    public int PlayerX, PlayerY;
    /// 0 = normal; during death: frames since death.
    public int DeathFrames;
    public bool PlayerVisible;
    public float PlayerAppear = 1;

    public static void Fill(GameSession g, FrameSnapshot s)
    {
        var w = g.World;
        s.Items.Clear();
        s.Score = g.Score; s.Reserve = g.Reserve; s.Wave = g.Wave; s.Phase = g.Phase;
        s.PhaseFrames = g.PhaseFrames; s.PhaseLength = g.PhaseLength; s.BrainWave = g.IsBrainWave;
        s.Tick = g.TickCount;
        s.BlockingRemaining = w.BlockingCount();
        var c = g.WaveFullCounts;   // progress spans deaths: a restart must not reset the meter
        s.BlockingAtStart = c.Grunts + c.Brains + c.Sphereoids + c.Quarks;
        s.PlayerX = w.Player.Px; s.PlayerY = w.Player.Py;
        s.DeathFrames = g.Phase is GamePhase.PlayerDying or GamePhase.GameOver ? (g.Phase == GamePhase.GameOver ? GameSession.DeathFrames : g.PhaseFrames) : 0;

        bool intro = g.Phase == GamePhase.WaveIntro;
        int robots = w.Enemies.Count(e => e is Grunt or Hulk or Brain);
        if (g.Phase is GamePhase.WaveCleared or GamePhase.Finished) { s.PlayerVisible = false; AddEffects(w, s); return; }

        float Appear(int order) =>
            !intro ? 1f
            : g.IsBrainWave ? Math.Clamp(g.PhaseFrames / 60f, 0, 1)
            : Math.Clamp((g.PhaseFrames - order) / (float)GameSession.AppearFrames, 0, 1);
        byte style = (byte)(g.IsBrainWave ? 2 : 0);

        foreach (var e in w.Electrodes) Add(s, e, Appear(0), style);
        foreach (var h in w.Family)
        {
            var it = Item(h, Appear(0), style);
            if (h.FacingLeft) it.Flags |= DrawFlags.Mirror;
            if (h.CapturedBy is not null && (g.TickCount / 4) % 2 == 0) it.Flags |= DrawFlags.Silhouette;
            s.Items.Add(it);
        }
        foreach (var e in w.Enemies)
        {
            if (e is Prog p) foreach (var (tx, ty) in p.Trail) s.Items.Add(new DrawItem { Kind = DrawKind.Trail, Sprite = p.Image, X = tx, Y = ty, Entity = p.Kind, Flags = DrawFlags.Dim });
            if (e is CruiseMissile m) foreach (var (tx, ty) in m.Trail) s.Items.Add(new DrawItem { Kind = DrawKind.Trail, X = tx, Y = ty, Entity = m.Kind });
            var it = Item(e, e is Grunt or Hulk or Brain ? Appear(e.AppearOrder) : Appear(robots), (byte)(e.AppearOrder % 4 == 3 && !g.IsBrainWave ? 1 : style));
            if (e is Enforcer { Harmful: false } en) { it.Kind = DrawKind.Appear; it.Progress = en.Age / (float)Enforcer.GrowFrames; it.Value = 0; }
            if (e is Tank { Harmful: false } tk) { it.Kind = DrawKind.Appear; it.Progress = tk.Age / (float)Tank.GrowFrames; it.Value = 0; }
            s.Items.Add(it);
        }
        foreach (var shot in w.Shots)
            s.Items.Add(new DrawItem { Kind = DrawKind.Shot, Sprite = shot.Image, X = shot.Px, Y = shot.Py, VX = shot.VX, VY = shot.VY, Entity = EntityKind.Shot });
        AddEffects(w, s);

        s.PlayerVisible = g.Phase != GamePhase.GameOver || false;
        s.PlayerAppear = intro ? Math.Clamp((g.PhaseFrames - (g.IsBrainWave ? GameSession.BrainWaveIntroFrames - GameSession.PlayerAppearFrames : robots + GameSession.AppearFrames)) / (float)GameSession.PlayerAppearFrames, 0, 1) : 1;
        var pi = new DrawItem { Kind = s.PlayerAppear < 1 ? DrawKind.Appear : DrawKind.Sprite, Sprite = w.Player.Image, X = w.Player.Px, Y = w.Player.Py, Progress = s.PlayerAppear, Entity = EntityKind.Player };
        if (g.Phase == GamePhase.GameOver) return;
        if (g.Phase == GamePhase.PlayerDying && g.PhaseFrames < GameSession.DeathFlashFrames && g.PhaseFrames % 8 < 2) pi.Flags |= DrawFlags.Silhouette;
        if (s.PlayerAppear > 0) s.Items.Add(pi);
    }

    static DrawItem Item(Entity e, float appear, byte style) => new()
    {
        Kind = appear < 1 ? DrawKind.Appear : DrawKind.Sprite, Sprite = e.Image, X = e.Px, Y = e.Py, Progress = appear, Value = style, Entity = e.Kind,
    };

    static void Add(FrameSnapshot s, Entity e, float appear, byte style) => s.Items.Add(Item(e, appear, style));

    static void AddEffects(World w, FrameSnapshot s)
    {
        foreach (var fx in w.Effects)
        {
            float t = fx.Age / (float)fx.Duration;
            s.Items.Add(fx.Kind switch
            {
                EffectKind.Explosion => new DrawItem { Kind = DrawKind.Explosion, Sprite = fx.Sprite, X = fx.X, Y = fx.Y, VX = fx.DirX, VY = fx.DirY, Progress = t },
                EffectKind.ElectrodeFade => new DrawItem { Kind = DrawKind.ElectrodeFade, Sprite = fx.Sprite, X = fx.X, Y = fx.Y, Progress = t, Entity = EntityKind.Electrode },
                EffectKind.Skull => new DrawItem { Kind = DrawKind.Sprite, Sprite = fx.Sprite, X = fx.X, Y = fx.Y, Progress = t },
                EffectKind.ScorePopup => new DrawItem { Kind = DrawKind.Popup, X = fx.X, Y = fx.Y, Value = fx.Value, Progress = t },
                _ => new DrawItem { Kind = DrawKind.WallGlow, X = fx.X, Y = fx.Y, VX = fx.DirX, VY = fx.DirY, Progress = t },
            });
        }
    }
}
