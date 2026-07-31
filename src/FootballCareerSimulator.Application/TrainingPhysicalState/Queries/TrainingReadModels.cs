namespace FootballCareerSimulator.Application.TrainingPhysicalState.Queries;

public sealed record ClubTrainingSummaryReadModel(
    long? ClubId,
    int? Focus,
    int? Intensity,
    int? RestApproach,
    string? FocusName,
    string? IntensityName,
    string? RestApproachName,
    int? SetAtDayNumber,
    int? AverageFatigue,
    int? AverageFitness,
    bool HasPlan,
    int InjuredSlotCount,
    int UnavailableSlotCount,
    IReadOnlyList<string>? InjuredPlayerNames = null)
{
    public IReadOnlyList<string> InjuredNames =>
        InjuredPlayerNames ?? Array.Empty<string>();
}
