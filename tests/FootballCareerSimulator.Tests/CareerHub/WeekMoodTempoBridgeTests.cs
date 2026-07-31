using FootballCareerSimulator.Application.CareerHub.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class WeekMoodTempoBridgeTests
{
    [Fact]
    public void Resolve_CalmToMatchDraft_RaisesTempo()
    {
        var shift = WeekMoodTempoBridge.Resolve(
            WeekMoodDigest.MoodCalm,
            WeekMoodDigest.MoodMatchDraft);

        Assert.NotNull(shift);
        Assert.Contains("Tempo yükseldi", shift.Headline, StringComparison.Ordinal);
        Assert.Contains("kadro", shift.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(TodayPulseDigest.FocusMatch, shift.NextFocusCode);
    }

    [Fact]
    public void Resolve_CalmToMatchReady_PointsToKickoff()
    {
        var shift = WeekMoodTempoBridge.Resolve(
            WeekMoodDigest.MoodCalm,
            WeekMoodDigest.MoodMatchReady);

        Assert.NotNull(shift);
        Assert.Contains("düdük", shift.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Maç Gününe", shift.AdviceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_SameMood_ReturnsNull()
    {
        Assert.Null(WeekMoodTempoBridge.Resolve(
            WeekMoodDigest.MoodCalm,
            WeekMoodDigest.MoodCalm));
    }

    [Fact]
    public void Resolve_PrepToMatch_IsNotCalmBridge()
    {
        Assert.Null(WeekMoodTempoBridge.Resolve(
            WeekMoodDigest.MoodPrep,
            WeekMoodDigest.MoodMatchDraft));
    }

    [Fact]
    public void Resolve_MatchDraftToReady_SettlesTempo()
    {
        var shift = WeekMoodTempoBridge.Resolve(
            WeekMoodDigest.MoodMatchDraft,
            WeekMoodDigest.MoodMatchReady);

        Assert.NotNull(shift);
        Assert.Contains("oturdu", shift.Headline, StringComparison.OrdinalIgnoreCase);
    }
}
