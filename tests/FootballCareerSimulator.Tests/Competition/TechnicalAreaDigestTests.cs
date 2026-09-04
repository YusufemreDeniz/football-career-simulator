using FootballCareerSimulator.Application.Competition.Queries;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class TechnicalAreaDigestTests
{
    [Fact]
    public void Attack_WinsSecondHalf_ReportsRewardedRisk()
    {
        var digest = Compose(
            halfTimeHomeGoals: 0,
            halfTimeAwayGoals: 1,
            finalHomeGoals: 2,
            finalAwayGoals: 1,
            managedIsHome: true,
            decision: MatchHalfTimeDigest.DecisionAttack);

        Assert.Equal(TechnicalAreaDigest.Brand, digest!.BrandTitle);
        Assert.Equal("Karar: Hücuma geçtin", digest.DecisionLine);
        Assert.Equal("Skor akışı: devre 0-1 · ikinci yarı 2-0 · final 2-1", digest.ScoreFlowLine);
        Assert.Contains("Risk karşılık buldu", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Attack_DrawsSecondHalfWithGoals_ReportsUnbrokenBalance()
    {
        var digest = Compose(0, 0, 1, 1, true, MatchHalfTimeDigest.DecisionAttack);

        Assert.Contains("dengeyi bozmadı", digest!.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Attack_ProducesNoSecondHalfGoals_ReportsNoScoreImpact()
    {
        var digest = Compose(1, 1, 1, 1, true, MatchHalfTimeDigest.DecisionAttack);

        Assert.Equal("Hücum kararı skora yansımadı.", digest!.VerdictLine);
    }

    [Fact]
    public void Attack_LosesSecondHalf_ReportsBackfire()
    {
        var digest = Compose(1, 0, 1, 2, true, MatchHalfTimeDigest.DecisionAttack);

        Assert.Contains("Risk geri tepti", digest!.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Defend_ProtectsHalfTimeLead_ReportsPlanHeld()
    {
        var digest = Compose(1, 0, 1, 0, true, MatchHalfTimeDigest.DecisionDefend);

        Assert.Equal("Karar: Savunmaya çektin", digest!.DecisionLine);
        Assert.Contains("üstünlüğünü korudun", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Defend_LosesHalfTimeLead_ReportsErodedAdvantage()
    {
        var digest = Compose(1, 0, 1, 1, true, MatchHalfTimeDigest.DecisionDefend);

        Assert.Contains("üstünlüğü eridi", digest!.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Defend_FromLevelStopsOpponent_ReportsSuccessfulBlock()
    {
        var digest = Compose(0, 0, 0, 0, true, MatchHalfTimeDigest.DecisionDefend);

        Assert.Equal("Savunma kararı rakibi durdurdu.", digest!.VerdictLine);
    }

    [Fact]
    public void Defend_FromLevelLosesSecondHalf_ReportsFailure()
    {
        var digest = Compose(0, 0, 0, 1, true, MatchHalfTimeDigest.DecisionDefend);

        Assert.Equal("Geri çekilmek rakibi durdurmadı.", digest!.VerdictLine);
    }

    [Theory]
    [InlineData(2, 0, "üstünlük getirdi")]
    [InlineData(1, 1, "dengeyi bozmadı")]
    [InlineData(0, 2, "cevap veremedi")]
    public void Continue_DescribesSecondHalfBalance(
        int finalHomeGoals,
        int finalAwayGoals,
        string expected)
    {
        var digest = Compose(
            0,
            0,
            finalHomeGoals,
            finalAwayGoals,
            true,
            MatchHalfTimeDigest.DecisionContinue);

        Assert.Equal("Karar: Aynı planla devam ettin", digest!.DecisionLine);
        Assert.Contains(expected, digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void AwayManager_ScoreFlowIsAlwaysManagedTeamFirst()
    {
        var digest = Compose(
            halfTimeHomeGoals: 1,
            halfTimeAwayGoals: 0,
            finalHomeGoals: 1,
            finalAwayGoals: 2,
            managedIsHome: false,
            decision: MatchHalfTimeDigest.DecisionAttack);

        Assert.Equal("Skor akışı: devre 0-1 · ikinci yarı 2-0 · final 2-1", digest!.ScoreFlowLine);
    }

    [Fact]
    public void MissingManagedHalfTime_ReturnsNull()
    {
        Assert.Null(TechnicalAreaDigest.Compose(
            MatchHalfTimeDigest.None(),
            finalHomeGoals: 1,
            finalAwayGoals: 0,
            managedSecondHalfDelta: MatchHalfTimeDigest.DecisionContinue));
    }

    [Fact]
    public void FinalScoreBelowHalfTime_ReturnsNull()
    {
        Assert.Null(TechnicalAreaDigest.Compose(
            HalfTime(homeGoals: 2, awayGoals: 0, managedIsHome: true),
            finalHomeGoals: 1,
            finalAwayGoals: 0,
            managedSecondHalfDelta: MatchHalfTimeDigest.DecisionContinue));
    }

    [Fact]
    public void UnknownDecision_ReturnsNull()
    {
        Assert.Null(TechnicalAreaDigest.Compose(
            HalfTime(homeGoals: 0, awayGoals: 0, managedIsHome: true),
            finalHomeGoals: 0,
            finalAwayGoals: 0,
            managedSecondHalfDelta: 99));
    }

    private static TechnicalAreaDigest? Compose(
        int halfTimeHomeGoals,
        int halfTimeAwayGoals,
        int finalHomeGoals,
        int finalAwayGoals,
        bool managedIsHome,
        int decision) =>
        TechnicalAreaDigest.Compose(
            HalfTime(halfTimeHomeGoals, halfTimeAwayGoals, managedIsHome),
            finalHomeGoals,
            finalAwayGoals,
            decision);

    private static MatchHalfTimeDigest HalfTime(
        int homeGoals,
        int awayGoals,
        bool managedIsHome) =>
        MatchHalfTimeDigest.Compose(
            "Ev",
            "Dep",
            homeGoals,
            awayGoals,
            managedIsHome);
}
