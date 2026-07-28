using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar.Events;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;

/// <summary>
/// Transfer penceresi kapanış gözlemi — foreign state değiştirmez.
/// </summary>
public sealed class ObserveTransferWindowClosedReactionRule : IReactionRule
{
    public const string Id = "WorldCalendar.ObserveTransferWindowClosed.v1";
    public const string IntentTypeCode = "TransferWindowClosedObserved";

    public string RuleId => Id;

    public IReadOnlyList<ReactionIntent> React(
        WorldCalendarDomainEvent domainEvent,
        EvaluatedWorldCalendarEffect appliedEffect)
    {
        if (domainEvent is not TransferWindowClosed)
        {
            return Array.Empty<ReactionIntent>();
        }

        return
        [
            new ReactionIntent(
                Id,
                appliedEffect.Envelope.EventId,
                IntentTypeCode,
                appliedEffect.Envelope.OccurredAtDayNumber),
        ];
    }
}
