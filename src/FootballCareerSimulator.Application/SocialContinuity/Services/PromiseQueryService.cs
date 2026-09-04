using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Application.SocialContinuity.Queries;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Promise salt-okunur sorguları. Relationship / diyalog yok.
/// </summary>
public sealed class PromiseQueryService
{
    private readonly IPromiseStore _store;

    public PromiseQueryService(IPromiseStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public ActorPromisesReadModel GetActiveForPromisor(
        ActorKind actorKind,
        long actorId,
        int take = 8)
    {
        if (take < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(take), take, "Take must be positive.");
        }

        var promisor = new ActorRef(actorKind, actorId);
        var active = _store.Promises
            .Where(p => p.IsActive && p.Promisor == promisor)
            .OrderBy(p => p.DeadlineOn.DayNumber)
            .ThenBy(p => p.PromiseId.Value)
            .ToArray();

        return new ActorPromisesReadModel(
            "Promisor",
            actorKind.ToString(),
            actorId,
            active.Length,
            active.Take(take).Select(ToLine).ToArray());
    }

    public ActorPromisesReadModel GetActiveForPromisee(
        ActorKind actorKind,
        long actorId,
        int take = 8)
    {
        if (take < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(take), take, "Take must be positive.");
        }

        var promisee = new ActorRef(actorKind, actorId);
        var active = _store.Promises
            .Where(p => p.IsActive && p.Promisee == promisee)
            .OrderBy(p => p.DeadlineOn.DayNumber)
            .ThenBy(p => p.PromiseId.Value)
            .ToArray();

        return new ActorPromisesReadModel(
            "Promisee",
            actorKind.ToString(),
            actorId,
            active.Length,
            active.Take(take).Select(ToLine).ToArray());
    }

    public ClubPromisesReadModel GetActiveForClub(ClubId clubId, int take = 8)
    {
        if (take < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(take), take, "Take must be positive.");
        }

        var active = _store.Promises
            .Where(p => p.IsActive && p.ClubId == clubId)
            .OrderBy(p => p.DeadlineOn.DayNumber)
            .ThenBy(p => p.PromiseId.Value)
            .ToArray();

        return new ClubPromisesReadModel(
            clubId.Value,
            active.Length,
            active.Take(take).Select(ToLine).ToArray());
    }

    private static PromiseLineReadModel ToLine(Promise promise) =>
        new(
            promise.PromiseId.Value,
            KindDisplayName(promise.Kind),
            StatusDisplayName(promise.Status),
            promise.Promisor.Kind.ToString(),
            promise.Promisor.Id,
            promise.Promisee.Kind.ToString(),
            promise.Promisee.Id,
            promise.ClubId.Value,
            promise.StartsGiven,
            promise.TargetStarts,
            promise.DeadlineOn.DayNumber,
            promise.CreatedOn.DayNumber,
            promise.TerminalOn?.DayNumber);

    private static string KindDisplayName(PromiseKind kind) =>
        kind switch
        {
            PromiseKind.StartingOpportunity => "İlk 11",
            PromiseKind.PlayingTime => "Oyun süresi",
            _ => kind.ToString(),
        };

    private static string StatusDisplayName(PromiseStatus status) =>
        status switch
        {
            PromiseStatus.Active => "Aktif",
            PromiseStatus.Fulfilled => "Tutuldu",
            PromiseStatus.Broken => "Bozuldu",
            PromiseStatus.Invalidated => "Geçersiz",
            PromiseStatus.Archived => "Arşiv",
            _ => status.ToString(),
        };
}
