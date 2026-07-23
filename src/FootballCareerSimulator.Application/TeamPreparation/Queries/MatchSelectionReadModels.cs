namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

public sealed record MatchSelectionReadModel(
    long FixtureId,
    long ClubId,
    string Status,
    IReadOnlyList<int> StartingSlotIndices,
    IReadOnlyList<int> BenchSlotIndices);

public sealed record ManagedFixtureSelectionStatusReadModel(
    long FixtureId,
    long SeasonId,
    long OpponentClubId,
    bool IsHome,
    int ScheduledDayNumber,
    string ScheduledIsoDate,
    bool IsApproved);
