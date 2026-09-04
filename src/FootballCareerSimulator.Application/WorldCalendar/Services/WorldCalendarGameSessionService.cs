namespace FootballCareerSimulator.Application.WorldCalendar.Services;

using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Ports;

public sealed class WorldCalendarGameSessionService
{
    private readonly IWorldTimelineStore _timelineStore;
    private readonly IWorldCalendarPersistence _persistence;
    private readonly IReadOnlyList<ICommandIdempotencyReset> _idempotencyResets;

    public WorldCalendarGameSessionService(
        IWorldTimelineStore timelineStore,
        IWorldCalendarPersistence persistence,
        IEnumerable<ICommandIdempotencyReset> idempotencyResets)
    {
        _timelineStore = timelineStore ?? throw new ArgumentNullException(nameof(timelineStore));
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        _idempotencyResets = idempotencyResets?.ToArray()
            ?? throw new ArgumentNullException(nameof(idempotencyResets));
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

        foreach (var reset in _idempotencyResets)
        {
            reset.ResetIdempotencyCache();
        }

        return new LoadWorldCalendarGameResult(
            Succeeded: true,
            SavePath: filePath,
            LoadedDayNumber: loaded.Timeline.CurrentDate.DayNumber,
            WasMigrated: loaded.WasMigrated);
    }
}
