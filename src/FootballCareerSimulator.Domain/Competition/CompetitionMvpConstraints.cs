namespace FootballCareerSimulator.Domain.Competition;

/// <summary>
/// Lig yapısı sabitleri. 18, Super Lig veri paketi ve mevcut soak yoludur.
/// 20, MVP production dünya bootstrap'idir (docs/02_MVP_SCOPE.md Bölüm 17.2, D-022).
/// </summary>
public static class CompetitionMvpConstraints
{
    public const int LeagueTeamCount = 18;

    public const int MaxLeagueTeamCount = 20;

    public const int LeagueMatchesPerTeam = 34;

    public const int SingleLegRoundCount = LeagueTeamCount - 1;

    public const int MaxLeagueFixtureRound = (MaxLeagueTeamCount - 1) * 2;

    public const int MaxLeaguePosition = MaxLeagueTeamCount;

    public const int LeagueFixturesPerRound = LeagueTeamCount / 2;

    public const int TotalLeagueFixtures =
        LeagueTeamCount * (LeagueTeamCount - 1);

    public const int DefaultDaysBetweenRounds = 7;

    public static bool IsSupportedLeagueSize(int teamCount) =>
        teamCount is LeagueTeamCount or MaxLeagueTeamCount;

    public static int MatchesPerTeamFor(int teamCount)
    {
        EnsureSupportedLeagueSize(teamCount);
        return (teamCount - 1) * 2;
    }

    public static int FixturesPerRoundFor(int teamCount)
    {
        EnsureSupportedLeagueSize(teamCount);
        return teamCount / 2;
    }

    public static int TotalFixturesFor(int teamCount)
    {
        EnsureSupportedLeagueSize(teamCount);
        return teamCount * (teamCount - 1);
    }

    public static void EnsureSupportedLeagueSize(int teamCount)
    {
        if (!IsSupportedLeagueSize(teamCount))
        {
            throw new CompetitionInvariantViolationException(
                $"League size must be {LeagueTeamCount} or {MaxLeagueTeamCount}; received {teamCount}.");
        }
    }
}
