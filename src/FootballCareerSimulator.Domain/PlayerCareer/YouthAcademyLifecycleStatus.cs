namespace FootballCareerSimulator.Domain.PlayerCareer;

/// <summary>
/// Kabul edilmiş bir gencin akademiden profesyonel kadroya uzanan yaşam döngüsü.
/// Aday kabul/ret kararı Interaction bağlamında, profesyonel kayıt ise PlayerCareer,
/// ContractRegistration ve TeamPreparation bağlamlarında tutulur.
/// </summary>
public enum YouthAcademyLifecycleStatus
{
    Developing = 1,
    PromotionEligible = 2,
    PromotedToFirstTeam = 3,
}
