namespace FootballCareerSimulator.Application.TeamPreparation.Commands;

public sealed record ApproveDefaultMatchSelectionCommand(
    Guid CommandId,
    long FixtureId,
    long ClubId);

public sealed record ApproveDefaultMatchSelectionResult(
    bool Succeeded,
    long FixtureId,
    long ClubId,
    string Status);
