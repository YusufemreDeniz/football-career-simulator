namespace FootballCareerSimulator.Domain.Competition;

public readonly record struct SeasonId : IComparable<SeasonId>
{
    public long Value { get; }

    public SeasonId(long value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Season id must be positive.");
        }

        Value = value;
    }

    public int CompareTo(SeasonId other) => Value.CompareTo(other.Value);
}
