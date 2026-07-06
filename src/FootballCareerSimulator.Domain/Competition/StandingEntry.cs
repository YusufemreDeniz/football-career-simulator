namespace FootballCareerSimulator.Domain.Competition;

using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.Shared;

public sealed class StandingEntry
{
    internal StandingEntry(ClubId clubId)
    {
        ClubId = clubId;
    }

    public ClubId ClubId { get; }

    public int Played { get; private set; }

    public int Won { get; private set; }

    public int Drawn { get; private set; }

    public int Lost { get; private set; }

    public int GoalsFor { get; private set; }

    public int GoalsAgainst { get; private set; }

    public Points Points { get; private set; } = Points.Zero;

    public int GoalDifference => GoalsFor - GoalsAgainst;

    internal void ApplyResult(bool isHomeClub, MatchScore score)
    {
        var goalsFor = isHomeClub ? score.HomeGoals : score.AwayGoals;
        var goalsAgainst = isHomeClub ? score.AwayGoals : score.HomeGoals;

        Played++;
        GoalsFor += goalsFor;
        GoalsAgainst += goalsAgainst;

        if (goalsFor > goalsAgainst)
        {
            Won++;
            Points = Points.Add(new Points(3));
        }
        else if (goalsFor < goalsAgainst)
        {
            Lost++;
        }
        else
        {
            Drawn++;
            Points = Points.Add(new Points(1));
        }
    }
}
