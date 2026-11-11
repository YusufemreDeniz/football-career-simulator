using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Competition.Events;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Competition;

public class LeagueFixtureGeneratorTests
{
    private static readonly CompetitionId Competition = new(1);
    private static readonly SeasonId Season = new(1);
    private static readonly GameDate FirstMatchday = GameDate.FromCalendarDate(2026, 8, 1);

    private static IReadOnlyList<ClubId> CreateParticipants() =>
        Enumerable.Range(1, CompetitionMvpConstraints.LeagueTeamCount)
            .Select(index => new ClubId(index))
            .ToArray();

    [Fact]
    public void GenerateDoubleRoundRobin_ProducesThreeHundredSixPlannedFixtures()
    {
        var fixtures = LeagueFixtureGenerator.GenerateDoubleRoundRobin(
            Competition,
            Season,
            CreateParticipants(),
            FirstMatchday,
            daysBetweenRounds: 7,
            startingFixtureId: new FixtureId(1));

        Assert.Equal(CompetitionMvpConstraints.TotalLeagueFixtures, fixtures.Count);
        Assert.All(fixtures, fixture => Assert.Equal(FixtureStatus.Planned, fixture.Status));
        Assert.Equal(new FixtureId(306), fixtures[^1].Id);
    }

    [Fact]
    public void GenerateDoubleRoundRobin_EachClubPlaysThirtyFourMatches()
    {
        var fixtures = LeagueFixtureGenerator.GenerateDoubleRoundRobin(
            Competition,
            Season,
            CreateParticipants(),
            FirstMatchday,
            daysBetweenRounds: 7,
            startingFixtureId: new FixtureId(1));

        foreach (var clubId in CreateParticipants())
        {
            var appearances = fixtures.Count(fixture =>
                fixture.HomeClubId == clubId || fixture.AwayClubId == clubId);

            Assert.Equal(CompetitionMvpConstraints.LeagueMatchesPerTeam, appearances);
        }
    }

    [Fact]
    public void GenerateDoubleRoundRobin_EachPairPlaysHomeAndAwayOnce()
    {
        var fixtures = LeagueFixtureGenerator.GenerateDoubleRoundRobin(
            Competition,
            Season,
            CreateParticipants(),
            FirstMatchday,
            daysBetweenRounds: 7,
            startingFixtureId: new FixtureId(1));

        var clubs = CreateParticipants();

        foreach (var home in clubs)
        {
            foreach (var away in clubs)
            {
                if (home == away)
                {
                    continue;
                }

                var matches = fixtures.Where(fixture =>
                    (fixture.HomeClubId == home && fixture.AwayClubId == away)
                    || (fixture.HomeClubId == away && fixture.AwayClubId == home)).ToArray();

                Assert.Equal(2, matches.Length);
                Assert.Contains(matches, fixture => fixture.HomeClubId == home && fixture.AwayClubId == away);
                Assert.Contains(matches, fixture => fixture.HomeClubId == away && fixture.AwayClubId == home);
            }
        }
    }

    [Fact]
    public void GenerateDoubleRoundRobin_AssignsNineMatchesPerRoundAcrossThirtyFourRounds()
    {
        var fixtures = LeagueFixtureGenerator.GenerateDoubleRoundRobin(
            Competition,
            Season,
            CreateParticipants(),
            FirstMatchday,
            daysBetweenRounds: 7,
            startingFixtureId: new FixtureId(1));

        for (var round = 1; round <= CompetitionMvpConstraints.LeagueMatchesPerTeam; round++)
        {
            var roundFixtures = fixtures.Where(fixture => fixture.Round.Value == round).ToArray();

            Assert.Equal(CompetitionMvpConstraints.LeagueFixturesPerRound, roundFixtures.Length);

            var clubsInRound = roundFixtures
                .SelectMany(fixture => new[] { fixture.HomeClubId, fixture.AwayClubId })
                .ToArray();

            Assert.Equal(CompetitionMvpConstraints.LeagueTeamCount, clubsInRound.Length);
            Assert.Equal(CompetitionMvpConstraints.LeagueTeamCount, clubsInRound.Distinct().Count());
        }
    }

    [Fact]
    public void GenerateDoubleRoundRobin_SchedulesWeeklyMatchdays()
    {
        var fixtures = LeagueFixtureGenerator.GenerateDoubleRoundRobin(
            Competition,
            Season,
            CreateParticipants(),
            FirstMatchday,
            daysBetweenRounds: 7,
            startingFixtureId: new FixtureId(1));

        Assert.All(
            fixtures.Where(fixture => fixture.Round.Value == 1),
            fixture => Assert.Equal(FirstMatchday, fixture.ScheduledDate));

        var secondLegStart = fixtures.First(
            fixture => fixture.Round.Value == CompetitionMvpConstraints.LeagueTeamCount);
        var expectedSecondLegStart = FirstMatchday.AddDays(
            (CompetitionMvpConstraints.LeagueTeamCount - 1) * 7);
        Assert.Equal(expectedSecondLegStart, secondLegStart.ScheduledDate);
    }

    [Fact]
    public void GenerateDoubleRoundRobin_IsDeterministicForSameInput()
    {
        var participants = CreateParticipants();

        var first = LeagueFixtureGenerator.GenerateDoubleRoundRobin(
            Competition,
            Season,
            participants,
            FirstMatchday,
            daysBetweenRounds: 7,
            startingFixtureId: new FixtureId(1));

        var second = LeagueFixtureGenerator.GenerateDoubleRoundRobin(
            Competition,
            Season,
            participants,
            FirstMatchday,
            daysBetweenRounds: 7,
            startingFixtureId: new FixtureId(1));

        Assert.Equal(
            first.Select(fixture => (fixture.HomeClubId, fixture.AwayClubId, fixture.Round.Value, fixture.ScheduledDate.DayNumber)),
            second.Select(fixture => (fixture.HomeClubId, fixture.AwayClubId, fixture.Round.Value, fixture.ScheduledDate.DayNumber)));
    }

    [Fact]
    public void GenerateDoubleRoundRobin_TwentyTeams_ProducesThreeHundredEightyFixtures()
    {
        var participants = Enumerable.Range(1, CompetitionMvpConstraints.MaxLeagueTeamCount)
            .Select(index => new ClubId(index))
            .ToArray();

        var fixtures = LeagueFixtureGenerator.GenerateDoubleRoundRobin(
            Competition,
            Season,
            participants,
            FirstMatchday,
            daysBetweenRounds: 7,
            startingFixtureId: new FixtureId(1));

        Assert.Equal(
            CompetitionMvpConstraints.TotalFixturesFor(CompetitionMvpConstraints.MaxLeagueTeamCount),
            fixtures.Count);
        Assert.Equal(new FixtureId(380), fixtures[^1].Id);
        Assert.Equal(
            CompetitionMvpConstraints.FixturesPerRoundFor(CompetitionMvpConstraints.MaxLeagueTeamCount),
            fixtures.Count(fixture => fixture.Round.Value == 1));
    }
}
