using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation;

namespace FootballCareerSimulator.Simulation.TrainingPhysicalState;

/// <summary>
/// Deterministik sakatlık riski (antrenman / maç yükü).
/// </summary>
public static class MvpInjuryRiskEvaluator
{
    public static int ComputeTrainingRiskPercent(int fatigue, TrainingIntensity intensity)
    {
        var baseRisk = intensity switch
        {
            TrainingIntensity.Low => 2,
            TrainingIntensity.Medium => 8,
            TrainingIntensity.High => 24,
            _ => 8,
        };

        var fatigueBonus = Math.Max(0, fatigue - 45) / 2;
        return Math.Clamp(baseRisk + fatigueBonus, 0, 45);
    }

    public static int ComputeMatchRiskPercent(int fatigue) =>
        Math.Clamp(4 + Math.Max(0, fatigue - 40) / 3, 0, 30);

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

    public static int DaysOut(InjurySeverity severity) =>
        severity switch
        {
            InjurySeverity.Minor => 3,
            InjurySeverity.Moderate => 7,
            InjurySeverity.Serious => 14,
            _ => 0,
        };

    public static PlayerPhysicalState MaybeInjureFromTraining(
        PlayerPhysicalState state,
        WeeklyTrainingPlan plan,
        int rootSeed,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(plan);

        state = state.RecoverIfDue(day);
        if (!state.IsAvailableOn(day))
        {
            return state;
        }

        var risk = ComputeTrainingRiskPercent(state.Fatigue, plan.Intensity);
        var roll = Roll(rootSeed, state.ClubId.Value, state.SlotIndex, day.DayNumber, salt: 701);
        if (!ShouldInjure(risk, roll))
        {
            return state;
        }

        var severity = ResolveSeverity(roll, risk);
        return state.WithInjury(severity, day.AddDays(DaysOut(severity)));
    }

    public static PlayerPhysicalState MaybeInjureFromMatch(
        PlayerPhysicalState state,
        int rootSeed,
        long fixtureId,
        GameDate day,
        int riskBonusPercent = 0)
    {
        ArgumentNullException.ThrowIfNull(state);

        state = state.RecoverIfDue(day);
        if (!state.IsAvailableOn(day))
        {
            return state;
        }

        var afterMatch = state.WithLevels(
            Math.Clamp(state.Fatigue + 12, PlayerPhysicalState.MinLevel, PlayerPhysicalState.MaxLevel),
            Math.Clamp(state.Fitness - 2, PlayerPhysicalState.MinLevel, PlayerPhysicalState.MaxLevel));

        var risk = Math.Clamp(ComputeMatchRiskPercent(afterMatch.Fatigue) + riskBonusPercent, 0, 45);
        var roll = Roll(rootSeed, state.ClubId.Value, state.SlotIndex, day.DayNumber, salt: unchecked((int)fixtureId) ^ 909);
        if (!ShouldInjure(risk, roll))
        {
            return afterMatch;
        }

        var severity = ResolveSeverity(roll, risk);
        return afterMatch.WithInjury(severity, day.AddDays(DaysOut(severity)));
    }

    private static int Roll(int rootSeed, long clubId, int slotIndex, int dayNumber, int salt)
    {
        var rng = new SimulationRandomContext(
            unchecked(rootSeed * 911) ^ ((int)clubId * 47) ^ (slotIndex * 131) ^ (dayNumber * 17) ^ salt);
        return rng.NextInt(0, 100);
    }
}
