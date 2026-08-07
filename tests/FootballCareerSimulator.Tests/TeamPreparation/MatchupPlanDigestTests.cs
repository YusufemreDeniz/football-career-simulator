using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class MatchupPlanDigestTests
{
    [Fact]
    public void Compose_FormatsSelectedFormationAndApproach()
    {
        var digest = Compose(Formation.F433, TacticalApproach.Attacking);

        Assert.Equal(MatchupPlanDigest.Brand, digest.BrandTitle);
        Assert.Equal("Seçim: 4-3-3 · Hücum", digest.SelectionLine);
        Assert.Equal(Formation.F433, digest.Formation);
        Assert.Equal(TacticalApproach.Attacking, digest.Approach);
    }

    [Fact]
    public void Attacking352_AgainstProductiveAttack_WarnsAboutWideTransitions()
    {
        var digest = Compose(
            Formation.F352,
            TacticalApproach.Attacking,
            OpponentThreatKind.ProductiveAttack);

        Assert.Equal(MatchupPlanSignal.Risk, digest.Signal);
        Assert.Contains("kanat arkası", digest.VerdictLine, StringComparison.Ordinal);
        Assert.Contains("iki kanat bekini", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void AttackingAway_AgainstThreateningOpponent_FlagsTransitionRisk()
    {
        var digest = Compose(
            Formation.F433,
            TacticalApproach.Attacking,
            OpponentThreatKind.WinningStreak,
            managedIsHome: false);

        Assert.Equal(MatchupPlanSignal.Risk, digest.Signal);
        Assert.Contains("deplasmanda", digest.VerdictLine, StringComparison.Ordinal);
        Assert.Contains("geçiş tehdidine", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(Formation.F442, "iki forvetin")]
    [InlineData(Formation.F433, "kanat genişliği")]
    [InlineData(Formation.F352, "merkezdeki sayısal")]
    public void Attacking_AgainstDefensiveResistance_UsesFormationRoute(
        Formation formation,
        string expectedRoute)
    {
        var digest = Compose(
            formation,
            TacticalApproach.Attacking,
            OpponentThreatKind.DefensiveResistance);

        Assert.Equal(MatchupPlanSignal.Opportunity, digest.Signal);
        Assert.Contains(expectedRoute, digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Attacking_WhenClearlyStronger_PresentsOpportunityWithRestDefenseCaveat()
    {
        var digest = Compose(
            Formation.F433,
            TacticalApproach.Attacking,
            strengthDifference: -8);

        Assert.Equal(MatchupPlanSignal.Opportunity, digest.Signal);
        Assert.Contains("kalite üstünlüğünü", digest.VerdictLine, StringComparison.Ordinal);
        Assert.Contains("top kaybı emniyetini", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Defensive_WhenHomeFavourite_FlagsUnnecessaryPassivity()
    {
        var digest = Compose(
            Formation.F442,
            TacticalApproach.Defensive,
            managedIsHome: true,
            strengthDifference: -5);

        Assert.Equal(MatchupPlanSignal.Risk, digest.Signal);
        Assert.Contains("inisiyatifi", digest.VerdictLine, StringComparison.Ordinal);
        Assert.Contains("çok geriye", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(Formation.F442, "iki kompakt hat")]
    [InlineData(Formation.F433, "üçlü orta saha")]
    [InlineData(Formation.F352, "beşli yoğunluk")]
    public void Defensive_AgainstPressureThreat_UsesFormationProtection(
        Formation formation,
        string expectedProtection)
    {
        var digest = Compose(
            formation,
            TacticalApproach.Defensive,
            OpponentThreatKind.ProductiveAttack);

        Assert.Equal(MatchupPlanSignal.Opportunity, digest.Signal);
        Assert.Contains(expectedProtection, digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void DefensiveAway_WithNeutralDossier_RemainsBalanced()
    {
        var digest = Compose(
            Formation.F442,
            TacticalApproach.Defensive,
            managedIsHome: false);

        Assert.Equal(MatchupPlanSignal.Balance, digest.Signal);
        Assert.StartsWith("Denge:", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Balanced_AgainstWinningStreak_PrioritizesCalmOpening()
    {
        var digest = Compose(
            Formation.F442,
            TacticalApproach.Balanced,
            OpponentThreatKind.WinningStreak);

        Assert.Equal(MatchupPlanSignal.Opportunity, digest.Signal);
        Assert.Contains("ilk 20 dakikada", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Balanced433_AgainstDefensiveResistance_PreservesWidthAndSecurity()
    {
        var digest = Compose(
            Formation.F433,
            TacticalApproach.Balanced,
            OpponentThreatKind.DefensiveResistance);

        Assert.Equal(MatchupPlanSignal.Opportunity, digest.Signal);
        Assert.Contains("genişlik", digest.VerdictLine, StringComparison.Ordinal);
        Assert.Contains("merkez emniyetini", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Balanced_AgainstStrongerOpponent_DoesNotOverpromiseAnEdge()
    {
        var digest = Compose(
            Formation.F352,
            TacticalApproach.Balanced,
            OpponentThreatKind.SquadQuality,
            strengthDifference: 8);

        Assert.Equal(MatchupPlanSignal.Balance, digest.Signal);
        Assert.Contains("kontrollü başlangıç", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData((Formation)999, TacticalApproach.Balanced, "formation")]
    [InlineData(Formation.F442, (TacticalApproach)999, "approach")]
    public void Compose_RejectsUnknownTacticValues(
        Formation formation,
        TacticalApproach approach,
        string parameterName)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => MatchupPlanDigest.Compose(formation, approach, Dossier()));

        Assert.Equal(parameterName, exception.ParamName);
    }

    private static MatchupPlanDigest Compose(
        Formation formation,
        TacticalApproach approach,
        OpponentThreatKind threatKind = OpponentThreatKind.Neutral,
        bool managedIsHome = true,
        int strengthDifference = 0) =>
        MatchupPlanDigest.Compose(
            formation,
            approach,
            Dossier(threatKind, managedIsHome, strengthDifference));

    private static OpponentDossierDigest Dossier(
        OpponentThreatKind threatKind = OpponentThreatKind.Neutral,
        bool managedIsHome = true,
        int strengthDifference = 0) =>
        new(
            OpponentDossierDigest.Brand,
            "Rakip FK",
            "Lig",
            "Form",
            "Güç",
            "Tehdit",
            threatKind,
            managedIsHome,
            strengthDifference);
}
