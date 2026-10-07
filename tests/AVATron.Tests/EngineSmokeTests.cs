using AVATron.Core.Input;
using AVATron.Core.Simulation;
using Xunit;

namespace AVATron.Tests;

public class EngineSmokeTests
{
    [Fact]
    public void Bot_plays_many_ticks_without_error_and_progresses()
    {
        var g = TestSupport.NewGame(seed: 42, GameRules.Classic with { LivesPerGame = 20 });
        var snap = new FrameSnapshot();
        int maxWave = 1;
        for (int t = 0; t < 60 * 60 * 6 && g.Phase != GamePhase.Finished; t++)
        {
            g.Tick(TestSupport.Bot(g));
            FrameSnapshot.Fill(g, snap);
            maxWave = Math.Max(maxWave, g.Wave);
        }
        Assert.True(g.Score > 0);
        Assert.True(maxWave >= 2, $"bot only reached wave {maxWave}");
    }

    [Fact]
    public void Every_table_wave_builds_and_runs_a_few_seconds()
    {
        for (int wave = 1; wave <= 45; wave++)
        {
            var g = TestSupport.NewGame((ulong)wave, GameRules.Classic with { StartWave = wave });
            for (int t = 0; t < 600; t++) g.Tick(TestSupport.Bot(g));
        }
    }

    [Fact]
    public void Same_seed_and_inputs_give_identical_games()
    {
        static (long, int, int, long) Run()
        {
            var g = TestSupport.NewGame(seed: 99);
            for (int t = 0; t < 60 * 90; t++) g.Tick(TestSupport.Bot(g));
            return (g.Score, g.Wave, g.Reserve, g.World.Enemies.Sum(e => (long)e.X * 31 + e.Y));
        }
        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void Different_seeds_give_different_layouts()
    {
        var a = TestSupport.NewGame(1).World.Enemies.Select(e => e.X).ToArray();
        var b = TestSupport.NewGame(2).World.Enemies.Select(e => e.X).ToArray();
        Assert.NotEqual(a, b);
    }
}
