using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Competition;

public class CompetitionApplicationHandlerTests
{
    private static readonly GameDate PreseasonStart = GameDate.FromCalendarDate(2026, 7, 1);
    private static readonly GameDate FirstMatchday = GameDate.FromCalendarDate(2026, 8, 1);

    private static CompetitionModule CreateModule() => CompetitionModule.CreateNewLeague();

    private static void RegisterFullLeague(CompetitionModule module, long seasonId)
    {
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            module.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), seasonId, club));
        }
    }

    [Fact]
    public void CreateSeason_StartAndPlanFixtures_UpdatesQueries()
    {
        var module = CreateModule();
        const long seasonId = 1;

        module.CreateSeason.Handle(
            new CreateSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));

        RegisterFullLeague(module, seasonId);

        module.StartSeason.Handle(
            new StartSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));

        module.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                seasonId,
                FirstMatchday.DayNumber,
                StartingFixtureId: 1));

        var season = module.Queries.GetCurrentSeason();
        Assert.NotNull(season);
        Assert.Equal(nameof(SeasonStatus.Active), season.Status);
        Assert.Equal(CompetitionMvpConstraints.LeagueTeamCount, season.ParticipantCount);
        Assert.Equal(CompetitionMvpConstraints.TotalLeagueFixtures, season.FixtureCount);

        var roundOne = module.Queries.GetFixturesByRound(seasonId, round: 1);
        Assert.Equal(CompetitionMvpConstraints.LeagueFixturesPerRound, roundOne.Count);
        Assert.All(roundOne, fixture => Assert.Equal(FirstMatchday.DayNumber, fixture.ScheduledDayNumber));
    }

    [Fact]
    public void PlanLeagueFixtures_SameCommandId_IsIdempotent()
    {
        var module = CreateModule();
        const long seasonId = 1;
        var commandId = Guid.NewGuid();

        module.CreateSeason.Handle(
            new CreateSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        RegisterFullLeague(module, seasonId);
        module.StartSeason.Handle(
            new StartSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));

        var command = new PlanLeagueFixturesCommand(
            commandId,
            seasonId,
            FirstMatchday.DayNumber,
            StartingFixtureId: 1);

        var first = module.PlanLeagueFixtures.Handle(command);
        var second = module.PlanLeagueFixtures.Handle(command);

        Assert.Equal(first, second);
        Assert.Equal(
            CompetitionMvpConstraints.TotalLeagueFixtures,
            module.Queries.GetSeasonFixtures(seasonId).Count);
    }

    [Fact]
    public void GetSeasonFixtures_ReturnsPrimitiveReadModelFields()
    {
        var module = CreateModule();
        const long seasonId = 1;

        module.CreateSeason.Handle(
            new CreateSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        RegisterFullLeague(module, seasonId);
        module.StartSeason.Handle(
            new StartSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        module.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                seasonId,
                FirstMatchday.DayNumber,
                StartingFixtureId: 1));

        var fixture = module.Queries.GetSeasonFixtures(seasonId)[0];

        Assert.True(fixture.FixtureId > 0);
        Assert.Equal(seasonId, fixture.SeasonId);
        Assert.True(fixture.HomeClubId > 0);
        Assert.True(fixture.AwayClubId > 0);
        Assert.InRange(fixture.Round, 1, CompetitionMvpConstraints.MaxLeagueFixtureRound);
        Assert.False(string.IsNullOrWhiteSpace(fixture.ScheduledIsoDate));
        Assert.Equal(nameof(FixtureStatus.Planned), fixture.Status);
    }
}
