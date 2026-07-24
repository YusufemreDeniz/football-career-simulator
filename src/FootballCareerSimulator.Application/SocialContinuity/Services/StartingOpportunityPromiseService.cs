using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

public sealed class StartingOpportunityPromiseService
{
    private readonly IPromiseStore _store;

    public StartingOpportunityPromiseService(IPromiseStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public Promise Create(
        ManagerId managerId,
        PlayerId playerId,
        ClubId clubId,
        int targetStarts,
        GameDate deadlineOn,
        GameDate createdOn)
    {
        var hasActive = _store.Promises.Any(p =>
            p.IsActive
            && p.Kind == PromiseKind.StartingOpportunity
            && p.Promisee.Kind == ActorKind.Player
            && p.Promisee.Id == playerId.Value
            && p.ClubId == clubId);

        if (hasActive)
        {
            throw new SocialContinuityInvariantViolationException(
                $"Player {playerId.Value} already has an active starting opportunity promise at club {clubId.Value}.");
        }

        var nextId = _store.Promises.Count == 0
            ? 1L
            : _store.Promises.Max(p => p.PromiseId.Value) + 1;

        var promise = Promise.CreateStartingOpportunity(
            new PromiseId(nextId),
            managerId,
            playerId,
            clubId,
            targetStarts,
            deadlineOn,
            createdOn);
        _store.Upsert(promise);
        return promise;
    }

    public int RecordStartsForPlayers(
        FixtureId fixtureId,
        ClubId clubId,
        IReadOnlyList<PlayerId> startingPlayerIds,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(startingPlayerIds);

        var updated = 0;
        foreach (var playerId in startingPlayerIds.Distinct())
        {
            var active = _store.Promises.FirstOrDefault(p =>
                p.IsActive
                && p.Kind == PromiseKind.StartingOpportunity
                && p.ClubId == clubId
                && p.Promisee.Kind == ActorKind.Player
                && p.Promisee.Id == playerId.Value);

            if (active is null)
            {
                continue;
            }

            var next = active.RecordStartingAppearance(fixtureId, day);
            if (!ReferenceEquals(next, active))
            {
                _store.Upsert(next);
                updated++;
            }
        }

        return updated;
    }

    public int EvaluateDeadlines(GameDate day)
    {
        var resolved = 0;
        foreach (var promise in _store.Promises.Where(p => p.IsActive).ToArray())
        {
            var next = promise.EvaluateDeadline(day);
            if (next.Status != promise.Status)
            {
                _store.Upsert(next);
                resolved++;
            }
        }

        return resolved;
    }
}
