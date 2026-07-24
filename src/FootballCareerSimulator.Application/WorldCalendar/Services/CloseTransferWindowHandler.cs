using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Ports;

namespace FootballCareerSimulator.Application.WorldCalendar.Services;

public sealed class CloseTransferWindowHandler : ICommandIdempotencyReset
{
    private readonly IWorldTimelineStore _timelineStore;
    private readonly Dictionary<Guid, CloseTransferWindowResult> _completedCommands = new();

    public CloseTransferWindowHandler(IWorldTimelineStore timelineStore)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
    }

    public CloseTransferWindowResult Handle(CloseTransferWindowCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_completedCommands.TryGetValue(command.CommandId, out var cached))
        {
            return cached;
        }

        var window = _timelineStore.Timeline.CloseTransferWindow();
        _timelineStore.Timeline.ClearUncommittedEvents();

        var result = new CloseTransferWindowResult(command.CommandId, window.IsOpen);
        _completedCommands[command.CommandId] = result;
        return result;
    }

    public void ResetIdempotencyCache() => _completedCommands.Clear();
}
