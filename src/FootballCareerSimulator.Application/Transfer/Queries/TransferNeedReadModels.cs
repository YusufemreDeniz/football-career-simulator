namespace FootballCareerSimulator.Application.Transfer.Queries;

public sealed record TransferNeedLineReadModel(
    long NeedId,
    string KindName,
    string StatusName,
    int Priority,
    string ReasonCode,
    int IdentifiedDayNumber);

public sealed record ManagedClubTransferNeedsReadModel(
    long? ClubId,
    int OpenCount,
    IReadOnlyList<TransferNeedLineReadModel> OpenNeeds);
