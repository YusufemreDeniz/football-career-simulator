namespace FootballCareerSimulator.Domain.ClubGovernance;

public readonly record struct ClubCode
{
    public string Value { get; }

    public ClubCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ClubGovernanceInvariantViolationException("Club code cannot be empty.");
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length is < 2 or > 4)
        {
            throw new ClubGovernanceInvariantViolationException(
                "Club code must be between 2 and 4 characters.");
        }

        Value = normalized;
    }
}
