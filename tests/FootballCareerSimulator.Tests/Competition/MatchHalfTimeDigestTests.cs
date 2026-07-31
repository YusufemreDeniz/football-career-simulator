using FootballCareerSimulator.Application.Competition.Queries;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class MatchHalfTimeDigestTests
{
    [Fact]
    public void Trailing_UrgesAttack()
    {
        var digest = MatchHalfTimeDigest.Compose("Home", "Away", 0, 2, managedIsHome: true);

        Assert.True(digest.HasManagedMatch);
        Assert.Contains("Geridesin", digest.Headline, StringComparison.Ordinal);
        Assert.Contains("Hücum", digest.AdviceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Leading_UrgesManageGame()
    {
        var digest = MatchHalfTimeDigest.Compose("Home", "Away", 2, 0, managedIsHome: true);

        Assert.Contains("Öndesin", digest.Headline, StringComparison.Ordinal);
        Assert.Contains("Savunma", digest.AdviceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatDecisionKeyMoment_MapsApproachLabels()
    {
        Assert.Equal(
            "46' Karar · Hücuma geçtin",
            MatchHalfTimeDigest.FormatDecisionKeyMoment("Devre arasında hücuma geçtin."));
        Assert.Equal(
            "46' Karar · Savunmaya çektin",
            MatchHalfTimeDigest.FormatDecisionKeyMoment("Devre arasında savunmaya çektin."));
        Assert.Equal(
            "46' Karar · Aynı plan",
            MatchHalfTimeDigest.FormatDecisionKeyMoment("Devre arasında aynı planla devam ettin."));
        Assert.Null(MatchHalfTimeDigest.FormatDecisionKeyMoment("Devre arasında Ali↔Can."));
    }
}
