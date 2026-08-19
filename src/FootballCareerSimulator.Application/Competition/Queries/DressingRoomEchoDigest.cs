using FootballCareerSimulator.Domain.Competition;

namespace FootballCareerSimulator.Application.Competition.Queries;

/// <summary>
/// Son yönetilen maçın soyunma odasında bıraktığı yankı. Kalıcı ek state tutmaz;
/// authoritative fikstür sonucundan her okumada deterministik olarak türetilir.
/// </summary>
public sealed record DressingRoomEchoDigest(
    string BrandTitle,
    string Headline,
    string VoiceLine,
    string MomentumLine,
    long FixtureId)
{
    public const string Brand = "SOYUNMA ODASI NABZI";

    public static DressingRoomEchoDigest? Compose(
        IReadOnlyList<FixtureReadModel> fixtures,
        long managedClubId,
        int managedSinceDayNumber,
        IReadOnlyDictionary<long, string> clubNames)
    {
        ArgumentNullException.ThrowIfNull(fixtures);
        ArgumentNullException.ThrowIfNull(clubNames);

        var recent = fixtures
            .Where(fixture =>
                string.Equals(
                    fixture.Status,
                    nameof(FixtureStatus.ResultAccepted),
                    StringComparison.Ordinal)
                && fixture.HomeGoals is not null
                && fixture.AwayGoals is not null
                && fixture.ScheduledDayNumber >= managedSinceDayNumber
                && (fixture.HomeClubId == managedClubId || fixture.AwayClubId == managedClubId))
            .OrderByDescending(fixture => fixture.ScheduledDayNumber)
            .ThenByDescending(fixture => fixture.Round)
            .ThenByDescending(fixture => fixture.FixtureId)
            .Take(5)
            .ToArray();
        if (recent.Length == 0)
        {
            return null;
        }

        var latest = recent[0];

        var managedIsHome = latest.HomeClubId == managedClubId;
        var goalsFor = managedIsHome ? latest.HomeGoals!.Value : latest.AwayGoals!.Value;
        var goalsAgainst = managedIsHome ? latest.AwayGoals!.Value : latest.HomeGoals!.Value;
        var opponentId = managedIsHome ? latest.AwayClubId : latest.HomeClubId;
        var opponentName = clubNames.TryGetValue(opponentId, out var knownName)
            ? knownName
            : "Rakip";
        var result = goalsFor.CompareTo(goalsAgainst) switch
        {
            > 0 => "galibiyet",
            0 => "beraberlik",
            _ => "mağlubiyet",
        };
        var captain = CaptainReactionDigest.Compose(goalsFor - goalsAgainst, dismissed: false)!;
        var latestFirstForm = recent
            .Select(fixture => ResultCode(fixture, managedClubId))
            .ToArray();
        var displayForm = latestFirstForm.Reverse().ToArray();
        var momentumLine = latestFirstForm.Length >= 3
            && latestFirstForm.Take(3).All(code => code == "G")
                ? $"Form: {string.Join('-', displayForm)} · 3 maçlık galibiyet serisi"
                : latestFirstForm.Length >= 3
                  && latestFirstForm.Take(3).All(code => code == "M")
                    ? $"Form: {string.Join('-', displayForm)} · 3 maçlık mağlubiyet serisi"
                    : $"Form (eski→yeni): {string.Join('-', displayForm)}"
                      + $" · {latestFirstForm.Sum(ResultPoints)}/{latestFirstForm.Length * 3} puan";

        return new DressingRoomEchoDigest(
            Brand,
            $"Son maç: {opponentName} · {goalsFor}-{goalsAgainst} {result}",
            captain.VoiceLine,
            momentumLine,
            latest.FixtureId);
    }

    public string ToDisplayText() => $"{Headline}\n{VoiceLine}\n{MomentumLine}";

    private static string ResultCode(FixtureReadModel fixture, long clubId)
    {
        var goalsFor = fixture.HomeClubId == clubId
            ? fixture.HomeGoals!.Value
            : fixture.AwayGoals!.Value;
        var goalsAgainst = fixture.HomeClubId == clubId
            ? fixture.AwayGoals!.Value
            : fixture.HomeGoals!.Value;
        return goalsFor > goalsAgainst ? "G" : goalsFor == goalsAgainst ? "B" : "M";
    }

    private static int ResultPoints(string result) => result == "G" ? 3 : result == "B" ? 1 : 0;
}
