using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class AlternativePlanPrescriptionDigestTests
{
    [Fact]
    public void NoRepeatedWarning_DoesNotPrescribeAChange()
    {
        var digest = AlternativePlanPrescriptionDigest.Compose(
            Plan(),
            RepeatedPatternWarningDigest.Clear());

        Assert.False(digest.HasPrescription);
        Assert.Null(digest.SuggestedFormation);
        Assert.Null(digest.SuggestedApproach);
        Assert.Empty(digest.PrescriptionLine);
    }

    [Theory]
    [InlineData(OpponentThreatKind.Neutral, Formation.F433, TacticalApproach.Balanced)]
    [InlineData(OpponentThreatKind.WinningStreak, Formation.F442, TacticalApproach.Balanced)]
    [InlineData(OpponentThreatKind.ProductiveAttack, Formation.F442, TacticalApproach.Defensive)]
    [InlineData(OpponentThreatKind.SquadQuality, Formation.F433, TacticalApproach.Defensive)]
    [InlineData(OpponentThreatKind.TopZoneTempo, Formation.F442, TacticalApproach.Balanced)]
    [InlineData(OpponentThreatKind.DefensiveResistance, Formation.F433, TacticalApproach.Attacking)]
    [InlineData(OpponentThreatKind.LosingStreak, Formation.F433, TacticalApproach.Attacking)]
    public void RepeatedWarning_ProducesThreatAwareConcreteTactic(
        OpponentThreatKind threat,
        Formation expectedFormation,
        TacticalApproach expectedApproach)
    {
        var digest = AlternativePlanPrescriptionDigest.Compose(
            Plan(threat),
            Warning());

        Assert.True(digest.HasPrescription);
        Assert.Equal(expectedFormation, digest.SuggestedFormation);
        Assert.Equal(expectedApproach, digest.SuggestedApproach);
        Assert.StartsWith("Reçete:", digest.PrescriptionLine, StringComparison.Ordinal);
        Assert.Contains(
            MatchupPlanDigest.FormatFormationLabel(expectedFormation),
            digest.PrescriptionLine,
            StringComparison.Ordinal);
        Assert.Contains(
            MatchupPlanDigest.FormatApproachLabel(expectedApproach),
            digest.PrescriptionLine,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PrimaryRecommendationMatchesCurrentPlan_UsesDifferentFallback()
    {
        var digest = AlternativePlanPrescriptionDigest.Compose(
            Plan(
                OpponentThreatKind.ProductiveAttack,
                Formation.F442,
                TacticalApproach.Defensive),
            Warning());

        Assert.Equal(Formation.F433, digest.SuggestedFormation);
        Assert.Equal(TacticalApproach.Balanced, digest.SuggestedApproach);
        Assert.Contains("geçiş riskini azalt", digest.PrescriptionLine, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryThreatAndCurrentTactic_ProducesAnActuallyDifferentPlan()
    {
        foreach (var threat in Enum.GetValues<OpponentThreatKind>())
        {
            foreach (var formation in Enum.GetValues<Formation>())
            {
                foreach (var approach in Enum.GetValues<TacticalApproach>())
                {
                    var digest = AlternativePlanPrescriptionDigest.Compose(
                        Plan(threat, formation, approach),
                        Warning());

                    Assert.True(digest.HasPrescription);
                    Assert.False(
                        digest.SuggestedFormation == formation
                        && digest.SuggestedApproach == approach);
                }
            }
        }
    }

    private static MatchupPlanDigest Plan(
        OpponentThreatKind threat = OpponentThreatKind.ProductiveAttack,
        Formation formation = Formation.F352,
        TacticalApproach approach = TacticalApproach.Attacking) =>
        new(
            MatchupPlanDigest.Brand,
            $"Seçim: {MatchupPlanDigest.FormatFormationLabel(formation)}"
                + $" · {MatchupPlanDigest.FormatApproachLabel(approach)}",
            "Geçmişte uyarı veren plan.",
            MatchupPlanSignal.Risk,
            threat,
            formation,
            approach);

    private static RepeatedPatternWarningDigest Warning() =>
        new(true, RepeatedPatternWarningDigest.Brand, "Desen tekrarlandı.", "Koç uyarısı.", 1);
}
