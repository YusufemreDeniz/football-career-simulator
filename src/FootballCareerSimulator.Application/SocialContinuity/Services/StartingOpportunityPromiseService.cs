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
    private readonly PromiseMemoryService? _promiseMemory;
    private readonly RelationshipEvaluationService? _relationships;

    public StartingOpportunityPromiseService(
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
        int targetStarts,
        GameDate deadlineOn,
        GameDate createdOn)
    {
        if (_store.FindActivePlayerPromise(playerId.Value, clubId, PromiseKind.StartingOpportunity) is not null)
        {
            throw new SocialContinuityInvariantViolationException(
                $"Player {playerId.Value} already has an active starting opportunity promise at club {clubId.Value}.");
        }

        var nextId = _store.NextPromiseId;

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
            var active = _store.FindActivePlayerPromise(playerId.Value, clubId, PromiseKind.StartingOpportunity);
            if (active is null)
            {
                continue;
            }

            var next = active.RecordStartingAppearance(fixtureId, day);
            if (!ReferenceEquals(next, active))
            {
                _store.Upsert(next);
                updated++;
                TryRecordMemory(next, day);
            }
        }

        return updated;
    }

    public int EvaluateDeadlines(GameDate day, Action<Promise>? onResolved = null)
    {
        var resolved = 0;
        foreach (var promise in _store.ActivePromises.ToArray())
        {
            var next = promise.EvaluateDeadline(day);
            if (next.Status != promise.Status)
            {
                _store.Upsert(next);
                resolved++;
                TryRecordMemory(next, day);
                onResolved?.Invoke(next);
            }
        }

        return resolved;
    }

    private void TryRecordMemory(Promise promise, GameDate day)
    {
        _promiseMemory?.RecordOutcome(promise, day);
        _relationships?.ApplyPromiseOutcome(promise, day);
    }
}
