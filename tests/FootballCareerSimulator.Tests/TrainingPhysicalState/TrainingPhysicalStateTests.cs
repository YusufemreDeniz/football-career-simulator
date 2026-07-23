using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TrainingPhysicalState.Commands;
using FootballCareerSimulator.Application.TrainingPhysicalState.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Infrastructure.Career;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.TrainingPhysicalState;

public sealed class TrainingPhysicalStateTests : IDisposable
{
    private static readonly GameDate PreseasonStart = GameDate.FromCalendarDate(2026, 7, 1);
    private static readonly GameDate FirstMatchday = GameDate.FromCalendarDate(2026, 8, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-training",
        Guid.NewGuid().ToString("N"));

    public TrainingPhysicalStateTests() => Directory.CreateDirectory(_tempDirectory);

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
        TrainingPhysicalStateModule Training) CreateStack()
    {
        var world = WorldCalendarModule.Create(PreseasonStart, rootSeed: 42);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(PreseasonStart, startingClubId: 1);
        var selectionStore = new InMemoryMatchSelectionStore();
        var training = TrainingPhysicalStateModule.Create(manager.Store, world.TimelineStore);
        var competition = CompetitionModule.CreateForCareer(
            world.TimelineStore,
            clubs.Store,
            manager.Store,
            selectionStore,
            training.Store);
        var teamPrep = TeamPreparationModule.Create(
            competition.Store,
            manager.Store,
            selectionStore,
            training.Store,
            world.TimelineStore);
        return (world, clubs, competition, manager, teamPrep, training);
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
    public void SetWeeklyTrainingPlan_AppliesFatigueToSquad()
    {
        var (_, _, _, _, _, training) = CreateStack();

        var result = training.SetWeeklyPlan.Handle(
            new SetWeeklyTrainingPlanCommand(
                Guid.NewGuid(),
                (int)TrainingFocus.General,
                (int)TrainingIntensity.High,
                (int)RestApproach.Light));

        Assert.True(result.Succeeded);
        Assert.True(result.AverageFatigue > PlayerPhysicalState.DefaultFatigue);
        Assert.NotNull(training.Store.GetPlan(new Domain.Shared.ClubId(1)));
        Assert.Equal(25, training.Store.PhysicalStates.Count);
    }

    [Fact]
    public void HighFatigueLineup_ProducesNegativeMatchModifier()
    {
        var (_, _, _, _, _, training) = CreateStack();
        training.SetWeeklyPlan.Handle(
            new SetWeeklyTrainingPlanCommand(
                Guid.NewGuid(),
                (int)TrainingFocus.General,
                (int)TrainingIntensity.High,
                (int)RestApproach.Light));

        var clubId = new ClubId(1);
        var modifier = MvpPhysicalMatchModifier.ComputeLineupModifier(
            clubId,
            Enumerable.Range(0, MatchSelection.StartingXiSize).ToArray(),
            training.Store.PhysicalBySlot);

        Assert.True(modifier < 0, $"Expected negative modifier for high fatigue, got {modifier}.");
    }

    [Fact]
    public void PlayFixtureMatch_WithTraining_IsDeterministic()
    {
        long PlayOnce()
        {
            var (_, _, competition, _, teamPrep, training) = CreateStack();
            SetupLeague(competition, 1);
            var fixture = competition.Queries.GetSeasonFixtures(1)
                .First(f => f.HomeClubId == 1 || f.AwayClubId == 1);
            teamPrep.ApproveDefaultSelection.Handle(
                new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixture.FixtureId, 1));
            training.SetWeeklyPlan.Handle(
                new SetWeeklyTrainingPlanCommand(
                    Guid.NewGuid(),
                    (int)TrainingFocus.General,
                    (int)TrainingIntensity.Medium,
                    (int)RestApproach.Normal));
            var result = competition.PlayFixtureMatch!.Handle(
                new PlayFixtureMatchCommand(Guid.NewGuid(), 1, fixture.FixtureId, FirstMatchday.DayNumber));
            return (result.HomeGoals * 100L) + result.AwayGoals;
        }

        Assert.Equal(PlayOnce(), PlayOnce());
    }

    [Fact]
    public void SaveLoad_PreservesTrainingPlanAndPhysicalState()
    {
        var (world, clubs, _, manager, _, training) = CreateStack();
        training.SetWeeklyPlan.Handle(
            new SetWeeklyTrainingPlanCommand(
                Guid.NewGuid(),
                (int)TrainingFocus.Fitness,
                (int)TrainingIntensity.Medium,
                (int)RestApproach.Normal));

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "training.db");
        persistence.Save(
            path,
            world.TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            clubs.Store.Registry,
            manager.Store.Career,
            Array.Empty<Domain.TeamPreparation.MatchSelection>(),
            training.Store.Plans,
            training.Store.PhysicalStates,
            Array.Empty<Domain.PlayerCareer.PlayerCareer>(),
            Array.Empty<Domain.ContractRegistration.PlayerContract>(),
            Array.Empty<Domain.TeamPreparation.ClubSquad>(),
            Array.Empty<Domain.ContractRegistration.PlayerFreeAgency>(),
            Array.Empty<Domain.TeamPreparation.TacticPlan>(),
            Array.Empty<Domain.Transfer.TransferNeed>());

        var loaded = persistence.Load(path);
        Assert.Equal(19, loaded.SchemaVersion);
        Assert.Single(loaded.TrainingPlans);
        Assert.Equal(TrainingIntensity.Medium, loaded.TrainingPlans[0].Intensity);
        Assert.Equal(25, loaded.PhysicalStates.Count);
        Assert.Equal(
            training.Store.PhysicalStates[0].Fatigue,
            loaded.PhysicalStates[0].Fatigue);
    }
}
