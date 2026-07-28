namespace FootballCareerSimulator.Application.Competition.Queries;

/// <summary>
/// Dünya sayfası lig brifingi — "ligde neredeyim, ne hissediyorum?"
/// </summary>
public sealed record LeagueWorldBriefing(
    bool HasSeason,
    string BrandTitle,
    string Headline,
    string AdviceLine,
    IReadOnlyList<string> BeatLines)
{
    public const string Brand = "Lig Masası";

    public static LeagueWorldBriefing NoSeason() =>
        new(
            HasSeason: false,
            Brand,
            "Lig henüz kurulmadı.",
            "Gelişmiş araçlardan ligi kur; sonra puan durumu burada canlanır.",
            Array.Empty<string>());

    public static LeagueWorldBriefing Compose(
        string seasonStatus,
        int acceptedFixtureCount,
        int totalFixtureCount,
        int clubCount,
        int? managedRank,
        int? managedPoints,
        int? managedPlayed,
        int? managedGoalDifference,
        string? managedClubName,
        string? leaderClubName,
        int? leaderPoints,
        string? nextMatchLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seasonStatus);

        if (totalFixtureCount <= 0 && clubCount <= 0)
        {
            return NoSeason();
        }

        var beats = new List<string>();
        beats.Add($"Sezon: {TranslateStatus(seasonStatus)} · maç {acceptedFixtureCount}/{totalFixtureCount}");

        if (managedRank is int rank
            && managedPoints is int points
            && !string.IsNullOrWhiteSpace(managedClubName))
        {
            var gd = managedGoalDifference is int diff
                ? $" · Averaj {FormatSigned(diff)}"
                : string.Empty;
            var played = managedPlayed is int p ? $" · {p}M" : string.Empty;
            beats.Add($"Sen: {rank}. {managedClubName} — {points}p{played}{gd}");
        }
        else if (!string.IsNullOrWhiteSpace(managedClubName))
        {
            beats.Add($"Kulübün: {managedClubName} — henüz puan yok.");
        }
        else
        {
            beats.Add("İşsizsin — ligi izliyorsun, koltuk yok.");
        }

        if (!string.IsNullOrWhiteSpace(leaderClubName) && leaderPoints is int leadPts)
        {
            beats.Add($"Lider: {leaderClubName} ({leadPts}p)");
        }

        if (!string.IsNullOrWhiteSpace(nextMatchLine))
        {
            beats.Add($"Sıradaki senin maçın: {nextMatchLine}");
        }

        var headline = ResolveHeadline(
            seasonStatus,
            acceptedFixtureCount,
            totalFixtureCount,
            clubCount,
            managedRank);
        var advice = ResolveAdvice(
            seasonStatus,
            acceptedFixtureCount,
            totalFixtureCount,
            clubCount,
            managedRank,
            managedPoints,
            leaderPoints);

        return new LeagueWorldBriefing(true, Brand, headline, advice, beats);
    }

    public string ToDisplayText()
    {
        var beats = BeatLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", BeatLines.Select(b => "· " + b));
        var advice = string.IsNullOrWhiteSpace(AdviceLine)
            ? string.Empty
            : $"\nÖneri: {AdviceLine}";
        return $"{BrandTitle}\n{Headline}{beats}{advice}";
    }

    private static string ResolveHeadline(
        string status,
        int accepted,
        int total,
        int clubCount,
        int? managedRank)
    {
        if (string.Equals(status, "Completed", StringComparison.Ordinal)
            || string.Equals(status, "Archived", StringComparison.Ordinal))
        {
            return managedRank is 1
                ? "Sezon bitti — şampiyon sensin."
                : "Sezon bitti — lig masası arşivleniyor.";
        }

        if (accepted == 0)
        {
            return "Lig kuruldu — ilk düdükleri bekliyor.";
        }

        if (total > 0 && accepted == total)
        {
            return "Fikstür tükendi — sezon kapanışına bak.";
        }

        if (managedRank is int rank && clubCount > 0)
        {
            if (rank == 1)
            {
                return "Zirvedesin — hedefi koru.";
            }

            if (rank <= Math.Max(2, clubCount / 4))
            {
                return "Üst sıralarda mücadele ediyorsun.";
            }

            if (rank > clubCount - Math.Max(2, clubCount / 5))
            {
                return "Alt sıralar ısıyor — puan lazım.";
            }
        }

        if (managedRank is null)
        {
            return "Lig dönüyor — sen kenardan izliyorsun.";
        }

        return "Lig ortasında yol alıyorsun.";
    }

    private static string ResolveAdvice(
        string status,
        int accepted,
        int total,
        int clubCount,
        int? managedRank,
        int? managedPoints,
        int? leaderPoints)
    {
        if (string.Equals(status, "Completed", StringComparison.Ordinal)
            || string.Equals(status, "Archived", StringComparison.Ordinal))
        {
            return "Yeni sezon için arşivle / Yeni Sezon ile devam et.";
        }

        if (accepted == 0)
        {
            return "Bugün'den günü ilerlet; fikstür ve Sıradaki Maç dolacak.";
        }

        if (total > 0 && accepted == total)
        {
            return "Sezonu Bitir → Yeni Sezon ile kariyeri çevir.";
        }

        if (managedRank is 1 && managedPoints is int pts && leaderPoints is int lead && pts == lead)
        {
            return "Liderliği koru — Hazırlık Masası'nda yorgunluğa dikkat.";
        }

        if (managedRank is int rank
            && clubCount > 0
            && rank > clubCount - Math.Max(2, clubCount / 5))
        {
            return "Küme hattı yakın — kadro ve söz riskini Bugün'de sıkı tut.";
        }

        if (managedRank is int r
            && r > 1
            && managedPoints is int mp
            && leaderPoints is int lp
            && lp - mp <= 3)
        {
            return "Lidere yakınsın — sıradaki maç üç puanlık fırsat.";
        }

        if (managedRank is null)
        {
            return "İş teklifi ara; ligi kulüpten yönetmek daha anlamlı.";
        }

        return "Puan durumunu takip et; detay için hafta fikstürüne bak.";
    }

    private static string TranslateStatus(string status) => status switch
    {
        "Preseason" => "Hazırlık",
        "Active" => "Aktif",
        "Completed" => "Tamamlandı",
        "Archived" => "Arşiv",
        _ => status,
    };

    private static string FormatSigned(int value) =>
        value > 0 ? $"+{value}" : value.ToString();
}
