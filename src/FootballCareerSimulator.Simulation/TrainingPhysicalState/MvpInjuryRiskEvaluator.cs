using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation;

namespace FootballCareerSimulator.Simulation.TrainingPhysicalState;

/// <summary>
/// Deterministik sakatlık riski. Risk birikimli yorgunluk + maç yükünden gelir.
/// </summary>
public static class MvpInjuryRiskEvaluator
{
    public static int ComputeTrainingRiskPercent(
        PlayerPhysicalState state,
        TrainingIntensity intensity)
    {
        ArgumentNullException.ThrowIfNull(state);

        var baseRisk = intensity switch
        {
            TrainingIntensity.Low => 1,
            TrainingIntensity.Medium => 2,
            TrainingIntensity.High => 5,
            _ => 2,
        };

        var fatigueBonus = Math.Max(0, state.Fatigue - 50) / 4;
        var workloadBonus = state.MatchMinutesLast7Days switch
        {
            >= 270 => 6,
            >= 180 => 4,
            >= 90 => 2,
            _ => 0,
        };
        var returnBonus = string.Equals(
            state.LastInjuryReasonCode,
            PlayerPhysicalState.ReasonReturnFromInjury,
            StringComparison.Ordinal)
            ? 3
            : 0;

        return Math.Clamp(baseRisk + fatigueBonus + workloadBonus + returnBonus, 0, 28);
    }

    public static int ComputeDailyTrainingRiskPercent(
        PlayerPhysicalState state,
        TrainingIntensity intensity) =>
        Math.Max(
            0,
            (int)Math.Round(
                ComputeTrainingRiskPercent(state, intensity) / (double)MvpTrainingLoadApplier.TrainingLoadDaysPerWeek,
                MidpointRounding.AwayFromZero));

    public static int ComputeMatchRiskPercent(PlayerPhysicalState state, int minutesPlayed, GameDate day)
    {
        ArgumentNullException.ThrowIfNull(state);

        var minutesFactor = minutesPlayed switch
        {
            >= 80 => 2,
            >= 60 => 1,
            >= 30 => 0,
            > 0 => 0,
            _ => -1,
        };

        var workloadBonus = state.MatchMinutesLast7Days switch
        {
            >= 270 => 5,
            >= 180 => 3,
            >= 90 => 1,
            _ => 0,
        };
        var congestionBonus = state.HasCongestedFixture(day) ? 3 : 0;
        var returnBonus = string.Equals(
            state.LastInjuryReasonCode,
            PlayerPhysicalState.ReasonReturnFromInjury,
            StringComparison.Ordinal)
            ? 2
            : 0;

        return Math.Clamp(
            1 + Math.Max(0, state.Fatigue - 55) / 5 + minutesFactor + workloadBonus + congestionBonus + returnBonus,
            0,
            22);
    }

    public static bool ShouldInjure(int riskPercent, int roll0To99) =>
        roll0To99 >= 0 && roll0To99 < Math.Clamp(riskPercent, 0, 100);

    public static InjurySeverity ResolveSeverity(int roll0To99, int riskPercent)
    {
        if (riskPercent <= 0)
        {
            return InjurySeverity.None;
        }

        var band = Math.Max(1, riskPercent / 3);
        if (roll0To99 < band)
        {
            return InjurySeverity.Serious;
        }

        if (roll0To99 < band * 2)
        {
            return InjurySeverity.Moderate;
        }

        return InjurySeverity.Minor;
    }

    public static int DaysOut(InjurySeverity severity, int roll0To99 = 50) =>
        severity switch
        {
            InjurySeverity.Minor => 2 + (roll0To99 % 3),
            InjurySeverity.Moderate => 5 + (roll0To99 % 4),
            InjurySeverity.Serious => 9 + (roll0To99 % 5),
            _ => 0,
        };

    public static int MatchFatigueGain(int minutesPlayed) =>
        minutesPlayed switch
        {
            >= 80 => 14,
            >= 60 => 10,
            >= 30 => 6,
            >= 1 => 3,
            _ => 0,
        };

    public static int MatchFitnessLoss(int minutesPlayed) =>
        minutesPlayed switch
        {
            >= 80 => 3,
            >= 60 => 2,
            >= 30 => 1,
            _ => 0,
        };

    public static string ResolveTrainingInjuryReason(PlayerPhysicalState state, TrainingIntensity intensity) =>
        state.MatchMinutesLast7Days >= 180 || intensity == TrainingIntensity.High
            ? PlayerPhysicalState.ReasonAccumulatedWorkload
            : PlayerPhysicalState.ReasonTrainingLoad;

    public static string ResolveMatchInjuryReason(PlayerPhysicalState state, GameDate day) =>
        state.HasCongestedFixture(day) || state.MatchMinutesLast7Days >= 180
            ? PlayerPhysicalState.ReasonAccumulatedWorkload
            : string.Equals(
                state.LastInjuryReasonCode,
                PlayerPhysicalState.ReasonReturnFromInjury,
                StringComparison.Ordinal)
                ? PlayerPhysicalState.ReasonReturnFromInjury
                : PlayerPhysicalState.ReasonMatchLoad;

    public static PlayerPhysicalState MaybeInjureFromTraining(
        PlayerPhysicalState state,
        WeeklyTrainingPlan plan,
        int rootSeed,
        GameDate day,
        bool dailyTick = false)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(plan);

        state = state.RecoverIfDue(day);
        if (!state.IsAvailableOn(day))
        {
            return state;
        }

        var risk = dailyTick
            ? ComputeDailyTrainingRiskPercent(state, plan.Intensity)
            : ComputeTrainingRiskPercent(state, plan.Intensity);
        var salt = dailyTick ? 703 : 701;
        var roll = Roll(rootSeed, state.PlayerId.Value, day.DayNumber, salt);
        if (!ShouldInjure(risk, roll))
        {
            return state;
        }

        var severity = ResolveSeverity(roll, risk);
        return state.WithInjury(
            severity,
            day.AddDays(DaysOut(severity, roll)),
            ResolveTrainingInjuryReason(state, plan.Intensity));
    }

    public static PlayerPhysicalState MaybeInjureFromMatch(
        PlayerPhysicalState state,
        int rootSeed,
        long fixtureId,
        GameDate day,
        int minutesPlayed = 90,
        int riskBonusPercent = 0)
    {
        ArgumentNullException.ThrowIfNull(state);

        state = state.RecoverIfDue(day);
        if (!state.IsAvailableOn(day))
        {
            return state;
        }

        var afterMatch = state
            .WithLevels(
                Math.Clamp(
                    state.Fatigue + MatchFatigueGain(minutesPlayed),
                    PlayerPhysicalState.MinLevel,
                    PlayerPhysicalState.MaxLevel),
                Math.Clamp(
                    state.Fitness - MatchFitnessLoss(minutesPlayed),
                    PlayerPhysicalState.MinLevel,
                    PlayerPhysicalState.MaxLevel))
            .RecordMatchMinutes(day, minutesPlayed);

        if (minutesPlayed <= 0)
        {
            return afterMatch;
        }

        var risk = Math.Clamp(
            ComputeMatchRiskPercent(afterMatch, minutesPlayed, day) + riskBonusPercent,
            0,
            28);
        var roll = Roll(
            rootSeed,
            state.PlayerId.Value,
            day.DayNumber,
            salt: unchecked((int)fixtureId) ^ 909);
        if (!ShouldInjure(risk, roll))
        {
            return afterMatch;
        }

        var severity = ResolveSeverity(roll, risk);
        return afterMatch.WithInjury(
            severity,
            day.AddDays(DaysOut(severity, roll)),
            ResolveMatchInjuryReason(afterMatch, day));
    }

    private static int Roll(int rootSeed, long playerId, int dayNumber, int salt)
    {
        var rng = new SimulationRandomContext(
            unchecked(rootSeed * 911) ^ ((int)playerId * 47) ^ (dayNumber * 17) ^ salt);
        return rng.NextInt(0, 100);
    }
}
