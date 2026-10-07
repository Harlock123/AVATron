using Avalonia.Input;
using Robotron.Avalonia.Rendering;
using Robotron.Avalonia.Shell;
using Robotron.Core.Input;
using Robotron.Core.Simulation;
using Robotron.Infrastructure.Audio;
using Robotron.Infrastructure.Input;
using Robotron.Infrastructure.Persistence;
using Xunit;

namespace Robotron.Tests;

public sealed class AppFlowTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "robotron-app-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { try { Directory.Delete(_dir, true); } catch (IOException) { } }

    AppController NewApp(ulong seed = 5) => new(new DataPaths(_dir), new AudioMixer(), new NoGamepadSource("test"), "test audio", () => seed);

    static void Tap(AppController app, Key k) { app.KeyDown(k); app.Advance(1 / 60.0); app.KeyUp(k); app.Advance(1 / 60.0); }

    [Fact]
    public void Enter_starts_a_game_and_escape_pauses()
    {
        var app = NewApp();
        Assert.Equal(Screen.Title, app.Screen);
        Tap(app, Key.Enter);
        Assert.Equal(Screen.Playing, app.Screen);
        Tap(app, Key.Escape);
        Assert.Equal(Screen.Paused, app.Screen);
        Tap(app, Key.Enter);   // "RESUME"
        Assert.Equal(Screen.Playing, app.Screen);
    }

    [Fact]
    public void Held_key_autorepeat_does_not_double_inputs()
    {
        var app = NewApp();
        Tap(app, Key.Enter);
        app.KeyDown(Key.D); app.KeyDown(Key.D); app.KeyDown(Key.D);   // OS auto-repeat
        var input = app.Input.BuildTickInput();
        Assert.Equal(127, input.Move.X);
        app.KeyUp(Key.D);
        Assert.True(app.Input.BuildTickInput().Move.IsNeutral);
    }

    [Fact]
    public void Focus_loss_releases_held_keys()
    {
        var app = NewApp();
        app.KeyDown(Key.Up);
        app.FocusLost();
        Assert.True(app.Input.BuildTickInput().Fire.IsNeutral);
    }

    [Fact]
    public void Simulation_runs_at_arcade_rate_from_real_time()
    {
        var app = NewApp();
        Tap(app, Key.Enter);
        long before = app.Session!.TickCount;
        for (int i = 0; i < 60; i++) app.Advance(1 / 60.0);
        long ticks = app.Session.TickCount - before;
        Assert.InRange(ticks, 59, 61);   // 60.096 Hz
    }

    [Fact]
    public void Game_over_with_qualifying_score_goes_to_initials_and_persists()
    {
        var app = NewApp();
        Tap(app, Key.Enter);
        var g = app.Session!;
        // Force a quick game over: drop reserve, score something, kill the player.
        for (int i = 0; i < 2000 && g.Phase == GamePhase.WaveIntro; i++) app.StepSimulation(TickInput.None);
        g.World.Family.Add(new Robotron.Core.Entities.Human(EntityKind.Mommy) { X = g.World.Player.X, Y = g.World.Player.Y, Sleep = 9999 });
        app.StepSimulation(TickInput.None);
        Assert.Equal(1000, g.Score);
        typeof(GameSession).GetProperty("Reserve")!.SetValue(g, 0);
        g.World.Enemies.Add(new Robotron.Core.Entities.Grunt { X = g.World.Player.X, Y = g.World.Player.Y, StepTimer = 999 });
        for (int i = 0; i < 400 && app.Screen == Screen.Playing; i++) app.StepSimulation(TickInput.None);
        Assert.Equal(Screen.EnterInitials, app.Screen);

        app.KeyDown(Key.Z); app.KeyDown(Key.A); app.KeyDown(Key.P);
        Tap(app, Key.Enter);
        Assert.Equal(Screen.HighScores, app.Screen);
        Assert.Equal("ZAP", app.HighScores.Entries[0].Initials);

        var reloaded = NewApp();
        Assert.Equal(1000, reloaded.HighScores.TopScore);
        Assert.Equal("ZAP", reloaded.HighScores.Entries[0].Initials);
    }

    [Fact]
    public void Classic_and_modern_keep_separate_high_score_tables()
    {
        var app = NewApp();
        app.Settings.Preset = PlayPreset.Modern;
        app.ApplySettings(); app.LoadHighScores();
        Assert.Empty(app.HighScores.Entries);
        Assert.Contains("modern", new DataPaths(_dir).HighScoreFile(1, "Modern"));
    }

    [Fact]
    public void Modern_suspend_then_resume_restores_the_game_and_is_single_use()
    {
        var app = NewApp(seed: 31);
        app.Settings.Preset = PlayPreset.Modern; app.ApplySettings(); app.SaveSettings();
        Tap(app, Key.Enter);
        for (int i = 0; i < 900; i++) app.StepSimulation(TestSupport.Bot(app.Session!));
        long hash = app.Session!.StateHash(), score = app.Session.Score;
        Assert.True(app.SuspendAndQuit());
        Assert.Null(app.Session);

        var app2 = NewApp();
        Assert.True(app2.SuspendAvailable);
        Assert.True(app2.ResumeSuspended());
        Assert.Equal(hash, app2.Session!.StateHash());
        Assert.Equal(score, app2.Session.Score);
        Assert.Equal(Screen.Paused, app2.Screen);
        Assert.False(app2.SuspendAvailable);
    }

    [Fact]
    public void Classic_cannot_suspend()
    {
        var app = NewApp();
        Tap(app, Key.Enter);
        Assert.False(app.SuspendAndQuit());
        Assert.NotNull(app.Session);
    }

    [Fact]
    public void Corrupt_suspend_file_is_reported_not_loaded()
    {
        var paths = new DataPaths(_dir);
        Directory.CreateDirectory(Path.GetDirectoryName(paths.SuspendFile(1, "modern"))!);
        File.WriteAllText(paths.SuspendFile(1, "modern"), "{\"schemaVersion\":1,\"engineVersion\":1,\"state\":null}");
        var app = NewApp();
        app.Settings.Preset = PlayPreset.Modern; app.ApplySettings();
        Assert.False(app.ResumeSuspended());
        Assert.NotNull(app.Notice);
        Assert.Null(app.Session);
    }

    [Fact]
    public void Corrupt_settings_fall_back_to_defaults_with_notice()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(new DataPaths(_dir).SettingsFile, "garbage");
        var app = NewApp();
        Assert.Equal(PlayPreset.Classic, app.Settings.Preset);
        Assert.NotNull(app.Notice);
    }

    [Fact]
    public void Policy_differences_classic_vs_modern()
    {
        var app = NewApp();
        Assert.False(app.Input.AnalogMove);
        app.Settings.Preset = PlayPreset.Modern; app.ApplySettings();
        Assert.True(app.Input.AnalogMove && app.Input.AnalogFire);
        Tap(app, Key.Enter);
        Assert.True(app.Session!.Rules.AnalogFire);
    }

    [Fact]
    public void Every_screen_renders_without_error()
    {
        var app = NewApp();
        var fb = new FrameBuffer(GameRenderer.Width, GameRenderer.Height);
        for (int i = 0; i < 1500; i++) { app.Advance(1 / 60.0); app.Render(fb); }   // attract cycle
        Tap(app, Key.Down); Tap(app, Key.Down); Tap(app, Key.Down); Tap(app, Key.Enter);   // settings
        Assert.Equal(Screen.Settings, app.Screen);
        for (int i = 0; i < 25; i++) { Tap(app, Key.Down); app.Render(fb); Tap(app, Key.Right); app.Render(fb); }
        Tap(app, Key.Escape);
        Assert.Equal(Screen.Title, app.Screen);
        Tap(app, Key.F1); app.Render(fb);
        Assert.Equal(Screen.Help, app.Screen);
    }

    [Fact]
    public void Remapping_a_key_moves_it_between_actions()
    {
        var app = NewApp();
        // Settings -> REMAP KEYS (index 19) -> first row (MOVE UP) -> press I
        Tap(app, Key.Down); Tap(app, Key.Down); Tap(app, Key.Down); Tap(app, Key.Enter);
        for (int i = 0; i < 19; i++) Tap(app, Key.Down);
        Tap(app, Key.Enter);
        Assert.Equal(Screen.Rebind, app.Screen);
        Tap(app, Key.Enter);
        app.KeyDown(Key.I); app.KeyUp(Key.I);
        Assert.Equal(["I"], app.Settings.KeyBindings[InputAction.MoveUp]);
    }
}
