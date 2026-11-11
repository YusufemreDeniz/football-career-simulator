using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.ContractRegistration;

namespace FootballCareerSimulator.Tests.ContractRegistration;

public sealed class FreeAgentResignTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 7, 1);

    [Fact]
    public void SignFreeAgentToLastClub_RestoresActiveContractAndSquadMembership()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 8);
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

        players.Development.EnsureClub(new ClubId(1), world.TimelineStore.Timeline.RootSeed, Day);
        teamPrep.ClubSquad!.SyncFromActiveContracts(new ClubId(1), Day);

        var playerId = PlayerId.FromClubSlot(1, MvpContractFactory.ShortContractSquadSlot);
        var afterEnd = Day.AddDays(46);
        var expiry = contracts.Registration.ExpireDueContracts(afterEnd);
        teamPrep.ClubSquad.SyncClubs(expiry.AffectedClubIds, afterEnd);
        Assert.True(contracts.Registration.IsFreeAgent(playerId));
        Assert.Equal(24, teamPrep.SquadStore.Get(new ClubId(1))!.Members.Count);

        var signable = contracts.Queries.GetNextSignableFreeAgentForManagedClub();
        Assert.NotNull(signable);
        Assert.Equal(playerId.Value, signable.PlayerId);

        var resign = contracts.Registration.SignFreeAgentToLastClub(
            playerId,
            new ClubId(1),
            afterEnd,
            contractYears: 2);
        teamPrep.ClubSquad.SyncFromActiveContracts(new ClubId(1), afterEnd);

        Assert.Equal(playerId.Value, resign.PlayerId);
        Assert.False(contracts.Registration.IsFreeAgent(playerId));
        Assert.Equal(new ClubId(1), contracts.Registration.GetActiveClub(playerId, afterEnd));
        Assert.Equal(ContractStatus.Active, contracts.Store.GetByPlayer(playerId)!.Status);
        Assert.Equal(25, teamPrep.SquadStore.Get(new ClubId(1))!.Members.Count);
        Assert.True(teamPrep.SquadStore.Get(new ClubId(1))!.ContainsPlayer(playerId));
        Assert.Equal(0, contracts.Queries.GetManagedClubSummary().FreeAgentReleasedCount);
        Assert.Null(contracts.Queries.GetNextSignableFreeAgentForManagedClub());
    }

    [Fact]
    public void SignFreeAgent_RejectsDifferentClub_NoTransfer()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 3);
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var players = PlayerCareerModule.Create(manager.Store, world.TimelineStore, trainingStore);
        var contracts = ContractRegistrationModule.Create(
            players.Store,
            manager.Store,
            world.TimelineStore);

        var playerId = PlayerId.FromClubSlot(1, 0);
        players.Store.Upsert(Domain.PlayerCareer.PlayerCareer.CreateForSlot(
            new ClubId(1),
            0,
            55,
            70,
            2001));
        contracts.Store.Upsert(PlayerContract.Activate(
            playerId,
            new ClubId(1),
            Day,
            Day.AddDays(1),
            900));
        contracts.Registration.ExpireDueContracts(Day.AddDays(2));

        Assert.Throws<ContractRegistrationInvariantViolationException>(() =>
            contracts.Registration.SignFreeAgentToLastClub(
                playerId,
                new ClubId(2),
                Day.AddDays(3)));
    }
}
