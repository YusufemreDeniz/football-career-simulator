namespace FootballCareerSimulator.Domain.EventRuleEvaluation;

/// <summary>
/// Reaction rule çıktısı — foreign state değiştirmez; owner command / scheduled evaluation adayı taşır.
/// </summary>
public sealed record ReactionIntent(
    string RuleId,
    Guid SourceEventId,
    string IntentTypeCode,
    int OccurredAtDayNumber);
