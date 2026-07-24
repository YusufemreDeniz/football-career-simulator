using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
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
using FootballCareerSimulator.Simulation.ContractRegistration;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.ContractRegistration;

public sealed class FreeAgencyTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-free-agency",
        Guid.NewGuid().ToString("N"));

    public FreeAgencyTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Expire_RegistersFreeAgentAndRemovesFromSquad()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 9);
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
        var competition = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        var teamPrep = TeamPreparationModule.Create(
            competition.Store,
            manager.Store,
            contractStore: contracts.Store,
            playerCareerStore: players.Store);

        players.Development.EnsureClub(new ClubId(1), world.TimelineStore.Timeline.RootSeed, Day);
        teamPrep.ClubSquad!.SyncFromActiveContracts(new ClubId(1), Day);
        Assert.Equal(25, teamPrep.SquadStore.Get(new ClubId(1))!.Members.Count);

        var shortPlayer = PlayerId.FromClubSlot(1, MvpContractFactory.ShortContractSquadSlot);
        var shortContract = contracts.Store.GetByPlayer(shortPlayer)!;
        Assert.Equal(Day.AddDays(45), shortContract.EndDate);

        var afterEnd = Day.AddDays(46);
        var expiry = contracts.Registration.ExpireDueContracts(afterEnd);
        teamPrep.ClubSquad.SyncClubs(expiry.AffectedClubIds, afterEnd);

        Assert.Equal(1, expiry.ExpiredCount);
        Assert.Contains(shortPlayer.Value, expiry.FreeAgentPlayerIds);
        Assert.True(contracts.Registration.IsFreeAgent(shortPlayer));
        Assert.Null(contracts.Registration.GetActiveClub(shortPlayer, afterEnd));
        Assert.Equal(24, teamPrep.SquadStore.Get(new ClubId(1))!.Members.Count);
        Assert.False(teamPrep.SquadStore.Get(new ClubId(1))!.ContainsPlayer(shortPlayer));
        Assert.Equal(1, contracts.Queries.GetManagedClubSummary().FreeAgentReleasedCount);
    }

    [Fact]
    public void EnsureClubContracts_DoesNotReSignFreeAgent()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 2);
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

        var playerId = PlayerId.FromClubSlot(1, 0);
        players.Store.Upsert(Domain.PlayerCareer.PlayerCareer.CreateForSlot(
            new ClubId(1),
            0,
            50,
            70,
            2000));
        contracts.Store.Upsert(PlayerContract.Activate(
            playerId,
            new ClubId(1),
            Day,
            Day.AddDays(1),
            700));
        contracts.Registration.ExpireDueContracts(Day.AddDays(2));
        Assert.True(contracts.Registration.IsFreeAgent(playerId));

        contracts.Registration.EnsureClubContracts(new ClubId(1), Day.AddDays(3));
        Assert.True(contracts.Registration.IsFreeAgent(playerId));
        Assert.Equal(ContractStatus.Expired, contracts.Store.GetByPlayer(playerId)!.Status);
        Assert.Null(contracts.Registration.GetActiveClub(playerId, Day.AddDays(3)));
    }

    [Fact]
    public void SaveLoad_PreservesFreeAgencyAtSchemaV17()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 4);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var players = PlayerCareerModule.Create(manager.Store, world.TimelineStore, trainingStore);
        var contracts = ContractRegistrationModule.Create(
            players.Store,
            manager.Store,
            world.TimelineStore);

        var playerId = PlayerId.FromClubSlot(1, 3);
        players.Store.Upsert(Domain.PlayerCareer.PlayerCareer.CreateForSlot(
            new ClubId(1),
            3,
            60,
            75,
            1999));
        contracts.Store.Upsert(PlayerContract.Activate(
            playerId,
            new ClubId(1),
            Day,
            Day.AddDays(2),
            800));
        contracts.Registration.ExpireDueContracts(Day.AddDays(3));

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "free-agency.db");
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
            contracts.FreeAgentStore.FreeAgents,
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
        Assert.Equal(35, loaded.SchemaVersion);
        Assert.Single(loaded.FreeAgents);
        Assert.Equal(playerId, loaded.FreeAgents[0].PlayerId);
        Assert.Equal(1, loaded.FreeAgents[0].LastClubId.Value);
    }
}
