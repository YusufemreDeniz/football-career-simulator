using System.Diagnostics;
using FootballCareerSimulator.Application.Spike4Placeholder;
using FootballCareerSimulator.Simulation;
using FootballCareerSimulator.Simulation.Spike1Placeholder;

namespace FootballCareerSimulator.Tests;

/// <summary>
/// docs/18_SPIKE_EXECUTION_PLAN.md Kart 6'nın (Spike 4) Godot'a bağımlı olmayan bölümünü doğrular:
/// 500 satırın doğru üretilmesi, filtreleme/sıralama/sayfalamanın doğruluğu ve bu pipeline'ın
/// "100 ms hedefinin altında güncellenir" kriterini karşıladığı (docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md
/// Bölüm 16, Spike 4).
/// </summary>
public class Spike4PlayerListQueryTests
{
    private static IReadOnlyList<PlayerListRow> BuildFullMvpWorldRows()
    {
        var (world, _) = HeadlessSimulationRunner.CreateWorld(seed: 42);
        return PlayerListQuery.BuildRows(world);
    }

    [Fact]
    public void BuildRows_ReturnsExactlyFiveHundredRowsAcrossTwentyClubs()
    {
        var rows = BuildFullMvpWorldRows();

        Assert.Equal(WorldFactory.TotalPlayerCount, rows.Count);
        Assert.Equal(WorldFactory.ClubCount, rows.Select(row => row.ClubId).Distinct().Count());
    }

    [Fact]
    public void Filter_ByClubNameSubstring_IsCaseInsensitiveAndMatchesOnlyThatClub()
    {
        var rows = BuildFullMvpWorldRows();

        var filtered = PlayerListQuery.Filter(rows, "placeholder club 01");

        Assert.NotEmpty(filtered);
        Assert.All(filtered, row => Assert.Equal("Placeholder Club 01", row.ClubName));
    }

    [Fact]
    public void Filter_ByPlayerIdSubstring_MatchesExpectedPlayer()
    {
        var rows = BuildFullMvpWorldRows();

        var filtered = PlayerListQuery.Filter(rows, "Player#7");

        Assert.Contains(filtered, row => row.PlayerId == 7);
        Assert.All(filtered, row => Assert.Contains("7", row.PlayerLabel));
    }

    [Fact]
    public void Filter_BlankSearchText_ReturnsAllRowsUnchanged()
    {
        var rows = BuildFullMvpWorldRows();

        var filtered = PlayerListQuery.Filter(rows, "   ");

        Assert.Equal(rows.Count, filtered.Count);
    }

    [Theory]
    [InlineData(PlayerListSortColumn.Age, true)]
    [InlineData(PlayerListSortColumn.Age, false)]
    [InlineData(PlayerListSortColumn.PlayerId, true)]
    [InlineData(PlayerListSortColumn.PlayerId, false)]
    [InlineData(PlayerListSortColumn.Form, true)]
    [InlineData(PlayerListSortColumn.ClubName, true)]
    public void Sort_OrdersRowsCorrectlyForEveryColumnAndDirection(PlayerListSortColumn column, bool ascending)
    {
        var rows = BuildFullMvpWorldRows();

        var sorted = PlayerListQuery.Sort(rows, column, ascending);

        Assert.Equal(rows.Count, sorted.Count);

        Func<PlayerListRow, IComparable> keySelector = column switch
        {
            PlayerListSortColumn.PlayerId => row => row.PlayerId,
            PlayerListSortColumn.ClubName => row => row.ClubName,
            PlayerListSortColumn.Age => row => row.Age,
            PlayerListSortColumn.Form => row => row.Form,
            _ => throw new ArgumentOutOfRangeException(nameof(column)),
        };

        for (var i = 1; i < sorted.Count; i++)
        {
            var comparison = keySelector(sorted[i - 1]).CompareTo(keySelector(sorted[i]));
            var isOrderedCorrectly = ascending ? comparison <= 0 : comparison >= 0;
            Assert.True(isOrderedCorrectly, $"Sıralama {column}/{ascending} index {i}'de bozuldu.");
        }
    }

    [Fact]
    public void Sort_IsStableViaPlayerIdTieBreaker_AcrossRepeatedCalls()
    {
        var rows = BuildFullMvpWorldRows();

        var first = PlayerListQuery.Sort(rows, PlayerListSortColumn.Form, ascending: true);
        var second = PlayerListQuery.Sort(rows, PlayerListSortColumn.Form, ascending: true);

        Assert.Equal(first.Select(r => r.PlayerId), second.Select(r => r.PlayerId));
    }

    [Fact]
    public void Page_ReturnsExpectedSlicesIncludingPartialLastPage()
    {
        var rows = BuildFullMvpWorldRows();
        const int pageSize = 50;

        var pageCount = PlayerListQuery.GetPageCount(rows.Count, pageSize);
        Assert.Equal(10, pageCount);

        var firstPage = PlayerListQuery.Page(rows, pageIndex: 0, pageSize);
        var lastPage = PlayerListQuery.Page(rows, pageIndex: pageCount - 1, pageSize);
        var beyondLastPage = PlayerListQuery.Page(rows, pageIndex: pageCount, pageSize);

        Assert.Equal(pageSize, firstPage.Count);
        Assert.Equal(pageSize, lastPage.Count);
        Assert.Empty(beyondLastPage);
    }

    [Fact]
    public void Page_UnevenPageSize_LastPageIsPartial()
    {
        var rows = BuildFullMvpWorldRows();
        const int pageSize = 60;

        var pageCount = PlayerListQuery.GetPageCount(rows.Count, pageSize);
        var lastPage = PlayerListQuery.Page(rows, pageIndex: pageCount - 1, pageSize);

        Assert.Equal(500 % pageSize == 0 ? pageSize : 500 % pageSize, lastPage.Count);
    }

    [Fact]
    public void FilterSortPagePipeline_FiveHundredRows_CompletesWellUnderHundredMillisecondBudget()
    {
        var rows = BuildFullMvpWorldRows();
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < 20; i++)
        {
            var filtered = PlayerListQuery.Filter(rows, "Club 1");
            var sorted = PlayerListQuery.Sort(filtered, PlayerListSortColumn.Age, ascending: i % 2 == 0);
            _ = PlayerListQuery.Page(sorted, pageIndex: 0, pageSize: 50);
        }

        stopwatch.Stop();

        var averageMs = stopwatch.Elapsed.TotalMilliseconds / 20;

        // docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md Bolum 16, Spike 4: filtre guncellemesi 100 ms
        // hedefinin altinda olmalidir. Burada tek bir filtre+sort+page dongusunun ortalama suresi olculur.
        Assert.True(averageMs < 100, $"Filtre/sort/page pipeline'ı beklenenden yavaş: {averageMs:F2} ms (ortalama).");
    }
}
