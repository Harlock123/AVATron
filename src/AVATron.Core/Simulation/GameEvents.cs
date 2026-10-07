namespace AVATron.Core.Simulation;

public enum GameEventKind
{
    WaveStarting, WaveActive, WaveCleared, PlayerDied, GameOver, ExtraLife,
    PlayerShot, ShotHitWall,
    EnemyKilled, HulkHit, ElectrodeDestroyed,
    HumanRescued, HumanKilled, ProgCreated, BrainCapturing,
    GruntsMarched, HulkStep, EnforcerAppears, SparkFired, TankAppears, ShellFired, ShellBounce, MissileFired,
    SphereoidDrop, QuarkDrop,
}

/// Something that happened this tick, for audio and presentation. Value carries points or a count.
public readonly record struct GameEvent(GameEventKind Kind, int X = 0, int Y = 0, int Value = 0, EntityKind Entity = EntityKind.None);
