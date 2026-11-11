using FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar.Events;
using FootballCareerSimulator.Simulation.WorldCalendar;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Services;

public sealed record EvaluatedWorldCalendarEffect(
    EventEnvelopeMetadata Envelope,
    string EventType,
    EventEffectApplicationStatus Status);

public sealed record WorldCalendarEventEvaluationResult(
    IReadOnlyList<EvaluatedWorldCalendarEffect> Effects,
    IReadOnlyList<ReactionIntent> ReactionIntents);

/// <summary>
/// World Calendar raised event commit + effect idempotency + reaction kancası.
/// </summary>
public sealed class WorldCalendarEventEvaluationService
{
    private readonly EventEffectIdempotencyGate _gate;
    private readonly ReactionRuleDispatcher _reactions;

    public WorldCalendarEventEvaluationService(
        EventEffectIdempotencyGate gate,
        ReactionRuleDispatcher reactions)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _reactions = reactions ?? throw new ArgumentNullException(nameof(reactions));
    }

    public WorldCalendarEventEvaluationResult Evaluate(
        IReadOnlyList<WorldCalendarDomainEvent> raisedEvents,
        int rootSeed,
        Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(raisedEvents);

        var effects = new List<EvaluatedWorldCalendarEffect>(raisedEvents.Count);
        var paired = new List<(WorldCalendarDomainEvent, EvaluatedWorldCalendarEffect)>(raisedEvents.Count);
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

            var effect = new EvaluatedWorldCalendarEffect(
                envelope,
                MapEventType(domainEvent),
                status);
            effects.Add(effect);
            paired.Add((domainEvent, effect));

            causationId = committed.EventId;
        }

        var intents = _reactions.Dispatch(paired);
        return new WorldCalendarEventEvaluationResult(effects, intents);
    }

    private static string MapEventType(WorldCalendarDomainEvent domainEvent) => domainEvent switch
    {
        GameDayStarted => nameof(GameDayStarted),
        GameDayCompleted => nameof(GameDayCompleted),
        GameTimeAdvanced => nameof(GameTimeAdvanced),
        PlanningPeriodStarted => nameof(PlanningPeriodStarted),
        PlanningPeriodCompleted => nameof(PlanningPeriodCompleted),
        TransferWindowOpened => nameof(TransferWindowOpened),
        TransferWindowClosed => nameof(TransferWindowClosed),
        _ => domainEvent.GetType().Name,
    };
}
