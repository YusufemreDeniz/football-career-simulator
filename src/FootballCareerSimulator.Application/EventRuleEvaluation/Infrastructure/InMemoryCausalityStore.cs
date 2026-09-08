using FootballCareerSimulator.Application.EventRuleEvaluation.Ports;
using FootballCareerSimulator.Domain.EventRuleEvaluation;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Infrastructure;

/// <summary>
/// Bounded in-memory causality store (docs/20 §6 / D-414 / D-415).
/// Bellek şişmesini önlemek için maksimum 500 kayıt tutar; eskileri tahliye eder.
/// </summary>
public sealed class InMemoryCausalityStore : ICausalityStore
{
    public const int MaxBoundedCapacity = 500;

    private readonly object _lock = new();
    private readonly Dictionary<Guid, (CausalityRecord Record, Guid? CorrelationId, string? RuleId)> _records = new();
    private readonly Queue<Guid> _fifoOrder = new();

    public void Add(CausalityRecord record, Guid? correlationId = null, string? ruleId = null)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (_lock)
        {
            if (_records.ContainsKey(record.TargetEventId))
            {
                _records[record.TargetEventId] = (record, correlationId, ruleId);
                return;
            }

            while (_fifoOrder.Count >= MaxBoundedCapacity)
            {
                var oldestId = _fifoOrder.Dequeue();
                _records.Remove(oldestId);
            }

            _fifoOrder.Enqueue(record.TargetEventId);
            _records[record.TargetEventId] = (record, correlationId, ruleId);
        }
    }

    public CausalityRecord? FindByTargetEventId(Guid targetEventId)
    {
        lock (_lock)
        {
            return _records.TryGetValue(targetEventId, out var entry) ? entry.Record : null;
        }
    }

    public (Guid? CorrelationId, string? RuleId) GetDiagnostics(Guid targetEventId)
    {
        lock (_lock)
        {
            return _records.TryGetValue(targetEventId, out var entry)
                ? (entry.CorrelationId, entry.RuleId)
                : (null, null);
        }
    }

    public IReadOnlyList<CausalityRecord> GetRecent(int count = 50)
    {
        lock (_lock)
        {
            return _records.Values
                .OrderByDescending(v => v.Record.EvaluatedAtDayNumber)
                .Take(count)
                .Select(v => v.Record)
                .ToArray();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _records.Clear();
            _fifoOrder.Clear();
        }
    }
}
