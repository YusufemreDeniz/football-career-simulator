namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

public sealed record SquadPlayerReadModel(
    int SquadNumber,
    string DisplayName,
    int SlotIndex,
    int Rating);
