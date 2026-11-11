using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.WorldCalendar;

public class GameDateTests
{
    [Fact]
    public void FromCalendarDate_UsesProlepticGregorianDayNumber()
    {
        var date = GameDate.FromCalendarDate(2024, 2, 29);

        Assert.Equal(new DateOnly(2024, 2, 29).DayNumber, date.DayNumber);
        Assert.Equal(2024, date.Year);
        Assert.Equal(2, date.Month);
        Assert.Equal(29, date.Day);
        Assert.Equal("2024-02-29", date.ToIsoDateString());
        Assert.Equal("29.02.2024", date.ToDisplayDateString());
        Assert.Equal("18.08.2026", GameDate.FromCalendarDate(2026, 8, 18).ToDisplayDateString());
    }

    [Fact]
    public void NextDay_AdvancesAcrossMonthBoundary()
    {
        var date = GameDate.FromCalendarDate(2024, 1, 31);

        var next = date.NextDay();

        Assert.Equal(GameDate.FromCalendarDate(2024, 2, 1), next);
    }

    [Fact]
    public void NextDay_AdvancesAcrossLeapYearBoundary()
    {
        var date = GameDate.FromCalendarDate(2024, 2, 28);

        var next = date.NextDay();

        Assert.Equal(GameDate.FromCalendarDate(2024, 2, 29), next);
        Assert.Equal(GameDate.FromCalendarDate(2024, 3, 1), next.NextDay());
    }

    [Fact]
    public void Comparison_IsMonotonicByDayNumber()
    {
        var earlier = GameDate.FromCalendarDate(2026, 7, 1);
        var later = GameDate.FromCalendarDate(2026, 7, 6);

        Assert.True(earlier.IsBefore(later));
        Assert.True(later.IsAfter(earlier));
        Assert.True(earlier < later);
    }

    [Fact]
    public void FromDayNumber_RejectsValuesBelowMinimum()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GameDate.FromDayNumber(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => GameDate.FromDayNumber(-1));
    }

    [Fact]
    public void AddDays_RejectsNegativeAdvance()
    {
        var date = GameDate.FromCalendarDate(2026, 7, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => date.AddDays(-1));
    }

    [Fact]
    public void FromCalendarDate_RejectsInvalidGregorianDate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GameDate.FromCalendarDate(2023, 2, 29));
    }
}
