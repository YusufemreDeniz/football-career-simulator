namespace FootballCareerSimulator.Application.WorldCalendar.Infrastructure;

using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.WorldCalendar;

/// <summary>
/// Tek oturumluk in-memory timeline deposu. Persistence Kart 5'te değiştirilecektir.
/// </summary>
public sealed class InMemoryWorldTimelineStore : IWorldTimelineStore
{
    public InMemoryWorldTimelineStore(WorldTimeline timeline)
    {
        Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
    }

    public WorldTimeline Timeline { get; private set; }

    public void Replace(WorldTimeline timeline) => Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
}
