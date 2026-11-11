namespace FootballCareerSimulator.Application.SocialContinuity.Queries;

public sealed record MemoryLineReadModel(
    long MemoryId,
    string CategoryName,
    string ValenceName,
    string StatusName,
    string RememberingActorKind,
    long RememberingActorId,
    string SubjectKindName,
    long SubjectId,
    int BaseImportance,
    int CurrentInfluence,
    int CreatedOnDayNumber,
    string RuleId,
    long? RelatedPromiseId);

public sealed record ActorMemoriesReadModel(
    string ActorKind,
    long ActorId,
    int ActiveCount,
    IReadOnlyList<MemoryLineReadModel> RecentActive);

public sealed record MemoryCategoryCountReadModel(
    string CategoryName,
    int ActiveCount);
