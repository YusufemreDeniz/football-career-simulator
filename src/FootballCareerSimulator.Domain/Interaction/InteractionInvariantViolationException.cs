namespace FootballCareerSimulator.Domain.Interaction;

public sealed class InteractionInvariantViolationException : Exception
{
    public InteractionInvariantViolationException(string message)
        : base(message)
    {
    }
}
