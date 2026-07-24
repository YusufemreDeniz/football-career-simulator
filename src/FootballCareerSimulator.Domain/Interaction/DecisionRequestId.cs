namespace FootballCareerSimulator.Domain.Interaction;

public readonly record struct DecisionRequestId : IComparable<DecisionRequestId>
{
    public DecisionRequestId(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Decision request id must be positive.");
        }

        Value = value;
    }

    public long Value { get; }

    public int CompareTo(DecisionRequestId other) => Value.CompareTo(other.Value);
}
