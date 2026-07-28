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

public sealed record ClubPlayerReleaseResult(
    long PlayerId,
    long ClubId,
    bool WasOverflow,
    int RemainingActiveContracts);

public sealed record FreeAgentResignResult(
    long PlayerId,
    long ClubId,
    int WeeklyWage,
    int EndDayNumber);

public sealed record TransferContractActivationResult(
    long PlayerId,
    long ClubId,
    int WeeklyWage,
    int EndDayNumber,
    bool WasFreeAgent);

public sealed record SignableFreeAgentReadModel(
    long PlayerId,
    long LastClubId,
    int BecameFreeAgentDayNumber);
