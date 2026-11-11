using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Interaction.Services;

/// <summary>
/// Maç sonrası yönetim talebi: board assessment risk band'i ilk kez
/// Secure/Stable → UnderReview'a düştüğünde (mevcut <see cref="EmploymentRisk.FromConfidence"/>;
/// yeni eşik yok) <see cref="DecisionRequestKind.BoardDemandRequest"/> açar.
/// Açık yönetim talebi varken veya Critical işten çıkarma sonrası açılmaz.
/// </summary>
public sealed class PostMatchBoardDemandTrigger
{
    private readonly DecisionRequestService _decisions;

    public PostMatchBoardDemandTrigger(DecisionRequestService decisions)
    {
        _decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
    }

    public DecisionRequest? TryOpenAfterRiskEscalation(
        EmploymentRiskBand previousBand,
        EmploymentRiskBand newBand,
        GameDate day)
    {
        if (!IsEscalationIntoUnderReview(previousBand, newBand))
        {
            return null;
        }

        try
        {
            return _decisions.OpenBoardDemandRequest(day);
        }
        catch (InteractionInvariantViolationException)
        {
            return null;
        }
    }

    public static bool IsEscalationIntoUnderReview(
        EmploymentRiskBand previousBand,
        EmploymentRiskBand newBand) =>
        newBand == EmploymentRiskBand.UnderReview
        && previousBand is EmploymentRiskBand.Secure or EmploymentRiskBand.Stable;
}
