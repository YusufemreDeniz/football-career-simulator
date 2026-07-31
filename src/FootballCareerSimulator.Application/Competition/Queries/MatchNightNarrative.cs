namespace FootballCareerSimulator.Application.Competition.Queries;

using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.WorldCalendar;

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
    IReadOnlyList<string> OtherScorelines,
    IReadOnlyList<string> KickoffLines,
    MatchDayLineupStrip? LineupBridge = null)
{
    public static MatchNightNarrative Failure(string message) =>
        new(
            "Maç Gecesi",
            "—",
            message,
            string.Empty,
            Array.Empty<string>(),
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
        IReadOnlyList<string> otherScorelines,
        IReadOnlyList<string>? kickoffLines = null,
        bool enteredWithPromiseRisk = false,
        MatchDayLineupStrip? lineupBridge = null)
    {
        var tone = hasManagedMatch
            ? ToneForManaged(
                homeGoals,
                awayGoals,
                managedIsHome,
                afterWhistleLines,
                enteredWithPromiseRisk)
            : "Lig günü tamamlandı.";
        var matchDate = GameDate.FromDayNumber(dayNumber).ToIsoDateString();
        var support = string.IsNullOrWhiteSpace(tacticNote)
            ? $"Tarih {matchDate}"
            : $"Tarih {matchDate} · {tacticNote}";

        return new MatchNightNarrative(
            hasManagedMatch ? "Maç Gecesi" : "Lig Günü",
            scoreline,
            tone,
            support,
            beatLines,
            afterWhistleLines.Take(3).ToArray(),
            otherScorelines,
            kickoffLines is null || kickoffLines.Count == 0
                ? Array.Empty<string>()
                : kickoffLines.Take(4).ToArray(),
            hasManagedMatch && lineupBridge is { StartingXi.Count: > 0 }
                ? lineupBridge
                : null);
    }

    public static string ToneForManaged(
        int homeGoals,
        int awayGoals,
        bool managedIsHome,
        IReadOnlyList<string> afterWhistle,
        bool enteredWithPromiseRisk = false)
    {
        if (afterWhistle.Any(line => line.Contains("işten çıkardı", StringComparison.OrdinalIgnoreCase)))
        {
            return "Gecenin sonunda koltuk gitti.";
        }

        var managedGoals = managedIsHome ? homeGoals : awayGoals;
        var opponentGoals = managedIsHome ? awayGoals : homeGoals;
        var margin = managedGoals - opponentGoals;

        if (enteredWithPromiseRisk && margin > 0)
        {
            return "Söz gerilimine rağmen kazandın.";
        }

        if (enteredWithPromiseRisk && margin < 0)
        {
            return margin <= -3
                ? "Söz gerilimiyle girdin; saha dağıldı."
                : "Söz gerilimiyle girdin; gece ağır bitti.";
        }

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
            return enteredWithPromiseRisk
                ? "Söz gerilimiyle girdin; puanlar paylaşıldı."
                : "Puanlar paylaşıldı.";
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
