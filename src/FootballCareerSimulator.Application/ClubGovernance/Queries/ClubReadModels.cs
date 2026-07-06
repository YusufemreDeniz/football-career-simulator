namespace FootballCareerSimulator.Application.ClubGovernance.Queries;

public sealed record ClubReadModel(
    long ClubId,
    string DisplayName,
    string Code,
    int SportiveStrength);
