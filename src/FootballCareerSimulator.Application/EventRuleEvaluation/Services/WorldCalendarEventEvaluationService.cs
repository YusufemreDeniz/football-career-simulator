using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar.Events;
using FootballCareerSimulator.Simulation.WorldCalendar;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Services;

public sealed record EvaluatedWorldCalendarEffect(
    EventEnvelopeMetadata Envelope,
    string EventType,
    EventEffectApplicationStatus Status);

/// <summary>
/// World Calendar raised event'lerini deterministik EventId + causation zinciri ile commit eder;
/// effect idempotency kaydını günceller. Downstream reaction/ledger yok.
/// </summary>
public sealed class WorldCalendarEventEvaluationService
{
    private readonly EventEffectIdempotencyGate _gate;

    public WorldCalendarEventEvaluationService(EventEffectIdempotencyGate gate)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    }

    public IReadOnlyList<EvaluatedWorldCalendarEffect> Evaluate(
        IReadOnlyList<WorldCalendarDomainEvent> raisedEvents,
        int rootSeed,
        Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(raisedEvents);

        var results = new List<EvaluatedWorldCalendarEffect>(raisedEvents.Count);
        Guid? causationId = null;
        var sequence = 0L;

        foreach (var domainEvent in raisedEvents)
        {
            var localSequence = sequence++;
            var committed = WorldCalendarEventCommitment.Commit(
                domainEvent,
                correlationId,
                causationId,
                (_, _) => DeterministicGuidFactory.Create(
                    rootSeed,
                    unchecked(domainEvent.SimulationStepId.Value * 100_003L + localSequence)));

            var status = _gate.TryApplyCommit(committed.EventId);
            var envelope = new EventEnvelopeMetadata(
                committed.EventId,
                committed.CorrelationId,
                committed.CausationId,
                EventEffectIdempotencyGate.WorldCalendarConsumerId,
                committed.SimulationStepId.Value,
                domainEvent.OccurredAtGameTime.DayNumber);

            results.Add(new EvaluatedWorldCalendarEffect(
                envelope,
                MapEventType(domainEvent),
                status));

            causationId = committed.EventId;
        }

        return results;
    }

    private static string MapEventType(WorldCalendarDomainEvent domainEvent) => domainEvent switch
    {
        GameDayStarted => nameof(GameDayStarted),
        GameDayCompleted => nameof(GameDayCompleted),
        GameTimeAdvanced => nameof(GameTimeAdvanced),
        PlanningPeriodStarted => nameof(PlanningPeriodStarted),
        PlanningPeriodCompleted => nameof(PlanningPeriodCompleted),
        _ => domainEvent.GetType().Name,
    };
}
