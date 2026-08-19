using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class MatchKickoffMomentTests
{
    private static WeekMoodDigest Mood(string code) =>
        new(true, WeekMoodDigest.Brand, "hava", code);

    private static PreMatchBriefing Briefing(bool approved)
    {
        var pending = new ManagedFixtureSelectionStatusReadModel(
            FixtureId: 100,
            SeasonId: 1,
            ManagedClubId: 1,
            OpponentClubId: 2,
            IsHome: true,
            ScheduledDayNumber: 12,
            ScheduledIsoDate: "2026-08-15",
            IsApproved: approved);
        return PreMatchBriefing.Compose(
            pending,
            opponentName: "Rival FC",
            currentDayNumber: 12,
            formationName: "4-3-3",
            approachName: "Dengeli");
    }

    [Fact]
    public void Compose_ReadyMatch_CarriesTempoFlashIntoWhistle()
    {
        var flash = MatchDayTempoFlash.ResolveArrival(
            Mood(WeekMoodDigest.MoodMatchReady),
            hasDueMatch: true);
        var moment = MatchKickoffMoment.Compose(Briefing(approved: true), flash);

        Assert.True(moment.HasMatch);
        Assert.True(moment.IsReadyToKickOff);
        Assert.Contains("Düdük çaldı", moment.Headline, StringComparison.Ordinal);
        Assert.Contains("kadro kilitli", moment.BeatLines[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains(moment.BeatLines, b => b.Contains("düdük çaldı", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Compose_NoTempoFlash_StillShowsKickoffBridge()
    {
        var moment = MatchKickoffMoment.Compose(Briefing(approved: true), tempoFlash: null);

        Assert.True(moment.HasMatch);
        Assert.Contains(moment.BeatLines, b => b.Contains("düdük çaldı", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(moment.BeatLines, b => b.Contains("Taktik", StringComparison.Ordinal));
        Assert.Equal("Düdük çaldı — maç başladı.", moment.Headline);
    }

    [Fact]
    public void Compose_FixtureLineNotDuplicatedInsideBeats()
    {
        var moment = MatchKickoffMoment.Compose(Briefing(approved: true), tempoFlash: null);

        Assert.StartsWith("Ev vs Rival FC", moment.FixtureLine, StringComparison.Ordinal);
        Assert.DoesNotContain(moment.BeatLines, b => b.Contains("Rival FC", StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_UnapprovedMatch_WhistleClosed()
    {
        var flash = MatchDayTempoFlash.ResolveArrival(
            Mood(WeekMoodDigest.MoodMatchDraft),
            hasDueMatch: true);
        var moment = MatchKickoffMoment.Compose(Briefing(approved: false), flash);

        Assert.True(moment.HasMatch);
        Assert.False(moment.IsReadyToKickOff);
        Assert.Contains("Düdük kapalı", moment.Headline, StringComparison.Ordinal);
        Assert.Contains(moment.BeatLines, b => b.Contains("kadro kilidi bekliyor", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Compose_NoMatch_ReturnsClear()
    {
        var moment = MatchKickoffMoment.Compose(PreMatchBriefing.Clear(), tempoFlash: null);

        Assert.False(moment.HasMatch);
        Assert.False(moment.IsReadyToKickOff);
        Assert.Contains("Düdük kapalı", moment.Headline, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_DisplayText_ReadsLikeMatchEntrance()
    {
        var flash = MatchDayTempoFlash.ResolveArrival(
            Mood(WeekMoodDigest.MoodMatchReady),
            hasDueMatch: true);
        var moment = MatchKickoffMoment.Compose(Briefing(approved: true), flash);

        var text = moment.ToDisplayText();
        Assert.Contains("Maç Nabzı", text, StringComparison.Ordinal);
        Assert.Contains("Düdük çaldı", text, StringComparison.Ordinal);
        Assert.Contains("· Tempo oturdu", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_WinningStreak_PutsFourthWinOnTheLine()
    {
        var moment = MatchKickoffMoment.Compose(
            Briefing(approved: true),
            tempoFlash: null,
            formMomentumCode: DressingRoomEchoDigest.MomentumWinningStreak,
            formMomentumLength: 4);

        Assert.Contains(
            moment.BeatLines,
            beat => beat.Contains("5. zafer", StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_LosingStreak_FramesKickoffAsBreakingPoint()
    {
        var moment = MatchKickoffMoment.Compose(
            Briefing(approved: true),
            tempoFlash: null,
            formMomentumCode: DressingRoomEchoDigest.MomentumLosingStreak,
            formMomentumLength: 5);

        Assert.Contains(
            moment.BeatLines,
            beat => beat.Contains("kırılma maçı", StringComparison.Ordinal));
        Assert.Contains(
            moment.BeatLines,
            beat => beat.Contains("5 maçlık", StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_MixedForm_DoesNotInventStreakPressure()
    {
        var moment = MatchKickoffMoment.Compose(
            Briefing(approved: true),
            tempoFlash: null,
            formMomentumCode: DressingRoomEchoDigest.MomentumMixed);

        Assert.DoesNotContain(
            moment.BeatLines,
            beat => beat.Contains("maçlık", StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_BothTeamsOnWinningRuns_FramesAStreakBattle()
    {
        var moment = MatchKickoffMoment.Compose(
            Briefing(approved: true),
            tempoFlash: null,
            formMomentumCode: DressingRoomEchoDigest.MomentumWinningStreak,
            formMomentumLength: 5,
            opponentWinningStreakLength: 4);

        Assert.Contains(
            moment.BeatLines,
            beat => beat.Contains("Seri savaşı", StringComparison.Ordinal));
        Assert.Contains(
            moment.BeatLines,
            beat => beat.Contains("5 maçlık", StringComparison.Ordinal)
                && beat.Contains("4 maçlık", StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_OnlyOpponentOnWinningRun_CreatesStopTheRunChallenge()
    {
        var moment = MatchKickoffMoment.Compose(
            Briefing(approved: true),
            tempoFlash: null,
            formMomentumCode: DressingRoomEchoDigest.MomentumMixed,
            opponentWinningStreakLength: 6);

        Assert.Contains(
            moment.BeatLines,
            beat => beat.Contains("6 maçlık", StringComparison.Ordinal)
                && beat.Contains("durdurma sınavı", StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_LosingRunAgainstWinningOpponent_FramesACrisisMatch()
    {
        var moment = MatchKickoffMoment.Compose(
            Briefing(approved: true),
            tempoFlash: null,
            formMomentumCode: DressingRoomEchoDigest.MomentumLosingStreak,
            formMomentumLength: 4,
            opponentWinningStreakLength: 5);

        Assert.Contains(
            moment.BeatLines,
            beat => beat.Contains("Kriz maçı", StringComparison.Ordinal)
                && beat.Contains("4 yenilgiyi", StringComparison.Ordinal)
                && beat.Contains("5 galibiyeti", StringComparison.Ordinal));
    }
}
