using FootballCareerSimulator.Application.Competition.Queries;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class LeagueStatisticsDigestTests
{
    [Fact]
    public void Compose_UsesRealResultsForFormHomeAwayAndLeaders()
    {
        var standings = new[]
        {
            new StandingEntryReadModel(1, 2, 1, 1, 0, 4, 1, 4, 3),
            new StandingEntryReadModel(2, 2, 0, 1, 1, 1, 4, 1, -3),
        };
        var fixtures = new[]
        {
            Fixture(1, 1, 2, 1, 3, 0),
            Fixture(2, 2, 1, 2, 1, 1),
        };

        var digest = LeagueStatisticsDigest.Compose(
            standings,
            fixtures,
            new Dictionary<long, string> { [1] = "Lider", [2] = "Rakip" },
            managedClubId: 1);

        Assert.True(digest.HasData);
        Assert.Equal("G B", digest.GetForm(1));
        Assert.Contains("Lider (4)", digest.LeadersLine, StringComparison.Ordinal);
        Assert.Contains("iç saha 3/3", digest.ManagedClubLine, StringComparison.Ordinal);
        Assert.Contains("deplasman 1/3", digest.ManagedClubLine, StringComparison.Ordinal);
    }

    private static FixtureReadModel Fixture(long id, long home, long away, int round, int homeGoals, int awayGoals) =>
        new(id, 1, home, away, round, round, $"2026-08-{round + 10:D2}", "Accepted", homeGoals, awayGoals);
}
