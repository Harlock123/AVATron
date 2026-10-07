using Robotron.Core.Scoring;
using Xunit;

namespace Robotron.Tests;

public class HighScoreTests
{
    static HighScoreEntry E(string i, long s) => new(i, s, 1, DateTimeOffset.UnixEpoch);

    [Fact]
    public void Inserts_in_descending_order_and_reports_rank()
    {
        var t = new HighScoreTable();
        Assert.Equal(0, t.Insert(E("AAA", 100)));
        Assert.Equal(0, t.Insert(E("BBB", 300)));
        Assert.Equal(1, t.Insert(E("CCC", 200)));
        Assert.Equal(new long[] { 300, 200, 100 }, t.Entries.Select(e => e.Score));
    }

    [Fact]
    public void Tenth_entry_is_displaced_and_ties_do_not_displace()
    {
        var t = new HighScoreTable();
        for (int i = 1; i <= 10; i++) t.Insert(E("AAA", i * 1000));
        Assert.False(t.Qualifies(1000));                  // equals the 10th: does not place
        Assert.Equal(-1, t.Insert(E("ZZZ", 1000)));
        Assert.Equal(9, t.Insert(E("NEW", 1500)));        // beats the 10th
        Assert.Equal(10, t.Entries.Count);
        Assert.Equal(1500, t.Entries[^1].Score);
        Assert.DoesNotContain(t.Entries, e => e.Score == 1000);
    }

    [Fact]
    public void Equal_score_ranks_below_existing_entry()
    {
        var t = new HighScoreTable();
        t.Insert(E("OLD", 500));
        Assert.Equal(1, t.Insert(E("NEW", 500)));
        Assert.Equal("OLD", t.Entries[0].Initials);
    }

    [Theory]
    [InlineData("ab", "AB ")]
    [InlineData("abcdef", "ABC")]
    [InlineData("a$b", "AB ")]
    public void Initials_are_normalised(string raw, string expected) => Assert.Equal(expected, HighScoreTable.NormalizeInitials(raw));

    [Fact]
    public void Zero_score_never_qualifies() => Assert.False(new HighScoreTable().Qualifies(0));
}
