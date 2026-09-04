using FootballCareerSimulator.Domain.Spike1Placeholder;

namespace FootballCareerSimulator.Simulation.Spike1Placeholder;

/// <summary>
/// Spike 4 prototype için 500 futbolculuk test dünyası üretir.
/// </summary>
public static class HeadlessSimulationRunner
{
    public static (World World, SimulationRandomContext Random) CreateWorld(int seed)
    {
        var random = new SimulationRandomContext(seed);
        var world = WorldFactory.CreatePlaceholderWorld(random);

        WorldInvariantChecker.Validate(world, WorldFactory.ClubCount, WorldFactory.TotalPlayerCount);

        return (world, random);
    }
}
