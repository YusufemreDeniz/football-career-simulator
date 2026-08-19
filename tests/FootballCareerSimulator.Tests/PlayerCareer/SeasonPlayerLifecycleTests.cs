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
using FootballCareerSimulator.Simulation.PlayerCareer;

namespace FootballCareerSimulator.Tests.PlayerCareer;

public sealed class SeasonPlayerLifecycleTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 6, 1);

    [Fact]
    public void RetirementEvaluation_DoesNotRetireEveryPlayerAtOneFixedAge()
    {
        var decisions = Enumerable.Range(0, 25)
            .Select(slot => Domain.PlayerCareer.PlayerCareer.CreateForSlot(
                new ClubId(1),
                slot,
                currentAbility: 60,
                potentialAbility: 65,
                birthYear: 1991))
            .Select(career => MvpRetirementEvaluator.Evaluate(career, Day, rootSeed: 71).Decision)
            .ToArray();

        Assert.Contains(RetirementEvaluationDecision.Retire, decisions);
        Assert.Contains(RetirementEvaluationDecision.ReevaluateLater, decisions);
        Assert.Equal(decisions, Enumerable.Range(0, 25)
            .Select(slot => Domain.PlayerCareer.PlayerCareer.CreateForSlot(
                new ClubId(1),
                slot,
                currentAbility: 60,
                potentialAbility: 65,
                birthYear: 1991))
            .Select(career => MvpRetirementEvaluator.Evaluate(career, Day, rootSeed: 71).Decision));
    }

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
            birthYear: 1985);
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

    [Fact]
    public void TenSeasonRollover_PreservesActivePopulationContractsAndUniqueIdentity()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 83);
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
        var service = new SeasonPlayerLifecycleService(
            playerStore,
            players.Development,
            contracts.Registration,
            team.ClubSquad!,
            training,
            world.TimelineStore);
        var clubId = new ClubId(1);
        players.Development.EnsureClub(clubId, world.TimelineStore.Timeline.RootSeed, Day);
        team.ClubSquad!.SyncFromActiveContracts(clubId, Day);

        for (var season = 1; season <= 10; season++)
        {
            var rolloverDay = GameDate.FromCalendarDate(Day.Year + season, Day.Month, Day.Day);
            service.ApplySeasonRollover(rolloverDay);

            var active = playerStore.Careers.Where(career => !career.IsRetired).ToArray();
            Assert.Equal(25, active.Length);
            Assert.Equal(25, active.Select(career => career.Id).Distinct().Count());
            Assert.Equal(25, active.Select(career => career.SlotIndex).Distinct().Count());
            Assert.InRange(contracts.FreeAgentStore.FreeAgents.Count, 0, 2);

            var activeContracts = contracts.Store.GetForClub(clubId)
                .Where(contract => contract.IsActiveOn(rolloverDay))
                .ToArray();
            Assert.InRange(activeContracts.Length, 23, 25);
            Assert.Equal(activeContracts.Length, team.SquadStore.Get(clubId)!.Members.Count);
            Assert.DoesNotContain(
                playerStore.Careers.Where(career => career.IsRetired),
                retired => activeContracts.Any(contract => contract.PlayerId == retired.Id));
        }

        Assert.Contains(playerStore.Careers, career => career.IsRetired);
        Assert.Equal(
            playerStore.Careers.Count,
            playerStore.Careers.Select(career => career.Id).Distinct().Count());
    }
}
