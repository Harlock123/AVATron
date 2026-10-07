using Robotron.Core.Simulation;
using Robotron.Core.Sprites;

namespace Robotron.Avalonia.Rendering;

public sealed record GameRenderOptions
{
    /// Classic: approximate the arcade's flicker when the screen is busy.
    public bool Flicker { get; init; }
    public bool ReducedMotion { get; init; }
    /// Modern HUD: high score, wave progress, larger always-visible labels.
    public bool ModernHud { get; init; }
    public bool WaveProgress { get; init; }
    public string? StatusNote { get; init; }
}

/// Draws a FrameSnapshot into the 292x240 framebuffer (the monitor's visible area of the raw 304x256 screen).
public sealed class GameRenderer
{
    public const int Width = Arena.ViewWidth, Height = Arena.ViewHeight;
    const int OX = Arena.ViewX, OY = Arena.ViewY;

    /// Approximate per-frame draw budget before flicker begins (reconstruction; see FIDELITY.md §Flicker).
    public const int FlickerBudget = 72;

    readonly uint[] _pal = new uint[16];
    long _frame;

    public void Render(FrameSnapshot s, FrameBuffer fb, GameRenderOptions o)
    {
        _frame = s.Tick;
        WilliamsPalette.ForFrame(_pal, s.Tick, o.ReducedMotion);
        fb.Clear();

        if (s.Phase == GamePhase.WaveCleared) { DrawMarquee(s, fb, o); DrawHud(s, fb, o); return; }

        // Border in the wave colour.
        uint wall = WilliamsPalette.WallColour(s.Wave, s.Tick, o.ReducedMotion);
        if (s.Phase == GamePhase.PlayerDying && s.DeathFrames >= GameSession.DeathFlashFrames)
            wall = Fade(wall, (s.DeathFrames - GameSession.DeathFlashFrames) / (float)GameSession.DeathFadeFrames);
        fb.FillRect(Arena.BorderLeft - OX, Arena.BorderTop - OY, Arena.BorderRight - Arena.BorderLeft + 1, 2, wall);
        fb.FillRect(Arena.BorderLeft - OX, Arena.BorderBottom - 1 - OY, Arena.BorderRight - Arena.BorderLeft + 1, 2, wall);
        fb.FillRect(Arena.BorderLeft - OX, Arena.BorderTop - OY, 2, Arena.BorderBottom - Arena.BorderTop + 1, wall);
        fb.FillRect(Arena.BorderRight - 1 - OX, Arena.BorderTop - OY, 2, Arena.BorderBottom - Arena.BorderTop + 1, wall);

        bool fading = s.Phase is GamePhase.PlayerDying && s.DeathFrames >= GameSession.DeathFlashFrames;
        float fade = fading ? (s.DeathFrames - GameSession.DeathFlashFrames) / (float)GameSession.DeathFadeFrames : 0;
        bool gameOver = s.Phase is GamePhase.GameOver or GamePhase.Finished;

        if (!gameOver)
        {
            // Flicker: when more objects than the budget, draw a rotating window of them each frame.
            var items = s.Items;
            int n = items.Count, skipEvery = 0;
            if (o.Flicker && n > FlickerBudget) skipEvery = Math.Max(2, (int)Math.Ceiling(n / (double)(n - FlickerBudget)));
            for (int i = 0; i < n; i++)
            {
                var it = items[i];
                if (skipEvery > 0 && it.Entity != EntityKind.Player && (i + _frame) % skipEvery == 0) continue;
                DrawItem(it, fb, s, o, fade);
            }
        }

        DrawHud(s, fb, o);
        if (gameOver) fb.TextCentered("GAME OVER", 120, WilliamsPalette.Rgb(7, 0, 0), 2);
    }

    void DrawItem(in DrawItem it, FrameBuffer fb, FrameSnapshot s, GameRenderOptions o, float fade)
    {
        int x = it.X - OX, y = it.Y - OY;
        uint[] pal = _pal;
        if (fade > 0) { pal = (uint[])_pal.Clone(); for (int k = 1; k < 16; k++) pal[k] = Fade(pal[k], fade); }
        switch (it.Kind)
        {
            case DrawKind.Sprite:
                uint sil = (it.Flags & DrawFlags.Silhouette) != 0 ? (it.Entity == EntityKind.Player ? WilliamsPalette.Rgb(7, 7, 3) : _pal[Pal.Cycle1]) : 0;
                fb.Sprite(it.Sprite!, x, y, pal, (it.Flags & DrawFlags.Mirror) != 0, sil);
                break;
            case DrawKind.Appear:
                DrawAppear(it, fb, x, y, o);
                break;
            case DrawKind.Shot:
                uint lc = WilliamsPalette.LaserColour(s.Wave, _frame, o.ReducedMotion);
                if (it.VX != 0 && it.VY != 0 && Math.Abs(it.VX) != Math.Abs(it.VY))
                {
                    // Analog (Modern) angle: a 6-pixel streak along the velocity.
                    double len = Math.Sqrt((double)it.VX * it.VX + (double)it.VY * it.VY);
                    int ex = x + (int)Math.Round(it.VX / len * 5), ey = y + (int)Math.Round(it.VY / len * 5);
                    fb.Line(x, y, ex, ey, lc);
                }
                else fb.Sprite(it.Sprite!, x, y, pal, silhouette: lc);
                break;
            case DrawKind.Explosion:
            {
                float t = it.Progress, spread = 1 + t * (o.ReducedMotion ? 3 : 9);
                float sx = it.VX != 0 || it.VY == 0 ? spread : 1, sy = it.VY != 0 || it.VX == 0 ? spread : 1;
                fb.SpreadSprite(it.Sprite!, x, y, pal, sx, sy, t > 0.6f ? WilliamsPalette.Dim(_pal[Pal.Cycle2]) : 0);
                break;
            }
            case DrawKind.ElectrodeFade:
                fb.ScaledSprite(it.Sprite!, x, y, pal, 1 - it.Progress);
                break;
            case DrawKind.Popup:
                fb.Text(it.Value.ToString(), x, y, _pal[Pal.Cycle1]);
                break;
            case DrawKind.WallGlow:
                if (!o.ReducedMotion || it.Progress < 0.5f)
                {
                    uint g = WilliamsPalette.LaserColour(s.Wave, _frame, o.ReducedMotion);
                    if (it.VX != 0) fb.FillRect(x - 1, y - 3, 2, 8, g); else fb.FillRect(x - 3, y - 1, 8, 2, g);
                }
                break;
            case DrawKind.Trail:
                if (it.Sprite is not null) fb.Sprite(it.Sprite, x, y, pal, dim: true);
                else fb.FillRect(x - 1, y - 1, 2, 2, WilliamsPalette.Dim(_pal[Pal.Cycle1]));
                break;
        }
    }

    void DrawAppear(in DrawItem it, FrameBuffer fb, int x, int y, GameRenderOptions o)
    {
        float p = it.Progress;
        if (p <= 0) return;
        var sprite = it.Sprite!;
        if (it.Value == 2)
        {
            // "Transporter": pixels sparkle in progressively.
            uint seed = (uint)(it.X * 7919 + it.Y * 104729);
            for (int sy = 0; sy < sprite.Height; sy++)
                for (int sx = 0; sx < sprite.Width; sx++)
                {
                    byte idx = sprite.Pixels[sy * sprite.Width + sx];
                    if (idx == Pal.Clear) continue;
                    uint h = Hash(seed + (uint)(sy * 31 + sx));
                    if ((h & 0xFF) / 255f < p) fb.Set(x + sx, y + sy, _pal[idx]);
                    else if (!o.ReducedMotion && ((h >> 8) + (uint)_frame) % 7 == 0) fb.Set(x + sx + (int)(h % 5) - 2, y + sy, _pal[Pal.Cycle1]);
                }
            return;
        }
        if (it.Entity is EntityKind.Enforcer or EntityKind.Tank) { fb.ScaledSprite(sprite, x, y, _pal, Math.Max(0.2f, p)); return; }
        float spread = 1 + (1 - p) * (o.ReducedMotion ? 2 : 10);
        if (it.Value == 1) fb.SpreadSprite(sprite, x, y, _pal, spread, 1);
        else fb.SpreadSprite(sprite, x, y, _pal, 1, spread);
    }

    void DrawMarquee(FrameSnapshot s, FrameBuffer fb, GameRenderOptions o)
    {
        // Wave-transition rectangles expanding from the centre in cycling colours (reconstruction of the arcade's marquee).
        int f = s.PhaseFrames;
        int cx = Width / 2, cy = (Arena.Top + Arena.Bottom) / 2 - OY;
        int rings = o.ReducedMotion ? 3 : 10;
        for (int k = 0; k < rings; k++)
        {
            int r = f * 3 - k * 10;
            if (r <= 0) continue;
            int rw = Math.Min(r * 2, Width - 4), rh = Math.Min((int)(r * 1.6f), Height - 20);
            uint c = o.ReducedMotion ? WilliamsPalette.Rgb(4, 4, 2) : WilliamsPalette.Rainbow[(k + f / 2) % WilliamsPalette.Rainbow.Length];
            fb.Rect(cx - rw / 2, cy - rh / 2, rw, rh, c);
        }
        fb.TextCentered($"WAVE {s.Wave} COMPLETED", cy - 4, WilliamsPalette.Rgb(7, 7, 3));
    }

    void DrawHud(FrameSnapshot s, FrameBuffer fb, GameRenderOptions o)
    {
        uint scoreCol = WilliamsPalette.Rgb(0, 7, 3);
        int hy = Arena.HudY - OY;
        fb.Text(s.Score.ToString().PadLeft(7), Arena.ScoreX - OX, hy, scoreCol);
        // Reserve men, up to 7 icons, after the score.
        int icons = Math.Min(7, s.Reserve);
        for (int i = 0; i < icons; i++) fb.Sprite(SpriteLibrary.LifeIcon, Arena.LivesX - OX + i * 8, hy - 1, _pal);
        if (s.Reserve > 7 && o.ModernHud) fb.Text($"+{s.Reserve - 7}", Arena.LivesX - OX + 58, hy, scoreCol);

        // Wave counter under the playfield.
        string wave = $"{s.Wave} WAVE";
        fb.TextCentered(wave, Arena.BorderBottom + 2 - OY, WilliamsPalette.Rgb(0, 7, 3));

        if (o.ModernHud)
        {
            fb.Text("HI " + s.HighScore.ToString().PadLeft(7), 196, hy, WilliamsPalette.Rgb(7, 7, 0));
            if (o.WaveProgress && s.BlockingAtStart > 0 && s.Phase == GamePhase.Playing)
            {
                int bw = 60, filled = bw - bw * Math.Min(s.BlockingRemaining, s.BlockingAtStart) / s.BlockingAtStart;
                int bx = Width - bw - 10, by = Arena.BorderBottom + 3 - OY;
                fb.Rect(bx - 1, by - 1, bw + 2, 7, WilliamsPalette.Rgb(4, 4, 2));
                fb.FillRect(bx, by, filled, 5, WilliamsPalette.Rgb(0, 7, 0));
                fb.Text($"{s.BlockingRemaining}", bx - 24, by - 1, WilliamsPalette.Rgb(4, 4, 2));
            }
        }
        if (o.StatusNote is { } note) fb.Text(note, 8, Arena.BorderBottom + 2 - OY, WilliamsPalette.Rgb(7, 4, 0));
    }

    static uint Fade(uint c, float t)
    {
        t = Math.Clamp(1 - t, 0, 1);
        uint r = (uint)(((c >> 16) & 0xFF) * t), g = (uint)(((c >> 8) & 0xFF) * t), b = (uint)((c & 0xFF) * t);
        return 0xFF000000u | r << 16 | g << 8 | b;
    }

    static uint Hash(uint x) { x ^= x >> 16; x *= 0x7feb352d; x ^= x >> 15; x *= 0x846ca68b; x ^= x >> 16; return x; }
}
