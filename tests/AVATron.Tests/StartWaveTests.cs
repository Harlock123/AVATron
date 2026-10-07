using Avalonia.Input;
using AVATron.Avalonia;
using AVATron.Avalonia.Rendering;
using AVATron.Avalonia.Shell;
using AVATron.Core.Input;
using AVATron.Core.Simulation;
using AVATron.Infrastructure.Audio;
using AVATron.Infrastructure.Input;
using AVATron.Infrastructure.Persistence;
using Xunit;

namespace AVATron.Tests;

public sealed class StartWaveTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "avatron-wave-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { try { Directory.Delete(_dir, true); } catch (IOException) { } }

    AppController NewApp() => new(new DataPaths(_dir), new AudioMixer(), new NoGamepadSource("t"), "t", () => 9);
    static void Tap(AppController app, Key k) { app.KeyDown(k); app.Advance(1 / 60.0); app.KeyUp(k); app.Advance(1 / 60.0); }

    [Fact]
    public void Title_menu_picks_the_wave_and_the_game_starts_there()
    {
        var app = NewApp();
        Tap(app, Key.Down); Tap(app, Key.Down);                 // START WAVE row
        for (int i = 0; i < 6; i++) Tap(app, Key.Right);        // 1 -> 7
        Assert.Equal(7, app.Settings.StartWave);
        Tap(app, Key.Enter);                                     // Enter on the row starts the game
        Assert.Equal(Screen.Playing, app.Screen);
        Assert.Equal(7, app.Session!.Wave);
        Assert.True(app.IsPractice);
        Assert.Equal(TestSupport.Table.Raw(7).Counts, app.Session.WaveStartCounts);   // the Quark/Tank wave
        app.Render(new FrameBuffer(GameRenderer.Width, GameRenderer.Height));
    }

    [Fact]
    public void Start_wave_wraps_and_is_remembered()
    {
        var app = NewApp();
        app.AdjustStartWave(-1);
        Assert.Equal(SettingsDocument.MaxStartWave, app.Settings.StartWave);
        app.AdjustStartWave(+1);
        Assert.Equal(1, app.Settings.StartWave);
        app.AdjustStartWave(+19);
        Assert.Equal(20, NewApp().Settings.StartWave);
    }

    [Fact]
    public void Wave_one_is_a_normal_game()
    {
        var app = NewApp();
        Tap(app, Key.Enter);
        Assert.Equal(1, app.Session!.Wave);
        Assert.False(app.IsPractice);
    }

    [Fact]
    public void Practice_games_skip_the_high_score_table()
    {
        var app = NewApp();
        app.AdjustStartWave(4);                                  // wave 5
        Tap(app, Key.Enter);
        var g = app.Session!;
        for (int i = 0; i < 2000 && g.Phase == GamePhase.WaveIntro; i++) app.StepSimulation(TickInput.None);
        g.World.Family.Add(new AVATron.Core.Entities.Human(EntityKind.Mommy) { X = g.World.Player.X, Y = g.World.Player.Y, Sleep = 9999 });
        app.StepSimulation(TickInput.None);
        Assert.True(g.Score > 0);
        typeof(GameSession).GetProperty("Reserve")!.SetValue(g, 0);
        g.World.Enemies.Add(new AVATron.Core.Entities.Grunt { X = g.World.Player.X, Y = g.World.Player.Y, StepTimer = 999 });
        for (int i = 0; i < 400 && app.Screen == Screen.Playing; i++) app.StepSimulation(TickInput.None);
        Assert.Equal(Screen.HighScores, app.Screen);
        Assert.Empty(app.HighScores.Entries);
    }

    [Theory]
    [InlineData(new[] { "--start-wave", "12" }, 12)]
    [InlineData(new[] { "--start-wave", "500" }, 99)]
    [InlineData(new[] { "--start-wave", "x" }, null)]
    [InlineData(new string[0], null)]
    public void Command_line_start_wave_is_parsed_and_clamped(string[] args, int? expected) =>
        Assert.Equal(expected, StartupOptions.Parse(args).StartWave);

    [Theory]
    [InlineData(0)] [InlineData(100)]
    public void Out_of_range_saved_start_wave_is_rejected(int wave) =>
        Assert.NotNull(SettingsDocument.Validate(new SettingsDocument { StartWave = wave }));
}
