using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Promise terminal sonucu → Promise Memory (idempotent).
/// </summary>
public sealed class PromiseMemoryService
{
    private readonly IMemoryStore _store;

    public PromiseMemoryService(IMemoryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public int RecordOutcome(Promise promise, GameDate day)
    {
        ArgumentNullException.ThrowIfNull(promise);
        if (promise.Status is not (PromiseStatus.Fulfilled or PromiseStatus.Broken))
        {
            return 0;
        }

        var created = 0;
        created += TryCreateForActor(promise.Promisee, promise, day) ? 1 : 0;
        created += TryCreateForActor(promise.Promisor, promise, day) ? 1 : 0;
        return created;
    }

    private bool TryCreateForActor(ActorRef rememberingActor, Promise promise, GameDate day)
    {
        var sourceKey = MemoryRecord.BuildPromiseOutcomeSourceKey(promise.PromiseId, promise.Status);
        var exists = _store.Memories.Any(m =>
            m.SourceEventKey == sourceKey
            && m.RememberingActor == rememberingActor
            && m.RuleId == MemoryRecord.PromiseOutcomeRuleId
            && m.RuleVersion == MemoryRecord.PromiseOutcomeRuleVersion);

        if (exists)
        {
            return false;
        }

        var nextId = _store.Memories.Count == 0
            ? 1L
            : _store.Memories.Max(m => m.MemoryId.Value) + 1;

        var memory = MemoryRecord.CreatePromiseOutcome(
            new MemoryId(nextId),
            rememberingActor,
            promise,
            day);
        _store.Upsert(memory);
        return true;
    }
}
