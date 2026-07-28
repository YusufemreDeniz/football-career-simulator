using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Services;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.ContractRegistration;

namespace FootballCareerSimulator.Tests.ContractRegistration;

public sealed class ContractExpiryDayBoundaryConsequenceTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    [Fact]
    public void Advance_ExpiresDueContractViaDayBoundaryReaction()
    {
        var modules = CreateBound();
        modules.Players.Development.EnsureClub(
            new ClubId(1),
            modules.World.TimelineStore.Timeline.RootSeed,
            Day);
        modules.TeamPrep.ClubSquad!.SyncFromActiveContracts(new ClubId(1), Day);

        var shortPlayer = PlayerId.FromClubSlot(1, MvpContractFactory.ShortContractSquadSlot);
        var end = modules.Contracts.Store.GetByPlayer(shortPlayer)!.EndDate;
        var afterEnd = end.AddDays(1);

        var advance = modules.World.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), afterEnd.DayNumber));

        Assert.True(advance.Succeeded);
        Assert.Equal(1, advance.ExpiredContractCount);
        Assert.Contains(shortPlayer.Value, advance.NewlyFreeAgentPlayerIds);
        Assert.Contains(1L, advance.ContractExpiryAffectedClubIds);
        Assert.Equal(
            ContractStatus.Expired,
            modules.Contracts.Store.GetByPlayer(shortPlayer)!.Status);

        // Aynı gün sınırı effect'leri tekrar uygulanmaz; kalan aktifler için 0.
        var again = modules.World.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), afterEnd.AddDays(1).DayNumber));
        Assert.Equal(0, again.ExpiredContractCount);
    }

    private static (
        WorldCalendarModule World,
        ContractRegistrationModule Contracts,
        PlayerCareerModule Players,
        TeamPreparationModule TeamPrep) CreateBound()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 17);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var players = PlayerCareerModule.Create(manager.Store, world.TimelineStore, trainingStore);
        var contracts = ContractRegistrationModule.Create(
            players.Store,
            manager.Store,
            world.TimelineStore);
        players = PlayerCareerModule.Create(
            manager.Store,
            world.TimelineStore,
            trainingStore,
            players.Store,
            contracts.Registration);
        var competition = CompetitionModule.CreateForCareer(world.TimelineStore, clubs.Store);
        var teamPrep = TeamPreparationModule.Create(
            competition.Store,
            manager.Store,
            contractStore: contracts.Store,
            playerCareerStore: players.Store);

        world.AdvanceSimulationTime.BindContractExpiryConsequences(
            new ContractExpiryDayBoundaryApplier(
                contracts.Registration,
                world.EventRuleEvaluation!.Gate));

        return (world, contracts, players, teamPrep);
    }
}
