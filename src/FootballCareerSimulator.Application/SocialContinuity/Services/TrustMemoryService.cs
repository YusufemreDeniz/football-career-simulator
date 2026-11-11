using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Promise Fulfilled/Broken → Trust Memory (oyuncu → menajer; idempotent).
/// Invalidated güven sinyali üretmez.
/// </summary>
public sealed class TrustMemoryService
{
    private readonly IMemoryStore _store;

    public TrustMemoryService(IMemoryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public int RecordFromPromiseOutcome(Promise promise, GameDate day)
    {
        ArgumentNullException.ThrowIfNull(promise);
        if (promise.Status is not (PromiseStatus.Fulfilled or PromiseStatus.Broken))
        {
            return 0;
        }

        var sourceKey = MemoryRecord.BuildTrustFromPromiseSourceKey(promise.PromiseId, promise.Status);
        var remembering = promise.Promisee;
        var exists = _store.Memories.Any(m =>
            m.SourceEventKey == sourceKey
            && m.RememberingActor == remembering
            && m.RuleId == MemoryRecord.TrustFromPromiseRuleId
            && m.RuleVersion == MemoryRecord.TrustFromPromiseRuleVersion);

        if (exists)
        {
            return 0;
        }

        var nextId = _store.Memories.Count == 0
            ? 1L
            : _store.Memories.Max(m => m.MemoryId.Value) + 1;

        _store.Upsert(MemoryRecord.CreateTrustFromPromiseOutcome(
            new MemoryId(nextId),
            promise,
            day));
        return 1;
    }
}
