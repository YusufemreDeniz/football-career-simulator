using FootballCareerSimulator.Domain.Match;

namespace FootballCareerSimulator.Simulation.Match;

/// <summary>
/// MVP deterministik maç skoru + gol/asist/kart anları; güç farkına ve isteğe bağlı kadro bonusuna dayalı.
/// </summary>
public static class MvpFixtureMatchSimulator
{
    public const int MinMomentMinute = 1;
    public const int MaxMomentMinute = 90;
    public const int MinGoalMinute = MinMomentMinute;
    public const int MaxGoalMinute = MaxMomentMinute;
    public const int StartingXiSize = 11;
    public const int MaxCardsPerMatch = 3;

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
        var moments = new List<MatchKeyMoment>(homeGoals + awayGoals + MaxCardsPerMatch);

        for (var i = 0; i < homeGoals; i++)
        {
            moments.Add(BuildGoalMoment(rng, usedMinutes, isHomeSide: true));
        }

        for (var i = 0; i < awayGoals; i++)
        {
            moments.Add(BuildGoalMoment(rng, usedMinutes, isHomeSide: false));
        }

        var cardCount = rng.NextInt(0, MaxCardsPerMatch + 1);
        for (var i = 0; i < cardCount; i++)
        {
            moments.Add(BuildCardMoment(rng, usedMinutes));
        }

        return moments
            .OrderBy(moment => moment.Minute)
            .ThenBy(moment => (int)moment.Kind)
            .ThenBy(moment => moment.IsHomeSide ? 0 : 1)
            .ThenBy(moment => moment.PrimarySlotIndex)
            .ThenBy(moment => moment.AssistSlotIndex ?? -1)
            .ToArray();
    }

    private static MatchKeyMoment BuildGoalMoment(
        SimulationRandomContext rng,
        HashSet<int> usedMinutes,
        bool isHomeSide)
    {
        var scorer = rng.NextInt(0, StartingXiSize);
        int? assist = null;
        // ~2/3 golde asist; asistçi golcüden farklı slot.
        if (rng.NextInt(0, 3) != 0)
        {
            var assistSlot = rng.NextInt(0, StartingXiSize - 1);
            if (assistSlot >= scorer)
            {
                assistSlot++;
            }

            assist = assistSlot;
        }

        return new MatchKeyMoment(
            MatchKeyMomentKind.Goal,
            NextDistinctMinute(rng, usedMinutes),
            isHomeSide,
            scorer,
            assist);
    }

    private static MatchKeyMoment BuildCardMoment(
        SimulationRandomContext rng,
        HashSet<int> usedMinutes)
    {
        var kind = rng.NextInt(0, 5) == 0
            ? MatchKeyMomentKind.RedCard
            : MatchKeyMomentKind.YellowCard;
        return new MatchKeyMoment(
            kind,
            NextDistinctMinute(rng, usedMinutes),
            IsHomeSide: rng.NextInt(0, 2) == 0,
            PrimarySlotIndex: rng.NextInt(0, StartingXiSize));
    }

    private static int NextDistinctMinute(SimulationRandomContext rng, HashSet<int> usedMinutes)
    {
        for (var attempt = 0; attempt < 24; attempt++)
        {
            var minute = rng.NextInt(MinMomentMinute, MaxMomentMinute + 1);
            if (usedMinutes.Add(minute))
            {
                return minute;
            }
        }

        for (var minute = MinMomentMinute; minute <= MaxMomentMinute; minute++)
        {
            if (usedMinutes.Add(minute))
            {
                return minute;
            }
        }

        return MaxMomentMinute;
    }
}

public sealed record MatchSimulationOutcome(
    MatchScore Score,
    IReadOnlyList<MatchKeyMoment> KeyMoments);
