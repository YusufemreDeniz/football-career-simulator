namespace FootballCareerSimulator.Domain.PlayerCareer;

public sealed class PlayerCareerInvariantViolationException : Exception
{
    public PlayerCareerInvariantViolationException(string message)
        : base(message)
    {
    }
}
