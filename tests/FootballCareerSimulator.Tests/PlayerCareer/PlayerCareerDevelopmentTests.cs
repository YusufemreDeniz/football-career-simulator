using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
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
    public void Retire_EligiblePlayerBecomesImmutableHistory()
    {
        var retirementDay = GameDate.FromCalendarDate(2035, 7, 1);
        var veteran = Domain.PlayerCareer.PlayerCareer.CreateForSlot(
            new ClubId(1), 3, 70, 74, birthYear: 2000);

        var retired = veteran.Retire(retirementDay);

        Assert.True(retired.IsRetired);
        Assert.Equal(CareerPhase.Retired, retired.GetPhase(retirementDay));
        Assert.Equal(retirementDay, retired.RetiredOn);
        Assert.Same(retired, retired.ApplyAnnualAging(retirementDay.AddDays(365)));
        Assert.Same(retired, retired.ApplyDevelopmentGain(20, retirementDay.AddDays(1)));
    }

    [Fact]
    public void GeneratedSuccessor_UsesNewIdentityWhileRetiredHistoryRemains()
    {
        var clubId = new ClubId(1);
        var day = GameDate.FromCalendarDate(2035, 7, 1);
        var retired = Domain.PlayerCareer.PlayerCareer.CreateForSlot(clubId, 3, 70, 74, 2000).Retire(day);
        var successor = Domain.PlayerCareer.PlayerCareer.CreateGeneratedForSlot(
            clubId, 3, 55, 72, 2018, generation: 1);
        var store = new InMemoryPlayerCareerStore();
        store.ReplaceAll([retired, successor]);

        Assert.NotEqual(retired.Id, successor.Id);
        Assert.Equal(successor.Id, store.Get(clubId, 3)!.Id);
        Assert.Contains(store.Careers, career => career.Id == retired.Id && career.IsRetired);
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
            Array.Empty<Domain.Transfer.TransferNeed>(),
            Array.Empty<Domain.Transfer.ShortlistEntry>(),
            Array.Empty<Domain.Transfer.TransferTarget>(),
            Array.Empty<Domain.Transfer.TransferProcess>(),
            Array.Empty<Domain.Transfer.ClubOffer>(),
            Array.Empty<Domain.Transfer.PlayerContractProposal>(),
            Array.Empty<Domain.SocialContinuity.Promise>(),
            Array.Empty<Domain.SocialContinuity.MemoryRecord>());

        var loaded = persistence.Load(path);
        Assert.Equal(42, loaded.SchemaVersion);
        Assert.Equal(25, loaded.PlayerCareers.Count);
        var loadedFirst = loaded.PlayerCareers.Single(c => c.SlotIndex == 0);
        Assert.Equal(aged.BirthYear, loadedFirst.BirthYear);
        Assert.Equal(aged.CurrentAbility, loadedFirst.CurrentAbility);
        Assert.Equal(aged.LastAgedCalendarYear, loadedFirst.LastAgedCalendarYear);
    }
}
