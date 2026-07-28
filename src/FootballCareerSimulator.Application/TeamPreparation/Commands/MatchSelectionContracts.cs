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

public sealed record ApproveMatchSelectionCommand(
    Guid CommandId,
    long FixtureId,
    long ClubId,
    IReadOnlyList<int> StartingSlotIndices,
    IReadOnlyList<int> BenchSlotIndices);

public sealed record ApproveMatchSelectionResult(
    bool Succeeded,
    long FixtureId,
    long ClubId,
    string Status,
    IReadOnlyList<int> StartingSlotIndices,
    IReadOnlyList<int> BenchSlotIndices);

public sealed record SwapStarterWithBenchCommand(
    Guid CommandId,
    long FixtureId,
    long ClubId,
    int StartingIndex = 10,
    int BenchIndex = 0);

public sealed record SwapStarterWithBenchResult(
    bool Succeeded,
    long FixtureId,
    long ClubId,
    IReadOnlyList<int> StartingSlotIndices,
    IReadOnlyList<int> BenchSlotIndices);
