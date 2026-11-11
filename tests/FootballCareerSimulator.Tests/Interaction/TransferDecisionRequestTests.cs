using FootballCareerSimulator.Application.ContractRegistration.Infrastructure;
using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Application.Transfer.Services;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class TransferDecisionRequestTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    private sealed record Fixture(
        InteractionModule Interaction,
        SocialContinuityModule Social,
        ITransferNeedStore NeedStore,
        TransferNeedService Needs);

    private static Fixture Create()
    {
        var manager = ManagerCareerModule.CreateNewCareer(Day, startingClubId: 1);
        var social = SocialContinuityModule.Create();
        var needStore = new InMemoryTransferNeedStore();
        var needs = new TransferNeedService(
            needStore,
            new InMemoryContractStore(),
            new InMemoryClubSquadStore());
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore,
            startingOpportunity: social.StartingOpportunity,
            transferNeeds: needs);
        return new Fixture(interaction, social, needStore, needs);
    }

    [Fact]
    public void Acknowledge_CreatesPlayerExitTransferNeed_AndRaisesTrust()
    {
        var fx = Create();
        var request = fx.Interaction.Decisions.OpenTransferRequest(new PlayerId(50), Day);
        Assert.Equal(DecisionRequestKind.TransferRequest, request.Kind);
        Assert.Equal(
            DialogueSession.TransferRequestType,
            Assert.Single(fx.Interaction.DialogueSessionStore.Sessions).DialogueTypeCode);

        fx.Interaction.Decisions.Answer(
            request.DecisionRequestId,
            DecisionRequest.OptionAcknowledgeTransferRequest,
            Day);

        var need = Assert.Single(fx.NeedStore.Needs);
        Assert.Equal(TransferNeedKind.PlayerExitRequest, need.Kind);
        Assert.Equal(TransferNeed.BuildPlayerExitReasonCode(new PlayerId(50)), need.ReasonCode);
        Assert.Equal(56, fx.Social.RelationshipStore.FindPlayerToManager(50, 1)!.Trust);
        Assert.Equal(
            MemoryValence.Positive,
            Assert.Single(
                fx.Social.MemoryStore.Memories,
                m => m.RuleId == MemoryRecord.DecisionTransferAnswerRuleId).Valence);
        Assert.Equal(
            DialogueSessionStatus.Resolved,
            fx.Interaction.DialogueSessionStore.Sessions.Single().Status);
    }

    [Fact]
    public void Acknowledge_Ineligible_WhenOpenPlayerExitNeedExists()
    {
        var fx = Create();
        fx.Needs.DeclarePlayerExitRequest(new Domain.Shared.ClubId(1), new PlayerId(51), Day);

        var request = fx.Interaction.Decisions.OpenTransferRequest(new PlayerId(51), Day);
        var acknowledge = Assert.Single(
            fx.Interaction.DialogueOptions.GetForDecision(request.DecisionRequestId).Options,
            o => o.OptionCode == DecisionRequest.OptionAcknowledgeTransferRequest);
        Assert.False(acknowledge.IsEligible);

        Assert.Throws<InteractionInvariantViolationException>(() =>
            fx.Interaction.Decisions.Answer(
                request.DecisionRequestId,
                DecisionRequest.OptionAcknowledgeTransferRequest,
                Day));
    }

    [Fact]
    public void Refuse_LowersTrust_WithoutTransferNeed()
    {
        var fx = Create();
        var request = fx.Interaction.Decisions.OpenTransferRequest(new PlayerId(52), Day);
        fx.Interaction.Decisions.Answer(request.DecisionRequestId, DecisionRequest.OptionRefuse, Day);

        Assert.Empty(fx.NeedStore.Needs);
        Assert.Equal(40, fx.Social.RelationshipStore.FindPlayerToManager(52, 1)!.Trust);
        Assert.Equal(
            "DecisionTransferRefused",
            fx.Social.RelationshipStore.FindPlayerToManager(52, 1)!.LastChangeReasonCode);
    }
}
