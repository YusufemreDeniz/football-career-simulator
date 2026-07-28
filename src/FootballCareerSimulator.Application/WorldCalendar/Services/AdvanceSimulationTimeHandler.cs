namespace FootballCareerSimulator.Application.WorldCalendar.Services;

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
    private readonly Dictionary<Guid, AdvanceSimulationTimeResult> _completedCommands = new();

    public AdvanceSimulationTimeHandler(
        IWorldTimelineStore timelineStore,
        TimeAdvanceBlockerAggregator blockerAggregator,
        WorldCalendarEventEvaluationService? eventEvaluation = null)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _blockerAggregator = blockerAggregator ?? throw new ArgumentNullException(nameof(blockerAggregator));
        _eventEvaluation = eventEvaluation;
    }

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
        if (_eventEvaluation is not null)
        {
            var evaluated = _eventEvaluation.Evaluate(
                advancement.RaisedEvents,
                timeline.RootSeed,
                command.CommandId);
            applied = evaluated.Count(e => e.Status == EventEffectApplicationStatus.Applied);
            duplicates = evaluated.Count(e => e.Status == EventEffectApplicationStatus.Duplicate);
        }

        timeline.ClearUncommittedEvents();

        var result = AdvanceSimulationTimeResult.Advanced(
            advancement.PreviousDate.DayNumber,
            advancement.NewDate.DayNumber,
            advancement.RaisedEvents.Select(MapEventType).ToArray(),
            applied,
            duplicates);

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
        _ => domainEvent.GetType().Name,
    };
}
