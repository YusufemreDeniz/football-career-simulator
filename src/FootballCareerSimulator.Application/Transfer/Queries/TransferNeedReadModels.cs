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
    int StatusCode,
    string StatusName,
    string? FailureReasonCode,
    bool IsFreeAgent);

public sealed record ManagedClubTransferProcessesReadModel(
    long? ClubId,
    int ActiveCount,
    IReadOnlyList<TransferProcessLineReadModel> ActiveProcesses);

public sealed record ClubOfferLineReadModel(
    long OfferId,
    long ProcessId,
    int Round,
    int OfferedFee,
    string StatusName);

public sealed record ManagedClubOffersReadModel(
    long? ClubId,
    int PendingCount,
    IReadOnlyList<ClubOfferLineReadModel> RecentOffers);

public sealed record ContractProposalLineReadModel(
    long ProposalId,
    long ProcessId,
    int Round,
    int WeeklyWage,
    int ContractYears,
    string StatusName);

public sealed record ManagedClubContractProposalsReadModel(
    long? ClubId,
    int PendingCount,
    IReadOnlyList<ContractProposalLineReadModel> RecentProposals);
