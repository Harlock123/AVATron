using Robotron.Core.Entities;
using Robotron.Core.Input;
using Robotron.Core.Simulation;
using Robotron.Core.Waves;

namespace Robotron.Tests;

public static class TestSupport
{
    public static readonly WaveTable Table = WaveTable.LoadDefault();

    public static GameSession NewGame(ulong seed = 1, GameRules? rules = null) =>
        new(rules ?? GameRules.Classic, Table, new XorShiftRandom(seed));

    /// Run until the wave goes live.
    public static void SkipIntro(GameSession g)
    {
        for (int i = 0; i < 2000 && g.Phase == GamePhase.WaveIntro; i++) g.Tick(TickInput.None);
    }

    /// An empty, live world for unit-testing a single behaviour.
    public static World EmptyWorld(int wave = 1, ulong seed = 7, GameRules? rules = null)
    {
        var p = Table.Effective(wave, WaveTable.NeutralDifficulty, 2, 3);
        var w = new World(new XorShiftRandom(seed), p, wave, rules ?? GameRules.Classic);
        w.Activate();
        return w;
    }

    /// Crude autopilot: shoots at the nearest threat and steps away from it. Deterministic.
    public static TickInput Bot(GameSession g)
    {
        var w = g.World;
        Entity? near = null; long best = long.MaxValue;
        foreach (var e in w.Enemies)
        {
            if (e.Dead || e.Kind == EntityKind.Hulk) continue;
            long dx = e.CenterX - w.Player.CenterX, dy = e.CenterY - w.Player.CenterY, d = dx * dx + dy * dy;
            if (d < best) { best = d; near = e; }
        }
        if (near is null) return TickInput.None;
        int fx = near.CenterX - w.Player.CenterX, fy = near.CenterY - w.Player.CenterY;
        var fire = StickQuantizer.EightWay(fx, fy, 0);
        var move = best < 50 * 50 ? StickQuantizer.EightWay(-fx, -fy, 0) : StickInput.Neutral;
        return new TickInput(move, fire);
    }
}
