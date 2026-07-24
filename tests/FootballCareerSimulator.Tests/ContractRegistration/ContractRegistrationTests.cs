using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.ContractRegistration;

public sealed class ContractRegistrationTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-contracts",
        Guid.NewGuid().ToString("N"));

    public ContractRegistrationTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void EnsureClub_CreatesOneActiveContractPerPlayer()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 11);
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var players = PlayerCareerModule.Create(manager.Store, world.TimelineStore, trainingStore);
        var contracts = ContractRegistrationModule.Create(
            players.Store,
            manager.Store,
            world.TimelineStore);
        players = PlayerCareerModule.Create(
            manager.Store,
            world.TimelineStore,
            trainingStore,
            players.Store,
            contracts.Registration);

        players.Development.EnsureClub(new ClubId(1), world.TimelineStore.Timeline.RootSeed, Day);

        Assert.Equal(25, players.Store.Careers.Count);
        Assert.Equal(25, contracts.Store.Contracts.Count);
        Assert.All(contracts.Store.Contracts, c => Assert.Equal(ContractStatus.Active, c.Status));
        Assert.Equal(25, contracts.Queries.GetManagedClubSummary().ActiveCount);
        Assert.True(contracts.Queries.GetManagedClubSummary().AverageWeeklyWage >= 500);
    }

    [Fact]
    public void Upsert_RejectsSecondActiveContractForSamePlayer()
    {
        var store = new Application.ContractRegistration.Infrastructure.InMemoryContractStore();
        var playerId = PlayerId.FromClubSlot(1, 0);
        var first = PlayerContract.Activate(
            playerId,
            new ClubId(1),
            Day,
            GameDate.FromCalendarDate(2028, 7, 1),
            weeklyWage: 1000);
        store.Upsert(first);

        var second = PlayerContract.Rehydrate(
            new ContractId(9_999),
            playerId,
            new ClubId(2),
            Day,
            GameDate.FromCalendarDate(2029, 7, 1),
            weeklyWage: 2000,
            ContractStatus.Active);

        Assert.Throws<ContractRegistrationInvariantViolationException>(() => store.Upsert(second));
    }

    [Fact]
    public void ExpireDueContracts_MarksPastEndDateAsExpired()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 3);
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var players = PlayerCareerModule.Create(manager.Store, world.TimelineStore, trainingStore);
        var contracts = ContractRegistrationModule.Create(
            players.Store,
            manager.Store,
            world.TimelineStore);

        var playerId = PlayerId.FromClubSlot(1, 0);
        players.Store.Upsert(Domain.PlayerCareer.PlayerCareer.CreateForSlot(
            new ClubId(1),
            slotIndex: 0,
            currentAbility: 50,
            potentialAbility: 70,
            birthYear: 2000));
        contracts.Store.Upsert(PlayerContract.Activate(
            playerId,
            new ClubId(1),
            Day,
            GameDate.FromCalendarDate(2026, 7, 5),
            weeklyWage: 800));

        var afterEnd = GameDate.FromCalendarDate(2026, 7, 6);
        var expired = contracts.Registration.ExpireDueContracts(afterEnd);

        Assert.Equal(1, expired.ExpiredCount);
        Assert.Equal(ContractStatus.Expired, contracts.Store.GetByPlayer(playerId)!.Status);
        Assert.False(contracts.Store.GetByPlayer(playerId)!.IsActiveOn(afterEnd));
        Assert.True(contracts.Registration.IsFreeAgent(playerId));
        Assert.Null(contracts.Registration.GetActiveClub(playerId, afterEnd));
        Assert.Equal(1, contracts.Queries.GetManagedClubSummary().FreeAgentReleasedCount);
    }

    [Fact]
    public void SaveLoad_PreservesContractsAtSchemaV15()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 7);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var players = PlayerCareerModule.Create(manager.Store, world.TimelineStore, trainingStore);
        var contracts = ContractRegistrationModule.Create(
            players.Store,
            manager.Store,
            world.TimelineStore);
        players = PlayerCareerModule.Create(
            manager.Store,
            world.TimelineStore,
            trainingStore,
            players.Store,
            contracts.Registration);

        players.Development.EnsureClub(new ClubId(1), world.TimelineStore.Timeline.RootSeed, Day);

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "contracts.db");
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
            contracts.Store.Contracts,
            Array.Empty<ClubSquad>(),
            Array.Empty<PlayerFreeAgency>(),
            Array.Empty<TacticPlan>(),
            Array.Empty<Domain.Transfer.TransferNeed>(),
            Array.Empty<Domain.Transfer.ShortlistEntry>(),
            Array.Empty<Domain.Transfer.TransferTarget>(),
            Array.Empty<Domain.Transfer.TransferProcess>(),
            Array.Empty<Domain.Transfer.ClubOffer>(),
            Array.Empty<Domain.Transfer.PlayerContractProposal>(),
            Array.Empty<Domain.SocialContinuity.Promise>(),
            Array.Empty<Domain.SocialContinuity.MemoryRecord>());

        var loaded = persistence.Load(path);
        Assert.Equal(32, loaded.SchemaVersion);
        Assert.Equal(25, loaded.Contracts.Count);
        Assert.All(loaded.Contracts, c => Assert.Equal(ContractStatus.Active, c.Status));

        var sample = contracts.Store.Contracts.OrderBy(c => c.Id.Value).First();
        var loadedSample = loaded.Contracts.Single(c => c.Id == sample.Id);
        Assert.Equal(sample.PlayerId, loadedSample.PlayerId);
        Assert.Equal(sample.ClubId, loadedSample.ClubId);
        Assert.Equal(sample.WeeklyWage, loadedSample.WeeklyWage);
        Assert.Equal(sample.EndDate, loadedSample.EndDate);
    }
}
