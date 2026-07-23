namespace FootballCareerSimulator.Domain.Transfer;

public readonly record struct TransferTargetId : IComparable<TransferTargetId>
{
    public long Value { get; }

    public TransferTargetId(long value)
    {
        if (value <= 0)
        {
            throw new TransferInvariantViolationException("TransferTargetId must be positive.");
        }

        Value = value;
    }

    public int CompareTo(TransferTargetId other) => Value.CompareTo(other.Value);
}
