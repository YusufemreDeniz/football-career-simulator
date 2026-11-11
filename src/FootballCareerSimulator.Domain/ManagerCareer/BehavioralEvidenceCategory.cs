namespace FootballCareerSimulator.Domain.ManagerCareer;

/// <summary>
/// Menajerin davranış kanıtı kategorileri (docs/20 §7).
/// Bağımsız tarz kanıtlarıdır; tek bir "iyi/kötü" skoru oluşturulmaz.
/// </summary>
public enum BehavioralEvidenceCategory
{
    YouthOpportunity = 1,
    PromiseReliability = 2,
    PublicSupport = 3,
    NegotiationPersistence = 4,
    RotationConsistency = 5,
    DeadlineAggression = 6,
}
