using FootballCareerSimulator.Domain.EventRuleEvaluation;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Ports;

public interface IEventEffectIdempotencyRegistry
{
    int Count { get; }

    bool Contains(EventEffectProcessingKey key);

    /// <summary>İlk kayıt true; aynı anahtar tekrar false (duplicate).</summary>
    bool TryAdd(EventEffectProcessingKey key);

    IReadOnlyList<string> SnapshotKeys();

    void ReplaceAll(IEnumerable<string> keys);

    void Clear();
}
