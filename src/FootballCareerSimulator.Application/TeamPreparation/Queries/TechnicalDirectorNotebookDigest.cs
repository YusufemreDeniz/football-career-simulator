namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

using FootballCareerSimulator.Application.Competition.Queries;

/// <summary>
/// Son eşleşme planlarını kısa, karşılaştırılabilir derslere dönüştüren teknik direktör defteri.
/// </summary>
public sealed record TechnicalDirectorNotebookDigest(
    string BrandTitle,
    string Headline,
    IReadOnlyList<string> BeatLines)
{
    public const string Brand = "Teknik Direktör Defteri";

    public bool HasHistory => BeatLines.Count > 0;

    public static TechnicalDirectorNotebookDigest Compose(
        IReadOnlyList<MatchupPlanNotebookEntry>? history)
    {
        var recent = (history ?? Array.Empty<MatchupPlanNotebookEntry>())
            .OrderByDescending(entry => entry.DayNumber)
            .Take(MatchupPlanNotebookEntry.HistoryLimit)
            .ToArray();
        if (recent.Length == 0)
        {
            return new TechnicalDirectorNotebookDigest(
                Brand,
                "Henüz kayıtlı eşleşme dersi yok.",
                Array.Empty<string>());
        }

        return new TechnicalDirectorNotebookDigest(
            Brand,
            $"Son {recent.Length} maçtan dersler",
            recent.Select(FormatBeat).ToArray());
    }

    private static string FormatBeat(MatchupPlanNotebookEntry entry)
    {
        var selection = entry.SelectionLine.StartsWith("Seçim: ", StringComparison.Ordinal)
            ? entry.SelectionLine[7..]
            : entry.SelectionLine;
        return $"Defter · Gün {entry.DayNumber}, {entry.OpponentName}"
            + $" · {selection} · {FormatThreat(entry.ThreatKind)}"
            + $" · {FormatPlanSignal(entry.PlanSignal)}→{FormatOutcome(entry.OutcomeSignal)}";
    }

    private static string FormatThreat(OpponentThreatKind kind) => kind switch
    {
        OpponentThreatKind.WinningStreak => "galibiyet serisi",
        OpponentThreatKind.ProductiveAttack => "üretken hücum",
        OpponentThreatKind.SquadQuality => "kadro kalitesi",
        OpponentThreatKind.TopZoneTempo => "zirve temposu",
        OpponentThreatKind.DefensiveResistance => "savunma direnci",
        _ => "dengeli profil",
    };

    private static string FormatPlanSignal(MatchupPlanSignal signal) => signal switch
    {
        MatchupPlanSignal.Risk => "Risk",
        MatchupPlanSignal.Opportunity => "Fırsat",
        _ => "Denge",
    };

    private static string FormatOutcome(MatchupPlanOutcomeSignal signal) => signal switch
    {
        MatchupPlanOutcomeSignal.Positive => "Olumlu",
        MatchupPlanOutcomeSignal.Warning => "Uyarı",
        _ => "Nötr",
    };
}

public sealed record MatchupPlanNotebookEntry(
    int DayNumber,
    string OpponentName,
    string SelectionLine,
    OpponentThreatKind ThreatKind,
    MatchupPlanSignal PlanSignal,
    MatchupPlanOutcomeSignal OutcomeSignal,
    string VerdictLine)
{
    public const int HistoryLimit = 3;

    public static MatchupPlanNotebookEntry Compose(
        int dayNumber,
        string opponentName,
        string selectionLine,
        OpponentThreatKind threatKind,
        MatchupPlanSignal planSignal,
        MatchupPlanOutcomeSignal outcomeSignal,
        string verdictLine)
    {
        if (dayNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(dayNumber), dayNumber, null);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(opponentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectionLine);
        ArgumentException.ThrowIfNullOrWhiteSpace(verdictLine);
        if (!Enum.IsDefined(threatKind))
        {
            throw new ArgumentOutOfRangeException(nameof(threatKind), threatKind, null);
        }

        if (!Enum.IsDefined(planSignal))
        {
            throw new ArgumentOutOfRangeException(nameof(planSignal), planSignal, null);
        }

        if (!Enum.IsDefined(outcomeSignal))
        {
            throw new ArgumentOutOfRangeException(nameof(outcomeSignal), outcomeSignal, null);
        }

        return new MatchupPlanNotebookEntry(
            dayNumber,
            opponentName.Trim(),
            selectionLine.Trim(),
            threatKind,
            planSignal,
            outcomeSignal,
            verdictLine.Trim());
    }
}
