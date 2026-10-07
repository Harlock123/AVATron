using System.Text.Json;
using System.Text.Json.Serialization;

namespace AVATron.Core.Waves;

/// Entity counts for one wave.
public sealed record WaveCounts(int Grunts, int Electrodes, int Mommies, int Daddies, int Mikeys, int Hulks, int Brains, int Sphereoids, int Quarks)
{
    public int Family => Mommies + Daddies + Mikeys;
}

/// The twelve per-wave tuning parameters, in the arcade program's own units (see FIDELITY.md §Waves).
public sealed record WaveParameters(
    int GruntSpeed,          // grunt step timer ceiling, in 4-frame manager passes
    int GruntSpeedFloor,     // lowest value GruntSpeed may be driven to
    int SpawnerChildren2x,   // children per Sphereoid/Quark = ceil(RMAX(n)/2)
    int EnforcerShotTimer,   // RMAX ceiling, in 3-frame enforcer ticks
    int SphereoidDropTimer,  // RMAX ceiling, in 10-frame cycles
    int HulkSleep,           // frames between Hulk steps
    int BrainMissileTimer,   // RMAX ceiling, in Brain steps
    int BrainSleep,          // frames between Brain steps
    int TankShotSteps,       // Tank steps between shots
    int TankShellSpeed,      // shell speed factor /256
    int QuarkDropTimer,      // RMAX ceiling, in 15-frame cycles
    int QuarkSpeed);         // Quark speed factor

public sealed record WaveDefinition(int Wave, WaveCounts Counts, WaveParameters Parameters);

public sealed class WaveDataException(string message) : Exception(message);

/// Data-driven wave table. Behaviour code never hard-codes wave contents; it asks this class.
public sealed class WaveTable
{
    public const int TableLength = 40;
    public const int LoopBackBy = 20;
    public const int FactoryDifficulty = 3;   // Solid Blue Label factory default (RESEARCH.md §2.4)
    public const int NeutralDifficulty = 5;   // reproduces the raw table

    readonly WaveDefinition[] _waves;
    readonly ParamSpec[] _specs;
    readonly Dictionary<int, BozoRow> _bozo;
    readonly int _step;

    WaveTable(WaveDefinition[] waves, ParamSpec[] specs, Dictionary<int, BozoRow> bozo, int step)
    { _waves = waves; _specs = specs; _bozo = bozo; _step = step; }

    public static WaveTable LoadDefault()
    {
        using var s = typeof(WaveTable).Assembly.GetManifestResourceStream("AVATron.Core.Waves.waves.json")
            ?? throw new WaveDataException("Embedded wave table missing");
        return Parse(new StreamReader(s).ReadToEnd());
    }

    /// Maps any wave number to its table row: waves above 40 repeat 21..40 forever.
    public static int TableIndexFor(int wave)
    {
        if (wave < 1) throw new ArgumentOutOfRangeException(nameof(wave));
        while (wave > TableLength) wave -= LoopBackBy;
        return wave;
    }

    public WaveDefinition Raw(int wave) => _waves[TableIndexFor(wave) - 1];

    /// Parameters after the operator difficulty adjustment and the last-life "Bozo" assist.
    /// <paramref name="reserveLives"/> is the count of men in reserve (not including the one in play).
    public WaveParameters Effective(int wave, int difficulty, int reserveLives, int livesPerGame)
    {
        int row = TableIndexFor(wave);
        var raw = _waves[row - 1].Parameters;
        int d = Math.Clamp(difficulty, 0, 10);
        // Easier settings only protect early waves and weak players.
        if (d < NeutralDifficulty && (wave >= 14 || (wave >= 5 && reserveLives >= 3))) d = NeutralDifficulty;
        int[] v = ToArray(raw);
        for (int i = 0; i < v.Length; i++) v[i] = Adjust(v[i], d, _specs[i]);
        var p = FromArray(v);

        if (IsBozo(wave, reserveLives, livesPerGame) && _bozo.TryGetValue(wave, out var b))
            p = p with { SphereoidDropTimer = b.SphereoidDropTimer, EnforcerShotTimer = b.EnforcerShotTimer, GruntSpeed = b.GruntSpeed, GruntSpeedFloor = b.GruntSpeedFloor };
        return p;
    }

    /// The last-life assist ("Bozo" table) applies on waves 1-4 when the player is on their last man, or on
    /// waves 1-2 once they have lost a man.
    public bool IsBozo(int wave, int reserveLives, int livesPerGame) =>
        wave <= 4 && _bozo.ContainsKey(wave) && (reserveLives == 0 || (wave <= 2 && reserveLives < livesPerGame - 1));

    int Adjust(int value, int d, ParamSpec spec)
    {
        int m = Math.Abs(d - NeutralDifficulty);
        if (m == 0) return value;
        int adj = (int)Math.Round(value * m * _step / 256.0, MidpointRounding.AwayFromZero);
        bool easier = d < NeutralDifficulty;
        // Timers get longer when easier; rates get smaller when easier.
        int signed = spec.IsTimer == easier ? adj : -adj;
        return Math.Clamp(value + signed, spec.Min, spec.Max);
    }

    static int[] ToArray(WaveParameters p) =>
        [p.GruntSpeed, p.GruntSpeedFloor, p.SpawnerChildren2x, p.EnforcerShotTimer, p.SphereoidDropTimer, p.HulkSleep,
         p.BrainMissileTimer, p.BrainSleep, p.TankShotSteps, p.TankShellSpeed, p.QuarkDropTimer, p.QuarkSpeed];
    static WaveParameters FromArray(int[] v) => new(v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7], v[8], v[9], v[10], v[11]);

    static readonly string[] ParamNames =
        ["gruntSpeed", "gruntSpeedFloor", "spawnerChildren2x", "enforcerShotTimer", "sphereoidDropTimer", "hulkSleep",
         "brainMissileTimer", "brainSleep", "tankShotSteps", "tankShellSpeed", "quarkDropTimer", "quarkSpeed"];

    /// Parses and validates wave data. Throws <see cref="WaveDataException"/> describing the first problem.
    public static WaveTable Parse(string json)
    {
        Doc doc;
        try { doc = JsonSerializer.Deserialize<Doc>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip }) ?? throw new WaveDataException("empty wave data"); }
        catch (JsonException ex) { throw new WaveDataException($"wave data is not valid JSON: {ex.Message}"); }

        if (doc.SchemaVersion != 1) throw new WaveDataException($"unsupported wave schemaVersion {doc.SchemaVersion}");
        if (doc.DifficultyStepPer256 is < 0 or > 64) throw new WaveDataException("difficultyStepPer256 out of range");
        if (doc.Parameters is null || doc.Parameters.Count != ParamNames.Length) throw new WaveDataException("parameters must list all 12 parameters");
        var specs = new ParamSpec[ParamNames.Length];
        for (int i = 0; i < ParamNames.Length; i++)
        {
            var ps = doc.Parameters[i];
            if (!string.Equals(ps.Name, ParamNames[i], StringComparison.OrdinalIgnoreCase)) throw new WaveDataException($"parameter {i} should be '{ParamNames[i]}' but is '{ps.Name}'");
            if (ps.Kind is not ("timer" or "rate")) throw new WaveDataException($"parameter {ps.Name}: kind must be timer or rate");
            if (ps.Min < 0 || ps.Max > 255 || ps.Min > ps.Max) throw new WaveDataException($"parameter {ps.Name}: bad min/max");
            specs[i] = new ParamSpec(ps.Kind == "timer", ps.Min, ps.Max);
        }
        if (doc.Waves is null || doc.Waves.Count != TableLength) throw new WaveDataException($"exactly {TableLength} waves are required");
        var waves = new WaveDefinition[TableLength];
        for (int i = 0; i < TableLength; i++)
        {
            var w = doc.Waves[i];
            if (w.Wave != i + 1) throw new WaveDataException($"wave rows must be numbered 1..{TableLength} in order (row {i} is {w.Wave})");
            int[] counts = [w.Grunts, w.Electrodes, w.Mommies, w.Daddies, w.Mikeys, w.Hulks, w.Brains, w.Sphereoids, w.Quarks];
            if (counts.Any(c => c < 0 || c > 120)) throw new WaveDataException($"wave {w.Wave}: counts must be 0..120");
            if (w.Grunts + w.Sphereoids + w.Brains + w.Quarks == 0) throw new WaveDataException($"wave {w.Wave}: has nothing to clear");
            int[] p = [w.GruntSpeed, w.GruntSpeedFloor, w.SpawnerChildren2x, w.EnforcerShotTimer, w.SphereoidDropTimer, w.HulkSleep,
                       w.BrainMissileTimer, w.BrainSleep, w.TankShotSteps, w.TankShellSpeed, w.QuarkDropTimer, w.QuarkSpeed];
            for (int k = 0; k < p.Length; k++)
                if (p[k] < specs[k].Min || p[k] > specs[k].Max) throw new WaveDataException($"wave {w.Wave}: {ParamNames[k]}={p[k]} outside {specs[k].Min}..{specs[k].Max}");
            waves[i] = new WaveDefinition(w.Wave, new WaveCounts(counts[0], counts[1], counts[2], counts[3], counts[4], counts[5], counts[6], counts[7], counts[8]), FromArray(p));
        }
        var bozo = new Dictionary<int, BozoRow>();
        foreach (var b in doc.Bozo ?? [])
        {
            if (b.Wave is < 1 or > 4) throw new WaveDataException("bozo rows must be for waves 1..4");
            if (new[] { b.SphereoidDropTimer, b.EnforcerShotTimer, b.GruntSpeed, b.GruntSpeedFloor }.Any(x => x is < 1 or > 255)) throw new WaveDataException($"bozo wave {b.Wave}: values must be 1..255");
            bozo[b.Wave] = b;
        }
        return new WaveTable(waves, specs, bozo, doc.DifficultyStepPer256);
    }

    readonly record struct ParamSpec(bool IsTimer, int Min, int Max);

    sealed class Doc
    {
        public int SchemaVersion { get; set; }
        public int DifficultyStepPer256 { get; set; }
        public List<ParamDoc>? Parameters { get; set; }
        public List<BozoRow>? Bozo { get; set; }
        public List<WaveRow>? Waves { get; set; }
    }
    sealed class ParamDoc { public string Name { get; set; } = ""; public string Kind { get; set; } = ""; public int Min { get; set; } public int Max { get; set; } }
    sealed class BozoRow { public int Wave { get; set; } public int SphereoidDropTimer { get; set; } public int EnforcerShotTimer { get; set; } public int GruntSpeed { get; set; } public int GruntSpeedFloor { get; set; } }
    sealed class WaveRow
    {
        public int Wave { get; set; }
        public int Grunts { get; set; } public int Electrodes { get; set; } public int Mommies { get; set; } public int Daddies { get; set; } public int Mikeys { get; set; }
        public int Hulks { get; set; } public int Brains { get; set; } public int Sphereoids { get; set; } public int Quarks { get; set; }
        public int GruntSpeed { get; set; } public int GruntSpeedFloor { get; set; } public int SpawnerChildren2x { get; set; } public int EnforcerShotTimer { get; set; }
        public int SphereoidDropTimer { get; set; } public int HulkSleep { get; set; } public int BrainMissileTimer { get; set; } public int BrainSleep { get; set; }
        public int TankShotSteps { get; set; } public int TankShellSpeed { get; set; } public int QuarkDropTimer { get; set; } public int QuarkSpeed { get; set; }
    }
}
