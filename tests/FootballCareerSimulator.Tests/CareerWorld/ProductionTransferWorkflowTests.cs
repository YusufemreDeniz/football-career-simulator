using FootballCareerSimulator.Application.CareerWorld;
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
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.CareerWorld;

public sealed class ProductionTransferWorkflowTests
{
    private const int Seed = 626262;
    private static readonly GameDate Opening = ProductionCareerWorldConstraints.DefaultOpeningDate;

    [Fact]
    public void ProductionWorld_SupportsNeedTargetProcessLoop()
    {
        var generated = ProductionCareerWorldBootstrap.Create(Seed, Opening);
        var world = WorldCalendarModule.Create(Opening, rootSeed: Seed);
        var clubs = ClubGovernanceModule.Create(generated.ClubRegistry);
        var manager = ManagerCareerModule.CreateNewCareer(Opening, startingClubId: 1);
        var competition = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var playerStore = new InMemoryPlayerCareerStore();
        var contracts = ContractRegistrationModule.Create(
            playerStore,
            manager.Store,
            world.TimelineStore);
        ProductionCareerWorldBootstrap.HydratePeople(generated, playerStore, contracts.FreeAgentStore);
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
        var transfer = TransferModule.Create(
            contracts.Store,
            teamPrep.SquadStore,
            manager.Store,
            contracts.Registration,
            teamPrep.ClubSquad!,
            transferBudget: clubs.TransferBudget);

        var need = transfer.Needs.Declare(
            new ClubId(1),
            TransferNeedKind.PositionGap,
            priority: 4,
            "ManualPositionGap",
            Opening);
        Assert.True(need.IsOpen);
        Assert.Equal(1, transfer.Queries.GetManagedClubNeeds().OpenCount);

        var listed = transfer.ShortlistTargets.SuggestAndListTargetForOldestOpenNeed(new ClubId(1), Opening);
        Assert.NotNull(listed);
        Assert.True(transfer.Queries.GetManagedClubShortlistTargets().ListedTargetCount > 0);

        var process = transfer.Processes.OpenOldestListedTargetForClub(new ClubId(1), Opening);
        Assert.True(process.IsActive);
        Assert.Equal(TransferProcessStatus.UnderEvaluation, process.Status);
        Assert.Equal(1, transfer.Queries.GetManagedClubProcesses().ActiveCount);
        Assert.NotEqual(1, process.SellingClubId?.Value);
    }
}
