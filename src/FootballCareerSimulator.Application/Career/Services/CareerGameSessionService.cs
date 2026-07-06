namespace FootballCareerSimulator.Application.Career.Services;

using FootballCareerSimulator.Application.Career.Commands;
using FootballCareerSimulator.Application.Career.Ports;
using FootballCareerSimulator.Application.Competition.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Ports;

public sealed class CareerGameSessionService
{
    private readonly IWorldTimelineStore _timelineStore;
    private readonly ILeagueCompetitionStore _competitionStore;
    private readonly ICareerPersistence _persistence;
    private readonly IReadOnlyList<ICommandIdempotencyReset> _idempotencyResets;

    public CareerGameSessionService(
        IWorldTimelineStore timelineStore,
        ILeagueCompetitionStore competitionStore,
        ICareerPersistence persistence,
        IEnumerable<ICommandIdempotencyReset> idempotencyResets)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _competitionStore = competitionStore ?? throw new ArgumentNullException(nameof(competitionStore));
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        _idempotencyResets = idempotencyResets?.ToArray()
            ?? throw new ArgumentNullException(nameof(idempotencyResets));
    }

    public SaveCareerGameResult Save(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var timeline = _timelineStore.Timeline;
        var league = _competitionStore.League;
        _persistence.Save(filePath, timeline, league);

        var fixtureCount = league.Seasons.Sum(season => season.Fixtures.Count);

        return new SaveCareerGameResult(
            Succeeded: true,
            SavePath: filePath,
            SavedDayNumber: timeline.CurrentDate.DayNumber,
            SavedFixtureCount: fixtureCount);
    }

    public LoadCareerGameResult Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var loaded = _persistence.Load(filePath);
        _timelineStore.Replace(loaded.Timeline);
        _competitionStore.Replace(loaded.League);

        foreach (var reset in _idempotencyResets)
        {
            reset.ResetIdempotencyCache();
        }

        var fixtureCount = loaded.League.Seasons.Sum(season => season.Fixtures.Count);

        return new LoadCareerGameResult(
            Succeeded: true,
            SavePath: filePath,
            LoadedDayNumber: loaded.Timeline.CurrentDate.DayNumber,
            LoadedFixtureCount: fixtureCount,
            WasMigrated: loaded.WasMigrated);
    }
}
