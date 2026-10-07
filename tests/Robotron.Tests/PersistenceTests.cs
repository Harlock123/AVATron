using Robotron.Core.Scoring;
using Robotron.Infrastructure.Persistence;
using Xunit;

namespace Robotron.Tests;

public sealed class PersistenceTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "robotron-tests-" + Guid.NewGuid().ToString("N"));
    public PersistenceTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, true); } catch (IOException) { } }

    [Fact]
    public void High_scores_round_trip()
    {
        var t = new HighScoreTable();
        for (int i = 0; i < 12; i++) t.Insert(new HighScoreEntry("ABC", 1000 + i * 37, i + 1, new DateTimeOffset(2026, 10, 7, 0, 0, 0, TimeSpan.Zero)));
        var store = HighScoreDocument.Store();
        var path = Path.Combine(_dir, "hs.json");
        store.Save(path, HighScoreDocument.FromTable(t));
        var r = store.Load(path);
        Assert.Equal(LoadStatus.Ok, r.Status);
        Assert.Equal(t.Entries, r.Value.ToTable().Entries);
    }

    [Fact]
    public void Missing_file_gives_defaults()
    {
        var r = SettingsDocument.Store().Load(Path.Combine(_dir, "nope.json"));
        Assert.Equal(LoadStatus.Missing, r.Status);
        Assert.Equal(PlayPreset.Classic, r.Value.Preset);
    }

    [Theory]
    [InlineData("{ this is not json")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{\"schemaVersion\":1,\"masterVolume\":7}")]   // fails validation
    public void Corrupt_file_gives_defaults_message_and_backup(string contents)
    {
        var path = Path.Combine(_dir, "settings.json");
        File.WriteAllText(path, contents);
        var r = SettingsDocument.Store().Load(path);
        Assert.Equal(LoadStatus.Corrupt, r.Status);
        Assert.NotNull(r.Message);
        Assert.False(File.Exists(path));
        Assert.Single(Directory.GetFiles(_dir, "settings.json.bak-*"));
    }

    [Fact]
    public void Future_schema_version_is_rejected_not_misread()
    {
        var path = Path.Combine(_dir, "settings.json");
        File.WriteAllText(path, "{\"schemaVersion\":99,\"preset\":\"Modern\"}");
        var r = SettingsDocument.Store().Load(path);
        Assert.Equal(LoadStatus.IncompatibleVersion, r.Status);
        Assert.Equal(PlayPreset.Classic, r.Value.Preset);
    }

    [Fact]
    public void Atomic_write_replaces_and_leaves_no_temp_files()
    {
        var path = Path.Combine(_dir, "x.json");
        AtomicFile.WriteAllText(path, "one");
        AtomicFile.WriteAllText(path, "two");
        Assert.Equal("two", File.ReadAllText(path));
        Assert.Single(Directory.GetFiles(_dir));
    }

    [Fact]
    public void Settings_round_trip_including_bindings()
    {
        var s = new SettingsDocument { Preset = PlayPreset.Modern, EffectsVolume = 0.25f };
        s.KeyBindings[InputAction.FireUp] = ["I"];
        var store = SettingsDocument.Store();
        var path = Path.Combine(_dir, "settings.json");
        store.Save(path, s);
        var r = store.Load(path).Value;
        Assert.Equal(PlayPreset.Modern, r.Preset);
        Assert.Equal(0.25f, r.EffectsVolume);
        Assert.Equal(["I"], r.KeyBindings[InputAction.FireUp]);
    }
}
