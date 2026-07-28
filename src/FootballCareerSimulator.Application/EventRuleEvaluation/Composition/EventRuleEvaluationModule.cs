using FootballCareerSimulator.Application.EventRuleEvaluation.Infrastructure;
using FootballCareerSimulator.Application.EventRuleEvaluation.Ports;
using FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Services;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Composition;

/// <summary>
/// Event & Rule Evaluation minimal iskeleti — processing ledger yok.
/// </summary>
public sealed class EventRuleEvaluationModule : ICommandIdempotencyReset
{
    public EventRuleEvaluationModule(
        IEventEffectIdempotencyRegistry registry,
        EventEffectIdempotencyGate gate,
        ReactionRuleDispatcher reactionDispatcher,
        WorldCalendarEventEvaluationService worldCalendarEvaluation,
        IScheduledEvaluationStore scheduledEvaluationStore,
        TransferWindowCloseReactionScheduler? transferWindowCloseScheduler = null,
        DueScheduledEvaluationProcessor? dueProcessor = null)
    {
        Registry = registry;
        Gate = gate;
        ReactionDispatcher = reactionDispatcher;
        WorldCalendarEvaluation = worldCalendarEvaluation;
        ScheduledEvaluationStore = scheduledEvaluationStore;
        TransferWindowCloseScheduler = transferWindowCloseScheduler;
        DueProcessor = dueProcessor;
    }

    public IEventEffectIdempotencyRegistry Registry { get; }

    public EventEffectIdempotencyGate Gate { get; }

    public ReactionRuleDispatcher ReactionDispatcher { get; }

    public WorldCalendarEventEvaluationService WorldCalendarEvaluation { get; }

    public IScheduledEvaluationStore ScheduledEvaluationStore { get; }

    public TransferWindowCloseReactionScheduler? TransferWindowCloseScheduler { get; }

    public DueScheduledEvaluationProcessor? DueProcessor { get; }

    public void ResetIdempotencyCache()
    {
        Registry.Clear();
        ScheduledEvaluationStore.Clear();
    }

    public static EventRuleEvaluationModule Create()
    {
        var registry = new InMemoryEventEffectIdempotencyRegistry();
        var gate = new EventEffectIdempotencyGate(registry);
        var reactions = new ReactionRuleDispatcher(
            gate,
            [new ObserveGameDayStartedReactionRule()]);
        var evaluation = new WorldCalendarEventEvaluationService(gate, reactions);
        var scheduleStore = new InMemoryScheduledEvaluationStore();
        return new EventRuleEvaluationModule(registry, gate, reactions, evaluation, scheduleStore);
    }

    public static EventRuleEvaluationModule CreateForWorldCalendar(
        IWorldTimelineStore timelineStore,
        CloseTransferWindowHandler closeTransferWindow)
    {
        var module = Create();
        var scheduler = new TransferWindowCloseReactionScheduler(
            timelineStore,
            module.ScheduledEvaluationStore);
        var dueProcessor = new DueScheduledEvaluationProcessor(
            module.ScheduledEvaluationStore,
            timelineStore,
            closeTransferWindow);
        return new EventRuleEvaluationModule(
            module.Registry,
            module.Gate,
            module.ReactionDispatcher,
            module.WorldCalendarEvaluation,
            module.ScheduledEvaluationStore,
            scheduler,
            dueProcessor);
    }
}
