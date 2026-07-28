using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Interaction.Services;

/// <summary>
/// Promise Broken → kriz DecisionRequest (PlayingTime / StartingOpportunity).
/// Fulfilled/Invalidated tetiklemez; açık talep varken tekrar açmaz.
/// </summary>
public sealed class PromiseBrokenDecisionTrigger
{
    private readonly DecisionRequestService _decisions;

    public PromiseBrokenDecisionTrigger(DecisionRequestService decisions)
    {
        _decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
    }

    public DecisionRequest? TryOpenAfterBroken(Promise promise, GameDate day)
    {
        ArgumentNullException.ThrowIfNull(promise);

        if (promise.Status != PromiseStatus.Broken)
        {
            return null;
        }

        if (promise.Promisee.Kind != ActorKind.Player)
        {
            return null;
        }

        var playerId = new PlayerId(promise.Promisee.Id);

        try
        {
            return promise.Kind switch
            {
                PromiseKind.PlayingTime =>
                    _decisions.OpenPlayingTimeRequest(playerId, day),
                PromiseKind.StartingOpportunity =>
                    _decisions.OpenStartingOpportunityRequest(playerId, day),
                _ => null,
            };
        }
        catch (InteractionInvariantViolationException)
        {
            return null;
        }
    }
}
