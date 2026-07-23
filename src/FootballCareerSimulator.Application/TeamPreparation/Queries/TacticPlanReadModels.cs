namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

public sealed record TacticPlanReadModel(
    long? ClubId,
    string FormationName,
    string ApproachName,
    int LastUpdatedDayNumber);
