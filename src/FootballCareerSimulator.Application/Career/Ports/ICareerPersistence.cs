namespace FootballCareerSimulator.Application.Career.Ports;

using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;

public interface ICareerPersistence
{
    void Save(
        string filePath,
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareer managerCareer);

    CareerLoadResult Load(string filePath);
}

public sealed record CareerLoadResult(
    WorldTimeline Timeline,
    LeagueCompetition League,
    LeagueClubRegistry ClubRegistry,
    ManagerCareer ManagerCareer,
    int SchemaVersion,
    bool WasMigrated);