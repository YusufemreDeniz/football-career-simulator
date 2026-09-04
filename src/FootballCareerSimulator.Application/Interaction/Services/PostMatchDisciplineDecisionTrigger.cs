using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Interaction.Services;

/// <summary>
/// Maç sonrası disiplin DecisionRequest tetikleyicisi (yönetilen kulüp kırmızı kartı).
/// Yeni eşik yok; kırmızı kart zaten <see cref="Domain.Match.MatchKeyMomentKind.RedCard"/> olarak üretilir.
/// </summary>
public sealed class PostMatchDisciplineDecisionTrigger
{
    private readonly DecisionRequestService _decisions;

    public PostMatchDisciplineDecisionTrigger(DecisionRequestService decisions)
    {
        _decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
    }

    public DecisionRequest? TryOpenAfterManagedRedCards(
        IReadOnlyList<PlayerId> sentOffPlayerIds,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(sentOffPlayerIds);

        if (sentOffPlayerIds.Count == 0)
        {
            return null;
        }

        if (_decisions.HasOpenDisciplineForManagedClub())
        {
            return null;
        }

        var subject = sentOffPlayerIds
            .Distinct()
            .OrderBy(id => id.Value)
            .First();

        try
        {
            return _decisions.OpenDisciplineRequest(subject, day);
        }
        catch (InteractionInvariantViolationException)
        {
            return null;
        }
    }
}
