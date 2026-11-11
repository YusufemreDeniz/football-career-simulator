using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class StaffWhisperTests
{
    [Fact]
    public void CalmMatch_WithInjuries_MentionsFixtureAndInjuredPlayer()
    {
        var whisper = StaffWhisper.Compose(
            MatchRival(),
            PrepWithInjuries("Ali Yılmaz"),
            LeagueOk(),
            WeekMoodDigest.MoodCalmMatch,
            dayNumber: 10);

        Assert.NotNull(whisper);
        Assert.Contains("Ev vs Rival", whisper, StringComparison.Ordinal);
        Assert.Contains("Ali Yılmaz sahada yok", whisper, StringComparison.Ordinal);
    }

    [Fact]
    public void CalmMatch_NoInjuries_MentionsFixtureAndTempo()
    {
        var whisper = StaffWhisper.Compose(
            MatchRival(),
            PrepOk(),
            LeagueOk(),
            WeekMoodDigest.MoodCalmMatch,
            dayNumber: 10);

        Assert.NotNull(whisper);
        Assert.Contains("Ev vs Rival", whisper, StringComparison.Ordinal);
        Assert.Contains("tempo yerinde", whisper, StringComparison.Ordinal);
    }

    [Fact]
    public void Calm_WithInjuries_CountsSquadLosses()
    {
        var whisper = StaffWhisper.Compose(
            PreMatchBriefing.Clear(),
            PrepWithInjuries("Ali Yılmaz", "Can Demir"),
            LeagueOk(),
            WeekMoodDigest.MoodCalm,
            dayNumber: 10);

        Assert.Equal(
            "Not: Staff — 2 oyuncu sahalarda yok (Ali Yılmaz, Can Demir).",
            whisper);
    }

    [Fact]
    public void Calm_NoInjuries_UsesLeagueHeadline()
    {
        var whisper = StaffWhisper.Compose(
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk(),
            WeekMoodDigest.MoodCalm,
            dayNumber: 10);

        Assert.Equal("Not: Staff — Lig ortasında yol alıyorsun.", whisper);
    }

    [Fact]
    public void Calm_NoContext_FallsBackToGenericCalmNote()
    {
        var whisper = StaffWhisper.Compose(
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueWorldBriefing.NoSeason(),
            WeekMoodDigest.MoodCalm,
            dayNumber: 10);

        Assert.Equal(
            OfficeCalmNote.ToBeatLine(WeekMoodDigest.MoodCalm, 10),
            whisper);
    }

    [Fact]
    public void NonCalmMood_ReturnsNull()
    {
        var whisper = StaffWhisper.Compose(
            MatchRival(),
            PrepOk(),
            LeagueOk(),
            WeekMoodDigest.MoodMatchReady,
            dayNumber: 10);

        Assert.Null(whisper);
    }

    private static PreMatchBriefing MatchRival() =>
        PreMatchBriefing.Compose(
            new ManagedFixtureSelectionStatusReadModel(
                1, 1, 1, 2, IsHome: true, 10, "2026-08-15", IsApproved: true),
            "Rival",
            currentDayNumber: 10);

    private static PreparationBriefing PrepOk() =>
        PreparationBriefing.Compose(
            new ClubTrainingSummaryReadModel(
                1,
                (int)Domain.TrainingPhysicalState.TrainingFocus.General,
                (int)Domain.TrainingPhysicalState.TrainingIntensity.Medium,
                (int)Domain.TrainingPhysicalState.RestApproach.Normal,
                null, null, null, null, null, null,
                HasPlan: true, 0, 0),
            new TacticPlanReadModel(1, "4-4-2", "Dengeli", 1),
            "±0",
            daysUntilNextMatch: 4);

    private static PreparationBriefing PrepWithInjuries(params string[] names) =>
        PreparationBriefing.Compose(
            new ClubTrainingSummaryReadModel(
                1,
                (int)Domain.TrainingPhysicalState.TrainingFocus.General,
                (int)Domain.TrainingPhysicalState.TrainingIntensity.Medium,
                (int)Domain.TrainingPhysicalState.RestApproach.Normal,
                null, null, null, null, null, null,
                HasPlan: true, 1, 1, InjuredPlayerNames: names),
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
