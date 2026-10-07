namespace AVATron.Core.Simulation;

/// Rule-affecting policy. Classic and Modern presets share the engine and differ only here
/// (presentation differences such as flicker or audio mixing live in the host).
public sealed record GameRules
{
    /// Operator "difficulty of play" 0..10. Factory (Solid Blue Label) = 3; 5 = raw table.
    public int Difficulty { get; init; } = Waves.WaveTable.FactoryDifficulty;
    public int LivesPerGame { get; init; } = 3;
    public int ExtraLifeEvery { get; init; } = 25_000;
    /// Movement stick: false = 8-way switches (arcade); true = analog magnitude/direction (Modern).
    public bool AnalogMove { get; init; }
    /// Fire stick: false = 8 directions (arcade); true = any angle (Modern).
    public bool AnalogFire { get; init; }
    public int StartWave { get; init; } = 1;

    public static GameRules Classic { get; } = new();
    public static GameRules Modern { get; } = new() { AnalogMove = true, AnalogFire = true };
}
