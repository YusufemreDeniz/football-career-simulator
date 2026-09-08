namespace FootballCareerSimulator.Application.ManagerCareer.Queries;

/// <summary>
/// Menajerin davranış profili okuma modeli (docs/20 §7).
/// </summary>
public sealed record ManagerBehavioralProfileReadModel(
    int YouthOpportunity,
    int PromiseReliability,
    int PublicSupport,
    int NegotiationPersistence,
    int RotationConsistency,
    int DeadlineAggression,
    IReadOnlyList<string> StyleBadges,
    int TotalObservationsCount);
