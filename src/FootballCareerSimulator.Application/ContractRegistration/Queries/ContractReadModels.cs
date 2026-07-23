namespace FootballCareerSimulator.Application.ContractRegistration.Queries;

public sealed record ClubContractSummaryReadModel(
    long? ClubId,
    int ActiveCount,
    int ExpiredCount,
    int ExpiringWithinYearCount,
    int AverageWeeklyWage);
