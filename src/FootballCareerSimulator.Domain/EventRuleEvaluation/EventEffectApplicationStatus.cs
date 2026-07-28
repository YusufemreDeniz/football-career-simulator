namespace FootballCareerSimulator.Domain.EventRuleEvaluation;

/// <summary>
/// Tek bir consumer effect başvurusunun sonucu. Tam processing ledger state makinesi değil.
/// </summary>
public enum EventEffectApplicationStatus
{
    Applied = 0,
    Duplicate = 1,
}
