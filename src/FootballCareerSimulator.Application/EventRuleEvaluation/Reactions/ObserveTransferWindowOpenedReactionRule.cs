using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar.Events;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;

/// <summary>
/// Transfer penceresi açılış gözlemi — foreign state değiştirmez.
/// </summary>
public sealed class ObserveTransferWindowOpenedReactionRule : IReactionRule
{
    public const string Id = "WorldCalendar.ObserveTransferWindowOpened.v1";
    public const string IntentTypeCode = "TransferWindowOpenedObserved";

    public string RuleId => Id;

    public IReadOnlyList<ReactionIntent> React(
        WorldCalendarDomainEvent domainEvent,
        EvaluatedWorldCalendarEffect appliedEffect)
    {
        if (domainEvent is not TransferWindowOpened)
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
