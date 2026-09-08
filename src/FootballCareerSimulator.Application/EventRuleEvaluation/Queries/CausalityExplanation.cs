using FootballCareerSimulator.Domain.EventRuleEvaluation;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Queries;

/// <summary>
/// Oyuncuya yönelik semantik nedensellik açıklaması (docs/20 §6 / §29).
/// Kural ID'si veya ham puan göstermez; anlaşılır insan dili üretir.
/// </summary>
public sealed record CausalityExplanation(
    Guid TargetEventId,
    CausalityDimension Dimension,
    string Title,
    string PrimaryCause,
    IReadOnlyList<string> SupportingCauses,
    IReadOnlyList<string> MitigatingCauses,
    string? HistoricalContext,
    string Confidence,
    IReadOnlyList<string> RelatedActors,
    int OccurredAtDayNumber);
