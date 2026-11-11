namespace FootballCareerSimulator.Domain.Transfer;

public readonly record struct ShortlistEntryId : IComparable<ShortlistEntryId>
{
    public long Value { get; }

    public ShortlistEntryId(long value)
    {
        if (value <= 0)
        {
            throw new TransferInvariantViolationException("ShortlistEntryId must be positive.");
        }

        Value = value;
    }

    public int CompareTo(ShortlistEntryId other) => Value.CompareTo(other.Value);
}
