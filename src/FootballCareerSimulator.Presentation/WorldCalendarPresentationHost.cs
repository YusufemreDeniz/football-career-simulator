using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Services;
using FootballCareerSimulator.Infrastructure.WorldCalendar;
using Godot;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Godot Presentation host composition root (D-348).
/// </summary>
public sealed class WorldCalendarPresentationHost
{
    public WorldCalendarPresentationHost(
        WorldCalendarModule module,
        WorldCalendarGameSessionService gameSession,
        string defaultSavePath)
    {
        Module = module ?? throw new ArgumentNullException(nameof(module));
        GameSession = gameSession ?? throw new ArgumentNullException(nameof(gameSession));
        DefaultSavePath = defaultSavePath ?? throw new ArgumentNullException(nameof(defaultSavePath));
    }

    public WorldCalendarModule Module { get; }

    public WorldCalendarGameSessionService GameSession { get; }

    public string DefaultSavePath { get; }

    public static WorldCalendarPresentationHost CreateDefault(string? defaultSavePath = null)
    {
        var persistence = new WorldCalendarSqlitePersistence();
        var module = WorldCalendarModule.CreateNewGame(persistence: persistence);

        if (module.GameSession is null)
        {
            throw new InvalidOperationException("Game session service was not wired into the module.");
        }

        var savePath = defaultSavePath ?? Path.Combine(OS.GetUserDataDir(), "career_save.db");
        return new WorldCalendarPresentationHost(module, module.GameSession, savePath);
    }
}
