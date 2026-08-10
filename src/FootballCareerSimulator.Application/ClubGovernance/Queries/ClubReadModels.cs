namespace FootballCareerSimulator.Application.ClubGovernance.Queries;

public sealed record ClubReadModel(
    long ClubId,
    string DisplayName,
    string Code,
    int SportiveStrength,
    int TransferBudgetLimit,
    int ReservedTransferFunds,
    int SpentTransferFunds,
    int AvailableTransferFunds,
    int WageBudgetLimit,
    int ReservedWeeklyWage,
    string CrestResourcePath,
    string HomeKitResourcePath,
    string AwayKitResourcePath,
    string ThirdKitResourcePath,
    string DataSnapshotDate);
