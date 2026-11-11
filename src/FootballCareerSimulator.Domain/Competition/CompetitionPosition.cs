namespace FootballCareerSimulator.Domain.Competition;

public readonly record struct CompetitionPosition : IComparable<CompetitionPosition>
{
    public int Value { get; }

    public CompetitionPosition(int value)
    {
        if (value is < 1 or > CompetitionMvpConstraints.MaxLeaguePosition)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Competition position must be between 1 and {CompetitionMvpConstraints.MaxLeaguePosition}.");
        }

        Value = value;
    }

    public int CompareTo(CompetitionPosition other) => Value.CompareTo(other.Value);
}
