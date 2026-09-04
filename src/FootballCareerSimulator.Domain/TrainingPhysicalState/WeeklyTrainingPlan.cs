using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.TrainingPhysicalState;

public enum TrainingFocus
{
    General = 1,
    Fitness = 2,
    Recovery = 3,
    Tactical = 4,
}

public enum TrainingIntensity
{
    Low = 1,
    Medium = 2,
    High = 3,
}

public enum RestApproach
{
    Light = 1,
    Normal = 2,
    Heavy = 3,
}

/// <summary>
/// Kulüp bazlı haftalık antrenman planı (MVP: odak + yoğunluk + dinlenme).
/// </summary>
public sealed class WeeklyTrainingPlan
{
    private WeeklyTrainingPlan(
        ClubId clubId,
        TrainingFocus focus,
        TrainingIntensity intensity,
        RestApproach restApproach,
        GameDate setAt)
    {
        ClubId = clubId;
        Focus = focus;
        Intensity = intensity;
        RestApproach = restApproach;
        SetAt = setAt;
    }

    public ClubId ClubId { get; }

    public TrainingFocus Focus { get; }

    public TrainingIntensity Intensity { get; }

    public RestApproach RestApproach { get; }

    public GameDate SetAt { get; }

    public static WeeklyTrainingPlan Set(
        ClubId clubId,
        TrainingFocus focus,
        TrainingIntensity intensity,
        RestApproach restApproach,
        GameDate setAt)
    {
        EnsureDefined(focus, intensity, restApproach);
        return new WeeklyTrainingPlan(clubId, focus, intensity, restApproach, setAt);
    }

    public static WeeklyTrainingPlan Rehydrate(
        ClubId clubId,
        TrainingFocus focus,
        TrainingIntensity intensity,
        RestApproach restApproach,
        GameDate setAt) =>
        Set(clubId, focus, intensity, restApproach, setAt);

    private static void EnsureDefined(
        TrainingFocus focus,
        TrainingIntensity intensity,
        RestApproach restApproach)
    {
        if (!Enum.IsDefined(focus))
        {
            throw new TrainingPhysicalStateInvariantViolationException(
                $"Unknown training focus: {focus}.");
        }

        if (!Enum.IsDefined(intensity))
        {
            throw new TrainingPhysicalStateInvariantViolationException(
                $"Unknown training intensity: {intensity}.");
        }

        if (!Enum.IsDefined(restApproach))
        {
            throw new TrainingPhysicalStateInvariantViolationException(
                $"Unknown rest approach: {restApproach}.");
        }
    }
}
