using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Composition;
using FootballCareerSimulator.Application.Transfer.Services;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Infrastructure;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class TransferWindowCloseExpireCarryTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    [Fact]
    public void WindowClose_ExpiresClubNegotiationAndSupersedesPendingOffer()
    {
        var modules = CreateBase();
        SeedListedTarget(modules);
        var process = modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
        modules.Transfer.Processes.RequestSportingApproval(process.ProcessId);
        modules.Transfer.Processes.GrantSportingApproval(process.ProcessId);
        modules.Transfer.ClubOffers.SubmitClubOffer(process.ProcessId, 1_000_000, Day);

        modules.World.TimelineStore.Timeline.CloseTransferWindow();
        var outcome = modules.Transfer.WindowClose.ApplyWindowClosed(Day);

        Assert.Equal(1, outcome.ExpiredCount);
        Assert.Equal(0, outcome.CarriedCount);
        var closed = modules.Transfer.ProcessStore.Get(process.ProcessId)!;
        Assert.Equal(TransferProcessStatus.Archived, closed.Status);
        Assert.Equal(TransferWindowCloseService.WindowClosedReason, closed.FailureReasonCode);
        Assert.All(
            modules.Transfer.OfferStore.GetForProcess(process.ProcessId),
            o => Assert.NotEqual(ClubOfferStatus.Pending, o.Status));
    }

    [Fact]
    public void WindowClose_ExpiresPlayerNegotiationAndSupersedesPendingProposal()
    {
        var modules = CreateBase();
        SeedListedTarget(modules);
        var process = modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
        modules.Transfer.Processes.RequestSportingApproval(process.ProcessId);
        modules.Transfer.Processes.GrantSportingApproval(process.ProcessId);
        modules.Transfer.ClubOffers.SubmitClubOffer(process.ProcessId, 1_000_000, Day);
        modules.Transfer.ClubOffers.AcceptPendingOffer(process.ProcessId);
        modules.Transfer.ContractProposals.SubmitContractProposal(process.ProcessId, 20_000, 3, Day);

        modules.World.TimelineStore.Timeline.CloseTransferWindow();
        var outcome = modules.Transfer.WindowClose.ApplyWindowClosed(Day);

        Assert.Equal(1, outcome.ExpiredCount);
        var closed = modules.Transfer.ProcessStore.Get(process.ProcessId)!;
        Assert.Equal(TransferProcessStatus.Archived, closed.Status);
        Assert.All(
            modules.Transfer.ProposalStore.GetForProcess(process.ProcessId),
            p => Assert.NotEqual(PlayerContractProposalStatus.Pending, p.Status));
    }

    [Fact]
    public void WindowClose_CarriesFinancialApprovedAndCompletionPending()
    {
        var modules = CreateBase();
        SeedListedTarget(modules);
        var process = modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
        modules.Transfer.Processes.RequestSportingApproval(process.ProcessId);
        modules.Transfer.Processes.GrantSportingApproval(process.ProcessId);
        modules.Transfer.ClubOffers.SubmitClubOffer(process.ProcessId, 1_000_000, Day);
        modules.Transfer.ClubOffers.AcceptPendingOffer(process.ProcessId);
        modules.Transfer.ContractProposals.SubmitContractProposal(process.ProcessId, 20_000, 3, Day);
        modules.Transfer.ContractProposals.AcceptPendingProposal(process.ProcessId);
        modules.Transfer.Processes.RequestFinancialApproval(process.ProcessId);
        modules.Transfer.Processes.GrantFinancialApproval(process.ProcessId);

        modules.World.TimelineStore.Timeline.CloseTransferWindow();
        var outcome = modules.Transfer.WindowClose.ApplyWindowClosed(Day);

        Assert.Equal(0, outcome.ExpiredCount);
        Assert.Equal(1, outcome.CarriedCount);
        Assert.Equal(
            TransferProcessStatus.FinancialApproved,
            modules.Transfer.ProcessStore.Get(process.ProcessId)!.Status);

        modules.World.TimelineStore.Timeline.OpenTransferWindow();
        modules.Transfer.ProcessStore.Upsert(
            modules.Transfer.ProcessStore.Get(process.ProcessId)!.StartCompletion());
        modules.World.TimelineStore.Timeline.CloseTransferWindow();
        var second = modules.Transfer.WindowClose.ApplyWindowClosed(Day);

        Assert.Equal(0, second.ExpiredCount);
        Assert.Equal(1, second.CarriedCount);
        Assert.Equal(
            TransferProcessStatus.CompletionPending,
            modules.Transfer.ProcessStore.Get(process.ProcessId)!.Status);
    }

    [Fact]
    public void WindowClose_CarriesUnderEvaluationUntilNextWindow()
    {
        var modules = CreateBase();
        SeedListedTarget(modules);
        var process = modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);

        modules.World.TimelineStore.Timeline.CloseTransferWindow();
        var outcome = modules.Transfer.WindowClose.ApplyWindowClosed(Day);

        Assert.Equal(0, outcome.ExpiredCount);
        Assert.Equal(1, outcome.CarriedCount);
        Assert.Equal(
            TransferProcessStatus.UnderEvaluation,
            modules.Transfer.ProcessStore.Get(process.ProcessId)!.Status);
    }

    private static void SeedListedTarget(
        (WorldCalendarModule World, TransferModule Transfer) modules)
    {
        modules.Transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.PositionGap,
            priority: 4,
            "ManualPositionGap",
            Day);
        modules.Transfer.ShortlistTargets.SuggestAndListTargetForOldestOpenNeed(new ClubId(1), Day);
    }

    private static (WorldCalendarModule World, TransferModule Transfer) CreateBase()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 77);
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
            transferWindow: new TimelineTransferWindowQuery(world.TimelineStore));
        return (world, transfer);
    }
}
