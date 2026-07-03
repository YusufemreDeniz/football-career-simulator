using System.Diagnostics;
using FootballCareerSimulator.Domain.Spike1Placeholder;

namespace FootballCareerSimulator.Simulation.Spike1Placeholder;

/// <summary>
/// Spike 1/2'nin ana giriş noktasıdır: motor/UI olmadan (bkz. `tools/FootballCareerSimulator.SimulationRunner`)
/// yaklaşık 20 kulüp / 500 futbolculuk bir dünyayı N sezon ilerletir, her sezon sonunda invariant'ları
/// doğrular ve bir <see cref="SimulationRunReport"/> üretir. <see cref="CreateWorld"/> ve
/// <see cref="AdvanceSeasons"/>, Spike 2'nin ara-kesinti (checkpoint/resume) testlerinin de kullandığı
/// temel yapı taşlarıdır.
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

    public static void AdvanceSeasons(World world, SimulationRandomContext random, int seasonCount)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(random);

        if (seasonCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seasonCount), seasonCount, "Season count cannot be negative.");
        }

        for (var season = 0; season < seasonCount; season++)
        {
            SeasonAdvancer.AdvanceOneSeason(world, random);
            WorldInvariantChecker.Validate(world, WorldFactory.ClubCount, WorldFactory.TotalPlayerCount);
        }
    }

    public static SimulationRunReport Run(int seed, int seasonCount)
    {
        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);

        var stopwatch = Stopwatch.StartNew();

        var (world, random) = CreateWorld(seed);
        AdvanceSeasons(world, random, seasonCount);

        stopwatch.Stop();

        var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);

        return new SimulationRunReport(
            Seed: seed,
            RandomContextVersion: SimulationRandomContext.Version,
            SeasonCount: world.CurrentSeason,
            ClubCount: world.Clubs.Count,
            PlayerCount: world.Players.Count,
            CanonicalStateHash: CanonicalStateHasher.ComputeHash(world),
            ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
            MemoryBeforeBytes: memoryBefore,
            MemoryAfterBytes: memoryAfter);
    }
}
