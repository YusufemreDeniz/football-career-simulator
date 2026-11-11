namespace FootballCareerSimulator.Application.WorldCalendar.Services;

using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Domain.WorldCalendar.Events;

public sealed class OpenPlanningPeriodHandler : ICommandIdempotencyReset
{
    private readonly IWorldTimelineStore _timelineStore;
    private readonly Dictionary<Guid, OpenPlanningPeriodResult> _completedCommands = new();

    public OpenPlanningPeriodHandler(IWorldTimelineStore timelineStore)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
    }

    public OpenPlanningPeriodResult Handle(OpenPlanningPeriodCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completedCommands.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        GameDate startDate;
        GameDate? expectedEndDate = null;

        try
        {
            startDate = GameDate.FromDayNumber(command.StartDayNumber);
            if (command.ExpectedEndDayNumber is { } expectedEndDayNumber)
            {
                expectedEndDate = GameDate.FromDayNumber(expectedEndDayNumber);
            }
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new WorldCalendarInvariantViolationException(ex.Message);
        }

        var timeline = _timelineStore.Timeline;
        var period = timeline.OpenPlanningPeriod(
            new PlanningPeriodId(command.PlanningPeriodId),
            startDate,
            expectedEndDate);

        timeline.ClearUncommittedEvents();

        var result = new OpenPlanningPeriodResult(
            Succeeded: true,
            PlanningPeriodId: period.Id.Value,
            Status: period.Status.ToString(),
            RaisedEventTypes: [nameof(PlanningPeriodStarted)]);

        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();
}
