using System.Text.Json;
using System.Text.Json.Serialization;

namespace AVATron.Infrastructure.Persistence;

public interface IVersionedDocument
{
    int SchemaVersion { get; set; }
}

public enum LoadStatus { Ok, Missing, Corrupt, IncompatibleVersion, Migrated }

public sealed record LoadResult<T>(T Value, LoadStatus Status, string? Message = null)
{
    public bool UsedDefaults => Status is LoadStatus.Missing or LoadStatus.Corrupt or LoadStatus.IncompatibleVersion;
}

/// Reads and writes one JSON document type with schema-version checking and atomic replacement.
/// Policy: files newer than this build, or older than <see cref="MinSupportedVersion"/>, are set aside
/// (renamed *.bak-*) and defaults are used; older supported versions go through <see cref="Migrate"/>.
public sealed class VersionedJsonStore<T> where T : class, IVersionedDocument
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    readonly Func<T> _defaults;
    readonly Func<T, string?> _validate;

    public VersionedJsonStore(int currentVersion, Func<T> defaults, Func<T, string?>? validate = null, int? minSupportedVersion = null, Func<T, T>? migrate = null)
    {
        CurrentVersion = currentVersion;
        MinSupportedVersion = minSupportedVersion ?? currentVersion;
        _defaults = defaults;
        _validate = validate ?? (_ => null);
        Migrate = migrate ?? (t => t);
    }

    public int CurrentVersion { get; }
    public int MinSupportedVersion { get; }
    public Func<T, T> Migrate { get; }

    public LoadResult<T> Load(string path)
    {
        if (!File.Exists(path)) return new(_defaults(), LoadStatus.Missing);
        T? doc;
        try
        {
            doc = JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
            if (doc is null) throw new JsonException("empty document");
        }
        catch (Exception ex) when (ex is JsonException or IOException or NotSupportedException or UnauthorizedAccessException)
        {
            return SetAside(path, LoadStatus.Corrupt, $"Could not read {Path.GetFileName(path)} ({ex.Message}); using defaults. The damaged file was kept as a backup.");
        }

        if (doc.SchemaVersion > CurrentVersion || doc.SchemaVersion < MinSupportedVersion)
            return SetAside(path, LoadStatus.IncompatibleVersion, $"{Path.GetFileName(path)} has schema version {doc.SchemaVersion}; this build supports {MinSupportedVersion}..{CurrentVersion}. Using defaults; the old file was kept as a backup.");

        var status = LoadStatus.Ok;
        if (doc.SchemaVersion < CurrentVersion) { doc = Migrate(doc); doc.SchemaVersion = CurrentVersion; status = LoadStatus.Migrated; }

        var problem = _validate(doc);
        if (problem is not null)
            return SetAside(path, LoadStatus.Corrupt, $"{Path.GetFileName(path)} failed validation ({problem}); using defaults. The file was kept as a backup.");
        return new(doc, status);
    }

    public void Save(string path, T doc)
    {
        doc.SchemaVersion = CurrentVersion;
        AtomicFile.WriteAllText(path, JsonSerializer.Serialize(doc, Options));
    }

    LoadResult<T> SetAside(string path, LoadStatus status, string message)
    {
        try { File.Move(path, $"{path}.bak-{DateTime.UtcNow:yyyyMMddHHmmss}", overwrite: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return new(_defaults(), status, message);
    }
}

public static class AtomicFile
{
    /// Write to a temp file in the same directory, flush to disk, then rename over the target,
    /// so a crash mid-write leaves either the old file or the new one, never a torn file.
    public static void WriteAllText(string path, string contents)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(dir);
        var tmp = Path.Combine(dir, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var fs = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var w = new StreamWriter(fs))
            {
                w.Write(contents);
                w.Flush();
                fs.Flush(flushToDisk: true);
            }
            File.Move(tmp, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
