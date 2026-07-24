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

public sealed class PlayerContractProposalTests : IDisposable
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-contract-proposal",
        Guid.NewGuid().ToString("N"));

    public PlayerContractProposalTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Submit_AfterClubAgreement_EntersPlayerNegotiation()
    {
        var modules = CreateModulesWithClubAgreement();
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;

        var proposal = modules.Transfer.ContractProposals.SubmitContractProposal(
            processId,
            weeklyWage: 20_000,
            contractYears: 3,
            Day);

        Assert.True(proposal.IsPending);
        Assert.Equal(1, proposal.Round);
        Assert.Equal(
            TransferProcessStatus.PlayerNegotiation,
            modules.Transfer.ProcessStore.Get(processId)!.Status);
    }

    [Fact]
    public void CounterThenAccept_ReachesPlayerAgreement()
    {
        var modules = CreateModulesWithClubAgreement();
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;
        modules.Transfer.ContractProposals.SubmitContractProposal(processId, 15_000, 2, Day);
        var counter = modules.Transfer.ContractProposals.CounterPendingProposal(
            processId,
            22_000,
            3,
            Day.AddDays(1));
        Assert.Equal(2, counter.Round);

        var accepted = modules.Transfer.ContractProposals.AcceptPendingProposal(processId);
        Assert.Equal(PlayerContractProposalStatus.Accepted, accepted.Status);
        Assert.Equal(
            TransferProcessStatus.PlayerAgreementReached,
            modules.Transfer.ProcessStore.Get(processId)!.Status);
    }

    [Fact]
    public void FreeAgent_SubmitAfterSportingApproval_SkipsClubOffer()
    {
        var modules = CreateModulesWithFreeAgentSportingApproval();
        var process = modules.Transfer.ProcessStore.Processes.Single();
        Assert.True(process.IsFreeAgent);

        Assert.Throws<TransferInvariantViolationException>(() =>
            modules.Transfer.ClubOffers.SubmitClubOffer(process.ProcessId, 1_000_000, Day));

        var proposal = modules.Transfer.ContractProposals.SubmitContractProposal(
            process.ProcessId,
            weeklyWage: 18_000,
            contractYears: 2,
            Day);

        Assert.True(proposal.IsPending);
        Assert.Equal(
            TransferProcessStatus.PlayerNegotiation,
            modules.Transfer.ProcessStore.Get(process.ProcessId)!.Status);
    }

    [Fact]
    public void Reject_KeepsNegotiationOpenForNewProposal()
    {
        var modules = CreateModulesWithClubAgreement();
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;
        modules.Transfer.ContractProposals.SubmitContractProposal(processId, 10_000, 2, Day);
        modules.Transfer.ContractProposals.RejectPendingProposal(processId);

        var next = modules.Transfer.ContractProposals.SubmitContractProposal(
            processId,
            12_000,
            2,
            Day.AddDays(1));
        Assert.Equal(2, next.Round);
        Assert.True(next.IsPending);
    }

    [Fact]
    public void SaveLoad_PreservesProposalsAtSchemaV24()
    {
        var modules = CreateModulesWithClubAgreement();
        var processId = modules.Transfer.ProcessStore.Processes.Single().ProcessId;
        modules.Transfer.ContractProposals.SubmitContractProposal(processId, 30_000, 4, Day);

        var persistence = new CareerSqlitePersistence();
        var path = Path.Combine(_tempDirectory, "proposals.db");
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
        Assert.Equal(31, loaded.SchemaVersion);
        Assert.Single(loaded.ContractProposals);
        Assert.Equal(30_000, loaded.ContractProposals[0].WeeklyWage);
        Assert.Equal(4, loaded.ContractProposals[0].ContractYears);
        Assert.Equal(PlayerContractProposalStatus.Pending, loaded.ContractProposals[0].Status);
        Assert.Equal(TransferProcessStatus.PlayerNegotiation, loaded.TransferProcesses[0].Status);
    }

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        ManagerCareerModule Manager,
        TransferModule Transfer) CreateModulesWithClubAgreement()
    {
        var modules = CreateBaseModules();
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
        return modules;
    }

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        ManagerCareerModule Manager,
        TransferModule Transfer) CreateModulesWithFreeAgentSportingApproval()
    {
        var modules = CreateBaseModules();
        modules.Transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.PositionGap,
            priority: 4,
            "FreeAgentGap",
            Day);
        var need = modules.Transfer.NeedStore.Needs.Single();
        var target = TransferTarget.List(
            new TransferTargetId(1),
            need.NeedId,
            new ClubId(1),
            new PlayerId(42),
            shortlistEntryId: null,
            Day);
        modules.Transfer.TargetStore.Upsert(target);
        var process = modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
        Assert.True(process.IsFreeAgent);
        modules.Transfer.Processes.RequestSportingApproval(process.ProcessId);
        modules.Transfer.Processes.GrantSportingApproval(process.ProcessId);
        return modules;
    }

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        ManagerCareerModule Manager,
        TransferModule Transfer) CreateBaseModules()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 61);
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
