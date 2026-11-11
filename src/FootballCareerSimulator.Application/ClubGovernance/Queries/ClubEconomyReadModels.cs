namespace FootballCareerSimulator.Application.ClubGovernance.Queries;

public enum BoardObjectiveStatus
{
    NotStarted = 1,
    OnTrack = 2,
    AtRisk = 3,
    OffTrack = 4,
    Achieved = 5,
    Failed = 6,
}

public sealed record BoardObjectiveReadModel(
    string Code,
    string Title,
    string Target,
    string Current,
    int ProgressPercent,
    BoardObjectiveStatus Status);

public sealed record ClubEconomyReadModel(
    long ClubId,
    string ClubName,
    long? SeasonId,
    int? LeaguePosition,
    int LeagueSize,
    int PlayedMatches,
    int? BoardConfidence,
    string SeasonExpectation,
    string CurrencyCode,
    int TransferBudgetLimit,
    int ReservedTransferFunds,
    int SpentTransferFunds,
    int AvailableTransferFunds,
    int WeeklyWageLimit,
    int CommittedWeeklyWage,
    int ReservedWeeklyWage,
    int WeeklyWageHeadroom,
    int WageUtilizationPercent,
    int StadiumCapacity,
    int AttendancePercent,
    int ProjectedAverageAttendance,
    int AverageTicketPrice,
    long ProjectedMatchdayRevenue,
    long ProjectedSponsorRevenue,
    long ProjectedAnnualWageSpend,
    long ProjectedFootballOperationsCost,
    long ProjectedOperatingCosts,
    long ProjectedOperatingRevenue,
    long ProjectedOperatingBalance,
    IReadOnlyList<BoardObjectiveReadModel> BoardObjectives);
