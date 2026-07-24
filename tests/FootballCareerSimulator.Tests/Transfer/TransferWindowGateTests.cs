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
using FootballCareerSimulator.Application.WorldCalendar.Infrastructure;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class TransferWindowGateTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-transfer-window",
        Guid.NewGuid().ToString("N"));

    public TransferWindowGateTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ClosedWindow_BlocksOpenProcessAndOffers()
    {
        var modules = CreateModulesWithListedTarget();
        modules.World.TimelineStore.Timeline.CloseTransferWindow();

        Assert.Throws<TransferInvariantViolationException>(() =>
            modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day));

        modules.World.TimelineStore.Timeline.OpenTransferWindow();
        var process = modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
        modules.Transfer.Processes.RequestSportingApproval(process.ProcessId);
        modules.Transfer.Processes.GrantSportingApproval(process.ProcessId);

        modules.World.TimelineStore.Timeline.CloseTransferWindow();
        Assert.Throws<TransferInvariantViolationException>(() =>
            modules.Transfer.ClubOffers.SubmitClubOffer(process.ProcessId, 1_000_000, Day));
    }

    [Fact]
    public void ClosedWindow_AllowsCompletionPendingToFinish()
    {
        var modules = CreateModulesWithFinancialApproval();
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;

        // Start completion while open, then close window and finish.
        var process = modules.Transfer.ProcessStore.Get(processId)!;
        modules.Transfer.ProcessStore.Upsert(process.StartCompletion());
        modules.World.TimelineStore.Timeline.CloseTransferWindow();

        var completed = modules.Transfer.Completion.Complete(processId, Day);
        Assert.Equal(TransferProcessStatus.Archived, completed.Status);
    }

    [Fact]
    public void ClosedWindow_BlocksStartingCompletionFromFinancialApproved()
    {
        var modules = CreateModulesWithFinancialApproval();
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;
        modules.World.TimelineStore.Timeline.CloseTransferWindow();

        Assert.Throws<TransferInvariantViolationException>(() =>
            modules.Transfer.Completion.Complete(processId, Day));
    }

    [Fact]
    public void SaveLoad_PreservesClosedWindowAtSchemaV27()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 91);
        world.TimelineStore.Timeline.CloseTransferWindow();

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "window.db");
        persistence.Save(
            path,
            world.TimelineStore.Timeline,
            new Domain.Competition.LeagueCompetition(new Domain.Competition.CompetitionId(1)),
            ClubGovernanceModule.CreateMvpLeague().Store.Registry,
            ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1).Store.Career,
            Array.Empty<Domain.TeamPreparation.MatchSelection>(),
            Array.Empty<Domain.TrainingPhysicalState.WeeklyTrainingPlan>(),
            Array.Empty<Domain.TrainingPhysicalState.PlayerPhysicalState>(),
            Array.Empty<Domain.PlayerCareer.PlayerCareer>(),
            Array.Empty<Domain.ContractRegistration.PlayerContract>(),
            Array.Empty<Domain.TeamPreparation.ClubSquad>(),
            Array.Empty<Domain.ContractRegistration.PlayerFreeAgency>(),
            Array.Empty<Domain.TeamPreparation.TacticPlan>(),
            Array.Empty<TransferNeed>(),
            Array.Empty<ShortlistEntry>(),
            Array.Empty<TransferTarget>(),
            Array.Empty<TransferProcess>(),
            Array.Empty<ClubOffer>(),
            Array.Empty<PlayerContractProposal>(),
            Array.Empty<Domain.SocialContinuity.Promise>(),
            Array.Empty<Domain.SocialContinuity.MemoryRecord>());

        var loaded = persistence.Load(path);
        Assert.Equal(35, loaded.SchemaVersion);
        Assert.False(loaded.Timeline.TransferWindow.IsOpen);
    }

    private static (
        WorldCalendarModule World,
        TransferModule Transfer) CreateModulesWithListedTarget()
    {
        var modules = CreateBase();
        modules.Transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.PositionGap,
            priority: 4,
            "ManualPositionGap",
            Day);
        modules.Transfer.ShortlistTargets.SuggestAndListTargetForOldestOpenNeed(new ClubId(1), Day);
        return (modules.World, modules.Transfer);
    }

    private static (
        WorldCalendarModule World,
        TransferModule Transfer) CreateModulesWithFinancialApproval()
    {
        var modules = CreateBase();
        modules.Players.Development.EnsureClub(new ClubId(1), modules.World.TimelineStore.Timeline.RootSeed, Day);
        modules.Players.Development.EnsureClub(new ClubId(2), modules.World.TimelineStore.Timeline.RootSeed, Day);
        modules.TeamPrep.ClubSquad!.SyncFromActiveContracts(new ClubId(1), Day);
        modules.TeamPrep.ClubSquad.SyncFromActiveContracts(new ClubId(2), Day);

        modules.Transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.PositionGap,
            priority: 4,
            "ManualPositionGap",
            Day);
        modules.Transfer.ShortlistTargets.SuggestAndListTargetForOldestOpenNeed(new ClubId(1), Day);
        var process = modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
        modules.Transfer.Processes.RequestSportingApproval(process.ProcessId);
        modules.Transfer.Processes.GrantSportingApproval(process.ProcessId);
        modules.Transfer.ClubOffers.SubmitClubOffer(process.ProcessId, 5_000_000, Day);
        modules.Transfer.ClubOffers.AcceptPendingOffer(process.ProcessId);
        modules.Transfer.ContractProposals.SubmitContractProposal(process.ProcessId, 25_000, 3, Day);
        modules.Transfer.ContractProposals.AcceptPendingProposal(process.ProcessId);
        modules.Transfer.Processes.RequestFinancialApproval(process.ProcessId);
        modules.Transfer.Processes.GrantFinancialApproval(process.ProcessId);

        // Free a buying slot for completion path used by other tests.
        var outgoing = modules.Players.Store.Careers
            .Where(c => c.OriginClubId.Value == 1)
            .OrderByDescending(c => c.SlotIndex)
            .Select(c => c.Id)
            .First(id => modules.Contracts.Store.GetByPlayer(id)?.IsActiveOn(Day) == true);
        var existing = modules.Contracts.Store.GetByPlayer(outgoing)!;
        modules.Contracts.Store.Upsert(Domain.ContractRegistration.PlayerContract.Rehydrate(
            existing.Id,
            existing.PlayerId,
            existing.ClubId,
            existing.StartDate,
            existing.StartDate,
            existing.WeeklyWage,
            Domain.ContractRegistration.ContractStatus.Expired));
        modules.TeamPrep.ClubSquad.SyncFromActiveContracts(new ClubId(1), Day);

        return (modules.World, modules.Transfer);
    }

    private static (
        WorldCalendarModule World,
        TransferModule Transfer,
        ContractRegistrationModule Contracts,
        PlayerCareerModule Players,
        TeamPreparationModule TeamPrep) CreateBase()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 91);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var competition = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var playerStore = new InMemoryPlayerCareerStore();
        var contracts = ContractRegistrationModule.Create(
            playerStore,
            manager.Store,
            world.TimelineStore);
        var players = PlayerCareerModule.Create(
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
        var transfer = TransferModule.Create(
            contracts.Store,
            teamPrep.SquadStore,
            manager.Store,
            contracts.Registration,
            teamPrep.ClubSquad!,
            transferWindow: new TimelineTransferWindowQuery(world.TimelineStore),
            transferBudget: clubs.TransferBudget);
        return (world, transfer, contracts, players, teamPrep);
    }
}
