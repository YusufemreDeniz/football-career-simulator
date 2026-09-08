using FootballCareerSimulator.Application.SocialContinuity.Queries;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class MemoryResolutionLifecycleTests
{
    private static readonly GameDate Day1 = GameDate.FromCalendarDate(2026, 8, 1);
    private static readonly GameDate Day10 = GameDate.FromCalendarDate(2026, 8, 10);
    private static readonly GameDate Day20 = GameDate.FromCalendarDate(2026, 8, 20);

    [Fact]
    public void MemoryRecord_InitialState_IsUnresolved()
    {
        var memory = MemoryRecord.CreateSelectionOmitted(
            new MemoryId(1),
            new ActorRef(ActorKind.Player, 101),
            new FixtureId(501),
            Day1);

        Assert.Equal(MemoryResolutionStatus.Unresolved, memory.ResolutionStatus);
        Assert.Null(memory.ResolvedOn);
        Assert.Null(memory.ResolutionReasonCode);
        Assert.Equal(45, memory.CurrentInfluence);
        Assert.Equal(MemoryStatus.Active, memory.Status);
    }

    [Fact]
    public void MarkResolution_Resolved_ZeroesInfluenceAndMarksDormant()
    {
        var memory = MemoryRecord.CreateSelectionOmitted(
            new MemoryId(1),
            new ActorRef(ActorKind.Player, 101),
            new FixtureId(501),
            Day1);

        var resolved = memory.MarkResolution(
            MemoryResolutionStatus.Resolved,
            Day10,
            "Telafi edildi");

        Assert.Equal(MemoryResolutionStatus.Resolved, resolved.ResolutionStatus);
        Assert.Equal(0, resolved.CurrentInfluence);
        Assert.Equal(MemoryStatus.Dormant, resolved.Status);
        Assert.Equal(Day10, resolved.ResolvedOn);
        Assert.Equal("Telafi edildi", resolved.ResolutionReasonCode);
    }

    [Fact]
    public void MarkResolution_PartiallyResolved_ReducesInfluenceByHalf()
    {
        var memory = MemoryRecord.CreateSelectionOmitted(
            new MemoryId(1),
            new ActorRef(ActorKind.Player, 101),
            new FixtureId(501),
            Day1);

        var partially = memory.MarkResolution(
            MemoryResolutionStatus.PartiallyResolved,
            Day10,
            "Kısmen çözüldü");

        Assert.Equal(MemoryResolutionStatus.PartiallyResolved, partially.ResolutionStatus);
        Assert.Equal(22, partially.CurrentInfluence); // 45 / 2
        Assert.Equal(MemoryStatus.Active, partially.Status);
    }

    [Fact]
    public void MarkResolution_Contradicted_ReducesInfluenceTo70Percent()
    {
        var memory = MemoryRecord.CreateSelectionOmitted(
            new MemoryId(1),
            new ActorRef(ActorKind.Player, 101),
            new FixtureId(501),
            Day1);

        var contradicted = memory.MarkResolution(
            MemoryResolutionStatus.Contradicted,
            Day10,
            "Sorgulandı");

        Assert.Equal(MemoryResolutionStatus.Contradicted, contradicted.ResolutionStatus);
        Assert.Equal(31, contradicted.CurrentInfluence); // 45 * 0.7 = 31
    }

    [Fact]
    public void MarkResolution_Reopened_DoublesInfluenceAndBecomesActive()
    {
        var memory = MemoryRecord.CreateSelectionOmitted(
            new MemoryId(1),
            new ActorRef(ActorKind.Player, 101),
            new FixtureId(501),
            Day1);

        var partially = memory.MarkResolution(
            MemoryResolutionStatus.PartiallyResolved,
            Day10,
            "Kısmi çözüm");

        var reopened = partially.MarkResolution(
            MemoryResolutionStatus.Reopened,
            Day20,
            "Yara tekrar açıldı");

        Assert.Equal(MemoryResolutionStatus.Reopened, reopened.ResolutionStatus);
        Assert.Equal(MemoryStatus.Active, reopened.Status);
        Assert.Equal(45, reopened.CurrentInfluence); // Math.Max(BaseImportance 45, 22 * 2 = 44) => 45
    }

    [Fact]
    public void MemoryResolutionEvaluator_CrucialMatchSelection_ResolvesMemory()
    {
        var evaluator = new MemoryResolutionEvaluator();
        var memory = MemoryRecord.CreateSelectionOmitted(
            new MemoryId(1),
            new ActorRef(ActorKind.Player, 101),
            new FixtureId(501),
            Day1);

        var resolved = evaluator.EvaluateSelectionRedemption(
            memory,
            isStartingAppearance: true,
            isCrucialMatch: true,
            Day10);

        Assert.Equal(MemoryResolutionStatus.Resolved, resolved.ResolutionStatus);
        Assert.Equal(0, resolved.CurrentInfluence);
        Assert.Contains("Kritik", resolved.ResolutionReasonCode);
    }

    [Fact]
    public void MemoryResolutionEvaluator_BuildDigest_TransformsCorrectly()
    {
        var evaluator = new MemoryResolutionEvaluator();
        var memory1 = MemoryRecord.CreateSelectionOmitted(
            new MemoryId(1),
            new ActorRef(ActorKind.Player, 101),
            new FixtureId(501),
            Day1);

        var memory2 = MemoryRecord.CreateSelectionOmitted(
            new MemoryId(2),
            new ActorRef(ActorKind.Player, 101),
            new FixtureId(502),
            Day1).MarkResolution(MemoryResolutionStatus.Resolved, Day10, "Çözüldü");

        var digest = evaluator.BuildDigest(101, new[] { memory1, memory2 });

        Assert.Equal(101, digest.ActorId);
        Assert.Equal(1, digest.ActiveUnresolvedCount);
        Assert.Equal(1, digest.ResolvedCount);
        Assert.Equal(2, digest.Memories.Count);
        Assert.False(digest.Memories[0].IsRedeemed);
        Assert.True(digest.Memories[1].IsRedeemed);
    }

    [Fact]
    public void MemoryRecord_Rehydrate_WithResolutionFields_PreservesValues()
    {
        var rehydrated = MemoryRecord.Rehydrate(
            new MemoryId(99),
            new ActorRef(ActorKind.Player, 50),
            MemorySubjectKind.Fixture,
            123,
            "SourceKey:Test",
            MemoryCategory.Selection,
            Day1,
            Day1,
            baseImportance: 50,
            currentInfluence: 0,
            MemoryValence.Negative,
            MemoryVisibility.Private,
            MemoryStatus.Dormant,
            reinforcementCount: 1,
            relatedPromiseId: null,
            ruleId: "CustomRule",
            ruleVersion: 1,
            processedReinforcementKeys: null,
            resolutionStatus: MemoryResolutionStatus.Resolved,
            resolvedOn: Day10,
            resolutionReasonCode: "Affedildi");

        Assert.Equal(MemoryResolutionStatus.Resolved, rehydrated.ResolutionStatus);
        Assert.Equal(Day10, rehydrated.ResolvedOn);
        Assert.Equal("Affedildi", rehydrated.ResolutionReasonCode);
        Assert.Equal(0, rehydrated.CurrentInfluence);
    }
}
