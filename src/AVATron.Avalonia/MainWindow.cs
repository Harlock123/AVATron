using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AVATron.Avalonia.Shell;

namespace AVATron.Avalonia;

public sealed class MainWindow : Window
{
    readonly AppController _app;
    static readonly bool DebugKeys = Environment.GetEnvironmentVariable("AVATRON_DEBUG_KEYS") == "1";

    public MainWindow(AppController app)
    {
        _app = app;
        Title = Branding.WindowTitle;
        Width = 900; Height = 740;
        MinWidth = 320; MinHeight = 260;
        Background = Brushes.Black;
        Content = new GameView(app);
        if (app.Settings.Fullscreen) WindowState = WindowState.FullScreen;

        app.FullscreenRequested += full => WindowState = full ? WindowState.FullScreen : WindowState.Normal;
        app.QuitRequested += Close;
        Deactivated += (_, _) => { app.FocusLost(); if (app.Screen == Screen.Playing && app.IsModern) app.Pause(); };
        Closing += (_, _) => app.OnShutdown();

        // Handle keys before focus navigation can claim arrows/Tab.
        AddHandler(KeyDownEvent, (_, e) => { if (DebugKeys) Console.WriteLine($"[key] down {e.Key}"); _app.KeyDown(e.Key); e.Handled = true; }, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, (_, e) => { _app.KeyUp(e.Key); e.Handled = true; }, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }
}
