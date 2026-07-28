using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;
using FootballCareerSimulator.Domain.TrainingPhysicalState;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class PreparationBriefingTests
{
    [Fact]
    public void Unemployed_WhenNoClub()
    {
        var briefing = PreparationBriefing.Compose(
            new ClubTrainingSummaryReadModel(
                null, null, null, null, null, null, null, null, null, null,
                HasPlan: false, 0, 0),
            new TacticPlanReadModel(null, "—", "—", 0),
            "±0");

        Assert.False(briefing.IsEmployed);
        Assert.Contains("Kulüp yok", briefing.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void NoPlan_AsksToShapeTheWeek()
    {
        var briefing = PreparationBriefing.Compose(
            Employed(hasPlan: false),
            new TacticPlanReadModel(1, "4-3-3", "Dengeli", 1),
            "+1",
            nextMatchFixtureLine: "Ev vs Rival · 2 gün sonra",
            daysUntilNextMatch: 2);

        Assert.Equal("Antrenman planı boş — bu haftayı şekillendir.", briefing.Headline);
        Assert.Contains("Orta yoğunluk", briefing.AdviceLine, StringComparison.Ordinal);
        Assert.True(briefing.DemandsAttention);
        Assert.Equal(PrepPlanSuggestion.SeedWeek, briefing.Suggestion!.ActionCode);
        Assert.Contains(briefing.BeatLines, b => b.Contains("Sıradaki:", StringComparison.Ordinal));
        Assert.Contains("Öneri:", briefing.ToDisplayText(), StringComparison.Ordinal);
    }

    [Fact]
    public void HighFatigueNearMatch_UrgesRecovery()
    {
        var briefing = PreparationBriefing.Compose(
            Employed(
                hasPlan: true,
                intensity: (int)TrainingIntensity.High,
                focus: (int)TrainingFocus.Fitness,
                rest: (int)RestApproach.Light,
                fatigue: 68,
                fitness: 55),
            new TacticPlanReadModel(1, "4-4-2", "Hücum", 2),
            "+2",
            "Dep vs Away · yarın",
            daysUntilNextMatch: 1);

        Assert.Contains("yorgun", briefing.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Toparlanma", briefing.AdviceLine, StringComparison.Ordinal);
        Assert.True(briefing.DemandsAttention);
        Assert.Equal(PrepPlanSuggestion.ApplyRecovery, briefing.Suggestion!.ActionCode);
        Assert.Equal(TrainingIntensity.Low, briefing.Suggestion.Intensity);
        Assert.Equal(TrainingFocus.Recovery, briefing.Suggestion.Focus);
        Assert.Contains(briefing.BeatLines, b => b.Contains("yorgunluk 68", StringComparison.Ordinal));
        Assert.Contains(briefing.BeatLines, b => b.Contains("4-4-2", StringComparison.Ordinal));
    }

    [Fact]
    public void BalancedPlan_StaysOnTrack()
    {
        var briefing = PreparationBriefing.Compose(
            Employed(
                hasPlan: true,
                intensity: (int)TrainingIntensity.Medium,
                focus: (int)TrainingFocus.General,
                rest: (int)RestApproach.Normal,
                fatigue: 35,
                fitness: 70),
            new TacticPlanReadModel(1, "3-5-2", "Dengeli", 3),
            "±0",
            daysUntilNextMatch: 5);

        Assert.Equal("Haftalık hazırlık masası açık.", briefing.Headline);
        Assert.Contains("Sıradaki Maç brifingine", briefing.AdviceLine, StringComparison.Ordinal);
        Assert.False(briefing.DemandsAttention);
        Assert.Null(briefing.Suggestion);
    }

    [Fact]
    public void AlreadyOnRecovery_DoesNotSuggestAgain()
    {
        var briefing = PreparationBriefing.Compose(
            Employed(
                hasPlan: true,
                intensity: (int)TrainingIntensity.Low,
                focus: (int)TrainingFocus.Recovery,
                rest: (int)RestApproach.Heavy,
                fatigue: 62,
                fitness: 50),
            new TacticPlanReadModel(1, "4-4-2", "Dengeli", 1),
            "±0",
            daysUntilNextMatch: 2);

        Assert.False(briefing.DemandsAttention);
        Assert.Null(briefing.Suggestion);
        Assert.Contains("doğru yönde", briefing.AdviceLine, StringComparison.Ordinal);
    }

    private static ClubTrainingSummaryReadModel Employed(
        bool hasPlan,
        int? intensity = null,
        int? focus = null,
        int? rest = null,
        int fatigue = 40,
        int fitness = 60,
        int injured = 0) =>
        new(
            ClubId: 1,
            Focus: focus,
            Intensity: intensity,
            RestApproach: rest,
            FocusName: null,
            IntensityName: null,
            RestApproachName: null,
            SetAtDayNumber: hasPlan ? 1 : null,
            AverageFatigue: hasPlan ? fatigue : null,
            AverageFitness: hasPlan ? fitness : null,
            HasPlan: hasPlan,
            InjuredSlotCount: injured,
            UnavailableSlotCount: injured);
}
