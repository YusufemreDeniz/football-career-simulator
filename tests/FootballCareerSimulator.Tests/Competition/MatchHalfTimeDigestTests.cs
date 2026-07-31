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
}
