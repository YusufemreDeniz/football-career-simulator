namespace FootballCareerSimulator.Domain.Competition;

public readonly record struct FixtureRound : IComparable<FixtureRound>
{
    public int Value { get; }

    public FixtureRound(int value)
    {
        if (value is < 1 or > CompetitionMvpConstraints.MaxLeagueFixtureRound)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Fixture round must be between 1 and {CompetitionMvpConstraints.MaxLeagueFixtureRound}.");
        }

        Value = value;
    }

    public int CompareTo(FixtureRound other) => Value.CompareTo(other.Value);
}
