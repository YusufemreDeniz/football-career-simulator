using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class MatchupPlanOutcomeDigestTests
{
    [Fact]
    public void Compose_CarriesPlanSignalAndObservableMatchEvidence()
    {
        var digest = Compose(
            MatchupPlanSignal.Risk,
            homeGoals: 2,
            awayGoals: 1,
            halfTimeHomeGoals: 1,
            halfTimeAwayGoals: 1,
            moments: [Moment(MatchKeyMomentKind.Goal, 18, isHomeSide: true)]);

        Assert.NotNull(digest);
        Assert.Equal(MatchupPlanOutcomeDigest.Brand, digest!.BrandTitle);
        Assert.Equal("Seçim: 4-3-3 · Hücum", digest.SelectionLine);
        Assert.Equal("Düdük öncesi: Risk", digest.PreMatchLine);
        Assert.Contains("18' ilk golü sen attın", digest.EvidenceLine, StringComparison.Ordinal);
        Assert.Contains("ikinci yarı 1-0", digest.EvidenceLine, StringComparison.Ordinal);
        Assert.Contains("final 2-1", digest.EvidenceLine, StringComparison.Ordinal);
        Assert.Contains(digest.BrandTitle, digest.SummaryLine, StringComparison.Ordinal);
    }

    [Fact]
    public void FirstBreak_UsesManagedPerspectiveWhenManagedClubIsAway()
    {
        var digest = Compose(
            MatchupPlanSignal.Balance,
            homeGoals: 0,
            awayGoals: 1,
            managedIsHome: false,
            halfTimeHomeGoals: 0,
            halfTimeAwayGoals: 0,
            moments: [Moment(MatchKeyMomentKind.Goal, 67, isHomeSide: false)]);

        Assert.Contains("67' ilk golü sen attın", digest!.EvidenceLine, StringComparison.Ordinal);
        Assert.Contains("ikinci yarı 1-0", digest.EvidenceLine, StringComparison.Ordinal);
        Assert.Contains("final 1-0", digest.EvidenceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void FirstBreak_RedCardCreditsTheOtherSide()
    {
        var digest = Compose(
            MatchupPlanSignal.Opportunity,
            homeGoals: 0,
            awayGoals: 0,
            moments: [Moment(MatchKeyMomentKind.RedCard, 24, isHomeSide: true)]);

        Assert.Contains("24' kırmızı kartı sen gördün", digest!.EvidenceLine, StringComparison.Ordinal);
        Assert.Equal(MatchupPlanOutcomeSignal.Neutral, digest.Signal);
        Assert.Contains("Fırsat skora dönüşmedi", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void FirstBreak_PrefersEarlierRedCardOverLaterGoal()
    {
        var digest = Compose(
            MatchupPlanSignal.Balance,
            homeGoals: 1,
            awayGoals: 0,
            moments:
            [
                Moment(MatchKeyMomentKind.Goal, 40, isHomeSide: true),
                Moment(MatchKeyMomentKind.RedCard, 12, isHomeSide: false),
            ]);

        Assert.Contains("12' kırmızı kartı rakip gördü", digest!.EvidenceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void RiskPlan_Loss_ReportsWarningMaterialized()
    {
        var digest = Compose(MatchupPlanSignal.Risk, homeGoals: 0, awayGoals: 2);

        Assert.Equal(MatchupPlanOutcomeSignal.Warning, digest!.Signal);
        Assert.Contains("Uyarı sahada karşılık buldu", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void RiskPlan_Win_ReportsRiskManaged()
    {
        var digest = Compose(MatchupPlanSignal.Risk, homeGoals: 2, awayGoals: 0);

        Assert.Equal(MatchupPlanOutcomeSignal.Positive, digest!.Signal);
        Assert.Contains("Riski yönettin", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void RiskPlan_DrawAfterOpponentFirstGoal_ReportsRiskVisible()
    {
        var digest = Compose(
            MatchupPlanSignal.Risk,
            homeGoals: 1,
            awayGoals: 1,
            moments: [Moment(MatchKeyMomentKind.Goal, 10, isHomeSide: false)]);

        Assert.Equal(MatchupPlanOutcomeSignal.Neutral, digest!.Signal);
        Assert.Contains("Risk maç içinde göründü", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void OpportunityPlan_Win_ReportsOpportunityConverted()
    {
        var digest = Compose(MatchupPlanSignal.Opportunity, homeGoals: 3, awayGoals: 1);

        Assert.Equal(MatchupPlanOutcomeSignal.Positive, digest!.Signal);
        Assert.Contains("Fırsatı sonuca çevirdin", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void OpportunityPlan_SecondHalfRecoveryWithoutWin_ReportsPartialTrace()
    {
        var digest = Compose(
            MatchupPlanSignal.Opportunity,
            homeGoals: 1,
            awayGoals: 2,
            halfTimeHomeGoals: 0,
            halfTimeAwayGoals: 2);

        Assert.Equal(MatchupPlanOutcomeSignal.Neutral, digest!.Signal);
        Assert.Contains("Fırsat maç içinde göründü", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void OpportunityPlan_LossWithoutPositiveEvidence_ReportsMissedOpportunity()
    {
        var digest = Compose(
            MatchupPlanSignal.Opportunity,
            homeGoals: 0,
            awayGoals: 1,
            halfTimeHomeGoals: 0,
            halfTimeAwayGoals: 0);

        Assert.Equal(MatchupPlanOutcomeSignal.Warning, digest!.Signal);
        Assert.Contains("Fırsat kağıt üzerinde kaldı", digest.VerdictLine, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(2, 0, MatchupPlanOutcomeSignal.Positive, "Dengeyi lehine bozdun")]
    [InlineData(1, 1, MatchupPlanOutcomeSignal.Neutral, "Öngörülen denge")]
    [InlineData(0, 1, MatchupPlanOutcomeSignal.Warning, "Denge rakibin lehine kırıldı")]
    public void BalancePlan_UsesFinalOutcome(
        int homeGoals,
        int awayGoals,
        MatchupPlanOutcomeSignal expectedSignal,
        string expectedVerdict)
    {
        var digest = Compose(MatchupPlanSignal.Balance, homeGoals, awayGoals);

        Assert.Equal(expectedSignal, digest!.Signal);
        Assert.Contains(expectedVerdict, digest.VerdictLine, StringComparison.Ordinal);
    }

    [Fact]
    public void InconsistentHalfTime_FallsBackWithoutInventingSecondHalfScore()
    {
        var digest = Compose(
            MatchupPlanSignal.Balance,
            homeGoals: 1,
            awayGoals: 0,
            halfTimeHomeGoals: 2,
            halfTimeAwayGoals: 0);

        Assert.Contains("ikinci yarı verisi yok", digest!.EvidenceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void FailedMatch_ReturnsNull()
    {
        var result = Result(homeGoals: 0, awayGoals: 0) with { Succeeded = false };

        Assert.Null(MatchupPlanOutcomeDigest.Compose(
            Plan(MatchupPlanSignal.Risk),
            halfTime: null,
            result,
            managedIsHome: true));
    }

    private static MatchupPlanOutcomeDigest? Compose(
        MatchupPlanSignal planSignal,
        int homeGoals,
        int awayGoals,
        bool managedIsHome = true,
        int? halfTimeHomeGoals = null,
        int? halfTimeAwayGoals = null,
        IReadOnlyList<MatchKeyMomentReadModel>? moments = null)
    {
        var halfTime = halfTimeHomeGoals is int home && halfTimeAwayGoals is int away
            ? MatchHalfTimeDigest.Compose("Ev", "Dep", home, away, managedIsHome)
            : null;
        return MatchupPlanOutcomeDigest.Compose(
            Plan(planSignal),
            halfTime,
            Result(homeGoals, awayGoals, moments),
            managedIsHome);
    }

    private static MatchupPlanDigest Plan(MatchupPlanSignal signal) =>
        new(
            MatchupPlanDigest.Brand,
            "Seçim: 4-3-3 · Hücum",
            "Maç önü değerlendirmesi",
            signal,
            OpponentThreatKind.Neutral,
            Formation.F433,
            TacticalApproach.Attacking);

    private static PlayFixtureMatchResult Result(
        int homeGoals,
        int awayGoals,
        IReadOnlyList<MatchKeyMomentReadModel>? moments = null) =>
        new(
            Succeeded: true,
            SeasonId: 1,
            FixtureId: 10,
            homeGoals,
            awayGoals,
            Status: "ResultAccepted",
            KeyMoments: moments);

    private static MatchKeyMomentReadModel Moment(
        MatchKeyMomentKind kind,
        int minute,
        bool isHomeSide) =>
        new(
            kind.ToString(),
            minute,
            isHomeSide,
            PrimarySlotIndex: 4);
}
