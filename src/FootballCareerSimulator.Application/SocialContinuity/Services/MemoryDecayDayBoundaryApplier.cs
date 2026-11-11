using FootballCareerSimulator.Application.EventRuleEvaluation.Reactions;
using FootballCareerSimulator.Application.EventRuleEvaluation.Services;
using FootballCareerSimulator.Domain.EventRuleEvaluation;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// DayBoundaryObserved reaction → owner MemoryDecayService.ApplyDue.
/// </summary>
public sealed class MemoryDecayDayBoundaryApplier
{
    public const string ConsumerId = "SocialContinuity";
    public const string EffectType = "ApplyMemoryDecay";

    private readonly MemoryDecayService _memoryDecay;
    private readonly EventEffectIdempotencyGate _gate;

    public MemoryDecayDayBoundaryApplier(
        MemoryDecayService memoryDecay,
        EventEffectIdempotencyGate gate)
    {
        _memoryDecay = memoryDecay ?? throw new ArgumentNullException(nameof(memoryDecay));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    }

    public int ApplyFromReactions(IReadOnlyList<ReactionIntent> intents)
    {
        ArgumentNullException.ThrowIfNull(intents);

        var updated = 0;
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

            updated += _memoryDecay.ApplyDue(GameDate.FromDayNumber(intent.OccurredAtDayNumber));
        }

        return updated;
    }
}
