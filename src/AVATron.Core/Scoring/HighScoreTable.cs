namespace AVATron.Core.Scoring;

public sealed record HighScoreEntry(string Initials, long Score, int Wave, DateTimeOffset Date);

/// Ordered top-N table. Ties keep the earlier entry ahead (a new score must beat, not equal, to pass it).
public sealed class HighScoreTable
{
    public const int DefaultCapacity = 10;
    public const int InitialsLength = 3;
    readonly List<HighScoreEntry> _entries = [];

    public HighScoreTable(int capacity = DefaultCapacity) { Capacity = capacity; }
    public int Capacity { get; }
    public IReadOnlyList<HighScoreEntry> Entries => _entries;
    public long TopScore => _entries.Count > 0 ? _entries[0].Score : 0;

    public bool Qualifies(long score) => score > 0 && (_entries.Count < Capacity || score > _entries[^1].Score);

    /// Inserts if the score qualifies; returns the 0-based rank, or -1 if it did not place.
    public int Insert(HighScoreEntry entry)
    {
        if (!Qualifies(entry.Score)) return -1;
        var clean = entry with { Initials = NormalizeInitials(entry.Initials) };
        int i = 0;
        while (i < _entries.Count && _entries[i].Score >= entry.Score) i++;
        _entries.Insert(i, clean);
        if (_entries.Count > Capacity) _entries.RemoveAt(_entries.Count - 1);
        return i;
    }

    public static string NormalizeInitials(string s)
    {
        var chars = (s ?? "").ToUpperInvariant().Where(c => (c >= 'A' && c <= 'Z') || c == ' ' || c == '.' || (c >= '0' && c <= '9')).Take(InitialsLength).ToArray();
        return new string(chars).PadRight(InitialsLength);
    }
}
