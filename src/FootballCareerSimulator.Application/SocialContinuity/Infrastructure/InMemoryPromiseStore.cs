using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Infrastructure;

public sealed class InMemoryPromiseStore : IPromiseStore
{
    private readonly Dictionary<long, Promise> _byId = new();
    private readonly HashSet<long> _activeIds = new();
    private readonly Dictionary<PlayerClubKindKey, long> _activeByPlayerClubKind = new();
    private readonly Dictionary<long, HashSet<long>> _activeByPlayer = new();
    private readonly Dictionary<ManagerClubKey, HashSet<long>> _activeByManagerClub = new();
    private IReadOnlyList<Promise>? _orderedPromises;
    private IReadOnlyList<Promise>? _orderedActivePromises;
    private long _nextPromiseId = 1;

    public IReadOnlyList<Promise> Promises =>
        _orderedPromises ??= Array.AsReadOnly(
            _byId.Values.OrderBy(p => p.PromiseId.Value).ToArray());

    public long NextPromiseId => _nextPromiseId;

    public IReadOnlyList<Promise> ActivePromises =>
        _orderedActivePromises ??= Array.AsReadOnly(
            _activeIds
                .OrderBy(id => id)
                .Select(id => _byId[id])
                .ToArray());

    public Promise? Get(PromiseId promiseId) =>
        _byId.TryGetValue(promiseId.Value, out var promise) ? promise : null;

    public Promise? FindActivePlayerPromise(long playerId, ClubId clubId, PromiseKind kind) =>
        _activeByPlayerClubKind.TryGetValue(new PlayerClubKindKey(playerId, clubId, kind), out var id)
        && _byId.TryGetValue(id, out var promise)
            ? promise
            : null;

    public IReadOnlyList<Promise> GetActiveByPlayer(long playerId)
    {
        if (!_activeByPlayer.TryGetValue(playerId, out var ids) || ids.Count == 0)
        {
            return Array.Empty<Promise>();
        }

        return ids
            .OrderBy(id => id)
            .Select(id => _byId[id])
            .ToArray();
    }

    public IReadOnlyList<Promise> GetActiveByManagerAndClub(long managerId, ClubId clubId)
    {
        if (!_activeByManagerClub.TryGetValue(new ManagerClubKey(managerId, clubId), out var ids) || ids.Count == 0)
        {
            return Array.Empty<Promise>();
        }

        return ids
            .OrderBy(id => id)
            .Select(id => _byId[id])
            .ToArray();
    }

    public void Upsert(Promise promise)
    {
        ArgumentNullException.ThrowIfNull(promise);
        if (_byId.TryGetValue(promise.PromiseId.Value, out var previous))
        {
            RemoveIndexes(previous);
        }

        _byId[promise.PromiseId.Value] = promise;
        AddIndexes(promise);
        _nextPromiseId = Math.Max(_nextPromiseId, promise.PromiseId.Value + 1);
        _orderedPromises = null;
        _orderedActivePromises = null;
    }

    public void ReplaceAll(IEnumerable<Promise> promises)
    {
        ArgumentNullException.ThrowIfNull(promises);
        _byId.Clear();
        _activeIds.Clear();
        _activeByPlayerClubKind.Clear();
        _activeByPlayer.Clear();
        _activeByManagerClub.Clear();
        _nextPromiseId = 1;
        foreach (var promise in promises)
        {
            Upsert(promise);
        }

        _orderedPromises = null;
        _orderedActivePromises = null;
    }

    private void AddIndexes(Promise promise)
    {
        if (!promise.IsActive)
        {
            return;
        }

        var id = promise.PromiseId.Value;
        _activeIds.Add(id);

        if (promise.Promisee.Kind == ActorKind.Player)
        {
            var playerId = promise.Promisee.Id;
            _activeByPlayerClubKind[new PlayerClubKindKey(playerId, promise.ClubId, promise.Kind)] = id;
            if (!_activeByPlayer.TryGetValue(playerId, out var playerSet))
            {
                playerSet = new HashSet<long>();
                _activeByPlayer[playerId] = playerSet;
            }

            playerSet.Add(id);
        }

        if (promise.Promisor.Kind == ActorKind.Manager)
        {
            var mgrKey = new ManagerClubKey(promise.Promisor.Id, promise.ClubId);
            if (!_activeByManagerClub.TryGetValue(mgrKey, out var mgrSet))
            {
                mgrSet = new HashSet<long>();
                _activeByManagerClub[mgrKey] = mgrSet;
            }

            mgrSet.Add(id);
        }
    }

    private void RemoveIndexes(Promise promise)
    {
        var id = promise.PromiseId.Value;
        _activeIds.Remove(id);

        if (promise.Promisee.Kind == ActorKind.Player)
        {
            var playerId = promise.Promisee.Id;
            var key = new PlayerClubKindKey(playerId, promise.ClubId, promise.Kind);
            if (_activeByPlayerClubKind.TryGetValue(key, out var existingId) && existingId == id)
            {
                _activeByPlayerClubKind.Remove(key);
            }

            if (_activeByPlayer.TryGetValue(playerId, out var playerSet))
            {
                playerSet.Remove(id);
                if (playerSet.Count == 0)
                {
                    _activeByPlayer.Remove(playerId);
                }
            }
        }

        if (promise.Promisor.Kind == ActorKind.Manager)
        {
            var mgrKey = new ManagerClubKey(promise.Promisor.Id, promise.ClubId);
            if (_activeByManagerClub.TryGetValue(mgrKey, out var mgrSet))
            {
                mgrSet.Remove(id);
                if (mgrSet.Count == 0)
                {
                    _activeByManagerClub.Remove(mgrKey);
                }
            }
        }
    }

    private readonly record struct PlayerClubKindKey(long PlayerId, ClubId ClubId, PromiseKind Kind);

    private readonly record struct ManagerClubKey(long ManagerId, ClubId ClubId);
}
