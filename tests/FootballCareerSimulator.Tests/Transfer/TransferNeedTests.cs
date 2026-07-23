using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class TransferNeedTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-transfer-need",
        Guid.NewGuid().ToString("N"));

    public TransferNeedTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Declare_CreatesOpenPositionGapNeed()
    {
        var modules = CreateModules();
        var need = modules.Transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.PositionGap,
            priority: 4,
            "ManualPositionGap",
            Day);

        Assert.True(need.IsOpen);
        Assert.Equal(TransferNeedKind.PositionGap, need.Kind);
        Assert.Equal(1, modules.Transfer.Queries.GetManagedClubNeeds().OpenCount);
        Assert.Equal("Pozisyon açığı", modules.Transfer.Queries.GetManagedClubNeeds().OpenNeeds[0].KindName);
    }

    [Fact]
    public void Declare_SameKind_IsIdempotent()
    {
        var modules = CreateModules();
        var first = modules.Transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.PositionGap,
            priority: 4,
            "ManualPositionGap",
            Day);
        var second = modules.Transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.PositionGap,
            priority: 5,
            "ManualPositionGap",
            Day.AddDays(1));

        Assert.Equal(first.NeedId, second.NeedId);
        Assert.Single(modules.Transfer.NeedStore.Needs);
    }

    [Fact]
    public void RefreshSuggestions_CreatesSquadDepthWhenThin()
    {
        var modules = CreateModules();
        var clubId = new ClubId(1);
        var thinMembers = Enumerable.Range(0, 11)
            .Select(slot => SquadMember.Create(PlayerId.FromClubSlot(1, slot), slot, Day))
            .ToArray();
        modules.TeamPrep.SquadStore.Upsert(ClubSquad.Rehydrate(clubId, thinMembers));

        var suggested = modules.Transfer.Needs.RefreshSuggestions(clubId, Day);
        Assert.Contains(suggested, n => n.Kind == TransferNeedKind.SquadDepth);
        Assert.Equal("ThinSquad", suggested.Single(n => n.Kind == TransferNeedKind.SquadDepth).ReasonCode);
    }

    [Fact]
    public void Close_MarksNeedClosed()
    {
        var modules = CreateModules();
        var need = modules.Transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.TacticalRequirement,
            priority: 2,
            "ManualTactical",
            Day);

        var closed = modules.Transfer.Needs.Close(need.NeedId, Day.AddDays(2));
        Assert.Equal(TransferNeedStatus.Closed, closed.Status);
        Assert.Equal(0, modules.Transfer.Queries.GetManagedClubNeeds().OpenCount);
    }

    [Fact]
    public void SaveLoad_PreservesTransferNeedAtSchemaV19()
    {
        var modules = CreateModules();
        modules.Transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.ExpiringContract,
            priority: 3,
            "ExpiringContracts",
            Day);

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "needs.db");
        persistence.Save(
            path,
            modules.World.TimelineStore.Timeline,
            new LeagueCompetition(new CompetitionId(1)),
            modules.Clubs.Store.Registry,
            modules.Manager.Store.Career,
            Array.Empty<MatchSelection>(),
            Array.Empty<WeeklyTrainingPlan>(),
            Array.Empty<PlayerPhysicalState>(),
            Array.Empty<Domain.PlayerCareer.PlayerCareer>(),
            Array.Empty<Domain.ContractRegistration.PlayerContract>(),
            Array.Empty<ClubSquad>(),
            Array.Empty<Domain.ContractRegistration.PlayerFreeAgency>(),
            Array.Empty<TacticPlan>(),
            modules.Transfer.NeedStore.Needs,
            Array.Empty<ShortlistEntry>(),
            Array.Empty<TransferTarget>(),
            Array.Empty<TransferProcess>(),
            Array.Empty<ClubOffer>());

        var loaded = persistence.Load(path);
        Assert.Equal(23, loaded.SchemaVersion);
        Assert.Single(loaded.TransferNeeds);
        Assert.Equal(TransferNeedKind.ExpiringContract, loaded.TransferNeeds[0].Kind);
        Assert.Equal(TransferNeedStatus.Open, loaded.TransferNeeds[0].Status);
        Assert.Equal("ExpiringContracts", loaded.TransferNeeds[0].ReasonCode);
    }

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        ManagerCareerModule Manager,
        TeamPreparationModule TeamPrep,
        TransferModule Transfer) CreateModules()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 11);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var competition = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var playerStore = new InMemoryPlayerCareerStore();
        var contracts = ContractRegistrationModule.Create(
            playerStore,
            manager.Store,
            world.TimelineStore);
        _ = PlayerCareerModule.Create(
            manager.Store,
            world.TimelineStore,
            trainingStore,
            playerStore,
            contracts.Registration);
        var teamPrep = TeamPreparationModule.Create(
            competition.Store,
            manager.Store,
            trainingStore: trainingStore,
            timelineStore: world.TimelineStore,
            contractStore: contracts.Store,
            playerCareerStore: playerStore);
        var transfer = TransferModule.Create(contracts.Store, teamPrep.SquadStore, manager.Store);
        return (world, clubs, manager, teamPrep, transfer);
    }
}
