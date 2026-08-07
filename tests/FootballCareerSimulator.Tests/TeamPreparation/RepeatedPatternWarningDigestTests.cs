using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class RepeatedPatternWarningDigestTests
{
    [Fact]
    public void EmptyHistory_DoesNotWarn()
    {
        var digest = RepeatedPatternWarningDigest.Compose(Plan(), []);

        Assert.False(digest.HasWarning);
        Assert.Empty(digest.WarningLine);
    }

    [Fact]
    public void ExactThreatAndTacticWarning_ProducesCoachMessage()
    {
        var digest = RepeatedPatternWarningDigest.Compose(
            Plan(),
            [Entry(10)]);

        Assert.True(digest.HasWarning);
        Assert.Equal(1, digest.MatchingWarningCount);
        Assert.Equal("Bu desen daha önce uyarı verdi.", digest.Headline);
        Assert.Contains("üretken hücum", digest.WarningLine, StringComparison.Ordinal);
        Assert.Contains("4-3-3 · Hücum", digest.WarningLine, StringComparison.Ordinal);
        Assert.Contains("bir kez uyarıyla", digest.WarningLine, StringComparison.Ordinal);
    }

    [Fact]
    public void TwoExactWarnings_EscalateRepeatedPatternHeadline()
    {
        var digest = RepeatedPatternWarningDigest.Compose(
            Plan(),
            [Entry(10), Entry(12)]);

        Assert.True(digest.HasWarning);
        Assert.Equal(2, digest.MatchingWarningCount);
        Assert.Equal("Aynı olumsuz desen tekrarlanıyor.", digest.Headline);
        Assert.Contains("2 kez", digest.WarningLine, StringComparison.Ordinal);
    }

    [Fact]
    public void PositiveOutcome_DoesNotCreateFalseWarning()
    {
        var digest = RepeatedPatternWarningDigest.Compose(
            Plan(),
            [Entry(10, outcome: MatchupPlanOutcomeSignal.Positive)]);

        Assert.False(digest.HasWarning);
    }

    [Fact]
    public void DifferentThreat_DoesNotMatch()
    {
        var digest = RepeatedPatternWarningDigest.Compose(
            Plan(),
            [Entry(10, threat: OpponentThreatKind.DefensiveResistance)]);

        Assert.False(digest.HasWarning);
    }

    [Fact]
    public void DifferentTactic_DoesNotMatch()
    {
        var digest = RepeatedPatternWarningDigest.Compose(
            Plan(),
            [Entry(10, selection: "Seçim: 4-4-2 · Dengeli")]);

        Assert.False(digest.HasWarning);
    }

    [Fact]
    public void DifferentPlanSignal_DoesNotMatch()
    {
        var digest = RepeatedPatternWarningDigest.Compose(
            Plan(),
            [Entry(10, planSignal: MatchupPlanSignal.Opportunity)]);

        Assert.False(digest.HasWarning);
    }

    private static MatchupPlanDigest Plan() =>
        new(
            MatchupPlanDigest.Brand,
            "Seçim: 4-3-3 · Hücum",
            "Risk değerlendirmesi.",
            MatchupPlanSignal.Risk,
            OpponentThreatKind.ProductiveAttack);

    private static MatchupPlanNotebookEntry Entry(
        int day,
        string selection = "Seçim: 4-3-3 · Hücum",
        OpponentThreatKind threat = OpponentThreatKind.ProductiveAttack,
        MatchupPlanOutcomeSignal outcome = MatchupPlanOutcomeSignal.Warning,
        MatchupPlanSignal planSignal = MatchupPlanSignal.Risk) =>
        MatchupPlanNotebookEntry.Compose(
            day,
            "Rakip FK",
            selection,
            threat,
            planSignal,
            outcome,
            "Eşleşme riski giderilemedi.");
}
