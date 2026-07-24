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
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class AiClubTransferSimulationTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    [Fact]
    public void WindowTick_SignsFreeAgentForUnmanagedClub_DeterministicBuyer()
    {
        var modules = CreateBase(seed: 42);
        SeedFreeAgent(modules, new PlayerId(42));

        var first = modules.Transfer.AiSimulation.RunWindowTick(Day, worldSeed: 42);
        Assert.Equal(1, first.CompletedCount);

        var archived = modules.Transfer.ProcessStore.Processes
            .Single(p => p.Status == TransferProcessStatus.Archived);
        Assert.True(archived.IsFreeAgent);
        Assert.NotEqual(1, archived.BuyingClubId.Value);
        Assert.Equal(42, archived.PlayerId.Value);
        Assert.NotNull(modules.Contracts.Store.GetByPlayer(new PlayerId(42)));
        Assert.Null(modules.Contracts.FreeAgentStore.Get(new PlayerId(42)));

        // Same seed with no remaining free agents → no second completion.
        var second = modules.Transfer.AiSimulation.RunWindowTick(Day, worldSeed: 42);
        Assert.Equal(0, second.CompletedCount);
    }

    [Fact]
    public void WindowTick_DoesNotTouchHumanManagedClub()
    {
        var modules = CreateBase(seed: 7);
        SeedFreeAgent(modules, new PlayerId(99));

        _ = modules.Transfer.AiSimulation.RunWindowTick(Day, worldSeed: 7);

        Assert.DoesNotContain(
            modules.Transfer.ProcessStore.Processes,
            p => p.BuyingClubId.Value == 1);
    }

    [Fact]
    public void WindowTick_ClosedWindow_NoOp()
    {
        var modules = CreateBase(seed: 3);
        SeedFreeAgent(modules, new PlayerId(55));
        modules.World.TimelineStore.Timeline.CloseTransferWindow();

        var outcome = modules.Transfer.AiSimulation.RunWindowTick(Day, worldSeed: 3);
        Assert.Equal(0, outcome.CompletedCount);
        Assert.Empty(modules.Transfer.ProcessStore.Processes);
    }

    [Fact]
    public void SameSeed_SelectsSameBuyingClub()
    {
        var a = CreateBase(seed: 11);
        var b = CreateBase(seed: 11);
        SeedFreeAgent(a, new PlayerId(77));
        SeedFreeAgent(b, new PlayerId(77));

        _ = a.Transfer.AiSimulation.RunWindowTick(Day, worldSeed: 11);
        _ = b.Transfer.AiSimulation.RunWindowTick(Day, worldSeed: 11);

        Assert.Equal(
            a.Transfer.ProcessStore.Processes.Single().BuyingClubId,
            b.Transfer.ProcessStore.Processes.Single().BuyingClubId);
    }

    private static void SeedFreeAgent(
        (
            WorldCalendarModule World,
            ClubGovernanceModule Clubs,
            TransferModule Transfer,
            ContractRegistrationModule Contracts) modules,
        PlayerId playerId)
    {
        modules.Contracts.FreeAgentStore.Upsert(
            Domain.ContractRegistration.PlayerFreeAgency.Release(playerId, new ClubId(2), Day));
    }

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        TransferModule Transfer,
        ContractRegistrationModule Contracts) CreateBase(int seed)
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
        return (world, clubs, transfer, contracts);
    }
}
