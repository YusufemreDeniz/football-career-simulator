using FootballCareerSimulator.Domain.Match;

namespace FootballCareerSimulator.Simulation.Match;

/// <summary>
/// MVP deterministik maç skoru + gol anları; güç farkına ve isteğe bağlı kadro bonusuna dayalı.
/// </summary>
public static class MvpFixtureMatchSimulator
{
    public const int MinGoalMinute = 1;
    public const int MaxGoalMinute = 90;
    public const int StartingXiSize = 11;

    public static MatchScore Simulate(
        int simulationSeed,
        long fixtureId,
        int homeStrength,
        int awayStrength,
        int homeLineupBonus = 0,
        int awayLineupBonus = 0) =>
        SimulateWithKeyMoments(
            simulationSeed,
            fixtureId,
            homeStrength,
            awayStrength,
            homeLineupBonus,
            awayLineupBonus).Score;

    public static MatchSimulationOutcome SimulateWithKeyMoments(
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
        var score = new MatchScore(homeGoals, awayGoals);
        var moments = BuildKeyMoments(rng, homeGoals, awayGoals);
        return new MatchSimulationOutcome(score, moments);
    }

    private static IReadOnlyList<MatchKeyMoment> BuildKeyMoments(
        SimulationRandomContext rng,
        int homeGoals,
        int awayGoals)
    {
        var usedMinutes = new HashSet<int>();
        var moments = new List<MatchKeyMoment>(homeGoals + awayGoals);

        for (var i = 0; i < homeGoals; i++)
        {
            moments.Add(new MatchKeyMoment(
                NextDistinctMinute(rng, usedMinutes),
                IsHomeGoal: true,
                ScorerSlotIndex: rng.NextInt(0, StartingXiSize)));
        }

        for (var i = 0; i < awayGoals; i++)
        {
            moments.Add(new MatchKeyMoment(
                NextDistinctMinute(rng, usedMinutes),
                IsHomeGoal: false,
                ScorerSlotIndex: rng.NextInt(0, StartingXiSize)));
        }

        return moments
            .OrderBy(moment => moment.Minute)
            .ThenBy(moment => moment.IsHomeGoal ? 0 : 1)
            .ThenBy(moment => moment.ScorerSlotIndex)
            .ToArray();
    }

    private static int NextDistinctMinute(SimulationRandomContext rng, HashSet<int> usedMinutes)
    {
        for (var attempt = 0; attempt < 24; attempt++)
        {
            var minute = rng.NextInt(MinGoalMinute, MaxGoalMinute + 1);
            if (usedMinutes.Add(minute))
            {
                return minute;
            }
        }

        for (var minute = MinGoalMinute; minute <= MaxGoalMinute; minute++)
        {
            if (usedMinutes.Add(minute))
            {
                return minute;
            }
        }

        return MaxGoalMinute;
    }
}

public sealed record MatchSimulationOutcome(
    MatchScore Score,
    IReadOnlyList<MatchKeyMoment> KeyMoments);
