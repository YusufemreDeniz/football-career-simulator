namespace FootballCareerSimulator.Domain.WorldCalendar;

public readonly record struct PlanningPeriodId : IComparable<PlanningPeriodId>
{
    public long Value { get; }

    public PlanningPeriodId(long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Planning period id cannot be negative.");
        }

        Value = value;
    }

    public int CompareTo(PlanningPeriodId other) => Value.CompareTo(other.Value);
}
