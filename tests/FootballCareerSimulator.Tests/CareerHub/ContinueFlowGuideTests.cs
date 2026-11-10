using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class ContinueFlowGuideTests
{
    private static ContinueFlowInput Calm(
        PrepPlanSuggestion? prep = null,
        string? opponent = "AMED",
        int? days = 3) =>
        new(
            HasOpenDecision: false,
            HasDueContinentalMatch: false,
            HasDueManagedMatch: false,
            DueMatchApproved: false,
            SeasonTransitionReady: false,
            SeasonArchivePhase: false,
            CanAdvance: true,
            BlockerAdvice: null,
            PrepSuggestion: prep,
            NextOpponentName: opponent,
            NextMatchIsHome: true,
            DaysToNextMatch: days);

    [Fact]
    public void CalmDay_AdvancesWithUpcomingMatchPreview()
    {
        var step = ContinueFlowGuide.Resolve(Calm());

        Assert.Equal(ContinueFlowGuide.KindAdvance, step.Kind);
        Assert.Equal("DEVAM", step.ButtonLabel);
        Assert.Contains("AMED", step.PreviewTitle, StringComparison.Ordinal);
        Assert.Contains("3 gün sonra", step.PreviewTitle, StringComparison.Ordinal);
        Assert.Null(step.AutoApplyPlan);
    }

    [Fact]
    public void EmptyWeekPlan_IsAutoAppliedInsteadOfHijackingPrimaryButton()
    {
        var step = ContinueFlowGuide.Resolve(Calm(PrepPlanSuggestion.SeedWeekPlan()));

        Assert.Equal(ContinueFlowGuide.KindAdvance, step.Kind);
        Assert.Equal("DEVAM", step.ButtonLabel);
        Assert.Equal(PrepPlanSuggestion.SeedWeek, step.AutoApplyPlan?.ActionCode);
    }

    [Fact]
    public void RecoveryPlan_IsAutoApplied()
    {
        var step = ContinueFlowGuide.Resolve(Calm(PrepPlanSuggestion.RecoveryPlan()));

        Assert.Equal(PrepPlanSuggestion.ApplyRecovery, step.AutoApplyPlan?.ActionCode);
    }

    [Fact]
    public void LoadAdjustmentSuggestion_IsAdvisoryOnly()
    {
        var step = ContinueFlowGuide.Resolve(Calm(PrepPlanSuggestion.SoftenLoadPlan(null)));

        Assert.Equal(ContinueFlowGuide.KindAdvance, step.Kind);
        Assert.Null(step.AutoApplyPlan);
    }

    [Fact]
    public void DueMatch_StopsAtMatchPrepAndNeverAdvances()
    {
        var step = ContinueFlowGuide.Resolve(Calm() with { HasDueManagedMatch = true, CanAdvance = false });

        Assert.Equal(ContinueFlowGuide.KindPrepareMatch, step.Kind);
        Assert.Equal("Maç Öncesine Git", step.ButtonLabel);
        Assert.Contains("onayla", step.PreviewBody, StringComparison.Ordinal);
    }

    [Fact]
    public void ContinentalMatch_TakesPriorityOverLeagueMatch()
    {
        var step = ContinueFlowGuide.Resolve(Calm() with
        {
            HasDueManagedMatch = true,
            HasDueContinentalMatch = true,
        });

        Assert.Equal(ContinueFlowGuide.KindPlayContinental, step.Kind);
    }

    [Fact]
    public void MandatoryDecision_ComesFirst()
    {
        var step = ContinueFlowGuide.Resolve(Calm() with
        {
            HasOpenDecision = true,
            HasDueManagedMatch = true,
            CanAdvance = false,
        });

        Assert.Equal(ContinueFlowGuide.KindAnswerDecision, step.Kind);
    }

    [Fact]
    public void SeasonEnd_TransitionsBeforeAdvancing()
    {
        var step = ContinueFlowGuide.Resolve(Calm() with { SeasonTransitionReady = true, SeasonArchivePhase = true });

        Assert.Equal(ContinueFlowGuide.KindTransitionSeason, step.Kind);
        Assert.Equal("Yeni Sezona Geç", step.ButtonLabel);
    }

    [Fact]
    public void Blocked_ExplainsBlockerAdvice()
    {
        var step = ContinueFlowGuide.Resolve(Calm() with { CanAdvance = false, BlockerAdvice = "Kadro eksik." });

        Assert.Equal(ContinueFlowGuide.KindBlocked, step.Kind);
        Assert.Equal("Kadro eksik.", step.PreviewBody);
    }

    [Fact]
    public void NoUpcomingFixture_UsesGenericPreview()
    {
        var step = ContinueFlowGuide.Resolve(Calm(opponent: null, days: null));

        Assert.Equal("Sıradaki olaya ilerle", step.PreviewTitle);
    }

    [Theory]
    [InlineData(0, "Bugün")]
    [InlineData(1, "Yarın")]
    [InlineData(5, "5 gün sonra")]
    public void Countdown_IsReadable(int days, string expected) =>
        Assert.Equal(expected, ContinueFlowGuide.FormatCountdown(days));
}
