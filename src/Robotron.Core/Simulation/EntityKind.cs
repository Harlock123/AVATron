namespace Robotron.Core.Simulation;

public enum EntityKind : byte
{
    None, Player, Shot,
    Grunt, Hulk, Brain, Prog, CruiseMissile, Tank,       // "robot list": fatal on touch
    Electrode,                                         // fatal on touch, static
    Sphereoid, Enforcer, Spark, Quark, Shell,          // "motion objects": fatal on touch
    Mommy, Daddy, Mikey,                               // family: rescued on touch
}

public static class EntityScores
{
    /// Points for destroying each entity with a shot (RESEARCH.md §Scoring). Electrode = 0; Hulk cannot be destroyed.
    public static int For(EntityKind k) => k switch
    {
        EntityKind.Grunt => 100, EntityKind.Brain => 500, EntityKind.Prog => 100,
        EntityKind.Sphereoid => 1000, EntityKind.Enforcer => 150, EntityKind.Quark => 1000, EntityKind.Tank => 200,
        EntityKind.Spark or EntityKind.Shell or EntityKind.CruiseMissile => 25,
        _ => 0,
    };

    public const int RescueStep = 1000, RescueCapMultiplier = 5;
}
