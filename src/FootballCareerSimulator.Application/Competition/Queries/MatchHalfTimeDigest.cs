namespace FootballCareerSimulator.Application.Competition.Queries;

/// <summary>
/// Devre arası kontrol noktası — skor + sınırlı ikinci yarı kararı.
/// </summary>
public sealed record MatchHalfTimeDigest(
    bool HasManagedMatch,
    string BrandTitle,
    string FixtureLine,
    string Scoreline,
    int HomeGoals,
    int AwayGoals,
    bool ManagedIsHome,
    string Headline,
    string AdviceLine,
    IReadOnlyList<string> BeatLines)
{
    public const string Brand = "Devre Arası";

    public const int DecisionContinue = 0;
    public const int DecisionAttack = 2;
    public const int DecisionDefend = 1;

    public static MatchHalfTimeDigest None() =>
        new(
            HasManagedMatch: false,
            Brand,
            "—",
            "—",
            0,
            0,
            ManagedIsHome: true,
            "Devre arası yok.",
            "Önce kendi maçına çık.",
            Array.Empty<string>());

    public static MatchHalfTimeDigest Compose(
        string homeClubName,
        string awayClubName,
        int homeGoals,
        int awayGoals,
        bool managedIsHome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(homeClubName);
        ArgumentException.ThrowIfNullOrWhiteSpace(awayClubName);

        var scoreline = $"{homeClubName} {homeGoals}-{awayGoals} {awayClubName}";
        var managedGoals = managedIsHome ? homeGoals : awayGoals;
        var opponentGoals = managedIsHome ? awayGoals : homeGoals;
        var (headline, advice) = ResolveAdvice(managedGoals, opponentGoals);
        var beats = new List<string>
        {
            $"İlk yarı: {scoreline}",
            "İkinci yarı için yaklaşım seç — veya bir değişiklik yap.",
        };

        return new MatchHalfTimeDigest(
            HasManagedMatch: true,
            Brand,
            $"{homeClubName} vs {awayClubName}",
            scoreline,
            homeGoals,
            awayGoals,
            managedIsHome,
            headline,
            advice,
            beats);
    }

    public string ToDisplayText()
    {
        var beats = BeatLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", BeatLines.Select(b => "· " + b));
        return $"{BrandTitle}\n{Headline}\n{Scoreline}{beats}\nÖneri: {AdviceLine}";
    }

    /// <summary>
    /// Maç gecesi Anlar — HT yaklaşım kararı.
    /// </summary>
    public static string? FormatDecisionKeyMoment(string? decisionLabel)
    {
        if (string.IsNullOrWhiteSpace(decisionLabel))
        {
            return null;
        }

        if (decisionLabel.Contains("hücuma", StringComparison.OrdinalIgnoreCase))
        {
            return "46' Karar · Hücuma geçtin";
        }

        if (decisionLabel.Contains("savunmaya", StringComparison.OrdinalIgnoreCase))
        {
            return "46' Karar · Savunmaya çektin";
        }

        if (decisionLabel.Contains("aynı plan", StringComparison.OrdinalIgnoreCase))
        {
            return "46' Karar · Aynı plan";
        }

        return null;
    }

    private static (string Headline, string Advice) ResolveAdvice(int managedGoals, int opponentGoals)
    {
        if (managedGoals > opponentGoals)
        {
            return (
                "Öndesin — ikinci yarıyı yönet.",
                "Skoru koru: Savunmaya çek veya aynı planla devam.");
        }

        if (managedGoals < opponentGoals)
        {
            return (
                "Geridesin — risk almanın zamanı.",
                "Hücuma geç; bir değişiklik de düşünebilirsin.");
        }

        return (
            "Berabere — ikinci yarıyı sen yaz.",
            "Hücumla kır veya dengede kal.");
    }
}
