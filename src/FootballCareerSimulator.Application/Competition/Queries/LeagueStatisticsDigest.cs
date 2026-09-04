namespace FootballCareerSimulator.Application.Competition.Queries;

public sealed record LeagueTeamStatisticsLine(
    long ClubId,
    string ClubName,
    string LastFiveForm,
    int LastFivePoints,
    int HomePlayed,
    int HomePoints,
    int AwayPlayed,
    int AwayPoints);

public sealed record LeagueStatisticsDigest(
    bool HasData,
    string Headline,
    string LeadersLine,
    string ManagedClubLine,
    IReadOnlyList<LeagueTeamStatisticsLine> Teams)
{
    public static LeagueStatisticsDigest Empty() =>
        new(false, "Lig istatistik merkezi · sonuç bekleniyor", "Liderler henüz oluşmadı.", "Kulüp performansı henüz oluşmadı.", Array.Empty<LeagueTeamStatisticsLine>());

    public string GetForm(long clubId) =>
        Teams.FirstOrDefault(team => team.ClubId == clubId)?.LastFiveForm ?? "—";

    public static LeagueStatisticsDigest Compose(
        IReadOnlyList<StandingEntryReadModel> standings,
        IReadOnlyList<FixtureReadModel> fixtures,
        IReadOnlyDictionary<long, string> clubNames,
        long? managedClubId)
    {
        ArgumentNullException.ThrowIfNull(standings);
        ArgumentNullException.ThrowIfNull(fixtures);
        ArgumentNullException.ThrowIfNull(clubNames);

        var played = fixtures
            .Where(fixture => fixture.HomeGoals is not null && fixture.AwayGoals is not null)
            .OrderBy(fixture => fixture.ScheduledDayNumber)
            .ThenBy(fixture => fixture.FixtureId)
            .ToArray();
        if (played.Length == 0 || standings.Count == 0)
        {
            return Empty();
        }

        var teams = standings.Select(standing =>
        {
            var matches = played
                .Where(fixture => fixture.HomeClubId == standing.ClubId || fixture.AwayClubId == standing.ClubId)
                .ToArray();
            var lastFive = matches.TakeLast(5).Select(fixture => ResultCode(fixture, standing.ClubId)).ToArray();
            var home = matches.Where(fixture => fixture.HomeClubId == standing.ClubId).ToArray();
            var away = matches.Where(fixture => fixture.AwayClubId == standing.ClubId).ToArray();
            return new LeagueTeamStatisticsLine(
                standing.ClubId,
                clubNames.GetValueOrDefault(standing.ClubId, $"Kulüp {standing.ClubId}"),
                lastFive.Length == 0 ? "—" : string.Join(" ", lastFive),
                lastFive.Sum(ResultPoints),
                home.Length,
                home.Sum(fixture => ResultPoints(ResultCode(fixture, standing.ClubId))),
                away.Length,
                away.Sum(fixture => ResultPoints(ResultCode(fixture, standing.ClubId))));
        }).ToArray();

        var attack = standings.Where(entry => entry.Played > 0)
            .OrderByDescending(entry => entry.GoalsFor)
            .ThenByDescending(entry => entry.Points)
            .First();
        var defense = standings.Where(entry => entry.Played > 0)
            .OrderBy(entry => entry.GoalsAgainst)
            .ThenByDescending(entry => entry.Points)
            .First();
        var form = teams.OrderByDescending(team => team.LastFivePoints)
            .ThenByDescending(team => standings.First(entry => entry.ClubId == team.ClubId).GoalDifference)
            .First();
        var totalGoals = played.Sum(fixture => fixture.HomeGoals!.Value + fixture.AwayGoals!.Value);
        var managed = managedClubId is long clubId
            ? teams.FirstOrDefault(team => team.ClubId == clubId)
            : null;
        var managedStanding = managed is null
            ? null
            : standings.FirstOrDefault(entry => entry.ClubId == managed.ClubId);

        return new LeagueStatisticsDigest(
            true,
            $"Lig istatistik merkezi · {played.Length} maç · maç başı {(double)totalGoals / played.Length:0.00} gol",
            $"En iyi hücum: {clubNames.GetValueOrDefault(attack.ClubId, attack.ClubId.ToString())} ({attack.GoalsFor})"
            + $" · En sıkı savunma: {clubNames.GetValueOrDefault(defense.ClubId, defense.ClubId.ToString())} ({defense.GoalsAgainst})"
            + $" · Form lideri: {form.ClubName} ({form.LastFivePoints}/15)",
            managed is null || managedStanding is null
                ? "Yönetilen kulüp verisi yok."
                : $"{managed.ClubName}: {managedStanding.Points} puan · form {managed.LastFiveForm}"
                  + $" · iç saha {managed.HomePoints}/{managed.HomePlayed * 3}"
                  + $" · deplasman {managed.AwayPoints}/{managed.AwayPlayed * 3}",
            teams);
    }

    private static string ResultCode(FixtureReadModel fixture, long clubId)
    {
        var goalsFor = fixture.HomeClubId == clubId ? fixture.HomeGoals!.Value : fixture.AwayGoals!.Value;
        var goalsAgainst = fixture.HomeClubId == clubId ? fixture.AwayGoals!.Value : fixture.HomeGoals!.Value;
        return goalsFor > goalsAgainst ? "G" : goalsFor == goalsAgainst ? "B" : "M";
    }

    private static int ResultPoints(string result) => result == "G" ? 3 : result == "B" ? 1 : 0;
}
