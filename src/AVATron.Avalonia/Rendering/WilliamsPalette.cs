using AVATron.Core.Sprites;

namespace AVATron.Avalonia.Rendering;

/// Colours constrained to the arcade's 256-colour space: one byte BBGGGRRR through the board's resistor
/// DAC (levels from MAME's resistor-weight model, RESEARCH.md §Palette). Specific picks and cycling
/// sequences below are this project's own choices within that space.
public static class WilliamsPalette
{
    static readonly byte[] RG = [0, 38, 81, 118, 137, 174, 217, 255];
    static readonly byte[] B = [0, 95, 160, 255];

    /// BBGGGRRR -> 0xAARRGGBB (premultiplied opaque, BGRA in memory on little-endian).
    public static uint Decode(byte v) => 0xFF000000u | (uint)RG[v & 7] << 16 | (uint)RG[(v >> 3) & 7] << 8 | B[v >> 6];

    public static uint Rgb(int r3, int g3, int b2) => Decode((byte)((b2 << 6) | (g3 << 3) | r3));

    /// Static palette for Pal.* entries 1..13 (cycle entries filled per frame).
    public static readonly uint[] Base =
    [
        0,                 // Clear
        Rgb(7, 7, 3),      // White
        Rgb(7, 0, 0),      // Red
        Rgb(0, 7, 0),      // Green
        Rgb(0, 0, 3),      // Blue
        Rgb(7, 7, 0),      // Yellow
        Rgb(0, 7, 3),      // Cyan
        Rgb(7, 0, 3),      // Magenta
        Rgb(7, 2, 0),      // Orange
        Rgb(7, 5, 1),      // Skin
        Rgb(4, 4, 2),      // Grey
        Rgb(4, 0, 0),      // DarkRed
        Rgb(4, 5, 3),      // LightBlue
        Rgb(7, 4, 2),      // Pink
        0, 0,
    ];

    /// Rainbow used for Cycle1 (steps every 2 frames) and laser colours.
    public static readonly uint[] Rainbow =
    [
        Rgb(7,0,0), Rgb(7,2,0), Rgb(7,4,0), Rgb(7,7,0), Rgb(4,7,0), Rgb(0,7,0), Rgb(0,7,2), Rgb(0,7,3),
        Rgb(0,4,3), Rgb(0,0,3), Rgb(3,0,3), Rgb(7,0,3), Rgb(7,0,2), Rgb(7,7,3),
    ];

    /// Blue-purple-red ramp used for Cycle2 (steps every frame).
    public static readonly uint[] BluePurpleRed =
    [
        Rgb(0,0,1), Rgb(0,0,2), Rgb(0,0,3), Rgb(1,0,3), Rgb(2,0,3), Rgb(3,0,3), Rgb(4,0,3), Rgb(5,0,3), Rgb(6,0,3), Rgb(7,0,3),
        Rgb(7,0,2), Rgb(7,0,1), Rgb(7,0,0), Rgb(7,1,0), Rgb(7,2,0), Rgb(7,1,0), Rgb(7,0,0), Rgb(7,0,1), Rgb(7,0,2), Rgb(7,0,3),
        Rgb(5,0,3), Rgb(3,0,3), Rgb(1,0,3), Rgb(0,0,3), Rgb(0,0,2), Rgb(0,0,1),
    ];

    /// Per-wave border/wall colours, cycling every ten waves (the arcade changes the border every wave).
    public static readonly uint[] Walls =
    [
        Rgb(7,2,0), Rgb(7,7,0), Rgb(7,0,0), 0 /* cycles */, Rgb(0,0,3), Rgb(7,0,3), Rgb(7,3,0), Rgb(4,4,2), Rgb(0,7,0), Rgb(4,1,3),
    ];

    /// Builds the 16-entry palette for one frame. ReducedMotion slows cycling to a calmer pace.
    public static void ForFrame(uint[] dest, long frame, bool reducedMotion)
    {
        Array.Copy(Base, dest, 16);
        long f = reducedMotion ? frame / 8 : frame;
        dest[Pal.Cycle1] = Rainbow[(f / 2) % Rainbow.Length];
        dest[Pal.Cycle2] = BluePurpleRed[f % BluePurpleRed.Length];
    }

    public static uint WallColour(int wave, long frame, bool reducedMotion)
    {
        uint c = Walls[(Math.Max(1, wave) - 1) % 10];
        return c != 0 ? c : BluePurpleRed[(reducedMotion ? frame / 8 : frame) % BluePurpleRed.Length];
    }

    public static uint LaserColour(int wave, long frame, bool reducedMotion) =>
        reducedMotion ? Rgb(7, 7, 3) : Rainbow[((frame / 2) + wave * 3) % Rainbow.Length];

    public static uint Dim(uint c) => 0xFF000000u | ((c >> 1) & 0x007F7F7Fu);
}
