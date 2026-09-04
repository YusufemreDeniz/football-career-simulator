using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation;

namespace FootballCareerSimulator.Simulation.TrainingPhysicalState;

/// <summary>
/// Deterministik sakatlık riski. Risk birikimli yorgunluk + yükten gelir; zar yalnız tetikleyicidir.
/// </summary>
public static class MvpInjuryRiskEvaluator
{
    public static int ComputeTrainingRiskPercent(int fatigue, TrainingIntensity intensity)
    {
        // Haftalık plan seçiminde değil günlük tick'te kullanılır; taban kasıtlı düşük tutulur.
        var baseRisk = intensity switch
        {
            TrainingIntensity.Low => 1,
            TrainingIntensity.Medium => 2,
            TrainingIntensity.High => 5,
            _ => 2,
        };

        var fatigueBonus = Math.Max(0, fatigue - 50) / 4;
        return Math.Clamp(baseRisk + fatigueBonus, 0, 18);
    }

    public static int ComputeDailyTrainingRiskPercent(int fatigue, TrainingIntensity intensity) =>
        Math.Max(0, (int)Math.Round(ComputeTrainingRiskPercent(fatigue, intensity) / 7.0, MidpointRounding.AwayFromZero));

    public static int ComputeMatchRiskPercent(int fatigue, int minutesPlayed)
    {
        var minutesFactor = minutesPlayed switch
        {
            >= 80 => 2,
            >= 60 => 1,
            >= 30 => 0,
            > 0 => 0,
            _ => -1,
        };

        return Math.Clamp(1 + Math.Max(0, fatigue - 55) / 5 + minutesFactor, 0, 14);
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
            ? ComputeDailyTrainingRiskPercent(state.Fatigue, plan.Intensity)
            : ComputeTrainingRiskPercent(state.Fatigue, plan.Intensity);
        var salt = dailyTick ? 703 : 701;
        var roll = Roll(rootSeed, state.ClubId.Value, state.SlotIndex, day.DayNumber, salt);
        if (!ShouldInjure(risk, roll))
        {
            return state;
        }

        var severity = ResolveSeverity(roll, risk);
        return state.WithInjury(severity, day.AddDays(DaysOut(severity, roll)));
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

        var afterMatch = state.WithLevels(
            Math.Clamp(
                state.Fatigue + MatchFatigueGain(minutesPlayed),
                PlayerPhysicalState.MinLevel,
                PlayerPhysicalState.MaxLevel),
            Math.Clamp(
                state.Fitness - MatchFitnessLoss(minutesPlayed),
                PlayerPhysicalState.MinLevel,
                PlayerPhysicalState.MaxLevel));

        if (minutesPlayed <= 0)
        {
            return afterMatch;
        }

        var risk = Math.Clamp(
            ComputeMatchRiskPercent(afterMatch.Fatigue, minutesPlayed) + riskBonusPercent,
            0,
            25);
        var roll = Roll(
            rootSeed,
            state.ClubId.Value,
            state.SlotIndex,
            day.DayNumber,
            salt: unchecked((int)fixtureId) ^ 909);
        if (!ShouldInjure(risk, roll))
        {
            return afterMatch;
        }

        var severity = ResolveSeverity(roll, risk);
        return afterMatch.WithInjury(severity, day.AddDays(DaysOut(severity, roll)));
    }

    private static int Roll(int rootSeed, long clubId, int slotIndex, int dayNumber, int salt)
    {
        var rng = new SimulationRandomContext(
            unchecked(rootSeed * 911) ^ ((int)clubId * 47) ^ (slotIndex * 131) ^ (dayNumber * 17) ^ salt);
        return rng.NextInt(0, 100);
    }
}
