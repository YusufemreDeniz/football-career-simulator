using FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.PlayerCareer.Services;

/// <summary>
/// DayBoundaryObserved reaction → owner PlayerCareerDevelopmentService.ApplyDueAging.
/// </summary>
public sealed class PlayerAgingDayBoundaryApplier
{
    public const string ConsumerId = "PlayerCareer";
    public const string EffectType = "ApplyDueAging";

    private readonly PlayerCareerDevelopmentService _development;
    private readonly EventEffectIdempotencyGate _gate;

    public PlayerAgingDayBoundaryApplier(
        PlayerCareerDevelopmentService development,
        EventEffectIdempotencyGate gate)
    {
        _development = development ?? throw new ArgumentNullException(nameof(development));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    }

    public int ApplyFromReactions(IReadOnlyList<ReactionIntent> intents)
    {
        ArgumentNullException.ThrowIfNull(intents);

        var aged = 0;
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

            aged += _development.ApplyDueAging(GameDate.FromDayNumber(intent.OccurredAtDayNumber));
        }

        return aged;
    }
}
