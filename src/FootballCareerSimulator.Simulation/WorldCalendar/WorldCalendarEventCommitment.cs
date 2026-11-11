namespace FootballCareerSimulator.Simulation.WorldCalendar;

using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Domain.WorldCalendar.Events;

public sealed record CommittedWorldCalendarEvent(
    Guid EventId,
    Guid CorrelationId,
    Guid? CausationId,
    SimulationStepId SimulationStepId,
    WorldCalendarDomainEvent DomainEvent);

public static class WorldCalendarEventCommitment
{
    public static CommittedWorldCalendarEvent Commit(
        WorldCalendarDomainEvent domainEvent,
        Guid correlationId,
        Guid? causationId,
        Func<int, long, Guid> eventIdFactory)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(eventIdFactory);

        return new CommittedWorldCalendarEvent(
            eventIdFactory(0, domainEvent.SimulationStepId.Value),
            correlationId,
            causationId,
            domainEvent.SimulationStepId,
            domainEvent);
    }
}
