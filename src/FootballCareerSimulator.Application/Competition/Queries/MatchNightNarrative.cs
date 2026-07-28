namespace FootballCareerSimulator.Application.Competition.Queries;

/// <summary>
/// Maç gecesi anlatısı — skor, ton, anlar, düdük sonrası (UI bu modeli gösterir).
/// </summary>
public sealed record MatchNightNarrative(
    string BrandTitle,
    string Scoreline,
    string OutcomeTone,
    string SupportingLine,
    IReadOnlyList<string> BeatLines,
    IReadOnlyList<string> AfterWhistleLines,
    IReadOnlyList<string> OtherScorelines)
{
    public static MatchNightNarrative Failure(string message) =>
        new(
            "Maç Gecesi",
            "—",
            message,
            string.Empty,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>());

    public static MatchNightNarrative Compose(
        string scoreline,
        int homeGoals,
        int awayGoals,
        bool managedIsHome,
        bool hasManagedMatch,
        string? tacticNote,
        int dayNumber,
        IReadOnlyList<string> beatLines,
        IReadOnlyList<string> afterWhistleLines,
        IReadOnlyList<string> otherScorelines)
    {
        var tone = hasManagedMatch
            ? ToneForManaged(homeGoals, awayGoals, managedIsHome, afterWhistleLines)
            : "Lig günü tamamlandı.";
        var support = string.IsNullOrWhiteSpace(tacticNote)
            ? $"Gün {dayNumber}"
            : $"Gün {dayNumber} · {tacticNote}";

        return new MatchNightNarrative(
            hasManagedMatch ? "Maç Gecesi" : "Lig Günü",
            scoreline,
            tone,
            support,
            beatLines,
            afterWhistleLines.Take(3).ToArray(),
            otherScorelines);
    }

    public static string ToneForManaged(
        int homeGoals,
        int awayGoals,
        bool managedIsHome,
        IReadOnlyList<string> afterWhistle)
    {
        if (afterWhistle.Any(line => line.Contains("işten çıkardı", StringComparison.OrdinalIgnoreCase)))
        {
            return "Gecenin sonunda koltuk gitti.";
        }

        var managedGoals = managedIsHome ? homeGoals : awayGoals;
        var opponentGoals = managedIsHome ? awayGoals : homeGoals;
        var margin = managedGoals - opponentGoals;

        if (margin > 0)
        {
            return margin >= 3
                ? "Sahayı domine ettik."
                : margin == 1
                    ? "İnce bir galibiyet."
                    : "Üç puan bizim.";
        }

        if (margin == 0)
        {
            return "Puanlar paylaşıldı.";
        }

        if (afterWhistle.Any(line => line.Contains("basın sorusu", StringComparison.OrdinalIgnoreCase)))
        {
            return "Ağır yenilgi — basın kapıda.";
        }

        return margin <= -3
            ? "Sahada dağıldık."
            : "Bu gece olmadı.";
    }
}
