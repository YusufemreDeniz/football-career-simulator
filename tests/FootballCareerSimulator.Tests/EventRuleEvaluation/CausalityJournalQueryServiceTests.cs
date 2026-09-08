using FootballCareerSimulator.Application.EventRuleEvaluation.Infrastructure;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Tests.EventRuleEvaluation;

public sealed class CausalityJournalQueryServiceTests
{
    [Fact]
    public void ExplainForPlayer_RelationshipDelta_ProducesCorrectSemanticHierarchy()
    {
        var store = new InMemoryCausalityStore();
        var service = new CausalityJournalQueryService(store);

        var targetEventId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var playerRef = new ActorRef(ActorKind.Player, 501);

        var primary = new CausalFactor(
            "PromiseBroken",
            CausalFactorWeight.Primary,
            "Verilen forma sözünün tutulmaması",
            playerRef,
            -15,
            120);

        var supporting = new CausalFactor(
            "PreviousSittingOut",
            CausalFactorWeight.Contributing,
            "Son 3 maçta kadro dışı kalması",
            playerRef,
            -5,
            118);

        var mitigating = new CausalFactor(
            "PublicSupport",
            CausalFactorWeight.Mitigating,
            "Basında oyuncuyu desteklemeniz",
            playerRef,
            +3,
            119);

        var contextual = new CausalFactor(
            "FormerDisputeHistory",
            CausalFactorWeight.Contextual,
            "Geçmiş sezonda benzer bir güven krizi yaşanmış olması",
            playerRef,
            null,
            100);

        var record = new CausalityRecord(
            targetEventId,
            CausalityDimension.RelationshipDelta,
            "Relationship.Trust.Drop.BrokenPromise",
            120,
            new[] { primary, supporting, mitigating, contextual });

        service.RecordCausality(record, correlationId, "Rule.Social.TrustDrop");

        var explanation = service.ExplainForPlayer(targetEventId);
        Assert.NotNull(explanation);
        Assert.Equal(targetEventId, explanation.TargetEventId);
        Assert.Equal(CausalityDimension.RelationshipDelta, explanation.Dimension);
        Assert.Equal("İlişki / Güven Değişimi", explanation.Title);
        Assert.Equal("Verilen forma sözünün tutulmaması", explanation.PrimaryCause);
        Assert.Single(explanation.SupportingCauses);
        Assert.Equal("Son 3 maçta kadro dışı kalması", explanation.SupportingCauses[0]);
        Assert.Single(explanation.MitigatingCauses);
        Assert.Equal("Basında oyuncuyu desteklemeniz", explanation.MitigatingCauses[0]);
        Assert.Equal("Geçmiş sezonda benzer bir güven krizi yaşanmış olması", explanation.HistoricalContext);
        Assert.Equal("Yüksek", explanation.Confidence);
        Assert.Contains("Player #501", explanation.RelatedActors);

        // Technical diagnostic trace verification (Dual Projection separation)
        var trace = service.GetDiagnosticTrace(targetEventId);
        Assert.NotNull(trace);
        Assert.Equal("Rule.Social.TrustDrop", trace.RuleId);
        Assert.Equal(correlationId, trace.CorrelationId);
        Assert.Equal(4, trace.FactorCodes.Count);
        Assert.Contains("PromiseBroken", trace.FactorCodes);
    }

    [Fact]
    public void ExplainForPlayer_PromiseOutcome_ProducesSemanticExplanation()
    {
        var store = new InMemoryCausalityStore();
        var service = new CausalityJournalQueryService(store);

        var targetEventId = Guid.NewGuid();
        var playerRef = new ActorRef(ActorKind.Player, 502);

        var primary = new CausalFactor(
            "AppearancesTargetMet",
            CausalFactorWeight.Primary,
            "Taahhüt edilen 5 maçta ilk 11'de sahaya çıkma şartı tamamlandı",
            playerRef,
            null,
            140);

        var record = new CausalityRecord(
            targetEventId,
            CausalityDimension.PromiseOutcome,
            "Promise.StartingOpportunity.Fulfilled",
            140,
            new[] { primary });

        service.RecordCausality(record, null, "Rule.Promise.Fulfill");

        var explanation = service.ExplainForPlayer(targetEventId);
        Assert.NotNull(explanation);
        Assert.Equal(CausalityDimension.PromiseOutcome, explanation.Dimension);
        Assert.Equal("Söz Sonucu", explanation.Title);
        Assert.Equal("Taahhüt edilen 5 maçta ilk 11'de sahaya çıkma şartı tamamlandı", explanation.PrimaryCause);
    }

    [Fact]
    public void ExplainForPlayer_TransferOutcome_ProducesSemanticExplanation()
    {
        var store = new InMemoryCausalityStore();
        var service = new CausalityJournalQueryService(store);

        var targetEventId = Guid.NewGuid();
        var playerRef = new ActorRef(ActorKind.Player, 601);

        var primary = new CausalFactor(
            "WageProposalRejected",
            CausalFactorWeight.Primary,
            "Oyuncu menajerinin talep ettiği yıllık ücret ile kulüp bütçesi uzlaşamadı",
            playerRef,
            null,
            75);

        var supporting = new CausalFactor(
            "NegotiationRoundsExhausted",
            CausalFactorWeight.Contributing,
            "Görüşmelerde azami tur sayısına ulaşıldı",
            playerRef,
            null,
            75);

        var record = new CausalityRecord(
            targetEventId,
            CausalityDimension.TransferResult,
            "Transfer.Failed.WageDispute",
            75,
            new[] { primary, supporting });

        service.RecordCausality(record, null, "Rule.Transfer.Breakdown");

        var explanation = service.ExplainForPlayer(targetEventId);
        Assert.NotNull(explanation);
        Assert.Equal(CausalityDimension.TransferResult, explanation.Dimension);
        Assert.Equal("Transfer Sonucu", explanation.Title);
        Assert.Equal("Oyuncu menajerinin talep ettiği yıllık ücret ile kulüp bütçesi uzlaşamadı", explanation.PrimaryCause);
        Assert.Single(explanation.SupportingCauses);
        Assert.Equal("Görüşmelerde azami tur sayısına ulaşıldı", explanation.SupportingCauses[0]);
    }

    [Fact]
    public void ExplainForPlayer_BoardConfidenceDelta_ProducesSemanticExplanation()
    {
        var store = new InMemoryCausalityStore();
        var service = new CausalityJournalQueryService(store);

        var targetEventId = Guid.NewGuid();
        var primary = new CausalFactor(
            "DerbyLossStreak",
            CausalFactorWeight.Primary,
            "Lig derbisinde alınan farklı mağlubiyet ve hedefin gerisine düşülmesi",
            null,
            -12,
            160);

        var mitigating = new CausalFactor(
            "StableFinances",
            CausalFactorWeight.Mitigating,
            "Kulüp bütçesinin ve mali disiplinin hedeflenen seviyede seyretmesi",
            null,
            +3,
            160);

        var record = new CausalityRecord(
            targetEventId,
            CausalityDimension.BoardConfidenceDelta,
            "Board.Confidence.Drop.PerformanceCrisis",
            160,
            new[] { primary, mitigating });

        service.RecordCausality(record, null, "Rule.Board.Assessment");

        var explanation = service.ExplainForPlayer(targetEventId);
        Assert.NotNull(explanation);
        Assert.Equal(CausalityDimension.BoardConfidenceDelta, explanation.Dimension);
        Assert.Equal("Yönetim Güveni Değişimi", explanation.Title);
        Assert.Equal("Lig derbisinde alınan farklı mağlubiyet ve hedefin gerisine düşülmesi", explanation.PrimaryCause);
        Assert.Single(explanation.MitigatingCauses);
        Assert.Equal("Kulüp bütçesinin ve mali disiplinin hedeflenen seviyede seyretmesi", explanation.MitigatingCauses[0]);
    }

    [Fact]
    public void InMemoryCausalityStore_ExceedingCapacity_EvictsOldestRecordsSafely()
    {
        var store = new InMemoryCausalityStore();
        var firstId = Guid.NewGuid();

        var firstRecord = new CausalityRecord(
            firstId,
            CausalityDimension.RelationshipDelta,
            "Key.First",
            1,
            new[] { new CausalFactor("F1", CausalFactorWeight.Primary, "First cause", null, null, 1) });
        store.Add(firstRecord);

        // Fill up to capacity
        for (var i = 1; i < InMemoryCausalityStore.MaxBoundedCapacity; i++)
        {
            var nextId = Guid.NewGuid();
            store.Add(new CausalityRecord(
                nextId,
                CausalityDimension.RelationshipDelta,
                $"Key.{i}",
                i + 1,
                new[] { new CausalFactor("F", CausalFactorWeight.Primary, $"Cause {i}", null, null, i + 1) }));
        }

        // First item should still exist at capacity
        Assert.NotNull(store.FindByTargetEventId(firstId));

        // Add one more to trigger eviction
        var overflowId = Guid.NewGuid();
        store.Add(new CausalityRecord(
            overflowId,
            CausalityDimension.RelationshipDelta,
            "Key.Overflow",
            9999,
            new[] { new CausalFactor("F", CausalFactorWeight.Primary, "Overflow cause", null, null, 9999) }));

        // Oldest item should now be evicted
        Assert.Null(store.FindByTargetEventId(firstId));
        // New item should exist
        Assert.NotNull(store.FindByTargetEventId(overflowId));
    }
}
