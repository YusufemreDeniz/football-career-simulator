using FootballCareerSimulator.Application.CareerHub.Queries;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class MatchDayTempoFlashTests
{
    private static WeekMoodDigest Mood(string code) =>
        new(true, WeekMoodDigest.Brand, "hava", code);

    [Fact]
    public void ResolveArrival_MatchReady_SaysWhistleClose()
    {
        var flash = MatchDayTempoFlash.ResolveArrival(
            Mood(WeekMoodDigest.MoodMatchReady),
            hasDueMatch: true);

        Assert.NotNull(flash);
        Assert.Contains("kadro kilitli", flash.BeatLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("düdük", flash.BeatLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("çalabilirsin", flash.AdviceLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveArrival_MatchDraft_WaitsForApproval()
    {
        var flash = MatchDayTempoFlash.ResolveArrival(
            Mood(WeekMoodDigest.MoodMatchDraft),
            hasDueMatch: true);

        Assert.NotNull(flash);
        Assert.Contains("kadro kilidi bekliyor", flash.BeatLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Düdük kapalı", flash.AdviceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveArrival_PromiseMood_WarnsAboutSwap()
    {
        var flash = MatchDayTempoFlash.ResolveArrival(
            Mood(WeekMoodDigest.MoodPromise),
            hasDueMatch: true);

        Assert.NotNull(flash);
        Assert.Contains("söz gerilimi", flash.BeatLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveArrival_CalmMatch_TempoNotSettled()
    {
        var flash = MatchDayTempoFlash.ResolveArrival(
            Mood(WeekMoodDigest.MoodCalmMatch),
            hasDueMatch: true);

        Assert.NotNull(flash);
        Assert.Contains("tempo henüz oturmadı", flash.BeatLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveArrival_InjuryPressure_OverridesMood()
    {
        var flash = MatchDayTempoFlash.ResolveArrival(
            Mood(WeekMoodDigest.MoodMatchReady),
            hasDueMatch: true,
            hasInjuryPressure: true);

        Assert.NotNull(flash);
        Assert.Contains("sakatlık baskısı", flash.BeatLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveArrival_NoDueMatch_ReturnsNull()
    {
        Assert.Null(MatchDayTempoFlash.ResolveArrival(
            Mood(WeekMoodDigest.MoodMatchReady),
            hasDueMatch: false));
    }

    [Fact]
    public void ResolveArrival_InactiveMood_ReturnsNull()
    {
        Assert.Null(MatchDayTempoFlash.ResolveArrival(
            WeekMoodDigest.Clear(),
            hasDueMatch: true));
    }

    [Fact]
    public void ResolveArrival_CalmMood_NoTempoFlash()
    {
        Assert.Null(MatchDayTempoFlash.ResolveArrival(
            Mood(WeekMoodDigest.MoodCalm),
            hasDueMatch: true));
    }
}
