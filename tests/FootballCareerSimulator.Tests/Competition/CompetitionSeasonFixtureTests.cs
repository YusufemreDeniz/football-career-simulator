using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Competition.Events;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Competition;

public class CompetitionSeasonFixtureTests
{
    private static readonly CompetitionId Competition = new(1);
    private static readonly SeasonId Season = new(1);
    private static readonly GameDate PreseasonStart = GameDate.FromCalendarDate(2026, 7, 1);
    private static readonly GameDate FirstMatchday = GameDate.FromCalendarDate(2026, 8, 1);

    private static CompetitionSeason CreateActiveSeasonWithParticipants()
    {
        var season = CompetitionSeason.Create(Competition, Season, PreseasonStart);

        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            season.RegisterParticipant(new ClubId(club));
        }

        season.StartActiveSeason(PreseasonStart);
        return season;
    }

    [Fact]
    public void PlanLeagueFixtures_AddsFixturesAndRaisesDomainEvent()
    {
        var season = CreateActiveSeasonWithParticipants();

        season.PlanLeagueFixtures(FirstMatchday, new FixtureId(1));

        Assert.Equal(CompetitionMvpConstraints.TotalLeagueFixtures, season.Fixtures.Count);
        Assert.Contains(season.UncommittedEvents, e => e is LeagueFixturesPlanned planned
            && planned.FixtureCount == CompetitionMvpConstraints.TotalLeagueFixtures
            && planned.FirstMatchdayDate == FirstMatchday);
    }

    [Fact]
    public void PlanLeagueFixtures_RejectsWhenSeasonIsNotActive()
    {
        var season = CompetitionSeason.Create(Competition, Season, PreseasonStart);

        Assert.Throws<CompetitionInvariantViolationException>(() =>
            season.PlanLeagueFixtures(FirstMatchday, new FixtureId(1)));
    }

    [Fact]
    public void PlanLeagueFixtures_RejectsSecondPlanningAttempt()
    {
        var season = CreateActiveSeasonWithParticipants();
        season.PlanLeagueFixtures(FirstMatchday, new FixtureId(1));

        Assert.Throws<CompetitionInvariantViolationException>(() =>
            season.PlanLeagueFixtures(FirstMatchday.AddDays(7), new FixtureId(381)));
    }

    [Fact]
    public void Fixture_RejectsSameHomeAndAwayClub()
    {
        Assert.Throws<CompetitionInvariantViolationException>(() =>
            Fixture.Rehydrate(
                new FixtureId(1),
                Competition,
                Season,
                new ClubId(1),
                new ClubId(1),
                new FixtureRound(1),
                FirstMatchday,
                FixtureStatus.Planned));
    }
}
