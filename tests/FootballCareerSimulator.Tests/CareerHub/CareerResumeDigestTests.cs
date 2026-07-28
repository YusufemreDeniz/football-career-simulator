using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class CareerResumeDigestTests
{
    [Fact]
    public void CalmPulse_WelcomesBackWithAdvice()
    {
        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk());

        var resume = CareerResumeDigest.Compose(
            pulse,
            dayNumber: 40,
            isoDate: "2026-09-09",
            managerDisplayName: "Yusuf",
            clubDisplayName: "Home FC",
            loadedFixtureCount: 12,
            wasMigrated: false);

        Assert.Equal(CareerResumeDigest.Brand, resume.BrandTitle);
        Assert.Equal(TodayPulseDigest.FocusCalm, resume.PulseFocusCode);
        Assert.Contains("Tekrar ofistesin", resume.Headline, StringComparison.Ordinal);
        Assert.Contains(resume.BeatLines, b => b.Contains("Home FC", StringComparison.Ordinal));
        Assert.Contains(resume.BeatLines, b => b.Contains("Nabız:", StringComparison.Ordinal));
        Assert.Contains("Öneri:", resume.ToDisplayText(), StringComparison.Ordinal);
        Assert.Contains("Bugün nabzını", resume.AdviceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void SquadFocus_PointsToYerAc_AndNotesMigration()
    {
        var squad = SquadCapacityDigest.Compose(
            26,
            25,
            ClubSquad.MaxMembers,
            [2001]);
        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk(),
            squad);

        var resume = CareerResumeDigest.Compose(
            pulse,
            22,
            "2026-08-22",
            "Yusuf",
            "Home FC",
            loadedFixtureCount: 5,
            wasMigrated: true);

        Assert.Equal(TodayPulseDigest.FocusSquad, resume.PulseFocusCode);
        Assert.Contains("Yer Aç", resume.AdviceLine, StringComparison.Ordinal);
        Assert.Contains(resume.BeatLines, b => b.Contains("şeması güncellendi", StringComparison.Ordinal));
        Assert.Contains("önce:", resume.Headline, StringComparison.OrdinalIgnoreCase);
    }

    private static PreparationBriefing PrepOk() =>
        PreparationBriefing.Compose(
            new ClubTrainingSummaryReadModel(
                1,
                (int)Domain.TrainingPhysicalState.TrainingFocus.General,
                (int)Domain.TrainingPhysicalState.TrainingIntensity.Medium,
                (int)Domain.TrainingPhysicalState.RestApproach.Normal,
                null, null, null, 1, 30, 70, true, 0, 0),
            new TacticPlanReadModel(1, "4-4-2", "Dengeli", 1),
            "±0",
            daysUntilNextMatch: 4);

    private static LeagueWorldBriefing LeagueOk() =>
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
            nextMatchLine: null);
}
