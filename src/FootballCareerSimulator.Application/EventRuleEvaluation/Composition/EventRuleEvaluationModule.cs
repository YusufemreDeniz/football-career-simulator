using FootballCareerSimulator.Application.EventRuleEvaluation.Infrastructure;
using FootballCareerSimulator.Application.EventRuleEvaluation.Ports;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Application.WorldCalendar.Ports;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Composition;

/// <summary>
/// Event & Rule Evaluation minimal iskeleti — ledger/SQLite yok.
/// </summary>
public sealed class EventRuleEvaluationModule : ICommandIdempotencyReset
{
    public EventRuleEvaluationModule(
        IEventEffectIdempotencyRegistry registry,
        EventEffectIdempotencyGate gate,
        WorldCalendarEventEvaluationService worldCalendarEvaluation)
    {
        Registry = registry;
        Gate = gate;
        WorldCalendarEvaluation = worldCalendarEvaluation;
    }

    public IEventEffectIdempotencyRegistry Registry { get; }

    public EventEffectIdempotencyGate Gate { get; }

    public WorldCalendarEventEvaluationService WorldCalendarEvaluation { get; }

    public void ResetIdempotencyCache() => Registry.Clear();

    public static EventRuleEvaluationModule Create()
    {
        var registry = new InMemoryEventEffectIdempotencyRegistry();
        var gate = new EventEffectIdempotencyGate(registry);
        var evaluation = new WorldCalendarEventEvaluationService(gate);
        return new EventRuleEvaluationModule(registry, gate, evaluation);
    }
}
