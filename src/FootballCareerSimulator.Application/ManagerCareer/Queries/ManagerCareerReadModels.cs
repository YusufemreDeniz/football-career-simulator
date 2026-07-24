namespace FootballCareerSimulator.Application.ManagerCareer.Queries;

public sealed record ManagerCareerReadModel(
    long ManagerId,
    string DisplayName,
    string EmploymentStatus,
    long? EmployedClubId,
    int? EmploymentStartedDayNumber,
    string? SeasonExpectation,
    int? BoardConfidence,
    string? EmploymentRiskBand,
    string? LastAssessmentReasonCode,
    long? LastAssessedFixtureId,
    string? EmploymentEndReason,
    long? LastClubId,
    long? DismissedDueToFixtureId,
    int? DismissedAtDayNumber,
    long? PendingOfferId,
    long? PendingOfferClubId,
    string? PendingOfferStatus,
    int ManagerReputation,
    string? LastReputationReasonCode);
