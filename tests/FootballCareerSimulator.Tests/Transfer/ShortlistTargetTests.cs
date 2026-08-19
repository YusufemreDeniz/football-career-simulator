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

public sealed class ShortlistTargetTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-shortlist-target",
        Guid.NewGuid().ToString("N"));

    public ShortlistTargetTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SuggestAndList_CreatesShortlistAndTargetWithoutProcess()
    {
        var modules = CreateModules();
        modules.Transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.PositionGap,
            priority: 4,
            "ManualPositionGap",
            Day);

        var target = modules.Transfer.ShortlistTargets.SuggestAndListTargetForOldestOpenNeed(
            new ClubId(1),
            Day);

        Assert.True(target.IsListed);
        Assert.Equal(PlayerId.FromClubSlot(2, 0), target.PlayerId);
        Assert.Equal(1, modules.Transfer.Queries.GetManagedClubShortlistTargets().ActiveShortlistCount);
        Assert.Equal(1, modules.Transfer.Queries.GetManagedClubShortlistTargets().ListedTargetCount);
    }

    [Fact]
    public void AddTransferTarget_RequiresOpenNeed()
    {
        var modules = CreateModules();
        Assert.Throws<TransferInvariantViolationException>(() =>
            modules.Transfer.ShortlistTargets.AddTransferTarget(
                new TransferNeedId(99),
                PlayerId.FromClubSlot(2, 0),
                shortlistEntryId: null,
                Day));
    }

    [Fact]
    public void Drop_RemovesFromListedQuery()
    {
        var modules = CreateModules();
        modules.Transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.SquadDepth,
            priority: 2,
            "ThinSquad",
            Day);
        var target = modules.Transfer.ShortlistTargets.SuggestAndListTargetForOldestOpenNeed(
            new ClubId(1),
            Day);

        modules.Transfer.ShortlistTargets.DropTransferTarget(target.TargetId, Day.AddDays(1));
        Assert.Equal(0, modules.Transfer.Queries.GetManagedClubShortlistTargets().ListedTargetCount);
    }

    [Fact]
    public void SaveLoad_PreservesShortlistAndTargetAtSchemaV20()
    {
        var modules = CreateModules();
        modules.Transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.PositionGap,
            priority: 3,
            "ManualPositionGap",
            Day);
        modules.Transfer.ShortlistTargets.SuggestAndListTargetForOldestOpenNeed(new ClubId(1), Day);

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "targets.db");
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
            modules.Transfer.ShortlistStore.Entries,
            modules.Transfer.TargetStore.Targets,
            modules.Transfer.ProcessStore.Processes,
            modules.Transfer.OfferStore.Offers,
            modules.Transfer.ProposalStore.Proposals,
            Array.Empty<Domain.SocialContinuity.Promise>(),
            Array.Empty<Domain.SocialContinuity.MemoryRecord>());

        var loaded = persistence.Load(path);
        Assert.Equal(43, loaded.SchemaVersion);
        Assert.Single(loaded.ShortlistEntries);
        Assert.Single(loaded.TransferTargets);
        Assert.Equal(TransferTargetStatus.Listed, loaded.TransferTargets[0].Status);
        Assert.NotNull(loaded.TransferTargets[0].ShortlistEntryId);
    }

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        ManagerCareerModule Manager,
        TransferModule Transfer) CreateModules()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 21);
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
        var transfer = TransferModule.Create(contracts.Store, teamPrep.SquadStore, manager.Store, contracts.Registration, teamPrep.ClubSquad!, transferBudget: clubs.TransferBudget);
        return (world, clubs, manager, transfer);
    }
}
