using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Tests.EventRuleEvaluation;

public sealed class CausalityRecordTests
{
    [Fact]
    public void CausalityRecord_ValidFactors_CorrectlySegregatesFactorWeights()
    {
        var targetEventId = Guid.NewGuid();
        var playerActor = new ActorRef(ActorKind.Player, 101);

        var primary = new CausalFactor(
            "PromiseBroken",
            CausalFactorWeight.Primary,
            "Forma sözünün tutulmaması",
            playerActor,
            -15,
            120);

        var contributing = new CausalFactor(
            "PreviousSittingOut",
            CausalFactorWeight.Contributing,
            "Son maçlarda yedek kalması",
            playerActor,
            -5,
            115);

        var mitigating = new CausalFactor(
            "PublicSupport",
            CausalFactorWeight.Mitigating,
            "Basında oyuncuyu desteklemeniz",
            playerActor,
            +4,
            118);

        var contextual = new CausalFactor(
            "HighPressureFixture",
            CausalFactorWeight.Contextual,
            "Derbi maçı atmosferi",
            null,
            null,
            120);

        var record = new CausalityRecord(
            targetEventId,
            CausalityDimension.RelationshipDelta,
            "Relationship.Trust.Drop.BrokenPromise",
            120,
            new[] { primary, contributing, mitigating, contextual });

        Assert.Equal(targetEventId, record.TargetEventId);
        Assert.Equal(CausalityDimension.RelationshipDelta, record.Dimension);
        Assert.Equal("Relationship.Trust.Drop.BrokenPromise", record.ExplanationKey);
        Assert.Equal(120, record.EvaluatedAtDayNumber);
        Assert.Equal(4, record.Factors.Count);

        Assert.NotNull(record.PrimaryCause);
        Assert.Equal("PromiseBroken", record.PrimaryCause.FactorCode);

        Assert.Single(record.ContributingCauses);
        Assert.Equal("PreviousSittingOut", record.ContributingCauses[0].FactorCode);

        Assert.Single(record.MitigatingCauses);
        Assert.Equal("PublicSupport", record.MitigatingCauses[0].FactorCode);

        Assert.Single(record.ContextualCauses);
        Assert.Equal("HighPressureFixture", record.ContextualCauses[0].FactorCode);
    }

    [Fact]
    public void CausalityRecord_EmptyTargetEventId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new CausalityRecord(
            Guid.Empty,
            CausalityDimension.PromiseOutcome,
            "SomeKey",
            100,
            Array.Empty<CausalFactor>()));
    }

    [Fact]
    public void CausalityRecord_EmptyExplanationKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new CausalityRecord(
            Guid.NewGuid(),
            CausalityDimension.PromiseOutcome,
            "   ",
            100,
            Array.Empty<CausalFactor>()));
    }
}
