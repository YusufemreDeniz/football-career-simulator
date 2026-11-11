namespace FootballCareerSimulator.Application.WorldCalendar.Services;

using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Domain.WorldCalendar.Events;

public sealed class CompletePlanningPeriodHandler : ICommandIdempotencyReset
{
    private readonly IWorldTimelineStore _timelineStore;
    private readonly Dictionary<Guid, CompletePlanningPeriodResult> _completedCommands = new();

    public CompletePlanningPeriodHandler(IWorldTimelineStore timelineStore)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
    }

    public CompletePlanningPeriodResult Handle(CompletePlanningPeriodCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completedCommands.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        var timeline = _timelineStore.Timeline;
        var periodId = timeline.ActivePlanningPeriod?.Id.Value
            ?? throw new WorldCalendarInvariantViolationException("No active planning period exists to complete.");

        var period = timeline.CompleteActivePlanningPeriod();
        timeline.ClearUncommittedEvents();

        var result = new CompletePlanningPeriodResult(
            Succeeded: true,
            PlanningPeriodId: periodId,
            Status: period.Status.ToString(),
            CompletedAtDayNumber: timeline.CurrentDate.DayNumber,
            RaisedEventTypes: [nameof(PlanningPeriodCompleted)]);

        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();
}
