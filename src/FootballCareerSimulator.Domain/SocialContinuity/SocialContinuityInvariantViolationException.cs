namespace FootballCareerSimulator.Domain.SocialContinuity;

public sealed class SocialContinuityInvariantViolationException : Exception
{
    public SocialContinuityInvariantViolationException(string message)
        : base(message)
    {
    }
}
