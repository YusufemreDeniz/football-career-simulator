namespace FootballCareerSimulator.Application.WorldCalendar.Queries;

public sealed record CurrentGameDateReadModel(
    int DayNumber,
    string IsoDate,
    int Year,
    int Month,
    int Day);

public sealed record CurrentPlanningPeriodReadModel(
    long PlanningPeriodId,
    string Status,
    int StartDayNumber,
    string StartIsoDate,
    int? ExpectedEndDayNumber,
    string? ExpectedEndIsoDate);

public sealed record TimeAdvanceBlockerReadModel(
    string SourceContext,
    string BlockerTypeCode,
    string DescriptionCode,
    bool IsHardBlocker);

public sealed record TimeAdvanceEligibilityReadModel(
    bool CanAdvance,
    int CurrentDayNumber,
    IReadOnlyList<TimeAdvanceBlockerReadModel> Blockers);
