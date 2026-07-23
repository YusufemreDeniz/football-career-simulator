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

public sealed record ShortlistLineReadModel(
    long EntryId,
    long PlayerId,
    long? NeedId,
    int Priority,
    string StatusName);

public sealed record TransferTargetLineReadModel(
    long TargetId,
    long NeedId,
    long PlayerId,
    long? ShortlistEntryId,
    string StatusName);

public sealed record ManagedClubShortlistTargetsReadModel(
    long? ClubId,
    int ActiveShortlistCount,
    int ListedTargetCount,
    IReadOnlyList<ShortlistLineReadModel> ActiveShortlist,
    IReadOnlyList<TransferTargetLineReadModel> ListedTargets);

public sealed record TransferProcessLineReadModel(
    long ProcessId,
    long TargetId,
    long PlayerId,
    string StatusName,
    string? FailureReasonCode);

public sealed record ManagedClubTransferProcessesReadModel(
    long? ClubId,
    int ActiveCount,
    IReadOnlyList<TransferProcessLineReadModel> ActiveProcesses);
