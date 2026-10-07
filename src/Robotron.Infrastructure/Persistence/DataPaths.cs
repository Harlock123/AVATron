namespace Robotron.Infrastructure.Persistence;

/// Resolves the per-user data directory. Override with ROBOTRON_DATA_DIR (used by tests and portable installs).
public sealed class DataPaths
{
    /// Branding-neutral folder name lives in one place so it can be renamed.
    public const string FolderName = "Robotron2084";

    public DataPaths(string root) { Root = root; }
    public string Root { get; }

    public string SettingsFile => Path.Combine(Root, "settings.json");
    public string SlotDirectory(int slot) => Path.Combine(Root, $"slot{slot}");
    public string HighScoreFile(int slot, string preset) => Path.Combine(SlotDirectory(slot), $"highscores-{preset.ToLowerInvariant()}.json");
    public string SuspendFile(int slot, string preset) => Path.Combine(SlotDirectory(slot), $"suspend-{preset.ToLowerInvariant()}.json");

    public static DataPaths Default()
    {
        var env = Environment.GetEnvironmentVariable("ROBOTRON_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(env)) return new DataPaths(env);
        string baseDir;
        if (OperatingSystem.IsWindows())
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);          // %APPDATA%
        else if (OperatingSystem.IsMacOS())
            baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");
        else
        {
            var xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            baseDir = string.IsNullOrWhiteSpace(xdg)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config")
                : xdg;
        }
        return new DataPaths(Path.Combine(baseDir, FolderName));
    }
}
