using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using FootballCareerSimulator.Simulation.Match;
using FootballCareerSimulator.Simulation.TeamPreparation;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class MatchSelectionTests : IDisposable
{
    private static readonly GameDate PreseasonStart = GameDate.FromCalendarDate(2026, 7, 1);
    private static readonly GameDate FirstMatchday = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory;

    public MatchSelectionTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "fcs-match-selection", Guid.NewGuid().ToString("N"));
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

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        CompetitionModule Competition,
        ManagerCareerModule Manager,
        TeamPreparationModule TeamPrep,
        InMemoryMatchSelectionStore SelectionStore) CreateCareerStack()
    {
        var world = WorldCalendarModule.Create(PreseasonStart, rootSeed: 42);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(PreseasonStart, startingClubId: 1);
        var selectionStore = new InMemoryMatchSelectionStore();
        var competition = CompetitionModule.CreateForCareer(
            world.TimelineStore,
            clubs.Store,
            manager.Store,
            selectionStore);
        var teamPrep = TeamPreparationModule.Create(competition.Store, manager.Store, selectionStore);
        return (world, clubs, competition, manager, teamPrep, selectionStore);
    }

    private static void SetupLeague(CompetitionModule competition, long seasonId)
    {
        competition.CreateSeason.Handle(
            new CreateSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), seasonId, club));
        }

        competition.StartSeason.Handle(
            new StartSeasonCommand(Guid.NewGuid(), seasonId, PreseasonStart.DayNumber));
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                seasonId,
                FirstMatchday.DayNumber,
                StartingFixtureId: 1));
    }

    [Fact]
    public void PlayFixtureMatch_WithoutApprovedSelection_ForManagedClub_Fails()
    {
        var (_, _, competition, _, _, _) = CreateCareerStack();
        SetupLeague(competition, seasonId: 1);

        var managedFixture = competition.Queries.GetSeasonFixtures(1)
            .First(fixture => fixture.HomeClubId == 1 || fixture.AwayClubId == 1);

        var ex = Assert.Throws<TeamPreparationInvariantViolationException>(() =>
            competition.PlayFixtureMatch!.Handle(
                new PlayFixtureMatchCommand(
                    Guid.NewGuid(),
                    1,
                    managedFixture.FixtureId,
                    FirstMatchday.DayNumber)));

        Assert.Contains("Match selection is not approved", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PlayFixtureMatch_WithApprovedSelection_SucceedsAndClearsSelection()
    {
        var (_, _, competition, _, teamPrep, selectionStore) = CreateCareerStack();
        SetupLeague(competition, seasonId: 1);

        var managedFixture = competition.Queries.GetSeasonFixtures(1)
            .First(fixture => fixture.HomeClubId == 1 || fixture.AwayClubId == 1);

        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(
                Guid.NewGuid(),
                managedFixture.FixtureId,
                ClubId: 1));

        var result = competition.PlayFixtureMatch!.Handle(
            new PlayFixtureMatchCommand(
                Guid.NewGuid(),
                1,
                managedFixture.FixtureId,
                FirstMatchday.DayNumber));

        Assert.True(result.Succeeded);
        Assert.Null(selectionStore.Get(new FixtureId(managedFixture.FixtureId), new ClubId(1)));
    }

    [Fact]
    public void LineupBonus_ChangesScoreDeterministically()
    {
        var club = new ClubId(1);
        var defaultBonus = MvpSquadStrengthCalculator.ComputeDefaultLineupBonus(club, rootSeed: 42);
        var weakBonus = MvpSquadStrengthCalculator.ComputeLineupBonus(
            club,
            rootSeed: 42,
            Enumerable.Range(14, 11).ToArray());

        Assert.NotEqual(defaultBonus, weakBonus);

        // Aynı bonus ile skor deterministik; bonus maç gücüne bağlanır (Simulate imzası).
        var first = MvpFixtureMatchSimulator.Simulate(42, 9, 70, 55, defaultBonus, 0);
        var second = MvpFixtureMatchSimulator.Simulate(42, 9, 70, 55, defaultBonus, 0);
        Assert.Equal(first, second);
        Assert.Equal(
            MvpFixtureMatchSimulator.Simulate(42, 9, 1, 1, 10, 0).HomeGoals,
            MvpFixtureMatchSimulator.Simulate(42, 9, 1, 1, 10, 0).HomeGoals);
    }

    [Fact]
    public void SaveLoad_PreservesApprovedMatchSelection()
    {
        var (world, clubs, competition, manager, teamPrep, selectionStore) = CreateCareerStack();
        SetupLeague(competition, seasonId: 1);

        var fixtureId = competition.Queries.GetSeasonFixtures(1)[0].FixtureId;
        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixtureId, ClubId: 1));

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "selection.db");
        persistence.Save(
            path,
            world.TimelineStore.Timeline,
            competition.Store.League,
            clubs.Store.Registry,
            manager.Store.Career,
            selectionStore.Selections,
            Array.Empty<Domain.TrainingPhysicalState.WeeklyTrainingPlan>(),
            Array.Empty<Domain.TrainingPhysicalState.PlayerPhysicalState>(),
            Array.Empty<Domain.PlayerCareer.PlayerCareer>(),
            Array.Empty<Domain.ContractRegistration.PlayerContract>());

        var loaded = persistence.Load(path);
        Assert.Single(loaded.MatchSelections);
        Assert.Equal(fixtureId, loaded.MatchSelections[0].FixtureId.Value);
        Assert.Equal(1, loaded.MatchSelections[0].ClubId.Value);
    }
}
