using FootballCareerSimulator.Application.EventRuleEvaluation.Infrastructure;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Application.Interaction.Infrastructure;
using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Application.SocialContinuity.Infrastructure;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class DecisionRequestStructuredCausalityTests
{
    private static readonly GameDate Day = GameDate.FromCalendarDate(2026, 9, 1);

    [Fact]
    public void ExplainStructuredCausality_BrokenPromiseAndLowTrust_ProducesRichStructuredExplanation()
    {
        var requestStore = new InMemoryDecisionRequestStore();
        var relationshipStore = new InMemoryRelationshipStore();
        var promiseStore = new InMemoryPromiseStore();
        var memoryStore = new InMemoryMemoryStore();
        var causalityStore = new InMemoryCausalityStore();
        var causalityJournal = new CausalityJournalQueryService(causalityStore);

        var queryService = new DecisionRequestQueryService(
            requestStore,
            relationshipStore,
            promiseStore,
            memoryStore,
            null,
            causalityJournal);

        var playerId = new PlayerId(101);
        var managerId = new ManagerId(1);
        var clubId = new ClubId(1);

        var createdDay = GameDate.FromCalendarDate(2026, 8, 1);
        var brokenDay = GameDate.FromCalendarDate(2026, 8, 20);

        // 1. Setup broken promise via Rehydrate
        var brokenPromise = Promise.Rehydrate(
            new PromiseId(99),
            PromiseKind.StartingOpportunity,
            new ActorRef(ActorKind.Manager, managerId.Value),
            new ActorRef(ActorKind.Player, playerId.Value),
            clubId,
            targetStarts: 3,
            startsGiven: 0,
            deadlineOn: brokenDay,
            createdOn: createdDay,
            status: PromiseStatus.Broken,
            terminalOn: brokenDay,
            countedFixtureIds: Array.Empty<long>());
        promiseStore.Upsert(brokenPromise);

        // 2. Setup Low Trust relationship via Rehydrate
        var relationship = RelationshipRecord.Rehydrate(
            new RelationshipId(1),
            new ActorRef(ActorKind.Player, playerId.Value),
            new ActorRef(ActorKind.Manager, managerId.Value),
            trust: 20, // Low trust band (<= 33)
            respect: 50,
            professionalCompatibility: 50,
            status: RelationshipStatus.Active,
            createdOn: createdDay,
            lastChangedOn: brokenDay,
            lastChangeReasonCode: "PromiseBrokenTrust",
            processedEffectKeys: Array.Empty<string>());
        relationshipStore.Upsert(relationship);

        // 3. Open transfer request
        var decisionRequest = DecisionRequest.OpenTransferRequest(
            new DecisionRequestId(42),
            managerId,
            playerId,
            clubId,
            Day,
            Day.AddDays(3),
            isHardBlocker: true);
        requestStore.Upsert(decisionRequest);

        // Act
        var explanation = queryService.ExplainStructuredCausality(decisionRequest.DecisionRequestId);

        // Assert
        Assert.NotNull(explanation);
        Assert.Equal(CausalityDimension.TransferResult, explanation.Dimension);
        Assert.Contains("tutulmaması", explanation.PrimaryCause);
        Assert.NotEmpty(explanation.SupportingCauses);
        Assert.Contains("güvenin düşük", explanation.SupportingCauses[0]);
        Assert.Equal("Yüksek", explanation.Confidence);

        // Verify it was recorded in the causality journal store
        var storedRecord = causalityStore.FindByTargetEventId(explanation.TargetEventId);
        Assert.NotNull(storedRecord);
        Assert.Equal(CausalityDimension.TransferResult, storedRecord.Dimension);
    }
}
