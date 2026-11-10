using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.SocialContinuity;

namespace FootballCareerSimulator.Application.SocialContinuity.Infrastructure;

public sealed class InMemoryMemoryStore : IMemoryStore
{
    private readonly Dictionary<long, MemoryRecord> _byId = new();
    private readonly Dictionary<ActorRef, HashSet<long>> _idsByActor = new();
    private readonly Dictionary<ActorRuleKey, HashSet<long>> _idsByActorRule = new();
    private readonly HashSet<SourceRuleKey> _sourceEvents = new();
    private readonly HashSet<ActorSourceKey> _reinforcementEvents = new();
    private IReadOnlyList<MemoryRecord>? _orderedMemories;
    private long _nextMemoryId = 1;

    public IReadOnlyList<MemoryRecord> Memories =>
        _orderedMemories ??= Array.AsReadOnly(
            _byId.Values.OrderBy(memory => memory.MemoryId.Value).ToArray());

    public long NextMemoryId => _nextMemoryId;

    public MemoryRecord? Get(MemoryId memoryId) =>
        _byId.TryGetValue(memoryId.Value, out var memory) ? memory : null;

    public bool WasProcessed(
        ActorRef rememberingActor,
        string sourceEventKey,
        string ruleId,
        int ruleVersion) =>
        _sourceEvents.Contains(new SourceRuleKey(
            rememberingActor,
            sourceEventKey,
            ruleId,
            ruleVersion))
        || _reinforcementEvents.Contains(new ActorSourceKey(rememberingActor, sourceEventKey));

    public MemoryRecord? FindLatestActiveOrDormant(
        ActorRef rememberingActor,
        string ruleId,
        int ruleVersion)
    {
        var key = new ActorRuleKey(rememberingActor, ruleId, ruleVersion);
        if (!_idsByActorRule.TryGetValue(key, out var ids))
        {
            return null;
        }

        MemoryRecord? latest = null;
        foreach (var id in ids)
        {
            var candidate = _byId[id];
            if (candidate.Status is not (MemoryStatus.Active or MemoryStatus.Dormant))
            {
                continue;
            }

            if (latest is null
                || candidate.LastReinforcedOn.DayNumber > latest.LastReinforcedOn.DayNumber
                || (candidate.LastReinforcedOn == latest.LastReinforcedOn
                    && candidate.MemoryId.Value > latest.MemoryId.Value))
            {
                latest = candidate;
            }
        }

        return latest;
    }

    public IReadOnlyList<MemoryRecord> GetByRememberingActor(ActorRef rememberingActor)
    {
        if (!_idsByActor.TryGetValue(rememberingActor, out var ids) || ids.Count == 0)
        {
            return Array.Empty<MemoryRecord>();
        }

        var result = new MemoryRecord[ids.Count];
        var index = 0;
        foreach (var id in ids)
        {
            result[index++] = _byId[id];
        }

        return result;
    }

    public void Upsert(MemoryRecord memory)
    {
        ArgumentNullException.ThrowIfNull(memory);
        if (_byId.TryGetValue(memory.MemoryId.Value, out var previous))
        {
            RemoveIndexes(previous);
        }

        _byId[memory.MemoryId.Value] = memory;
        AddIndexes(memory);
        _nextMemoryId = Math.Max(_nextMemoryId, memory.MemoryId.Value + 1);
        _orderedMemories = null;
    }

    public void ReplaceAll(IEnumerable<MemoryRecord> memories)
    {
        ArgumentNullException.ThrowIfNull(memories);
        _byId.Clear();
        _idsByActor.Clear();
        _idsByActorRule.Clear();
        _sourceEvents.Clear();
        _reinforcementEvents.Clear();
        _nextMemoryId = 1;
        foreach (var memory in memories)
        {
            Upsert(memory);
        }

        _orderedMemories = null;
    }

    private void AddIndexes(MemoryRecord memory)
    {
        if (!_idsByActor.TryGetValue(memory.RememberingActor, out var actorIds))
        {
            actorIds = new HashSet<long>();
            _idsByActor[memory.RememberingActor] = actorIds;
        }

        actorIds.Add(memory.MemoryId.Value);

        var actorRule = new ActorRuleKey(memory.RememberingActor, memory.RuleId, memory.RuleVersion);
        if (!_idsByActorRule.TryGetValue(actorRule, out var ids))
        {
            ids = new HashSet<long>();
            _idsByActorRule[actorRule] = ids;
        }

        ids.Add(memory.MemoryId.Value);
        _sourceEvents.Add(new SourceRuleKey(
            memory.RememberingActor,
            memory.SourceEventKey,
            memory.RuleId,
            memory.RuleVersion));
        foreach (var sourceKey in memory.ProcessedReinforcementKeys)
        {
            _reinforcementEvents.Add(new ActorSourceKey(memory.RememberingActor, sourceKey));
        }
    }

    private void RemoveIndexes(MemoryRecord memory)
    {
        if (_idsByActor.TryGetValue(memory.RememberingActor, out var actorIds))
        {
            actorIds.Remove(memory.MemoryId.Value);
            if (actorIds.Count == 0)
            {
                _idsByActor.Remove(memory.RememberingActor);
            }
        }

        var actorRule = new ActorRuleKey(memory.RememberingActor, memory.RuleId, memory.RuleVersion);
        if (_idsByActorRule.TryGetValue(actorRule, out var ids))
        {
            ids.Remove(memory.MemoryId.Value);
            if (ids.Count == 0)
            {
                _idsByActorRule.Remove(actorRule);
            }
        }

        _sourceEvents.Remove(new SourceRuleKey(
            memory.RememberingActor,
            memory.SourceEventKey,
            memory.RuleId,
            memory.RuleVersion));
        foreach (var sourceKey in memory.ProcessedReinforcementKeys)
        {
            _reinforcementEvents.Remove(new ActorSourceKey(memory.RememberingActor, sourceKey));
        }
    }

    private readonly record struct ActorRuleKey(ActorRef Actor, string RuleId, int RuleVersion);

    private readonly record struct SourceRuleKey(
        ActorRef Actor,
        string SourceEventKey,
        string RuleId,
        int RuleVersion);

    private readonly record struct ActorSourceKey(ActorRef Actor, string SourceEventKey);
}
