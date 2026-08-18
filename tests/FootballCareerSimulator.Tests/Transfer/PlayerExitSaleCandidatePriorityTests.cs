using FootballCareerSimulator.Application.ContractRegistration.Infrastructure;
using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Queries;
using FootballCareerSimulator.Application.Transfer.Services;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Transfer;

public sealed class PlayerExitSaleCandidatePriorityTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 8, 1);

    [Fact]
    public void TryParsePlayerExitPlayerId_ReadsReasonCode()
    {
        var code = TransferNeed.BuildPlayerExitReasonCode(new PlayerId(501));
        Assert.True(TransferNeed.TryParsePlayerExitPlayerId(code, out var parsed));
        Assert.Equal(501, parsed);
        Assert.False(TransferNeed.TryParsePlayerExitPlayerId("SquadDepth", out _));
    }

    [Fact]
    public void AcknowledgeTransfer_PreferredSaleCandidate_IsExitPlayer_AndDeskPointsToSell()
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
            transferNeeds: needs,
            relationshipStore: social.RelationshipStore,
            memoryStore: social.MemoryStore);

        // Forma Sözü: iki kırılma → Low Trust TransferRequest
        social.PlayingTime.Create(
            manager.Store.Career.ManagerId,
            new PlayerId(501),
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(2),
            createdOn: Day);
        social.StartingOpportunity.EvaluateDeadlines(Day.AddDays(2));
        social.PlayingTime.Create(
            manager.Store.Career.ManagerId,
            new PlayerId(501),
            new ClubId(1),
            targetAppearances: 2,
            deadlineOn: Day.AddDays(8),
            createdOn: Day.AddDays(3));
        social.StartingOpportunity.EvaluateDeadlines(
            Day.AddDays(8),
            promise => interaction.PromiseBroken.TryOpenAfterBroken(promise, Day.AddDays(8)));

        var transfer = Assert.Single(
            interaction.DecisionRequestStore.Requests,
            r => r.IsOpen && r.Kind == DecisionRequestKind.TransferRequest);
        interaction.Decisions.Answer(
            transfer.DecisionRequestId,
            DecisionRequest.OptionAcknowledgeTransferRequest,
            Day.AddDays(8));

        var queries = new TransferNeedQueryService(
            needStore,
            new InMemoryShortlistStore(),
            new InMemoryTransferTargetStore(),
            new InMemoryTransferProcessStore(),
            new InMemoryClubOfferStore(),
            new InMemoryPlayerContractProposalStore(),
            manager.Store);

        Assert.Equal(501, queries.GetPreferredPlayerExitSaleCandidateId());
        Assert.True(needs.HasOpenPlayerExitRequest(new ClubId(1), new PlayerId(501)));

        var desk = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            windowClosesOnDayNumber: 90,
            openNeedCount: 1,
            openExitNeedCount: 1,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: queries.GetPreferredPlayerExitSaleCandidateId(),
            currentDayNumber: Day.AddDays(8).DayNumber);

        Assert.True(desk.DemandsAttention);
        Assert.Equal(TransferNextStep.ReasonSellFringe, desk.NextStep!.ReasonCode);
        Assert.Contains("#501", desk.NextStep.ButtonLabel, StringComparison.Ordinal);
        Assert.Contains("Ayrılma listesi", desk.Headline, StringComparison.Ordinal);
    }
}
