using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Services;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.WorldCalendar;

namespace FootballCareerSimulator.Tests.WorldCalendar;

public sealed class WorldCalendarGameSessionServiceTests : IDisposable
{
    private static readonly GameDate NewGameStart = GameDate.FromCalendarDate(2026, 8, 10);
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

    [Fact]
    public void CreateNewGame_StartsOnDataPackSnapshotDate()
    {
        var module = WorldCalendarModule.CreateNewGame(persistence: _persistence);

        Assert.Equal(NewGameStart.DayNumber, module.Queries.GetCurrentGameDate().DayNumber);
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
                NewGameStart.AddDays(4).DayNumber));

        var path = Path.Combine(_tempDirectory, "session.db");
        var saveResult = session.Save(path);

        Assert.True(saveResult.Succeeded);
        Assert.Equal(NewGameStart.AddDays(4).DayNumber, saveResult.SavedDayNumber);

        module.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(
                Guid.NewGuid(),
                NewGameStart.AddDays(9).DayNumber));

        var loadResult = session.Load(path);

        Assert.True(loadResult.Succeeded);
        Assert.Equal(NewGameStart.AddDays(4).DayNumber, loadResult.LoadedDayNumber);
        Assert.Equal(
            NewGameStart.AddDays(4).DayNumber,
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
                NewGameStart.AddDays(4).DayNumber));

        Assert.Equal(
            NewGameStart.AddDays(4).DayNumber,
            module.Queries.GetCurrentGameDate().DayNumber);

        session.Load(Path.Combine(_tempDirectory, "checkpoint.db"));

        var replay = module.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(
                advanceToDayFiveId,
                NewGameStart.AddDays(4).DayNumber));

        Assert.True(replay.Succeeded);
        Assert.Equal(
            NewGameStart.AddDays(4).DayNumber,
            module.Queries.GetCurrentGameDate().DayNumber);
    }
}
