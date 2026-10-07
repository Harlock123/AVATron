using Robotron.Core.Entities;
using Robotron.Core.Input;
using Robotron.Core.Simulation;
using Xunit;

namespace Robotron.Tests;

public class PlayerAndShotTests
{
    static TickInput Move(int dx, int dy) => new(StickQuantizer.Digital(dx, dy), StickInput.Neutral);
    static TickInput Fire(int dx, int dy) => new(StickInput.Neutral, StickQuantizer.Digital(dx, dy));

    [Theory]
    [InlineData(-1, 0)] [InlineData(1, 0)] [InlineData(0, -1)] [InlineData(0, 1)]
    [InlineData(-1, -1)] [InlineData(1, -1)] [InlineData(-1, 1)] [InlineData(1, 1)]
    public void Player_stops_at_every_edge_and_corner(int dx, int dy)
    {
        var w = TestSupport.EmptyWorld();
        for (int i = 0; i < 400; i++) w.Step(Move(dx, dy));
        int expX = dx < 0 ? Arena.Left : dx > 0 ? Arena.Right - 8 : Arena.PlayerStartX;
        int expY = dy < 0 ? Arena.Top : dy > 0 ? Arena.Bottom - 12 : Arena.PlayerStartY;
        Assert.Equal((expX, expY), (w.Player.Px, w.Player.Py));
    }

    [Fact]
    public void Digital_movement_is_one_pixel_and_one_line_per_frame()
    {
        var w = TestSupport.EmptyWorld();
        w.Step(Move(1, 1));
        Assert.Equal((Arena.PlayerStartX + 1, Arena.PlayerStartY + 1), (w.Player.Px, w.Player.Py));
    }

    [Fact]
    public void Analog_half_deflection_moves_half_speed()
    {
        var w = TestSupport.EmptyWorld(rules: GameRules.Modern);
        for (int i = 0; i < 10; i++) w.Step(new TickInput(new StickInput(64, 0), StickInput.Neutral));
        Assert.InRange(w.Player.Px - Arena.PlayerStartX, 4, 6);
    }

    [Fact]
    public void Fire_cadence_is_2_then_every_8_frames_and_capped_at_4_shots()
    {
        var w = TestSupport.EmptyWorld();
        var shotFrames = new List<int>();
        for (int f = 1; f <= 40; f++)
        {
            int before = w.Shots.Count;
            w.Step(Fire(0, -1));
            if (w.Events.Any(e => e.Kind == GameEventKind.PlayerShot)) shotFrames.Add(f);
            Assert.True(w.Shots.Count <= Player.MaxShots);
        }
        // Frame 1 registers the new direction; shots at hold counts 2, 8, 16, 24... => frames 3, 9, 17, 25, 33.
        Assert.Equal([3, 9, 17, 25, 33], shotFrames);
    }

    [Fact]
    public void Shot_limit_holds_fire_until_a_slot_frees()
    {
        var w = TestSupport.EmptyWorld();
        // Fire up from near the bottom so shots live long: move to the bottom first.
        for (int i = 0; i < 200; i++) w.Step(Move(0, 1));
        int max = 0;
        for (int f = 0; f < 120; f++) { w.Step(Fire(0, -1)); max = Math.Max(max, w.Shots.Count); }
        Assert.Equal(Player.MaxShots, max);
    }

    [Fact]
    public void Neutral_or_opposing_fire_does_not_shoot()
    {
        var w = TestSupport.EmptyWorld();
        for (int f = 0; f < 30; f++) w.Step(new TickInput(StickInput.Neutral, StickQuantizer.Digital(left: true, right: true, up: false, down: false)));
        Assert.Empty(w.Shots);
    }

    [Fact]
    public void Shots_move_6_per_frame_and_die_at_the_wall()
    {
        var w = TestSupport.EmptyWorld();
        for (int f = 0; f < 3; f++) w.Step(Fire(1, 0));
        var s = Assert.Single(w.Shots);
        int x0 = s.Px;
        w.Step(TickInput.None);
        Assert.Equal(x0 + 6, s.Px);
        bool glow = false;
        for (int f = 0; f < 40; f++) { w.Step(TickInput.None); glow |= w.Effects.Any(e => e.Kind == EffectKind.WallGlow); }
        Assert.Empty(w.Shots);
        Assert.True(glow);
    }

    [Fact]
    public void Shot_kills_one_target_only()
    {
        var w = TestSupport.EmptyWorld();
        // Two grunts in a row to the right of the player at shot height.
        var a = new Grunt { X = (Arena.PlayerStartX + 30) * Arena.One, Y = (Arena.PlayerStartY - 2) * Arena.One, StepTimer = 9999 };
        var b = new Grunt { X = (Arena.PlayerStartX + 44) * Arena.One, Y = (Arena.PlayerStartY - 2) * Arena.One, StepTimer = 9999 };
        w.Enemies.Add(a); w.Enemies.Add(b);
        for (int f = 0; f < 3; f++) w.Step(Fire(1, 0));
        for (int f = 0; f < 15; f++) w.Step(TickInput.None);
        Assert.True(a.Dead);
        Assert.False(b.Dead);
    }

    [Fact]
    public void Shots_pass_through_family()
    {
        var w = TestSupport.EmptyWorld();
        var h = new Human(EntityKind.Mommy) { X = (Arena.PlayerStartX + 30) * Arena.One, Y = (Arena.PlayerStartY - 4) * Arena.One, Sleep = 99999 };
        w.Family.Add(h);
        for (int f = 0; f < 30; f++) w.Step(Fire(1, 0));
        Assert.False(h.Dead);
    }
}
