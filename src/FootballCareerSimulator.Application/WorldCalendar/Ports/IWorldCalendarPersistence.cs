using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.WorldCalendar.Ports;

public interface IWorldCalendarPersistence
{
    void Save(string filePath, WorldTimeline timeline);

    WorldCalendarLoadResult Load(string filePath);
}

public sealed record WorldCalendarLoadResult(
    WorldTimeline Timeline,
    int SchemaVersion,
    bool WasMigrated);
