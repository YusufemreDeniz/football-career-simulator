namespace FootballCareerSimulator.Application.WorldCalendar.Services;

using FootballCareerSimulator.Application.ContractRegistration.Services;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Domain.WorldCalendar.Events;

public sealed class AdvanceSimulationTimeHandler : ICommandIdempotencyReset
{
    private readonly IWorldTimelineStore _timelineStore;
    private readonly TimeAdvanceBlockerAggregator _blockerAggregator;
    private readonly WorldCalendarEventEvaluationService? _eventEvaluation;
    private readonly TransferWindowCloseReactionScheduler? _reactionScheduler;
    private readonly DueScheduledEvaluationProcessor? _dueProcessor;
    private ContractExpiryDayBoundaryApplier? _contractExpiry;
    private readonly Dictionary<Guid, AdvanceSimulationTimeResult> _completedCommands = new();

    public AdvanceSimulationTimeHandler(
        IWorldTimelineStore timelineStore,
        TimeAdvanceBlockerAggregator blockerAggregator,
        WorldCalendarEventEvaluationService? eventEvaluation = null,
        TransferWindowCloseReactionScheduler? reactionScheduler = null,
        DueScheduledEvaluationProcessor? dueProcessor = null)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _blockerAggregator = blockerAggregator ?? throw new ArgumentNullException(nameof(blockerAggregator));
        _eventEvaluation = eventEvaluation;
        _reactionScheduler = reactionScheduler;
        _dueProcessor = dueProcessor;
    }

    public void BindContractExpiryConsequences(ContractExpiryDayBoundaryApplier applier) =>
        _contractExpiry = applier ?? throw new ArgumentNullException(nameof(applier));

    public AdvanceSimulationTimeResult Handle(AdvanceSimulationTimeCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completedCommands.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        var timeline = _timelineStore.Timeline;
        var currentDayNumber = timeline.CurrentDate.DayNumber;

        var blockers = _blockerAggregator.GetActiveBlockers();
        if (blockers.Count > 0)
        {
            var blocked = AdvanceSimulationTimeResult.Blocked(
                currentDayNumber,
                blockers
                    .Select(blocker => new TimeAdvanceBlockedItem(
                        blocker.SourceContext,
                        blocker.BlockerTypeCode,
                        blocker.DescriptionCode,
                        blocker.IsHardBlocker))
                    .ToArray());

            _completedCommands[command.CommandId] = blocked;
            return blocked;
        }

        GameDate targetDate;
        try
        {
            targetDate = GameDate.FromDayNumber(command.TargetDayNumber);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new WorldCalendarInvariantViolationException(ex.Message);
        }

        WorldTimelineAdvancementResult advancement;
        try
        {
            advancement = timeline.AdvanceTo(targetDate);
        }
        catch (WorldCalendarInvariantViolationException)
        {
            throw;
        }

        var applied = 0;
        var duplicates = 0;
        var reactionCount = 0;
        IReadOnlyList<string> reactionTypes = Array.Empty<string>();
        var scheduledCount = 0;
        var dueProcessed = 0;
        var windowsClosed = 0;
        var expiredContracts = 0;
        IReadOnlyList<long> affectedClubs = Array.Empty<long>();
        IReadOnlyList<long> freeAgents = Array.Empty<long>();
        if (_eventEvaluation is not null)
        {
            var evaluated = _eventEvaluation.Evaluate(
                advancement.RaisedEvents,
                timeline.RootSeed,
                command.CommandId);
            applied = evaluated.Effects.Count(e => e.Status == EventEffectApplicationStatus.Applied);
            duplicates = evaluated.Effects.Count(e => e.Status == EventEffectApplicationStatus.Duplicate);
            reactionCount = evaluated.ReactionIntents.Count;
            reactionTypes = evaluated.ReactionIntents
                .Select(intent => intent.IntentTypeCode)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToArray();

            if (_contractExpiry is not null)
            {
                var expiry = _contractExpiry.ApplyFromReactions(evaluated.ReactionIntents);
                expiredContracts = expiry.ExpiredCount;
                affectedClubs = expiry.AffectedClubIds;
                freeAgents = expiry.FreeAgentPlayerIds;
            }

            if (_reactionScheduler is not null)
            {
                scheduledCount = _reactionScheduler.ScheduleFromReactions(evaluated.ReactionIntents);
            }

            if (_dueProcessor is not null)
            {
                var due = _dueProcessor.ProcessDueThrough(
                    advancement.NewDate.DayNumber,
                    timeline.RootSeed,
                    command.CommandId);
                dueProcessed = due.ProcessedCount;
                windowsClosed = due.TransferWindowsClosed;
            }
        }

        timeline.ClearUncommittedEvents();

        var result = AdvanceSimulationTimeResult.Advanced(
            advancement.PreviousDate.DayNumber,
            advancement.NewDate.DayNumber,
            advancement.RaisedEvents.Select(MapEventType).ToArray(),
            applied,
            duplicates,
            reactionCount,
            reactionTypes,
            scheduledCount,
            dueProcessed,
            windowsClosed,
            expiredContracts,
            affectedClubs,
            freeAgents);

        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();

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
