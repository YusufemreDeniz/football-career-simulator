using FootballCareerSimulator.Domain.EventRuleEvaluation;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Queries;

/// <summary>
/// Geliştirici ve test doğrulaması için detaylı teşhis izleme kaydı (docs/20 §6 / §29).
/// </summary>
public sealed record CausalityDiagnosticTrace(
    Guid TargetEventId,
    CausalityDimension Dimension,
    string? RuleId,
    Guid? CorrelationId,
    IReadOnlyList<string> FactorCodes,
    int EvaluatedAtDayNumber);
