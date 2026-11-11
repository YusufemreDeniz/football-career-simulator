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
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class TransferWindowOpenedReactionConsequenceTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    [Fact]
    public void OpenTransferWindowHandler_RunsAiWindowTickViaOpenedReaction()
    {
        var modules = CreateBound(seed: 42);
        modules.World.CloseTransferWindow.Handle(new CloseTransferWindowCommand(Guid.NewGuid()));
        modules.Contracts.FreeAgentStore.Upsert(
            Domain.ContractRegistration.PlayerFreeAgency.Release(new PlayerId(42), new ClubId(2), Day));

        var result = modules.World.OpenTransferWindow.Handle(
            new OpenTransferWindowCommand(Guid.NewGuid(), Day.AddDays(10).DayNumber));

        Assert.True(result.IsOpen);
        Assert.Equal(1, result.ReactionIntentCount);
        Assert.Equal(1, result.AiTransferCompletedCount);
        Assert.Contains(
            modules.Transfer.ProcessStore.Processes,
            p => p.Status == TransferProcessStatus.Archived && p.PlayerId.Value == 42);
    }

    private static (
        WorldCalendarModule World,
        TransferModule Transfer,
        ContractRegistrationModule Contracts) CreateBound(int seed)
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: seed);
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
            transferBudget: clubs.TransferBudget,
            clubRegistry: clubs.Store,
            freeAgentStore: contracts.FreeAgentStore);

        world.OpenTransferWindow.BindWindowOpenedConsequences(
            new TransferWindowOpenedConsequenceApplier(
                transfer.AiSimulation,
                world.EventRuleEvaluation!.Gate));

        return (world, transfer, contracts);
    }
}
