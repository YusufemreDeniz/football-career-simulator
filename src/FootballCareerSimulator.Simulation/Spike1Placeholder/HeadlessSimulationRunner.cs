using System.Diagnostics;

namespace FootballCareerSimulator.Simulation.Spike1Placeholder;

/// <summary>
/// Spike 1'in ana giriş noktasıdır: motor/UI olmadan (bkz. `tools/FootballCareerSimulator.SimulationRunner`)
/// yaklaşık 20 kulüp / 500 futbolculuk bir dünyayı N sezon ilerletir, her sezon sonunda invariant'ları
/// doğrular ve bir <see cref="SimulationRunReport"/> üretir.
/// </summary>
public static class HeadlessSimulationRunner
{
    public static SimulationRunReport Run(int seed, int seasonCount)
    {
        if (seasonCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seasonCount), seasonCount, "Season count cannot be negative.");
        }

        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);

        var random = new SimulationRandomContext(seed);
        var world = WorldFactory.CreatePlaceholderWorld(random);

        WorldInvariantChecker.Validate(world, WorldFactory.ClubCount, WorldFactory.TotalPlayerCount);

        var stopwatch = Stopwatch.StartNew();

        for (var season = 0; season < seasonCount; season++)
        {
            SeasonAdvancer.AdvanceOneSeason(world);
            WorldInvariantChecker.Validate(world, WorldFactory.ClubCount, WorldFactory.TotalPlayerCount);
        }

        stopwatch.Stop();

        var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);

        return new SimulationRunReport(
            Seed: seed,
            SeasonCount: world.CurrentSeason,
            ClubCount: world.Clubs.Count,
            PlayerCount: world.Players.Count,
            ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
            MemoryBeforeBytes: memoryBefore,
            MemoryAfterBytes: memoryAfter);
    }
}
