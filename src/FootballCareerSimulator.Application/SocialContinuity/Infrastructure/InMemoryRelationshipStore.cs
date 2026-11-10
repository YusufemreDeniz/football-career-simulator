using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Infrastructure;

public sealed class InMemoryRelationshipStore : IRelationshipStore
{
    private readonly Dictionary<long, RelationshipRecord> _byId = new();
    private readonly Dictionary<PlayerManagerKey, long> _idByPlayerManager = new();
    private readonly Dictionary<long, HashSet<long>> _idsByPlayerObserver = new();
    private readonly Dictionary<long, HashSet<long>> _idsByManagerSubject = new();
    private IReadOnlyList<RelationshipRecord>? _orderedRelationships;
    private long _nextRelationshipId = 1;

    public IReadOnlyList<RelationshipRecord> Relationships =>
        _orderedRelationships ??= Array.AsReadOnly(
            _byId.Values.OrderBy(r => r.RelationshipId.Value).ToArray());

    public long NextRelationshipId => _nextRelationshipId;

    public RelationshipRecord? Get(RelationshipId relationshipId) =>
        _byId.GetValueOrDefault(relationshipId.Value);

    public RelationshipRecord? FindPlayerToManager(long playerId, long managerId) =>
        _idByPlayerManager.TryGetValue(new PlayerManagerKey(playerId, managerId), out var id)
        && _byId.TryGetValue(id, out var record)
            ? record
            : null;

    public IReadOnlyList<RelationshipRecord> GetByPlayerObserver(long playerId)
    {
        if (!_idsByPlayerObserver.TryGetValue(playerId, out var ids) || ids.Count == 0)
        {
            return Array.Empty<RelationshipRecord>();
        }

        return ids
            .OrderBy(id => id)
            .Select(id => _byId[id])
            .ToArray();
    }

    public IReadOnlyList<RelationshipRecord> GetByManagerSubject(long managerId)
    {
        if (!_idsByManagerSubject.TryGetValue(managerId, out var ids) || ids.Count == 0)
        {
            return Array.Empty<RelationshipRecord>();
        }

        return ids
            .OrderBy(id => id)
            .Select(id => _byId[id])
            .ToArray();
    }

    public void Upsert(RelationshipRecord relationship)
    {
        ArgumentNullException.ThrowIfNull(relationship);
        if (_byId.TryGetValue(relationship.RelationshipId.Value, out var previous))
        {
            RemoveIndexes(previous);
        }

        _byId[relationship.RelationshipId.Value] = relationship;
        AddIndexes(relationship);
        _nextRelationshipId = Math.Max(_nextRelationshipId, relationship.RelationshipId.Value + 1);
        _orderedRelationships = null;
    }

    public void ReplaceAll(IEnumerable<RelationshipRecord> relationships)
    {
        ArgumentNullException.ThrowIfNull(relationships);
        _byId.Clear();
        _idByPlayerManager.Clear();
        _idsByPlayerObserver.Clear();
        _idsByManagerSubject.Clear();
        _nextRelationshipId = 1;
        foreach (var relationship in relationships)
        {
            Upsert(relationship);
        }

        _orderedRelationships = null;
    }

    private void AddIndexes(RelationshipRecord relationship)
    {
        var id = relationship.RelationshipId.Value;
        if (relationship.Observer.Kind == ActorKind.Player)
        {
            if (!_idsByPlayerObserver.TryGetValue(relationship.Observer.Id, out var playerSet))
            {
                playerSet = new HashSet<long>();
                _idsByPlayerObserver[relationship.Observer.Id] = playerSet;
            }

            playerSet.Add(id);

            if (relationship.Subject.Kind == ActorKind.Manager)
            {
                _idByPlayerManager[new PlayerManagerKey(relationship.Observer.Id, relationship.Subject.Id)] = id;
            }
        }

        if (relationship.Subject.Kind == ActorKind.Manager)
        {
            if (!_idsByManagerSubject.TryGetValue(relationship.Subject.Id, out var mgrSet))
            {
                mgrSet = new HashSet<long>();
                _idsByManagerSubject[relationship.Subject.Id] = mgrSet;
            }

            mgrSet.Add(id);
        }
    }

    private void RemoveIndexes(RelationshipRecord relationship)
    {
        var id = relationship.RelationshipId.Value;
        if (relationship.Observer.Kind == ActorKind.Player)
        {
            if (_idsByPlayerObserver.TryGetValue(relationship.Observer.Id, out var playerSet))
            {
                playerSet.Remove(id);
                if (playerSet.Count == 0)
                {
                    _idsByPlayerObserver.Remove(relationship.Observer.Id);
                }
            }

            if (relationship.Subject.Kind == ActorKind.Manager)
            {
                var key = new PlayerManagerKey(relationship.Observer.Id, relationship.Subject.Id);
                if (_idByPlayerManager.TryGetValue(key, out var existingId) && existingId == id)
                {
                    _idByPlayerManager.Remove(key);
                }
            }
        }

        if (relationship.Subject.Kind == ActorKind.Manager
            && _idsByManagerSubject.TryGetValue(relationship.Subject.Id, out var mgrSet))
        {
            mgrSet.Remove(id);
            if (mgrSet.Count == 0)
            {
                _idsByManagerSubject.Remove(relationship.Subject.Id);
            }
        }
    }

    private readonly record struct PlayerManagerKey(long PlayerId, long ManagerId);
}
