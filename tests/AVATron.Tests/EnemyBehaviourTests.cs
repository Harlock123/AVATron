using AVATron.Core.Entities;
using AVATron.Core.Input;
using AVATron.Core.Simulation;
using Xunit;

namespace AVATron.Tests;

public class EnemyBehaviourTests
{
    static int Px(int x) => x * Arena.One;

    [Fact]
    public void Grunt_steps_4px_and_4_lines_toward_the_player()
    {
        var w = TestSupport.EmptyWorld();
        var g = new Grunt { X = Px(40), Y = Px(40) };
        w.Enemies.Add(g);
        Assert.True(g.Step(w));
        Assert.Equal((44, 44), (g.Px, g.Py));
        var g2 = new Grunt { X = Px(250), Y = Px(200) };
        g2.Step(w);
        Assert.Equal((246, 196), (g2.Px, g2.Py));
    }

    [Fact]
    public void Grunts_converge_on_a_stationary_player()
    {
        var w = TestSupport.EmptyWorld();
        var g = new Grunt { X = Px(30), Y = Px(200) };
        w.Enemies.Add(g);
        for (int t = 0; t < 3000 && !w.PlayerHit; t++) w.Step(TickInput.None);
        Assert.True(w.PlayerHit);
    }

    [Fact]
    public void Grunt_walking_into_electrode_destroys_both_and_scores_100()
    {
        var w = TestSupport.EmptyWorld();
        var e = new Electrode { X = Px(44), Y = Px(44) };
        w.Electrodes.Add(e);
        var g = new Grunt { X = Px(40), Y = Px(40) };
        w.Enemies.Add(g);
        g.Step(w);
        Assert.True(g.Dead);
        Assert.True(e.Dead);
        Assert.Equal(100, w.PointsThisTick);
    }

    [Fact]
    public void Each_grunt_kill_speeds_up_survivors_by_seven_eighths_down_to_floor()
    {
        var w = TestSupport.EmptyWorld();
        w.RobSpd = 16; w.RobMax = 9;
        w.OnGruntKilled(); Assert.Equal(14, w.RobSpd);
        w.OnGruntKilled(); Assert.Equal(12, w.RobSpd);
        w.OnGruntKilled(); Assert.Equal(10, w.RobSpd);
        w.OnGruntKilled(); Assert.Equal(9, w.RobSpd);
        w.OnGruntKilled(); Assert.Equal(9, w.RobSpd);
    }

    [Fact]
    public void Hulk_is_invulnerable_and_pushed_back_by_shots()
    {
        var w = TestSupport.EmptyWorld();
        var h = new Hulk { X = Px(Arena.PlayerStartX + 40), Y = Px(Arena.PlayerStartY - 4), Sleep = 99999 };
        w.Enemies.Add(h);
        int x0 = h.X;
        int hits = 0;
        for (int f = 0; f < 40; f++) { w.Step(new TickInput(StickInput.Neutral, StickQuantizer.Digital(1, 0))); hits += w.Events.Count(e => e.Kind == GameEventKind.HulkHit); }
        Assert.False(h.Dead);
        Assert.True(h.X > x0, "hulk should be pushed in the shot direction");
        Assert.True(hits >= 3);
        Assert.Equal(0, w.BlockingCount());   // hulks never block the end of a wave
    }

    [Fact]
    public void Hulk_moves_only_orthogonally_and_kills_family_it_touches()
    {
        var w = TestSupport.EmptyWorld();
        var human = new Human(EntityKind.Daddy) { X = Px(100), Y = Px(60), Sleep = 999999 };
        w.Family.Add(human);
        var h = new Hulk { X = Px(60), Y = Px(60), Target = human, Sleep = 1 };
        w.Enemies.Add(h);
        int lastX = h.X, lastY = h.Y;
        for (int t = 0; t < 2000 && !human.Dead; t++)
        {
            w.Step(TickInput.None);
            Assert.False(h.X != lastX && h.Y != lastY, "hulk moved diagonally");
            lastX = h.X; lastY = h.Y;
        }
        Assert.True(human.Dead);
        Assert.Contains(w.Effects, e => e.Kind == EffectKind.Skull);
    }

    [Fact]
    public void Brain_reprograms_a_human_into_a_prog()
    {
        var w = TestSupport.EmptyWorld(wave: 5);
        var human = new Human(EntityKind.Mikey) { X = Px(60), Y = Px(60), Sleep = 999999 };
        w.Family.Add(human);
        var b = new Brain { X = Px(40), Y = Px(50), Sleep = 1 };
        b.Init(w);
        w.Enemies.Add(b);
        for (int t = 0; t < 1500 && !w.Enemies.Any(e => e is Prog); t++) w.Step(TickInput.None);
        var prog = Assert.Single(w.Enemies.OfType<Prog>());
        Assert.Equal(EntityKind.Mikey, prog.From);
        Assert.Empty(w.Family);
        Assert.Equal(12, prog.Image.Width);
        Assert.Equal(16, prog.Image.Height);
    }

    [Fact]
    public void Killing_brain_mid_conversion_leaves_a_skull()
    {
        var w = TestSupport.EmptyWorld(wave: 5);
        var human = new Human(EntityKind.Mommy) { X = Px(60), Y = Px(60), Sleep = 999999 };
        w.Family.Add(human);
        var b = new Brain { X = Px(40), Y = Px(50), Sleep = 1 };
        b.Init(w);
        w.Enemies.Add(b);
        for (int t = 0; t < 1500 && !b.IsConverting; t++) w.Step(TickInput.None);
        Assert.True(b.IsConverting);
        b.Destroy(w, 1, 0);
        Assert.True(human.Dead);
        Assert.Contains(w.Effects, e => e.Kind == EffectKind.Skull);
        Assert.Equal(500, w.PointsThisTick);
    }

    [Fact]
    public void Sphereoid_drops_enforcers_then_leaves()
    {
        var w = TestSupport.EmptyWorld(wave: 2);
        var s = new Sphereoid { X = Px(150), Y = Px(100) };
        s.Init(w);
        w.Enemies.Add(s);
        int maxEnforcers = 0;
        for (int t = 0; t < 60 * 60 && !s.Dead; t++)
        {
            w.Step(TickInput.None);
            if (w.PlayerHit) break;
            maxEnforcers = Math.Max(maxEnforcers, w.Count(EntityKind.Enforcer));
        }
        Assert.InRange(maxEnforcers, 1, Sphereoid.MaxEnforcers);
    }

    [Fact]
    public void Enforcer_count_never_exceeds_eight()
    {
        var w = TestSupport.EmptyWorld(wave: 21);
        for (int i = 0; i < 6; i++) { var s = new Sphereoid { X = Px(30 + i * 40), Y = Px(40) }; s.Init(w); w.Enemies.Add(s); }
        for (int t = 0; t < 60 * 40; t++) { w.Step(TickInput.None); Assert.True(w.Count(EntityKind.Enforcer) <= 8); }
    }

    [Fact]
    public void Quark_drops_tanks_and_tanks_fire_bouncing_shells()
    {
        var w = TestSupport.EmptyWorld(wave: 7);
        var q = new Quark { X = Px(150), Y = Px(30) };
        q.Init(w);
        w.Enemies.Add(q);
        bool sawTank = false, sawShell = false, sawBounce = false;
        for (int t = 0; t < 60 * 60; t++)
        {
            w.Step(TickInput.None);
            sawTank |= w.Count(EntityKind.Tank) > 0;
            sawShell |= w.Count(EntityKind.Shell) > 0;
            sawBounce |= w.Events.Any(e => e.Kind == GameEventKind.ShellBounce);
            Assert.True(w.Count(EntityKind.Tank) <= Quark.MaxTanks);
            if (w.PlayerHit) break;
        }
        Assert.True(sawTank && sawShell);
        Assert.True(sawBounce || w.PlayerHit);
    }

    [Fact]
    public void Electrodes_kill_the_player_and_give_no_points_when_shot()
    {
        var w = TestSupport.EmptyWorld();
        var e = new Electrode { X = Px(Arena.PlayerStartX + 30), Y = Px(Arena.PlayerStartY + 1) };
        w.Electrodes.Add(e);
        for (int f = 0; f < 20 && !e.Dead; f++) w.Step(new TickInput(StickInput.Neutral, StickQuantizer.Digital(1, 0)));
        Assert.True(e.Dead);
        Assert.Equal(0, w.PointsThisTick);

        var w2 = TestSupport.EmptyWorld();
        w2.Electrodes.Add(new Electrode { X = Px(Arena.PlayerStartX + 12), Y = Px(Arena.PlayerStartY + 2) });
        for (int f = 0; f < 30 && !w2.PlayerHit; f++) w2.Step(new TickInput(StickQuantizer.Digital(1, 0), StickInput.Neutral));
        Assert.True(w2.PlayerHit);
    }

    [Theory]
    [InlineData(1)] [InlineData(4)] [InlineData(12)]
    public void Wave_placement_keeps_grunts_hulks_electrodes_out_of_the_safety_box(int wave)
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var g = TestSupport.NewGame(seed, GameRules.Classic with { StartWave = wave });
            var box = WaveBuilder.SafetyBox(wave);
            foreach (var e in g.World.Enemies.Where(e => e is Grunt or Hulk).Concat<Entity>(g.World.Electrodes))
            {
                bool inside = e.Px + e.Image.Width > box.X0 && e.Px < box.X1 && e.Py + e.Image.Height > box.Y0 && e.Py < box.Y1;
                Assert.False(inside, $"{e.Kind} at {e.Px},{e.Py} inside box on wave {wave}");
                Assert.InRange(e.Px, Arena.Left, Arena.Right - e.Image.Width);
                Assert.InRange(e.Py, Arena.Top, Arena.Bottom - e.Image.Height);
            }
        }
    }
}
