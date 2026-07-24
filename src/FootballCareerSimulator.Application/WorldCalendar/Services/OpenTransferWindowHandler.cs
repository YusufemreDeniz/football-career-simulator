using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.WorldCalendar.Services;

public sealed class OpenTransferWindowHandler : ICommandIdempotencyReset
{
    private readonly IWorldTimelineStore _timelineStore;
    private readonly Dictionary<Guid, OpenTransferWindowResult> _completedCommands = new();

    public OpenTransferWindowHandler(IWorldTimelineStore timelineStore)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
    }

    public OpenTransferWindowResult Handle(OpenTransferWindowCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_completedCommands.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        GameDate? closesOn = command.ClosesOnDayNumber is { } day
            ? GameDate.FromDayNumber(day)
            : null;
        var window = _timelineStore.Timeline.OpenTransferWindow(closesOn);
        _timelineStore.Timeline.ClearUncommittedEvents();

        var result = new OpenTransferWindowResult(
            command.CommandId,
            window.IsOpen,
            window.OpenedOn!.Value.DayNumber,
            window.ClosesOn?.DayNumber);
        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();
}
