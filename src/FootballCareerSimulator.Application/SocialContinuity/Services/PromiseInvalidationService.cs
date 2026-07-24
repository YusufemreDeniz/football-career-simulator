using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Bağlam sona erince (transfer / dismissal / serbest kalma) aktif Promise'ları Invalidated yapar.
/// </summary>
public sealed class PromiseInvalidationService
{
    private readonly IPromiseStore _store;
    private readonly PromiseMemoryService? _promiseMemory;

    public PromiseInvalidationService(
        IPromiseStore store,
        PromiseMemoryService? promiseMemory = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _promiseMemory = promiseMemory;
    }

    public int InvalidateForPlayerLeaving(PlayerId playerId, GameDate day)
    {
        var count = 0;
        foreach (var promise in _store.Promises.Where(p =>
                     p.IsActive
                     && p.Promisee.Kind == ActorKind.Player
                     && p.Promisee.Id == playerId.Value).ToArray())
        {
            var next = promise.Invalidate(day);
            _store.Upsert(next);
            _promiseMemory?.RecordOutcome(next, day);
            count++;
        }

        return count;
    }

    public int InvalidateForManagerLeavingClub(ManagerId managerId, ClubId clubId, GameDate day)
    {
        var count = 0;
        foreach (var promise in _store.Promises.Where(p =>
                     p.IsActive
                     && p.Promisor.Kind == ActorKind.Manager
                     && p.Promisor.Id == managerId.Value
                     && p.ClubId == clubId).ToArray())
        {
            var next = promise.Invalidate(day);
            _store.Upsert(next);
            _promiseMemory?.RecordOutcome(next, day);
            count++;
        }

        return count;
    }
}
