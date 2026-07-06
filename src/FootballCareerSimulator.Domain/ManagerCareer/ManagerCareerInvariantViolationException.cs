namespace FootballCareerSimulator.Domain.ManagerCareer;

public sealed class ManagerCareerInvariantViolationException : Exception
{
    public ManagerCareerInvariantViolationException(string message)
        : base(message)
    {
    }
}
