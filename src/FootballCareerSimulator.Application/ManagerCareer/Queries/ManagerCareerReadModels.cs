namespace FootballCareerSimulator.Application.ManagerCareer.Queries;

public sealed record ManagerCareerReadModel(
    long ManagerId,
    string DisplayName,
    long? EmployedClubId,
    int? EmploymentStartedDayNumber);
