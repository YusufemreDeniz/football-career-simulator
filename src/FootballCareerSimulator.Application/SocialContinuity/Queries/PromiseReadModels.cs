namespace FootballCareerSimulator.Application.SocialContinuity.Queries;

public sealed record PromiseLineReadModel(
    long PromiseId,
    string KindName,
    string StatusName,
    string PromisorKind,
    long PromisorId,
    string PromiseeKind,
    long PromiseeId,
    long ClubId,
    int ProgressCount,
    int TargetCount,
    int DeadlineDayNumber,
    int CreatedOnDayNumber,
    int? TerminalOnDayNumber);

public sealed record ActorPromisesReadModel(
    string ActorRole,
    string ActorKind,
    long ActorId,
    int ActiveCount,
    IReadOnlyList<PromiseLineReadModel> RecentActive);

public sealed record ClubPromisesReadModel(
    long ClubId,
    int ActiveCount,
    IReadOnlyList<PromiseLineReadModel> RecentActive);
