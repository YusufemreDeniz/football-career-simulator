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
            potentialAbility: 62,
            birthYear: 2002);

        var grown = career.ApplyDevelopmentGain(25, Day);

        Assert.Equal(62, grown.CurrentAbility);
        Assert.True(grown.DevelopmentPoints < 10);
        Assert.Equal(Day, grown.LastDevelopedOn);
    }

    [Fact]
    public void AnnualAging_DeclinesAbilityForOlderPlayers()
    {
        var veteran = Domain.PlayerCareer.PlayerCareer.CreateForSlot(
            new ClubId(1),
            slotIndex: 3,
            currentAbility: 70,
            potentialAbility: 72,
            birthYear: 1994);

        Assert.Equal(CareerPhase.Declining, veteran.GetPhase(Day));
        var aged = veteran.ApplyAnnualAging(Day);
        Assert.Equal(69, aged.CurrentAbility);
        Assert.Equal(2026, aged.LastAgedCalendarYear);
        Assert.Equal(aged, aged.ApplyAnnualAging(Day));
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
        Assert.True(players.Queries.GetManagedClubSummary().AverageAge >= 18);
    }

    [Fact]
    public void SaveLoad_PreservesBirthYearAndAging()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 7);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var players = PlayerCareerModule.Create(manager.Store, world.TimelineStore, trainingStore);
        players.Development.EnsureClub(new ClubId(1), world.TimelineStore.Timeline.RootSeed, Day);

        var first = players.Store.Get(new ClubId(1), 0)!;
        var aged = first.ApplyDevelopmentGain(12, Day).ApplyAnnualAging(Day);
        players.Store.Upsert(aged);

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
            players.Store.Careers,
            Array.Empty<Domain.ContractRegistration.PlayerContract>(),
            Array.Empty<Domain.TeamPreparation.ClubSquad>(),
            Array.Empty<Domain.ContractRegistration.PlayerFreeAgency>(),
            Array.Empty<Domain.TeamPreparation.TacticPlan>(),
            Array.Empty<Domain.Transfer.TransferNeed>());

        var loaded = persistence.Load(path);
        Assert.Equal(19, loaded.SchemaVersion);
        Assert.Equal(25, loaded.PlayerCareers.Count);
        var loadedFirst = loaded.PlayerCareers.Single(c => c.SlotIndex == 0);
        Assert.Equal(aged.BirthYear, loadedFirst.BirthYear);
        Assert.Equal(aged.CurrentAbility, loadedFirst.CurrentAbility);
        Assert.Equal(aged.LastAgedCalendarYear, loadedFirst.LastAgedCalendarYear);
    }
}
