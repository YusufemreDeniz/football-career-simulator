using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Interaction.Services;

/// <summary>
/// Maç sonrası kritik basın DecisionRequest tetikleyicisi (blowout mağlubiyet).
/// Gazeteci ağı yok; yalnız mevcut PressQuestion DecisionRequest açılır.
/// </summary>
public sealed class PostMatchPressDecisionTrigger
{
    public const int BlowoutGoalDifference = MemoryRecord.MatchBlowoutMinGoalDifference;

    private readonly DecisionRequestService _decisions;

    public PostMatchPressDecisionTrigger(DecisionRequestService decisions)
    {
        _decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
    }

    public DecisionRequest? TryOpenAfterManagedBlowoutLoss(
        int managedGoals,
        int opponentGoals,
        IReadOnlyList<PlayerId> startingPlayerIds,
        GameDate day)
    {
        ArgumentNullException.ThrowIfNull(startingPlayerIds);

        if (opponentGoals - managedGoals < BlowoutGoalDifference)
        {
            return null;
        }

        if (startingPlayerIds.Count == 0)
        {
            return null;
        }

        if (_decisions.HasOpenPressQuestionForManagedClub())
        {
            return null;
        }

        var subject = startingPlayerIds
            .OrderBy(id => id.Value)
            .First();

        try
        {
            return _decisions.OpenPressQuestionRequest(subject, day);
        }
        catch (InteractionInvariantViolationException)
        {
            return null;
        }
    }
}
