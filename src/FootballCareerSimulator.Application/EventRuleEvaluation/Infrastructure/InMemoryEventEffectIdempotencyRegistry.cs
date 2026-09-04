using FootballCareerSimulator.Application.EventRuleEvaluation.Ports;
using FootballCareerSimulator.Domain.EventRuleEvaluation;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Infrastructure;

/// <summary>
/// Effect idempotency seti — processing ledger değil; save/load ile anahtar listesi taşınır.
/// </summary>
public sealed class InMemoryEventEffectIdempotencyRegistry : IEventEffectIdempotencyRegistry
{
    private readonly HashSet<string> _keys = new(StringComparer.Ordinal);

    public int Count => _keys.Count;

    public bool Contains(EventEffectProcessingKey key) => _keys.Contains(key.Value);

    public bool TryAdd(EventEffectProcessingKey key) => _keys.Add(key.Value);

    public IReadOnlyList<string> SnapshotKeys() =>
        _keys.OrderBy(key => key, StringComparer.Ordinal).ToArray();

    public void ReplaceAll(IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        _keys.Clear();
        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _keys.Add(key);
            }
        }
    }

    public void Clear() => _keys.Clear();
}
