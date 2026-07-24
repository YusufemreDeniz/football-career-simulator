using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Ports;

public interface IRelationshipStore
{
    IReadOnlyList<RelationshipRecord> Relationships { get; }

    RelationshipRecord? Get(RelationshipId relationshipId);

    RelationshipRecord? FindPlayerToManager(long playerId, long managerId);

    void Upsert(RelationshipRecord relationship);

    void ReplaceAll(IEnumerable<RelationshipRecord> relationships);
}
