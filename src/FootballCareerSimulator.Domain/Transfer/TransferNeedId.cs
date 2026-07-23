namespace FootballCareerSimulator.Domain.Transfer;

public readonly record struct TransferNeedId : IComparable<TransferNeedId>
{
    public long Value { get; }

    public TransferNeedId(long value)
    {
        if (value <= 0)
        {
            throw new TransferInvariantViolationException("TransferNeedId must be positive.");
        }

        Value = value;
    }

    public int CompareTo(TransferNeedId other) => Value.CompareTo(other.Value);
}
