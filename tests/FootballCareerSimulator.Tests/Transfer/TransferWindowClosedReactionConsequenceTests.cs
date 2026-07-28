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
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Infrastructure;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class TransferWindowClosedReactionConsequenceTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    [Fact]
    public void CloseTransferWindowHandler_AppliesExpireViaClosedReaction()
    {
        var modules = CreateBound();
        SeedListedTarget(modules);
        var process = modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
        modules.Transfer.Processes.RequestSportingApproval(process.ProcessId);
        modules.Transfer.Processes.GrantSportingApproval(process.ProcessId);
        modules.Transfer.ClubOffers.SubmitClubOffer(process.ProcessId, 1_000_000, Day);

        var result = modules.World.CloseTransferWindow.Handle(
            new CloseTransferWindowCommand(Guid.NewGuid()));

        Assert.False(result.IsOpen);
        Assert.Equal(1, result.AppliedEffectCount);
        Assert.Equal(1, result.ReactionIntentCount);
        Assert.Equal(1, result.ExpiredProcessCount);
        Assert.Equal(0, result.CarriedProcessCount);

        var closed = modules.Transfer.ProcessStore.Get(process.ProcessId)!;
        Assert.Equal(TransferProcessStatus.Archived, closed.Status);
        Assert.Equal(TransferWindowCloseService.WindowClosedReason, closed.FailureReasonCode);
    }

    [Fact]
    public void ScheduledWindowClose_AlsoExpiresNegotiatingProcess()
    {
        var modules = CreateBound();
        var closesOn = Day.AddDays(1);
        modules.World.CloseTransferWindow.Handle(new CloseTransferWindowCommand(Guid.NewGuid()));
        modules.World.OpenTransferWindow.Handle(
            new OpenTransferWindowCommand(Guid.NewGuid(), closesOn.DayNumber));

        SeedListedTarget(modules);
        var process = modules.Transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Day);
        modules.Transfer.Processes.RequestSportingApproval(process.ProcessId);
        modules.Transfer.Processes.GrantSportingApproval(process.ProcessId);
        modules.Transfer.ClubOffers.SubmitClubOffer(process.ProcessId, 1_000_000, Day);

        var advance = modules.World.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), closesOn.DayNumber));

        Assert.Equal(1, advance.TransferWindowsClosedBySchedule);
        Assert.False(modules.World.TimelineStore.Timeline.TransferWindow.IsOpen);
        Assert.Equal(
            TransferProcessStatus.Archived,
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

    private static (WorldCalendarModule World, TransferModule Transfer) CreateBound()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 88);
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

        world.CloseTransferWindow.BindWindowClosedConsequences(
            new TransferWindowClosedConsequenceApplier(
                transfer.WindowClose,
                world.EventRuleEvaluation!.Gate));

        return (world, transfer);
    }
}
