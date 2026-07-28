using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;

namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Bugün nabzı — ofis, maç, hazırlık ve lig masalarını tek bakışta bağlar.
/// </summary>
public sealed record TodayPulseDigest(
    string BrandTitle,
    string Headline,
    string PrimaryFocusCode,
    IReadOnlyList<string> PulseLines)
{
    public const string Brand = "Günün Nabzı";

    public const string FocusDesk = "Desk";
    public const string FocusMatch = "Match";
    public const string FocusPrep = "Prep";
    public const string FocusLeague = "League";
    public const string FocusCalm = "Calm";

    public static TodayPulseDigest Compose(
        DecisionDeskDigest desk,
        PreMatchBriefing match,
        PreparationBriefing prep,
        LeagueWorldBriefing league)
    {
        ArgumentNullException.ThrowIfNull(desk);
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(prep);
        ArgumentNullException.ThrowIfNull(league);

        var lines = new List<string>();
        if (desk.HasOpenDecision)
        {
            lines.Add($"Masada: {desk.Headline}");
        }

        if (match.HasMatch)
        {
            lines.Add($"Maç: {match.Headline}");
        }

        if (prep.IsEmployed)
        {
            lines.Add($"Hazırlık: {prep.Headline}");
        }

        if (league.HasSeason)
        {
            lines.Add($"Lig: {league.Headline}");
        }

        var (focus, headline) = ResolveFocus(desk, match, prep, league);
        return new TodayPulseDigest(Brand, headline, focus, lines.Take(4).ToArray());
    }

    public string ToDisplayText()
    {
        var pulses = PulseLines.Count == 0
            ? string.Empty
            : "\n" + string.Join("\n", PulseLines.Select(l => "· " + l));
        return $"{BrandTitle}\n{Headline}{pulses}";
    }

    private static (string Focus, string Headline) ResolveFocus(
        DecisionDeskDigest desk,
        PreMatchBriefing match,
        PreparationBriefing prep,
        LeagueWorldBriefing league)
    {
        if (desk.IsHardBlocker)
        {
            return (FocusDesk, "Önce Masada — zaman burada kilitli.");
        }

        if (desk.HasOpenDecision)
        {
            return (FocusDesk, "Masada iş var — ofisi temizle.");
        }

        if (match.HasMatch && !match.IsReadyToKickOff)
        {
            return (FocusMatch, "Maç kapıda — kadroyu kilitle.");
        }

        if (match is { HasMatch: true, HasPromiseRisk: true })
        {
            return (FocusMatch, "Söz riski var — XI↔Yedek düşün.");
        }

        if (prep.IsEmployed
            && (prep.Headline.Contains("yorgun", StringComparison.OrdinalIgnoreCase)
                || prep.Headline.Contains("sakat", StringComparison.OrdinalIgnoreCase)
                || prep.Headline.Contains("boş", StringComparison.OrdinalIgnoreCase)))
        {
            return (FocusPrep, "Hazırlık Masası çağırıyor.");
        }

        if (league.HasSeason
            && (league.Headline.Contains("Alt sıralar", StringComparison.Ordinal)
                || league.Headline.Contains("Zirvedesin", StringComparison.Ordinal)))
        {
            return (FocusLeague, "Lig Masası'na bir bak — sıralama konuşuyor.");
        }

        if (match.HasMatch && match.IsReadyToKickOff)
        {
            return (FocusMatch, "Hazırsın — düdük için Bugün'de kal.");
        }

        return (FocusCalm, "Sakin bir gün — nabız dengede.");
    }
}
