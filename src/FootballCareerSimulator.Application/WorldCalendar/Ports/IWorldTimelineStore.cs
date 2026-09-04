namespace FootballCareerSimulator.Application.WorldCalendar.Ports;

using FootballCareerSimulator.Domain.WorldCalendar;

public interface IWorldTimelineStore
{
    WorldTimeline Timeline { get; }

    void Replace(WorldTimeline timeline);
}
