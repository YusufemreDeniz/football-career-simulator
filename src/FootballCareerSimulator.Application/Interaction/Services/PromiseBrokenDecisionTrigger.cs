using FootballCareerSimulator.Application.SocialContinuity.Ports;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Interaction.Services;

/// <summary>
/// Promise Broken → kriz DecisionRequest.
/// Trust Low band'deyse TransferRequest; aksi halde söz türüne göre PlayingTime/StartingOpportunity.
/// Fulfilled/Invalidated tetiklemez; aynı tür açık talep varken tekrar açmaz.
/// </summary>
public sealed class PromiseBrokenDecisionTrigger
{
    private readonly DecisionRequestService _decisions;
    private readonly IRelationshipStore? _relationships;

    public PromiseBrokenDecisionTrigger(
        DecisionRequestService decisions,
        IRelationshipStore? relationships = null)
    {
        _decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
        _relationships = relationships;
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
            if (IsTrustLow(promise))
            {
                return _decisions.OpenTransferRequest(playerId, day);
            }

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

    private bool IsTrustLow(Promise promise)
    {
        if (_relationships is null
            || promise.Promisee.Kind != ActorKind.Player
            || promise.Promisor.Kind != ActorKind.Manager)
        {
            return false;
        }

        var relationship = _relationships.FindPlayerToManager(
            promise.Promisee.Id,
            promise.Promisor.Id);
        if (relationship is null || relationship.Status != RelationshipStatus.Active)
        {
            return false;
        }

        return RelationshipDimensionBands.FromValue(relationship.Trust)
            == RelationshipDimensionBand.Low;
    }
}
