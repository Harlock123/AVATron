using AVATron.Core.Scoring;

namespace AVATron.Infrastructure.Persistence;

public enum PlayPreset { Classic, Modern }

/// Logical actions that can be bound to keys. The engine never sees keys, only TickInput.
public enum InputAction
{
    MoveUp, MoveDown, MoveLeft, MoveRight,
    FireUp, FireDown, FireLeft, FireRight,
    Start, Pause, Fullscreen, Help,
}

public sealed class SettingsDocument : IVersionedDocument
{
    public const int Version = 1;
    public int SchemaVersion { get; set; } = Version;

    public PlayPreset Preset { get; set; } = PlayPreset.Classic;
    public int SaveSlot { get; set; } = 1;

    // Operator adjustments (apply to both presets; arcade factory values by default)
    public int Difficulty { get; set; } = 3;
    public int LivesPerGame { get; set; } = 3;
    public int ExtraLifeEvery { get; set; } = 25_000;

    /// Practice: wave to start new games on (1 = normal game). Games started past wave 1 don't enter the high-score table.
    public int StartWave { get; set; } = 1;
    public const int MaxStartWave = 99;

    // Audio (0..1)
    public bool AudioEnabled { get; set; } = true;
    public float MasterVolume { get; set; } = 0.8f;
    public float EffectsVolume { get; set; } = 0.9f;
    public float AmbientVolume { get; set; } = 0.6f;
    /// Modern addition (the arcade had no background audio): a low looping hum under play.
    public bool AmbientHum { get; set; }

    // Display
    public bool Fullscreen { get; set; }
    public bool IntegerScaling { get; set; } = true;
    /// False = stretch to the arcade monitor's 4:3 shape (authentic); true = square pixels.
    public bool SquarePixels { get; set; }
    public bool SmoothScalingInModern { get; set; }
    public bool ReducedMotion { get; set; }
    /// Modern only: suppress the hardware-flicker approximation.
    public bool SuppressFlickerInModern { get; set; } = true;
    public bool ShowWaveProgressInModern { get; set; } = true;

    // Input
    public float GamepadDeadzone { get; set; } = 0.2f;
    /// Modern only. Accessibility: simulation rate multiplier (0.5..1.0). Disclosed on the HUD when below 1.
    public float ModernGameSpeed { get; set; } = 1.0f;
    public Dictionary<InputAction, List<string>> KeyBindings { get; set; } = DefaultKeyBindings();

    /// Key names are Avalonia.Input.Key enum names; kept as strings so this layer has no UI dependency.
    public static Dictionary<InputAction, List<string>> DefaultKeyBindings() => new()
    {
        [InputAction.MoveUp] = ["W"], [InputAction.MoveDown] = ["S"],
        [InputAction.MoveLeft] = ["A"], [InputAction.MoveRight] = ["D"],
        [InputAction.FireUp] = ["Up"], [InputAction.FireDown] = ["Down"],
        [InputAction.FireLeft] = ["Left"], [InputAction.FireRight] = ["Right"],
        [InputAction.Start] = ["Enter", "Space"], [InputAction.Pause] = ["Escape", "P"],
        [InputAction.Fullscreen] = ["F11"], [InputAction.Help] = ["F1", "H"],
    };

    public static string? Validate(SettingsDocument s)
    {
        if (s.SaveSlot is < 1 or > 3) return "saveSlot out of range 1..3";
        foreach (var v in new[] { s.MasterVolume, s.EffectsVolume, s.AmbientVolume })
            if (float.IsNaN(v) || v < 0 || v > 1) return "volume out of range 0..1";
        if (float.IsNaN(s.GamepadDeadzone) || s.GamepadDeadzone < 0 || s.GamepadDeadzone > 0.9f) return "gamepadDeadzone out of range 0..0.9";
        if (float.IsNaN(s.ModernGameSpeed) || s.ModernGameSpeed < 0.25f || s.ModernGameSpeed > 1f) return "modernGameSpeed out of range 0.25..1";
        if (s.KeyBindings is null) return "keyBindings missing";
        if (s.Difficulty is < 0 or > 10) return "difficulty out of range 0..10";
        if (s.LivesPerGame is < 1 or > 20) return "livesPerGame out of range 1..20";
        if (s.ExtraLifeEvery is < 0 or > 50_000) return "extraLifeEvery out of range 0..50000";
        if (s.StartWave is < 1 or > MaxStartWave) return $"startWave out of range 1..{MaxStartWave}";
        return null;
    }

    /// Fill any action missing from a hand-edited or older file with its default binding.
    public void EnsureAllBindings()
    {
        foreach (var (action, keys) in DefaultKeyBindings())
            if (!KeyBindings.TryGetValue(action, out var have) || have is null || have.Count == 0)
                KeyBindings[action] = keys;
    }

    public static VersionedJsonStore<SettingsDocument> Store() =>
        new(Version, () => new SettingsDocument(), Validate);
}

public sealed class HighScoreDocument : IVersionedDocument
{
    public const int Version = 1;
    public int SchemaVersion { get; set; } = Version;
    public List<HighScoreRow> Entries { get; set; } = [];

    public sealed class HighScoreRow
    {
        public string Initials { get; set; } = "";
        public long Score { get; set; }
        public int Wave { get; set; }
        public DateTimeOffset Date { get; set; }
    }

    public HighScoreTable ToTable()
    {
        var t = new HighScoreTable();
        foreach (var r in Entries) t.Insert(new HighScoreEntry(r.Initials, r.Score, r.Wave, r.Date));
        return t;
    }

    public static HighScoreDocument FromTable(HighScoreTable t) => new()
    {
        Entries = t.Entries.Select(e => new HighScoreRow { Initials = e.Initials, Score = e.Score, Wave = e.Wave, Date = e.Date }).ToList(),
    };

    public static string? Validate(HighScoreDocument d)
    {
        if (d.Entries is null) return "entries missing";
        if (d.Entries.Count > HighScoreTable.DefaultCapacity) return "too many entries";
        foreach (var e in d.Entries)
            if (e.Score < 0 || e.Wave < 0 || e.Initials is null || e.Initials.Length > 8) return "invalid entry";
        return null;
    }

    public static VersionedJsonStore<HighScoreDocument> Store() => new(Version, () => new HighScoreDocument(), Validate);
}
