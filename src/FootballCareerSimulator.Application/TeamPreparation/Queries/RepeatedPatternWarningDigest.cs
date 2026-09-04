namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

/// <summary>
/// Mevcut eşleşme planını son üç maçtaki aynı tehdit/taktik desenleriyle karşılaştırır.
/// Yalnızca geçmişte uyarıyla sonuçlanmış tam eşleşmeler için koç mesajı üretir.
/// </summary>
public sealed record RepeatedPatternWarningDigest(
    bool HasWarning,
    string BrandTitle,
    string Headline,
    string WarningLine,
    int MatchingWarningCount)
{
    public const string Brand = "Koç Uyarısı";

    public static RepeatedPatternWarningDigest Clear() =>
        new(false, Brand, "Tekrarlanan olumsuz desen yok.", string.Empty, 0);

    public static RepeatedPatternWarningDigest Compose(
        MatchupPlanDigest? currentPlan,
        IReadOnlyList<MatchupPlanNotebookEntry>? history)
    {
        if (currentPlan is null || history is null || history.Count == 0)
        {
            return Clear();
        }

        var matchingWarnings = history
            .Where(entry =>
                entry.ThreatKind == currentPlan.ThreatKind
                && entry.PlanSignal == currentPlan.Signal
                && string.Equals(
                    entry.SelectionLine,
                    currentPlan.SelectionLine,
                    StringComparison.Ordinal)
                && entry.OutcomeSignal == Competition.Queries.MatchupPlanOutcomeSignal.Warning)
            .OrderByDescending(entry => entry.DayNumber)
            .Take(MatchupPlanNotebookEntry.HistoryLimit)
            .ToArray();
        if (matchingWarnings.Length == 0)
        {
            return Clear();
        }

        var selection = currentPlan.SelectionLine.StartsWith("Seçim: ", StringComparison.Ordinal)
            ? currentPlan.SelectionLine[7..]
            : currentPlan.SelectionLine;
        var threat = TechnicalDirectorNotebookDigest.FormatThreatLabel(currentPlan.ThreatKind);
        var countLabel = matchingWarnings.Length == 1
            ? "bir kez"
            : $"{matchingWarnings.Length} kez";
        return new RepeatedPatternWarningDigest(
            true,
            Brand,
            matchingWarnings.Length == 1
                ? "Bu desen daha önce uyarı verdi."
                : "Aynı olumsuz desen tekrarlanıyor.",
            $"Koç: {threat} karşısında {selection} son üç maçta {countLabel} uyarıyla kapandı; "
                + "aynı planı otomatik tekrarlama.",
            matchingWarnings.Length);
    }
}
