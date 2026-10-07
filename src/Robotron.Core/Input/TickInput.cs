namespace Robotron.Core.Input;

/// A stick deflection quantised to signed bytes (-127..127). Y grows downward (screen space).
/// Integer storage keeps replays and suspend saves bit-exact regardless of the input device.
public readonly record struct StickInput(sbyte X, sbyte Y)
{
    public static readonly StickInput Neutral = default;
    public bool IsNeutral => X == 0 && Y == 0;
    public int DirX => Math.Sign(X);
    public int DirY => Math.Sign(Y);
}

[Flags]
public enum InputButtons : byte
{
    None = 0,
    Start = 1,
    Pause = 2,
}

/// Everything the engine is told about the player for one simulation tick.
public readonly record struct TickInput(StickInput Move, StickInput Fire, InputButtons Buttons = InputButtons.None)
{
    public static readonly TickInput None = default;
}

public static class StickQuantizer
{
    /// Analog vector (-1..1 per axis, Y down) to a signed-byte stick, applying a radial deadzone.
    public static StickInput Analog(float x, float y, float deadzone)
    {
        float mag = MathF.Sqrt(x * x + y * y);
        if (mag <= deadzone || mag == 0) return StickInput.Neutral;
        float scaled = Math.Min(1f, (mag - deadzone) / (1f - deadzone)) / mag;
        return new StickInput(ToByte(x * scaled), ToByte(y * scaled));
    }

    /// Analog vector to an 8-way digital stick, as the cabinet's leaf-switch joysticks would read it.
    /// Sectors are 45 degrees wide centred on each of the eight directions.
    public static StickInput EightWay(float x, float y, float deadzone)
    {
        float mag = MathF.Sqrt(x * x + y * y);
        if (mag <= deadzone) return StickInput.Neutral;
        double angle = Math.Atan2(y, x);
        int sector = (int)Math.Round(angle / (Math.PI / 4)) & 7;
        return Digital(SectorX[sector], SectorY[sector]);
    }

    /// Discrete switches (keyboard keys or a d-pad). Opposing switches cancel to zero on that axis.
    public static StickInput Digital(bool left, bool right, bool up, bool down) =>
        Digital((right ? 1 : 0) - (left ? 1 : 0), (down ? 1 : 0) - (up ? 1 : 0));

    public static StickInput Digital(int dx, int dy) => new((sbyte)(Math.Sign(dx) * 127), (sbyte)(Math.Sign(dy) * 127));

    /// Collapse any stick to its 8-way digital equivalent (used by Classic policy).
    public static StickInput ToEightWay(StickInput s) =>
        s.IsNeutral ? s : EightWay(s.X / 127f, s.Y / 127f, 0.0001f);

    static readonly int[] SectorX = [1, 1, 0, -1, -1, -1, 0, 1];
    static readonly int[] SectorY = [0, 1, 1, 1, 0, -1, -1, -1];
    static sbyte ToByte(float v) => (sbyte)Math.Clamp((int)MathF.Round(v * 127f), -127, 127);
}
