using FootballCareerSimulator.Application.EventRuleEvaluation.Infrastructure;
using FootballCareerSimulator.Application.EventRuleEvaluation.Ports;
using FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Application.WorldCalendar.Ports;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Composition;

/// <summary>
/// Event & Rule Evaluation minimal iskeleti — ledger/SQLite processing ledger yok.
/// </summary>
public sealed class EventRuleEvaluationModule : ICommandIdempotencyReset
{
    public EventRuleEvaluationModule(
        IEventEffectIdempotencyRegistry registry,
        EventEffectIdempotencyGate gate,
        ReactionRuleDispatcher reactionDispatcher,
        WorldCalendarEventEvaluationService worldCalendarEvaluation)
    {
        Registry = registry;
        Gate = gate;
        ReactionDispatcher = reactionDispatcher;
        WorldCalendarEvaluation = worldCalendarEvaluation;
    }

    public IEventEffectIdempotencyRegistry Registry { get; }

    public EventEffectIdempotencyGate Gate { get; }

    public ReactionRuleDispatcher ReactionDispatcher { get; }

    public WorldCalendarEventEvaluationService WorldCalendarEvaluation { get; }

    public void ResetIdempotencyCache() => Registry.Clear();

    public static EventRuleEvaluationModule Create()
    {
        var registry = new InMemoryEventEffectIdempotencyRegistry();
        var gate = new EventEffectIdempotencyGate(registry);
        var reactions = new ReactionRuleDispatcher(
            gate,
            [new ObserveGameDayStartedReactionRule()]);
        var evaluation = new WorldCalendarEventEvaluationService(gate, reactions);
        return new EventRuleEvaluationModule(registry, gate, reactions, evaluation);
    }
}
