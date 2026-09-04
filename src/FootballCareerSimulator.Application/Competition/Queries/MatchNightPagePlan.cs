namespace FootballCareerSimulator.Application.Competition.Queries;

/// <summary>
/// Maç sonucu ekranı sayfa planı — tek uzun kaydırma yerine skor → maç → sonrası.
/// </summary>
public enum MatchNightPageKind
{
    Score = 1,
    Match = 2,
    Aftermath = 3,
}

public sealed record MatchNightPage(
    MatchNightPageKind Kind,
    string MarkerCode,
    string MarkerTitle,
    string AccentLabel,
    string ContinueLabel,
    bool IsFinal);

public static class MatchNightPagePlan
{
    public static IReadOnlyList<MatchNightPage> Build(
        MatchNightNarrative narrative,
        bool hasReport,
        bool hasTechnicalArea,
        bool hasRoundup,
        bool hasDressingRoom)
    {
        ArgumentNullException.ThrowIfNull(narrative);

        var hasMatchBody = narrative.KickoffLines.Count > 0
            || narrative.LineupBridge is { StartingXi.Count: > 0 }
            || narrative.BeatLines.Count > 0
            || hasTechnicalArea;

        var hasAftermathBody = hasReport
            || narrative.AfterWhistleLines.Count > 0
            || hasRoundup
            || narrative.OtherScorelines.Count > 0;

        // Dressing room always rides on the score page when present; it does not create a page alone.
        _ = hasDressingRoom;

        var kinds = new List<MatchNightPageKind> { MatchNightPageKind.Score };
        if (hasMatchBody)
        {
            kinds.Add(MatchNightPageKind.Match);
        }

        if (hasAftermathBody)
        {
            kinds.Add(MatchNightPageKind.Aftermath);
        }

        var pages = new List<MatchNightPage>(kinds.Count);
        for (var i = 0; i < kinds.Count; i++)
        {
            var isFinal = i == kinds.Count - 1;
            pages.Add(Describe(kinds[i], markerCode: $"{i + 1:00}", isFinal));
        }

        return pages;
    }

    private static MatchNightPage Describe(MatchNightPageKind kind, string markerCode, bool isFinal)
    {
        var continueLabel = isFinal ? "Kariyere Dön" : "Devam";
        return kind switch
        {
            MatchNightPageKind.Score => new MatchNightPage(
                kind, markerCode, "SONUÇ", "SKOR", continueLabel, isFinal),
            MatchNightPageKind.Match => new MatchNightPage(
                kind, markerCode, "MAÇ", "AKIŞ", continueLabel, isFinal),
            MatchNightPageKind.Aftermath => new MatchNightPage(
                kind, markerCode, "SONRASI", "DÜDÜK", continueLabel, isFinal),
            _ => new MatchNightPage(kind, markerCode, "MAÇ", "RAPOR", continueLabel, isFinal),
        };
    }
}
