namespace FootballCareerSimulator.Domain.EventRuleEvaluation;

/// <summary>
/// Nedensellik faktörünün etkisi ve hiyerarşik ağırlığı (docs/20 §6 / §29).
/// </summary>
public enum CausalFactorWeight
{
    Primary = 1,
    Contributing = 2,
    Mitigating = 3,
    Contextual = 4,
}
