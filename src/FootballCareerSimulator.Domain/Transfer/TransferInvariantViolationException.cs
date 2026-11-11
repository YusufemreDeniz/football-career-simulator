namespace FootballCareerSimulator.Domain.Transfer;

public sealed class TransferInvariantViolationException : Exception
{
    public TransferInvariantViolationException(string message)
        : base(message)
    {
    }
}
