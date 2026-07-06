namespace FootballCareerSimulator.Domain.Competition;

public readonly record struct FixtureId : IComparable<FixtureId>
{
    public long Value { get; }

    public FixtureId(long value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Fixture id must be positive.");
        }

        Value = value;
    }

    public int CompareTo(FixtureId other) => Value.CompareTo(other.Value);
}
