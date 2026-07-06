namespace FootballCareerSimulator.Application.WorldCalendar.Services;

using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Ports;

public sealed class WorldCalendarGameSessionService
{
    private readonly IWorldTimelineStore _timelineStore;
    private readonly AdvanceSimulationTimeHandler _advanceSimulationTime;
    private readonly IWorldCalendarPersistence _persistence;

    public WorldCalendarGameSessionService(
        IWorldTimelineStore timelineStore,
        AdvanceSimulationTimeHandler advanceSimulationTime,
        IWorldCalendarPersistence persistence)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _advanceSimulationTime = advanceSimulationTime ?? throw new ArgumentNullException(nameof(advanceSimulationTime));
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
    }

    public SaveWorldCalendarGameResult Save(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var timeline = _timelineStore.Timeline;
        _persistence.Save(filePath, timeline);

        return new SaveWorldCalendarGameResult(
            Succeeded: true,
            SavePath: filePath,
            SavedDayNumber: timeline.CurrentDate.DayNumber);
    }

    public LoadWorldCalendarGameResult Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var loaded = _persistence.Load(filePath);
        _timelineStore.Replace(loaded.Timeline);
        _advanceSimulationTime.ResetIdempotencyCache();

        return new LoadWorldCalendarGameResult(
            Succeeded: true,
            SavePath: filePath,
            LoadedDayNumber: loaded.Timeline.CurrentDate.DayNumber,
            WasMigrated: loaded.WasMigrated);
    }
}
