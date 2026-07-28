using FootballCareerSimulator.Application.EventRuleEvaluation.Ports;
using FootballCareerSimulator.Domain.EventRuleEvaluation;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Infrastructure;

/// <summary>
/// Oturum içi effect idempotency seti — SQLite/processing ledger yok.
/// </summary>
public sealed class InMemoryEventEffectIdempotencyRegistry : IEventEffectIdempotencyRegistry
{
    private readonly HashSet<string> _keys = new(StringComparer.Ordinal);

    public int Count => _keys.Count;

    public bool Contains(EventEffectProcessingKey key) => _keys.Contains(key.Value);

    public bool TryAdd(EventEffectProcessingKey key) => _keys.Add(key.Value);

    public void Clear() => _keys.Clear();
}
