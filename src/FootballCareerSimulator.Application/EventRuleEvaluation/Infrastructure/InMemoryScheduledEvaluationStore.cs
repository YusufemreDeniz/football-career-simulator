using FootballCareerSimulator.Application.EventRuleEvaluation.Ports;
using FootballCareerSimulator.Domain.EventRuleEvaluation;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Infrastructure;

public sealed class InMemoryScheduledEvaluationStore : IScheduledEvaluationStore
{
    private readonly Dictionary<long, ScheduledEvaluation> _items = new();

    public IReadOnlyList<ScheduledEvaluation> Items =>
        _items.Values
            .OrderBy(item => item.DueDayNumber)
            .ThenBy(item => item.Id.Value)
            .ToArray();

    public void Add(ScheduledEvaluation evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        _items[evaluation.Id.Value] = evaluation;
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
    }

    public void Clear() => _items.Clear();
}
