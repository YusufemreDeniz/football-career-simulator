using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Ports;

public interface IPromiseStore
{
    IReadOnlyList<Promise> Promises { get; }

    long NextPromiseId => Promises.Count == 0 ? 1L : Promises.Max(p => p.PromiseId.Value) + 1;

    IReadOnlyList<Promise> ActivePromises => Promises.Where(p => p.IsActive).ToArray();

    Promise? Get(PromiseId promiseId);

    Promise? FindActivePlayerPromise(long playerId, ClubId clubId, PromiseKind kind) =>
        Promises.FirstOrDefault(p =>
            p.IsActive
            && p.Kind == kind
            && p.ClubId == clubId
            && p.Promisee.Kind == ActorKind.Player
            && p.Promisee.Id == playerId);

    IReadOnlyList<Promise> GetActiveByPlayer(long playerId) =>
        Promises
            .Where(p => p.IsActive && p.Promisee.Kind == ActorKind.Player && p.Promisee.Id == playerId)
            .OrderBy(p => p.PromiseId.Value)
            .ToArray();

    IReadOnlyList<Promise> GetActiveByManagerAndClub(long managerId, ClubId clubId) =>
        Promises
            .Where(p =>
                p.IsActive
                && p.Promisor.Kind == ActorKind.Manager
                && p.Promisor.Id == managerId
                && p.ClubId == clubId)
            .OrderBy(p => p.PromiseId.Value)
            .ToArray();

    void Upsert(Promise promise);

    void ReplaceAll(IEnumerable<Promise> promises);
}
