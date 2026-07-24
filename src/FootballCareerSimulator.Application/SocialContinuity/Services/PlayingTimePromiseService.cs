using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

public sealed class PlayingTimePromiseService
{
    private readonly IPromiseStore _store;
    private readonly PromiseMemoryService? _promiseMemory;
    private readonly RelationshipEvaluationService? _relationships;

    public PlayingTimePromiseService(
        IPromiseStore store,
        PromiseMemoryService? promiseMemory = null,
        RelationshipEvaluationService? relationships = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _promiseMemory = promiseMemory;
        _relationships = relationships;
    }

    public Promise Create(
        ManagerId managerId,
        PlayerId playerId,
        ClubId clubId,
        int targetAppearances,
        GameDate deadlineOn,
        GameDate createdOn)
    {
        var hasActive = _store.Promises.Any(p =>
            p.IsActive
            && p.Kind == PromiseKind.PlayingTime
            && p.Promisee.Kind == ActorKind.Player
            && p.Promisee.Id == playerId.Value
            && p.ClubId == clubId);

        if (hasActive)
        {
            throw new SocialContinuityInvariantViolationException(
                $"Player {playerId.Value} already has an active playing time promise at club {clubId.Value}.");
        }

        var nextId = _store.Promises.Count == 0
            ? 1L
            : _store.Promises.Max(p => p.PromiseId.Value) + 1;

        var promise = Promise.CreatePlayingTime(
            new PromiseId(nextId),
            managerId,
            playerId,
            clubId,
            targetAppearances,
            deadlineOn,
            createdOn);
        _store.Upsert(promise);
        return promise;
    }

    public int RecordAppearancesForPlayers(
        FixtureId fixtureId,
        ClubId clubId,
        IReadOnlyList<PlayerId> participantPlayerIds,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(participantPlayerIds);

        var updated = 0;
        foreach (var playerId in participantPlayerIds.Distinct())
        {
            var active = _store.Promises.FirstOrDefault(p =>
                p.IsActive
                && p.Kind == PromiseKind.PlayingTime
                && p.ClubId == clubId
                && p.Promisee.Kind == ActorKind.Player
                && p.Promisee.Id == playerId.Value);

            if (active is null)
            {
                continue;
            }

            var next = active.RecordMatchAppearance(fixtureId, day);
            if (!ReferenceEquals(next, active))
            {
                _store.Upsert(next);
                updated++;
                _promiseMemory?.RecordOutcome(next, day);
                _relationships?.ApplyPromiseOutcome(next, day);
            }
        }

        return updated;
    }
}
