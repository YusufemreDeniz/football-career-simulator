namespace FootballCareerSimulator.Domain.EventRuleEvaluation;

/// <summary>
/// Nedensellik kategorisi / etki boyutu (docs/20 §6).
/// </summary>
public enum CausalityDimension
{
    RelationshipDelta = 1,
    PromiseOutcome = 2,
    TransferResult = 3,
    BoardConfidenceDelta = 4,
}
