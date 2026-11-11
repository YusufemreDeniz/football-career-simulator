namespace FootballCareerSimulator.Domain.Competition;

public readonly record struct CompetitionId : IComparable<CompetitionId>
{
    public long Value { get; }

    public CompetitionId(long value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Competition id must be positive.");
        }

        Value = value;
    }

    public int CompareTo(CompetitionId other) => Value.CompareTo(other.Value);
}
