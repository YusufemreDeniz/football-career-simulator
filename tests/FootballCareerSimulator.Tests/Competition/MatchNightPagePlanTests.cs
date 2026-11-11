using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Competition;

public sealed class MatchNightPagePlanTests
{
    private static MatchNightNarrative BaseNarrative(
        IReadOnlyList<string>? beats = null,
        IReadOnlyList<string>? after = null,
        IReadOnlyList<string>? others = null,
        IReadOnlyList<string>? kickoff = null) =>
        MatchNightNarrative.Compose(
            "A 1-0 B",
            1,
            0,
            managedIsHome: true,
            hasManagedMatch: true,
            tacticNote: null,
            dayNumber: GameDate.FromCalendarDate(2026, 8, 15).DayNumber,
            beatLines: beats ?? Array.Empty<string>(),
            afterWhistleLines: after ?? Array.Empty<string>(),
            otherScorelines: others ?? Array.Empty<string>(),
            kickoffLines: kickoff);

    [Fact]
    public void FullManagedNight_IsThreePages_WithDevamThenCareer()
    {
        var narrative = BaseNarrative(
            beats: ["1. Yarı", "12' gol"],
            after: ["Yönetim güveni -6 → 49 (UnderReview)"],
            others: ["C 0-0 D"],
            kickoff: ["Maça girdin."]);

        var pages = MatchNightPagePlan.Build(
            narrative,
            hasReport: true,
            hasTechnicalArea: true,
            hasRoundup: true,
            hasDressingRoom: true);

        Assert.Equal(3, pages.Count);
        Assert.Equal(MatchNightPageKind.Score, pages[0].Kind);
        Assert.Equal("Devam", pages[0].ContinueLabel);
        Assert.False(pages[0].IsFinal);

        Assert.Equal(MatchNightPageKind.Match, pages[1].Kind);
        Assert.Equal("Devam", pages[1].ContinueLabel);

        Assert.Equal(MatchNightPageKind.Aftermath, pages[2].Kind);
        Assert.Equal("Kariyere Dön", pages[2].ContinueLabel);
        Assert.True(pages[2].IsFinal);
        Assert.Equal("03", pages[2].MarkerCode);
    }

    [Fact]
    public void ScoreOnly_IsSingleCareerReturnPage()
    {
        var pages = MatchNightPagePlan.Build(
            BaseNarrative(),
            hasReport: false,
            hasTechnicalArea: false,
            hasRoundup: false,
            hasDressingRoom: true);

        Assert.Single(pages);
        Assert.Equal(MatchNightPageKind.Score, pages[0].Kind);
        Assert.Equal("Kariyere Dön", pages[0].ContinueLabel);
        Assert.True(pages[0].IsFinal);
        Assert.Equal("01", pages[0].MarkerCode);
    }

    [Fact]
    public void SkipsEmptyMatchPage_WhenOnlyAftermathExists()
    {
        var pages = MatchNightPagePlan.Build(
            BaseNarrative(after: ["Basın sorusu açıldı."]),
            hasReport: false,
            hasTechnicalArea: false,
            hasRoundup: false,
            hasDressingRoom: false);

        Assert.Equal(2, pages.Count);
        Assert.Equal(MatchNightPageKind.Score, pages[0].Kind);
        Assert.Equal(MatchNightPageKind.Aftermath, pages[1].Kind);
        Assert.Equal("02", pages[1].MarkerCode);
        Assert.Equal("Kariyere Dön", pages[1].ContinueLabel);
    }

    [Fact]
    public void PreferCriticalAfterWhistle_KeepsBoardDemandLine()
    {
        var narrative = MatchNightNarrative.Compose(
            "A 0-1 B",
            0,
            1,
            managedIsHome: true,
            hasManagedMatch: true,
            tacticNote: null,
            dayNumber: GameDate.FromCalendarDate(2026, 8, 15).DayNumber,
            beatLines: Array.Empty<string>(),
            afterWhistleLines:
            [
                "Devre arasında aynı planla devam ettin.",
                "Yedek değişiklik notu.",
                "Yönetim talebi açıldı.",
                "Forma süresi talebi açıldı.",
            ],
            otherScorelines: Array.Empty<string>());

        Assert.Contains(narrative.AfterWhistleLines, line => line.Contains("Yönetim talebi", StringComparison.Ordinal));
        Assert.Contains(narrative.AfterWhistleLines, line => line.Contains("forma süresi", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, narrative.AfterWhistleLines.Count);
    }
}
