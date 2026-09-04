namespace FootballCareerSimulator.Application.Interaction.Queries;

public sealed record DecisionRequestLineReadModel(
    long DecisionRequestId,
    string KindName,
    long SubjectPlayerId,
    long ClubId,
    string StatusName,
    bool IsHardBlocker,
    int OpenedDayNumber,
    int DeadlineDayNumber,
    string? SelectedOptionCode);

public sealed record PendingDecisionsReadModel(
    int OpenCount,
    IReadOnlyList<DecisionRequestLineReadModel> OpenRequests);
