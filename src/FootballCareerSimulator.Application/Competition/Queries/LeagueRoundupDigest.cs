namespace FootballCareerSimulator.Application.Competition.Queries;

/// <summary>
/// Lig Akşamı — hafta bitince tüm sonuçların kısa tablo hükmü:
/// lider (değişim), yönetilenin sıra hareketi, küme hattı, sıradaki rakibin sonucu.
/// </summary>
public sealed record LeagueRoundupDigest(
    string BrandTitle,
    string Headline,
    IReadOnlyList<string> BeatLines)
{
    public const string Brand = "Lig Akşamı";

    public static LeagueRoundupDigest Compose(
        string? beforeLeaderName,
        string? afterLeaderName,
        int? afterLeaderPoints,
        int? beforeManagedRank,
        int? afterManagedRank,
        IReadOnlyList<string> relegationZone,
        string? managedClubName,
        IReadOnlyList<string> otherScorelines,
        string? nextOpponentName)
    {
        var beats = new List<string>();

        if (!string.IsNullOrWhiteSpace(afterLeaderName) && afterLeaderPoints is int leadPts)
        {
            var leaderChanged = !string.IsNullOrWhiteSpace(beforeLeaderName)
                && !string.Equals(beforeLeaderName, afterLeaderName, StringComparison.Ordinal);
            beats.Add(leaderChanged
                ? $"Lider değişti: {afterLeaderName} ({leadPts}p)"
                : $"Lider: {afterLeaderName} ({leadPts}p)");
        }

        if (afterManagedRank is int rank)
        {
            if (rank == 1)
            {
                beats.Add("Zirvedesin.");
            }
            else if (beforeManagedRank is int beforeRank && beforeRank != rank)
            {
                var direction = rank < beforeRank ? "yükseldi" : "geriledi";
                beats.Add($"Sıran {beforeRank}. → {rank}. ({direction})");
            }
            else
            {
                beats.Add($"Sıran: {rank}.");
            }
        }

        if (relegationZone.Count > 0)
        {
            var zoneText = string.Join(", ", relegationZone);
            if (!string.IsNullOrWhiteSpace(managedClubName)
                && relegationZone.Contains(managedClubName, StringComparer.Ordinal))
            {
                beats.Add($"Küme hattındasın — {zoneText} arasında.");
            }
            else
            {
                beats.Add($"Küme hattı: {zoneText}");
            }
        }

        if (!string.IsNullOrWhiteSpace(nextOpponentName)
            && TryFindOpponentResult(nextOpponentName, otherScorelines, out var opponentResult))
        {
            beats.Add($"Sıradaki rakip {nextOpponentName}: {opponentResult}");
        }

        return new LeagueRoundupDigest(Brand, ResolveHeadline(
            beforeLeaderName,
            afterLeaderName,
            beforeManagedRank,
            afterManagedRank), beats);
    }

    private static string ResolveHeadline(
        string? beforeLeaderName,
        string? afterLeaderName,
        int? beforeManagedRank,
        int? afterManagedRank)
    {
        var leaderChanged = !string.IsNullOrWhiteSpace(beforeLeaderName)
            && !string.IsNullOrWhiteSpace(afterLeaderName)
            && !string.Equals(beforeLeaderName, afterLeaderName, StringComparison.Ordinal);

        if (leaderChanged)
        {
            return $"Zirve el değiştirdi — {afterLeaderName}.";
        }

        if (afterManagedRank == 1)
        {
            return "Tablo senin — zirvedesin.";
        }

        if (beforeManagedRank is int beforeRank
            && afterManagedRank is int afterRank
            && beforeRank != afterRank)
        {
            return afterRank < beforeRank
                ? "Hafta lehine bitti — sıra yükseldi."
                : "Hafta aleyhe bitti — sıra geriledi.";
        }

        return "Lig akşamı — tablo güncellendi.";
    }

    private static bool TryFindOpponentResult(
        string opponentName,
        IReadOnlyList<string> scorelines,
        out string result)
    {
        var match = scorelines.FirstOrDefault(line =>
            line.Contains(opponentName, StringComparison.Ordinal));
        result = match ?? string.Empty;
        return match is not null;
    }
}
