namespace FootballCareerSimulator.Application.TrainingPhysicalState.Commands;

public sealed record SetWeeklyTrainingPlanCommand(
    Guid CommandId,
    int Focus,
    int Intensity,
    int RestApproach);

public sealed record SetWeeklyTrainingPlanResult(
    bool Succeeded,
    long ClubId,
    int Focus,
    int Intensity,
    int RestApproach,
    int AverageFatigue,
    int AverageFitness,
    int InjuredSlotCount,
    int InvalidatedSelectionCount = 0,
    bool PhysicalLoadApplied = true);
