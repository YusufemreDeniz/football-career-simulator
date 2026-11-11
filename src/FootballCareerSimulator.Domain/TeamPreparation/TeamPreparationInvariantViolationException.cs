namespace FootballCareerSimulator.Domain.TeamPreparation;

public sealed class TeamPreparationInvariantViolationException : Exception
{
    public TeamPreparationInvariantViolationException(string message)
        : base(message)
    {
    }
}
