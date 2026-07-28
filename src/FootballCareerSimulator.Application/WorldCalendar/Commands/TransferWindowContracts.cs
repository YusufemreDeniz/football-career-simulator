namespace FootballCareerSimulator.Application.WorldCalendar.Commands;

public sealed record OpenTransferWindowCommand(
    Guid CommandId,
    int? ClosesOnDayNumber);

public sealed record OpenTransferWindowResult(
    Guid CommandId,
    bool IsOpen,
    int OpenedOnDayNumber,
    int? ClosesOnDayNumber,
    int AppliedEffectCount = 0,
    int ReactionIntentCount = 0,
    IReadOnlyList<string>? RaisedEventTypes = null);

public sealed record CloseTransferWindowCommand(Guid CommandId);

public sealed record CloseTransferWindowResult(
    Guid CommandId,
    bool IsOpen,
    int AppliedEffectCount = 0,
    int ReactionIntentCount = 0,
    IReadOnlyList<string>? RaisedEventTypes = null);
