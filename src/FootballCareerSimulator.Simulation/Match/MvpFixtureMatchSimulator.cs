using FootballCareerSimulator.Domain.Match;

namespace FootballCareerSimulator.Simulation.Match;

/// <summary>
/// MVP deterministik maç skoru üretici; güç farkına ve isteğe bağlı kadro bonusuna dayalı.
/// </summary>
public static class MvpFixtureMatchSimulator
{
    public static MatchScore Simulate(
        int simulationSeed,
        long fixtureId,
        int homeStrength,
        int awayStrength,
        int homeLineupBonus = 0,
        int awayLineupBonus = 0)
    {
        var effectiveHome = Math.Clamp(homeStrength + homeLineupBonus, 1, 100);
        var effectiveAway = Math.Clamp(awayStrength + awayLineupBonus, 1, 100);

        var rng = new SimulationRandomContext(unchecked(simulationSeed * 397) ^ (int)fixtureId);
        var homeBase = Math.Clamp(effectiveHome / 35, 0, 4);
        var awayBase = Math.Clamp(effectiveAway / 35, 0, 4);
        var homeGoals = Math.Clamp(homeBase + rng.NextInt(-1, 3), 0, 6);
        var awayGoals = Math.Clamp(awayBase + rng.NextInt(-1, 3), 0, 6);
        return new MatchScore(homeGoals, awayGoals);
    }
}
