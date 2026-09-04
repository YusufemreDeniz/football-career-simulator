using FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Interaction.Services;

/// <summary>
/// DayBoundaryObserved reaction → owner DecisionRequestService.ExpireDue.
/// </summary>
public sealed class DecisionExpireDayBoundaryApplier
{
    public const string ConsumerId = "Interaction";
    public const string EffectType = "ExpireDueDecisions";

    private readonly DecisionRequestService _decisions;
    private readonly EventEffectIdempotencyGate _gate;

    public DecisionExpireDayBoundaryApplier(
        DecisionRequestService decisions,
        EventEffectIdempotencyGate gate)
    {
        _decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    }

    public int ApplyFromReactions(IReadOnlyList<ReactionIntent> intents)
    {
        ArgumentNullException.ThrowIfNull(intents);

        var expired = 0;
        foreach (var intent in intents
                     .Where(i => string.Equals(
                         i.IntentTypeCode,
                         ObserveGameDayStartedReactionRule.IntentTypeCode,
                         StringComparison.Ordinal))
                     .OrderBy(i => i.OccurredAtDayNumber)
                     .ThenBy(i => i.SourceEventId))
        {
            var key = EventEffectProcessingKey.ForConsumerEffect(
                ConsumerId,
                intent.SourceEventId,
                EffectType);
            if (_gate.TryApply(key) == EventEffectApplicationStatus.Duplicate)
            {
                continue;
            }

            expired += _decisions.ExpireDue(GameDate.FromDayNumber(intent.OccurredAtDayNumber));
        }

        return expired;
    }
}
