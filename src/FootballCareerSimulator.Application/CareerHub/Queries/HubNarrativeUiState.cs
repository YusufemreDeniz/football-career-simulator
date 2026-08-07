namespace FootballCareerSimulator.Application.CareerHub.Queries;

using FootballCareerSimulator.Application.TeamPreparation.Queries;

/// <summary>
/// Kariyer kaydına yazılan Bugün anlatı durumu — Haftanın Hikâyesi / Temiz XI / İyileşti.
/// </summary>
public sealed record HubNarrativeUiState(
    string? WeekStoryClosureBeat,
    bool WeekStoryDismissOnNextAdvance,
    IReadOnlyList<string> CleanXiNames,
    IReadOnlyList<string> InjuryClearedNames,
    IReadOnlyList<MatchupPlanNotebookEntry> MatchupPlanHistory)
{
    public static HubNarrativeUiState Empty { get; } =
        new(
            null,
            false,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<MatchupPlanNotebookEntry>());

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(WeekStoryClosureBeat)
        && !WeekStoryDismissOnNextAdvance
        && CleanXiNames.Count == 0
        && InjuryClearedNames.Count == 0
        && MatchupPlanHistory.Count == 0;

    public static HubNarrativeUiState Compose(
        string? weekStoryClosureBeat,
        bool weekStoryDismissOnNextAdvance,
        IReadOnlyList<string>? cleanXiNames,
        IReadOnlyList<string>? injuryClearedNames,
        IReadOnlyList<MatchupPlanNotebookEntry>? matchupPlanHistory = null) =>
        new(
            string.IsNullOrWhiteSpace(weekStoryClosureBeat) ? null : weekStoryClosureBeat.Trim(),
            weekStoryDismissOnNextAdvance,
            (cleanXiNames ?? Array.Empty<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            (injuryClearedNames ?? Array.Empty<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            (matchupPlanHistory ?? Array.Empty<MatchupPlanNotebookEntry>())
                .OrderByDescending(entry => entry.DayNumber)
                .DistinctBy(entry => (entry.DayNumber, entry.OpponentName, entry.SelectionLine))
                .Take(MatchupPlanNotebookEntry.HistoryLimit)
                .OrderBy(entry => entry.DayNumber)
                .ToArray());
}
