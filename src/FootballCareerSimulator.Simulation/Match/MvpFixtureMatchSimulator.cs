using FootballCareerSimulator.Domain.Match;

namespace FootballCareerSimulator.Simulation.Match;

/// <summary>
/// MVP deterministik maç: ilk yarı + ikinci yarı (devre arası müdahale deltası) + gol/kart anları.
/// </summary>
public static class MvpFixtureMatchSimulator
{
    public const int MinMomentMinute = 1;
    public const int MaxMomentMinute = 90;
    public const int HalfTimeMinute = 45;
    public const int MinGoalMinute = MinMomentMinute;
    public const int MaxGoalMinute = MaxMomentMinute;
    public const int StartingXiSize = 11;
    public const int MaxCardsPerMatch = 3;
    public const int MaxGoalsPerHalf = 4;

    public static MatchScore Simulate(
        int simulationSeed,
        long fixtureId,
        int homeStrength,
        int awayStrength,
        int homeLineupBonus = 0,
        int awayLineupBonus = 0,
        int homeSecondHalfDelta = 0,
        int awaySecondHalfDelta = 0,
        MatchScore? forcedHalfTime = null) =>
        SimulateWithKeyMoments(
            simulationSeed,
            fixtureId,
            homeStrength,
            awayStrength,
            homeLineupBonus,
            awayLineupBonus,
            homeSecondHalfDelta,
            awaySecondHalfDelta,
            forcedHalfTime).Score;

    public static MatchScore PreviewHalfTime(
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
            awayLineupBonus).HalfTimeScore;

    public static MatchSimulationOutcome SimulateWithKeyMoments(
        int simulationSeed,
        long fixtureId,
        int homeStrength,
        int awayStrength,
        int homeLineupBonus = 0,
        int awayLineupBonus = 0,
        int homeSecondHalfDelta = 0,
        int awaySecondHalfDelta = 0,
        MatchScore? forcedHalfTime = null)
    {
        var effectiveHome = Math.Clamp(homeStrength + homeLineupBonus, 1, 100);
        var effectiveAway = Math.Clamp(awayStrength + awayLineupBonus, 1, 100);

        var rng = new SimulationRandomContext(unchecked(simulationSeed * 397) ^ (int)fixtureId);
        // RNG sırası sabit kalsın; forced HT yalnızca skor/anları kilitler (devre arası değişiklik sonrası).
        var rolledHtHome = RollHalfGoals(rng, effectiveHome);
        var rolledHtAway = RollHalfGoals(rng, effectiveAway);
        var htHome = forcedHalfTime?.HomeGoals ?? rolledHtHome;
        var htAway = forcedHalfTime?.AwayGoals ?? rolledHtAway;

        var secondHome = Math.Clamp(effectiveHome + homeSecondHalfDelta, 1, 100);
        var secondAway = Math.Clamp(effectiveAway + awaySecondHalfDelta, 1, 100);
        var shHome = RollHalfGoals(rng, secondHome);
        var shAway = RollHalfGoals(rng, secondAway);

        var score = new MatchScore(htHome + shHome, htAway + shAway);
        var halfTime = new MatchScore(htHome, htAway);
        var moments = BuildKeyMoments(rng, htHome, htAway, shHome, shAway);
        var statistics = BuildStatistics(
            rng,
            (effectiveHome + secondHome) / 2,
            (effectiveAway + secondAway) / 2,
            score);
        return new MatchSimulationOutcome(score, moments, statistics, halfTime);
    }

    private static int RollHalfGoals(SimulationRandomContext rng, int effectiveStrength)
    {
        // Poisson approximation: λ = effectiveStrength / 30
        // Gerçekçi yarı gol dağılımı — strength 30 → λ≈1.0, strength 60 → λ≈2.0, strength 90 → λ≈3.0
        // Knuth algoritması integer versiyonu: 1000 ölçeğinde limit = e^(-λ) * 1000
        // Math.Exp yerine tablo kullanarak floating-point bağımlılığı yok.
        var lambdaTimes10 = Math.Clamp(effectiveStrength / 3, 1, 33); // λ*10 ∈ [0.1, 3.3]
        var limit = PoissonLimit(lambdaTimes10);                       // e^(-λ) * 10_000 (tamsayı)
        var product = 10_000;
        var k = 0;
        while (product > limit && k < MaxGoalsPerHalf + 2)
        {
            // uniform [0,1) → [1, 10_000] arasında tamsayı çekimi
            product = (product * rng.NextInt(1, 10_001)) / 10_000;
            k++;
        }

        return Math.Clamp(k - 1, 0, MaxGoalsPerHalf);
    }

    /// <summary>
    /// e^(-λ) * 10_000 değerini tamsayı olarak döner.
    /// λ = <paramref name="lambdaTimes10"/> / 10 (örn. 15 → λ=1.5).
    /// Tablo değerleri: Python'da round(math.exp(-x/10) * 10_000) ile üretildi.
    /// Aralık: λ*10 ∈ [1..33] → strength ∈ [3..99]
    /// </summary>
    private static int PoissonLimit(int lambdaTimes10) => lambdaTimes10 switch
    {
        1  => 9048,
        2  => 8187,
        3  => 7408,
        4  => 6703,
        5  => 6065,
        6  => 5488,
        7  => 4966,
        8  => 4493,
        9  => 4066,
        10 => 3679,
        11 => 3329,
        12 => 3012,
        13 => 2725,
        14 => 2466,
        15 => 2231,
        16 => 2019,
        17 => 1827,
        18 => 1653,
        19 => 1496,
        20 => 1353,
        21 => 1225,
        22 => 1108,
        23 => 1003,
        24 =>  907,
        25 =>  821,
        26 =>  743,
        27 =>  672,
        28 =>  608,
        29 =>  550,
        30 =>  498,
        31 =>  450,
        32 =>  407,
        33 =>  368,
        _  =>  368, // λ > 3.3 için alt sınır (güçlü takımlar zaten MaxGoalsPerHalf ile kırpılıyor)
    };

    private static MatchStatistics BuildStatistics(
        SimulationRandomContext rng,
        int effectiveHome,
        int effectiveAway,
        MatchScore score)
    {
        var homePossession = Math.Clamp(
            50 + ((effectiveHome - effectiveAway) / 3) + rng.NextInt(-5, 6),
            30,
            70);
        var awayPossession = 100 - homePossession;

        var homeShots = Math.Clamp(
            Math.Max(score.HomeGoals, 4 + (effectiveHome / 15) + rng.NextInt(-1, 4)),
            1,
            24);
        var awayShots = Math.Clamp(
            Math.Max(score.AwayGoals, 4 + (effectiveAway / 15) + rng.NextInt(-1, 4)),
            1,
            24);
        var homeShotsOnTarget = Math.Clamp(
            score.HomeGoals + rng.NextInt(0, 4),
            score.HomeGoals,
            homeShots);
        var awayShotsOnTarget = Math.Clamp(
            score.AwayGoals + rng.NextInt(0, 4),
            score.AwayGoals,
            awayShots);

        return new MatchStatistics(
            homePossession,
            awayPossession,
            homeShots,
            awayShots,
            homeShotsOnTarget,
            awayShotsOnTarget,
            Math.Clamp((homeShots / 3) + rng.NextInt(0, 4), 0, 12),
            Math.Clamp((awayShots / 3) + rng.NextInt(0, 4), 0, 12));
    }

    private static IReadOnlyList<MatchKeyMoment> BuildKeyMoments(
        SimulationRandomContext rng,
        int htHomeGoals,
        int htAwayGoals,
        int shHomeGoals,
        int shAwayGoals)
    {
        var usedMinutes = new HashSet<int>();
        var moments = new List<MatchKeyMoment>(
            htHomeGoals + htAwayGoals + shHomeGoals + shAwayGoals + MaxCardsPerMatch);

        for (var i = 0; i < htHomeGoals; i++)
        {
            moments.Add(BuildGoalMoment(rng, usedMinutes, isHomeSide: true, MinMomentMinute, HalfTimeMinute));
        }

        for (var i = 0; i < htAwayGoals; i++)
        {
            moments.Add(BuildGoalMoment(rng, usedMinutes, isHomeSide: false, MinMomentMinute, HalfTimeMinute));
        }

        for (var i = 0; i < shHomeGoals; i++)
        {
            moments.Add(BuildGoalMoment(rng, usedMinutes, isHomeSide: true, HalfTimeMinute + 1, MaxMomentMinute));
        }

        for (var i = 0; i < shAwayGoals; i++)
        {
            moments.Add(BuildGoalMoment(rng, usedMinutes, isHomeSide: false, HalfTimeMinute + 1, MaxMomentMinute));
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
        bool isHomeSide,
        int minMinute,
        int maxMinute)
    {
        var scorer = rng.NextInt(0, StartingXiSize);
        int? assist = null;
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
            NextDistinctMinute(rng, usedMinutes, minMinute, maxMinute),
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
            NextDistinctMinute(rng, usedMinutes, MinMomentMinute, MaxMomentMinute),
            IsHomeSide: rng.NextInt(0, 2) == 0,
            PrimarySlotIndex: rng.NextInt(0, StartingXiSize));
    }

    private static int NextDistinctMinute(
        SimulationRandomContext rng,
        HashSet<int> usedMinutes,
        int minMinute,
        int maxMinute)
    {
        for (var attempt = 0; attempt < 24; attempt++)
        {
            var minute = rng.NextInt(minMinute, maxMinute + 1);
            if (usedMinutes.Add(minute))
            {
                return minute;
            }
        }

        for (var minute = minMinute; minute <= maxMinute; minute++)
        {
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

        return maxMinute;
    }
}

public sealed record MatchSimulationOutcome(
    MatchScore Score,
    IReadOnlyList<MatchKeyMoment> KeyMoments,
    MatchStatistics Statistics,
    MatchScore HalfTimeScore);

public sealed record MatchStatistics(
    int HomePossessionPercent,
    int AwayPossessionPercent,
    int HomeShots,
    int AwayShots,
    int HomeShotsOnTarget,
    int AwayShotsOnTarget,
    int HomeCorners,
    int AwayCorners);
