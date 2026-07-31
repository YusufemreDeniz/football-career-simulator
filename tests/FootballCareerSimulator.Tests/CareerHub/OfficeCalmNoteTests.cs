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

    [Fact]
    public void ToAdvanceConfirmation_WhenNoteChanges_SaysYenilendi()
    {
        var before = OfficeCalmNote.Resolve(WeekMoodDigest.MoodCalm, 12);
        var after = OfficeCalmNote.Resolve(WeekMoodDigest.MoodCalm, 13);
        Assert.NotEqual(before, after);

        var confirm = OfficeCalmNote.ToAdvanceConfirmation(before, after);

        Assert.NotNull(confirm);
        Assert.StartsWith("Not yenilendi:", confirm, StringComparison.Ordinal);
        Assert.Contains(after!, confirm, StringComparison.Ordinal);
    }

    [Fact]
    public void ToAdvanceConfirmation_WhenSameNote_KeepsCalmBeat()
    {
        var note = OfficeCalmNote.Resolve(WeekMoodDigest.MoodCalm, 12);

        Assert.Equal(
            "Yeni gün — sakin tempo sürüyor.",
            OfficeCalmNote.ToAdvanceConfirmation(note, note));
    }

    [Fact]
    public void ToAdvanceConfirmation_WhenLeavingCalm_ReturnsNull()
    {
        Assert.Null(OfficeCalmNote.ToAdvanceConfirmation(
            OfficeCalmNote.Resolve(WeekMoodDigest.MoodCalm, 12),
            nextNote: null));
    }
}
