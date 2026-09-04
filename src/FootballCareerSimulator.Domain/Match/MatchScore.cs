namespace FootballCareerSimulator.Domain.Match;

public readonly record struct MatchScore
{
    public int HomeGoals { get; }

    public int AwayGoals { get; }

    public MatchScore(int homeGoals, int awayGoals)
    {
        if (homeGoals < 0 || awayGoals < 0)
        {
            throw new MatchInvariantViolationException("Goals cannot be negative.");
        }

        HomeGoals = homeGoals;
        AwayGoals = awayGoals;
    }
}
