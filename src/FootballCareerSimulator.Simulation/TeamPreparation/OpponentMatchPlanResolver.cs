namespace FootballCareerSimulator.Simulation.TeamPreparation;

public enum OpponentMatchPriority
{
    Rotation = 1,
    Standard = 2,
    High = 3,
    MustWin = 4,
}

public enum OpponentTacticalIntent
{
    Balanced = 1,
    CompactCounter = 2,
    AggressivePress = 3,
}

public sealed record OpponentMatchPlanInput(
    long ClubId,
    long FixtureId,
    int Round,
    int LeagueSize,
    int LeaguePosition,
    int ClubStrength,
    int OpponentStrength,
    int DaysSincePreviousMatch,
    int RootSeed,
    IReadOnlyList<int> AvailableSlots);

public sealed record OpponentMatchPlan(
    OpponentMatchPriority Priority,
    OpponentTacticalIntent Intent,
    IReadOnlyList<int> StartingSlots,
    int RotationCount,
    int MatchStrengthModifier,
    string Headline);

/// <summary>
/// Deterministic AI selection policy. It rotates under congestion, protects
/// high-stakes late-season fixtures and adapts intent to the strength gap.
/// </summary>
public static class OpponentMatchPlanResolver
{
    private const int StartingXiSize = 11;

    public static OpponentMatchPlan Resolve(OpponentMatchPlanInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.AvailableSlots);
        if (input.LeagueSize < 2 || input.Round < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(input));
        }

        var lateSeason = input.Round >= Math.Max(1, (input.LeagueSize - 1) * 3 / 2);
        var titleRace = input.LeaguePosition <= 3;
        var relegationRisk = input.LeaguePosition >= Math.Max(1, input.LeagueSize - 2);
        var congested = input.DaysSincePreviousMatch is >= 0 and <= 4;
        var priority = lateSeason && (titleRace || relegationRisk)
            ? OpponentMatchPriority.MustWin
            : titleRace || relegationRisk
                ? OpponentMatchPriority.High
                : congested
                    ? OpponentMatchPriority.Rotation
                    : OpponentMatchPriority.Standard;

        var rotationCount = priority switch
        {
            OpponentMatchPriority.Rotation => 3,
            OpponentMatchPriority.Standard when congested => 2,
            OpponentMatchPriority.High when congested => 1,
            _ => 0,
        };
        var intent = (input.OpponentStrength - input.ClubStrength) switch
        {
            >= 7 => OpponentTacticalIntent.CompactCounter,
            <= -7 => OpponentTacticalIntent.AggressivePress,
            _ => OpponentTacticalIntent.Balanced,
        };
        var modifier = Math.Clamp(
            (priority == OpponentMatchPriority.MustWin ? 1 : 0)
            + (intent == OpponentTacticalIntent.Balanced ? 0 : 1)
            - (rotationCount >= 3 ? 1 : 0),
            -1,
            2);
        var starters = BuildStartingSlots(input, rotationCount);
        return new OpponentMatchPlan(
            priority,
            intent,
            starters,
            rotationCount,
            modifier,
            BuildHeadline(priority, intent, rotationCount));
    }

    private static IReadOnlyList<int> BuildStartingSlots(OpponentMatchPlanInput input, int rotationCount)
    {
        var available = input.AvailableSlots.Distinct().OrderBy(slot => slot).ToArray();
        var starters = available.Where(slot => slot is >= 0 and < StartingXiSize).Take(StartingXiSize).ToList();
        foreach (var slot in available.Where(slot => slot >= StartingXiSize))
        {
            if (starters.Count >= StartingXiSize)
            {
                break;
            }

            starters.Add(slot);
        }

        if (starters.Count < StartingXiSize)
        {
            return starters;
        }

        var reserves = available.Where(slot => !starters.Contains(slot)).ToArray();
        var count = Math.Min(rotationCount, reserves.Length);
        var reserveOffset = PositiveModulo(
            input.RootSeed + (int)input.FixtureId + ((int)input.ClubId * 17),
            reserves.Length);
        for (var index = 0; index < count; index++)
        {
            var reserveIndex = (reserveOffset + index) % reserves.Length;
            var replaceIndex = StartingXiSize - 1 - index;
            starters[replaceIndex] = reserves[reserveIndex];
        }

        return starters.Distinct().Take(StartingXiSize).ToArray();
    }

    private static int PositiveModulo(int value, int divisor) =>
        ((value % divisor) + divisor) % divisor;

    private static string BuildHeadline(
        OpponentMatchPriority priority,
        OpponentTacticalIntent intent,
        int rotationCount)
    {
        var priorityText = priority switch
        {
            OpponentMatchPriority.Rotation => "fikstür rotasyonu",
            OpponentMatchPriority.High => "yüksek öncelik",
            OpponentMatchPriority.MustWin => "kazanılması gereken maç",
            _ => "standart maç planı",
        };
        var intentText = intent switch
        {
            OpponentTacticalIntent.CompactCounter => "kompakt geçiş",
            OpponentTacticalIntent.AggressivePress => "agresif pres",
            _ => "dengeli oyun",
        };
        return $"{priorityText} · {intentText} · {rotationCount} değişiklik";
    }
}
