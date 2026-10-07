using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Robotron.Avalonia.Shell;
using Robotron.Infrastructure.Audio;
using Robotron.Infrastructure.Input;
using Robotron.Infrastructure.Persistence;

namespace Robotron.Avalonia;

public partial class App : Application
{
    public static StartupOptions Options { get; set; } = new();
    IAudioOutput? _audio;
    IGamepadSource? _pad;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var paths = Options.DataDir is { } d ? new DataPaths(d) : DataPaths.Default();
            var mixer = new AudioMixer();
            _audio = Options.NoAudio ? new NullAudioOutput("Audio disabled (--no-audio)") : SdlAudioOutput.TryCreate(mixer);
            _pad = Options.NoGamepad ? new NoGamepadSource("Gamepad disabled") : SdlGamepadSource.TryCreate();
            Console.WriteLine($"[robotron] data: {paths.Root}");
            Console.WriteLine($"[robotron] {_audio.Status}; {_pad.Status}");

            var app = new AppController(paths, mixer, _pad, _audio.Status);
            if (Options.Windowed) app.Settings.Fullscreen = false;
            desktop.MainWindow = new MainWindow(app);
            desktop.Exit += (_, _) => { _audio?.Dispose(); _pad?.Dispose(); };
        }
        base.OnFrameworkInitializationCompleted();
    }
}

public sealed record StartupOptions
{
    public bool NoAudio { get; init; }
    public bool NoGamepad { get; init; }
    public bool Windowed { get; init; }
    public string? DataDir { get; init; }

    public static StartupOptions Parse(string[] args)
    {
        string? dataDir = null;
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == "--data-dir") dataDir = args[i + 1];
        return new StartupOptions
        {
            NoAudio = args.Contains("--no-audio") || Environment.GetEnvironmentVariable("ROBOTRON_NO_AUDIO") == "1",
            NoGamepad = args.Contains("--no-gamepad"),
            Windowed = args.Contains("--windowed"),
            DataDir = dataDir,
        };
    }
}
