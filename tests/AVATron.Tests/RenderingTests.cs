using Avalonia;
using AVATron.Avalonia;
using AVATron.Avalonia.Rendering;
using AVATron.Core.Simulation;
using AVATron.Core.Sprites;
using Xunit;

namespace AVATron.Tests;

public class RenderingTests
{
    [Fact]
    public void Sprites_have_the_documented_arcade_sizes()
    {
        Assert.Equal((8, 12), (SpriteLibrary.PlayerDown[0].Width, SpriteLibrary.PlayerDown[0].Height));
        Assert.Equal((10, 13), (SpriteLibrary.Grunt[0].Width, SpriteLibrary.Grunt[0].Height));
        Assert.Equal((14, 16), (SpriteLibrary.Hulk[0].Width, SpriteLibrary.Hulk[0].Height));
        Assert.Equal((14, 16), (SpriteLibrary.Brain[0].Width, SpriteLibrary.Brain[0].Height));
        Assert.Equal((8, 14), (SpriteLibrary.Mommy[0].Width, SpriteLibrary.Mommy[0].Height));
        Assert.Equal((10, 13), (SpriteLibrary.Daddy[0].Width, SpriteLibrary.Daddy[0].Height));
        Assert.Equal((6, 11), (SpriteLibrary.Mikey[0].Width, SpriteLibrary.Mikey[0].Height));
        Assert.Equal((16, 15), (SpriteLibrary.Sphereoid[0].Width, SpriteLibrary.Sphereoid[0].Height));
        Assert.Equal((16, 15), (SpriteLibrary.Quark[0].Width, SpriteLibrary.Quark[0].Height));
        Assert.Equal((10, 11), (SpriteLibrary.Enforcer.Width, SpriteLibrary.Enforcer.Height));
        Assert.Equal((14, 16), (SpriteLibrary.Tank[0].Width, SpriteLibrary.Tank[0].Height));
        Assert.All(SpriteLibrary.Electrode, e => Assert.Equal((10, 9), (e.Width, e.Height)));
        Assert.Equal((6, 8), (SpriteLibrary.LifeIcon.Width, SpriteLibrary.LifeIcon.Height));
    }

    [Fact]
    public void Mask_collision_respects_transparent_pixels()
    {
        var a = Sprite.Parse("a", "W.|..");
        var b = Sprite.Parse("b", ".W|..");
        Assert.False(Sprite.Overlaps(a, 0, 0, b, 0, 0));
        Assert.True(Sprite.Overlaps(a, 1, 0, b, 0, 0));
        Assert.False(Sprite.Overlaps(a, 0, 0, b, 5, 5));
    }

    [Fact]
    public void Palette_decodes_bbgggrrr_with_mame_levels()
    {
        Assert.Equal(0xFFFF0000u, WilliamsPalette.Decode(0x07));   // red
        Assert.Equal(0xFF00FF00u, WilliamsPalette.Decode(0x38));   // green
        Assert.Equal(0xFF0000FFu, WilliamsPalette.Decode(0xC0));   // blue
        Assert.Equal(0xFF8989A0u, WilliamsPalette.Decode(0xA4));   // grey: R137 G137 B160
    }

    [Fact]
    public void Renderer_draws_every_wave_and_phase_without_error()
    {
        var r = new GameRenderer();
        var fb = new FrameBuffer(GameRenderer.Width, GameRenderer.Height);
        var snap = new FrameSnapshot();
        foreach (bool classic in new[] { true, false })
            for (int wave = 1; wave <= 41; wave += 4)
            {
                var g = TestSupport.NewGame((ulong)wave, GameRules.Classic with { StartWave = wave, LivesPerGame = 1 });
                for (int t = 0; t < 1200 && g.Phase != GamePhase.Finished; t++)
                {
                    g.Tick(TestSupport.Bot(g));
                    if (t % 7 != 0) continue;
                    FrameSnapshot.Fill(g, snap);
                    r.Render(snap, fb, new GameRenderOptions { Flicker = classic, ModernHud = !classic, WaveProgress = !classic, ReducedMotion = wave % 8 == 1 });
                }
            }
    }

    [Fact]
    public void Border_is_drawn_in_the_wave_colour()
    {
        var g = TestSupport.NewGame(1);
        var snap = new FrameSnapshot(); FrameSnapshot.Fill(g, snap);
        var fb = new FrameBuffer(GameRenderer.Width, GameRenderer.Height);
        new GameRenderer().Render(snap, fb, new GameRenderOptions());
        uint c = fb.Pixels[(Arena.BorderTop - Arena.ViewY) * fb.Width + 100];
        Assert.Equal(WilliamsPalette.WallColour(1, 0, false), c);
    }

    [Theory]
    [InlineData(1024, 768, 1.0, true, true)]
    [InlineData(1920, 1080, 1.0, false, true)]
    [InlineData(800, 600, 2.0, false, false)]
    [InlineData(333, 1000, 1.25, true, true)]
    public void Destination_is_centred_and_keeps_aspect(double w, double h, double scaling, bool square, bool integer)
    {
        var r = GameView.ComputeDestination(new Size(w, h), scaling, square, integer);
        Assert.True(r.X >= -0.5 && r.Y >= -0.5 && r.Right <= w + 0.5 && r.Bottom <= h + 0.5);
        double aspect = r.Width / r.Height;
        double expected = square ? 292.0 / 240 : 4.0 / 3;
        Assert.Equal(expected, aspect, 2);
        if (integer && !square) Assert.Equal(0, (r.Height * scaling) % 240, 3);
    }

    [Fact]
    public void Flicker_only_happens_over_budget()
    {
        var g = TestSupport.NewGame(1, GameRules.Classic with { StartWave = 19 });   // 70 grunts
        TestSupport.SkipIntro(g);
        var snap = new FrameSnapshot(); FrameSnapshot.Fill(g, snap);
        Assert.True(snap.Items.Count > GameRenderer.FlickerBudget);
        var a = new FrameBuffer(GameRenderer.Width, GameRenderer.Height);
        var b = new FrameBuffer(GameRenderer.Width, GameRenderer.Height);
        var r = new GameRenderer();
        r.Render(snap, a, new GameRenderOptions { Flicker = false });
        r.Render(snap, b, new GameRenderOptions { Flicker = true });
        Assert.True(a.Pixels.Count(p => p != 0xFF000000) > b.Pixels.Count(p => p != 0xFF000000));
    }
}

public class PhaseRenderingTests
{
    [Fact]
    public void Wave_clear_marquee_and_game_over_render()
    {
        var g = TestSupport.NewGame(2, GameRules.Classic with { LivesPerGame = 1 });
        TestSupport.SkipIntro(g);
        foreach (var e in g.World.Enemies) e.Dead = true;
        for (int t = 0; t < 20 && g.Phase != GamePhase.WaveCleared; t++) g.Tick(AVATron.Core.Input.TickInput.None);
        Assert.Equal(GamePhase.WaveCleared, g.Phase);
        for (int t = 0; t < 30; t++) g.Tick(AVATron.Core.Input.TickInput.None);
        var snap = new FrameSnapshot(); FrameSnapshot.Fill(g, snap);
        var fb = new FrameBuffer(GameRenderer.Width, GameRenderer.Height);
        new GameRenderer().Render(snap, fb, new GameRenderOptions());
        Assert.Contains(fb.Pixels, p => p != 0xFF000000);   // rings + "WAVE 1 COMPLETED"

        for (int t = 0; t < 2000 && g.Phase != GamePhase.Playing; t++) g.Tick(AVATron.Core.Input.TickInput.None);
        g.World.Enemies.Add(new AVATron.Core.Entities.Grunt { X = g.World.Player.X, Y = g.World.Player.Y, StepTimer = 999 });
        for (int t = 0; t < GameSession.DeathFrames + 5; t++) g.Tick(AVATron.Core.Input.TickInput.None);
        Assert.Equal(GamePhase.GameOver, g.Phase);
        FrameSnapshot.Fill(g, snap);
        new GameRenderer().Render(snap, fb, new GameRenderOptions());
        Assert.Contains(fb.Pixels, p => p == WilliamsPalette.Rgb(7, 0, 0));   // red GAME OVER text
    }
}
