using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Commands;
using FootballCareerSimulator.Application.TrainingPhysicalState.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.PlayerCareer;

public sealed class PlayerCareerDevelopmentTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-player-career",
        Guid.NewGuid().ToString("N"));

    public PlayerCareerDevelopmentTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ApplyDevelopmentGain_ConvertsPointsToAbilityUpToPotential()
    {
        var career = Domain.PlayerCareer.PlayerCareer.CreateForSlot(
            new ClubId(1),
            slotIndex: 0,
            currentAbility: 60,
            potentialAbility: 62);

        var grown = career.ApplyDevelopmentGain(25, Day);

        Assert.Equal(62, grown.CurrentAbility);
        Assert.True(grown.DevelopmentPoints < 10);
        Assert.Equal(Day, grown.LastDevelopedOn);
    }

    [Fact]
    public void WeeklyTraining_CreatesSquadAndRaisesDevelopmentPoints()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 42);
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var players = PlayerCareerModule.Create(manager.Store, world.TimelineStore, trainingStore);
        var training = TrainingPhysicalStateModule.Create(
            manager.Store,
            world.TimelineStore,
            trainingStore,
            players.Development);

        training.SetWeeklyPlan.Handle(
            new SetWeeklyTrainingPlanCommand(
                Guid.NewGuid(),
                (int)TrainingFocus.General,
                (int)TrainingIntensity.High,
                (int)RestApproach.Normal));

        Assert.Equal(25, players.Store.Careers.Count);
        Assert.Contains(players.Store.Careers, career => career.DevelopmentPoints > 0 || career.LastDevelopedOn is not null);
        Assert.True(players.Queries.GetManagedClubSummary().AverageCurrentAbility >= 40);
    }

    [Fact]
    public void SaveLoad_PreservesCurrentAbilityAndPoints()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 7);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var players = PlayerCareerModule.Create(manager.Store, world.TimelineStore, trainingStore);
        players.Development.EnsureClub(new ClubId(1), world.TimelineStore.Timeline.RootSeed);

        var first = players.Store.Get(new ClubId(1), 0)!;
        players.Store.Upsert(first.ApplyDevelopmentGain(12, Day));

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "players.db");
        persistence.Save(
            path,
            world.TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            clubs.Store.Registry,
            manager.Store.Career,
            Array.Empty<MatchSelection>(),
            Array.Empty<WeeklyTrainingPlan>(),
            Array.Empty<PlayerPhysicalState>(),
            players.Store.Careers);

        var loaded = persistence.Load(path);
        Assert.Equal(13, loaded.SchemaVersion);
        Assert.Equal(25, loaded.PlayerCareers.Count);
        var loadedFirst = loaded.PlayerCareers.Single(c => c.SlotIndex == 0);
        Assert.Equal(first.CurrentAbility + 1, loadedFirst.CurrentAbility);
        Assert.Equal(2, loadedFirst.DevelopmentPoints);
    }
}
