using FootballCareerSimulator.Domain.WorldCalendar.Events;

namespace FootballCareerSimulator.Domain.WorldCalendar;

public sealed record WorldTimelineAdvancementResult(
    GameDate PreviousDate,
    GameDate NewDate,
    SimulationStepId FirstCommittedStepId,
    SimulationStepId LastCommittedStepId,
    IReadOnlyList<WorldCalendarDomainEvent> RaisedEvents);
