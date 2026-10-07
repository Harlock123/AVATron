using Robotron.Core.Entities;
using Robotron.Core.Input;
using Robotron.Core.Simulation;
using Robotron.Core.Waves;
using Xunit;

namespace Robotron.Tests;

public class ScoringAndSessionTests
{
    [Theory]
    [InlineData(EntityKind.Grunt, 100)] [InlineData(EntityKind.Brain, 500)] [InlineData(EntityKind.Prog, 100)]
    [InlineData(EntityKind.Sphereoid, 1000)] [InlineData(EntityKind.Enforcer, 150)] [InlineData(EntityKind.Quark, 1000)]
    [InlineData(EntityKind.Tank, 200)] [InlineData(EntityKind.Spark, 25)] [InlineData(EntityKind.Shell, 25)]
    [InlineData(EntityKind.CruiseMissile, 25)] [InlineData(EntityKind.Electrode, 0)] [InlineData(EntityKind.Hulk, 0)]
    public void Point_values_match_the_arcade_program(EntityKind k, int points) => Assert.Equal(points, EntityScores.For(k));

    [Fact]
    public void Rescue_bonus_climbs_1000_to_5000_then_stays()
    {
        var w = TestSupport.EmptyWorld();
        var values = new List<int>();
        for (int i = 0; i < 7; i++)
        {
            w.Family.Add(new Human(EntityKind.Mommy) { X = w.Player.X, Y = w.Player.Y, Sleep = 99999 });
            w.Step(TickInput.None);
            values.Add(w.PointsThisTick);
        }
        Assert.Equal([1000, 2000, 3000, 4000, 5000, 5000, 5000], values);
    }

    [Fact]
    public void Rescue_multiplier_resets_after_death()
    {
        var g = TestSupport.NewGame(3);
        TestSupport.SkipIntro(g);
        g.World.Rescues = 4;
        // Walk into an electrode-free death: place a grunt on the player.
        g.World.Enemies.Add(new Grunt { X = g.World.Player.X, Y = g.World.Player.Y, StepTimer = 999 });
        g.Tick(TickInput.None);
        Assert.Equal(GamePhase.PlayerDying, g.Phase);
        for (int i = 0; i < GameSession.DeathFrames + 1; i++) g.Tick(TickInput.None);
        Assert.Equal(0, g.World.Rescues);
    }

    [Fact]
    public void Extra_man_every_25000()
    {
        var g = TestSupport.NewGame(5);
        TestSupport.SkipIntro(g);
        int start = g.Reserve;
        var gained = new List<long>();
        for (int i = 0; i < 60 && g.Phase == GamePhase.Playing; i++)
        {
            // 1000 points per tick by rescuing a human placed on the player (multiplier capped at 5000).
            g.World.Family.Add(new Human(EntityKind.Daddy) { X = g.World.Player.X, Y = g.World.Player.Y, Sleep = 99999 });
            long before = g.Score;
            g.Tick(TickInput.None);
            if (g.Events.Any(e => e.Kind == GameEventKind.ExtraLife)) gained.Add(g.Score);
        }
        Assert.True(gained.Count >= 3);
        Assert.True(gained[0] >= 25000 && gained[0] < 30000);
        Assert.True(gained[1] >= 50000 && gained[1] < 55000);
        Assert.Equal(start + gained.Count, g.Reserve);
    }

    [Fact]
    public void Game_starts_with_two_in_reserve_and_ends_after_three_deaths()
    {
        var g = TestSupport.NewGame(11);
        Assert.Equal(2, g.Reserve);
        int deaths = 0;
        for (int t = 0; t < 60 * 60 * 10 && g.Phase != GamePhase.Finished; t++)
        {
            if (g.Phase == GamePhase.Playing && g.World.Enemies.Count(e => e is Grunt) > 0 && !g.World.PlayerHit)
            {
                var gr = g.World.Enemies.OfType<Grunt>().First();
                gr.X = g.World.Player.X; gr.Y = g.World.Player.Y;
            }
            g.Tick(TickInput.None);
            if (g.Events.Any(e => e.Kind == GameEventKind.PlayerDied)) deaths++;
        }
        Assert.Equal(GamePhase.Finished, g.Phase);
        Assert.Equal(3, deaths);
    }

    [Fact]
    public void Wave_clears_when_only_hulks_electrodes_family_remain()
    {
        var g = TestSupport.NewGame(8, GameRules.Classic with { StartWave = 2 });
        TestSupport.SkipIntro(g);
        foreach (var e in g.World.Enemies.Where(e => e is not Hulk)) e.Dead = true;
        for (int t = 0; t < 20; t++) g.Tick(TickInput.None);
        Assert.Equal(GamePhase.WaveCleared, g.Phase);
        for (int t = 0; t < GameSession.WaveClearFrames + 1; t++) g.Tick(TickInput.None);
        Assert.Equal(3, g.Wave);
    }

    [Fact]
    public void Death_restarts_the_same_wave_with_survivors_only()
    {
        var g = TestSupport.NewGame(21);
        TestSupport.SkipIntro(g);
        var grunts = g.World.Enemies.OfType<Grunt>().ToList();
        for (int i = 0; i < 5; i++) grunts[i].Dead = true;
        grunts[5].X = g.World.Player.X; grunts[5].Y = g.World.Player.Y;
        g.Tick(TickInput.None);
        Assert.Equal(GamePhase.PlayerDying, g.Phase);
        for (int i = 0; i < GameSession.DeathFrames; i++) g.Tick(TickInput.None);
        Assert.Equal(GamePhase.WaveIntro, g.Phase);
        Assert.Equal(1, g.Wave);
        Assert.Equal(15 - 5, g.World.Enemies.Count(e => e is Grunt));
        Assert.Equal(1, g.Reserve);
    }

    [Fact]
    public void Enforcers_fold_back_into_sphereoids_after_death()
    {
        var g = TestSupport.NewGame(4, GameRules.Classic with { StartWave = 6 });
        TestSupport.SkipIntro(g);
        foreach (var s in g.World.Enemies.OfType<Sphereoid>()) s.Dead = true;
        for (int i = 0; i < 5; i++) g.World.Enemies.Add(new Enforcer { X = 30 * Arena.One, Y = 30 * Arena.One, Age = 0, Sleep = 99999 });
        g.World.Enemies.Add(new Grunt { X = g.World.Player.X, Y = g.World.Player.Y, StepTimer = 999 });
        g.Tick(TickInput.None);
        for (int i = 0; i < GameSession.DeathFrames; i++) g.Tick(TickInput.None);
        // 5 enforcers, no sphereoids -> max(1, 5/4) = 1 sphereoid.
        Assert.Equal(1, g.World.Enemies.Count(e => e is Sphereoid));
        Assert.Equal(0, g.World.Enemies.Count(e => e is Enforcer));
    }

    [Fact]
    public void Wave_table_loops_21_to_40_after_wave_40()
    {
        Assert.Equal(40, WaveTable.TableIndexFor(40));
        Assert.Equal(21, WaveTable.TableIndexFor(41));
        Assert.Equal(40, WaveTable.TableIndexFor(60));
        Assert.Equal(21, WaveTable.TableIndexFor(61));
        Assert.Equal(TestSupport.Table.Raw(25).Counts, TestSupport.Table.Raw(45).Counts);
    }

    [Fact]
    public void Golden_wave_counts_from_the_arcade_table()
    {
        var t = TestSupport.Table;
        Assert.Equal(new WaveCounts(15, 5, 1, 1, 0, 0, 0, 0, 0), t.Raw(1).Counts);
        Assert.Equal(new WaveCounts(20, 20, 15, 0, 1, 0, 15, 1, 0), t.Raw(5).Counts);
        Assert.Equal(new WaveCounts(0, 0, 4, 4, 4, 12, 0, 0, 10), t.Raw(7).Counts);
        Assert.Equal(60, t.Raw(9).Counts.Grunts);
        Assert.Equal(20, t.Raw(14).Counts.Hulks);
        Assert.Equal(new WaveCounts(30, 15, 10, 10, 10, 2, 25, 1, 1), t.Raw(40).Counts);
        int[] brainWaves = Enumerable.Range(1, 40).Where(w => t.Raw(w).Counts.Brains > 0).ToArray();
        Assert.Equal([5, 10, 15, 20, 25, 30, 35, 40], brainWaves);
    }

    [Fact]
    public void Difficulty_5_is_the_raw_table_and_3_eases_early_timers()
    {
        var t = TestSupport.Table;
        var raw = t.Raw(1).Parameters;
        Assert.Equal(raw, t.Effective(1, 5, 2, 3));
        var easy = t.Effective(1, 3, 2, 3);
        // Timers get longer (easier) but are clamped to their ranges; enforcer shot timer 30 -> 30 + round(30*2*14/256)=33.
        Assert.Equal(33, easy.EnforcerShotTimer);
        Assert.True(easy.GruntSpeed >= raw.GruntSpeed);
        // From wave 14 an easy setting is forced back to 5.
        Assert.Equal(t.Raw(14).Parameters, t.Effective(14, 0, 2, 3));
        var hard = t.Effective(1, 10, 2, 3);
        Assert.True(hard.EnforcerShotTimer < raw.EnforcerShotTimer);
    }

    [Fact]
    public void Bozo_assist_applies_on_last_life_in_early_waves()
    {
        var t = TestSupport.Table;
        Assert.True(t.IsBozo(3, 0, 3));
        Assert.False(t.IsBozo(3, 1, 3));
        Assert.True(t.IsBozo(1, 1, 3));
        Assert.False(t.IsBozo(5, 0, 3));
        Assert.Equal(30, t.Effective(1, 5, 0, 3).GruntSpeed);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{\"schemaVersion\":2}")]
    public void Malformed_wave_data_is_rejected(string json) => Assert.Throws<WaveDataException>(() => WaveTable.Parse(json));

    [Fact]
    public void Out_of_range_wave_parameter_is_rejected()
    {
        using var s = typeof(WaveTable).Assembly.GetManifestResourceStream("Robotron.Core.Waves.waves.json")!;
        var json = new StreamReader(s).ReadToEnd().Replace("\"hulkSleep\": 8", "\"hulkSleep\": 99");
        var ex = Assert.Throws<WaveDataException>(() => WaveTable.Parse(json));
        Assert.Contains("hulkSleep", ex.Message);
    }

    [Fact]
    public void Suspend_and_resume_reproduce_the_exact_state()
    {
        var g = TestSupport.NewGame(77, GameRules.Modern);
        for (int t = 0; t < 60 * 50; t++) g.Tick(TestSupport.Bot(g));
        var state = g.CreateSuspendState();
        var resumed = GameSession.Resume(state, TestSupport.Table, new XorShiftRandom(12345));
        Assert.Equal(g.StateHash(), resumed.StateHash());
        Assert.Equal(g.Score, resumed.Score);
        // And they stay in lock-step afterwards.
        for (int t = 0; t < 600; t++) { var i = TestSupport.Bot(g); g.Tick(i); resumed.Tick(i); }
        Assert.Equal(g.StateHash(), resumed.StateHash());
    }
}
