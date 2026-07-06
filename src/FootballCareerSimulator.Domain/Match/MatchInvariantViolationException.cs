namespace FootballCareerSimulator.Domain.Match;

public sealed class MatchInvariantViolationException : Exception
{
    public MatchInvariantViolationException(string message)
        : base(message)
    {
    }
}
