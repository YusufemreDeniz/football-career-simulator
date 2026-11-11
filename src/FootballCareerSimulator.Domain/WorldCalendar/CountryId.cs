namespace FootballCareerSimulator.Domain.WorldCalendar;

public readonly record struct CountryId : IComparable<CountryId>
{
    public long Value { get; }

    public CountryId(long value)
    {
        if (value < 1)
        {
            throw new WorldCalendarInvariantViolationException("Country id must be positive.");
        }

        Value = value;
    }

    public int CompareTo(CountryId other) => Value.CompareTo(other.Value);
}
