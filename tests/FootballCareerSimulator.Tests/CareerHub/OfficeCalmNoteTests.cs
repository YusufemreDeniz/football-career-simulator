using FootballCareerSimulator.Application.CareerHub.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class OfficeCalmNoteTests
{
    [Fact]
    public void Resolve_Calm_IsDeterministicByDay()
    {
        var a = OfficeCalmNote.Resolve(WeekMoodDigest.MoodCalm, 12);
        var b = OfficeCalmNote.Resolve(WeekMoodDigest.MoodCalm, 12);

        Assert.False(string.IsNullOrWhiteSpace(a));
        Assert.Equal(a, b);
        Assert.NotEqual(
            OfficeCalmNote.Resolve(WeekMoodDigest.MoodCalm, 12),
            OfficeCalmNote.Resolve(WeekMoodDigest.MoodCalm, 13));
    }

    [Fact]
    public void ToBeatLine_CalmMatch_PrefixedNot()
    {
        var beat = OfficeCalmNote.ToBeatLine(WeekMoodDigest.MoodCalmMatch, 3);

        Assert.NotNull(beat);
        Assert.StartsWith("Not:", beat, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_BusyMood_ReturnsNull()
    {
        Assert.Null(OfficeCalmNote.Resolve(WeekMoodDigest.MoodPrep, 5));
        Assert.Null(OfficeCalmNote.ToBeatLine(WeekMoodDigest.MoodDesk, 5));
    }
}
