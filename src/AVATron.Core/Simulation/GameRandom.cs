namespace AVATron.Core.Simulation;

/// Injectable randomness. All simulation randomness must flow through this so a seed plus an input
/// sequence reproduces a game exactly (replays, tests, suspend saves).
public interface IGameRandom
{
    uint NextUInt();
    /// Uniform integer in [0, maxExclusive).
    int Next(int maxExclusive);
    /// Uniform integer in [minInclusive, maxExclusive).
    int Next(int minInclusive, int maxExclusive);
    bool Chance(int numerator, int denominator);
    ulong State { get; set; }
}

/// xorshift64* — small, fast, fully deterministic, and its whole state is one serialisable ulong.
/// (The arcade used its own 8-bit generator; we do not claim bit-exact random sequences — see FIDELITY.md.)
public sealed class XorShiftRandom : IGameRandom
{
    ulong _s;
    public XorShiftRandom(ulong seed) { _s = seed == 0 ? 0x9E3779B97F4A7C15UL : seed; }
    public ulong State { get => _s; set => _s = value == 0 ? 0x9E3779B97F4A7C15UL : value; }

    public uint NextUInt()
    {
        _s ^= _s >> 12; _s ^= _s << 25; _s ^= _s >> 27;
        return (uint)((_s * 0x2545F4914F6CDD1DUL) >> 32);
    }

    public int Next(int maxExclusive) => maxExclusive <= 0 ? 0 : (int)((ulong)NextUInt() * (ulong)maxExclusive >> 32);
    public int Next(int minInclusive, int maxExclusive) => minInclusive + Next(maxExclusive - minInclusive);
    public bool Chance(int numerator, int denominator) => Next(denominator) < numerator;
}
