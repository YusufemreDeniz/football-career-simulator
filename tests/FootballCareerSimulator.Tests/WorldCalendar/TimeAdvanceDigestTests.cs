using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Queries;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.WorldCalendar;

public sealed class TimeAdvanceDigestTests
{
    [Fact]
    public void QuietWeek_HasCalmHeadline()
    {
        var from = GameDate.FromCalendarDate(2026, 8, 18);
        var to = GameDate.FromCalendarDate(2026, 8, 25);
        var digest = TimeAdvanceDigest.Compose(
            QuietAdvance(from.DayNumber, to.DayNumber),
            requestedDayCount: 7);

        Assert.Equal("Hafta Özeti", digest.BrandTitle);
        Assert.Equal("Sakin bir hafta — sahaya odaklan.", digest.Headline);
        Assert.Equal("18.08.2026 → 25.08.2026", digest.SpanLine);
        Assert.Empty(digest.BeatLines);
    }

    [Fact]
    public void PromiseCrisis_LeadsHeadline()
    {
        var result = QuietAdvance(5, 6) with
        {
            PromiseDeadlineResolvedCount = 1,
            PromiseBrokenCrisisOpenedCount = 1,
        };

        var digest = TimeAdvanceDigest.Compose(result, requestedDayCount: 1);
        Assert.Equal("Gün Özeti", digest.BrandTitle);
        Assert.Equal("Ofiste fırtına — sözler bozuldu.", digest.Headline);
        Assert.Contains(digest.BeatLines, b => b.Contains("söz ihlali", StringComparison.Ordinal));
    }

    [Fact]
    public void StatusMessage_IsReadableParagraph()
    {
        var digest = TimeAdvanceDigest.Compose(
            QuietAdvance(1, 8) with { ExpiredContractCount = 2, NewlyFreeAgentPlayerIds = [10, 11] },
            requestedDayCount: 7,
            nextMatchHint: "Sıradaki maç: Ev vs Rival — kadro onayı bekliyor.");

        var text = digest.ToStatusMessage();
        Assert.Contains("Hafta Özeti", text, StringComparison.Ordinal);
        Assert.Contains("Sözleşme masası", text, StringComparison.Ordinal);
        Assert.Contains("· 2 sözleşme bitti", text, StringComparison.Ordinal);
        Assert.Contains("Sıradaki maç", text, StringComparison.Ordinal);
    }

    private static AdvanceSimulationTimeResult QuietAdvance(int from, int to) =>
        AdvanceSimulationTimeResult.Advanced(
            from,
            to,
            Array.Empty<string>());
}
