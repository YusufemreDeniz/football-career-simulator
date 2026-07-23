namespace FootballCareerSimulator.Application.PlayerCareer.Queries;

public sealed record ClubDevelopmentSummaryReadModel(
    long? ClubId,
    int PlayerCount,
    int AverageCurrentAbility,
    int AveragePotentialAbility,
    int DevelopedThisWeekCount,
    int AverageAge,
    int DecliningCount);
