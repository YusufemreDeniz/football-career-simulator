using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class PressQuestionDecisionRequestTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private static (InteractionModule Interaction, SocialContinuityModule Social) Create()
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
        return (interaction, social);
    }

    [Fact]
    public void Defend_RaisesTrustAndRecordsPositiveMemory()
    {
        var (interaction, social) = Create();
        var request = interaction.Decisions.OpenPressQuestionRequest(new PlayerId(70), Day);
        Assert.Equal(DecisionRequestKind.PressQuestionRequest, request.Kind);

        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionPubliclyDefend,
            Day);

        var relationship = social.RelationshipStore.FindPlayerToManager(70, 1)!;
        Assert.Equal(58, relationship.Trust);
        Assert.Equal(52, relationship.Respect);
        Assert.Equal(
            MemoryValence.Positive,
            Assert.Single(
                social.MemoryStore.Memories,
                m => m.RuleId == MemoryRecord.DecisionPressQuestionAnswerRuleId).Valence);
    }

    [Fact]
    public void Criticize_LowersTrustAndRecordsNegativeMemory()
    {
        var (interaction, social) = Create();
        var request = interaction.Decisions.OpenPressQuestionRequest(new PlayerId(71), Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionPubliclyCriticize,
            Day);

        var relationship = social.RelationshipStore.FindPlayerToManager(71, 1)!;
        Assert.Equal(40, relationship.Trust);
        Assert.Equal(46, relationship.Respect);
        Assert.Equal(
            MemoryValence.Negative,
            Assert.Single(
                social.MemoryStore.Memories,
                m => m.RuleId == MemoryRecord.DecisionPressQuestionAnswerRuleId).Valence);
    }

    [Fact]
    public void DialogueOptions_IncludeDefendAndCriticize()
    {
        var (interaction, _) = Create();
        var request = interaction.Decisions.OpenPressQuestionRequest(new PlayerId(72), Day);
        var codes = interaction.DialogueOptions.GetForDecision(request.DecisionRequestId)
            .Options
            .Select(o => o.OptionCode)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                DecisionRequest.OptionPubliclyCriticize,
                DecisionRequest.OptionPubliclyDefend,
            },
            codes);
    }

    [Fact]
    public void Open_SecondPressQuestionForSamePlayer_IsRejected()
    {
        var (interaction, _) = Create();
        interaction.Decisions.OpenPressQuestionRequest(new PlayerId(73), Day);
        Assert.Throws<InteractionInvariantViolationException>(() =>
            interaction.Decisions.OpenPressQuestionRequest(new PlayerId(73), Day));
    }
}
