namespace FootballCareerSimulator.Application.ManagerCareer.Queries;

public sealed record ManagerCareerReadModel(
    long ManagerId,
    string DisplayName,
    long? EmployedClubId,
    int? EmploymentStartedDayNumber,
    string? SeasonExpectation,
    int? BoardConfidence,
    string? EmploymentRiskBand,
    string? LastAssessmentReasonCode,
    long? LastAssessedFixtureId);
