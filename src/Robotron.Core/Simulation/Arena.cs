namespace Robotron.Core.Simulation;

/// Geometry in the arcade's raw screen space: X in pixels (0..303), Y in scan lines (0..255).
/// Positions are 8.8 fixed point (<see cref="One"/> = 1 pixel or 1 line) so the original's fractional
/// byte arithmetic maps to exact integers (1 arcade "byte" = 2 px = 512 units).
public static class Arena
{
    public const int One = 256;

    /// Playfield interior, pixels/lines. Objects keep their whole image inside [Left, Right) x [Top, Bottom).
    public const int Left = 14, Right = 288, Top = 24, Bottom = 235;

    /// Outer border rectangle (drawn in the wave's wall colour): px 12..289, lines 22..236 inclusive.
    public const int BorderLeft = 12, BorderRight = 289, BorderTop = 22, BorderBottom = 236;

    /// Visible window shown by the monitor (MAME visible area): 292 x 240 starting at raw (6, 7).
    public const int ViewX = 6, ViewY = 7, ViewWidth = 292, ViewHeight = 240;

    /// Score line (raw Y) and Player 1 score X.
    public const int HudY = 14, ScoreX = 48, LivesX = 92;

    /// Player start (top-left).
    public const int PlayerStartX = 148, PlayerStartY = 124;

    /// Arcade frame rate: 8 MHz pixel clock / (512 x 260) = 60.096 Hz.
    public const double FrameRate = 8_000_000.0 / (512 * 260);

    public static int MinX(int width) => Left * One;
    public static int MaxX(int width) => (Right - width) * One;
    public static int MinY(int height) => Top * One;
    public static int MaxY(int height) => (Bottom - height) * One;

    public static int ClampX(int x, int width) => Math.Clamp(x, MinX(width), MaxX(width));
    public static int ClampY(int y, int height) => Math.Clamp(y, MinY(height), MaxY(height));
}
