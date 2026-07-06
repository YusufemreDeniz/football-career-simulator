namespace FootballCareerSimulator.Domain.ClubGovernance;

public sealed class ClubGovernanceInvariantViolationException : Exception
{
    public ClubGovernanceInvariantViolationException(string message)
        : base(message)
    {
    }
}
