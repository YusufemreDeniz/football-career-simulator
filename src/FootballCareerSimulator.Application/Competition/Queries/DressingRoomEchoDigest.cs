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

        var latest = fixtures
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
            .FirstOrDefault();
        if (latest is null)
        {
            return null;
        }

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

        return new DressingRoomEchoDigest(
            Brand,
            $"Son maç: {opponentName} · {goalsFor}-{goalsAgainst} {result}",
            captain.VoiceLine,
            latest.FixtureId);
    }

    public string ToDisplayText() => $"{Headline}\n{VoiceLine}";
}
