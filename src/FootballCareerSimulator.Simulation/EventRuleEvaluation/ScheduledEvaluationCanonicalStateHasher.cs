using FootballCareerSimulator.Domain.EventRuleEvaluation;

namespace FootballCareerSimulator.Simulation.EventRuleEvaluation;

public static class ScheduledEvaluationCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<ScheduledEvaluation> evaluations)
    {
        ArgumentNullException.ThrowIfNull(evaluations);

        if (evaluations.Count == 0)
        {
            return "scheduledEval:0";
        }

        var parts = evaluations
            .OrderBy(item => item.Id.Value)
            .Select(item =>
                $"{item.Id.Value}:{item.EvaluationTypeCode}:{item.DueDayNumber}:{(int)item.Status}:{item.SourceEventId?.ToString("N") ?? "-"}");
        return $"scheduledEval:{evaluations.Count}:{string.Join(';', parts)}";
    }
}
