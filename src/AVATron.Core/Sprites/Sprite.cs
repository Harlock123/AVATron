namespace AVATron.Core.Sprites;

/// 16-entry logical palette (the arcade showed 16 colours at once). Renderer maps these to RGB;
/// Cycle* entries are animated by the renderer each frame (colour-cycling effects).
public static class Pal
{
    public const byte Clear = 0, White = 1, Red = 2, Green = 3, Blue = 4, Yellow = 5, Cyan = 6, Magenta = 7,
        Orange = 8, Skin = 9, Grey = 10, DarkRed = 11, LightBlue = 12, Pink = 13, Cycle1 = 14, Cycle2 = 15;

    public static byte FromChar(char c) => c switch
    {
        '.' or ' ' => Clear, 'W' => White, 'R' => Red, 'G' => Green, 'B' => Blue, 'Y' => Yellow, 'C' => Cyan,
        'M' => Magenta, 'O' => Orange, 'S' => Skin, 'K' => Grey, 'r' => DarkRed, 'L' => LightBlue, 'P' => Pink,
        '1' => Cycle1, '2' => Cycle2,
        _ => throw new ArgumentException($"unknown palette char '{c}'"),
    };
}

/// One image: palette indices plus its collision mask (every non-clear pixel is solid).
public sealed class Sprite
{
    public Sprite(string name, int width, int height, byte[] pixels)
    {
        Name = name; Width = width; Height = height; Pixels = pixels;
        Mask = new ulong[height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (pixels[y * width + x] != Pal.Clear) Mask[y] |= 1UL << x;
    }

    public string Name { get; }
    public int Width { get; }
    public int Height { get; }
    public byte[] Pixels { get; }
    /// Per-row bitmask, bit x set = solid. Widths are at most 64.
    public ulong[] Mask { get; }

    public static Sprite Parse(string name, string art)
    {
        var rows = art.Split('|', StringSplitOptions.TrimEntries);
        int w = rows[0].Length;
        if (w > 64 || rows.Any(r => r.Length != w)) throw new ArgumentException($"sprite {name}: ragged or too wide");
        var px = new byte[w * rows.Length];
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < w; x++) px[y * w + x] = Pal.FromChar(rows[y][x]);
        return new Sprite(name, w, rows.Length, px);
    }

    public static Sprite Solid(string name, int w, int h, byte colour = Pal.White) =>
        new(name, w, h, Enumerable.Repeat(colour, w * h).ToArray());

    /// Pixel-mask overlap test (bounding boxes first). Positions are integer pixels of the top-left corners.
    public static bool Overlaps(Sprite a, int ax, int ay, Sprite b, int bx, int by)
    {
        int x0 = Math.Max(ax, bx), x1 = Math.Min(ax + a.Width, bx + b.Width);
        int y0 = Math.Max(ay, by), y1 = Math.Min(ay + a.Height, by + b.Height);
        if (x0 >= x1 || y0 >= y1) return false;
        for (int y = y0; y < y1; y++)
        {
            ulong ma = Shift(a.Mask[y - ay], ax - x0), mb = Shift(b.Mask[y - by], bx - x0);
            if ((ma & mb) != 0) return true;
        }
        return false;
    }

    // Re-base a row mask so bit 0 is column x0; offset = spriteX - x0 (<= 0 here).
    static ulong Shift(ulong m, int offset) => offset >= 0 ? m << offset : m >> -offset;
}
