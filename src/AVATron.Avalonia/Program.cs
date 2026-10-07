using Avalonia;

namespace AVATron.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.Options = StartupOptions.Parse(args);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
