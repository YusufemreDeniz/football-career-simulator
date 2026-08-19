using FootballCareerSimulator.Application.Competition.Infrastructure;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.PlayerCareer.Services;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.PlayerCareer;

public sealed class SeasonPlayerLifecycleTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 6, 1);

    [Fact]
    public void SeasonRollover_RetiresEligiblePlayerAndSynchronizesSuccessorAcrossContexts()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 71);
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var training = new InMemoryTrainingPhysicalStateStore();
        var playerStore = new InMemoryPlayerCareerStore();
        var contracts = ContractRegistrationModule.Create(
            playerStore,
            manager.Store,
            world.TimelineStore);
        var players = PlayerCareerModule.Create(
            manager.Store,
            world.TimelineStore,
            training,
            playerStore,
            contracts.Registration);
        var competitionStore = new InMemoryLeagueCompetitionStore(
            new LeagueCompetition(new CompetitionId(1)));
        var team = TeamPreparationModule.Create(
            competitionStore,
            manager.Store,
            trainingStore: training,
            timelineStore: world.TimelineStore,
            contractStore: contracts.Store,
            playerCareerStore: playerStore);

        var clubId = new ClubId(1);
        players.Development.EnsureClub(clubId, world.TimelineStore.Timeline.RootSeed, Day);
        var eligible = Domain.PlayerCareer.PlayerCareer.CreateForSlot(
            clubId,
            slotIndex: 0,
            currentAbility: 61,
            potentialAbility: 66,
            birthYear: 1991);
        playerStore.Upsert(eligible);
        contracts.Registration.EnsureClubContracts(clubId, Day);
        team.ClubSquad!.SyncFromActiveContracts(clubId, Day);
        training.ReplacePhysicalStatesForClub(
            clubId,
            [PlayerPhysicalState.CreateRested(clubId, 0).WithInjury(InjurySeverity.Moderate, Day.AddDays(30))]);

        var service = new SeasonPlayerLifecycleService(
            playerStore,
            players.Development,
            contracts.Registration,
            team.ClubSquad,
            training,
            world.TimelineStore);

        var result = service.ApplySeasonRollover(Day);

        Assert.Equal(1, result.RetiredPlayerCount);
        Assert.Equal(1, result.GeneratedPlayerCount);
        Assert.Equal([1L], result.AffectedClubIds);

        var retired = playerStore.Careers.Single(career => career.Id == eligible.Id);
        Assert.True(retired.IsRetired);
        Assert.Equal(Day, retired.RetiredOn);
        var successor = playerStore.Get(clubId, 0)!;
        Assert.NotEqual(retired.Id, successor.Id);
        Assert.Equal(1, successor.Generation);
        Assert.InRange(successor.AgeYears(Day), 17, 20);

        Assert.Equal(ContractStatus.Expired, contracts.Store.GetByPlayer(retired.Id)!.Status);
        Assert.True(contracts.Store.GetByPlayer(successor.Id)!.IsActiveOn(Day));
        Assert.DoesNotContain(team.SquadStore.Get(clubId)!.Members, member => member.PlayerId == retired.Id);
        Assert.Contains(team.SquadStore.Get(clubId)!.Members, member => member.PlayerId == successor.Id);
        Assert.False(training.GetPhysical(clubId, 0)!.IsInjured);

        var repeated = service.ApplySeasonRollover(Day);
        Assert.Equal(0, repeated.RetiredPlayerCount);
        Assert.Equal(26, playerStore.Careers.Count);
    }
}
