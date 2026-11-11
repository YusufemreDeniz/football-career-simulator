using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class WeekStoryDigestTests
{
    [Fact]
    public void Clear_WhenNoArc()
    {
        var story = WeekStoryDigest.Compose(
            InjuryRecoveryPathDigest.Clear(),
            PreMatchBriefing.Clear());

        Assert.False(story.IsActive);
        Assert.Empty(story.ToDisplayText());
        Assert.Empty(story.ToPulseLine());
    }

    [Fact]
    public void ActiveInjury_TellsToparlanmaStory()
    {
        var path = InjuryRecoveryPathDigest.Compose(
            hasInjuryPressure: true,
            injuredPlayerNames: ["Tolga Kurt"],
            isOnRecoveryPlan: false,
            hasDueMatch: true,
            isMatchApproved: false);

        var story = WeekStoryDigest.Compose(path, PreMatchBriefing.Clear());

        Assert.True(story.IsActive);
        Assert.Equal(WeekStoryDigest.PhaseInjury, story.PhaseCode);
        Assert.Contains("Tolga Kurt", story.StoryLine, StringComparison.Ordinal);
        Assert.Contains("Toparlanma", story.StoryLine, StringComparison.Ordinal);
        Assert.StartsWith("Hikâye:", story.ToPulseLine(), StringComparison.Ordinal);
        Assert.Contains("Haftanın Hikâyesi", story.ToDisplayText(), StringComparison.Ordinal);
    }

    [Fact]
    public void CleanXiMatch_TellsReturnStory()
    {
        var match = PreMatchBriefing.Compose(
            new ManagedFixtureSelectionStatusReadModel(
                1, 1, 1, 2, true, 10, "2026-08-15", IsApproved: true),
            "Rival",
            10,
            cleanReturnNames: ["Ali Yılmaz"]);

        var story = WeekStoryDigest.Compose(InjuryRecoveryPathDigest.Clear(), match);

        Assert.Equal(WeekStoryDigest.PhaseCleanXi, story.PhaseCode);
        Assert.Contains("Temiz XI", story.StoryLine, StringComparison.Ordinal);
        Assert.Contains("Ali Yılmaz", story.StoryLine, StringComparison.Ordinal);
    }

    [Fact]
    public void ClosedVerdict_LingersAsWeekStory()
    {
        var story = WeekStoryDigest.Compose(
            InjuryRecoveryPathDigest.Clear(),
            PreMatchBriefing.Clear(),
            closedArcVerdictBeat: "Dönenler işe yaradı — Kurt");

        Assert.Equal(WeekStoryDigest.PhaseVerdict, story.PhaseCode);
        Assert.Contains("işe yaradı", story.StoryLine, StringComparison.Ordinal);
        Assert.EndsWith(".", story.StoryLine);
    }

    [Fact]
    public void TodayPulse_PrefersWeekStoryOverRawRecoveryLine()
    {
        var path = InjuryRecoveryPathDigest.Compose(
            true,
            ["Tolga Kurt"],
            isOnRecoveryPlan: false,
            hasDueMatch: true,
            isMatchApproved: false);
        var story = WeekStoryDigest.Compose(path, PreMatchBriefing.Clear());

        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Compose(
                new ManagedFixtureSelectionStatusReadModel(
                    1, 1, 1, 2, true, 10, "2026-08-15", IsApproved: false),
                "Rival",
                10,
                injuredSlotCount: 1,
                injuredPlayerNames: ["Tolga Kurt"]),
            PreparationBriefing.Compose(
                new ClubTrainingSummaryReadModel(
                    1,
                    (int)Domain.TrainingPhysicalState.TrainingFocus.General,
                    (int)Domain.TrainingPhysicalState.TrainingIntensity.Medium,
                    (int)Domain.TrainingPhysicalState.RestApproach.Normal,
                    null, null, null, 1, 30, 70, true, 1, 1,
                    InjuredPlayerNames: ["Tolga Kurt"]),
                new TacticPlanReadModel(1, "4-4-2", "Dengeli", 1),
                "±0",
                daysUntilNextMatch: 2),
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
            recoveryPath: path,
            weekStory: story);

        Assert.Contains(pulse.PulseLines, l => l.StartsWith("Hikâye:", StringComparison.Ordinal));
        Assert.DoesNotContain(pulse.PulseLines, l => l.StartsWith("İyileşme:", StringComparison.Ordinal));
    }
}
