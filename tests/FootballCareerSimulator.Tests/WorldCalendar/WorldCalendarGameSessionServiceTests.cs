using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Services;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.WorldCalendar;

namespace FootballCareerSimulator.Tests.WorldCalendar;

public sealed class WorldCalendarGameSessionServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly WorldCalendarSqlitePersistence _persistence = new();

    public WorldCalendarGameSessionServiceTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "fcs-worldcalendar-session-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_RestoresTimelineInStore()
    {
        var module = WorldCalendarModule.CreateNewGame(persistence: _persistence);
        var session = module.GameSession!;

        module.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(
                Guid.NewGuid(),
                GameDate.FromCalendarDate(2026, 7, 5).DayNumber));

        var path = Path.Combine(_tempDirectory, "session.db");
        var saveResult = session.Save(path);

        Assert.True(saveResult.Succeeded);
        Assert.Equal(GameDate.FromCalendarDate(2026, 7, 5).DayNumber, saveResult.SavedDayNumber);

        module.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(
                Guid.NewGuid(),
                GameDate.FromCalendarDate(2026, 7, 10).DayNumber));

        var loadResult = session.Load(path);

        Assert.True(loadResult.Succeeded);
        Assert.Equal(GameDate.FromCalendarDate(2026, 7, 5).DayNumber, loadResult.LoadedDayNumber);
        Assert.Equal(
            GameDate.FromCalendarDate(2026, 7, 5).DayNumber,
            module.Queries.GetCurrentGameDate().DayNumber);
    }

    [Fact]
    public void Load_ClearsAdvanceHandlerIdempotencyCache()
    {
        var module = WorldCalendarModule.CreateNewGame(persistence: _persistence);
        var session = module.GameSession!;
        var advanceToDayFiveId = Guid.NewGuid();

        session.Save(Path.Combine(_tempDirectory, "checkpoint.db"));

        module.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(
                advanceToDayFiveId,
                GameDate.FromCalendarDate(2026, 7, 5).DayNumber));

        Assert.Equal(
            GameDate.FromCalendarDate(2026, 7, 5).DayNumber,
            module.Queries.GetCurrentGameDate().DayNumber);

        session.Load(Path.Combine(_tempDirectory, "checkpoint.db"));

        var replay = module.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(
                advanceToDayFiveId,
                GameDate.FromCalendarDate(2026, 7, 5).DayNumber));

        Assert.True(replay.Succeeded);
        Assert.Equal(
            GameDate.FromCalendarDate(2026, 7, 5).DayNumber,
            module.Queries.GetCurrentGameDate().DayNumber);
    }
}
