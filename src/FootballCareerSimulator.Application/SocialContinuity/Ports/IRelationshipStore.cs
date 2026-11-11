using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Ports;

public interface IRelationshipStore
{
    IReadOnlyList<RelationshipRecord> Relationships { get; }

    long NextRelationshipId => Relationships.Count == 0 ? 1L : Relationships.Max(r => r.RelationshipId.Value) + 1;

    RelationshipRecord? Get(RelationshipId relationshipId);

    RelationshipRecord? FindPlayerToManager(long playerId, long managerId);

    IReadOnlyList<RelationshipRecord> GetByPlayerObserver(long playerId) =>
        Relationships
            .Where(r => r.Observer.Kind == ActorKind.Player && r.Observer.Id == playerId)
            .OrderBy(r => r.RelationshipId.Value)
            .ToArray();

    IReadOnlyList<RelationshipRecord> GetByManagerSubject(long managerId) =>
        Relationships
            .Where(r => r.Subject.Kind == ActorKind.Manager && r.Subject.Id == managerId)
            .OrderBy(r => r.RelationshipId.Value)
            .ToArray();

    void Upsert(RelationshipRecord relationship);

    void ReplaceAll(IEnumerable<RelationshipRecord> relationships);
}
