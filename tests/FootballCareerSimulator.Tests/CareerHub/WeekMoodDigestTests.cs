using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;
using FootballCareerSimulator.Application.Transfer.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class WeekMoodDigestTests
{
    [Fact]
    public void Clear_WhenWeekStoryActive()
    {
        var mood = WeekMoodDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk(),
            weekStoryActive: true,
            formMomentumCode: DressingRoomEchoDigest.MomentumWinningStreak);

        Assert.False(mood.IsActive);
    }

    [Fact]
    public void CalmEmployed_NoMatch_SoftWeekMood()
    {
        var mood = WeekMoodDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk());

        Assert.True(mood.IsActive);
        Assert.Equal(WeekMoodDigest.MoodCalm, mood.MoodCode);
        Assert.Contains("Sakin hafta", mood.MoodLine, StringComparison.Ordinal);
        Assert.StartsWith("Hava:", mood.ToPulseLine(), StringComparison.Ordinal);
        Assert.Contains("Haftanın Havası", mood.ToDisplayText(), StringComparison.Ordinal);
    }

    [Fact]
    public void ReadyMatch_SetsDudukMood()
    {
        var match = PreMatchBriefing.Compose(
            new ManagedFixtureSelectionStatusReadModel(
                1, 1, 1, 2, true, 10, "2026-08-15", IsApproved: true),
            "Rival",
            10);

        var mood = WeekMoodDigest.Compose(
            DecisionDeskDigest.Clear(),
            match,
            PrepOk(),
            LeagueOk());

        Assert.Equal(WeekMoodDigest.MoodMatchReady, mood.MoodCode);
        Assert.Contains("Düdük yakın", mood.MoodLine, StringComparison.Ordinal);
    }

    [Fact]
    public void TodayPulse_PrefersMoodWhenNoStory()
    {
        var mood = WeekMoodDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk());

        var pulse = TodayPulseDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk(),
            weekMood: mood,
            dayNumber: 12);

        Assert.Contains(pulse.PulseLines, l => l.StartsWith("Hava:", StringComparison.Ordinal));
        Assert.Contains(pulse.PulseLines, l => l.StartsWith("Not:", StringComparison.Ordinal));
        Assert.Equal(
            "Not: Staff — Lig ortasında yol alıyorsun.",
            pulse.PulseLines.First(l => l.StartsWith("Not:", StringComparison.Ordinal)));
        Assert.DoesNotContain(pulse.PulseLines, l => l.StartsWith("Hikâye:", StringComparison.Ordinal));
    }

    [Fact]
    public void PlayerExitSellFringe_BeatsPrepDemandMood()
    {
        var prep = PreparationBriefing.Compose(
            new ClubTrainingSummaryReadModel(
                1,
                (int)Domain.TrainingPhysicalState.TrainingFocus.General,
                (int)Domain.TrainingPhysicalState.TrainingIntensity.Medium,
                (int)Domain.TrainingPhysicalState.RestApproach.Normal,
                null, null, null, 1, 30, 70, true, 1, 1,
                InjuredPlayerNames: ["Yorgun"]),
            new TacticPlanReadModel(1, "4-4-2", "Dengeli", 1),
            "±0",
            daysUntilNextMatch: 5);

        var transfer = TransferDeskBriefing.Compose(
            windowOpen: true,
            "Açık",
            90,
            openNeedCount: 1,
            openExitNeedCount: 1,
            listedTargetCount: 0,
            activeProcessCount: 0,
            pendingOfferCount: 0,
            budgetAvailable: null,
            budgetSpent: null,
            squadFull: false,
            saleCandidatePlayerId: 501,
            currentDayNumber: 40);

        var mood = WeekMoodDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            prep,
            LeagueOk(),
            transfer);

        Assert.Equal(WeekMoodDigest.MoodTransfer, mood.MoodCode);
        Assert.Contains("Transfer masası sıcak", mood.MoodLine, StringComparison.Ordinal);
    }

    [Fact]
    public void SittingOutDesk_SetsPlayingTimeMoodLine()
    {
        var desk = new DecisionDeskDigest(
            true,
            true,
            "Masada (zorunlu)",
            "Yedek kaldı — forma süresi istiyor.",
            "destek",
            1,
            "Forma süresi talebi",
            1,
            "Son 3 maçta yedek/kadro dışı — forma istiyor");

        var mood = WeekMoodDigest.Compose(
            desk,
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk(),
            formMomentumCode: DressingRoomEchoDigest.MomentumLosingStreak);

        Assert.Equal(WeekMoodDigest.MoodDesk, mood.MoodCode);
        Assert.Contains("Yedek kalan forma istiyor", mood.MoodLine, StringComparison.Ordinal);
    }

    [Fact]
    public void RedCardDesk_SetsDisciplineMoodLine()
    {
        var desk = new DecisionDeskDigest(
            true,
            true,
            "Masada (zorunlu)",
            "Kırmızı kart — soyunma odasında konuşma.",
            "destek",
            1,
            "Disiplin görüşmesi",
            1,
            "Kırmızı kart gördü — soyunma odasında konuşma şart");

        var mood = WeekMoodDigest.Compose(
            desk,
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk());

        Assert.Equal(WeekMoodDigest.MoodDesk, mood.MoodCode);
        Assert.Contains("Kırmızı kart — soyunma odasını temizle", mood.MoodLine, StringComparison.Ordinal);
    }

    [Fact]
    public void WinningStreak_ReplacesCalmMoodWithKeepRhythmPrompt()
    {
        var mood = WeekMoodDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk(),
            formMomentumCode: DressingRoomEchoDigest.MomentumWinningStreak);

        Assert.Equal(WeekMoodDigest.MoodFormRise, mood.MoodCode);
        Assert.Contains("ritmi koru", mood.MoodLine, StringComparison.Ordinal);
    }

    [Fact]
    public void LosingStreak_ReplacesCalmMoodWithRecoveryPrompt()
    {
        var mood = WeekMoodDigest.Compose(
            DecisionDeskDigest.Clear(),
            PreMatchBriefing.Clear(),
            PrepOk(),
            LeagueOk(),
            formMomentumCode: DressingRoomEchoDigest.MomentumLosingStreak);

        Assert.Equal(WeekMoodDigest.MoodFormCrisis, mood.MoodCode);
        Assert.Contains("toparlanma", mood.MoodLine, StringComparison.Ordinal);
    }

    [Fact]
    public void DueMatchPreparation_BeatsLosingStreakMood()
    {
        var match = PreMatchBriefing.Compose(
            new ManagedFixtureSelectionStatusReadModel(
                1, 1, 1, 2, true, 10, "2026-08-15", IsApproved: false),
            "Rival",
            10);

        var mood = WeekMoodDigest.Compose(
            DecisionDeskDigest.Clear(),
            match,
            PrepOk(),
            LeagueOk(),
            formMomentumCode: DressingRoomEchoDigest.MomentumLosingStreak);

        Assert.Equal(WeekMoodDigest.MoodMatchDraft, mood.MoodCode);
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
