namespace FootballCareerSimulator.Application.SocialContinuity.Queries;

public sealed record RelationshipLineReadModel(
    long RelationshipId,
    long ObserverPlayerId,
    long SubjectManagerId,
    int Trust,
    int Respect,
    int ProfessionalCompatibility,
    string TrustLabel,
    string RespectLabel,
    string CompatibilityLabel,
    string StatusName,
    string? LastChangeReasonCode,
    int LastChangedDayNumber);

public sealed record ManagerRelationshipsReadModel(
    long ManagerId,
    int ActiveCount,
    IReadOnlyList<RelationshipLineReadModel> RecentActive);
