using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;
using FootballCareerSimulator.Domain.TrainingPhysicalState;

namespace FootballCareerSimulator.Tests.TrainingPhysicalState;

public sealed class MatchTrainingPriorityDigestTests
{
    [Fact]
    public void ProductiveAttack_HealthySquad_RecommendsDefensiveTransitions()
    {
        var digest = MatchTrainingPriorityDigest.Compose(
            Summary(fatigue: 35, fitness: 82),
            Opponent(OpponentThreatKind.ProductiveAttack, isHome: false, strengthDifference: 4),
            daysUntilMatch: 3);

        Assert.True(digest.IsAvailable);
        Assert.Equal(MatchTrainingPriority.DefensiveTransitions, digest.RecommendedPriority);
        Assert.Contains("yorgunluk 35", digest.SquadStatusLine, StringComparison.Ordinal);
        Assert.Contains("fitness 82", digest.SquadStatusLine, StringComparison.Ordinal);
        Assert.Contains("ilk beş saniyeyi", digest.StaffFeedback, StringComparison.Ordinal);

        var option = Assert.Single(
            digest.Options,
            item => item.Priority == MatchTrainingPriority.DefensiveTransitions);
        Assert.True(option.IsRecommended);
        Assert.Equal(2, option.TemporaryMatchModifier);
        Assert.Equal(5, option.ProjectedFatigueDelta);
        Assert.Equal(2, option.InjuryRiskDeltaPercent);
        Assert.Equal(TrainingFocus.General, option.SuggestedFocus);
        Assert.Equal(TrainingIntensity.Medium, option.SuggestedIntensity);
        Assert.Equal(RestApproach.Normal, option.SuggestedRest);
    }

    [Fact]
    public void HeavyFatigueAndAbsences_OverrideOpponentThreatWithRecovery()
    {
        var digest = MatchTrainingPriorityDigest.Compose(
            Summary(fatigue: 78, fitness: 61, injured: 3, unavailable: 2),
            Opponent(OpponentThreatKind.ProductiveAttack, isHome: false, strengthDifference: 8),
            daysUntilMatch: 1);

        Assert.Equal(MatchTrainingPriority.Recovery, digest.RecommendedPriority);
        Assert.Contains("3", digest.SquadStatusLine, StringComparison.Ordinal);
        Assert.Contains("2 oyuncu kullanılamıyor", digest.StaffFeedback, StringComparison.Ordinal);

        var recovery = digest.RecommendedOption;
        Assert.NotNull(recovery);
        Assert.Equal(2, recovery!.TemporaryMatchModifier);
        Assert.Equal(-12, recovery.ProjectedFatigueDelta);
        Assert.Equal(-7, recovery.InjuryRiskDeltaPercent);
        Assert.Contains("mevcut sakatlar", recovery.RiskLine, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(OpponentThreatKind.WinningStreak, MatchTrainingPriority.PressResistance)]
    [InlineData(OpponentThreatKind.TopZoneTempo, MatchTrainingPriority.PressResistance)]
    [InlineData(OpponentThreatKind.DefensiveResistance, MatchTrainingPriority.AttackingPatterns)]
    [InlineData(OpponentThreatKind.LosingStreak, MatchTrainingPriority.AttackingPatterns)]
    public void OpponentThreat_SelectsMatchingPriority(
        OpponentThreatKind threat,
        MatchTrainingPriority expected)
    {
        var digest = MatchTrainingPriorityDigest.Compose(
            Summary(fatigue: 30, fitness: 85),
            Opponent(threat, isHome: true),
            daysUntilMatch: 4);

        Assert.Equal(expected, digest.RecommendedPriority);
        Assert.Equal(expected, digest.Options[0].Priority);
    }

    [Fact]
    public void LowFitness_SelectsMatchSharpnessEvenWithoutSpecificOpponentEdge()
    {
        var digest = MatchTrainingPriorityDigest.Compose(
            Summary(fatigue: 28, fitness: 49),
            Opponent(OpponentThreatKind.Neutral),
            daysUntilMatch: 3);

        Assert.Equal(MatchTrainingPriority.MatchSharpness, digest.RecommendedPriority);
        Assert.Contains("XI fitnessı 49", digest.StaffFeedback, StringComparison.Ordinal);
        Assert.Equal(TrainingFocus.Fitness, digest.RecommendedOption!.SuggestedFocus);
    }

    [Fact]
    public void HighFatigue_ReducesAggressiveBoostAndRaisesItsRisk()
    {
        var fresh = MatchTrainingPriorityDigest.Compose(
            Summary(fatigue: 35, fitness: 82),
            Opponent(OpponentThreatKind.ProductiveAttack),
            daysUntilMatch: 3);
        var tired = MatchTrainingPriorityDigest.Compose(
            Summary(fatigue: 68, fitness: 82),
            Opponent(OpponentThreatKind.ProductiveAttack),
            daysUntilMatch: 3);

        var freshTransitions = fresh.Options.Single(
            option => option.Priority == MatchTrainingPriority.DefensiveTransitions);
        var tiredTransitions = tired.Options.Single(
            option => option.Priority == MatchTrainingPriority.DefensiveTransitions);

        Assert.True(tiredTransitions.TemporaryMatchModifier < freshTransitions.TemporaryMatchModifier);
        Assert.True(tiredTransitions.ProjectedFatigueDelta > freshTransitions.ProjectedFatigueDelta);
        Assert.True(tiredTransitions.InjuryRiskDeltaPercent > freshTransitions.InjuryRiskDeltaPercent);
        Assert.Contains("bir kademe düşürüyor", tiredTransitions.RiskLine, StringComparison.Ordinal);
    }

    [Fact]
    public void NoPhysicalMeasurements_UsesSafeDefaultsButLabelsUncertainty()
    {
        var digest = MatchTrainingPriorityDigest.Compose(
            Summary(fatigue: null, fitness: null, hasPlan: false),
            opponent: null,
            daysUntilMatch: 5);

        Assert.True(digest.IsAvailable);
        Assert.False(digest.HasPhysicalData);
        Assert.Contains("ölçümü henüz oluşmadı", digest.SquadStatusLine, StringComparison.Ordinal);
        Assert.All(
            digest.Options,
            option => Assert.Contains("sağlık ekibi kontrolü", option.RiskLine, StringComparison.Ordinal));
    }

    [Fact]
    public void Unemployed_ReturnsUnavailableBoardAndRejectsSelection()
    {
        var digest = MatchTrainingPriorityDigest.Compose(
            Summary(clubId: null),
            opponent: null,
            daysUntilMatch: 4);

        Assert.False(digest.IsAvailable);
        Assert.Null(digest.RecommendedPriority);
        Assert.Empty(digest.Options);
        Assert.Throws<InvalidOperationException>(
            () => digest.ResolveSelection(MatchTrainingPriority.Recovery));
    }

    [Fact]
    public void NoPlannedMatch_ReturnsUnavailableBoard()
    {
        var digest = MatchTrainingPriorityDigest.Compose(
            Summary(),
            opponent: null,
            daysUntilMatch: 0,
            hasPlannedMatch: false);

        Assert.False(digest.IsAvailable);
        Assert.Null(digest.RecommendedPriority);
        Assert.Empty(digest.Options);
        Assert.Contains("Planlı maç yok", digest.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveSelection_ReturnsOneMatchOutcomeAndSameNumericEffects()
    {
        var digest = MatchTrainingPriorityDigest.Compose(
            Summary(fatigue: 36, fitness: 84),
            Opponent(OpponentThreatKind.ProductiveAttack),
            daysUntilMatch: 3);
        var option = digest.RecommendedOption!;

        var outcome = digest.ResolveSelection(option.Priority);
        var staticOutcome = MatchTrainingPriorityDigest.ResolveSelection(option.Priority, digest);

        Assert.Equal(option.StableCode, outcome.StableCode);
        Assert.Equal(option.TemporaryMatchModifier, outcome.TemporaryMatchModifier);
        Assert.Equal(option.ProjectedFatigueDelta, outcome.ProjectedFatigueDelta);
        Assert.Equal(option.InjuryRiskDeltaPercent, outcome.InjuryRiskDeltaPercent);
        Assert.True(outcome.WasRecommended);
        Assert.Contains("yalnız sıradaki maç", outcome.OutcomeText, StringComparison.Ordinal);
        Assert.Contains("Staff ekibi de bu seçimi öneriyor", outcome.OutcomeText, StringComparison.Ordinal);
        Assert.Equal(outcome, staticOutcome);
    }

    [Fact]
    public void Options_ExposeStableUniqueCodesForEveryPriority()
    {
        var digest = MatchTrainingPriorityDigest.Compose(
            Summary(),
            Opponent(OpponentThreatKind.Neutral),
            daysUntilMatch: 4);

        Assert.Equal(Enum.GetValues<MatchTrainingPriority>().Length, digest.Options.Count);
        Assert.Equal(digest.Options.Count, digest.Options.Select(option => option.StableCode).Distinct().Count());
        Assert.Equal(digest.Options.Count, digest.Options.Select(option => option.NumericCode).Distinct().Count());
        Assert.Equal("recovery", digest.Options.Single(
            option => option.Priority == MatchTrainingPriority.Recovery).StableCode);
        Assert.Equal("match_sharpness", digest.Options.Single(
            option => option.Priority == MatchTrainingPriority.MatchSharpness).StableCode);
        Assert.Equal("press_resistance", digest.Options.Single(
            option => option.Priority == MatchTrainingPriority.PressResistance).StableCode);
        Assert.Equal("defensive_transitions", digest.Options.Single(
            option => option.Priority == MatchTrainingPriority.DefensiveTransitions).StableCode);
        Assert.Equal("attacking_patterns", digest.Options.Single(
            option => option.Priority == MatchTrainingPriority.AttackingPatterns).StableCode);
    }

    [Fact]
    public void Compose_IsDeterministicForSameInputs()
    {
        var summary = Summary(fatigue: 52, fitness: 73, injured: 1, unavailable: 1);
        var opponent = Opponent(
            OpponentThreatKind.SquadQuality,
            isHome: false,
            strengthDifference: 9);

        var first = MatchTrainingPriorityDigest.Compose(summary, opponent, daysUntilMatch: 2);
        var second = MatchTrainingPriorityDigest.Compose(summary, opponent, daysUntilMatch: 2);

        Assert.Equal(first.RecommendedPriority, second.RecommendedPriority);
        Assert.Equal(first.Headline, second.Headline);
        Assert.Equal(first.StaffFeedback, second.StaffFeedback);
        Assert.Equal(
            first.Options.Select(ToComparable),
            second.Options.Select(ToComparable));
    }

    [Fact]
    public void InvalidInput_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MatchTrainingPriorityDigest.Compose(Summary(), opponent: null, daysUntilMatch: -1));

        var digest = MatchTrainingPriorityDigest.Compose(
            Summary(),
            opponent: null,
            daysUntilMatch: 2);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            digest.ResolveSelection((MatchTrainingPriority)999));
    }

    private static object ToComparable(MatchTrainingPriorityOptionReadModel option) => new
    {
        option.Priority,
        option.StableCode,
        option.Rank,
        option.TemporaryMatchModifier,
        option.ProjectedFatigueDelta,
        option.InjuryRiskDeltaPercent,
        option.IsRecommended,
    };

    private static ClubTrainingSummaryReadModel Summary(
        long? clubId = 1,
        int? fatigue = 35,
        int? fitness = 82,
        int injured = 0,
        int unavailable = 0,
        bool hasPlan = true) =>
        new(
            ClubId: clubId,
            Focus: hasPlan ? (int)TrainingFocus.General : null,
            Intensity: hasPlan ? (int)TrainingIntensity.Medium : null,
            RestApproach: hasPlan ? (int)RestApproach.Normal : null,
            FocusName: null,
            IntensityName: null,
            RestApproachName: null,
            SetAtDayNumber: hasPlan ? 1 : null,
            AverageFatigue: fatigue,
            AverageFitness: fitness,
            HasPlan: hasPlan,
            InjuredSlotCount: injured,
            UnavailableSlotCount: unavailable);

    private static OpponentDossierDigest Opponent(
        OpponentThreatKind threat,
        bool isHome = true,
        int strengthDifference = 0) =>
        new(
            OpponentDossierDigest.Brand,
            isHome ? "Rakip evine geliyor." : "Rakip deplasmanındasın.",
            "Lig: 4/18 · 20 puan",
            "Form: G-B-G",
            "Güç: test bağlamı",
            "Tehdit: test bağlamı",
            threat,
            isHome,
            strengthDifference,
            WinningStreakLength: threat == OpponentThreatKind.WinningStreak ? 3 : 0,
            LosingStreakLength: threat == OpponentThreatKind.LosingStreak ? 3 : 0);
}
