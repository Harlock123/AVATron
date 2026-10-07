using AVATron.Core.Sprites;

namespace AVATron.Avalonia.Rendering;

/// A small software framebuffer (32-bit BGRA). All game and menu drawing goes through here so pixels stay
/// exact; the result is blitted into a WriteableBitmap and scaled by the GPU.
public sealed class FrameBuffer(int width, int height)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public uint[] Pixels { get; } = new uint[width * height];

    public void Clear(uint c = 0xFF000000) => Array.Fill(Pixels, c);

    public void Set(int x, int y, uint c)
    {
        if ((uint)x < (uint)Width && (uint)y < (uint)Height) Pixels[y * Width + x] = c;
    }

    public void FillRect(int x, int y, int w, int h, uint c)
    {
        int x0 = Math.Max(0, x), y0 = Math.Max(0, y), x1 = Math.Min(Width, x + w), y1 = Math.Min(Height, y + h);
        for (int yy = y0; yy < y1; yy++) Array.Fill(Pixels, c, yy * Width + x0, Math.Max(0, x1 - x0));
    }

    public void Rect(int x, int y, int w, int h, uint c, int thickness = 1)
    {
        FillRect(x, y, w, thickness, c); FillRect(x, y + h - thickness, w, thickness, c);
        FillRect(x, y, thickness, h, c); FillRect(x + w - thickness, y, thickness, h, c);
    }

    public void Line(int x0, int y0, int x1, int y1, uint c)
    {
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
        while (true)
        {
            Set(x0, y0, c);
            if (x0 == x1 && y0 == y1) return;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    /// Draw a palette sprite. silhouette != 0 paints every solid pixel that colour.
    public void Sprite(Sprite s, int x, int y, uint[] pal, bool mirror = false, uint silhouette = 0, bool dim = false)
    {
        for (int sy = 0; sy < s.Height; sy++)
        {
            int py = y + sy;
            if ((uint)py >= (uint)Height) continue;
            int row = sy * s.Width;
            for (int sx = 0; sx < s.Width; sx++)
            {
                byte idx = s.Pixels[row + (mirror ? s.Width - 1 - sx : sx)];
                if (idx == Pal.Clear) continue;
                int px = x + sx;
                if ((uint)px >= (uint)Width) continue;
                uint c = silhouette != 0 ? silhouette : pal[idx];
                Pixels[py * Width + px] = dim ? WilliamsPalette.Dim(c) : c;
            }
        }
    }

    /// Draw a sprite whose rows (vertical) and/or columns (horizontal) are spread apart around its centre:
    /// spread 1 = normal. This is the Williams-style appear/explode "line spread" look.
    public void SpreadSprite(Sprite s, int x, int y, uint[] pal, float spreadX, float spreadY, uint tint = 0)
    {
        float cx = x + s.Width / 2f, cy = y + s.Height / 2f;
        for (int sy = 0; sy < s.Height; sy++)
        {
            int py = (int)MathF.Round(cy + (sy - s.Height / 2f) * spreadY);
            for (int sx = 0; sx < s.Width; sx++)
            {
                byte idx = s.Pixels[sy * s.Width + sx];
                if (idx == Pal.Clear) continue;
                int px = (int)MathF.Round(cx + (sx - s.Width / 2f) * spreadX);
                Set(px, py, tint != 0 ? tint : pal[idx]);
            }
        }
    }

    /// Scaled sprite (nearest neighbour) around its centre, for grow-in and shrink effects.
    public void ScaledSprite(Sprite s, int x, int y, uint[] pal, float scale)
    {
        if (scale <= 0.01f) return;
        int w = Math.Max(1, (int)(s.Width * scale)), h = Math.Max(1, (int)(s.Height * scale));
        int ox = x + (s.Width - w) / 2, oy = y + (s.Height - h) / 2;
        for (int dy = 0; dy < h; dy++)
            for (int dx = 0; dx < w; dx++)
            {
                byte idx = s.Pixels[(dy * s.Height / h) * s.Width + dx * s.Width / w];
                if (idx != Pal.Clear) Set(ox + dx, oy + dy, pal[idx]);
            }
    }

    public int Text(string text, int x, int y, uint c, int scale = 1)
    {
        int cx = x;
        foreach (char ch in text)
        {
            if (PixelFont.TryGet(ch, out var rows))
                for (int ry = 0; ry < PixelFont.GlyphHeight; ry++)
                    for (int rx = 0; rx < PixelFont.GlyphWidth; rx++)
                        if ((rows[ry] & (1 << (PixelFont.GlyphWidth - 1 - rx))) != 0)
                            if (scale == 1) Set(cx + rx, y + ry, c); else FillRect(cx + rx * scale, y + ry * scale, scale, scale, c);
            cx += PixelFont.Advance * scale;
        }
        return cx;
    }

    public void TextCentered(string text, int y, uint c, int scale = 1) =>
        Text(text, (Width - PixelFont.Measure(text) * scale) / 2, y, c, scale);
}
