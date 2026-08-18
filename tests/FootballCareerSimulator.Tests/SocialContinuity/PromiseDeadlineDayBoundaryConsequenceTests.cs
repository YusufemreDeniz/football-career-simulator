using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class PromiseDeadlineDayBoundaryConsequenceTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void Advance_ResolvesBrokenPromise_AndOpensCrisisViaDayBoundary()
    {
        var modules = CreateBound();
        modules.Social.StartingOpportunity.Create(
            new ManagerId(1),
            new PlayerId(41),
            new ClubId(1),
            targetStarts: 2,
            deadlineOn: Day.AddDays(3),
            createdOn: Day);

        var advance = modules.World.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), Day.AddDays(3).DayNumber));

        Assert.True(advance.Succeeded);
        Assert.Equal(1, advance.PromiseDeadlineResolvedCount);
        Assert.Equal(1, advance.PromiseBrokenCrisisOpenedCount);
        Assert.Equal(PromiseStatus.Broken, modules.Social.PromiseStore.Promises.Single().Status);
        Assert.Contains(
            modules.Interaction.DecisionRequestStore.Requests,
            r => r.Kind == DecisionRequestKind.StartingOpportunityRequest
                 && r.SubjectPlayerId.Value == 41);
    }

    private static (
        WorldCalendarModule World,
        SocialContinuityModule Social,
        InteractionModule Interaction) CreateBound()
    {
        var world = WorldCalendarModule.Create(Day, rootSeed: 29);
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore,
            startingOpportunity: social.StartingOpportunity,
            relationshipStore: social.RelationshipStore);

        world.AdvanceSimulationTime.BindPromiseDeadlineConsequences(
            new PromiseDeadlineDayBoundaryApplier(
                social.StartingOpportunity,
                world.EventRuleEvaluation!.Gate,
                interaction.PromiseBroken));

        return (world, social, interaction);
    }
}
