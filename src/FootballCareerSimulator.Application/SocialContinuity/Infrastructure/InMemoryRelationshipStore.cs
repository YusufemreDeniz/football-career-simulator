using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Infrastructure;

public sealed class InMemoryRelationshipStore : IRelationshipStore
{
    private readonly Dictionary<long, RelationshipRecord> _byId = new();

    public IReadOnlyList<RelationshipRecord> Relationships =>
        _byId.Values.OrderBy(r => r.RelationshipId.Value).ToArray();

    public RelationshipRecord? Get(RelationshipId relationshipId) =>
        _byId.GetValueOrDefault(relationshipId.Value);

    public RelationshipRecord? FindPlayerToManager(long playerId, long managerId) =>
        _byId.Values.FirstOrDefault(r =>
            r.Observer.Kind == ActorKind.Player
            && r.Observer.Id == playerId
            && r.Subject.Kind == ActorKind.Manager
            && r.Subject.Id == managerId);

    public void Upsert(RelationshipRecord relationship)
    {
        ArgumentNullException.ThrowIfNull(relationship);
        _byId[relationship.RelationshipId.Value] = relationship;
    }

    public void ReplaceAll(IEnumerable<RelationshipRecord> relationships)
    {
        ArgumentNullException.ThrowIfNull(relationships);
        _byId.Clear();
        foreach (var relationship in relationships)
        {
            _byId[relationship.RelationshipId.Value] = relationship;
        }
    }
}
