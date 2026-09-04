namespace FootballCareerSimulator.Domain.Competition;

public sealed class CompetitionInvariantViolationException : Exception
{
    public CompetitionInvariantViolationException(string message)
        : base(message)
    {
    }
}
