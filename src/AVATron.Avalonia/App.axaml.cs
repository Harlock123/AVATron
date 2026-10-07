using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AVATron.Avalonia.Shell;
using AVATron.Infrastructure.Audio;
using AVATron.Infrastructure.Input;
using AVATron.Infrastructure.Persistence;

namespace AVATron.Avalonia;

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
            Console.WriteLine($"[avatron] data: {paths.Root}");
            Console.WriteLine($"[avatron] {_audio.Status}; {_pad.Status}");

            var app = new AppController(paths, mixer, _pad, _audio.Status);
            if (Options.Windowed) app.Settings.Fullscreen = false;
            if (Options.StartWave is { } sw) { app.Settings.StartWave = sw; app.SaveSettings(); }   // same as choosing it on the title menu
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
    public int? StartWave { get; init; }

    public static StartupOptions Parse(string[] args)
    {
        string? dataDir = null;
        int? startWave = null;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--data-dir") dataDir = args[i + 1];
            if (args[i] == "--start-wave" && int.TryParse(args[i + 1], out int w)) startWave = Math.Clamp(w, 1, SettingsDocument.MaxStartWave);
        }
        return new StartupOptions
        {
            NoAudio = args.Contains("--no-audio") || Environment.GetEnvironmentVariable("AVATRON_NO_AUDIO") == "1",
            NoGamepad = args.Contains("--no-gamepad"),
            Windowed = args.Contains("--windowed"),
            DataDir = dataDir,
            StartWave = startWave,
        };
    }
}
