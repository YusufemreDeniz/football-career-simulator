using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;

namespace FootballCareerSimulator.Infrastructure.WorldCalendar;

public sealed class WorldCalendarSqlitePersistence : IWorldCalendarPersistence
{
    private readonly CareerSqlitePersistence _careerPersistence = new();

    public void Save(string filePath, WorldTimeline timeline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(timeline);

        var managerCareer = ManagerCareer.StartNewCareer(
            new ManagerId(1),
            "Teknik Direktör",
            new ClubId(1),
            timeline.CurrentDate);

        _careerPersistence.Save(
            filePath,
            timeline,
            new LeagueCompetition(new CompetitionId(1)),
            LeagueClubRegistry.CreateMvpLeague(),
            managerCareer);
    }

    public WorldCalendarLoadResult Load(string filePath)
    {
        var loaded = _careerPersistence.Load(filePath);
        return new WorldCalendarLoadResult(
            loaded.Timeline,
            loaded.SchemaVersion,
            loaded.WasMigrated);
    }
}
