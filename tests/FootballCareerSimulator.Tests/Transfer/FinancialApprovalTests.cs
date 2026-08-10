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
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class FinancialApprovalTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-financial-approval",
        Guid.NewGuid().ToString("N"));

    public FinancialApprovalTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void RequestAndGrant_MovesToFinancialApproved()
    {
        var modules = CreateModulesWithPlayerAgreement();
        var process = modules.Transfer.ProcessStore.Processes.Single();

        var pending = modules.Transfer.Processes.RequestFinancialApproval(process.ProcessId);
        Assert.Equal(TransferProcessStatus.FinancialApprovalPending, pending.Status);
        Assert.True(pending.AwaitsFinancialDecision);

        var approved = modules.Transfer.Processes.GrantFinancialApproval(process.ProcessId);
        Assert.True(approved.HasFinancialApproval);
        Assert.Equal(TransferProcessStatus.FinancialApproved, approved.Status);
        Assert.True(approved.IsActive);
    }

    [Fact]
    public void Reject_TerminatesAsRejected()
    {
        var modules = CreateModulesWithPlayerAgreement();
        var process = modules.Transfer.ProcessStore.Processes.Single();
        modules.Transfer.Processes.RequestFinancialApproval(process.ProcessId);

        var rejected = modules.Transfer.Processes.RejectFinancialApproval(
            process.ProcessId,
            "BudgetExceeded",
            Day.AddDays(1));

        Assert.Equal(TransferProcessStatus.Rejected, rejected.Status);
        Assert.False(rejected.IsActive);
        Assert.Equal("BudgetExceeded", rejected.FailureReasonCode);
        Assert.Equal(0, modules.Transfer.Queries.GetManagedClubProcesses().ActiveCount);
    }

    [Fact]
    public void Grant_WithoutPending_Throws()
    {
        var modules = CreateModulesWithPlayerAgreement();
        var process = modules.Transfer.ProcessStore.Processes.Single();

        Assert.Throws<TransferInvariantViolationException>(() =>
            modules.Transfer.Processes.GrantFinancialApproval(process.ProcessId));
    }

    [Fact]
    public void Request_IsIdempotentWhilePending()
    {
        var modules = CreateModulesWithPlayerAgreement();
        var process = modules.Transfer.ProcessStore.Processes.Single();
        var first = modules.Transfer.Processes.RequestFinancialApproval(process.ProcessId);
        var second = modules.Transfer.Processes.RequestFinancialApproval(process.ProcessId);

        Assert.Equal(first.Status, second.Status);
        Assert.Equal(TransferProcessStatus.FinancialApprovalPending, second.Status);
    }

    [Fact]
    public void SaveLoad_PreservesFinancialApprovedAtSchemaV25()
    {
        var modules = CreateModulesWithPlayerAgreement();
        var process = modules.Transfer.ProcessStore.Processes.Single();
        modules.Transfer.Processes.RequestFinancialApproval(process.ProcessId);
        modules.Transfer.Processes.GrantFinancialApproval(process.ProcessId);

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "financial.db");
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
        Assert.Equal(42, loaded.SchemaVersion);
        Assert.Single(loaded.TransferProcesses);
        Assert.Equal(TransferProcessStatus.FinancialApproved, loaded.TransferProcesses[0].Status);
    }

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        ManagerCareerModule Manager,
        TransferModule Transfer) CreateModulesWithPlayerAgreement()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 71);
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
        transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.PositionGap,
            priority: 4,
            "ManualPositionGap",
            Day);
        transfer.ShortlistTargets.SuggestAndListTargetForOldestOpenNeed(new ClubId(1), Day);
        var process = transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
        transfer.Processes.RequestSportingApproval(process.ProcessId);
        transfer.Processes.GrantSportingApproval(process.ProcessId);
        transfer.ClubOffers.SubmitClubOffer(process.ProcessId, 5_000_000, Day);
        transfer.ClubOffers.AcceptPendingOffer(process.ProcessId);
        transfer.ContractProposals.SubmitContractProposal(process.ProcessId, 25_000, 3, Day);
        transfer.ContractProposals.AcceptPendingProposal(process.ProcessId);
        return (world, clubs, manager, transfer);
    }
}
