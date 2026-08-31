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
    IReadOnlyList<MatchupPlanNotebookEntry> MatchupPlanHistory,
    long? PendingMatchTrainingFixtureId,
    string? PendingMatchTrainingPriorityCode,
    int? PendingMatchTrainingModifier)
{
    public static HubNarrativeUiState Empty { get; } =
        new(
            null,
            false,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<MatchupPlanNotebookEntry>(),
            null,
            null,
            null);

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(WeekStoryClosureBeat)
        && !WeekStoryDismissOnNextAdvance
        && CleanXiNames.Count == 0
        && InjuryClearedNames.Count == 0
        && MatchupPlanHistory.Count == 0
        && PendingMatchTrainingFixtureId is null;

    public static HubNarrativeUiState Compose(
        string? weekStoryClosureBeat,
        bool weekStoryDismissOnNextAdvance,
        IReadOnlyList<string>? cleanXiNames,
        IReadOnlyList<string>? injuryClearedNames,
        IReadOnlyList<MatchupPlanNotebookEntry>? matchupPlanHistory = null,
        long? pendingMatchTrainingFixtureId = null,
        string? pendingMatchTrainingPriorityCode = null,
        int? pendingMatchTrainingModifier = null)
    {
        var hasPendingMatchTraining = pendingMatchTrainingFixtureId is > 0
            && !string.IsNullOrWhiteSpace(pendingMatchTrainingPriorityCode)
            && pendingMatchTrainingModifier is not null;
        return
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
                .ToArray(),
            hasPendingMatchTraining ? pendingMatchTrainingFixtureId : null,
            hasPendingMatchTraining ? pendingMatchTrainingPriorityCode!.Trim() : null,
            hasPendingMatchTraining ? Math.Clamp(pendingMatchTrainingModifier!.Value, -4, 4) : null);
    }
}
