namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Kariyer kaydına yazılan Bugün anlatı durumu — Haftanın Hikâyesi / Temiz XI / İyileşti.
/// </summary>
public sealed record HubNarrativeUiState(
    string? WeekStoryClosureBeat,
    bool WeekStoryDismissOnNextAdvance,
    IReadOnlyList<string> CleanXiNames,
    IReadOnlyList<string> InjuryClearedNames)
{
    public static HubNarrativeUiState Empty { get; } =
        new(null, false, Array.Empty<string>(), Array.Empty<string>());

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(WeekStoryClosureBeat)
        && !WeekStoryDismissOnNextAdvance
        && CleanXiNames.Count == 0
        && InjuryClearedNames.Count == 0;

    public static HubNarrativeUiState Compose(
        string? weekStoryClosureBeat,
        bool weekStoryDismissOnNextAdvance,
        IReadOnlyList<string>? cleanXiNames,
        IReadOnlyList<string>? injuryClearedNames) =>
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
                .ToArray());
}
