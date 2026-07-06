using FootballCareerSimulator.Application.WorldCalendar.Composition;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Godot Presentation host composition root (D-348).
/// </summary>
public sealed class WorldCalendarPresentationHost
{
    public WorldCalendarPresentationHost(WorldCalendarModule module)
    {
        Module = module ?? throw new ArgumentNullException(nameof(module));
    }

    public WorldCalendarModule Module { get; }

    public static WorldCalendarPresentationHost CreateDefault() =>
        new(WorldCalendarModule.CreateNewGame());
}
