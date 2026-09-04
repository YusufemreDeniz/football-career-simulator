using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;

namespace FootballCareerSimulator.Application.WorldCalendar.Infrastructure;

public sealed class TimelineTransferWindowQuery : ITransferWindowQuery
{
    private readonly IWorldTimelineStore _timelineStore;

    public TimelineTransferWindowQuery(IWorldTimelineStore timelineStore)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
    }

    public bool IsOpen => _timelineStore.Timeline.TransferWindow.IsOpen;
}
