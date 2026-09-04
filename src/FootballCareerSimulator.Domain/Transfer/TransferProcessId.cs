namespace FootballCareerSimulator.Domain.Transfer;

public readonly record struct TransferProcessId : IComparable<TransferProcessId>
{
    public long Value { get; }

    public TransferProcessId(long value)
    {
        if (value <= 0)
        {
            throw new TransferInvariantViolationException("TransferProcessId must be positive.");
        }

        Value = value;
    }

    public int CompareTo(TransferProcessId other) => Value.CompareTo(other.Value);
}
