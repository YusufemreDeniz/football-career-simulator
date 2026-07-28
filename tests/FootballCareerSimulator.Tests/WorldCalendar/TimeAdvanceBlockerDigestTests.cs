using FootballCareerSimulator.Application.WorldCalendar.Queries;

namespace FootballCareerSimulator.Tests.WorldCalendar;

public sealed class TimeAdvanceBlockerDigestTests
{
    [Fact]
    public void Clear_WhenCanAdvance()
    {
        var digest = TimeAdvanceBlockerDigest.Compose(true, Array.Empty<(string, string, bool)>());
        Assert.True(digest.CanAdvance);
        Assert.Null(digest.PrimaryBlockerCode);
        Assert.Contains("açık", digest.Headline, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnplayedFixtures_AdvisePlay()
    {
        var digest = TimeAdvanceBlockerDigest.Compose(
            canAdvance: false,
            [
                ("Competition", TimeAdvanceBlockerDigest.CodeUnplayedFixtures, false),
            ]);

        Assert.False(digest.CanAdvance);
        Assert.Equal(TimeAdvanceBlockerDigest.CodeUnplayedFixtures, digest.PrimaryBlockerCode);
        Assert.Contains("kilitli", digest.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Oyna", digest.AdviceLine, StringComparison.Ordinal);
        Assert.Contains("Öneri:", digest.ToDisplayText(), StringComparison.Ordinal);
    }

    [Fact]
    public void HardDecision_BeatsSoftFixtureInPrimary()
    {
        var digest = TimeAdvanceBlockerDigest.Compose(
            false,
            [
                ("Competition", TimeAdvanceBlockerDigest.CodeUnplayedFixtures, false),
                ("Interaction", TimeAdvanceBlockerDigest.CodePendingDecision, true),
            ]);

        Assert.Equal(TimeAdvanceBlockerDigest.CodePendingDecision, digest.PrimaryBlockerCode);
        Assert.Contains("Masada", digest.AdviceLine, StringComparison.Ordinal);
    }
}
