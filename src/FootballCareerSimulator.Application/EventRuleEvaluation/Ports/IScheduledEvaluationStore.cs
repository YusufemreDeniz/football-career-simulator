using FootballCareerSimulator.Domain.EventRuleEvaluation;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Ports;

public interface IScheduledEvaluationStore
{
    IReadOnlyList<ScheduledEvaluation> Items { get; }

    long AllocateNextId();

    void Add(ScheduledEvaluation evaluation);

    ScheduledEvaluation? FindPending(string evaluationTypeCode, int dueDayNumber);

    IReadOnlyList<ScheduledEvaluation> GetPendingDueThrough(int dayNumber);

    void Replace(ScheduledEvaluation evaluation);

    void ReplaceAll(IEnumerable<ScheduledEvaluation> evaluations);

    void Clear();
}
