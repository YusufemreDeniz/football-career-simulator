using FootballCareerSimulator.Application.EventRuleEvaluation.Ports;
using FootballCareerSimulator.Domain.EventRuleEvaluation;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Infrastructure;

public sealed class InMemoryScheduledEvaluationStore : IScheduledEvaluationStore
{
    private readonly Dictionary<long, ScheduledEvaluation> _items = new();
    private long _nextId = 1;

    public IReadOnlyList<ScheduledEvaluation> Items =>
        _items.Values
            .OrderBy(item => item.DueDayNumber)
            .ThenBy(item => item.Id.Value)
            .ToArray();

    public long AllocateNextId() => _nextId++;

    public void Add(ScheduledEvaluation evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        _items[evaluation.Id.Value] = evaluation;
        if (evaluation.Id.Value >= _nextId)
        {
            _nextId = evaluation.Id.Value + 1;
        }
    }

    public ScheduledEvaluation? FindPending(string evaluationTypeCode, int dueDayNumber) =>
        _items.Values.FirstOrDefault(item =>
            item.Status == ScheduledEvaluationStatus.Pending
            && string.Equals(item.EvaluationTypeCode, evaluationTypeCode, StringComparison.Ordinal)
            && item.DueDayNumber == dueDayNumber);

    public IReadOnlyList<ScheduledEvaluation> GetPendingDueThrough(int dayNumber) =>
        _items.Values
            .Where(item =>
                item.Status == ScheduledEvaluationStatus.Pending
                && item.DueDayNumber <= dayNumber)
            .OrderBy(item => item.DueDayNumber)
            .ThenBy(item => item.Id.Value)
            .ToArray();

    public void Replace(ScheduledEvaluation evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        _items[evaluation.Id.Value] = evaluation;
        if (evaluation.Id.Value >= _nextId)
        {
            _nextId = evaluation.Id.Value + 1;
        }
    }

    public void ReplaceAll(IEnumerable<ScheduledEvaluation> evaluations)
    {
        ArgumentNullException.ThrowIfNull(evaluations);
        _items.Clear();
        _nextId = 1;
        foreach (var evaluation in evaluations)
        {
            Add(evaluation);
        }
    }

    public void Clear()
    {
        _items.Clear();
        _nextId = 1;
    }
}
