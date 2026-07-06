namespace FootballCareerSimulator.Domain.Competition;

/// <summary>
/// MVP lig yapısı sabitleri (docs/02_MVP_SCOPE.md Bölüm 17.2).
/// </summary>
public static class CompetitionMvpConstraints
{
    public const int LeagueTeamCount = 20;

    public const int LeagueMatchesPerTeam = 38;

    public const int SingleLegRoundCount = LeagueTeamCount - 1;

    public const int MaxLeagueFixtureRound = LeagueMatchesPerTeam;

    public const int MaxLeaguePosition = LeagueTeamCount;

    public const int LeagueFixturesPerRound = LeagueTeamCount / 2;

    public const int TotalLeagueFixtures =
        LeagueTeamCount * (LeagueTeamCount - 1);

    public const int DefaultDaysBetweenRounds = 7;
}
