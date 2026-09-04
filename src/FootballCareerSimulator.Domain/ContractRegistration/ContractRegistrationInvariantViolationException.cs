namespace FootballCareerSimulator.Domain.ContractRegistration;

public sealed class ContractRegistrationInvariantViolationException : Exception
{
    public ContractRegistrationInvariantViolationException(string message)
        : base(message)
    {
    }
}
