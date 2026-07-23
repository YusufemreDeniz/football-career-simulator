using FootballCareerSimulator.Application.Career.Services;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.Career;

public sealed class CareerGameSessionServiceTests : IDisposable
{
    private static readonly GameDate PreseasonStart = GameDate.FromCalendarDate(2026, 7, 1);
    private static readonly GameDate FirstMatchday = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory;
    private readonly CareerSqlitePersistence _persistence = new();

    public CareerGameSessionServiceTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "fcs-career-session-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private CareerGameSessionService CreateSession(
        WorldCalendarModule world,
        CompetitionModule competition,
        ClubGovernanceModule clubs,
        ManagerCareerModule manager,
        TeamPreparationModule teamPreparation,
        TrainingPhysicalStateModule training,
        PlayerCareerModule playerCareer,
        ContractRegistrationModule contracts)
    {
        var idempotencyResets = new List<ICommandIdempotencyReset>
        {
            world.AdvanceSimulationTime,
            world.OpenPlanningPeriod,
            world.CompletePlanningPeriod,
        };
        idempotencyResets.AddRange(competition.IdempotencyResets);
        idempotencyResets.Add(teamPreparation.IdempotencyReset);
        idempotencyResets.Add(training.IdempotencyReset);

        return new CareerGameSessionService(
            world.TimelineStore,
            competition.Store,
            clubs.Store,
            manager.Store,
            teamPreparation.SelectionStore,
            teamPreparation.SquadStore,
            teamPreparation.TacticPlanStore,
            training.Store,
            playerCareer.Store,
            contracts.Store,
            contracts.FreeAgentStore,
            _persistence,
            idempotencyResets);
    }

    private static void RegisterFullLeague(CompetitionModule module, long seasonId)
    {
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            module.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), seasonId, club));
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_RestoresTimelineAndFixturesInStores()
    {
        var world = WorldCalendarModule.Create(PreseasonStart, rootSeed: 7);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(PreseasonStart);
        var competition = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        var teamPreparation = TeamPreparationModule.Create(competition.Store, manager.Store);
        var training = TrainingPhysicalStateModule.Create(manager.Store, world.TimelineStore);
        var playerCareer = PlayerCareerModule.Create(manager.Store, world.TimelineStore, training.Store);
        var contracts = ContractRegistrationModule.Create(
            playerCareer.Store,
            manager.Store,
            world.TimelineStore);
        var session = CreateSession(
            world, competition, clubs, manager, teamPreparation, training, playerCareer, contracts);
        const long seasonId = 1;

        world.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(
                Guid.NewGuid(),
                GameDate.FromCalendarDate(2026, 7, 20).DayNumber));

        competition.CreateSeason.Handle(
            new CreateSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        RegisterFullLeague(competition, seasonId);
        competition.StartSeason.Handle(
            new StartSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                seasonId,
                FirstMatchday.DayNumber,
                StartingFixtureId: 1));

        var path = Path.Combine(_tempDirectory, "career-session.db");
        var saveResult = session.Save(path);

        Assert.True(saveResult.Succeeded);
        Assert.Equal(GameDate.FromCalendarDate(2026, 7, 20).DayNumber, saveResult.SavedDayNumber);
        Assert.Equal(CompetitionMvpConstraints.TotalLeagueFixtures, saveResult.SavedFixtureCount);

        world.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(
                Guid.NewGuid(),
                GameDate.FromCalendarDate(2026, 8, 1).DayNumber));

        var loadResult = session.Load(path);

        Assert.True(loadResult.Succeeded);
        Assert.Equal(GameDate.FromCalendarDate(2026, 7, 20).DayNumber, loadResult.LoadedDayNumber);
        Assert.Equal(CompetitionMvpConstraints.TotalLeagueFixtures, loadResult.LoadedFixtureCount);
        Assert.Equal(
            GameDate.FromCalendarDate(2026, 7, 20).DayNumber,
            world.Queries.GetCurrentGameDate().DayNumber);
        Assert.Equal(
            CompetitionMvpConstraints.TotalLeagueFixtures,
            competition.Queries.GetSeasonFixtures(seasonId).Count);
    }
}
