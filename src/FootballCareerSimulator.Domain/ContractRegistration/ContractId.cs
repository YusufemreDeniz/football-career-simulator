namespace FootballCareerSimulator.Domain.ContractRegistration;

public readonly record struct ContractId : IComparable<ContractId>
{
    public long Value { get; }

    public ContractId(long value)
    {
        if (value <= 0)
        {
            throw new ContractRegistrationInvariantViolationException("ContractId must be positive.");
        }

        Value = value;
    }

    public int CompareTo(ContractId other) => Value.CompareTo(other.Value);

    public static ContractId ForPlayer(long playerId) => new(playerId);
}
