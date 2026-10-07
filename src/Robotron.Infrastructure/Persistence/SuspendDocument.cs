using Robotron.Core.Simulation;

namespace Robotron.Infrastructure.Persistence;

/// Modern-mode suspend save: the last wave checkpoint plus the run-length-encoded inputs since then.
/// Resuming restores the checkpoint and replays the inputs, reproducing the game exactly.
/// Single use: the file is deleted when resumed.
public sealed class SuspendDocument : IVersionedDocument
{
    public const int Version = 1;
    public int SchemaVersion { get; set; } = Version;
    /// Engine behaviour version; a replay recorded by a different engine would diverge, so it is rejected.
    public int EngineVersion { get; set; } = GameSession.EngineVersion;
    public DateTimeOffset Saved { get; set; }
    public long StateHash { get; set; }
    public SuspendState? State { get; set; }

    public static string? Validate(SuspendDocument d)
    {
        if (d.EngineVersion != GameSession.EngineVersion) return $"made by engine version {d.EngineVersion}, this build is {GameSession.EngineVersion}";
        if (d.State is null || d.State.Checkpoint is null || d.State.Rules is null || d.State.Inputs is null) return "state missing";
        if (d.State.Checkpoint.Wave is < 1 or > 255 || d.State.Checkpoint.Score < 0 || d.State.Checkpoint.Reserve < 0) return "checkpoint out of range";
        if (d.State.Inputs.Any(r => r.Count <= 0)) return "bad input run";
        return null;
    }

    public static VersionedJsonStore<SuspendDocument> Store() => new(Version, () => new SuspendDocument(), Validate);
}
