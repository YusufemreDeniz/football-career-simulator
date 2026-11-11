namespace FootballCareerSimulator.Domain.ManagerCareer;

/// <summary>
/// Menajerin gerçekleştirdiği tekil davranış gözlem kaydı (docs/20 §7).
/// </summary>
public sealed record BehavioralObservation(
    string ObservationCode,
    BehavioralEvidenceCategory Category,
    int ScoreDelta,
    int OccurredAtDayNumber,
    string DescriptionKey);
