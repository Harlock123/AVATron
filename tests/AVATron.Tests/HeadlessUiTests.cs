using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using AVATron.Avalonia;
using AVATron.Avalonia.Shell;
using AVATron.Infrastructure.Audio;
using AVATron.Infrastructure.Input;
using AVATron.Infrastructure.Persistence;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(AVATron.Tests.HeadlessApp))]

namespace AVATron.Tests;

public static class HeadlessApp
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

public class HeadlessUiTests
{
    static AppController NewApp() =>
        new(new DataPaths(Path.Combine(Path.GetTempPath(), "avatron-ui-" + Guid.NewGuid().ToString("N"))), new AudioMixer(), new NoGamepadSource("t"), "t", () => 3);

    static void Pump(int frames)
    {
        for (int i = 0; i < frames; i++) { AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); }
    }

    [AvaloniaFact]
    public void Window_renders_the_game_canvas()
    {
        var app = NewApp();
        var win = new MainWindow(app) { Width = 600, Height = 500 };
        win.Show();
        Pump(10);
        var frame = win.CaptureRenderedFrame();
        Assert.NotNull(frame);
        win.Close();
    }

    [AvaloniaFact]
    public void Keyboard_input_reaches_the_engine()
    {
        var app = NewApp();
        var win = new MainWindow(app) { Width = 600, Height = 500 };
        win.Show();
        Pump(3);
        win.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Pump(3);
        win.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Pump(3);
        Assert.Equal(Screen.Playing, app.Screen);
        win.Close();
    }

    [AvaloniaFact]
    public void Window_resizes_and_canvas_follows()
    {
        var app = NewApp();
        var win = new MainWindow(app) { Width = 600, Height = 500 };
        win.Show();
        Pump(2);
        win.Width = 1000; win.Height = 400;
        Pump(2);
        var view = Assert.IsType<GameView>(win.Content);
        Assert.Equal(1000, view.Bounds.Width, 0);
        Assert.Equal(400, view.Bounds.Height, 0);
        win.Close();
    }
}
