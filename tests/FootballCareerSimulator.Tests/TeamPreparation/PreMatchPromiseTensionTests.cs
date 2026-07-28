using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TeamPreparation.Services;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class PreMatchPromiseTensionTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void BenchPromise_DefaultXi_IsAtRisk()
    {
        var modules = CreateBound();
        var benchMember = modules.TeamPrep.SquadStore.Get(new ClubId(1))!.Members
            .Single(m => m.SlotIndex == MatchSelection.StartingXiSize);

        modules.Social.StartingOpportunity.Create(
            modules.Manager.Store.Career.ManagerId,
            benchMember.PlayerId,
            new ClubId(1),
            targetStarts: 2,
            deadlineOn: Day.AddDays(14),
            createdOn: Day);

        AdvanceToManagedMatchday(modules);

        var tension = modules.TeamPrep.PromiseTension.GetForNextDueMatch(
            modules.World.Queries.GetCurrentGameDate().DayNumber);
        Assert.NotNull(tension);
        Assert.True(tension!.HasTension);
        Assert.Equal(PreMatchPromiseTensionQueryService.ToneAtRisk, tension.ToneCode);
        Assert.Contains("YEDEKTE", tension.Headline, StringComparison.Ordinal);
        Assert.Equal(
            PreMatchPromiseTensionQueryService.PlacementBench,
            tension.Lines.Single().PlacementCode);
    }

    [Fact]
    public void StartingPromise_DefaultXi_IsOnTrack()
    {
        var modules = CreateBound();
        var starter = modules.TeamPrep.SquadStore.Get(new ClubId(1))!.Members
            .Single(m => m.SlotIndex == 0);

        modules.Social.StartingOpportunity.Create(
            modules.Manager.Store.Career.ManagerId,
            starter.PlayerId,
            new ClubId(1),
            targetStarts: 2,
            deadlineOn: Day.AddDays(14),
            createdOn: Day);

        AdvanceToManagedMatchday(modules);

        var tension = modules.TeamPrep.PromiseTension.GetForNextDueMatch(
            modules.World.Queries.GetCurrentGameDate().DayNumber);
        Assert.NotNull(tension);
        Assert.Equal(PreMatchPromiseTensionQueryService.ToneOnTrack, tension!.ToneCode);
        Assert.Contains("XI'da", tension.Headline, StringComparison.Ordinal);
    }

    private static void AdvanceToManagedMatchday(
        (
            WorldCalendarModule World,
            CompetitionModule Competition,
            ManagerCareerModule Manager,
            TeamPreparationModule TeamPrep,
            SocialContinuityModule Social) modules)
    {
        var fixtures = modules.Competition.Queries.GetSeasonFixtures(1);
        var managed = fixtures.First(f => f.HomeClubId == 1 || f.AwayClubId == 1);
        var current = modules.World.Queries.GetCurrentGameDate().DayNumber;
        if (managed.ScheduledDayNumber > current)
        {
            modules.World.AdvanceSimulationTime.Handle(
                new AdvanceSimulationTimeCommand(Guid.NewGuid(), managed.ScheduledDayNumber));
        }
    }

    private static (
        WorldCalendarModule World,
        CompetitionModule Competition,
        ManagerCareerModule Manager,
        TeamPreparationModule TeamPrep,
        SocialContinuityModule Social) CreateBound()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 61);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var selectionStore = new InMemoryMatchSelectionStore();
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
        var competition = CompetitionModule.CreateForCareer(
            world.TimelineStore,
            clubs.Store,
            manager.Store,
            selectionStore);
        var social = SocialContinuityModule.Create();
        var teamPrep = TeamPreparationModule.Create(
            competition.Store,
            manager.Store,
            selectionStore,
            trainingStore,
            world.TimelineStore,
            contracts.Store,
            playerStore,
            promiseStore: social.PromiseStore);

        competition.CreateSeason.Handle(new CreateSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        for (var club = 1L; club <= CompetitionMvpConstraints.LeagueTeamCount; club++)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), 1, club));
        }

        competition.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), 1, Day.DayNumber));
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(Guid.NewGuid(), 1, Day.DayNumber, StartingFixtureId: 1));
        players.Development.EnsureClub(new ClubId(1), 61, Day);
        teamPrep.ClubSquad!.SyncFromActiveContracts(new ClubId(1), Day);

        return (world, competition, manager, teamPrep, social);
    }
}
