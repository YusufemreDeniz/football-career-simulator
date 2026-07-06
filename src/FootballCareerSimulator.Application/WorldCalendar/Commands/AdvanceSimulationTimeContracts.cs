namespace FootballCareerSimulator.Application.WorldCalendar.Commands;

public sealed record AdvanceSimulationTimeCommand(
    Guid CommandId,
    int TargetDayNumber);

public sealed record AdvanceSimulationTimeResult
{
    public required bool Succeeded { get; init; }

    public required bool WasBlocked { get; init; }

    public required int PreviousDayNumber { get; init; }

    public required int NewDayNumber { get; init; }

    public required IReadOnlyList<string> RaisedEventTypes { get; init; }

    public required IReadOnlyList<TimeAdvanceBlockedItem> Blockers { get; init; }

    public static AdvanceSimulationTimeResult Blocked(
        int currentDayNumber,
        IReadOnlyList<TimeAdvanceBlockedItem> blockers) =>
        new()
        {
            Succeeded = false,
            WasBlocked = true,
            PreviousDayNumber = currentDayNumber,
            NewDayNumber = currentDayNumber,
            RaisedEventTypes = Array.Empty<string>(),
            Blockers = blockers,
        };

    public static AdvanceSimulationTimeResult Advanced(
        int previousDayNumber,
        int newDayNumber,
        IReadOnlyList<string> raisedEventTypes) =>
        new()
        {
            Succeeded = true,
            WasBlocked = false,
            PreviousDayNumber = previousDayNumber,
            NewDayNumber = newDayNumber,
            RaisedEventTypes = raisedEventTypes,
            Blockers = Array.Empty<TimeAdvanceBlockedItem>(),
        };
}

public sealed record TimeAdvanceBlockedItem(
    string SourceContext,
    string BlockerTypeCode,
    string DescriptionCode,
    bool IsHardBlocker);
