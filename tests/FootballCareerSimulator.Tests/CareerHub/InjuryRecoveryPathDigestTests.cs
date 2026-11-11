using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class InjuryRecoveryPathDigestTests
{
    [Fact]
    public void Clear_WhenNoInjuryPressure()
    {
        var path = InjuryRecoveryPathDigest.Compose(
            hasInjuryPressure: false,
            injuredPlayerNames: null,
            isOnRecoveryPlan: false,
            hasDueMatch: true,
            isMatchApproved: false);

        Assert.False(path.IsActive);
        Assert.Empty(path.ToDisplayText());
    }

    [Fact]
    public void Cleared_CelebratesPathClosure()
    {
        var path = InjuryRecoveryPathDigest.Compose(
            hasInjuryPressure: false,
            injuredPlayerNames: null,
            isOnRecoveryPlan: true,
            hasDueMatch: false,
            isMatchApproved: false,
            freshlyRecoveredNames: ["Tolga Kurt"]);

        Assert.True(path.IsActive);
        Assert.Equal(InjuryRecoveryPathDigest.StepCleared, path.CurrentStepCode);
        Assert.Contains("İyileşti", path.Headline, StringComparison.Ordinal);
        Assert.Contains("Tolga Kurt", path.Headline, StringComparison.Ordinal);
        Assert.All(path.StepLines, s => Assert.StartsWith("✓ ", s, StringComparison.Ordinal));
    }

    [Fact]
    public void NeedsRecovery_IsStepOne()
    {
        var path = InjuryRecoveryPathDigest.Compose(
            hasInjuryPressure: true,
            injuredPlayerNames: ["Tolga Kurt"],
            isOnRecoveryPlan: false,
            hasDueMatch: true,
            isMatchApproved: false);

        Assert.True(path.IsActive);
        Assert.Equal(InjuryRecoveryPathDigest.StepRecovery, path.CurrentStepCode);
        Assert.Contains("1/3", path.Headline, StringComparison.Ordinal);
        Assert.Contains("Tolga Kurt", path.Headline, StringComparison.Ordinal);
        Assert.Contains(path.StepLines, s => s.StartsWith("→ Toparlanma", StringComparison.Ordinal));
        Assert.Contains(path.StepLines, s => s.StartsWith("○ Sakatsız", StringComparison.Ordinal));
    }

    [Fact]
    public void OnRecoveryWithUnapprovedMatch_IsStepTwo()
    {
        var path = InjuryRecoveryPathDigest.Compose(
            hasInjuryPressure: true,
            injuredPlayerNames: ["Tolga Kurt"],
            isOnRecoveryPlan: true,
            hasDueMatch: true,
            isMatchApproved: false);

        Assert.Equal(InjuryRecoveryPathDigest.StepXi, path.CurrentStepCode);
        Assert.Contains("2/3", path.Headline, StringComparison.Ordinal);
        Assert.Contains(path.StepLines, s => s.StartsWith("✓ Toparlanma", StringComparison.Ordinal));
        Assert.Contains(path.StepLines, s => s.StartsWith("→ Sakatsız", StringComparison.Ordinal));
    }

    [Fact]
    public void ApprovedMatch_IsStepThreeKickoff()
    {
        var path = InjuryRecoveryPathDigest.Compose(
            hasInjuryPressure: true,
            injuredPlayerNames: ["Ali Yılmaz"],
            isOnRecoveryPlan: true,
            hasDueMatch: true,
            isMatchApproved: true);

        Assert.Equal(InjuryRecoveryPathDigest.StepKickoff, path.CurrentStepCode);
        Assert.Contains("3/3", path.Headline, StringComparison.Ordinal);
        Assert.Contains(path.StepLines, s => s.StartsWith("→ Maç gününe", StringComparison.Ordinal));
    }

    [Fact]
    public void TodayPulse_SurfacesRecoveryPathLine()
    {
        var path = InjuryRecoveryPathDigest.Compose(
            true,
            ["Tolga Kurt"],
            isOnRecoveryPlan: false,
            hasDueMatch: true,
            isMatchApproved: false);

        var prep = PreparationBriefing.Compose(
            new ClubTrainingSummaryReadModel(
                1,
                (int)Domain.TrainingPhysicalState.TrainingFocus.General,
                (int)Domain.TrainingPhysicalState.TrainingIntensity.Medium,
                (int)Domain.TrainingPhysicalState.RestApproach.Normal,
                null, null, null, 1, 30, 70, true, 1, 1,
                InjuredPlayerNames: ["Tolga Kurt"]),
            new TacticPlanReadModel(1, "4-4-2", "Dengeli", 1),
            "±0",
            daysUntilNextMatch: 2);

        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Compose(
                new ManagedFixtureSelectionStatusReadModel(
                    1, 1, 1, 2, true, 10, "2026-08-15", IsApproved: false),
                "Rival",
                10,
                injuredSlotCount: 1,
                injuredPlayerNames: ["Tolga Kurt"]),
            prep,
            LeagueWorldBriefing.Compose(
                "Active",
                8,
                30,
                8,
                managedRank: 4,
                managedPoints: 12,
                managedPlayed: 8,
                managedGoalDifference: 1,
                managedClubName: "Home",
                leaderClubName: "Leaders",
                leaderPoints: 18,
                nextMatchLine: null),
            recoveryPath: path);

        Assert.Contains(pulse.PulseLines, l => l.StartsWith("İyileşme:", StringComparison.Ordinal));
        Assert.Contains("İyileşme 1/3", pulse.ToDisplayText(), StringComparison.Ordinal);
    }

    [Fact]
    public void TodayPulse_SurfacesClearedCelebrationWithoutPrefix()
    {
        var path = InjuryRecoveryPathDigest.ComposeCleared(["Ali Yılmaz"]);
        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PreparationBriefing.Compose(
                new ClubTrainingSummaryReadModel(
                    1,
                    (int)Domain.TrainingPhysicalState.TrainingFocus.Recovery,
                    (int)Domain.TrainingPhysicalState.TrainingIntensity.Low,
                    (int)Domain.TrainingPhysicalState.RestApproach.Heavy,
                    null, null, null, 1, 20, 75, true, 0, 0),
                new TacticPlanReadModel(1, "4-4-2", "Dengeli", 1),
                "±0",
                daysUntilNextMatch: 4),
            LeagueWorldBriefing.Compose(
                "Active",
                8,
                30,
                8,
                managedRank: 4,
                managedPoints: 12,
                managedPlayed: 8,
                managedGoalDifference: 1,
                managedClubName: "Home",
                leaderClubName: "Leaders",
                leaderPoints: 18,
                nextMatchLine: null),
            recoveryPath: path);

        Assert.Contains(pulse.PulseLines, l => l.StartsWith("İyileşti", StringComparison.Ordinal));
        Assert.DoesNotContain(pulse.PulseLines, l => l.StartsWith("İyileşme:", StringComparison.Ordinal));
    }
}
