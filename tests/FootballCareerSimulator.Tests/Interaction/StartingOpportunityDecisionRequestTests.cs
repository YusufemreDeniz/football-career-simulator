using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class StartingOpportunityDecisionRequestTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void GrantAnswer_CreatesStartingOpportunityPromise_AndRaisesTrust()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore,
            startingOpportunity: social.StartingOpportunity);

        var request = interaction.Decisions.OpenStartingOpportunityRequest(new PlayerId(40), Day);
        Assert.Equal(DecisionRequestKind.StartingOpportunityRequest, request.Kind);
        Assert.Equal(DialogueSession.StartingOpportunityRequestType,
            Assert.Single(interaction.DialogueSessionStore.Sessions).DialogueTypeCode);

        var answered = interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionGrantStartingOpportunityPromise,
            Day);

        Assert.Equal(DecisionRequestStatus.Answered, answered.Status);
        var promise = Assert.Single(social.PromiseStore.Promises);
        Assert.Equal(PromiseKind.StartingOpportunity, promise.Kind);
        Assert.Equal(56, social.RelationshipStore.FindPlayerToManager(40, 1)!.Trust);
        Assert.Equal(
            MemoryValence.Positive,
            Assert.Single(
                social.MemoryStore.Memories,
                m => m.RuleId == MemoryRecord.DecisionStartingOpportunityAnswerRuleId).Valence);
        Assert.Equal(
            DialogueSessionStatus.Resolved,
            interaction.DialogueSessionStore.Sessions.Single().Status);
    }

    [Fact]
    public void GrantOption_Ineligible_WhenActiveStartingPromiseExists()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            promiseStore: social.PromiseStore,
            startingOpportunity: social.StartingOpportunity);

        social.StartingOpportunity.Create(
            manager.Store.Career.ManagerId,
            new PlayerId(41),
            manager.Store.Career.ActiveEmployment!.ClubId,
            targetStarts: 2,
            deadlineOn: Day.AddDays(20),
            createdOn: Day);

        var request = interaction.Decisions.OpenStartingOpportunityRequest(new PlayerId(41), Day);
        var grant = Assert.Single(
            interaction.DialogueOptions.GetForDecision(request.DecisionRequestId).Options,
            o => o.OptionCode == DecisionRequest.OptionGrantStartingOpportunityPromise);
        Assert.False(grant.IsEligible);

        Assert.Throws<InteractionInvariantViolationException>(() =>
            interaction.Decisions.Answer(
                request.DecisionRequestId,
                DecisionRequest.OptionGrantStartingOpportunityPromise,
                Day));
    }

    [Fact]
    public void RefuseAnswer_LowersTrust_WithoutPromise()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var interaction = InteractionModule.Create(
            manager.Store,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore,
            startingOpportunity: social.StartingOpportunity);

        var request = interaction.Decisions.OpenStartingOpportunityRequest(new PlayerId(42), Day);
        interaction.Decisions.Answer(request.DecisionRequestId, DecisionRequest.OptionRefuse, Day);

        Assert.Empty(social.PromiseStore.Promises);
        Assert.Equal(40, social.RelationshipStore.FindPlayerToManager(42, 1)!.Trust);
        Assert.Equal(
            "DecisionStartingOpportunityRefused",
            social.RelationshipStore.FindPlayerToManager(42, 1)!.LastChangeReasonCode);
    }
}
