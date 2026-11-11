namespace FootballCareerSimulator.Domain.SocialContinuity;

public readonly record struct PromiseId : IComparable<PromiseId>
{
    public PromiseId(long value)
    {
        if (value <= 0)
        {
            throw new SocialContinuityInvariantViolationException("Promise id must be positive.");
        }

        Value = value;
    }

    public long Value { get; }

    public int CompareTo(PromiseId other) => Value.CompareTo(other.Value);
}
