namespace FootballCareerSimulator.Domain.Transfer;

public readonly record struct PlayerContractProposalId : IComparable<PlayerContractProposalId>
{
    public long Value { get; }

    public PlayerContractProposalId(long value)
    {
        if (value <= 0)
        {
            throw new TransferInvariantViolationException("PlayerContractProposalId must be positive.");
        }

        Value = value;
    }

    public int CompareTo(PlayerContractProposalId other) => Value.CompareTo(other.Value);
}
