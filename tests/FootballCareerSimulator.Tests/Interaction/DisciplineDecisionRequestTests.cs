using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Domain.Discipline;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class DisciplineDecisionRequestTests
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
    public void Warning_CreatesDisciplinaryAction_AndAdjustsDimensions()
    {
        var (interaction, social) = Create();
        var request = interaction.Decisions.OpenDisciplineRequest(new PlayerId(60), Day);
        Assert.Equal(DecisionRequestKind.DisciplineRequest, request.Kind);

        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionIssueWarning,
            Day);

        var action = Assert.Single(interaction.DisciplinaryActionStore.Actions);
        Assert.Equal(DisciplinaryActionKind.Warning, action.Kind);
        var relationship = social.RelationshipStore.FindPlayerToManager(60, 1)!;
        Assert.Equal(48, relationship.Trust);
        Assert.Equal(54, relationship.Respect);
        Assert.Equal(
            MemoryValence.Negative,
            Assert.Single(
                social.MemoryStore.Memories,
                m => m.RuleId == MemoryRecord.DecisionDisciplineAnswerRuleId).Valence);
    }

    [Fact]
    public void Fine_RequiresPriorWarning()
    {
        var (interaction, _) = Create();
        var request = interaction.Decisions.OpenDisciplineRequest(new PlayerId(61), Day);
        var fine = Assert.Single(
            interaction.DialogueOptions.GetForDecision(request.DecisionRequestId).Options,
            o => o.OptionCode == DecisionRequest.OptionIssueFine);
        Assert.False(fine.IsEligible);

        Assert.Throws<InteractionInvariantViolationException>(() =>
            interaction.Decisions.Answer(
                request.DecisionRequestId,
                DecisionRequest.OptionIssueFine,
                Day));
    }

    [Fact]
    public void Fine_AfterWarning_AppliesAndAdjustsCompatibility()
    {
        var (interaction, social) = Create();
        interaction.Discipline.Apply(
            DisciplinaryActionKind.Warning,
            new Domain.ManagerCareer.ManagerId(1),
            new PlayerId(62),
            new Domain.Shared.ClubId(1),
            Day);

        var request = interaction.Decisions.OpenDisciplineRequest(new PlayerId(62), Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionIssueFine,
            Day);

        Assert.Equal(2, interaction.DisciplinaryActionStore.Actions.Count);
        Assert.Contains(
            interaction.DisciplinaryActionStore.Actions,
            a => a.Kind == DisciplinaryActionKind.Fine);
        var relationship = social.RelationshipStore.FindPlayerToManager(62, 1)!;
        Assert.Equal(44, relationship.Trust);
        Assert.Equal(56, relationship.Respect);
        Assert.Equal(48, relationship.ProfessionalCompatibility);
    }

    [Fact]
    public void Support_RaisesTrust()
    {
        var (interaction, social) = Create();
        var request = interaction.Decisions.OpenDisciplineRequest(new PlayerId(63), Day);
        interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionOfferSupport,
            Day);

        Assert.Equal(DisciplinaryActionKind.Support, interaction.DisciplinaryActionStore.Actions.Single().Kind);
        Assert.Equal(56, social.RelationshipStore.FindPlayerToManager(63, 1)!.Trust);
        Assert.Equal(48, social.RelationshipStore.FindPlayerToManager(63, 1)!.Respect);
    }
}
