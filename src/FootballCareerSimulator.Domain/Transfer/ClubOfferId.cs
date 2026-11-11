namespace FootballCareerSimulator.Domain.Transfer;

public readonly record struct ClubOfferId : IComparable<ClubOfferId>
{
    public long Value { get; }

    public ClubOfferId(long value)
    {
        if (value <= 0)
        {
            throw new TransferInvariantViolationException("ClubOfferId must be positive.");
        }

        Value = value;
    }

    public int CompareTo(ClubOfferId other) => Value.CompareTo(other.Value);
}
