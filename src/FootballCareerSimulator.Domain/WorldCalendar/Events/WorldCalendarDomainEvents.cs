namespace FootballCareerSimulator.Domain.WorldCalendar.Events;

public abstract record WorldCalendarDomainEvent(
    SimulationStepId SimulationStepId,
    GameDate OccurredAtGameTime);

public sealed record GameDayStarted(
    SimulationStepId SimulationStepId,
    GameDate OccurredAtGameTime)
    : WorldCalendarDomainEvent(SimulationStepId, OccurredAtGameTime);

public sealed record GameDayCompleted(
    SimulationStepId SimulationStepId,
    GameDate OccurredAtGameTime)
    : WorldCalendarDomainEvent(SimulationStepId, OccurredAtGameTime);

public sealed record GameTimeAdvanced(
    SimulationStepId SimulationStepId,
    GameDate OccurredAtGameTime,
    GameDate PreviousGameDate)
    : WorldCalendarDomainEvent(SimulationStepId, OccurredAtGameTime);

public sealed record PlanningPeriodStarted(
    SimulationStepId SimulationStepId,
    GameDate OccurredAtGameTime,
    PlanningPeriodId PlanningPeriodId,
    GameDate StartDate)
    : WorldCalendarDomainEvent(SimulationStepId, OccurredAtGameTime);

public sealed record PlanningPeriodCompleted(
    SimulationStepId SimulationStepId,
    GameDate OccurredAtGameTime,
    PlanningPeriodId PlanningPeriodId,
    GameDate CompletedAt)
    : WorldCalendarDomainEvent(SimulationStepId, OccurredAtGameTime);
