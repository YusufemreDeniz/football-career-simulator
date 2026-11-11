namespace FootballCareerSimulator.Domain.WorldCalendar;

/// <summary>
/// docs/12_WORLD_SIMULATION.md Bölüm 10.1 yaşam döngüsü.
/// </summary>
public enum PlanningPeriodStatus
{
    Created = 0,
    Open = 1,
    AwaitingRequiredDecisions = 2,
    ReadyToAdvance = 3,
    Processing = 4,
    Interrupted = 5,
    Completed = 6,
    Archived = 7,
}
