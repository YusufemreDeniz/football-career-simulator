namespace FootballCareerSimulator.Application.ContractRegistration.Queries;

public sealed record ClubContractSummaryReadModel(
    long? ClubId,
    int ActiveCount,
    int ExpiredCount,
    int ExpiringWithinYearCount,
    int AverageWeeklyWage,
    int FreeAgentReleasedCount);

public sealed record FreeAgencyExpiryResult(
    int ExpiredCount,
    IReadOnlyList<long> AffectedClubIds,
    IReadOnlyList<long> FreeAgentPlayerIds);
