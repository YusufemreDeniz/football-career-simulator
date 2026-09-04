using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class BoardDemandDecisionRequestTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private static (InteractionModule Interaction, ManagerCareerModule Manager, SocialContinuityModule Social)
        Create()
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
        return (interaction, manager, social);
    }

    [Fact]
    public void Accept_RaisesBoardConfidence_AndRecordsManagerMemory()
    {
        var (interaction, manager, social) = Create();
        var before = manager.Store.Career.ActiveEmployment!.BoardConfidence.Value;
        var request = interaction.Decisions.OpenBoardDemandRequest(Day);
        Assert.Equal(DecisionRequestKind.BoardDemandRequest, request.Kind);
        Assert.Equal(DecisionRequest.BoardDemandParticipantPlayerId, request.SubjectPlayerId);

        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionAcceptBoardDemand,
            Day);

        Assert.Equal(before + 8, manager.Store.Career.ActiveEmployment!.BoardConfidence.Value);
        Assert.Equal("BoardDemandAccepted", manager.Store.Career.ActiveEmployment.LastAssessmentReasonCode);
        Assert.Equal(
            MemoryValence.Positive,
            Assert.Single(
                social.MemoryStore.Memories,
                m => m.RuleId == MemoryRecord.DecisionBoardDemandAnswerRuleId).Valence);
        Assert.Empty(social.RelationshipStore.Relationships);
    }

    [Fact]
    public void Refuse_LowersBoardConfidence()
    {
        var (interaction, manager, _) = Create();
        var before = manager.Store.Career.ActiveEmployment!.BoardConfidence.Value;
        var request = interaction.Decisions.OpenBoardDemandRequest(Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionRefuse,
            Day);

        Assert.Equal(before - 12, manager.Store.Career.ActiveEmployment!.BoardConfidence.Value);
        Assert.Equal("BoardDemandRefused", manager.Store.Career.ActiveEmployment.LastAssessmentReasonCode);
    }

    [Fact]
    public void Counter_SlightlyLowersBoardConfidence()
    {
        var (interaction, manager, _) = Create();
        var before = manager.Store.Career.ActiveEmployment!.BoardConfidence.Value;
        var request = interaction.Decisions.OpenBoardDemandRequest(Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionCounterBoardDemand,
            Day);

        Assert.Equal(before - 4, manager.Store.Career.ActiveEmployment!.BoardConfidence.Value);
        Assert.Equal("BoardDemandCountered", manager.Store.Career.ActiveEmployment.LastAssessmentReasonCode);
    }

    [Fact]
    public void Open_SecondBoardDemandForSameClub_IsRejected()
    {
        var (interaction, _, _) = Create();
        interaction.Decisions.OpenBoardDemandRequest(Day);
        Assert.Throws<InteractionInvariantViolationException>(() =>
            interaction.Decisions.OpenBoardDemandRequest(Day));
    }

    [Fact]
    public void DialogueOptions_IncludeAcceptCounterRefuse()
    {
        var (interaction, _, _) = Create();
        var request = interaction.Decisions.OpenBoardDemandRequest(Day);
        var codes = interaction.DialogueOptions.GetForDecision(request.DecisionRequestId)
            .Options
            .Select(o => o.OptionCode)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                DecisionRequest.OptionAcceptBoardDemand,
                DecisionRequest.OptionCounterBoardDemand,
                DecisionRequest.OptionRefuse,
            }.OrderBy(c => c, StringComparer.Ordinal),
            codes);
    }
}
