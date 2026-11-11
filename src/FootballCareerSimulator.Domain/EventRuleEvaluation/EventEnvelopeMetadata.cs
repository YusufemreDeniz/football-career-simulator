namespace FootballCareerSimulator.Domain.EventRuleEvaluation;

/// <summary>
/// Commit edilmiş domain olayının izlenebilir üst verisi (causation/correlation).
/// </summary>
public sealed record EventEnvelopeMetadata(
    Guid EventId,
    Guid CorrelationId,
    Guid? CausationId,
    string SourceContext,
    long SimulationStepId,
    int OccurredAtDayNumber);
