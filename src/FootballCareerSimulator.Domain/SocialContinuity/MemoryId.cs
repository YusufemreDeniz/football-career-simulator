namespace FootballCareerSimulator.Domain.SocialContinuity;

public readonly record struct MemoryId : IComparable<MemoryId>
{
    public MemoryId(long value)
    {
        if (value <= 0)
        {
            throw new SocialContinuityInvariantViolationException("Memory id must be positive.");
        }

        Value = value;
    }

    public long Value { get; }

    public int CompareTo(MemoryId other) => Value.CompareTo(other.Value);
}
