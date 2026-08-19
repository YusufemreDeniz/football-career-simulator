namespace FootballCareerSimulator.Application.Competition.Queries;

using FootballCareerSimulator.Domain.Competition;

/// <summary>
/// Maç öncesi rakip okuması: lig konumu, yakın form, göreli kadro gücü ve
/// mevcut veriden çıkarılabilen tek belirgin tehdit.
/// </summary>
public sealed record OpponentDossierDigest(
    string BrandTitle,
    string Headline,
    string StandingLine,
    string FormLine,
    string StrengthLine,
    string ThreatLine,
    OpponentThreatKind ThreatKind,
    bool ManagedIsHome,
    int StrengthDifference,
    int WinningStreakLength = 0,
    int LosingStreakLength = 0)
{
    public const string Brand = "Rakip Dosyası";

    public IReadOnlyList<string> DetailLines =>
        [StandingLine, FormLine, StrengthLine, ThreatLine];

    public static OpponentDossierDigest Compose(
        long opponentClubId,
        string opponentName,
        bool managedIsHome,
        int managedStrength,
        int opponentStrength,
        IReadOnlyList<StandingEntryReadModel> standings,
        IReadOnlyList<FixtureReadModel> fixtures)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(opponentName);
        ArgumentNullException.ThrowIfNull(standings);
        ArgumentNullException.ThrowIfNull(fixtures);

        var standingIndex = standings
            .Select((entry, index) => (Entry: entry, Rank: index + 1))
            .FirstOrDefault(item => item.Entry.ClubId == opponentClubId);
        var standing = standingIndex.Entry;
        var hasLeagueData = standing is { Played: > 0 };
        var standingLine = hasLeagueData
            ? $"Lig: {standingIndex.Rank}/{standings.Count} · {standing!.Points} puan"
                + $" · averaj {FormatSigned(standing.GoalDifference)}"
            : "Lig: henüz sonuç verisi oluşmadı.";

        var latestFirstResults = fixtures
            .Where(fixture =>
                string.Equals(
                    fixture.Status,
                    nameof(FixtureStatus.ResultAccepted),
                    StringComparison.Ordinal)
                && fixture.HomeGoals is not null
                && fixture.AwayGoals is not null
                && (fixture.HomeClubId == opponentClubId
                    || fixture.AwayClubId == opponentClubId))
            .OrderByDescending(fixture => fixture.ScheduledDayNumber)
            .ThenByDescending(fixture => fixture.FixtureId)
            .Select(fixture => ToFormResult(fixture, opponentClubId))
            .ToArray();
        var recent = latestFirstResults.Take(5).Reverse().ToArray();
        var currentWinningStreakLength = latestFirstResults
            .TakeWhile(result => result > 0)
            .Count();
        var winningStreakLength = currentWinningStreakLength >= 3
            ? currentWinningStreakLength
            : 0;
        var currentLosingStreakLength = latestFirstResults
            .TakeWhile(result => result < 0)
            .Count();
        var losingStreakLength = currentLosingStreakLength >= 3
            ? currentLosingStreakLength
            : 0;
        var formPoints = recent.Sum(result => result switch
        {
            > 0 => 3,
            0 => 1,
            _ => 0,
        });
        var formLine = recent.Length == 0
            ? "Form: henüz tamamlanmış maç yok."
            : $"Form (eski→yeni): {string.Join('-', recent.Select(FormCode))}"
                + $" · {formPoints}/{recent.Length * 3} puan";

        var strengthDifference = opponentStrength - managedStrength;
        var strengthLine = strengthDifference switch
        {
            >= 7 => $"Güç: rakip belirgin üstün ({opponentStrength} vs {managedStrength}).",
            >= 3 => $"Güç: rakip az farkla güçlü ({opponentStrength} vs {managedStrength}).",
            <= -7 => $"Güç: kağıt üzerinde belirgin üstünlüğün var ({managedStrength} vs {opponentStrength}).",
            <= -3 => $"Güç: kağıt üzerinde az farkla üstünsün ({managedStrength} vs {opponentStrength}).",
            _ => $"Güç: dengeli eşleşme ({managedStrength} vs {opponentStrength}).",
        };

        var threat = ResolveThreat(
            managedIsHome,
            strengthDifference,
            standingIndex.Rank,
            standings.Count,
            standing,
            recent,
            winningStreakLength,
            losingStreakLength);

        return new OpponentDossierDigest(
            Brand,
            managedIsHome
                ? $"{opponentName} evine geliyor."
                : $"{opponentName} deplasmanındasın.",
            standingLine,
            formLine,
            strengthLine,
            threat.Line,
            threat.Kind,
            managedIsHome,
            strengthDifference,
            winningStreakLength,
            losingStreakLength);
    }

    private static ThreatAssessment ResolveThreat(
        bool managedIsHome,
        int strengthDifference,
        int rank,
        int clubCount,
        StandingEntryReadModel? standing,
        IReadOnlyList<int> recent,
        int winningStreakLength,
        int losingStreakLength)
    {
        if (winningStreakLength >= 3)
        {
            return new ThreatAssessment(
                OpponentThreatKind.WinningStreak,
                managedIsHome
                    ? $"Tehdit: {winningStreakLength} maçlık galibiyet serisi — evde ilk bölümde sabırlı kal."
                    : $"Tehdit: {winningStreakLength} maçlık galibiyet serisi — deplasmanda erken baskıya hazırlan.");
        }

        if (losingStreakLength >= 3)
        {
            return new ThreatAssessment(
                OpponentThreatKind.LosingStreak,
                managedIsHome
                    ? $"Fırsat: rakip {losingStreakLength} maçtır kaybediyor — evde ilk golle baskıyı büyüt."
                    : $"Fırsat: rakip {losingStreakLength} maçtır kaybediyor — özgüvenini erken baskıyla sına.");
        }

        if (standing is { Played: >= 3 }
            && standing.GoalsFor * 2 >= standing.Played * 3)
        {
            return new ThreatAssessment(
                OpponentThreatKind.ProductiveAttack,
                managedIsHome
                    ? "Tehdit: üretken hücum — top kaybı sonrası merkezi kapat."
                    : "Tehdit: üretken hücum — deplasmanda geçiş savunmasını koru.");
        }

        if (strengthDifference >= 7)
        {
            return new ThreatAssessment(
                OpponentThreatKind.SquadQuality,
                managedIsHome
                    ? "Tehdit: kadro kalitesi — ev avantajını tempoyla kullan."
                    : "Tehdit: kadro kalitesi — deplasmanda alanı daralt.");
        }

        var topZone = standing is { Played: > 0 }
            && clubCount > 0
            && rank > 0
            && rank <= Math.Max(3, clubCount / 4);
        if (topZone)
        {
            return new ThreatAssessment(
                OpponentThreatKind.TopZoneTempo,
                "Tehdit: zirve temposu — ilk 20 dakikadaki baskıya hazır ol.");
        }

        if (standing is { Played: >= 3 }
            && standing.GoalsAgainst * 5 <= standing.Played * 4)
        {
            return new ThreatAssessment(
                OpponentThreatKind.DefensiveResistance,
                "Tehdit: savunma direnci — ilk golü ararken acele etme.");
        }

        return new ThreatAssessment(
            OpponentThreatKind.Neutral,
            "Tehdit: belirgin bir uç yok — dengeyi sen bozabilirsin.");
    }

    private static int ToFormResult(FixtureReadModel fixture, long opponentClubId)
    {
        var opponentGoals = fixture.HomeClubId == opponentClubId
            ? fixture.HomeGoals!.Value
            : fixture.AwayGoals!.Value;
        var otherGoals = fixture.HomeClubId == opponentClubId
            ? fixture.AwayGoals!.Value
            : fixture.HomeGoals!.Value;
        return Math.Sign(opponentGoals - otherGoals);
    }

    private static string FormCode(int result) => result switch
    {
        > 0 => "G",
        0 => "B",
        _ => "M",
    };

    private static string FormatSigned(int value) => value > 0 ? $"+{value}" : value.ToString();

    private readonly record struct ThreatAssessment(OpponentThreatKind Kind, string Line);
}

public enum OpponentThreatKind
{
    Neutral = 0,
    WinningStreak = 1,
    ProductiveAttack = 2,
    SquadQuality = 3,
    TopZoneTempo = 4,
    DefensiveResistance = 5,
    LosingStreak = 6,
}
