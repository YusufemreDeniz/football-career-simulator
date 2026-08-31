using FootballCareerSimulator.Application.CareerWorld;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TrainingPhysicalState.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.CareerWorld;

public sealed class ProductionMatchConsequenceTests
{
    private const int Seed = 515151;
    private static readonly GameDate Opening = ProductionCareerWorldConstraints.DefaultOpeningDate;

    [Fact]
    public void ProductionMatch_AppliesFatigueBoardAndSocialLoop()
    {
        var world = ProductionCareerWorldBootstrap.Create(Seed, Opening);
        var calendar = WorldCalendarModule.Create(Opening, rootSeed: Seed);
        var clubs = ClubGovernanceModule.Create(world.ClubRegistry);
        var manager = ManagerCareerModule.CreateNewCareer(Opening, startingClubId: 1);
        var selectionStore = new InMemoryMatchSelectionStore();
        var tacticStore = new InMemoryTacticPlanStore();
        var training = TrainingPhysicalStateModule.Create(manager.Store, calendar.TimelineStore);
        var social = SocialContinuityModule.Create();
        var competition = CompetitionModule.CreateForCareer(
            calendar.TimelineStore,
            clubs.Store,
            manager.Store,
            selectionStore,
            training.Store,
            tacticPlanStore: tacticStore,
            startingOpportunityPromises: social.StartingOpportunity,
            selectionMemory: social.SelectionMemory,
            relationships: social.RelationshipEvaluation);
        var teamPrep = TeamPreparationModule.Create(
            competition.Store,
            manager.Store,
            selectionStore,
            training.Store,
            calendar.TimelineStore,
            tacticPlanStore: tacticStore);

        competition.CreateSeason.Handle(new CreateSeasonCommand(Guid.NewGuid(), 1, Opening.DayNumber));
        foreach (var club in world.Clubs)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), 1, club.Id.Value));
        }

        competition.StartSeason.Handle(new StartSeasonCommand(Guid.NewGuid(), 1, Opening.DayNumber));
        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(Guid.NewGuid(), 1, Opening.DayNumber, StartingFixtureId: 1));

        var fixtureId = competition.Queries.GetSeasonFixtures(1)
            .Where(fixture => fixture.HomeClubId == 1 || fixture.AwayClubId == 1)
            .OrderBy(fixture => fixture.ScheduledDayNumber)
            .ThenBy(fixture => fixture.FixtureId)
            .First()
            .FixtureId;

        teamPrep.ApproveDefaultSelection.Handle(
            new ApproveDefaultMatchSelectionCommand(Guid.NewGuid(), fixtureId, ClubId: 1));
        var selection = teamPrep.SelectionQueries.Get(fixtureId, 1)!;
        var starterSlot = selection.StartingSlotIndices[0];
        social.StartingOpportunity.Create(
            manager.Store.Career.ManagerId,
            PlayerId.FromClubSlot(1, starterSlot),
            new ClubId(1),
            targetStarts: 3,
            deadlineOn: Opening.AddDays(40),
            createdOn: Opening);

        var played = competition.PlayFixtureMatch!.Handle(
            new PlayFixtureMatchCommand(Guid.NewGuid(), 1, fixtureId, Opening.DayNumber));

        Assert.True(played.Succeeded);
        Assert.NotNull(played.Consequences);
        Assert.NotNull(played.Consequences!.BoardConfidenceAfter);

        var starter = training.Store.PhysicalBySlot[(1, starterSlot)];
        Assert.True(starter.Fatigue > PlayerPhysicalState.DefaultFatigue);

        var promise = Assert.Single(social.PromiseStore.Promises);
        Assert.Equal(1, promise.StartsGiven);
        Assert.Equal(PromiseStatus.Active, promise.Status);
        Assert.Contains(
            social.MemoryStore.Memories,
            memory => memory.Category == MemoryCategory.Selection);
    }
}
