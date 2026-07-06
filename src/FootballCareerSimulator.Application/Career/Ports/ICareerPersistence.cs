namespace FootballCareerSimulator.Application.Career.Ports;

using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.WorldCalendar;

public interface ICareerPersistence
{
    void Save(string filePath, WorldTimeline timeline, LeagueCompetition league);

    CareerLoadResult Load(string filePath);
}

public sealed record CareerLoadResult(
    WorldTimeline Timeline,
    LeagueCompetition League,
    int SchemaVersion,
    bool WasMigrated);
