using FootballCareerSimulator.Domain.Match;

namespace FootballCareerSimulator.Simulation.Match;

/// <summary>
/// MVP deterministik maç skoru üretici; güç farkına dayalı basit model.
/// </summary>
public static class MvpFixtureMatchSimulator
{
    public static MatchScore Simulate(int simulationSeed, long fixtureId, int homeStrength, int awayStrength)
    {
        var rng = new SimulationRandomContext(unchecked(simulationSeed * 397) ^ (int)fixtureId);
        var homeBase = Math.Clamp(homeStrength / 35, 0, 4);
        var awayBase = Math.Clamp(awayStrength / 35, 0, 4);
        var homeGoals = Math.Clamp(homeBase + rng.NextInt(-1, 3), 0, 6);
        var awayGoals = Math.Clamp(awayBase + rng.NextInt(-1, 3), 0, 6);
        return new MatchScore(homeGoals, awayGoals);
    }
}
