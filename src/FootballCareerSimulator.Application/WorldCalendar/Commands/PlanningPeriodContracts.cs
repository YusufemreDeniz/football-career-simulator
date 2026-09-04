namespace FootballCareerSimulator.Application.WorldCalendar.Commands;

public sealed record OpenPlanningPeriodCommand(
    Guid CommandId,
    long PlanningPeriodId,
    int StartDayNumber,
    int? ExpectedEndDayNumber = null);

public sealed record OpenPlanningPeriodResult(
    bool Succeeded,
    long PlanningPeriodId,
    string Status,
    IReadOnlyList<string> RaisedEventTypes);

public sealed record CompletePlanningPeriodCommand(Guid CommandId);

public sealed record CompletePlanningPeriodResult(
    bool Succeeded,
    long PlanningPeriodId,
    string Status,
    int CompletedAtDayNumber,
    IReadOnlyList<string> RaisedEventTypes);
