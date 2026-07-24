using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Services;
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

        // Same seed with no remaining free agents and no club contracts → no second completion.
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

    [Fact]
    public void WindowTick_WithoutFreeAgents_CompletesClubToClubSale()
    {
        var modules = CreateBase(seed: 5);
        SeedUnmanagedClubSquadsWithSpace(modules);

        var outcome = modules.Transfer.AiSimulation.RunWindowTick(Day, worldSeed: 5);
        Assert.Equal(1, outcome.CompletedCount);

        var process = modules.Transfer.ProcessStore.Processes.Single();
        Assert.False(process.IsFreeAgent);
        Assert.NotEqual(1, process.BuyingClubId.Value);
        Assert.NotEqual(1, process.SellingClubId!.Value.Value);
        Assert.Equal(TransferProcessStatus.Archived, process.Status);

        var buyerBudget = modules.Clubs.TransferBudget.Get(process.BuyingClubId);
        Assert.Equal(0, buyerBudget.Reserved);
        Assert.Equal(
            AiClubTransferSimulationService.DefaultClubTransferFee,
            buyerBudget.Spent);

        Assert.NotNull(modules.Contracts.Store.GetByPlayer(process.PlayerId));
        Assert.Equal(
            process.BuyingClubId,
            modules.Contracts.Store.GetByPlayer(process.PlayerId)!.ClubId);
    }

    [Fact]
    public void WindowTick_ClubToClub_DoesNotSellFromHumanClub()
    {
        var modules = CreateBase(seed: 9);
        modules.Players.Development.EnsureClub(new ClubId(1), 9, Day);
        modules.TeamPrep.ClubSquad!.SyncFromActiveContracts(new ClubId(1), Day);
        SeedUnmanagedClubSquadsWithSpace(modules);

        _ = modules.Transfer.AiSimulation.RunWindowTick(Day, worldSeed: 9);

        Assert.DoesNotContain(
            modules.Transfer.ProcessStore.Processes,
            p => p.SellingClubId?.Value == 1);
    }

    private static void SeedFreeAgent(
        (
            WorldCalendarModule World,
            ClubGovernanceModule Clubs,
            TransferModule Transfer,
            ContractRegistrationModule Contracts,
            PlayerCareerModule Players,
            TeamPreparationModule TeamPrep) modules,
        PlayerId playerId)
    {
        modules.Contracts.FreeAgentStore.Upsert(
            Domain.ContractRegistration.PlayerFreeAgency.Release(playerId, new ClubId(2), Day));
    }

    private static void SeedUnmanagedClubSquadsWithSpace(
        (
            WorldCalendarModule World,
            ClubGovernanceModule Clubs,
            TransferModule Transfer,
            ContractRegistrationModule Contracts,
            PlayerCareerModule Players,
            TeamPreparationModule TeamPrep) modules)
    {
        for (var clubId = 2L; clubId <= 6L; clubId++)
        {
            var id = new ClubId(clubId);
            modules.Players.Development.EnsureClub(id, modules.World.TimelineStore.Timeline.RootSeed, Day);
            FreeOneSlot(modules.Contracts, modules.TeamPrep.ClubSquad!, modules.Players.Store, id, Day);
        }
    }

    private static void FreeOneSlot(
        ContractRegistrationModule contracts,
        ClubSquadService clubSquad,
        Application.PlayerCareer.Ports.IPlayerCareerStore playerStore,
        ClubId clubId,
        GameDate day)
    {
        var outgoing = playerStore.Careers
            .Where(c => c.OriginClubId.Value == clubId.Value)
            .OrderByDescending(c => c.SlotIndex)
            .Select(c => c.Id)
            .First(id => contracts.Store.GetByPlayer(id)?.IsActiveOn(day) == true);
        var existing = contracts.Store.GetByPlayer(outgoing)!;
        contracts.Store.Upsert(Domain.ContractRegistration.PlayerContract.Rehydrate(
            existing.Id,
            existing.PlayerId,
            existing.ClubId,
            existing.StartDate,
            existing.StartDate,
            existing.WeeklyWage,
            Domain.ContractRegistration.ContractStatus.Expired));
        clubSquad.SyncFromActiveContracts(clubId, day);
    }

    private static (
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        TransferModule Transfer,
        ContractRegistrationModule Contracts,
        PlayerCareerModule Players,
        TeamPreparationModule TeamPrep) CreateBase(int seed)
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
        return (world, clubs, transfer, contracts, players, teamPrep);
    }
}
