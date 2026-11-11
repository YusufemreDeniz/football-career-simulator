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
    MatchDayLineupStrip? LineupBridge = null,
    int? ManagedGoalMargin = null,
    StadiumAtmosphereDigest? Atmosphere = null)
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
        MatchDayLineupStrip? lineupBridge = null,
        StadiumAtmosphereDigest? atmosphere = null)
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
        int? margin = hasManagedMatch
            ? (managedIsHome ? homeGoals - awayGoals : awayGoals - homeGoals)
            : null;

        return new MatchNightNarrative(
            hasManagedMatch ? "Maç Gecesi" : "Lig Günü",
            scoreline,
            tone,
            support,
            beatLines,
            PreferCriticalAfterWhistleLines(afterWhistleLines),
            otherScorelines,
            PreferKickoffBridgeLines(kickoffLines),
            hasManagedMatch && lineupBridge is { StartingXi.Count: > 0 }
                ? lineupBridge
                : null,
            margin,
            hasManagedMatch ? atmosphere : null);
    }

    /// <summary>
    /// Düdük sonrası satırlarını kısarken kritik olanları koru (kovulma, basın, güven).
    /// </summary>
    private static IReadOnlyList<string> PreferCriticalAfterWhistleLines(
        IReadOnlyList<string> afterWhistleLines)
    {
        if (afterWhistleLines.Count <= 3)
        {
            return afterWhistleLines.ToArray();
        }

        static bool IsCritical(string line) =>
            line.Contains("işten çıkardı", StringComparison.OrdinalIgnoreCase)
            || line.Contains("basın", StringComparison.OrdinalIgnoreCase)
            || line.Contains("Yönetim güveni", StringComparison.Ordinal)
            || line.Contains("Yönetim talebi", StringComparison.OrdinalIgnoreCase)
            || line.Contains("forma süresi talebi", StringComparison.OrdinalIgnoreCase)
            || line.Contains("Disiplin", StringComparison.OrdinalIgnoreCase)
            || line.Contains("kırmızı kart", StringComparison.OrdinalIgnoreCase)
            || line.Contains(MatchupPlanOutcomeDigest.Brand, StringComparison.Ordinal);

        return afterWhistleLines
            .Where(IsCritical)
            .Concat(afterWhistleLines.Where(line => !IsCritical(line)))
            .Take(3)
            .ToArray();
    }

    /// <summary>
    /// Anları yarılara böler — 45' eşiği; HT ekstraları (karar/değişiklik) ikinci yarı başına koyar.
    /// </summary>
    public static IReadOnlyList<string> ComposeHalfSegmentedBeats(
        IReadOnlyList<string> firstHalfLines,
        IReadOnlyList<string> secondHalfLines,
        IReadOnlyList<string>? secondHalfExtras = null)
    {
        ArgumentNullException.ThrowIfNull(firstHalfLines);
        ArgumentNullException.ThrowIfNull(secondHalfLines);

        var beats = new List<string>();
        if (firstHalfLines.Count > 0)
        {
            beats.Add("1. Yarı");
            beats.AddRange(firstHalfLines);
        }

        var hasExtras = secondHalfExtras is { Count: > 0 };
        if (secondHalfLines.Count > 0 || hasExtras)
        {
            beats.Add("2. Yarı");
            if (hasExtras)
            {
                beats.AddRange(secondHalfExtras!);
            }

            beats.AddRange(secondHalfLines);
        }

        return beats;
    }

    /// <summary>
    /// HT karar/değişim satırlarını kesmeden köprüye sığdır (en fazla 6).
    /// </summary>
    public static IReadOnlyList<string> PreferKickoffBridgeLines(
        IReadOnlyList<string>? kickoffLines,
        int maxLines = 6)
    {
        if (kickoffLines is null || kickoffLines.Count == 0 || maxLines <= 0)
        {
            return Array.Empty<string>();
        }

        if (kickoffLines.Count <= maxLines)
        {
            return kickoffLines.ToArray();
        }

        static bool IsHalfTimeLine(string line) =>
            line.StartsWith("Devre arası", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("Devre arasında", StringComparison.OrdinalIgnoreCase);

        var preferred = kickoffLines.Where(IsHalfTimeLine).ToList();
        var rest = kickoffLines.Where(line => !IsHalfTimeLine(line)).ToList();
        var ordered = new List<string>();
        if (rest.Count > 0)
        {
            ordered.Add(rest[0]);
            rest.RemoveAt(0);
        }

        ordered.AddRange(preferred);
        ordered.AddRange(rest);
        return ordered.Take(maxLines).ToArray();
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

        if (afterWhistle.Any(line =>
                line.Contains("Disiplin", StringComparison.OrdinalIgnoreCase)
                || line.Contains("kırmızı kart", StringComparison.OrdinalIgnoreCase)))
        {
            return "Kırmızı kart — soyunma odası bekliyor.";
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
