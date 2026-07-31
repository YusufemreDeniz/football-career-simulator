using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.Match;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class MatchReportDigestTests
{
    [Fact]
    public void Compose_FormatsStatsAndGoalContributionStandout()
    {
        var result = new PlayFixtureMatchResult(
            Succeeded: true,
            SeasonId: 1,
            FixtureId: 10,
            HomeGoals: 2,
            AwayGoals: 1,
            Status: "ResultAccepted",
            KeyMoments:
            [
                new MatchKeyMomentReadModel(
                    nameof(MatchKeyMomentKind.Goal),
                    12,
                    IsHomeSide: true,
                    PrimarySlotIndex: 8,
                    AssistSlotIndex: 6,
                    PrimaryPlayerName: "Deniz Tekin",
                    AssistPlayerName: "Can Yılmaz"),
                new MatchKeyMomentReadModel(
                    nameof(MatchKeyMomentKind.Goal),
                    67,
                    IsHomeSide: true,
                    PrimarySlotIndex: 8,
                    PrimaryPlayerName: "Deniz Tekin"),
                new MatchKeyMomentReadModel(
                    nameof(MatchKeyMomentKind.Goal),
                    81,
                    IsHomeSide: false,
                    PrimarySlotIndex: 9,
                    PrimaryPlayerName: "Eren Çelik"),
            ],
            Statistics: new MatchStatisticsReadModel(
                HomePossessionPercent: 56,
                AwayPossessionPercent: 44,
                HomeShots: 13,
                AwayShots: 9,
                HomeShotsOnTarget: 6,
                AwayShotsOnTarget: 3,
                HomeCorners: 5,
                AwayCorners: 2));

        var report = MatchReportDigest.Compose(result, "Boğaziçi Spor", "Merkez FK");

        Assert.NotNull(report);
        Assert.Equal(4, report!.StatLines.Count);
        Assert.Equal("%56", report.StatLines[0].HomeValue);
        Assert.Equal("%44", report.StatLines[0].AwayValue);
        Assert.Contains("Deniz Tekin", report.StandoutLine, StringComparison.Ordinal);
        Assert.Contains("2 gol", report.StandoutLine, StringComparison.Ordinal);
        Assert.Contains("Boğaziçi Spor", report.StandoutLine, StringComparison.Ordinal);
        Assert.Null(report.InjuryLine);
    }

    [Fact]
    public void Compose_SurfacesInjuryLineFromKeyMoments()
    {
        var result = new PlayFixtureMatchResult(
            Succeeded: true,
            SeasonId: 1,
            FixtureId: 11,
            HomeGoals: 1,
            AwayGoals: 1,
            Status: "ResultAccepted",
            KeyMoments:
            [
                new MatchKeyMomentReadModel(
                    nameof(MatchKeyMomentKind.Injury),
                    71,
                    IsHomeSide: true,
                    PrimarySlotIndex: 4,
                    PrimaryPlayerName: "Tolga Kurt"),
            ],
            Statistics: new MatchStatisticsReadModel(50, 50, 8, 7, 3, 2, 4, 3));

        var report = MatchReportDigest.Compose(result, "Home", "Away");

        Assert.Equal("Sakatlık: 71' Tolga Kurt", report!.InjuryLine);
    }

    [Fact]
    public void Compose_WithoutStatistics_ReturnsNull()
    {
        var result = new PlayFixtureMatchResult(true, 1, 10, 0, 0, "ResultAccepted");

        Assert.Null(MatchReportDigest.Compose(result, "Ev", "Dep"));
    }
}
