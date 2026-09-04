namespace FootballCareerSimulator.Domain.WorldCalendar;

/// <summary>
/// Oyun içi yetkili tarih. Canonical temsil <see cref="DayNumber"/> (proleptic Gregorian gün sayısı);
/// yıl/ay/gün yalnızca türetilmiş projection'dır (bkz. docs/19_PRODUCTION_IMPLEMENTATION_PLAN.md Bölüm 5.7, D-343).
/// </summary>
public readonly record struct GameDate : IComparable<GameDate>
{
    public const int MinDayNumber = 1;

    public int DayNumber { get; }

    private GameDate(int dayNumber) => DayNumber = dayNumber;

    public static GameDate FromDayNumber(int dayNumber)
    {
        if (dayNumber < MinDayNumber)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dayNumber),
                dayNumber,
                $"Day number must be at least {MinDayNumber}.");
        }

        _ = DateOnly.FromDayNumber(dayNumber);
        return new GameDate(dayNumber);
    }

    public static GameDate FromCalendarDate(int year, int month, int day)
    {
        var calendarDate = new DateOnly(year, month, day);
        return new GameDate(calendarDate.DayNumber);
    }

    public int Year => ToDateOnly().Year;

    public int Month => ToDateOnly().Month;

    public int Day => ToDateOnly().Day;

    public GameDate AddDays(int days)
    {
        if (days < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days), days, "Cannot advance game date by a negative number of days.");
        }

        return FromDayNumber(checked(DayNumber + days));
    }

    public GameDate NextDay() => AddDays(1);

    public bool IsBefore(GameDate other) => DayNumber < other.DayNumber;

    public bool IsAfter(GameDate other) => DayNumber > other.DayNumber;

    public string ToIsoDateString() => ToDateOnly().ToString("yyyy-MM-dd");

    /// <summary>Oyuncu yüzü: gerçek takvim tarihi, ham gün numarası değil.</summary>
    public string ToDisplayDateString() => ToDateOnly().ToString("dd.MM.yyyy");

    public static string ToDisplayDateString(int dayNumber) =>
        FromDayNumber(dayNumber).ToDisplayDateString();

    public int CompareTo(GameDate other) => DayNumber.CompareTo(other.DayNumber);

    public static bool operator <(GameDate left, GameDate right) => left.DayNumber < right.DayNumber;

    public static bool operator >(GameDate left, GameDate right) => left.DayNumber > right.DayNumber;

    public static bool operator <=(GameDate left, GameDate right) => left.DayNumber <= right.DayNumber;

    public static bool operator >=(GameDate left, GameDate right) => left.DayNumber >= right.DayNumber;

    private DateOnly ToDateOnly() => DateOnly.FromDayNumber(DayNumber);
}
