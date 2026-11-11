using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar.Events;

namespace FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;

/// <summary>
/// İlk reaction kancası: gün sınırı gözlemi. Owner command üretmez; intent kaydı bırakır.
/// </summary>
public sealed class ObserveGameDayStartedReactionRule : IReactionRule
{
    public const string Id = "WorldCalendar.ObserveGameDayStarted.v1";
    public const string IntentTypeCode = "DayBoundaryObserved";

    public string RuleId => Id;

    public IReadOnlyList<ReactionIntent> React(
        WorldCalendarDomainEvent domainEvent,
        EvaluatedWorldCalendarEffect appliedEffect)
    {
        if (domainEvent is not GameDayStarted)
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
